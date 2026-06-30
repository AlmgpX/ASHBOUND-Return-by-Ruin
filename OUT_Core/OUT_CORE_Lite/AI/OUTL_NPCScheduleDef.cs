using System;
using UnityEngine;

public enum OUTL_NPCScheduleActionType
{
    Idle = 0,
    Sleep = 1,
    Work = 2,
    Patrol = 3,
    TravelTo = 4,
    Trade = 5,
    Guard = 6,
    Eat = 7,
    Loot = 8,
    Wander = 9,
    ReturnHome = 10,
    Flee = 11,
    Investigate = 12,
    Combat = 13
}

public enum OUTL_NPCScheduleTargetMode
{
    None = 0,
    FixedWorldPosition = 1,
    SectorId = 2,
    TargetName = 3,
    EntityClass = 4,
    TagQuery = 5,
    FactionTerritory = 6
}

[CreateAssetMenu(menuName = "OUT CORE Lite/NPC/Schedule Def", fileName = "OUTL_NPCScheduleDef")]
public sealed class OUTL_NPCScheduleDef : ScriptableObject
{
    public string ScheduleId = "npc_schedule";
    public OUTL_NPCScheduleEntry[] Entries;

    public OUTL_NPCScheduleEntry FindEntry(float normalizedDayTime)
    {
        if (Entries == null || Entries.Length == 0) return null;
        float t = Mathf.Repeat(normalizedDayTime, 1f);
        OUTL_NPCScheduleEntry fallback = null;
        for (int i = 0; i < Entries.Length; i++)
        {
            OUTL_NPCScheduleEntry entry = Entries[i];
            if (entry == null) continue;
            if (fallback == null) fallback = entry;
            if (entry.Contains(t)) return entry;
        }
        return fallback;
    }
}

[Serializable]
public sealed class OUTL_NPCScheduleEntry
{
    public string EntryId = "entry";
    [Range(0f, 1f)] public float StartTimeNormalized = 0f;
    [Range(0f, 1f)] public float EndTimeNormalized = 1f;
    public OUTL_NPCScheduleActionType Action = OUTL_NPCScheduleActionType.Idle;
    public OUTL_NPCScheduleTargetMode TargetMode = OUTL_NPCScheduleTargetMode.None;
    public Vector3 TargetPosition;
    public int TargetSectorId;
    public string TargetName;
    public string EntityClass;
    public string[] RequiredTags;
    public OUTL_ConditionDef[] Conditions;
    public OUTL_EffectDef[] OnStartEffects;
    public OUTL_EffectDef[] OnCompleteEffects;
    public bool CanBeInterrupted = true;
    public float MinDuration = 1f;
    public float Priority = 1f;
    public string RouteKey;

    public bool Contains(float normalizedDayTime)
    {
        float start = Mathf.Repeat(StartTimeNormalized, 1f);
        float end = Mathf.Repeat(EndTimeNormalized, 1f);
        float t = Mathf.Repeat(normalizedDayTime, 1f);
        if (Mathf.Approximately(start, end)) return true;
        if (start < end) return t >= start && t < end;
        return t >= start || t < end;
    }
}
