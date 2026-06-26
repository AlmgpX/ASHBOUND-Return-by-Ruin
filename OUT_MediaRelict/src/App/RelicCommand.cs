namespace MediaRelic.App;

public enum RelicCommandKind
{
    OpenFile,
    OpenFolder,
    ApplyCover,
    PlayPause,
    SeekRelative,
    SetVolume,
    SetSpeed,
    ToggleLoop,
    ToggleReverb,
    ScanSilence,
    ExportCuts,
    NextTrack,
    PreviousTrack,
    ToggleTopMost,
    Minimize,
    Close
}

public readonly record struct RelicCommand(
    RelicCommandKind Kind,
    string? Path = null,
    double Value = 0.0);
