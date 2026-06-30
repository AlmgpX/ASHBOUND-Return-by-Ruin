using System.Collections.Generic;
using UnityEngine;

public enum OUTL_WorldUpdateMode
{
    UnityUpdate = 0,
    UnityFixedUpdate = 1,
    CustomFixedStep = 2,
    Manual = 3
}

public enum OUTL_WorldTimeSource
{
    ScaledUnityTime = 0,
    UnscaledUnityTime = 1
}

[DefaultExecutionOrder(-9500)]
[DisallowMultipleComponent]
public class OUTL_World : MonoBehaviour
{
    public static OUTL_World Instance { get; private set; }

    private const float MinTickInterval = 0.001f;
    private const int MaxCatchUpTicksPerFrame = 8;

    [Header("Time")]
    public OUTL_WorldUpdateMode UpdateMode = OUTL_WorldUpdateMode.CustomFixedStep;
    public OUTL_WorldTimeSource TimeSource = OUTL_WorldTimeSource.UnscaledUnityTime;
    public bool Paused = false;
    [Tooltip("When true, OUTL_World.Paused also sets Unity Time.timeScale to 0. Use this if legacy Update/NavMesh/Animator code must stop too.")]
    public bool DriveUnityTimeScaleOnPause = true;
    public bool FlushEventsAndDespawnsWhilePaused = true;
    public float TimeScale = 1f;
    [Tooltip("Simulation step used by CustomFixedStep mode. This is the OUTL clock step, not Unity fixedDeltaTime.")]
    public float SimulationStep = 0.05f;
    [Tooltip("Maximum OUTL simulation steps processed during one Unity frame. Prevents death spirals after stalls.")]
    public int MaxSimulationStepsPerFrame = 4;
    public float LogicTickInterval = 0.1f;
    public float AITickInterval = 0.2f;
    public float QuestTickInterval = 1f;
    public float CustomTickInterval = 0.1f;
    public float RandomTickInterval = 0.25f;
    public int RandomTickBudget = 64;

    [Header("World Time")]
    public float DayLengthSeconds = 1440f;
    public OUTL_DayPhase CurrentDayPhase = OUTL_DayPhase.Day;

    [Header("Tick Profile")]
    public OUTL_TickProfile TickProfile;
    public bool ApplyTickProfileOnAwake = true;
    public float StimulusTickInterval = 0.25f;
    public int MaxStimuliProcessedPerFrame = 256;
    public int MaxEgregoreSignalsPerFrame = 64;
    public int MaxSectorUpdatesPerFrame = 256;
    public int MaxNpcBehaviorTicksPerFrame = 128;
    public int MaxNpcRouteUpdatesPerFrame = 64;
    public int MaxNpcPathRequestsPerFrame = 16;
    public int MaxNpcStimulusInterruptsPerFrame = 64;

    [Header("Physics")]
    public bool ApplySvGravityToPhysics = true;

    [Header("Lifecycle")]
    public bool DontDestroy = false;
    public bool AutoFindAdaptersOnStart = true;
    public int DespawnBudgetPerFrame = 64;

    [Header("Materialization")]
    public bool EnableAutomaticMaterialization = false;
    public float MaterializationTickInterval = 0.5f;
    public int MaterializationBudgetPerTick = 8;
    public float MaterializeEnterDistance = 42f;
    public float DematerializeExitDistance = 72f;
    public string MaterializationFocusTargetName = "skeleton.player";

    [Header("Abstract Encounters")]
    public float AbstractEncounterTickInterval = 2f;
    public int MaxAbstractEncountersPerTick = 4;
    [Range(0f, 1f)] public float AbstractEncounterDangerThreshold = 0.55f;

    [Header("Debug")]
    public bool DebugStats;

    private readonly OUTL_Registry registry = new OUTL_Registry();
    private readonly OUTL_EventBus eventBus = new OUTL_EventBus();
    private readonly OUTL_CommandSystem commandSystem = new OUTL_CommandSystem();
    private readonly OUTL_EffectSystem effectSystem = new OUTL_EffectSystem();
    private readonly OUTL_InventorySystem inventorySystem = new OUTL_InventorySystem();
    private readonly OUTL_QuestSystem questSystem = new OUTL_QuestSystem();
    private readonly OUTL_Scheduler scheduler = new OUTL_Scheduler();
    private readonly OUTL_SectorGrid sectorGrid = new OUTL_SectorGrid();
    private readonly OUTL_WorldLedger worldLedger = new OUTL_WorldLedger();
    private readonly OUTL_WorldRouteGraph worldRouteGraph = new OUTL_WorldRouteGraph();
    private readonly OUTL_RouteCache worldRouteCache = new OUTL_RouteCache();
    private readonly OUTL_MaterializationSystem materializationSystem = new OUTL_MaterializationSystem();
    private readonly OUTL_AbstractEncounterSystem abstractEncounterSystem = new OUTL_AbstractEncounterSystem();
    private readonly OUTL_FactionSystem factionSystem = new OUTL_FactionSystem();
    private readonly OUTL_SaveSystem saveSystem = new OUTL_SaveSystem();
    private readonly List<OUTL_EntityId> despawnQueue = new List<OUTL_EntityId>(128);

    private float worldTime;
    private float unscaledWorldTime;
    private float deltaTime;
    private float simulationAcc;
    private float logicAcc;
    private float aiAcc;
    private float questAcc;
    private float customAcc;
    private float randomAcc;
    private float stimulusAcc;
    private float materializationAcc;
    private float abstractEncounterAcc;
    private OUTL_DayPhase lastDayPhase = OUTL_DayPhase.Day;
    private bool lastPaused;
    private bool unityTimeScaleCaptured;
    private float capturedUnityTimeScale = 1f;

    public OUTL_Registry Registry { get { return registry; } }
    public OUTL_EventBus Events { get { return eventBus; } }
    public OUTL_CommandSystem Commands { get { return commandSystem; } }
    public OUTL_EffectSystem Effects { get { return effectSystem; } }
    public OUTL_InventorySystem Inventory { get { return inventorySystem; } }
    public OUTL_QuestSystem Quests { get { return questSystem; } }
    public OUTL_Scheduler Scheduler { get { return scheduler; } }
    public OUTL_SectorGrid Sectors { get { return sectorGrid; } }
    public OUTL_WorldLedger WorldLedger { get { return worldLedger; } }
    public OUTL_WorldRouteGraph WorldRouteGraph { get { return worldRouteGraph; } }
    public OUTL_RouteCache WorldRouteCache { get { return worldRouteCache; } }
    public OUTL_MaterializationSystem Materialization { get { return materializationSystem; } }
    public OUTL_AbstractEncounterSystem AbstractEncounters { get { return abstractEncounterSystem; } }
    public OUTL_FactionSystem Factions { get { return factionSystem; } }
    public OUTL_SaveSystem Save { get { return saveSystem; } }
    public float WorldTime { get { return worldTime; } }
    public float UnscaledWorldTime { get { return unscaledWorldTime; } }
    public float DeltaTime { get { return deltaTime; } }
    public bool IsPaused { get { return Paused || TimeScale <= 0f; } }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (ApplyTickProfileOnAwake && TickProfile != null) ApplyTickProfile(TickProfile);
        worldTime = UnityEngine.Time.time;
        unscaledWorldTime = UnityEngine.Time.unscaledTime;
        CurrentDayPhase = ResolveDayPhase(worldTime);
        lastDayPhase = CurrentDayPhase;
        lastPaused = IsPaused;
        if (DontDestroy) DontDestroyOnLoad(gameObject);
        EnsurePoolSystem();
        BindSystems();
        ApplyGlobalGravity();
        SyncUnityPauseState();
    }

    private void Start()
    {
        if (AutoFindAdaptersOnStart)
        {
            OUTL_EntityAdapter[] adapters = FindObjectsOfType<OUTL_EntityAdapter>(true);
            for (int i = 0; i < adapters.Length; i++)
                if (adapters[i] != null && adapters[i].isActiveAndEnabled)
                    adapters[i].RegisterNow(this);
        }
    }

    private void OnValidate()
    {
        TimeScale = Mathf.Max(0f, TimeScale);
        SimulationStep = Mathf.Max(MinTickInterval, SimulationStep);
        MaxSimulationStepsPerFrame = Mathf.Max(1, MaxSimulationStepsPerFrame);
        LogicTickInterval = Mathf.Max(MinTickInterval, LogicTickInterval);
        AITickInterval = Mathf.Max(MinTickInterval, AITickInterval);
        QuestTickInterval = Mathf.Max(MinTickInterval, QuestTickInterval);
        CustomTickInterval = Mathf.Max(MinTickInterval, CustomTickInterval);
        RandomTickInterval = Mathf.Max(MinTickInterval, RandomTickInterval);
        RandomTickBudget = Mathf.Max(0, RandomTickBudget);
        DayLengthSeconds = Mathf.Max(60f, DayLengthSeconds);
        StimulusTickInterval = Mathf.Max(MinTickInterval, StimulusTickInterval);
        MaxStimuliProcessedPerFrame = Mathf.Max(0, MaxStimuliProcessedPerFrame);
        MaxEgregoreSignalsPerFrame = Mathf.Max(0, MaxEgregoreSignalsPerFrame);
        MaxSectorUpdatesPerFrame = Mathf.Max(0, MaxSectorUpdatesPerFrame);
        MaxNpcBehaviorTicksPerFrame = Mathf.Max(0, MaxNpcBehaviorTicksPerFrame);
        MaxNpcRouteUpdatesPerFrame = Mathf.Max(0, MaxNpcRouteUpdatesPerFrame);
        MaxNpcPathRequestsPerFrame = Mathf.Max(0, MaxNpcPathRequestsPerFrame);
        MaxNpcStimulusInterruptsPerFrame = Mathf.Max(0, MaxNpcStimulusInterruptsPerFrame);
        MaterializationTickInterval = Mathf.Max(MinTickInterval, MaterializationTickInterval);
        MaterializationBudgetPerTick = Mathf.Max(0, MaterializationBudgetPerTick);
        MaterializeEnterDistance = Mathf.Max(0f, MaterializeEnterDistance);
        DematerializeExitDistance = Mathf.Max(MaterializeEnterDistance, DematerializeExitDistance);
        AbstractEncounterTickInterval = Mathf.Max(MinTickInterval, AbstractEncounterTickInterval);
        MaxAbstractEncountersPerTick = Mathf.Max(0, MaxAbstractEncountersPerTick);
        AbstractEncounterDangerThreshold = Mathf.Clamp01(AbstractEncounterDangerThreshold);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (DriveUnityTimeScaleOnPause && unityTimeScaleCaptured)
        {
            UnityEngine.Time.timeScale = capturedUnityTimeScale <= 0f ? 1f : capturedUnityTimeScale;
            unityTimeScaleCaptured = false;
        }
    }

    private void BindSystems()
    {
        eventBus.Bind(this);
        commandSystem.Bind(this);
        effectSystem.Bind(this);
        inventorySystem.Bind(this);
        questSystem.Bind(this);
        scheduler.Bind(this);
        sectorGrid.Bind(this);
        factionSystem.Bind(this);
        saveSystem.Bind(this);
        materializationSystem.Bind(this);
        abstractEncounterSystem.Bind(this);
    }

    private void Update()
    {
        SyncUnityPauseState();
        if (UpdateMode == OUTL_WorldUpdateMode.UnityUpdate || UpdateMode == OUTL_WorldUpdateMode.CustomFixedStep)
            AdvanceFromUnityDelta(ReadUnityDelta());
    }

    private void FixedUpdate()
    {
        SyncUnityPauseState();
        if (UpdateMode == OUTL_WorldUpdateMode.UnityFixedUpdate)
            AdvanceFromUnityDelta(ReadUnityDelta());
    }

    public void ManualAdvance(float externalDeltaTime)
    {
        SyncUnityPauseState();
        if (UpdateMode != OUTL_WorldUpdateMode.Manual) return;
        AdvanceFromUnityDelta(Mathf.Max(0f, externalDeltaTime));
    }

    public void SetPaused(bool paused)
    {
        Paused = paused;
        if (paused)
        {
            deltaTime = 0f;
            simulationAcc = 0f;
        }
        SyncUnityPauseState();
    }

    public void TogglePaused()
    {
        SetPaused(!Paused);
    }

    public void StepOnce(float step = -1f)
    {
        float dt = step > 0f ? step : SafeInterval(SimulationStep);
        RunFrameBegin();
        ApplyGlobalGravity();
        TickWorldStep(dt);
        FlushEndOfFrame();
    }

    private void AdvanceFromUnityDelta(float sourceDelta)
    {
        using (OUTL_Profile.WorldUpdate.Auto())
        {
            RunFrameBegin();
            ApplyGlobalGravity();

            if (IsPaused)
            {
                deltaTime = 0f;
                if (FlushEventsAndDespawnsWhilePaused) FlushEndOfFrame();
                return;
            }

            float scaledDelta = sourceDelta * Mathf.Max(0f, TimeScale);
            if (scaledDelta <= 0f)
            {
                deltaTime = 0f;
                if (FlushEventsAndDespawnsWhilePaused) FlushEndOfFrame();
                return;
            }

            if (UpdateMode == OUTL_WorldUpdateMode.CustomFixedStep)
                AdvanceFixedSteps(scaledDelta);
            else
                TickWorldStep(scaledDelta);

            FlushEndOfFrame();
        }
    }

    private void AdvanceFixedSteps(float scaledDelta)
    {
        float step = SafeInterval(SimulationStep);
        simulationAcc += scaledDelta;
        int maxSteps = Mathf.Max(1, MaxSimulationStepsPerFrame);
        int steps = 0;

        while (simulationAcc >= step && steps < maxSteps)
        {
            simulationAcc -= step;
            TickWorldStep(step);
            steps++;
        }

        if (steps >= maxSteps) simulationAcc = 0f;
    }

    private void TickWorldStep(float stepDelta)
    {
        deltaTime = Mathf.Max(0f, stepDelta);
        if (deltaTime <= 0f) return;

        unscaledWorldTime = UnityEngine.Time.unscaledTime;
        worldTime += deltaTime;
        TickDayPhase();
        commandSystem.TickQueue(worldTime);

        scheduler.TickLane(OUTL_TickLane.Full, worldTime, deltaTime);

        logicAcc += deltaTime;
        aiAcc += deltaTime;
        questAcc += deltaTime;
        customAcc += deltaTime;
        randomAcc += deltaTime;
        stimulusAcc += deltaTime;
        materializationAcc += deltaTime;
        abstractEncounterAcc += deltaTime;

        TickAccumulatedLane(ref logicAcc, LogicTickInterval, OUTL_TickLane.Logic, false);
        TickAccumulatedLane(ref aiAcc, AITickInterval, OUTL_TickLane.AI, false);
        TickAccumulatedLane(ref questAcc, QuestTickInterval, OUTL_TickLane.Quest, true);
        TickAccumulatedLane(ref customAcc, CustomTickInterval, OUTL_TickLane.Custom, false);
        TickStimulusBus();
        TickMaterialization();
        TickAbstractEncounters();
        TickRandomLane();
    }

    private float ReadUnityDelta()
    {
        return TimeSource == OUTL_WorldTimeSource.UnscaledUnityTime ? UnityEngine.Time.unscaledDeltaTime : UnityEngine.Time.deltaTime;
    }

    private void RunFrameBegin()
    {
        OUTL_Profile.BeginFrame(registry.Count, scheduler.TickableCount, scheduler.RandomTickableCount);
        unscaledWorldTime = UnityEngine.Time.unscaledTime;
    }

    private void FlushEndOfFrame()
    {
        eventBus.Flush();
        ProcessDespawnQueue();
    }

    private void SyncUnityPauseState()
    {
        bool nowPaused = IsPaused;
        if (!DriveUnityTimeScaleOnPause)
        {
            lastPaused = nowPaused;
            return;
        }

        if (nowPaused && !unityTimeScaleCaptured)
        {
            capturedUnityTimeScale = UnityEngine.Time.timeScale <= 0f ? 1f : UnityEngine.Time.timeScale;
            unityTimeScaleCaptured = true;
            UnityEngine.Time.timeScale = 0f;
        }
        else if (!nowPaused && unityTimeScaleCaptured)
        {
            UnityEngine.Time.timeScale = capturedUnityTimeScale <= 0f ? 1f : capturedUnityTimeScale;
            unityTimeScaleCaptured = false;
        }

        if (nowPaused && !lastPaused)
        {
            deltaTime = 0f;
            simulationAcc = 0f;
        }

        lastPaused = nowPaused;
    }

    private void TickAccumulatedLane(ref float acc, float interval, OUTL_TickLane lane, bool tickQuestSystem)
    {
        float step = SafeInterval(interval);
        int guard = 0;
        while (acc >= step && guard < MaxCatchUpTicksPerFrame)
        {
            acc -= step;
            if (tickQuestSystem) questSystem.Tick(worldTime, step);
            scheduler.TickLane(lane, worldTime, step);
            guard++;
        }

        if (guard >= MaxCatchUpTicksPerFrame) acc = 0f;
    }

    private void TickRandomLane()
    {
        float step = SafeInterval(RandomTickInterval);
        int guard = 0;
        while (randomAcc >= step && guard < MaxCatchUpTicksPerFrame)
        {
            randomAcc -= step;
            scheduler.RandomTick(worldTime, Mathf.Max(0, RandomTickBudget));
            guard++;
        }

        if (guard >= MaxCatchUpTicksPerFrame) randomAcc = 0f;
    }

    private void TickStimulusBus()
    {
        float step = SafeInterval(StimulusTickInterval);
        int guard = 0;
        while (stimulusAcc >= step && guard < MaxCatchUpTicksPerFrame)
        {
            stimulusAcc -= step;
            OUTL_StimulusBus.Tick(worldTime, Mathf.Max(0, MaxStimuliProcessedPerFrame));
            guard++;
        }

        if (guard >= MaxCatchUpTicksPerFrame) stimulusAcc = 0f;
    }

    private void TickMaterialization()
    {
        if (!EnableAutomaticMaterialization || MaterializationBudgetPerTick <= 0) return;
        float step = SafeInterval(MaterializationTickInterval);
        int guard = 0;
        while (materializationAcc >= step && guard < MaxCatchUpTicksPerFrame)
        {
            materializationAcc -= step;
            Vector3 focus;
            if (TryResolveMaterializationFocus(out focus))
                materializationSystem.Tick(worldTime, Mathf.Max(0, MaterializationBudgetPerTick), focus, MaterializeEnterDistance, DematerializeExitDistance);
            guard++;
        }

        if (guard >= MaxCatchUpTicksPerFrame) materializationAcc = 0f;
    }

    private void TickAbstractEncounters()
    {
        if (MaxAbstractEncountersPerTick <= 0) return;
        float step = SafeInterval(AbstractEncounterTickInterval);
        int guard = 0;
        while (abstractEncounterAcc >= step && guard < MaxCatchUpTicksPerFrame)
        {
            abstractEncounterAcc -= step;
            abstractEncounterSystem.Tick(worldTime, step, Mathf.Max(0, MaxAbstractEncountersPerTick), AbstractEncounterDangerThreshold);
            guard++;
        }

        if (guard >= MaxCatchUpTicksPerFrame) abstractEncounterAcc = 0f;
    }

    private bool TryResolveMaterializationFocus(out Vector3 focus)
    {
        focus = Vector3.zero;
        OUTL_EntityRuntime runtime = registry.FindFirstByTargetName(MaterializationFocusTargetName);
        if (runtime == null) runtime = registry.FindFirstByClassName("actor.controlled");
        if (runtime == null || runtime.Adapter == null) return false;
        focus = runtime.Adapter.transform.position;
        return true;
    }

    private void TickDayPhase()
    {
        CurrentDayPhase = ResolveDayPhase(worldTime);
        if (CurrentDayPhase == lastDayPhase) return;
        lastDayPhase = CurrentDayPhase;
        eventBus.Emit(new OUTL_Event(OUTL_EventType.DayPhaseChanged, OUTL_EntityId.None, OUTL_EntityId.None) { Key = CurrentDayPhase.ToString(), IntValue = (int)CurrentDayPhase });
    }

    private OUTL_DayPhase ResolveDayPhase(float time)
    {
        float normalized = Mathf.Repeat(time / Mathf.Max(60f, DayLengthSeconds), 1f);
        if (normalized < 0.08f || normalized >= 0.92f) return OUTL_DayPhase.Midnight;
        if (normalized < 0.18f) return OUTL_DayPhase.Dawn;
        if (normalized < 0.58f) return OUTL_DayPhase.Day;
        if (normalized < 0.70f) return OUTL_DayPhase.Dusk;
        return OUTL_DayPhase.Night;
    }

    private static float SafeInterval(float value)
    {
        return Mathf.Max(MinTickInterval, value);
    }

    public void ApplyGlobalGravity()
    {
        if (!ApplySvGravityToPhysics) return;
        Physics.gravity = Vector3.down * OUTL_Cheats.UnityGravity;
    }

    public void ApplyTickProfile(OUTL_TickProfile profile)
    {
        if (profile == null) return;
        profile.Sanitize();
        LogicTickInterval = profile.logicInterval;
        AITickInterval = profile.aiNearInterval;
        QuestTickInterval = profile.questInterval;
        CustomTickInterval = profile.chunkProcessingInterval;
        StimulusTickInterval = profile.stimulusInterval;
        MaxStimuliProcessedPerFrame = profile.maxStimuliProcessedPerFrame;
        MaxEgregoreSignalsPerFrame = profile.maxEgregoreSignalsPerFrame;
        MaxSectorUpdatesPerFrame = profile.maxSectorUpdatesPerFrame;
        MaxNpcBehaviorTicksPerFrame = profile.maxNpcBehaviorTicksPerFrame;
        MaxNpcRouteUpdatesPerFrame = profile.maxNpcRouteUpdatesPerFrame;
        MaxNpcPathRequestsPerFrame = profile.maxNpcPathRequestsPerFrame;
        MaxNpcStimulusInterruptsPerFrame = profile.maxNpcStimulusInterruptsPerFrame;
        RandomTickBudget = Mathf.Max(RandomTickBudget, profile.maxAITicksPerFrame);
    }

    public OUTL_EntityRuntime Spawn(OUTL_EntityDef def, Vector3 position, Quaternion rotation)
    {
        if (def == null) return null;
        if (def.Prefab == null)
        {
            Debug.LogWarning("OUTL_World.Spawn requires OUTL_EntityDef.Prefab so runtime lifetime can go through OUTL_PoolSystem. Def=" + def.name, this);
            return null;
        }

        GameObject go = OUTL_PoolSystem.SpawnShared(def.Prefab, position, rotation, false);
        if (go == null) return null;
        OUTL_EntityAdapter adapter = go.GetComponent<OUTL_EntityAdapter>();
        if (adapter == null)
        {
            Debug.LogWarning("OUTL_World.Spawn prefab must contain OUTL_EntityAdapter. Prefab=" + def.Prefab.name, def.Prefab);
            OUTL_PoolSystem.ReleaseShared(go);
            return null;
        }
        adapter.Def = def;
        adapter.RegisterNow(this);
        if (!go.activeSelf) go.SetActive(true);
        eventBus.Emit(new OUTL_Event(OUTL_EventType.Spawned, adapter.Id, adapter.Id));
        return adapter.Runtime;
    }

    public void QueueDespawn(OUTL_EntityId id)
    {
        if (!id.IsValid) return;
        despawnQueue.Add(id);
    }

    public void Despawn(OUTL_EntityId id)
    {
        OUTL_EntityRuntime runtime;
        if (!registry.TryGet(id, out runtime)) return;
        OUTL_Profile.Frame.Despawns++;
        eventBus.Emit(new OUTL_Event(OUTL_EventType.Despawned, id, id));
        scheduler.Unregister(runtime.Adapter);
        scheduler.UnregisterRandom(runtime.Adapter);
        sectorGrid.Unregister(id);
        worldLedger.RemoveEntity(id);
        registry.Unregister(id);
        if (runtime.Adapter != null)
        {
            runtime.Adapter.ClearRuntimeRegistration();
            OUTL_PoolSystem.ReleaseShared(runtime.Adapter.gameObject);
        }
    }

    private void ProcessDespawnQueue()
    {
        int budget = Mathf.Min(Mathf.Max(1, DespawnBudgetPerFrame), despawnQueue.Count);
        for (int i = 0; i < budget; i++)
        {
            int last = despawnQueue.Count - 1;
            OUTL_EntityId id = despawnQueue[last];
            despawnQueue.RemoveAt(last);
            Despawn(id);
        }
    }

    private void EnsurePoolSystem()
    {
        if (OUTL_PoolSystem.Instance != null) return;
        OUTL_PoolSystem pool = GetComponent<OUTL_PoolSystem>();
        if (pool == null) gameObject.AddComponent<OUTL_PoolSystem>();
    }

    private void OnGUI()
    {
        OUTL_DebugHealthOverlay.Draw(this);
        OUTL_DebugInventoryOverlay.Draw(this);
        OUTL_DebugMapOverlay.Draw(this);
        if (!DebugStats) return;
        OUTL_FrameStats s = OUTL_Profile.LastFrame;
        GUI.Label(new Rect(12, 84, 1400, 22), "OUTL World mode=" + UpdateMode + " paused=" + IsPaused + " unityTS=" + UnityEngine.Time.timeScale.ToString("0.00") + " t=" + worldTime.ToString("0.00") + " dt=" + deltaTime.ToString("0.000") + " step=" + SimulationStep + " entities=" + registry.Count + " tickables=" + scheduler.TickableCount + " random=" + scheduler.RandomTickableCount + " queued=" + commandSystem.QueuedCount + " events=" + eventBus.PendingCount + " quests=" + questSystem.QuestCount + " despawnQ=" + despawnQueue.Count + " sv_gravity=" + OUTL_Cheats.SvGravity);
        GUI.Label(new Rect(12, 106, 1400, 22), "OUTL Perf full=" + s.FullTicks + " logic=" + s.LogicTicks + " ai=" + s.AITicks + " quest=" + s.QuestTicks + " random=" + s.RandomTicks + " ev=" + s.EventsFlushed + " cmd=" + s.CommandsSent + "/" + s.CommandsHandled + " q=" + s.QueuedCommands + " fx=" + s.EffectsApplied + " ray=" + s.Raycasts + " ov=" + s.Overlaps + " pool=" + s.PoolSpawns + "/" + s.PoolReleases);
    }
}

public sealed class OUTL_EntityRuntime
{
    public OUTL_EntityId Id;
    public OUTL_EntityDef Def;
    public OUTL_EntityAdapter Adapter;
    public OUTL_RuntimeTier Tier = OUTL_RuntimeTier.Full;
    public OUTL_FactionDef Faction;
    public readonly OUTL_StateBag State = new OUTL_StateBag();
    public readonly OUTL_StatBlock Stats = new OUTL_StatBlock();
    public string[] Tags;
    public string ClassName;
    public string TargetName;
    public string Target;
    public string KillTarget;
    public string StableId;
    public bool SavePersistent;
    public OUTL_LifeState LifeState = OUTL_LifeState.Alive;
    public bool Dead;
    public float DeathTime;
    public OUTL_EntityId KillerId = OUTL_EntityId.None;
    public string DeathKey;

    public bool HasTag(string tag)
    {
        if (string.IsNullOrEmpty(tag) || Tags == null) return false;
        for (int i = 0; i < Tags.Length; i++) if (Tags[i] == tag) return true;
        return false;
    }
}

public enum OUTL_WorldCellLayer : byte
{
    Region = 0,
    Province = 1,
    TravelCell = 2,
    ActivityCell = 3
}

[System.Flags]
public enum OUTL_WorldCellFlags
{
    None = 0,
    Traversable = 1 << 0,
    Road = 1 << 1,
    Forest = 1 << 2,
    Swamp = 1 << 3,
    Mountain = 1 << 4,
    Water = 1 << 5,
    Crossing = 1 << 6,
    Hostile = 1 << 7,
    Dangerous = 1 << 8,
    Resource = 1 << 9,
    Occupied = 1 << 10,
    Dirty = 1 << 11
}

[System.Serializable]
public struct OUTL_WorldCellKey : System.IEquatable<OUTL_WorldCellKey>
{
    public int X;
    public int Z;
    public OUTL_WorldCellLayer Layer;

    public OUTL_WorldCellKey(int x, int z, OUTL_WorldCellLayer layer)
    {
        X = x;
        Z = z;
        Layer = layer;
    }

    public bool IsValid { get { return Layer >= OUTL_WorldCellLayer.Region && Layer <= OUTL_WorldCellLayer.ActivityCell; } }

    public bool Equals(OUTL_WorldCellKey other)
    {
        return X == other.X && Z == other.Z && Layer == other.Layer;
    }

    public override bool Equals(object obj)
    {
        return obj is OUTL_WorldCellKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + X;
            hash = hash * 31 + Z;
            hash = hash * 31 + (int)Layer;
            return hash;
        }
    }

    public override string ToString()
    {
        return Layer + ":" + X + "," + Z;
    }

    public static bool operator ==(OUTL_WorldCellKey a, OUTL_WorldCellKey b) { return a.Equals(b); }
    public static bool operator !=(OUTL_WorldCellKey a, OUTL_WorldCellKey b) { return !a.Equals(b); }

    public static OUTL_WorldCellKey FromWorldPosition(Vector3 position, float cellSize, OUTL_WorldCellLayer layer)
    {
        float size = Mathf.Max(1f, cellSize);
        return new OUTL_WorldCellKey(Mathf.FloorToInt(position.x / size), Mathf.FloorToInt(position.z / size), layer);
    }
}

[System.Serializable]
public struct OUTL_WorldAddress
{
    public OUTL_WorldCellKey Region;
    public OUTL_WorldCellKey Province;
    public OUTL_WorldCellKey TravelCell;
    public OUTL_WorldCellKey ActivityCell;
    public Vector3 Position;

    public static OUTL_WorldAddress FromWorldPosition(Vector3 position, float activityCellSize)
    {
        float activity = Mathf.Max(1f, activityCellSize);
        return new OUTL_WorldAddress
        {
            Position = position,
            ActivityCell = OUTL_WorldCellKey.FromWorldPosition(position, activity, OUTL_WorldCellLayer.ActivityCell),
            TravelCell = OUTL_WorldCellKey.FromWorldPosition(position, activity * 4f, OUTL_WorldCellLayer.TravelCell),
            Province = OUTL_WorldCellKey.FromWorldPosition(position, activity * 16f, OUTL_WorldCellLayer.Province),
            Region = OUTL_WorldCellKey.FromWorldPosition(position, activity * 64f, OUTL_WorldCellLayer.Region)
        };
    }
}

public static class OUTL_WorldCellUtility
{
    public static int StableStringHash(string value)
    {
        if (string.IsNullOrEmpty(value)) return 0;
        unchecked
        {
            int hash = 23;
            for (int i = 0; i < value.Length; i++) hash = hash * 31 + value[i];
            return hash;
        }
    }

    public static float ManhattanDistance(OUTL_WorldCellKey a, OUTL_WorldCellKey b)
    {
        return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Z - b.Z);
    }
}

[System.Flags]
public enum OUTL_AbstractEntityFlags
{
    None = 0,
    Alive = 1 << 0,
    Dead = 1 << 1,
    SavePersistent = 1 << 2,
    HasAI = 1 << 3,
    HasInventory = 1 << 4,
    HasCombat = 1 << 5,
    HasPickup = 1 << 6,
    HasRoute = 1 << 7,
    HasActiveStimulus = 1 << 8,
    Materialized = 1 << 9,
    Dormant = 1 << 10
}

[System.Serializable]
public struct OUTL_AbstractEntityRecord
{
    public OUTL_EntityId EntityId;
    public OUTL_WorldAddress Address;
    public OUTL_WorldCellKey Cell;
    public OUTL_RuntimeTier RuntimeTier;
    public OUTL_AbstractEntityFlags Flags;
    public Vector3 Position;
    public int ClassHash;
    public int FactionHash;
    public string StableId;
    public string ClassName;
    public string FactionId;
    public string TargetName;
    public float Health;
    public float MaxHealth;
    public float LastUpdatedTime;
    public int Version;
}

[System.Serializable]
public struct OUTL_AbstractNpcRecord
{
    public OUTL_EntityId EntityId;
    public OUTL_WorldAddress Address;
    public OUTL_WorldCellKey Cell;
    public OUTL_RuntimeTier RuntimeTier;
    public OUTL_AbstractEntityFlags Flags;
    public string StableId;
    public string ClassName;
    public string FactionId;
    public string RoleId;
    public string ScheduleId;
    public string ScheduleEntryId;
    public string RouteKey;
    public Vector3 ExactPosition;
    public Vector3 AbstractPosition;
    public Vector3 TargetPosition;
    public int StartCellHash;
    public int TargetCellHash;
    public float RouteProgress;
    public float EstimatedArrivalTime;
    public OUTL_NPCScheduleActionType CurrentAction;
    public OUTL_NPCTravelMode TravelMode;
    public OUTL_StimulusType LastStimulusType;
    public string LastStimulusKey;
    public float LastStimulusTime;
    public float Health;
    public float MaxHealth;
    public float LastUpdatedTime;
    public int Version;
}

public enum OUTL_MaterializationReason
{
    None = 0,
    EnteredFocusRange = 1,
    NeedsCombat = 2,
    NeedsInteraction = 3,
    SaveRestore = 4,
    DebugRequest = 5
}

[System.Serializable]
public struct OUTL_MaterializationRequest
{
    public OUTL_EntityId EntityId;
    public OUTL_WorldAddress Address;
    public OUTL_RuntimeTier TargetTier;
    public OUTL_MaterializationReason Reason;
    public Vector3 Position;
    public float Priority;
    public float RequestTime;
}

public sealed class OUTL_MaterializationSystem
{
    private OUTL_World world;
    private readonly Dictionary<string, OUTL_EntitySaveRecord> abstractRecords = new Dictionary<string, OUTL_EntitySaveRecord>(512);
    private readonly List<string> abstractKeys = new List<string>(512);
    private readonly Dictionary<string, int> abstractKeyIndices = new Dictionary<string, int>(512);
    private readonly Dictionary<long, List<string>> abstractCells = new Dictionary<long, List<string>>(256);
    private readonly Dictionary<string, long> abstractKeyCells = new Dictionary<string, long>(512);
    private readonly List<OUTL_EntityRuntime> runtimeBuffer = new List<OUTL_EntityRuntime>(512);
    public float AbstractCellSize = 64f;

    public int AbstractCount { get { return abstractRecords.Count; } }

    public bool TryGetAbstractRecord(string stableId, out OUTL_EntitySaveRecord record)
    {
        record = null;
        return !string.IsNullOrEmpty(stableId) && abstractRecords.TryGetValue(stableId, out record) && record != null;
    }

    public bool ContainsStableId(string stableId)
    {
        return !string.IsNullOrEmpty(stableId) && abstractRecords.ContainsKey(stableId);
    }

    public void Bind(OUTL_World world)
    {
        this.world = world;
    }

    public bool TryDematerialize(OUTL_EntityAdapter adapter, OUTL_MaterializationReason reason, OUTL_RuntimeTier targetTier = OUTL_RuntimeTier.Far)
    {
        if (world == null || adapter == null || adapter.Runtime == null) return false;
        OUTL_EntityRuntime runtime = adapter.Runtime;
        if (!CanDematerialize(runtime)) return false;

        OUTL_EntitySaveRecord record = OUTL_SaveSystem.BuildRecordFromRuntime(world, runtime, true);
        if (record == null) return false;
        record.Materialized = false;
        record.RestoreSpawnIfMissing = true;
        record.Tier = targetTier;

        OUTL_AbstractEntityRecord entityRecord = BuildEntityRecord(record, false, world.WorldTime);
        OUTL_NPCBehaviorController npc = adapter.GetComponent<OUTL_NPCBehaviorController>();
        OUTL_AbstractNpcRecord npcRecord = default(OUTL_AbstractNpcRecord);
        bool hasNpc = npc != null && npc.Runtime != null && BuildNpcRecord(runtime, npc.Runtime, record, world.WorldTime, out npcRecord);

        string key = BuildKey(record);
        Store(record, key);

        OUTL_EntityId oldId = runtime.Id;
        world.Events.Emit(new OUTL_Event(OUTL_EventType.Despawned, oldId, oldId) { Key = "dematerialized:" + reason, Point = record.Position });
        world.Scheduler.Unregister(adapter);
        world.Scheduler.UnregisterRandom(adapter);
        world.Sectors.Unregister(oldId);
        world.Registry.Unregister(oldId);
        adapter.ClearRuntimeRegistration();
        OUTL_PoolSystem.ReleaseShared(adapter.gameObject);

        world.WorldLedger.RegisterEntity(entityRecord);
        if (hasNpc) world.WorldLedger.RegisterNpcRecord(npcRecord);
        return true;
    }

    public OUTL_EntityAdapter Materialize(OUTL_EntitySaveRecord record, OUTL_MaterializationReason reason, OUTL_RuntimeTier targetTier = OUTL_RuntimeTier.Near)
    {
        if (world == null || record == null) return null;
        if (record.Dead || record.LifeState == OUTL_LifeState.Dead) return null;

        string key = BuildKey(record);
        record.Materialized = true;
        record.RestoreSpawnIfMissing = true;
        record.Tier = targetTier;

        OUTL_EntityAdapter adapter = world.Save.ResolveOrSpawn(record);
        if (adapter == null || adapter.Runtime == null) return null;

        OUTL_SaveSystem.ApplyRecordToRuntime(world, adapter.Runtime, record, true);
        world.Events.Emit(new OUTL_Event(OUTL_EventType.Spawned, adapter.Id, adapter.Id) { Key = "materialized:" + reason, Point = adapter.transform.position });
        RemoveStored(key);
        return adapter;
    }

    public OUTL_EntityAdapter MaterializeByStableId(string stableId, OUTL_MaterializationReason reason = OUTL_MaterializationReason.DebugRequest)
    {
        if (string.IsNullOrEmpty(stableId)) return null;
        OUTL_EntitySaveRecord record;
        return abstractRecords.TryGetValue(stableId, out record) ? Materialize(record, reason, OUTL_RuntimeTier.Near) : null;
    }

    public void Tick(float time, int budget, Vector3 focus, float enterDistance, float exitDistance)
    {
        if (world == null || budget <= 0) return;
        int remaining = budget;
        float enterSqr = Mathf.Max(0f, enterDistance) * Mathf.Max(0f, enterDistance);
        float cellSize = Mathf.Max(1f, AbstractCellSize);
        int centerX = Mathf.FloorToInt(focus.x / cellSize);
        int centerZ = Mathf.FloorToInt(focus.z / cellSize);
        int cellRadius = Mathf.CeilToInt(Mathf.Max(0f, enterDistance) / cellSize);
        for (int z = -cellRadius; z <= cellRadius && remaining > 0; z++)
        {
            for (int x = -cellRadius; x <= cellRadius && remaining > 0; x++)
            {
                List<string> keys;
                if (!abstractCells.TryGetValue(CellKey(centerX + x, centerZ + z), out keys) || keys == null) continue;
                for (int i = keys.Count - 1; i >= 0 && remaining > 0; i--)
                {
                    string key = keys[i];
                    OUTL_EntitySaveRecord record;
                    if (!abstractRecords.TryGetValue(key, out record) || record == null) continue;
                    if (record.Dead || record.LifeState == OUTL_LifeState.Dead) continue;
                    if ((record.Position - focus).sqrMagnitude > enterSqr) continue;
                    if (Materialize(record, OUTL_MaterializationReason.EnteredFocusRange, OUTL_RuntimeTier.Near) != null)
                        remaining--;
                }
            }
        }

        if (remaining <= 0) return;
        float exitSqr = Mathf.Max(enterDistance, exitDistance) * Mathf.Max(enterDistance, exitDistance);
        world.Registry.CopyAll(runtimeBuffer);
        for (int i = 0; i < runtimeBuffer.Count && remaining > 0; i++)
        {
            OUTL_EntityRuntime runtime = runtimeBuffer[i];
            if (runtime == null || runtime.Adapter == null) continue;
            if ((runtime.Adapter.transform.position - focus).sqrMagnitude < exitSqr) continue;
            if (TryDematerialize(runtime.Adapter, OUTL_MaterializationReason.EnteredFocusRange, OUTL_RuntimeTier.Far)) remaining--;
        }
        runtimeBuffer.Clear();
    }

    public void CopyAbstractRecords(List<OUTL_EntitySaveRecord> output)
    {
        if (output == null) return;
        output.Clear();
        for (int i = 0; i < abstractKeys.Count; i++)
        {
            OUTL_EntitySaveRecord record;
            if (abstractRecords.TryGetValue(abstractKeys[i], out record) && record != null) output.Add(record);
        }
    }

    public void RestoreAbstractRecords(List<OUTL_EntitySaveRecord> input)
    {
        abstractRecords.Clear();
        abstractKeys.Clear();
        abstractKeyIndices.Clear();
        abstractCells.Clear();
        abstractKeyCells.Clear();
        if (input == null) return;
        for (int i = 0; i < input.Count; i++)
        {
            OUTL_EntitySaveRecord record = input[i];
            if (record == null) continue;
            record.Materialized = false;
            Store(record, BuildKey(record));
            if (world != null)
            {
                world.WorldLedger.RegisterEntity(BuildEntityRecord(record, false, world.WorldTime));
                OUTL_AbstractNpcRecord npcRecord;
                if (BuildNpcRecord(record, world.WorldTime, out npcRecord)) world.WorldLedger.RegisterNpcRecord(npcRecord);
            }
        }
    }

    private void Store(OUTL_EntitySaveRecord record, string key)
    {
        if (record == null || string.IsNullOrEmpty(key)) return;
        long newCell = PositionCell(record.Position);
        long oldCell;
        if (abstractKeyCells.TryGetValue(key, out oldCell) && oldCell != newCell) RemoveFromCell(key, oldCell);
        abstractRecords[key] = record;
        if (!abstractKeyIndices.ContainsKey(key))
        {
            abstractKeyIndices[key] = abstractKeys.Count;
            abstractKeys.Add(key);
        }
        AddToCell(key, newCell);
    }

    public bool RegisterAbstractSpawn(
        OUTL_EntityDef def,
        Vector3 position,
        Quaternion rotation,
        string stableId,
        OUTL_RuntimeTier tier = OUTL_RuntimeTier.Dormant,
        string targetName = null,
        string outpostId = null)
    {
        if (world == null || def == null || def.Prefab == null || string.IsNullOrEmpty(stableId)) return false;
        OUTL_EntitySaveRecord record = new OUTL_EntitySaveRecord
        {
            Id = 0,
            StableId = stableId,
            ClassName = def.ClassName,
            TargetName = targetName ?? string.Empty,
            DefId = def.ClassName,
            DefName = def.name,
            RestoreSpawnIfMissing = true,
            Materialized = false,
            Position = position,
            Rotation = rotation,
            Tier = tier,
            LifeState = OUTL_LifeState.Alive,
            Dead = false
        };
        if (!string.IsNullOrEmpty(outpostId))
            record.StateStrings.Add(new OUTL_StringPair { Key = "outpost", Value = outpostId });
        if (def.BaseStats != null)
        {
            for (int i = 0; i < def.BaseStats.Length; i++)
                record.Stats.Add(new OUTL_FloatPair { Key = def.BaseStats[i].Key, Value = def.BaseStats[i].Value });
        }
        OUTL_CharacterIdentity identityTemplate = def.Prefab.GetComponent<OUTL_CharacterIdentity>();
        OUTL_ComponentSavePayload identityPayload = OUTL_CharacterIdentity.BuildInitialPayload(identityTemplate, stableId);
        if (identityPayload != null) record.ComponentPayloads.Add(identityPayload);
        Store(record, stableId);
        world.WorldLedger.RegisterEntity(BuildEntityRecord(record, false, world.WorldTime));
        return true;
    }

    private void RemoveStored(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        abstractRecords.Remove(key);
        long cell;
        if (abstractKeyCells.TryGetValue(key, out cell)) RemoveFromCell(key, cell);
        int index;
        if (!abstractKeyIndices.TryGetValue(key, out index)) return;
        int last = abstractKeys.Count - 1;
        string moved = abstractKeys[last];
        abstractKeys[index] = moved;
        abstractKeys.RemoveAt(last);
        abstractKeyIndices.Remove(key);
        if (index < abstractKeys.Count) abstractKeyIndices[moved] = index;
    }

    private void AddToCell(string key, long cell)
    {
        long existing;
        if (abstractKeyCells.TryGetValue(key, out existing) && existing == cell) return;
        List<string> list;
        if (!abstractCells.TryGetValue(cell, out list))
        {
            list = new List<string>(16);
            abstractCells[cell] = list;
        }
        list.Add(key);
        abstractKeyCells[key] = cell;
    }

    private void RemoveFromCell(string key, long cell)
    {
        List<string> list;
        if (abstractCells.TryGetValue(cell, out list) && list != null)
        {
            list.Remove(key);
            if (list.Count == 0) abstractCells.Remove(cell);
        }
        abstractKeyCells.Remove(key);
    }

    private long PositionCell(Vector3 position)
    {
        float size = Mathf.Max(1f, AbstractCellSize);
        return CellKey(Mathf.FloorToInt(position.x / size), Mathf.FloorToInt(position.z / size));
    }

    private static long CellKey(int x, int z)
    {
        unchecked { return ((long)x << 32) ^ (uint)z; }
    }

    private static bool CanDematerialize(OUTL_EntityRuntime runtime)
    {
        if (runtime == null || runtime.Adapter == null) return false;
        if (!runtime.SavePersistent) return false;
        if (!runtime.Adapter.RestoreSpawnIfMissing && runtime.Def == null) return false;
        if (runtime.Def != null && runtime.Def.Prefab == null) return false;
        if (runtime.ClassName != null && runtime.ClassName.IndexOf("egregore", System.StringComparison.OrdinalIgnoreCase) >= 0) return false;
        if (runtime.ClassName != null && runtime.ClassName.IndexOf("pickup", System.StringComparison.OrdinalIgnoreCase) >= 0) return false;
        return true;
    }

    private static string BuildKey(OUTL_EntitySaveRecord record)
    {
        if (record == null) return string.Empty;
        if (!string.IsNullOrEmpty(record.StableId)) return record.StableId;
        return record.Id > 0 ? "id:" + record.Id : string.Empty;
    }

    private static OUTL_AbstractEntityRecord BuildEntityRecord(OUTL_EntitySaveRecord record, bool materialized, float time)
    {
        Vector3 position = record != null ? record.Position : Vector3.zero;
        OUTL_World world = OUTL_World.Instance;
        float cellSize = world != null ? world.WorldLedger.ActivityCellSize : 64f;
        OUTL_WorldAddress address = OUTL_WorldAddress.FromWorldPosition(position, cellSize);
        OUTL_AbstractEntityFlags flags = record != null && (record.Dead || record.LifeState == OUTL_LifeState.Dead) ? OUTL_AbstractEntityFlags.Dead : OUTL_AbstractEntityFlags.Alive;
        flags |= OUTL_AbstractEntityFlags.SavePersistent;
        if (materialized) flags |= OUTL_AbstractEntityFlags.Materialized;
        else flags |= OUTL_AbstractEntityFlags.Dormant;
        if (HasPayload(record, "OUTL_NPCBehaviorRuntime")) flags |= OUTL_AbstractEntityFlags.HasAI | OUTL_AbstractEntityFlags.HasRoute;
        if (HasPayload(record, "OUTL_InventoryRuntime")) flags |= OUTL_AbstractEntityFlags.HasInventory;
        if (record != null && record.ClassName != null && record.ClassName.IndexOf("pickup", System.StringComparison.OrdinalIgnoreCase) >= 0) flags |= OUTL_AbstractEntityFlags.HasPickup;
        if (record != null && record.ClassName != null && (record.ClassName.IndexOf("actor", System.StringComparison.OrdinalIgnoreCase) >= 0 || record.ClassName.IndexOf("destructible", System.StringComparison.OrdinalIgnoreCase) >= 0)) flags |= OUTL_AbstractEntityFlags.HasCombat;

        return new OUTL_AbstractEntityRecord
        {
            EntityId = record != null ? new OUTL_EntityId(record.Id) : OUTL_EntityId.None,
            Address = address,
            Cell = address.ActivityCell,
            RuntimeTier = record != null ? record.Tier : OUTL_RuntimeTier.Dormant,
            Flags = flags,
            Position = position,
            ClassHash = OUTL_WorldCellUtility.StableStringHash(record != null ? record.ClassName : string.Empty),
            FactionHash = OUTL_WorldCellUtility.StableStringHash(record != null ? record.FactionName : string.Empty),
            StableId = record != null ? record.StableId : string.Empty,
            ClassName = record != null ? record.ClassName : string.Empty,
            FactionId = record != null ? record.FactionName : string.Empty,
            TargetName = record != null ? record.TargetName : string.Empty,
            Health = GetStat(record, OUTL_StatId.Health, 0f),
            MaxHealth = GetStat(record, "MaxHealth", 0f),
            LastUpdatedTime = time
        };
    }

    private static bool BuildNpcRecord(OUTL_EntityRuntime runtime, OUTL_NPCBehaviorRuntime npcRuntime, OUTL_EntitySaveRecord record, float time, out OUTL_AbstractNpcRecord npcRecord)
    {
        npcRecord = default(OUTL_AbstractNpcRecord);
        if (runtime == null || npcRuntime == null || record == null) return false;
        Vector3 position = npcRuntime.AbstractPosition != Vector3.zero ? npcRuntime.AbstractPosition : record.Position;
        OUTL_World world = OUTL_World.Instance;
        float cellSize = world != null ? world.WorldLedger.ActivityCellSize : 64f;
        OUTL_WorldAddress address = OUTL_WorldAddress.FromWorldPosition(position, cellSize);
        OUTL_AbstractEntityFlags flags = record.Dead || record.LifeState == OUTL_LifeState.Dead ? OUTL_AbstractEntityFlags.Dead : OUTL_AbstractEntityFlags.Alive;
        flags |= OUTL_AbstractEntityFlags.SavePersistent | OUTL_AbstractEntityFlags.HasAI | OUTL_AbstractEntityFlags.HasRoute | OUTL_AbstractEntityFlags.Dormant;
        npcRecord = new OUTL_AbstractNpcRecord
        {
            EntityId = new OUTL_EntityId(record.Id),
            Address = address,
            Cell = address.ActivityCell,
            RuntimeTier = record.Tier,
            Flags = flags,
            StableId = record.StableId,
            ClassName = record.ClassName,
            FactionId = record.FactionName,
            RoleId = npcRuntime.Role,
            ScheduleId = npcRuntime.CurrentScheduleId,
            ScheduleEntryId = npcRuntime.CurrentEntryId,
            RouteKey = npcRuntime.Travel.RouteKey,
            ExactPosition = record.Position,
            AbstractPosition = npcRuntime.AbstractPosition,
            TargetPosition = npcRuntime.CurrentTargetPosition,
            StartCellHash = npcRuntime.Travel.StartSectorId,
            TargetCellHash = npcRuntime.Travel.TargetSectorId,
            RouteProgress = npcRuntime.RouteProgress,
            EstimatedArrivalTime = npcRuntime.EstimatedArrivalTime,
            CurrentAction = npcRuntime.CurrentAction,
            TravelMode = npcRuntime.Travel.Mode,
            LastStimulusType = npcRuntime.LastStimulus,
            LastStimulusKey = npcRuntime.LastStimulusKey,
            LastStimulusTime = npcRuntime.LastStimulusTime,
            Health = GetStat(record, OUTL_StatId.Health, 0f),
            MaxHealth = GetStat(record, "MaxHealth", 0f),
            LastUpdatedTime = time
        };
        return true;
    }

    private static bool BuildNpcRecord(OUTL_EntitySaveRecord record, float time, out OUTL_AbstractNpcRecord npcRecord)
    {
        npcRecord = default(OUTL_AbstractNpcRecord);
        OUTL_ComponentSavePayload payload = FindPayload(record, "OUTL_NPCBehaviorRuntime");
        if (record == null || payload == null) return false;
        OUTL_ComponentSaveReader reader = new OUTL_ComponentSaveReader(payload);
        Vector3 abstractPosition = new Vector3(reader.GetFloat("abstract.x", record.Position.x), reader.GetFloat("abstract.y", record.Position.y), reader.GetFloat("abstract.z", record.Position.z));
        Vector3 targetPosition = new Vector3(reader.GetFloat("target.x", record.Position.x), reader.GetFloat("target.y", record.Position.y), reader.GetFloat("target.z", record.Position.z));
        OUTL_World world = OUTL_World.Instance;
        float cellSize = world != null ? world.WorldLedger.ActivityCellSize : 64f;
        OUTL_WorldAddress address = OUTL_WorldAddress.FromWorldPosition(abstractPosition, cellSize);
        OUTL_AbstractEntityFlags flags = record.Dead || record.LifeState == OUTL_LifeState.Dead ? OUTL_AbstractEntityFlags.Dead : OUTL_AbstractEntityFlags.Alive;
        flags |= OUTL_AbstractEntityFlags.SavePersistent | OUTL_AbstractEntityFlags.HasAI | OUTL_AbstractEntityFlags.HasRoute | OUTL_AbstractEntityFlags.Dormant;
        npcRecord = new OUTL_AbstractNpcRecord
        {
            EntityId = new OUTL_EntityId(record.Id),
            Address = address,
            Cell = address.ActivityCell,
            RuntimeTier = record.Tier,
            Flags = flags,
            StableId = record.StableId,
            ClassName = record.ClassName,
            FactionId = record.FactionName,
            RoleId = reader.GetString("role", string.Empty),
            ScheduleId = reader.GetString("schedule", string.Empty),
            ScheduleEntryId = reader.GetString("entry", string.Empty),
            RouteKey = reader.GetString("travelRoute", reader.GetString("route", string.Empty)),
            ExactPosition = record.Position,
            AbstractPosition = abstractPosition,
            TargetPosition = targetPosition,
            StartCellHash = reader.GetInt("travelStartSector", 0),
            TargetCellHash = reader.GetInt("travelTargetSector", reader.GetInt("targetSector", 0)),
            RouteProgress = Mathf.Clamp01(reader.GetFloat("travelProgress", reader.GetFloat("progress", 0f))),
            EstimatedArrivalTime = reader.GetFloat("travelEta", reader.GetFloat("eta", 0f)),
            CurrentAction = (OUTL_NPCScheduleActionType)Mathf.Clamp(reader.GetInt("action", 0), 0, (int)OUTL_NPCScheduleActionType.Combat),
            TravelMode = (OUTL_NPCTravelMode)Mathf.Clamp(reader.GetInt("travelMode", 0), 0, (int)OUTL_NPCTravelMode.RouteFailed),
            LastStimulusType = (OUTL_StimulusType)Mathf.Clamp(reader.GetInt("lastStimulus", 0), 0, 255),
            LastStimulusKey = reader.GetString("lastStimulusKey", string.Empty),
            LastStimulusTime = reader.GetFloat("lastStimulusTime", 0f),
            Health = GetStat(record, OUTL_StatId.Health, 0f),
            MaxHealth = GetStat(record, "MaxHealth", 0f),
            LastUpdatedTime = time
        };
        return true;
    }

    private static bool HasPayload(OUTL_EntitySaveRecord record, string key)
    {
        return FindPayload(record, key) != null;
    }

    private static OUTL_ComponentSavePayload FindPayload(OUTL_EntitySaveRecord record, string key)
    {
        if (record == null || record.ComponentPayloads == null || string.IsNullOrEmpty(key)) return null;
        for (int i = 0; i < record.ComponentPayloads.Count; i++)
            if (record.ComponentPayloads[i] != null && record.ComponentPayloads[i].Key == key)
                return record.ComponentPayloads[i];
        return null;
    }

    private static float GetStat(OUTL_EntitySaveRecord record, string key, float fallback)
    {
        if (record == null || record.Stats == null || string.IsNullOrEmpty(key)) return fallback;
        for (int i = record.Stats.Count - 1; i >= 0; i--)
            if (record.Stats[i].Key == key) return record.Stats[i].Value;
        return fallback;
    }

    private static float GetStat(OUTL_EntitySaveRecord record, OUTL_StatId id, float fallback)
    {
        return GetStat(record, OUTL_CompactIds.StatToKey(id), fallback);
    }
}

public sealed class OUTL_AbstractEncounterSystem
{
    private OUTL_World world;
    private readonly List<OUTL_WorldCellSummary> summaries = new List<OUTL_WorldCellSummary>(256);
    private readonly Dictionary<OUTL_WorldCellKey, float> lastResolvedByCell = new Dictionary<OUTL_WorldCellKey, float>(128);

    public int LastResolvedCount { get; private set; }

    public void Bind(OUTL_World world)
    {
        this.world = world;
    }

    public void Tick(float time, float deltaTime, int budget, float dangerThreshold)
    {
        LastResolvedCount = 0;
        if (world == null || budget <= 0) return;
        world.WorldLedger.CopyCellSummaries(summaries);
        float threshold = Mathf.Clamp01(dangerThreshold);
        for (int i = 0; i < summaries.Count && LastResolvedCount < budget; i++)
        {
            OUTL_WorldCellSummary summary = summaries[i];
            float pressure = Mathf.Clamp01(Mathf.Max(summary.Danger, summary.SpawnPressure, summary.EgregoreFear, summary.EgregoreViolence));
            if (pressure < threshold) continue;
            if (summary.NpcCount > 0 && summary.NpcCount <= summary.CombatantCount) continue;
            float lastTime;
            if (lastResolvedByCell.TryGetValue(summary.Cell, out lastTime) && time - lastTime < 8f) continue;

            Vector3 position = CellCenter(summary.Cell, world.WorldLedger.ActivityCellSize);
            lastResolvedByCell[summary.Cell] = time;
            OUTL_StimulusBus.Emit(new OUTL_Stimulus(OUTL_StimulusType.SightDanger, OUTL_EntityId.None, position, world.WorldLedger.ActivityCellSize, pressure, 1f, pressure, 6f, "abstract.encounter", null));
            world.Events.Emit(new OUTL_Event(OUTL_EventType.Custom, OUTL_EntityId.None, OUTL_EntityId.None) { Key = "abstract.encounter", FloatValue = pressure, Point = position });
            LastResolvedCount++;
        }
        summaries.Clear();
    }

    private static Vector3 CellCenter(OUTL_WorldCellKey cell, float cellSize)
    {
        float size = Mathf.Max(1f, cellSize);
        return new Vector3((cell.X + 0.5f) * size, 0f, (cell.Z + 0.5f) * size);
    }
}

[System.Serializable]
public struct OUTL_WorldCellSummary
{
    public OUTL_WorldCellKey Cell;
    public OUTL_WorldCellFlags Flags;
    public int EntityCount;
    public int NpcCount;
    public int AliveCount;
    public int DeadCount;
    public int CombatantCount;
    public int PickupCount;
    public int SavePersistentCount;
    public int DominantFactionHash;
    public float Danger;
    public float Food;
    public OUTL_StimulusType LastStimulusType;
    public Vector3 LastStimulusPosition;
    public float LastStimulusPriority;
    public OUTL_EgregoreMood EgregoreMood;
    public OUTL_EgregoreCyclePhase EgregoreCyclePhase;
    public OUTL_EgregoreArchetypeId DominantArchetype;
    public OUTL_EgregoreArchetypeId ShadowArchetype;
    public float EgregoreFear;
    public float EgregoreViolence;
    public float EgregoreCorruption;
    public float EgregoreProsperity;
    public float SpawnPressure;
    public float QuestPressure;
    public float LootPressure;
    public float BehaviorPressure;
    public float Safety;
    public float RitualTension;
    public float LastUpdatedTime;
    public int Version;

    public bool IsEmpty { get { return EntityCount <= 0 && NpcCount <= 0; } }
}

public sealed class OUTL_WorldLedger
{
    public float ActivityCellSize = 64f;
    public int Version { get; private set; }
    public int EntityCount { get { return entityRecords.Count; } }
    public int NpcCount { get { return npcRecords.Count; } }
    public int CellCount { get { return cellSummaries.Count; } }

    private readonly Dictionary<int, OUTL_AbstractEntityRecord> entityRecords = new Dictionary<int, OUTL_AbstractEntityRecord>(2048);
    private readonly Dictionary<int, OUTL_AbstractNpcRecord> npcRecords = new Dictionary<int, OUTL_AbstractNpcRecord>(1024);
    private readonly Dictionary<OUTL_WorldCellKey, List<int>> entityIdsByCell = new Dictionary<OUTL_WorldCellKey, List<int>>(512);
    private readonly Dictionary<OUTL_WorldCellKey, OUTL_WorldCellSummary> cellSummaries = new Dictionary<OUTL_WorldCellKey, OUTL_WorldCellSummary>(512);

    public void RegisterEntity(OUTL_AbstractEntityRecord record)
    {
        if (!record.EntityId.IsValid) return;
        OUTL_AbstractEntityRecord old;
        bool hadOld = entityRecords.TryGetValue(record.EntityId.Value, out old);
        if (!record.Cell.IsValid) record.Cell = record.Address.ActivityCell;
        if (hadOld && old.Cell != record.Cell) RemoveFromCell(old.Cell, record.EntityId.Value);

        record.Version = ++Version;
        entityRecords[record.EntityId.Value] = record;
        AddToCell(record.Cell, record.EntityId.Value);
        RebuildSummary(record.Cell, record.LastUpdatedTime);
        if (hadOld && old.Cell != record.Cell) RebuildSummary(old.Cell, record.LastUpdatedTime);
    }

    public void RegisterOrUpdateEntity(OUTL_EntityRuntime runtime, Vector3 position, float time)
    {
        if (runtime == null || !runtime.Id.IsValid) return;
        string factionId = runtime.Faction != null ? runtime.Faction.FactionId : string.Empty;
        OUTL_WorldAddress address = OUTL_WorldAddress.FromWorldPosition(position, ActivityCellSize);
        OUTL_AbstractEntityFlags flags = runtime.Dead || runtime.LifeState == OUTL_LifeState.Dead
            ? OUTL_AbstractEntityFlags.Dead
            : OUTL_AbstractEntityFlags.Alive;
        if (runtime.SavePersistent) flags |= OUTL_AbstractEntityFlags.SavePersistent;
        if (runtime.Tier == OUTL_RuntimeTier.Dormant) flags |= OUTL_AbstractEntityFlags.Dormant;

        RegisterEntity(new OUTL_AbstractEntityRecord
        {
            EntityId = runtime.Id,
            Address = address,
            Cell = address.ActivityCell,
            RuntimeTier = runtime.Tier,
            Flags = flags,
            Position = position,
            ClassHash = OUTL_WorldCellUtility.StableStringHash(runtime.ClassName),
            FactionHash = OUTL_WorldCellUtility.StableStringHash(factionId),
            StableId = runtime.StableId,
            ClassName = runtime.ClassName,
            FactionId = factionId,
            TargetName = runtime.TargetName,
            Health = runtime.Stats.Get(OUTL_StatId.Health, 0f),
            MaxHealth = runtime.Stats.Get("MaxHealth", 0f),
            LastUpdatedTime = time
        });
    }

    public void RegisterOrUpdateNpc(OUTL_EntityRuntime runtime, OUTL_NPCBehaviorRuntime npcRuntime, Vector3 position, float time)
    {
        if (runtime == null || npcRuntime == null || !runtime.Id.IsValid) return;
        RegisterOrUpdateEntity(runtime, position, time);

        string factionId = runtime.Faction != null ? runtime.Faction.FactionId : npcRuntime.Faction;
        OUTL_WorldAddress address = OUTL_WorldAddress.FromWorldPosition(position, ActivityCellSize);
        OUTL_AbstractEntityFlags flags = runtime.Dead || runtime.LifeState == OUTL_LifeState.Dead
            ? OUTL_AbstractEntityFlags.Dead
            : OUTL_AbstractEntityFlags.Alive;
        flags |= OUTL_AbstractEntityFlags.HasAI;
        if (!string.IsNullOrEmpty(npcRuntime.Travel.RouteKey)) flags |= OUTL_AbstractEntityFlags.HasRoute;
        if (npcRuntime.LastStimulus != OUTL_StimulusType.None) flags |= OUTL_AbstractEntityFlags.HasActiveStimulus;
        if (runtime.Tier == OUTL_RuntimeTier.Full || runtime.Tier == OUTL_RuntimeTier.Near) flags |= OUTL_AbstractEntityFlags.Materialized;
        if (runtime.Tier == OUTL_RuntimeTier.Dormant) flags |= OUTL_AbstractEntityFlags.Dormant;

        npcRecords[runtime.Id.Value] = new OUTL_AbstractNpcRecord
        {
            EntityId = runtime.Id,
            Address = address,
            Cell = address.ActivityCell,
            RuntimeTier = runtime.Tier,
            Flags = flags,
            StableId = runtime.StableId,
            ClassName = runtime.ClassName,
            FactionId = factionId,
            RoleId = npcRuntime.Role,
            ScheduleId = npcRuntime.CurrentScheduleId,
            ScheduleEntryId = npcRuntime.CurrentEntryId,
            RouteKey = npcRuntime.Travel.RouteKey,
            ExactPosition = npcRuntime.LastExactPosition,
            AbstractPosition = npcRuntime.AbstractPosition,
            TargetPosition = npcRuntime.CurrentTargetPosition,
            StartCellHash = npcRuntime.Travel.StartSectorId,
            TargetCellHash = npcRuntime.Travel.TargetSectorId,
            RouteProgress = npcRuntime.RouteProgress,
            EstimatedArrivalTime = npcRuntime.EstimatedArrivalTime,
            CurrentAction = npcRuntime.CurrentAction,
            TravelMode = npcRuntime.Travel.Mode,
            LastStimulusType = npcRuntime.LastStimulus,
            LastStimulusKey = npcRuntime.LastStimulusKey,
            LastStimulusTime = npcRuntime.LastStimulusTime,
            Health = runtime.Stats.Get(OUTL_StatId.Health, 0f),
            MaxHealth = runtime.Stats.Get("MaxHealth", 0f),
            LastUpdatedTime = time,
            Version = Version
        };
        RebuildSummary(address.ActivityCell, time);
    }

    public void RegisterNpcRecord(OUTL_AbstractNpcRecord record)
    {
        if (!record.EntityId.IsValid) return;
        if (!record.Cell.IsValid) record.Cell = record.Address.ActivityCell;
        record.Version = ++Version;
        npcRecords[record.EntityId.Value] = record;
        RebuildSummary(record.Cell, record.LastUpdatedTime);
    }

    public bool MoveEntity(OUTL_EntityId entityId, Vector3 position, float time)
    {
        OUTL_AbstractEntityRecord record;
        if (!entityId.IsValid || !entityRecords.TryGetValue(entityId.Value, out record)) return false;
        OUTL_WorldAddress address = OUTL_WorldAddress.FromWorldPosition(position, ActivityCellSize);
        OUTL_WorldCellKey oldCell = record.Cell;
        record.Address = address;
        record.Cell = address.ActivityCell;
        record.Position = position;
        record.LastUpdatedTime = time;
        record.Version = ++Version;
        entityRecords[entityId.Value] = record;

        if (oldCell != record.Cell)
        {
            RemoveFromCell(oldCell, entityId.Value);
            AddToCell(record.Cell, entityId.Value);
            RebuildSummary(oldCell, time);
        }
        RebuildSummary(record.Cell, time);
        return true;
    }

    public bool RemoveEntity(OUTL_EntityId entityId)
    {
        OUTL_AbstractEntityRecord record;
        if (!entityId.IsValid || !entityRecords.TryGetValue(entityId.Value, out record)) return false;
        entityRecords.Remove(entityId.Value);
        npcRecords.Remove(entityId.Value);
        RemoveFromCell(record.Cell, entityId.Value);
        Version++;
        RebuildSummary(record.Cell, record.LastUpdatedTime);
        return true;
    }

    public bool QueryCell(OUTL_WorldCellKey cell, List<OUTL_AbstractEntityRecord> output)
    {
        if (output == null) return false;
        output.Clear();
        List<int> ids;
        if (!entityIdsByCell.TryGetValue(cell, out ids) || ids == null) return false;
        for (int i = 0; i < ids.Count; i++)
        {
            OUTL_AbstractEntityRecord record;
            if (entityRecords.TryGetValue(ids[i], out record)) output.Add(record);
        }
        return output.Count > 0;
    }

    public int QueryNearbyCells(OUTL_WorldCellKey center, int radius, List<OUTL_WorldCellSummary> output)
    {
        if (output == null) return 0;
        output.Clear();
        int r = Mathf.Max(0, radius);
        for (int z = -r; z <= r; z++)
            for (int x = -r; x <= r; x++)
            {
                OUTL_WorldCellKey key = new OUTL_WorldCellKey(center.X + x, center.Z + z, center.Layer);
                OUTL_WorldCellSummary summary;
                if (cellSummaries.TryGetValue(key, out summary)) output.Add(summary);
            }
        return output.Count;
    }

    public OUTL_RuntimeTier GetTierRelativeToFocus(Vector3 position, Vector3 focus, float fullDistance, float nearDistance, float midDistance, float farDistance)
    {
        float sqr = (position - focus).sqrMagnitude;
        float full = Mathf.Max(0f, fullDistance);
        float near = Mathf.Max(full, nearDistance);
        float mid = Mathf.Max(near, midDistance);
        float far = Mathf.Max(mid, farDistance);
        if (sqr <= full * full) return OUTL_RuntimeTier.Full;
        if (sqr <= near * near) return OUTL_RuntimeTier.Near;
        if (sqr <= mid * mid) return OUTL_RuntimeTier.Mid;
        if (sqr <= far * far) return OUTL_RuntimeTier.Far;
        return OUTL_RuntimeTier.Dormant;
    }

    public bool GetCellSummary(OUTL_WorldCellKey cell, out OUTL_WorldCellSummary summary)
    {
        return cellSummaries.TryGetValue(cell, out summary);
    }

    public bool TryGetNpc(OUTL_EntityId entityId, out OUTL_AbstractNpcRecord record)
    {
        record = default(OUTL_AbstractNpcRecord);
        return entityId.IsValid && npcRecords.TryGetValue(entityId.Value, out record);
    }

    public bool TryGetEntity(OUTL_EntityId entityId, out OUTL_AbstractEntityRecord record)
    {
        record = default(OUTL_AbstractEntityRecord);
        return entityId.IsValid && entityRecords.TryGetValue(entityId.Value, out record);
    }

    public void ApplyEgregoreField(in OUTL_EgregoreField field)
    {
        OUTL_WorldCellSummary summary;
        if (!cellSummaries.TryGetValue(field.Cell, out summary))
            summary = new OUTL_WorldCellSummary { Cell = field.Cell, Flags = OUTL_WorldCellFlags.Traversable };

        summary.Flags |= OUTL_WorldCellFlags.Dirty;
        if (field.Hostility > 0.45f || field.Corruption > 0.45f || field.Fear > 0.6f)
            summary.Flags |= OUTL_WorldCellFlags.Dangerous;
        if (field.Hunger > 0.45f || field.Prosperity > 0.45f)
            summary.Flags |= OUTL_WorldCellFlags.Resource;

        summary.EgregoreMood = field.Mood;
        summary.EgregoreCyclePhase = field.CyclePhase;
        summary.DominantArchetype = field.DominantArchetype;
        summary.ShadowArchetype = field.ShadowArchetype;
        summary.EgregoreFear = field.Fear;
        summary.EgregoreViolence = field.Violence;
        summary.EgregoreCorruption = field.Corruption;
        summary.EgregoreProsperity = field.Prosperity;
        summary.Danger = Mathf.Clamp01(Mathf.Max(summary.Danger, field.Hostility, field.Fear, field.Corruption));
        summary.Food = Mathf.Clamp01(Mathf.Max(summary.Food, 1f - field.Hunger));
        summary.SpawnPressure = field.SpawnPressure;
        summary.QuestPressure = field.QuestPressure;
        summary.LootPressure = field.LootPressure;
        summary.BehaviorPressure = field.BehaviorPressure;
        summary.Safety = field.Safety;
        summary.RitualTension = field.RitualTension;
        summary.LastStimulusType = OUTL_StimulusType.Egregore;
        summary.LastStimulusPosition = Vector3.zero;
        summary.LastStimulusPriority = Mathf.Clamp01(Mathf.Max(field.SpawnPressure, field.QuestPressure, field.BehaviorPressure));
        summary.LastUpdatedTime = field.LastUpdatedTime;
        summary.Version = ++Version;
        cellSummaries[field.Cell] = summary;
    }

    public int CopyCellSummaries(List<OUTL_WorldCellSummary> output)
    {
        if (output == null) return 0;
        output.Clear();
        foreach (KeyValuePair<OUTL_WorldCellKey, OUTL_WorldCellSummary> pair in cellSummaries)
            output.Add(pair.Value);
        return output.Count;
    }

    public void Clear()
    {
        entityRecords.Clear();
        npcRecords.Clear();
        entityIdsByCell.Clear();
        cellSummaries.Clear();
        Version++;
    }

    private void AddToCell(OUTL_WorldCellKey cell, int entityId)
    {
        List<int> ids;
        if (!entityIdsByCell.TryGetValue(cell, out ids))
        {
            ids = new List<int>(16);
            entityIdsByCell[cell] = ids;
        }
        if (!ids.Contains(entityId)) ids.Add(entityId);
    }

    private void RemoveFromCell(OUTL_WorldCellKey cell, int entityId)
    {
        List<int> ids;
        if (!entityIdsByCell.TryGetValue(cell, out ids) || ids == null) return;
        ids.Remove(entityId);
        if (ids.Count == 0) entityIdsByCell.Remove(cell);
    }

    private void RebuildSummary(OUTL_WorldCellKey cell, float time)
    {
        List<int> ids;
        if (!entityIdsByCell.TryGetValue(cell, out ids) || ids == null || ids.Count == 0)
        {
            cellSummaries.Remove(cell);
            return;
        }

        OUTL_WorldCellSummary summary = new OUTL_WorldCellSummary
        {
            Cell = cell,
            Flags = OUTL_WorldCellFlags.Traversable | OUTL_WorldCellFlags.Occupied,
            LastUpdatedTime = time,
            Version = Version
        };
        OUTL_WorldCellSummary previous;
        if (cellSummaries.TryGetValue(cell, out previous))
        {
            summary.EgregoreMood = previous.EgregoreMood;
            summary.EgregoreCyclePhase = previous.EgregoreCyclePhase;
            summary.DominantArchetype = previous.DominantArchetype;
            summary.ShadowArchetype = previous.ShadowArchetype;
            summary.EgregoreFear = previous.EgregoreFear;
            summary.EgregoreViolence = previous.EgregoreViolence;
            summary.EgregoreCorruption = previous.EgregoreCorruption;
            summary.EgregoreProsperity = previous.EgregoreProsperity;
            summary.SpawnPressure = previous.SpawnPressure;
            summary.QuestPressure = previous.QuestPressure;
            summary.LootPressure = previous.LootPressure;
            summary.BehaviorPressure = previous.BehaviorPressure;
            summary.Safety = previous.Safety;
            summary.RitualTension = previous.RitualTension;
            summary.Danger = previous.Danger;
            summary.Food = previous.Food;
            summary.LastStimulusType = previous.LastStimulusType;
            summary.LastStimulusPosition = previous.LastStimulusPosition;
            summary.LastStimulusPriority = previous.LastStimulusPriority;
        }

        int dominantFaction = 0;
        int dominantFactionCount = 0;
        for (int i = 0; i < ids.Count; i++)
        {
            OUTL_AbstractEntityRecord record;
            if (!entityRecords.TryGetValue(ids[i], out record)) continue;
            summary.EntityCount++;
            if ((record.Flags & OUTL_AbstractEntityFlags.Dead) != 0) summary.DeadCount++;
            else summary.AliveCount++;
            if ((record.Flags & OUTL_AbstractEntityFlags.HasCombat) != 0) summary.CombatantCount++;
            if ((record.Flags & OUTL_AbstractEntityFlags.HasPickup) != 0) summary.PickupCount++;
            if ((record.Flags & OUTL_AbstractEntityFlags.SavePersistent) != 0) summary.SavePersistentCount++;
            if (npcRecords.ContainsKey(record.EntityId.Value)) summary.NpcCount++;

            int faction = record.FactionHash;
            if (faction == 0) continue;
            int count = CountFactionInIds(ids, faction);
            if (count > dominantFactionCount)
            {
                dominantFaction = faction;
                dominantFactionCount = count;
            }
        }

        summary.DominantFactionHash = dominantFaction;
        cellSummaries[cell] = summary;
    }

    private int CountFactionInIds(List<int> ids, int factionHash)
    {
        int count = 0;
        for (int i = 0; i < ids.Count; i++)
        {
            OUTL_AbstractEntityRecord record;
            if (entityRecords.TryGetValue(ids[i], out record) && record.FactionHash == factionHash) count++;
        }
        return count;
    }
}

public sealed class OUTL_MovementCostProfile : ScriptableObject
{
    public string ProfileId = "movement_generic";
    public float BaseSpeed = 2.2f;
    public float RoadMultiplier = 0.65f;
    public float ForestMultiplier = 1.25f;
    public float SwampMultiplier = 1.75f;
    public float MountainMultiplier = 2.25f;
    public float WaterMultiplier = 4.00f;
    public float CrossingMultiplier = 0.85f;
    public float HostileMultiplier = 1.50f;
    public float DangerMultiplier = 1.35f;

    public int StableHash { get { return OUTL_WorldCellUtility.StableStringHash(ProfileId); } }

    public float GetCostMultiplier(OUTL_WorldCellFlags flags)
    {
        float cost = 1f;
        if ((flags & OUTL_WorldCellFlags.Road) != 0) cost *= Mathf.Max(0.01f, RoadMultiplier);
        if ((flags & OUTL_WorldCellFlags.Forest) != 0) cost *= Mathf.Max(0.01f, ForestMultiplier);
        if ((flags & OUTL_WorldCellFlags.Swamp) != 0) cost *= Mathf.Max(0.01f, SwampMultiplier);
        if ((flags & OUTL_WorldCellFlags.Mountain) != 0) cost *= Mathf.Max(0.01f, MountainMultiplier);
        if ((flags & OUTL_WorldCellFlags.Water) != 0) cost *= Mathf.Max(0.01f, WaterMultiplier);
        if ((flags & OUTL_WorldCellFlags.Crossing) != 0) cost *= Mathf.Max(0.01f, CrossingMultiplier);
        if ((flags & OUTL_WorldCellFlags.Hostile) != 0) cost *= Mathf.Max(0.01f, HostileMultiplier);
        if ((flags & OUTL_WorldCellFlags.Dangerous) != 0) cost *= Mathf.Max(0.01f, DangerMultiplier);
        return Mathf.Max(0.01f, cost);
    }
}

[System.Serializable]
public struct OUTL_RouteCell
{
    public OUTL_WorldCellKey Key;
    public OUTL_WorldCellFlags Flags;
    public float BaseCost;
    public float Danger;
    public int RegionId;
    public int ProvinceId;

    public bool Traversable { get { return (Flags & OUTL_WorldCellFlags.Traversable) != 0; } }
}

public sealed class OUTL_WorldRouteGraph
{
    private readonly Dictionary<OUTL_WorldCellKey, OUTL_RouteCell> cells = new Dictionary<OUTL_WorldCellKey, OUTL_RouteCell>(2048);
    private readonly List<OUTL_WorldCellKey> open = new List<OUTL_WorldCellKey>(128);
    private readonly List<OUTL_WorldCellKey> rebuilt = new List<OUTL_WorldCellKey>(128);
    private readonly Dictionary<OUTL_WorldCellKey, OUTL_WorldCellKey> cameFrom = new Dictionary<OUTL_WorldCellKey, OUTL_WorldCellKey>(256);
    private readonly Dictionary<OUTL_WorldCellKey, float> costSoFar = new Dictionary<OUTL_WorldCellKey, float>(256);

    public int Count { get { return cells.Count; } }

    public void SetCell(OUTL_RouteCell cell)
    {
        if (!cell.Key.IsValid) return;
        if (cell.BaseCost <= 0f) cell.BaseCost = 1f;
        if ((cell.Flags & OUTL_WorldCellFlags.Traversable) == 0) cell.Flags |= OUTL_WorldCellFlags.Traversable;
        cells[cell.Key] = cell;
    }

    public bool TryGetCell(OUTL_WorldCellKey key, out OUTL_RouteCell cell)
    {
        return cells.TryGetValue(key, out cell);
    }

    public float CalculateCost(OUTL_WorldCellKey from, OUTL_WorldCellKey to, OUTL_MovementCostProfile profile)
    {
        OUTL_RouteCell cell;
        float cost = 1f;
        if (cells.TryGetValue(to, out cell))
        {
            if (!cell.Traversable) return float.PositiveInfinity;
            cost = Mathf.Max(0.01f, cell.BaseCost) + Mathf.Max(0f, cell.Danger);
            if (profile != null) cost *= profile.GetCostMultiplier(cell.Flags);
        }
        else if (profile != null)
        {
            cost *= profile.GetCostMultiplier(OUTL_WorldCellFlags.Traversable);
        }

        return cost * Mathf.Max(1f, OUTL_WorldCellUtility.ManhattanDistance(from, to));
    }

    public int BuildStraightCellPath(OUTL_WorldCellKey start, OUTL_WorldCellKey end, List<OUTL_WorldCellKey> output)
    {
        if (output == null) return 0;
        output.Clear();
        int steps = Mathf.Max(Mathf.Abs(end.X - start.X), Mathf.Abs(end.Z - start.Z));
        int lastX = int.MinValue;
        int lastZ = int.MinValue;
        for (int i = 0; i <= steps; i++)
        {
            float t = steps <= 0 ? 1f : i / (float)steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(start.X, end.X, t));
            int z = Mathf.RoundToInt(Mathf.Lerp(start.Z, end.Z, t));
            if (x == lastX && z == lastZ) continue;
            output.Add(new OUTL_WorldCellKey(x, z, start.Layer));
            lastX = x;
            lastZ = z;
        }
        return output.Count;
    }

    public int BuildGridCellPath(OUTL_WorldCellKey start, OUTL_WorldCellKey end, OUTL_MovementCostProfile profile, List<OUTL_WorldCellKey> output, int maxExpansions = 512)
    {
        if (output == null) return 0;
        output.Clear();
        if (!start.IsValid || !end.IsValid) return 0;
        if (start == end)
        {
            output.Add(start);
            return 1;
        }

        open.Clear();
        rebuilt.Clear();
        cameFrom.Clear();
        costSoFar.Clear();
        open.Add(start);
        costSoFar[start] = 0f;

        bool found = false;
        int guard = Mathf.Max(8, maxExpansions);
        for (int expanded = 0; expanded < guard && open.Count > 0; expanded++)
        {
            int currentIndex = FindBestOpen(end);
            OUTL_WorldCellKey current = open[currentIndex];
            open.RemoveAt(currentIndex);
            if (current == end)
            {
                found = true;
                break;
            }

            for (int n = 0; n < 4; n++)
            {
                OUTL_WorldCellKey next = Neighbor(current, n);
                if (IsBlocked(next, profile)) continue;
                float stepCost = CalculateCost(current, next, profile);
                if (float.IsInfinity(stepCost)) continue;
                float newCost = costSoFar[current] + stepCost;
                float oldCost;
                if (costSoFar.TryGetValue(next, out oldCost) && newCost >= oldCost) continue;
                costSoFar[next] = newCost;
                cameFrom[next] = current;
                if (!open.Contains(next)) open.Add(next);
            }
        }

        if (!found) return BuildStraightCellPath(start, end, output);
        rebuilt.Add(end);
        OUTL_WorldCellKey step = end;
        int rebuildGuard = 0;
        while (step != start && rebuildGuard++ < guard)
        {
            if (!cameFrom.TryGetValue(step, out step)) break;
            rebuilt.Add(step);
        }

        for (int i = rebuilt.Count - 1; i >= 0; i--) output.Add(rebuilt[i]);
        return output.Count;
    }

    public void Clear()
    {
        cells.Clear();
    }

    private int FindBestOpen(OUTL_WorldCellKey end)
    {
        int bestIndex = 0;
        float bestScore = float.MaxValue;
        for (int i = 0; i < open.Count; i++)
        {
            OUTL_WorldCellKey key = open[i];
            float cost;
            costSoFar.TryGetValue(key, out cost);
            float score = cost + OUTL_WorldCellUtility.ManhattanDistance(key, end);
            if (score < bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }
        return bestIndex;
    }

    private static OUTL_WorldCellKey Neighbor(OUTL_WorldCellKey key, int index)
    {
        switch (index)
        {
            case 0: return new OUTL_WorldCellKey(key.X + 1, key.Z, key.Layer);
            case 1: return new OUTL_WorldCellKey(key.X - 1, key.Z, key.Layer);
            case 2: return new OUTL_WorldCellKey(key.X, key.Z + 1, key.Layer);
            default: return new OUTL_WorldCellKey(key.X, key.Z - 1, key.Layer);
        }
    }

    private bool IsBlocked(OUTL_WorldCellKey key, OUTL_MovementCostProfile profile)
    {
        OUTL_RouteCell cell;
        if (!cells.TryGetValue(key, out cell)) return false;
        if (!cell.Traversable) return true;
        if ((cell.Flags & OUTL_WorldCellFlags.Water) != 0 && (cell.Flags & OUTL_WorldCellFlags.Crossing) == 0) return true;
        if ((cell.Flags & OUTL_WorldCellFlags.Mountain) != 0 && cell.BaseCost >= 999f) return true;
        return false;
    }
}

public enum OUTL_RouteStatus
{
    Unknown = 0,
    Ready = 1,
    Blocked = 2,
    Partial = 3
}

[System.Serializable]
public struct OUTL_RouteCacheKey : System.IEquatable<OUTL_RouteCacheKey>
{
    public OUTL_WorldCellKey Start;
    public OUTL_WorldCellKey End;
    public int MovementProfileHash;
    public int WorldVersion;
    public int Flags;

    public bool Equals(OUTL_RouteCacheKey other)
    {
        return Start == other.Start && End == other.End && MovementProfileHash == other.MovementProfileHash && WorldVersion == other.WorldVersion && Flags == other.Flags;
    }

    public override bool Equals(object obj)
    {
        return obj is OUTL_RouteCacheKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = Start.GetHashCode();
            hash = hash * 31 + End.GetHashCode();
            hash = hash * 31 + MovementProfileHash;
            hash = hash * 31 + WorldVersion;
            hash = hash * 31 + Flags;
            return hash;
        }
    }
}

public sealed class OUTL_RouteResult
{
    public OUTL_RouteCacheKey Key;
    public OUTL_RouteStatus Status;
    public float TotalCost;
    public float EstimatedSeconds;
    public float LastUsedTime;
    public int UsageCount;
    public int Version;
    public readonly List<OUTL_WorldCellKey> Cells = new List<OUTL_WorldCellKey>(16);
}

public sealed class OUTL_RouteCache
{
    public int MaxRoutes = 512;
    public int TotalRequests;
    public int TotalHits;
    public int TotalCreated;

    private readonly Dictionary<OUTL_RouteCacheKey, OUTL_RouteResult> routesByKey = new Dictionary<OUTL_RouteCacheKey, OUTL_RouteResult>(512);
    private readonly List<OUTL_RouteCacheKey> keys = new List<OUTL_RouteCacheKey>(512);
    private readonly List<OUTL_WorldCellKey> scratchPath = new List<OUTL_WorldCellKey>(32);

    public int Count { get { return routesByKey.Count; } }

    public bool TryGet(OUTL_RouteCacheKey key, float time, out OUTL_RouteResult route)
    {
        TotalRequests++;
        if (routesByKey.TryGetValue(key, out route) && route != null)
        {
            route.LastUsedTime = time;
            route.UsageCount++;
            TotalHits++;
            return true;
        }
        return false;
    }

    public OUTL_RouteResult GetOrCreateStraightRoute(OUTL_WorldRouteGraph graph, OUTL_RouteCacheKey key, OUTL_MovementCostProfile profile, float time)
    {
        OUTL_RouteResult route;
        if (TryGet(key, time, out route)) return route;

        route = new OUTL_RouteResult
        {
            Key = key,
            Status = OUTL_RouteStatus.Ready,
            LastUsedTime = time,
            UsageCount = 1,
            Version = key.WorldVersion
        };

        if (graph != null) graph.BuildGridCellPath(key.Start, key.End, profile, scratchPath);
        else BuildFallbackPath(key.Start, key.End, scratchPath);

        route.TotalCost = 0f;
        for (int i = 0; i < scratchPath.Count; i++)
        {
            route.Cells.Add(scratchPath[i]);
            if (i <= 0) continue;
            float cost = graph != null ? graph.CalculateCost(scratchPath[i - 1], scratchPath[i], profile) : 1f;
            if (float.IsInfinity(cost))
            {
                route.Status = OUTL_RouteStatus.Blocked;
                break;
            }
            route.TotalCost += cost;
        }

        float speed = profile != null ? Mathf.Max(0.01f, profile.BaseSpeed) : 2.2f;
        route.EstimatedSeconds = route.TotalCost / speed;
        Store(route);
        TotalCreated++;
        return route;
    }

    public void Store(OUTL_RouteResult route)
    {
        if (route == null) return;
        if (!routesByKey.ContainsKey(route.Key))
        {
            if (keys.Count >= Mathf.Max(1, MaxRoutes)) RemoveLeastRecentlyUsed();
            keys.Add(route.Key);
        }
        routesByKey[route.Key] = route;
    }

    public int ClearOld(float time, float maxAge)
    {
        int removed = 0;
        float age = Mathf.Max(0f, maxAge);
        for (int i = keys.Count - 1; i >= 0; i--)
        {
            OUTL_RouteResult route;
            if (!routesByKey.TryGetValue(keys[i], out route) || route == null || time - route.LastUsedTime > age)
            {
                routesByKey.Remove(keys[i]);
                keys.RemoveAt(i);
                removed++;
            }
        }
        return removed;
    }

    public void Clear()
    {
        routesByKey.Clear();
        keys.Clear();
    }

    private void RemoveLeastRecentlyUsed()
    {
        if (keys.Count == 0) return;
        int index = 0;
        float oldest = float.MaxValue;
        for (int i = 0; i < keys.Count; i++)
        {
            OUTL_RouteResult route;
            float t = routesByKey.TryGetValue(keys[i], out route) && route != null ? route.LastUsedTime : float.MinValue;
            if (t < oldest)
            {
                oldest = t;
                index = i;
            }
        }
        routesByKey.Remove(keys[index]);
        keys.RemoveAt(index);
    }

    private static void BuildFallbackPath(OUTL_WorldCellKey start, OUTL_WorldCellKey end, List<OUTL_WorldCellKey> output)
    {
        if (output == null) return;
        output.Clear();
        int steps = Mathf.Max(Mathf.Abs(end.X - start.X), Mathf.Abs(end.Z - start.Z));
        for (int i = 0; i <= steps; i++)
        {
            float t = steps <= 0 ? 1f : i / (float)steps;
            output.Add(new OUTL_WorldCellKey(Mathf.RoundToInt(Mathf.Lerp(start.X, end.X, t)), Mathf.RoundToInt(Mathf.Lerp(start.Z, end.Z, t)), start.Layer));
        }
    }
}

public static class OUTL_WorldMapDebugTexture
{
    public static Texture2D BuildCellSummaryTexture(List<OUTL_WorldCellSummary> summaries, int width, int height)
    {
        int w = Mathf.Max(1, width);
        int h = Mathf.Max(1, height);
        Texture2D texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(8, 10, 12, 255);

        if (summaries != null && summaries.Count > 0)
        {
            int minX = summaries[0].Cell.X;
            int minZ = summaries[0].Cell.Z;
            int maxX = minX;
            int maxZ = minZ;
            for (int i = 1; i < summaries.Count; i++)
            {
                OUTL_WorldCellKey key = summaries[i].Cell;
                if (key.X < minX) minX = key.X;
                if (key.Z < minZ) minZ = key.Z;
                if (key.X > maxX) maxX = key.X;
                if (key.Z > maxZ) maxZ = key.Z;
            }

            int spanX = Mathf.Max(1, maxX - minX + 1);
            int spanZ = Mathf.Max(1, maxZ - minZ + 1);
            for (int i = 0; i < summaries.Count; i++)
            {
                OUTL_WorldCellSummary summary = summaries[i];
                int x = Mathf.Clamp(Mathf.RoundToInt((summary.Cell.X - minX) / (float)spanX * (w - 1)), 0, w - 1);
                int y = Mathf.Clamp(Mathf.RoundToInt((summary.Cell.Z - minZ) / (float)spanZ * (h - 1)), 0, h - 1);
                pixels[y * w + x] = ColorForSummary(summary);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }

    public static void OverlayRoute(Texture2D texture, List<OUTL_WorldCellKey> route, Color32 color)
    {
        if (texture == null || route == null || route.Count == 0) return;
        int w = texture.width;
        int h = texture.height;
        int minX = route[0].X;
        int minZ = route[0].Z;
        int maxX = minX;
        int maxZ = minZ;
        for (int i = 1; i < route.Count; i++)
        {
            OUTL_WorldCellKey key = route[i];
            if (key.X < minX) minX = key.X;
            if (key.Z < minZ) minZ = key.Z;
            if (key.X > maxX) maxX = key.X;
            if (key.Z > maxZ) maxZ = key.Z;
        }

        int spanX = Mathf.Max(1, maxX - minX + 1);
        int spanZ = Mathf.Max(1, maxZ - minZ + 1);
        for (int i = 0; i < route.Count; i++)
        {
            int x = Mathf.Clamp(Mathf.RoundToInt((route[i].X - minX) / (float)spanX * (w - 1)), 0, w - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt((route[i].Z - minZ) / (float)spanZ * (h - 1)), 0, h - 1);
            texture.SetPixel(x, y, color);
        }
        texture.Apply(false, false);
    }

    private static Color32 ColorForSummary(OUTL_WorldCellSummary summary)
    {
        if (summary.DeadCount > 0) return new Color32(110, 45, 45, 255);
        if (summary.CombatantCount > 0) return new Color32(185, 95, 35, 255);
        if (summary.NpcCount > 0) return new Color32(50, 135, 190, 255);
        if (summary.PickupCount > 0) return new Color32(75, 170, 95, 255);
        if (summary.EntityCount > 0) return new Color32(140, 140, 145, 255);
        return new Color32(12, 14, 18, 255);
    }
}
