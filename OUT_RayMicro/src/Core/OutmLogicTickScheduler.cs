using System.Numerics;

namespace OUT_RayMicro.Core;

public enum OutmLogicTickTier : byte
{
    Always,
    Near,
    Mid,
    Far,
    Dormant
}

public readonly struct OutmLogicTickDecision
{
    public readonly bool ShouldTick;
    public readonly OutmLogicTickTier Tier;
    public readonly int IntervalTicks;
    public readonly int Phase;

    public OutmLogicTickDecision(bool shouldTick, OutmLogicTickTier tier, int intervalTicks, int phase)
    {
        ShouldTick = shouldTick;
        Tier = tier;
        IntervalTicks = intervalTicks;
        Phase = phase;
    }
}

public sealed class OutmLogicTickPolicy
{
    public float NearDistance = 24.0f;
    public float MidDistance = 64.0f;
    public float FarDistance = 128.0f;

    public int AlwaysEveryTicks = 1;
    public int NearEveryTicks = 1;
    public int MidEveryTicks = 4;
    public int FarEveryTicks = 16;
    public int DormantEveryTicks = 60;

    // Deterministic randomization phase. This is the OUT version of random tick spread:
    // not global RNG chaos, but stable per-entity/per-chunk staggering so 900 objects do not wake on the same frame like bureaucrats at lunch.
    public int RandomPhaseSpreadTicks = 8;

    public int ResolveInterval(OutmLogicTickTier tier)
    {
        int interval = tier switch
        {
            OutmLogicTickTier.Always => AlwaysEveryTicks,
            OutmLogicTickTier.Near => NearEveryTicks,
            OutmLogicTickTier.Mid => MidEveryTicks,
            OutmLogicTickTier.Far => FarEveryTicks,
            _ => DormantEveryTicks
        };

        return Math.Max(1, interval);
    }

    public OutmLogicTickTier ResolveTier(Vector3 position, Vector3 focus)
    {
        float distanceSq = Vector3.DistanceSquared(position, focus);
        if (distanceSq <= NearDistance * NearDistance)
            return OutmLogicTickTier.Near;
        if (distanceSq <= MidDistance * MidDistance)
            return OutmLogicTickTier.Mid;
        if (distanceSq <= FarDistance * FarDistance)
            return OutmLogicTickTier.Far;

        return OutmLogicTickTier.Dormant;
    }
}

public readonly struct OutmChunkKey : IEquatable<OutmChunkKey>
{
    public readonly int X;
    public readonly int Y;
    public readonly int Z;

    public OutmChunkKey(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public bool Equals(OutmChunkKey other) => X == other.X && Y == other.Y && Z == other.Z;
    public override bool Equals(object? obj) => obj is OutmChunkKey other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    public override string ToString() => $"C{X},{Y},{Z}";
}

public sealed class OutmLogicTickScheduler
{
    public readonly OutmLogicTickPolicy Policy = new();
    public float ChunkSize = 16.0f;

    public OutmLogicTickDecision DecideEntity(EntityId entity, Vector3 position, Vector3 focus, int worldTick, OutmLogicTickTier? forcedTier = null)
    {
        OutmLogicTickTier tier = forcedTier ?? Policy.ResolveTier(position, focus);
        int interval = Policy.ResolveInterval(tier);
        int phase = StablePhase(entity.Index, Policy.RandomPhaseSpreadTicks, interval);
        bool tick = ((worldTick + phase) % interval) == 0;
        return new OutmLogicTickDecision(tick, tier, interval, phase);
    }

    public OutmLogicTickDecision DecideChunk(OutmChunkKey chunk, Vector3 focus, int worldTick)
    {
        Vector3 center = ChunkCenter(chunk);
        OutmLogicTickTier tier = Policy.ResolveTier(center, focus);
        int interval = Policy.ResolveInterval(tier);
        int phase = StablePhase(chunk.GetHashCode(), Policy.RandomPhaseSpreadTicks, interval);
        bool tick = ((worldTick + phase) % interval) == 0;
        return new OutmLogicTickDecision(tick, tier, interval, phase);
    }

    public OutmChunkKey PositionToChunk(Vector3 position)
    {
        float size = MathF.Max(0.001f, ChunkSize);
        return new OutmChunkKey(
            (int)MathF.Floor(position.X / size),
            (int)MathF.Floor(position.Y / size),
            (int)MathF.Floor(position.Z / size));
    }

    public Vector3 ChunkCenter(OutmChunkKey key)
    {
        float size = MathF.Max(0.001f, ChunkSize);
        return new Vector3(
            (key.X + 0.5f) * size,
            (key.Y + 0.5f) * size,
            (key.Z + 0.5f) * size);
    }

    private static int StablePhase(int seed, int spreadTicks, int interval)
    {
        int spread = Math.Max(0, Math.Min(spreadTicks, interval - 1));
        if (spread <= 0)
            return 0;

        uint hash = (uint)seed;
        hash ^= 0x9E3779B9u;
        hash *= 0x85EBCA6Bu;
        hash ^= hash >> 13;
        hash *= 0xC2B2AE35u;
        hash ^= hash >> 16;
        return (int)(hash % (uint)(spread + 1));
    }
}
