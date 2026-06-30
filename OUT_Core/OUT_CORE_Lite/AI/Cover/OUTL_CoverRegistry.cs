using System.Collections.Generic;
using UnityEngine;

public static class OUTL_CoverRegistry
{
    private static readonly List<OUTL_CoverPoint> points = new List<OUTL_CoverPoint>(256);
    private static readonly Dictionary<int, List<OUTL_CoverPoint>> bySector = new Dictionary<int, List<OUTL_CoverPoint>>(64);
    private static readonly Dictionary<OUTL_CoverPoint, int> pointSector = new Dictionary<OUTL_CoverPoint, int>(256);
    private static readonly int[] querySectors = new int[9];

    public static int Count { get { return points.Count; } }
    public static int SectorBucketCount { get { return bySector.Count; } }
    public static int LastQueryTouchedBuckets { get; private set; }
    public static int LastQueryTouchedPoints { get; private set; }

    public static void Register(OUTL_CoverPoint point)
    {
        if (point == null) return;
        if (!points.Contains(point)) points.Add(point);
        RegisterSector(point, ResolveSectorId(point));
    }

    public static void Unregister(OUTL_CoverPoint point)
    {
        points.Remove(point);
        UnregisterSector(point);
    }

    public static void RebuildAll()
    {
        bySector.Clear();
        pointSector.Clear();
        for (int i = points.Count - 1; i >= 0; i--)
        {
            OUTL_CoverPoint point = points[i];
            if (point == null)
            {
                points.RemoveAt(i);
                continue;
            }
            RegisterSector(point, ResolveSectorId(point));
        }
    }

    public static int QueryNonAlloc(in OUTL_CoverQuery query, OUTL_CoverQueryResult[] results)
    {
        if (results == null || results.Length == 0 || query.Seeker == null) return 0;
        LastQueryTouchedBuckets = 0;
        LastQueryTouchedPoints = 0;
        int written = 0;
        float radius = Mathf.Max(0.1f, query.SearchRadius);
        float radiusSqr = radius * radius;
        float time = query.Time > 0f ? query.Time : (OUTL_World.Instance != null ? OUTL_World.Instance.WorldTime : Time.time);
        int sectorCount = BuildQuerySectors(query);
        if (sectorCount > 0)
        {
            for (int i = 0; i < sectorCount; i++)
            {
                List<OUTL_CoverPoint> bucket;
                if (!bySector.TryGetValue(querySectors[i], out bucket) || bucket == null) continue;
                LastQueryTouchedBuckets++;
                EvaluateList(bucket, query, radiusSqr, time, results, ref written);
            }
            if (written > 0) return written;
        }

        LastQueryTouchedBuckets = 0;
        for (int i = points.Count - 1; i >= 0; i--)
        {
            OUTL_CoverPoint point = points[i];
            if (point == null)
            {
                points.RemoveAt(i);
                continue;
            }

            EvaluatePoint(point, query, radiusSqr, time, results, ref written);
        }

        return written;
    }

    public static bool TryReserveBest(in OUTL_CoverQuery query, float duration, string reason, out OUTL_CoverQueryResult result)
    {
        OUTL_CoverQueryResult[] buffer = SharedBuffer;
        int count = QueryNonAlloc(query, buffer);
        for (int i = 0; i < count; i++)
        {
            OUTL_CoverPoint point = buffer[i].Point;
            if (point != null && point.Reserve(query.Seeker, duration, reason))
            {
                result = buffer[i];
                Clear(buffer, count);
                return true;
            }
        }
        Clear(buffer, count);
        result = default(OUTL_CoverQueryResult);
        return false;
    }

    public static bool BlocksThreat(Vector3 threatPosition, Vector3 coverPoint, LayerMask visibilityMask)
    {
        Vector3 from = threatPosition + Vector3.up * 1.45f;
        Vector3 to = coverPoint;
        Vector3 dir = to - from;
        if (dir.sqrMagnitude <= 0.01f) return false;
        OUTL_Profile.Frame.Raycasts++;
        return Physics.Raycast(from, dir.normalized, dir.magnitude, visibilityMask, QueryTriggerInteraction.Ignore);
    }

    private static float Score(OUTL_CoverPoint point, in OUTL_CoverQuery query, float distSqr, float time)
    {
        float dist = Mathf.Sqrt(distSqr);
        float threatDist = Vector3.Distance(query.ThreatPosition, point.StandPoint);
        float reservedBonus = point.Reservation.IsActive(time) && point.Reservation.EntityId == query.Seeker.Id ? 8f : 0f;
        float exposure = Mathf.Max(0.05f, point.ExposureWeight);
        float danger = Mathf.Max(0.05f, point.DangerWeight);
        return threatDist * 0.35f * danger - dist * exposure + reservedBonus;
    }

    private static void EvaluateList(List<OUTL_CoverPoint> list, in OUTL_CoverQuery query, float radiusSqr, float time, OUTL_CoverQueryResult[] results, ref int written)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            OUTL_CoverPoint point = list[i];
            if (point == null)
            {
                list.RemoveAt(i);
                continue;
            }
            EvaluatePoint(point, query, radiusSqr, time, results, ref written);
        }
    }

    private static void EvaluatePoint(OUTL_CoverPoint point, in OUTL_CoverQuery query, float radiusSqr, float time, OUTL_CoverQueryResult[] results, ref int written)
    {
        LastQueryTouchedPoints++;
        if (point == null || !point.Active || !point.IsFreeFor(query.Seeker)) return;
        if (!point.AllowsRole(query.WeaponRole)) return;
        Vector3 stand = point.StandPoint;
        float distSqr = (stand - query.SeekerPosition).sqrMagnitude;
        if (distSqr > radiusSqr) return;
        if (query.RequireBlocksThreat && !BlocksThreat(query.ThreatPosition, point.PeekPoint, query.VisibilityMask)) return;

        OUTL_CoverQueryResult candidate = new OUTL_CoverQueryResult
        {
            Point = point,
            StandPoint = stand,
            PeekPoint = point.PeekPoint,
            Score = Score(point, query, distSqr, time)
        };
        InsertSorted(results, ref written, candidate);
    }

    private static int BuildQuerySectors(in OUTL_CoverQuery query)
    {
        OUTL_World world = OUTL_World.Instance;
        int count = 0;
        if (query.SectorId != 0)
            querySectors[count++] = query.SectorId;

        if (world == null)
        {
            return count;
        }

        Vector2Int center = world.Sectors.WorldToCell(query.SeekerPosition);
        for (int z = -1; z <= 1; z++)
        {
            for (int x = -1; x <= 1; x++)
            {
                int id = world.Sectors.CellToId(new Vector2Int(center.x + x, center.y + z));
                if (!ContainsSector(querySectors, count, id) && count < querySectors.Length)
                    querySectors[count++] = id;
            }
        }
        return count;
    }

    private static bool ContainsSector(int[] sectors, int count, int sectorId)
    {
        for (int i = 0; i < count; i++)
            if (sectors[i] == sectorId)
                return true;
        return false;
    }

    public static int ResolveSectorId(OUTL_CoverPoint point)
    {
        if (point == null) return 0;
        if (point.SectorId != 0) return point.SectorId;
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return 0;
        int id = world.Sectors.CellToId(world.Sectors.WorldToCell(point.transform.position));
        point.SectorId = id;
        return id;
    }

    private static void RegisterSector(OUTL_CoverPoint point, int sectorId)
    {
        if (point == null) return;
        UnregisterSector(point);
        List<OUTL_CoverPoint> bucket;
        if (!bySector.TryGetValue(sectorId, out bucket) || bucket == null)
        {
            bucket = new List<OUTL_CoverPoint>(16);
            bySector[sectorId] = bucket;
        }
        if (!bucket.Contains(point)) bucket.Add(point);
        pointSector[point] = sectorId;
    }

    private static void UnregisterSector(OUTL_CoverPoint point)
    {
        if (point == null) return;
        int sectorId;
        if (!pointSector.TryGetValue(point, out sectorId)) return;
        pointSector.Remove(point);
        List<OUTL_CoverPoint> bucket;
        if (bySector.TryGetValue(sectorId, out bucket) && bucket != null)
        {
            bucket.Remove(point);
            if (bucket.Count == 0) bySector.Remove(sectorId);
        }
    }

    private static void InsertSorted(OUTL_CoverQueryResult[] results, ref int written, OUTL_CoverQueryResult candidate)
    {
        int limit = results.Length;
        int index = Mathf.Min(written, limit - 1);
        if (written >= limit && candidate.Score <= results[limit - 1].Score) return;
        if (written < limit) written++;
        while (index > 0 && candidate.Score > results[index - 1].Score)
        {
            results[index] = results[index - 1];
            index--;
        }
        results[index] = candidate;
    }

    private static void Clear(OUTL_CoverQueryResult[] results, int count)
    {
        for (int i = 0; i < count && i < results.Length; i++)
            results[i] = default(OUTL_CoverQueryResult);
    }

    private static OUTL_CoverQueryResult[] SharedBuffer
    {
        get { return sharedBuffer; }
    }

    private static readonly OUTL_CoverQueryResult[] sharedBuffer = new OUTL_CoverQueryResult[16];
}
