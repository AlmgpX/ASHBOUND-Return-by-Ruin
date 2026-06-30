using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class OUTL_FactionSystem
{
    private OUTL_World world;
    public void Bind(OUTL_World world) { this.world = world; }

    public bool AreHostile(OUTL_EntityRuntime a, OUTL_EntityRuntime b)
    {
        if (a == null || b == null) return false;
        if (a == b) return false;
        if (a.Faction == null || b.Faction == null) return false;
        return a.Faction.IsHostileTo(b.Faction) || b.Faction.IsHostileTo(a.Faction);
    }

    public bool AreFriendly(OUTL_EntityRuntime a, OUTL_EntityRuntime b)
    {
        if (a == null || b == null) return false;
        if (a == b) return true;
        if (a.Faction == null || b.Faction == null) return false;
        return a.Faction.GetRelationTo(b.Faction) > 0.25f || b.Faction.GetRelationTo(a.Faction) > 0.25f;
    }
}

[Serializable]
public class OUTL_WorldSaveFile
{
    public float Time;
    public List<OUTL_EntitySaveRecord> Entities = new List<OUTL_EntitySaveRecord>();
    public List<OUTL_EntitySaveRecord> AbstractEntities = new List<OUTL_EntitySaveRecord>();
    public List<OUTL_QuestSaveRecord> Quests = new List<OUTL_QuestSaveRecord>();
}

[Serializable]
public class OUTL_EntitySaveRecord
{
    public int Id;
    public string StableId;
    public string ClassName;
    public string TargetName;
    public string DefId;
    public string DefName;
    public string FactionName;
    public bool RestoreSpawnIfMissing;
    public bool Materialized = true;
    public Vector3 Position;
    public Quaternion Rotation;
    public OUTL_RuntimeTier Tier;
    public OUTL_LifeState LifeState = OUTL_LifeState.Alive;
    public bool Dead;
    public float DeathTime;
    public OUTL_EntityId KillerId = OUTL_EntityId.None;
    public string DeathKey;
    public List<OUTL_FloatPair> Stats = new List<OUTL_FloatPair>();
    public List<string> Flags = new List<string>();
    public List<OUTL_FloatPair> StateFloats = new List<OUTL_FloatPair>();
    public List<OUTL_IntPair> StateInts = new List<OUTL_IntPair>();
    public List<OUTL_StringPair> StateStrings = new List<OUTL_StringPair>();
    public List<OUTL_ComponentSavePayload> ComponentPayloads = new List<OUTL_ComponentSavePayload>();
}

[Serializable]
public class OUTL_QuestSaveRecord
{
    public string QuestId;
    public int Stage;
    public List<OUTL_IntPair> Objectives = new List<OUTL_IntPair>();
}

public sealed class OUTL_SaveSystem
{
    private OUTL_World world;
    private readonly List<OUTL_EntityRuntime> entityBuffer = new List<OUTL_EntityRuntime>(1024);
    private readonly List<OUTL_FloatPair> statBuffer = new List<OUTL_FloatPair>(32);
    private readonly List<string> flagBuffer = new List<string>(32);
    private readonly List<OUTL_FloatPair> floatBuffer = new List<OUTL_FloatPair>(32);
    private readonly List<OUTL_IntPair> intBuffer = new List<OUTL_IntPair>(32);
    private readonly List<OUTL_StringPair> stringBuffer = new List<OUTL_StringPair>(32);
    private readonly Dictionary<int, OUTL_EntityRuntime> restoreMap = new Dictionary<int, OUTL_EntityRuntime>(1024);
    private readonly Dictionary<string, OUTL_EntityRuntime> restoreStableMap = new Dictionary<string, OUTL_EntityRuntime>(1024);
    private OUTL_ISpawnResolver spawnResolver;

    public void Bind(OUTL_World world)
    {
        this.world = world;
        if (spawnResolver == null) spawnResolver = FindSpawnResolver(world);
    }

    public void SetSpawnResolver(OUTL_ISpawnResolver resolver)
    {
        spawnResolver = resolver;
    }

    public string DefaultPath
    {
        get { return Path.Combine(Application.persistentDataPath, "outl_save.json"); }
    }

    public OUTL_WorldSaveFile Capture()
    {
        using (OUTL_Profile.SaveCapture.Auto())
        {
            OUTL_WorldSaveFile file = new OUTL_WorldSaveFile();
            file.Time = world != null ? world.WorldTime : 0f;
            if (world == null) return file;

            world.Registry.CopyAll(entityBuffer);
            for (int i = 0; i < entityBuffer.Count; i++)
            {
                OUTL_EntityRuntime e = entityBuffer[i];
                if (e == null) continue;
                if (!e.SavePersistent) continue;

                OUTL_EntitySaveRecord r = BuildRecordFromRuntime(world, e, true);
                if (r == null) continue;
                file.Entities.Add(r);
                OUTL_Profile.Frame.SaveEntities++;
            }

            world.Materialization.CopyAbstractRecords(file.AbstractEntities);
            world.Quests.CopyStages(file.Quests);
            return file;
        }
    }

    public static OUTL_EntitySaveRecord BuildRecordFromRuntime(OUTL_World world, OUTL_EntityRuntime e, bool captureComponents)
    {
        if (e == null) return null;
        OUTL_EntitySaveRecord r = new OUTL_EntitySaveRecord();
        r.Id = e.Id.Value;
        r.StableId = e.StableId;
        r.ClassName = e.ClassName;
        r.TargetName = e.TargetName;
        r.DefId = e.Def != null ? (!string.IsNullOrEmpty(e.Def.ClassName) ? e.Def.ClassName : e.Def.name) : string.Empty;
        r.DefName = e.Def != null ? e.Def.name : string.Empty;
        r.FactionName = e.Faction != null ? e.Faction.name : string.Empty;
        r.RestoreSpawnIfMissing = e.Adapter != null && e.Adapter.RestoreSpawnIfMissing;
        r.Materialized = e.Adapter != null && e.Adapter.gameObject.activeInHierarchy;
        r.Tier = e.Tier;
        r.LifeState = e.LifeState;
        r.Dead = e.Dead;
        r.DeathTime = e.DeathTime;
        r.KillerId = e.KillerId;
        r.DeathKey = e.DeathKey;

        if (e.Adapter != null)
        {
            r.Position = e.Adapter.transform.position;
            r.Rotation = e.Adapter.transform.rotation;
        }
        else
        {
            OUTL_AbstractEntityRecord abstractRecord;
            if (world != null && world.WorldLedger.TryGetEntity(e.Id, out abstractRecord))
                r.Position = abstractRecord.Position;
        }

        List<OUTL_FloatPair> stats = new List<OUTL_FloatPair>(32);
        e.Stats.CopyTo(stats);
        for (int s = 0; s < stats.Count; s++) r.Stats.Add(stats[s]);

        List<string> flags = new List<string>(32);
        e.State.CopyFlags(flags);
        for (int f = 0; f < flags.Count; f++) r.Flags.Add(flags[f]);

        List<OUTL_FloatPair> floats = new List<OUTL_FloatPair>(32);
        e.State.CopyFloats(floats);
        for (int f = 0; f < floats.Count; f++) r.StateFloats.Add(floats[f]);

        List<OUTL_IntPair> ints = new List<OUTL_IntPair>(32);
        e.State.CopyInts(ints);
        for (int n = 0; n < ints.Count; n++) r.StateInts.Add(ints[n]);

        List<OUTL_StringPair> strings = new List<OUTL_StringPair>(32);
        e.State.CopyStrings(strings);
        for (int st = 0; st < strings.Count; st++) r.StateStrings.Add(strings[st]);

        if (captureComponents && e.Adapter != null) OUTL_ComponentSaveUtility.CaptureComponents(e.Adapter, r.ComponentPayloads);
        return r;
    }

    public void SaveToFile(string path = null)
    {
        if (string.IsNullOrEmpty(path)) path = DefaultPath;
        OUTL_WorldSaveFile file = Capture();
        string json = JsonUtility.ToJson(file, true);
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, json);
    }

    public bool LoadFromFile(string path = null)
    {
        if (string.IsNullOrEmpty(path)) path = DefaultPath;
        if (!File.Exists(path)) return false;
        string json = File.ReadAllText(path);
        OUTL_WorldSaveFile file = JsonUtility.FromJson<OUTL_WorldSaveFile>(json);
        return Restore(file);
    }

    public bool Restore(OUTL_WorldSaveFile file)
    {
        using (OUTL_Profile.SaveRestore.Auto())
        {
            if (file == null || world == null) return false;

            restoreMap.Clear();
            restoreStableMap.Clear();
            world.Registry.CopyAll(entityBuffer);
            for (int i = 0; i < entityBuffer.Count; i++)
            {
                OUTL_EntityRuntime e = entityBuffer[i];
                if (e == null) continue;
                if (e.Id.IsValid) restoreMap[e.Id.Value] = e;
                if (!string.IsNullOrEmpty(e.StableId) && !restoreStableMap.ContainsKey(e.StableId)) restoreStableMap[e.StableId] = e;
            }

            OUTL_ISpawnResolver resolver = spawnResolver != null ? spawnResolver : FindSpawnResolver(world);
            world.Materialization.RestoreAbstractRecords(file.AbstractEntities);
            int entityCount = file.Entities != null ? file.Entities.Count : 0;
            for (int i = 0; i < entityCount; i++)
            {
                OUTL_EntitySaveRecord r = file.Entities[i];
                if (r == null) continue;
                OUTL_EntityRuntime match = null;
                if (!string.IsNullOrEmpty(r.StableId)) restoreStableMap.TryGetValue(r.StableId, out match);
                if (match == null) restoreMap.TryGetValue(r.Id, out match);
                if (match == null && resolver != null && resolver.CanResolve(r))
                {
                    OUTL_EntityAdapter spawned = resolver.ResolveOrSpawn(world, r);
                    match = spawned != null ? spawned.Runtime : null;
                    if (match != null)
                    {
                        if (match.Id.IsValid) restoreMap[match.Id.Value] = match;
                        if (!string.IsNullOrEmpty(match.StableId) && !restoreStableMap.ContainsKey(match.StableId)) restoreStableMap[match.StableId] = match;
                    }
                }
                if (match == null) continue;
                ApplyRecordToRuntime(world, match, r, true);
                OUTL_Profile.Frame.RestoreEntities++;
            }

            world.Quests.RestoreStages(file.Quests);
            return true;
        }
    }

    public OUTL_EntityAdapter ResolveOrSpawn(OUTL_EntitySaveRecord record)
    {
        if (world == null || record == null) return null;
        OUTL_ISpawnResolver resolver = spawnResolver != null ? spawnResolver : FindSpawnResolver(world);
        if (resolver == null || !resolver.CanResolve(record)) return null;
        return resolver.ResolveOrSpawn(world, record);
    }

    public static void ApplyRecordToRuntime(OUTL_World world, OUTL_EntityRuntime runtime, OUTL_EntitySaveRecord record, bool restoreComponents)
    {
        if (runtime == null || record == null) return;

        runtime.Tier = record.Tier;
        runtime.StableId = record.StableId;
        runtime.ClassName = record.ClassName;
        runtime.TargetName = record.TargetName;
        runtime.SavePersistent = true;
        runtime.LifeState = record.LifeState;
        runtime.Dead = record.Dead || record.LifeState == OUTL_LifeState.Dead;
        runtime.DeathTime = record.DeathTime;
        runtime.KillerId = record.KillerId;
        runtime.DeathKey = record.DeathKey;

        if (runtime.Adapter != null)
        {
            runtime.Adapter.transform.position = record.Position;
            runtime.Adapter.transform.rotation = record.Rotation;
            runtime.Adapter.Tier = record.Tier;
            runtime.Adapter.StableId = record.StableId;
            runtime.Adapter.ClassNameOverride = record.ClassName;
            runtime.Adapter.TargetName = record.TargetName;
            runtime.Adapter.SavePersistent = true;
            runtime.Adapter.RestoreSpawnIfMissing = record.RestoreSpawnIfMissing;
            runtime.Adapter.MarkAddressDirty();
            if (world != null) runtime.Adapter.RebindRuntime(world);
        }

        runtime.Stats.ApplyBaseStats(null);
        runtime.State.Clear();

        if (record.Stats != null)
            for (int s = 0; s < record.Stats.Count; s++) runtime.Stats.Set(record.Stats[s].Key, record.Stats[s].Value);
        if (record.Flags != null)
            for (int f = 0; f < record.Flags.Count; f++) runtime.State.SetFlag(record.Flags[f], true);
        if (record.StateFloats != null)
            for (int f = 0; f < record.StateFloats.Count; f++) runtime.State.SetFloat(record.StateFloats[f].Key, record.StateFloats[f].Value);
        if (record.StateInts != null)
            for (int n = 0; n < record.StateInts.Count; n++) runtime.State.SetInt(record.StateInts[n].Key, record.StateInts[n].Value);
        if (record.StateStrings != null)
            for (int st = 0; st < record.StateStrings.Count; st++) runtime.State.SetString(record.StateStrings[st].Key, record.StateStrings[st].Value);

        if (world != null && runtime.Adapter != null)
        {
            world.Registry.ReindexAddress(runtime);
            world.Sectors.RegisterOrUpdate(runtime);
        }

        if (restoreComponents && runtime.Adapter != null)
            OUTL_ComponentSaveUtility.RestoreComponents(runtime.Adapter, record.ComponentPayloads);

        if (runtime.Adapter != null)
        {
            runtime.Adapter.RefreshProcessingTierState();

            OUTL_CharacterIdentity identity = runtime.Adapter.GetComponent<OUTL_CharacterIdentity>();
            if (identity != null) identity.EnsureGenerated();

            OUTL_NavMeshMover navMover = runtime.Adapter.GetComponent<OUTL_NavMeshMover>();
            if (navMover != null) navMover.ResumeAfterMaterialization();

            OUTL_OutpostEnemyBrain outpostBrain = runtime.Adapter.GetComponent<OUTL_OutpostEnemyBrain>();
            if (outpostBrain != null) outpostBrain.ResumeAfterMaterialization();
        }

        if (runtime.Dead && runtime.Adapter != null)
        {
            OUTL_NPCBehaviorController npc = runtime.Adapter.GetComponent<OUTL_NPCBehaviorController>();
            if (npc != null) npc.ApplyDeadState();
        }
    }

    private static OUTL_ISpawnResolver FindSpawnResolver(OUTL_World world)
    {
        if (world == null) return null;
        MonoBehaviour[] behaviours = world.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            OUTL_ISpawnResolver resolver = behaviours[i] as OUTL_ISpawnResolver;
            if (resolver != null) return resolver;
        }
        return null;
    }
}
