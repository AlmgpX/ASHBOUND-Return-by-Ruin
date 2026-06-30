public readonly struct OUT_UseDecision
{
    public readonly bool Allowed;
    public readonly bool AllowContinuousUse;
    public readonly bool ConsumeInput;
    public readonly string Reason;

    public OUT_UseDecision(
        bool allowed,
        bool allowContinuousUse,
        bool consumeInput,
        string reason = "")
    {
        Allowed = allowed;
        AllowContinuousUse = allowContinuousUse;
        ConsumeInput = consumeInput;
        Reason = reason;
    }

    public static OUT_UseDecision Deny(string reason = "")
    {
        return new OUT_UseDecision(false, false, false, reason);
    }

    public static OUT_UseDecision Allow(bool allowContinuousUse = false, bool consumeInput = true, string reason = "")
    {
        return new OUT_UseDecision(true, allowContinuousUse, consumeInput, reason);
    }
}