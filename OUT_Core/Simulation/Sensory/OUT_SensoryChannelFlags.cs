using System;

[Flags]
public enum OUT_SensoryChannelFlags
{
    None = 0,

    // Static / quasi-static scene channels.
    Luminance = 1 << 0,
    Occlusion = 1 << 1,
    Cover = 1 << 2,
    GroundSafety = 1 << 3,
    AreaCost = 1 << 4,
    SkyLuminance = 1 << 5,
    GroundLuminance = 1 << 6,

    // Dynamic stimuli.
    Noise = 1 << 7,
    Danger = 1 << 8,
    Food = 1 << 9,
    Fire = 1 << 10,

    AllStatic = Luminance | SkyLuminance | GroundLuminance | Occlusion | Cover | GroundSafety | AreaCost,
    AllDynamic = Noise | Danger | Food | Fire,
    All = AllStatic | AllDynamic
}
