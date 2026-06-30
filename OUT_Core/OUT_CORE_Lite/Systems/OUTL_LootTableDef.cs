using System;
using UnityEngine;

[Serializable]
public sealed class OUTL_LootTableEntry
{
    public string Label = "loot";
    public OUTL_ItemDef Item;
    public OUTL_EntityDef EntityDef;
    public GameObject PickupPrefab;
    [Range(0f, 1f)] public float Chance = 1f;
    [Min(0f)] public float Weight = 1f;
    public int MinCount = 1;
    public int MaxCount = 1;
    public bool SpawnOneObjectPerCount;
    public float ScatterRadius = 0.5f;
    public Vector3 SpawnOffset = Vector3.up * 0.35f;
    public string ContextTag = "";
    public OUTL_EgregoreCyclePhase PreferredEgregorePhase = OUTL_EgregoreCyclePhase.StableWorld;
    [Range(0f, 4f)] public float EgregoreChanceMultiplier = 1f;
    [Range(0f, 4f)] public float EgregoreCountMultiplier = 1f;
}

public struct OUTL_LootContext
{
    public string TableId;
    public Vector3 Origin;
    public OUTL_EgregoreCyclePhase EgregorePhase;
    public OUTL_EgregoreMood EgregoreMood;
    public float Danger;
    public float Corruption;
    public float Prosperity;
    public float LootPressure;

    public static OUTL_LootContext Build(string tableId, Vector3 origin)
    {
        OUTL_LootContext context = new OUTL_LootContext
        {
            TableId = tableId,
            Origin = origin,
            EgregorePhase = OUTL_EgregoreCyclePhase.StableWorld,
            EgregoreMood = OUTL_EgregoreMood.Stable
        };

        OUTL_World world = OUTL_World.Instance;
        if (world == null) return context;
        OUTL_WorldAddress address = OUTL_WorldAddress.FromWorldPosition(origin, world.WorldLedger.ActivityCellSize);
        OUTL_WorldCellSummary summary;
        if (!world.WorldLedger.GetCellSummary(address.ActivityCell, out summary)) return context;
        context.EgregorePhase = summary.EgregoreCyclePhase;
        context.EgregoreMood = summary.EgregoreMood;
        context.Danger = summary.Danger;
        context.Corruption = summary.EgregoreCorruption;
        context.Prosperity = summary.EgregoreProsperity;
        context.LootPressure = summary.LootPressure;
        return context;
    }
}

public static class OUTL_LootResolver
{
    public static float RollChance(OUTL_LootTableEntry entry, OUTL_LootContext context)
    {
        if (entry == null) return 0f;
        float chance = Mathf.Clamp01(entry.Chance);
        chance *= GetPhaseChanceMultiplier(context.EgregorePhase);
        if (entry.PreferredEgregorePhase != OUTL_EgregoreCyclePhase.StableWorld && entry.PreferredEgregorePhase == context.EgregorePhase)
            chance *= Mathf.Max(0f, entry.EgregoreChanceMultiplier);
        if (!string.IsNullOrEmpty(entry.ContextTag))
            chance *= ContextTagMultiplier(entry.ContextTag, context);
        return Mathf.Clamp01(chance);
    }

    public static int RollStackCount(OUTL_LootTableEntry entry, OUTL_LootContext context)
    {
        if (entry == null) return 0;
        int min = Mathf.Max(0, entry.MinCount);
        int max = Mathf.Max(min, entry.MaxCount);
        int count = UnityEngine.Random.Range(min, max + 1);
        float multiplier = 1f;
        if (entry.PreferredEgregorePhase != OUTL_EgregoreCyclePhase.StableWorld && entry.PreferredEgregorePhase == context.EgregorePhase)
            multiplier *= Mathf.Max(0f, entry.EgregoreCountMultiplier);
        if (!string.IsNullOrEmpty(entry.ContextTag))
            multiplier *= ContextTagMultiplier(entry.ContextTag, context);
        return Mathf.Max(0, Mathf.RoundToInt(count * multiplier));
    }

    public static float GetPhaseChanceMultiplier(OUTL_EgregoreCyclePhase phase)
    {
        switch (phase)
        {
            case OUTL_EgregoreCyclePhase.Trials:
            case OUTL_EgregoreCyclePhase.ShadowConfrontation:
                return 1.10f;
            case OUTL_EgregoreCyclePhase.Crisis:
            case OUTL_EgregoreCyclePhase.SacrificeOrDeath:
            case OUTL_EgregoreCyclePhase.CorruptionLoop:
                return 1.20f;
            case OUTL_EgregoreCyclePhase.RevelationOrBoon:
                return 1.35f;
            case OUTL_EgregoreCyclePhase.Renewal:
                return 1.10f;
            case OUTL_EgregoreCyclePhase.Collapse:
                return 0.85f;
            default:
                return 1f;
        }
    }

    private static float ContextTagMultiplier(string tag, OUTL_LootContext context)
    {
        if (string.IsNullOrEmpty(tag)) return 1f;
        if (tag.IndexOf("cursed", StringComparison.OrdinalIgnoreCase) >= 0 || tag.IndexOf("forbidden", StringComparison.OrdinalIgnoreCase) >= 0)
            return context.EgregorePhase == OUTL_EgregoreCyclePhase.ShadowConfrontation || context.EgregorePhase == OUTL_EgregoreCyclePhase.CorruptionLoop || context.Corruption > 0.45f ? 1.35f : 0.75f;
        if (tag.IndexOf("boon", StringComparison.OrdinalIgnoreCase) >= 0 || tag.IndexOf("rare", StringComparison.OrdinalIgnoreCase) >= 0)
            return context.EgregorePhase == OUTL_EgregoreCyclePhase.RevelationOrBoon ? 1.5f : 0.9f;
        if (tag.IndexOf("renewal", StringComparison.OrdinalIgnoreCase) >= 0 || tag.IndexOf("trade", StringComparison.OrdinalIgnoreCase) >= 0 || tag.IndexOf("food", StringComparison.OrdinalIgnoreCase) >= 0)
            return context.EgregorePhase == OUTL_EgregoreCyclePhase.Renewal || context.Prosperity > 0.55f ? 1.25f : 0.95f;
        if (tag.IndexOf("ruined", StringComparison.OrdinalIgnoreCase) >= 0 || tag.IndexOf("scavenged", StringComparison.OrdinalIgnoreCase) >= 0)
            return context.EgregorePhase == OUTL_EgregoreCyclePhase.Collapse ? 1.4f : 0.95f;
        if (tag.IndexOf("ritual", StringComparison.OrdinalIgnoreCase) >= 0)
            return context.EgregorePhase == OUTL_EgregoreCyclePhase.Threshold || context.EgregorePhase == OUTL_EgregoreCyclePhase.RevelationOrBoon ? 1.3f : 1f;
        return 1f;
    }
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Loot/Loot Table Def", fileName = "OUTL_LootTableDef")]
public sealed class OUTL_LootTableDef : ScriptableObject
{
    public string TableId = "loot.table";
    public OUTL_LootTableEntry[] Entries;
    public bool RollEachEntry = true;
    public int MaxDrops = 16;

    public int Roll(Vector3 origin, Quaternion rotation, OUTL_EntityAdapter source)
    {
        if (Entries == null || !OUTL_NetworkAuthority.CanSpawnDrop()) return 0;
        OUTL_LootContext context = OUTL_LootContext.Build(TableId, origin);
        if (OUTL_World.Instance != null)
            OUTL_World.Instance.Events.Emit(new OUTL_Event(OUTL_EventType.LootRolled, source != null ? source.Id : OUTL_EntityId.None, OUTL_EntityId.None) { Key = TableId, IntValue = (int)context.EgregorePhase, FloatValue = context.LootPressure, Point = origin });
        int spawned = 0;
        int maxDrops = Mathf.Max(0, MaxDrops);
        if (!RollEachEntry)
        {
            OUTL_LootTableEntry weighted = PickWeightedEntry(context);
            if (weighted == null || UnityEngine.Random.value > OUTL_LootResolver.RollChance(weighted, context)) return 0;
            int stack = OUTL_LootResolver.RollStackCount(weighted, context);
            return SpawnRolledEntry(weighted, stack, origin, rotation, source, maxDrops);
        }

        for (int i = 0; i < Entries.Length; i++)
        {
            if (maxDrops > 0 && spawned >= maxDrops) break;
            OUTL_LootTableEntry entry = Entries[i];
            if (entry == null || UnityEngine.Random.value > OUTL_LootResolver.RollChance(entry, context)) continue;
            spawned += SpawnRolledEntry(entry, OUTL_LootResolver.RollStackCount(entry, context), origin, rotation, source, maxDrops > 0 ? maxDrops - spawned : int.MaxValue);
        }
        return spawned;
    }

    public int RollDeterministic(int seed, Vector3 origin, Quaternion rotation, OUTL_EntityAdapter source)
    {
        UnityEngine.Random.State previous = UnityEngine.Random.state;
        UnityEngine.Random.InitState(seed);
        int spawned = Roll(origin, rotation, source);
        UnityEngine.Random.state = previous;
        return spawned;
    }

    private static int SpawnRolledEntry(OUTL_LootTableEntry entry, int stackCount, Vector3 origin, Quaternion rotation, OUTL_EntityAdapter source, int remainingDrops)
    {
        if (entry == null || stackCount <= 0 || remainingDrops <= 0) return 0;
        int objectCount = entry.SpawnOneObjectPerCount ? Mathf.Min(stackCount, remainingDrops) : 1;
        int spawned = 0;
        for (int i = 0; i < objectCount; i++)
        {
            int count = entry.SpawnOneObjectPerCount ? 1 : stackCount;
            if (SpawnEntry(entry, count, origin, rotation, source)) spawned++;
        }
        return spawned;
    }

    private static bool SpawnEntry(OUTL_LootTableEntry entry, int stackCount, Vector3 origin, Quaternion rotation, OUTL_EntityAdapter source)
    {
        Vector2 circle = UnityEngine.Random.insideUnitCircle * Mathf.Max(0f, entry.ScatterRadius);
        Vector3 position = origin + entry.SpawnOffset + new Vector3(circle.x, 0f, circle.y);
        OUTL_EntityRuntime runtime = null;
        if (entry.EntityDef != null && OUTL_World.Instance != null)
            runtime = OUTL_World.Instance.Spawn(entry.EntityDef, position, rotation);
        else if (entry.PickupPrefab != null)
        {
            GameObject go = OUTL_PoolSystem.SpawnShared(entry.PickupPrefab, position, rotation);
            OUTL_EntityAdapter adapter = go != null ? go.GetComponent<OUTL_EntityAdapter>() : null;
            runtime = adapter != null ? adapter.Runtime : null;
            ConfigurePickup(go, entry, stackCount, source);
        }

        if (runtime != null && runtime.Adapter != null)
            ConfigurePickup(runtime.Adapter.gameObject, entry, stackCount, source);

        OUTL_StimulusBus.EmitResource(source != null ? source.Id : OUTL_EntityId.None, position, 10f, 1f, 0.5f, entry.Label);
        if (OUTL_World.Instance != null)
            OUTL_World.Instance.Events.Emit(new OUTL_Event(OUTL_EventType.ItemDropped, source != null ? source.Id : OUTL_EntityId.None, runtime != null ? runtime.Id : OUTL_EntityId.None) { Key = entry.Label, Point = position });
        return runtime != null || entry.PickupPrefab != null;
    }

    private static void ConfigurePickup(GameObject go, OUTL_LootTableEntry entry, int stackCount, OUTL_EntityAdapter source)
    {
        if (go == null) return;
        OUTL_ItemPickup pickup = go.GetComponent<OUTL_ItemPickup>();
        if (pickup == null) return;
        pickup.Item = entry.Item;
        pickup.Count = Mathf.Max(1, stackCount);
        pickup.Source = source;
    }

    private OUTL_LootTableEntry PickWeightedEntry(OUTL_LootContext context)
    {
        float total = 0f;
        for (int i = 0; i < Entries.Length; i++)
        {
            OUTL_LootTableEntry entry = Entries[i];
            if (entry != null) total += Mathf.Max(0f, entry.Weight) * Mathf.Max(0.01f, OUTL_LootResolver.RollChance(entry, context));
        }
        if (total <= 0f) return null;
        float roll = UnityEngine.Random.value * total;
        for (int i = 0; i < Entries.Length; i++)
        {
            OUTL_LootTableEntry entry = Entries[i];
            if (entry == null) continue;
            roll -= Mathf.Max(0f, entry.Weight) * Mathf.Max(0.01f, OUTL_LootResolver.RollChance(entry, context));
            if (roll <= 0f) return entry;
        }
        return null;
    }
}
