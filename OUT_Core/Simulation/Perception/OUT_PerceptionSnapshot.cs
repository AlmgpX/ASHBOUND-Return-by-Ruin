using UnityEngine;

public readonly struct OUT_PerceptionSnapshot
{
    public readonly GameObject TargetObject;
    public readonly Vector3 TargetOrigin;
    public readonly float Distance;
    public readonly bool IsVisible;
    public readonly bool IsAudible;
    public readonly OUT_RelationshipKind Relationship;

    public OUT_PerceptionSnapshot(
        GameObject targetObject,
        Vector3 targetOrigin,
        float distance,
        bool isVisible,
        bool isAudible,
        OUT_RelationshipKind relationship)
    {
        TargetObject = targetObject;
        TargetOrigin = targetOrigin;
        Distance = distance;
        IsVisible = isVisible;
        IsAudible = isAudible;
        Relationship = relationship;
    }
}