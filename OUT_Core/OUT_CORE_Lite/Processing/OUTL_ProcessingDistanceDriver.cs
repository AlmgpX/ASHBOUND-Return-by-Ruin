using System;
using System.Collections.Generic;
using UnityEngine;

[Obsolete("Use OUTL_ChunkProcessingDriver as the canonical sector/ring processing driver. This distance driver remains only as a simple legacy fallback.")]
[DisallowMultipleComponent]
public class OUTL_ProcessingDistanceDriver : MonoBehaviour, OUTL_ITickable
{
    public OUTL_ProcessingProfileAsset ProfileAsset;
    public OUTL_ProcessingBuiltInPreset BuiltInPreset = OUTL_ProcessingBuiltInPreset.StreamingWorld;
    public bool UseAssetProfile = false;
    public bool ApplyPresetOnEnable = true;
    public Transform Focus;
    [Tooltip("Legacy compatibility field. Runtime scene camera lookup is intentionally not used by the canonical focus resolver.")]
    public bool UseMainCameraAsFocus = false;
    [Tooltip("Legacy compatibility field. Runtime Unity tag lookup is intentionally not used by the canonical focus resolver.")]
    public bool UsePlayerTagAsFallback = false;
    public bool UseRegistryFocusFallback = true;
    public string FocusTargetName = "player";
    public string FocusClassName = "player";
    public bool AutoRegister = false;
    public bool DebugLogTierChanges;

    [Header("Overrides")]
    public bool OverrideDriverTickInterval;
    public float DriverTickInterval = 0.25f;
    public bool OverrideEntitiesPerTick;
    public int EntitiesPerTick = 128;

    private readonly OUTL_ProcessingProfile runtimeProfile = new OUTL_ProcessingProfile();
    private readonly List<OUTL_EntityRuntime> entityBuffer = new List<OUTL_EntityRuntime>(1024);
    private readonly Dictionary<int, OUTL_NavMeshMover> navMoverCache = new Dictionary<int, OUTL_NavMeshMover>(1024);
    private int cursor;
    private bool registered;

    public OUTL_ProcessingProfile Profile { get { return UseAssetProfile && ProfileAsset != null && ProfileAsset.Profile != null ? ProfileAsset.Profile : runtimeProfile; } }
    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled && OUTL_World.Instance != null; } }
    public OUTL_TickLane OUTL_TickLane { get { return OUTL_TickLane.Custom; } }
    public float OUTL_TickInterval { get { return Mathf.Max(0.01f, OverrideDriverTickInterval ? DriverTickInterval : Profile.DriverTickInterval); } }

    private void Awake()
    {
        ResolveFocus();
        if (!UseAssetProfile) runtimeProfile.ApplyBuiltIn(BuiltInPreset);
    }

    private void OnEnable()
    {
        if (!UseAssetProfile && ApplyPresetOnEnable) runtimeProfile.ApplyBuiltIn(BuiltInPreset);
        ApplyWorldSettings();
        if (AutoRegister) Register();
    }

    private void OnDisable() { Unregister(); }

    private void OnValidate()
    {
        DriverTickInterval = Mathf.Max(0.01f, DriverTickInterval);
        EntitiesPerTick = Mathf.Max(1, EntitiesPerTick);
        if (!UseAssetProfile && runtimeProfile != null) runtimeProfile.Sanitize();
    }

    [ContextMenu("Apply Built-In Preset Now")]
    public void ApplyBuiltInPresetNow()
    {
        UseAssetProfile = false;
        runtimeProfile.ApplyBuiltIn(BuiltInPreset);
        ApplyWorldSettings();
    }

    [ContextMenu("Process All Entities Now")]
    public void ProcessAllNow()
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return;
        ResolveFocus();
        if (Focus == null) return;
        world.Registry.CopyAll(entityBuffer);
        for (int i = 0; i < entityBuffer.Count; i++) ProcessEntity(world, entityBuffer[i], Focus.position, true);
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
        if (profile.ApplySectorCellSize) world.Sectors.SetCellSize(profile.SectorCellSize);
        world.Registry.CopyAll(entityBuffer);
        if (entityBuffer.Count == 0) return;
        int budget = Mathf.Clamp(OverrideEntitiesPerTick ? EntitiesPerTick : profile.EntitiesPerTick, 1, entityBuffer.Count);
        Vector3 focusPos = Focus.position;
        for (int n = 0; n < budget; n++)
        {
            if (cursor >= entityBuffer.Count) cursor = 0;
            ProcessEntity(world, entityBuffer[cursor++], focusPos, false);
        }
    }

    private void ProcessEntity(OUTL_World world, OUTL_EntityRuntime entity, Vector3 focusPos, bool force)
    {
        if (entity == null || entity.Adapter == null || !entity.Id.IsValid) return;
        OUTL_ProcessingProfile profile = Profile;
        if (profile == null) return;
        Vector3 delta = entity.Adapter.transform.position - focusPos;
        OUTL_RuntimeTier next = profile.EvaluateTier(delta.sqrMagnitude);
        OUTL_RuntimeTier old = entity.Tier;
        if (!force && old == next) return;
        ApplyToEntity(entity, profile.GetSettings(next), profile);
        if (DebugLogTierChanges && old != next) OUTL_DebugLog.Log(OUTL_DebugChannel.Perf, "legacy distance processing tier entity=" + entity.Id + " " + old + " -> " + next);
    }

    private void ApplyToEntity(OUTL_EntityRuntime entity, OUTL_TierProcessingSettings settings, OUTL_ProcessingProfile profile)
    {
        OUTL_EntityAdapter adapter = entity.Adapter;
        if (adapter == null) return;
        entity.Tier = settings.Tier;
        adapter.Tier = settings.Tier;
        if (profile.ApplyEntityTickInterval) adapter.TickInterval = Mathf.Max(0.01f, settings.EntityTickInterval);
        if (profile.ApplyRandomTick)
        {
            adapter.RegisterRandomTick = settings.EnableRandomTick;
            adapter.RandomTickInterval = Mathf.Max(0.01f, settings.RandomTickInterval);
            OUTL_World world = OUTL_World.Instance;
            if (world != null)
            {
                if (adapter.RegisterRandomTick) world.Scheduler.RegisterRandom(adapter);
                else world.Scheduler.UnregisterRandom(adapter);
            }
        }
        if (profile.ApplyNavMeshMover)
        {
            OUTL_NavMeshMover mover = ResolveNavMover(entity);
            if (mover != null)
            {
                mover.TickInterval = Mathf.Max(0.001f, settings.NavTickInterval);
                mover.RepathInterval = Mathf.Max(0.02f, settings.NavRepathInterval);
                mover.AllowVisualUpdate = settings.NavAllowVisualUpdate;
                mover.SetOUTLTickMode(settings.EnableNavTick);
                mover.RefreshRuntimeMode();
                if ((settings.StopNavOnEnterTier || (profile.StopNavWhenDormant && settings.Tier == OUTL_RuntimeTier.Dormant)) && mover.HasDestination) mover.Stop();
            }
        }
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
        OUTL_ProcessingProfile profile = Profile;
        if (world == null || profile == null) return;
        profile.Sanitize();
        if (profile.ApplySectorCellSize) world.Sectors.SetCellSize(profile.SectorCellSize);
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
}
