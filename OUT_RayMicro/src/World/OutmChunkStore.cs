using System.Numerics;
using OUT_RayMicro.Core;

namespace OUT_RayMicro.World;

public enum OutmChunkState : byte
{
    Unloaded,
    Sleeping,
    Resident,
    Active
}

public struct OutmChunkRuntime
{
    public OutmChunkKey Key;
    public OutmChunkState State;
    public OutmLogicTickTier TickTier;
    public OutmLogicTickDecision TickDecision;
    public int LastSeenRevision;
    public int LastTouchedTick;
    public bool Dirty;

    public OutmChunkRuntime(OutmChunkKey key)
    {
        Key = key;
        State = OutmChunkState.Unloaded;
        TickTier = OutmLogicTickTier.Dormant;
        TickDecision = new OutmLogicTickDecision(false, OutmLogicTickTier.Dormant, 60, 0);
        LastSeenRevision = 0;
        LastTouchedTick = 0;
        Dirty = false;
    }
}

public sealed class OutmChunkStore
{
    private readonly Dictionary<OutmChunkKey, OutmChunkRuntime> chunks = new();
    private readonly List<OutmChunkKey> active = new(16);
    private readonly List<OutmChunkKey> resident = new(64);
    private readonly List<OutmChunkKey> sleeping = new(256);
    private readonly List<OutmChunkKey> keyScratch = new(512);
    private int revision;

    // FPS default: central chunk is hot, 3x3 around it is resident, outer tracked ring is cheap/sleeping.
    // Vertical chunking is off by default because ticking layers of empty air is a classic human hobby and we decline.
    public int ActiveRadiusChunks = 0;
    public int ResidentRadiusChunks = 1;
    public int TrackedRadiusChunks = 4;
    public int SleepingAfterTicks = 240;
    public bool UseVerticalChunking;

    public int ActiveCount => active.Count;
    public int ResidentCount => resident.Count;
    public int SleepingCount => sleeping.Count;
    public int KnownCount => chunks.Count;
    public OutmChunkKey FocusChunk { get; private set; }

    public IReadOnlyList<OutmChunkKey> ActiveChunks => active;
    public IReadOnlyList<OutmChunkKey> ResidentChunks => resident;
    public IReadOnlyList<OutmChunkKey> SleepingChunks => sleeping;

    public void UpdateAroundFocus(OutmLogicTickScheduler scheduler, Vector3 focusPosition, int worldTick)
    {
        revision++;
        active.Clear();
        resident.Clear();
        sleeping.Clear();

        OutmChunkKey rawFocus = scheduler.PositionToChunk(focusPosition);
        FocusChunk = UseVerticalChunking ? rawFocus : new OutmChunkKey(rawFocus.X, 0, rawFocus.Z);

        int activeRadius = Math.Max(0, ActiveRadiusChunks);
        int residentRadius = Math.Max(activeRadius, ResidentRadiusChunks);
        int trackedRadius = Math.Max(residentRadius, TrackedRadiusChunks);
        int yMin = UseVerticalChunking ? -trackedRadius : 0;
        int yMax = UseVerticalChunking ? trackedRadius : 0;

        for (int z = -trackedRadius; z <= trackedRadius; z++)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = -trackedRadius; x <= trackedRadius; x++)
                {
                    OutmChunkKey key = new(FocusChunk.X + x, FocusChunk.Y + y, FocusChunk.Z + z);
                    int chebyshev = UseVerticalChunking
                        ? Math.Max(Math.Abs(x), Math.Max(Math.Abs(y), Math.Abs(z)))
                        : Math.Max(Math.Abs(x), Math.Abs(z));

                    OutmChunkState state = chebyshev <= activeRadius
                        ? OutmChunkState.Active
                        : chebyshev <= residentRadius
                            ? OutmChunkState.Resident
                            : OutmChunkState.Sleeping;

                    TouchChunk(scheduler, key, state, focusPosition, worldTick);
                }
            }
        }

        keyScratch.Clear();
        foreach (OutmChunkKey key in chunks.Keys)
            keyScratch.Add(key);

        for (int i = 0; i < keyScratch.Count; i++)
        {
            OutmChunkKey key = keyScratch[i];
            OutmChunkRuntime chunk = chunks[key];
            if (chunk.LastSeenRevision == revision)
                continue;

            if (worldTick - chunk.LastTouchedTick >= SleepingAfterTicks)
                chunk.State = OutmChunkState.Sleeping;
            else if (chunk.State == OutmChunkState.Active)
                chunk.State = OutmChunkState.Resident;

            chunk.TickDecision = scheduler.DecideChunk(key, focusPosition, worldTick);
            chunk.TickTier = chunk.TickDecision.Tier;
            chunks[key] = chunk;
        }

        RebuildViews();
    }

    public bool TryGet(OutmChunkKey key, out OutmChunkRuntime chunk)
    {
        return chunks.TryGetValue(key, out chunk);
    }

    public OutmChunkState GetState(OutmChunkKey key)
    {
        return chunks.TryGetValue(key, out OutmChunkRuntime chunk) ? chunk.State : OutmChunkState.Unloaded;
    }

    public void MarkDirty(OutmChunkKey key)
    {
        if (!chunks.TryGetValue(key, out OutmChunkRuntime chunk))
            chunk = new OutmChunkRuntime(key);

        chunk.Dirty = true;
        chunks[key] = chunk;
    }

    public void ClearDirty(OutmChunkKey key)
    {
        if (!chunks.TryGetValue(key, out OutmChunkRuntime chunk))
            return;

        chunk.Dirty = false;
        chunks[key] = chunk;
    }

    private void TouchChunk(OutmLogicTickScheduler scheduler, OutmChunkKey key, OutmChunkState state, Vector3 focusPosition, int worldTick)
    {
        if (!chunks.TryGetValue(key, out OutmChunkRuntime chunk))
            chunk = new OutmChunkRuntime(key);

        chunk.State = state;
        chunk.LastSeenRevision = revision;
        chunk.LastTouchedTick = worldTick;
        chunk.TickDecision = scheduler.DecideChunk(key, focusPosition, worldTick);
        chunk.TickTier = ResolveTierForState(state, chunk.TickDecision.Tier);
        chunks[key] = chunk;
    }

    private static OutmLogicTickTier ResolveTierForState(OutmChunkState state, OutmLogicTickTier distanceTier)
    {
        return state switch
        {
            OutmChunkState.Active => OutmLogicTickTier.Always,
            OutmChunkState.Resident => distanceTier < OutmLogicTickTier.Mid ? OutmLogicTickTier.Mid : distanceTier,
            OutmChunkState.Sleeping => distanceTier < OutmLogicTickTier.Far ? OutmLogicTickTier.Far : distanceTier,
            _ => OutmLogicTickTier.Dormant
        };
    }

    private void RebuildViews()
    {
        active.Clear();
        resident.Clear();
        sleeping.Clear();

        foreach (KeyValuePair<OutmChunkKey, OutmChunkRuntime> pair in chunks)
        {
            switch (pair.Value.State)
            {
                case OutmChunkState.Active:
                    active.Add(pair.Key);
                    break;
                case OutmChunkState.Resident:
                    resident.Add(pair.Key);
                    break;
                case OutmChunkState.Sleeping:
                    sleeping.Add(pair.Key);
                    break;
            }
        }
    }
}
