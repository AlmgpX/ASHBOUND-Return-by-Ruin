public static class OUTL_Cheats
{
    public static bool SvCheats;
    public static bool GodMode;
    public static OUTL_EntityId GodEntity = OUTL_EntityId.None;
    public static bool NoClip;
    public static OUTL_EntityId NoClipEntity = OUTL_EntityId.None;

    public static float SvGravity = 981f;

    public static float UnityGravity
    {
        get { return SvGravity * 0.01f; }
    }

    public static bool IsGodProtected(OUTL_EntityId target)
    {
        if (!SvCheats || !GodMode || !target.IsValid) return false;
        return !GodEntity.IsValid || GodEntity == target;
    }

    public static bool IsNoClipEntity(OUTL_EntityId target)
    {
        if (!SvCheats || !NoClip || !target.IsValid) return false;
        return !NoClipEntity.IsValid || NoClipEntity == target;
    }

    public static void SetGod(bool enabled, OUTL_EntityId entity)
    {
        GodMode = enabled;
        GodEntity = entity;
    }

    public static void SetNoClip(bool enabled, OUTL_EntityId entity)
    {
        NoClip = enabled;
        NoClipEntity = entity;
    }
}
