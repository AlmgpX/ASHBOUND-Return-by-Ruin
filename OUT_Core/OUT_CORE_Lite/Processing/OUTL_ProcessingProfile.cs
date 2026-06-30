using System;
using UnityEngine;

public enum OUTL_ProcessingBuiltInPreset
{
    CombatClose = 0,
    CombatDense = 1,
    StreamingWorld = 2,
    SearchMemoryAI = 3,
    AmbientEcology = 4
}

[Serializable]
public struct OUTL_TierProcessingSettings
{
    public OUTL_RuntimeTier Tier;
    public float EntityTickInterval;
    public bool EnableRandomTick;
    public float RandomTickInterval;
    public bool EnableNavTick;
    public float NavTickInterval;
    public float NavRepathInterval;
    public bool NavAllowVisualUpdate;
    public bool StopNavOnEnterTier;

    public static OUTL_TierProcessingSettings Make(OUTL_RuntimeTier tier, float entityTick, bool randomTick, float randomInterval, bool navTick, float navInterval, float repath, bool visual, bool stopNav)
    {
        return new OUTL_TierProcessingSettings
        {
            Tier = tier,
            EntityTickInterval = Mathf.Max(0.01f, entityTick),
            EnableRandomTick = randomTick,
            RandomTickInterval = Mathf.Max(0.01f, randomInterval),
            EnableNavTick = navTick,
            NavTickInterval = Mathf.Max(0.001f, navInterval),
            NavRepathInterval = Mathf.Max(0.02f, repath),
            NavAllowVisualUpdate = visual,
            StopNavOnEnterTier = stopNav
        };
    }
}

[Serializable]
public sealed class OUTL_ProcessingProfile
{
    public string ProfileId = "outl.processing.streaming_world";
    public int PresetVersion = 2;
    public float SectorCellSize = 64f;
    public float FullDistance = 35f;
    public float NearDistance = 95f;
    public float MidDistance = 260f;
    public float FarDistance = 700f;
    public int EntitiesPerTick = 220;
    public float DriverTickInterval = 0.3f;
    public bool ApplySectorCellSize = true;
    public bool ApplyEntityTickInterval = true;
    public bool ApplyRandomTick = true;
    public bool ApplyNavMeshMover = true;
    public bool StopNavWhenDormant = true;
    public OUTL_TierProcessingSettings Full;
    public OUTL_TierProcessingSettings Near;
    public OUTL_TierProcessingSettings Mid;
    public OUTL_TierProcessingSettings Far;
    public OUTL_TierProcessingSettings Dormant;

    public OUTL_RuntimeTier EvaluateTier(float sqrDistance)
    {
        if (sqrDistance <= FullDistance * FullDistance) return OUTL_RuntimeTier.Full;
        if (sqrDistance <= NearDistance * NearDistance) return OUTL_RuntimeTier.Near;
        if (sqrDistance <= MidDistance * MidDistance) return OUTL_RuntimeTier.Mid;
        if (sqrDistance <= FarDistance * FarDistance) return OUTL_RuntimeTier.Far;
        return OUTL_RuntimeTier.Dormant;
    }

    public OUTL_TierProcessingSettings GetSettings(OUTL_RuntimeTier tier)
    {
        switch (tier)
        {
            case OUTL_RuntimeTier.Full: return Full;
            case OUTL_RuntimeTier.Near: return Near;
            case OUTL_RuntimeTier.Mid: return Mid;
            case OUTL_RuntimeTier.Far: return Far;
            default: return Dormant;
        }
    }

    public void Sanitize()
    {
        PresetVersion = Mathf.Max(1, PresetVersion);
        SectorCellSize = Mathf.Max(1f, SectorCellSize);
        FullDistance = Mathf.Max(0f, FullDistance);
        NearDistance = Mathf.Max(FullDistance, NearDistance);
        MidDistance = Mathf.Max(NearDistance, MidDistance);
        FarDistance = Mathf.Max(MidDistance, FarDistance);
        EntitiesPerTick = Mathf.Max(1, EntitiesPerTick);
        DriverTickInterval = Mathf.Max(0.01f, DriverTickInterval);
    }

    public static OUTL_ProcessingProfile Create(OUTL_ProcessingBuiltInPreset preset)
    {
        OUTL_ProcessingProfile p = new OUTL_ProcessingProfile();
        p.ApplyBuiltIn(preset);
        return p;
    }

    public void ApplyBuiltIn(OUTL_ProcessingBuiltInPreset preset)
    {
        PresetVersion = 2;
        ApplySectorCellSize = true;
        ApplyEntityTickInterval = true;
        ApplyRandomTick = true;
        ApplyNavMeshMover = true;
        StopNavWhenDormant = true;

        switch (NormalizePresetValue((int)preset))
        {
            case OUTL_ProcessingBuiltInPreset.CombatClose:
                ApplyCombatClose();
                break;
            case OUTL_ProcessingBuiltInPreset.CombatDense:
                ApplyCombatDense();
                break;
            case OUTL_ProcessingBuiltInPreset.SearchMemoryAI:
                ApplySearchMemoryAI();
                break;
            case OUTL_ProcessingBuiltInPreset.AmbientEcology:
                ApplyAmbientEcology();
                break;
            case OUTL_ProcessingBuiltInPreset.StreamingWorld:
            default:
                ApplyStreamingWorld();
                break;
        }

        Sanitize();
    }

    private static OUTL_ProcessingBuiltInPreset NormalizePresetValue(int value)
    {
        if (value == 5) return OUTL_ProcessingBuiltInPreset.AmbientEcology; // legacy VegetationAmbient
        if (value == 6) return OUTL_ProcessingBuiltInPreset.CombatDense;    // legacy CinematicFocus
        if (value < 0 || value > 4) return OUTL_ProcessingBuiltInPreset.StreamingWorld;
        return (OUTL_ProcessingBuiltInPreset)value;
    }

    private void ApplyCombatClose()
    {
        ProfileId = "outl.processing.combat_close";
        SectorCellSize = 16f;
        FullDistance = 10f;
        NearDistance = 30f;
        MidDistance = 85f;
        FarDistance = 180f;
        EntitiesPerTick = 112;
        DriverTickInterval = 0.20f;
        Full = OUTL_TierProcessingSettings.Make(OUTL_RuntimeTier.Full, 0.04f, true, 0.75f, true, 0.035f, 0.12f, true, false);
        Near = OUTL_TierProcessingSettings.Make(OUTL_RuntimeTier.Near, 0.08f, true, 1.25f, true, 0.05f, 0.20f, true, false);
        Mid = OUTL_TierProcessingSettings.Make(OUTL_RuntimeTier.Mid, 0.22f, true, 4.0f, true, 0.12f, 0.45f, true, false);
        Far = OUTL_TierProcessingSettings.Make(OUTL_RuntimeTier.Far, 0.85f, true, 10.0f, true, 0.35f, 1.25f, false, false);
        Dormant = OUTL_TierProcessingSettings.Make(OUTL_RuntimeTier.Dormant, 2.5f, false, 30.0f, false, 1.0f, 3.0f, false, true);
    }

    private void ApplyCombatDense()
    {
        ProfileId = "outl.processing.combat_dense";
        SectorCellSize = 24f;
        FullDistance = 16f;
        NearDistance = 48f;
        MidDistance = 130f;
        FarDistance = 280f;
        EntitiesPerTick = 176;
        DriverTickInterval = 0.18f;
        Full = OUTL_TierProcessingSettings.Make(OUTL_RuntimeTier.Full, 0.035f, true, 0.50f, true, 0.035f, 0.10f, true, false);
        Near = OUTL_TierProcessingSettings.Make(OUTL_RuntimeTier.Near, 0.06f, true, 1.00f, true, 0.045f, 0.18f, true, false);
        Mid = OUTL_TierProcessingSettings.Make(OUTL_RuntimeTier.Mid, 0.18f, true, 3.00f, true, 0.10f, 0.35f, true, false);
        Far = OUTL_TierProcessingSettings.Make(OUTL_RuntimeTier.Far, 0.70f, true, 8.00f, true, 0.28f, 1.00f, false, false);
        Dormant = OUTL_TierProcessingSettings.Make(OUTL_RuntimeTier.Dormant, 2.0f, false, 24.0f, false, 1.0f, 2.50f, false, true);
    }

    private void ApplyStreamingWorld()
    {
        ProfileId = "outl.processing.streaming_world";
        SectorCellSize = 64f;
        FullDistance = 35f;
        NearDistance = 100f;
        MidDistance = 280f;
        FarDistance = 720f;
        EntitiesPerTick = 220;
        DriverTickInterval = 0.30f;
        Full = OUTL_TierProcessingSettings.Make(OUTL_RuntimeTier.Full, 0.05f, true, 1.00f, true, 0.04f, 0.18f, true, false);
        Near = OUTL_TierProcessingSettings.Make(OUTL_RuntimeTier.Near, 0.12f, true, 2.50f, true, 0.08f, 0.35f, true, false);
        Mid = OUTL_TierProcessingSettings.Make(OUTL_RuntimeTier.Mid, 0.35f, true, 7.00f, true, 0.20f, 0.80f, false, false);
        Far = OUTL_TierProcessingSettings.Make(OUTL_RuntimeTier.Far, 1.25f, true, 18.0f, true, 0.55f, 2.50f, false, false);
        Dormant = OUTL_TierProcessingSettings.Make(OUTL_RuntimeTier.Dormant, 4.0f, false, 60.0f, false, 1.50f, 5.00f, false, true);
    }

    private void ApplySearchMemoryAI()
    {
        ProfileId = "outl.processing.search_memory_ai";
        SectorCellSize = 32f;
        FullDistance = 12f;
        NearDistance = 42f;
        MidDistance = 140f;
        FarDistance = 380f;
        EntitiesPerTick = 128;
        DriverTickInterval = 0.22f;
        Full = OUTL_TierProcessingSettings.Make(OUTL_RuntimeTier.Full, 0.05f, true, 0.80f, true, 0.045f, 0.16f, true, false);
        Near = OUTL_TierProcessingSettings.Make(OUTL_RuntimeTier.Near, 0.10f, true, 1.50f, true, 0.07f, 0.25f, true, false);
        Mid = OUTL_TierProcessingSettings.Make(OUTL_RuntimeTier.Mid, 0.30f, true, 5.00f, true, 0.16f, 0.75f, true, false);
        Far = OUTL_TierProcessingSettings.Make(OUTL_RuntimeTier.Far, 1.00f, true, 12.0f, true, 0.35f, 1.75f, false, false);
        Dormant = OUTL_TierProcessingSettings.Make(OUTL_RuntimeTier.Dormant, 3.0f, true, 35.0f, false, 1.20f, 4.00f, false, true);
    }

    private void ApplyAmbientEcology()
    {
        ProfileId = "outl.processing.ambient_ecology";
        SectorCellSize = 48f;
        FullDistance = 18f;
        NearDistance = 70f;
        MidDistance = 190f;
        FarDistance = 440f;
        EntitiesPerTick = 300;
        DriverTickInterval = 0.50f;
        ApplyNavMeshMover = false;
        StopNavWhenDormant = false;
        Full = OUTL_TierProcessingSettings.Make(OUTL_RuntimeTier.Full, 0.25f, true, 8.0f, false, 1.0f, 3.0f, false, false);
        Near = OUTL_TierProcessingSettings.Make(OUTL_RuntimeTier.Near, 0.80f, true, 18.0f, false, 1.0f, 3.0f, false, false);
        Mid = OUTL_TierProcessingSettings.Make(OUTL_RuntimeTier.Mid, 2.00f, true, 45.0f, false, 1.0f, 3.0f, false, false);
        Far = OUTL_TierProcessingSettings.Make(OUTL_RuntimeTier.Far, 5.00f, true, 90.0f, false, 1.0f, 3.0f, false, false);
        Dormant = OUTL_TierProcessingSettings.Make(OUTL_RuntimeTier.Dormant, 10.0f, true, 180.0f, false, 1.0f, 3.0f, false, false);
    }
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Processing/Profile", fileName = "OUTL_ProcessingProfile")]
public sealed class OUTL_ProcessingProfileAsset : ScriptableObject
{
    public OUTL_ProcessingBuiltInPreset SeedPreset = OUTL_ProcessingBuiltInPreset.StreamingWorld;
    public OUTL_ProcessingProfile Profile = OUTL_ProcessingProfile.Create(OUTL_ProcessingBuiltInPreset.StreamingWorld);

    [ContextMenu("Apply Seed Preset")]
    public void ApplySeedPreset()
    {
        if (Profile == null) Profile = new OUTL_ProcessingProfile();
        Profile.ApplyBuiltIn(SeedPreset);
    }

    private void OnValidate()
    {
        if (Profile == null) Profile = OUTL_ProcessingProfile.Create(SeedPreset);
        Profile.Sanitize();
    }
}
