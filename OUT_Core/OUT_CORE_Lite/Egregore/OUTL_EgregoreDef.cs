using UnityEngine;

[CreateAssetMenu(menuName = "OUT CORE Lite/Egregore/Egregore Def", fileName = "OUTL_EgregoreDef")]
public class OUTL_EgregoreDef : ScriptableObject
{
    public string EgregoreId = "egregore_generic";
    public string DisplayName = "Generic Egregore";
    public OUTL_EgregoreScope Scope = OUTL_EgregoreScope.Local;
    public float UpdateInterval = 2.0f;
    public float InfluenceRadius = 64f;
    public AnimationCurve DecayCurve;

    [Header("Weights")]
    public float ViolenceWeight = 0.35f;
    public float FearWeight = 0.25f;
    public float ProsperityWeight = 0.20f;
    public float CorruptionWeight = 0.0f;
    public float AlertnessWeight = 0.25f;
    public float HostilityWeight = 0.55f;
    public float ResourceWeight = 0.25f;

    [Header("Thresholds")]
    public float AlertThreshold = 0.45f;
    public float HostilityThreshold = 0.65f;
    public float FearThreshold = 0.60f;
    public float ThresholdOpenTension = 0.55f;
    public float CrisisTension = 0.70f;
    public float RenewalThreshold = 0.70f;
    public float CollapseThreshold = 0.85f;

    [Header("Archetypal Cycle")]
    public OUTL_EgregoreArchetypeDef ArchetypeProfile;
    public OUTL_EgregoreArchetypalCycle ArchetypalCycle = new OUTL_EgregoreArchetypalCycle();
    public OUTL_EgregoreArchetypePressure[] InitialArchetypePressures;
    public OUTL_EgregoreTransformationRule[] TransformationRules;
    public OUTL_EgregoreShadowRule[] ShadowRules;
    public OUTL_EgregoreIntegrationRule[] IntegrationRules;

    [Header("Sectors")]
    public int[] OwnedSectorIds;

    public OUTL_EgregoreScale Scale
    {
        get
        {
            if (Scope == OUTL_EgregoreScope.World) return OUTL_EgregoreScale.World;
            if (Scope == OUTL_EgregoreScope.Regional || Scope == OUTL_EgregoreScope.Faction) return OUTL_EgregoreScale.Regional;
            return OUTL_EgregoreScale.Local;
        }
        set
        {
            if (value == OUTL_EgregoreScale.World) Scope = OUTL_EgregoreScope.World;
            else if (value == OUTL_EgregoreScale.Regional) Scope = OUTL_EgregoreScope.Regional;
            else Scope = OUTL_EgregoreScope.Local;
        }
    }

    private void OnValidate()
    {
        UpdateInterval = Mathf.Max(0.1f, UpdateInterval);
        InfluenceRadius = Mathf.Max(0.1f, InfluenceRadius);
        ViolenceWeight = Mathf.Max(0f, ViolenceWeight);
        FearWeight = Mathf.Max(0f, FearWeight);
        ProsperityWeight = Mathf.Max(0f, ProsperityWeight);
        AlertnessWeight = Mathf.Max(0f, AlertnessWeight);
        HostilityWeight = Mathf.Max(0f, HostilityWeight);
        ResourceWeight = Mathf.Max(0f, ResourceWeight);
        AlertThreshold = Mathf.Clamp01(AlertThreshold);
        HostilityThreshold = Mathf.Clamp01(HostilityThreshold);
        FearThreshold = Mathf.Clamp01(FearThreshold);
        ThresholdOpenTension = Mathf.Clamp01(ThresholdOpenTension);
        CrisisTension = Mathf.Clamp01(CrisisTension);
        RenewalThreshold = Mathf.Clamp01(RenewalThreshold);
        CollapseThreshold = Mathf.Clamp01(CollapseThreshold);
        if (ArchetypalCycle == null) ArchetypalCycle = new OUTL_EgregoreArchetypalCycle();
        ArchetypalCycle.Sanitize();
    }
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Egregore/Local Egregore Def", fileName = "OUTL_LocalEgregoreDef")]
public sealed partial class OUTL_LocalEgregoreDef
{
    public string DebugName = "Local Egregore";
    public OUTL_LocalEgregoreArchetype PlaceArchetype = OUTL_LocalEgregoreArchetype.Custom;
    public OUTL_LocalEgregoreDrive[] DominantDrives;
    public OUTL_EgregoreInfluenceRule[] InfluenceRules;
    public OUTL_EgregoreOutputRule[] OutputRules;
    public OUTL_EgregoreQuestHookRule[] QuestHooks;
    public OUTL_EgregoreLootModifier[] LootModifiers;
    public OUTL_EgregoreBehaviorModifier[] BehaviorModifiers;
    public OUTL_EgregoreSpawnModifier[] SpawnModifiers;
}

public enum OUTL_LocalEgregoreArchetype
{
    Custom = 0,
    Forest = 1,
    City = 2,
    Road = 3,
    Market = 4,
    Temple = 5,
    Swamp = 6,
    Battlefield = 7,
    BanditCamp = 8,
    Village = 9,
    Ruin = 10
}

public enum OUTL_LocalEgregoreDrive
{
    Fear = 0,
    Hunger = 1,
    Greed = 2,
    Attraction = 3,
    Violence = 4,
    Protection = 5,
    Curiosity = 6,
    Corruption = 7,
    Prosperity = 8,
    Isolation = 9,
    Ritual = 10,
    Revenge = 11,
    Order = 12,
    Chaos = 13
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Egregore/Archetype Def", fileName = "OUTL_EgregoreArchetypeDef")]
public sealed partial class OUTL_EgregoreArchetypeDef : ScriptableObject
{
    public OUTL_EgregoreArchetypeId Archetype = OUTL_EgregoreArchetypeId.SelfCenter;
    public string DebugName = "Archetype";
    public OUTL_EgregoreArchetypePressure[] DefaultPressures;
}

[System.Serializable]
public sealed class OUTL_EgregoreArchetypalCycle
{
    public OUTL_EgregoreCyclePhase InitialPhase = OUTL_EgregoreCyclePhase.StableWorld;
    public OUTL_EgregoreCyclePhase RenewalFallback = OUTL_EgregoreCyclePhase.Renewal;
    public OUTL_EgregoreCyclePhase CorruptionFallback = OUTL_EgregoreCyclePhase.CorruptionLoop;
    public float TensionDecay = 0.025f;
    public float IntegrationDecay = 0.01f;
    public float MemoryDecay = 0.015f;
    public int MaxMemoryTraces = 32;
    public int MemoryDecayBudget = 8;

    public void Sanitize()
    {
        TensionDecay = Mathf.Max(0f, TensionDecay);
        IntegrationDecay = Mathf.Max(0f, IntegrationDecay);
        MemoryDecay = Mathf.Max(0f, MemoryDecay);
        MaxMemoryTraces = Mathf.Clamp(MaxMemoryTraces, 4, 256);
        MemoryDecayBudget = Mathf.Clamp(MemoryDecayBudget, 1, MaxMemoryTraces);
    }
}

[System.Serializable]
public sealed class OUTL_EgregoreTransformationRule
{
    public OUTL_EgregoreCyclePhase From = OUTL_EgregoreCyclePhase.StableWorld;
    public OUTL_EgregoreCyclePhase To = OUTL_EgregoreCyclePhase.Disturbance;
    public OUTL_EgregoreArchetypeId RequiredArchetype = OUTL_EgregoreArchetypeId.None;
    [Range(0f, 1f)] public float MinTension = 0.5f;
    [Range(0f, 1f)] public float MinCorruption = 0f;
    [Range(0f, 1f)] public float MinIntegration = 0f;
    public string OutputKey = "";
}

[System.Serializable]
public sealed class OUTL_EgregoreShadowRule
{
    public OUTL_EgregoreTraceType TraceType = OUTL_EgregoreTraceType.Death;
    public OUTL_EgregoreArchetypeId ShadowArchetype = OUTL_EgregoreArchetypeId.Shadow;
    [Range(0f, 4f)] public float Pressure = 1f;
    [Range(0f, 1f)] public float Trauma = 0.1f;
    [Range(0f, 1f)] public float Corruption = 0.05f;
}

[System.Serializable]
public sealed class OUTL_EgregoreIntegrationRule
{
    public OUTL_EgregoreQuestHook Hook = OUTL_EgregoreQuestHook.IntegrationQuest;
    public OUTL_EgregoreArchetypeId Archetype = OUTL_EgregoreArchetypeId.SelfCenter;
    [Range(0f, 1f)] public float Integration = 0.25f;
    [Range(0f, 1f)] public float Renewal = 0.15f;
    [Range(0f, 1f)] public float CorruptionRelief = 0.15f;
}

[System.Serializable]
public sealed class OUTL_EgregoreInfluenceRule
{
    public OUTL_EgregoreCyclePhase Phase = OUTL_EgregoreCyclePhase.StableWorld;
    public OUTL_BehaviorModeId BehaviorMode = OUTL_BehaviorModeId.Normal;
    [Range(0f, 1f)] public float Fear;
    [Range(0f, 1f)] public float Aggression;
    [Range(0f, 1f)] public float Morale = 1f;
}

[System.Serializable]
public sealed class OUTL_EgregoreOutputRule
{
    public OUTL_EgregoreCyclePhase Phase = OUTL_EgregoreCyclePhase.StableWorld;
    public OUTL_EgregoreSignalType Signal = OUTL_EgregoreSignalType.None;
    [Range(0f, 1f)] public float MinIntensity = 0.25f;
    public string Key = "";
}

[System.Serializable]
public sealed class OUTL_EgregoreQuestHookRule
{
    public OUTL_EgregoreCyclePhase Phase = OUTL_EgregoreCyclePhase.Call;
    public OUTL_EgregoreQuestHook Hook = OUTL_EgregoreQuestHook.CallQuest;
    [Range(0f, 1f)] public float Availability = 1f;
}

[System.Serializable]
public sealed class OUTL_EgregoreLootModifier
{
    public OUTL_EgregoreCyclePhase Phase = OUTL_EgregoreCyclePhase.StableWorld;
    [Range(0f, 4f)] public float ChanceMultiplier = 1f;
    [Range(0f, 4f)] public float CountMultiplier = 1f;
    public string ContextTag = "";
}

[System.Serializable]
public sealed class OUTL_EgregoreBehaviorModifier
{
    public OUTL_EgregoreCyclePhase Phase = OUTL_EgregoreCyclePhase.StableWorld;
    public OUTL_BehaviorModeId BehaviorMode = OUTL_BehaviorModeId.Normal;
    [Range(0f, 1f)] public float Priority = 0.5f;
}

[System.Serializable]
public sealed class OUTL_EgregoreSpawnModifier
{
    public OUTL_EgregoreCyclePhase Phase = OUTL_EgregoreCyclePhase.StableWorld;
    [Range(0f, 1f)] public float SpawnPressure = 0.25f;
    public string EncounterKey = "";
}
