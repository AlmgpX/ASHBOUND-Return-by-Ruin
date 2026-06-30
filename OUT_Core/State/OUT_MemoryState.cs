using System;

[Serializable]
public struct OUT_MemoryState
{
    public OUT_MemoryFlags Flags;

    public OUT_MemoryState(OUT_MemoryFlags initialFlags)
    {
        Flags = initialFlags;
    }

    public bool Has(OUT_MemoryFlags flags)
    {
        return (Flags & flags) == flags;
    }

    public bool Any(OUT_MemoryFlags flags)
    {
        return (Flags & flags) != 0;
    }

    public void Remember(OUT_MemoryFlags flags)
    {
        Flags |= flags;
    }

    public void Forget(OUT_MemoryFlags flags)
    {
        Flags &= ~flags;
    }

    public void Assign(bool value, OUT_MemoryFlags flags)
    {
        if (value)
            Remember(flags);
        else
            Forget(flags);
    }

    public void Reset()
    {
        Flags = OUT_MemoryFlags.None;
    }
}