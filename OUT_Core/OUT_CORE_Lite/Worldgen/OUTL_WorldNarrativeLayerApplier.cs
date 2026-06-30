using UnityEngine;

public static class OUTL_WorldNarrativeLayerApplier
{
    public static void ApplyArchetypeAndVisibilityLayers(OUTL_WorldNarrativeConfig config, OUTL_WorldNarrativeResult result)
    {
        if (config == null || result == null || result.Tiles == null) return;
        if (!config.UseJungArchetypes && !config.ComputeVisibility) return;

        for (int y = 0; y < result.Height; y++)
        {
            for (int x = 0; x < result.Width; x++)
            {
                OUTL_WorldTile tile = result.Tiles[x, y];
                if (config.UseJungArchetypes)
                {
                    OUTL_WorldArchetypeDef def = FindDominantArchetypeDef(config, tile);
                    if (def != null)
                    {
                        tile.Prosperity += Mathf.RoundToInt(def.ProsperityBias * config.ArchetypeStrength);
                        tile.Danger += Mathf.RoundToInt(def.DangerBias * config.ArchetypeStrength);
                        tile.Sanctity += Mathf.RoundToInt(def.SanctityBias * config.ArchetypeStrength);
                    }
                }
                result.Tiles[x, y] = tile;
            }
        }
    }

    public static OUTL_WorldArchetypeDef FindDominantArchetypeDef(OUTL_WorldNarrativeConfig config, OUTL_WorldTile tile)
    {
        if (config == null || config.Archetypes == null || config.Archetypes.Length == 0) return null;
        OUTL_WorldArchetypeType type = OUTL_WorldArchetypeUtility.GetDominantArchetype(config, tile);
        for (int i = 0; i < config.Archetypes.Length; i++)
            if (config.Archetypes[i] != null && config.Archetypes[i].Type == type)
                return config.Archetypes[i];
        return null;
    }
}
