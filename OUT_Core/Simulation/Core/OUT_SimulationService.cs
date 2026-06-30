using System;
using System.Collections.Generic;
using Stopwatch = System.Diagnostics.Stopwatch;
using UnityEngine;

[DefaultExecutionOrder(-7600)]
[DisallowMultipleComponent]
public class OUT_SimulationService : MonoBehaviour
{
    public static OUT_SimulationService Instance { get; private set; }

    [Header("Lifecycle")]
    [SerializeField] private bool dontDestroyOnLoad = false;
    [SerializeField] private bool autoDiscoverSystemsOnStart = true;
    [SerializeField] private bool autoRegisterConsoleCommands = true;

    [Header("Simulation Clock")]
    [SerializeField] [Min(1f)] private float fullTickRate = 30f;
    [SerializeField] [Min(0.1f)] private float reducedTickRate = 5f;
    [SerializeField] [Min(0.05f)] private float abstractTickRate = 1f;
    [SerializeField] [Min(0.05f)] private float randomWorldTickRate = 1f;
    [SerializeField] [Min(0f)] private float simulationTimeScale = 1f;
    [SerializeField] [Min(1)] private int maxStepsPerFrame = 4;
    [SerializeField] private bool paused;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool logTicks;

    [Header("Random World Ticks")]
    [SerializeField] [Min(0)] private int randomTicksPerRandomWorldStep = 4;
    [SerializeField] private bool discoverRandomReceivers = true;

    [Header("Debug")]
    [SerializeField] private bool drawDebugStats;

    private readonly OUT_SimWorld world = new OUT_SimWorld();
    private readonly List<IOutSimSystem> systems = new List<IOutSimSystem>(64);
    private readonly List<IOutRandomWorldTickReceiver> randomReceivers = new List<IOutRandomWorldTickReceiver>(128);
    private readonly Stopwatch stopwatch = new Stopwatch();

    private OUT_SimulationStats stats;
    private float fullAccumulator;
    private float reducedAccumulator;
    private float abstractAccumulator;
    private float randomWorldAccumulator;
    private int fullTick;
    private int reducedTick;
    private int abstractTick;
    private int randomWorldTick;
    private bool stepRequested;
    private float runtimeTime;
    private float runtimeDeltaTime;
    private float runtimeUnscaledDeltaTime;

    public OUT_SimWorld World => world;
    public OUT_SimulationStats Stats => stats;
    public bool Paused { get => paused; set => paused = value; }
    public float SimulationTimeScale { get => simulationTimeScale; set => simulationTimeScale = Mathf.Max(0f, value); }
    public float FullTickRate { get => fullTickRate; set => fullTickRate = Mathf.Max(1f, value); }
    public float ReducedTickRate { get => reducedTickRate; set => reducedTickRate = Mathf.Max(0.1f, value); }
    public float AbstractTickRate { get => abstractTickRate; set => abstractTickRate = Mathf.Max(0.05f, value); }
    public float RandomWorldTickRate { get => randomWorldTickRate; set => randomWorldTickRate = Mathf.Max(0.05f, value); }
    public int MaxStepsPerFrame { get => maxStepsPerFrame; set => maxStepsPerFrame = Mathf.Max(1, value); }
    public int RandomTicksPerRandomWorldStep { get => randomTicksPerRandomWorldStep; set => randomTicksPerRandomWorldStep = Mathf.Max(0, value); }
    public float RuntimeTime => runtimeTime;
    public float RuntimeDeltaTime => runtimeDeltaTime;
    public float RuntimeUnscaledDeltaTime => runtimeUnscaledDeltaTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        stats.Initialize();
    }

    private void Start()
    {
        if (autoDiscoverSystemsOnStart)
            DiscoverSystems();

        if (autoRegisterConsoleCommands)
            EnsureConsoleCommands();
    }

    private void Update()
    {
        stats.BeginFrame(Time.frameCount);
        stats.RegisteredSystems = systems.Count;
        stats.RegisteredRandomReceivers = randomReceivers.Count;

        runtimeUnscaledDeltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        runtimeDeltaTime = 0f;

        if (paused && !stepRequested)
        {
            stats.Accumulator = fullAccumulator;
            stats.InterpolationAlpha = GetFullDelta() > 0f ? Mathf.Clamp01(fullAccumulator / GetFullDelta()) : 0f;
            return;
        }

        float dt = runtimeUnscaledDeltaTime * simulationTimeScale;

        if (stepRequested && paused)
            dt = GetFullDelta();

        runtimeDeltaTime = Mathf.Max(0f, dt);
        runtimeTime += runtimeDeltaTime;

        StepTier(OUT_SimulationTier.Full, ref fullAccumulator, GetFullDelta(), ref fullTick, dt);
        StepTier(OUT_SimulationTier.Reduced, ref reducedAccumulator, GetReducedDelta(), ref reducedTick, dt);
        StepTier(OUT_SimulationTier.Abstract, ref abstractAccumulator, GetAbstractDelta(), ref abstractTick, dt);
        StepTier(OUT_SimulationTier.RandomWorld, ref randomWorldAccumulator, GetRandomWorldDelta(), ref randomWorldTick, dt);

        stats.Accumulator = fullAccumulator;
        stats.InterpolationAlpha = GetFullDelta() > 0f ? Mathf.Clamp01(fullAccumulator / GetFullDelta()) : 0f;
        stepRequested = false;
    }

    public void RequestSingleStep()
    {
        stepRequested = true;
    }

    public void DiscoverSystems()
    {
        systems.Clear();
        randomReceivers.Clear();

        MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null)
                continue;

            if (behaviour is IOutSimSystem simSystem)
                RegisterSystem(simSystem);

            if (discoverRandomReceivers && behaviour is IOutRandomWorldTickReceiver randomReceiver)
                RegisterRandomReceiver(randomReceiver);
        }
    }

    public void RegisterSystem(IOutSimSystem system)
    {
        if (system == null || systems.Contains(system))
            return;

        systems.Add(system);
    }

    public void UnregisterSystem(IOutSimSystem system)
    {
        if (system == null)
            return;

        systems.Remove(system);
    }

    public void RegisterRandomReceiver(IOutRandomWorldTickReceiver receiver)
    {
        if (receiver == null || randomReceivers.Contains(receiver))
            return;

        randomReceivers.Add(receiver);
    }

    public void UnregisterRandomReceiver(IOutRandomWorldTickReceiver receiver)
    {
        if (receiver == null)
            return;

        randomReceivers.Remove(receiver);
    }

    public void ResetStats()
    {
        stats.Reset();
    }

    public void ResetClock()
    {
        fullAccumulator = 0f;
        reducedAccumulator = 0f;
        abstractAccumulator = 0f;
        randomWorldAccumulator = 0f;
        fullTick = 0;
        reducedTick = 0;
        abstractTick = 0;
        randomWorldTick = 0;
        runtimeTime = 0f;
        runtimeDeltaTime = 0f;
        runtimeUnscaledDeltaTime = 0f;
        world.CurrentTick = 0;
        world.CurrentTime = 0f;
    }

    public string BuildDebugReport()
    {
        return stats.BuildSummary() +
               "\ntime:" + runtimeTime.ToString("0.###") +
               " dt:" + runtimeDeltaTime.ToString("0.####") +
               " rate full:" + fullTickRate.ToString("0.###") +
               " reduced:" + reducedTickRate.ToString("0.###") +
               " abstract:" + abstractTickRate.ToString("0.###") +
               " random:" + randomWorldTickRate.ToString("0.###") +
               " scale:" + simulationTimeScale.ToString("0.###") +
               " paused:" + paused +
               " maxSteps:" + maxStepsPerFrame +
               " randomTicks:" + randomTicksPerRandomWorldStep +
               " entities:" + world.EntityCount;
    }

    private void StepTier(OUT_SimulationTier tier, ref float accumulator, float tickDelta, ref int tickCounter, float delta)
    {
        if (tickDelta <= 0f)
            return;

        accumulator += delta;
        int steps = 0;

        while (accumulator >= tickDelta)
        {
            if (steps >= maxStepsPerFrame)
            {
                accumulator = Mathf.Min(accumulator, tickDelta);
                stats.DroppedSteps++;
                return;
            }

            bool isCatchUp = steps > 0;
            TickTier(tier, tickCounter, tickDelta, isCatchUp);
            tickCounter++;
            accumulator -= tickDelta;
            steps++;
            stats.TotalSteps++;
        }
    }

    private void TickTier(OUT_SimulationTier tier, int tick, float tickDelta, bool isCatchUp)
    {
        uint seed = Hash(world.Seed, tick, (int)tier);
        OUT_SimTickContext context = new OUT_SimTickContext(
            tick,
            runtimeTime,
            tickDelta,
            simulationTimeScale,
            seed,
            tier,
            isCatchUp);

        world.CurrentTick = tick;
        world.CurrentTime = context.Time;

        stopwatch.Restart();

        if (tier == OUT_SimulationTier.RandomWorld)
            TickRandomWorld(context);

        for (int i = 0; i < systems.Count; i++)
        {
            IOutSimSystem system = systems[i];
            if (system == null)
                continue;

            if ((system.Tiers & tier) == 0)
                continue;

            try
            {
                system.Tick(world, in context);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("OUT_SimulationService: system '" + system.SimSystemName + "' failed on " + tier + " tick: " + ex.Message);
            }
        }

        stopwatch.Stop();
        float ms = (float)stopwatch.Elapsed.TotalMilliseconds;
        stats.Record(tier, ms);

        if (logTicks)
            UnityEngine.Debug.Log("OUT Sim " + tier + " tick " + tick + " " + ms.ToString("0.000") + "ms");
    }

    private void TickRandomWorld(in OUT_SimTickContext context)
    {
        if (randomTicksPerRandomWorldStep <= 0 || randomReceivers.Count == 0)
            return;

        for (int i = 0; i < randomTicksPerRandomWorldStep; i++)
        {
            uint value = Hash(context.RandomSeed, i, randomReceivers.Count);
            int index = (int)(value % (uint)randomReceivers.Count);
            IOutRandomWorldTickReceiver receiver = randomReceivers[index];
            if (receiver == null)
                continue;

            try
            {
                receiver.RandomWorldTick(world, in context, value);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("OUT_SimulationService: random receiver failed: " + ex.Message);
            }
        }
    }

    private void EnsureConsoleCommands()
    {
        OUT_SimulationConsoleCommands commands = GetComponent<OUT_SimulationConsoleCommands>();
        if (commands == null)
            commands = gameObject.AddComponent<OUT_SimulationConsoleCommands>();
    }

    private float GetFullDelta() => 1f / Mathf.Max(1f, fullTickRate);
    private float GetReducedDelta() => 1f / Mathf.Max(0.1f, reducedTickRate);
    private float GetAbstractDelta() => 1f / Mathf.Max(0.05f, abstractTickRate);
    private float GetRandomWorldDelta() => 1f / Mathf.Max(0.05f, randomWorldTickRate);

    public static uint Hash(int a, int b, int c)
    {
        unchecked
        {
            return Hash((uint)a, b, c);
        }
    }

    public static uint Hash(uint a, int b, int c)
    {
        unchecked
        {
            uint x = a;
            x ^= (uint)b + 0x9e3779b9u + (x << 6) + (x >> 2);
            x ^= (uint)c + 0x85ebca6bu + (x << 6) + (x >> 2);
            x ^= x >> 16;
            x *= 0x7feb352du;
            x ^= x >> 15;
            x *= 0x846ca68bu;
            x ^= x >> 16;
            return x;
        }
    }

    private void OnGUI()
    {
        if (!drawDebugStats)
            return;

        GUI.Label(new Rect(10, 10, 980, 80), BuildDebugReport());
    }
}
