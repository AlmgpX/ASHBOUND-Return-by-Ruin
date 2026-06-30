public static class OUT_GameRulesService
{
    private static readonly IOutGameRules _defaultRules = new OUT_DefaultGameRules();
    private static IOutGameRules _currentRules = _defaultRules;

    public static IOutGameRules Current => _currentRules ?? _defaultRules;

    public static void SetCurrent(IOutGameRules rules)
    {
        _currentRules = rules ?? _defaultRules;
    }

    public static void ResetToDefault()
    {
        _currentRules = _defaultRules;
    }

    public static bool CanTakeDamage(
        IOutActor target,
        in OUT_DamageContext damageContext,
        in OUT_GameRuleContext ruleContext)
    {
        return Current.CanTakeDamage(target, damageContext, ruleContext);
    }

    public static OUT_PickupDecision EvaluatePickup(
        in OUT_GameRuleContext ruleContext,
        int requestedAmount = 0)
    {
        return Current.EvaluatePickup(ruleContext, requestedAmount);
    }

    public static OUT_UseDecision EvaluateUse(
        IOutUsable usable,
        in OUT_UseRequest useRequest,
        in OUT_GameRuleContext ruleContext)
    {
        return Current.EvaluateUse(usable, useRequest, ruleContext);
    }

    public static OUT_RespawnPolicy ResolveRespawnPolicy(
        in OUT_GameRuleContext ruleContext)
    {
        return Current.ResolveRespawnPolicy(ruleContext);
    }
}