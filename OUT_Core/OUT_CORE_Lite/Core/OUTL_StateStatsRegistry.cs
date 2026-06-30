using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct OUTL_FloatPair
{
    public string Key;
    public float Value;
}

[Serializable]
public struct OUTL_IntPair
{
    public string Key;
    public int Value;
}

[Serializable]
public struct OUTL_StringPair
{
    public string Key;
    public string Value;
}

[Serializable]
public sealed class OUTL_StateBag
{
    private uint hotFlags;
    private readonly Dictionary<string, float> floats = new Dictionary<string, float>(16);
    private readonly Dictionary<string, int> ints = new Dictionary<string, int>(16);
    private readonly Dictionary<string, string> strings = new Dictionary<string, string>(8);
    private readonly HashSet<string> coldFlags = new HashSet<string>();

    public bool Has(string key)
    {
        OUTL_StateId id = OUTL_CompactIds.StateFromKey(key);
        if (id != OUTL_StateId.None) return GetFlag(id);
        return coldFlags.Contains(key) || floats.ContainsKey(key) || ints.ContainsKey(key) || strings.ContainsKey(key);
    }

    public void SetFlag(string key, bool value)
    {
        if (string.IsNullOrEmpty(key)) return;
        OUTL_StateId id = OUTL_CompactIds.StateFromKey(key);
        if (id != OUTL_StateId.None) { SetFlag(id, value); return; }
        if (value) coldFlags.Add(key); else coldFlags.Remove(key);
    }

    public bool GetFlag(string key, bool fallback = false)
    {
        if (string.IsNullOrEmpty(key)) return fallback;
        OUTL_StateId id = OUTL_CompactIds.StateFromKey(key);
        if (id != OUTL_StateId.None) return GetFlag(id);
        return coldFlags.Contains(key);
    }

    public void SetFlag(OUTL_StateId id, bool value)
    {
        if (id == OUTL_StateId.None || id >= OUTL_StateId.Count) return;
        uint bit = 1u << (int)id;
        if (value) hotFlags |= bit; else hotFlags &= ~bit;
    }

    public bool GetFlag(OUTL_StateId id, bool fallback = false)
    {
        if (id == OUTL_StateId.None || id >= OUTL_StateId.Count) return fallback;
        return (hotFlags & (1u << (int)id)) != 0u;
    }

    public void SetFloat(string key, float value) { if (!string.IsNullOrEmpty(key)) floats[key] = value; }
    public float GetFloat(string key, float fallback = 0f) { float v; return !string.IsNullOrEmpty(key) && floats.TryGetValue(key, out v) ? v : fallback; }
    public void SetInt(string key, int value) { if (!string.IsNullOrEmpty(key)) ints[key] = value; }
    public int GetInt(string key, int fallback = 0) { int v; return !string.IsNullOrEmpty(key) && ints.TryGetValue(key, out v) ? v : fallback; }
    public void SetString(string key, string value) { if (!string.IsNullOrEmpty(key)) strings[key] = value; }
    public string GetString(string key, string fallback = "") { string v; return !string.IsNullOrEmpty(key) && strings.TryGetValue(key, out v) ? v : fallback; }

    public void CopyFlags(List<string> output)
    {
        if (output == null) return;
        output.Clear();
        for (int i = 0; i < (int)OUTL_StateId.Count; i++)
        {
            OUTL_StateId id = (OUTL_StateId)i;
            if (GetFlag(id)) output.Add(OUTL_CompactIds.StateToKey(id));
        }
        foreach (string key in coldFlags) output.Add(key);
    }

    public void CopyFloats(List<OUTL_FloatPair> output)
    {
        if (output == null) return;
        output.Clear();
        foreach (KeyValuePair<string, float> pair in floats) output.Add(new OUTL_FloatPair { Key = pair.Key, Value = pair.Value });
    }

    public void CopyInts(List<OUTL_IntPair> output)
    {
        if (output == null) return;
        output.Clear();
        foreach (KeyValuePair<string, int> pair in ints) output.Add(new OUTL_IntPair { Key = pair.Key, Value = pair.Value });
    }

    public void CopyStrings(List<OUTL_StringPair> output)
    {
        if (output == null) return;
        output.Clear();
        foreach (KeyValuePair<string, string> pair in strings) output.Add(new OUTL_StringPair { Key = pair.Key, Value = pair.Value });
    }

    public void Clear()
    {
        hotFlags = 0u;
        floats.Clear();
        ints.Clear();
        strings.Clear();
        coldFlags.Clear();
    }
}

[Serializable]
public sealed class OUTL_StatBlock
{
    private readonly float[] hot = new float[(int)OUTL_StatId.Count];
    private readonly uint hotSetMaskDefaultOne = 0u;
    private uint hotSetMask;
    private readonly Dictionary<string, float> cold = new Dictionary<string, float>(16);

    public void ApplyBaseStats(OUTL_StatEntry[] entries)
    {
        Array.Clear(hot, 0, hot.Length);
        hotSetMask = hotSetMaskDefaultOne;
        cold.Clear();
        if (entries == null) return;
        for (int i = 0; i < entries.Length; i++) Set(entries[i].Key, entries[i].Value);
    }

    public float Get(OUTL_StatId id, float fallback = 0f)
    {
        if (id == OUTL_StatId.None || id >= OUTL_StatId.Count) return fallback;
        int index = (int)id;
        return (hotSetMask & (1u << index)) != 0u ? hot[index] : fallback;
    }

    public void Set(OUTL_StatId id, float value)
    {
        if (id == OUTL_StatId.None || id >= OUTL_StatId.Count) return;
        int index = (int)id;
        hot[index] = value;
        hotSetMask |= 1u << index;
    }

    public void Add(OUTL_StatId id, float delta)
    {
        if (id == OUTL_StatId.None || id >= OUTL_StatId.Count) return;
        int index = (int)id;
        hot[index] += delta;
        hotSetMask |= 1u << index;
    }

    public float Get(string key, float fallback = 0f)
    {
        OUTL_StatId id = OUTL_CompactIds.StatFromKey(key);
        if (id != OUTL_StatId.None) return Get(id, fallback);
        float value;
        return !string.IsNullOrEmpty(key) && cold.TryGetValue(key, out value) ? value : fallback;
    }

    public void Set(string key, float value)
    {
        if (string.IsNullOrEmpty(key)) return;
        OUTL_StatId id = OUTL_CompactIds.StatFromKey(key);
        if (id != OUTL_StatId.None) { Set(id, value); return; }
        cold[key] = value;
    }

    public void Add(string key, float delta)
    {
        if (string.IsNullOrEmpty(key)) return;
        OUTL_StatId id = OUTL_CompactIds.StatFromKey(key);
        if (id != OUTL_StatId.None) { Add(id, delta); return; }
        cold[key] = Get(key) + delta;
    }

    public void CopyTo(List<OUTL_FloatPair> output)
    {
        if (output == null) return;
        output.Clear();
        for (int i = 0; i < (int)OUTL_StatId.Count; i++)
        {
            if ((hotSetMask & (1u << i)) == 0u) continue;
            OUTL_StatId id = (OUTL_StatId)i;
            output.Add(new OUTL_FloatPair { Key = OUTL_CompactIds.StatToKey(id), Value = hot[i] });
        }
        foreach (KeyValuePair<string, float> pair in cold) output.Add(new OUTL_FloatPair { Key = pair.Key, Value = pair.Value });
    }

    public bool IsZeroOrLess(string key)
    {
        return Get(key) <= 0f;
    }
}

public sealed class OUTL_Registry
{
    private readonly Dictionary<int, OUTL_EntityRuntime> byId = new Dictionary<int, OUTL_EntityRuntime>(1024);
    private readonly Dictionary<GameObject, OUTL_EntityRuntime> byObject = new Dictionary<GameObject, OUTL_EntityRuntime>(1024);
    private readonly Dictionary<string, List<OUTL_EntityRuntime>> byTargetName = new Dictionary<string, List<OUTL_EntityRuntime>>(256);
    private readonly Dictionary<string, List<OUTL_EntityRuntime>> byClassName = new Dictionary<string, List<OUTL_EntityRuntime>>(256);
    private readonly Dictionary<string, OUTL_EntityRuntime> byStableId = new Dictionary<string, OUTL_EntityRuntime>(1024);
    private readonly Dictionary<int, int> indexById = new Dictionary<int, int>(1024);
    private readonly List<OUTL_EntityRuntime> all = new List<OUTL_EntityRuntime>(1024);
    private int nextId = 1;

    public int Count { get { return all.Count; } }

    public OUTL_EntityRuntime Register(OUTL_EntityDef def, OUTL_EntityAdapter adapter)
    {
        OUTL_EntityRuntime existing;
        if (adapter != null && adapter.Id.IsValid && TryGet(adapter.Id, out existing)) return existing;

        OUTL_EntityRuntime runtime = new OUTL_EntityRuntime();
        runtime.Id = new OUTL_EntityId(nextId++);
        runtime.Def = def;
        runtime.Adapter = adapter;
        runtime.Tags = def != null ? def.Tags : null;
        ApplyAddress(runtime, adapter, def);
        if (def != null) runtime.Stats.ApplyBaseStats(def.BaseStats);

        byId[runtime.Id.Value] = runtime;
        if (adapter != null) byObject[adapter.gameObject] = runtime;
        indexById[runtime.Id.Value] = all.Count;
        all.Add(runtime);
        AddToAddressIndexes(runtime);
        return runtime;
    }

    public void Unregister(OUTL_EntityId id)
    {
        OUTL_EntityRuntime runtime;
        if (!TryGet(id, out runtime)) return;

        RemoveFromAddressIndexes(runtime);
        byId.Remove(id.Value);
        if (runtime.Adapter != null) byObject.Remove(runtime.Adapter.gameObject);

        int index;
        if (indexById.TryGetValue(id.Value, out index)) RemoveAtSwapBack(index, id.Value);
        else all.Remove(runtime);
    }

    public void ReindexAddress(OUTL_EntityRuntime runtime)
    {
        if (runtime == null) return;
        RemoveFromAddressIndexes(runtime);
        ApplyAddress(runtime, runtime.Adapter, runtime.Def);
        AddToAddressIndexes(runtime);
    }

    public bool TryGet(OUTL_EntityId id, out OUTL_EntityRuntime runtime)
    {
        runtime = null;
        return id.IsValid && byId.TryGetValue(id.Value, out runtime) && runtime != null;
    }

    public bool TryGet(GameObject go, out OUTL_EntityRuntime runtime)
    {
        runtime = null;
        return go != null && byObject.TryGetValue(go, out runtime) && runtime != null;
    }

    public OUTL_EntityRuntime FindFirstByTargetName(string targetName)
    {
        List<OUTL_EntityRuntime> list;
        if (string.IsNullOrEmpty(targetName) || !byTargetName.TryGetValue(targetName, out list) || list == null) return null;
        for (int i = 0; i < list.Count; i++) if (list[i] != null) return list[i];
        return null;
    }

    public OUTL_EntityRuntime FindFirstByClassName(string className)
    {
        List<OUTL_EntityRuntime> list;
        if (string.IsNullOrEmpty(className) || !byClassName.TryGetValue(className, out list) || list == null) return null;
        for (int i = 0; i < list.Count; i++) if (list[i] != null) return list[i];
        return null;
    }

    public OUTL_EntityRuntime FindByStableId(string stableId)
    {
        OUTL_EntityRuntime runtime;
        return !string.IsNullOrEmpty(stableId) && byStableId.TryGetValue(stableId, out runtime) ? runtime : null;
    }

    public List<OUTL_EntityRuntime> FindByTargetName(string targetName)
    {
        List<OUTL_EntityRuntime> list;
        if (string.IsNullOrEmpty(targetName) || !byTargetName.TryGetValue(targetName, out list) || list == null) return new List<OUTL_EntityRuntime>(0);
        return new List<OUTL_EntityRuntime>(list);
    }

    public List<OUTL_EntityRuntime> FindByClassName(string className)
    {
        List<OUTL_EntityRuntime> list;
        if (string.IsNullOrEmpty(className) || !byClassName.TryGetValue(className, out list) || list == null) return new List<OUTL_EntityRuntime>(0);
        return new List<OUTL_EntityRuntime>(list);
    }

    public int CopyByTargetName(string targetName, List<OUTL_EntityRuntime> buffer)
    {
        if (buffer == null) return 0;
        buffer.Clear();
        List<OUTL_EntityRuntime> list;
        if (string.IsNullOrEmpty(targetName) || !byTargetName.TryGetValue(targetName, out list) || list == null) return 0;
        for (int i = 0; i < list.Count; i++) if (list[i] != null) buffer.Add(list[i]);
        return buffer.Count;
    }

    public int CopyByClassName(string className, List<OUTL_EntityRuntime> buffer)
    {
        if (buffer == null) return 0;
        buffer.Clear();
        List<OUTL_EntityRuntime> list;
        if (string.IsNullOrEmpty(className) || !byClassName.TryGetValue(className, out list) || list == null) return 0;
        for (int i = 0; i < list.Count; i++) if (list[i] != null) buffer.Add(list[i]);
        return buffer.Count;
    }

    public int CopyAll(List<OUTL_EntityRuntime> buffer)
    {
        if (buffer == null) return 0;
        buffer.Clear();
        buffer.AddRange(all);
        return buffer.Count;
    }

    public OUTL_EntityRuntime FindNearest(Vector3 point, string[] requiredTags, float maxDistance)
    {
        float bestSqr = maxDistance > 0f ? maxDistance * maxDistance : float.MaxValue;
        OUTL_EntityRuntime best = null;
        for (int i = 0; i < all.Count; i++)
        {
            OUTL_EntityRuntime e = all[i];
            if (e == null || e.Adapter == null) continue;
            if (requiredTags != null && requiredTags.Length > 0)
            {
                bool ok = false;
                for (int t = 0; t < requiredTags.Length; t++) if (e.HasTag(requiredTags[t])) { ok = true; break; }
                if (!ok) continue;
            }
            float sqr = (e.Adapter.transform.position - point).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = e; }
        }
        return best;
    }

    private static void ApplyAddress(OUTL_EntityRuntime runtime, OUTL_EntityAdapter adapter, OUTL_EntityDef def)
    {
        if (runtime == null) return;
        runtime.ClassName = adapter != null && !string.IsNullOrEmpty(adapter.ClassNameOverride) ? adapter.ClassNameOverride : (def != null ? def.ClassName : string.Empty);
        runtime.TargetName = adapter != null ? adapter.TargetName : string.Empty;
        runtime.Target = adapter != null ? adapter.Target : string.Empty;
        runtime.KillTarget = adapter != null ? adapter.KillTarget : string.Empty;
        runtime.StableId = adapter != null ? adapter.StableId : string.Empty;
        runtime.SavePersistent = adapter != null && adapter.SavePersistent;
    }

    private void AddToAddressIndexes(OUTL_EntityRuntime runtime)
    {
        if (runtime == null) return;
        AddToIndex(byTargetName, runtime.TargetName, runtime);
        AddToIndex(byClassName, runtime.ClassName, runtime);
        if (!string.IsNullOrEmpty(runtime.StableId)) byStableId[runtime.StableId] = runtime;
    }

    private void RemoveFromAddressIndexes(OUTL_EntityRuntime runtime)
    {
        if (runtime == null) return;
        RemoveFromIndex(byTargetName, runtime.TargetName, runtime);
        RemoveFromIndex(byClassName, runtime.ClassName, runtime);
        if (!string.IsNullOrEmpty(runtime.StableId))
        {
            OUTL_EntityRuntime indexed;
            if (byStableId.TryGetValue(runtime.StableId, out indexed) && indexed == runtime)
                byStableId.Remove(runtime.StableId);
        }
    }

    private static void AddToIndex(Dictionary<string, List<OUTL_EntityRuntime>> index, string key, OUTL_EntityRuntime runtime)
    {
        if (index == null || string.IsNullOrEmpty(key) || runtime == null) return;
        List<OUTL_EntityRuntime> list;
        if (!index.TryGetValue(key, out list) || list == null)
        {
            list = new List<OUTL_EntityRuntime>(4);
            index[key] = list;
        }
        if (!list.Contains(runtime)) list.Add(runtime);
    }

    private static void RemoveFromIndex(Dictionary<string, List<OUTL_EntityRuntime>> index, string key, OUTL_EntityRuntime runtime)
    {
        if (index == null || string.IsNullOrEmpty(key) || runtime == null) return;
        List<OUTL_EntityRuntime> list;
        if (!index.TryGetValue(key, out list) || list == null) return;
        list.Remove(runtime);
        if (list.Count == 0) index.Remove(key);
    }

    private void RemoveAtSwapBack(int index, int removedId)
    {
        int last = all.Count - 1;
        indexById.Remove(removedId);
        if (index < 0 || index > last) return;

        if (index != last)
        {
            OUTL_EntityRuntime moved = all[last];
            all[index] = moved;
            if (moved != null) indexById[moved.Id.Value] = index;
        }

        all.RemoveAt(last);
    }
}

public sealed class OUTL_SaveData
{
    public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
    public void Set(string key, string value) { if (!string.IsNullOrEmpty(key)) Values[key] = value; }
    public string Get(string key, string fallback = "") { string value; return Values.TryGetValue(key, out value) ? value : fallback; }
}
