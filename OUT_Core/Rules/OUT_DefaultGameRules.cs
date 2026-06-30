public sealed class OUT_DefaultGameRules : IOutGameRules
{
    public bool CanTakeDamage(
        IOutActor target,
        in OUT_DamageContext damageContext,
        in OUT_GameRuleContext ruleContext)
    {
        if (target == null)
            return false;

        if (!target.IsAlive)
            return false;

        if (damageContext.DamageAmount <= 0)
            return false;

        return true;
    }

    public OUT_PickupDecision EvaluatePickup(
        in OUT_GameRuleContext ruleContext,
        int requestedAmount = 0)
    {
        if (!ruleContext.HasTargetObject)
            return OUT_PickupDecision.Deny("No target object");

        if (requestedAmount < 0)
            return OUT_PickupDecision.Deny("Negative pickup amount");

        if (requestedAmount == 0)
            return OUT_PickupDecision.Allow(0, true, "Pickup allowed");

        return OUT_PickupDecision.Allow(requestedAmount, true, "Pickup allowed");
    }

    public OUT_UseDecision EvaluateUse(
        IOutUsable usable,
        in OUT_UseRequest useRequest,
        in OUT_GameRuleContext ruleContext)
    {
        if (usable == null)
            return OUT_UseDecision.Deny("No usable target");

        if (!usable.CanUse(useRequest))
            return OUT_UseDecision.Deny("Usable rejected request");

        bool allowContinuousUse =
            (usable.UseCaps & OUT_UseCapabilityFlags.ContinuousUse) != 0 ||
            usable is IOutContinuousUsable;

        return OUT_UseDecision.Allow(
            allowContinuousUse: allowContinuousUse,
            consumeInput: true,
            reason: "Use allowed");
    }

    public OUT_RespawnPolicy ResolveRespawnPolicy(
        in OUT_GameRuleContext ruleContext)
    {
        return OUT_RespawnPolicy.None();
    }
}