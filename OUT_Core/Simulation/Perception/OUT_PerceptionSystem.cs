using System.Collections.Generic;
using UnityEngine;

public static class OUT_PerceptionSystem
{
    public static bool TryFindBestTarget(
        IOutFactionMember selfFaction,
        Vector3 observerOrigin,
        Vector3 observerForward,
        IEnumerable<IOutPerceptionTarget> candidates,
        IOutRelationshipResolver relationshipResolver,
        float maxDistance,
        float minDot,
        out OUT_PerceptionSnapshot bestSnapshot)
    {
        bestSnapshot = default;
        bool found = false;
        float bestScore = float.MinValue;

        if (candidates == null)
            return false;

        Vector3 normalizedForward = observerForward.sqrMagnitude > 0.0001f
            ? observerForward.normalized
            : Vector3.forward;

        foreach (IOutPerceptionTarget candidate in candidates)
        {
            if (candidate == null || !candidate.CanBePerceived)
                continue;

            if (!(candidate is IOutActor actor))
                continue;

            Vector3 targetOrigin = candidate.PerceptionOrigin;
            Vector3 toTarget = targetOrigin - observerOrigin;
            float distance = toTarget.magnitude;

            if (distance > maxDistance)
                continue;

            Vector3 direction = distance > 0.001f ? toTarget / distance : Vector3.forward;
            float dot = Vector3.Dot(normalizedForward, direction);

            bool isVisible = dot >= minDot;
            bool isAudible = candidate.NoiseRadius > 0f && distance <= candidate.NoiseRadius;

            if (!isVisible && !isAudible)
                continue;

            OUT_RelationshipKind relationship = OUT_RelationshipKind.Neutral;

            if (relationshipResolver != null &&
                selfFaction != null &&
                candidate is IOutFactionMember targetFaction)
            {
                relationship = relationshipResolver.Resolve(selfFaction, targetFaction);
            }

            float score = 0f;
            score += isVisible ? 100f : 0f;
            score += isAudible ? 25f : 0f;
            score += dot * 10f;
            score -= distance;

            if (!found || score > bestScore)
            {
                bestScore = score;
                found = true;
                bestSnapshot = new OUT_PerceptionSnapshot(
                    actor.ActorObject,
                    targetOrigin,
                    distance,
                    isVisible,
                    isAudible,
                    relationship
                );
            }
        }

        return found;
    }
}