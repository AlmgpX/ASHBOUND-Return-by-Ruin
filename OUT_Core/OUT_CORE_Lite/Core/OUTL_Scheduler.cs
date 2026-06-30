using System.Collections.Generic;
using UnityEngine;

public sealed class OUTL_Scheduler
{
    private const int LaneCount = 6;

    private OUTL_World world;
    private readonly List<Entry>[] lanes = new List<Entry>[LaneCount];
    private readonly List<RandomEntry> randomTickables = new List<RandomEntry>(1024);
    private readonly Dictionary<OUTL_ITickable, IndexRef> indices = new Dictionary<OUTL_ITickable, IndexRef>(1024);
    private readonly Dictionary<OUTL_IRandomTickable, int> randomIndices = new Dictionary<OUTL_IRandomTickable, int>(1024);
    private int randomCursor;
    private int tickableCount;

    public int TickableCount { get { return tickableCount; } }
    public int RandomTickableCount { get { return randomTickables.Count; } }

    private struct Entry
    {
        public OUTL_ITickable Tickable;
        public float NextTime;
        public OUTL_TickLane Lane;
    }

    private struct RandomEntry
    {
        public OUTL_IRandomTickable Tickable;
        public float NextTime;
    }

    private struct IndexRef
    {
        public OUTL_TickLane Lane;
        public int Index;
    }

    public OUTL_Scheduler()
    {
        for (int i = 0; i < lanes.Length; i++) lanes[i] = new List<Entry>(256);
    }

    public void Bind(OUTL_World world) { this.world = world; }

    public void Register(OUTL_ITickable tickable)
    {
        if (tickable == null) return;
        OUTL_TickLane lane = SanitizeLane(tickable.OUTL_TickLane);

        IndexRef existing;
        if (indices.TryGetValue(tickable, out existing))
        {
            if (existing.Lane == lane) return;
            RemoveAt(existing.Lane, existing.Index);
        }

        float interval = Mathf.Max(0.01f, tickable.OUTL_TickInterval);
        Entry entry = new Entry
        {
            Tickable = tickable,
            Lane = lane,
            NextTime = world != null ? world.WorldTime + StablePhase01(tickable, (int)lane) * interval : 0f
        };

        List<Entry> list = lanes[(int)lane];
        indices[tickable] = new IndexRef { Lane = lane, Index = list.Count };
        list.Add(entry);
        tickableCount++;
    }

    public void Unregister(OUTL_ITickable tickable)
    {
        IndexRef index;
        if (tickable == null || !indices.TryGetValue(tickable, out index)) return;
        RemoveAt(index.Lane, index.Index);
    }

    public void RegisterRandom(OUTL_IRandomTickable tickable)
    {
        if (tickable == null || randomIndices.ContainsKey(tickable)) return;
        float interval = GetRandomInterval(tickable);
        RandomEntry entry = new RandomEntry
        {
            Tickable = tickable,
            NextTime = world != null ? world.WorldTime + StablePhase01(tickable, 97) * interval : 0f
        };
        randomIndices[tickable] = randomTickables.Count;
        randomTickables.Add(entry);
    }

    public void UnregisterRandom(OUTL_IRandomTickable tickable)
    {
        int index;
        if (tickable == null || !randomIndices.TryGetValue(tickable, out index)) return;
        RemoveRandomAt(index);
    }

    public void TickLane(OUTL_TickLane lane, float time, float deltaTime)
    {
        using (OUTL_Profile.SchedulerTickLane.Auto())
        {
            lane = SanitizeLane(lane);
            List<Entry> list = lanes[(int)lane];
            for (int i = 0; i < list.Count; i++)
            {
                Entry entry = list[i];
                OUTL_ITickable tickable = entry.Tickable;
                if (tickable == null)
                {
                    RemoveAt(lane, i);
                    i--;
                    continue;
                }

                OUTL_TickLane currentLane = SanitizeLane(tickable.OUTL_TickLane);
                if (currentLane != lane)
                {
                    RemoveAt(lane, i);
                    Register(tickable);
                    i--;
                    continue;
                }

                if (!tickable.OUTL_IsTickEnabled) continue;
                if (time < entry.NextTime) continue;

                entry.NextTime = time + Mathf.Max(0.01f, tickable.OUTL_TickInterval);
                list[i] = entry;
                OUTL_Profile.AddTick(lane);
                tickable.OUTL_Tick(world, time, deltaTime);

                IndexRef after;
                if (!indices.TryGetValue(tickable, out after) || after.Lane != lane || after.Index != i)
                {
                    i--;
                    continue;
                }
            }
        }
    }

    public void RandomTick(float time, int budget)
    {
        using (OUTL_Profile.SchedulerRandomTick.Auto())
        {
            if (budget <= 0 || randomTickables.Count == 0) return;
            int count = Mathf.Min(budget, randomTickables.Count);
            for (int i = 0; i < count && randomTickables.Count > 0; i++)
            {
                if (randomCursor >= randomTickables.Count) randomCursor = 0;
                int index = randomCursor++;
                RandomEntry entry = randomTickables[index];
                OUTL_IRandomTickable tickable = entry.Tickable;
                if (tickable == null)
                {
                    RemoveRandomAt(index);
                    randomCursor = index;
                    continue;
                }

                if (time < entry.NextTime) continue;

                float interval = GetRandomInterval(tickable);
                entry.NextTime = time + interval;
                randomTickables[index] = entry;

                if (tickable.OUTL_IsRandomTickEnabled)
                {
                    OUTL_Profile.Frame.RandomTicks++;
                    tickable.OUTL_RandomTick(world, time);
                    if (!randomIndices.ContainsKey(tickable))
                        randomCursor = Mathf.Min(index, randomTickables.Count);
                }
            }
        }
    }

    private static float GetRandomInterval(OUTL_IRandomTickable tickable)
    {
        OUTL_IRandomTickIntervalProvider provider = tickable as OUTL_IRandomTickIntervalProvider;
        if (provider != null) return Mathf.Max(0.01f, provider.OUTL_RandomTickInterval);
        return 0.25f;
    }

    private static OUTL_TickLane SanitizeLane(OUTL_TickLane lane)
    {
        int value = (int)lane;
        if (value < 0 || value >= LaneCount) return OUTL_TickLane.Logic;
        return lane;
    }

    private static float StablePhase01(object obj, int salt)
    {
        unchecked
        {
            int h = 216613626;
            if (obj != null)
            {
                Object unityObject = obj as Object;
                h = (h * 16777619) ^ (unityObject != null ? unityObject.GetInstanceID() : obj.GetHashCode());
                System.Type type = obj.GetType();
                h = (h * 16777619) ^ (type != null ? type.FullName.GetHashCode() : 0);
            }
            h = (h * 16777619) ^ salt;
            uint u = (uint)h;
            return (u & 0x00FFFFFFu) / 16777215f;
        }
    }

    private void RemoveAt(OUTL_TickLane lane, int index)
    {
        List<Entry> list = lanes[(int)SanitizeLane(lane)];
        int last = list.Count - 1;
        if (index < 0 || index > last) return;

        OUTL_ITickable removed = list[index].Tickable;
        indices.Remove(removed);
        if (index != last)
        {
            Entry moved = list[last];
            list[index] = moved;
            if (moved.Tickable != null) indices[moved.Tickable] = new IndexRef { Lane = moved.Lane, Index = index };
        }
        list.RemoveAt(last);
        tickableCount = Mathf.Max(0, tickableCount - 1);
    }

    private void RemoveRandomAt(int index)
    {
        int last = randomTickables.Count - 1;
        if (index < 0 || index > last) return;
        OUTL_IRandomTickable removed = randomTickables[index].Tickable;
        randomIndices.Remove(removed);
        if (index != last)
        {
            RandomEntry moved = randomTickables[last];
            randomTickables[index] = moved;
            if (moved.Tickable != null) randomIndices[moved.Tickable] = index;
        }
        randomTickables.RemoveAt(last);
        if (randomCursor > randomTickables.Count) randomCursor = randomTickables.Count;
    }
}

[System.Serializable]
public struct OUTL_SectorCellStats
{
    public long Key;
    public int X;
    public int Z;
    public int CellId;
    public int EntityCount;
    public int AIActorCount;
    public int StimulusCount;
    public OUTL_RuntimeTier MaxTier;
}

[System.Serializable]
public struct OUTL_SectorIntegrityStats
{
    public int RegistryEntityCount;
    public int SectorEntityCount;
    public int CellCount;
    public int MissingFromSector;
    public int MissingFromRegistry;
    public int DuplicateSectorEntries;
    public int StaleSectorAddress;
    public int WorstSectorEntityCount;
}

public sealed class OUTL_SectorGrid
{
    private OUTL_World world;
    private float cellSize = 32f;
    private readonly Dictionary<long, List<OUTL_EntityRuntime>> cells = new Dictionary<long, List<OUTL_EntityRuntime>>(256);
    private readonly Dictionary<int, long> entityCell = new Dictionary<int, long>(1024);
    private readonly Dictionary<int, int> entityCellIndex = new Dictionary<int, int>(1024);
    private readonly Dictionary<int, OUTL_AIActor> aiActorCache = new Dictionary<int, OUTL_AIActor>(1024);
    private readonly List<OUTL_EntityRuntime> rebuildBuffer = new List<OUTL_EntityRuntime>(1024);

    public void Bind(OUTL_World world) { this.world = world; }

    public void SetCellSize(float size)
    {
        float newSize = Mathf.Max(1f, size);
        if (Mathf.Abs(newSize - cellSize) <= 0.001f) return;
        cellSize = newSize;
        RebuildFromRegistry();
    }

    public Vector2Int WorldToCell(Vector3 position)
    {
        return new Vector2Int(Mathf.FloorToInt(position.x / cellSize), Mathf.FloorToInt(position.z / cellSize));
    }

    public int CellToId(Vector2Int cell)
    {
        unchecked { return (cell.x * 73856093) ^ (cell.y * 19349663); }
    }

    public long CellToKey(Vector2Int cell)
    {
        unchecked { return ((long)cell.x << 32) ^ (uint)cell.y; }
    }

    private static void DecodeCellKey(long key, out int x, out int z)
    {
        x = (int)(key >> 32);
        z = (int)(key & 0xffffffff);
    }

    public void RegisterOrUpdate(OUTL_EntityRuntime entity)
    {
        using (OUTL_Profile.SectorRegisterOrUpdate.Auto())
        {
            RegisterOrUpdateInternal(entity);
        }
    }

    public void Unregister(OUTL_EntityId id)
    {
        if (!id.IsValid) return;
        long cell;
        if (!entityCell.TryGetValue(id.Value, out cell)) return;
        RemoveFromCell(id.Value, cell);
    }

    public int CellCount { get { return cells.Count; } }
    public int IndexedEntityCount { get { return entityCell.Count; } }

    public void RebuildSectorIndexSafe()
    {
        using (OUTL_Profile.SectorRegisterOrUpdate.Auto())
        {
            RebuildFromRegistry();
        }
    }

    public bool Contains(OUTL_EntityId id)
    {
        return id.IsValid && entityCell.ContainsKey(id.Value);
    }

    public bool TryGetEntityCell(OUTL_EntityId id, out long cellKey)
    {
        cellKey = 0L;
        return id.IsValid && entityCell.TryGetValue(id.Value, out cellKey);
    }

    public int CopyCellStats(List<OUTL_SectorCellStats> output)
    {
        if (output == null) return 0;
        output.Clear();
        foreach (KeyValuePair<long, List<OUTL_EntityRuntime>> pair in cells)
        {
            List<OUTL_EntityRuntime> list = pair.Value;
            int x;
            int z;
            DecodeCellKey(pair.Key, out x, out z);
            OUTL_RuntimeTier maxTier = OUTL_RuntimeTier.Dormant;
            int aiCount = 0;
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    OUTL_EntityRuntime e = list[i];
                    if (e == null || e.Adapter == null) continue;
                    if ((int)e.Tier > (int)maxTier) maxTier = e.Tier;
                    if (HasCachedAIActor(e)) aiCount++;
                }
            }

            output.Add(new OUTL_SectorCellStats
            {
                Key = pair.Key,
                X = x,
                Z = z,
                CellId = CellToId(new Vector2Int(x, z)),
                EntityCount = list != null ? list.Count : 0,
                AIActorCount = aiCount,
                StimulusCount = OUTL_StimulusBus.CountInCell(pair.Key),
                MaxTier = maxTier
            });
        }

        return output.Count;
    }

    public int ValidateIntegrity(List<string> output, out OUTL_SectorIntegrityStats stats)
    {
        stats = default(OUTL_SectorIntegrityStats);
        if (output != null) output.Clear();
        stats.CellCount = cells.Count;
        stats.SectorEntityCount = entityCell.Count;
        if (world == null) return 0;

        HashSet<int> seen = new HashSet<int>();
        foreach (KeyValuePair<long, List<OUTL_EntityRuntime>> pair in cells)
        {
            List<OUTL_EntityRuntime> list = pair.Value;
            if (list == null) continue;
            if (list.Count > stats.WorstSectorEntityCount) stats.WorstSectorEntityCount = list.Count;
            for (int i = 0; i < list.Count; i++)
            {
                OUTL_EntityRuntime e = list[i];
                if (e == null || !e.Id.IsValid)
                {
                    stats.MissingFromRegistry++;
                    if (output != null) output.Add("sector cell contains null/invalid runtime at cell=" + pair.Key);
                    continue;
                }

                if (!seen.Add(e.Id.Value))
                {
                    stats.DuplicateSectorEntries++;
                    if (output != null) output.Add("duplicate sector entry id=" + e.Id.Value);
                }

                OUTL_EntityRuntime registryRuntime;
                if (!world.Registry.TryGet(e.Id, out registryRuntime) || registryRuntime != e)
                {
                    stats.MissingFromRegistry++;
                    if (output != null) output.Add("sector has id missing from registry id=" + e.Id.Value);
                }

                if (e.Adapter != null)
                {
                    long expected = CellToKey(WorldToCell(e.Adapter.transform.position));
                    if (expected != pair.Key)
                    {
                        stats.StaleSectorAddress++;
                        if (output != null) output.Add("stale sector address id=" + e.Id.Value + " stored=" + pair.Key + " expected=" + expected);
                    }
                }
            }
        }

        world.Registry.CopyAll(rebuildBuffer);
        stats.RegistryEntityCount = rebuildBuffer.Count;
        for (int i = 0; i < rebuildBuffer.Count; i++)
        {
            OUTL_EntityRuntime e = rebuildBuffer[i];
            if (e == null || !e.Id.IsValid) continue;
            if (e.Adapter != null && !e.Adapter.RegisterInSectors) continue;
            if (!entityCell.ContainsKey(e.Id.Value))
            {
                stats.MissingFromSector++;
                if (output != null) output.Add("registry entity missing sector id=" + e.Id.Value);
            }
        }

        return stats.MissingFromSector + stats.MissingFromRegistry + stats.DuplicateSectorEntries + stats.StaleSectorAddress;
    }

    public OUTL_EntityRuntime FindNearestHostile(OUTL_EntityRuntime self, Vector3 point, float maxDistance)
    {
        if (self == null || world == null) return null;
        float bestSqr = maxDistance > 0f ? maxDistance * maxDistance : float.MaxValue;
        OUTL_EntityRuntime best = null;

        using (OUTL_Profile.SectorQuery.Auto())
        {
            int radius;
            Vector2Int center;
            BuildCellQuery(point, maxDistance, out radius, out center);
            for (int z = -radius; z <= radius; z++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    List<OUTL_EntityRuntime> list;
                    if (!cells.TryGetValue(CellToKey(new Vector2Int(center.x + x, center.y + z)), out list)) continue;
                    for (int i = 0; i < list.Count; i++)
                    {
                        OUTL_EntityRuntime e = list[i];
                        if (e == null || e == self || e.Adapter == null) continue;
                        if (IsDeadOrZeroHealth(e)) continue;
                        if (!world.Factions.AreHostile(self, e)) continue;
                        float sqr = (e.Adapter.transform.position - point).sqrMagnitude;
                        if (sqr < bestSqr)
                        {
                            bestSqr = sqr;
                            best = e;
                        }
                    }
                }
            }
        }

        return best;
    }

    public OUTL_EntityRuntime FindNearestWithTags(Vector3 point, string[] requiredTags, float maxDistance, OUTL_EntityRuntime ignore = null)
    {
        if (requiredTags == null || requiredTags.Length == 0) return null;
        float bestSqr = maxDistance > 0f ? maxDistance * maxDistance : float.MaxValue;
        OUTL_EntityRuntime best = null;

        using (OUTL_Profile.SectorQuery.Auto())
        {
            int radius;
            Vector2Int center;
            BuildCellQuery(point, maxDistance, out radius, out center);
            for (int z = -radius; z <= radius; z++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    List<OUTL_EntityRuntime> list;
                    if (!cells.TryGetValue(CellToKey(new Vector2Int(center.x + x, center.y + z)), out list)) continue;
                    for (int i = 0; i < list.Count; i++)
                    {
                        OUTL_EntityRuntime e = list[i];
                        if (e == null || e == ignore || e.Adapter == null) continue;
                        if (IsDeadOrZeroHealth(e)) continue;
                        if (!HasAnyTag(e, requiredTags)) continue;
                        float sqr = (e.Adapter.transform.position - point).sqrMagnitude;
                        if (sqr < bestSqr)
                        {
                            bestSqr = sqr;
                            best = e;
                        }
                    }
                }
            }
        }

        return best;
    }

    public int CollectHostileCandidates(OUTL_EntityRuntime self, Vector3 point, float maxDistance, List<OUTL_EntityRuntime> output, int maxCount)
    {
        if (output == null) return 0;
        output.Clear();
        if (self == null || world == null || maxCount <= 0) return 0;

        using (OUTL_Profile.SectorQuery.Auto())
        {
            int radius;
            Vector2Int center;
            BuildCellQuery(point, maxDistance, out radius, out center);
            for (int z = -radius; z <= radius; z++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    List<OUTL_EntityRuntime> list;
                    if (!cells.TryGetValue(CellToKey(new Vector2Int(center.x + x, center.y + z)), out list)) continue;
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (output.Count >= maxCount) break;
                        OUTL_EntityRuntime e = list[i];
                        if (e == null || e == self || e.Adapter == null) continue;
                        if (IsDeadOrZeroHealth(e)) continue;
                        if (!world.Factions.AreHostile(self, e)) continue;
                        output.Add(e);
                    }
                    if (output.Count >= maxCount) break;
                }
                if (output.Count >= maxCount) break;
            }
        }

        SortByDistance(output, point);
        if (output.Count > maxCount) output.RemoveRange(maxCount, output.Count - maxCount);
        return output.Count;
    }

    public int CollectTagCandidates(Vector3 point, string[] requiredTags, float maxDistance, OUTL_EntityRuntime ignore, List<OUTL_EntityRuntime> output, int maxCount)
    {
        if (output == null) return 0;
        output.Clear();
        if (requiredTags == null || requiredTags.Length == 0 || maxCount <= 0) return 0;

        using (OUTL_Profile.SectorQuery.Auto())
        {
            int radius;
            Vector2Int center;
            BuildCellQuery(point, maxDistance, out radius, out center);
            for (int z = -radius; z <= radius; z++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    List<OUTL_EntityRuntime> list;
                    if (!cells.TryGetValue(CellToKey(new Vector2Int(center.x + x, center.y + z)), out list)) continue;
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (output.Count >= maxCount) break;
                        OUTL_EntityRuntime e = list[i];
                        if (e == null || e == ignore || e.Adapter == null) continue;
                        if (IsDeadOrZeroHealth(e)) continue;
                        if (!HasAnyTag(e, requiredTags)) continue;
                        output.Add(e);
                    }
                    if (output.Count >= maxCount) break;
                }
                if (output.Count >= maxCount) break;
            }
        }

        SortByDistance(output, point);
        if (output.Count > maxCount) output.RemoveRange(maxCount, output.Count - maxCount);
        return output.Count;
    }

    private void BuildCellQuery(Vector3 point, float maxDistance, out int radius, out Vector2Int center)
    {
        float distance = maxDistance > 0f ? maxDistance : cellSize;
        radius = Mathf.Max(0, Mathf.CeilToInt(distance / Mathf.Max(1f, cellSize)));
        center = WorldToCell(point);
    }

    private void RegisterOrUpdateInternal(OUTL_EntityRuntime entity)
    {
        if (entity == null || entity.Adapter == null || !entity.Id.IsValid) return;
        int id = entity.Id.Value;
        long cell = CellToKey(WorldToCell(entity.Adapter.transform.position));
        long oldCell;
        if (entityCell.TryGetValue(id, out oldCell))
        {
            if (oldCell == cell) return;
            RemoveFromCell(id, oldCell);
        }

        List<OUTL_EntityRuntime> list = GetCellList(cell);
        entityCell[id] = cell;
        entityCellIndex[id] = list.Count;
        list.Add(entity);
    }

    private void RebuildFromRegistry()
    {
        cells.Clear();
        entityCell.Clear();
        entityCellIndex.Clear();
        aiActorCache.Clear();
        if (world == null) return;
        world.Registry.CopyAll(rebuildBuffer);
        for (int i = 0; i < rebuildBuffer.Count; i++)
            RegisterOrUpdateInternal(rebuildBuffer[i]);
    }

    private bool HasCachedAIActor(OUTL_EntityRuntime entity)
    {
        if (entity == null || entity.Adapter == null || !entity.Id.IsValid) return false;

        OUTL_AIActor ai;
        if (aiActorCache.TryGetValue(entity.Id.Value, out ai))
        {
            if (ai != null && ai.gameObject == entity.Adapter.gameObject) return true;
            aiActorCache.Remove(entity.Id.Value);
        }

        ai = entity.Adapter.GetComponent<OUTL_AIActor>();
        if (ai != null) aiActorCache[entity.Id.Value] = ai;
        return ai != null;
    }

    private static void SortByDistance(List<OUTL_EntityRuntime> list, Vector3 point)
    {
        for (int i = 1; i < list.Count; i++)
        {
            OUTL_EntityRuntime key = list[i];
            float keySqr = key != null && key.Adapter != null ? (key.Adapter.transform.position - point).sqrMagnitude : float.MaxValue;
            int j = i - 1;
            while (j >= 0)
            {
                OUTL_EntityRuntime prev = list[j];
                float prevSqr = prev != null && prev.Adapter != null ? (prev.Adapter.transform.position - point).sqrMagnitude : float.MaxValue;
                if (prevSqr <= keySqr) break;
                list[j + 1] = list[j];
                j--;
            }
            list[j + 1] = key;
        }
    }

    private static bool HasAnyTag(OUTL_EntityRuntime entity, string[] tags)
    {
        if (entity == null || entity.Tags == null || tags == null) return false;
        for (int t = 0; t < tags.Length; t++)
        {
            string tag = tags[t];
            if (string.IsNullOrEmpty(tag)) continue;
            for (int e = 0; e < entity.Tags.Length; e++)
                if (entity.Tags[e] == tag)
                    return true;
        }
        return false;
    }

    private static bool IsDeadOrZeroHealth(OUTL_EntityRuntime entity)
    {
        if (entity == null) return true;
        if (entity.State.GetFlag(OUTL_StateId.Dead)) return true;
        return entity.Stats.Get(OUTL_StatId.Health, 1f) <= 0f;
    }

    private List<OUTL_EntityRuntime> GetCellList(long cellId)
    {
        List<OUTL_EntityRuntime> list;
        if (!cells.TryGetValue(cellId, out list))
        {
            list = new List<OUTL_EntityRuntime>(32);
            cells[cellId] = list;
        }
        return list;
    }

    private void RemoveFromCell(int entityId, long cellId)
    {
        List<OUTL_EntityRuntime> list;
        if (!cells.TryGetValue(cellId, out list))
        {
            entityCell.Remove(entityId);
            entityCellIndex.Remove(entityId);
            aiActorCache.Remove(entityId);
            return;
        }

        int index;
        if (!entityCellIndex.TryGetValue(entityId, out index))
        {
            entityCell.Remove(entityId);
            aiActorCache.Remove(entityId);
            return;
        }

        int last = list.Count - 1;
        if (index >= 0 && index <= last)
        {
            if (index != last)
            {
                OUTL_EntityRuntime moved = list[last];
                list[index] = moved;
                if (moved != null) entityCellIndex[moved.Id.Value] = index;
            }
            list.RemoveAt(last);
        }

        if (list.Count == 0) cells.Remove(cellId);
        entityCell.Remove(entityId);
        entityCellIndex.Remove(entityId);
        aiActorCache.Remove(entityId);
    }
}
