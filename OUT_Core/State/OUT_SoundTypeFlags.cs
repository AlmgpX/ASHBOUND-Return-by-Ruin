using System;

[Flags]
public enum OUT_SoundTypeFlags
{
    None = 0,

    Combat = 1 << 0,
    World = 1 << 1,
    Player = 1 << 2,
    Danger = 1 << 3,
    Carcass = 1 << 4,
    Ambient = 1 << 5,
    Vocal = 1 << 6,
    Movement = 1 << 7
}