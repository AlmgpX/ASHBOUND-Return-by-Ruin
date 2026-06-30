using System.Collections.Generic;
using UnityEngine;

public sealed class OUTL_EgregoreSystem
{
    private readonly List<OUTL_EgregoreRuntime> runtimes = new List<OUTL_EgregoreRuntime>(16);

    public int Count { get { return runtimes.Count; } }

    public void Register(OUTL_EgregoreRuntime runtime)
    {
        if (runtime == null || runtimes.Contains(runtime)) return;
        runtimes.Add(runtime);
    }

    public void Unregister(OUTL_EgregoreRuntime runtime)
    {
        runtimes.Remove(runtime);
    }

    public void Add(OUTL_EgregoreRuntime runtime)
    {
        Register(runtime);
    }

    public void Remove(OUTL_EgregoreRuntime runtime)
    {
        Unregister(runtime);
    }

    public void ApplyStimulus(OUTL_Stimulus stimulus, OUTL_EgregoreDef def)
    {
        for (int i = 0; i < runtimes.Count; i++)
            if (runtimes[i] != null)
                runtimes[i].ApplyStimulus(stimulus, def);
    }

    public void ApplyEvent(in OUTL_Event evt, OUTL_EgregoreDef def)
    {
        for (int i = 0; i < runtimes.Count; i++)
            if (runtimes[i] != null)
                runtimes[i].ApplyEvent(evt, def);
    }

    public int Tick(float time, int signalBudget)
    {
        int budget = Mathf.Max(0, signalBudget);
        int processed = 0;
        for (int i = 0; i < runtimes.Count && processed < budget; i++)
        {
            OUTL_EgregoreRuntime runtime = runtimes[i];
            if (runtime == null) continue;
            processed += runtime.ProcessSignals(time, budget - processed);
        }
        return processed;
    }

    public void Broadcast(OUTL_EgregoreSignal signal)
    {
        if (signal.SignalType == OUTL_EgregoreSignalType.None) return;
        for (int i = 0; i < runtimes.Count; i++)
        {
            OUTL_EgregoreRuntime runtime = runtimes[i];
            if (runtime == null) continue;
            if (!string.IsNullOrEmpty(signal.TargetId) && signal.TargetId != runtime.EgregoreId) continue;
            runtime.ReceiveSignal(signal);
        }
    }
}

[DisallowMultipleComponent]
public sealed class OUTL_EgregoreComponent : MonoBehaviour, OUTL_ITickable, OUTL_IEventListener, OUTL_IComponentSaveParticipant
{
    public OUTL_EgregoreDef Def;
    public OUTL_EgregoreScope ScopeOverride = OUTL_EgregoreScope.Local;
    public OUTL_EgregoreScale ScaleOverride = OUTL_EgregoreScale.Local;
    public bool UseDefScope = true;
    public bool UseDefScale = true;
    public bool AutoRegister = true;
    public OUTL_EgregoreInfluenceZone[] InfluenceZones;

    public OUTL_EgregoreRuntime Runtime { get { return runtime; } }
    public OUTL_EgregoreSystem System { get { return system; } }

    private readonly OUTL_EgregoreRuntime runtime = new OUTL_EgregoreRuntime();
    private readonly OUTL_EgregoreSystem system = new OUTL_EgregoreSystem();
    private readonly List<OUTL_SectorCellStats> sectorStats = new List<OUTL_SectorCellStats>(128);
    private bool registered;

    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled && Def != null; } }
    public OUTL_TickLane OUTL_TickLane { get { return OUTL_TickLane.Logic; } }
    public float OUTL_TickInterval { get { return ResolveInterval(); } }
    public string OUTL_SaveKey { get { return "OUTL_EgregoreRuntime"; } }

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        OUTL_StimulusBus.OnStimulus += OnStimulus;
        if (OUTL_World.Instance != null) OUTL_World.Instance.Events.Register(this);
        if (AutoRegister) Register();
    }

    private void OnDisable()
    {
        OUTL_StimulusBus.OnStimulus -= OnStimulus;
        if (OUTL_World.Instance != null) OUTL_World.Instance.Events.Unregister(this);
        Unregister();
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

    public void OUTL_OnEvent(in OUTL_Event evt, OUTL_World world)
    {
        if (Def == null) return;
        runtime.ApplyEvent(evt, Def);
    }

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        if (world == null || Def == null) return;
        RefreshSectorStats(world);
        OUTL_EgregoreSignal signal = runtime.Tick(Def, time, deltaTime, transform.position);
        world.WorldLedger.ApplyEgregoreField(runtime.BuildField(transform.position, world.WorldLedger.ActivityCellSize, time));
        if (signal.SignalType != OUTL_EgregoreSignalType.None)
        {
            system.Broadcast(signal);
            OUTL_StimulusBus.Emit(new OUTL_Stimulus(OUTL_StimulusType.Egregore, OUTL_EntityId.None, transform.position, Mathf.Max(0.1f, Def.InfluenceRadius), signal.Intensity, 1f, signal.Intensity, signal.Ttl, runtime.BuildStimulusKey(signal.Key), null));
            world.Events.Emit(new OUTL_Event(OUTL_EventType.EgregoreMoodChanged, OUTL_EntityId.None, OUTL_EntityId.None) { Key = runtime.BuildStimulusKey(signal.Key), IntValue = (int)runtime.CurrentCyclePhase, FloatValue = signal.Intensity, Point = transform.position });
        }

        system.Tick(time, world.MaxEgregoreSignalsPerFrame);
    }

    public void OUTL_Capture(OUTL_ComponentSaveWriter writer)
    {
        runtime.Capture(writer);
    }

    public void OUTL_Restore(OUTL_ComponentSaveReader reader)
    {
        runtime.Restore(reader);
    }

    private void OnStimulus(OUTL_Stimulus stimulus)
    {
        if (Def == null) return;
        if (!OwnsPosition(stimulus.Position)) return;
        runtime.ApplyStimulus(stimulus, Def);
    }

    private void Initialize()
    {
        runtime.Initialize(Def);
        if (!UseDefScope) runtime.Scope = ScopeOverride;
        else if (!UseDefScale) runtime.Scope = ScopeFromScale(ScaleOverride);
        system.Register(runtime);
    }

    private void RefreshSectorStats(OUTL_World world)
    {
        runtime.LastSectorEntityCount = 0;
        runtime.LastSectorStimulusCount = 0;
        if (world == null) return;
        world.Sectors.CopyCellStats(sectorStats);
        for (int i = 0; i < sectorStats.Count; i++)
        {
            OUTL_SectorCellStats cell = sectorStats[i];
            if (!OwnsSectorId(cell.CellId)) continue;
            runtime.LastSectorEntityCount += cell.EntityCount;
            runtime.LastSectorStimulusCount += cell.StimulusCount;
        }
    }

    private bool OwnsPosition(Vector3 position)
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null || Def == null) return true;
        if ((position - transform.position).sqrMagnitude <= Mathf.Max(0.1f, Def.InfluenceRadius) * Mathf.Max(0.1f, Def.InfluenceRadius)) return true;
        int sectorId = world.Sectors.CellToId(world.Sectors.WorldToCell(position));
        return OwnsSectorId(sectorId);
    }

    private bool OwnsSectorId(int sectorId)
    {
        int[] sectors = runtime.OwnedSectorIds;
        if (sectors == null || sectors.Length == 0) return true;
        for (int i = 0; i < sectors.Length; i++)
            if (sectors[i] == sectorId)
                return true;

        if (InfluenceZones != null)
        {
            for (int i = 0; i < InfluenceZones.Length; i++)
                if (InfluenceZones[i] != null && InfluenceZones[i].ContainsSector(sectorId))
                    return true;
        }
        return false;
    }

    private float ResolveInterval()
    {
        OUTL_EgregoreScope scope = ResolveScope();
        OUTL_World world = OUTL_World.Instance;
        OUTL_TickProfile profile = world != null ? world.TickProfile : null;
        if (profile != null)
        {
            if (scope == OUTL_EgregoreScope.World) return Mathf.Max(0.1f, profile.egregoreWorldInterval);
            if (scope == OUTL_EgregoreScope.Regional || scope == OUTL_EgregoreScope.Faction) return Mathf.Max(0.1f, profile.egregoreRegionalInterval);
            return Mathf.Max(0.1f, profile.egregoreLocalInterval);
        }
        return Def != null ? Mathf.Max(0.1f, Def.UpdateInterval) : 1f;
    }

    private OUTL_EgregoreScope ResolveScope()
    {
        if (UseDefScope && Def != null) return Def.Scope;
        if (!UseDefScale) return ScopeOverride;
        return ScopeFromScale(ScaleOverride);
    }

    private static OUTL_EgregoreScope ScopeFromScale(OUTL_EgregoreScale scale)
    {
        if (scale == OUTL_EgregoreScale.World) return OUTL_EgregoreScope.World;
        if (scale == OUTL_EgregoreScale.Regional) return OUTL_EgregoreScope.Regional;
        return OUTL_EgregoreScope.Local;
    }
}
