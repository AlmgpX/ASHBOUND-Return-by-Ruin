using System;

[Flags]
public enum OUT_UseCapabilityFlags
{
    None = 0,
    ImpulseUse = 1 << 0,
    ContinuousUse = 1 << 1,
    OnOffUse = 1 << 2,
    DirectionalUse = 1 << 3
}
