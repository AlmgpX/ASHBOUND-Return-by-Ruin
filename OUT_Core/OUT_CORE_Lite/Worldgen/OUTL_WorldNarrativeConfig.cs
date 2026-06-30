using System;
using UnityEngine;

public enum OUTL_WorldZoneType : byte
{
    Ocean = 0,
    Coast = 1,
    Plains = 2,
    Forest = 3,
    Hills = 4,
    Mountains = 5,
    Swamp = 6,
    Desert = 7,
    Wasteland = 8,
    Sacred = 9,
    Ruins = 10,
    River = 11,
    Lake = 12,
    Tundra = 13,
    Steppe = 14
}

public enum OUTL_WorldArchetypeType : byte
{
    None = 0,
    Self = 1,
    Shadow = 2,
    Persona = 3,
    Anima = 4,
    Animus = 5,
    Hero = 6,
    Trickster = 7,
    WiseElder = 8,
    GreatMother = 9,
    Child = 10,
    DeathRebirth = 11
}

public enum OUTL_WorldSimulationTemperament : byte
{
    Balanced = 0,
    Stable = 1,
    Harsh = 2,
    Mythic = 3,
    Chaotic = 4,
    CivilizedGrowth = 5,
    Decay = 6
}

public enum OUTL_WorldGenerationPipelineMode : byte
{
    RawGenerateOnly = 0,
    FullAuthoringWriteAndExport = 1
}

[Serializable]
public struct OUTL_LocalizedString
{
    public string Language;
    [TextArea] public string Text;

    public OUTL_LocalizedString(string language, string text)
    {
        Language = language;
        Text = text;
    }
}

[Serializable]
public class OUTL_LocalizedText
{
    public string Key = "text.key";
    public OUTL_LocalizedString[] Variants;

    public string Get(string language, string fallback = "")
    {
        if (Variants != null)
        {
            for (int i = 0; i < Variants.Length; i++)
                if (Variants[i].Language == language && !string.IsNullOrEmpty(Variants[i].Text))
                    return Variants[i].Text;
            for (int i = 0; i < Variants.Length; i++)
                if (Variants[i].Language == "ru" && !string.IsNullOrEmpty(Variants[i].Text))
                    return Variants[i].Text;
            for (int i = 0; i < Variants.Length; i++)
                if (!string.IsNullOrEmpty(Variants[i].Text))
                    return Variants[i].Text;
        }
        return string.IsNullOrEmpty(fallback) ? Key : fallback;
    }
}

[Serializable]
public class OUTL_WorldArchetypeDef
{
    public string Id = "shadow";
    public OUTL_WorldArchetypeType Type = OUTL_WorldArchetypeType.Shadow;
    public OUTL_LocalizedText DisplayName;
    [Range(0f, 10f)] public float Weight = 1f;
    public OUTL_WorldZoneType[] AllowedZones;
    public float MinHeight = 0f;
    public float MaxHeight = 1f;
    public float MinMoisture = 0f;
    public float MaxMoisture = 1f;
    public float MinHeat = 0f;
    public float MaxHeat = 1f;
    public float MinDrainage = 0f;
    public float MaxDrainage = 1f;
    [Header("Influence")]
    public float ResourceChanceMultiplier = 1f;
    public float PlantChanceMultiplier = 1f;
    public float EventChanceMultiplier = 1f;
    public int ProsperityBias;
    public int DangerBias;
    public int SanctityBias;
    public float VisibilityBias;
    [TextArea] public string DesignNoteRu;
    public OUTL_LocalizedText[] Lines;
}

[Serializable]
public class OUTL_WorldResourceDef
{
    public string Id = "stone";
    public OUTL_LocalizedText DisplayName;
    public OUTL_WorldZoneType[] AllowedZones;
    [Range(0f, 1f)] public float BaseChance = 0.1f;
    public float MinHeight = 0f;
    public float MaxHeight = 1f;
    public float MinMoisture = 0f;
    public float MaxMoisture = 1f;
    public float MinHeat = 0f;
    public float MaxHeat = 1f;
    public float MinDrainage = 0f;
    public float MaxDrainage = 1f;
    public OUTL_LocalizedText[] DiscoveryLines;
}

[Serializable]
public class OUTL_WorldPlantDef
{
    public string Id = "grass";
    public OUTL_LocalizedText DisplayName;
    public OUTL_WorldZoneType[] AllowedZones;
    [Range(0f, 1f)] public float BaseChance = 0.2f;
    public float MinHeight = 0f;
    public float MaxHeight = 1f;
    public float MinMoisture = 0f;
    public float MaxMoisture = 1f;
    public float MinHeat = 0f;
    public float MaxHeat = 1f;
    public float MinDrainage = 0f;
    public float MaxDrainage = 1f;
    public OUTL_LocalizedText[] SpreadLines;
}

[Serializable]
public class OUTL_WorldEventDef
{
    public string Id = "migration";
    public OUTL_LocalizedText DisplayName;
    public OUTL_WorldZoneType[] AllowedZones;
    [Range(0f, 1f)] public float BaseChancePerYear = 0.05f;
    public float MinHeight = 0f;
    public float MaxHeight = 1f;
    public float MinMoisture = 0f;
    public float MaxMoisture = 1f;
    public float MinHeat = 0f;
    public float MaxHeat = 1f;
    public float MinDrainage = 0f;
    public float MaxDrainage = 1f;
    public int MinYear = 0;
    public int MaxYear = 99999;
    public int ProsperityDelta;
    public int DangerDelta;
    public int SanctityDelta;
    public OUTL_LocalizedText[] Lines;
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Worldgen/World Narrative Config", fileName = "OUTL_WorldNarrativeConfig")]
public class OUTL_WorldNarrativeConfig : ScriptableObject
{
    [Header("Localization")]
    public string Language = "ru";

    [Header("Identity")]
    public OUTL_LocalizedText WorldName;
    public OUTL_LocalizedText EraName;

    [Header("Pipeline")]
    public OUTL_WorldGenerationPipelineMode RecommendedPipeline = OUTL_WorldGenerationPipelineMode.FullAuthoringWriteAndExport;
    [TextArea(3, 8)] public string PipelineNoteRu = "Raw Generate() создаёт базовый ландшафт, климат, зоны, ресурсы и хронику. Полный writer pipeline через OUTL_WorldNarrativeWriter.GenerateWriteAndExport() дополнительно применяет архетипические/видимые слои, пишет markdown-отчёт и экспортирует PNG-карты. Для готовой игры обычно используй writer pipeline.";
    public bool ApplyAdvancedLayersInRawGenerate = false;

    [Header("Map")]
    public int Width = 64;
    public int Height = 48;
    public int Years = 120;
    public int Seed = 12345;
    public float HeightScale = 18f;
    public float MoistureScale = 14f;
    public float HeatScale = 24f;
    public int Octaves = 5;
    public float Persistence = 0.52f;
    public float Lacunarity = 2.05f;

    [Header("World Behaviour")]
    public OUTL_WorldSimulationTemperament Temperament = OUTL_WorldSimulationTemperament.Balanced;
    [Range(0f, 4f)] public float EventDensity = 1f;
    [Range(0f, 4f)] public float ConflictPressure = 1f;
    [Range(0f, 4f)] public float GrowthPressure = 1f;
    [Range(0f, 4f)] public float MythicPressure = 1f;
    [Range(0f, 4f)] public float ArchetypeStrength = 1f;

    [Header("Jung Archetypes")]
    public bool UseJungArchetypes = true;
    public OUTL_WorldArchetypeDef[] Archetypes;

    [Header("Realistic Climate")]
    public bool UseLatitudeHeat = true;
    public float EquatorY = 0.5f;
    public float PolarCooling = 0.65f;
    public float AltitudeCooling = 0.48f;
    public float RainShadowStrength = 0.22f;
    public float RiverMoistureBoost = 0.18f;
    public float DrainageFromHeight = 0.55f;
    public float DrainageFromSlope = 0.45f;

    [Header("Hydrology")]
    public bool GenerateRivers = true;
    public int RiverCount = 5;
    public int RiverMaxSteps = 256;
    public float RiverSourceMinHeight = 0.68f;
    public float LakeChance = 0.035f;
    public float LakeMaxHeight = 0.52f;

    [Header("Visibility")]
    public bool ComputeVisibility = true;
    public float VisibilityFromHeight = 0.35f;
    public float VisibilityFromProsperity = 0.25f;
    public float VisibilityFromSacred = 0.25f;
    public float VisibilityFromWater = 0.15f;
    public float VisibilityDangerPenalty = 0.25f;
    public float VisibilityFog = 0.08f;

    [Header("Zone Thresholds")]
    public float OceanHeight = 0.28f;
    public float CoastHeight = 0.34f;
    public float HillsHeight = 0.62f;
    public float MountainHeight = 0.78f;
    public float TundraHeat = 0.24f;
    public float DesertHeat = 0.68f;
    public float DesertMoisture = 0.28f;
    public float SteppeMoisture = 0.38f;
    public float SwampMoisture = 0.72f;
    public float ForestMoisture = 0.52f;
    public float WastelandChance = 0.018f;
    public float SacredChance = 0.009f;
    public float RuinsChance = 0.012f;

    [Header("Content")]
    public OUTL_WorldResourceDef[] Resources;
    public OUTL_WorldPlantDef[] Plants;
    public OUTL_WorldEventDef[] Events;

    [Header("Output")]
    public bool WriteToPersistentDataPath = true;
    public string OutputFolder = "OUTL_WorldNarratives";
    public bool IncludeTileDump = false;

    [Header("Texture Export")]
    public bool ExportLayerTextures = true;
    public int TextureWidth = 256;
    public int TextureHeight = 256;
    public string TextureFolder = "Textures";
    public string TexturePrefix = "world";
    public bool ExportHeightTexture = true;
    public bool ExportMoistureTexture = true;
    public bool ExportHeatTexture = true;
    public bool ExportDrainageTexture = true;
    public bool ExportZoneTexture = true;
    public bool ExportResourceTexture = true;
    public bool ExportPlantTexture = true;
    public bool ExportDangerTexture = true;
    public bool ExportProsperityTexture = true;
    public bool ExportSanctityTexture = true;
    public bool ExportArchetypeTexture = true;
    public bool ExportVisibilityTexture = true;
}
