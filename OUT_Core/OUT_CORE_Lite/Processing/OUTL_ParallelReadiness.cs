using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum OUTL_ActorSnapshotFlags
{
    None = 0,
    HasAI = 1 << 0,
    Dead = 1 << 1,
    HasTarget = 1 << 2,
    TargetVisible = 1 << 3,
    Stationary = 1 << 4,
    PreferRanged = 1 << 5,
    Creature = 1 << 6,
    FleeFromDanger = 1 << 7,
    HasNav = 1 << 8,
    HasAttack = 1 << 9,
    RegisterRandomTick = 1 << 10
}

[Flags]
public enum OUTL_ParallelResultWriteMask
{
    None = 0,
    Tier = 1 << 0,
    AIState = 1 << 1,
    Goal = 1 << 2,
    Command = 1 << 3,
    Debug = 1 << 4
}

[Serializable]
public struct OUTL_ActorSnapshotRow
{
    public int EntityId;
    public int TargetEntityId;
    public OUTL_RuntimeTier RuntimeTier;
    public OUTL_TickLane TickLane;
    public OUTL_AIStateId AIState;
    public OUTL_StimulusType LastStimulusType;
    public OUTL_ActorSnapshotFlags Flags;
    public Vector3 Position;
    public Vector3 Forward;
    public Vector3 LastKnownTargetPosition;
    public Vector3 LastStimulusPosition;
    public int ChunkX;
    public int ChunkZ;
    public float Health;
    public float MaxHealth;
    public float TargetDistance;
    public float LastStimulusPriority;
    public float LastStimulusAge;
    public float Danger;
    public float Food;
    public float Suspicion;
    public float Fear;
    public float Aggression;
    public float Morale;
    public float AllegianceInfluence;
    public float FactionInfluence;
    public float ThinkIntervalNear;
    public float ThinkIntervalMid;
    public float ThinkIntervalFar;
    public float ViewDistance;
    public float AttackDistance;
    public float PreferredRange;
    public float MinSafeRange;
}

[Serializable]
public struct OUTL_ActorTierResultRow
{
    public int EntityId;
    public OUTL_RuntimeTier PreviousTier;
    public OUTL_RuntimeTier NewTier;
    public OUTL_ParallelResultWriteMask FutureWriteMask;
    public float SqrDistanceToFocus;
    public float DistanceToFocus;
    public float EntityTickInterval;
    public float RandomTickInterval;
    public float NavTickInterval;
    public int Changed;
}

[Serializable]
public struct OUTL_AIStimulusScoreRow
{
    public int EntityId;
    public int SourceEntityId;
    public OUTL_StimulusType Type;
    public Vector3 Position;
    public float Strength;
    public float Confidence;
    public float Priority;
    public float Age;
    public float Score;
}

[Serializable]
public struct OUTL_AIDecisionResultRow
{
    public int EntityId;
    public int TargetEntityId;
    public OUTL_AIStateId State;
    public OUTL_CommandType Command;
    public OUTL_StimulusType DominantStimulusType;
    public OUTL_ParallelResultWriteMask FutureWriteMask;
    public Vector3 MoveTarget;
    public float Score;
}

[Serializable]
public struct OUTL_AIDebugTableRow
{
    public int EntityId;
    public int TargetEntityId;
    public OUTL_AIStateId State;
    public OUTL_StimulusType StimulusType;
    public OUTL_CommandType NextCommand;
    public OUTL_ActorSnapshotFlags Flags;
    public float Health;
    public float Fear;
    public float Aggression;
    public float Morale;
    public float Distance;
    public float Visibility;
    public float Danger;
    public float Food;
    public float Suspicion;
}

public sealed class OUTL_ParallelReadinessBuffers
{
    public readonly List<OUTL_EntityRuntime> RegistryRows = new List<OUTL_EntityRuntime>(1024);
    public readonly List<OUTL_ActorSnapshotRow> ActorSnapshots = new List<OUTL_ActorSnapshotRow>(1024);
    public readonly List<OUTL_ActorTierResultRow> TierResults = new List<OUTL_ActorTierResultRow>(1024);
    public readonly List<OUTL_AIStimulusScoreRow> StimulusScores = new List<OUTL_AIStimulusScoreRow>(256);
    public readonly List<OUTL_AIDecisionResultRow> DecisionResults = new List<OUTL_AIDecisionResultRow>(256);
    public readonly List<OUTL_AIDebugTableRow> DebugRows = new List<OUTL_AIDebugTableRow>(256);

    private readonly Dictionary<int, OUTL_AIActor> aiCache = new Dictionary<int, OUTL_AIActor>(1024);

    public int SnapshotRowCount { get { return ActorSnapshots.Count; } }
    public int TierResultCount { get { return TierResults.Count; } }
    public int StimulusScoreCount { get { return StimulusScores.Count; } }
    public int DecisionResultCount { get { return DecisionResults.Count; } }
    public int DebugRowCount { get { return DebugRows.Count; } }

    public void ClearRows()
    {
        ActorSnapshots.Clear();
        TierResults.Clear();
        StimulusScores.Clear();
        DecisionResults.Clear();
        DebugRows.Clear();
    }

    public void ClearCaches()
    {
        aiCache.Clear();
    }

    internal OUTL_AIActor ResolveAI(int entityId, OUTL_EntityAdapter adapter)
    {
        OUTL_AIActor ai;
        if (aiCache.TryGetValue(entityId, out ai)) return ai;
        ai = adapter != null ? adapter.GetComponent<OUTL_AIActor>() : null;
        aiCache[entityId] = ai;
        return ai;
    }
}

public static class OUTL_ParallelReadiness
{
    public static int BuildSnapshotFromRegistry(OUTL_World world, OUTL_ParallelReadinessBuffers buffers, float chunkSize)
    {
        if (world == null || buffers == null) return 0;
        world.Registry.CopyAll(buffers.RegistryRows);
        return BuildSnapshotFromEntities(buffers.RegistryRows, buffers, world.WorldTime, chunkSize);
    }

    public static int BuildSnapshotFromEntities(List<OUTL_EntityRuntime> entities, OUTL_ParallelReadinessBuffers buffers, float worldTime, float chunkSize)
    {
        if (buffers == null) return 0;
        buffers.ClearRows();
        if (entities == null || entities.Count == 0) return 0;

        EnsureCapacity(buffers.ActorSnapshots, entities.Count);
        float safeChunkSize = Mathf.Max(1f, chunkSize);

        for (int i = 0; i < entities.Count; i++)
        {
            OUTL_EntityRuntime runtime = entities[i];
            if (runtime == null || runtime.Adapter == null || !runtime.Id.IsValid) continue;

            OUTL_EntityAdapter adapter = runtime.Adapter;
            Transform t = adapter.transform;
            Vector3 position = t.position;
            Vector2Int chunk = OUTL_ChunkProcessingDriver.WorldToChunk(position, safeChunkSize);

            OUTL_ActorSnapshotRow row = new OUTL_ActorSnapshotRow
            {
                EntityId = runtime.Id.Value,
                RuntimeTier = runtime.Tier,
                TickLane = adapter.TickLane,
                Position = position,
                Forward = t.forward,
                ChunkX = chunk.x,
                ChunkZ = chunk.y,
                Health = runtime.Stats.Get(OUTL_StatId.Health, 0f),
                MaxHealth = runtime.Stats.Get("MaxHealth", 0f),
                Flags = adapter.RegisterRandomTick ? OUTL_ActorSnapshotFlags.RegisterRandomTick : OUTL_ActorSnapshotFlags.None
            };

            if (runtime.State.GetFlag(OUTL_StateId.Dead) || row.Health <= 0f)
                row.Flags |= OUTL_ActorSnapshotFlags.Dead;

            OUTL_AIActor ai = buffers.ResolveAI(runtime.Id.Value, adapter);
            if (ai != null) FillAIFields(ai, worldTime, ref row, buffers);

            buffers.ActorSnapshots.Add(row);
        }

        return buffers.ActorSnapshots.Count;
    }

    public static int CalculateTierResults(List<OUTL_ActorSnapshotRow> snapshots, Vector3 focusPosition, OUTL_ProcessingProfile profile, List<OUTL_ActorTierResultRow> results)
    {
        if (results == null) return 0;
        results.Clear();
        if (snapshots == null || snapshots.Count == 0 || profile == null) return 0;

        EnsureCapacity(results, snapshots.Count);
        for (int i = 0; i < snapshots.Count; i++)
        {
            OUTL_ActorSnapshotRow snapshot = snapshots[i];
            float sqr = (snapshot.Position - focusPosition).sqrMagnitude;
            OUTL_RuntimeTier next = EvaluateTier(sqr, profile);
            OUTL_TierProcessingSettings settings = profile.GetSettings(next);
            results.Add(new OUTL_ActorTierResultRow
            {
                EntityId = snapshot.EntityId,
                PreviousTier = snapshot.RuntimeTier,
                NewTier = next,
                FutureWriteMask = OUTL_ParallelResultWriteMask.Tier,
                SqrDistanceToFocus = sqr,
                DistanceToFocus = Mathf.Sqrt(sqr),
                EntityTickInterval = settings.EntityTickInterval,
                RandomTickInterval = settings.RandomTickInterval,
                NavTickInterval = settings.NavTickInterval,
                Changed = snapshot.RuntimeTier != next ? 1 : 0
            });
        }

        return results.Count;
    }

    public static int CountChangedTierResults(List<OUTL_ActorTierResultRow> results)
    {
        if (results == null) return 0;
        int count = 0;
        for (int i = 0; i < results.Count; i++)
            if (results[i].Changed != 0)
                count++;
        return count;
    }

    public static int ApplyResultsMainThread(OUTL_World world, List<OUTL_ActorTierResultRow> tierResults, List<OUTL_AIDecisionResultRow> decisionResults)
    {
        // v0.1 deliberately performs no gameplay writes. Future jobs write result rows; main thread consumes them here.
        return 0;
    }

    private static void FillAIFields(OUTL_AIActor ai, float worldTime, ref OUTL_ActorSnapshotRow row, OUTL_ParallelReadinessBuffers buffers)
    {
        row.Flags |= OUTL_ActorSnapshotFlags.HasAI;
        if (ai.CurrentTarget.IsValid) row.Flags |= OUTL_ActorSnapshotFlags.HasTarget;
        if (ai.CurrentTargetVisible) row.Flags |= OUTL_ActorSnapshotFlags.TargetVisible;
        if (ai.Stationary) row.Flags |= OUTL_ActorSnapshotFlags.Stationary;
        if (ai.PreferRangedCombat) row.Flags |= OUTL_ActorSnapshotFlags.PreferRanged;
        if (ai.CreatureUsesFoodStimulus) row.Flags |= OUTL_ActorSnapshotFlags.Creature;
        if (ai.FleeFromDanger) row.Flags |= OUTL_ActorSnapshotFlags.FleeFromDanger;
        if (ai.NavMover != null) row.Flags |= OUTL_ActorSnapshotFlags.HasNav;
        if (ai.AttackDriver != null) row.Flags |= OUTL_ActorSnapshotFlags.HasAttack;

        row.TargetEntityId = ai.CurrentTarget.Value;
        row.AIState = ai.CurrentState;
        row.LastStimulusType = ai.LastStimulusType;
        row.LastKnownTargetPosition = ai.LastKnownTargetPosition;
        row.LastStimulusPosition = ai.LastStimulusPosition;
        row.TargetDistance = ai.CurrentTargetDistance;
        row.LastStimulusPriority = ai.LastStimulusPriority;
        row.LastStimulusAge = ai.LastStimulusTime > 0f ? Mathf.Max(0f, worldTime - ai.LastStimulusTime) : 0f;
        row.Danger = ai.CurrentDanger;
        row.Food = ai.CurrentFood;
        row.Suspicion = ai.Suspicion;
        row.Fear = ai.CurrentFear;
        row.Aggression = ai.CurrentAggression;
        row.Morale = ai.CurrentMorale;
        row.AllegianceInfluence = ai.AllegianceInfluence;
        row.FactionInfluence = ai.FactionInfluence;
        row.PreferredRange = ai.PreferredRange;
        row.MinSafeRange = ai.MinSafeRange;

        OUTL_AIProfile profile = ai.Profile;
        if (profile != null)
        {
            row.ThinkIntervalNear = profile.ThinkIntervalNear;
            row.ThinkIntervalMid = profile.ThinkIntervalMid;
            row.ThinkIntervalFar = profile.ThinkIntervalFar;
            row.ViewDistance = profile.ViewDistance;
            row.AttackDistance = profile.AttackDistance;
        }

        buffers.DebugRows.Add(new OUTL_AIDebugTableRow
        {
            EntityId = row.EntityId,
            TargetEntityId = row.TargetEntityId,
            State = row.AIState,
            StimulusType = row.LastStimulusType,
            NextCommand = OUTL_CommandType.None,
            Flags = row.Flags,
            Health = row.Health,
            Fear = row.Fear,
            Aggression = row.Aggression,
            Morale = row.Morale,
            Distance = row.TargetDistance,
            Visibility = (row.Flags & OUTL_ActorSnapshotFlags.TargetVisible) != 0 ? 1f : 0f,
            Danger = row.Danger,
            Food = row.Food,
            Suspicion = row.Suspicion
        });

        if (row.LastStimulusType != OUTL_StimulusType.None)
        {
            float confidence = Mathf.Clamp01(1f - row.LastStimulusAge / Mathf.Max(0.1f, ai.StimulusForgetAfter));
            buffers.StimulusScores.Add(new OUTL_AIStimulusScoreRow
            {
                EntityId = row.EntityId,
                SourceEntityId = ai.LastStimulus.Source.Value,
                Type = row.LastStimulusType,
                Position = row.LastStimulusPosition,
                Strength = ai.LastStimulus.Strength,
                Confidence = confidence,
                Priority = row.LastStimulusPriority,
                Age = row.LastStimulusAge,
                Score = row.LastStimulusPriority * confidence
            });
        }

        buffers.DecisionResults.Add(new OUTL_AIDecisionResultRow
        {
            EntityId = row.EntityId,
            TargetEntityId = row.TargetEntityId,
            State = row.AIState,
            Command = OUTL_CommandType.None,
            DominantStimulusType = row.LastStimulusType,
            FutureWriteMask = OUTL_ParallelResultWriteMask.AIState | OUTL_ParallelResultWriteMask.Goal | OUTL_ParallelResultWriteMask.Command,
            MoveTarget = row.LastKnownTargetPosition,
            Score = Mathf.Max(row.Aggression, row.Fear)
        });
    }

    private static OUTL_RuntimeTier EvaluateTier(float sqrDistance, OUTL_ProcessingProfile profile)
    {
        float full = Mathf.Max(0f, profile.FullDistance);
        float near = Mathf.Max(full, profile.NearDistance);
        float mid = Mathf.Max(near, profile.MidDistance);
        float far = Mathf.Max(mid, profile.FarDistance);
        if (sqrDistance <= full * full) return OUTL_RuntimeTier.Full;
        if (sqrDistance <= near * near) return OUTL_RuntimeTier.Near;
        if (sqrDistance <= mid * mid) return OUTL_RuntimeTier.Mid;
        if (sqrDistance <= far * far) return OUTL_RuntimeTier.Far;
        return OUTL_RuntimeTier.Dormant;
    }

    private static void EnsureCapacity<T>(List<T> list, int count)
    {
        if (list != null && list.Capacity < count) list.Capacity = count;
    }
}
