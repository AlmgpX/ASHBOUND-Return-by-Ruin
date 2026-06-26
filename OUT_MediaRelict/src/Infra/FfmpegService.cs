using System.Globalization;
using System.Text.RegularExpressions;
using MediaRelic.Domain;

namespace MediaRelic.Infra;

public sealed class FfmpegService
{
    private static readonly Regex SilenceStartRegex =
        new(@"silence_start:\s*(?<value>[0-9]+(?:\.[0-9]+)?)", RegexOptions.Compiled);

    private static readonly Regex SilenceEndRegex =
        new(@"silence_end:\s*(?<end>[0-9]+(?:\.[0-9]+)?).*silence_duration:\s*(?<duration>[0-9]+(?:\.[0-9]+)?)",
            RegexOptions.Compiled);

    private readonly string _ffmpegPath;
    private readonly string? _ffprobePath;

    public FfmpegService(string ffmpegPath, string? ffprobePath)
    {
        _ffmpegPath = ffmpegPath;
        _ffprobePath = ffprobePath;
    }

    public async Task<double> ProbeDurationAsync(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_ffprobePath))
            return 0.0;

        var result = await ProcessRunner.RunTextAsync(
            _ffprobePath,
            new[]
            {
                "-v", "error",
                "-show_entries", "format=duration",
                "-of", "default=noprint_wrappers=1:nokey=1",
                path
            },
            cancellationToken);

        if (double.TryParse(
                result.StdOut.Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var duration))
            return Math.Max(0.0, duration);

        return 0.0;
    }

    public async Task<List<SilenceHit>> DetectSilenceAsync(
        string path,
        double noiseDb,
        double minDuration,
        CancellationToken cancellationToken)
    {
        var noise = noiseDb.ToString("0.###", CultureInfo.InvariantCulture) + "dB";
        var duration = minDuration.ToString("0.###", CultureInfo.InvariantCulture);

        var result = await ProcessRunner.RunTextAsync(
            _ffmpegPath,
            new[]
            {
                "-hide_banner",
                "-nostdin",
                "-i", path,
                "-af", $"silencedetect=n={noise}:d={duration}",
                "-f", "null",
                "-"
            },
            cancellationToken);

        return ParseSilence(result.StdErr);
    }

    public async Task ExportSegmentsAsync(
        string inputPath,
        IReadOnlyList<TimeRange> ranges,
        string outputDirectory,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory);

        var baseName = Path.GetFileNameWithoutExtension(inputPath);

        for (var i = 0; i < ranges.Count; i++)
        {
            var range = ranges[i];
            var output = Path.Combine(outputDirectory, $"{baseName}_relic_{i + 1:000}.wav");

            progress?.Report($"EXPORT {i + 1}/{ranges.Count}: {Path.GetFileName(output)}");

            await ProcessRunner.RunTextAsync(
                _ffmpegPath,
                new[]
                {
                    "-hide_banner",
                    "-nostdin",
                    "-y",
                    "-ss", ToStamp(range.Start),
                    "-to", ToStamp(range.End),
                    "-i", inputPath,
                    "-vn",
                    "-c:a", "pcm_s16le",
                    output
                },
                cancellationToken);
        }
    }

    private static List<SilenceHit> ParseSilence(string log)
    {
        var result = new List<SilenceHit>();
        double? currentStart = null;

        foreach (var rawLine in log.Split('\n'))
        {
            var line = rawLine.Trim();

            var start = SilenceStartRegex.Match(line);
            if (start.Success)
            {
                currentStart = ParseDouble(start.Groups["value"].Value);
                continue;
            }

            var end = SilenceEndRegex.Match(line);
            if (!end.Success)
                continue;

            var endValue = ParseDouble(end.Groups["end"].Value);

            if (currentStart.HasValue && endValue >= currentStart.Value)
                result.Add(new SilenceHit(currentStart.Value, endValue));

            currentStart = null;
        }

        return result;
    }

    private static double ParseDouble(string value)
    {
        return double.Parse(value, CultureInfo.InvariantCulture);
    }

    public static string ToStamp(double seconds)
    {
        if (seconds < 0)
            seconds = 0;

        var span = TimeSpan.FromSeconds(seconds);
        return span.ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture);
    }
}
