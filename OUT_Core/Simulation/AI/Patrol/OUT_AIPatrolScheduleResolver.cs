using UnityEngine;

[DisallowMultipleComponent]
public class OUT_AIPatrolScheduleResolver : OUT_AIScheduleResolver
{
    [Header("References")]
    [SerializeField] private MonoBehaviour fallbackResolverBehaviour;
    [SerializeField] private MonoBehaviour patrolProviderBehaviour;
    [SerializeField] private OUT_SoldierSquadAgent squadAgent;
    [SerializeField] private OUT_SoldierSquadCommander squadCommander;

    [Header("Patrol")]
    [SerializeField] private bool patrolWhenNoEnemy = true;
    [SerializeField][Min(0.1f)] private float reachDistance = 1.1f;
    [SerializeField] private bool facePointBeforeMove = false;

    [Header("Squad Anchor Patrol")]
    [SerializeField] private bool useSquadSlotsWhenProviderIsCommander = true;
    [SerializeField][Min(0.1f)] private float slotRepathDistance = 1.6f;
    [SerializeField][Min(0.01f)] private float waitNearSlot = 0.18f;
    [SerializeField][Min(0.5f)] private float forwardProbeDistance = 6f;

    private OUT_AIScheduleResolver fallbackResolver;
    private IOutAIPatrolProvider patrolProvider;
    private bool wasInterrupted;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (squadAgent == null)
            squadAgent = GetComponent<OUT_SoldierSquadAgent>();

        fallbackResolver = fallbackResolverBehaviour as OUT_AIScheduleResolver;
        if (fallbackResolver == null)
        {
            OUT_AIScheduleResolver[] all = GetComponents<OUT_AIScheduleResolver>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i] != this)
                {
                    fallbackResolver = all[i];
                    break;
                }
            }
        }

        patrolProvider = patrolProviderBehaviour as IOutAIPatrolProvider;

        if (squadCommander == null)
            squadCommander = patrolProviderBehaviour as OUT_SoldierSquadCommander;

        if (squadCommander == null && squadAgent != null)
            squadCommander = squadAgent.Commander;

        if (squadCommander == null)
            squadCommander = GetComponentInParent<OUT_SoldierSquadCommander>();

        if (patrolProvider == null)
        {
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IOutAIPatrolProvider provider)
                {
                    patrolProvider = provider;
                    break;
                }
            }
        }
    }

    public override OUT_AISchedule Resolve(OUT_AIState state, OUT_AIConditionFlags conditions, OUT_AIBlackboard blackboard)
    {
        bool hasEnemy = blackboard != null && blackboard.Enemy != null;
        bool hasMemory = blackboard != null && blackboard.EnemyLastKnownPosition != Vector3.zero;
        bool hasStimulus = (conditions & (OUT_AIConditionFlags.HearDanger | OUT_AIConditionFlags.HearCombat | OUT_AIConditionFlags.HearPlayer | OUT_AIConditionFlags.SeeEnemy)) != 0;
        bool isDamaged = (conditions & (OUT_AIConditionFlags.LightDamage | OUT_AIConditionFlags.HeavyDamage)) != 0;

        if (hasEnemy || hasMemory || hasStimulus || isDamaged)
        {
            if (patrolProvider != null && !wasInterrupted)
            {
                patrolProvider.NotifyPatrolInterrupted();
                wasInterrupted = true;
            }

            return ResolveFallback(state, conditions, blackboard);
        }

        if (!patrolWhenNoEnemy)
            return ResolveFallback(state, conditions, blackboard);

        if (useSquadSlotsWhenProviderIsCommander && squadCommander != null)
            return BuildSquadSlotSchedule(blackboard);

        if (patrolProvider != null && patrolProvider.HasPatrol)
        {
            OUT_AISchedule schedule = BuildPatrolSchedule(blackboard);
            if (schedule != null)
                return schedule;
        }

        return ResolveFallback(state, conditions, blackboard);
    }

    private OUT_AISchedule BuildSquadSlotSchedule(OUT_AIBlackboard blackboard)
    {
        if (wasInterrupted)
            wasInterrupted = false;

        Transform anchor = squadCommander.SquadAnchor;
        Vector3 lookPoint = anchor != null
            ? anchor.position + anchor.forward * forwardProbeDistance
            : transform.position + transform.forward * forwardProbeDistance;

        Vector3 point = squadCommander.GetSlotWorldPoint(squadAgent, transform.position, lookPoint);
        if (blackboard != null)
            blackboard.MoveTargetPoint = point;

        OUT_AIConditionFlags interrupts = OUT_AIConditionFlags.SeeEnemy | OUT_AIConditionFlags.HearDanger | OUT_AIConditionFlags.HearCombat | OUT_AIConditionFlags.HearPlayer | OUT_AIConditionFlags.LightDamage | OUT_AIConditionFlags.HeavyDamage | OUT_AIConditionFlags.MoveFailed;
        float distance = Vector3.Distance(GetPlanar(transform.position), GetPlanar(point));

        if (distance > slotRepathDistance)
        {
            if (facePointBeforeMove)
            {
                return new OUT_AISchedule("SquadPatrolMoveToSlot", interrupts,
                    new OUT_AITask(OUT_AITaskType.FacePoint, lookPoint),
                    new OUT_AITask(OUT_AITaskType.MoveToPoint, point),
                    new OUT_AITask(OUT_AITaskType.Wait, waitNearSlot));
            }

            return new OUT_AISchedule("SquadPatrolMoveToSlot", interrupts,
                new OUT_AITask(OUT_AITaskType.MoveToPoint, point),
                new OUT_AITask(OUT_AITaskType.FacePoint, lookPoint),
                new OUT_AITask(OUT_AITaskType.Wait, waitNearSlot));
        }

        return new OUT_AISchedule("SquadPatrolHoldSlot", interrupts,
            new OUT_AITask(OUT_AITaskType.FacePoint, lookPoint),
            new OUT_AITask(OUT_AITaskType.Wait, waitNearSlot));
    }

    private OUT_AISchedule BuildPatrolSchedule(OUT_AIBlackboard blackboard)
    {
        if (wasInterrupted)
        {
            if (patrolProvider is OUT_AIPatrolRoute route)
                route.ForceNearest(transform.position);

            patrolProvider.NotifyPatrolResumed();
            wasInterrupted = false;
        }

        if (!patrolProvider.TryGetCurrentPatrolPoint(transform.position, out Vector3 point, out float waitTime))
            return null;

        if (Vector3.Distance(transform.position, point) <= reachDistance)
        {
            patrolProvider.NotifyPatrolPointReached(transform.position);
            if (!patrolProvider.TryGetCurrentPatrolPoint(transform.position, out point, out waitTime))
                return null;
        }

        if (blackboard != null)
            blackboard.MoveTargetPoint = point;

        OUT_AIConditionFlags interrupts = OUT_AIConditionFlags.SeeEnemy | OUT_AIConditionFlags.HearDanger | OUT_AIConditionFlags.HearCombat | OUT_AIConditionFlags.HearPlayer | OUT_AIConditionFlags.LightDamage | OUT_AIConditionFlags.HeavyDamage | OUT_AIConditionFlags.MoveFailed;
        string name = patrolProvider.IsReturningToPatrol ? "ReturnToPatrol" : "Patrol";

        if (facePointBeforeMove)
        {
            return new OUT_AISchedule(name, interrupts,
                new OUT_AITask(OUT_AITaskType.FacePoint, point),
                new OUT_AITask(OUT_AITaskType.MoveToPoint, point),
                new OUT_AITask(OUT_AITaskType.Wait, Mathf.Max(0.01f, waitTime)));
        }

        return new OUT_AISchedule(name, interrupts,
            new OUT_AITask(OUT_AITaskType.MoveToPoint, point),
            new OUT_AITask(OUT_AITaskType.Wait, Mathf.Max(0.01f, waitTime)));
    }

    private OUT_AISchedule ResolveFallback(OUT_AIState state, OUT_AIConditionFlags conditions, OUT_AIBlackboard blackboard)
    {
        if (fallbackResolver != null)
            return fallbackResolver.Resolve(state, conditions, blackboard);

        return new OUT_AISchedule("IdleNoFallbackResolver", OUT_AIConditionFlags.SeeEnemy | OUT_AIConditionFlags.HearDanger | OUT_AIConditionFlags.HearCombat | OUT_AIConditionFlags.HearPlayer,
            new OUT_AITask(OUT_AITaskType.StopMoving),
            new OUT_AITask(OUT_AITaskType.Wait, 0.25f));
    }

    private Vector3 GetPlanar(Vector3 value)
    {
        value.y = 0f;
        return value;
    }
}
