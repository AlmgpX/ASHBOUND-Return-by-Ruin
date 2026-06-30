using System;

[Flags]
public enum OUT_SimulationTier
{
    None = 0,
    Full = 1 << 0,
    Reduced = 1 << 1,
    Abstract = 1 << 2,
    RandomWorld = 1 << 3,
    All = Full | Reduced | Abstract | RandomWorld
}

public enum OUT_SimulationStepResult
{
    Skipped = 0,
    Tick = 1,
    Clamped = 2
}
