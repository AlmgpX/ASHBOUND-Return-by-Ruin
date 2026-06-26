namespace MediaRelic.Infra;

public static class CoverService
{
    private static readonly string[] SupportedExtensions =
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".bmp"
    };

    public static bool IsSupportedImage(string path)
    {
        var ext = Path.GetExtension(path);
        return SupportedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
    }

    public static string ApplySidecarCover(string mediaPath, string imagePath)
    {
        if (string.IsNullOrWhiteSpace(mediaPath))
            throw new ArgumentException("Media path is empty.", nameof(mediaPath));

        if (string.IsNullOrWhiteSpace(imagePath))
            throw new ArgumentException("Image path is empty.", nameof(imagePath));

        if (!File.Exists(mediaPath))
            throw new FileNotFoundException("Media file not found.", mediaPath);

        if (!File.Exists(imagePath))
            throw new FileNotFoundException("Cover image not found.", imagePath);

        if (!IsSupportedImage(imagePath))
            throw new InvalidOperationException("Unsupported cover image. Use JPG, JPEG, PNG, WEBP, or BMP.");

        var dir = Path.GetDirectoryName(mediaPath)
                  ?? throw new InvalidOperationException("Media file has no directory.");

        var mediaBase = Path.GetFileNameWithoutExtension(mediaPath);
        var imageExt = Path.GetExtension(imagePath).ToLowerInvariant();
        var target = Path.Combine(dir, mediaBase + imageExt);

        RemoveOldSameBaseCovers(dir, mediaBase, target);

        if (!Path.GetFullPath(imagePath).Equals(Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
            File.Copy(imagePath, target, overwrite: true);

        return target;
    }

    private static void RemoveOldSameBaseCovers(string dir, string mediaBase, string keepPath)
    {
        var keepFull = Path.GetFullPath(keepPath);

        foreach (var ext in SupportedExtensions)
        {
            var candidate = Path.Combine(dir, mediaBase + ext);
            var candidateFull = Path.GetFullPath(candidate);

            if (candidateFull.Equals(keepFull, StringComparison.OrdinalIgnoreCase))
                continue;

            if (File.Exists(candidate))
            {
                try
                {
                    File.Delete(candidate);
                }
                catch
                {
                    // If the OS refuses to delete an old cover, the new cover may lose priority.
                    // Not ideal, but not worth burning the village over.
                }
            }
        }
    }
}
