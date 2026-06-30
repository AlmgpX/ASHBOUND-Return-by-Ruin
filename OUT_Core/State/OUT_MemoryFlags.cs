using System;

[Flags]
public enum OUT_MemoryFlags
{
    None = 0,

    Provoked = 1 << 0,
    InCover = 1 << 1,
    Suspicious = 1 << 2,
    PathFinished = 1 << 3,
    OnPath = 1 << 4,
    MoveFailed = 1 << 5,
    Flinched = 1 << 6,
    Killed = 1 << 7,

    // Value-drive flags. Added after old bits so existing serialized values stay valid.
    Interested = 1 << 8,
    Tempted = 1 << 9,
    Repulsed = 1 << 10,
    SeekingReward = 1 << 11,

    Custom1 = 1 << 28,
    Custom2 = 1 << 29,
    Custom3 = 1 << 30,
    Custom4 = 1 << 31
}
