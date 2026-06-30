using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_ChunkProcessingDriver : MonoBehaviour, OUTL_ITickable
{
    public OUTL_ProcessingProfileAsset ProfileAsset;
    public OUTL_ProcessingBuiltInPreset BuiltInPreset = OUTL_ProcessingBuiltInPreset.StreamingWorld;
    public bool UseAssetProfile;
    public bool ApplyPresetOnEnable = true;
    public Transform Focus;
    [Tooltip("Legacy compatibility field. Runtime scene camera lookup is intentionally not used by the canonical driver; assign Focus explicitly or use registry focus fallback.")]
    public bool UseMainCameraAsFocus = false;
    [Tooltip("Legacy compatibility field. Runtime Unity tag lookup is intentionally not used; use TargetName/ClassName focus fallback instead.")]
    public bool UsePlayerTagAsFallback = false;
    public bool UseRegistryFocusFallback = true;
    public string FocusTargetName = "player";
    public string FocusClassName = "player";
    public bool AutoRegister = true;

    [Header("Chunk Rings")]
    [Tooltip("Canonical OUTL streaming: only the center 3x3 is Near, ring 2 is Mid, ring 3 is Far, everything beyond is Dormant.")]
    public bool EnforceCanonicalThreeByThree = true;
    public float ChunkSize = 64f;
    public int FullRadius = 0;
    public int NearRadius = 1;
    public int MidRadius = 2;
    public int FarRadius = 3;
    public bool UseChebyshevDistance = true;
    public bool WriteChunkState = true;
    public bool DebugLogTierChanges;

    [Header("Budget")]
    public bool OverrideDriverTickInterval;
    public float DriverTickInterval = 0.30f;
    public bool OverrideEntitiesPerTick;
    public int EntitiesPerTick = 220;

    [Header("Snapshot Cache")]
    [Tooltip("When enabled, the registry entity list is copied only when count changes or the refresh timer expires. This avoids copying the full registry every chunk tick.")]
    public bool CacheRegistrySnapshot = true;
    [Tooltip("Safety refresh for stale/despawned references even when entity count did not change.")]
    public float FullRefreshInterval = 2.0f;

    [Header("Parallel Readiness Preview")]
    [Tooltip("Diagnostic full-registry snapshot. Keep disabled in production scenes: it performs O(N) work on every driver tick.")]
    public bool BuildParallelReadinessSnapshot = false;
    [Tooltip("Diagnostic tier calculation over the full snapshot. Keep disabled in production scenes.")]
    public bool CalculateParallelTierPreview = false;
    public int ParallelSnapshotRowCount;
    public int ParallelTierResultCount;
    public int ParallelTierChangeCount;
    public int ParallelAIDebugRowCount;
    public int LastProcessedCount;
    public int LastTierChangeCount;
    public int CompletedSweeps;

    private readonly OUTL_ProcessingProfile runtimeProfile = new OUTL_ProcessingProfile();
    private readonly List<OUTL_EntityRuntime> entityBuffer = new List<OUTL_EntityRuntime>(1024);
    private readonly Dictionary<int, OUTL_NavMeshMover> navMoverCache = new Dictionary<int, OUTL_NavMeshMover>(1024);
    private readonly OUTL_ParallelReadinessBuffers parallelBuffers = new OUTL_ParallelReadinessBuffers();
    private int cursor;
    private bool registered;
    private int cachedRegistryCount = -1;
    private float nextSnapshotRefresh;

    public OUTL_ProcessingProfile Profile { get { return UseAssetProfile && ProfileAsset != null && ProfileAsset.Profile != null ? ProfileAsset.Profile : runtimeProfile; } }
    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled && OUTL_World.Instance != null; } }
    public OUTL_TickLane OUTL_TickLane { get { return OUTL_TickLane.Custom; } }
    public float OUTL_TickInterval { get { return Mathf.Max(0.01f, OverrideDriverTickInterval ? DriverTickInterval : Profile.DriverTickInterval); } }
    public int CachedEntityCount { get { return entityBuffer.Count; } }
    public int Cursor { get { return cursor; } }

    private void Awake()
    {
        ResolveFocus();
        if (!UseAssetProfile) runtimeProfile.ApplyBuiltIn(BuiltInPreset);
    }

    private void OnEnable()
    {
        if (!UseAssetProfile && ApplyPresetOnEnable) runtimeProfile.ApplyBuiltIn(BuiltInPreset);
        SanitizeChunkRings();
        ApplyWorldSettings();
        if (AutoRegister) Register();
        RefreshEntitySnapshot(OUTL_World.Instance, true, 0f);
    }

    private void OnDisable() { Unregister(); }

    private void OnValidate()
    {
        ChunkSize = Mathf.Max(1f, ChunkSize);
        DriverTickInterval = Mathf.Max(0.01f, DriverTickInterval);
        EntitiesPerTick = Mathf.Max(1, EntitiesPerTick);
        FullRefreshInterval = Mathf.Max(0.05f, FullRefreshInterval);
        SanitizeChunkRings();
    }

    public Vector2Int FocusChunk
    {
        get
        {
            ResolveFocus();
            return Focus != null ? WorldToChunk(Focus.position, ChunkSize) : Vector2Int.zero;
        }
    }

    [ContextMenu("Process All Entities Now")]
    public void ProcessAllNow()
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return;
        ResolveFocus();
        if (Focus == null) return;
        RefreshEntitySnapshot(world, true, world.WorldTime);
        Vector2Int fc = WorldToChunk(Focus.position, ChunkSize);
        for (int i = 0; i < entityBuffer.Count; i++) ProcessEntity(world, entityBuffer[i], fc, true);
    }

    [ContextMenu("Refresh Entity Snapshot")]
    public void RefreshSnapshotNow()
    {
        OUTL_World world = OUTL_World.Instance;
        RefreshEntitySnapshot(world, true, world != null ? world.WorldTime : Time.unscaledTime);
    }

    public void Register()
    {
        if (registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Register(this);
        registered = true;
    }

    public void Unregister()
    {
        if (!registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Unregister(this);
        registered = false;
    }

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        if (world == null) return;
        ResolveFocus();
        if (Focus == null) return;
        OUTL_ProcessingProfile profile = Profile;
        if (profile == null) return;
        profile.Sanitize();
        SanitizeChunkRings();
        ApplyWorldSettings();

        RefreshEntitySnapshot(world, false, time);
        UpdateParallelReadinessPreview(world, profile, time);
        if (entityBuffer.Count == 0) return;

        int budget = Mathf.Clamp(OverrideEntitiesPerTick ? EntitiesPerTick : profile.EntitiesPerTick, 1, entityBuffer.Count);
        Vector2Int fc = WorldToChunk(Focus.position, ChunkSize);
        LastProcessedCount = 0;
        LastTierChangeCount = 0;
        for (int n = 0; n < budget; n++)
        {
            if (cursor >= entityBuffer.Count)
            {
                cursor = 0;
                CompletedSweeps++;
            }
            if (ProcessEntity(world, entityBuffer[cursor++], fc, false)) LastTierChangeCount++;
            LastProcessedCount++;
        }
    }

    private void RefreshEntitySnapshot(OUTL_World world, bool force, float time)
    {
        if (world == null) return;
        int registryCount = world.Registry.Count;
        bool timerExpired = FullRefreshInterval > 0f && time >= nextSnapshotRefresh;
        if (!force && CacheRegistrySnapshot && registryCount == cachedRegistryCount && entityBuffer.Count > 0 && !timerExpired) return;

        world.Registry.CopyAll(entityBuffer);
        cachedRegistryCount = registryCount;
        nextSnapshotRefresh = time + Mathf.Max(0.05f, FullRefreshInterval);
        if (cursor >= entityBuffer.Count) cursor = 0;
    }

    private bool ProcessEntity(OUTL_World world, OUTL_EntityRuntime e, Vector2Int focusChunk, bool force)
    {
        if (e == null || e.Adapter == null || !e.Id.IsValid) return false;
        Vector2Int c = WorldToChunk(e.Adapter.transform.position, ChunkSize);
        int ring = RingDistance(c, focusChunk);
        OUTL_RuntimeTier next = EvaluateTier(ring);
        OUTL_RuntimeTier old = e.Tier;
        if (!force && next == old) return false;
        ApplyToEntity(e, Profile.GetSettings(next));
        if (WriteChunkState)
        {
            e.State.SetInt("Chunk.X", c.x);
            e.State.SetInt("Chunk.Z", c.y);
            e.State.SetInt("Chunk.Ring", ring);
            e.State.SetString("Chunk.Tier", next.ToString());
        }
        if (DebugLogTierChanges && old != next)
            OUTL_DebugLog.Log(OUTL_DebugChannel.Perf, "chunk tier entity=" + e.Id + " " + old + " -> " + next + " chunk=" + c + " ring=" + ring);
        return old != next;
    }

    private void UpdateParallelReadinessPreview(OUTL_World world, OUTL_ProcessingProfile profile, float time)
    {
        if (!BuildParallelReadinessSnapshot)
        {
            ClearParallelReadinessCounts();
            return;
        }

        OUTL_ParallelReadiness.BuildSnapshotFromEntities(entityBuffer, parallelBuffers, time, ChunkSize);
        ParallelSnapshotRowCount = parallelBuffers.SnapshotRowCount;
        ParallelAIDebugRowCount = parallelBuffers.DebugRowCount;

        if (CalculateParallelTierPreview && Focus != null && profile != null)
        {
            OUTL_ParallelReadiness.CalculateTierResults(parallelBuffers.ActorSnapshots, Focus.position, profile, parallelBuffers.TierResults);
            ParallelTierResultCount = parallelBuffers.TierResultCount;
            ParallelTierChangeCount = OUTL_ParallelReadiness.CountChangedTierResults(parallelBuffers.TierResults);
            OUTL_ParallelReadiness.ApplyResultsMainThread(world, parallelBuffers.TierResults, parallelBuffers.DecisionResults);
            return;
        }

        parallelBuffers.TierResults.Clear();
        ParallelTierResultCount = 0;
        ParallelTierChangeCount = 0;
    }

    private void ClearParallelReadinessCounts()
    {
        parallelBuffers.ClearRows();
        ParallelSnapshotRowCount = 0;
        ParallelTierResultCount = 0;
        ParallelTierChangeCount = 0;
        ParallelAIDebugRowCount = 0;
    }

    private OUTL_RuntimeTier EvaluateTier(int ring)
    {
        if (ring <= FullRadius) return OUTL_RuntimeTier.Full;
        if (ring <= NearRadius) return OUTL_RuntimeTier.Near;
        if (ring <= MidRadius) return OUTL_RuntimeTier.Mid;
        if (ring <= FarRadius) return OUTL_RuntimeTier.Far;
        return OUTL_RuntimeTier.Dormant;
    }

    private void ApplyToEntity(OUTL_EntityRuntime e, OUTL_TierProcessingSettings s)
    {
        OUTL_EntityAdapter a = e.Adapter;
        if (a == null) return;
        a.ApplyProcessingTier(s.Tier, s);
        OUTL_ProcessingProfile p = Profile;
        if (p.ApplyEntityTickInterval) a.TickInterval = Mathf.Max(0.01f, s.EntityTickInterval);
        if (p.ApplyRandomTick)
        {
            a.RegisterRandomTick = s.EnableRandomTick;
            a.RandomTickInterval = Mathf.Max(0.01f, s.RandomTickInterval);
            OUTL_World world = OUTL_World.Instance;
            if (world != null)
            {
                if (a.RegisterRandomTick) world.Scheduler.RegisterRandom(a);
                else world.Scheduler.UnregisterRandom(a);
            }
        }
        if (p.ApplyNavMeshMover)
        {
            OUTL_NavMeshMover m = ResolveNavMover(e);
            if (m != null)
            {
                m.TickInterval = Mathf.Max(0.001f, s.NavTickInterval);
                m.RepathInterval = Mathf.Max(0.02f, s.NavRepathInterval);
                m.AllowVisualUpdate = s.NavAllowVisualUpdate;
                m.SetOUTLTickMode(s.EnableNavTick);
                m.RefreshRuntimeMode();
                if ((s.StopNavOnEnterTier || (p.StopNavWhenDormant && s.Tier == OUTL_RuntimeTier.Dormant)) && m.HasDestination) m.Stop();
            }
        }
        OUTL_World w = OUTL_World.Instance;
        if (w != null) w.Sectors.RegisterOrUpdate(e);
    }

    private OUTL_NavMeshMover ResolveNavMover(OUTL_EntityRuntime entity)
    {
        if (entity == null || entity.Adapter == null || !entity.Id.IsValid) return null;

        OUTL_NavMeshMover mover;
        if (navMoverCache.TryGetValue(entity.Id.Value, out mover))
        {
            if (mover != null && mover.gameObject == entity.Adapter.gameObject) return mover;
            navMoverCache.Remove(entity.Id.Value);
        }

        mover = entity.Adapter.GetComponent<OUTL_NavMeshMover>();
        if (mover != null) navMoverCache[entity.Id.Value] = mover;
        return mover;
    }

    private void ApplyWorldSettings()
    {
        OUTL_World world = OUTL_World.Instance;
        if (world != null) world.Sectors.SetCellSize(ChunkSize);
    }

    private void ResolveFocus()
    {
        if (Focus != null) return;
        if (!UseRegistryFocusFallback) return;
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return;

        OUTL_EntityRuntime runtime = null;
        if (!string.IsNullOrEmpty(FocusTargetName))
            runtime = world.Registry.FindFirstByTargetName(FocusTargetName);
        if (runtime == null && !string.IsNullOrEmpty(FocusClassName))
            runtime = world.Registry.FindFirstByClassName(FocusClassName);
        if (runtime != null && runtime.Adapter != null)
            Focus = runtime.Adapter.transform;
    }

    private void SanitizeChunkRings()
    {
        if (EnforceCanonicalThreeByThree)
        {
            FullRadius = 0;
            NearRadius = 1;
            MidRadius = 2;
            FarRadius = 3;
            return;
        }

        FullRadius = Mathf.Max(0, FullRadius);
        NearRadius = Mathf.Max(FullRadius, NearRadius);
        MidRadius = Mathf.Max(NearRadius, MidRadius);
        FarRadius = Mathf.Max(MidRadius, FarRadius);
    }

    public int RingDistance(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dz = Mathf.Abs(a.y - b.y);
        return UseChebyshevDistance ? Mathf.Max(dx, dz) : dx + dz;
    }

    public static Vector2Int WorldToChunk(Vector3 p, float size) { return new Vector2Int(Mathf.FloorToInt(p.x / Mathf.Max(1f, size)), Mathf.FloorToInt(p.z / Mathf.Max(1f, size))); }
    public static Vector3 ChunkCenter(Vector2Int c, float size, float y) { return new Vector3((c.x + 0.5f) * size, y, (c.y + 0.5f) * size); }
}
