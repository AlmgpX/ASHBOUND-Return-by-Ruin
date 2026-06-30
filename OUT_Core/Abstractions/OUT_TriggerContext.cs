using UnityEngine;

public readonly struct OUT_TriggerContext
{
    public readonly GameObject Sender;
    public readonly GameObject Instigator;
    public readonly Vector3 Origin;
    public readonly Vector3 Direction;
    public readonly string TriggerName;
    public readonly float TriggerValue;

    public OUT_TriggerContext(
        GameObject sender,
        GameObject instigator,
        Vector3 origin,
        Vector3 direction,
        string triggerName = "",
        float triggerValue = 1f)
    {
        Sender = sender;
        Instigator = instigator;
        Origin = origin;
        Direction = direction;
        TriggerName = triggerName;
        TriggerValue = triggerValue;
    }
}
