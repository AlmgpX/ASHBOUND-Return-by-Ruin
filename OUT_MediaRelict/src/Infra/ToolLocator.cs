namespace MediaRelic.Infra;

public static class ToolLocator
{
    public static string? Find(string fileName)
    {
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "tools", fileName),
            Path.Combine(Environment.CurrentDirectory, "tools", fileName),
            Path.Combine(baseDir, fileName),
            Path.Combine(Environment.CurrentDirectory, fileName)
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in path.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(dir))
                continue;

            try
            {
                var candidate = Path.Combine(dir.Trim(), fileName);
                if (File.Exists(candidate))
                    return candidate;
            }
            catch
            {
                // PATH can contain garbage. Humanity survives by ignoring some of it.
            }
        }

        return null;
    }
}
