using System;
using UnityEngine;

[CreateAssetMenu(menuName = "OUT CORE Lite/Faction Def", fileName = "OUTL_FactionDef")]
public partial class OUTL_FactionDef : ScriptableObject
{
    public string FactionId = "neutral";
    public string DisplayName = "Neutral";
    public OUTL_FactionRelation[] Relations;

    public float GetRelationTo(OUTL_FactionDef other)
    {
        if (other == null) return 0f;
        if (other == this) return 1f;
        if (Relations == null) return 0f;
        for (int i = 0; i < Relations.Length; i++)
            if (Relations[i] != null && Relations[i].Faction == other)
                return Relations[i].Relation;
        return 0f;
    }

    public bool IsHostileTo(OUTL_FactionDef other, float threshold = -0.25f)
    {
        return GetRelationTo(other) <= threshold;
    }
}

[Serializable]
public class OUTL_FactionRelation
{
    public OUTL_FactionDef Faction;
    [Range(-1f, 1f)] public float Relation;
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Character Template", fileName = "OUTL_CharacterTemplate")]
public partial class OUTL_CharacterTemplate
{
    public string ClassName = "mob";
    public string DisplayName = "Mob";
    public string[] Tags = new[] { "Actor" };
    public OUTL_FactionDef Faction;
    public OUTL_StatEntry[] Stats = new[] { new OUTL_StatEntry { Key = "Health", Value = 50f }, new OUTL_StatEntry { Key = "Damage", Value = 8f } };
    public OUTL_AIProfile AIProfile;
    public OUTL_ModuleDef[] Modules;
    public GameObject Prefab;
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Entity Def", fileName = "OUTL_EntityDef")]
public partial class OUTL_EntityDef
{
    public string ClassName = "entity";
    public string DisplayName = "Entity";
    public string[] Tags;
    public OUTL_StatEntry[] BaseStats;
    public OUTL_ActionDef[] Actions;
    public OUTL_ModuleDef[] Modules;
    public GameObject Prefab;
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Action Def", fileName = "OUTL_ActionDef")]
public partial class OUTL_ActionDef
{
    public string ActionId = "action";
    public OUTL_CommandType TriggerCommand = OUTL_CommandType.Use;
    public float Cooldown = 0f;
    public OUTL_ConditionDef[] Conditions;
    public OUTL_EffectDef[] Effects;
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Item Def", fileName = "OUTL_ItemDef")]
public partial class OUTL_ItemDef
{
    public int MaxStack = 1;
    public bool Equippable = false;
    public OUTL_ActionDef OnUse;
    public OUTL_ActionDef OnEquip;
    public OUTL_ActionDef OnUnequip;
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Quest Def", fileName = "OUTL_QuestDef")]
public partial class OUTL_QuestDef
{
    public string QuestId = "quest";
    public string DisplayName = "Quest";
    public string DebugName = "Quest";
    public OUTL_EgregoreQuestHook ArchetypalHook = OUTL_EgregoreQuestHook.None;
    public int CompletedStage = 100;
    public int FailedStage = -1;
    public string RequiredEgregore;
    public string[] Tags;
    public OUTL_QuestObjectiveDef[] Objectives;
    public OUTL_QuestStageDef[] Stages;
}

[Serializable]
public class OUTL_QuestStageDef
{
    public int Stage;
    public string Description;
    public OUTL_EgregoreQuestHook ArchetypalHook = OUTL_EgregoreQuestHook.None;
    public bool CompletesQuest;
    public bool FailsQuest;
    public OUTL_EventType ListenEvent;
    public string ListenKey;
    public OUTL_ConditionDef[] Conditions;
    public OUTL_EffectDef[] OnEnterEffects;
    public OUTL_EffectDef[] OnEventEffects;
}

public enum OUTL_QuestObjectiveType
{
    Custom = 0,
    Call = 1,
    Threshold = 2,
    Trial = 3,
    Shadow = 4,
    Sacrifice = 5,
    Boon = 6,
    Return = 7,
    Integration = 8,
    Kill = 9,
    Collect = 10,
    Reach = 11,
    Interact = 12,
    OpenChest = 13
}

[Serializable]
public class OUTL_QuestObjectiveDef
{
    public string ObjectiveId = "objective";
    public OUTL_QuestObjectiveType Type = OUTL_QuestObjectiveType.Custom;
    public string ListenKey;
    public int RequiredCount = 1;
    public OUTL_ConditionDef[] Conditions;
}

[Serializable]
public sealed class OUTL_QuestObjectiveRuntime
{
    public string ObjectiveId = "";
    public int Count;
    public bool Complete;
}

[Serializable]
public sealed class OUTL_QuestRuntime
{
    public string QuestId = "";
    public int Stage;
    public OUTL_EgregoreQuestHook ArchetypalHook = OUTL_EgregoreQuestHook.None;
    public OUTL_QuestObjectiveRuntime[] Objectives;
}

[Serializable]
public sealed class OUTL_QuestLogRuntime
{
    public OUTL_QuestRuntime[] ActiveQuests;
}

public static class OUTL_QuestConditionEvaluator
{
    public static bool Check(OUTL_QuestStageDef stage, OUTL_Event evt, OUTL_World world)
    {
        if (stage == null) return false;
        if (stage.ListenEvent == OUTL_EventType.None || stage.ListenEvent != evt.Type) return false;
        if (!string.IsNullOrEmpty(stage.ListenKey) && stage.ListenKey != evt.Key) return false;
        return OUTL_Rules.CheckAll(stage.Conditions, evt.Source, evt.Target, world);
    }
}

public static class OUTL_QuestRewardResolver
{
    public static OUTL_EgregoreQuestHook ResolveHook(OUTL_QuestDef quest, OUTL_QuestStageDef stage)
    {
        if (stage != null && stage.ArchetypalHook != OUTL_EgregoreQuestHook.None) return stage.ArchetypalHook;
        return quest != null ? quest.ArchetypalHook : OUTL_EgregoreQuestHook.None;
    }
}

[CreateAssetMenu(menuName = "OUT CORE Lite/AI Profile", fileName = "OUTL_AIProfile")]
public partial class OUTL_AIProfile
{
    public string ProfileId = "ai";
    public bool UseFactionHostility = true;
    public string[] EnemyTags;
    public string[] FriendTags;
    public float ViewDistance = 30f;
    public float AttackDistance = 2f;
    public float MoveSpeed = 3f;
    public float ThinkIntervalNear = 0.1f;
    public float ThinkIntervalMid = 0.5f;
    public float ThinkIntervalFar = 2f;
    public OUTL_AIScheduleLite IdleSchedule;
    public OUTL_AIScheduleLite CombatSchedule;
    public OUTL_AIScheduleLite SearchSchedule;
    public OUTL_AIScheduleLite FleeSchedule;
    public float LowHealthThreshold = 15f;
    public OUTL_AIIntentRule[] Rules;
}

[Serializable]
public class OUTL_AIIntentRule
{
    public string Intent = "Idle";
    public OUTL_ConditionDef[] Conditions;
    public OUTL_CommandType Command = OUTL_CommandType.None;
    public OUTL_EffectDef[] Effects;
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Module Def", fileName = "OUTL_ModuleDef")]
public partial class OUTL_ModuleDef
{
    public string ModuleId = "module";
    public OUTL_CommandType[] HandledCommands;
    public OUTL_EffectDef[] OnCommandEffects;
    public OUTL_EffectDef[] OnRandomTickEffects;
}

[Serializable]
public struct OUTL_StatEntry
{
    public string Key;
    public float Value;
}

[Serializable]
public class OUTL_ConditionDef
{
    public string Key;
    public OUTL_ConditionOp Op = OUTL_ConditionOp.Exists;
    public OUTL_ConditionSubject Subject = OUTL_ConditionSubject.Target;
    public float FloatValue;
    public int IntValue = 1;
    public string StringValue;
    public OUTL_ItemDef ItemDef;
    public OUTL_TagMask RequiredTags;
    public bool Invert;
}

public enum OUTL_ConditionSubject : byte
{
    Target = 0,
    Source = 1
}

public enum OUTL_ConditionOp
{
    Exists = 0,
    NotEmpty = 1,
    EqualsFloat = 2,
    GreaterOrEqual = 3,
    LessOrEqual = 4,
    EqualsString = 5,
    HasAnyTag = 6,
    HasItem = 7,
    QuestStageEquals = 8,
    QuestStageAtLeast = 9,
    StateFlagEquals = 10,
    HasAllTags = 11
}

[Serializable]
public class OUTL_EffectDef
{
    public OUTL_EffectType Type = OUTL_EffectType.None;
    public string Key;
    public float FloatValue;
    public int IntValue;
    public string StringValue;
    public OUTL_EntityDef EntityDef;
    public OUTL_ItemDef ItemDef;
    public OUTL_ActionDef ActionDef;
    public OUTL_CommandType CommandType;
    public OUTL_EventType EventType;
    public GameObject Prefab;
    public AudioClip AudioClip;
    public bool TargetSelf;
    public bool TargetSource;
}
