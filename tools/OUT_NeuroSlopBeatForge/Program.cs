using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace OUT.NeuroSlopBeatForge;

internal static class Program
{
    private static readonly string[] SupportedImages = [".png", ".jpg", ".jpeg", ".webp", ".bmp"];

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        try
        {
            var config = Config.Parse(args);
            if (config.ShowHelp)
            {
                PrintHelp();
                return 0;
            }

            config.Validate();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(config.OutputPath)) ?? ".");

            var images = LoadImages(config.ImagesDirectory, config.Shuffle, config.Seed);
            Console.WriteLine("OUT NeuroSlop Beat Forge");
            Console.WriteLine($"Audio:  {config.AudioPath}");
            Console.WriteLine($"Images: {images.Count}");
            Console.WriteLine($"Output: {config.OutputPath}");

            var duration = config.DurationSeconds > 0
                ? config.DurationSeconds
                : await FfmpegProbe.GetDurationSecondsAsync(config.FfprobePath, config.AudioPath);

            if (duration <= 0)
            {
                throw new InvalidOperationException("FFprobe could not detect audio duration. Use --duration to override it.");
            }

            Console.WriteLine($"Duration: {duration.ToString("0.00", CultureInfo.InvariantCulture)} sec");

            var beatTimes = await BeatDetector.DetectBeatTimesAsync(
                config.FfmpegPath,
                config.AudioPath,
                duration,
                config.BeatThreshold,
                config.MinBeatGapSeconds);

            if (beatTimes.Count < Math.Max(4, duration / 4))
            {
                Console.WriteLine("Beat detector found too few rhythm points. Falling back to fixed cuts.");
                beatTimes = BeatDetector.BuildFixedCuts(duration, config.FallbackCutSeconds);
            }

            beatTimes = BeatDetector.NormalizeTimeline(beatTimes, duration, config.MinSegmentSeconds);
            Console.WriteLine($"Cuts: {Math.Max(0, beatTimes.Count - 1)}");

            var tempRoot = Path.Combine(Path.GetTempPath(), "OUT_NeuroSlopBeatForge_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            try
            {
                var concatPath = Path.Combine(tempRoot, "image_timeline.ffconcat");
                var textOverlayPath = await ResolveTextOverlayAsync(config, tempRoot);

                await TimelineWriter.WriteConcatFileAsync(concatPath, images, beatTimes);

                var ffmpegArgs = FfmpegRender.BuildRenderArgs(config, concatPath, textOverlayPath, duration);

                Console.WriteLine();
                Console.WriteLine("FFmpeg command:");
                Console.WriteLine(ProcessRunner.ToCommandLine(config.FfmpegPath, ffmpegArgs));
                Console.WriteLine();

                if (config.DryRun)
                {
                    Console.WriteLine("Dry run enabled. Video was not rendered.");
                    return 0;
                }

                await ProcessRunner.RunAsync(config.FfmpegPath, ffmpegArgs);

                Console.WriteLine();
                Console.WriteLine($"Done: {Path.GetFullPath(config.OutputPath)}");
                return 0;
            }
            finally
            {
                if (config.KeepTemp)
                {
                    Console.WriteLine($"Temp kept: {tempRoot}");
                }
                else
                {
                    TryDeleteDirectory(tempRoot);
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("OUT NeuroSlop Beat Forge failed.");
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static List<string> LoadImages(string directory, bool shuffle, int? seed)
    {
        var root = Path.GetFullPath(directory);
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException($"Images directory not found: {root}");
        }

        var images = Directory
            .EnumerateFiles(root, "*.*", SearchOption.TopDirectoryOnly)
            .Where(path => SupportedImages.Contains(Path.GetExtension(path).ToLowerInvariant()))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (images.Count == 0)
        {
            throw new InvalidOperationException($"No images found in {root}. Supported: {string.Join(", ", SupportedImages)}");
        }

        if (shuffle)
        {
            var random = seed.HasValue ? new Random(seed.Value) : new Random();
            for (var i = images.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (images[i], images[j]) = (images[j], images[i]);
            }
        }

        return images;
    }

    private static async Task<string?> ResolveTextOverlayAsync(Config config, string tempRoot)
    {
        if (!string.IsNullOrWhiteSpace(config.TextFilePath))
        {
            var fullPath = Path.GetFullPath(config.TextFilePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("Text file not found.", fullPath);
            }
            return fullPath;
        }

        if (string.IsNullOrWhiteSpace(config.Text))
        {
            return null;
        }

        var textPath = Path.Combine(tempRoot, "overlay.txt");
        await File.WriteAllTextAsync(textPath, config.Text, new UTF8Encoding(false));
        return textPath;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Ignore temp cleanup failure.
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
OUT NeuroSlop Beat Forge

Required:
  --audio <file>        Audio track.
  --images <folder>    Folder with generated images.
  --out <file>         Output video path.

Optional:
  --ffmpeg <file>      Path to ffmpeg. Default: ffmpeg from PATH.
  --ffprobe <file>     Path to ffprobe. Default: ffprobe from PATH.
  --width <int>        Output width. Default: 1920.
  --height <int>       Output height. Default: 1080.
  --fps <int>          Output FPS. Default: 30.
  --threshold <float>  Beat sensitivity. Lower means more cuts. Default: 1.35.
  --min-gap <float>    Minimum seconds between beat cuts. Default: 0.22.
  --fallback-cut <s>   Fixed cut length if beat detection is too weak. Default: 0.75.
  --motion <float>     Movement overscale. Default: 1.10.
  --text <text>        Draw UTF-8 text over video.
  --text-file <file>   Draw text from UTF-8 file.
  --font-file <file>   Font for drawtext. Useful for Cyrillic.
  --font-size <int>    Text size. Default: 54.
  --shuffle            Shuffle image order.
  --seed <int>         Stable shuffle seed.
  --duration <float>   Override audio duration in seconds.
  --dry-run            Print FFmpeg command without rendering.
  --keep-temp          Keep temporary concat/text files.
""");
    }
}

internal sealed class Config
{
    public bool ShowHelp { get; private set; }
    public string AudioPath { get; private set; } = "";
    public string ImagesDirectory { get; private set; } = "";
    public string OutputPath { get; private set; } = "";
    public string FfmpegPath { get; private set; } = "ffmpeg";
    public string FfprobePath { get; private set; } = "ffprobe";
    public int Width { get; private set; } = 1920;
    public int Height { get; private set; } = 1080;
    public int Fps { get; private set; } = 30;
    public double BeatThreshold { get; private set; } = 1.35;
    public double MinBeatGapSeconds { get; private set; } = 0.22;
    public double MinSegmentSeconds { get; private set; } = 0.08;
    public double FallbackCutSeconds { get; private set; } = 0.75;
    public double DurationSeconds { get; private set; }
    public double MotionScale { get; private set; } = 1.10;
    public string? Text { get; private set; }
    public string? TextFilePath { get; private set; }
    public string? FontFilePath { get; private set; }
    public int FontSize { get; private set; } = 54;
    public bool Shuffle { get; private set; }
    public int? Seed { get; private set; }
    public bool DryRun { get; private set; }
    public bool KeepTemp { get; private set; }

    public static Config Parse(string[] args)
    {
        var config = new Config();
        for (var i = 0; i < args.Length; i++)
        {
            var token = args[i];
            switch (token)
            {
                case "-h":
                case "--help": config.ShowHelp = true; break;
                case "--audio": config.AudioPath = RequiredValue(args, ref i, token); break;
                case "--images": config.ImagesDirectory = RequiredValue(args, ref i, token); break;
                case "--out": config.OutputPath = RequiredValue(args, ref i, token); break;
                case "--ffmpeg": config.FfmpegPath = RequiredValue(args, ref i, token); break;
                case "--ffprobe": config.FfprobePath = RequiredValue(args, ref i, token); break;
                case "--width": config.Width = ParseInt(RequiredValue(args, ref i, token), token); break;
                case "--height": config.Height = ParseInt(RequiredValue(args, ref i, token), token); break;
                case "--fps": config.Fps = ParseInt(RequiredValue(args, ref i, token), token); break;
                case "--threshold": config.BeatThreshold = ParseDouble(RequiredValue(args, ref i, token), token); break;
                case "--min-gap": config.MinBeatGapSeconds = ParseDouble(RequiredValue(args, ref i, token), token); break;
                case "--min-segment": config.MinSegmentSeconds = ParseDouble(RequiredValue(args, ref i, token), token); break;
                case "--fallback-cut": config.FallbackCutSeconds = ParseDouble(RequiredValue(args, ref i, token), token); break;
                case "--duration": config.DurationSeconds = ParseDouble(RequiredValue(args, ref i, token), token); break;
                case "--motion": config.MotionScale = ParseDouble(RequiredValue(args, ref i, token), token); break;
                case "--text": config.Text = RequiredValue(args, ref i, token); break;
                case "--text-file": config.TextFilePath = RequiredValue(args, ref i, token); break;
                case "--font-file": config.FontFilePath = RequiredValue(args, ref i, token); break;
                case "--font-size": config.FontSize = ParseInt(RequiredValue(args, ref i, token), token); break;
                case "--shuffle": config.Shuffle = true; break;
                case "--seed": config.Seed = ParseInt(RequiredValue(args, ref i, token), token); break;
                case "--dry-run": config.DryRun = true; break;
                case "--keep-temp": config.KeepTemp = true; break;
                default: throw new ArgumentException($"Unknown argument: {token}");
            }
        }
        return config;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AudioPath)) throw new ArgumentException("Missing --audio <file>.");
        if (!File.Exists(AudioPath)) throw new FileNotFoundException("Audio file not found.", AudioPath);
        if (string.IsNullOrWhiteSpace(ImagesDirectory)) throw new ArgumentException("Missing --images <folder>.");
        if (string.IsNullOrWhiteSpace(OutputPath)) throw new ArgumentException("Missing --out <file>.");
        if (!string.IsNullOrWhiteSpace(TextFilePath) && !string.IsNullOrWhiteSpace(Text)) throw new ArgumentException("Use either --text or --text-file, not both.");
        if (!string.IsNullOrWhiteSpace(FontFilePath) && !File.Exists(FontFilePath)) throw new FileNotFoundException("Font file not found.", FontFilePath);
        if (Width <= 0 || Height <= 0) throw new ArgumentOutOfRangeException(nameof(Width));
        if (Fps <= 0 || Fps > 240) throw new ArgumentOutOfRangeException(nameof(Fps));
        if (BeatThreshold <= 0) throw new ArgumentOutOfRangeException(nameof(BeatThreshold));
        if (MinBeatGapSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(MinBeatGapSeconds));
        if (FallbackCutSeconds <= 0.05) throw new ArgumentOutOfRangeException(nameof(FallbackCutSeconds));
        if (MotionScale < 1.0 || MotionScale > 1.5) throw new ArgumentOutOfRangeException(nameof(MotionScale));
        if (FontSize <= 0) throw new ArgumentOutOfRangeException(nameof(FontSize));
    }

    private static string RequiredValue(string[] args, ref int index, string optionName)
    {
        if (index + 1 >= args.Length) throw new ArgumentException($"Missing value after {optionName}.");
        return args[++index];
    }

    private static int ParseInt(string value, string optionName)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)) throw new ArgumentException($"{optionName} expects integer value.");
        return result;
    }

    private static double ParseDouble(string value, string optionName)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)) throw new ArgumentException($"{optionName} expects floating point value. Use dot as decimal separator.");
        return result;
    }
}

internal static class FfmpegProbe
{
    public static async Task<double> GetDurationSecondsAsync(string ffprobePath, string audioPath)
    {
        var output = await ProcessRunner.CaptureStdoutAsync(ffprobePath,
        [
            "-v", "error",
            "-show_entries", "format=duration",
            "-of", "default=noprint_wrappers=1:nokey=1",
            audioPath
        ]);
        var firstLine = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return double.TryParse(firstLine, NumberStyles.Float, CultureInfo.InvariantCulture, out var duration) ? duration : 0;
    }
}

internal static class BeatDetector
{
    private const int SampleRate = 8000;
    private const double WindowSeconds = 0.05;

    public static async Task<List<double>> DetectBeatTimesAsync(string ffmpegPath, string audioPath, double durationSeconds, double threshold, double minGapSeconds)
    {
        var energies = await ExtractWindowEnergiesAsync(ffmpegPath, audioPath);
        if (energies.Count < 8) return BuildFixedCuts(durationSeconds, 0.75);

        var flux = new double[energies.Count];
        for (var i = 1; i < energies.Count; i++) flux[i] = Math.Max(0, energies[i] - energies[i - 1]);

        var globalEnergy = energies.Average();
        var beats = new List<double>();
        var lastBeat = -minGapSeconds;
        const int history = 12;

        for (var i = history + 1; i < flux.Length - 1; i++)
        {
            var localMean = 0.000000001;
            for (var j = i - history; j < i; j++) localMean += flux[j];
            localMean /= history;

            var isLocalPeak = flux[i] >= flux[i - 1] && flux[i] >= flux[i + 1];
            var isAboveAdaptiveNoise = flux[i] > localMean * threshold;
            var isAudibleEnough = energies[i] > globalEnergy * 0.55;
            var time = i * WindowSeconds;

            if (isLocalPeak && isAboveAdaptiveNoise && isAudibleEnough && time - lastBeat >= minGapSeconds && time < durationSeconds - 0.05)
            {
                beats.Add(time);
                lastBeat = time;
            }
        }
        return beats;
    }

    public static List<double> NormalizeTimeline(List<double> beatTimes, double durationSeconds, double minSegmentSeconds)
    {
        var timeline = beatTimes.Where(t => t > 0 && t < durationSeconds).OrderBy(t => t).ToList();
        timeline.Insert(0, 0);
        var compact = new List<double> { timeline[0] };
        foreach (var t in timeline.Skip(1)) if (t - compact[^1] >= minSegmentSeconds) compact.Add(t);
        if (durationSeconds - compact[^1] >= minSegmentSeconds) compact.Add(durationSeconds); else compact[^1] = durationSeconds;
        return compact;
    }

    public static List<double> BuildFixedCuts(double durationSeconds, double intervalSeconds)
    {
        var cuts = new List<double>();
        for (var t = 0.0; t < durationSeconds; t += intervalSeconds) cuts.Add(t);
        cuts.Add(durationSeconds);
        return cuts;
    }

    private static async Task<List<double>> ExtractWindowEnergiesAsync(string ffmpegPath, string audioPath)
    {
        var startInfo = new ProcessStartInfo { FileName = ffmpegPath, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var arg in new[] { "-nostdin", "-hide_banner", "-loglevel", "error", "-i", audioPath, "-vn", "-ac", "1", "-ar", SampleRate.ToString(CultureInfo.InvariantCulture), "-f", "s16le", "-acodec", "pcm_s16le", "pipe:1" }) startInfo.ArgumentList.Add(arg);

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start FFmpeg for audio analysis.");
        var stderrTask = process.StandardError.ReadToEndAsync();
        var energies = new List<double>(capacity: 4096);
        var buffer = new byte[64 * 1024];
        var windowSamples = Math.Max(1, (int)(SampleRate * WindowSeconds));
        long sumSquares = 0;
        var samplesInWindow = 0;
        byte? danglingByte = null;

        while (true)
        {
            var read = await process.StandardOutput.BaseStream.ReadAsync(buffer);
            if (read <= 0) break;
            var offset = 0;
            if (danglingByte.HasValue)
            {
                var sample = unchecked((short)(danglingByte.Value | (buffer[0] << 8)));
                AddSample(sample);
                danglingByte = null;
                offset = 1;
            }
            for (var i = offset; i + 1 < read; i += 2)
            {
                var sample = unchecked((short)(buffer[i] | (buffer[i + 1] << 8)));
                AddSample(sample);
            }
            if (((read - offset) & 1) == 1) danglingByte = buffer[read - 1];
        }

        if (samplesInWindow > 0) energies.Add(Math.Sqrt(sumSquares / (double)samplesInWindow));
        await process.WaitForExitAsync();
        var stderr = await stderrTask;
        if (process.ExitCode != 0) throw new InvalidOperationException($"FFmpeg audio analysis failed:\n{stderr}");
        return energies;

        void AddSample(short sample)
        {
            sumSquares += (long)sample * sample;
            samplesInWindow++;
            if (samplesInWindow >= windowSamples)
            {
                energies.Add(Math.Sqrt(sumSquares / (double)samplesInWindow));
                sumSquares = 0;
                samplesInWindow = 0;
            }
        }
    }
}

internal static class TimelineWriter
{
    public static async Task WriteConcatFileAsync(string concatPath, IReadOnlyList<string> images, IReadOnlyList<double> timeline)
    {
        if (timeline.Count < 2) throw new ArgumentException("Timeline must contain at least start and end time.");
        var builder = new StringBuilder();
        var imageIndex = 0;
        string? lastImage = null;

        for (var i = 0; i < timeline.Count - 1; i++)
        {
            var segmentDuration = timeline[i + 1] - timeline[i];
            if (segmentDuration <= 0) continue;
            var image = images[imageIndex % images.Count];
            imageIndex++;
            lastImage = image;
            builder.Append("file '").Append(EscapeConcatPath(image)).AppendLine("'");
            builder.Append("duration ").AppendLine(segmentDuration.ToString("0.000000", CultureInfo.InvariantCulture));
        }

        if (lastImage is not null) builder.Append("file '").Append(EscapeConcatPath(lastImage)).AppendLine("'");
        await File.WriteAllTextAsync(concatPath, builder.ToString(), new UTF8Encoding(false));
    }

    private static string EscapeConcatPath(string path) => Path.GetFullPath(path).Replace("\\", "/").Replace("'", "'\\''");
}

internal static class FfmpegRender
{
    public static List<string> BuildRenderArgs(Config config, string concatPath, string? textOverlayPath, double durationSeconds)
    {
        return
        [
            "-y", "-hide_banner", "-f", "concat", "-safe", "0", "-i", concatPath, "-i", config.AudioPath,
            "-map", "0:v:0", "-map", "1:a:0", "-vf", BuildVideoFilter(config, textOverlayPath),
            "-r", config.Fps.ToString(CultureInfo.InvariantCulture), "-t", durationSeconds.ToString("0.000", CultureInfo.InvariantCulture),
            "-c:v", "libx264", "-preset", "veryfast", "-crf", "18", "-pix_fmt", "yuv420p",
            "-c:a", "aac", "-b:a", "192k", "-shortest", "-movflags", "+faststart", config.OutputPath
        ];
    }

    private static string BuildVideoFilter(Config config, string? textOverlayPath)
    {
        var width = config.Width.ToString(CultureInfo.InvariantCulture);
        var height = config.Height.ToString(CultureInfo.InvariantCulture);
        var fps = config.Fps.ToString(CultureInfo.InvariantCulture);
        var overscale = config.MotionScale.ToString("0.000", CultureInfo.InvariantCulture);
        var scaledWidth = $"ceil({width}*{overscale}/2)*2";
        var scaledHeight = $"ceil({height}*{overscale}/2)*2";

        var filter = string.Join(",",
        [
            $"scale=w='{scaledWidth}':h='{scaledHeight}':force_original_aspect_ratio=increase:eval=frame",
            $"crop={width}:{height}:x='(iw-ow)*(0.5+0.5*sin(t*0.45))':y='(ih-oh)*(0.5+0.5*cos(t*0.33))'",
            "setsar=1",
            $"fps={fps}"
        ]);

        if (!string.IsNullOrWhiteSpace(textOverlayPath)) filter += "," + BuildDrawTextFilter(config, textOverlayPath);
        return filter;
    }

    private static string BuildDrawTextFilter(Config config, string textOverlayPath)
    {
        var parts = new List<string>
        {
            "drawtext",
            $"textfile='{EscapeFilterPath(textOverlayPath)}'",
            "fontcolor=white",
            $"fontsize={config.FontSize.ToString(CultureInfo.InvariantCulture)}",
            "borderw=3",
            "bordercolor=black",
            "x=(w-text_w)/2",
            "y=h-text_h-72",
            "box=1",
            "boxcolor=black@0.45",
            "boxborderw=24"
        };
        if (!string.IsNullOrWhiteSpace(config.FontFilePath)) parts.Insert(1, $"fontfile='{EscapeFilterPath(config.FontFilePath)}'");
        return string.Join(":", parts);
    }

    private static string EscapeFilterPath(string path) => Path.GetFullPath(path).Replace("\\", "/").Replace(":", "\\:").Replace("'", "\\'");
}

internal static class ProcessRunner
{
    public static async Task<string> CaptureStdoutAsync(string fileName, IReadOnlyList<string> args)
    {
        var startInfo = BuildStartInfo(fileName, args);
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start process: {fileName}");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        if (process.ExitCode != 0) throw new InvalidOperationException($"{fileName} failed with exit code {process.ExitCode}:\n{stderr}");
        return stdout;
    }

    public static async Task RunAsync(string fileName, IReadOnlyList<string> args)
    {
        var startInfo = BuildStartInfo(fileName, args);
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start process: {fileName}");
        var stdoutTask = PipeAsync(process.StandardOutput, Console.Out);
        var stderrTask = PipeAsync(process.StandardError, Console.Error);
        await process.WaitForExitAsync();
        await Task.WhenAll(stdoutTask, stderrTask);
        if (process.ExitCode != 0) throw new InvalidOperationException($"{fileName} failed with exit code {process.ExitCode}.");
    }

    public static string ToCommandLine(string fileName, IReadOnlyList<string> args) => Quote(fileName) + " " + string.Join(" ", args.Select(Quote));

    private static ProcessStartInfo BuildStartInfo(string fileName, IReadOnlyList<string> args)
    {
        var startInfo = new ProcessStartInfo { FileName = fileName, UseShellExecute = false };
        foreach (var arg in args) startInfo.ArgumentList.Add(arg);
        return startInfo;
    }

    private static async Task PipeAsync(StreamReader reader, TextWriter writer)
    {
        while (await reader.ReadLineAsync() is { } line) await writer.WriteLineAsync(line);
    }

    private static string Quote(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "\"\"";
        return value.Any(char.IsWhiteSpace) || value.Contains('"') ? "\"" + value.Replace("\"", "\\\"") + "\"" : value;
    }
}
