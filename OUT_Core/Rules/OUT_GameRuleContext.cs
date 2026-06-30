using UnityEngine;

public readonly struct OUT_GameRuleContext
{
    public readonly GameObject InstigatorObject;
    public readonly GameObject TargetObject;
    public readonly OUT_EntityId TargetEntityId;
    public readonly string RuleSetId;
    public readonly string SceneName;

    public OUT_GameRuleContext(
        GameObject instigatorObject,
        GameObject targetObject,
        OUT_EntityId targetEntityId,
        string ruleSetId,
        string sceneName)
    {
        InstigatorObject = instigatorObject;
        TargetObject = targetObject;
        TargetEntityId = targetEntityId;
        RuleSetId = ruleSetId;
        SceneName = sceneName;
    }

    public bool HasTargetObject => TargetObject != null;
    public bool HasInstigatorObject => InstigatorObject != null;
}