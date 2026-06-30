#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class OUTL_SaveWorkbenchEditor
{
    private const string SaveRoot = "OUT CORE Lite/Save/";
    private const string DebugRoot = "OUT CORE Lite/Debug/";

    // [MenuItem(SaveRoot + "Quick Save OUTL Runtime")]
    public static void QuickSaveOUTLRuntime()
    {
        OUTL_World world = ResolveWorld();
        if (world == null)
        {
            Debug.LogWarning("Quick Save OUTL Runtime requires an OUTL_World in the open scene.");
            return;
        }

        world.Save.SaveToFile();
        Debug.Log("OUTL quick save complete: " + world.Save.DefaultPath, world);
    }

    // [MenuItem(SaveRoot + "Quick Load OUTL Runtime")]
    public static void QuickLoadOUTLRuntime()
    {
        OUTL_World world = ResolveWorld();
        if (world == null)
        {
            Debug.LogWarning("Quick Load OUTL Runtime requires an OUTL_World in the open scene.");
            return;
        }

        bool ok = world.Save.LoadFromFile();
        if (ok) Debug.Log("OUTL quick load complete: " + world.Save.DefaultPath, world);
        else Debug.LogWarning("OUTL quick load failed or file is missing: " + world.Save.DefaultPath, world);
    }

    // [MenuItem(SaveRoot + "Validate Runtime Save Roundtrip")]
    public static void ValidateRuntimeSaveRoundtrip()
    {
        OUTL_World world = ResolveWorld();
        if (world == null)
        {
            Debug.LogWarning("Validate Runtime Save Roundtrip requires an OUTL_World in the open scene.");
            return;
        }

        OUTL_WorldSaveFile file = world.Save.Capture();
        bool ok = world.Save.Restore(file);
        int entities = file != null && file.Entities != null ? file.Entities.Count : 0;
        int payloads = CountPayloads(file);
        if (ok) Debug.Log("OUTL save roundtrip OK. entities=" + entities + " componentPayloads=" + payloads + ".", world);
        else Debug.LogWarning("OUTL save roundtrip failed. entities=" + entities + " componentPayloads=" + payloads + ".", world);
    }

    // [MenuItem(DebugRoot + "Show NPC Tick Budget Snapshot")]
    public static void ShowNpcTickBudgetSnapshot()
    {
        OUTL_NPCBehaviorBudgetSnapshot snapshot = OUTL_NPCBehaviorDispatcher.LastSnapshot;
        Debug.Log("OUTL NPC budget snapshot: registered=" + snapshot.Registered +
                  " ticked=" + snapshot.TickedThisFrame +
                  " skippedTier=" + snapshot.SkippedByTier +
                  " skippedBudget=" + snapshot.SkippedByBudget +
                  " routeUpdates=" + snapshot.RouteUpdatesUsed +
                  " pathRequests=" + snapshot.PathRequestsUsed +
                  " stimulusInterrupts=" + snapshot.StimulusInterruptsUsed + ".");
    }

    private static OUTL_World ResolveWorld()
    {
        if (OUTL_World.Instance != null) return OUTL_World.Instance;
        return Object.FindObjectOfType<OUTL_World>();
    }

    private static int CountPayloads(OUTL_WorldSaveFile file)
    {
        if (file == null || file.Entities == null) return 0;
        int count = 0;
        for (int i = 0; i < file.Entities.Count; i++)
        {
            OUTL_EntitySaveRecord record = file.Entities[i];
            if (record != null && record.ComponentPayloads != null) count += record.ComponentPayloads.Count;
        }
        return count;
    }
}
#endif
