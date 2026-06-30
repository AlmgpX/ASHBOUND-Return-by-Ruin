using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class OUTL_ExtendedWorldSaveFile
{
    public OUTL_WorldSaveFile Base = new OUTL_WorldSaveFile();
    public List<OUTL_ExtendedEntitySaveRecord> Entities = new List<OUTL_ExtendedEntitySaveRecord>();
}

[Serializable]
public class OUTL_ExtendedEntitySaveRecord
{
    public OUTL_EntitySaveRecord Entity = new OUTL_EntitySaveRecord();
    public List<OUTL_ComponentSavePayload> ComponentPayloads = new List<OUTL_ComponentSavePayload>();
}

[DisallowMultipleComponent]
public sealed class OUTL_ExtendedSaveSystem : MonoBehaviour
{
    public OUTL_SaveSpawnResolverRegistry SpawnResolver;
    public string FileName = "outl_save.json";

    public string Path
    {
        get
        {
            OUTL_World world = OUTL_World.Instance;
            if (string.IsNullOrEmpty(FileName) && world != null) return world.Save.DefaultPath;
            return System.IO.Path.Combine(Application.persistentDataPath, string.IsNullOrEmpty(FileName) ? "outl_save.json" : FileName);
        }
    }

    private void Awake()
    {
        RegisterSpawnResolverWithWorld();
    }

    private void OnEnable()
    {
        RegisterSpawnResolverWithWorld();
    }

    [ContextMenu("OUT Canonical Save")]
    public void SaveNow()
    {
        SaveToFile(Path);
    }

    [ContextMenu("OUT Canonical Load")]
    public void LoadNow()
    {
        LoadFromFile(Path);
    }

    public void SaveToFile(string path)
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return;
        RegisterSpawnResolverWithWorld();
        world.Save.SaveToFile(path);
    }

    public bool LoadFromFile(string path)
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null || string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
        RegisterSpawnResolverWithWorld();

        string json = File.ReadAllText(path);
        if (LooksLikeLegacyExtendedSave(json))
            return Restore(JsonUtility.FromJson<OUTL_ExtendedWorldSaveFile>(json));

        OUTL_WorldSaveFile canonical = JsonUtility.FromJson<OUTL_WorldSaveFile>(json);
        return world.Save.Restore(canonical);
    }

    public OUTL_ExtendedWorldSaveFile Capture()
    {
        OUTL_ExtendedWorldSaveFile file = new OUTL_ExtendedWorldSaveFile();
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return file;
        RegisterSpawnResolverWithWorld();
        file.Base = world.Save.Capture();
        file.Entities.Clear();
        return file;
    }

    public bool Restore(OUTL_ExtendedWorldSaveFile file)
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null || file == null) return false;
        RegisterSpawnResolverWithWorld();

        OUTL_WorldSaveFile canonical = BuildCanonicalFile(file);
        return world.Save.Restore(canonical);
    }

    private static OUTL_WorldSaveFile BuildCanonicalFile(OUTL_ExtendedWorldSaveFile file)
    {
        if (file == null) return null;
        if (file.Base != null && file.Base.Entities != null && file.Base.Entities.Count > 0)
            return file.Base;

        OUTL_WorldSaveFile canonical = file.Base != null ? file.Base : new OUTL_WorldSaveFile();
        if (canonical.Entities == null) canonical.Entities = new List<OUTL_EntitySaveRecord>();
        canonical.Entities.Clear();

        if (file.Entities != null)
        {
            for (int i = 0; i < file.Entities.Count; i++)
            {
                OUTL_ExtendedEntitySaveRecord legacy = file.Entities[i];
                if (legacy == null || legacy.Entity == null) continue;
                if (legacy.ComponentPayloads != null && legacy.ComponentPayloads.Count > 0 && (legacy.Entity.ComponentPayloads == null || legacy.Entity.ComponentPayloads.Count == 0))
                    legacy.Entity.ComponentPayloads = legacy.ComponentPayloads;
                canonical.Entities.Add(legacy.Entity);
            }
        }

        return canonical;
    }

    private static bool LooksLikeLegacyExtendedSave(string json)
    {
        return !string.IsNullOrEmpty(json)
            && json.IndexOf("\"Base\"", StringComparison.Ordinal) >= 0
            && json.IndexOf("\"Entities\"", StringComparison.Ordinal) >= 0;
    }

    private void RegisterSpawnResolverWithWorld()
    {
        if (SpawnResolver == null) SpawnResolver = GetComponent<OUTL_SaveSpawnResolverRegistry>();
        if (OUTL_World.Instance != null && SpawnResolver != null)
            OUTL_World.Instance.Save.SetSpawnResolver(SpawnResolver);
    }
}
