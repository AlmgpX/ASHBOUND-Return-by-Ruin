using System.Collections.Generic;
using UnityEngine;

public class OUTL_AIActor : MonoBehaviour, OUTL_ITickable, OUTL_IEventListener
{
    public OUTL_AIProfile Profile;
    public OUTL_AIPerceptionProfile PerceptionProfile;
    public OUTL_AIStateTable StateTable;
    public OUTL_EntityAdapter Entity;
    public Transform MoveRoot;
    public OUTL_NavMeshMover NavMover;
    public OUTL_AttackDriver AttackDriver;
    public OUTL_CharacterAnimationBridge AnimationBridge;
    public OUTL_PatrolRoute PatrolRoute;
    public OUTL_EntityDiary Diary;
    public OUTL_AIInterceptPlanner InterceptPlanner;
    public OUTL_TacticalPlanner TacticalPlanner;
    public OUTL_EntityId CurrentTarget;
    public bool MoveToTarget = true;
    public bool UseNavMeshMover = true;
    public bool UseAttackDriver = true;
    public bool Stationary;
    [Tooltip("When true, this AIActor only exposes state/targets/schedules; movement and weapons are driven by OUTL_BotInputDriver -> OUTL_ActorControlBridge.")]
    public bool UseActorInputContract;

    [Header("Perception")]
    public bool RequireLineOfSightToAcquireTarget = true;
    public bool RequireLineOfSightToKeepTarget = true;
    public LayerMask SightBlockMask = ~0;
    public float EyeHeight = 1.45f;
    public float TargetEyeHeight = 1.15f;
    public float LostTargetGraceTime = 1.25f;
    public bool ClearTargetWhenLost = true;
    public bool LastTargetVisible;
    public bool UseStimulusInterrupts = true;
    public bool CreatureUsesFoodStimulus;
    public bool FleeFromDanger = true;
    public bool PreferRangedCombat;
    public bool FleeWhenTargetTooClose;
    public float PreferredRange = 0f;
    public float MinSafeRange = 1.5f;
    public float SwitchCooldown = 0.5f;

    [Header("Tactical Lite")]
    public float StimulusPriorityThreshold = 0.1f;
    public float StimulusForgetAfter = 8f;
    public float InvestigateReachDistance = 1.35f;
    public bool ClearStimulusWhenInvestigated = true;
    public bool ReturnToPatrolAfterInterestLost = true;
    public float CoverSearchRadius = 18f;
    public LayerMask CoverVisibilityMask = ~0;
    public OUTL_CoverPoint CurrentCover;
    public Vector3 CurrentOrderPosition;
    public OUTL_SquadOrder CurrentOrder;

    [Header("Memory")]
    public float TargetRefreshInterval = 0.5f;
    public float ForgetTargetAfter = 3f;
    public Vector3 LastKnownTargetPosition;
    public Vector3 LastStimulusPosition;
    public float LastStimulusPriority;
    public float LastStimulusTime;
    public float LastTargetVisibleTime;
    public float Suspicion;
    public float MemoryFear;
    public float MemoryAggression;
    public float AllegianceInfluence = 1f;
    public float FactionInfluence = 1f;
    public string CurrentIntent = "Idle";
    public string CurrentScheduleId = "";
    public int CurrentTaskIndex;
    public string CurrentTaskName = "";

    [Header("Visible State Table")]
    public bool ExposeDebugState = true;
    public OUTL_AIStateId CurrentState = OUTL_AIStateId.Idle;
    public string CurrentGoal = "Idle";
    public OUTL_Stimulus LastStimulus;
    public OUTL_StimulusType LastStimulusType = OUTL_StimulusType.None;
    public string LastStimulusKey = "";
    public string CurrentWeapon = "";
    public OUTL_AttackProfile CurrentAttackProfile;
    public string CurrentAnimationHint = "";
    public Color CurrentDebugColor = Color.white;
    public float CurrentTargetDistance;
    public bool CurrentTargetVisible;
    public float CurrentDanger;
    public float CurrentFood;
    public float CurrentFear;
    public float CurrentAggression;
    public float CurrentMorale = 1f;
    public string NextAction = "";
    public string LastEvent = "";

    private float nextTargetRefreshTime;
    private float nextAmbientSenseTime;
    private float lastWeaponSwitchTime = -999f;
    private float lastTargetSeenTime;
    private float taskEndTime;
    private OUTL_AIScheduleLite activeSchedule;
    private string lastTraceState = "";
    private int patrolIndex;
    private int patrolDirection = 1;
    private bool reportedLostTarget;
    private readonly List<OUTL_Stimulus> ambientStimulusBuffer = new List<OUTL_Stimulus>(16);

    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled && Profile != null && Entity != null && Entity.Runtime != null; } }
    public OUTL_TickLane OUTL_TickLane { get { return OUTL_TickLane.AI; } }
    public float OUTL_TickInterval
    {
        get
        {
            OUTL_RuntimeTier tier = Entity != null && Entity.Runtime != null ? Entity.Runtime.Tier : OUTL_RuntimeTier.Full;
            if (tier == OUTL_RuntimeTier.Far || tier == OUTL_RuntimeTier.Dormant) return Profile != null ? Profile.ThinkIntervalFar : 2f;
            if (tier == OUTL_RuntimeTier.Mid) return Profile != null ? Profile.ThinkIntervalMid : 0.5f;
            return Profile != null ? Profile.ThinkIntervalNear : 0.1f;
        }
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        nextTargetRefreshTime = 0f;
        nextAmbientSenseTime = 0f;
        taskEndTime = 0f;
        CurrentTarget = OUTL_EntityId.None;
        CurrentOrder = default(OUTL_SquadOrder);
        CurrentCover = null;
        activeSchedule = null;
        CurrentTaskIndex = 0;
        CurrentTaskName = "";
        LastTargetVisible = false;
        CurrentTargetVisible = false;
        LastTargetVisibleTime = -999f;
        LastStimulusTime = -999f;
        Suspicion = 0f;
        MemoryFear = 0f;
        MemoryAggression = 0f;
        AllegianceInfluence = 1f;
        FactionInfluence = 1f;
        LastStimulusType = OUTL_StimulusType.None;
        LastStimulusKey = "";
        CurrentState = OUTL_AIStateId.Idle;
        CurrentGoal = "Idle";
        CurrentWeapon = "";
        CurrentAttackProfile = null;
        CurrentAnimationHint = "";
        CurrentDebugColor = OUTL_AIStateTable.DefaultColor(CurrentState);
        LastEvent = "Enabled";
        reportedLostTarget = false;
        lastTraceState = "";
        if (OUTL_World.Instance != null)
        {
            OUTL_World.Instance.Scheduler.Register(this);
            OUTL_World.Instance.Events.Register(this, OUTL_EventType.Damaged);
            OUTL_World.Instance.Events.Register(this, OUTL_EventType.Killed);
        }
    }

    private void OnDisable()
    {
        if (Entity != null && Entity.Runtime != null && Entity.Runtime.State.GetFlag(OUTL_StateId.Dead))
            ApplyDeadState("");
        if (OUTL_World.Instance != null)
        {
            OUTL_World.Instance.Scheduler.Unregister(this);
            OUTL_World.Instance.Events.Unregister(this);
        }
        if (CurrentCover != null && CurrentCover.Occupant == Entity) CurrentCover.Occupant = null;
    }

    public void OUTL_OnEvent(in OUTL_Event evt, OUTL_World world)
    {
        if (Entity == null || !Entity.Id.IsValid || evt.Target != Entity.Id) return;
        if (evt.Type == OUTL_EventType.Damaged)
        {
            OUTL_Stimulus stimulus = new OUTL_Stimulus(OUTL_StimulusType.TookDamage, evt.Source, evt.Point != Vector3.zero ? evt.Point : transform.position, 0f, Mathf.Max(0.1f, evt.FloatValue), 1f, 1f, 2f, evt.Key, null);
            ReceiveStimulus(stimulus, Mathf.Max(0.5f, stimulus.Priority));
            LastEvent = "TookDamage:" + evt.Key;
        }
        else if (evt.Type == OUTL_EventType.Killed)
        {
            ApplyDeadState("Killed:" + evt.Key);
        }
    }

    public void ReceiveStimulus(OUTL_Stimulus stimulus, float priority)
    {
        float now = stimulus.Time;
        if (stimulus.Type == LastStimulusType && stimulus.Source == LastStimulus.Source && now - LastStimulusTime < 0.25f)
            return;
        if (priority < StimulusPriorityThreshold || priority < LastStimulusPriority * 0.75f) return;
        LastStimulus = stimulus;
        LastStimulusType = stimulus.Type;
        LastStimulusKey = stimulus.Key;
        LastStimulusPosition = stimulus.Position;
        LastStimulusPriority = priority;
        LastStimulusTime = now;
        LastKnownTargetPosition = stimulus.Position;
        ApplyStimulusMemory(stimulus, priority);
        if (stimulus.Source.IsValid && stimulus.Source != (Entity != null ? Entity.Id : OUTL_EntityId.None) && IsTargetStimulus(stimulus.Type))
            CurrentTarget = stimulus.Source;
        reportedLostTarget = false;
        LastEvent = "Stimulus:" + stimulus.Type + ":" + stimulus.Key;
        InterruptScheduleForStimulus(stimulus.Type);
        if (Diary != null) Diary.Write(OUTL_DiaryEventType.HeardSound, stimulus.Type + " " + stimulus.Key);
        OUTL_DebugLog.TraceAI(Entity != null ? Entity.Id : OUTL_EntityId.None, "stimulus " + stimulus.Type + " p=" + priority.ToString("0.00") + " pos=" + stimulus.Position);
    }

    private void ApplyStimulusMemory(OUTL_Stimulus stimulus, float priority)
    {
        float p = Mathf.Clamp01(priority);
        switch (stimulus.Type)
        {
            case OUTL_StimulusType.SightEnemy:
            case OUTL_StimulusType.HeardCombat:
            case OUTL_StimulusType.Combat:
            case OUTL_StimulusType.TookDamage:
            case OUTL_StimulusType.Damage:
                Suspicion = Mathf.Clamp01(Mathf.Max(Suspicion, p));
                MemoryAggression = Mathf.Clamp01(Mathf.Max(MemoryAggression, p));
                break;
            case OUTL_StimulusType.SightDanger:
            case OUTL_StimulusType.Fear:
            case OUTL_StimulusType.Fire:
            case OUTL_StimulusType.Death:
            case OUTL_StimulusType.LowHealth:
                Suspicion = Mathf.Clamp01(Mathf.Max(Suspicion, p * 0.75f));
                MemoryFear = Mathf.Clamp01(Mathf.Max(MemoryFear, p));
                break;
            case OUTL_StimulusType.HeardNoise:
            case OUTL_StimulusType.Suspicion:
            case OUTL_StimulusType.Alert:
            case OUTL_StimulusType.Smell:
            case OUTL_StimulusType.Territory:
            case OUTL_StimulusType.Egregore:
                Suspicion = Mathf.Clamp01(Mathf.Max(Suspicion, p));
                ApplyEgregoreStimulusMemory(stimulus.Key, p);
                break;
            case OUTL_StimulusType.SightFood:
            case OUTL_StimulusType.Resource:
                CurrentFood = Mathf.Clamp01(Mathf.Max(CurrentFood, p));
                break;
            case OUTL_StimulusType.SightAlly:
            case OUTL_StimulusType.Social:
                CurrentMorale = Mathf.Clamp01(Mathf.Max(CurrentMorale, 0.5f + p * 0.5f));
                break;
        }
    }

    private void ApplyEgregoreStimulusMemory(string key, float priority)
    {
        if (string.IsNullOrEmpty(key)) return;
        float p = Mathf.Clamp01(priority);
        if (key.IndexOf("ShadowConfrontation") >= 0 || key.IndexOf("Crisis") >= 0 || key.IndexOf("SacrificeOrDeath") >= 0)
        {
            MemoryFear = Mathf.Clamp01(Mathf.Max(MemoryFear, p));
            MemoryAggression = Mathf.Clamp01(Mathf.Max(MemoryAggression, p * 0.75f));
            CurrentDanger = Mathf.Clamp01(Mathf.Max(CurrentDanger, p));
            CurrentMorale = Mathf.Clamp01(Mathf.Min(CurrentMorale, 1f - p * 0.45f));
            return;
        }
        if (key.IndexOf("CorruptionLoop") >= 0 || key.IndexOf("Collapse") >= 0)
        {
            MemoryFear = Mathf.Clamp01(Mathf.Max(MemoryFear, p));
            Suspicion = Mathf.Clamp01(Mathf.Max(Suspicion, p));
            CurrentDanger = Mathf.Clamp01(Mathf.Max(CurrentDanger, p));
            CurrentMorale = Mathf.Clamp01(Mathf.Min(CurrentMorale, 1f - p * 0.65f));
            return;
        }
        if (key.IndexOf("Threshold") >= 0 || key.IndexOf("Trials") >= 0 || key.IndexOf("Descent") >= 0)
        {
            Suspicion = Mathf.Clamp01(Mathf.Max(Suspicion, p));
            MemoryAggression = Mathf.Clamp01(Mathf.Max(MemoryAggression, p * 0.35f));
            return;
        }
        if (key.IndexOf("Renewal") >= 0 || key.IndexOf("Integration") >= 0 || key.IndexOf("RevelationOrBoon") >= 0)
        {
            MemoryFear = Mathf.MoveTowards(MemoryFear, 0f, p * 0.25f);
            CurrentDanger = Mathf.MoveTowards(CurrentDanger, 0f, p * 0.25f);
            CurrentMorale = Mathf.Clamp01(Mathf.Max(CurrentMorale, 0.5f + p * 0.5f));
        }
    }

    public void ReceiveSquadOrder(OUTL_SquadOrder order)
    {
        CurrentOrder = order;
        CurrentOrderPosition = order.Position;
        if (order.Target.IsValid) CurrentTarget = order.Target;
        if (Diary != null) Diary.Write(OUTL_DiaryEventType.ReceivedOrder, order.Type + " " + order.Key);
    }

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        ResolveReferences();
        OUTL_EntityRuntime self = Entity != null ? Entity.Runtime : null;
        if (self == null || Profile == null) return;
        world.Sectors.RegisterOrUpdate(self);

        if (self.State.GetFlag(OUTL_StateId.Dead) || self.Stats.Get(OUTL_StatId.Health, 1f) <= 0f)
        {
            ApplyDeadState("");
            UpdateVisibleState(self, null, time);
            TraceState(self);
            PushAnimationState();
            return;
        }

        DecayInterest(time);
        UpdateAmbientStimuli(world, self, time);

        OUTL_EntityRuntime target = ResolveTarget(world, self, time);
        if (target != null && target.Adapter != null)
        {
            CurrentTarget = target.Id;
            LastKnownTargetPosition = target.Adapter.transform.position;
            lastTargetSeenTime = time;
            LastTargetVisibleTime = time;
            LastTargetVisible = true;
            CurrentTargetVisible = true;
            reportedLostTarget = false;
            if (UseStimulusInterrupts && (CurrentIntent == "Idle" || CurrentIntent == "Patrol" || CurrentIntent == "Work"))
                SetVisibleStimulus(OUTL_StimulusType.SightEnemy, target.Id, target.Adapter.transform.position, "enemy", 1f);
        }
        else
        {
            LastTargetVisible = false;
            CurrentTargetVisible = false;
            HandleLostTarget(world, time);
        }

        OUTL_AIScheduleLite wanted = SelectSchedule(self, target, time);
        if (wanted != activeSchedule || (wanted != null && wanted.RestartWhenSelected && CurrentScheduleId != wanted.ScheduleId)) SetSchedule(wanted, time);

        if (activeSchedule != null && activeSchedule.Tasks != null && activeSchedule.Tasks.Length > 0)
        {
            ExecuteSchedule(world, self, target, time, deltaTime);
            UpdateVisibleState(self, target, time);
            TraceState(self);
            PushAnimationState();
            return;
        }

        CurrentTaskName = "FallbackCombat";
        ExecuteFallbackCombat(world, self, target, time, deltaTime);
        UpdateVisibleState(self, target, time);
        TraceState(self);
        PushAnimationState();
    }

    private void PushAnimationState()
    {
        if (AnimationBridge != null) AnimationBridge.PushAI(this);
    }

    public string DescribeThinking()
    {
        return "state=" + CurrentState + " goal=" + CurrentGoal + " intent=" + CurrentIntent + " schedule=" + CurrentScheduleId + " task=" + CurrentTaskIndex + ":" + CurrentTaskName + " target=" + CurrentTarget + " visible=" + LastTargetVisible + " weapon=" + CurrentWeapon + " profile=" + AttackProfileName(CurrentAttackProfile) + " stim=" + LastStimulusType + ":" + LastStimulusKey + " suspicion=" + Suspicion.ToString("0.00") + " fear=" + CurrentFear.ToString("0.00") + " aggression=" + CurrentAggression.ToString("0.00") + " morale=" + CurrentMorale.ToString("0.00") + " order=" + CurrentOrder.Type + " cover=" + (CurrentCover != null ? CurrentCover.name : "none") + " lkp=" + LastKnownTargetPosition;
    }

    private void DecayInterest(float time)
    {
        if (LastStimulusPriority > 0f && time - LastStimulusTime > StimulusForgetAfter) ClearStimulusMemory("expired");
        if (CurrentOrder.IsValid == false && CurrentOrder.Type != OUTL_SquadOrderType.None) CurrentOrder = default(OUTL_SquadOrder);
        Suspicion = Mathf.MoveTowards(Suspicion, 0f, 0.025f);
        MemoryFear = Mathf.MoveTowards(MemoryFear, 0f, 0.02f);
        MemoryAggression = Mathf.MoveTowards(MemoryAggression, 0f, 0.02f);
    }

    private void HandleLostTarget(OUTL_World world, float time)
    {
        if (!CurrentTarget.IsValid) return;
        if (time - LastTargetVisibleTime <= LostTargetGraceTime) return;

        if (!reportedLostTarget)
        {
            reportedLostTarget = true;
            SetVisibleStimulus(OUTL_StimulusType.LostTarget, CurrentTarget, LastKnownTargetPosition, "lost_target", 0.75f);
            if (Diary != null) Diary.Write(OUTL_DiaryEventType.LostEnemy, "last known " + LastKnownTargetPosition);
            OUTL_DebugLog.TraceAI(Entity != null ? Entity.Id : OUTL_EntityId.None, "lost target " + CurrentTarget + " -> investigate last known");
        }

        if (ClearTargetWhenLost && time - LastTargetVisibleTime > Mathf.Max(LostTargetGraceTime, ForgetTargetAfter)) CurrentTarget = OUTL_EntityId.None;
    }

    private OUTL_AIScheduleLite SelectSchedule(OUTL_EntityRuntime self, OUTL_EntityRuntime target, float time)
    {
        float health = self.Stats.Get(OUTL_StatId.Health, 100f);
        if (health > 0f && health <= Profile.LowHealthThreshold && Profile.FleeSchedule != null)
        {
            SetVisibleStimulus(OUTL_StimulusType.LowHealth, self.Id, transform.position, "low_health", 1f);
            CurrentIntent = "Flee";
            return Profile.FleeSchedule;
        }

        if (FleeFromDanger && LastStimulusType == OUTL_StimulusType.SightDanger && time - LastStimulusTime <= ReadMemoryDuration() && Profile.FleeSchedule != null)
        {
            CurrentIntent = "Flee";
            return Profile.FleeSchedule;
        }

        if (target != null && target.Adapter != null && FleeWhenTargetTooClose && Profile.FleeSchedule != null && (AttackDriver == null || AttackDriver.Melee == null))
        {
            float minSafe = Mathf.Max(0.1f, MinSafeRange);
            if ((target.Adapter.transform.position - transform.position).sqrMagnitude <= minSafe * minSafe)
            {
                CurrentIntent = "Flee";
                return Profile.FleeSchedule;
            }
        }

        if (target != null)
        {
            CurrentIntent = "Combat";
            return Profile.CombatSchedule;
        }

        if (CreatureUsesFoodStimulus && LastStimulusType == OUTL_StimulusType.SightFood && time - LastStimulusTime <= ReadMemoryDuration() && Profile.SearchSchedule != null)
        {
            CurrentIntent = "EatOrUseResource";
            LastKnownTargetPosition = LastStimulusPosition;
            return Profile.SearchSchedule;
        }

        if (CurrentOrder.IsValid && Profile.SearchSchedule != null)
        {
            CurrentIntent = "Order";
            LastKnownTargetPosition = CurrentOrder.Position;
            return Profile.SearchSchedule;
        }

        if (IsInvestigateStimulus(LastStimulusType) && time - LastStimulusTime <= StimulusForgetAfter && LastStimulusPriority >= StimulusPriorityThreshold && Profile.SearchSchedule != null)
        {
            CurrentIntent = "Investigate";
            LastKnownTargetPosition = LastStimulusPosition;
            return Profile.SearchSchedule;
        }

        if (time - LastTargetVisibleTime <= ForgetTargetAfter && LastKnownTargetPosition != Vector3.zero && Profile.SearchSchedule != null)
        {
            CurrentIntent = "Search";
            return Profile.SearchSchedule;
        }

        CurrentIntent = ReturnToPatrolAfterInterestLost && PatrolRoute != null ? "Patrol" : "Idle";
        return Profile.IdleSchedule;
    }

    private void SetSchedule(OUTL_AIScheduleLite schedule, float time)
    {
        activeSchedule = schedule;
        CurrentScheduleId = activeSchedule != null ? activeSchedule.ScheduleId : "";
        CurrentTaskIndex = 0;
        CurrentTaskName = "";
        taskEndTime = 0f;
        SetVisibleStimulus(OUTL_StimulusType.ScheduleChanged, Entity != null ? Entity.Id : OUTL_EntityId.None, transform.position, CurrentScheduleId, 0.25f);
        LastEvent = "ScheduleChanged:" + CurrentScheduleId;
    }

    private void ExecuteSchedule(OUTL_World world, OUTL_EntityRuntime self, OUTL_EntityRuntime target, float time, float deltaTime)
    {
        OUTL_AITaskDef[] tasks = activeSchedule.Tasks;
        if (CurrentTaskIndex < 0 || CurrentTaskIndex >= tasks.Length) CurrentTaskIndex = 0;
        OUTL_AITaskDef task = tasks[CurrentTaskIndex];
        if (task == null)
        {
            CurrentTaskName = "Null";
            AdvanceTask(time);
            return;
        }

        CurrentTaskName = task.Type.ToString();
        bool complete = ExecuteTask(world, self, target, task, time, deltaTime);
        if (complete || (task.Duration > 0f && time >= taskEndTime && taskEndTime > 0f)) AdvanceTask(time);
    }

    private bool ExecuteTask(OUTL_World world, OUTL_EntityRuntime self, OUTL_EntityRuntime target, OUTL_AITaskDef task, float time, float deltaTime)
    {
        if (taskEndTime <= 0f && task.Duration > 0f) taskEndTime = time + task.Duration;

        switch (task.Type)
        {
            case OUTL_AITaskType.Wait: return task.Duration <= 0f || time >= taskEndTime;
            case OUTL_AITaskType.Stop: StopMove(); return true;
            case OUTL_AITaskType.FindTarget:
                OUTL_EntityRuntime found = FindTarget(world, self);
                CurrentTarget = found != null ? found.Id : OUTL_EntityId.None;
                return true;
            case OUTL_AITaskType.MoveToTarget:
                if (target == null || target.Adapter == null) return true;
                return MoveTowards(ResolveChasePoint(target, target.Adapter.transform.position, time), task.Distance, task.SpeedMultiplier, deltaTime);
            case OUTL_AITaskType.AttackTarget:
                if (target == null || target.Adapter == null) return true;
                Attack(world, self, target);
                return !UseActorInputContract;
            case OUTL_AITaskType.MoveToPoint: return MoveTowards(LastKnownTargetPosition, task.Distance, task.SpeedMultiplier, deltaTime);
            case OUTL_AITaskType.FleeFromTarget:
                if (target == null || target.Adapter == null) return true;
                Vector3 away = transform.position - target.Adapter.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude <= 0.001f) away = -transform.forward;
                return MoveTowards(transform.position + away.normalized * Mathf.Max(2f, task.Distance), task.Distance, task.SpeedMultiplier, deltaTime);
            case OUTL_AITaskType.FaceTarget:
                if (target == null || target.Adapter == null) return true;
                Face(target.Adapter.transform.position, deltaTime);
                return true;
            case OUTL_AITaskType.SendCommandToTarget:
                if (target == null) return true;
                world.Commands.Send(new OUTL_Command(task.Command, self.Id, target.Id));
                return true;
            case OUTL_AITaskType.ApplyEffects:
                world.Effects.ApplyAll(task.Effects, self.Id, target != null ? target.Id : self.Id, transform.position);
                return true;
            case OUTL_AITaskType.SetStateFlag:
                self.State.SetFlag(task.StateKey, task.StateValue);
                return true;
            case OUTL_AITaskType.Patrol: return Patrol(deltaTime);
            case OUTL_AITaskType.InvestigateStimulus: return Investigate(task, deltaTime);
            case OUTL_AITaskType.FindCover: return FindCover(task.Distance > 0f ? task.Distance : CoverSearchRadius, task.Mask);
            case OUTL_AITaskType.MoveToCover: return CurrentCover == null || MoveTowards(CurrentCover.StandPoint, task.Distance, task.SpeedMultiplier, deltaTime);
            case OUTL_AITaskType.FollowSquadOrder: return FollowSquadOrder(task, deltaTime);
        }

        return true;
    }

    private bool Patrol(float deltaTime)
    {
        if (PatrolRoute == null || PatrolRoute.Count == 0) return true;
        Transform point = PatrolRoute.GetPoint(patrolIndex);
        if (point == null) return true;
        bool reached = MoveTowards(point.position, PatrolRoute.PointReachDistance, 1f, deltaTime);
        if (reached)
        {
            if (Diary != null) Diary.Write(OUTL_DiaryEventType.Patrol, point.name);
            patrolIndex = PatrolRoute.NextIndex(patrolIndex, ref patrolDirection);
            return true;
        }
        return false;
    }

    private bool Investigate(OUTL_AITaskDef task, float deltaTime)
    {
        float stop = task.Distance > 0f ? task.Distance : InvestigateReachDistance;
        bool reached = MoveTowards(LastKnownTargetPosition, stop, task.SpeedMultiplier, deltaTime);
        if (reached && ClearStimulusWhenInvestigated)
        {
            ClearStimulusMemory("investigated");
            if (!CurrentTarget.IsValid) LastKnownTargetPosition = Vector3.zero;
            if (Diary != null) Diary.Write(OUTL_DiaryEventType.Idle, "interest cleared");
        }
        return reached;
    }

    private void ClearStimulusMemory(string reason)
    {
        if (LastStimulusPriority <= 0f) return;
        OUTL_DebugLog.TraceAI(Entity != null ? Entity.Id : OUTL_EntityId.None, "clear stimulus " + reason);
        LastStimulusPriority = 0f;
        LastStimulusTime = -999f;
        LastStimulusPosition = Vector3.zero;
        LastStimulusType = OUTL_StimulusType.None;
        LastStimulusKey = "";
    }

    private void UpdateAmbientStimuli(OUTL_World world, OUTL_EntityRuntime self, float time)
    {
        CurrentDanger = 0f;
        CurrentFood = 0f;
        if (world == null || self == null) return;
        if (time < nextAmbientSenseTime) return;
        nextAmbientSenseTime = time + 0.35f;

        ProcessAmbientStimulusStore(self);
        if (PerceptionProfile == null) return;

        if (PerceptionProfile.DangerTags != null && PerceptionProfile.DangerTags.Length > 0)
        {
            OUTL_EntityRuntime danger = world.Sectors.FindNearestWithTags(transform.position, PerceptionProfile.DangerTags, PerceptionProfile.DangerRadius, self);
            if (danger != null && danger.Adapter != null)
            {
                float distance = Vector3.Distance(transform.position, danger.Adapter.transform.position);
                CurrentDanger = Mathf.Clamp01(1f - distance / Mathf.Max(0.1f, PerceptionProfile.DangerRadius));
                if (FleeFromDanger && CurrentDanger >= StimulusPriorityThreshold)
                    ReceiveStimulus(new OUTL_Stimulus(OUTL_StimulusType.SightDanger, danger.Id, danger.Adapter.transform.position, PerceptionProfile.DangerRadius, CurrentDanger, 1f, CurrentDanger, ReadMemoryDuration(), "danger", danger.Tags), CurrentDanger);
            }
        }

        if (CreatureUsesFoodStimulus && PerceptionProfile.FoodTags != null && PerceptionProfile.FoodTags.Length > 0)
        {
            OUTL_EntityRuntime food = world.Sectors.FindNearestWithTags(transform.position, PerceptionProfile.FoodTags, PerceptionProfile.FoodRadius, self);
            if (food != null && food.Adapter != null)
            {
                float distance = Vector3.Distance(transform.position, food.Adapter.transform.position);
                CurrentFood = Mathf.Clamp01(1f - distance / Mathf.Max(0.1f, PerceptionProfile.FoodRadius));
                if (CurrentFood >= StimulusPriorityThreshold)
                    ReceiveStimulus(new OUTL_Stimulus(OUTL_StimulusType.SightFood, food.Id, food.Adapter.transform.position, PerceptionProfile.FoodRadius, CurrentFood, 1f, CurrentFood, ReadMemoryDuration(), "food", food.Tags), CurrentFood);
            }
        }
    }

    private void ProcessAmbientStimulusStore(OUTL_EntityRuntime self)
    {
        float radius = 16f;
        if (PerceptionProfile != null)
        {
            radius = Mathf.Max(radius, PerceptionProfile.HearingRadius);
            radius = Mathf.Max(radius, PerceptionProfile.DangerRadius);
            radius = Mathf.Max(radius, PerceptionProfile.FoodRadius);
            radius = Mathf.Max(radius, PerceptionProfile.SightDistance);
        }

        OUTL_StimulusQuery query = new OUTL_StimulusQuery
        {
            Position = transform.position,
            Radius = Mathf.Max(0.1f, radius),
            Type = OUTL_StimulusType.None,
            MinPriority = StimulusPriorityThreshold,
            MaxCount = 12,
            IgnoreSource = self != null ? self.Id : OUTL_EntityId.None
        };

        int count = OUTL_StimulusBus.Query(query, ambientStimulusBuffer);
        for (int i = 0; i < count; i++)
        {
            OUTL_Stimulus stimulus = ambientStimulusBuffer[i];
            if (self != null && stimulus.Source == self.Id) continue;
            float priority = ScoreAmbientStimulus(stimulus, query.Radius);
            if (priority < StimulusPriorityThreshold) continue;

            if (stimulus.Type == OUTL_StimulusType.SightDanger || stimulus.Type == OUTL_StimulusType.Fear || stimulus.Type == OUTL_StimulusType.Fire || stimulus.Type == OUTL_StimulusType.Death || stimulus.Type == OUTL_StimulusType.Alert)
                CurrentDanger = Mathf.Clamp01(Mathf.Max(CurrentDanger, priority));
            if (stimulus.Type == OUTL_StimulusType.SightFood || stimulus.Type == OUTL_StimulusType.Resource)
                CurrentFood = Mathf.Clamp01(Mathf.Max(CurrentFood, priority));

            ReceiveStimulus(stimulus, priority);
        }
    }

    private float ScoreAmbientStimulus(OUTL_Stimulus stimulus, float radius)
    {
        float dist = Vector3.Distance(transform.position, stimulus.Position);
        float effectiveRadius = Mathf.Max(0.1f, Mathf.Max(radius, stimulus.Radius));
        float falloff = Mathf.Clamp01(1f - dist / effectiveRadius);
        float strength = stimulus.Strength > 0f ? stimulus.Strength : stimulus.Loudness;
        return Mathf.Max(0f, stimulus.Priority) * Mathf.Max(0.01f, strength) * Mathf.Max(0.05f, stimulus.Confidence) * Mathf.Max(0.05f, falloff);
    }

    private void UpdateVisibleState(OUTL_EntityRuntime self, OUTL_EntityRuntime target, float time)
    {
        if (self == null) return;
        CurrentTargetDistance = target != null && target.Adapter != null ? Vector3.Distance(transform.position, target.Adapter.transform.position) : 0f;
        OUTL_AIStateId nextState = DeriveVisibleState(self, target, time);
        CurrentState = nextState;
        CurrentGoal = CurrentIntent;
        CurrentTargetVisible = LastTargetVisible;
        CurrentFear = Mathf.Clamp01(Mathf.Max(CurrentDanger, MemoryFear, self.Stats.Get(OUTL_StatId.Health, 100f) <= Profile.LowHealthThreshold ? 0.75f : 0f));
        CurrentAggression = target != null ? Mathf.Max(1f, MemoryAggression) : Mathf.Clamp01(Mathf.Max(LastStimulusPriority, MemoryAggression, Suspicion * 0.5f));
        CurrentMorale = Mathf.Clamp01(1f - CurrentFear * 0.65f);
        ApplyStateTableRow();
    }

    private OUTL_AIStateId DeriveVisibleState(OUTL_EntityRuntime self, OUTL_EntityRuntime target, float time)
    {
        if (self.State.GetFlag(OUTL_StateId.Dead) || self.Stats.Get(OUTL_StatId.Health, 1f) <= 0f) return OUTL_AIStateId.Dead;
        if (CurrentIntent == "Flee") return OUTL_AIStateId.Flee;
        if (CurrentIntent == "Investigate") return OUTL_AIStateId.Investigate;
        if (CurrentIntent == "Search") return OUTL_AIStateId.Search;
        if (CurrentIntent == "Patrol") return OUTL_AIStateId.Patrol;
        if (CurrentIntent == "EatOrUseResource") return OUTL_AIStateId.EatOrUseResource;
        if (LastStimulusType == OUTL_StimulusType.TookDamage && time - LastStimulusTime <= 1.25f) return CurrentCover != null ? OUTL_AIStateId.TakeCover : OUTL_AIStateId.Alert;
        if (LastStimulusType == OUTL_StimulusType.HeardNoise && target == null && time - LastStimulusTime <= ReadMemoryDuration()) return OUTL_AIStateId.Investigate;
        if (LastStimulusType == OUTL_StimulusType.LostTarget && target == null && time - LastStimulusTime <= ReadMemoryDuration()) return OUTL_AIStateId.Search;
        if (LastStimulusType == OUTL_StimulusType.SightDanger && FleeFromDanger) return OUTL_AIStateId.Flee;
        if (target != null)
        {
            OUTL_AttackProfile profile = ResolveAttackProfile(target.Adapter != null ? target.Adapter.transform.position : transform.position);
            if (profile == null) return OUTL_AIStateId.Alert;
            if (PreferRangedCombat && AttackDriver != null && profile == AttackDriver.Melee && CurrentTargetDistance < Mathf.Max(0.1f, MinSafeRange) && ReadWorldTime() - lastWeaponSwitchTime <= Mathf.Max(0.05f, SwitchCooldown))
                return OUTL_AIStateId.SwitchWeapon;
            if ((AttackDriver != null && profile == AttackDriver.Melee) || profile.Mode == OUTL_AttackMode.Melee) return OUTL_AIStateId.AttackMelee;
            return OUTL_AIStateId.AttackRanged;
        }
        if (CurrentIntent == "Work") return OUTL_AIStateId.Work;
        return OUTL_AIStateId.Idle;
    }

    private void ApplyStateTableRow()
    {
        OUTL_AIStateTableRow row;
        if (StateTable != null && StateTable.TryGetRow(CurrentState, out row) && row != null)
        {
            CurrentAnimationHint = row.AnimationHint;
            CurrentDebugColor = row.DebugColor;
            NextAction = row.MainCommand;
            if (CurrentAttackProfile == null && row.AttackProfile != null) CurrentAttackProfile = row.AttackProfile;
            return;
        }

        CurrentAnimationHint = CurrentState.ToString();
        CurrentDebugColor = OUTL_AIStateTable.DefaultColor(CurrentState);
        NextAction = CurrentTaskName;
    }

    private void ApplyDeadState(string eventText)
    {
        activeSchedule = null;
        CurrentScheduleId = "";
        CurrentTaskIndex = 0;
        CurrentTaskName = "Dead";
        CurrentIntent = "Dead";
        CurrentGoal = "Dead";
        CurrentState = OUTL_AIStateId.Dead;
        CurrentTarget = OUTL_EntityId.None;
        CurrentOrder = default(OUTL_SquadOrder);
        LastTargetVisible = false;
        CurrentTargetVisible = false;
        CurrentAttackProfile = null;
        CurrentWeapon = "";
        NextAction = "Stop";
        if (!string.IsNullOrEmpty(eventText)) LastEvent = eventText;
        StopMove();
        if (CurrentCover != null && CurrentCover.Occupant == Entity) CurrentCover.Occupant = null;
        CurrentCover = null;
        if (AttackDriver != null) AttackDriver.BlockedByVitals = true;
    }

    private void SetVisibleStimulus(OUTL_StimulusType type, OUTL_EntityId source, Vector3 position, string key, float priority)
    {
        LastStimulus = new OUTL_Stimulus(type, source, position, 0f, priority, 1f, priority, ReadMemoryDuration(), key, null);
        LastStimulusType = type;
        LastStimulusKey = key;
        LastStimulusPosition = position;
        LastStimulusPriority = Mathf.Max(LastStimulusPriority, priority);
        LastStimulusTime = LastStimulus.Time;
    }

    private void InterruptScheduleForStimulus(OUTL_StimulusType type)
    {
        if (!UseStimulusInterrupts || !IsInterruptStimulus(type)) return;
        activeSchedule = null;
        CurrentScheduleId = "";
        CurrentTaskIndex = 0;
        CurrentTaskName = "Interrupted:" + type;
        taskEndTime = 0f;
    }

    private static bool IsInterruptStimulus(OUTL_StimulusType type)
    {
        return type == OUTL_StimulusType.TookDamage
            || type == OUTL_StimulusType.SightEnemy
            || type == OUTL_StimulusType.HeardNoise
            || type == OUTL_StimulusType.HeardCombat
            || type == OUTL_StimulusType.LostTarget
            || type == OUTL_StimulusType.LowHealth
            || type == OUTL_StimulusType.SightDanger
            || type == OUTL_StimulusType.SightFood
            || type == OUTL_StimulusType.Combat
            || type == OUTL_StimulusType.Death
            || type == OUTL_StimulusType.Fear
            || type == OUTL_StimulusType.Fire
            || type == OUTL_StimulusType.Resource
            || type == OUTL_StimulusType.Alert
            || type == OUTL_StimulusType.Egregore;
    }

    private static bool IsTargetStimulus(OUTL_StimulusType type)
    {
        return type == OUTL_StimulusType.SightEnemy
            || type == OUTL_StimulusType.TookDamage
            || type == OUTL_StimulusType.HeardCombat
            || type == OUTL_StimulusType.SightDanger
            || type == OUTL_StimulusType.Combat
            || type == OUTL_StimulusType.Damage
            || type == OUTL_StimulusType.Fear
            || type == OUTL_StimulusType.Fire
            || type == OUTL_StimulusType.Alert;
    }

    private static bool IsInvestigateStimulus(OUTL_StimulusType type)
    {
        return type == OUTL_StimulusType.HeardNoise
            || type == OUTL_StimulusType.HeardCombat
            || type == OUTL_StimulusType.TookDamage
            || type == OUTL_StimulusType.Sound
            || type == OUTL_StimulusType.Damage
            || type == OUTL_StimulusType.Command
            || type == OUTL_StimulusType.Touch
            || type == OUTL_StimulusType.Suspicion
            || type == OUTL_StimulusType.Smell
            || type == OUTL_StimulusType.Territory
            || type == OUTL_StimulusType.Resource
            || type == OUTL_StimulusType.Alert
            || type == OUTL_StimulusType.Egregore
            || type == OUTL_StimulusType.Scripted;
    }

    private float ReadMemoryDuration()
    {
        if (PerceptionProfile != null && PerceptionProfile.MemoryDuration > 0f) return PerceptionProfile.MemoryDuration;
        return Mathf.Max(0.1f, StimulusForgetAfter);
    }

    private bool FindCover(float radius, LayerMask mask)
    {
        Vector3 threat = LastKnownTargetPosition != Vector3.zero ? LastKnownTargetPosition : transform.position + transform.forward * 8f;
        CurrentCover = OUTL_CoverSystem.FindBestCover(Entity, threat, radius, mask.value == 0 ? CoverVisibilityMask : mask);
        if (CurrentCover != null)
        {
            SetVisibleStimulus(OUTL_StimulusType.FoundCover, Entity != null ? Entity.Id : OUTL_EntityId.None, CurrentCover.StandPoint, "cover", 0.45f);
            if (Diary != null) Diary.Write(OUTL_DiaryEventType.TookCover, CurrentCover.name);
            return true;
        }
        return true;
    }

    private bool FollowSquadOrder(OUTL_AITaskDef task, float deltaTime)
    {
        if (!CurrentOrder.IsValid) return true;
        if (CurrentOrder.Type == OUTL_SquadOrderType.TakeCover)
        {
            if (CurrentCover == null) FindCover(task.Distance > 0f ? task.Distance : CoverSearchRadius, task.Mask);
            if (CurrentCover != null) return MoveTowards(CurrentCover.StandPoint, 1.1f, task.SpeedMultiplier, deltaTime);
        }
        if (CurrentOrder.Type == OUTL_SquadOrderType.Attack && CurrentOrder.Target.IsValid) return true;
        return MoveTowards(CurrentOrder.Position, task.Distance, task.SpeedMultiplier, deltaTime);
    }

    private void AdvanceTask(float time)
    {
        LastEvent = "GoalCompleted:" + CurrentTaskName;
        taskEndTime = 0f;
        CurrentTaskIndex++;
        if (activeSchedule == null || activeSchedule.Tasks == null || activeSchedule.Tasks.Length == 0)
        {
            CurrentTaskIndex = 0;
            return;
        }
        if (CurrentTaskIndex >= activeSchedule.Tasks.Length) CurrentTaskIndex = activeSchedule.Loop ? 0 : activeSchedule.Tasks.Length - 1;
    }

    private void ExecuteFallbackCombat(OUTL_World world, OUTL_EntityRuntime self, OUTL_EntityRuntime target, float time, float deltaTime)
    {
        if (target == null || target.Adapter == null)
        {
            if (UseActorInputContract)
            {
                NextAction = "InputLostTarget";
                return;
            }
            if (NavMover != null && time - lastTargetSeenTime > ForgetTargetAfter) NavMover.Stop("ai_actor_lost_target");
            return;
        }

        Vector3 targetPos = target.Adapter.transform.position;
        float preferredRange = PreferredRange > 0f ? PreferredRange : Profile.AttackDistance;
        float attackSqr = preferredRange * preferredRange;
        float distSqr = (targetPos - transform.position).sqrMagnitude;

        if (distSqr <= attackSqr)
        {
            StopMove();
            Attack(world, self, target);
            return;
        }

        if (MoveToTarget) MoveTowards(ResolveChasePoint(target, targetPos, time), preferredRange, 1f, deltaTime);
    }

    private Vector3 ResolveChasePoint(OUTL_EntityRuntime target, Vector3 fallback, float time)
    {
        if (InterceptPlanner == null) return fallback;
        return InterceptPlanner.ResolveMoveTarget(target, fallback, time);
    }

    private void Attack(OUTL_World world, OUTL_EntityRuntime self, OUTL_EntityRuntime target)
    {
        if (target == null || target.Adapter == null) return;
        if (self == null || self.State.GetFlag(OUTL_StateId.Dead)) return;
        if (target.State.GetFlag(OUTL_StateId.Dead) || target.Stats.Get(OUTL_StatId.Health, 1f) <= 0f) return;

        Vector3 targetPos = target.Adapter.transform.position;
        Vector3 aimPoint = targetPos + Vector3.up;

        OUTL_AttackProfile attackProfile = ResolveAttackProfile(targetPos);
        if (UseActorInputContract)
        {
            NextAction = "InputAttack";
            LastKnownTargetPosition = targetPos;
            CurrentTarget = target.Id;
            return;
        }

        Face(targetPos, Mathf.Max(0.02f, ReadVisualDeltaTime()));
        if (UseAttackDriver && AttackDriver != null && attackProfile != null)
        {
            if (AttackDriver.FireAt(attackProfile, aimPoint))
            {
                if (AnimationBridge != null) AnimationBridge.NotifyAttack();
                if (Diary != null) Diary.Write(OUTL_DiaryEventType.Attacked, target.Id.ToString());
            }
            return;
        }

        float attackDistance = Profile != null ? Mathf.Max(0.1f, Profile.AttackDistance) : 1.5f;
        float distSqr = (targetPos - transform.position).sqrMagnitude;
        if (distSqr > (attackDistance + 0.35f) * (attackDistance + 0.35f)) return;

        OUTL_Command cmd = new OUTL_Command(OUTL_CommandType.Attack, self.Id, target.Id);
        cmd.FloatValue = self.Stats.Get(OUTL_StatId.Damage, 8f);
        cmd.Point = targetPos;
        world.Commands.Send(cmd);
    }

    private OUTL_AttackProfile ResolveAttackProfile(Vector3 targetPosition)
    {
        if (AttackDriver == null) return null;
        Vector3 origin = AttackDriver.Muzzle != null ? AttackDriver.Muzzle.position : transform.position + Vector3.up;
        float distSqr = (targetPosition + Vector3.up - origin).sqrMagnitude;
        CurrentTargetDistance = Mathf.Sqrt(distSqr);

        if (AttackDriver.Melee != null)
        {
            float meleeRange = Mathf.Max(0.05f, AttackDriver.Melee.Range + AttackDriver.Melee.Radius + AttackDriver.SmartMeleeExtraRange);
            if (distSqr <= meleeRange * meleeRange)
            {
                if (CurrentAttackProfile != AttackDriver.Melee) lastWeaponSwitchTime = ReadWorldTime();
                CurrentWeapon = "Melee";
                CurrentAttackProfile = AttackDriver.Melee;
                return AttackDriver.Melee;
            }
        }

        if (AttackDriver.Primary != null)
        {
            CurrentWeapon = "Primary";
            CurrentAttackProfile = AttackDriver.Primary;
            return AttackDriver.Primary;
        }
        if (AttackDriver.Secondary != null)
        {
            CurrentWeapon = "Secondary";
            CurrentAttackProfile = AttackDriver.Secondary;
            return AttackDriver.Secondary;
        }
        CurrentWeapon = AttackDriver.Melee != null ? "Melee" : "";
        CurrentAttackProfile = AttackDriver.Melee;
        return AttackDriver.Melee;
    }

    private bool MoveTowards(Vector3 point, float stopDistance, float speedMultiplier, float deltaTime)
    {
        if (Stationary) return true;
        if (UseActorInputContract)
        {
            Vector3 flat = point - transform.position;
            flat.y = 0f;
            bool reached = flat.sqrMagnitude <= stopDistance * stopDistance;
            NextAction = reached ? "InputMoveReached" : "InputMove";
            return reached;
        }
        return OUTL_AIMovementUtility.MoveTowards(MoveRoot, Profile, NavMover, UseNavMeshMover, MoveToTarget, point, stopDistance, speedMultiplier, deltaTime);
    }

    private void Face(Vector3 point, float deltaTime)
    {
        if (UseActorInputContract) return;
        OUTL_AIMovementUtility.Face(transform, point, deltaTime);
    }

    private void StopMove()
    {
        if (UseActorInputContract)
        {
            NextAction = "InputStop";
            return;
        }
        OUTL_AIMovementUtility.Stop(NavMover);
    }

    private OUTL_EntityRuntime ResolveTarget(OUTL_World world, OUTL_EntityRuntime self, float time)
    {
        OUTL_EntityRuntime target;
        if (CurrentTarget.IsValid && world.Registry.TryGet(CurrentTarget, out target) && target != null && target.Adapter != null)
        {
            bool visible = CanSeeRuntime(target);
            if (!RequireLineOfSightToKeepTarget || visible)
            {
                if (time < nextTargetRefreshTime) return target;
                nextTargetRefreshTime = time + Mathf.Max(0.05f, TargetRefreshInterval);
                return target;
            }
        }

        if (time < nextTargetRefreshTime) return null;
        nextTargetRefreshTime = time + Mathf.Max(0.05f, TargetRefreshInterval);
        return FindTarget(world, self);
    }

    private OUTL_EntityRuntime FindTarget(OUTL_World world, OUTL_EntityRuntime self)
    {
        return OUTL_AIPerceptionUtility.FindTarget(world, self, transform, Profile, PerceptionProfile, Profile != null ? Profile.EnemyTags : null, RequireLineOfSightToAcquireTarget, EyeHeight, TargetEyeHeight, SightBlockMask);
    }

    private bool CanSeeRuntime(OUTL_EntityRuntime target)
    {
        return OUTL_AIPerceptionUtility.CanSeeRuntime(transform, Entity, Profile, PerceptionProfile, target, EyeHeight, TargetEyeHeight, SightBlockMask);
    }

    public void FillDebugRow(ref OUTL_AIStateDebugRow row)
    {
        OUTL_EntityRuntime runtime = Entity != null ? Entity.Runtime : null;
        row.Entity = runtime != null && !string.IsNullOrEmpty(runtime.TargetName) ? runtime.TargetName : (runtime != null && !string.IsNullOrEmpty(runtime.ClassName) ? runtime.ClassName : (Entity != null && Entity.Id.IsValid ? Entity.Id.ToString() : "unbound"));
        row.State = CurrentState;
        row.Goal = CurrentGoal;
        row.Stimulus = LastStimulusType != OUTL_StimulusType.None ? LastStimulusType + ":" + LastStimulusKey : "none";
        row.Target = CurrentTarget.IsValid ? CurrentTarget.ToString() : "none";
        row.Weapon = string.IsNullOrEmpty(CurrentWeapon) ? "-" : CurrentWeapon;
        row.AttackProfile = AttackProfileName(CurrentAttackProfile);
        row.AnimationHint = string.IsNullOrEmpty(CurrentAnimationHint) ? "-" : CurrentAnimationHint;
        row.Health = Entity != null && Entity.Runtime != null ? Entity.Runtime.Stats.Get(OUTL_StatId.Health, 0f) : 0f;
        row.Fear = CurrentFear;
        row.Aggression = CurrentAggression;
        row.Morale = CurrentMorale;
        row.Suspicion = Suspicion;
        row.Distance = CurrentTargetDistance;
        row.Visibility = CurrentTargetVisible;
        row.Danger = CurrentDanger;
        row.Food = CurrentFood;
        row.NextAction = string.IsNullOrEmpty(NextAction) ? "-" : NextAction;
        row.LastEvent = string.IsNullOrEmpty(LastEvent) ? "-" : LastEvent;
        row.DebugColor = CurrentDebugColor;
    }

    private void TraceState(OUTL_EntityRuntime self)
    {
        if (self == null || !OUTL_DebugLog.ShouldTraceAI(self.Id)) return;
        string state = DescribeThinking();
        if (state == lastTraceState) return;
        lastTraceState = state;
        OUTL_DebugLog.TraceAI(self.Id, state);
    }

    private void ResolveReferences()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (MoveRoot == null) MoveRoot = transform;
        if (NavMover == null) NavMover = GetComponent<OUTL_NavMeshMover>();
        if (AttackDriver == null) AttackDriver = GetComponent<OUTL_AttackDriver>();
        if (AnimationBridge == null) AnimationBridge = GetComponentInChildren<OUTL_CharacterAnimationBridge>(true);
        if (PatrolRoute == null) PatrolRoute = GetComponent<OUTL_PatrolRoute>();
        if (Diary == null) Diary = GetComponent<OUTL_EntityDiary>();
        if (InterceptPlanner == null) InterceptPlanner = GetComponent<OUTL_AIInterceptPlanner>();
        if (TacticalPlanner == null) TacticalPlanner = GetComponent<OUTL_TacticalPlanner>();
    }

    private static string AttackProfileName(OUTL_AttackProfile profile)
    {
        if (profile == null) return "-";
        return !string.IsNullOrEmpty(profile.AttackId) ? profile.AttackId : profile.name;
    }

    private float ReadVisualDeltaTime()
    {
        OUTL_World world = OUTL_World.Instance;
        if (world != null) return world.IsPaused ? 0f : world.DeltaTime;
        return Time.deltaTime;
    }

    private static float ReadWorldTime()
    {
        OUTL_World world = OUTL_World.Instance;
        return world != null ? world.WorldTime : Time.time;
    }
}
