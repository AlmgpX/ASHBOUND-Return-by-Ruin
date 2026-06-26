namespace MediaRelic.Domain;

public sealed class RelicState
{
    public RelicMode Mode { get; set; } = RelicMode.Empty;

    public string? MediaPath { get; set; }
    public bool IsPaused { get; set; } = true;
    public bool IsLooping { get; set; }
    public bool IsReverbEnabled { get; set; }
    public bool IsTopMost { get; set; } = true;

    public double Position { get; set; }
    public double Duration { get; set; }
    public double Speed { get; set; } = 1.0;
    public double Volume { get; set; } = 100.0;

    public string Status { get; set; } = "DROP MEDIA, PRESS O, OR PRESS P FOR FOLDER";
    public GlyphFrame Preview { get; set; } = GlyphFrame.Empty(256, 256);
    public List<TimeRange> SoundRanges { get; set; } = new();

    public List<string> Playlist { get; set; } = new();
    public int PlaylistIndex { get; set; } = -1;

    public bool HasPlaylist => Playlist.Count > 0 && PlaylistIndex >= 0 && PlaylistIndex < Playlist.Count;

    public string PlaylistLabel
    {
        get
        {
            if (!HasPlaylist)
                return "PLAYLIST ·";

            return $"PLAYLIST {PlaylistIndex + 1}/{Playlist.Count}";
        }
    }

    public string DisplayName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(MediaPath))
                return "NO MEDIA LOADED";

            return Path.GetFileName(MediaPath);
        }
    }
}
