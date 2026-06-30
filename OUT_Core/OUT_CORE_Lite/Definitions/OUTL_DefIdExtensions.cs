public static class OUTL_DefIdExtensions
{
    public static string GetDefId(this OUTL_EntityDef def)
    {
        if (def == null) return string.Empty;
        if (!string.IsNullOrWhiteSpace(def.ClassName)) return def.ClassName.Trim();
        return def.name;
    }

    public static string GetFactionIdSafe(this OUTL_FactionDef faction)
    {
        if (faction == null) return string.Empty;
        if (!string.IsNullOrWhiteSpace(faction.FactionId)) return faction.FactionId.Trim();
        return faction.name;
    }
}
