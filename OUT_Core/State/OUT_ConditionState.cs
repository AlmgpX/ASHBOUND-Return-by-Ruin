using System;

[Serializable]
public struct OUT_ConditionState
{
    public OUT_ConditionFlags Flags;

    public OUT_ConditionState(OUT_ConditionFlags initialFlags)
    {
        Flags = initialFlags;
    }

    public bool Has(OUT_ConditionFlags flags)
    {
        return (Flags & flags) == flags;
    }

    public bool Any(OUT_ConditionFlags flags)
    {
        return (Flags & flags) != 0;
    }

    public void Set(OUT_ConditionFlags flags)
    {
        Flags |= flags;
    }

    public void Clear(OUT_ConditionFlags flags)
    {
        Flags &= ~flags;
    }

    public void Assign(bool value, OUT_ConditionFlags flags)
    {
        if (value)
            Set(flags);
        else
            Clear(flags);
    }

    public void Reset()
    {
        Flags = OUT_ConditionFlags.None;
    }
}