using UnityEngine;

public static class OUTL_WorldArchetypeUtility
{
    public static OUTL_WorldArchetypeType GetDominantArchetype(OUTL_WorldNarrativeConfig config, OUTL_WorldTile tile)
    {
        if (config == null || !config.UseJungArchetypes || config.Archetypes == null || config.Archetypes.Length == 0)
            return InferFallbackArchetype(tile);

        OUTL_WorldArchetypeDef best = null;
        float bestScore = float.MinValue;
        for (int i = 0; i < config.Archetypes.Length; i++)
        {
            OUTL_WorldArchetypeDef a = config.Archetypes[i];
            if (a == null || a.Type == OUTL_WorldArchetypeType.None) continue;
            if (!Matches(tile, a.AllowedZones, a.MinHeight, a.MaxHeight, a.MinMoisture, a.MaxMoisture, a.MinHeat, a.MaxHeat, a.MinDrainage, a.MaxDrainage)) continue;
            float score = Mathf.Max(0.001f, a.Weight);
            score += tile.Sanctity * 0.22f * config.MythicPressure;
            score += tile.Danger * 0.18f * config.ConflictPressure;
            score += tile.Prosperity * 0.08f * config.GrowthPressure;
            score += ZoneAffinity(a.Type, tile.Zone);
            if (score > bestScore)
            {
                bestScore = score;
                best = a;
            }
        }

        return best != null ? best.Type : InferFallbackArchetype(tile);
    }

    public static float GetVisibility(OUTL_WorldNarrativeConfig config, OUTL_WorldTile tile)
    {
        if (config == null) return 0.5f;
        float v = 0.1f;
        v += tile.Height * config.VisibilityFromHeight;
        v += Mathf.Clamp01((tile.Prosperity + 5f) / 14f) * config.VisibilityFromProsperity;
        v += Mathf.Clamp01(tile.Sanctity / 10f) * config.VisibilityFromSacred;
        if (tile.HasRiver || tile.HasLake || tile.Zone == OUTL_WorldZoneType.Coast) v += config.VisibilityFromWater;
        v -= Mathf.Clamp01(tile.Danger / 10f) * config.VisibilityDangerPenalty;
        v -= Mathf.Clamp01(config.VisibilityFog);
        if (config.UseJungArchetypes)
            v += GetArchetypeVisibilityBias(config, tile);
        return Mathf.Clamp01(v);
    }

    public static float GetEventPressure(OUTL_WorldNarrativeConfig config, OUTL_WorldTile tile)
    {
        if (config == null) return 1f;
        float pressure = Mathf.Max(0f, config.EventDensity);
        pressure *= TemperamentEventMultiplier(config.Temperament);
        pressure *= 1f + Mathf.Max(0, tile.Danger) * 0.04f * config.ConflictPressure;
        pressure *= 1f + Mathf.Max(0, tile.Prosperity) * 0.025f * config.GrowthPressure;
        pressure *= 1f + Mathf.Max(0, tile.Sanctity) * 0.03f * config.MythicPressure;
        return Mathf.Max(0f, pressure);
    }

    public static string ArchetypeNameRu(OUTL_WorldArchetypeType type)
    {
        switch (type)
        {
            case OUTL_WorldArchetypeType.Self: return "Самость";
            case OUTL_WorldArchetypeType.Shadow: return "Тень";
            case OUTL_WorldArchetypeType.Persona: return "Персона";
            case OUTL_WorldArchetypeType.Anima: return "Анима";
            case OUTL_WorldArchetypeType.Animus: return "Анимус";
            case OUTL_WorldArchetypeType.Hero: return "Герой";
            case OUTL_WorldArchetypeType.Trickster: return "Трикстер";
            case OUTL_WorldArchetypeType.WiseElder: return "Мудрый старец";
            case OUTL_WorldArchetypeType.GreatMother: return "Великая мать";
            case OUTL_WorldArchetypeType.Child: return "Ребёнок";
            case OUTL_WorldArchetypeType.DeathRebirth: return "Смерть/Возрождение";
            default: return "Нет";
        }
    }

    public static Color ArchetypeColor(OUTL_WorldArchetypeType type)
    {
        switch (type)
        {
            case OUTL_WorldArchetypeType.Self: return new Color32(255, 255, 255, 255);
            case OUTL_WorldArchetypeType.Shadow: return new Color32(25, 20, 35, 255);
            case OUTL_WorldArchetypeType.Persona: return new Color32(210, 210, 190, 255);
            case OUTL_WorldArchetypeType.Anima: return new Color32(120, 180, 255, 255);
            case OUTL_WorldArchetypeType.Animus: return new Color32(255, 145, 80, 255);
            case OUTL_WorldArchetypeType.Hero: return new Color32(255, 225, 80, 255);
            case OUTL_WorldArchetypeType.Trickster: return new Color32(180, 70, 220, 255);
            case OUTL_WorldArchetypeType.WiseElder: return new Color32(150, 160, 190, 255);
            case OUTL_WorldArchetypeType.GreatMother: return new Color32(60, 180, 95, 255);
            case OUTL_WorldArchetypeType.Child: return new Color32(255, 190, 210, 255);
            case OUTL_WorldArchetypeType.DeathRebirth: return new Color32(160, 25, 25, 255);
            default: return Color.black;
        }
    }

    private static float GetArchetypeVisibilityBias(OUTL_WorldNarrativeConfig config, OUTL_WorldTile tile)
    {
        OUTL_WorldArchetypeType type = GetDominantArchetype(config, tile);
        if (config.Archetypes != null)
        {
            for (int i = 0; i < config.Archetypes.Length; i++)
                if (config.Archetypes[i] != null && config.Archetypes[i].Type == type)
                    return config.Archetypes[i].VisibilityBias;
        }
        return 0f;
    }

    private static float TemperamentEventMultiplier(OUTL_WorldSimulationTemperament temperament)
    {
        switch (temperament)
        {
            case OUTL_WorldSimulationTemperament.Stable: return 0.65f;
            case OUTL_WorldSimulationTemperament.Harsh: return 1.25f;
            case OUTL_WorldSimulationTemperament.Mythic: return 1.35f;
            case OUTL_WorldSimulationTemperament.Chaotic: return 1.75f;
            case OUTL_WorldSimulationTemperament.CivilizedGrowth: return 1.1f;
            case OUTL_WorldSimulationTemperament.Decay: return 1.3f;
            default: return 1f;
        }
    }

    private static OUTL_WorldArchetypeType InferFallbackArchetype(OUTL_WorldTile tile)
    {
        if (tile.Sanctity >= 4) return OUTL_WorldArchetypeType.Self;
        if (tile.Danger >= 5) return OUTL_WorldArchetypeType.Shadow;
        if (tile.Zone == OUTL_WorldZoneType.Ruins || tile.Zone == OUTL_WorldZoneType.Wasteland) return OUTL_WorldArchetypeType.DeathRebirth;
        if (tile.Zone == OUTL_WorldZoneType.River || tile.Zone == OUTL_WorldZoneType.Lake || tile.Zone == OUTL_WorldZoneType.Forest) return OUTL_WorldArchetypeType.GreatMother;
        if (tile.Zone == OUTL_WorldZoneType.Mountains) return OUTL_WorldArchetypeType.WiseElder;
        if (tile.Zone == OUTL_WorldZoneType.Steppe || tile.Zone == OUTL_WorldZoneType.Plains) return OUTL_WorldArchetypeType.Hero;
        return OUTL_WorldArchetypeType.Persona;
    }

    private static float ZoneAffinity(OUTL_WorldArchetypeType type, OUTL_WorldZoneType zone)
    {
        if (type == OUTL_WorldArchetypeType.Shadow && (zone == OUTL_WorldZoneType.Wasteland || zone == OUTL_WorldZoneType.Ruins || zone == OUTL_WorldZoneType.Swamp)) return 2f;
        if (type == OUTL_WorldArchetypeType.Self && zone == OUTL_WorldZoneType.Sacred) return 3f;
        if (type == OUTL_WorldArchetypeType.GreatMother && (zone == OUTL_WorldZoneType.Forest || zone == OUTL_WorldZoneType.River || zone == OUTL_WorldZoneType.Lake)) return 2f;
        if (type == OUTL_WorldArchetypeType.WiseElder && zone == OUTL_WorldZoneType.Mountains) return 2f;
        if (type == OUTL_WorldArchetypeType.Trickster && (zone == OUTL_WorldZoneType.Coast || zone == OUTL_WorldZoneType.Ruins)) return 1.5f;
        if (type == OUTL_WorldArchetypeType.DeathRebirth && (zone == OUTL_WorldZoneType.Ruins || zone == OUTL_WorldZoneType.Wasteland || zone == OUTL_WorldZoneType.Desert)) return 2f;
        return 0f;
    }

    private static bool Matches(OUTL_WorldTile tile, OUTL_WorldZoneType[] zones, float minH, float maxH, float minM, float maxM, float minHeat, float maxHeat, float minDrainage, float maxDrainage)
    {
        if (zones != null && zones.Length > 0)
        {
            bool ok = false;
            for (int i = 0; i < zones.Length; i++) if (zones[i] == tile.Zone) { ok = true; break; }
            if (!ok) return false;
        }
        return tile.Height >= minH && tile.Height <= maxH && tile.Moisture >= minM && tile.Moisture <= maxM && tile.Heat >= minHeat && tile.Heat <= maxHeat && tile.Drainage >= minDrainage && tile.Drainage <= maxDrainage;
    }
}
