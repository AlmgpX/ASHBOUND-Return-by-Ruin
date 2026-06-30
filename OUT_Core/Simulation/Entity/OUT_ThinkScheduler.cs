using System.Collections.Generic;
using UnityEngine;
using Time = OUT_SimTime;

[DefaultExecutionOrder(-7900)]
[DisallowMultipleComponent]
public class OUT_ThinkScheduler : MonoBehaviour, IOutSimSystem
{
    public static OUT_ThinkScheduler Instance { get; private set; }

    [Header("Lifecycle")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private int initialCapacity = 1024;
    [SerializeField] private bool randomizeInitialOffset = true;

    [Header("Simulation")]
    [SerializeField] private OUT_SimulationTier simulationTiers = OUT_SimulationTier.Full;
    [SerializeField] private bool fallbackUpdateWhenNoSimulationService = true;

    [Header("Debug")]
    [SerializeField] private bool logExceptions = true;

    private readonly List<Entry> entries = new List<Entry>(1024);
    private readonly Dictionary<IOutThinkable, int> indexByThinkable = new Dictionary<IOutThinkable, int>(1024);

    public string SimSystemName { get { return "OUT_ThinkScheduler"; } }
    public OUT_SimulationTier Tiers { get { return simulationTiers; } }

    private struct Entry
    {
        public IOutThinkable Thinkable;
        public float NextTime;
        public float LastTime;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (entries.Capacity < initialCapacity) entries.Capacity = initialCapacity;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (OUT_SimulationService.Instance != null)
            OUT_SimulationService.Instance.RegisterSystem(this);
    }

    private void OnDisable()
    {
        if (OUT_SimulationService.Instance != null)
            OUT_SimulationService.Instance.UnregisterSystem(this);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!fallbackUpdateWhenNoSimulationService)
            return;

        if (OUT_SimulationService.Instance != null)
            return;

        Process(Time.time);
    }

    public void Tick(OUT_SimWorld world, in OUT_SimTickContext context)
    {
        Process(context.Time);
    }

    private void Process(float now)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            IOutThinkable thinkable = entry.Thinkable;

            if (thinkable == null)
            {
                RemoveAt(i);
                i--;
                continue;
            }

            if (!thinkable.IsThinkEnabled)
                continue;

            if (now < entry.NextTime)
                continue;

            float interval = Mathf.Max(0.01f, thinkable.ThinkInterval);
            float delta = entry.LastTime > 0f ? now - entry.LastTime : interval;
            entry.LastTime = now;
            entry.NextTime = now + interval;
            entries[i] = entry;

            try
            {
                thinkable.OutThink(delta);
            }
            catch (System.Exception ex)
            {
                if (logExceptions)
                    Debug.LogException(ex);
            }
        }
    }

    public static OUT_ThinkScheduler EnsureExists()
    {
        if (Instance != null) return Instance;
        OUT_ThinkScheduler existing = FindObjectOfType<OUT_ThinkScheduler>();
        if (existing != null) return existing;
        GameObject go = new GameObject("OUT_ThinkScheduler");
        return go.AddComponent<OUT_ThinkScheduler>();
    }

    public static void Register(IOutThinkable thinkable)
    {
        if (thinkable == null) return;
        EnsureExists().RegisterInternal(thinkable);
    }

    public static void Unregister(IOutThinkable thinkable)
    {
        if (Instance != null) Instance.UnregisterInternal(thinkable);
    }

    public static void RegisterFromGameObject(GameObject go)
    {
        if (go == null) return;
        MonoBehaviour[] behaviours = go.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
            if (behaviours[i] is IOutThinkable thinkable)
                Register(thinkable);
    }

    public static void UnregisterFromGameObject(GameObject go)
    {
        if (go == null || Instance == null) return;
        MonoBehaviour[] behaviours = go.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
            if (behaviours[i] is IOutThinkable thinkable)
                Unregister(thinkable);
    }

    private void RegisterInternal(IOutThinkable thinkable)
    {
        if (indexByThinkable.ContainsKey(thinkable))
            return;

        float interval = Mathf.Max(0.01f, thinkable.ThinkInterval);
        Entry entry = new Entry
        {
            Thinkable = thinkable,
            LastTime = 0f,
            NextTime = Time.time + (randomizeInitialOffset ? Random.Range(0f, interval) : 0f)
        };

        indexByThinkable[thinkable] = entries.Count;
        entries.Add(entry);
    }

    private void UnregisterInternal(IOutThinkable thinkable)
    {
        int index;
        if (!indexByThinkable.TryGetValue(thinkable, out index))
            return;

        RemoveAt(index);
    }

    private void RemoveAt(int index)
    {
        int last = entries.Count - 1;
        IOutThinkable removed = entries[index].Thinkable;
        indexByThinkable.Remove(removed);

        if (index != last)
        {
            entries[index] = entries[last];
            if (entries[index].Thinkable != null)
                indexByThinkable[entries[index].Thinkable] = index;
        }

        entries.RemoveAt(last);
    }
}
