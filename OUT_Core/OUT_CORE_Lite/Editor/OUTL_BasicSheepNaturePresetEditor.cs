#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

public static class OUTL_BasicSheepNaturePresetEditor
{
    private const string MenuRoot = "OUT CORE Lite/Advanced/Samples/Nature/";
    private const string RootFolder = "Assets/OUT/OUT_Core/OUT_CORE_Lite/Actors/Nature/BasicSheep";
    private const string DefFolder = RootFolder + "/Defs";
    private const string ProfileFolder = RootFolder + "/Profiles";
    private const string ItemFolder = RootFolder + "/Items";
    private const string LootFolder = RootFolder + "/Loot";
    private const string PrefabFolder = RootFolder + "/Prefabs";
    private const string GenericPerceptionPath = "Assets/OUT/OUT_Core/OUT_CORE_Lite/Templates/Foundation/AI/OUTL_AIPerception_Generic.asset";
    private const string GenericStateTablePath = "Assets/OUT/OUT_Core/OUT_CORE_Lite/Templates/Foundation/AI/OUTL_AIStateTable_GenericCombat.asset";

    [MenuItem(MenuRoot + "Create Basic Sheep")]
    public static void CreateBasicSheep()
    {
        EnsureFolders();

        OUTL_FactionDef passiveFaction = EnsureFaction("OUTL_Faction_PassiveAnimal.asset", "faction.passive_animal", "Passive Animal");
        OUTL_EntityDef sheepDef = EnsureSheepEntityDef();
        OUTL_ActorShapeProfileDef shape = EnsureSheepShapeProfile();
        OUTL_HurtboxProfileDef hurtboxes = EnsureSheepHurtboxProfile();
        OUTL_NPCNavigationProfile nav = EnsureSheepNavigationProfile();
        OUTL_NPCScheduleDef schedule = EnsureSheepSchedule();
        OUTL_NPCBehaviorModel behavior = EnsureSheepBehaviorModel(schedule, nav);
        OUTL_AIProfile ai = EnsureSheepAIProfile();
        OUTL_DriveProfileDef drives = EnsureSheepDriveProfile();
        OUTL_ItemDef meat = EnsureItem("OUTL_Item_Sheep_Meat.asset", "item.food.sheep_meat", "Sheep Meat", 8, new[] { "Item", "Food", "Meat", "Sheep" });
        OUTL_ItemDef wool = EnsureItem("OUTL_Item_Sheep_Wool.asset", "item.material.wool", "Wool", 16, new[] { "Item", "Material", "Wool", "Sheep" });
        OUTL_ItemDef dung = EnsureItem("OUTL_Item_Sheep_Dung.asset", "item.resource.dung", "Dung", 32, new[] { "Item", "Resource", "Dung", "Fertilizer" });
        GameObject pickupPrefab = CreateSheepPickupPrefab(passiveFaction, wool);
        OUTL_BehaviorActionSetDef actions = EnsureSheepActionSet(dung, pickupPrefab);
        OUTL_LootTableDef loot = EnsureSheepLootTable(meat, wool, dung, pickupPrefab);

        GameObject sheepPrefab = CreateSheepPrefab(sheepDef, passiveFaction, shape, hurtboxes, nav, behavior, ai, drives, actions, loot, pickupPrefab);
        sheepDef.Prefab = sheepPrefab;
        EditorUtility.SetDirty(sheepDef);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = sheepPrefab;
        ValidateBasicSheep();
        Debug.Log("OUTL Basic Sheep created at " + PrefabFolder + "/OUTL_Nature_BasicSheep.prefab. Profiles are in " + ProfileFolder + ".");
    }

    [MenuItem(MenuRoot + "Add Basic Sheep To Scene")]
    public static void AddBasicSheepToScene()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/OUTL_Nature_BasicSheep.prefab");
        if (prefab == null)
        {
            CreateBasicSheep();
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/OUTL_Nature_BasicSheep.prefab");
        }
        if (prefab == null)
        {
            Debug.LogError("OUTL Basic Sheep prefab could not be created.");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "OUTL_Nature_BasicSheep";
        instance.transform.position = GetSpawnPosition();
        OUTL_EntityAdapter entity = instance.GetComponent<OUTL_EntityAdapter>();
        if (entity != null)
        {
            entity.StableId = "sheep.basic." + System.Guid.NewGuid().ToString("N").Substring(0, 8);
            entity.TargetName = entity.StableId;
            EditorUtility.SetDirty(entity);
        }
        Selection.activeGameObject = instance;
    }

    [MenuItem(MenuRoot + "Create Living Gameplay Playground")]
    public static void CreateLivingGameplayPlayground()
    {
        CreateBasicSheep();
        OUTL_AbstractPrefabGeneratorEditor.GenerateAllFoundationPrefabs();

        GameObject existing = GameObject.Find("OUTL_LivingGameplayPlayground");
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        GameObject root = new GameObject("OUTL_LivingGameplayPlayground");
        Undo.RegisterCreatedObjectUndo(root, "Create OUTL Living Gameplay Playground");

        OUTL_World world = CreatePlaygroundRuntime(root.transform);
        CreatePlaygroundGround(root.transform);
        GameObject player = CreatePlaygroundPlayer(root.transform);
        CreatePlaygroundSheep(root.transform);
        CreatePlaygroundResourceDefs(out OUTL_EntityDef grassDef, out OUTL_EntityDef dangerDef);

        Vector3[] grassPositions =
        {
            new Vector3(-5f, 0.08f, -1f),
            new Vector3(-2f, 0.08f, 3f),
            new Vector3(2.5f, 0.08f, 2f),
            new Vector3(5f, 0.08f, -2.5f),
            new Vector3(0f, 0.08f, -4.5f)
        };
        for (int i = 0; i < grassPositions.Length; i++)
            CreateLivingResourcePatch(root.transform, grassDef, "GrassPatch_" + i, "living.grass." + i, grassPositions[i], new Vector3(1.8f, 0.16f, 1.8f), OUTL_StimulusType.SightFood, new[] { "Resource", "Food", "Grass" }, 24f, 0.25f, 9f);

        CreateLivingResourcePatch(root.transform, dangerDef, "PredatorDangerDummy", "living.danger.predator", new Vector3(7f, 0.5f, 4.5f), new Vector3(0.7f, 1f, 0.7f), OUTL_StimulusType.SightDanger, new[] { "Danger", "Predator", "Hostile" }, 999f, 0f, 14f);
        CreatePlaygroundEgregore(root.transform);

        if (world != null)
        {
            world.MaterializationFocusTargetName = "living.player";
            EditorUtility.SetDirty(world);
        }
        if (player != null) Selection.activeGameObject = player;
        EditorSceneManager.MarkSceneDirty(root.scene);
        AssetDatabase.SaveAssets();
        Debug.Log(ValidateLivingGameplayPlaygroundInternal(root), root);
    }

    [MenuItem(MenuRoot + "Validate Living Gameplay Playground")]
    public static void ValidateLivingGameplayPlayground()
    {
        GameObject root = GameObject.Find("OUTL_LivingGameplayPlayground");
        string report = ValidateLivingGameplayPlaygroundInternal(root);
        if (report.IndexOf("[ERROR]", System.StringComparison.Ordinal) >= 0) Debug.LogError(report, root);
        else Debug.Log(report, root);
    }

    private static OUTL_World CreatePlaygroundRuntime(Transform parent)
    {
        GameObject runtime = new GameObject("OUTL_Runtime");
        Undo.RegisterCreatedObjectUndo(runtime, "Create OUTL Runtime");
        runtime.transform.SetParent(parent, false);

        OUTL_World world = Undo.AddComponent<OUTL_World>(runtime);
        world.UpdateMode = OUTL_WorldUpdateMode.CustomFixedStep;
        world.SimulationStep = 0.05f;
        world.MaxSimulationStepsPerFrame = 4;
        world.AutoFindAdaptersOnStart = true;
        world.EnableAutomaticMaterialization = true;
        world.MaterializationTickInterval = 0.5f;
        world.MaterializationBudgetPerTick = 4;
        world.MaterializeEnterDistance = 36f;
        world.DematerializeExitDistance = 64f;

        OUTL_PoolSystem pool = Undo.AddComponent<OUTL_PoolSystem>(runtime);
        pool.DefaultCapacity = 8;
        pool.MaxSize = 256;
        pool.CollectionChecks = false;

        Undo.AddComponent<OUTL_QuickSaveInput>(runtime);
        OUTL_DevConsole console = Undo.AddComponent<OUTL_DevConsole>(runtime);
        console.ToggleKey = KeyCode.BackQuote;
        console.AlternateToggleKeys = new[] { KeyCode.F1, KeyCode.Backslash, KeyCode.Insert };
        console.PauseGameWhenOpen = true;
        console.UnlockCursorWhenOpen = true;
        console.RestoreCursorLockOnClose = true;
        return world;
    }

    private static void CreatePlaygroundGround(Transform parent)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(ground, "Create Living Ground");
        ground.name = "LivingGround";
        ground.transform.SetParent(parent, false);
        ground.transform.position = new Vector3(0f, -0.05f, 0f);
        ground.transform.localScale = new Vector3(24f, 0.1f, 24f);
        ground.isStatic = true;
    }

    private static GameObject CreatePlaygroundPlayer(Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/OUT/OUT_Core/OUT_CORE_Lite/Templates/Foundation/Prefabs/OUTL_Abstract_Actor_Controlled.prefab");
        if (prefab == null)
        {
            Debug.LogWarning("Foundation controlled actor prefab missing. This sample is under Advanced; use OUT CORE Lite/Scene Setup for production scenes.");
            return null;
        }

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(player, "Create Living Player");
        player.name = "PlayerActor";
        player.transform.SetParent(parent, true);
        player.transform.position = new Vector3(0f, 0f, -7f);
        ConfigureSceneEntity(player, "living.player", "actor.controlled", true);

        OUTL_InventoryRuntime inventory = player.GetComponent<OUTL_InventoryRuntime>();
        if (inventory == null) inventory = Undo.AddComponent<OUTL_InventoryRuntime>(player);
        inventory.Entity = player.GetComponent<OUTL_EntityAdapter>();
        inventory.KnownItems = new[]
        {
            AssetDatabase.LoadAssetAtPath<OUTL_ItemDef>(ItemFolder + "/OUTL_Item_Sheep_Meat.asset"),
            AssetDatabase.LoadAssetAtPath<OUTL_ItemDef>(ItemFolder + "/OUTL_Item_Sheep_Wool.asset"),
            AssetDatabase.LoadAssetAtPath<OUTL_ItemDef>(ItemFolder + "/OUTL_Item_Sheep_Dung.asset")
        };
        EditorUtility.SetDirty(player);
        return player;
    }

    private static void CreatePlaygroundSheep(Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/OUTL_Nature_BasicSheep.prefab");
        if (prefab == null) return;
        Vector3[] positions =
        {
            new Vector3(-2.5f, 0f, 0f),
            new Vector3(0f, 0f, 1.5f),
            new Vector3(2.5f, 0f, 0f)
        };
        for (int i = 0; i < positions.Length; i++)
        {
            GameObject sheep = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(sheep, "Create Living Sheep");
            sheep.name = "BasicSheep_" + i;
            sheep.transform.SetParent(parent, true);
            sheep.transform.position = positions[i];
            ConfigureSceneEntity(sheep, "living.sheep." + i, "actor.nature.basic_sheep", true);
        }
    }

    private static void CreatePlaygroundResourceDefs(out OUTL_EntityDef grassDef, out OUTL_EntityDef dangerDef)
    {
        grassDef = EnsureAsset<OUTL_EntityDef>(DefFolder + "/OUTL_Def_Living_GrassResource.asset");
        grassDef.ClassName = "resource.living.grass";
        grassDef.DisplayName = "Living Grass Resource";
        grassDef.Tags = new[] { "Resource", "Food", "Grass" };
        grassDef.BaseStats = new OUTL_StatEntry[0];
        EditorUtility.SetDirty(grassDef);

        dangerDef = EnsureAsset<OUTL_EntityDef>(DefFolder + "/OUTL_Def_Living_DangerSource.asset");
        dangerDef.ClassName = "stimulus.living.danger";
        dangerDef.DisplayName = "Living Danger Source";
        dangerDef.Tags = new[] { "Danger", "Predator", "Hostile" };
        dangerDef.BaseStats = new OUTL_StatEntry[0];
        EditorUtility.SetDirty(dangerDef);
    }

    private static GameObject CreateLivingResourcePatch(Transform parent, OUTL_EntityDef def, string name, string stableId, Vector3 position, Vector3 scale, OUTL_StimulusType stimulus, string[] tags, float amount, float regen, float radius)
    {
        GameObject go = GameObject.CreatePrimitive(stimulus == OUTL_StimulusType.SightDanger ? PrimitiveType.Capsule : PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(go, "Create Living Resource");
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = scale;

        Collider collider = go.GetComponent<Collider>();
        if (collider != null) collider.isTrigger = stimulus != OUTL_StimulusType.SightDanger;

        OUTL_EntityAdapter entity = Undo.AddComponent<OUTL_EntityAdapter>(go);
        entity.Def = def;
        entity.ClassNameOverride = def != null ? def.ClassName : stableId;
        entity.TargetName = stableId;
        entity.StableId = stableId;
        entity.SavePersistent = true;
        entity.RegisterOnEnable = true;
        entity.RegisterTick = false;
        entity.RegisterInSectors = true;

        OUTL_LivingResourceSource source = Undo.AddComponent<OUTL_LivingResourceSource>(go);
        source.Entity = entity;
        source.ResourceId = stableId;
        source.ResourceTags = tags;
        source.EgregoreTags = tags;
        source.StimulusType = stimulus;
        source.AlsoEmitResourceStimulus = stimulus == OUTL_StimulusType.SightFood;
        source.Amount = amount;
        source.MaxAmount = amount;
        source.RegenerationPerSecond = regen;
        source.ConsumeAmount = 1f;
        source.Radius = radius;
        source.Priority = stimulus == OUTL_StimulusType.SightDanger ? 0.85f : 0.35f;
        source.TickInterval = 1f;
        EditorUtility.SetDirty(go);
        return go;
    }

    private static GameObject CreatePlaygroundEgregore(Transform parent)
    {
        OUTL_LocalEgregoreDef def = EnsurePlaygroundEgregoreDef();
        GameObject go = new GameObject("LivingLocalEgregore");
        Undo.RegisterCreatedObjectUndo(go, "Create Living Egregore");
        go.transform.SetParent(parent, false);
        go.transform.position = Vector3.zero;

        OUTL_EntityAdapter entity = Undo.AddComponent<OUTL_EntityAdapter>(go);
        entity.ClassNameOverride = "egregore.local.living";
        entity.TargetName = "living.egregore.local";
        entity.StableId = "living.egregore.local";
        entity.SavePersistent = true;
        entity.RegisterOnEnable = true;
        entity.RegisterTick = false;
        entity.RegisterInSectors = true;

        OUTL_EgregoreComponent component = Undo.AddComponent<OUTL_EgregoreComponent>(go);
        component.Def = def;
        component.AutoRegister = true;
        component.UseDefScope = true;
        component.UseDefScale = true;
        return go;
    }

    private static OUTL_LocalEgregoreDef EnsurePlaygroundEgregoreDef()
    {
        OUTL_LocalEgregoreDef def = EnsureAsset<OUTL_LocalEgregoreDef>(ProfileFolder + "/OUTL_Egregore_LivingPlayground.asset");
        def.EgregoreId = "egregore.living.playground";
        def.DisplayName = "Living Playground Egregore";
        def.DebugName = "Living Playground";
        def.Scope = OUTL_EgregoreScope.Local;
        def.PlaceArchetype = OUTL_LocalEgregoreArchetype.Forest;
        def.UpdateInterval = 2f;
        def.InfluenceRadius = 30f;
        def.FearWeight = 0.28f;
        def.ProsperityWeight = 0.34f;
        def.ResourceWeight = 0.42f;
        def.HostilityWeight = 0.30f;
        def.BehaviorModifiers = new[]
        {
            new OUTL_EgregoreBehaviorModifier { Phase = OUTL_EgregoreCyclePhase.Renewal, BehaviorMode = OUTL_BehaviorModeId.Work, Priority = 0.45f },
            new OUTL_EgregoreBehaviorModifier { Phase = OUTL_EgregoreCyclePhase.ShadowConfrontation, BehaviorMode = OUTL_BehaviorModeId.Flee, Priority = 0.75f },
            new OUTL_EgregoreBehaviorModifier { Phase = OUTL_EgregoreCyclePhase.Collapse, BehaviorMode = OUTL_BehaviorModeId.Flee, Priority = 0.9f }
        };
        EditorUtility.SetDirty(def);
        return def;
    }

    private static void ConfigureSceneEntity(GameObject go, string stableId, string className, bool persistent)
    {
        if (go == null) return;
        OUTL_EntityAdapter entity = go.GetComponent<OUTL_EntityAdapter>();
        if (entity == null) entity = Undo.AddComponent<OUTL_EntityAdapter>(go);
        entity.TargetName = stableId;
        entity.StableId = stableId;
        entity.ClassNameOverride = className;
        entity.SavePersistent = persistent;
        entity.RegisterOnEnable = true;
        entity.RegisterInSectors = true;
        EditorUtility.SetDirty(entity);
    }

    private static string ValidateLivingGameplayPlaygroundInternal(GameObject root)
    {
        StringBuilder sb = new StringBuilder(1024);
        int errors = 0;
        int warnings = 0;
        sb.AppendLine("OUTL Living Gameplay Playground validation:");
        Check(root != null, "playground root exists", ref errors, sb);
        if (root == null) return sb.ToString();

        Check(root.GetComponentInChildren<OUTL_World>(true) != null, "world exists", ref errors, sb);
        Check(root.GetComponentInChildren<OUTL_PoolSystem>(true) != null, "pool exists", ref errors, sb);
        Check(root.GetComponentInChildren<OUTL_DevConsole>(true) != null, "console exists", ref errors, sb);
        Check(root.GetComponentInChildren<OUTL_EgregoreComponent>(true) != null, "local egregore exists", ref errors, sb);
        Check(CountStableEntities(root, "living.sheep.") >= 3, "at least 3 sheep exist", ref errors, sb);
        Check(CountResources(root, OUTL_StimulusType.SightFood) >= 5, "at least 5 food/grass resources exist", ref errors, sb);
        Check(CountResources(root, OUTL_StimulusType.SightDanger) >= 1, "danger stimulus source exists", ref errors, sb);
        Check(FindStableEntity(root, "living.player") != null, "player actor exists", ref errors, sb);

        OUTL_DriveRuntime[] drives = root.GetComponentsInChildren<OUTL_DriveRuntime>(true);
        for (int i = 0; i < drives.Length; i++)
        {
            if (drives[i] == null) continue;
            Check(drives[i].TargetMemory != null || drives[i].GetComponent<OUTL_LivingActionTargetMemory>() != null, drives[i].name + " has living target memory", ref errors, sb);
            Check(drives[i].ActionSet != null, drives[i].name + " has action set", ref errors, sb);
            if (drives[i].transform.position.y < -0.5f)
            {
                errors++;
                sb.AppendLine("[ERROR] " + drives[i].name + " is below ground");
            }
        }

        if (Application.isPlaying)
        {
            for (int i = 0; i < drives.Length; i++)
            {
                OUTL_EntityAdapter entity = drives[i] != null ? drives[i].Entity : null;
                Check(entity != null && entity.Id.IsValid, drives[i].name + " has runtime id in Play Mode", ref errors, sb);
                Check(drives[i].CurrentAction != OUTL_BehaviorActionId.Count, drives[i].name + " has valid current action", ref errors, sb);
            }
        }
        else
        {
            warnings++;
            sb.AppendLine("[WARN] Play Mode runtime id/action checks skipped outside Play Mode");
        }

        if (LivingRuntimeFileHasDirectConstruction("AI/OUTL_LivingResourceSource.cs") || LivingRuntimeFileHasDirectConstruction("AI/OUTL_DriveActionLayer.cs"))
        {
            errors++;
            sb.AppendLine("[ERROR] living runtime file contains direct runtime construction token");
        }
        else sb.AppendLine("[OK] living runtime files avoid direct construction tokens");

        sb.AppendLine("Errors: " + errors + ", warnings: " + warnings);
        return sb.ToString();
    }

    private static int CountStableEntities(GameObject root, string stablePrefix)
    {
        if (root == null) return 0;
        int count = 0;
        OUTL_EntityAdapter[] entities = root.GetComponentsInChildren<OUTL_EntityAdapter>(true);
        for (int i = 0; i < entities.Length; i++)
            if (entities[i] != null && !string.IsNullOrEmpty(entities[i].StableId) && entities[i].StableId.StartsWith(stablePrefix, System.StringComparison.Ordinal))
                count++;
        return count;
    }

    private static OUTL_EntityAdapter FindStableEntity(GameObject root, string stableId)
    {
        if (root == null) return null;
        OUTL_EntityAdapter[] entities = root.GetComponentsInChildren<OUTL_EntityAdapter>(true);
        for (int i = 0; i < entities.Length; i++)
            if (entities[i] != null && entities[i].StableId == stableId)
                return entities[i];
        return null;
    }

    private static int CountResources(GameObject root, OUTL_StimulusType type)
    {
        if (root == null) return 0;
        int count = 0;
        OUTL_LivingResourceSource[] sources = root.GetComponentsInChildren<OUTL_LivingResourceSource>(true);
        for (int i = 0; i < sources.Length; i++)
            if (sources[i] != null && sources[i].StimulusType == type)
                count++;
        return count;
    }

    private static bool LivingRuntimeFileHasDirectConstruction(string relativePath)
    {
        string path = "Assets/OUT/OUT_Core/OUT_CORE_Lite/" + relativePath;
        if (!System.IO.File.Exists(path)) return false;
        string text = System.IO.File.ReadAllText(path);
        return text.IndexOf("new GameObject", System.StringComparison.Ordinal) >= 0 ||
               text.IndexOf("Object.Instantiate", System.StringComparison.Ordinal) >= 0 ||
               text.IndexOf("UnityEngine.Object.Instantiate", System.StringComparison.Ordinal) >= 0 ||
               text.IndexOf("Destroy(", System.StringComparison.Ordinal) >= 0 ||
               text.IndexOf("AddComponent<", System.StringComparison.Ordinal) >= 0;
    }

    [MenuItem(MenuRoot + "Validate Basic Sheep")]
    public static void ValidateBasicSheep()
    {
        StringBuilder sb = new StringBuilder(1024);
        int errors = 0;
        int warnings = 0;
        sb.AppendLine("OUTL Basic Sheep validation:");

        GameObject prefab = LoadRequiredAsset<GameObject>("prefab", PrefabFolder + "/OUTL_Nature_BasicSheep.prefab", ref errors, sb);
        LoadRequiredAsset<OUTL_EntityDef>("entity def", DefFolder + "/OUTL_Def_Nature_BasicSheep.asset", ref errors, sb);
        LoadRequiredAsset<OUTL_FactionDef>("passive faction", DefFolder + "/OUTL_Faction_PassiveAnimal.asset", ref errors, sb);
        OUTL_ActorShapeProfileDef shape = LoadRequiredAsset<OUTL_ActorShapeProfileDef>("shape profile", ProfileFolder + "/OUTL_Shape_QuadrupedSmall_Sheep.asset", ref errors, sb);
        OUTL_HurtboxProfileDef hurtboxes = LoadRequiredAsset<OUTL_HurtboxProfileDef>("hurtbox profile", ProfileFolder + "/OUTL_Hurtbox_QuadrupedSmall_Sheep.asset", ref errors, sb);
        LoadRequiredAsset<OUTL_NPCNavigationProfile>("navigation profile", ProfileFolder + "/OUTL_Nav_Sheep_Herbivore.asset", ref errors, sb);
        LoadRequiredAsset<OUTL_NPCScheduleDef>("schedule", ProfileFolder + "/OUTL_Schedule_Sheep_DayGrazeNightRest.asset", ref errors, sb);
        LoadRequiredAsset<OUTL_NPCBehaviorModel>("npc behavior model", ProfileFolder + "/OUTL_NPCBehavior_Sheep_Herbivore.asset", ref errors, sb);
        LoadRequiredAsset<OUTL_AIProfile>("ai profile", ProfileFolder + "/OUTL_AI_Sheep_PassiveHerbivore.asset", ref errors, sb);
        LoadRequiredAsset<OUTL_DriveProfileDef>("drive profile", ProfileFolder + "/OUTL_Drive_Sheep_HerbivorePassive.asset", ref errors, sb);
        OUTL_BehaviorActionSetDef actionSet = LoadRequiredAsset<OUTL_BehaviorActionSetDef>("action set", ProfileFolder + "/OUTL_ActionSet_Sheep_Herbivore.asset", ref errors, sb);
        LoadRequiredAsset<OUTL_LootTableDef>("loot table", LootFolder + "/OUTL_Loot_Sheep_Basic.asset", ref errors, sb);
        LoadRequiredAsset<OUTL_ItemDef>("meat item", ItemFolder + "/OUTL_Item_Sheep_Meat.asset", ref errors, sb);
        LoadRequiredAsset<OUTL_ItemDef>("wool item", ItemFolder + "/OUTL_Item_Sheep_Wool.asset", ref errors, sb);
        LoadRequiredAsset<OUTL_ItemDef>("dung item", ItemFolder + "/OUTL_Item_Sheep_Dung.asset", ref errors, sb);
        LoadRequiredAsset<GameObject>("pickup prefab", PrefabFolder + "/OUTL_Nature_SheepLootPickup.prefab", ref errors, sb);

        if (prefab != null)
        {
            Check(prefab.GetComponent<OUTL_EntityAdapter>() != null, "prefab has OUTL_EntityAdapter", ref errors, sb);
            Check(prefab.GetComponent<OUTL_Vitals>() != null, "prefab has OUTL_Vitals", ref errors, sb);
            Check(prefab.GetComponent<OUTL_DamageReceiver>() != null, "prefab has OUTL_DamageReceiver", ref errors, sb);
            Check(prefab.GetComponent<OUTL_DeathHandler>() != null, "prefab has OUTL_DeathHandler", ref errors, sb);
            Check(prefab.GetComponent<OUTL_DeathRuntime>() != null, "prefab has OUTL_DeathRuntime", ref errors, sb);
            Check(prefab.GetComponent<OUTL_AIActor>() != null, "prefab has OUTL_AIActor", ref errors, sb);
            Check(prefab.GetComponent<OUTL_NPCBehaviorController>() != null, "prefab has OUTL_NPCBehaviorController", ref errors, sb);
            Check(prefab.GetComponent<OUTL_DriveRuntime>() != null, "prefab has OUTL_DriveRuntime", ref errors, sb);
            Check(prefab.GetComponent<OUTL_LivingActionTargetMemory>() != null, "prefab has OUTL_LivingActionTargetMemory", ref errors, sb);
            Check(prefab.GetComponent<OUTL_BotInputDriver>() != null, "prefab has OUTL_BotInputDriver", ref errors, sb);
            Check(prefab.GetComponent<OUTL_ActorControlBridge>() != null, "prefab has OUTL_ActorControlBridge", ref errors, sb);
            Check(prefab.GetComponent<OUTL_NavMoverInputSink>() != null, "prefab has OUTL_NavMoverInputSink", ref errors, sb);
            Check(prefab.GetComponent<OUTL_NavMeshMover>() != null, "prefab has OUTL_NavMeshMover", ref errors, sb);
            Check(prefab.GetComponent<NavMeshAgent>() != null, "prefab has NavMeshAgent", ref errors, sb);
            Check(prefab.GetComponent<OUTL_LootDropper>() != null, "prefab has OUTL_LootDropper", ref errors, sb);
            Check(prefab.GetComponent<OUTL_ActorShapeRuntime>() != null, "prefab has OUTL_ActorShapeRuntime", ref errors, sb);
            Check(prefab.GetComponentsInChildren<OUTL_Hitbox>(true).Length >= 3, "prefab has authored hitbox children", ref errors, sb);

            OUTL_AIActor sheepAI = prefab.GetComponent<OUTL_AIActor>();
            if (sheepAI != null)
            {
                Check(sheepAI.PerceptionProfile != null, "AI actor has perception profile", ref errors, sb);
                Check(sheepAI.StateTable != null, "AI actor has visible state table", ref errors, sb);
                Check(sheepAI.UseActorInputContract, "AI actor uses actor input contract", ref errors, sb);
            }

            OUTL_ActorShapeRuntime shapeRuntime = prefab.GetComponent<OUTL_ActorShapeRuntime>();
            if (shapeRuntime != null)
            {
                Check(shapeRuntime.ShapeProfile != null, "shape runtime has shape profile", ref errors, sb);
                Check(shapeRuntime.HurtboxProfile != null, "shape runtime has hurtbox profile", ref errors, sb);
                Check(!shapeRuntime.AutoApplyHurtboxesOnAwake, "runtime auto hurtbox generation disabled", ref errors, sb);
            }

            WarnIf(prefab.GetComponent<OUTL_AttackDriver>() != null, "sheep prefab has AttackDriver; passive sheep should stay unarmed", ref warnings, sb);
            OUTL_ActorControlBridge bridge = prefab.GetComponent<OUTL_ActorControlBridge>();
            if (bridge != null)
            {
                Check(bridge.InputSourceBehaviour != null, "actor bridge has input source", ref errors, sb);
                Check(bridge.InputSinkBehaviours != null && bridge.InputSinkBehaviours.Length > 0, "actor bridge has input sinks", ref errors, sb);
                Check(bridge.ApplyNearActorsEveryFrame, "near sheep input can execute every frame", ref errors, sb);
            }

            OUTL_LootDropper dropper = prefab.GetComponent<OUTL_LootDropper>();
            if (dropper != null) Check(dropper.LootTable != null, "loot dropper has loot table", ref errors, sb);
            CheckNoBannedControllers(prefab, ref errors, sb);
        }

        if (shape != null)
        {
            Check(shape.BodyLength > 0f && shape.BodyHeight > 0f && shape.BodyWidth > 0f, "shape dimensions are positive", ref errors, sb);
            Check(shape.NavAgentHeight > 0f && shape.NavAgentRadius > 0f, "shape nav hull is configured", ref errors, sb);
        }

        if (hurtboxes != null)
        {
            Check(HasHurtboxTag(hurtboxes, OUTL_HurtboxTagFlags.Body), "hurtbox profile has Body", ref errors, sb);
            Check(HasHurtboxTag(hurtboxes, OUTL_HurtboxTagFlags.Head), "hurtbox profile has Head", ref errors, sb);
            Check(HasHurtboxTag(hurtboxes, OUTL_HurtboxTagFlags.Leg), "hurtbox profile has Leg", ref errors, sb);
        }

        if (actionSet != null)
        {
            Check(HasAction(actionSet, OUTL_BehaviorActionId.Idle), "action set has Idle", ref errors, sb);
            Check(HasAction(actionSet, OUTL_BehaviorActionId.Wander), "action set has Wander", ref errors, sb);
            Check(HasAction(actionSet, OUTL_BehaviorActionId.FleeFromThreat), "action set has FleeFromThreat", ref errors, sb);
            Check(HasAction(actionSet, OUTL_BehaviorActionId.FindFood), "action set has FindFood", ref errors, sb);
            Check(HasAction(actionSet, OUTL_BehaviorActionId.Eat), "action set has Eat", ref errors, sb);
            Check(HasAction(actionSet, OUTL_BehaviorActionId.FollowHerd), "action set has FollowHerd", ref errors, sb);
            Check(HasAction(actionSet, OUTL_BehaviorActionId.Rest), "action set has Rest", ref errors, sb);
            Check(HasAction(actionSet, OUTL_BehaviorActionId.WasteDrop), "action set has WasteDrop", ref errors, sb);
            Check(HasAction(actionSet, OUTL_BehaviorActionId.SeekMate), "action set has SeekMate", ref errors, sb);
            Check(HasAction(actionSet, OUTL_BehaviorActionId.ReproduceAbstract), "action set has ReproduceAbstract", ref errors, sb);
            Check(HasLivingEffect(actionSet, OUTL_BehaviorActionId.Eat, OUTL_LivingActionEffectType.ConsumeNearbyResource), "Eat consumes a living resource", ref errors, sb);
            Check(HasLivingEffect(actionSet, OUTL_BehaviorActionId.FleeFromThreat, OUTL_LivingActionEffectType.FleeFromLastThreat), "Flee uses last threat target", ref errors, sb);
            Check(HasLivingEffect(actionSet, OUTL_BehaviorActionId.WasteDrop, OUTL_LivingActionEffectType.SpawnDrop), "WasteDrop spawns pooled pickup", ref errors, sb);
            Check(HasLivingEffect(actionSet, OUTL_BehaviorActionId.ReproduceAbstract, OUTL_LivingActionEffectType.RequestAbstractOffspring), "ReproduceAbstract stays abstract", ref errors, sb);
        }

        sb.AppendLine("Errors: " + errors + ", warnings: " + warnings);
        if (errors > 0) Debug.LogError(sb.ToString());
        else Debug.Log(sb.ToString());
    }

    private static Vector3 GetSpawnPosition()
    {
        if (Selection.activeTransform != null) return Selection.activeTransform.position + Vector3.right * 1.5f;
        SceneView view = SceneView.lastActiveSceneView;
        if (view != null) return view.pivot;
        return Vector3.zero;
    }

    private static T LoadRequiredAsset<T>(string label, string path, ref int errors, StringBuilder sb) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        Check(asset != null, label + " exists at " + path, ref errors, sb);
        return asset;
    }

    private static void Check(bool condition, string message, ref int errors, StringBuilder sb)
    {
        if (condition)
        {
            sb.AppendLine("[OK] " + message);
            return;
        }

        errors++;
        sb.AppendLine("[ERROR] " + message);
    }

    private static void WarnIf(bool condition, string message, ref int warnings, StringBuilder sb)
    {
        if (!condition) return;
        warnings++;
        sb.AppendLine("[WARN] " + message);
    }

    private static void CheckNoBannedControllers(GameObject prefab, ref int errors, StringBuilder sb)
    {
        MonoBehaviour[] behaviours = prefab.GetComponentsInChildren<MonoBehaviour>(true);
        bool foundBanned = false;
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null) continue;
            string typeName = behaviour.GetType().Name;
            bool banned =
                typeName.IndexOf("SheepController", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("AnimalManager", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("NeedsManager", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (!banned) continue;
            foundBanned = true;
            errors++;
            sb.AppendLine("[ERROR] banned controller/manager component: " + typeName);
        }

        if (!foundBanned) sb.AppendLine("[OK] no banned SheepController/AnimalManager/NeedsManager component");
    }

    private static bool HasHurtboxTag(OUTL_HurtboxProfileDef profile, OUTL_HurtboxTagFlags tag)
    {
        if (profile == null || profile.Hurtboxes == null) return false;
        for (int i = 0; i < profile.Hurtboxes.Length; i++)
        {
            OUTL_HurtboxProfileEntry entry = profile.Hurtboxes[i];
            if (entry != null && (entry.Tags & tag) != 0) return true;
        }
        return false;
    }

    private static bool HasAction(OUTL_BehaviorActionSetDef set, OUTL_BehaviorActionId action)
    {
        if (set == null || set.Actions == null) return false;
        for (int i = 0; i < set.Actions.Length; i++)
            if (set.Actions[i] != null && set.Actions[i].Type == action)
                return true;
        return false;
    }

    private static bool HasLivingEffect(OUTL_BehaviorActionSetDef set, OUTL_BehaviorActionId action, OUTL_LivingActionEffectType effectType)
    {
        if (set == null || set.Actions == null) return false;
        for (int i = 0; i < set.Actions.Length; i++)
        {
            OUTL_BehaviorActionDef def = set.Actions[i];
            if (def == null || def.Type != action || def.OnStartLivingEffects == null) continue;
            for (int e = 0; e < def.OnStartLivingEffects.Length; e++)
                if (def.OnStartLivingEffects[e] != null && def.OnStartLivingEffects[e].Type == effectType)
                    return true;
        }
        return false;
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/OUT/OUT_Core/OUT_CORE_Lite", "Actors");
        EnsureFolder("Assets/OUT/OUT_Core/OUT_CORE_Lite/Actors", "Nature");
        EnsureFolder("Assets/OUT/OUT_Core/OUT_CORE_Lite/Actors/Nature", "BasicSheep");
        EnsureFolder(RootFolder, "Defs");
        EnsureFolder(RootFolder, "Profiles");
        EnsureFolder(RootFolder, "Items");
        EnsureFolder(RootFolder, "Loot");
        EnsureFolder(RootFolder, "Prefabs");
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
    }

    private static OUTL_FactionDef EnsureFaction(string fileName, string id, string displayName)
    {
        OUTL_FactionDef faction = EnsureAsset<OUTL_FactionDef>(DefFolder + "/" + fileName);
        faction.FactionId = id;
        faction.DisplayName = displayName;
        faction.Relations = new OUTL_FactionRelation[] { new OUTL_FactionRelation { Faction = faction, Relation = 1f } };
        EditorUtility.SetDirty(faction);
        return faction;
    }

    private static OUTL_EntityDef EnsureSheepEntityDef()
    {
        OUTL_EntityDef def = EnsureAsset<OUTL_EntityDef>(DefFolder + "/OUTL_Def_Nature_BasicSheep.asset");
        def.ClassName = "actor.nature.basic_sheep";
        def.DisplayName = "Basic Sheep";
        def.Tags = new[] { "Actor", "NPC", "Creature", "Animal", "PassiveAnimal", "Herbivore", "Sheep", "Role.Targetable", "FoodSource" };
        def.BaseStats = new[]
        {
            new OUTL_StatEntry { Key = "Health", Value = 35f },
            new OUTL_StatEntry { Key = "MaxHealth", Value = 35f },
            new OUTL_StatEntry { Key = "MoveSpeed", Value = 2.2f },
            new OUTL_StatEntry { Key = "Fear", Value = 0.25f },
            new OUTL_StatEntry { Key = "Hunger", Value = 0.35f }
        };
        EditorUtility.SetDirty(def);
        return def;
    }

    private static OUTL_ActorShapeProfileDef EnsureSheepShapeProfile()
    {
        OUTL_ActorShapeProfileDef shape = EnsureAsset<OUTL_ActorShapeProfileDef>(ProfileFolder + "/OUTL_Shape_QuadrupedSmall_Sheep.asset");
        shape.ShapeId = "shape.quadruped.small.sheep";
        shape.Medium = OUTL_ActorMedium.Ground;
        shape.BodyLength = 1.1f;
        shape.BodyHeight = 0.72f;
        shape.BodyWidth = 0.46f;
        shape.CenterOffset = new Vector3(0f, 0.48f, 0f);
        shape.GroundOffset = 0f;
        shape.EyeHeight = 0.82f;
        shape.MovementRadius = 0.33f;
        shape.NavAgentHeight = 0.95f;
        shape.NavAgentRadius = 0.33f;
        shape.InteractionRadius = 1.15f;
        shape.HasCrouchPose = false;
        EditorUtility.SetDirty(shape);
        return shape;
    }

    private static OUTL_HurtboxProfileDef EnsureSheepHurtboxProfile()
    {
        OUTL_HurtboxProfileDef profile = EnsureAsset<OUTL_HurtboxProfileDef>(ProfileFolder + "/OUTL_Hurtbox_QuadrupedSmall_Sheep.asset");
        profile.ProfileId = "hurtbox.quadruped.small.sheep";
        profile.AllowProjectileHits = true;
        profile.AllowMeleeHits = true;
        profile.Hurtboxes = new[]
        {
            new OUTL_HurtboxProfileEntry
            {
                Id = "Body",
                Shape = OUTL_HurtboxShape.Box,
                Zone = OUTL_HitboxZone.Torso,
                Tags = OUTL_HurtboxTagFlags.Body,
                DamageMultiplier = 1f,
                LocalCenter = new Vector3(0f, 0.48f, 0f),
                BoxSize = new Vector3(0.48f, 0.55f, 1.05f)
            },
            new OUTL_HurtboxProfileEntry
            {
                Id = "Head",
                Shape = OUTL_HurtboxShape.Sphere,
                Zone = OUTL_HitboxZone.Head,
                Tags = OUTL_HurtboxTagFlags.Head | OUTL_HurtboxTagFlags.WeakPoint,
                DamageMultiplier = 1.8f,
                LocalCenter = new Vector3(0f, 0.78f, 0.58f),
                Radius = 0.18f
            },
            new OUTL_HurtboxProfileEntry
            {
                Id = "Legs",
                Shape = OUTL_HurtboxShape.Box,
                Zone = OUTL_HitboxZone.Leg,
                Tags = OUTL_HurtboxTagFlags.Leg,
                DamageMultiplier = 0.65f,
                LocalCenter = new Vector3(0f, 0.23f, 0f),
                BoxSize = new Vector3(0.42f, 0.32f, 0.88f)
            }
        };
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static OUTL_NPCNavigationProfile EnsureSheepNavigationProfile()
    {
        OUTL_NPCNavigationProfile nav = EnsureAsset<OUTL_NPCNavigationProfile>(ProfileFolder + "/OUTL_Nav_Sheep_Herbivore.asset");
        nav.ProfileId = "nav.sheep.herbivore";
        nav.WalkSpeed = 1.45f;
        nav.RunSpeed = 4.2f;
        nav.RoadPreference = 0.15f;
        nav.AvoidDangerWeight = 3.5f;
        nav.FactionTerritoryWeight = 0.5f;
        nav.CanUseAbstractTravel = true;
        nav.CanTeleportWhenDormant = false;
        nav.MaxPathRequestRate = 0.35f;
        nav.RepathCooldown = 2f;
        nav.StuckTimeout = 5f;
        nav.NavAgentRadius = 0.33f;
        nav.NavAgentHeight = 0.95f;
        nav.AbstractTravelSpeedMultiplier = 0.8f;
        nav.MaterializeTransformOnNear = true;
        nav.UpdateTransformWhileAbstract = true;
        EditorUtility.SetDirty(nav);
        return nav;
    }

    private static OUTL_NPCScheduleDef EnsureSheepSchedule()
    {
        OUTL_NPCScheduleDef schedule = EnsureAsset<OUTL_NPCScheduleDef>(ProfileFolder + "/OUTL_Schedule_Sheep_DayGrazeNightRest.asset");
        schedule.ScheduleId = "schedule.sheep.day_graze_night_rest";
        schedule.Entries = new[]
        {
            new OUTL_NPCScheduleEntry
            {
                EntryId = "night_rest",
                StartTimeNormalized = 0.00f,
                EndTimeNormalized = 0.22f,
                Action = OUTL_NPCScheduleActionType.Sleep,
                TargetMode = OUTL_NPCScheduleTargetMode.None,
                MinDuration = 6f,
                Priority = 0.8f
            },
            new OUTL_NPCScheduleEntry
            {
                EntryId = "day_graze",
                StartTimeNormalized = 0.22f,
                EndTimeNormalized = 0.78f,
                Action = OUTL_NPCScheduleActionType.Wander,
                TargetMode = OUTL_NPCScheduleTargetMode.TagQuery,
                RequiredTags = new[] { "Food", "Grass", "Resource" },
                MinDuration = 2f,
                Priority = 1f
            },
            new OUTL_NPCScheduleEntry
            {
                EntryId = "dusk_return_rest",
                StartTimeNormalized = 0.78f,
                EndTimeNormalized = 1.00f,
                Action = OUTL_NPCScheduleActionType.ReturnHome,
                TargetMode = OUTL_NPCScheduleTargetMode.None,
                MinDuration = 3f,
                Priority = 0.9f
            }
        };
        EditorUtility.SetDirty(schedule);
        return schedule;
    }

    private static OUTL_NPCBehaviorModel EnsureSheepBehaviorModel(OUTL_NPCScheduleDef schedule, OUTL_NPCNavigationProfile nav)
    {
        OUTL_NPCBehaviorModel model = EnsureAsset<OUTL_NPCBehaviorModel>(ProfileFolder + "/OUTL_NPCBehavior_Sheep_Herbivore.asset");
        model.ModelId = "npc_behavior.sheep.herbivore";
        model.Archetype = OUTL_NPCBehaviorArchetype.Wildlife;
        model.Schedule = schedule;
        model.NavigationProfile = nav;
        model.UseAIActorForNearTactics = true;
        model.ResumeScheduleAfterInterrupt = true;
        model.DayLengthSeconds = 1440f;
        model.StimulusRadius = 22f;
        model.StimulusMinimumPriority = 0.1f;
        model.StimulusBudget = 6;
        model.Role = "passive_herbivore";
        model.InterruptPolicies = new[]
        {
            new OUTL_NPCStimulusInterruptPolicy
            {
                StimulusType = OUTL_StimulusType.TookDamage,
                InterruptAction = OUTL_NPCScheduleActionType.Flee,
                MinimumPriority = 0.1f,
                MaxDuration = 6f,
                Cooldown = 0.5f
            },
            new OUTL_NPCStimulusInterruptPolicy
            {
                StimulusType = OUTL_StimulusType.Fear,
                InterruptAction = OUTL_NPCScheduleActionType.Flee,
                MinimumPriority = 0.15f,
                MaxDuration = 5f,
                Cooldown = 1f
            },
            new OUTL_NPCStimulusInterruptPolicy
            {
                StimulusType = OUTL_StimulusType.SightDanger,
                InterruptAction = OUTL_NPCScheduleActionType.Flee,
                MinimumPriority = 0.2f,
                MaxDuration = 8f,
                Cooldown = 1f
            },
            new OUTL_NPCStimulusInterruptPolicy
            {
                StimulusType = OUTL_StimulusType.HeardCombat,
                InterruptAction = OUTL_NPCScheduleActionType.Flee,
                MinimumPriority = 0.25f,
                MaxDuration = 4f,
                Cooldown = 2f
            },
            new OUTL_NPCStimulusInterruptPolicy
            {
                StimulusType = OUTL_StimulusType.SightFood,
                InterruptAction = OUTL_NPCScheduleActionType.Eat,
                MinimumPriority = 0.2f,
                MaxDuration = 3f,
                Cooldown = 4f
            }
        };
        EditorUtility.SetDirty(model);
        return model;
    }

    private static OUTL_AIProfile EnsureSheepAIProfile()
    {
        OUTL_AIProfile ai = EnsureAsset<OUTL_AIProfile>(ProfileFolder + "/OUTL_AI_Sheep_PassiveHerbivore.asset");
        ai.ProfileId = "ai.sheep.passive_herbivore";
        ai.UseFactionHostility = true;
        ai.EnemyTags = new[] { "Predator", "Hostile", "Monster", "PlayerThreat" };
        ai.FriendTags = new[] { "PassiveAnimal", "Sheep", "Herbivore" };
        ai.ViewDistance = 18f;
        ai.AttackDistance = 0f;
        ai.MoveSpeed = 2.2f;
        ai.ThinkIntervalNear = 0.15f;
        ai.ThinkIntervalMid = 0.75f;
        ai.ThinkIntervalFar = 4f;
        ai.LowHealthThreshold = 12f;
        ai.Rules = new OUTL_AIIntentRule[0];
        EditorUtility.SetDirty(ai);
        return ai;
    }

    private static OUTL_DriveProfileDef EnsureSheepDriveProfile()
    {
        OUTL_DriveProfileDef profile = EnsureAsset<OUTL_DriveProfileDef>(ProfileFolder + "/OUTL_Drive_Sheep_HerbivorePassive.asset");
        profile.ProfileId = "drive.sheep.herbivore_passive";
        profile.InitializeMissingDrives = false;
        profile.LocalSeedSalt = 12021;
        profile.Drives = new[]
        {
            Drive(OUTL_DriveId.Fear, 0.18f, 0.00f, 0.004f, 0f, 1f, 0.35f, "danger", "herbivore"),
            Drive(OUTL_DriveId.Hunger, 0.35f, 0.006f, 0.000f, 0f, 1f, 0.55f, "food"),
            Drive(OUTL_DriveId.Thirst, 0.20f, 0.003f, 0.000f, 0f, 1f, 0.65f, "water"),
            Drive(OUTL_DriveId.Fatigue, 0.20f, 0.002f, 0.001f, 0f, 1f, 0.70f, "rest"),
            Drive(OUTL_DriveId.SocialHerd, 0.55f, 0.002f, 0.000f, 0f, 1f, 0.45f, "herd", "social"),
            Drive(OUTL_DriveId.Comfort, 0.45f, 0.001f, 0.001f, 0f, 1f, 0.50f, "comfort"),
            Drive(OUTL_DriveId.ReproductionPressure, 0.10f, 0.001f, 0.000f, 0f, 1f, 0.75f, "reproduction", "seasonal"),
            Drive(OUTL_DriveId.PairBond, 0.15f, 0.001f, 0.000f, 0f, 1f, 0.55f, "bond"),
            Drive(OUTL_DriveId.OffspringProtection, 0.00f, 0.000f, 0.001f, 0f, 1f, 0.50f, "brood")
        };
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static OUTL_BehaviorActionSetDef EnsureSheepActionSet(OUTL_ItemDef dung, GameObject pickupPrefab)
    {
        OUTL_BehaviorActionDef idle = EnsureAction("OUTL_Action_Sheep_Idle.asset", "sheep.idle", OUTL_BehaviorActionId.Idle, OUTL_NPCScheduleActionType.Idle, 0.08f, null, null);
        OUTL_BehaviorActionDef wander = EnsureAction("OUTL_Action_Sheep_Wander.asset", "sheep.wander", OUTL_BehaviorActionId.Wander, OUTL_NPCScheduleActionType.Wander, 0.18f, null, null);
        OUTL_BehaviorActionDef flee = EnsureAction("OUTL_Action_Sheep_Flee.asset", "sheep.flee", OUTL_BehaviorActionId.FleeFromThreat, OUTL_NPCScheduleActionType.Flee, 0.05f,
            new[] { Weight(OUTL_DriveId.Fear, 3.2f), Weight(OUTL_DriveId.Pain, 2.2f), Weight(OUTL_DriveId.SocialHerd, 0.35f) },
            new[] { Stim(OUTL_StimulusType.TookDamage, 3f), Stim(OUTL_StimulusType.SightDanger, 2.4f), Stim(OUTL_StimulusType.HeardCombat, 1.4f), Stim(OUTL_StimulusType.Death, 1.6f), Stim(OUTL_StimulusType.Fear, 2f) });
        flee.LocalDangerWeight = 1.6f;
        flee.EgregoreWeights = new[] { Eg(OUTL_EgregoreCyclePhase.ShadowConfrontation, 0.9f), Eg(OUTL_EgregoreCyclePhase.Crisis, 1.2f), Eg(OUTL_EgregoreCyclePhase.Collapse, 1.4f) };
        flee.MinDuration = 1.4f;
        flee.Cooldown = 0.25f;
        flee.OnStartLivingEffects = new[]
        {
            LivingEffect(OUTL_LivingActionEffectType.FleeFromLastThreat, OUTL_DriveId.Fear, -0.05f, 1, 0f, null, OUTL_StimulusType.None, "sheep.flee", null, null, null, OUTL_EventType.Custom, OUTL_BehaviorModeId.Flee, 9f)
        };

        OUTL_BehaviorActionDef findFood = EnsureAction("OUTL_Action_Sheep_FindFood.asset", "sheep.find_food", OUTL_BehaviorActionId.FindFood, OUTL_NPCScheduleActionType.Wander, 0.05f,
            new[] { Weight(OUTL_DriveId.Hunger, 2.4f), Weight(OUTL_DriveId.Comfort, 0.2f), Weight(OUTL_DriveId.Fear, -1.2f) },
            new[] { Stim(OUTL_StimulusType.SightFood, 2f), Stim(OUTL_StimulusType.Resource, 1.2f) });
        findFood.RequiredDrive = OUTL_DriveId.Hunger;
        findFood.RequiredDriveMinimum = 0.35f;
        findFood.LocalDangerWeight = -1.0f;
        findFood.LocalSafetyWeight = 0.35f;
        findFood.EgregoreWeights = new[] { Eg(OUTL_EgregoreCyclePhase.Renewal, 0.5f), Eg(OUTL_EgregoreCyclePhase.Collapse, -0.8f) };
        findFood.OnStartLivingEffects = new[]
        {
            LivingEffect(OUTL_LivingActionEffectType.MoveToLastResource, OUTL_DriveId.Count, 0f, 1, 18f, "Food", OUTL_StimulusType.None, "sheep.find_food", null, null, null, OUTL_EventType.Custom, OUTL_BehaviorModeId.Work, 0f)
        };

        OUTL_BehaviorActionDef eat = EnsureAction("OUTL_Action_Sheep_Eat.asset", "sheep.eat", OUTL_BehaviorActionId.Eat, OUTL_NPCScheduleActionType.Eat, 0.04f,
            new[] { Weight(OUTL_DriveId.Hunger, 3f), Weight(OUTL_DriveId.Fear, -2f) },
            new[] { Stim(OUTL_StimulusType.SightFood, 3f), Stim(OUTL_StimulusType.Resource, 1.5f) });
        eat.RequiredDrive = OUTL_DriveId.Hunger;
        eat.RequiredDriveMinimum = 0.50f;
        eat.RequiresSafeArea = true;
        eat.MinDuration = 2f;
        eat.Cooldown = 1f;
        eat.OnStartLivingEffects = new[]
        {
            LivingEffect(OUTL_LivingActionEffectType.ConsumeNearbyResource, OUTL_DriveId.Count, 0f, 1, 1.8f, "Food", OUTL_StimulusType.None, "sheep.eat.consume", null, null, null, OUTL_EventType.Custom, OUTL_BehaviorModeId.Work, 0f),
            LivingEffect(OUTL_LivingActionEffectType.EmitStimulus, OUTL_DriveId.Count, 0f, 1, 5f, null, OUTL_StimulusType.Resource, "sheep.eat", new[] { "Sheep", "Food", "Grass" }, null, null, OUTL_EventType.Custom, OUTL_BehaviorModeId.Work, 0f)
        };

        OUTL_BehaviorActionDef followHerd = EnsureAction("OUTL_Action_Sheep_FollowHerd.asset", "sheep.follow_herd", OUTL_BehaviorActionId.FollowHerd, OUTL_NPCScheduleActionType.Wander, 0.06f,
            new[] { Weight(OUTL_DriveId.SocialHerd, 2.3f), Weight(OUTL_DriveId.Fear, 0.8f) },
            new[] { Stim(OUTL_StimulusType.Social, 1.2f), Stim(OUTL_StimulusType.SightAlly, 1.4f) });
        followHerd.MinDuration = 1.5f;

        OUTL_BehaviorActionDef rest = EnsureAction("OUTL_Action_Sheep_Rest.asset", "sheep.rest", OUTL_BehaviorActionId.Rest, OUTL_NPCScheduleActionType.Sleep, 0.03f,
            new[] { Weight(OUTL_DriveId.Fatigue, 2.2f), Weight(OUTL_DriveId.Fear, -1.2f) }, null);
        rest.RequiresSafeArea = true;
        rest.MinDuration = 3f;

        OUTL_BehaviorActionDef waste = EnsureAction("OUTL_Action_Sheep_WasteDrop.asset", "sheep.waste_drop", OUTL_BehaviorActionId.WasteDrop, OUTL_NPCScheduleActionType.Idle, 0.01f,
            new[] { Weight(OUTL_DriveId.Comfort, 0.5f), Weight(OUTL_DriveId.Fear, -1.0f) }, null);
        waste.RequiresSafeArea = true;
        waste.Cooldown = 90f;
        waste.OutputStimulus = OUTL_StimulusType.Smell;
        waste.OutputKey = "sheep.waste";
        waste.OutputStimulusPriority = 0.15f;
        waste.OnStartLivingEffects = new[]
        {
            LivingEffect(OUTL_LivingActionEffectType.SpawnDrop, OUTL_DriveId.Count, 0f, 1, 0.5f, null, OUTL_StimulusType.None, "sheep.waste.drop", new[] { "Sheep", "Dung", "Resource" }, pickupPrefab, dung, OUTL_EventType.ItemDropped, OUTL_BehaviorModeId.Work, 0f),
            LivingEffect(OUTL_LivingActionEffectType.EmitStimulus, OUTL_DriveId.Count, 0f, 1, 7f, null, OUTL_StimulusType.Smell, "sheep.waste.smell", new[] { "Sheep", "Dung", "Smell" }, null, null, OUTL_EventType.Custom, OUTL_BehaviorModeId.Work, 0f)
        };

        OUTL_BehaviorActionDef seekMate = EnsureAction("OUTL_Action_Sheep_SeekMate.asset", "sheep.seek_mate", OUTL_BehaviorActionId.SeekMate, OUTL_NPCScheduleActionType.Wander, 0.01f,
            new[] { Weight(OUTL_DriveId.ReproductionPressure, 2.6f), Weight(OUTL_DriveId.PairBond, 0.8f), Weight(OUTL_DriveId.Fear, -2.0f), Weight(OUTL_DriveId.Hunger, -0.8f) },
            new[] { Stim(OUTL_StimulusType.Social, 1.0f), Stim(OUTL_StimulusType.SightAlly, 0.8f) });
        seekMate.RequiredDrive = OUTL_DriveId.ReproductionPressure;
        seekMate.RequiredDriveMinimum = 0.70f;
        seekMate.RequiresSafeArea = true;
        seekMate.Cooldown = 20f;
        seekMate.EgregoreWeights = new[] { Eg(OUTL_EgregoreCyclePhase.Renewal, 0.65f), Eg(OUTL_EgregoreCyclePhase.Collapse, -1.0f), Eg(OUTL_EgregoreCyclePhase.Crisis, -1.0f) };
        seekMate.OnStartLivingEffects = new[]
        {
            LivingEffect(OUTL_LivingActionEffectType.EmitStimulus, OUTL_DriveId.Count, 0f, 1, 8f, null, OUTL_StimulusType.Social, "sheep.seek_mate", new[] { "Sheep", "Mate", "Herd" }, null, null, OUTL_EventType.Custom, OUTL_BehaviorModeId.Work, 0f)
        };

        OUTL_BehaviorActionDef reproduce = EnsureAction("OUTL_Action_Sheep_ReproduceAbstract.asset", "sheep.reproduce_abstract", OUTL_BehaviorActionId.ReproduceAbstract, OUTL_NPCScheduleActionType.Idle, 0.005f,
            new[] { Weight(OUTL_DriveId.ReproductionPressure, 3f), Weight(OUTL_DriveId.PairBond, 1.1f), Weight(OUTL_DriveId.Fear, -2.5f) }, null);
        reproduce.RequiredDrive = OUTL_DriveId.ReproductionPressure;
        reproduce.RequiredDriveMinimum = 0.85f;
        reproduce.RequiresSafeArea = true;
        reproduce.Cooldown = 300f;
        reproduce.OutputStimulus = OUTL_StimulusType.Social;
        reproduce.OutputKey = "sheep.reproduction";
        reproduce.OutputStimulusPriority = 0.25f;
        reproduce.EgregoreWeights = new[] { Eg(OUTL_EgregoreCyclePhase.Renewal, 0.8f), Eg(OUTL_EgregoreCyclePhase.CorruptionLoop, -0.7f), Eg(OUTL_EgregoreCyclePhase.Collapse, -1.2f) };
        reproduce.OnStartLivingEffects = new[]
        {
            LivingEffect(OUTL_LivingActionEffectType.RequestAbstractOffspring, OUTL_DriveId.ReproductionPressure, -1f, 1, 8f, null, OUTL_StimulusType.Social, "sheep.offspring.abstract", new[] { "Sheep", "Offspring", "Abstract" }, null, null, OUTL_EventType.Custom, OUTL_BehaviorModeId.Work, 0f)
        };

        OUTL_BehaviorActionSetDef set = EnsureAsset<OUTL_BehaviorActionSetDef>(ProfileFolder + "/OUTL_ActionSet_Sheep_Herbivore.asset");
        set.ActionSetId = "actionset.sheep.herbivore";
        set.FallbackAction = wander;
        set.SwitchHysteresis = 0.15f;
        set.SpeciesTags = new[] { "Sheep", "Herbivore", "PassiveAnimal" };
        set.Actions = new[] { idle, wander, flee, findFood, eat, followHerd, rest, waste, seekMate, reproduce };
        EditorUtility.SetDirty(set);
        return set;
    }

    private static OUTL_DriveTuning Drive(OUTL_DriveId id, float initial, float growth, float decay, float min, float max, float threshold, params string[] tags)
    {
        return new OUTL_DriveTuning { Drive = id, InitialValue = initial, GrowthPerSecond = growth, DecayPerSecond = decay, Minimum = min, Maximum = max, Threshold = threshold, Tags = tags };
    }

    private static OUTL_ActionDriveWeight Weight(OUTL_DriveId id, float weight)
    {
        return new OUTL_ActionDriveWeight { Drive = id, Weight = weight };
    }

    private static OUTL_ActionStimulusWeight Stim(OUTL_StimulusType type, float weight)
    {
        return new OUTL_ActionStimulusWeight { Stimulus = type, Weight = weight };
    }

    private static OUTL_ActionEgregoreWeight Eg(OUTL_EgregoreCyclePhase phase, float weight)
    {
        return new OUTL_ActionEgregoreWeight { Phase = phase, Weight = weight };
    }

    private static OUTL_LivingActionEffect LivingEffect(OUTL_LivingActionEffectType type, OUTL_DriveId drive, float floatValue, int intValue, float radius, string requiredTag, OUTL_StimulusType stimulus, string key, string[] tags, GameObject prefab, OUTL_ItemDef item, OUTL_EventType eventType, OUTL_BehaviorModeId behaviorMode, float moveDistance)
    {
        return new OUTL_LivingActionEffect
        {
            Type = type,
            Drive = drive,
            FloatValue = floatValue,
            IntValue = intValue,
            Radius = radius,
            RequiredTag = requiredTag,
            StimulusType = stimulus,
            Key = key,
            Tags = tags,
            Prefab = prefab,
            Item = item,
            EventType = eventType,
            BehaviorMode = behaviorMode,
            MoveDistance = moveDistance,
            Strength = 0.65f,
            Confidence = 1f,
            Priority = 0.25f,
            DecayTime = 3f
        };
    }

    private static OUTL_BehaviorActionDef EnsureAction(string fileName, string id, OUTL_BehaviorActionId type, OUTL_NPCScheduleActionType output, float baseWeight, OUTL_ActionDriveWeight[] driveWeights, OUTL_ActionStimulusWeight[] stimulusWeights)
    {
        OUTL_BehaviorActionDef action = EnsureAsset<OUTL_BehaviorActionDef>(ProfileFolder + "/" + fileName);
        action.ActionId = id;
        action.Type = type;
        action.OutputAction = output;
        action.BaseWeight = baseWeight;
        action.DriveWeights = driveWeights;
        action.StimulusWeights = stimulusWeights;
        action.Tags = new[] { "Sheep", "Herbivore", type.ToString() };
        action.RandomJitter = 0.025f;
        action.MinDuration = 0.4f;
        action.MaxDuration = 0f;
        action.SupportsAbstractMode = true;
        action.RequiresAdult = false;
        action.RequiresSafeArea = false;
        action.CannotRunInCombat = type != OUTL_BehaviorActionId.FleeFromThreat;
        action.OutputStimulus = OUTL_StimulusType.None;
        action.OnStartLivingEffects = null;
        EditorUtility.SetDirty(action);
        return action;
    }

    private static OUTL_ItemDef EnsureItem(string fileName, string className, string displayName, int maxStack, string[] tags)
    {
        OUTL_ItemDef item = EnsureAsset<OUTL_ItemDef>(ItemFolder + "/" + fileName);
        item.ClassName = className;
        item.DisplayName = displayName;
        item.Tags = tags;
        item.MaxStack = maxStack;
        item.Equippable = false;
        item.BaseStats = new OUTL_StatEntry[0];
        EditorUtility.SetDirty(item);
        return item;
    }

    private static OUTL_LootTableDef EnsureSheepLootTable(OUTL_ItemDef meat, OUTL_ItemDef wool, OUTL_ItemDef dung, GameObject pickupPrefab)
    {
        OUTL_LootTableDef table = EnsureAsset<OUTL_LootTableDef>(LootFolder + "/OUTL_Loot_Sheep_Basic.asset");
        table.TableId = "loot.sheep.basic";
        table.RollEachEntry = true;
        table.MaxDrops = 4;
        table.Entries = new[]
        {
            new OUTL_LootTableEntry { Label = "sheep.meat", Item = meat, PickupPrefab = pickupPrefab, Chance = 0.85f, Weight = 1f, MinCount = 1, MaxCount = 2, ScatterRadius = 0.45f, ContextTag = "food renewal" },
            new OUTL_LootTableEntry { Label = "sheep.wool", Item = wool, PickupPrefab = pickupPrefab, Chance = 0.95f, Weight = 1f, MinCount = 1, MaxCount = 3, ScatterRadius = 0.45f, ContextTag = "trade renewal" },
            new OUTL_LootTableEntry { Label = "sheep.dung", Item = dung, PickupPrefab = pickupPrefab, Chance = 0.35f, Weight = 0.5f, MinCount = 1, MaxCount = 1, ScatterRadius = 0.35f, ContextTag = "resource" }
        };
        EditorUtility.SetDirty(table);
        return table;
    }

    private static GameObject CreateSheepPickupPrefab(OUTL_FactionDef faction, OUTL_ItemDef defaultItem)
    {
        string path = PrefabFolder + "/OUTL_Nature_SheepLootPickup.prefab";
        OUTL_EntityDef def = EnsureAsset<OUTL_EntityDef>(DefFolder + "/OUTL_Def_Nature_SheepLootPickup.asset");
        def.ClassName = "pickup.nature.sheep_resource";
        def.DisplayName = "Sheep Resource Pickup";
        def.Tags = new[] { "Pickup", "Item", "Resource", "Sheep" };
        def.BaseStats = new OUTL_StatEntry[0];

        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "OUTL_Nature_SheepLootPickup";
        root.transform.localScale = Vector3.one * 0.25f;
        SphereCollider collider = root.GetComponent<SphereCollider>();
        if (collider != null) collider.isTrigger = true;
        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        OUTL_EntityAdapter entity = root.AddComponent<OUTL_EntityAdapter>();
        entity.Def = def;
        entity.Faction = faction;
        entity.ClassNameOverride = def.ClassName;
        entity.SavePersistent = false;
        entity.RegisterRandomTick = false;
        OUTL_ItemPickup pickup = root.AddComponent<OUTL_ItemPickup>();
        pickup.Entity = entity;
        pickup.Item = defaultItem;
        pickup.Count = 1;
        pickup.UseCommand = true;
        pickup.AutoDespawnOnPickup = true;
        pickup.PickupKey = "pickup.sheep_resource";

        GameObject prefab = SavePrefab(root, path);
        def.Prefab = prefab;
        EditorUtility.SetDirty(def);
        return prefab;
    }

    private static GameObject CreateSheepPrefab(OUTL_EntityDef def, OUTL_FactionDef faction, OUTL_ActorShapeProfileDef shape, OUTL_HurtboxProfileDef hurtboxes, OUTL_NPCNavigationProfile nav, OUTL_NPCBehaviorModel behavior, OUTL_AIProfile aiProfile, OUTL_DriveProfileDef driveProfile, OUTL_BehaviorActionSetDef actionSet, OUTL_LootTableDef loot, GameObject pickupPrefab)
    {
        string path = PrefabFolder + "/OUTL_Nature_BasicSheep.prefab";
        GameObject root = new GameObject("OUTL_Nature_BasicSheep");

        OUTL_EntityAdapter entity = root.AddComponent<OUTL_EntityAdapter>();
        entity.Def = def;
        entity.Faction = faction;
        entity.ClassNameOverride = def.ClassName;
        entity.TargetName = "";
        entity.Target = "";
        entity.KillTarget = "";
        entity.SavePersistent = true;
        entity.RegisterOnEnable = true;
        entity.RegisterTick = true;
        entity.RegisterInSectors = true;
        entity.IncludeChildCommandReceivers = true;
        entity.TickInterval = 0.25f;
        entity.Tier = OUTL_RuntimeTier.Full;

        OUTL_DamageReceiver receiver = root.AddComponent<OUTL_DamageReceiver>();
        receiver.Entity = entity;
        OUTL_Vitals vitals = root.AddComponent<OUTL_Vitals>();
        vitals.Entity = entity;
        vitals.InitializeMissingStats = true;
        vitals.DefaultHealth = 35f;
        vitals.DefaultMaxHealth = 35f;
        OUTL_DeathHandler death = root.AddComponent<OUTL_DeathHandler>();
        death.Entity = entity;
        death.DisableAI = true;
        death.DisableColliders = false;
        death.DisableRenderers = false;
        death.QueueDespawn = true;
        death.DespawnDelay = 6f;
        OUTL_DeathRuntime deathRuntime = root.AddComponent<OUTL_DeathRuntime>();
        deathRuntime.Entity = entity;

        OUTL_ActorShapeRuntime shapeRuntime = root.AddComponent<OUTL_ActorShapeRuntime>();
        shapeRuntime.Entity = entity;
        shapeRuntime.ShapeProfile = shape;
        shapeRuntime.HurtboxProfile = hurtboxes;
        shapeRuntime.AutoApplyHurtboxesOnAwake = false;

        NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
        agent.height = shape.NavAgentHeight;
        agent.radius = shape.NavAgentRadius;
        agent.speed = nav.WalkSpeed;
        agent.angularSpeed = 360f;
        agent.acceleration = 8f;
        agent.stoppingDistance = 1.15f;
        agent.updateRotation = true;

        OUTL_NavMeshMover mover = root.AddComponent<OUTL_NavMeshMover>();
        mover.Entity = entity;
        mover.Agent = agent;
        mover.FallbackSpeed = nav.WalkSpeed;
        mover.StopDistance = 1.15f;
        mover.RepathInterval = nav.MaxPathRequestRate;
        mover.TickInterval = 0.05f;
        mover.StableGroundFallback = true;
        mover.DisableGravityWhenNoGround = true;
        mover.GroundProbeRadius = 0.22f;
        mover.GroundProbeDistance = 0.5f;
        mover.GroundSnapDistance = 0.45f;

        OUTL_AIActor ai = root.AddComponent<OUTL_AIActor>();
        ai.Entity = entity;
        ai.Profile = aiProfile;
        ai.PerceptionProfile = AssetDatabase.LoadAssetAtPath<OUTL_AIPerceptionProfile>(GenericPerceptionPath);
        ai.StateTable = AssetDatabase.LoadAssetAtPath<OUTL_AIStateTable>(GenericStateTablePath);
        ai.NavMover = mover;
        ai.MoveRoot = root.transform;
        ai.UseActorInputContract = true;
        ai.UseAttackDriver = false;
        ai.UseNavMeshMover = true;
        ai.CreatureUsesFoodStimulus = true;
        ai.FleeFromDanger = true;
        ai.PreferRangedCombat = false;
        ai.MoveToTarget = true;
        ai.EyeHeight = shape.EyeHeight;
        ai.TargetEyeHeight = shape.EyeHeight;
        ai.StimulusPriorityThreshold = 0.1f;
        ai.StimulusForgetAfter = 10f;
        ai.CurrentMorale = 0.75f;

        OUTL_NPCBehaviorController npc = root.AddComponent<OUTL_NPCBehaviorController>();
        npc.Entity = entity;
        npc.AIActor = ai;
        npc.NavMover = mover;
        npc.Model = behavior;
        npc.NavigationProfileOverride = nav;
        npc.UseSharedRouteCache = true;
        npc.MaxLocalRoutes = 64;

        OUTL_LivingActionTargetMemory memory = root.AddComponent<OUTL_LivingActionTargetMemory>();
        memory.Entity = entity;
        memory.MemoryDuration = 14f;

        OUTL_DriveRuntime drives = root.AddComponent<OUTL_DriveRuntime>();
        drives.Entity = entity;
        drives.Behavior = npc;
        drives.TargetMemory = memory;
        drives.DriveProfile = driveProfile;
        drives.ActionSet = actionSet;
        drives.TickInterval = 0.5f;
        drives.ApplyActionToNPCBehavior = true;
        drives.EmitActionStimuli = true;
        drives.UseWorldLedgerEgregore = true;
        drives.StimulusMemoryRadius = 24f;
        drives.EatResourceRadius = 1.8f;
        drives.WanderRadius = 6f;
        drives.FleeDistance = 9f;

        OUTL_BotInputDriver bot = root.AddComponent<OUTL_BotInputDriver>();
        bot.Entity = entity;
        bot.AIActor = ai;
        bot.MoveRoot = root.transform;
        bot.AllowFire = false;
        bot.ProduceInputOnlyNearOrMid = true;
        bot.StopDistance = 1.1f;

        OUTL_NavMoverInputSink navSink = root.AddComponent<OUTL_NavMoverInputSink>();
        navSink.Entity = entity;
        navSink.NavMover = mover;
        navSink.MoveRoot = root.transform;
        navSink.InputStepDistance = 2.0f;
        navSink.CharacterControllerSpeed = nav.WalkSpeed;
        navSink.FaceAimYaw = false;

        OUTL_ActorControlBridge bridge = root.AddComponent<OUTL_ActorControlBridge>();
        bridge.Entity = entity;
        bridge.InputSourceBehaviour = bot;
        bridge.InputSinkBehaviours = new Behaviour[] { navSink };
        bridge.TickLane = OUTL_TickLane.AI;
        bridge.TickInterval = 0.08f;
        bridge.NearTickInterval = 0.12f;
        bridge.MidTickInterval = 0.50f;
        bridge.FarTickInterval = 3f;
        bridge.DormantTickInterval = 12f;
        bridge.LocalPlayerUpdateMode = OUTL_ActorInputUpdateMode.FullAndNearActors;
        bridge.ApplyLocalPlayerEveryFrame = false;
        bridge.ApplyNearActorsEveryFrame = true;
        bridge.UseUnityUpdateForLocalInput = false;

        OUTL_LootDropper dropper = root.AddComponent<OUTL_LootDropper>();
        dropper.Entity = entity;
        dropper.LootTable = loot;
        dropper.InventoryPickupPrefab = pickupPrefab;
        dropper.DropOnKilled = true;
        dropper.DropOnlyOnce = true;
        dropper.DropInventoryItems = false;

        AddSheepVisuals(root.transform);
        shapeRuntime.ApplyHurtboxProfile(true);
        shapeRuntime.ApplyShapeToNavAgent(agent);

        GameObject prefab = SavePrefab(root, path);
        def.Prefab = prefab;
        return prefab;
    }

    private static void AddSheepVisuals(Transform root)
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Visual_Body";
        body.transform.SetParent(root, false);
        body.transform.localPosition = new Vector3(0f, 0.48f, 0f);
        body.transform.localScale = new Vector3(0.48f, 0.55f, 1.05f);
        DestroyCollider(body.GetComponent<Collider>());

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Visual_Head";
        head.transform.SetParent(root, false);
        head.transform.localPosition = new Vector3(0f, 0.78f, 0.58f);
        head.transform.localScale = Vector3.one * 0.34f;
        DestroyCollider(head.GetComponent<Collider>());

        for (int i = 0; i < 4; i++)
        {
            float x = i < 2 ? -0.17f : 0.17f;
            float z = (i % 2 == 0) ? -0.32f : 0.32f;
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leg.name = "Visual_Leg_" + i;
            leg.transform.SetParent(root, false);
            leg.transform.localPosition = new Vector3(x, 0.20f, z);
            leg.transform.localScale = new Vector3(0.06f, 0.20f, 0.06f);
            DestroyCollider(leg.GetComponent<Collider>());
        }
    }

    private static void DestroyCollider(Collider collider)
    {
        if (collider != null) Object.DestroyImmediate(collider);
    }

    private static T EnsureAsset<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static GameObject SavePrefab(GameObject root, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }
}
#endif
