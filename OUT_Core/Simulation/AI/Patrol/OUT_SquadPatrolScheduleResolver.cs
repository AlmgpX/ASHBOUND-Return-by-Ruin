using UnityEngine;

[DisallowMultipleComponent]
public class OUT_SquadPatrolScheduleResolver : OUT_AIScheduleResolver
{
    [Header("References")]
    [SerializeField] private OUT_SoldierSquadCommander commander;
    [SerializeField] private OUT_SoldierSquadAgent squadAgent;
    [SerializeField] private MonoBehaviour fallbackResolverBehaviour;

    [Header("Squad Patrol")]
    [SerializeField] private bool patrolWhenNoEnemy = true;
    [SerializeField][Min(0.1f)] private float slotReachDistance = 1.1f;
    [SerializeField][Min(0.1f)] private float slotRepathDistance = 1.6f;
    [SerializeField][Min(0.01f)] private float waitNearSlot = 0.18f;
    [SerializeField] private bool faceAnchorForward = true;
    [SerializeField][Min(0.5f)] private float forwardProbeDistance = 6f;

    private OUT_AIScheduleResolver fallbackResolver;

    private void Awake()
    {
        if (squadAgent == null)
            squadAgent = GetComponent<OUT_SoldierSquadAgent>();

        if (commander == null && squadAgent != null)
            commander = squadAgent.Commander;

        if (commander == null)
            commander = GetComponentInParent<OUT_SoldierSquadCommander>();

        fallbackResolver = fallbackResolverBehaviour as OUT_AIScheduleResolver;
        if (fallbackResolver == null)
        {
            OUT_AIScheduleResolver[] all = GetComponents<OUT_AIScheduleResolver>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i] != this && !(all[i] is OUT_AIPatrolScheduleResolver))
                {
                    fallbackResolver = all[i];
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

        if (!patrolWhenNoEnemy || hasEnemy || hasMemory || hasStimulus || isDamaged || commander == null)
            return ResolveFallback(state, conditions, blackboard);

        Vector3 desired = GetDesiredSlotPoint();
        if (blackboard != null)
            blackboard.MoveTargetPoint = desired;

        float distance = Vector3.Distance(GetPlanar(transform.position), GetPlanar(desired));
        OUT_AIConditionFlags interrupts = OUT_AIConditionFlags.SeeEnemy | OUT_AIConditionFlags.HearDanger | OUT_AIConditionFlags.HearCombat | OUT_AIConditionFlags.HearPlayer | OUT_AIConditionFlags.LightDamage | OUT_AIConditionFlags.HeavyDamage | OUT_AIConditionFlags.MoveFailed;

        if (distance > slotRepathDistance)
        {
            if (faceAnchorForward)
            {
                return new OUT_AISchedule("SquadPatrolMoveToSlot", interrupts,
                    new OUT_AITask(OUT_AITaskType.MoveToPoint, desired),
                    new OUT_AITask(OUT_AITaskType.FacePoint, GetLookPoint()),
                    new OUT_AITask(OUT_AITaskType.Wait, waitNearSlot));
            }

            return new OUT_AISchedule("SquadPatrolMoveToSlot", interrupts,
                new OUT_AITask(OUT_AITaskType.MoveToPoint, desired),
                new OUT_AITask(OUT_AITaskType.Wait, waitNearSlot));
        }

        if (faceAnchorForward)
        {
            return new OUT_AISchedule("SquadPatrolHoldSlot", interrupts,
                new OUT_AITask(OUT_AITaskType.FacePoint, GetLookPoint()),
                new OUT_AITask(OUT_AITaskType.Wait, waitNearSlot));
        }

        return new OUT_AISchedule("SquadPatrolHoldSlot", interrupts,
            new OUT_AITask(OUT_AITaskType.Wait, waitNearSlot));
    }

    private Vector3 GetDesiredSlotPoint()
    {
        Transform anchor = commander.SquadAnchor;
        Vector3 anchorForwardPoint = anchor != null
            ? anchor.position + anchor.forward * forwardProbeDistance
            : transform.position + transform.forward * forwardProbeDistance;

        return commander.GetSlotWorldPoint(squadAgent, transform.position, anchorForwardPoint);
    }

    private Vector3 GetLookPoint()
    {
        Transform anchor = commander != null ? commander.SquadAnchor : null;
        if (anchor == null)
            return transform.position + transform.forward * forwardProbeDistance;

        return anchor.position + anchor.forward * forwardProbeDistance;
    }

    private OUT_AISchedule ResolveFallback(OUT_AIState state, OUT_AIConditionFlags conditions, OUT_AIBlackboard blackboard)
    {
        if (fallbackResolver != null)
            return fallbackResolver.Resolve(state, conditions, blackboard);

        return new OUT_AISchedule("SquadPatrolNoFallback", OUT_AIConditionFlags.SeeEnemy | OUT_AIConditionFlags.HearDanger | OUT_AIConditionFlags.HearCombat | OUT_AIConditionFlags.HearPlayer,
            new OUT_AITask(OUT_AITaskType.StopMoving),
            new OUT_AITask(OUT_AITaskType.Wait, 0.25f));
    }

    private Vector3 GetPlanar(Vector3 value)
    {
        value.y = 0f;
        return value;
    }
}
