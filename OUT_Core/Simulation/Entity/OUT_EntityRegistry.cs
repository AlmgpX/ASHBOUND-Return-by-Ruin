using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-8000)]
[DisallowMultipleComponent]
public class OUT_EntityRegistry : MonoBehaviour
{
    public static OUT_EntityRegistry Instance { get; private set; }

    [Header("Lifecycle")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private int initialCapacity = 1024;

    [Header("Debug")]
    [SerializeField] private bool logRegistrations = false;

    private readonly Dictionary<int, OUT_EntityAdapter> bySimId = new Dictionary<int, OUT_EntityAdapter>(1024);
    private readonly Dictionary<GameObject, OUT_EntityAdapter> byObject = new Dictionary<GameObject, OUT_EntityAdapter>(1024);
    private readonly List<OUT_EntityAdapter> entities = new List<OUT_EntityAdapter>(1024);

    public int Count { get { return entities.Count; } }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (initialCapacity > 0 && entities.Capacity < initialCapacity)
            entities.Capacity = initialCapacity;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static OUT_EntityRegistry EnsureExists()
    {
        if (Instance != null)
            return Instance;

        OUT_EntityRegistry existing = FindObjectOfType<OUT_EntityRegistry>();
        if (existing != null)
            return existing;

        GameObject go = new GameObject("OUT_EntityRegistry");
        return go.AddComponent<OUT_EntityRegistry>();
    }

    public OUT_SimEntityId Register(OUT_EntityAdapter entity)
    {
        if (entity == null)
            return OUT_SimEntityId.None;

        if (entity.SimEntityId.IsValid && bySimId.ContainsKey(entity.SimEntityId.Value))
            return entity.SimEntityId;

        OUT_SimEntityId id = AllocateSimEntity(entity);
        if (!id.IsValid)
            return OUT_SimEntityId.None;

        bySimId[id.Value] = entity;
        if (entity.gameObject != null)
            byObject[entity.gameObject] = entity;

        if (!entities.Contains(entity))
            entities.Add(entity);

        entity.ApplySimEntityId(id);

        if (logRegistrations)
            Debug.Log("OUT_EntityRegistry registered " + entity.name + " as SimEntity " + id, entity);

        return id;
    }

    public void Unregister(OUT_EntityAdapter entity)
    {
        if (entity == null)
            return;

        OUT_SimEntityId id = entity.SimEntityId;
        if (id.IsValid)
        {
            OUT_EntityAdapter existing;
            if (bySimId.TryGetValue(id.Value, out existing) && existing == entity)
                bySimId.Remove(id.Value);
        }

        if (entity.gameObject != null)
            byObject.Remove(entity.gameObject);

        for (int i = entities.Count - 1; i >= 0; i--)
        {
            if (entities[i] == entity)
            {
                int last = entities.Count - 1;
                entities[i] = entities[last];
                entities.RemoveAt(last);
                break;
            }
        }

        OUT_SimulationService service = OUT_SimulationService.Instance;
        if (service != null && id.IsValid)
            service.World.RemoveEntity(id);

        entity.ClearSimEntityId();
    }

    public bool TryGet(OUT_SimEntityId id, out OUT_EntityAdapter entity)
    {
        entity = null;
        return id.IsValid && bySimId.TryGetValue(id.Value, out entity) && entity != null;
    }

    public bool TryGet(int simEntityId, out OUT_EntityAdapter entity)
    {
        return bySimId.TryGetValue(simEntityId, out entity) && entity != null;
    }

    public bool TryGet(GameObject go, out OUT_EntityAdapter entity)
    {
        entity = null;
        if (go == null)
            return false;

        return byObject.TryGetValue(go, out entity) && entity != null;
    }

    public OUT_EntityAdapter FindNearest(Vector3 position, float maxDistance, OUT_RuntimeTier minTier = OUT_RuntimeTier.Dormant)
    {
        float maxSqr = maxDistance <= 0f ? float.MaxValue : maxDistance * maxDistance;
        OUT_EntityAdapter best = null;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < entities.Count; i++)
        {
            OUT_EntityAdapter entity = entities[i];
            if (entity == null || entity.RuntimeTier < minTier)
                continue;

            float sqr = (entity.transform.position - position).sqrMagnitude;
            if (sqr <= maxSqr && sqr < bestSqr)
            {
                bestSqr = sqr;
                best = entity;
            }
        }

        return best;
    }

    public int CopyEntities(List<OUT_EntityAdapter> destination, bool includeInactive = false)
    {
        if (destination == null)
            return 0;

        destination.Clear();
        for (int i = 0; i < entities.Count; i++)
        {
            OUT_EntityAdapter entity = entities[i];
            if (entity == null)
                continue;

            if (!includeInactive && !entity.gameObject.activeInHierarchy)
                continue;

            destination.Add(entity);
        }

        return destination.Count;
    }

    private OUT_SimEntityId AllocateSimEntity(OUT_EntityAdapter entity)
    {
        OUT_SimulationService service = OUT_SimulationService.Instance;
        if (service != null)
        {
            OUT_ChunkId chunk = default;
            OUT_SimEntityId id = service.World.AllocateEntity(entity.EntityName, chunk);
            service.World.SetChunk(id, chunk);
            return id;
        }

        return new OUT_SimEntityId(Mathf.Abs(entity.GetInstanceID()));
    }
}
