using System;
using UnityEngine;

public enum OUTL_EgregoreScope
{
    Local = 0,
    Regional = 1,
    World = 2,
    Faction = 3
}

public enum OUTL_EgregoreScale
{
    Local = 0,
    Regional = 1,
    World = 2
}

public enum OUTL_EgregoreSignalType
{
    None = 0,
    RaiseAlert = 1,
    SpawnPatrol = 2,
    CalmWildlife = 3,
    IncreaseHostility = 4,
    ChangeAmbientProfile = 5,
    ModifyFactionRelations = 6,
    CyclePhaseChanged = 7,
    OpenThreshold = 8,
    CloseThreshold = 9,
    SpawnPressure = 10,
    QuestHook = 11,
    LootContextChanged = 12,
    BehaviorModePressure = 13,
    RoutePressure = 14,
    CollapseWarning = 15,
    RenewalPulse = 16
}

public enum OUTL_EgregoreMood
{
    Stable = 0,
    Alert = 1,
    Afraid = 2,
    Hostile = 3,
    Prosperous = 4,
    Corrupt = 5,
    Entropic = 6
}

public enum OUTL_EgregoreArchetypeId
{
    None = 0,
    SelfCenter = 1,
    Shadow = 2,
    Persona = 3,
    AnimaAnimus = 4,
    GreatMother = 5,
    FatherAuthority = 6,
    Trickster = 7,
    Hero = 8,
    Mentor = 9,
    ThresholdGuardian = 10,
    Child = 11,
    Devourer = 12,
    WoundedKing = 13,
    Sacrifice = 14,
    Wanderer = 15,
    Lover = 16,
    Warrior = 17,
    Sage = 18,
    Beast = 19,
    VoidDeathRebirth = 20,
    Count = 21
}

public enum OUTL_EgregoreCyclePhase
{
    StableWorld = 0,
    Disturbance = 1,
    Call = 2,
    RefusalOrResistance = 3,
    Threshold = 4,
    Descent = 5,
    Trials = 6,
    ShadowConfrontation = 7,
    Crisis = 8,
    SacrificeOrDeath = 9,
    RevelationOrBoon = 10,
    Return = 11,
    Integration = 12,
    Renewal = 13,
    CorruptionLoop = 14,
    Collapse = 15
}

public enum OUTL_EgregoreQuestHook
{
    None = 0,
    CallQuest = 1,
    ThresholdQuest = 2,
    TrialQuest = 3,
    ShadowQuest = 4,
    SacrificeQuest = 5,
    BoonQuest = 6,
    ReturnQuest = 7,
    IntegrationQuest = 8
}

public enum OUTL_EgregoreTraceType
{
    None = 0,
    Combat = 1,
    Death = 2,
    Theft = 3,
    Betrayal = 4,
    QuestCompleted = 5,
    QuestFailed = 6,
    Ritual = 7,
    Raid = 8,
    Rescue = 9,
    Trade = 10,
    Prosperity = 11,
    Massacre = 12,
    Desire = 13,
    Hunger = 14,
    ResourceDepletion = 15,
    Rumor = 16,
    DayPhase = 17,
    WorldLedger = 18
}

public enum OUTL_BehaviorModeId
{
    Normal = 0,
    Work = 1,
    Trade = 2,
    Patrol = 3,
    Sleep = 4,
    Guard = 5,
    Raid = 6,
    Hide = 7,
    Flee = 8,
    Hunt = 9,
    Ritual = 10,
    Alert = 11,
    Lockdown = 12
}

[Serializable]
public struct OUTL_EgregoreSignal
{
    public string SourceId;
    public string TargetId;
    public OUTL_EgregoreSignalType SignalType;
    public float Intensity;
    public float Ttl;
    public Vector3 Position;
    public string Key;
    public float Time;

    public string SourceEgregore { get { return SourceId; } set { SourceId = value; } }
    public string TargetEgregore { get { return TargetId; } set { TargetId = value; } }
    public string PayloadKey { get { return Key; } set { Key = value; } }
}

[Serializable]
public struct OUTL_EgregoreArchetypePressure
{
    public OUTL_EgregoreArchetypeId Archetype;
    [Range(0f, 1f)] public float Pressure;
    public float DecayRate;
}

[Serializable]
public struct OUTL_EgregoreMemoryTrace
{
    public OUTL_EgregoreTraceType Type;
    public OUTL_EntityId Source;
    public OUTL_WorldCellKey Cell;
    public Vector3 Position;
    public float Intensity;
    public float Time;
    public float DecayRate;
    public string Key;
    public string Tags;

    public bool IsAlive(float time)
    {
        return Intensity > 0.001f && (DecayRate <= 0f || time - Time < Intensity / Mathf.Max(0.001f, DecayRate));
    }
}

[Serializable]
public struct OUTL_EgregoreField
{
    public string EgregoreId;
    public OUTL_WorldCellKey Cell;
    public OUTL_EgregoreMood Mood;
    public OUTL_EgregoreCyclePhase CyclePhase;
    public OUTL_EgregoreArchetypeId DominantArchetype;
    public OUTL_EgregoreArchetypeId ShadowArchetype;
    public float Fear;
    public float Violence;
    public float Prosperity;
    public float Corruption;
    public float Alertness;
    public float Hostility;
    public float Hunger;
    public float Desire;
    public float Safety;
    public float RitualTension;
    public float SpawnPressure;
    public float QuestPressure;
    public float LootPressure;
    public float BehaviorPressure;
    public float LastUpdatedTime;
}

public static class OUTL_EgregoreUtility
{
    public static OUTL_BehaviorModeId BehaviorModeForPhase(OUTL_EgregoreCyclePhase phase)
    {
        switch (phase)
        {
            case OUTL_EgregoreCyclePhase.Threshold:
                return OUTL_BehaviorModeId.Guard;
            case OUTL_EgregoreCyclePhase.Descent:
            case OUTL_EgregoreCyclePhase.Trials:
                return OUTL_BehaviorModeId.Patrol;
            case OUTL_EgregoreCyclePhase.ShadowConfrontation:
                return OUTL_BehaviorModeId.Alert;
            case OUTL_EgregoreCyclePhase.Crisis:
            case OUTL_EgregoreCyclePhase.SacrificeOrDeath:
                return OUTL_BehaviorModeId.Lockdown;
            case OUTL_EgregoreCyclePhase.RevelationOrBoon:
            case OUTL_EgregoreCyclePhase.Return:
            case OUTL_EgregoreCyclePhase.Integration:
                return OUTL_BehaviorModeId.Work;
            case OUTL_EgregoreCyclePhase.Renewal:
                return OUTL_BehaviorModeId.Trade;
            case OUTL_EgregoreCyclePhase.CorruptionLoop:
                return OUTL_BehaviorModeId.Raid;
            case OUTL_EgregoreCyclePhase.Collapse:
                return OUTL_BehaviorModeId.Flee;
            case OUTL_EgregoreCyclePhase.Disturbance:
            case OUTL_EgregoreCyclePhase.Call:
            case OUTL_EgregoreCyclePhase.RefusalOrResistance:
                return OUTL_BehaviorModeId.Alert;
            default:
                return OUTL_BehaviorModeId.Normal;
        }
    }

    public static float SpawnPressureForPhase(OUTL_EgregoreCyclePhase phase, float hostility, float corruption, float trauma)
    {
        float basePressure = Mathf.Clamp01(Mathf.Max(hostility, Mathf.Max(corruption, trauma)));
        switch (phase)
        {
            case OUTL_EgregoreCyclePhase.Descent:
            case OUTL_EgregoreCyclePhase.Trials:
                return Mathf.Clamp01(0.35f + basePressure * 0.45f);
            case OUTL_EgregoreCyclePhase.ShadowConfrontation:
            case OUTL_EgregoreCyclePhase.Crisis:
            case OUTL_EgregoreCyclePhase.SacrificeOrDeath:
                return Mathf.Clamp01(0.55f + basePressure * 0.45f);
            case OUTL_EgregoreCyclePhase.CorruptionLoop:
            case OUTL_EgregoreCyclePhase.Collapse:
                return Mathf.Clamp01(0.7f + basePressure * 0.3f);
            case OUTL_EgregoreCyclePhase.Renewal:
            case OUTL_EgregoreCyclePhase.Integration:
                return 0.05f;
            default:
                return basePressure * 0.2f;
        }
    }

    public static float QuestPressureForPhase(OUTL_EgregoreCyclePhase phase, float tension, float integration, float renewal)
    {
        switch (phase)
        {
            case OUTL_EgregoreCyclePhase.Disturbance:
            case OUTL_EgregoreCyclePhase.Call:
            case OUTL_EgregoreCyclePhase.Threshold:
                return Mathf.Clamp01(0.35f + tension * 0.45f);
            case OUTL_EgregoreCyclePhase.Trials:
            case OUTL_EgregoreCyclePhase.ShadowConfrontation:
            case OUTL_EgregoreCyclePhase.Crisis:
                return Mathf.Clamp01(0.45f + tension * 0.4f);
            case OUTL_EgregoreCyclePhase.RevelationOrBoon:
            case OUTL_EgregoreCyclePhase.Return:
            case OUTL_EgregoreCyclePhase.Integration:
            case OUTL_EgregoreCyclePhase.Renewal:
                return Mathf.Clamp01(0.25f + Mathf.Max(integration, renewal) * 0.45f);
            default:
                return tension * 0.2f;
        }
    }

    public static float LootPressureForPhase(OUTL_EgregoreCyclePhase phase, float boon, float corruption)
    {
        switch (phase)
        {
            case OUTL_EgregoreCyclePhase.Trials:
            case OUTL_EgregoreCyclePhase.ShadowConfrontation:
                return Mathf.Clamp01(0.35f + corruption * 0.3f);
            case OUTL_EgregoreCyclePhase.Crisis:
            case OUTL_EgregoreCyclePhase.SacrificeOrDeath:
            case OUTL_EgregoreCyclePhase.CorruptionLoop:
            case OUTL_EgregoreCyclePhase.Collapse:
                return Mathf.Clamp01(0.45f + corruption * 0.45f);
            case OUTL_EgregoreCyclePhase.RevelationOrBoon:
                return Mathf.Clamp01(0.6f + boon * 0.4f);
            case OUTL_EgregoreCyclePhase.Renewal:
                return 0.35f;
            default:
                return Mathf.Clamp01(boon * 0.2f);
        }
    }

    public static float BehaviorPressureForPhase(OUTL_EgregoreCyclePhase phase, float fear, float hostility, float corruption)
    {
        float pressure = Mathf.Clamp01(Mathf.Max(fear, Mathf.Max(hostility, corruption)));
        switch (phase)
        {
            case OUTL_EgregoreCyclePhase.StableWorld:
                return 0f;
            case OUTL_EgregoreCyclePhase.Renewal:
            case OUTL_EgregoreCyclePhase.Integration:
                return 0.2f;
            case OUTL_EgregoreCyclePhase.Collapse:
            case OUTL_EgregoreCyclePhase.CorruptionLoop:
            case OUTL_EgregoreCyclePhase.Crisis:
                return Mathf.Clamp01(0.55f + pressure * 0.45f);
            default:
                return Mathf.Clamp01(0.25f + pressure * 0.35f);
        }
    }
}
