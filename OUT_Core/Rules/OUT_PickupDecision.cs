public readonly struct OUT_PickupDecision
{
    public readonly bool Allowed;
    public readonly int GrantedAmount;
    public readonly bool ConsumePickup;
    public readonly string Reason;

    public OUT_PickupDecision(
        bool allowed,
        int grantedAmount,
        bool consumePickup,
        string reason = "")
    {
        Allowed = allowed;
        GrantedAmount = grantedAmount;
        ConsumePickup = consumePickup;
        Reason = reason;
    }

    public static OUT_PickupDecision Deny(string reason = "")
    {
        return new OUT_PickupDecision(false, 0, false, reason);
    }

    public static OUT_PickupDecision Allow(int grantedAmount, bool consumePickup = true, string reason = "")
    {
        return new OUT_PickupDecision(true, grantedAmount, consumePickup, reason);
    }
}