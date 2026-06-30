#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

public static class OUTL_CoreGameplaySkeletonSetupEditor
{
    private const string MenuPath = "OUT CORE Lite/Advanced/Samples/Create Full Gameplay Skeleton";
    private const string AddMenuRoot = "OUT CORE Lite/Add to Scene/Generic OUTL/";
    private const string RootName = "OUTL_CoreGameplaySkeleton";
    private const string RuntimeName = "OUTL_Runtime";
    private const string FoundationRoot = "Assets/OUT/OUT_Core/OUT_CORE_Lite/Templates/Foundation";
    private const string PrefabFolder = FoundationRoot + "/Prefabs";
    private const string DefFolder = FoundationRoot + "/Defs";
    private const string CombatFolder = FoundationRoot + "/Combat";
    private const string ItemFolder = FoundationRoot + "/Items";
    private const string LootFolder = FoundationRoot + "/Loot";
    private const string QuestFolder = FoundationRoot + "/Quests";
    private const string ProfileFolder = FoundationRoot + "/Core";
    private const string EgregoreFolder = FoundationRoot + "/Egregore";

    [MenuItem(MenuPath)]
    public static void CreateCoreGameplaySkeleton()
    {
        OUTL_AbstractPrefabGeneratorEditor.GenerateAllFoundationPrefabs();

        GameObject existing = GameObject.Find(RootName);
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Create OUTL Core Gameplay Skeleton");

        OUTL_TickProfile tickProfile = EnsureTickProfile();
        OUTL_World world = CreateRuntimeRoot(root.transform, tickProfile);
        CreateGround(root.transform);

        GameObject player = InstantiateFoundation("OUTL_Abstract_Actor_Controlled.prefab", "PlayerActor", new Vector3(0f, 0f, -5f), root.transform);
        GameObject enemy = InstantiateFoundation("OUTL_Abstract_NPC_Ranged.prefab", "EnemyNPC", new Vector3(0f, 0f, 6f), root.transform);
        GameObject friendly = InstantiateFoundation("OUTL_Abstract_NPC_Melee.prefab", "FriendlyNPC", new Vector3(-4f, 0f, 3f), root.transform);
        GameObject creature = InstantiateFoundation("OUTL_Abstract_Creature.prefab", "CreatureNPC", new Vector3(4f, 0f, 4f), root.transform);
        GameObject pickup = InstantiateFoundation("OUTL_Abstract_ItemPickup.prefab", "Pickup", new Vector3(-1.5f, 0.35f, -2.2f), root.transform);
        GameObject destructible = InstantiateFoundation("OUTL_Abstract_Object_Destructible.prefab", "DestructibleObject", new Vector3(2.5f, 0.5f, 1.5f), root.transform);
        GameObject chest = CreateContainer(root.transform);
        GameObject egregore = CreateLocalEgregore(root.transform, world.gameObject);
        OUTL_QuestDef quest = EnsureCoreSkeletonQuest();
        ConfigureRuntimeServices(world, quest);

        OUTL_FactionDef controlled = Load<OUTL_FactionDef>(DefFolder + "/OUTL_Faction_Controlled.asset");
        OUTL_FactionDef groupA = Load<OUTL_FactionDef>(DefFolder + "/OUTL_Faction_GroupA.asset");
        OUTL_FactionDef groupB = Load<OUTL_FactionDef>(DefFolder + "/OUTL_Faction_GroupB.asset");
        OUTL_FactionDef neutral = Load<OUTL_FactionDef>(DefFolder + "/OUTL_Faction_Neutral.asset");

        ConfigurePlacedEntity(player, "skeleton.player", "actor.controlled", controlled, true);
        ConfigurePlacedEntity(enemy, "skeleton.enemy", "actor.npc.ranged", groupA, true);
        ConfigurePlacedEntity(friendly, "skeleton.friendly", "actor.npc.melee", groupB, true);
        ConfigurePlacedEntity(creature, "skeleton.creature", "actor.creature", groupA, true);
        ConfigurePlacedEntity(pickup, "skeleton.pickup", "pickup.item", neutral, true);
        ConfigurePlacedEntity(destructible, "skeleton.destructible", "object.destructible", neutral, true);
        ConfigurePlacedEntity(chest, "skeleton.chest", "object.container", neutral, true);

        Face(enemy, player);
        Face(friendly, enemy);
        Face(creature, player);

        ConfigurePoolPrewarm(world.gameObject);
        string report = ValidateSkeleton(root, world, player, enemy, friendly, creature, pickup, destructible, chest, egregore, quest);
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        EditorSceneManager.MarkSceneDirty(root.scene);
        AssetDatabase.SaveAssets();
        Debug.Log(report, root);
    }

    [MenuItem(AddMenuRoot + "Player Actor")]
    public static void AddPlayerActor()
    {
        AddFoundationEntity("OUTL_Abstract_Actor_Controlled.prefab", "PlayerActor", "added.player", "actor.controlled", Load<OUTL_FactionDef>(DefFolder + "/OUTL_Faction_Controlled.asset"), 0f);
    }

    [MenuItem(AddMenuRoot + "Enemy NPC")]
    public static void AddEnemyNpc()
    {
        AddFoundationEntity("OUTL_Abstract_NPC_Ranged.prefab", "EnemyNPC", "added.enemy", "actor.npc.ranged", Load<OUTL_FactionDef>(DefFolder + "/OUTL_Faction_GroupA.asset"), 0f);
    }

    [MenuItem(AddMenuRoot + "Friendly NPC")]
    public static void AddFriendlyNpc()
    {
        AddFoundationEntity("OUTL_Abstract_NPC_Melee.prefab", "FriendlyNPC", "added.friendly", "actor.npc.melee", Load<OUTL_FactionDef>(DefFolder + "/OUTL_Faction_GroupB.asset"), 0f);
    }

    [MenuItem(AddMenuRoot + "Creature NPC")]
    public static void AddCreatureNpc()
    {
        AddFoundationEntity("OUTL_Abstract_Creature.prefab", "CreatureNPC", "added.creature", "actor.creature", Load<OUTL_FactionDef>(DefFolder + "/OUTL_Faction_GroupA.asset"), 0f);
    }

    [MenuItem(AddMenuRoot + "Turret")]
    public static void AddTurret()
    {
        AddFoundationEntity("OUTL_Abstract_Turret_Projectile.prefab", "Turret", "added.turret", "emitter.turret.projectile", Load<OUTL_FactionDef>(DefFolder + "/OUTL_Faction_GroupA.asset"), 0f);
    }

    [MenuItem(AddMenuRoot + "Pickup")]
    public static void AddPickup()
    {
        AddFoundationEntity("OUTL_Abstract_ItemPickup.prefab", "Pickup", "added.pickup", "pickup.item", Load<OUTL_FactionDef>(DefFolder + "/OUTL_Faction_Neutral.asset"), 0.35f);
    }

    [MenuItem(AddMenuRoot + "Door")]
    public static void AddDoor()
    {
        Transform parent = ResolveAddParent();
        GameObject door = CreateDoor(parent, "Door", ResolveAddPosition(1.3f), BuildUniqueStableId("added.door"));
        FinishAddedObject(door);
    }

    [MenuItem(AddMenuRoot + "Chest")]
    public static void AddChest()
    {
        OUTL_AbstractPrefabGeneratorEditor.GenerateAllFoundationPrefabs();
        Transform parent = ResolveAddParent();
        GameObject chest = CreateContainer(parent);
        if (chest == null) return;
        chest.name = "ChestContainer";
        chest.transform.position = ResolveAddPosition(0.5f);
        ConfigurePlacedEntity(chest, BuildUniqueStableId("added.chest"), "object.container", Load<OUTL_FactionDef>(DefFolder + "/OUTL_Faction_Neutral.asset"), true);
        FinishAddedObject(chest);
    }

    [MenuItem(AddMenuRoot + "Local Egregore")]
    public static void AddLocalEgregore()
    {
        Transform parent = ResolveAddParent();
        OUTL_World world = Object.FindObjectOfType<OUTL_World>(true);
        GameObject egregore = CreateLocalEgregore(parent, world != null ? world.gameObject : null);
        OUTL_EntityAdapter entity = egregore != null ? egregore.GetComponent<OUTL_EntityAdapter>() : null;
        if (entity != null)
        {
            string stable = BuildUniqueStableId("added.egregore.local");
            entity.TargetName = stable;
            entity.StableId = stable;
        }
        if (egregore != null) egregore.transform.position = ResolveAddPosition(0f);
        FinishAddedObject(egregore);
    }

    private static OUTL_World CreateRuntimeRoot(Transform parent, OUTL_TickProfile tickProfile)
    {
        GameObject runtime = new GameObject(RuntimeName);
        Undo.RegisterCreatedObjectUndo(runtime, "Create OUTL Runtime");
        runtime.transform.SetParent(parent, false);

        OUTL_World world = Undo.AddComponent<OUTL_World>(runtime);
        world.TickProfile = tickProfile;
        world.ApplyTickProfileOnAwake = true;
        world.UpdateMode = OUTL_WorldUpdateMode.CustomFixedStep;
        world.SimulationStep = 0.05f;
        world.MaxSimulationStepsPerFrame = 4;
        world.AutoFindAdaptersOnStart = true;

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

    private static GameObject CreateLocalEgregore(Transform parent, GameObject runtime)
    {
        OUTL_LocalEgregoreDef def = EnsureLocalEgregoreDef();
        GameObject go = new GameObject("LocalEgregore");
        Undo.RegisterCreatedObjectUndo(go, "Create OUTL Local Egregore");
        go.transform.SetParent(parent, false);
        go.transform.position = Vector3.zero;

        OUTL_EntityAdapter entity = Undo.AddComponent<OUTL_EntityAdapter>(go);
        entity.ClassNameOverride = "egregore.local";
        entity.TargetName = "skeleton.egregore.local";
        entity.StableId = "skeleton.egregore.local";
        entity.SavePersistent = true;
        entity.RegisterOnEnable = true;
        entity.RegisterTick = false;
        entity.RegisterInSectors = true;

        OUTL_EgregoreComponent component = Undo.AddComponent<OUTL_EgregoreComponent>(go);
        component.Def = def;
        component.AutoRegister = true;
        component.UseDefScope = true;
        component.UseDefScale = true;

        OUTL_EgregoreDebugView debug = runtime != null ? runtime.GetComponent<OUTL_EgregoreDebugView>() : null;
        if (runtime != null && debug == null) debug = Undo.AddComponent<OUTL_EgregoreDebugView>(runtime);
        if (debug != null)
        {
            debug.Sources = new[] { component };
            debug.Show = false;
        }

        EditorUtility.SetDirty(go);
        return go;
    }

    private static GameObject CreateContainer(Transform parent)
    {
        GameObject go = InstantiateFoundation("OUTL_Abstract_Object_Interactable.prefab", "ChestContainer", new Vector3(-3.2f, 0.5f, 0.5f), parent);
        if (go == null) return null;
        OUTL_EntityAdapter entity = go.GetComponent<OUTL_EntityAdapter>();
        OUTL_ContainerRuntime container = go.GetComponent<OUTL_ContainerRuntime>();
        if (container == null) container = Undo.AddComponent<OUTL_ContainerRuntime>(go);
        container.Entity = entity;
        container.Def = EnsureContainerDef();
        container.IsLocked = false;
        container.RolledSeed = container.Def != null && container.Def.Seed != 0 ? container.Def.Seed : 771337;
        OUTL_ChestInteractable interactable = go.GetComponent<OUTL_ChestInteractable>();
        if (interactable == null) interactable = Undo.AddComponent<OUTL_ChestInteractable>(go);
        interactable.Container = container;
        EditorUtility.SetDirty(go);
        return go;
    }

    private static GameObject CreateDoor(Transform parent, string name, Vector3 position, string stableId)
    {
        OUTL_EntityDef def = EnsureDoorDef();
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(go, "Create OUTL Door");
        go.name = name;
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = new Vector3(1.8f, 2.6f, 0.28f);
        SetPreviewColor(go, new Color(1f, 0.55f, 0.12f, 1f));

        OUTL_EntityAdapter entity = Undo.AddComponent<OUTL_EntityAdapter>(go);
        entity.Def = def;
        ConfigurePlacedEntity(go, stableId, "object.door", Load<OUTL_FactionDef>(DefFolder + "/OUTL_Faction_Neutral.asset"), true);

        OUTL_Door door = Undo.AddComponent<OUTL_Door>(go);
        door.Entity = entity;
        door.DoorRoot = go.transform;
        door.ClosedLocalPosition = go.transform.localPosition;
        door.OpenLocalPosition = go.transform.localPosition + Vector3.up * 2.5f;
        door.ToggleMode = false;
        door.AutoClose = false;
        door.CheckBlockers = false;

        OUTL_Interactable interactable = Undo.AddComponent<OUTL_Interactable>(go);
        interactable.Entity = entity;
        interactable.DisplayNameKey = "object.door.name";
        interactable.DisplayName = "Door";
        interactable.DescriptionEn = "Open/close door";
        interactable.DescriptionRu = "Открыть/закрыть дверь";
        interactable.Command = OUTL_CommandType.Use;

        entity.RebuildCommandReceiverCache();
        EditorUtility.SetDirty(entity);
        EditorUtility.SetDirty(door);
        EditorUtility.SetDirty(interactable);
        EditorUtility.SetDirty(go);
        return go;
    }

    private static void ConfigureRuntimeServices(OUTL_World world, OUTL_QuestDef quest)
    {
        if (world == null) return;
        world.EnableAutomaticMaterialization = true;
        world.MaterializationFocusTargetName = "skeleton.player";
        world.MaterializationTickInterval = 0.5f;
        world.MaterializationBudgetPerTick = 4;
        world.MaterializeEnterDistance = 36f;
        world.DematerializeExitDistance = 64f;
        world.DayLengthSeconds = 240f;
        world.AbstractEncounterTickInterval = 2f;
        world.MaxAbstractEncountersPerTick = 4;
        world.AbstractEncounterDangerThreshold = 0.45f;

        OUTL_SaveSpawnResolverRegistry resolver = world.GetComponent<OUTL_SaveSpawnResolverRegistry>();
        if (resolver == null) resolver = Undo.AddComponent<OUTL_SaveSpawnResolverRegistry>(world.gameObject);
        resolver.RequireRestoreSpawnIfMissingFlag = true;
        resolver.EntityDefs = new[]
        {
            Load<OUTL_EntityDef>(DefFolder + "/OUTL_Def_Actor_Controlled.asset"),
            Load<OUTL_EntityDef>(DefFolder + "/OUTL_Def_NPC_Ranged.asset"),
            Load<OUTL_EntityDef>(DefFolder + "/OUTL_Def_NPC_Melee.asset"),
            Load<OUTL_EntityDef>(DefFolder + "/OUTL_Def_Creature.asset"),
            Load<OUTL_EntityDef>(DefFolder + "/OUTL_Def_ItemPickup.asset"),
            Load<OUTL_EntityDef>(DefFolder + "/OUTL_Def_Object_Destructible.asset"),
            Load<OUTL_EntityDef>(DefFolder + "/OUTL_Def_Object_Interactable.asset"),
            Load<OUTL_EntityDef>(DefFolder + "/OUTL_Def_Turret_Projectile.asset")
        };

        OUTL_QuestBootstrap bootstrap = world.GetComponent<OUTL_QuestBootstrap>();
        if (bootstrap == null) bootstrap = Undo.AddComponent<OUTL_QuestBootstrap>(world.gameObject);
        bootstrap.Quests = quest != null ? new[] { quest } : new OUTL_QuestDef[0];
        EditorUtility.SetDirty(world);
    }

    private static OUTL_ContainerDef EnsureContainerDef()
    {
        EnsureFolder(LootFolder);
        string path = LootFolder + "/OUTL_Container_CoreSkeleton.asset";
        OUTL_ContainerDef def = AssetDatabase.LoadAssetAtPath<OUTL_ContainerDef>(path);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<OUTL_ContainerDef>();
            AssetDatabase.CreateAsset(def, path);
        }

        def.ContainerId = "skeleton.container";
        def.StartsLocked = false;
        def.Seed = 771337;
        def.OpenKey = "skeleton.container.open";
        def.LootKey = "skeleton.container.loot";
        def.LootTable = Load<OUTL_LootTableDef>(LootFolder + "/OUTL_Loot_Generic_Drop.asset");
        EditorUtility.SetDirty(def);
        return def;
    }

    private static OUTL_EntityDef EnsureDoorDef()
    {
        EnsureFolder(DefFolder);
        string path = DefFolder + "/OUTL_Def_Object_Door.asset";
        OUTL_EntityDef def = AssetDatabase.LoadAssetAtPath<OUTL_EntityDef>(path);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<OUTL_EntityDef>();
            AssetDatabase.CreateAsset(def, path);
        }

        def.ClassName = "object.door";
        def.DisplayName = "Door";
        def.Tags = new[] { "Entity", "Object", "Door", "Interactable", "Puzzle" };
        def.BaseStats = new[]
        {
            new OUTL_StatEntry { Key = "Health", Value = 100f },
            new OUTL_StatEntry { Key = "Damage", Value = 0f },
            new OUTL_StatEntry { Key = "Speed", Value = 0f },
            new OUTL_StatEntry { Key = "Armor", Value = 0f }
        };
        EditorUtility.SetDirty(def);
        return def;
    }

    private static void CreateGround(Transform parent)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(ground, "Create OUTL Skeleton Ground");
        ground.name = "Ground";
        ground.transform.SetParent(parent, false);
        ground.transform.position = new Vector3(0f, -0.05f, 0f);
        ground.transform.localScale = new Vector3(18f, 0.1f, 18f);
        BoxCollider collider = ground.GetComponent<BoxCollider>();
        if (collider == null) collider = Undo.AddComponent<BoxCollider>(ground);
        collider.isTrigger = false;
        TryAttachNavMeshSurface(ground);
    }

    private static void TryAttachNavMeshSurface(GameObject ground)
    {
        if (ground == null) return;
        System.Type surfaceType = System.Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
        if (surfaceType == null) surfaceType = System.Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation.Runtime");
        if (surfaceType == null) return;
        if (ground.GetComponent(surfaceType) != null) return;

        Component surface = Undo.AddComponent(ground, surfaceType);
        if (surface == null) return;
        System.Reflection.MethodInfo build = surfaceType.GetMethod("BuildNavMesh", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (build == null) return;
        try { build.Invoke(surface, null); }
        catch (System.Exception e) { Debug.LogWarning("OUTL skeleton could not build optional NavMeshSurface: " + e.Message, ground); }
    }

    private static void SetPreviewColor(GameObject go, Color color)
    {
        Renderer renderer = go != null ? go.GetComponent<Renderer>() : null;
        if (renderer == null) return;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return;

        Material material = new Material(shader);
        material.name = go.name + "_PreviewMaterial";
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        renderer.sharedMaterial = material;
    }

    private static GameObject InstantiateFoundation(string prefabName, string instanceName, Vector3 position, Transform parent)
    {
        GameObject prefab = Load<GameObject>(PrefabFolder + "/" + prefabName);
        if (prefab == null)
        {
            Debug.LogError("OUTL skeleton setup missing Foundation prefab: " + prefabName);
            return null;
        }

        GameObject go = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (go == null) return null;
        Undo.RegisterCreatedObjectUndo(go, "Create " + instanceName);
        go.name = instanceName;
        go.transform.position = position;
        return go;
    }

    private static void ConfigurePlacedEntity(GameObject go, string stableId, string className, OUTL_FactionDef faction, bool persistent)
    {
        if (go == null) return;
        OUTL_EntityAdapter entity = go.GetComponent<OUTL_EntityAdapter>();
        if (entity == null) return;
        entity.Faction = faction;
        entity.ClassNameOverride = className;
        entity.TargetName = stableId;
        entity.Target = "";
        entity.KillTarget = "";
        entity.StableId = stableId;
        entity.SavePersistent = persistent;
        entity.RestoreSpawnIfMissing = persistent;
        entity.RegisterOnEnable = true;
        entity.RegisterTick = true;
        entity.RegisterInSectors = true;
        entity.IncludeChildCommandReceivers = true;
        entity.RebuildCommandReceiverCache();

        OUTL_Vitals vitals = go.GetComponent<OUTL_Vitals>();
        if (vitals != null) vitals.Entity = entity;
        OUTL_DamageReceiver receiver = go.GetComponent<OUTL_DamageReceiver>();
        if (receiver != null) receiver.Entity = entity;
        OUTL_DeathRuntime deathRuntime = go.GetComponent<OUTL_DeathRuntime>();
        if (deathRuntime != null) deathRuntime.Entity = entity;
        OUTL_DeathHandler deathHandler = go.GetComponent<OUTL_DeathHandler>();
        if (deathHandler != null) deathHandler.Entity = entity;
    }

    private static void ConfigurePoolPrewarm(GameObject runtime)
    {
        OUTL_PoolPrewarmPlan prewarm = Undo.AddComponent<OUTL_PoolPrewarmPlan>(runtime);
        prewarm.Entries = new[]
        {
            new OUTL_PoolPrewarmEntry { Prefab = Load<GameObject>(PrefabFolder + "/OUTL_Abstract_Projectile.prefab"), Count = 12 },
            new OUTL_PoolPrewarmEntry { Prefab = Load<GameObject>(PrefabFolder + "/OUTL_Abstract_ItemPickup.prefab"), Count = 4 },
            new OUTL_PoolPrewarmEntry { Prefab = Load<GameObject>(PrefabFolder + "/OUTL_Abstract_NPC_Ranged.prefab"), Count = 2 },
            new OUTL_PoolPrewarmEntry { Prefab = Load<GameObject>(PrefabFolder + "/OUTL_Abstract_Creature.prefab"), Count = 2 }
        };
    }

    private static OUTL_TickProfile EnsureTickProfile()
    {
        EnsureFolder(ProfileFolder);
        string path = ProfileFolder + "/OUTL_TickProfile_CoreSkeleton.asset";
        OUTL_TickProfile profile = AssetDatabase.LoadAssetAtPath<OUTL_TickProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<OUTL_TickProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        profile.logicInterval = 0.10f;
        profile.aiNearInterval = 0.12f;
        profile.aiMidInterval = 0.45f;
        profile.aiFarInterval = 1.50f;
        profile.aiDormantInterval = 3.00f;
        profile.npcFullInterval = 0.05f;
        profile.npcNearInterval = 0.25f;
        profile.npcMidInterval = 2.00f;
        profile.npcFarInterval = 10.00f;
        profile.npcDormantInterval = 60.00f;
        profile.maxAITicksPerFrame = 64;
        profile.maxNpcBehaviorTicksPerFrame = 64;
        profile.maxNpcRouteUpdatesPerFrame = 32;
        profile.maxNpcPathRequestsPerFrame = 8;
        profile.maxNpcStimulusInterruptsPerFrame = 32;
        profile.Sanitize();
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static OUTL_QuestDef EnsureCoreSkeletonQuest()
    {
        EnsureFolder(QuestFolder);
        string path = QuestFolder + "/OUTL_Quest_CoreSkeleton.asset";
        OUTL_QuestDef quest = AssetDatabase.LoadAssetAtPath<OUTL_QuestDef>(path);
        if (quest == null)
        {
            quest = ScriptableObject.CreateInstance<OUTL_QuestDef>();
            AssetDatabase.CreateAsset(quest, path);
        }

        quest.QuestId = "skeleton.core_loop";
        quest.DisplayName = "Core Loop";
        quest.DebugName = "Core Skeleton Loop";
        quest.ArchetypalHook = OUTL_EgregoreQuestHook.IntegrationQuest;
        quest.CompletedStage = 100;
        quest.FailedStage = -1;
        quest.Tags = new[] { "skeleton", "core" };
        quest.Objectives = new[]
        {
            new OUTL_QuestObjectiveDef { ObjectiveId = "kill_actor", Type = OUTL_QuestObjectiveType.Kill, RequiredCount = 1 },
            new OUTL_QuestObjectiveDef { ObjectiveId = "collect_item", Type = OUTL_QuestObjectiveType.Collect, RequiredCount = 1 },
            new OUTL_QuestObjectiveDef { ObjectiveId = "open_container", Type = OUTL_QuestObjectiveType.OpenChest, RequiredCount = 1 }
        };
        quest.Stages = new[]
        {
            new OUTL_QuestStageDef { Stage = 1, Description = "Core loop active" },
            new OUTL_QuestStageDef { Stage = 100, Description = "Core loop complete", CompletesQuest = true, ArchetypalHook = OUTL_EgregoreQuestHook.IntegrationQuest },
            new OUTL_QuestStageDef { Stage = -1, Description = "Core loop failed", FailsQuest = true, ArchetypalHook = OUTL_EgregoreQuestHook.ShadowQuest }
        };
        EditorUtility.SetDirty(quest);
        return quest;
    }

    private static OUTL_LocalEgregoreDef EnsureLocalEgregoreDef()
    {
        EnsureFolder(EgregoreFolder);
        string path = EgregoreFolder + "/OUTL_LocalEgregore_CoreSkeleton.asset";
        OUTL_LocalEgregoreDef def = AssetDatabase.LoadAssetAtPath<OUTL_LocalEgregoreDef>(path);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<OUTL_LocalEgregoreDef>();
            AssetDatabase.CreateAsset(def, path);
        }

        def.EgregoreId = "skeleton.local_egregore";
        def.DisplayName = "Local Egregore";
        def.DebugName = "Local Egregore";
        def.Scope = OUTL_EgregoreScope.Local;
        def.PlaceArchetype = OUTL_LocalEgregoreArchetype.Custom;
        def.UpdateInterval = 2.0f;
        def.InfluenceRadius = 48f;
        def.ViolenceWeight = 0.35f;
        def.FearWeight = 0.30f;
        def.ProsperityWeight = 0.25f;
        def.AlertnessWeight = 0.25f;
        def.HostilityWeight = 0.55f;
        def.ThresholdOpenTension = 0.45f;
        def.CrisisTension = 0.65f;
        def.RenewalThreshold = 0.70f;
        def.CollapseThreshold = 0.85f;
        if (def.ArchetypalCycle == null) def.ArchetypalCycle = new OUTL_EgregoreArchetypalCycle();
        def.ArchetypalCycle.InitialPhase = OUTL_EgregoreCyclePhase.StableWorld;
        def.ArchetypalCycle.MaxMemoryTraces = 32;
        def.ArchetypalCycle.MemoryDecayBudget = 8;
        def.InitialArchetypePressures = new[]
        {
            new OUTL_EgregoreArchetypePressure { Archetype = OUTL_EgregoreArchetypeId.SelfCenter, Pressure = 0.35f, DecayRate = 0.01f },
            new OUTL_EgregoreArchetypePressure { Archetype = OUTL_EgregoreArchetypeId.Shadow, Pressure = 0.05f, DecayRate = 0.01f }
        };
        def.ShadowRules = new[]
        {
            new OUTL_EgregoreShadowRule { TraceType = OUTL_EgregoreTraceType.Death, ShadowArchetype = OUTL_EgregoreArchetypeId.VoidDeathRebirth, Pressure = 0.6f, Trauma = 0.2f, Corruption = 0.05f },
            new OUTL_EgregoreShadowRule { TraceType = OUTL_EgregoreTraceType.Theft, ShadowArchetype = OUTL_EgregoreArchetypeId.Trickster, Pressure = 0.35f, Trauma = 0.05f, Corruption = 0.04f }
        };
        def.IntegrationRules = new[]
        {
            new OUTL_EgregoreIntegrationRule { Hook = OUTL_EgregoreQuestHook.IntegrationQuest, Archetype = OUTL_EgregoreArchetypeId.SelfCenter, Integration = 0.35f, Renewal = 0.25f, CorruptionRelief = 0.20f },
            new OUTL_EgregoreIntegrationRule { Hook = OUTL_EgregoreQuestHook.BoonQuest, Archetype = OUTL_EgregoreArchetypeId.Hero, Integration = 0.20f, Renewal = 0.15f, CorruptionRelief = 0.10f }
        };
        def.OutputRules = new[]
        {
            new OUTL_EgregoreOutputRule { Phase = OUTL_EgregoreCyclePhase.Threshold, Signal = OUTL_EgregoreSignalType.OpenThreshold, MinIntensity = 0.35f, Key = "threshold" },
            new OUTL_EgregoreOutputRule { Phase = OUTL_EgregoreCyclePhase.CorruptionLoop, Signal = OUTL_EgregoreSignalType.CollapseWarning, MinIntensity = 0.50f, Key = "corruption" },
            new OUTL_EgregoreOutputRule { Phase = OUTL_EgregoreCyclePhase.Renewal, Signal = OUTL_EgregoreSignalType.RenewalPulse, MinIntensity = 0.50f, Key = "renewal" }
        };
        def.ArchetypalCycle.Sanitize();
        EditorUtility.SetDirty(def);
        return def;
    }

    private static string ValidateSkeleton(GameObject root, OUTL_World world, GameObject player, GameObject enemy, GameObject friendly, GameObject creature, GameObject pickup, GameObject destructible, GameObject chest, GameObject egregore, OUTL_QuestDef quest)
    {
        int errors = 0;
        int warnings = 0;
        StringBuilder sb = new StringBuilder(2048);
        sb.AppendLine("OUTL Core Gameplay Skeleton setup report");

        Check(world != null, "OUTL_World root exists", ref errors, sb);
        Check(world != null && world.TickProfile != null, "TickProfile assigned", ref errors, sb);
        Check(world != null && world.GetComponent<OUTL_PoolSystem>() != null, "OUTL_PoolSystem exists", ref errors, sb);
        Check(world != null && world.GetComponent<OUTL_PoolPrewarmPlan>() != null, "Pool prewarm plan exists", ref errors, sb);
        OUTL_DevConsole console = world != null ? world.GetComponent<OUTL_DevConsole>() : null;
        Check(console != null, "Dev console exists on runtime root", ref errors, sb);
        Check(console != null && console.AlternateToggleKeys != null && console.AlternateToggleKeys.Length > 0, "Dev console has alternate toggle keys and SV_ command host", ref errors, sb);

        BoxCollider ground = root != null ? root.GetComponentInChildren<BoxCollider>() : null;
        Check(ground != null && !ground.isTrigger, "Ground has non-trigger BoxCollider", ref errors, sb);

        ValidatePlayer(player, ref errors, sb);
        ValidateNpc(enemy, "EnemyNPC", false, ref errors, sb);
        ValidateNpc(friendly, "FriendlyNPC", false, ref errors, sb);
        ValidateNpc(creature, "CreatureNPC", true, ref errors, sb);
        ValidatePickup(pickup, player, ref errors, sb);
        ValidateDestructible(destructible, ref errors, sb);
        ValidateContainer(chest, ref errors, sb);
        ValidateQuest(quest, world, ref errors, sb);
        ValidateSaveMaterialization(world, enemy, ref errors, sb);
        ValidateProjectilePrefab(ref errors, sb);
        ValidateLocalEgregore(egregore, world, ref errors, sb);

        MonoBehaviour[] behaviours = root != null ? root.GetComponentsInChildren<MonoBehaviour>(true) : new MonoBehaviour[0];
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour b = behaviours[i];
            if (b == null) continue;
            if (b is OUTL_BasicPlayerController || b is OUTL_BasicHUD || b is OUTL_UIDataBinder || b is OUTL_UICollectionBinder)
            {
                errors++;
                sb.AppendLine("ERROR legacy component in skeleton: " + b.GetType().Name + " on " + b.name);
            }
        }

        OUTL_NPCBehaviorBudgetSnapshot budget = OUTL_NPCBehaviorDispatcher.LastSnapshot;
        sb.AppendLine("NPC dispatcher counters available: registered=" + OUTL_NPCBehaviorDispatcher.RegisteredCount + " lastTicked=" + budget.TickedThisFrame + " lastSkippedBudget=" + budget.SkippedByBudget);

        if (warnings > 0) sb.AppendLine("Warnings: " + warnings);
        sb.AppendLine(errors == 0 ? "OK: skeleton is structurally ready. Press Play." : "FAILED: fix errors above before Play.");
        return sb.ToString();
    }

    private static void ValidatePlayer(GameObject go, ref int errors, StringBuilder sb)
    {
        Check(go != null, "PlayerActor exists", ref errors, sb);
        if (go == null) return;
        Check(go.GetComponent<OUTL_EntityAdapter>() != null, "Player has EntityAdapter", ref errors, sb);
        Check(go.GetComponent<OUTL_Vitals>() != null, "Player has Vitals", ref errors, sb);
        Check(go.GetComponent<OUTL_DamageReceiver>() != null, "Player has DamageReceiver", ref errors, sb);
        Check(go.GetComponent<OUTL_DeathRuntime>() != null && go.GetComponent<OUTL_DeathHandler>() != null, "Player has death runtime/handler", ref errors, sb);
        Check(go.GetComponent<OUTL_PlayerInputSource>() != null, "Player has PlayerInputSource", ref errors, sb);
        Check(go.GetComponent<OUTL_ActorControlBridge>() != null, "Player has ActorControlBridge", ref errors, sb);
        Check(go.GetComponent<OUTL_CharacterControllerInputSink>() != null, "Player has CharacterControllerInputSink", ref errors, sb);
        Check(go.GetComponent<OUTL_AttackDriverInputSink>() != null, "Player has AttackDriverInputSink", ref errors, sb);
        Check(go.GetComponent<OUTL_InteractionInputSink>() != null, "Player has InteractionInputSink for Use/Pickup", ref errors, sb);
        Check(go.GetComponent<OUTL_InventoryRuntime>() != null && go.GetComponent<OUTL_EquipmentRuntime>() != null, "Player has inventory/equipment", ref errors, sb);
        Check(IsAboveGround(go), "Player is above ground", ref errors, sb);
        OUTL_PlayerInputSource input = go.GetComponent<OUTL_PlayerInputSource>();
        Check(input != null && input.ViewCamera != null, "Player input has ViewCamera", ref errors, sb);
        Check(input != null && !string.IsNullOrEmpty(input.HorizontalAxis) && !string.IsNullOrEmpty(input.VerticalAxis) && !string.IsNullOrEmpty(input.MouseXAxis) && !string.IsNullOrEmpty(input.MouseYAxis), "Player input axes are assigned", ref errors, sb);
        Check(input != null && input.LockCursorOnStart, "Player locks cursor for mouse look", ref errors, sb);
        OUTL_CharacterControllerInputSink movement = go.GetComponent<OUTL_CharacterControllerInputSink>();
        Check(movement != null && movement.ViewPitchRoot != null, "Player has ViewPitchRoot for mouse pitch", ref errors, sb);
        Check(movement != null && movement.UseGoldSrcUnits && movement.GroundAcceleration > 0f && movement.AirAcceleration > 0f && movement.AirWishSpeedCap > 0f && movement.SkipFrictionOnJumpFrame, "Player motor has GoldSrc acceleration/air-strafe/bunnyhop fields", ref errors, sb);
        Check(movement != null && movement.Gravity > 0f && movement.GravityMultiplier > 0f && !movement.AllowFallbackGravityWhenSvGravityZero, "Player motor uses sv_gravity as primary gravity source", ref errors, sb);
        Check(movement != null && movement.StandingHeight > movement.CrouchHeight && movement.CrouchViewHeight < movement.StandingViewHeight && movement.CrouchNoiseMultiplier < 1f, "Player crouch changes hull/view/noise", ref errors, sb);
        OUTL_ActorControlBridge bridge = go.GetComponent<OUTL_ActorControlBridge>();
        Check(bridge != null && bridge.UseUnityUpdateForLocalInput && bridge.ApplyLocalPlayerEveryFrame && bridge.LocalPlayerUpdateMode != OUTL_ActorInputUpdateMode.SchedulerOnly, "Player input applies every Unity frame", ref errors, sb);
        CheckInputSinkOrder(go, ref errors, sb);
    }

    private static void ValidateNpc(GameObject go, string label, bool creature, ref int errors, StringBuilder sb)
    {
        Check(go != null, label + " exists", ref errors, sb);
        if (go == null) return;
        Check(go.GetComponent<OUTL_EntityAdapter>() != null, label + " has EntityAdapter", ref errors, sb);
        Check(go.GetComponent<OUTL_AIActor>() != null, label + " has AIActor", ref errors, sb);
        Check(go.GetComponent<OUTL_NPCBehaviorController>() != null, label + " has NPCBehaviorController", ref errors, sb);
        Check(go.GetComponent<OUTL_BotInputDriver>() != null, label + " has BotInputDriver", ref errors, sb);
        Check(go.GetComponent<OUTL_ActorControlBridge>() != null, label + " has ActorControlBridge", ref errors, sb);
        Check(go.GetComponent<OUTL_NavMoverInputSink>() != null, label + " has NavMoverInputSink", ref errors, sb);
        Check(go.GetComponent<OUTL_AimInputSink>() != null, label + " has AimInputSink", ref errors, sb);
        Check(go.GetComponent<OUTL_AttackDriverInputSink>() != null, label + " has AttackDriverInputSink", ref errors, sb);
        Check(go.GetComponent<OUTL_TacticalPlanner>() != null, label + " has TacticalPlanner", ref errors, sb);
        Check(go.GetComponent<OUTL_AimPlanner>() != null, label + " has AimPlanner", ref errors, sb);
        Check(go.GetComponent<OUTL_AIArsenalSelector>() != null, label + " has AIArsenalSelector", ref errors, sb);
        Check(go.GetComponent<OUTL_Vitals>() != null && go.GetComponent<OUTL_DamageReceiver>() != null, label + " has vitals/damage", ref errors, sb);
        Check(go.GetComponent<OUTL_DeathRuntime>() != null && go.GetComponent<OUTL_DeathHandler>() != null, label + " has death runtime/handler", ref errors, sb);
        Check(go.GetComponent<CharacterController>() != null, label + " has CharacterController fallback motor", ref errors, sb);
        Check(!creature || go.GetComponent<OUTL_AbilityInputSink>() != null, label + " has ability input", ref errors, sb);
        Check(!creature || HasLeap(go), label + " has leap ability profile", ref errors, sb);
        Check(IsAboveGround(go), label + " is above ground", ref errors, sb);
        OUTL_ActorControlBridge bridge = go.GetComponent<OUTL_ActorControlBridge>();
        Check(bridge != null && bridge.UseUnityUpdateForLocalInput && bridge.ApplyNearActorsEveryFrame, label + " applies Full/Near input every Unity frame", ref errors, sb);
        OUTL_NavMeshMover mover = go.GetComponent<OUTL_NavMeshMover>();
        Check(mover != null && mover.UseUnityUpdateForFullNear, label + " NavMover updates Full/Near movement every Unity frame", ref errors, sb);
        Check(mover != null && mover.StableGroundFallback && mover.DisableGravityWhenNoGround && mover.UseCharacterControllerFallback, label + " has stable no-NavMesh ground fallback", ref errors, sb);
        CheckInputSinkOrder(go, ref errors, sb);
    }

    private static void ValidatePickup(GameObject pickup, GameObject player, ref int errors, StringBuilder sb)
    {
        Check(pickup != null, "Pickup exists", ref errors, sb);
        if (pickup == null) return;
        OUTL_ItemPickup itemPickup = pickup.GetComponent<OUTL_ItemPickup>();
        Check(itemPickup != null && itemPickup.Item != null && itemPickup.Count > 0, "Pickup has item/count", ref errors, sb);
        Check(pickup.GetComponent<OUTL_Interactable>() != null, "Pickup has Interactable for Use command", ref errors, sb);
        Check(player != null && player.GetComponent<OUTL_InventoryRuntime>() != null, "Pickup receiver inventory exists", ref errors, sb);
    }

    private static void ValidateDestructible(GameObject destructible, ref int errors, StringBuilder sb)
    {
        Check(destructible != null, "Destructible exists", ref errors, sb);
        if (destructible == null) return;
        Check(destructible.GetComponent<OUTL_DamageReceiver>() != null && destructible.GetComponent<OUTL_Vitals>() != null, "Destructible damage stack exists", ref errors, sb);
        Check(destructible.GetComponent<OUTL_LootDropper>() != null, "Destructible has loot dropper", ref errors, sb);
    }

    private static void ValidateContainer(GameObject chest, ref int errors, StringBuilder sb)
    {
        Check(chest != null, "Chest container exists", ref errors, sb);
        if (chest == null) return;
        OUTL_ContainerRuntime container = chest.GetComponent<OUTL_ContainerRuntime>();
        Check(container != null, "Chest has ContainerRuntime", ref errors, sb);
        Check(chest.GetComponent<OUTL_ChestInteractable>() != null, "Chest has command interactable", ref errors, sb);
        Check(container != null && container.Def != null, "Chest has ContainerDef", ref errors, sb);
        Check(container != null && container.Def != null && container.Def.LootTable != null, "Chest uses OUTL_LootTableDef", ref errors, sb);
        Check(chest.GetComponent<OUTL_EntityAdapter>() != null && chest.GetComponent<OUTL_EntityAdapter>().RestoreSpawnIfMissing, "Chest can restore through save spawn resolver", ref errors, sb);
    }

    private static void ValidateQuest(OUTL_QuestDef quest, OUTL_World world, ref int errors, StringBuilder sb)
    {
        Check(quest != null, "Core quest asset exists", ref errors, sb);
        Check(quest != null && quest.Objectives != null && quest.Objectives.Length >= 3, "Core quest has kill/collect/open objectives", ref errors, sb);
        Check(quest != null && HasObjective(quest, OUTL_QuestObjectiveType.Kill), "Core quest listens for kill", ref errors, sb);
        Check(quest != null && HasObjective(quest, OUTL_QuestObjectiveType.Collect), "Core quest listens for collect", ref errors, sb);
        Check(quest != null && HasObjective(quest, OUTL_QuestObjectiveType.OpenChest), "Core quest listens for chest open", ref errors, sb);
        OUTL_QuestBootstrap bootstrap = world != null ? world.GetComponent<OUTL_QuestBootstrap>() : null;
        Check(bootstrap != null && bootstrap.Quests != null && bootstrap.Quests.Length > 0, "Runtime has quest bootstrap", ref errors, sb);
    }

    private static void ValidateSaveMaterialization(OUTL_World world, GameObject enemy, ref int errors, StringBuilder sb)
    {
        Check(world != null && world.EnableAutomaticMaterialization, "World automatic materialization enabled", ref errors, sb);
        Check(world != null && world.MaterializationBudgetPerTick > 0, "Materialization has per-tick budget", ref errors, sb);
        Check(world != null && world.MaterializeEnterDistance < world.DematerializeExitDistance, "Materialization has enter/exit hysteresis", ref errors, sb);
        Check(world != null && world.MaxAbstractEncountersPerTick > 0, "Abstract encounter budget enabled", ref errors, sb);
        OUTL_SaveSpawnResolverRegistry resolver = world != null ? world.GetComponent<OUTL_SaveSpawnResolverRegistry>() : null;
        Check(resolver != null && resolver.EntityDefs != null && resolver.EntityDefs.Length > 0, "Save spawn resolver has EntityDefs", ref errors, sb);
        OUTL_EntityAdapter entity = enemy != null ? enemy.GetComponent<OUTL_EntityAdapter>() : null;
        Check(entity != null && entity.RestoreSpawnIfMissing, "NPC can re-materialize from save record", ref errors, sb);
    }

    private static void ValidateProjectilePrefab(ref int errors, StringBuilder sb)
    {
        OUTL_AttackProfile projectile = Load<OUTL_AttackProfile>(CombatFolder + "/OUTL_Attack_Generic_Projectile.asset");
        GameObject prefab = projectile != null ? projectile.ProjectilePrefab : null;
        Check(prefab != null, "Projectile attack has prefab", ref errors, sb);
        if (prefab == null) return;
        Check(prefab.GetComponent<OUTL_Projectile>() != null, "Projectile prefab has OUTL_Projectile", ref errors, sb);
        Check(prefab.GetComponent<Collider>() != null || prefab.GetComponentInChildren<Collider>() != null, "Projectile prefab has collider", ref errors, sb);
        Check(prefab.GetComponent<OUTL_IPoolReset>() != null, "Projectile prefab has pool reset", ref errors, sb);
    }

    private static void ValidateLocalEgregore(GameObject go, OUTL_World world, ref int errors, StringBuilder sb)
    {
        Check(go != null, "Local Egregore exists", ref errors, sb);
        if (go == null) return;
        OUTL_EgregoreComponent component = go.GetComponent<OUTL_EgregoreComponent>();
        Check(component != null && component.Def != null, "Local Egregore has component/def", ref errors, sb);
        Check(go.GetComponent<OUTL_EntityAdapter>() != null, "Local Egregore has EntityAdapter save boundary", ref errors, sb);
        if (component == null || component.Def == null) return;

        OUTL_EgregoreRuntime runtime = component.Runtime;
        runtime.Initialize(component.Def);
        runtime.ApplyEvent(new OUTL_Event(OUTL_EventType.Killed, OUTL_EntityId.None, OUTL_EntityId.None) { Key = "skeleton.death", Point = go.transform.position }, component.Def);
        runtime.Tick(component.Def, 1f, 1f, go.transform.position);
        Check(runtime.TraumaMemory > 0f && (runtime.CurrentCyclePhase == OUTL_EgregoreCyclePhase.ShadowConfrontation || runtime.CurrentCyclePhase == OUTL_EgregoreCyclePhase.Crisis || runtime.CurrentCyclePhase == OUTL_EgregoreCyclePhase.Call), "death/combat pushes Egregore toward Shadow/Crisis", ref errors, sb);

        runtime.ApplyEvent(new OUTL_Event(OUTL_EventType.QuestCompleted, OUTL_EntityId.None, OUTL_EntityId.None) { Key = "skeleton.integration", IntValue = (int)OUTL_EgregoreQuestHook.IntegrationQuest, Point = go.transform.position }, component.Def);
        runtime.Tick(component.Def, 2f, 1f, go.transform.position);
        Check(runtime.IntegrationProgress > 0f || runtime.RenewalProgress > 0f || runtime.CurrentCyclePhase == OUTL_EgregoreCyclePhase.Integration || runtime.CurrentCyclePhase == OUTL_EgregoreCyclePhase.Renewal, "completed quest pushes Egregore toward Integration/Renewal", ref errors, sb);

        runtime.ApplyEvent(new OUTL_Event(OUTL_EventType.QuestFailed, OUTL_EntityId.None, OUTL_EntityId.None) { Key = "skeleton.failure", IntValue = (int)OUTL_EgregoreQuestHook.ShadowQuest, Point = go.transform.position }, component.Def);
        runtime.Tick(component.Def, 3f, 1f, go.transform.position);
        Check(runtime.CorruptionProgress > 0f || runtime.CurrentCyclePhase == OUTL_EgregoreCyclePhase.CorruptionLoop || runtime.CurrentCyclePhase == OUTL_EgregoreCyclePhase.Collapse, "failed quest pushes Egregore toward Corruption/Collapse", ref errors, sb);

        Check(OUTL_EgregoreUtility.BehaviorModeForPhase(OUTL_EgregoreCyclePhase.Crisis) == OUTL_BehaviorModeId.Lockdown, "Egregore cycle modifies NPC behavior mode", ref errors, sb);
        Check(OUTL_LootResolver.GetPhaseChanceMultiplier(OUTL_EgregoreCyclePhase.RevelationOrBoon) > 1f, "Egregore cycle modifies loot context", ref errors, sb);

        OUTL_ComponentSavePayload payload = new OUTL_ComponentSavePayload { Key = component.OUTL_SaveKey };
        OUTL_ComponentSaveWriter writer = new OUTL_ComponentSaveWriter(payload);
        int traceCount = runtime.MemoryTraceCount;
        OUTL_EgregoreCyclePhase savedPhase = runtime.CurrentCyclePhase;
        component.OUTL_Capture(writer);
        runtime.Initialize(component.Def);
        component.OUTL_Restore(new OUTL_ComponentSaveReader(payload));
        Check(runtime.CurrentCyclePhase == savedPhase && runtime.MemoryTraceCount == traceCount, "save/load restores Egregore cycle phase and memory traces", ref errors, sb);

        if (world != null)
        {
            world.WorldLedger.ApplyEgregoreField(runtime.BuildField(go.transform.position, world.WorldLedger.ActivityCellSize, 4f));
            OUTL_WorldAddress address = OUTL_WorldAddress.FromWorldPosition(go.transform.position, world.WorldLedger.ActivityCellSize);
            OUTL_WorldCellSummary summary;
            Check(world.WorldLedger.GetCellSummary(address.ActivityCell, out summary) && summary.EgregoreCyclePhase == runtime.CurrentCyclePhase, "WorldLedger stores Egregore phase summary", ref errors, sb);
        }
    }

    private static void CheckInputSinkOrder(GameObject go, ref int errors, StringBuilder sb)
    {
        OUTL_ActorControlBridge bridge = go != null ? go.GetComponent<OUTL_ActorControlBridge>() : null;
        Check(bridge != null && bridge.InputSinkBehaviours != null && bridge.InputSinkBehaviours.Length > 0, go.name + " has input sink list", ref errors, sb);
        if (bridge == null || bridge.InputSinkBehaviours == null) return;
        OUTL_ActorInputPhase last = OUTL_ActorInputPhase.Movement;
        for (int i = 0; i < bridge.InputSinkBehaviours.Length; i++)
        {
            OUTL_IActorInputSink sink = bridge.InputSinkBehaviours[i] as OUTL_IActorInputSink;
            if (sink == null) continue;
            OUTL_IActorInputPhasedSink phased = sink as OUTL_IActorInputPhasedSink;
            OUTL_ActorInputPhase phase = phased != null ? phased.Phase : OUTL_ActorInputPhase.Interaction;
            if (i > 0 && phase < last)
            {
                errors++;
                sb.AppendLine("ERROR input sinks not phase sorted on " + go.name);
                return;
            }
            last = phase;
        }
    }

    private static bool HasLeap(GameObject go)
    {
        OUTL_AbilityInputSink ability = go != null ? go.GetComponent<OUTL_AbilityInputSink>() : null;
        return ability != null && ability.PrimaryAbility is OUTL_LeapAbilityProfile;
    }

    private static bool HasObjective(OUTL_QuestDef quest, OUTL_QuestObjectiveType type)
    {
        if (quest == null || quest.Objectives == null) return false;
        for (int i = 0; i < quest.Objectives.Length; i++)
            if (quest.Objectives[i] != null && quest.Objectives[i].Type == type)
                return true;
        return false;
    }

    private static bool IsAboveGround(GameObject go)
    {
        Collider collider = go != null ? go.GetComponentInChildren<Collider>() : null;
        return collider != null && collider.bounds.min.y >= -0.08f;
    }

    private static void Check(bool condition, string message, ref int errors, StringBuilder sb)
    {
        if (condition)
        {
            sb.AppendLine("OK " + message);
            return;
        }
        errors++;
        sb.AppendLine("ERROR " + message);
    }

    private static void Face(GameObject actor, GameObject target)
    {
        if (actor == null || target == null) return;
        Vector3 to = target.transform.position - actor.transform.position;
        to.y = 0f;
        if (to.sqrMagnitude > 0.001f) actor.transform.rotation = Quaternion.LookRotation(to.normalized);
    }

    private static GameObject AddFoundationEntity(string prefabName, string instanceName, string stablePrefix, string className, OUTL_FactionDef faction, float y)
    {
        OUTL_AbstractPrefabGeneratorEditor.GenerateAllFoundationPrefabs();
        if (faction == null) faction = ResolveDefaultFaction(className);
        Transform parent = ResolveAddParent();
        GameObject go = InstantiateFoundation(prefabName, instanceName, ResolveAddPosition(y), parent);
        if (go == null) return null;
        ConfigurePlacedEntity(go, BuildUniqueStableId(stablePrefix), className, faction, true);
        FinishAddedObject(go);
        return go;
    }

    private static OUTL_FactionDef ResolveDefaultFaction(string className)
    {
        if (className == "actor.controlled") return Load<OUTL_FactionDef>(DefFolder + "/OUTL_Faction_Controlled.asset");
        if (className == "actor.npc.melee") return Load<OUTL_FactionDef>(DefFolder + "/OUTL_Faction_GroupB.asset");
        if (className == "actor.npc.ranged" || className == "actor.creature" || className == "emitter.turret.projectile") return Load<OUTL_FactionDef>(DefFolder + "/OUTL_Faction_GroupA.asset");
        return Load<OUTL_FactionDef>(DefFolder + "/OUTL_Faction_Neutral.asset");
    }

    private static Transform ResolveAddParent()
    {
        GameObject root = GameObject.Find(RootName);
        if (root != null) return root.transform;
        if (Selection.activeTransform != null) return Selection.activeTransform;
        return null;
    }

    private static Vector3 ResolveAddPosition(float y)
    {
        SceneView view = SceneView.lastActiveSceneView;
        Vector3 position = view != null ? view.pivot : Vector3.zero;
        position.y = y;
        return position;
    }

    private static string BuildUniqueStableId(string prefix)
    {
        OUTL_EntityAdapter[] entities = Object.FindObjectsOfType<OUTL_EntityAdapter>(true);
        for (int i = 1; i < 10000; i++)
        {
            string candidate = prefix + "." + i.ToString("00");
            bool used = false;
            for (int j = 0; j < entities.Length; j++)
            {
                OUTL_EntityAdapter entity = entities[j];
                if (entity == null) continue;
                if (entity.StableId == candidate || entity.TargetName == candidate)
                {
                    used = true;
                    break;
                }
            }
            if (!used) return candidate;
        }
        return prefix + "." + System.Guid.NewGuid().ToString("N");
    }

    private static void FinishAddedObject(GameObject go)
    {
        if (go == null) return;
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
        EditorSceneManager.MarkSceneDirty(go.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("OUTL added abstract scene object: " + go.name, go);
    }

    private static T Load<T>(string path) where T : Object
    {
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    private static void EnsureFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder)) return;
        string[] parts = assetFolder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
