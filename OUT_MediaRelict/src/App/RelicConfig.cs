using System.Text.Json;

namespace MediaRelic.App;

public sealed class RelicConfig
{
    public double MinPlaylistDurationSeconds { get; set; } = 15.0;
    public double SilenceNoiseDb { get; set; } = -38.0;
    public double SilenceMinDurationSeconds { get; set; } = 0.35;
    public double MinExportSegmentDurationSeconds { get; set; } = 0.08;
    public int PreviewMaxWidth { get; set; } = 256;
    public int PreviewMaxHeight { get; set; } = 256;
    public double DefaultVolume { get; set; } = 100.0;

    public static RelicConfig Load()
    {
        var path = FindConfigPath();

        if (path is null)
            return new RelicConfig();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<RelicConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            }) ?? new RelicConfig();
        }
        catch
        {
            return new RelicConfig();
        }
    }

    private static string? FindConfigPath()
    {
        var names = new[]
        {
            "mediarelic.config.json",
            Path.Combine("OUT_MediaRelict", "mediarelic.config.json")
        };

        foreach (var name in names)
        {
            var current = Path.Combine(Environment.CurrentDirectory, name);
            if (File.Exists(current))
                return current;

            var app = Path.Combine(AppContext.BaseDirectory, name);
            if (File.Exists(app))
                return app;
        }

        return null;
    }
}
