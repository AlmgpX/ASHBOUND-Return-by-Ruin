using System.Text.Json;

namespace MediaRelic.App;

public sealed class RelicConfig
{
    public double MinPlaylistDurationSeconds { get; set; } = 15.0;
    public double SilenceNoiseDb { get; set; } = -38.0;
    public double SilenceMinDurationSeconds { get; set; } = 0.35;
    public double MinExportSegmentDurationSeconds { get; set; } = 0.08;
    public int PreviewMaxWidth { get; set; } = 96;
    public int PreviewMaxHeight { get; set; } = 96;
    public double DefaultVolume { get; set; } = 100.0;
    public bool StartTopMost { get; set; } = false;
    public float UiScale { get; set; } = 1.0f;

    public static RelicConfig Load()
    {
        var path = FindConfigPath();

        if (path is null)
            return new RelicConfig().Sanitized();

        try
        {
            var json = File.ReadAllText(path);
            return (JsonSerializer.Deserialize<RelicConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            }) ?? new RelicConfig()).Sanitized();
        }
        catch
        {
            return new RelicConfig().Sanitized();
        }
    }

    private RelicConfig Sanitized()
    {
        PreviewMaxWidth = Math.Clamp(PreviewMaxWidth, 24, 256);
        PreviewMaxHeight = Math.Clamp(PreviewMaxHeight, 24, 256);
        MinPlaylistDurationSeconds = Math.Max(0.0, MinPlaylistDurationSeconds);
        SilenceMinDurationSeconds = Math.Max(0.01, SilenceMinDurationSeconds);
        MinExportSegmentDurationSeconds = Math.Max(0.01, MinExportSegmentDurationSeconds);
        DefaultVolume = Math.Clamp(DefaultVolume, 0.0, 130.0);
        UiScale = Math.Clamp(UiScale, 0.75f, 1.75f);
        return this;
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
