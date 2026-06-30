using UnityEngine;

[DisallowMultipleComponent]
public class OUTL_EntityAdapter : MonoBehaviour, OUTL_ITickable, OUTL_IRandomTickable, OUTL_IRandomTickIntervalProvider, OUTL_ICommandReceiver, OUTL_ISaveState, OUTL_IPoolReset
{
    [Tooltip("Optional data definition for this entity. Supplies base stats, tags, class name, modules and data-driven actions.")]
    public OUTL_EntityDef Def;

    [Tooltip("Optional faction used by OUTL_FactionSystem, AI target selection and hostile/friendly checks.")]
    public OUTL_FactionDef Faction;

    [Header("OUTL Addressing")]
    [Tooltip("Optional logical class name for this entity. Leave empty to use the class name from Entity Def. Used by registry, debug graph, validators, save/load and future network authority.")]
    public string ClassNameOverride;

    [Tooltip("Runtime address of this entity. OutputLinks and OUTL_Commands resolve this through OUTL_World.Registry and OUTL_CommandSystem. This is the native OUT CORE Lite wiring path.")]
    public string TargetName;

    [Tooltip("Default outgoing TargetName for entity logic that needs a conventional receiver address. Runtime dispatch should still go through OUTL_OutputLink -> OUTL_CommandSystem, not direct component references.")]
    public string Target;

    [Tooltip("Optional outgoing TargetName intended for remove/disable/despawn style logic. Resolve it through OUTL_CommandSystem; do not use direct deletion or SendMessage as runtime canon.")]
    public string KillTarget;

    [Header("Persistence")]
    [Tooltip("Stable save/network key. Required for persistent entities that must survive save/load without depending on runtime instance ids.")]
    public string StableId;

    [Tooltip("When true, OUTL_SaveSystem captures this entity. Persistent entities should have StableId set.")]
    public bool SavePersistent = true;

    [Tooltip("Reserved hook for future restore-by-definition if the saved entity is missing from the loaded scene.")]
    public bool RestoreSpawnIfMissing;

    [Header("Runtime")]
    [Tooltip("Current processing tier. Usually driven by the canonical OUTL_ChunkProcessingDriver.")]
    public OUTL_RuntimeTier Tier = OUTL_RuntimeTier.Full;

    [Tooltip("If true, this adapter registers itself with OUTL_World when enabled.")]
    public bool RegisterOnEnable = true;

    [Tooltip("If true, this adapter is registered as an OUTL tickable for address/sector/runtime maintenance.")]
    public bool RegisterTick = true;

    [Tooltip("If true, OUTL_World.Scheduler can call OUTL_RandomTick on this adapter for data-driven random effects.")]
    public bool RegisterRandomTick = false;

    [Tooltip("If true, this entity is indexed in OUTL_SectorGrid for nearby queries, AI and processing distance logic.")]
    public bool RegisterInSectors = true;

    [Tooltip("If true, command receivers on child objects are cached and called by OUTL_CommandSystem.")]
    public bool IncludeChildCommandReceivers = true;

    [Tooltip("Scheduler lane used for this adapter maintenance tick.")]
    public OUTL_TickLane TickLane = OUTL_TickLane.Logic;

    [Tooltip("Interval for this adapter maintenance tick. Processing profiles may override it by tier.")]
    public float TickInterval = 0.25f;

    [Tooltip("Interval for this adapter random tick when Register Random Tick is enabled. Processing profiles may override it by tier.")]
    public float RandomTickInterval = 2f;

    private OUTL_EntityRuntime runtime;
    private OUTL_EntityId id;
    private OUTL_ICommandReceiver[] cachedCommandReceivers;
    private OUTL_ICommandGuard[] cachedCommandGuards;
    private OUTL_IProcessingTierReceiver[] cachedTierReceivers;
    private string cachedClassNameOverride;
    private string cachedTargetName;
    private string cachedTarget;
    private string cachedKillTarget;
    private string cachedStableId;
    private bool cachedSavePersistent;
    private OUTL_EntityDef cachedDef;
    private OUTL_FactionDef cachedFaction;
    private OUTL_RuntimeTier cachedTier;
    private bool addressDirty = true;

    public OUTL_EntityId Id { get { return id; } }
    public OUTL_EntityRuntime Runtime { get { return runtime; } }
    public OUTL_ICommandReceiver[] CommandReceivers { get { if (cachedCommandReceivers == null) RebuildCommandReceiverCache(); return cachedCommandReceivers; } }
    public OUTL_ICommandGuard[] CommandGuards { get { if (cachedCommandGuards == null) RebuildCommandGuardCache(); return cachedCommandGuards; } }
    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled && runtime != null; } }
    public OUTL_TickLane OUTL_TickLane { get { return TickLane; } }
    public float OUTL_TickInterval { get { return Mathf.Max(0.01f, TickInterval); } }
    public bool OUTL_IsRandomTickEnabled { get { return isActiveAndEnabled && runtime != null; } }
    public float OUTL_RandomTickInterval { get { return Mathf.Max(0.01f, RandomTickInterval); } }

    private void Awake()
    {
        RebuildCommandReceiverCache();
        RebuildCommandGuardCache();
        RebuildProcessingTierReceiverCache();
        CaptureAddressSnapshot();
    }

    private void OnEnable()
    {
        RebuildCommandReceiverCache();
        RebuildCommandGuardCache();
        RebuildProcessingTierReceiverCache();
        if (!RegisterOnEnable || OUTL_World.Instance == null) return;

        if (runtime == null) RegisterNow(OUTL_World.Instance);
        else RebindRuntime(OUTL_World.Instance);
    }

    private void OnDisable()
    {
        UnregisterRuntime();
    }

    private void OnValidate()
    {
        MarkAddressDirty();
    }

    private void OnTransformChildrenChanged()
    {
        RebuildCommandReceiverCache();
        RebuildCommandGuardCache();
        RebuildProcessingTierReceiverCache();
    }

    public void MarkAddressDirty()
    {
        addressDirty = true;
    }

    public void RebuildCommandReceiverCache()
    {
        OUTL_ICommandReceiver[] all = IncludeChildCommandReceivers ? GetComponentsInChildren<OUTL_ICommandReceiver>(true) : GetComponents<OUTL_ICommandReceiver>();
        int count = 0;
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && !ReferenceEquals(all[i], this)) count++;

        cachedCommandReceivers = new OUTL_ICommandReceiver[count];
        int write = 0;
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && !ReferenceEquals(all[i], this)) cachedCommandReceivers[write++] = all[i];
    }

    public void RebuildCommandGuardCache()
    {
        OUTL_ICommandGuard[] all = IncludeChildCommandReceivers ? GetComponentsInChildren<OUTL_ICommandGuard>(true) : GetComponents<OUTL_ICommandGuard>();
        int count = 0;
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null) count++;

        cachedCommandGuards = new OUTL_ICommandGuard[count];
        int write = 0;
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null) cachedCommandGuards[write++] = all[i];
    }

    public void RegisterNow(OUTL_World world)
    {
        if (world == null) return;
        if (runtime != null)
        {
            RebindRuntime(world);
            return;
        }

        runtime = world.Registry.Register(Def, this);
        id = runtime.Id;
        addressDirty = true;
        RebindRuntime(world);
    }

    public void RebindRuntime(OUTL_World world)
    {
        if (world == null || runtime == null) return;
        runtime.Tier = Tier;
        runtime.Faction = Faction;
        runtime.Def = Def;
        runtime.Adapter = this;
        runtime.Tags = Def != null ? Def.Tags : runtime.Tags;
        ReindexAddressIfDirty(world);
        if (RegisterInSectors) world.Sectors.RegisterOrUpdate(runtime);
        world.WorldLedger.RegisterOrUpdateEntity(runtime, transform.position, world.WorldTime);
        if (RegisterTick) world.Scheduler.Register(this);
        else world.Scheduler.Unregister(this);
        if (RegisterRandomTick) world.Scheduler.RegisterRandom(this);
        else world.Scheduler.UnregisterRandom(this);
        CaptureRuntimeSnapshot();
    }

    public void ClearRuntimeRegistration()
    {
        runtime = null;
        id = OUTL_EntityId.None;
        addressDirty = true;
    }

    public bool OUTL_CanReceive(in OUTL_Command command, OUTL_World world)
    {
        return false;
    }

    public void OUTL_Receive(in OUTL_Command command, OUTL_World world)
    {
    }

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        if (runtime == null || world == null) return;
        runtime.Tier = Tier;
        runtime.Faction = Faction;
        runtime.Def = Def;
        runtime.Tags = Def != null ? Def.Tags : runtime.Tags;
        if (AddressSnapshotChanged()) addressDirty = true;
        ReindexAddressIfDirty(world);
        if (RegisterInSectors) world.Sectors.RegisterOrUpdate(runtime);
        world.WorldLedger.RegisterOrUpdateEntity(runtime, transform.position, world.WorldTime);
        CaptureRuntimeSnapshot();
    }

    public void OUTL_RandomTick(OUTL_World world, float time)
    {
        if (runtime == null || runtime.Def == null || runtime.Def.Modules == null) return;
        for (int i = 0; i < runtime.Def.Modules.Length; i++)
        {
            OUTL_ModuleDef module = runtime.Def.Modules[i];
            if (module != null) world.Effects.ApplyAll(module.OnRandomTickEffects, id, id, transform.position);
        }
        world.Events.Emit(new OUTL_Event(OUTL_EventType.RandomTick, id, id) { Point = transform.position });
    }

    public void OUTL_Capture(OUTL_SaveData data)
    {
        data.Set("id", id.Value.ToString());
        data.Set("stableId", StableId);
        data.Set("className", !string.IsNullOrEmpty(ClassNameOverride) ? ClassNameOverride : (Def != null ? Def.ClassName : string.Empty));
        data.Set("targetName", TargetName);
        data.Set("target", Target);
        data.Set("killTarget", KillTarget);
        data.Set("savePersistent", SavePersistent ? "1" : "0");
        data.Set("def", Def != null ? Def.name : string.Empty);
        data.Set("faction", Faction != null ? Faction.name : string.Empty);
        data.Set("tier", Tier.ToString());
        data.Set("pos", string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1},{2}", transform.position.x, transform.position.y, transform.position.z));
    }

    public void RebuildProcessingTierReceiverCache()
    {
        cachedTierReceivers = GetComponentsInChildren<OUTL_IProcessingTierReceiver>(true);
    }

    public void ApplyProcessingTier(OUTL_RuntimeTier nextTier, in OUTL_TierProcessingSettings settings)
    {
        ApplyProcessingTier(nextTier, settings, false);
    }

    public void ApplyProcessingTier(OUTL_RuntimeTier nextTier, in OUTL_TierProcessingSettings settings, bool forceNotify)
    {
        OUTL_RuntimeTier oldTier = Tier;
        Tier = nextTier;
        if (runtime != null) runtime.Tier = nextTier;
        if (oldTier == nextTier && !forceNotify) return;

        if (cachedTierReceivers == null) RebuildProcessingTierReceiverCache();
        for (int i = 0; i < cachedTierReceivers.Length; i++)
        {
            OUTL_IProcessingTierReceiver receiver = cachedTierReceivers[i];
            if (receiver != null) receiver.OUTL_OnProcessingTierChanged(oldTier, nextTier, settings);
        }
    }

    public void RefreshProcessingTierState()
    {
        OUTL_TierProcessingSettings settings = default(OUTL_TierProcessingSettings);
        settings.Tier = Tier;
        ApplyProcessingTier(Tier, settings, true);
    }

    public void OUTL_Restore(OUTL_SaveData data)
    {
        if (data == null) return;
        StableId = data.Get("stableId", StableId);
        ClassNameOverride = data.Get("className", ClassNameOverride);
        TargetName = data.Get("targetName", TargetName);
        Target = data.Get("target", Target);
        KillTarget = data.Get("killTarget", KillTarget);
        SavePersistent = data.Get("savePersistent", SavePersistent ? "1" : "0") != "0";

        OUTL_RuntimeTier tier;
        if (System.Enum.TryParse(data.Get("tier", Tier.ToString()), true, out tier)) Tier = tier;

        Vector3 position;
        if (TryParseVector3(data.Get("pos"), out position)) transform.position = position;

        MarkAddressDirty();
        if (OUTL_World.Instance != null)
        {
            if (runtime == null) RegisterNow(OUTL_World.Instance);
            else RebindRuntime(OUTL_World.Instance);
        }
    }

    public void OUTL_OnPoolSpawn()
    {
        RebuildCommandReceiverCache();
        RebuildCommandGuardCache();
        RebuildProcessingTierReceiverCache();
        addressDirty = true;
        if (RegisterOnEnable && gameObject.activeInHierarchy && OUTL_World.Instance != null && runtime == null)
            RegisterNow(OUTL_World.Instance);
    }

    public void OUTL_OnPoolRelease()
    {
        UnregisterRuntime();
    }

    private void UnregisterRuntime()
    {
        OUTL_World world = OUTL_World.Instance;
        if (world != null && runtime != null)
        {
            world.Scheduler.Unregister(this);
            world.Scheduler.UnregisterRandom(this);
            if (RegisterInSectors) world.Sectors.Unregister(id);
            world.WorldLedger.RemoveEntity(id);
            world.Registry.Unregister(id);
        }
        ClearRuntimeRegistration();
    }

    private void ReindexAddressIfDirty(OUTL_World world)
    {
        if (!addressDirty || world == null || runtime == null) return;
        world.Registry.ReindexAddress(runtime);
        addressDirty = false;
        CaptureAddressSnapshot();
    }

    private bool AddressSnapshotChanged()
    {
        return cachedClassNameOverride != ClassNameOverride || cachedTargetName != TargetName || cachedTarget != Target || cachedKillTarget != KillTarget || cachedStableId != StableId || cachedSavePersistent != SavePersistent || cachedDef != Def;
    }

    private void CaptureAddressSnapshot()
    {
        cachedClassNameOverride = ClassNameOverride;
        cachedTargetName = TargetName;
        cachedTarget = Target;
        cachedKillTarget = KillTarget;
        cachedStableId = StableId;
        cachedSavePersistent = SavePersistent;
        cachedDef = Def;
    }

    private void CaptureRuntimeSnapshot()
    {
        cachedFaction = Faction;
        cachedTier = Tier;
    }

    private static bool TryParseVector3(string value, out Vector3 result)
    {
        result = Vector3.zero;
        if (string.IsNullOrEmpty(value)) return false;
        string[] parts = value.Split(',');
        if (parts.Length != 3) return false;
        float x;
        float y;
        float z;
        if (!float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x)) return false;
        if (!float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out y)) return false;
        if (!float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out z)) return false;
        result = new Vector3(x, y, z);
        return true;
    }
}
