using UnityEngine;
using Time = OUT_SimTime;

[DisallowMultipleComponent]
public class OUT_AIActorBrain : MonoBehaviour, IOutThinkable, IOutRuntimeTierReceiver
{
    [Header("References")]
    [SerializeField] private OUT_AIPerception perception;
    [SerializeField] private OUT_AIHearingSensor hearingSensor;
    [SerializeField] private OUT_AIMemoryBuffer memoryBuffer;
    [SerializeField] private OUT_AIScheduleResolver scheduleResolver;
    [SerializeField] private OUT_AITaskRunner taskRunner;
    [SerializeField] private OUT_AIAttackEvaluator attackEvaluator;

    [Header("Think")]
    [SerializeField][Min(0.01f)] private float thinkInterval = 0.1f;
    [SerializeField] private bool useRandomThinkInterval = false;
    [SerializeField][Min(0.01f)] private float thinkIntervalMin = 0.08f;
    [SerializeField][Min(0.01f)] private float thinkIntervalMax = 0.16f;
    [SerializeField] private bool randomizeInitialThinkOffset = true;

    [Header("OUT CORE Scheduler")]
    [SerializeField] private bool useCentralThinkScheduler = false;
    [SerializeField] private OUT_ThinkGroup thinkGroup = OUT_ThinkGroup.NearAI;
    [SerializeField] private bool scaleThinkByRuntimeTier = true;
    [SerializeField][Min(0.01f)] private float nearThinkInterval = 0.1f;
    [SerializeField][Min(0.05f)] private float midThinkInterval = 0.35f;
    [SerializeField][Min(0.1f)] private float farThinkInterval = 1.25f;
    [SerializeField] private bool thinkWhenDormant = false;
    [SerializeField][Min(0.25f)] private float dormantThinkInterval = 3f;

    [Header("Debug")]
    [SerializeField] private bool logBrainEvents = false;
    [SerializeField] private bool logConditionChanges = false;
    [SerializeField] private bool logScheduleChanges = true;
    [SerializeField] private float minLogInterval = 0.15f;

    [Header("Runtime")]
    [SerializeField] private OUT_AIBlackboard blackboard = new OUT_AIBlackboard();

    private OUT_AIState _currentState = OUT_AIState.Idle;
    private OUT_AIConditionFlags _conditions = OUT_AIConditionFlags.None;
    private OUT_AIConditionFlags _lastLoggedConditions = OUT_AIConditionFlags.None;
    private OUT_AIState _lastLoggedState = OUT_AIState.None;
    private string _lastScheduleName;
    private float _lastLogTime;
    private float _nextThinkTime;
    private OUT_RuntimeTier _runtimeTier = OUT_RuntimeTier.Full;

    public OUT_AIState CurrentState => _currentState;
    public OUT_AIConditionFlags Conditions => _conditions;
    public OUT_AIBlackboard Blackboard => blackboard;

    public bool IsThinkEnabled => isActiveAndEnabled && (_runtimeTier != OUT_RuntimeTier.Dormant || thinkWhenDormant);
    public OUT_ThinkGroup ThinkGroup => thinkGroup;
    public float ThinkInterval => GetEffectiveThinkInterval();

    private void Awake()
    {
        if (perception == null)
            perception = GetComponent<OUT_AIPerception>();

        if (hearingSensor == null)
            hearingSensor = GetComponent<OUT_AIHearingSensor>();

        if (memoryBuffer == null)
            memoryBuffer = GetComponent<OUT_AIMemoryBuffer>();

        if (scheduleResolver == null)
            scheduleResolver = GetComponent<OUT_AIScheduleResolver>();

        if (taskRunner == null)
            taskRunner = GetComponent<OUT_AITaskRunner>();

        if (attackEvaluator == null)
            attackEvaluator = GetComponent<OUT_AIAttackEvaluator>();
    }

    private void OnEnable()
    {
        ResetRuntimeState();

        float initialInterval = GetNextThinkInterval();

        if (randomizeInitialThinkOffset)
            _nextThinkTime = Time.time + Random.Range(0f, initialInterval);
        else
            _nextThinkTime = Time.time;

        if (useCentralThinkScheduler)
            OUT_ThinkScheduler.Register(this);
    }

    private void OnDisable()
    {
        if (useCentralThinkScheduler)
            OUT_ThinkScheduler.Unregister(this);
    }

    private void Update()
    {
        if (useCentralThinkScheduler)
            return;

        if (Time.time < _nextThinkTime)
            return;

        _nextThinkTime = Time.time + GetNextThinkInterval();
        Think();
    }

    public void OutThink(float deltaTime)
    {
        Think();
    }

    public void OnRuntimeTierChanged(OUT_RuntimeTier oldTier, OUT_RuntimeTier newTier)
    {
        _runtimeTier = newTier;
    }

    private float GetNextThinkInterval()
    {
        if (scaleThinkByRuntimeTier)
            return GetEffectiveThinkInterval();

        if (!useRandomThinkInterval)
            return Mathf.Max(0.01f, thinkInterval);

        float min = Mathf.Max(0.01f, thinkIntervalMin);
        float max = Mathf.Max(min, thinkIntervalMax);
        return Random.Range(min, max);
    }

    private float GetEffectiveThinkInterval()
    {
        if (!scaleThinkByRuntimeTier)
            return Mathf.Max(0.01f, thinkInterval);

        switch (_runtimeTier)
        {
            case OUT_RuntimeTier.Dormant: return Mathf.Max(0.25f, dormantThinkInterval);
            case OUT_RuntimeTier.Far: return Mathf.Max(0.1f, farThinkInterval);
            case OUT_RuntimeTier.Mid: return Mathf.Max(0.05f, midThinkInterval);
            case OUT_RuntimeTier.Near:
            case OUT_RuntimeTier.Full:
            default:
                return Mathf.Max(0.01f, nearThinkInterval);
        }
    }

    private void Think()
    {
        OUT_AIConditionFlags beforeConditions = _conditions;
        OUT_AIState beforeState = _currentState;

        if (perception != null)
            perception.Evaluate(blackboard, ref _conditions);

        if (blackboard.Enemy != null && memoryBuffer != null)
            memoryBuffer.ObserveEnemy(blackboard.Enemy, blackboard.EnemyLastKnownPosition);

        if (hearingSensor != null)
            hearingSensor.Evaluate(blackboard, ref _conditions);

        if (memoryBuffer != null)
            memoryBuffer.ApplyToBlackboard(blackboard, ref _conditions);

        ClearAttackConditions();

        if (attackEvaluator != null)
            attackEvaluator.Evaluate(blackboard, ref _conditions, _currentState);

        UpdateState();
        LogBrainIfNeeded(beforeState, beforeConditions);

        if (scheduleResolver == null || taskRunner == null)
            return;

        bool needsSchedule =
            !taskRunner.HasActiveSchedule ||
            (_conditions & OUT_AIConditionFlags.MoveFailed) != 0 ||
            (taskRunner.CurrentSchedule != null &&
             (taskRunner.CurrentSchedule.InterruptMask & _conditions) != 0);

        if (needsSchedule)
        {
            string oldSchedule = taskRunner.CurrentSchedule != null ? taskRunner.CurrentSchedule.Name : "<none>";
            OUT_AISchedule next = scheduleResolver.Resolve(_currentState, _conditions, blackboard);
            _conditions &= ~OUT_AIConditionFlags.MoveFailed;

            if (next != null)
            {
                taskRunner.SetSchedule(next);
                LogScheduleIfNeeded(oldSchedule, next);
            }
        }

        taskRunner.Tick(blackboard, ref _conditions, _currentState);
    }

    private void ResetRuntimeState()
    {
        _currentState = OUT_AIState.Idle;
        _conditions = OUT_AIConditionFlags.None;
        _lastLoggedConditions = OUT_AIConditionFlags.None;
        _lastLoggedState = OUT_AIState.None;
        _lastScheduleName = string.Empty;
        _lastLogTime = 0f;

        if (blackboard == null)
            blackboard = new OUT_AIBlackboard();

        blackboard.ResetState();
    }

    private void ClearAttackConditions()
    {
        _conditions &= ~(
            OUT_AIConditionFlags.CanRangeAttack1 |
            OUT_AIConditionFlags.CanRangeAttack2 |
            OUT_AIConditionFlags.CanMeleeAttack1 |
            OUT_AIConditionFlags.CanMeleeAttack2 |
            OUT_AIConditionFlags.EnemyTooFar |
            OUT_AIConditionFlags.NoFriendlyFire);
    }

    private void UpdateState()
    {
        if ((_conditions & OUT_AIConditionFlags.ScriptLocked) != 0)
        {
            _currentState = OUT_AIState.Scripted;
            return;
        }

        if (blackboard.Enemy != null || (_conditions & OUT_AIConditionFlags.SeeEnemy) != 0)
        {
            _currentState = OUT_AIState.Combat;
            return;
        }

        if ((_conditions & OUT_AIConditionFlags.HasEnemyLKP) != 0 ||
            (_conditions & OUT_AIConditionFlags.HearCombat) != 0 ||
            (_conditions & OUT_AIConditionFlags.HearPlayer) != 0 ||
            (_conditions & OUT_AIConditionFlags.HearDanger) != 0)
        {
            _currentState = OUT_AIState.Alert;
            return;
        }

        _currentState = OUT_AIState.Idle;
    }

    private void LogBrainIfNeeded(OUT_AIState beforeState, OUT_AIConditionFlags beforeConditions)
    {
        if (!logBrainEvents && !logConditionChanges)
            return;

        if (Time.time - _lastLogTime < minLogInterval)
            return;

        bool stateChanged = beforeState != _currentState || _lastLoggedState != _currentState;
        bool conditionsChanged = beforeConditions != _conditions || _lastLoggedConditions != _conditions;

        if (logBrainEvents && stateChanged)
        {
            OUT_AIDebugLogService.Log(this, OUT_AIDebugLogService.AIEventKind.Brain,
                $"state {beforeState} -> {_currentState} enemy:{(blackboard.Enemy != null ? blackboard.Enemy.name : "none")} interest:{blackboard.InterestStrength:0.00}");
            _lastLogTime = Time.time;
        }

        if (logConditionChanges && conditionsChanged)
        {
            OUT_AIDebugLogService.Log(this, OUT_AIDebugLogService.AIEventKind.Brain,
                $"conditions {_conditions}");
            _lastLogTime = Time.time;
        }

        _lastLoggedState = _currentState;
        _lastLoggedConditions = _conditions;
    }

    private void LogScheduleIfNeeded(string oldSchedule, OUT_AISchedule next)
    {
        if (!logScheduleChanges || next == null)
            return;

        if (_lastScheduleName == next.Name && oldSchedule == next.Name)
            return;

        OUT_AIDebugLogService.Log(this, OUT_AIDebugLogService.AIEventKind.Schedule,
            $"schedule {oldSchedule} -> {next.Name} state:{_currentState} conditions:{_conditions}");

        _lastScheduleName = next.Name;
    }
}
