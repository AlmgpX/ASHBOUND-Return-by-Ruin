using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_SaveSpawnResolverRegistry : MonoBehaviour, OUTL_ISpawnResolver
{
    public OUTL_DefDatabase DefDatabase;
    public OUTL_EntityDef[] EntityDefs;
    public bool AllowBareEntityFallback = false;
    public bool RequireRestoreSpawnIfMissingFlag = true;

    private void OnEnable()
    {
        BindToWorld();
    }

    private void Start()
    {
        BindToWorld();
    }

    [ContextMenu("Bind To OUTL World")]
    public void BindToWorld()
    {
        if (OUTL_World.Instance != null) OUTL_World.Instance.Save.SetSpawnResolver(this);
    }

    public bool CanResolve(OUTL_EntitySaveRecord record)
    {
        if (record == null) return false;
        if (RequireRestoreSpawnIfMissingFlag && !record.RestoreSpawnIfMissing) return false;
        return !string.IsNullOrEmpty(record.DefName) || !string.IsNullOrEmpty(record.DefId) || !string.IsNullOrEmpty(record.ClassName);
    }

    public OUTL_EntityAdapter ResolveOrSpawn(OUTL_World world, OUTL_EntitySaveRecord record)
    {
        if (world == null || record == null) return null;
        OUTL_EntityDef def = FindDef(record);
        OUTL_EntityRuntime runtime = null;
        if (def != null) runtime = world.Spawn(def, record.Position, record.Rotation);
        else if (AllowBareEntityFallback)
            Debug.LogWarning("OUTL save restore skipped bare entity spawn. Assign DefDatabase/EntityDefs with prefabs so restore can use OUTL_World.Spawn.", this);
        if (runtime == null || runtime.Adapter == null) return null;

        OUTL_EntityAdapter adapter = runtime.Adapter;
        adapter.StableId = record.StableId;
        adapter.TargetName = record.TargetName;
        adapter.ClassNameOverride = !string.IsNullOrEmpty(record.ClassName) ? record.ClassName : (def != null ? def.ClassName : string.Empty);
        adapter.SavePersistent = true;
        adapter.RestoreSpawnIfMissing = true;
        adapter.Tier = record.Tier;
        adapter.MarkAddressDirty();
        adapter.RebindRuntime(world);
        return adapter;
    }

    private OUTL_EntityDef FindDef(OUTL_EntitySaveRecord record)
    {
        OUTL_EntityDef def = FindDefInDatabase(record);
        if (def != null) return def;
        if (EntityDefs == null) return null;
        for (int i = 0; i < EntityDefs.Length; i++)
        {
            def = EntityDefs[i];
            if (def == null) continue;
            if (!string.IsNullOrEmpty(record.DefName) && def.name == record.DefName) return def;
            if (!string.IsNullOrEmpty(record.DefId) && (def.ClassName == record.DefId || def.name == record.DefId)) return def;
            if (!string.IsNullOrEmpty(record.ClassName) && def.ClassName == record.ClassName) return def;
        }
        return null;
    }

    private OUTL_EntityDef FindDefInDatabase(OUTL_EntitySaveRecord record)
    {
        if (DefDatabase == null || record == null) return null;
        OUTL_EntityDef def = DefDatabase.FindEntityDef(record.DefId);
        if (def != null) return def;
        def = DefDatabase.FindEntityDef(record.DefName);
        if (def != null) return def;
        return DefDatabase.FindEntityDef(record.ClassName);
    }
}
