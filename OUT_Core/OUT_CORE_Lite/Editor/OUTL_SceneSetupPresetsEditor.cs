#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class OUTL_SceneSetupPresetsEditor
{
    private const string RootName = "OUTL_SCENE_ROOT";
    private const string RuntimeName = "OUTL_Runtime";
    private const string PresetFolder = "Assets/OUT/OUT_Core/OUT_CORE_Lite/Generated/ScenePresets";
    private const string TickProfilePath = PresetFolder + "/OUTL_TickProfile_Production.asset";
    private const string OccultistRoot = "Assets/OUT/OUT_Core/OUT_CORE_Lite/Generated/OccultistEnemyPack";
    private const string RuntimeRigPath = OccultistRoot + "/Prefabs/OUTL_Occultist_RuntimeRig.prefab";
    private const string PopulationFieldPath = OccultistRoot + "/Prefabs/OUTL_Occultist_1000_AbstractField.prefab";
    private const string PlayerFactionPath = OccultistRoot + "/Definitions/OUTL_Faction_Player.asset";
    private const string ShotgunEnemyPath = OccultistRoot + "/Prefabs/OUTL_Enemy_Occultist_Shotgun.prefab";

    [MenuItem("OUT CORE Lite/Scene Setup/Setup Open Scene - Production", priority = 1)]
    public static void SetupOpenSceneProduction()
    {
        SetupRuntime(false);
    }

    [MenuItem("OUT CORE Lite/Scene Setup/Setup Open Scene - OuterDriver Legacy Player", priority = 2)]
    public static void SetupOpenSceneLegacyPlayer()
    {
        OUTL_World world = SetupRuntime(false);
        GameObject player = ResolveLegacyPlayer();
        if (player == null)
        {
            Debug.LogWarning("OUTL setup: select the GameObject containing OUT_Health_Main (or its child), then run this preset again.");
            return;
        }

        ConfigureLegacyPlayer(player, world);
        Selection.activeGameObject = player;
        EditorGUIUtility.PingObject(player);
        EditorSceneManager.MarkSceneDirty(player.scene);
        Debug.Log("OUT CORE Lite: production scene setup complete; legacy OUT_Health_Main player is now an OUTL player target.", player);
    }

    [MenuItem("OUT CORE Lite/Scene Setup/Create Empty Enemy Test Scene", priority = 20)]
    public static void CreateEmptyEnemyTestScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        OUTL_World world = SetupRuntime(true);
        GameObject root = GameObject.Find(RootName);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Undo.RegisterCreatedObjectUndo(ground, "Create OUTL Test Ground");
        ground.name = "OUTL_TestGround";
        ground.transform.SetParent(root.transform, false);
        ground.transform.localScale = new Vector3(30f, 1f, 30f);
        TryBuildNavMesh(ground);

        GameObject player = CreatePureOUTLTestPlayer(root.transform);
        EnsureOccultistPopulation(root.transform, Vector3.zero, new Vector2(260f, 260f));
        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShotgunEnemyPath);
        if (enemyPrefab != null)
        {
            GameObject enemy = PrefabUtility.InstantiatePrefab(enemyPrefab, root.transform) as GameObject;
            if (enemy != null)
            {
                Undo.RegisterCreatedObjectUndo(enemy, "Create OUTL Test Enemy");
                enemy.name = "OUTL_TestEnemy_Shotgun";
                enemy.transform.position = new Vector3(0f, 0f, 16f);
                enemy.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
        }

        OUTL_ChunkProcessingDriver chunks = world != null ? world.GetComponent<OUTL_ChunkProcessingDriver>() : null;
        if (chunks != null) chunks.Focus = player.transform;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = player;
        Debug.Log("OUT CORE Lite: empty enemy test scene created. Press Play; the red capsule is a pure OUTL damage target.");
    }

    [MenuItem("OUT CORE Lite/Scene Setup/Repair Open Scene - 1000 Occultists", priority = 21)]
    public static void RepairOpenSceneOccultistPopulationTest()
    {
        OUTL_World world = SetupRuntime(true);
        GameObject runtime = GameObject.Find(RuntimeName);
        GameObject root = GameObject.Find(RootName);
        if (runtime == null || root == null || world == null) return;

        NormalizeOccultistRuntimeRig(runtime.transform);

        Transform player = ResolveOUTLPlayerTransform();
        if (player == null)
        {
            GameObject legacyPlayer = ResolveLegacyPlayer();
            if (legacyPlayer != null)
            {
                ConfigureLegacyPlayer(legacyPlayer, world);
                player = ResolveOUTLPlayerTransform();
            }
        }
        if (player == null)
            player = CreatePureOUTLTestPlayer(root.transform).transform;

        GameObject playerRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(player.gameObject);
        Transform moveTarget = playerRoot != null ? playerRoot.transform : player;
        Undo.RecordObject(moveTarget, "Center OUTL Test Player");
        moveTarget.position = Vector3.zero;
        NormalizeAudioListeners(moveTarget);

        GameObject ground = GameObject.Find("OUTL_TestGround");
        if (ground == null) ground = GameObject.Find("Plane");
        if (ground != null)
        {
            Undo.RecordObject(ground.transform, "Resize OUTL Test Ground");
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(30f, 1f, 30f);
            TryBuildNavMesh(ground);
        }

        EnsureOccultistPopulation(root.transform, Vector3.zero, new Vector2(260f, 260f));
        world.EnableAutomaticMaterialization = true;
        world.MaterializationFocusTargetName = "player";
        world.MaterializationTickInterval = 0.25f;
        world.MaterializationBudgetPerTick = 8;
        world.MaterializeEnterDistance = 72f;
        world.DematerializeExitDistance = 128f;

        OUTL_ChunkProcessingDriver chunks = world.GetComponent<OUTL_ChunkProcessingDriver>();
        if (chunks != null) chunks.Focus = player;

        EditorUtility.SetDirty(world);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = root;
        Debug.Log("OUT CORE Lite: scene repaired for 1000 abstract Occultists. One runtime rig, one population field, centered player and NavMesh.");
    }

    [MenuItem("OUT CORE Lite/Add to Scene/Configure Selected Legacy Player", priority = 1)]
    public static void ConfigureSelectedLegacyPlayer()
    {
        OUTL_World world = Object.FindObjectOfType<OUTL_World>(true);
        if (world == null) world = SetupRuntime(false);
        GameObject player = ResolveLegacyPlayer();
        if (player == null)
        {
            Debug.LogWarning("Select an object containing OUT_Health_Main or one of its children.");
            return;
        }
        ConfigureLegacyPlayer(player, world);
        EditorSceneManager.MarkSceneDirty(player.scene);
    }

    [MenuItem("OUT CORE Lite/Add to Scene/Add Occultist Runtime Rig", priority = 20)]
    public static void AddOccultistRuntimeRig()
    {
        GameObject runtime = FindOrCreateRuntime();
        EnsureOccultistRuntimeRig(runtime.transform);
        EditorSceneManager.MarkSceneDirty(runtime.scene);
    }

    [MenuItem("OUT CORE Lite/Add to Scene/Add Occultist Shotgun Enemy", priority = 21)]
    public static void AddOccultistShotgunEnemy()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShotgunEnemyPath);
        if (prefab == null)
        {
            OUTL_OccultistEnemyPackGeneratorEditor.Generate();
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShotgunEnemyPath);
        }
        if (prefab == null) return;
        GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (go == null) return;
        Undo.RegisterCreatedObjectUndo(go, "Add OUTL Occultist Shotgun");
        go.transform.position = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(go.scene);
    }

    [MenuItem("OUT CORE Lite/Scene Setup/Select Runtime Root", priority = 100)]
    public static void SelectRuntimeRoot()
    {
        GameObject runtime = GameObject.Find(RuntimeName);
        if (runtime == null) runtime = FindOrCreateRuntime();
        Selection.activeGameObject = runtime;
        EditorGUIUtility.PingObject(runtime);
    }

    private static OUTL_World SetupRuntime(bool testScene)
    {
        EnsureFolder(PresetFolder);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RuntimeRigPath) == null)
            OUTL_OccultistEnemyPackGeneratorEditor.Generate();

        GameObject runtime = FindOrCreateRuntime();
        OUTL_World world = GetOrAdd<OUTL_World>(runtime);
        world.TickProfile = EnsureProductionTickProfile();
        world.ApplyTickProfileOnAwake = true;
        world.UpdateMode = OUTL_WorldUpdateMode.CustomFixedStep;
        world.SimulationStep = 0.05f;
        world.MaxSimulationStepsPerFrame = 3;
        world.AutoFindAdaptersOnStart = true;
        world.EnableAutomaticMaterialization = true;
        world.MaterializationFocusTargetName = "player";
        world.MaterializationTickInterval = 0.25f;
        world.MaterializationBudgetPerTick = 12;
        world.MaterializeEnterDistance = testScene ? 72f : 88f;
        world.DematerializeExitDistance = testScene ? 128f : 144f;
        world.MaxNpcBehaviorTicksPerFrame = 96;
        world.MaxNpcRouteUpdatesPerFrame = 48;
        world.MaxNpcPathRequestsPerFrame = 8;
        world.MaxNpcStimulusInterruptsPerFrame = 48;

        OUTL_PoolSystem pool = GetOrAdd<OUTL_PoolSystem>(runtime);
        pool.DefaultCapacity = 16;
        pool.MaxSize = 2048;
        pool.CollectionChecks = false;

        OUTL_ChunkProcessingDriver chunks = GetOrAdd<OUTL_ChunkProcessingDriver>(runtime);
        chunks.BuiltInPreset = OUTL_ProcessingBuiltInPreset.StreamingWorld;
        chunks.UseAssetProfile = false;
        chunks.ApplyPresetOnEnable = true;
        chunks.FocusTargetName = "player";
        chunks.FocusClassName = "player";
        chunks.UseRegistryFocusFallback = true;
        chunks.EnforceCanonicalThreeByThree = true;
        chunks.ChunkSize = 64f;
        chunks.FullRadius = 0;
        chunks.NearRadius = 1;
        chunks.MidRadius = 2;
        chunks.FarRadius = 3;
        chunks.OverrideDriverTickInterval = true;
        chunks.DriverTickInterval = 0.30f;
        chunks.OverrideEntitiesPerTick = true;
        chunks.EntitiesPerTick = 320;
        chunks.CacheRegistrySnapshot = true;
        chunks.FullRefreshInterval = 2f;
        chunks.BuildParallelReadinessSnapshot = false;
        chunks.CalculateParallelTierPreview = false;

        EnsureOccultistRuntimeRig(runtime.transform);
        Transform player = ResolveOUTLPlayerTransform();
        if (player != null) chunks.Focus = player;

        EditorUtility.SetDirty(world);
        EditorUtility.SetDirty(pool);
        EditorUtility.SetDirty(chunks);
        EditorSceneManager.MarkSceneDirty(runtime.scene);
        Selection.activeGameObject = runtime;
        EditorGUIUtility.PingObject(runtime);
        Debug.Log("OUT CORE Lite: production runtime installed/updated. Canonical runtime is OUTL_ only.", runtime);
        return world;
    }

    private static GameObject FindOrCreateRuntime()
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create OUTL Scene Root");
        }
        Transform child = root.transform.Find(RuntimeName);
        if (child != null) return child.gameObject;
        GameObject runtime = new GameObject(RuntimeName);
        Undo.RegisterCreatedObjectUndo(runtime, "Create OUTL Runtime");
        runtime.transform.SetParent(root.transform, false);
        return runtime;
    }

    private static void EnsureOccultistRuntimeRig(Transform parent)
    {
        if (parent == null) return;
        Transform existing = parent.Find("OUTL_Occultist_RuntimeRig");
        if (existing != null) return;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RuntimeRigPath);
        if (prefab == null) return;
        GameObject rig = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (rig == null) return;
        Undo.RegisterCreatedObjectUndo(rig, "Add OUTL Occultist Runtime Rig");
        rig.name = "OUTL_Occultist_RuntimeRig";
    }

    private static void NormalizeOccultistRuntimeRig(Transform parent)
    {
        if (parent == null) return;
        OUTL_SaveSpawnResolverRegistry[] resolvers = parent.GetComponentsInChildren<OUTL_SaveSpawnResolverRegistry>(true);
        OUTL_SaveSpawnResolverRegistry keep = null;
        for (int i = 0; i < resolvers.Length; i++)
        {
            OUTL_SaveSpawnResolverRegistry resolver = resolvers[i];
            if (resolver == null) continue;
            if (keep == null || resolver.gameObject.name == "OUTL_Occultist_RuntimeRig")
                keep = resolver;
        }

        for (int i = 0; i < resolvers.Length; i++)
        {
            OUTL_SaveSpawnResolverRegistry resolver = resolvers[i];
            if (resolver == null || resolver == keep) continue;
            Undo.DestroyObjectImmediate(resolver.gameObject);
        }

        if (keep == null)
        {
            EnsureOccultistRuntimeRig(parent);
            keep = parent.GetComponentInChildren<OUTL_SaveSpawnResolverRegistry>(true);
        }
        if (keep == null) return;

        keep.gameObject.name = "OUTL_Occultist_RuntimeRig";
        keep.transform.SetParent(parent, false);
        keep.transform.localPosition = Vector3.zero;
        keep.transform.localRotation = Quaternion.identity;
        keep.transform.localScale = Vector3.one;
    }

    private static OUTL_EnemyPopulationField EnsureOccultistPopulation(Transform parent, Vector3 position, Vector2 size)
    {
        OUTL_EnemyPopulationField[] fields = Object.FindObjectsOfType<OUTL_EnemyPopulationField>(true);
        OUTL_EnemyPopulationField field = fields.Length > 0 ? fields[0] : null;
        for (int i = 1; i < fields.Length; i++)
            if (fields[i] != null) Undo.DestroyObjectImmediate(fields[i].gameObject);

        if (field == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PopulationFieldPath);
            if (prefab == null)
            {
                OUTL_OccultistEnemyPackGeneratorEditor.Generate();
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PopulationFieldPath);
            }
            GameObject instance = prefab != null ? PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject : null;
            if (instance == null) return null;
            Undo.RegisterCreatedObjectUndo(instance, "Add OUTL Abstract Occultist Population");
            field = instance.GetComponent<OUTL_EnemyPopulationField>();
        }

        field.gameObject.name = "OUTL_Occultist_1000_AbstractField";
        field.transform.position = position;
        field.Count = 1000;
        field.Size = size;
        field.RegisterOnStart = true;
        EditorUtility.SetDirty(field);
        return field;
    }

    private static void NormalizeAudioListeners(Transform playerRoot)
    {
        AudioListener[] listeners = Object.FindObjectsOfType<AudioListener>(true);
        AudioListener keep = null;
        for (int i = 0; i < listeners.Length; i++)
        {
            AudioListener listener = listeners[i];
            if (listener != null && playerRoot != null && listener.transform.IsChildOf(playerRoot))
            {
                keep = listener;
                break;
            }
        }
        if (keep == null && listeners.Length > 0) keep = listeners[0];

        for (int i = 0; i < listeners.Length; i++)
        {
            AudioListener listener = listeners[i];
            if (listener == null) continue;
            Undo.RecordObject(listener, "Normalize OUTL Audio Listeners");
            listener.enabled = listener == keep;
            EditorUtility.SetDirty(listener);
        }
    }

    private static void ConfigureLegacyPlayer(GameObject player, OUTL_World world)
    {
        if (player == null) return;
        OUT_Health_Main health = player.GetComponent<OUT_Health_Main>();
        if (health == null) health = player.GetComponentInParent<OUT_Health_Main>(true);
        if (health == null) health = player.GetComponentInChildren<OUT_Health_Main>(true);
        if (health == null) return;
        GameObject owner = health.gameObject;

        OUTL_EntityAdapter entity = owner.GetComponent<OUTL_EntityAdapter>();
        if (entity == null) entity = Undo.AddComponent<OUTL_EntityAdapter>(owner);
        entity.Def = null;
        entity.Faction = AssetDatabase.LoadAssetAtPath<OUTL_FactionDef>(PlayerFactionPath);
        entity.ClassNameOverride = "player";
        entity.TargetName = "player";
        entity.StableId = string.Empty;
        entity.SavePersistent = false;
        entity.RestoreSpawnIfMissing = false;
        entity.RegisterOnEnable = true;
        entity.RegisterTick = true;
        entity.RegisterRandomTick = false;
        entity.RegisterInSectors = true;
        entity.TickLane = OUTL_TickLane.Logic;
        entity.TickInterval = 0.1f;

        OUTL_LegacyPlayerHealthBridge bridge = owner.GetComponent<OUTL_LegacyPlayerHealthBridge>();
        if (bridge == null) bridge = Undo.AddComponent<OUTL_LegacyPlayerHealthBridge>(owner);
        bridge.Entity = entity;
        bridge.LegacyHealth = health;
        bridge.SyncInterval = 0.1f;

        if (world != null)
        {
            world.MaterializationFocusTargetName = "player";
            OUTL_ChunkProcessingDriver chunks = world.GetComponent<OUTL_ChunkProcessingDriver>();
            if (chunks != null)
            {
                chunks.Focus = owner.transform;
                chunks.FocusTargetName = "player";
                chunks.FocusClassName = "player";
            }
        }

        EditorUtility.SetDirty(entity);
        EditorUtility.SetDirty(bridge);
        EditorUtility.SetDirty(owner);
    }

    private static GameObject ResolveLegacyPlayer()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected != null)
        {
            OUT_Health_Main health = selected.GetComponent<OUT_Health_Main>();
            if (health == null) health = selected.GetComponentInParent<OUT_Health_Main>(true);
            if (health == null) health = selected.GetComponentInChildren<OUT_Health_Main>(true);
            if (health != null) return health.gameObject;
        }

        OUT_Health_Main[] all = Object.FindObjectsOfType<OUT_Health_Main>(true);
        if (all.Length == 1) return all[0].gameObject;
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && (all[i].CompareTag("Player") || all[i].GetComponent<OUT_FirstPersonController>() != null))
                return all[i].gameObject;
        return null;
    }

    private static Transform ResolveOUTLPlayerTransform()
    {
        OUTL_EntityAdapter[] entities = Object.FindObjectsOfType<OUTL_EntityAdapter>(true);
        for (int i = 0; i < entities.Length; i++)
        {
            OUTL_EntityAdapter entity = entities[i];
            if (entity == null) continue;
            if (entity.TargetName == "player" || entity.ClassNameOverride == "player") return entity.transform;
        }
        return null;
    }

    private static GameObject CreatePureOUTLTestPlayer(Transform parent)
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Undo.RegisterCreatedObjectUndo(player, "Create OUTL Test Player");
        player.name = "OUTL_TestPlayerTarget";
        player.transform.SetParent(parent, false);
        player.transform.position = Vector3.zero;

        OUTL_EntityAdapter entity = player.AddComponent<OUTL_EntityAdapter>();
        entity.Faction = AssetDatabase.LoadAssetAtPath<OUTL_FactionDef>(PlayerFactionPath);
        entity.ClassNameOverride = "player";
        entity.TargetName = "player";
        entity.SavePersistent = false;
        entity.RegisterInSectors = true;
        entity.TickInterval = 0.1f;

        OUTL_Hitbox hitbox = player.AddComponent<OUTL_Hitbox>();
        hitbox.Entity = entity;
        hitbox.Zone = OUTL_HitboxZone.Torso;
        OUTL_DamageReceiver damage = player.AddComponent<OUTL_DamageReceiver>();
        damage.Entity = entity;
        OUTL_Vitals vitals = player.AddComponent<OUTL_Vitals>();
        vitals.Entity = entity;
        vitals.DefaultHealth = 250f;
        vitals.DefaultMaxHealth = 250f;
        OUTL_DeathHandler death = player.AddComponent<OUTL_DeathHandler>();
        death.Entity = entity;
        death.QueueDespawn = false;
        death.DisableColliders = false;
        death.DisableRenderers = false;

        Renderer renderer = player.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.72f, 0.08f, 0.08f);
            renderer.sharedMaterial = material;
        }

        GameObject cameraObject = new GameObject("TestCamera");
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.position = new Vector3(0f, 8f, -14f);
        cameraObject.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
        cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        return player;
    }

    private static OUTL_TickProfile EnsureProductionTickProfile()
    {
        OUTL_TickProfile profile = AssetDatabase.LoadAssetAtPath<OUTL_TickProfile>(TickProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<OUTL_TickProfile>();
            AssetDatabase.CreateAsset(profile, TickProfilePath);
        }
        profile.logicInterval = 0.1f;
        profile.aiNearInterval = 0.1f;
        profile.aiMidInterval = 0.6f;
        profile.aiFarInterval = 2f;
        profile.aiDormantInterval = 8f;
        profile.questInterval = 0.5f;
        profile.stimulusInterval = 0.25f;
        profile.chunkProcessingInterval = 0.3f;
        profile.npcFullInterval = 0.05f;
        profile.npcNearInterval = 0.2f;
        profile.npcMidInterval = 2f;
        profile.npcFarInterval = 10f;
        profile.npcDormantInterval = 60f;
        profile.maxAITicksPerFrame = 96;
        profile.maxStimuliProcessedPerFrame = 192;
        profile.maxSectorUpdatesPerFrame = 192;
        profile.maxEgregoreSignalsPerFrame = 32;
        profile.maxNpcBehaviorTicksPerFrame = 96;
        profile.maxNpcRouteUpdatesPerFrame = 48;
        profile.maxNpcPathRequestsPerFrame = 8;
        profile.maxNpcStimulusInterruptsPerFrame = 48;
        profile.Sanitize();
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    private static void TryBuildNavMesh(GameObject ground)
    {
        System.Type surfaceType = System.Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
        if (surfaceType == null) surfaceType = System.Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation.Runtime");
        if (surfaceType == null) return;
        Component surface = ground.GetComponent(surfaceType);
        if (surface == null) surface = Undo.AddComponent(ground, surfaceType);
        System.Reflection.MethodInfo build = surfaceType.GetMethod("BuildNavMesh", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (build == null) return;
        try { build.Invoke(surface, null); }
        catch (System.Exception e) { Debug.LogWarning("OUTL test scene NavMesh build failed: " + e.Message, ground); }
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T value = go.GetComponent<T>();
        return value != null ? value : Undo.AddComponent<T>(go);
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
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
