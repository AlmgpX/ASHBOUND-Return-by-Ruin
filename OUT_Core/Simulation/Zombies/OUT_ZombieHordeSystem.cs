using System.Collections.Generic;
using UnityEngine;
using Time = OUT_SimTime;

[DefaultExecutionOrder(-7600)]
[DisallowMultipleComponent]
public class OUT_ZombieHordeSystem : MonoBehaviour, IOutSimSystem
{
    public static OUT_ZombieHordeSystem Instance { get; private set; }
    public static OUT_ZombieHordeProfile DefaultProfile { get { return Instance != null ? Instance.defaultProfile : null; } }

    [Header("Profile")]
    [SerializeField] private OUT_ZombieHordeProfile defaultProfile;

    [Header("Center")]
    [SerializeField] private Transform runtimeCenter;
    [SerializeField] private bool useMainCameraIfMissing = true;

    [Header("Tick Budget")]
    [SerializeField] private OUT_SimulationTier simulationTiers = OUT_SimulationTier.Full;
    [SerializeField][Min(1)] private int agentsPerTick = 96;
    [SerializeField] private bool fallbackUpdateWhenNoSimulationService = true;

    [Header("LOD")]
    [SerializeField][Min(0.05f)] private float tierRefreshInterval = 0.5f;
    [SerializeField][Min(1)] private int tierUpdatesPerTick = 128;

    [Header("Debug")]
    [SerializeField] private bool debugStats = false;

    private readonly List<OUT_ZombieHordeAgent> agents = new List<OUT_ZombieHordeAgent>(1024);
    private int tickCursor;
    private int tierCursor;
    private float nextTierRefresh;
    private float lastFallbackTime;

    public string SimSystemName { get { return "OUT_ZombieHordeSystem"; } }
    public OUT_SimulationTier Tiers { get { return simulationTiers; } }
    public int AgentCount { get { return agents.Count; } }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        OUT_SimulationService service = OUT_SimulationService.Instance;
        if (service != null) service.RegisterSystem(this);
    }

    private void OnDisable()
    {
        OUT_SimulationService service = OUT_SimulationService.Instance;
        if (service != null) service.UnregisterSystem(this);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!fallbackUpdateWhenNoSimulationService) return;
        if (OUT_SimulationService.Instance != null) return;

        float now = Time.time;
        float dt = lastFallbackTime > 0f ? now - lastFallbackTime : UnityEngine.Time.deltaTime;
        lastFallbackTime = now;
        Process(now, Mathf.Max(0.001f, dt));
    }

    public void Tick(OUT_SimWorld world, in OUT_SimTickContext context)
    {
        Process(context.Time, context.DeltaTime);
    }

    public static void Register(OUT_ZombieHordeAgent agent)
    {
        if (agent == null) return;
        OUT_ZombieHordeSystem system = EnsureExists();
        if (!system.agents.Contains(agent)) system.agents.Add(agent);
    }

    public static void Unregister(OUT_ZombieHordeAgent agent)
    {
        if (Instance == null || agent == null) return;
        Instance.agents.Remove(agent);
    }

    public static OUT_ZombieHordeSystem EnsureExists()
    {
        if (Instance != null) return Instance;
        OUT_ZombieHordeSystem existing = FindObjectOfType<OUT_ZombieHordeSystem>();
        if (existing != null) return existing;
        GameObject go = new GameObject("OUT_ZombieHordeSystem");
        return go.AddComponent<OUT_ZombieHordeSystem>();
    }

    private void Process(float now, float deltaTime)
    {
        if (agents.Count == 0) return;

        CleanDeadSlots();
        Vector3 center = GetRuntimeCenterPosition();

        if (now >= nextTierRefresh)
        {
            nextTierRefresh = now + tierRefreshInterval;
            RefreshTiers(center);
        }

        int count = agents.Count;
        int budget = Mathf.Min(agentsPerTick, count);
        for (int i = 0; i < budget; i++)
        {
            if (tickCursor >= agents.Count) tickCursor = 0;
            OUT_ZombieHordeAgent agent = agents[tickCursor++];
            if (agent == null || agent.IsDead) continue;

            OUT_ZombieHordeProfile p = agent.Profile != null ? agent.Profile : defaultProfile;
            float interval = GetTierInterval(agent.RuntimeTier, p);
            float scaledDelta = Mathf.Max(deltaTime, interval);
            agent.HordeTick(now, scaledDelta, center);
        }
    }

    private float GetTierInterval(OUT_RuntimeTier tier, OUT_ZombieHordeProfile profile)
    {
        if (profile == null) return 0.1f;
        switch (tier)
        {
            case OUT_RuntimeTier.Dormant: return profile.FarThinkInterval * 2f;
            case OUT_RuntimeTier.Far: return profile.FarThinkInterval;
            case OUT_RuntimeTier.Mid: return profile.MidThinkInterval;
            case OUT_RuntimeTier.Near:
            case OUT_RuntimeTier.Full:
            default: return profile.NearThinkInterval;
        }
    }

    private void RefreshTiers(Vector3 center)
    {
        int count = agents.Count;
        if (count == 0) return;

        int budget = Mathf.Min(tierUpdatesPerTick, count);
        for (int i = 0; i < budget; i++)
        {
            if (tierCursor >= agents.Count) tierCursor = 0;
            OUT_ZombieHordeAgent agent = agents[tierCursor++];
            if (agent == null || agent.IsDead) continue;

            OUT_ZombieHordeProfile p = agent.Profile != null ? agent.Profile : defaultProfile;
            if (p == null) continue;

            float sqr = (agent.transform.position - center).sqrMagnitude;
            OUT_RuntimeTier tier;
            if (sqr <= p.NearDistance * p.NearDistance) tier = OUT_RuntimeTier.Near;
            else if (sqr <= p.MidDistance * p.MidDistance) tier = OUT_RuntimeTier.Mid;
            else if (sqr <= p.FarDistance * p.FarDistance) tier = OUT_RuntimeTier.Far;
            else tier = OUT_RuntimeTier.Dormant;

            agent.OnRuntimeTierChanged(agent.RuntimeTier, tier);
        }
    }

    private void CleanDeadSlots()
    {
        for (int i = agents.Count - 1; i >= 0; i--)
        {
            if (agents[i] == null || agents[i].IsDead)
                agents.RemoveAt(i);
        }
    }

    private Vector3 GetRuntimeCenterPosition()
    {
        if (runtimeCenter != null) return runtimeCenter.position;
        if (useMainCameraIfMissing && Camera.main != null) return Camera.main.transform.position;
        return Vector3.zero;
    }

    private void OnGUI()
    {
        if (!debugStats) return;
        GUI.Label(new Rect(12, 60, 700, 22), "OUT Zombie Horde: agents=" + agents.Count + " cursor=" + tickCursor + " tierCursor=" + tierCursor);
    }
}
