using UnityEngine;

public enum OUTL_FriendlyFireResult : byte
{
    Clear = 0,
    Risky = 1,
    BlockedByFriendly = 2,
    FriendlyHitLikely = 3
}

public struct OUTL_FriendlyFireEvaluation
{
    public OUTL_FriendlyFireResult Result;
    public OUTL_EntityId Friendly;
    public float Risk;
    public Vector3 HitPoint;
    public string Reason;

    public bool BlocksFire
    {
        get { return Result == OUTL_FriendlyFireResult.BlockedByFriendly || Result == OUTL_FriendlyFireResult.FriendlyHitLikely; }
    }
}

public static class OUTL_FriendlyFireEvaluator
{
    private static readonly RaycastHit[] rayHits = new RaycastHit[32];
    private static readonly Collider[] overlapHits = new Collider[48];

    public static OUTL_FriendlyFireEvaluation Evaluate(OUTL_World world, OUTL_EntityAdapter source, OUTL_EntityRuntime target, Vector3 origin, Vector3 aimPoint, float radius, LayerMask mask, OUTL_FactionDisciplineProfile discipline)
    {
        OUTL_FriendlyFireEvaluation evaluation = default(OUTL_FriendlyFireEvaluation);
        evaluation.Result = OUTL_FriendlyFireResult.Clear;
        evaluation.Reason = "clear";
        if (world == null || source == null || source.Runtime == null) return evaluation;

        Vector3 delta = aimPoint - origin;
        float distance = delta.magnitude;
        if (distance <= 0.05f) return evaluation;
        Vector3 dir = delta / distance;

        int hitCount = Physics.RaycastNonAlloc(origin, dir, rayHits, distance, mask, QueryTriggerInteraction.Ignore);
        SortByDistance(rayHits, hitCount);
        for (int i = 0; i < hitCount; i++)
        {
            Collider c = rayHits[i].collider;
            if (c == null) continue;
            OUTL_EntityAdapter hitEntity;
            if (!OUTL_Combat.TryGetEntityFromCollider(c, out hitEntity)) continue;
            if (hitEntity == source) continue;
            if (target != null && hitEntity.Id == target.Id) break;
            if (IsFriendly(world, source.Runtime, hitEntity.Runtime))
            {
                evaluation.Result = OUTL_FriendlyFireResult.BlockedByFriendly;
                evaluation.Friendly = hitEntity.Id;
                evaluation.Risk = 1f;
                evaluation.HitPoint = rayHits[i].point;
                evaluation.Reason = "friendly_in_line";
                ClearRayHits(hitCount);
                return evaluation;
            }
        }
        ClearRayHits(hitCount);

        float probeRadius = Mathf.Max(0.05f, radius);
        int overlapCount = Physics.OverlapSphereNonAlloc(aimPoint, probeRadius, overlapHits, mask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider c = overlapHits[i];
            overlapHits[i] = null;
            OUTL_EntityAdapter entity;
            if (!OUTL_Combat.TryGetEntityFromCollider(c, out entity)) continue;
            if (entity == source) continue;
            if (target != null && entity.Id == target.Id) continue;
            if (!IsFriendly(world, source.Runtime, entity.Runtime)) continue;

            float tolerance = discipline != null ? discipline.FriendlyFireTolerance : 0.05f;
            evaluation.Result = tolerance <= 0.1f ? OUTL_FriendlyFireResult.FriendlyHitLikely : OUTL_FriendlyFireResult.Risky;
            evaluation.Friendly = entity.Id;
            evaluation.Risk = Mathf.Clamp01(1f - tolerance);
            evaluation.HitPoint = entity.transform.position;
            evaluation.Reason = "friendly_near_impact";
            return evaluation;
        }

        return evaluation;
    }

    private static bool IsFriendly(OUTL_World world, OUTL_EntityRuntime source, OUTL_EntityRuntime other)
    {
        if (source == null || other == null || source == other) return false;
        return world.Factions.AreFriendly(source, other);
    }

    private static void SortByDistance(RaycastHit[] hits, int count)
    {
        for (int i = 1; i < count; i++)
        {
            RaycastHit key = hits[i];
            int j = i - 1;
            while (j >= 0 && hits[j].distance > key.distance)
            {
                hits[j + 1] = hits[j];
                j--;
            }
            hits[j + 1] = key;
        }
    }

    private static void ClearRayHits(int count)
    {
        for (int i = 0; i < count && i < rayHits.Length; i++)
            rayHits[i] = default(RaycastHit);
    }
}
