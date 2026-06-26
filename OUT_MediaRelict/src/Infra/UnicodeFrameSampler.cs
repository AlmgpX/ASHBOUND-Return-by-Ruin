using System.Diagnostics;
using System.Drawing;
using System.Text.Json;
using MediaRelic.Domain;

namespace MediaRelic.Infra;

public sealed class UnicodeFrameSampler
{
    private const int ExtractedBitmapWidth = 1024;
    private const int MinPreviewCells = 16;

    private static readonly char[] DensityRamp =
        " .'`^,:;Il!i><~+_-?][}{1)(|\\/tfjrxnuvczXYUJCLQ0OZmwqpdbkhao*#MW&8%B@$█".ToCharArray();

    private static readonly string[] CoverNames =
    {
        "cover",
        "folder",
        "front",
        "album",
        "artwork",
        "poster"
    };

    private static readonly string[] CoverExts =
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".bmp"
    };

    private readonly string _ffmpegPath;
    private readonly string? _ffprobePath;

    private string? _cachedImageKey;
    private GlyphFrame? _cachedImageFrame;
    private string? _knownNoImagePath;

    public UnicodeFrameSampler(string ffmpegPath, string? ffprobePath)
    {
        _ffmpegPath = ffmpegPath;
        _ffprobePath = ffprobePath;
    }

    public void InvalidateForPath(string? mediaPath)
    {
        if (string.IsNullOrWhiteSpace(mediaPath))
            return;

        if (_knownNoImagePath == mediaPath)
            _knownNoImagePath = null;

        if (_cachedImageKey is not null && _cachedImageKey.EndsWith(mediaPath, StringComparison.OrdinalIgnoreCase))
        {
            _cachedImageKey = null;
            _cachedImageFrame = null;
        }

        if (_cachedImageKey is not null && _cachedImageKey.StartsWith("sidecar:", StringComparison.OrdinalIgnoreCase))
        {
            _cachedImageKey = null;
            _cachedImageFrame = null;
        }
    }

    public async Task<GlyphFrame> SampleAsync(
        string mediaPath,
        double seconds,
        int width,
        int height,
        CancellationToken cancellationToken)
    {
        try
        {
            var cover = await TryGetCoverArtAsync(mediaPath, width, height, cancellationToken);
            if (cover is not null)
                return cover;

            var frameBytes = await ExtractVideoFrameBmpAsync(mediaPath, seconds, cancellationToken);
            if (frameBytes.Length > 0)
            {
                using var stream = new MemoryStream(frameBytes);
                using var bitmap = new Bitmap(stream);
                return ConvertBitmapAspectCorrect(bitmap, width, height);
            }

            return BuildNoCoverFrame(width, height);
        }
        catch
        {
            return BuildNoCoverFrame(width, height);
        }
    }

    private async Task<GlyphFrame?> TryGetCoverArtAsync(
        string mediaPath,
        int width,
        int height,
        CancellationToken cancellationToken)
    {
        if (_knownNoImagePath == mediaPath)
            return null;

        // Priority 1: embedded cover art from the media file itself.
        // For MP3 this is usually an ID3 APIC attached picture stream.
        if (!string.IsNullOrWhiteSpace(_ffprobePath))
        {
            var embeddedKey = "embedded:" + mediaPath;

            if (_cachedImageKey == embeddedKey && _cachedImageFrame is not null)
                return _cachedImageFrame;

            var coverStreamIndex = await FindAttachedPictureStreamAsync(mediaPath, cancellationToken);
            if (coverStreamIndex is not null)
            {
                var bytes = await ExtractMappedBmpAsync(
                    mediaPath,
                    streamMap: $"0:{coverStreamIndex.Value}",
                    seconds: null,
                    cancellationToken);

                if (bytes.Length > 0)
                {
                    using var stream = new MemoryStream(bytes);
                    using var bitmap = new Bitmap(stream);

                    var frame = ConvertBitmapAspectCorrect(bitmap, width, height);

                    _cachedImageKey = embeddedKey;
                    _cachedImageFrame = frame;

                    return frame;
                }
            }
        }

        // Priority 2: sidecar cover file near the track.
        // This is a fallback, not the main path.
        var sidecar = FindSidecarCover(mediaPath);
        if (sidecar is not null)
        {
            var key = "sidecar:" + sidecar;

            if (_cachedImageKey == key && _cachedImageFrame is not null)
                return _cachedImageFrame;

            var sidecarFrame = await ConvertImageFileAsync(sidecar, width, height, cancellationToken);
            if (sidecarFrame is not null)
            {
                _cachedImageKey = key;
                _cachedImageFrame = sidecarFrame;
                return sidecarFrame;
            }
        }

        _knownNoImagePath = mediaPath;
        return null;
    }

    private static string? FindSidecarCover(string mediaPath)
    {
        var dir = Path.GetDirectoryName(mediaPath);
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
            return null;

        var mediaBase = Path.GetFileNameWithoutExtension(mediaPath);

        foreach (var ext in CoverExts)
        {
            var sameBase = Path.Combine(dir, mediaBase + ext);
            if (File.Exists(sameBase))
                return sameBase;
        }

        foreach (var name in CoverNames)
        {
            foreach (var ext in CoverExts)
            {
                var candidate = Path.Combine(dir, name + ext);
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return null;
    }

    private async Task<GlyphFrame?> ConvertImageFileAsync(
        string imagePath,
        int width,
        int height,
        CancellationToken cancellationToken)
    {
        var bytes = await ExtractImageBmpAsync(imagePath, cancellationToken);

        if (bytes.Length == 0)
            return null;

        using var stream = new MemoryStream(bytes);
        using var bitmap = new Bitmap(stream);

        return ConvertBitmapAspectCorrect(bitmap, width, height);
    }

    private async Task<int?> FindAttachedPictureStreamAsync(
        string mediaPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_ffprobePath))
            return null;

        var result = await ProcessRunner.RunTextAsync(
            _ffprobePath,
            new[]
            {
                "-v", "error",
                "-select_streams", "v",
                "-show_entries", "stream=index,disposition",
                "-of", "json",
                mediaPath
            },
            cancellationToken);

        if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.StdOut))
            return null;

        using var doc = JsonDocument.Parse(result.StdOut);

        if (!doc.RootElement.TryGetProperty("streams", out var streams))
            return null;

        foreach (var stream in streams.EnumerateArray())
        {
            if (!stream.TryGetProperty("index", out var indexElement))
                continue;

            if (!stream.TryGetProperty("disposition", out var disposition))
                continue;

            if (!disposition.TryGetProperty("attached_pic", out var attachedPic))
                continue;

            if (attachedPic.GetInt32() == 1)
                return indexElement.GetInt32();
        }

        return null;
    }

    private Task<byte[]> ExtractVideoFrameBmpAsync(
        string mediaPath,
        double seconds,
        CancellationToken cancellationToken)
    {
        return ExtractMappedBmpAsync(
            mediaPath,
            streamMap: "0:v:0?",
            seconds,
            cancellationToken);
    }

    private Task<byte[]> ExtractImageBmpAsync(
        string imagePath,
        CancellationToken cancellationToken)
    {
        return ExtractMappedBmpAsync(
            imagePath,
            streamMap: "0:v:0?",
            seconds: null,
            cancellationToken);
    }

    private async Task<byte[]> ExtractMappedBmpAsync(
        string inputPath,
        string streamMap,
        double? seconds,
        CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        start.ArgumentList.Add("-hide_banner");
        start.ArgumentList.Add("-loglevel");
        start.ArgumentList.Add("error");
        start.ArgumentList.Add("-nostdin");

        if (seconds is not null)
        {
            start.ArgumentList.Add("-ss");
            start.ArgumentList.Add(FfmpegService.ToStamp(seconds.Value));
        }

        start.ArgumentList.Add("-i");
        start.ArgumentList.Add(inputPath);
        start.ArgumentList.Add("-map");
        start.ArgumentList.Add(streamMap);
        start.ArgumentList.Add("-frames:v");
        start.ArgumentList.Add("1");

        // Do not pad or force the cover into the preview rectangle here.
        // We keep the real bitmap aspect and fit it later in glyph-space,
        // where character cells are not square. This prevents cover squashing.
        start.ArgumentList.Add("-vf");
        start.ArgumentList.Add($"scale={ExtractedBitmapWidth}:-2:flags=lanczos");

        start.ArgumentList.Add("-f");
        start.ArgumentList.Add("image2pipe");
        start.ArgumentList.Add("-vcodec");
        start.ArgumentList.Add("bmp");
        start.ArgumentList.Add("-");

        using var process = new Process { StartInfo = start };
        process.Start();

        using var output = new MemoryStream();
        await process.StandardOutput.BaseStream.CopyToAsync(output, cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
            return Array.Empty<byte>();

        return output.ToArray();
    }

    private static GlyphFrame ConvertBitmapAspectCorrect(Bitmap bitmap, int maxWidth, int maxHeight)
    {
        var (width, height) = ComputeAspectLockedCellSize(
            bitmap.Width,
            bitmap.Height,
            maxWidth,
            maxHeight);

        var cells = new GlyphCell[width * height];

        // The number of glyph columns and rows follows the source pixel aspect.
        // Square cover -> square glyph grid. 16:9 -> 16:9 glyph grid.
        // This renderer intentionally uses visible runes instead of half-block pixels,
        // because half-blocks were accurate and spiritually dead.
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var center = SampleNormalized(bitmap, x, y, width, height, 0.5, 0.5);
                var right = SampleNormalized(bitmap, Math.Min(x + 1, width - 1), y, width, height, 0.5, 0.5);
                var down = SampleNormalized(bitmap, x, Math.Min(y + 1, height - 1), width, height, 0.5, 0.5);

                var luma = Luma(center);
                var dx = Luma(right) - luma;
                var dy = Luma(down) - luma;
                var edge = Math.Abs(dx) + Math.Abs(dy);

                var rune = PickRune(luma, dx, dy, edge);
                var color = GradeColor(center);

                cells[y * width + x] = new GlyphCell(
                    rune,
                    color.R,
                    color.G,
                    color.B,
                    3,
                    5,
                    8);
            }
        }

        return new GlyphFrame(width, height, cells);
    }

    private static char PickRune(double luma, double dx, double dy, double edge)
    {
        if (edge > 0.28)
        {
            if (Math.Abs(dx) > Math.Abs(dy) * 1.7)
                return '│';

            if (Math.Abs(dy) > Math.Abs(dx) * 1.7)
                return '─';

            return Math.Sign(dx) == Math.Sign(dy) ? '╲' : '╱';
        }

        var index = (int)Math.Round(luma * (DensityRamp.Length - 1));
        index = Math.Clamp(index, 0, DensityRamp.Length - 1);

        return DensityRamp[index];
    }

    private static (int Width, int Height) ComputeAspectLockedCellSize(
        int imageWidth,
        int imageHeight,
        int maxWidth,
        int maxHeight)
    {
        imageWidth = Math.Max(1, imageWidth);
        imageHeight = Math.Max(1, imageHeight);
        maxWidth = Math.Max(MinPreviewCells, maxWidth);
        maxHeight = Math.Max(MinPreviewCells, maxHeight);

        var aspect = imageWidth / (double)imageHeight;

        int width;
        int height;

        if (aspect >= 1.0)
        {
            width = maxWidth;
            height = (int)Math.Round(width / aspect);

            if (height > maxHeight)
            {
                height = maxHeight;
                width = (int)Math.Round(height * aspect);
            }
        }
        else
        {
            height = maxHeight;
            width = (int)Math.Round(height * aspect);

            if (width > maxWidth)
            {
                width = maxWidth;
                height = (int)Math.Round(width / aspect);
            }
        }

        width = Math.Clamp(width, MinPreviewCells, maxWidth);
        height = Math.Clamp(height, MinPreviewCells, maxHeight);

        return (width, height);
    }

    private static Color SampleNormalized(
        Bitmap bitmap,
        int cellX,
        int cellY,
        int cellsWidth,
        int cellsHeight,
        double partX,
        double partY)
    {
        var u = (cellX + partX) / Math.Max(1.0, cellsWidth);
        var v = (cellY + partY) / Math.Max(1.0, cellsHeight);

        var sx = Math.Clamp((int)Math.Round(u * (bitmap.Width - 1)), 0, bitmap.Width - 1);
        var sy = Math.Clamp((int)Math.Round(v * (bitmap.Height - 1)), 0, bitmap.Height - 1);

        return bitmap.GetPixel(sx, sy);
    }

    private static Color GradeColor(Color pixel)
    {
        if (pixel.A == 0)
            return Color.FromArgb(3, 5, 8);

        var luma = Luma(pixel);

        var contrast = 1.14;
        var lift = 10.0;

        var r = (int)(((pixel.R - 128) * contrast) + 128 + lift);
        var g = (int)(((pixel.G - 128) * contrast) + 128 + lift);
        var b = (int)(((pixel.B - 128) * contrast) + 128 + lift);

        if (luma < 0.045)
            return Color.FromArgb(8, 10, 14);

        return Color.FromArgb(
            Math.Clamp(r, 0, 255),
            Math.Clamp(g, 0, 255),
            Math.Clamp(b, 0, 255));
    }

    private static double Luma(Color color)
    {
        return (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) / 255.0;
    }

    private static GlyphFrame BuildNoCoverFrame(int width, int height)
    {
        var cells = new GlyphCell[width * height];

        for (var i = 0; i < cells.Length; i++)
            cells[i] = new GlyphCell(' ', 60, 75, 80, 3, 5, 8);

        var lines = new[]
        {
            "╔════════════════════════╗",
            "║      NO COVER ART      ║",
            "║                        ║",
            "║  C  = APPLY COVER      ║",
            "║  PNG/JPG/WEBP/BMP      ║",
            "╚════════════════════════╝"
        };

        var y0 = Math.Max(0, height / 2 - lines.Length / 2);
        for (var y = 0; y < lines.Length && y0 + y < height; y++)
        {
            var line = lines[y];
            var x0 = Math.Max(0, width / 2 - line.Length / 2);

            for (var x = 0; x < line.Length && x0 + x < width; x++)
                cells[(y0 + y) * width + (x0 + x)] = new GlyphCell(line[x], 116, 224, 220, 3, 5, 8);
        }

        return new GlyphFrame(width, height, cells);
    }
}
