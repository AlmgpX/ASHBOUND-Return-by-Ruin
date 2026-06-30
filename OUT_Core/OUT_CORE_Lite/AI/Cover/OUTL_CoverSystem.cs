using UnityEngine;

public static class OUTL_CoverSystem
{
    private static readonly OUTL_CoverQueryResult[] queryBuffer = new OUTL_CoverQueryResult[8];

    public static OUTL_CoverPoint FindBestCover(OUTL_EntityAdapter seeker, Vector3 threatPosition, float searchRadius, LayerMask visibilityMask)
    {
        if (seeker == null) return null;
        OUTL_CoverQuery query = new OUTL_CoverQuery
        {
            Seeker = seeker,
            SeekerPosition = seeker.transform.position,
            ThreatPosition = threatPosition,
            SearchRadius = searchRadius,
            WeaponRole = OUTL_WeaponRole.Any,
            VisibilityMask = visibilityMask,
            RequireBlocksThreat = true,
            Time = OUTL_World.Instance != null ? OUTL_World.Instance.WorldTime : Time.time
        };

        int count = OUTL_CoverRegistry.QueryNonAlloc(query, queryBuffer);
        OUTL_CoverPoint best = count > 0 ? queryBuffer[0].Point : null;
        if (best != null) best.Reserve(seeker, 4f, "legacy_cover_system");
        for (int i = 0; i < count && i < queryBuffer.Length; i++) queryBuffer[i] = default(OUTL_CoverQueryResult);
        return best;
    }

    public static bool BlocksThreat(Vector3 threatPosition, Vector3 coverPoint, LayerMask visibilityMask)
    {
        return OUTL_CoverRegistry.BlocksThreat(threatPosition, coverPoint, visibilityMask);
    }
}
