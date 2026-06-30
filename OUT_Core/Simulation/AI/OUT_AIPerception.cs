using UnityEngine;
using Time = OUT_SimTime;

[DisallowMultipleComponent]
public class OUT_AIPerception : MonoBehaviour
{
    [Header("Vision")]
    [SerializeField] private Transform eyePoint;
    [SerializeField] private float sightDistance = 35f;
    [SerializeField] private float fieldOfView = 120f;
    [SerializeField] private LayerMask targetMask = ~0;
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
    [SerializeField] private bool requireDamageableTarget = false;
    [SerializeField] private bool requireHostileFaction = false;

    [Header("Optimization")]
    [SerializeField] [Min(8)] private int overlapBufferSize = 64;
    [SerializeField] private bool warnWhenOverlapBufferFull = false;
    [SerializeField] private bool throttleChecks = true;
    [SerializeField] [Range(0.02f, 1f)] private float checkInterval = 0.2f;
    [SerializeField] private bool randomizeInitialCheckOffset = true;
    [SerializeField] [Min(1f)] private float maxCheckDistanceMultiplier = 1.25f;

    [Header("Timing")]
    [SerializeField] private float forgetEnemyAfter = 4f;

    private Collider[] _overlapBuffer;
    private float _sightDistanceSqr;
    private float _maxCheckDistanceSqr;
    private float _halfFovCos;
    private float _cachedSightDistance = -1f;
    private float _cachedMaxCheckDistanceMultiplier = -1f;
    private float _cachedFieldOfView = -1f;
    private int _cachedBufferSize = -1;
    private float _nextCheckTime;
    private bool _hasEvaluatedOnce;
    private Transform _ownEntityRoot;
    private OUT_FactionAgent _ownFaction;

    private void Awake()
    {
        _ownEntityRoot = ResolveEntityRoot(transform);
        _ownFaction = GetComponentInParent<OUT_FactionAgent>();
        RefreshCachesIfNeeded(force: true);
        _nextCheckTime = randomizeInitialCheckOffset ? Time.time + Random.Range(0f, Mathf.Max(0.02f, checkInterval)) : Time.time;
    }

    private void OnEnable()
    {
        _hasEvaluatedOnce = false;
        _ownEntityRoot = ResolveEntityRoot(transform);
        _ownFaction = GetComponentInParent<OUT_FactionAgent>();
        _nextCheckTime = randomizeInitialCheckOffset ? Time.time + Random.Range(0f, Mathf.Max(0.02f, checkInterval)) : Time.time;
    }

    private void OnValidate()
    {
        sightDistance = Mathf.Max(0.01f, sightDistance);
        fieldOfView = Mathf.Clamp(fieldOfView, 0.01f, 360f);
        overlapBufferSize = Mathf.Max(8, overlapBufferSize);
        checkInterval = Mathf.Max(0.02f, checkInterval);
        maxCheckDistanceMultiplier = Mathf.Max(1f, maxCheckDistanceMultiplier);
    }

    public void Evaluate(OUT_AIBlackboard blackboard, ref OUT_AIConditionFlags conditions)
    {
        RefreshCachesIfNeeded(force: false);

        if (throttleChecks && _hasEvaluatedOnce && Time.time < _nextCheckTime)
        {
            PreserveMemoryFlags(blackboard, ref conditions);
            return;
        }

        _hasEvaluatedOnce = true;
        _nextCheckTime = Time.time + Mathf.Max(0.02f, checkInterval);

        conditions &= ~(OUT_AIConditionFlags.SeeEnemy | OUT_AIConditionFlags.NewEnemy | OUT_AIConditionFlags.EnemyOccluded | OUT_AIConditionFlags.EnemyTooFar | OUT_AIConditionFlags.HasEnemyLKP);

        Vector3 eye = GetEyePosition();
        GameObject previousEnemy = blackboard.Enemy;
        GameObject bestEnemy = null;
        Vector3 bestPoint = Vector3.zero;
        float bestSqrDistance = float.MaxValue;

        int hitCount = Physics.OverlapSphereNonAlloc(eye, sightDistance, _overlapBuffer, targetMask, triggerInteraction);
        if (warnWhenOverlapBufferFull && hitCount >= _overlapBuffer.Length)
            Debug.LogWarning($"{nameof(OUT_AIPerception)} on {name}: overlap buffer is full ({_overlapBuffer.Length}). Increase Overlap Buffer Size.", this);

        Vector3 forward = transform.forward;

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = _overlapBuffer[i];
            if (col == null)
                continue;

            Transform entity = ResolveEntityRoot(col.transform);
            if (entity == null || entity == _ownEntityRoot || entity == transform || entity.IsChildOf(transform))
                continue;

            if (!IsValidTargetEntity(entity))
                continue;

            Vector3 targetPoint = GetTargetPoint(col);
            Vector3 toTarget = targetPoint - eye;
            float sqrDistance = toTarget.sqrMagnitude;

            if (sqrDistance > _maxCheckDistanceSqr)
                continue;

            if (sqrDistance > _sightDistanceSqr)
            {
                conditions |= OUT_AIConditionFlags.EnemyTooFar;
                continue;
            }

            float magnitude = Mathf.Sqrt(sqrDistance);
            if (magnitude <= 0.001f)
                continue;

            Vector3 direction = toTarget / magnitude;
            if (Vector3.Dot(forward, direction) < _halfFovCos)
                continue;

            if (!HasLineOfSight(eye, targetPoint, entity, direction, magnitude))
                continue;

            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                bestEnemy = entity.gameObject;
                bestPoint = targetPoint;
            }
        }

        if (bestEnemy != null)
        {
            blackboard.Enemy = bestEnemy;
            blackboard.EnemyLastKnownPosition = bestPoint;
            blackboard.LastEnemySeenTime = Time.time;
            conditions |= OUT_AIConditionFlags.SeeEnemy | OUT_AIConditionFlags.HasEnemyLKP;
            if (previousEnemy != bestEnemy)
                conditions |= OUT_AIConditionFlags.NewEnemy;
        }
        else
        {
            if (blackboard.Enemy != null)
            {
                blackboard.EnemyLastKnownPosition = GetTargetPoint(blackboard.Enemy.transform);
                blackboard.LastEnemySeenTime = Time.time;
                blackboard.Enemy = null;
                conditions |= OUT_AIConditionFlags.EnemyOccluded | OUT_AIConditionFlags.HasEnemyLKP;
            }
            else if (blackboard.EnemyLastKnownPosition != Vector3.zero && Time.time - blackboard.LastEnemySeenTime <= forgetEnemyAfter)
            {
                conditions |= OUT_AIConditionFlags.HasEnemyLKP;
            }
            else
            {
                blackboard.ClearEnemy();
            }
        }
    }

    private bool IsValidTargetEntity(Transform entity)
    {
        if (requireDamageableTarget && entity.GetComponentInChildren<IOutDamageable>() == null)
            return false;

        OUT_FactionAgent targetFaction = OUT_FactionAgent.FindOn(entity);
        if (_ownFaction != null && targetFaction != null)
            return _ownFaction.GetRelationTo(targetFaction) == OUT_FactionAgent.Relation.Hostile;

        return !requireHostileFaction;
    }

    private void PreserveMemoryFlags(OUT_AIBlackboard blackboard, ref OUT_AIConditionFlags conditions)
    {
        conditions &= ~(OUT_AIConditionFlags.NewEnemy | OUT_AIConditionFlags.EnemyTooFar);
        if (blackboard == null)
            return;

        if (blackboard.Enemy != null)
            conditions |= OUT_AIConditionFlags.SeeEnemy;

        if (blackboard.Enemy != null || (blackboard.EnemyLastKnownPosition != Vector3.zero && Time.time - blackboard.LastEnemySeenTime <= forgetEnemyAfter))
            conditions |= OUT_AIConditionFlags.HasEnemyLKP;
        else
            conditions &= ~OUT_AIConditionFlags.HasEnemyLKP;
    }

    private void RefreshCachesIfNeeded(bool force)
    {
        int desiredBufferSize = Mathf.Max(8, overlapBufferSize);
        if (force || _overlapBuffer == null || _cachedBufferSize != desiredBufferSize)
        {
            _overlapBuffer = new Collider[desiredBufferSize];
            _cachedBufferSize = desiredBufferSize;
        }

        if (force || !Mathf.Approximately(_cachedSightDistance, sightDistance) || !Mathf.Approximately(_cachedMaxCheckDistanceMultiplier, maxCheckDistanceMultiplier))
        {
            _cachedSightDistance = sightDistance;
            _cachedMaxCheckDistanceMultiplier = maxCheckDistanceMultiplier;
            _sightDistanceSqr = sightDistance * sightDistance;
            float maxCheckDistance = sightDistance * Mathf.Max(1f, maxCheckDistanceMultiplier);
            _maxCheckDistanceSqr = maxCheckDistance * maxCheckDistance;
        }

        if (force || !Mathf.Approximately(_cachedFieldOfView, fieldOfView))
        {
            _cachedFieldOfView = fieldOfView;
            float halfFov = Mathf.Clamp(fieldOfView * 0.5f, 0.005f, 180f);
            _halfFovCos = Mathf.Cos(halfFov * Mathf.Deg2Rad);
        }
    }

    private Vector3 GetEyePosition() => eyePoint != null ? eyePoint.position : transform.position + Vector3.up * 1.5f;
    private Vector3 GetTargetPoint(Collider col) => col != null ? col.bounds.center : transform.position + Vector3.up;

    private Vector3 GetTargetPoint(Transform target)
    {
        Collider col = target.GetComponentInChildren<Collider>();
        return col != null ? col.bounds.center : target.position + Vector3.up;
    }

    private bool HasLineOfSight(Vector3 eye, Vector3 targetPoint, Transform expectedEntity, Vector3 direction, float distance)
    {
        if (distance <= 0.001f)
            return true;

        if (Physics.Raycast(eye, direction, out RaycastHit hit, distance, obstacleMask, triggerInteraction))
            return ResolveEntityRoot(hit.transform) == expectedEntity;

        return true;
    }

    public static Transform ResolveEntityRoot(Transform source)
    {
        if (source == null)
            return null;

        OUT_FactionAgent faction = source.GetComponentInParent<OUT_FactionAgent>();
        if (faction != null)
            return faction.transform;

        OUT_AIActorBrain brain = source.GetComponentInParent<OUT_AIActorBrain>();
        if (brain != null)
            return brain.transform;

        OUT_HealthSimple health = source.GetComponentInParent<OUT_HealthSimple>();
        if (health != null)
            return health.transform;

        CharacterController controller = source.GetComponentInParent<CharacterController>();
        if (controller != null)
            return controller.transform;

        return source.root;
    }
}
