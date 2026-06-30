#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class OUTL_NPCWorldWorkbenchEditor
{
    private const string MenuRoot = "OUT CORE Lite/Legacy Demo/Workbench/";
    private const string Root = "Assets/OUT/OUT_Core/OUT_CORE_Lite/Samples/NPCWorld";

    // [MenuItem(MenuRoot + "Create Merchant Traveler Preset")]
    public static void CreateMerchantTravelerPreset()
    {
        CreatePreset("MerchantTraveler", OUTL_NPCBehaviorArchetype.MerchantTraveler, OUTL_NPCScheduleActionType.TravelTo, OUTL_NPCScheduleActionType.Trade, true);
    }

    // [MenuItem(MenuRoot + "Create Bandit Patrol Preset")]
    public static void CreateBanditPatrolPreset()
    {
        CreatePreset("BanditPatrol", OUTL_NPCBehaviorArchetype.BanditPatrol, OUTL_NPCScheduleActionType.Patrol, OUTL_NPCScheduleActionType.Loot, false);
    }

    // [MenuItem(MenuRoot + "Create Guard Preset")]
    public static void CreateGuardPreset()
    {
        CreatePreset("Guard", OUTL_NPCBehaviorArchetype.Guard, OUTL_NPCScheduleActionType.Guard, OUTL_NPCScheduleActionType.Investigate, false);
    }

    // [MenuItem(MenuRoot + "Create NPC Schedule Demo")]
    public static void CreateNpcScheduleDemo()
    {
        OUTL_NPCBehaviorModel merchant = CreatePreset("MerchantTraveler", OUTL_NPCBehaviorArchetype.MerchantTraveler, OUTL_NPCScheduleActionType.TravelTo, OUTL_NPCScheduleActionType.Trade, true);
        OUTL_NPCBehaviorModel bandit = CreatePreset("BanditPatrol", OUTL_NPCBehaviorArchetype.BanditPatrol, OUTL_NPCScheduleActionType.Patrol, OUTL_NPCScheduleActionType.Loot, false);
        OUTL_NPCBehaviorModel guard = CreatePreset("Guard", OUTL_NPCBehaviorArchetype.Guard, OUTL_NPCScheduleActionType.Guard, OUTL_NPCScheduleActionType.Investigate, false);

        GameObject root = new GameObject("OUTL_NPCSchedule_Demo");
        OUTL_World world = Object.FindObjectOfType<OUTL_World>();
        if (world == null)
        {
            GameObject worldGo = new GameObject("OUTL_World");
            world = worldGo.AddComponent<OUTL_World>();
            worldGo.transform.SetParent(root.transform);
        }

        CreateDemoNpc(root.transform, "Merchant", merchant, new Vector3(-4f, 0f, 0f));
        CreateDemoNpc(root.transform, "Bandit", bandit, new Vector3(0f, 0f, 0f));
        CreateDemoNpc(root.transform, "Guard", guard, new Vector3(4f, 0f, 0f));
        Selection.activeObject = root;
        Debug.Log("Created minimal NPC schedule demo with merchant, bandit and guard.", root);
    }

    // [MenuItem(MenuRoot + "Create Death Network NPC Demo")]
    public static void CreateDeathNetworkNpcDemo()
    {
        EnsureFolder();
        GameObject root = new GameObject("OUTL_DeathNetworkNPC_Demo");
        if (Object.FindObjectOfType<OUTL_World>() == null) root.AddComponent<OUTL_World>();
        root.AddComponent<OUTL_NetworkSession>();
        Selection.activeObject = root;
        Debug.Log("Created minimal Death Network NPC demo root. Add actors with OUTL_EntityAdapter, OUTL_Vitals, OUTL_DamageReceiver, OUTL_DeathRuntime and OUTL_LootDropper.", root);
    }

    // [MenuItem(MenuRoot + "Validate NPC Behavior Slice")]
    public static void ValidateNpcBehaviorSlice()
    {
        OUTL_NPCBehaviorController[] controllers = Object.FindObjectsOfType<OUTL_NPCBehaviorController>(true);
        int warnings = 0;
        for (int i = 0; i < controllers.Length; i++)
        {
            OUTL_NPCBehaviorController c = controllers[i];
            if (c == null) continue;
            if (c.Entity == null && c.GetComponent<OUTL_EntityAdapter>() == null) { Debug.LogWarning("NPC behavior missing OUTL_EntityAdapter", c); warnings++; }
            if (c.Model == null) { Debug.LogWarning("NPC behavior missing model", c); warnings++; }
            if (c.GetComponent<OUTL_DeathRuntime>() == null) { Debug.LogWarning("NPC behavior actor missing OUTL_DeathRuntime", c); warnings++; }
        }
        Debug.Log("OUTL NPC Behavior Slice validation complete. controllers=" + controllers.Length + " warnings=" + warnings);
    }

    private static OUTL_NPCBehaviorModel CreatePreset(string prefix, OUTL_NPCBehaviorArchetype archetype, OUTL_NPCScheduleActionType firstAction, OUTL_NPCScheduleActionType secondAction, bool social)
    {
        EnsureFolder();
        OUTL_NPCNavigationProfile nav = ScriptableObject.CreateInstance<OUTL_NPCNavigationProfile>();
        nav.ProfileId = prefix.ToLowerInvariant() + "_nav";
        string navPath = AssetDatabase.GenerateUniqueAssetPath(Root + "/OUTL_Nav_" + prefix + ".asset");
        AssetDatabase.CreateAsset(nav, navPath);

        OUTL_NPCScheduleDef schedule = ScriptableObject.CreateInstance<OUTL_NPCScheduleDef>();
        schedule.ScheduleId = prefix.ToLowerInvariant() + "_schedule";
        schedule.Entries = new[]
        {
            new OUTL_NPCScheduleEntry { EntryId = "morning", Action = firstAction, TargetMode = OUTL_NPCScheduleTargetMode.FixedWorldPosition, TargetPosition = new Vector3(8f, 0f, 8f), StartTimeNormalized = 0f, EndTimeNormalized = 0.5f, RouteKey = prefix + "_morning" },
            new OUTL_NPCScheduleEntry { EntryId = "evening", Action = secondAction, TargetMode = OUTL_NPCScheduleTargetMode.FixedWorldPosition, TargetPosition = new Vector3(-8f, 0f, -8f), StartTimeNormalized = 0.5f, EndTimeNormalized = 1f, RouteKey = prefix + "_evening" }
        };
        string schedulePath = AssetDatabase.GenerateUniqueAssetPath(Root + "/OUTL_Schedule_" + prefix + ".asset");
        AssetDatabase.CreateAsset(schedule, schedulePath);

        OUTL_NPCBehaviorModel model = ScriptableObject.CreateInstance<OUTL_NPCBehaviorModel>();
        model.ModelId = prefix.ToLowerInvariant();
        model.Archetype = archetype;
        model.Schedule = schedule;
        model.NavigationProfile = nav;
        model.Role = prefix.ToLowerInvariant();
        model.InterruptPolicies = new[]
        {
            new OUTL_NPCStimulusInterruptPolicy { StimulusTypes = new[] { OUTL_StimulusType.Death, OUTL_StimulusType.HeardCombat, OUTL_StimulusType.Fire }, MinimumPriority = 0.25f, InterruptAction = OUTL_NPCScheduleActionType.Flee, MaxDuration = 8f },
            new OUTL_NPCStimulusInterruptPolicy { StimulusTypes = social ? new[] { OUTL_StimulusType.Social, OUTL_StimulusType.Resource } : new[] { OUTL_StimulusType.HeardNoise, OUTL_StimulusType.Resource }, MinimumPriority = 0.2f, InterruptAction = social ? OUTL_NPCScheduleActionType.Trade : OUTL_NPCScheduleActionType.Investigate, MaxDuration = 6f }
        };
        string modelPath = AssetDatabase.GenerateUniqueAssetPath(Root + "/OUTL_Model_" + prefix + ".asset");
        AssetDatabase.CreateAsset(model, modelPath);
        AssetDatabase.SaveAssets();
        Selection.activeObject = model;
        Debug.Log("Created OUTL NPC preset: " + prefix + " at " + Root, model);
        return model;
    }

    private static GameObject CreateDemoNpc(Transform parent, string name, OUTL_NPCBehaviorModel model, Vector3 position)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "OUTL_Demo_" + name;
        go.transform.SetParent(parent);
        go.transform.position = position;
        OUTL_EntityAdapter entity = go.AddComponent<OUTL_EntityAdapter>();
        entity.ClassNameOverride = "npc_" + name.ToLowerInvariant();
        entity.TargetName = "npc." + name.ToLowerInvariant();
        entity.StableId = "demo.npc." + name.ToLowerInvariant();
        entity.RestoreSpawnIfMissing = true;
        OUTL_Vitals vitals = go.AddComponent<OUTL_Vitals>();
        vitals.Entity = entity;
        go.AddComponent<OUTL_DamageReceiver>().Entity = entity;
        go.AddComponent<OUTL_DeathRuntime>().Entity = entity;
        OUTL_DeathHandler death = go.AddComponent<OUTL_DeathHandler>();
        death.Entity = entity;
        death.QueueDespawn = false;
        OUTL_NavMeshMover mover = go.AddComponent<OUTL_NavMeshMover>();
        mover.UseTransformFallback = true;
        mover.UseOUTLTick = true;
        OUTL_NPCBehaviorController behavior = go.AddComponent<OUTL_NPCBehaviorController>();
        behavior.Entity = entity;
        behavior.NavMover = mover;
        behavior.Model = model;
        if (name == "Bandit" || name == "Guard")
            go.AddComponent<OUTL_AttackDriver>().Source = entity;
        return go;
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/OUT/OUT_Core/OUT_CORE_Lite/Samples"))
            AssetDatabase.CreateFolder("Assets/OUT/OUT_Core/OUT_CORE_Lite", "Samples");
        if (!AssetDatabase.IsValidFolder(Root))
            AssetDatabase.CreateFolder("Assets/OUT/OUT_Core/OUT_CORE_Lite/Samples", "NPCWorld");
    }
}
#endif
