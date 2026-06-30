using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OUTL_InventoryItemSnapshot
{
    public OUTL_ItemDef Item;
    public int Count;
}

public sealed class OUTL_InventorySystem
{
    private OUTL_World world;
    private readonly Dictionary<int, List<Stack>> inventories = new Dictionary<int, List<Stack>>(128);

    private struct Stack { public OUTL_ItemDef Item; public int Count; }

    public void Bind(OUTL_World world) { this.world = world; }

    public void AddItem(OUTL_EntityId owner, OUTL_ItemDef item, int count)
    {
        if (!owner.IsValid || item == null || count <= 0) return;
        int addedCount = count;
        List<Stack> list = GetList(owner);
        int max = UnityEngine.Mathf.Max(1, item.MaxStack);
        for (int i = 0; i < list.Count; i++)
        {
            Stack stack = list[i];
            if (stack.Item == item && stack.Count < max)
            {
                int add = UnityEngine.Mathf.Min(count, max - stack.Count);
                stack.Count += add;
                count -= add;
                list[i] = stack;
                if (count <= 0) break;
            }
        }
        while (count > 0)
        {
            int add = UnityEngine.Mathf.Min(count, max);
            list.Add(new Stack { Item = item, Count = add });
            count -= add;
        }
        if (world != null) world.Events.Emit(new OUTL_Event(OUTL_EventType.ItemAdded, owner, owner) { Key = item.name, IntValue = addedCount });
    }

    public bool RemoveItem(OUTL_EntityId owner, OUTL_ItemDef item, int count)
    {
        if (!owner.IsValid || item == null || count <= 0) return false;
        if (CountItem(owner, item) < count) return false;
        List<Stack> list;
        if (!inventories.TryGetValue(owner.Value, out list)) return false;
        int remaining = count;
        for (int i = list.Count - 1; i >= 0 && remaining > 0; i--)
        {
            Stack stack = list[i];
            if (stack.Item != item) continue;
            int take = UnityEngine.Mathf.Min(remaining, stack.Count);
            stack.Count -= take;
            remaining -= take;
            if (stack.Count <= 0) list.RemoveAt(i); else list[i] = stack;
        }
        if (remaining <= 0 && world != null) world.Events.Emit(new OUTL_Event(OUTL_EventType.ItemRemoved, owner, owner) { Key = item.name, IntValue = count });
        return remaining <= 0;
    }

    public bool TryConsume(OUTL_EntityId owner, OUTL_ItemDef item, int count = 1)
    {
        return RemoveItem(owner, item, Mathf.Max(1, count));
    }

    public bool HasItem(OUTL_EntityId owner, OUTL_ItemDef item, int count = 1)
    {
        return CountItem(owner, item) >= count;
    }

    public int CountItem(OUTL_EntityId owner, OUTL_ItemDef item)
    {
        if (!owner.IsValid || item == null) return 0;
        List<Stack> list;
        if (!inventories.TryGetValue(owner.Value, out list)) return 0;
        int total = 0;
        for (int i = 0; i < list.Count; i++) if (list[i].Item == item) total += list[i].Count;
        return total;
    }

    public int CopyItems(OUTL_EntityId owner, List<OUTL_InventoryItemSnapshot> output)
    {
        if (output == null) return 0;
        if (!owner.IsValid) { TrimOutput(output, 0); return 0; }
        List<Stack> list;
        if (!inventories.TryGetValue(owner.Value, out list)) { TrimOutput(output, 0); return 0; }

        int write = 0;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Item == null || list[i].Count <= 0) continue;
            OUTL_InventoryItemSnapshot snapshot;
            if (write < output.Count)
            {
                snapshot = output[write];
                if (snapshot == null)
                {
                    snapshot = new OUTL_InventoryItemSnapshot();
                    output[write] = snapshot;
                }
            }
            else
            {
                snapshot = new OUTL_InventoryItemSnapshot();
                output.Add(snapshot);
            }

            snapshot.Item = list[i].Item;
            snapshot.Count = list[i].Count;
            write++;
        }

        TrimOutput(output, write);
        return write;
    }

    public void Clear(OUTL_EntityId owner)
    {
        if (!owner.IsValid) return;
        inventories.Remove(owner.Value);
    }

    private List<Stack> GetList(OUTL_EntityId owner)
    {
        List<Stack> list;
        if (!inventories.TryGetValue(owner.Value, out list))
        {
            list = new List<Stack>(16);
            inventories[owner.Value] = list;
        }
        return list;
    }

    private static void TrimOutput(List<OUTL_InventoryItemSnapshot> output, int count)
    {
        if (output == null) return;
        for (int i = count; i < output.Count; i++)
        {
            if (output[i] == null) continue;
            output[i].Item = null;
            output[i].Count = 0;
        }

        if (output.Count > count) output.RemoveRange(count, output.Count - count);
    }
}

public sealed class OUTL_QuestSystem : OUTL_IEventListener
{
    private OUTL_World world;
    private readonly Dictionary<string, int> stages = new Dictionary<string, int>(64);
    private readonly Dictionary<string, int> objectiveCounts = new Dictionary<string, int>(128);
    private readonly List<OUTL_QuestDef> quests = new List<OUTL_QuestDef>(64);

    public int QuestCount { get { return quests.Count; } }

    public void Bind(OUTL_World world)
    {
        this.world = world;
        world.Events.Register(this);
    }

    public void AddQuest(OUTL_QuestDef quest)
    {
        if (quest != null && !quests.Contains(quest)) quests.Add(quest);
    }

    public int GetStage(string questId)
    {
        int stage;
        return !string.IsNullOrEmpty(questId) && stages.TryGetValue(questId, out stage) ? stage : 0;
    }

    public void SetStage(string questId, int stage)
    {
        if (string.IsNullOrEmpty(questId)) return;
        int previous = GetStage(questId);
        stages[questId] = stage;
        if (world == null) return;

        OUTL_QuestDef quest = FindQuest(questId);
        OUTL_QuestStageDef stageDef = FindStage(quest, stage);
        OUTL_EgregoreQuestHook hook = OUTL_QuestRewardResolver.ResolveHook(quest, stageDef);
        world.Events.Emit(new OUTL_Event(OUTL_EventType.QuestStageChanged, OUTL_EntityId.None, OUTL_EntityId.None) { Key = questId, IntValue = stage });
        if (previous == 0 && stage != 0)
            world.Events.Emit(new OUTL_Event(OUTL_EventType.QuestStarted, OUTL_EntityId.None, OUTL_EntityId.None) { Key = questId, IntValue = (int)hook });
        if (IsCompletedStage(quest, stageDef, stage))
            world.Events.Emit(new OUTL_Event(OUTL_EventType.QuestCompleted, OUTL_EntityId.None, OUTL_EntityId.None) { Key = questId, IntValue = (int)hook });
        else if (IsFailedStage(quest, stageDef, stage))
            world.Events.Emit(new OUTL_Event(OUTL_EventType.QuestFailed, OUTL_EntityId.None, OUTL_EntityId.None) { Key = questId, IntValue = (int)hook });
    }

    public void CopyStages(List<OUTL_QuestSaveRecord> output)
    {
        if (output == null) return;
        output.Clear();
        foreach (KeyValuePair<string, int> pair in stages)
        {
            OUTL_QuestSaveRecord record = new OUTL_QuestSaveRecord { QuestId = pair.Key, Stage = pair.Value };
            CopyObjectiveCounts(pair.Key, record.Objectives);
            output.Add(record);
        }
    }

    public void RestoreStages(List<OUTL_QuestSaveRecord> input)
    {
        stages.Clear();
        objectiveCounts.Clear();
        if (input == null) return;
        for (int i = 0; i < input.Count; i++)
            if (input[i] != null && !string.IsNullOrEmpty(input[i].QuestId))
            {
                stages[input[i].QuestId] = input[i].Stage;
                if (input[i].Objectives != null)
                    for (int o = 0; o < input[i].Objectives.Count; o++)
                        objectiveCounts[BuildObjectiveKey(input[i].QuestId, input[i].Objectives[o].Key)] = input[i].Objectives[o].Value;
            }
    }

    public void Tick(float time, float deltaTime) { }

    public void OUTL_OnEvent(in OUTL_Event evt, OUTL_World world)
    {
        for (int q = 0; q < quests.Count; q++)
        {
            OUTL_QuestDef quest = quests[q];
            if (quest == null) continue;
            ProcessObjectives(quest, evt, world);
            if (quest.Stages == null) continue;
            int current = GetStage(quest.QuestId);
            for (int s = 0; s < quest.Stages.Length; s++)
            {
                OUTL_QuestStageDef stage = quest.Stages[s];
                if (stage == null || stage.Stage != current) continue;
                if (!OUTL_QuestConditionEvaluator.Check(stage, evt, world)) continue;
                world.Effects.ApplyAll(stage.OnEventEffects, evt.Source, evt.Target, evt.Point);
            }
        }
    }

    private void ProcessObjectives(OUTL_QuestDef quest, in OUTL_Event evt, OUTL_World world)
    {
        if (quest == null || quest.Objectives == null || quest.Objectives.Length == 0) return;
        int current = GetStage(quest.QuestId);
        if (current == quest.CompletedStage || current == quest.FailedStage) return;

        bool changed = false;
        for (int i = 0; i < quest.Objectives.Length; i++)
        {
            OUTL_QuestObjectiveDef objective = quest.Objectives[i];
            if (objective == null || !MatchesObjective(objective, evt, world)) continue;
            string key = BuildObjectiveKey(quest.QuestId, objective.ObjectiveId);
            int currentCount;
            objectiveCounts.TryGetValue(key, out currentCount);
            int add = ResolveObjectiveDelta(objective, evt);
            int next = Mathf.Min(Mathf.Max(1, objective.RequiredCount), currentCount + add);
            if (next == currentCount) continue;
            objectiveCounts[key] = next;
            changed = true;
        }

        if (!changed) return;
        if (current == 0) SetStage(quest.QuestId, 1);
        if (AllObjectivesComplete(quest)) SetStage(quest.QuestId, quest.CompletedStage);
    }

    private static bool MatchesObjective(OUTL_QuestObjectiveDef objective, in OUTL_Event evt, OUTL_World world)
    {
        if (objective == null) return false;
        if (!string.IsNullOrEmpty(objective.ListenKey) && objective.ListenKey != evt.Key) return false;
        if (!OUTL_Rules.CheckAll(objective.Conditions, evt.Source, evt.Target, world)) return false;
        switch (objective.Type)
        {
            case OUTL_QuestObjectiveType.Kill:
                return evt.Type == OUTL_EventType.Killed;
            case OUTL_QuestObjectiveType.Collect:
                return evt.Type == OUTL_EventType.PickedUp || evt.Type == OUTL_EventType.ItemAdded || evt.Type == OUTL_EventType.ItemTaken;
            case OUTL_QuestObjectiveType.OpenChest:
                return evt.Type == OUTL_EventType.ContainerOpened || evt.Type == OUTL_EventType.ContainerLooted;
            case OUTL_QuestObjectiveType.Interact:
                return evt.Type == OUTL_EventType.Used || evt.Type == OUTL_EventType.ContainerOpened || evt.Type == OUTL_EventType.Custom;
            case OUTL_QuestObjectiveType.Reach:
                return evt.Type == OUTL_EventType.Custom || evt.Type == OUTL_EventType.Signal;
            case OUTL_QuestObjectiveType.Call:
            case OUTL_QuestObjectiveType.Threshold:
            case OUTL_QuestObjectiveType.Trial:
            case OUTL_QuestObjectiveType.Shadow:
            case OUTL_QuestObjectiveType.Sacrifice:
            case OUTL_QuestObjectiveType.Boon:
            case OUTL_QuestObjectiveType.Return:
            case OUTL_QuestObjectiveType.Integration:
                return evt.Type == OUTL_EventType.QuestStageChanged || evt.Type == OUTL_EventType.Custom || evt.Type == OUTL_EventType.Signal;
            default:
                return evt.Type == OUTL_EventType.Custom || evt.Type == OUTL_EventType.Signal;
        }
    }

    private static int ResolveObjectiveDelta(OUTL_QuestObjectiveDef objective, in OUTL_Event evt)
    {
        if (objective != null && objective.Type == OUTL_QuestObjectiveType.Collect && evt.IntValue > 0) return evt.IntValue;
        return 1;
    }

    private bool AllObjectivesComplete(OUTL_QuestDef quest)
    {
        if (quest == null || quest.Objectives == null || quest.Objectives.Length == 0) return false;
        for (int i = 0; i < quest.Objectives.Length; i++)
        {
            OUTL_QuestObjectiveDef objective = quest.Objectives[i];
            if (objective == null) continue;
            int count;
            objectiveCounts.TryGetValue(BuildObjectiveKey(quest.QuestId, objective.ObjectiveId), out count);
            if (count < Mathf.Max(1, objective.RequiredCount)) return false;
        }
        return true;
    }

    private void CopyObjectiveCounts(string questId, List<OUTL_IntPair> output)
    {
        if (output == null || string.IsNullOrEmpty(questId)) return;
        output.Clear();
        string prefix = questId + "/";
        foreach (KeyValuePair<string, int> pair in objectiveCounts)
            if (pair.Key.StartsWith(prefix, StringComparison.Ordinal))
                output.Add(new OUTL_IntPair { Key = pair.Key.Substring(prefix.Length), Value = pair.Value });
    }

    private static string BuildObjectiveKey(string questId, string objectiveId)
    {
        return (questId ?? string.Empty) + "/" + (string.IsNullOrEmpty(objectiveId) ? "objective" : objectiveId);
    }

    private OUTL_QuestDef FindQuest(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return null;
        for (int i = 0; i < quests.Count; i++)
            if (quests[i] != null && quests[i].QuestId == questId)
                return quests[i];
        return null;
    }

    private static OUTL_QuestStageDef FindStage(OUTL_QuestDef quest, int stage)
    {
        if (quest == null || quest.Stages == null) return null;
        for (int i = 0; i < quest.Stages.Length; i++)
            if (quest.Stages[i] != null && quest.Stages[i].Stage == stage)
                return quest.Stages[i];
        return null;
    }

    private static bool IsCompletedStage(OUTL_QuestDef quest, OUTL_QuestStageDef stageDef, int stage)
    {
        if (stageDef != null && stageDef.CompletesQuest) return true;
        return quest != null && stage == quest.CompletedStage;
    }

    private static bool IsFailedStage(OUTL_QuestDef quest, OUTL_QuestStageDef stageDef, int stage)
    {
        if (stageDef != null && stageDef.FailsQuest) return true;
        return quest != null && stage == quest.FailedStage;
    }
}

[DisallowMultipleComponent]
public sealed class OUTL_QuestBootstrap : MonoBehaviour
{
    public OUTL_QuestDef[] Quests;
    public bool RegisterOnEnable = true;

    private void OnEnable()
    {
        if (!RegisterOnEnable || OUTL_World.Instance == null || Quests == null) return;
        for (int i = 0; i < Quests.Length; i++)
            if (Quests[i] != null)
                OUTL_World.Instance.Quests.AddQuest(Quests[i]);
    }
}
