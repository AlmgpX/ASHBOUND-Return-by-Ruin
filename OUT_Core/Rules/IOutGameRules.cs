public interface IOutGameRules
{
    bool CanTakeDamage(
        IOutActor target,
        in OUT_DamageContext damageContext,
        in OUT_GameRuleContext ruleContext);

    OUT_PickupDecision EvaluatePickup(
        in OUT_GameRuleContext ruleContext,
        int requestedAmount = 0);

    OUT_UseDecision EvaluateUse(
        IOutUsable usable,
        in OUT_UseRequest useRequest,
        in OUT_GameRuleContext ruleContext);

    OUT_RespawnPolicy ResolveRespawnPolicy(
        in OUT_GameRuleContext ruleContext);
}