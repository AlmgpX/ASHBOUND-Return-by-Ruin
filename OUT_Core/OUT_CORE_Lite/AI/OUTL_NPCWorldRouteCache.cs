using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class OUTL_NPCRouteRecord
{
    public string Key;
    public int StartSector;
    public int EndSector;
    public Vector3 Start;
    public Vector3 End;
    public float Distance;
    public float EstimatedTravelTime;
    public float LastUsedTime;
    public int UsageCount;
    public readonly List<Vector3> RoutePoints = new List<Vector3>(4);
    public readonly List<int> SectorPath = new List<int>(8);
}

[Serializable]
public sealed class OUTL_NPCWorldRouteCache
{
    public int MaxRoutes = 128;
    public int WorldVersion;
    public int TotalRouteRequests;
    public int TotalRouteHits;
    public int TotalRouteCreated;

    private readonly List<OUTL_NPCRouteRecord> routes = new List<OUTL_NPCRouteRecord>(128);

    public int Count { get { return routes.Count; } }

    public OUTL_NPCRouteRecord GetOrCreateRoute(OUTL_World world, Vector3 start, Vector3 end, OUTL_NPCNavigationProfile profile, string routeKey, float time)
    {
        int startSector = BuildSectorId(world, start);
        int endSector = BuildSectorId(world, end);
        string key = BuildKey(startSector, endSector, profile, routeKey);
        TotalRouteRequests++;
        OUTL_NPCRouteRecord route = Find(key);
        if (route != null)
        {
            TotalRouteHits++;
            route.LastUsedTime = time;
            route.UsageCount++;
            return route;
        }

        route = new OUTL_NPCRouteRecord();
        route.Key = key;
        route.StartSector = startSector;
        route.EndSector = endSector;
        route.Start = start;
        route.End = end;
        route.Distance = Vector3.Distance(start, end);
        float speed = profile != null ? Mathf.Max(0.01f, profile.WalkSpeed * profile.AbstractTravelSpeedMultiplier) : 2.2f;
        route.EstimatedTravelTime = route.Distance / speed;
        route.LastUsedTime = time;
        route.UsageCount = 1;
        BuildSectorPath(world, start, end, profile, route, time);
        Add(route);
        TotalRouteCreated++;
        return route;
    }

    public static string BuildKey(int startSector, int endSector, OUTL_NPCNavigationProfile profile, string routeKey)
    {
        string profileId = profile != null && !string.IsNullOrEmpty(profile.ProfileId) ? profile.ProfileId : "default";
        string semantic = string.IsNullOrEmpty(routeKey) ? "route" : routeKey;
        return startSector + ">" + endSector + ":" + profileId + ":" + semantic;
    }

    private OUTL_NPCRouteRecord Find(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        for (int i = 0; i < routes.Count; i++)
            if (routes[i] != null && routes[i].Key == key)
                return routes[i];
        return null;
    }

    private void Add(OUTL_NPCRouteRecord route)
    {
        if (route == null) return;
        if (routes.Count >= Mathf.Max(1, MaxRoutes)) RemoveLeastRecentlyUsed();
        routes.Add(route);
    }

    private void RemoveLeastRecentlyUsed()
    {
        if (routes.Count == 0) return;
        int index = 0;
        float oldest = routes[0] != null ? routes[0].LastUsedTime : float.MaxValue;
        for (int i = 1; i < routes.Count; i++)
        {
            OUTL_NPCRouteRecord r = routes[i];
            float t = r != null ? r.LastUsedTime : float.MaxValue;
            if (t < oldest)
            {
                oldest = t;
                index = i;
            }
        }
        routes.RemoveAt(index);
    }

    private static int BuildSectorId(OUTL_World world, Vector3 position)
    {
        if (world == null) return Mathf.FloorToInt(position.x) ^ (Mathf.FloorToInt(position.z) << 8);
        return world.Sectors.CellToId(world.Sectors.WorldToCell(position));
    }

    private static void BuildSectorPath(OUTL_World world, Vector3 start, Vector3 end, OUTL_NPCNavigationProfile profile, OUTL_NPCRouteRecord route, float time)
    {
        if (route == null) return;
        route.RoutePoints.Clear();
        route.SectorPath.Clear();
        route.RoutePoints.Add(start);

        if (world == null)
        {
            route.SectorPath.Add(route.StartSector);
            if (route.EndSector != route.StartSector) route.SectorPath.Add(route.EndSector);
            route.RoutePoints.Add(end);
            return;
        }

        float cellSize = world.WorldLedger != null ? world.WorldLedger.ActivityCellSize : 64f;
        OUTL_WorldCellKey routeStart = OUTL_WorldCellKey.FromWorldPosition(start, cellSize, OUTL_WorldCellLayer.ActivityCell);
        OUTL_WorldCellKey routeEnd = OUTL_WorldCellKey.FromWorldPosition(end, cellSize, OUTL_WorldCellLayer.ActivityCell);
        OUTL_RouteCacheKey key = new OUTL_RouteCacheKey
        {
            Start = routeStart,
            End = routeEnd,
            MovementProfileHash = OUTL_WorldCellUtility.StableStringHash(profile != null ? profile.ProfileId : "npc_nav"),
            WorldVersion = world.WorldLedger != null ? world.WorldLedger.Version : 0,
            Flags = 0
        };
        OUTL_RouteResult worldRoute = world.WorldRouteCache.GetOrCreateStraightRoute(world.WorldRouteGraph, key, null, time);
        if (worldRoute != null && worldRoute.Cells.Count > 0 && worldRoute.Status != OUTL_RouteStatus.Blocked)
        {
            int routedLastSector = int.MinValue;
            for (int i = 0; i < worldRoute.Cells.Count; i++)
            {
                OUTL_WorldCellKey cell = worldRoute.Cells[i];
                Vector3 point = new Vector3((cell.X + 0.5f) * cellSize, start.y, (cell.Z + 0.5f) * cellSize);
                if (i == 0) point = start;
                if (i == worldRoute.Cells.Count - 1) point = end;
                int sector = world.Sectors.CellToId(world.Sectors.WorldToCell(point));
                if (sector != routedLastSector)
                {
                    route.SectorPath.Add(sector);
                    routedLastSector = sector;
                }
                if (i > 0 && i < worldRoute.Cells.Count - 1) route.RoutePoints.Add(point);
            }
            route.RoutePoints.Add(end);
            route.Distance = worldRoute.TotalCost > 0f ? worldRoute.TotalCost : Vector3.Distance(start, end);
            float speed = profile != null ? Mathf.Max(0.01f, profile.WalkSpeed * profile.AbstractTravelSpeedMultiplier) : 2.2f;
            route.EstimatedTravelTime = route.Distance / speed;
            return;
        }

        Vector2Int a = world.Sectors.WorldToCell(start);
        Vector2Int b = world.Sectors.WorldToCell(end);
        int steps = Mathf.Max(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
        int lastSector = int.MinValue;
        for (int i = 0; i <= steps; i++)
        {
            float t = steps <= 0 ? 1f : i / (float)steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(a.x, b.x, t));
            int z = Mathf.RoundToInt(Mathf.Lerp(a.y, b.y, t));
            int sector = world.Sectors.CellToId(new Vector2Int(x, z));
            if (sector == lastSector) continue;
            route.SectorPath.Add(sector);
            lastSector = sector;
            if (i > 0 && i < steps)
                route.RoutePoints.Add(Vector3.Lerp(start, end, t));
        }

        route.RoutePoints.Add(end);
    }
}
