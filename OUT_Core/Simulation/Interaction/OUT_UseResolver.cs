using System.Collections.Generic;
using UnityEngine;

public static class OUT_UseResolver
{
    public static bool TryResolveBestCandidate(
        IEnumerable<IOutUsable> candidates,
        in OUT_UseRequest request,
        out IOutUsable bestCandidate)
    {
        bestCandidate = null;

        if (candidates == null)
            return false;

        float bestScore = float.MinValue;

        foreach (IOutUsable candidate in candidates)
        {
            if (candidate == null)
                continue;

            if (!candidate.CanUse(request))
                continue;

            float score = 0f;

            if (candidate is IOutActor actor && actor.ActorTransform != null)
            {
                Vector3 toTarget = actor.ActorTransform.position - request.Origin;
                float distance = toTarget.magnitude;

                if (distance > 0.001f)
                {
                    Vector3 dir = toTarget / distance;
                    float dot = Vector3.Dot(request.Direction.normalized, dir);

                    score += dot * 10f;
                    score -= distance;
                }
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestCandidate = candidate;
            }
        }

        return bestCandidate != null;
    }

    public static OUT_UseResult TryUseBestCandidate(
        IEnumerable<IOutUsable> candidates,
        in OUT_UseRequest request,
        out IOutUsable usedCandidate)
    {
        usedCandidate = null;

        if (!TryResolveBestCandidate(candidates, request, out IOutUsable best))
            return OUT_UseResult.Failed("No usable candidate");

        usedCandidate = best;
        return best.Use(request);
    }
}