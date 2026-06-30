using UnityEngine;

[DisallowMultipleComponent]
public class OUT_AITaskRunner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private OUT_AIAttackExecutor attackExecutor;

    [Header("Movement")]
    [SerializeField] private float defaultMoveAcceptanceRadius = 0.6f;
    [SerializeField] private float defaultCoverMinDistance = 3f;
    [SerializeField] private float defaultCoverMaxDistance = 18f;
    [SerializeField] private bool allowTriangulation = true;

    [Header("Debug")]
    [SerializeField] private bool logTasks = false;
    [SerializeField] private bool logMoveFailures = true;

    private IOutAILocomotion _locomotion;
    private IOutAIRoutePlanner _routePlanner;

    private OUT_AISchedule _currentSchedule;
    private int _taskIndex;
    private bool _taskStarted;
    private float _taskStartedTime;

    public OUT_AISchedule CurrentSchedule => _currentSchedule;
    public bool HasActiveSchedule => _currentSchedule != null && _currentSchedule.IsValid && _taskIndex < _currentSchedule.Tasks.Length;

    private void Awake()
    {
        _locomotion = FindInterface<IOutAILocomotion>();
        _routePlanner = FindInterface<IOutAIRoutePlanner>();

        if (attackExecutor == null)
            attackExecutor = GetComponent<OUT_AIAttackExecutor>();
    }

    private void OnDisable()
    {
        _currentSchedule = null;
        _taskIndex = 0;
        _taskStarted = false;
    }

    public void SetSchedule(OUT_AISchedule schedule)
    {
        _currentSchedule = schedule;
        _taskIndex = 0;
        _taskStarted = false;
    }

    public void Tick(OUT_AIBlackboard blackboard, ref OUT_AIConditionFlags conditions, OUT_AIState state)
    {
        if (!HasActiveSchedule)
            return;

        OUT_AITask task = _currentSchedule.Tasks[_taskIndex];

        if (!_taskStarted)
        {
            StartTask(task, blackboard);
            _taskStarted = true;
            _taskStartedTime = Time.time;
        }

        if (_currentSchedule == null)
            return;

        if (UpdateTask(task, blackboard, ref conditions, state))
        {
            if (logTasks)
                OUT_AIDebugLogService.Log(this, OUT_AIDebugLogService.AIEventKind.Task, $"finish task {_currentSchedule.Name}[{_taskIndex}] {task.Type}");

            _taskIndex++;
            _taskStarted = false;

            if (_taskIndex >= _currentSchedule.Tasks.Length)
                _currentSchedule = null;
        }
    }

    private void StartTask(OUT_AITask task, OUT_AIBlackboard blackboard)
    {
        if (logTasks)
            OUT_AIDebugLogService.Log(this, OUT_AIDebugLogService.AIEventKind.Task, $"start task {(_currentSchedule != null ? _currentSchedule.Name : "<none>")}[{_taskIndex}] {task.Type} vec:{task.VectorValue} value:{task.FloatValue:0.00}");

        switch (task.Type)
        {
            case OUT_AITaskType.StopMoving:
                _locomotion?.Stop();
                break;

            case OUT_AITaskType.Wait:
                blackboard.WaitUntilTime = Time.time + Mathf.Max(0.01f, task.FloatValue);
                break;

            case OUT_AITaskType.FaceEnemy:
                if (blackboard.Enemy != null)
                    _locomotion?.Face(GetTargetPoint(blackboard.Enemy.transform));
                break;

            case OUT_AITaskType.FacePoint:
                _locomotion?.Face(task.VectorValue);
                break;

            case OUT_AITaskType.MoveToEnemyLKP:
                {
                    Vector3 destination = blackboard.Enemy != null
                        ? GetTargetPoint(blackboard.Enemy.transform)
                        : blackboard.EnemyLastKnownPosition;

                    blackboard.MoveTargetPoint = destination;

                    if (_routePlanner != null)
                    {
                        OUT_AIRouteRequest request = new OUT_AIRouteRequest
                        {
                            Destination = destination,
                            ThreatPosition = blackboard.EnemyLastKnownPosition,
                            MinDistance = 0f,
                            MaxDistance = 0f,
                            RequireCover = false,
                            AllowTriangulation = allowTriangulation,
                            RefreshIfStale = true
                        };

                        if (_routePlanner.TryBuildRoute(request, out Vector3 firstWaypoint))
                        {
                            _locomotion?.TryMoveTo(firstWaypoint, defaultMoveAcceptanceRadius);
                            LogRoute("MoveToEnemyLKP", destination, firstWaypoint, true);
                        }
                        else
                        {
                            LogRoute("MoveToEnemyLKP", destination, Vector3.zero, false);
                        }
                    }
                    else
                    {
                        _locomotion?.TryMoveTo(destination, defaultMoveAcceptanceRadius);
                    }

                    break;
                }

            case OUT_AITaskType.MoveToPoint:
                blackboard.MoveTargetPoint = task.VectorValue;

                if (_routePlanner != null)
                {
                    OUT_AIRouteRequest request = new OUT_AIRouteRequest
                    {
                        Destination = task.VectorValue,
                        ThreatPosition = Vector3.zero,
                        MinDistance = 0f,
                        MaxDistance = 0f,
                        RequireCover = false,
                        AllowTriangulation = allowTriangulation,
                        RefreshIfStale = true
                    };

                    if (_routePlanner.TryBuildRoute(request, out Vector3 firstWaypoint))
                    {
                        _locomotion?.TryMoveTo(firstWaypoint, defaultMoveAcceptanceRadius);
                        LogRoute("MoveToPoint", task.VectorValue, firstWaypoint, true);
                    }
                    else
                    {
                        LogRoute("MoveToPoint", task.VectorValue, Vector3.zero, false);
                    }
                }
                else
                {
                    _locomotion?.TryMoveTo(task.VectorValue, defaultMoveAcceptanceRadius);
                }

                break;

            case OUT_AITaskType.TakeCover:
                {
                    Vector3 threat = blackboard.Enemy != null
                        ? GetTargetPoint(blackboard.Enemy.transform)
                        : blackboard.EnemyLastKnownPosition;

                    if (_routePlanner != null)
                    {
                        OUT_AIRouteRequest request = new OUT_AIRouteRequest
                        {
                            Destination = transform.position,
                            ThreatPosition = threat,
                            MinDistance = defaultCoverMinDistance,
                            MaxDistance = defaultCoverMaxDistance,
                            RequireCover = true,
                            AllowTriangulation = allowTriangulation,
                            RefreshIfStale = true
                        };

                        if (_routePlanner.TryBuildRoute(request, out Vector3 firstWaypoint))
                        {
                            blackboard.CoverPoint = firstWaypoint;
                            _locomotion?.TryMoveTo(firstWaypoint, defaultMoveAcceptanceRadius);
                            LogRoute("TakeCover", threat, firstWaypoint, true);
                        }
                        else
                        {
                            LogRoute("TakeCover", threat, Vector3.zero, false);
                        }
                    }

                    break;
                }

            case OUT_AITaskType.Reload:
            case OUT_AITaskType.RangeAttack1:
            case OUT_AITaskType.RangeAttack2:
            case OUT_AITaskType.MeleeAttack1:
            case OUT_AITaskType.MeleeAttack2:
                attackExecutor?.Execute(task.Type, blackboard);
                break;
        }
    }

    private bool UpdateTask(OUT_AITask task, OUT_AIBlackboard blackboard, ref OUT_AIConditionFlags conditions, OUT_AIState state)
    {
        switch (task.Type)
        {
            case OUT_AITaskType.StopMoving:
                return true;

            case OUT_AITaskType.Wait:
                return Time.time >= blackboard.WaitUntilTime;

            case OUT_AITaskType.FaceEnemy:
            case OUT_AITaskType.FacePoint:
                if (_locomotion is OUT_AILocomotion_CharacterController cc)
                    cc.ClearFace();
                return Time.time - _taskStartedTime >= 0.1f;

            case OUT_AITaskType.MoveToEnemyLKP:
            case OUT_AITaskType.MoveToPoint:
            case OUT_AITaskType.TakeCover:
                return UpdateMoveTask(ref conditions);

            case OUT_AITaskType.Reload:
            case OUT_AITaskType.RangeAttack1:
            case OUT_AITaskType.RangeAttack2:
            case OUT_AITaskType.MeleeAttack1:
            case OUT_AITaskType.MeleeAttack2:
                return true;

            default:
                return true;
        }
    }

    private bool UpdateMoveTask(ref OUT_AIConditionFlags conditions)
    {
        if (_locomotion is IOutAIStuckAwareLocomotion stuckAware && stuckAware.IsStuck)
        {
            AbortMoveTask(ref conditions);
            return false;
        }

        if (_routePlanner != null && _routePlanner.HasActiveRoute)
        {
            _locomotion?.TryMoveTo(_routePlanner.CurrentWaypoint, defaultMoveAcceptanceRadius);
            return false;
        }

        if (_locomotion == null)
            return true;

        return _locomotion.HasReachedDestination(defaultMoveAcceptanceRadius) || !_locomotion.IsMoving;
    }

    private void AbortMoveTask(ref OUT_AIConditionFlags conditions)
    {
        conditions |= OUT_AIConditionFlags.MoveFailed;
        _locomotion?.Stop();
        _routePlanner?.ClearRoute();

        if (logMoveFailures)
            OUT_AIDebugLogService.Log(this, OUT_AIDebugLogService.AIEventKind.Warning, $"move failed schedule:{(_currentSchedule != null ? _currentSchedule.Name : "<none>")} taskIndex:{_taskIndex}");

        _currentSchedule = null;
        _taskIndex = 0;
        _taskStarted = false;
    }

    private void LogRoute(string source, Vector3 destination, Vector3 firstWaypoint, bool success)
    {
        if (!logTasks)
            return;

        OUT_AIDebugLogService.Log(this, OUT_AIDebugLogService.AIEventKind.Route,
            $"{source} route {(success ? "ok" : "failed")} destination:{destination} first:{firstWaypoint}");
    }

    private Vector3 GetTargetPoint(Transform target)
    {
        Collider col = target.GetComponentInChildren<Collider>();
        if (col != null)
            return col.bounds.center;

        return target.position + Vector3.up;
    }

    private T FindInterface<T>() where T : class
    {
        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is T match)
                return match;
        }

        return null;
    }
}
