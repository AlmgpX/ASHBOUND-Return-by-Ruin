using System;

[Serializable]
public struct OUTL_CoverReservation
{
    public OUTL_EntityId EntityId;
    public float UntilTime;
    public string Reason;

    public OUTL_CoverReservation(OUTL_EntityId entityId, float untilTime, string reason)
    {
        EntityId = entityId;
        UntilTime = untilTime;
        Reason = reason;
    }

    public bool IsActive(float time)
    {
        return EntityId.IsValid && time <= UntilTime;
    }
}
