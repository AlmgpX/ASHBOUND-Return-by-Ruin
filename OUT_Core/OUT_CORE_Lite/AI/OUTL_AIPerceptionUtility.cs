using System.Collections.Generic;
using UnityEngine;

public static class OUTL_AIPerceptionUtility
{
    private const int MaxTargetCandidates = 16;

    private static readonly RaycastHit[] sightHits = new RaycastHit[32];
    private static readonly List<OUTL_EntityRuntime> candidates = new List<OUTL_EntityRuntime>(MaxTargetCandidates * 2);

    public static bool CanSeeRuntime(Transform observer, OUTL_AIProfile profile, OUTL_EntityRuntime target, float eyeHeight, float targetEyeHeight, LayerMask sightBlockMask)
    {
        OUTL_EntityAdapter self = observer != null ? observer.GetComponentInParent<OUTL_EntityAdapter>() : null;
        return CanSeeRuntime(observer, self, profile, null, target, eyeHeight, targetEyeHeight, sightBlockMask);
    }

    public static bool CanSeeRuntime(Transform observer, OUTL_EntityAdapter self, OUTL_AIProfile profile, OUTL_EntityRuntime target, float eyeHeight, float targetEyeHeight, LayerMask sightBlockMask)
    {
        return CanSeeRuntime(observer, self, profile, null, target, eyeHeight, targetEyeHeight, sightBlockMask);
    }

    public static bool CanSeeRuntime(Transform observer, OUTL_EntityAdapter self, OUTL_AIProfile profile, OUTL_AIPerceptionProfile perception, OUTL_EntityRuntime target, float eyeHeight, float targetEyeHeight, LayerMask sightBlockMask)
    {
        using (OUTL_Profile.Perception.Auto())
        {
            if (observer == null || target == null || target.Adapter == null) return false;
            Vector3 eye = observer.position + Vector3.up * eyeHeight;
            Vector3 targetEye = target.Adapter.transform.position + Vector3.up * targetEyeHeight;
            Vector3 dir = targetEye - eye;
            float maxDistance = Mathf.Max(0.1f, perception != null && perception.SightDistance > 0f ? perception.SightDistance : (profile != null ? profile.ViewDistance : 30f));
            if (dir.sqrMagnitude > maxDistance * maxDistance) return false;
            float dist = dir.magnitude;
            if (dist <= 0.05f) return true;

            float cone = perception != null ? perception.SightConeAngle : 360f;
            if (cone > 0f && cone < 359f)
            {
                Vector3 flatDir = dir;
                flatDir.y = 0f;
                Vector3 forward = observer.forward;
                forward.y = 0f;
                if (flatDir.sqrMagnitude > 0.0001f && forward.sqrMagnitude > 0.0001f)
                {
                    float dot = Vector3.Dot(forward.normalized, flatDir.normalized);
                    float minDot = Mathf.Cos(Mathf.Clamp(cone, 1f, 360f) * 0.5f * Mathf.Deg2Rad);
                    if (dot < minDot) return false;
                }
            }

            OUTL_Profile.Frame.Raycasts++;
            LayerMask mask = perception != null ? perception.SightBlockMask : sightBlockMask;
            int count = Physics.RaycastNonAlloc(eye, dir / dist, sightHits, dist, mask, QueryTriggerInteraction.Ignore);
            if (count <= 0) return true;

            SortByDistance(sightHits, count);

            for (int i = 0; i < count; i++)
            {
                Collider hitCollider = sightHits[i].collider;
                if (hitCollider == null) continue;
                OUTL_EntityAdapter hitEntity = hitCollider.GetComponentInParent<OUTL_EntityAdapter>();

                if (self != null && hitEntity != null && hitEntity.Id == self.Id)
                    continue;

                if (hitEntity != null && hitEntity.Id == target.Id)
                    return true;

                return false;
            }

            return true;
        }
    }

    public static OUTL_EntityRuntime FindTarget(OUTL_World world, OUTL_EntityRuntime self, Transform observer, OUTL_AIProfile profile, string[] enemyTags, bool requireLineOfSight, float eyeHeight, float targetEyeHeight, LayerMask sightBlockMask)
    {
        return FindTarget(world, self, observer, profile, null, enemyTags, requireLineOfSight, eyeHeight, targetEyeHeight, sightBlockMask);
    }

    public static OUTL_EntityRuntime FindTarget(OUTL_World world, OUTL_EntityRuntime self, Transform observer, OUTL_AIProfile profile, OUTL_AIPerceptionProfile perception, string[] enemyTags, bool requireLineOfSight, float eyeHeight, float targetEyeHeight, LayerMask sightBlockMask)
    {
        using (OUTL_Profile.Perception.Auto())
        {
            if (world == null || self == null || observer == null || profile == null) return null;

            candidates.Clear();
            float viewDistance = Mathf.Max(0.1f, perception != null && perception.SightDistance > 0f ? perception.SightDistance : profile.ViewDistance);
            bool useFaction = perception == null || perception.UseFactionFilter;
            bool useTags = perception == null || perception.UseProfileEnemyTags;
            bool needsLineOfSight = perception != null ? perception.RequireLineOfSight : requireLineOfSight;

            if (useFaction && profile.UseFactionHostility && self.Faction != null)
                world.Sectors.CollectHostileCandidates(self, observer.position, viewDistance, candidates, MaxTargetCandidates);

            if (useTags && (candidates.Count == 0) && enemyTags != null && enemyTags.Length > 0)
                world.Sectors.CollectTagCandidates(observer.position, enemyTags, viewDistance, self, candidates, MaxTargetCandidates);

            if (candidates.Count == 0) return null;
            OUTL_EntityAdapter selfAdapter = self.Adapter;

            if (!needsLineOfSight)
            {
                for (int i = 0; i < candidates.Count; i++)
                    if (IsAliveCandidate(candidates[i]))
                        return candidates[i];
                return null;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                OUTL_EntityRuntime candidate = candidates[i];
                if (candidate == null || candidate.Adapter == null) continue;
                if (!IsAliveCandidate(candidate)) continue;
                if (CanSeeRuntime(observer, selfAdapter, profile, perception, candidate, eyeHeight, targetEyeHeight, sightBlockMask))
                    return candidate;
            }

            return null;
        }
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

    private static bool IsAliveCandidate(OUTL_EntityRuntime candidate)
    {
        if (candidate == null) return false;
        if (candidate.State.GetFlag(OUTL_StateId.Dead)) return false;
        return candidate.Stats.Get(OUTL_StatId.Health, 1f) > 0f;
    }
}
