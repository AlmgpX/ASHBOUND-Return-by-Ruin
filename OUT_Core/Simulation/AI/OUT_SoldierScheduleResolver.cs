using UnityEngine;
using Time = OUT_SimTime;

[DisallowMultipleComponent]
public class OUT_SoldierScheduleResolver : OUT_AIScheduleResolver
{
    [Header("References")]
    [SerializeField] private OUT_SoldierSquadAgent squadAgent;
    [SerializeField] private OUT_WeaponController weapon;
    [SerializeField] private OUT_SoldierTacticalEvaluator tacticalEvaluator;
    [SerializeField] private MonoBehaviour routePlannerBehaviour;

    [Header("Timing")]
    [SerializeField][Min(0.05f)] private float firePauseMin = 0.08f;
    [SerializeField][Min(0.05f)] private float firePauseMax = 0.18f;
    [SerializeField][Min(0.05f)] private float suppressPause = 0.35f;
    [SerializeField][Min(0.05f)] private float regroupPause = 0.2f;

    [Header("Fallback Distances")]
    [SerializeField][Min(0.5f)] private float fallbackDistance = 8f;
    [SerializeField][Min(0.5f)] private float regroupDistanceTolerance = 4f;
    [SerializeField] private bool allowFallbackWhenCanShoot = false;
    [SerializeField] private bool takeCoverBeforeFireOnlyOnTakeCoverOrder = true;

    [Header("Signal / Interest Investigation")]
    [SerializeField][Range(0f, 1f)] private float minimumInterestStrengthToInvestigate = 0.15f;
    [SerializeField] private bool useSquadFormationForInterest = true;

    [Header("Idle Exploration")]
    [SerializeField] private bool exploreWhenIdle = true;
    [SerializeField][Min(0.25f)] private float idleExploreRadiusMin = 2f;
    [SerializeField][Min(0.25f)] private float idleExploreRadiusMax = 7f;
    [SerializeField][Min(0.05f)] private float idleLookPauseMin = 0.35f;
    [SerializeField][Min(0.05f)] private float idleLookPauseMax = 1.15f;
    [SerializeField][Range(0f, 1f)] private float idleMoveChance = 0.65f;
    [SerializeField][Min(1)] private int forceExploreAfterIdleResolves = 2;

    [Header("Debug")]
    [SerializeField] private bool logResolvedIntent = false;

    private IOutAIRoutePlanner _routePlanner;
    private int _stableSeed;
    private int _idleResolveCount;
    private Vector3 _homePoint;
    private bool _hasHomePoint;
    private OUT_AITacticalIntent _lastIntent;
    private string _lastIntentReason;

    public OUT_AITacticalIntent LastIntent { get { return _lastIntent; } }
    public string LastIntentReason { get { return _lastIntentReason; } }

    private void Awake()
    {
        if (squadAgent == null) squadAgent = GetComponent<OUT_SoldierSquadAgent>();
        if (weapon == null) weapon = GetComponent<OUT_WeaponController>();
        if (tacticalEvaluator == null) tacticalEvaluator = GetComponent<OUT_SoldierTacticalEvaluator>();

        _routePlanner = routePlannerBehaviour as IOutAIRoutePlanner;
        if (_routePlanner == null) _routePlanner = FindInterface<IOutAIRoutePlanner>();

        _stableSeed = BuildStableSeed();
        _homePoint = transform.position;
        _hasHomePoint = true;
    }

    private void OnEnable()
    {
        if (!_hasHomePoint)
        {
            _homePoint = transform.position;
            _hasHomePoint = true;
        }
    }

    public override OUT_AISchedule Resolve(OUT_AIState state, OUT_AIConditionFlags conditions, OUT_AIBlackboard blackboard)
    {
        if (blackboard == null)
            return BuildWait("SoldierNoBlackboard", 0.2f);

        Vector3 enemyPoint = blackboard.Enemy != null ? GetTargetPoint(blackboard.Enemy.transform) : blackboard.EnemyLastKnownPosition;

        bool canShootPrimary = (conditions & OUT_AIConditionFlags.CanRangeAttack1) != 0 && (conditions & OUT_AIConditionFlags.NoFriendlyFire) != 0;
        bool canShootSecondary = (conditions & OUT_AIConditionFlags.CanRangeAttack2) != 0 && (conditions & OUT_AIConditionFlags.NoFriendlyFire) != 0;
        bool forcedFallback = (conditions & OUT_AIConditionFlags.EnemyFacingMe) != 0 && squadAgent != null;
        bool shouldTakeCoverFirst = (conditions & OUT_AIConditionFlags.InCover) == 0 && ShouldTakeCoverFirst();

        Vector3 desiredPoint = GetRegroupPoint(blackboard, enemyPoint);
        bool needsReposition = blackboard.Enemy != null && Vector3.Distance(transform.position, desiredPoint) > regroupDistanceTolerance;

        OUT_AITacticalIntent intent;
        string reason;

        if (tacticalEvaluator != null)
        {
            intent = tacticalEvaluator.Evaluate(state, conditions, blackboard, canShootPrimary, canShootSecondary, forcedFallback, shouldTakeCoverFirst, needsReposition, out reason);
        }
        else
        {
            intent = EvaluateFallbackIntent(conditions, blackboard, canShootPrimary, canShootSecondary, forcedFallback, shouldTakeCoverFirst, needsReposition, out reason);
        }

        _lastIntent = intent;
        _lastIntentReason = reason;

        if (logResolvedIntent)
            OUT_AIDebugLogService.Log(this, OUT_AIDebugLogService.AIEventKind.Schedule, "resolve intent " + intent + " reason:" + reason);

        return BuildScheduleForIntent(intent, conditions, blackboard, enemyPoint, desiredPoint, canShootPrimary, canShootSecondary);
    }

    private OUT_AITacticalIntent EvaluateFallbackIntent(OUT_AIConditionFlags conditions, OUT_AIBlackboard blackboard, bool canShootPrimary, bool canShootSecondary, bool forcedFallback, bool shouldTakeCoverFirst, bool needsReposition, out string reason)
    {
        if ((conditions & OUT_AIConditionFlags.HeavyDamage) != 0)
        {
            reason = "legacy heavy damage";
            return OUT_AITacticalIntent.TakeCover;
        }

        if ((conditions & OUT_AIConditionFlags.NoAmmoLoaded) != 0 && weapon != null && weapon.Primary.CanReload)
        {
            reason = "legacy reload";
            return OUT_AITacticalIntent.ReloadSafe;
        }

        if (blackboard.Enemy == null)
        {
            if (blackboard.EnemyLastKnownPosition != Vector3.zero)
            {
                reason = "legacy hunt";
                return OUT_AITacticalIntent.Hunt;
            }

            if (blackboard.InterestStrength >= minimumInterestStrengthToInvestigate && blackboard.InterestPoint != Vector3.zero)
            {
                reason = "legacy investigate";
                return OUT_AITacticalIntent.Investigate;
            }

            reason = "legacy idle";
            return OUT_AITacticalIntent.Idle;
        }

        if (canShootSecondary)
        {
            reason = "legacy secondary";
            return OUT_AITacticalIntent.Suppress;
        }

        if (canShootPrimary)
        {
            if (shouldTakeCoverFirst)
            {
                reason = "legacy cover before fire";
                return OUT_AITacticalIntent.TakeCover;
            }

            reason = "legacy burst";
            return OUT_AITacticalIntent.BurstFire;
        }

        if (forcedFallback || (allowFallbackWhenCanShoot && forcedFallback))
        {
            reason = "legacy fallback";
            return OUT_AITacticalIntent.Fallback;
        }

        if (needsReposition)
        {
            reason = "legacy reposition";
            return OUT_AITacticalIntent.Reposition;
        }

        reason = "legacy wait";
        return OUT_AITacticalIntent.CombatWait;
    }

    private OUT_AISchedule BuildScheduleForIntent(OUT_AITacticalIntent intent, OUT_AIConditionFlags conditions, OUT_AIBlackboard blackboard, Vector3 enemyPoint, Vector3 desiredPoint, bool canShootPrimary, bool canShootSecondary)
    {
        switch (intent)
        {
            case OUT_AITacticalIntent.ReloadSafe:
                _idleResolveCount = 0;
                if (weapon != null && weapon.Primary.CanReload)
                {
                    return new OUT_AISchedule("SoldierReloadSafe", OUT_AIConditionFlags.HearDanger | OUT_AIConditionFlags.MoveFailed,
                        new OUT_AITask(OUT_AITaskType.StopMoving),
                        new OUT_AITask(OUT_AITaskType.Reload),
                        new OUT_AITask(OUT_AITaskType.Wait, weapon.Primary.ReloadDuration));
                }
                return BuildWait("SoldierReloadNoWeapon", 0.2f);

            case OUT_AITacticalIntent.TakeCover:
                _idleResolveCount = 0;
                return BuildTakeCoverOrFallback(enemyPoint, blackboard, "SoldierTakeCover");

            case OUT_AITacticalIntent.Fallback:
                _idleResolveCount = 0;
                return BuildFallback(enemyPoint, "SoldierFallback");

            case OUT_AITacticalIntent.Hunt:
                _idleResolveCount = 0;
                return BuildMoveTo("SoldierHunt", GetRegroupPoint(blackboard, enemyPoint), OUT_AIConditionFlags.SeeEnemy | OUT_AIConditionFlags.MoveFailed, regroupPause);

            case OUT_AITacticalIntent.Investigate:
                _idleResolveCount = 0;
                Vector3 investigatePoint = useSquadFormationForInterest ? GetRegroupPointAroundPoint(blackboard.InterestPoint) : blackboard.InterestPoint;
                blackboard.MoveTargetPoint = investigatePoint;
                return BuildMoveTo("SoldierInvestigateSignal", investigatePoint, OUT_AIConditionFlags.SeeEnemy | OUT_AIConditionFlags.HearCombat | OUT_AIConditionFlags.MoveFailed, regroupPause);

            case OUT_AITacticalIntent.Suppress:
                _idleResolveCount = 0;
                if (canShootSecondary)
                {
                    return new OUT_AISchedule("SoldierSuppressSecondary", OUT_AIConditionFlags.HeavyDamage | OUT_AIConditionFlags.MoveFailed,
                        new OUT_AITask(OUT_AITaskType.FaceEnemy),
                        new OUT_AITask(OUT_AITaskType.RangeAttack2),
                        new OUT_AITask(OUT_AITaskType.Wait, Random.Range(firePauseMin, firePauseMax) + suppressPause));
                }
                if (canShootPrimary)
                {
                    return new OUT_AISchedule("SoldierSuppressPrimary", OUT_AIConditionFlags.HeavyDamage | OUT_AIConditionFlags.MoveFailed | OUT_AIConditionFlags.HearDanger,
                        new OUT_AITask(OUT_AITaskType.FaceEnemy),
                        new OUT_AITask(OUT_AITaskType.RangeAttack1),
                        new OUT_AITask(OUT_AITaskType.Wait, Random.Range(firePauseMin, firePauseMax) + suppressPause));
                }
                return BuildMoveTo("SoldierSuppressReposition", desiredPoint, OUT_AIConditionFlags.SeeEnemy | OUT_AIConditionFlags.MoveFailed, regroupPause);

            case OUT_AITacticalIntent.BurstFire:
                _idleResolveCount = 0;
                if (canShootPrimary)
                {
                    return new OUT_AISchedule("SoldierBurstFire", OUT_AIConditionFlags.HeavyDamage | OUT_AIConditionFlags.MoveFailed | OUT_AIConditionFlags.HearDanger,
                        new OUT_AITask(OUT_AITaskType.FaceEnemy),
                        new OUT_AITask(OUT_AITaskType.RangeAttack1),
                        new OUT_AITask(OUT_AITaskType.Wait, Random.Range(firePauseMin, firePauseMax)));
                }
                return BuildWait("SoldierBurstNoShot", 0.12f);

            case OUT_AITacticalIntent.Reposition:
                _idleResolveCount = 0;
                return BuildMoveTo("SoldierReposition", desiredPoint, OUT_AIConditionFlags.SeeEnemy | OUT_AIConditionFlags.HearDanger | OUT_AIConditionFlags.MoveFailed, regroupPause);

            case OUT_AITacticalIntent.CombatWait:
                _idleResolveCount = 0;
                return new OUT_AISchedule("SoldierCombatWait", OUT_AIConditionFlags.SeeEnemy | OUT_AIConditionFlags.HearDanger | OUT_AIConditionFlags.MoveFailed,
                    new OUT_AITask(OUT_AITaskType.FaceEnemy),
                    new OUT_AITask(OUT_AITaskType.Wait, 0.12f));

            case OUT_AITacticalIntent.Idle:
            default:
                return BuildIdleExploreSchedule(blackboard);
        }
    }

    private OUT_AISchedule BuildWait(string name, float duration)
    {
        return new OUT_AISchedule(name, OUT_AIConditionFlags.SeeEnemy | OUT_AIConditionFlags.HearDanger | OUT_AIConditionFlags.HearCombat,
            new OUT_AITask(OUT_AITaskType.StopMoving),
            new OUT_AITask(OUT_AITaskType.Wait, duration));
    }

    private OUT_AISchedule BuildMoveTo(string name, Vector3 point, OUT_AIConditionFlags interrupts, float wait)
    {
        return new OUT_AISchedule(name, interrupts,
            new OUT_AITask(OUT_AITaskType.MoveToPoint, point),
            new OUT_AITask(OUT_AITaskType.FaceEnemy),
            new OUT_AITask(OUT_AITaskType.Wait, wait));
    }

    private OUT_AISchedule BuildFallback(Vector3 enemyPoint, string name)
    {
        Vector3 fallbackPoint = GetFallbackPoint(enemyPoint);
        return new OUT_AISchedule(name, OUT_AIConditionFlags.HeavyDamage | OUT_AIConditionFlags.MoveFailed | OUT_AIConditionFlags.CanRangeAttack1,
            new OUT_AITask(OUT_AITaskType.FaceEnemy),
            new OUT_AITask(OUT_AITaskType.MoveToPoint, fallbackPoint),
            new OUT_AITask(OUT_AITaskType.Wait, regroupPause));
    }

    private OUT_AISchedule BuildIdleExploreSchedule(OUT_AIBlackboard blackboard)
    {
        if (!exploreWhenIdle)
            return BuildWait("SoldierIdle", 0.2f);

        bool forceMove = _idleResolveCount >= Mathf.Max(1, forceExploreAfterIdleResolves);
        bool shouldMove = forceMove || GetHash01(Time.frameCount + _idleResolveCount * 17) <= idleMoveChance;

        if (shouldMove && TryPickIdleExplorePoint(out Vector3 point))
        {
            _idleResolveCount = 0;
            blackboard.MoveTargetPoint = point;
            return new OUT_AISchedule("SoldierExploreArea", OUT_AIConditionFlags.SeeEnemy | OUT_AIConditionFlags.HearDanger | OUT_AIConditionFlags.HearCombat | OUT_AIConditionFlags.MoveFailed,
                new OUT_AITask(OUT_AITaskType.MoveToPoint, point),
                new OUT_AITask(OUT_AITaskType.FacePoint, point + GetDeterministicLookDirection() * 3f),
                new OUT_AITask(OUT_AITaskType.Wait, Random.Range(idleLookPauseMin, idleLookPauseMax)));
        }

        _idleResolveCount++;
        Vector3 lookPoint = transform.position + GetDeterministicLookDirection() * 4f;
        return new OUT_AISchedule("SoldierScanArea", OUT_AIConditionFlags.SeeEnemy | OUT_AIConditionFlags.HearDanger | OUT_AIConditionFlags.HearCombat,
            new OUT_AITask(OUT_AITaskType.FacePoint, lookPoint),
            new OUT_AITask(OUT_AITaskType.Wait, Random.Range(idleLookPauseMin, idleLookPauseMax)));
    }

    private bool TryPickIdleExplorePoint(out Vector3 point)
    {
        Vector3 origin = _hasHomePoint ? _homePoint : transform.position;
        float minRadius = Mathf.Max(0.25f, idleExploreRadiusMin);
        float maxRadius = Mathf.Max(minRadius, idleExploreRadiusMax);

        for (int i = 0; i < 8; i++)
        {
            float angle = GetHash01(i + Time.frameCount + 101) * Mathf.PI * 2f;
            float radius = Mathf.Lerp(minRadius, maxRadius, GetHash01(i + Time.frameCount + 303));
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 candidate = origin + dir * radius;

            if (_routePlanner == null || _routePlanner.TryBuildRoute(new OUT_AIRouteRequest
                {
                    Destination = candidate,
                    ThreatPosition = Vector3.zero,
                    MinDistance = 0f,
                    MaxDistance = 0f,
                    RequireCover = false,
                    AllowTriangulation = true,
                    RefreshIfStale = true
                }, out _))
            {
                point = candidate;
                return true;
            }
        }

        point = transform.position;
        return false;
    }

    private Vector3 GetDeterministicLookDirection()
    {
        float angle = GetHash01(Time.frameCount + 701) * Mathf.PI * 2f;
        return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
    }

    private bool ShouldTakeCoverFirst()
    {
        if (squadAgent == null || squadAgent.Commander == null)
            return false;

        OUT_SoldierSquadCommander.SquadOrder order = squadAgent.Commander.CurrentOrder;
        if (takeCoverBeforeFireOnlyOnTakeCoverOrder)
            return order == OUT_SoldierSquadCommander.SquadOrder.TakeCover;

        return order == OUT_SoldierSquadCommander.SquadOrder.TakeCover || order == OUT_SoldierSquadCommander.SquadOrder.Suppress;
    }

    private OUT_AISchedule BuildTakeCoverOrFallback(Vector3 enemyPoint, OUT_AIBlackboard blackboard, string scheduleName)
    {
        if (_routePlanner != null && enemyPoint != Vector3.zero)
        {
            if (_routePlanner.TryFindCover(enemyPoint, 4f, 24f, out Vector3 coverPoint))
            {
                blackboard.CoverPoint = coverPoint;
                blackboard.MoveTargetPoint = coverPoint;
                return new OUT_AISchedule(scheduleName, OUT_AIConditionFlags.MoveFailed | OUT_AIConditionFlags.SeeEnemy,
                    new OUT_AITask(OUT_AITaskType.FaceEnemy),
                    new OUT_AITask(OUT_AITaskType.TakeCover),
                    new OUT_AITask(OUT_AITaskType.Wait, regroupPause));
            }
        }

        return BuildFallback(enemyPoint, scheduleName + "_Fallback");
    }

    private Vector3 GetRegroupPoint(OUT_AIBlackboard blackboard, Vector3 enemyPoint)
    {
        if (squadAgent != null)
            return squadAgent.GetDesiredCombatPoint(transform.position, enemyPoint);

        return blackboard.EnemyLastKnownPosition != Vector3.zero ? blackboard.EnemyLastKnownPosition : transform.position;
    }

    private Vector3 GetRegroupPointAroundPoint(Vector3 point)
    {
        if (squadAgent != null)
            return squadAgent.GetDesiredCombatPoint(transform.position, point);

        return point;
    }

    private Vector3 GetFallbackPoint(Vector3 enemyPoint)
    {
        if (enemyPoint == Vector3.zero)
            return transform.position - transform.forward * fallbackDistance;

        Vector3 away = transform.position - enemyPoint;
        away.y = 0f;
        if (away.sqrMagnitude < 0.001f)
            away = -transform.forward;

        return transform.position + away.normalized * fallbackDistance;
    }

    private Vector3 GetTargetPoint(Transform target)
    {
        if (target == null)
            return transform.position;

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
            if (behaviours[i] is T value)
                return value;
        }

        behaviours = GetComponentsInChildren<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is T value)
                return value;
        }

        return null;
    }

    private int BuildStableSeed()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + name.GetHashCode();
            hash = hash * 31 + Mathf.RoundToInt(transform.position.x * 10f);
            hash = hash * 31 + Mathf.RoundToInt(transform.position.z * 10f);
            return hash;
        }
    }

    private float GetHash01(int salt)
    {
        unchecked
        {
            uint x = (uint)(_stableSeed ^ salt);
            x ^= x >> 16;
            x *= 0x7feb352du;
            x ^= x >> 15;
            x *= 0x846ca68bu;
            x ^= x >> 16;
            return (x & 0x00FFFFFF) / 16777215f;
        }
    }
}
