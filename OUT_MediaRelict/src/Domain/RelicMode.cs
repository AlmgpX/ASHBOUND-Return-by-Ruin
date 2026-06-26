namespace MediaRelic.Domain;

public enum RelicMode
{
    Empty,
    Loading,
    Ready,
    Playing,
    Paused,
    ScanningSilence,
    Exporting,
    Error
}
