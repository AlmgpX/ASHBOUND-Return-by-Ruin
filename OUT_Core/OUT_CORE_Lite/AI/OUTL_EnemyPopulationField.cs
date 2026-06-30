using UnityEngine;

[DefaultExecutionOrder(-8900)]
[DisallowMultipleComponent]
public sealed class OUTL_EnemyPopulationField : MonoBehaviour
{
    // Stores population as OUTL abstract records; no enemy GameObjects are created until materialization range.
    public OUTL_EntityDef[] Variants;
    [Min(1)] public int Count = 1000;
    public Vector2 Size = new Vector2(400f, 400f);
    public int Seed = 1976;
    public string StableIdPrefix = "enemy.field";
    public bool RegisterOnStart = true;

    private bool registered;
    public int RegisteredCount { get; private set; }

    private void Start()
    {
        if (RegisterOnStart) RegisterPopulation();
    }

    [ContextMenu("Register Abstract Population")]
    public void RegisterPopulation()
    {
        if (registered) return;
        if (OUTL_World.Instance == null)
        {
            Debug.LogError("OUTL population field cannot register: OUTL_World is missing.", this);
            return;
        }
        if (Variants == null || Variants.Length == 0)
        {
            Debug.LogError("OUTL population field cannot register: no EntityDef variants assigned.", this);
            return;
        }

        int added = 0;
        int amount = Mathf.Max(1, Count);
        Vector3 center = transform.position;
        for (int i = 0; i < amount; i++)
        {
            OUTL_EntityDef def = Variants[PickVariant(i)];
            if (def == null || def.Prefab == null) continue;
            float x = (OUTL_HumanRandom.Value01(0x4649454Cu, Seed, i * 2) - 0.5f) * Mathf.Max(0f, Size.x);
            float z = (OUTL_HumanRandom.Value01(0x504F5055u, Seed, i * 2 + 1) - 0.5f) * Mathf.Max(0f, Size.y);
            float yaw = OUTL_HumanRandom.Value01(0x59415721u, Seed, i) * 360f;
            string stableId = StableIdPrefix + "." + Seed + "." + i;
            if (OUTL_World.Instance.Materialization.RegisterAbstractSpawn(def, center + new Vector3(x, 0f, z), Quaternion.Euler(0f, yaw, 0f), stableId))
                added++;
        }

        RegisteredCount = added;
        registered = true;
        if (added == 0)
            Debug.LogError("OUTL population field registered zero entities. Check EntityDef prefab references.", this);
        else
            Debug.Log("OUT CORE Lite: registered " + added + " abstract enemies; no enemy GameObjects were created yet.", this);
    }

    private int PickVariant(int index)
    {
        if (Variants.Length <= 1) return 0;
        float value = OUTL_HumanRandom.Value01(0x56415249u, Seed, index);
        return Mathf.Clamp(Mathf.FloorToInt(value * Variants.Length), 0, Variants.Length - 1);
    }
}
