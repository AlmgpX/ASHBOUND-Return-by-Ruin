using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_NPCBehaviorController : MonoBehaviour, OUTL_ITickable, OUTL_IEventListener, OUTL_IComponentSaveParticipant, OUTL_IPoolReset
{
    public OUTL_EntityAdapter Entity;
    public OUTL_AIActor AIActor;
    public OUTL_NavMeshMover NavMover;
    public OUTL_AttackDriver AttackDriver;
    public OUTL_BotInputDriver BotInputDriver;
    public OUTL_ActorControlBridge ActorControlBridge;
    public OUTL_NPCBehaviorModel Model;
    public OUTL_NPCScheduleDef ScheduleOverride;
    public OUTL_NPCNavigationProfile NavigationProfileOverride;
    public OUTL_NPCBehaviorRuntime Runtime = new OUTL_NPCBehaviorRuntime();
    public bool AutoRegister = true;
    [Tooltip("For actor-input NPCs, schedules publish movement intent and the BotInputDriver/ActorControlBridge applies movement every frame.")]
    public bool PreferActorInputForExactMovement = true;
    public bool UseSharedRouteCache = true;
    public bool UseLocalRouteCache = false;
    public int MaxLocalRoutes = 128;

    private readonly OUTL_NPCWorldRouteCache localRouteCache = new OUTL_NPCWorldRouteCache();
    private readonly OUTL_NPCAbstractNavigator abstractNavigator = new OUTL_NPCAbstractNavigator();
    private readonly List<OUTL_Stimulus> stimulusBuffer = new List<OUTL_Stimulus>(16);
    private readonly List<OUTL_EntityRuntime> entityQueryBuffer = new List<OUTL_EntityRuntime>(16);
    private OUTL_NPCScheduleEntry activeEntry;
    private bool registered;

    public string OUTL_SaveKey { get { return "OUTL_NPCBehaviorRuntime"; } }
    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled && Entity != null && Entity.Runtime != null && !Entity.Runtime.Dead && Entity.Runtime.LifeState != OUTL_LifeState.Dead; } }
    public OUTL_TickLane OUTL_TickLane { get { return OUTL_TickLane.AI; } }
    public float OUTL_TickInterval { get { return ResolveTickInterval(); } }
    public float OUTL_NPCRequestedTickInterval { get { return ResolveTickInterval(); } }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        localRouteCache.MaxRoutes = Mathf.Max(8, MaxLocalRoutes);
        if (AutoRegister) Register();
        if (OUTL_World.Instance != null)
        {
            OUTL_World.Instance.Events.Register(this, OUTL_EventType.Killed);
            OUTL_World.Instance.Events.Register(this, OUTL_EventType.Damaged);
        }
    }

    private void OnDisable()
    {
        Unregister();
        if (OUTL_World.Instance != null) OUTL_World.Instance.Events.Unregister(this);
    }

    public void Register()
    {
        if (registered || OUTL_World.Instance == null) return;
        OUTL_NPCBehaviorDispatcher.Register(this);
        registered = true;
    }

    public void Unregister()
    {
        if (!registered) return;
        OUTL_NPCBehaviorDispatcher.Unregister(this);
        registered = false;
    }

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        OUTL_RunBudgetedTick(world, time, deltaTime);
    }

    public void OUTL_RunBudgetedTick(OUTL_World world, float time, float deltaTime)
    {
        ResolveReferences();
        if (world == null || Entity == null || Entity.Runtime == null) return;
        if (!OUTL_NetworkAuthority.CanAdvanceNpcSchedule(Entity))
        {
            OUTL_NetworkAuthority.TraceBlocked("npc_schedule", Entity);
            return;
        }

        OUTL_EntityRuntime entityRuntime = Entity.Runtime;
        if (entityRuntime.Dead || entityRuntime.State.GetFlag(OUTL_StateId.Dead) || entityRuntime.Stats.Get(OUTL_StatId.Health, 1f) <= 0f)
        {
            ApplyDeadState();
            return;
        }

        Runtime.CurrentTier = entityRuntime.Tier;
        Runtime.LastBehaviorTick = time;
        Runtime.LastExactPosition = transform.position;
        if (Runtime.AbstractPosition == Vector3.zero) Runtime.AbstractPosition = transform.position;

        OUTL_NPCScheduleDef schedule = ResolveSchedule();
        OUTL_NPCScheduleEntry nextEntry = SelectEntry(schedule, world);
        if (!Runtime.HasActiveInterrupt) TryApplyEntry(world, nextEntry, time);
        ApplyEgregoreContext(world, time);

        ProcessStimulusInterrupts(world, time);
        if (Runtime.HasActiveInterrupt && time >= Runtime.InterruptEndTime) CompleteInterrupt(world, time);

        ExecuteCurrentBehavior(world, time, deltaTime);
        world.WorldLedger.RegisterOrUpdateNpc(entityRuntime, Runtime, Runtime.AbstractPosition != Vector3.zero ? Runtime.AbstractPosition : transform.position, time);
        PushToAIActor();
    }

    public void OUTL_OnEvent(in OUTL_Event evt, OUTL_World world)
    {
        if (Entity == null || evt.Target != Entity.Id) return;
        if (evt.Type == OUTL_EventType.Killed)
        {
            ApplyDeadState();
            return;
        }

        if (evt.Type == OUTL_EventType.Damaged)
        {
            Runtime.LastStimulus = OUTL_StimulusType.TookDamage;
            Runtime.LastStimulusKey = evt.Key;
            Runtime.LastStimulusTime = world != null ? world.WorldTime : Time.time;
        }
    }

    public void ApplyDeadState()
    {
        Runtime.CurrentAction = OUTL_NPCScheduleActionType.Combat;
        Runtime.HasActiveInterrupt = false;
        Runtime.CurrentEntryId = "dead";
        Runtime.Travel.Mode = OUTL_NPCTravelMode.None;
        if (NavMover != null) NavMover.Stop("npc_behavior_dead");
        if (AttackDriver != null) AttackDriver.BlockedByVitals = true;
        if (AIActor != null)
        {
            AIActor.CurrentIntent = "Dead";
            AIActor.CurrentGoal = "Dead";
            AIActor.CurrentState = OUTL_AIStateId.Dead;
            AIActor.NextAction = "Stop";
        }
    }

    public bool TryGetActorInputMoveIntent(float time, out OUTL_TacticalDecision decision)
    {
        decision = default(OUTL_TacticalDecision);
        if (!ShouldUseActorInputMovement()) return false;
        if (!IsMovementAction(Runtime.CurrentAction)) return false;

        Vector3 target = Runtime.CurrentTargetPosition;
        if (target == Vector3.zero) return false;

        Vector3 flat = target - transform.position;
        flat.y = 0f;
        const float stopDistance = 1.25f;
        if (flat.sqrMagnitude <= stopDistance * stopDistance) return false;

        OUTL_TacticalMoveMode moveMode = Runtime.CurrentAction == OUTL_NPCScheduleActionType.Flee && Runtime.HasActiveInterrupt
            ? OUTL_TacticalMoveMode.RetreatFromTarget
            : OUTL_TacticalMoveMode.MoveTo;

        decision = new OUTL_TacticalDecision
        {
            Intent = MapScheduleActionToIntent(Runtime.CurrentAction),
            MoveMode = moveMode,
            MoveTarget = target,
            HasMoveTarget = true,
            AimPoint = target + Vector3.up,
            HasAimPoint = Runtime.CurrentAction == OUTL_NPCScheduleActionType.Investigate || Runtime.CurrentAction == OUTL_NPCScheduleActionType.Guard,
            WeaponSlot = OUTL_EquipmentSlot.Primary,
            Reason = "npc_schedule:" + Runtime.CurrentEntryId,
            Score = 0.35f,
            DecisionTime = time
        };
        return true;
    }

    public void OUTL_Capture(OUTL_ComponentSaveWriter writer)
    {
        if (writer == null || Runtime == null) return;
        writer.SetString("schedule", Runtime.CurrentScheduleId);
        writer.SetString("entry", Runtime.CurrentEntryId);
        writer.SetInt("action", (int)Runtime.CurrentAction);
        writer.SetInt("targetSector", Runtime.CurrentTargetSector);
        writer.SetFloat("target.x", Runtime.CurrentTargetPosition.x);
        writer.SetFloat("target.y", Runtime.CurrentTargetPosition.y);
        writer.SetFloat("target.z", Runtime.CurrentTargetPosition.z);
        writer.SetString("route", Runtime.CurrentRouteKey);
        writer.SetFloat("progress", Runtime.RouteProgress);
        writer.SetFloat("eta", Runtime.EstimatedArrivalTime);
        writer.SetInt("tier", (int)Runtime.CurrentTier);
        writer.SetFloat("abstract.x", Runtime.AbstractPosition.x);
        writer.SetFloat("abstract.y", Runtime.AbstractPosition.y);
        writer.SetFloat("abstract.z", Runtime.AbstractPosition.z);
        writer.SetInt("lastStimulus", (int)Runtime.LastStimulus);
        writer.SetString("lastStimulusKey", Runtime.LastStimulusKey);
        writer.SetFloat("lastStimulusTime", Runtime.LastStimulusTime);
        writer.SetFlag("hasInterrupt", Runtime.HasActiveInterrupt);
        writer.SetInt("currentInterrupt", (int)Runtime.CurrentInterrupt);
        writer.SetFloat("interruptEnd", Runtime.InterruptEndTime);
        writer.SetString("previousEntry", Runtime.PreviousScheduleEntry);
        writer.SetInt("previousAction", (int)Runtime.PreviousAction);
        writer.SetInt("homeSector", Runtime.HomeSector);
        writer.SetString("faction", Runtime.Faction);
        writer.SetString("role", Runtime.Role);
        writer.SetInt("behaviorMode", (int)Runtime.CurrentBehaviorMode);
        writer.SetString("behaviorModeSource", Runtime.BehaviorModeSource);
        writer.SetInt("egregorePhase", (int)Runtime.LocalEgregorePhase);
        writer.SetFloat("localDanger", Runtime.LocalDanger);
        writer.SetFloat("localSafety", Runtime.LocalSafety);
        writer.SetInt("travelMode", (int)Runtime.Travel.Mode);
        writer.SetString("travelRoute", Runtime.Travel.RouteKey);
        writer.SetInt("travelStartSector", Runtime.Travel.StartSectorId);
        writer.SetInt("travelTargetSector", Runtime.Travel.TargetSectorId);
        writer.SetFloat("travelStart.x", Runtime.Travel.StartPosition.x);
        writer.SetFloat("travelStart.y", Runtime.Travel.StartPosition.y);
        writer.SetFloat("travelStart.z", Runtime.Travel.StartPosition.z);
        writer.SetFloat("travelTarget.x", Runtime.Travel.TargetPosition.x);
        writer.SetFloat("travelTarget.y", Runtime.Travel.TargetPosition.y);
        writer.SetFloat("travelTarget.z", Runtime.Travel.TargetPosition.z);
        writer.SetFloat("travelAbstract.x", Runtime.Travel.AbstractPosition.x);
        writer.SetFloat("travelAbstract.y", Runtime.Travel.AbstractPosition.y);
        writer.SetFloat("travelAbstract.z", Runtime.Travel.AbstractPosition.z);
        writer.SetFloat("travelProgress", Runtime.Travel.RouteProgress);
        writer.SetFloat("travelEta", Runtime.Travel.EstimatedArrivalTime);
        writer.SetFloat("travelLastUpdate", Runtime.Travel.LastRouteUpdateTime);
        writer.SetFloat("travelFailedUntil", Runtime.Travel.RouteFailedUntil);
        writer.SetInt("version", Runtime.SaveVersion);
    }

    public void OUTL_Restore(OUTL_ComponentSaveReader reader)
    {
        if (reader == null || Runtime == null) return;
        Runtime.CurrentScheduleId = reader.GetString("schedule", Runtime.CurrentScheduleId);
        Runtime.CurrentEntryId = reader.GetString("entry", Runtime.CurrentEntryId);
        Runtime.CurrentAction = (OUTL_NPCScheduleActionType)Mathf.Clamp(reader.GetInt("action", (int)Runtime.CurrentAction), 0, (int)OUTL_NPCScheduleActionType.Combat);
        Runtime.CurrentTargetSector = reader.GetInt("targetSector", Runtime.CurrentTargetSector);
        Runtime.CurrentTargetPosition = new Vector3(reader.GetFloat("target.x", Runtime.CurrentTargetPosition.x), reader.GetFloat("target.y", Runtime.CurrentTargetPosition.y), reader.GetFloat("target.z", Runtime.CurrentTargetPosition.z));
        Runtime.CurrentRouteKey = reader.GetString("route", Runtime.CurrentRouteKey);
        Runtime.RouteProgress = Mathf.Clamp01(reader.GetFloat("progress", Runtime.RouteProgress));
        Runtime.EstimatedArrivalTime = reader.GetFloat("eta", Runtime.EstimatedArrivalTime);
        Runtime.CurrentTier = (OUTL_RuntimeTier)Mathf.Clamp(reader.GetInt("tier", (int)Runtime.CurrentTier), 0, (int)OUTL_RuntimeTier.Full);
        Runtime.AbstractPosition = new Vector3(reader.GetFloat("abstract.x", Runtime.AbstractPosition.x), reader.GetFloat("abstract.y", Runtime.AbstractPosition.y), reader.GetFloat("abstract.z", Runtime.AbstractPosition.z));
        Runtime.LastStimulus = (OUTL_StimulusType)Mathf.Clamp(reader.GetInt("lastStimulus", (int)Runtime.LastStimulus), 0, 255);
        Runtime.LastStimulusKey = reader.GetString("lastStimulusKey", Runtime.LastStimulusKey);
        Runtime.LastStimulusTime = reader.GetFloat("lastStimulusTime", Runtime.LastStimulusTime);
        Runtime.HasActiveInterrupt = reader.GetFlag("hasInterrupt", Runtime.HasActiveInterrupt);
        Runtime.CurrentInterrupt = (OUTL_NPCScheduleActionType)Mathf.Clamp(reader.GetInt("currentInterrupt", (int)Runtime.CurrentInterrupt), 0, (int)OUTL_NPCScheduleActionType.Combat);
        Runtime.InterruptEndTime = reader.GetFloat("interruptEnd", Runtime.InterruptEndTime);
        Runtime.PreviousScheduleEntry = reader.GetString("previousEntry", Runtime.PreviousScheduleEntry);
        Runtime.PreviousAction = (OUTL_NPCScheduleActionType)Mathf.Clamp(reader.GetInt("previousAction", (int)Runtime.PreviousAction), 0, (int)OUTL_NPCScheduleActionType.Combat);
        Runtime.HomeSector = reader.GetInt("homeSector", Runtime.HomeSector);
        Runtime.Faction = reader.GetString("faction", Runtime.Faction);
        Runtime.Role = reader.GetString("role", Runtime.Role);
        Runtime.CurrentBehaviorMode = (OUTL_BehaviorModeId)Mathf.Clamp(reader.GetInt("behaviorMode", (int)Runtime.CurrentBehaviorMode), 0, (int)OUTL_BehaviorModeId.Lockdown);
        Runtime.BehaviorModeSource = reader.GetString("behaviorModeSource", Runtime.BehaviorModeSource);
        Runtime.LocalEgregorePhase = (OUTL_EgregoreCyclePhase)Mathf.Clamp(reader.GetInt("egregorePhase", (int)Runtime.LocalEgregorePhase), 0, (int)OUTL_EgregoreCyclePhase.Collapse);
        Runtime.LocalDanger = Mathf.Clamp01(reader.GetFloat("localDanger", Runtime.LocalDanger));
        Runtime.LocalSafety = Mathf.Clamp01(reader.GetFloat("localSafety", Runtime.LocalSafety));
        Runtime.Travel.Mode = (OUTL_NPCTravelMode)Mathf.Clamp(reader.GetInt("travelMode", (int)Runtime.Travel.Mode), 0, (int)OUTL_NPCTravelMode.RouteFailed);
        Runtime.Travel.RouteKey = reader.GetString("travelRoute", Runtime.Travel.RouteKey);
        Runtime.Travel.StartSectorId = reader.GetInt("travelStartSector", Runtime.Travel.StartSectorId);
        Runtime.Travel.TargetSectorId = reader.GetInt("travelTargetSector", Runtime.Travel.TargetSectorId);
        Runtime.Travel.StartPosition = new Vector3(reader.GetFloat("travelStart.x", Runtime.Travel.StartPosition.x), reader.GetFloat("travelStart.y", Runtime.Travel.StartPosition.y), reader.GetFloat("travelStart.z", Runtime.Travel.StartPosition.z));
        Runtime.Travel.TargetPosition = new Vector3(reader.GetFloat("travelTarget.x", Runtime.Travel.TargetPosition.x), reader.GetFloat("travelTarget.y", Runtime.Travel.TargetPosition.y), reader.GetFloat("travelTarget.z", Runtime.Travel.TargetPosition.z));
        Runtime.Travel.AbstractPosition = new Vector3(reader.GetFloat("travelAbstract.x", Runtime.Travel.AbstractPosition.x), reader.GetFloat("travelAbstract.y", Runtime.Travel.AbstractPosition.y), reader.GetFloat("travelAbstract.z", Runtime.Travel.AbstractPosition.z));
        Runtime.Travel.RouteProgress = Mathf.Clamp01(reader.GetFloat("travelProgress", Runtime.Travel.RouteProgress));
        Runtime.Travel.EstimatedArrivalTime = reader.GetFloat("travelEta", Runtime.Travel.EstimatedArrivalTime);
        Runtime.Travel.LastRouteUpdateTime = reader.GetFloat("travelLastUpdate", Runtime.Travel.LastRouteUpdateTime);
        Runtime.Travel.RouteFailedUntil = reader.GetFloat("travelFailedUntil", Runtime.Travel.RouteFailedUntil);
        Runtime.SaveVersion = reader.GetInt("version", Runtime.SaveVersion);
        if (Entity != null && Entity.Runtime != null) Entity.Runtime.Tier = Runtime.CurrentTier;
    }

    public void OUTL_OnPoolSpawn()
    {
        Runtime.ClearTransient();
        Register();
    }

    public void OUTL_OnPoolRelease()
    {
        Unregister();
        Runtime.ClearTransient();
    }

    private OUTL_NPCScheduleDef ResolveSchedule()
    {
        if (ScheduleOverride != null) return ScheduleOverride;
        return Model != null ? Model.Schedule : null;
    }

    private OUTL_NPCNavigationProfile ResolveNavigationProfile()
    {
        if (NavigationProfileOverride != null) return NavigationProfileOverride;
        return Model != null ? Model.NavigationProfile : null;
    }

    private OUTL_NPCScheduleEntry SelectEntry(OUTL_NPCScheduleDef schedule, OUTL_World world)
    {
        if (schedule == null) return null;
        float dayLength = Model != null ? Mathf.Max(1f, Model.DayLengthSeconds) : 1440f;
        float normalized = world != null ? Mathf.Repeat(world.WorldTime / dayLength, 1f) : 0f;
        return schedule.FindEntry(normalized);
    }

    private void TryApplyEntry(OUTL_World world, OUTL_NPCScheduleEntry entry, float time)
    {
        if (entry == null) return;
        OUTL_NPCScheduleDef schedule = ResolveSchedule();
        string scheduleId = schedule != null ? schedule.ScheduleId : "";
        if (activeEntry == entry && Runtime.CurrentScheduleId == scheduleId && Runtime.CurrentEntryId == entry.EntryId) return;
        if (!OUTL_Rules.CheckAll(entry.Conditions, Entity != null ? Entity.Id : OUTL_EntityId.None, Entity != null ? Entity.Id : OUTL_EntityId.None, world)) return;

        activeEntry = entry;
        Runtime.CurrentScheduleId = scheduleId;
        Runtime.CurrentEntryId = entry.EntryId;
        Runtime.CurrentAction = entry.Action;
        Runtime.CurrentTargetPosition = ResolveTargetPosition(world, entry);
        Runtime.CurrentTargetSector = entry.TargetSectorId;
        Runtime.CurrentRouteKey = entry.RouteKey;
        Runtime.EstimatedArrivalTime = 0f;
        Runtime.RouteProgress = 0f;
        Runtime.Travel.Reset();
        if (world != null) world.Effects.ApplyAll(entry.OnStartEffects, Entity.Id, Entity.Id, transform.position);
    }

    private Vector3 ResolveTargetPosition(OUTL_World world, OUTL_NPCScheduleEntry entry)
    {
        if (entry == null) return transform.position;
        switch (entry.TargetMode)
        {
            case OUTL_NPCScheduleTargetMode.FixedWorldPosition:
                return entry.TargetPosition;
            case OUTL_NPCScheduleTargetMode.TargetName:
                if (world != null)
                {
                    OUTL_EntityRuntime target = world.Registry.FindFirstByTargetName(entry.TargetName);
                    if (target != null && target.Adapter != null) return target.Adapter.transform.position;
                }
                break;
            case OUTL_NPCScheduleTargetMode.EntityClass:
                if (world != null)
                {
                    OUTL_EntityRuntime target = FindNearestClass(world, entry.EntityClass);
                    if (target != null && target.Adapter != null) return target.Adapter.transform.position;
                }
                break;
            case OUTL_NPCScheduleTargetMode.TagQuery:
                if (world != null)
                {
                    OUTL_EntityRuntime target = world.Sectors.FindNearestWithTags(transform.position, entry.RequiredTags, 64f, Entity != null ? Entity.Runtime : null);
                    if (target != null && target.Adapter != null) return target.Adapter.transform.position;
                }
                break;
        }
        return entry.TargetPosition != Vector3.zero ? entry.TargetPosition : transform.position;
    }

    private OUTL_EntityRuntime FindNearestClass(OUTL_World world, string className)
    {
        if (world == null || string.IsNullOrEmpty(className)) return null;
        world.Registry.CopyByClassName(className, entityQueryBuffer);
        OUTL_EntityRuntime best = null;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < entityQueryBuffer.Count; i++)
        {
            OUTL_EntityRuntime e = entityQueryBuffer[i];
            if (e == null || e.Adapter == null || e == (Entity != null ? Entity.Runtime : null)) continue;
            float sqr = (e.Adapter.transform.position - transform.position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = e;
            }
        }
        entityQueryBuffer.Clear();
        return best;
    }

    private void ProcessStimulusInterrupts(OUTL_World world, float time)
    {
        if (world == null || Model == null || Model.InterruptPolicies == null || Model.InterruptPolicies.Length == 0) return;
        if (!OUTL_NPCBehaviorDispatcher.TryConsumeStimulusInterrupt()) return;
        int budget = Mathf.Max(1, Model.StimulusBudget);
        OUTL_StimulusQuery query = new OUTL_StimulusQuery
        {
            Position = Runtime.AbstractPosition != Vector3.zero ? Runtime.AbstractPosition : transform.position,
            Radius = Mathf.Max(0.1f, Model.StimulusRadius),
            MinPriority = Model.StimulusMinimumPriority,
            MaxCount = budget,
            IgnoreSource = Entity != null ? Entity.Id : OUTL_EntityId.None
        };

        int count = OUTL_StimulusBus.Query(query, stimulusBuffer);
        for (int i = 0; i < count; i++)
        {
            OUTL_Stimulus stimulus = stimulusBuffer[i];
            for (int p = 0; p < Model.InterruptPolicies.Length; p++)
            {
                OUTL_NPCStimulusInterruptPolicy policy = Model.InterruptPolicies[p];
                if (policy == null || !policy.Matches(stimulus, Runtime.CurrentAction, time)) continue;
                StartInterrupt(stimulus, policy, time);
                policy.MarkUsed(time);
                return;
            }
        }
    }

    private void ApplyEgregoreContext(OUTL_World world, float time)
    {
        if (world == null || Runtime == null) return;
        Vector3 position = Runtime.AbstractPosition != Vector3.zero ? Runtime.AbstractPosition : transform.position;
        OUTL_WorldAddress address = OUTL_WorldAddress.FromWorldPosition(position, world.WorldLedger.ActivityCellSize);
        OUTL_WorldCellSummary summary;
        if (!world.WorldLedger.GetCellSummary(address.ActivityCell, out summary)) return;

        OUTL_EgregoreCyclePhase phase = summary.EgregoreCyclePhase;
        OUTL_BehaviorModeId mode = OUTL_EgregoreUtility.BehaviorModeForPhase(phase);
        Runtime.LocalEgregorePhase = phase;
        Runtime.LocalDanger = Mathf.Clamp01(Mathf.Max(summary.Danger, summary.EgregoreFear));
        Runtime.LocalSafety = Mathf.Clamp01(summary.Safety);

        if (mode != Runtime.CurrentBehaviorMode)
        {
            Runtime.CurrentBehaviorMode = mode;
            Runtime.BehaviorModeSource = summary.EgregoreMood + ":" + phase;
            if (Entity != null && Entity.Id.IsValid)
                world.Events.Emit(new OUTL_Event(OUTL_EventType.BehaviorModeChanged, Entity.Id, Entity.Id) { Key = Runtime.BehaviorModeSource, IntValue = (int)mode, FloatValue = summary.BehaviorPressure, Point = position });
        }

        if (Runtime.HasActiveInterrupt) return;
        OUTL_NPCScheduleActionType pressureAction = ResolveActionForBehaviorMode(mode);
        if (pressureAction == OUTL_NPCScheduleActionType.Idle) return;
        if (Runtime.CurrentAction == OUTL_NPCScheduleActionType.Combat && pressureAction != OUTL_NPCScheduleActionType.Flee) return;
        if (!ShouldOverrideActionForEgregore(mode, summary.BehaviorPressure)) return;

        Runtime.PreviousScheduleEntry = Runtime.CurrentEntryId;
        Runtime.PreviousAction = Runtime.CurrentAction;
        Runtime.CurrentAction = pressureAction;
        Runtime.CurrentEntryId = "egregore:" + phase;
        Runtime.CurrentTargetPosition = position;
        Runtime.LastStimulus = OUTL_StimulusType.Egregore;
        Runtime.LastStimulusKey = Runtime.BehaviorModeSource;
        Runtime.LastStimulusTime = time;
        Runtime.Travel.Reset();
    }

    private static OUTL_NPCScheduleActionType ResolveActionForBehaviorMode(OUTL_BehaviorModeId mode)
    {
        switch (mode)
        {
            case OUTL_BehaviorModeId.Work:
            case OUTL_BehaviorModeId.Ritual:
                return OUTL_NPCScheduleActionType.Work;
            case OUTL_BehaviorModeId.Trade:
                return OUTL_NPCScheduleActionType.Trade;
            case OUTL_BehaviorModeId.Patrol:
            case OUTL_BehaviorModeId.Hunt:
                return OUTL_NPCScheduleActionType.Patrol;
            case OUTL_BehaviorModeId.Guard:
            case OUTL_BehaviorModeId.Lockdown:
                return OUTL_NPCScheduleActionType.Guard;
            case OUTL_BehaviorModeId.Raid:
                return OUTL_NPCScheduleActionType.Combat;
            case OUTL_BehaviorModeId.Hide:
            case OUTL_BehaviorModeId.Flee:
                return OUTL_NPCScheduleActionType.Flee;
            case OUTL_BehaviorModeId.Alert:
                return OUTL_NPCScheduleActionType.Investigate;
            default:
                return OUTL_NPCScheduleActionType.Idle;
        }
    }

    private static bool ShouldOverrideActionForEgregore(OUTL_BehaviorModeId mode, float pressure)
    {
        if (mode == OUTL_BehaviorModeId.Flee || mode == OUTL_BehaviorModeId.Lockdown || mode == OUTL_BehaviorModeId.Raid) return pressure >= 0.35f;
        if (mode == OUTL_BehaviorModeId.Alert || mode == OUTL_BehaviorModeId.Guard) return pressure >= 0.45f;
        return pressure >= 0.65f;
    }

    private void StartInterrupt(OUTL_Stimulus stimulus, OUTL_NPCStimulusInterruptPolicy policy, float time)
    {
        if (!Runtime.HasActiveInterrupt)
        {
            Runtime.PreviousScheduleEntry = Runtime.CurrentEntryId;
            Runtime.PreviousAction = Runtime.CurrentAction;
        }

        Runtime.HasActiveInterrupt = true;
        Runtime.CurrentInterrupt = policy.InterruptAction;
        Runtime.CurrentAction = policy.InterruptAction;
        Runtime.CurrentEntryId = "interrupt:" + stimulus.Type;
        Runtime.CurrentTargetPosition = stimulus.Position;
        Runtime.LastStimulus = stimulus.Type;
        Runtime.LastStimulusKey = stimulus.Key;
        Runtime.LastStimulusTime = time;
        Runtime.InterruptEndTime = time + Mathf.Max(0.1f, policy.MaxDuration);
        Runtime.Travel.Reset();
        if (AIActor != null) AIActor.ReceiveStimulus(stimulus, Mathf.Max(policy.MinimumPriority, stimulus.Priority));
    }

    private void CompleteInterrupt(OUTL_World world, float time)
    {
        Runtime.HasActiveInterrupt = false;
        Runtime.CurrentInterrupt = OUTL_NPCScheduleActionType.Idle;
        if (world != null && activeEntry != null) world.Effects.ApplyAll(activeEntry.OnCompleteEffects, Entity.Id, Entity.Id, transform.position);
        activeEntry = null;
    }

    private void ExecuteCurrentBehavior(OUTL_World world, float time, float deltaTime)
    {
        switch (Runtime.CurrentAction)
        {
            case OUTL_NPCScheduleActionType.TravelTo:
            case OUTL_NPCScheduleActionType.ReturnHome:
            case OUTL_NPCScheduleActionType.Patrol:
            case OUTL_NPCScheduleActionType.Wander:
            case OUTL_NPCScheduleActionType.Investigate:
            case OUTL_NPCScheduleActionType.Flee:
                ExecuteTravel(world, time, deltaTime);
                break;
            case OUTL_NPCScheduleActionType.Combat:
                if (AIActor != null) AIActor.enabled = true;
                break;
            default:
                if (!ShouldUseActorInputMovement() && NavMover != null) NavMover.Stop("npc_behavior");
                break;
        }
    }

    private void ExecuteTravel(OUTL_World world, float time, float deltaTime)
    {
        OUTL_NPCNavigationProfile nav = ResolveNavigationProfile();
        if (nav != null) nav.Sanitize();
        OUTL_RuntimeTier tier = Entity != null && Entity.Runtime != null ? Entity.Runtime.Tier : Runtime.CurrentTier;
        bool exact = tier == OUTL_RuntimeTier.Full || tier == OUTL_RuntimeTier.Near;
        Vector3 target = Runtime.CurrentTargetPosition != Vector3.zero ? Runtime.CurrentTargetPosition : transform.position;

        if (exact)
        {
            if (ShouldUseActorInputMovement())
            {
                bool inputMaterialized = (nav == null || nav.MaterializeTransformOnNear) && abstractNavigator.Materialize(transform, Runtime);
                if (inputMaterialized && NavMover != null) NavMover.SyncAgentToTransform();
                Runtime.Travel.Mode = OUTL_NPCTravelMode.Exact;
                Runtime.Travel.TargetPosition = target;
                Runtime.Travel.AbstractPosition = transform.position;
                Runtime.AbstractPosition = transform.position;
                return;
            }

            if (!OUTL_NPCBehaviorDispatcher.TryConsumeRouteUpdate()) return;
            bool materialized = (nav == null || nav.MaterializeTransformOnNear) && abstractNavigator.Materialize(transform, Runtime);
            if (materialized && NavMover != null) NavMover.SyncAgentToTransform();
            if (NavMover != null)
            {
                if (!OUTL_NPCBehaviorDispatcher.TryConsumePathRequest()) return;
                NavMover.SetDestination(target, "npc_behavior");
                NavMover.StopDistance = 1.25f;
            }
            else
            {
                MoveTransformFallback(target, deltaTime, nav);
            }
            Runtime.AbstractPosition = transform.position;
            return;
        }

        if (!OUTL_NPCBehaviorDispatcher.TryConsumeRouteUpdate()) return;
        bool needsRouteRequest = string.IsNullOrEmpty(Runtime.Travel.RouteKey) || Runtime.Travel.TargetPosition != target || Runtime.Travel.Mode != OUTL_NPCTravelMode.Abstract;
        if (needsRouteRequest && !OUTL_NPCBehaviorDispatcher.TryConsumePathRequest()) return;

        if (!abstractNavigator.BeginOrContinue(world, Runtime, ResolveRouteCache(), nav, Runtime.AbstractPosition != Vector3.zero ? Runtime.AbstractPosition : transform.position, target, Runtime.CurrentRouteKey, time))
            return;

        bool arrived = abstractNavigator.Advance(Runtime, nav, deltaTime);
        if (nav == null || nav.UpdateTransformWhileAbstract || tier == OUTL_RuntimeTier.Mid)
            transform.position = Runtime.AbstractPosition;
        if (arrived && activeEntry != null && world != null)
        {
            world.Effects.ApplyAll(activeEntry.OnCompleteEffects, Entity.Id, Entity.Id, transform.position);
            Runtime.Travel.Mode = OUTL_NPCTravelMode.None;
        }
    }

    private void MoveTransformFallback(Vector3 target, float deltaTime, OUTL_NPCNavigationProfile nav)
    {
        Vector3 delta = target - transform.position;
        delta.y = 0f;
        float stop = 1.25f;
        if (delta.sqrMagnitude <= stop * stop) return;
        float speed = nav != null ? nav.WalkSpeed : 2.2f;
        transform.position += delta.normalized * speed * Mathf.Max(0f, deltaTime);
    }

    private void PushToAIActor()
    {
        if (AIActor == null) return;
        AIActor.CurrentIntent = Runtime.CurrentAction.ToString();
        AIActor.CurrentScheduleId = Runtime.CurrentScheduleId;
        AIActor.CurrentTaskName = Runtime.CurrentEntryId;
        AIActor.LastKnownTargetPosition = Runtime.CurrentTargetPosition;
        AIActor.CurrentDanger = Mathf.Clamp01(Mathf.Max(AIActor.CurrentDanger, Runtime.LocalDanger));
        AIActor.CurrentMorale = Mathf.Clamp01(Mathf.Min(AIActor.CurrentMorale, Mathf.Max(0f, Runtime.LocalSafety)));
        AIActor.LastEvent = Runtime.HasActiveInterrupt ? "ScheduleInterrupted:" + Runtime.LastStimulus : "Schedule:" + Runtime.CurrentEntryId + " mode=" + Runtime.CurrentBehaviorMode + " phase=" + Runtime.LocalEgregorePhase;
    }

    private float ResolveTickInterval()
    {
        OUTL_RuntimeTier tier = Entity != null && Entity.Runtime != null ? Entity.Runtime.Tier : Runtime.CurrentTier;
        OUTL_TickProfile profile = OUTL_World.Instance != null ? OUTL_World.Instance.TickProfile : null;
        switch (tier)
        {
            case OUTL_RuntimeTier.Full: return profile != null ? Mathf.Max(0.01f, profile.npcFullInterval) : 0.08f;
            case OUTL_RuntimeTier.Near: return profile != null ? Mathf.Max(0.01f, profile.npcNearInterval) : 0.25f;
            case OUTL_RuntimeTier.Mid: return profile != null ? Mathf.Max(0.01f, profile.npcMidInterval) : 2f;
            case OUTL_RuntimeTier.Far: return profile != null ? Mathf.Max(0.01f, profile.npcFarInterval) : 10f;
            case OUTL_RuntimeTier.Dormant:
            default: return profile != null ? Mathf.Max(0.01f, profile.npcDormantInterval) : 60f;
        }
    }

    private void ResolveReferences()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (AIActor == null) AIActor = GetComponent<OUTL_AIActor>();
        if (NavMover == null) NavMover = GetComponent<OUTL_NavMeshMover>();
        if (AttackDriver == null) AttackDriver = GetComponent<OUTL_AttackDriver>();
        if (BotInputDriver == null) BotInputDriver = GetComponent<OUTL_BotInputDriver>();
        if (ActorControlBridge == null) ActorControlBridge = GetComponent<OUTL_ActorControlBridge>();
        if (Runtime == null) Runtime = new OUTL_NPCBehaviorRuntime();
    }

    private OUTL_NPCWorldRouteCache ResolveRouteCache()
    {
        if (UseSharedRouteCache) return OUTL_NPCBehaviorDispatcher.SharedRouteCache;
        return localRouteCache;
    }

    private bool ShouldUseActorInputMovement()
    {
        return PreferActorInputForExactMovement
            && AIActor != null
            && AIActor.UseActorInputContract
            && BotInputDriver != null
            && ActorControlBridge != null;
    }

    private static bool IsMovementAction(OUTL_NPCScheduleActionType action)
    {
        return action == OUTL_NPCScheduleActionType.TravelTo
            || action == OUTL_NPCScheduleActionType.ReturnHome
            || action == OUTL_NPCScheduleActionType.Patrol
            || action == OUTL_NPCScheduleActionType.Wander
            || action == OUTL_NPCScheduleActionType.Investigate
            || action == OUTL_NPCScheduleActionType.Flee;
    }

    private static OUTL_TacticalIntentId MapScheduleActionToIntent(OUTL_NPCScheduleActionType action)
    {
        switch (action)
        {
            case OUTL_NPCScheduleActionType.Patrol:
            case OUTL_NPCScheduleActionType.Wander:
                return OUTL_TacticalIntentId.Patrol;
            case OUTL_NPCScheduleActionType.Investigate:
                return OUTL_TacticalIntentId.Investigate;
            case OUTL_NPCScheduleActionType.Flee:
                return OUTL_TacticalIntentId.Flee;
            case OUTL_NPCScheduleActionType.ReturnHome:
            case OUTL_NPCScheduleActionType.TravelTo:
            default:
                return OUTL_TacticalIntentId.Travel;
        }
    }
}
