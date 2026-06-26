namespace MediaRelic.Playlist;

public sealed record MediaItem(
    string Path,
    string Title,
    double Duration,
    bool IsValid,
    string? RejectReason = null);

public sealed record PlaylistScanResult(
    IReadOnlyList<MediaItem> Items,
    int AcceptedCount,
    int SkippedShortCount,
    int SkippedBrokenCount);
