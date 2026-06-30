using System.Collections.Generic;
using UnityEngine;

public struct OUTL_QueuedCommand
{
    public OUTL_Command Command;
    public float ExecuteAt;
    public string TargetName;
    public bool UseTargetName;
}

public sealed class OUTL_EventBus
{
    private const int EventTypeCount = 32;

    private OUTL_World world;
    private readonly List<OUTL_Event> pending = new List<OUTL_Event>(256);
    private readonly List<OUTL_Event> flushing = new List<OUTL_Event>(256);
    private readonly List<OUTL_IEventListener> listeners = new List<OUTL_IEventListener>(256);
    private readonly List<OUTL_IEventListener>[] typedListeners = new List<OUTL_IEventListener>[EventTypeCount];
    private int pendingHead;

    public int MaxEventsPerFlush = 512;
    public int PendingCount { get { return Mathf.Max(0, pending.Count - pendingHead); } }

    public OUTL_EventBus()
    {
        for (int i = 0; i < typedListeners.Length; i++) typedListeners[i] = new List<OUTL_IEventListener>(32);
    }

    public void Bind(OUTL_World world) { this.world = world; }

    public void Emit(in OUTL_Event evt)
    {
        pending.Add(evt);
        OUTL_Profile.Frame.EventsEmitted++;
        if (OUTL_DebugLog.EventTrace)
            OUTL_DebugLog.Log(OUTL_DebugChannel.Events, "event " + evt.Type + " src=" + evt.Source + " dst=" + evt.Target + " key=" + evt.Key + " i=" + evt.IntValue + " f=" + evt.FloatValue.ToString("0.00"));
    }

    public void Register(OUTL_IEventListener listener)
    {
        if (listener != null && !listeners.Contains(listener)) listeners.Add(listener);
    }

    public void Register(OUTL_IEventListener listener, OUTL_EventType eventType)
    {
        if (listener == null) return;
        int index = (int)eventType;
        if (index < 0 || index >= typedListeners.Length)
        {
            Register(listener);
            return;
        }
        List<OUTL_IEventListener> list = typedListeners[index];
        if (!list.Contains(listener)) list.Add(listener);
    }

    public void Unregister(OUTL_IEventListener listener)
    {
        listeners.Remove(listener);
        for (int i = 0; i < typedListeners.Length; i++) typedListeners[i].Remove(listener);
    }

    public void Flush()
    {
        using (OUTL_Profile.EventFlush.Auto())
        {
            int available = PendingCount;
            if (available <= 0) return;

            int count = Mathf.Min(Mathf.Max(1, MaxEventsPerFlush), available);
            flushing.Clear();
            int end = pendingHead + count;
            for (int i = pendingHead; i < end; i++) flushing.Add(pending[i]);
            pendingHead = end;

            if (pendingHead >= pending.Count)
            {
                pending.Clear();
                pendingHead = 0;
            }
            else if (pendingHead > 512 && pendingHead > pending.Count / 2)
            {
                pending.RemoveRange(0, pendingHead);
                pendingHead = 0;
            }

            for (int e = 0; e < flushing.Count; e++)
            {
                OUTL_Event evt = flushing[e];
                OUTL_Profile.Frame.EventsFlushed++;
                DispatchTo(listeners, evt);

                int index = (int)evt.Type;
                if (index >= 0 && index < typedListeners.Length)
                    DispatchTo(typedListeners[index], evt);
            }
            flushing.Clear();
        }
    }

    private void DispatchTo(List<OUTL_IEventListener> targetListeners, in OUTL_Event evt)
    {
        for (int i = targetListeners.Count - 1; i >= 0; i--)
        {
            OUTL_IEventListener l = targetListeners[i];
            if (l == null) { targetListeners.RemoveAt(i); continue; }
            l.OUTL_OnEvent(evt, world);
        }
    }
}

public sealed class OUTL_CommandSystem
{
    private OUTL_World world;
    private readonly List<OUTL_QueuedCommand> queue = new List<OUTL_QueuedCommand>(128);
    private readonly List<OUTL_EntityRuntime> targetBuffer = new List<OUTL_EntityRuntime>(16);
    private readonly HashSet<string> legacyScannedTargetNames = new HashSet<string>();
    public bool AllowLegacySceneScanOnTargetMiss;
    public int MaxQueuedCommandsPerTick = 64;
    public int QueuedCount { get { return queue.Count; } }

    public void Bind(OUTL_World world) { this.world = world; }

    public void Queue(in OUTL_Command command, float delay)
    {
        OUTL_QueuedCommand q = new OUTL_QueuedCommand { Command = command, ExecuteAt = (world != null ? world.WorldTime : Time.time) + Mathf.Max(0f, delay), UseTargetName = false, TargetName = string.Empty };
        queue.Add(q);
        OUTL_Profile.Frame.QueuedCommands++;
    }

    public void QueueToTargetName(string targetName, in OUTL_Command command, float delay)
    {
        if (string.IsNullOrEmpty(targetName)) return;
        OUTL_QueuedCommand q = new OUTL_QueuedCommand { Command = command, ExecuteAt = (world != null ? world.WorldTime : Time.time) + Mathf.Max(0f, delay), UseTargetName = true, TargetName = targetName };
        queue.Add(q);
        OUTL_Profile.Frame.QueuedCommands++;
    }

    public void TickQueue(float worldTime)
    {
        if (world == null || queue.Count == 0) return;
        int budget = Mathf.Max(1, MaxQueuedCommandsPerTick);
        int processed = 0;
        for (int i = queue.Count - 1; i >= 0 && processed < budget; i--)
        {
            OUTL_QueuedCommand q = queue[i];
            if (q.ExecuteAt > worldTime) continue;
            int last = queue.Count - 1;
            queue[i] = queue[last];
            queue.RemoveAt(last);
            processed++;

            if (q.UseTargetName) SendToTargetName(q.TargetName, q.Command);
            else Send(q.Command);
        }
    }

    public void ClearQueue()
    {
        queue.Clear();
    }

    public int SendToTargetName(string targetName, in OUTL_Command command)
    {
        if (world == null || string.IsNullOrEmpty(targetName)) return 0;
        world.Registry.CopyByTargetName(targetName, targetBuffer);
        if (targetBuffer.Count == 0)
        {
            LazyRegisterTargetName(targetName);
            world.Registry.CopyByTargetName(targetName, targetBuffer);
        }
        int sent = 0;
        for (int i = 0; i < targetBuffer.Count; i++)
        {
            OUTL_EntityRuntime target = targetBuffer[i];
            if (target == null || !target.Id.IsValid) continue;
            OUTL_Command c = command;
            c.Target = target.Id;
            if (!c.Source.IsValid) c.Source = OUTL_EntityId.None;
            if (Send(c)) sent++;
        }
        targetBuffer.Clear();
        return sent;
    }

    private void LazyRegisterTargetName(string targetName)
    {
        if (world == null || string.IsNullOrEmpty(targetName)) return;
        if (!AllowLegacySceneScanOnTargetMiss) return;
        if (!legacyScannedTargetNames.Add(targetName)) return;
        OUTL_EntityAdapter[] adapters = Object.FindObjectsOfType<OUTL_EntityAdapter>(true);
        for (int i = 0; i < adapters.Length; i++)
        {
            OUTL_EntityAdapter adapter = adapters[i];
            if (adapter == null || !adapter.isActiveAndEnabled) continue;
            if (adapter.TargetName != targetName) continue;
            adapter.RebuildCommandReceiverCache();
            adapter.RebuildCommandGuardCache();
            adapter.RegisterNow(world);
        }
    }

    public bool Send(in OUTL_Command command)
    {
        using (OUTL_Profile.CommandSend.Auto())
        {
            OUTL_Profile.Frame.CommandsSent++;
            OUTL_EntityRuntime target;
            if (world == null || !world.Registry.TryGet(command.Target, out target)) return false;

            OUTL_ICommandGuard[] guards = target.Adapter != null ? target.Adapter.CommandGuards : null;
            OUTL_ICommandGuard deniedBy = null;
            if (guards != null)
            {
                for (int i = 0; i < guards.Length; i++)
                {
                    OUTL_ICommandGuard guard = guards[i];
                    if (guard == null || guard.OUTL_Allows(command, world)) continue;
                    deniedBy = guard;
                    break;
                }
            }

            if (deniedBy != null)
            {
                deniedBy.OUTL_OnCommandDenied(command, world);
                return false;
            }

            bool handled = false;
            if (target.Adapter != null)
            {
                OUTL_ICommandReceiver[] receivers = target.Adapter.CommandReceivers;
                for (int i = 0; i < receivers.Length; i++)
                {
                    if (receivers[i] != null && receivers[i].OUTL_CanReceive(command, world))
                    {
                        receivers[i].OUTL_Receive(command, world);
                        handled = true;
                    }
                }
            }

            if (!handled) handled = TryRunActions(target, command);
            if (!handled) handled = TryRunModules(target, command);

            if (handled)
            {
                if (guards != null)
                {
                    for (int i = 0; i < guards.Length; i++)
                        if (guards[i] != null)
                            guards[i].OUTL_OnCommandAccepted(command, world);
                }
                OUTL_Profile.Frame.CommandsHandled++;
                int eventIntValue = command.IntValue != 0 ? command.IntValue : (int)command.Type;
                string eventKey = string.IsNullOrEmpty(command.Key) ? command.Type.ToString() : command.Key;
                world.Events.Emit(new OUTL_Event(OUTL_EventType.CommandExecuted, command.Source, command.Target) { Key = eventKey, FloatValue = command.FloatValue, IntValue = eventIntValue, Point = command.Point });
            }
            return handled;
        }
    }

    private bool TryRunActions(OUTL_EntityRuntime target, in OUTL_Command command)
    {
        if (target == null || target.Def == null || target.Def.Actions == null) return false;
        bool handled = false;
        for (int i = 0; i < target.Def.Actions.Length; i++)
        {
            OUTL_ActionDef action = target.Def.Actions[i];
            if (action == null || action.TriggerCommand != command.Type) continue;
            if (!OUTL_Rules.CheckAll(action.Conditions, command.Source, command.Target, world)) continue;
            world.Effects.ApplyAll(action.Effects, command.Source, command.Target, command.Point);
            handled = true;
        }
        return handled;
    }

    private bool TryRunModules(OUTL_EntityRuntime target, in OUTL_Command command)
    {
        if (target == null || target.Def == null || target.Def.Modules == null) return false;
        bool handled = false;
        for (int m = 0; m < target.Def.Modules.Length; m++)
        {
            OUTL_ModuleDef module = target.Def.Modules[m];
            if (module == null || module.HandledCommands == null) continue;
            bool accepts = false;
            for (int c = 0; c < module.HandledCommands.Length; c++) if (module.HandledCommands[c] == command.Type) { accepts = true; break; }
            if (!accepts) continue;
            world.Effects.ApplyAll(module.OnCommandEffects, command.Source, command.Target, command.Point);
            handled = true;
        }
        return handled;
    }
}

public sealed class OUTL_EffectSystem
{
    private OUTL_World world;
    public void Bind(OUTL_World world) { this.world = world; }

    public void ApplyAll(OUTL_EffectDef[] effects, OUTL_EntityId source, OUTL_EntityId target, Vector3 point)
    {
        using (OUTL_Profile.EffectsApplyAll.Auto())
        {
            if (effects == null) return;
            for (int i = 0; i < effects.Length; i++) Apply(effects[i], source, target, point);
        }
    }

    public void Apply(OUTL_EffectDef effect, OUTL_EntityId source, OUTL_EntityId target, Vector3 point)
    {
        using (OUTL_Profile.EffectApply.Auto())
        {
            if (effect == null || world == null) return;
            OUTL_Profile.Frame.EffectsApplied++;
            OUTL_EntityId realTarget = effect.TargetSource ? source : target;
            if (effect.TargetSelf) realTarget = target;

            OUTL_EntityRuntime runtime;
            world.Registry.TryGet(realTarget, out runtime);

            switch (effect.Type)
            {
                case OUTL_EffectType.Damage:
                    OUTL_Combat.ApplyDamage(source, realTarget, Mathf.Abs(effect.FloatValue), point, effect.Key);
                    break;
                case OUTL_EffectType.Heal:
                    if (runtime != null)
                    {
                        runtime.Stats.Add(OUTL_StatId.Health, Mathf.Abs(effect.FloatValue));
                        world.Events.Emit(new OUTL_Event(OUTL_EventType.Healed, source, realTarget) { FloatValue = effect.FloatValue, Point = point });
                    }
                    break;
                case OUTL_EffectType.ModifyStat:
                    if (runtime != null) runtime.Stats.Add(effect.Key, effect.FloatValue);
                    break;
                case OUTL_EffectType.SetStateBool:
                    if (runtime != null) runtime.State.SetFlag(effect.Key, effect.IntValue != 0);
                    break;
                case OUTL_EffectType.SetStateFloat:
                    if (runtime != null) runtime.State.SetFloat(effect.Key, effect.FloatValue);
                    break;
                case OUTL_EffectType.AddItem:
                    world.Inventory.AddItem(realTarget, effect.ItemDef, Mathf.Max(1, effect.IntValue));
                    break;
                case OUTL_EffectType.RemoveItem:
                    world.Inventory.RemoveItem(realTarget, effect.ItemDef, Mathf.Max(1, effect.IntValue));
                    break;
                case OUTL_EffectType.SendCommand:
                    world.Commands.Send(new OUTL_Command(effect.CommandType, source, realTarget) { Key = effect.Key, FloatValue = effect.FloatValue, IntValue = effect.IntValue, Point = point });
                    break;
                case OUTL_EffectType.SendEvent:
                    world.Events.Emit(new OUTL_Event(effect.EventType, source, realTarget) { Key = effect.Key, FloatValue = effect.FloatValue, IntValue = effect.IntValue, Point = point });
                    break;
                case OUTL_EffectType.SpawnPrefab:
                    if (effect.EntityDef != null) world.Spawn(effect.EntityDef, point, Quaternion.identity);
                    else if (effect.Prefab != null) OUTL_PoolSystem.SpawnShared(effect.Prefab, point, Quaternion.identity);
                    break;
                case OUTL_EffectType.PlaySound:
                    if (effect.AudioClip != null) OUTL_PoolSystem.PlayClipShared(effect.AudioClip, point);
                    break;
                case OUTL_EffectType.SetQuestStage:
                    world.Quests.SetStage(effect.Key, effect.IntValue);
                    break;
            }
        }
    }
}

public static class OUTL_Rules
{
    public static bool CheckAll(OUTL_ConditionDef[] conditions, OUTL_EntityId source, OUTL_EntityId target, OUTL_World world)
    {
        if (conditions == null) return true;
        for (int i = 0; i < conditions.Length; i++) if (!Check(conditions[i], source, target, world)) return false;
        return true;
    }

    public static bool Check(OUTL_ConditionDef condition, OUTL_EntityId source, OUTL_EntityId target, OUTL_World world)
    {
        if (condition == null || world == null) return true;
        OUTL_EntityId subject = condition.Subject == OUTL_ConditionSubject.Source ? source : target;
        OUTL_EntityRuntime runtime;
        world.Registry.TryGet(subject, out runtime);
        bool result = false;
        switch (condition.Op)
        {
            case OUTL_ConditionOp.Exists:
                result = runtime != null && runtime.State.Has(condition.Key);
                break;
            case OUTL_ConditionOp.NotEmpty:
                result = runtime != null && !string.IsNullOrEmpty(runtime.State.GetString(condition.Key));
                break;
            case OUTL_ConditionOp.EqualsFloat:
                result = runtime != null && Mathf.Approximately(runtime.Stats.Get(condition.Key, runtime.State.GetFloat(condition.Key)), condition.FloatValue);
                break;
            case OUTL_ConditionOp.GreaterOrEqual:
                result = runtime != null && runtime.Stats.Get(condition.Key, runtime.State.GetFloat(condition.Key)) >= condition.FloatValue;
                break;
            case OUTL_ConditionOp.LessOrEqual:
                result = runtime != null && runtime.Stats.Get(condition.Key, runtime.State.GetFloat(condition.Key)) <= condition.FloatValue;
                break;
            case OUTL_ConditionOp.EqualsString:
                result = runtime != null && runtime.State.GetString(condition.Key) == condition.StringValue;
                break;
            case OUTL_ConditionOp.HasAnyTag:
                result = runtime != null && condition.RequiredTags.MatchesAny(runtime.Tags);
                break;
            case OUTL_ConditionOp.HasItem:
                result = subject.IsValid && condition.ItemDef != null && world.Inventory.HasItem(subject, condition.ItemDef, Mathf.Max(1, condition.IntValue));
                break;
            case OUTL_ConditionOp.QuestStageEquals:
                result = !string.IsNullOrEmpty(condition.Key) && world.Quests.GetStage(condition.Key) == condition.IntValue;
                break;
            case OUTL_ConditionOp.QuestStageAtLeast:
                result = !string.IsNullOrEmpty(condition.Key) && world.Quests.GetStage(condition.Key) >= condition.IntValue;
                break;
            case OUTL_ConditionOp.StateFlagEquals:
                result = runtime != null && runtime.State.GetFlag(condition.Key, false) == (condition.IntValue != 0);
                break;
            case OUTL_ConditionOp.HasAllTags:
                result = runtime != null && HasAllTags(runtime, condition.RequiredTags.Tags);
                break;
        }
        return condition.Invert ? !result : result;
    }

    private static bool HasAllTags(OUTL_EntityRuntime runtime, string[] tags)
    {
        if (runtime == null || tags == null || tags.Length == 0) return false;
        for (int i = 0; i < tags.Length; i++)
            if (!string.IsNullOrEmpty(tags[i]) && !runtime.HasTag(tags[i]))
                return false;
        return true;
    }
}
