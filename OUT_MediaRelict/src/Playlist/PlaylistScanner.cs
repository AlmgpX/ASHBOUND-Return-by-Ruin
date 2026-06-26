using MediaRelic.Infra;

namespace MediaRelic.Playlist;

public sealed class PlaylistScanner
{
    private readonly FfmpegService _ffmpeg;
    private readonly double _minimumDurationSeconds;
    private readonly HashSet<string> _mediaExtensions;

    public PlaylistScanner(
        FfmpegService ffmpeg,
        double minimumDurationSeconds,
        IEnumerable<string> mediaExtensions)
    {
        _ffmpeg = ffmpeg;
        _minimumDurationSeconds = minimumDurationSeconds;
        _mediaExtensions = new HashSet<string>(mediaExtensions, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<PlaylistScanResult> ScanFolderAsync(
        string folderPath,
        CancellationToken cancellationToken)
    {
        var results = new List<MediaItem>();
        var skippedShort = 0;
        var skippedBroken = 0;

        var files = Directory
            .EnumerateFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(IsSupportedMediaPath)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            double duration;

            try
            {
                duration = await _ffmpeg.ProbeDurationAsync(file, cancellationToken);
            }
            catch
            {
                skippedBroken++;
                results.Add(new MediaItem(file, Path.GetFileName(file), 0.0, false, "broken"));
                continue;
            }

            if (duration < _minimumDurationSeconds)
            {
                skippedShort++;
                results.Add(new MediaItem(file, Path.GetFileName(file), duration, false, "too short"));
                continue;
            }

            results.Add(new MediaItem(file, Path.GetFileName(file), duration, true));
        }

        return new PlaylistScanResult(
            results,
            results.Count(x => x.IsValid),
            skippedShort,
            skippedBroken);
    }

    private bool IsSupportedMediaPath(string path)
    {
        return _mediaExtensions.Contains(Path.GetExtension(path));
    }
}
