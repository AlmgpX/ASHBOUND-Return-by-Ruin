using UnityEngine;

public readonly struct OUT_ScheduleContext
{
    public readonly GameObject ActorObject;
    public readonly OUT_ConditionState Conditions;
    public readonly OUT_MemoryState Memory;
    public readonly GameObject CurrentTarget;

    public OUT_ScheduleContext(
        GameObject actorObject,
        OUT_ConditionState conditions,
        OUT_MemoryState memory,
        GameObject currentTarget)
    {
        ActorObject = actorObject;
        Conditions = conditions;
        Memory = memory;
        CurrentTarget = currentTarget;
    }
}