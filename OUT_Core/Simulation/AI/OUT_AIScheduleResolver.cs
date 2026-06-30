using UnityEngine;

public abstract class OUT_AIScheduleResolver : MonoBehaviour
{
    public abstract OUT_AISchedule Resolve(
        OUT_AIState state,
        OUT_AIConditionFlags conditions,
        OUT_AIBlackboard blackboard);
}