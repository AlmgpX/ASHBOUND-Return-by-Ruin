#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class OUTL_GameLoopPresetEditor
{
    private const string MenuRoot = "OUT CORE Lite/Framework Presets/";
    private const string Folder = "Assets/OUT/OUT_Core/OUT_CORE_Lite/Templates/FrameworkPresets";

    // [MenuItem(MenuRoot + "Create Action Combat Kernel")]
    public static void CreateActionKernel() { CreateKernel("action_combat", "Action Combat Kernel", OUTL_EventType.Killed, string.Empty, null, new[] { "Role.Opposition" }, 8, 100, 50); }
    // [MenuItem(MenuRoot + "Create Wave Pressure Kernel")]
    public static void CreateWaveKernel() { CreateKernel("wave_pressure", "Wave Pressure Kernel", OUTL_EventType.Killed, string.Empty, null, new[] { "Role.Opposition" }, 25, 250, 75); }
    // [MenuItem(MenuRoot + "Create Progression Kernel")]
    public static void CreateProgressionKernel() { CreateKernel("progression", "Progression Kernel", OUTL_EventType.Killed, string.Empty, null, new[] { "Role.Opposition" }, 5, 50, 120); }
    // [MenuItem(MenuRoot + "Create Traversal Objective Kernel")]
    public static void CreateTraversalKernel() { CreateKernel("traversal", "Traversal Objective Kernel", OUTL_EventType.Custom, "Checkpoint", new[] { "Role.Controlled" }, null, 6, 300, 0); }
    // [MenuItem(MenuRoot + "Create Investigation Objective Kernel")]
    public static void CreateInvestigationKernel() { CreateKernel("investigation", "Investigation Objective Kernel", OUTL_EventType.Custom, "Objective", new[] { "Role.Controlled" }, new[] { "Role.Objective" }, 3, 150, 0); }
    // [MenuItem(MenuRoot + "Create Control Economy Kernel")]
    public static void CreateControlKernel() { CreateKernel("control_economy", "Control Economy Kernel", OUTL_EventType.Custom, "ControlPoint", null, new[] { "Role.Objective" }, 4, 400, 0); }

    private static void CreateKernel(string kernelId, string title, OUTL_EventType listen, string listenKey, string[] sourceTags, string[] targetTags, int count, int points, int xp)
    {
        EnsureFolder(Folder);
        OUTL_EntityDef controlled = CreateRoleEntityDef("OUTL_Role_ControlledActor_Def", "role.controlled_actor", "Controlled Actor Contract", new[] { "Actor", "Role.Controlled", "Role.CommandSource", "Role.Targetable" });
        OUTL_EntityDef autonomous = CreateRoleEntityDef("OUTL_Role_AutonomousActor_Def", "role.autonomous_actor", "Autonomous Actor Contract", new[] { "Actor", "Role.Autonomous", "Role.Targetable" });
        OUTL_EntityDef opposition = CreateRoleEntityDef("OUTL_Role_OppositionActor_Def", "role.opposition_actor", "Opposition Actor Contract", new[] { "Actor", "Role.Opposition", "Role.Targetable" });
        OUTL_EntityDef objective = CreateRoleEntityDef("OUTL_Role_Objective_Def", "role.objective", "Objective Contract", new[] { "Objective", "Role.Objective", "Role.Targetable" });
        OUTL_EntityDef emitter = CreateRoleEntityDef("OUTL_Role_CombatEmitter_Def", "role.combat_emitter", "Combat Emitter Contract", new[] { "Entity", "Role.CombatEmitter", "Role.CommandSource" });
        OUTL_RewardDef reward = CreateReward(kernelId, title, points, xp);
        OUTL_ChallengeDef challenge = CreateChallenge(kernelId, title, listen, listenKey, sourceTags, targetTags, count, reward);
        OUTL_GameLoopDef loop = CreateLoop(kernelId, title, challenge, reward);
        OUTL_EntityRoleBinding[] roles = CreateRoles(controlled, autonomous, opposition, objective, emitter);
        OUTL_KernelPresetDef kernel = CreateKernelPreset(kernelId, title, loop, roles);
        OUTL_GenrePresetDef compatibility = CreateCompatibilityPreset(kernelId, title, loop, roles);
        CreateRuntimeRig(loop, roles, kernel, compatibility);
        AssetDatabase.SaveAssets();
        Debug.Log("OUTL kernel preset created: " + kernelId + ". Roles are abstract contracts; bind concrete prefabs/assets later.");
    }

    private static OUTL_EntityRoleBinding[] CreateRoles(OUTL_EntityDef controlled, OUTL_EntityDef autonomous, OUTL_EntityDef opposition, OUTL_EntityDef objective, OUTL_EntityDef emitter)
    {
        return new[]
        {
            Role("role.controlled", "Controlled Actor", OUTL_EntityRoleKind.ControlledActor, controlled),
            Role("role.autonomous", "Autonomous Actor", OUTL_EntityRoleKind.AutonomousActor, autonomous),
            Role("role.opposition", "Opposition Actor", OUTL_EntityRoleKind.Opposition, opposition),
            Role("role.objective", "Objective", OUTL_EntityRoleKind.Objective, objective),
            Role("role.combat_emitter", "Combat Emitter", OUTL_EntityRoleKind.CombatEmitter, emitter)
        };
    }

    private static OUTL_EntityRoleBinding Role(string id, string title, OUTL_EntityRoleKind kind, OUTL_EntityDef def)
    {
        return new OUTL_EntityRoleBinding { RoleId = id, DisplayName = title, Kind = kind, Tags = def != null ? def.Tags : null, EntityDef = def, DefaultTier = OUTL_RuntimeTier.Full, Spawnable = true, NetworkReplicated = true };
    }

    private static OUTL_EntityDef CreateRoleEntityDef(string assetName, string className, string displayName, string[] tags)
    {
        string path = AssetDatabase.GenerateUniqueAssetPath(Folder + "/" + assetName + ".asset");
        OUTL_EntityDef def = ScriptableObject.CreateInstance<OUTL_EntityDef>();
        def.ClassName = className; def.DisplayName = displayName; def.Tags = tags;
        def.BaseStats = new[] { new OUTL_StatEntry { Key = "Health", Value = tags != null && System.Array.IndexOf(tags, "Objective") >= 0 ? 250f : 100f }, new OUTL_StatEntry { Key = "Damage", Value = 10f }, new OUTL_StatEntry { Key = "Speed", Value = 3.5f } };
        AssetDatabase.CreateAsset(def, path);
        return def;
    }

    private static OUTL_RewardDef CreateReward(string kernelId, string title, int points, int xp)
    {
        OUTL_RewardDef reward = ScriptableObject.CreateInstance<OUTL_RewardDef>();
        reward.RewardId = kernelId + ".reward.core"; reward.DisplayName = title + " Reward"; reward.CurrencyId = "score"; reward.Points = points; reward.XP = xp;
        AssetDatabase.CreateAsset(reward, AssetDatabase.GenerateUniqueAssetPath(Folder + "/OUTL_Reward_" + kernelId + ".asset"));
        return reward;
    }

    private static OUTL_ChallengeDef CreateChallenge(string kernelId, string title, OUTL_EventType listen, string key, string[] sourceTags, string[] targetTags, int count, OUTL_RewardDef reward)
    {
        OUTL_ChallengeDef challenge = ScriptableObject.CreateInstance<OUTL_ChallengeDef>();
        challenge.ChallengeId = kernelId + ".challenge.core"; challenge.DisplayName = title + " Objective"; challenge.Tags = new[] { "Kernel", kernelId }; challenge.ListenEvent = listen; challenge.ListenKey = key; challenge.RequiredSourceTags = sourceTags; challenge.RequiredTargetTags = targetTags; challenge.TargetCount = count; challenge.Rewards = new[] { reward };
        AssetDatabase.CreateAsset(challenge, AssetDatabase.GenerateUniqueAssetPath(Folder + "/OUTL_Challenge_" + kernelId + ".asset"));
        return challenge;
    }

    private static OUTL_GameLoopDef CreateLoop(string kernelId, string title, OUTL_ChallengeDef challenge, OUTL_RewardDef reward)
    {
        OUTL_GameLoopDef loop = ScriptableObject.CreateInstance<OUTL_GameLoopDef>();
        loop.LoopId = kernelId + ".loop.core"; loop.DisplayName = title; loop.StartupChallenges = new[] { challenge }; loop.RewardTable = new[] { reward }; loop.AutoStart = true; loop.ResetChallengesOnStart = true;
        AssetDatabase.CreateAsset(loop, AssetDatabase.GenerateUniqueAssetPath(Folder + "/OUTL_GameLoop_" + kernelId + ".asset"));
        return loop;
    }

    private static OUTL_KernelPresetDef CreateKernelPreset(string kernelId, string title, OUTL_GameLoopDef loop, OUTL_EntityRoleBinding[] roles)
    {
        OUTL_KernelPresetDef preset = ScriptableObject.CreateInstance<OUTL_KernelPresetDef>();
        preset.KernelId = kernelId; preset.DisplayName = title; preset.GameLoops = new[] { loop }; preset.Roles = roles; preset.OfflineReady = true; preset.HostAuthorityReady = true;
        AssetDatabase.CreateAsset(preset, AssetDatabase.GenerateUniqueAssetPath(Folder + "/OUTL_KernelPreset_" + kernelId + ".asset"));
        return preset;
    }

    private static OUTL_GenrePresetDef CreateCompatibilityPreset(string kernelId, string title, OUTL_GameLoopDef loop, OUTL_EntityRoleBinding[] roles)
    {
        OUTL_GenrePresetDef preset = ScriptableObject.CreateInstance<OUTL_GenrePresetDef>();
        preset.GenreId = kernelId; preset.DisplayName = title; preset.GameLoop = loop; preset.Roles = roles;
        AssetDatabase.CreateAsset(preset, AssetDatabase.GenerateUniqueAssetPath(Folder + "/OUTL_GenrePreset_" + kernelId + ".asset"));
        return preset;
    }

    private static void CreateRuntimeRig(OUTL_GameLoopDef loop, OUTL_EntityRoleBinding[] roles, OUTL_KernelPresetDef kernel, OUTL_GenrePresetDef compatibility)
    {
        GameObject runtime = GameObject.Find("OUTL_Runtime");
        if (runtime == null) { runtime = new GameObject("OUTL_Runtime"); Undo.RegisterCreatedObjectUndo(runtime, "Create OUTL Runtime"); }
        if (runtime.GetComponent<OUTL_World>() == null) runtime.AddComponent<OUTL_World>();
        if (runtime.GetComponent<OUTL_DevConsole>() == null) runtime.AddComponent<OUTL_DevConsole>();
        if (runtime.GetComponent<OUTL_PoolSystem>() == null) runtime.AddComponent<OUTL_PoolSystem>();
        OUTL_SaveSpawnResolverRegistry resolver = runtime.GetComponent<OUTL_SaveSpawnResolverRegistry>();
        if (resolver == null) resolver = runtime.AddComponent<OUTL_SaveSpawnResolverRegistry>();
        resolver.EntityDefs = ExtractEntityDefs(roles);
        EditorUtility.SetDirty(resolver);
        EnsureChunkRig(runtime);
        OUTL_GameLoopRunner runner = runtime.GetComponent<OUTL_GameLoopRunner>();
        if (runner == null) runner = runtime.AddComponent<OUTL_GameLoopRunner>();
        runner.GameLoop = loop;
        EditorUtility.SetDirty(runner);
        CreateLoopHUD(runner);
        Selection.activeGameObject = runtime;
        EditorGUIUtility.PingObject(runtime);
    }

    private static void EnsureChunkRig(GameObject runtime)
    {
        OUTL_ChunkProcessingDriver driver = runtime.GetComponent<OUTL_ChunkProcessingDriver>();
        if (driver == null) driver = runtime.AddComponent<OUTL_ChunkProcessingDriver>();
        driver.BuiltInPreset = OUTL_ProcessingBuiltInPreset.StreamingWorld;
        driver.ApplyPresetOnEnable = true;
        driver.CacheRegistrySnapshot = true;
        driver.ChunkSize = Mathf.Max(32f, driver.ChunkSize <= 0f ? 64f : driver.ChunkSize);
        OUTL_ChunkDebugView debug = runtime.GetComponent<OUTL_ChunkDebugView>();
        if (debug == null) debug = runtime.AddComponent<OUTL_ChunkDebugView>();
        debug.Driver = driver;
        debug.ChunkSize = driver.ChunkSize;
        EditorUtility.SetDirty(driver);
        EditorUtility.SetDirty(debug);
    }

    private static OUTL_EntityDef[] ExtractEntityDefs(OUTL_EntityRoleBinding[] roles)
    {
        if (roles == null) return new OUTL_EntityDef[0];
        OUTL_EntityDef[] defs = new OUTL_EntityDef[roles.Length];
        for (int i = 0; i < roles.Length; i++) defs[i] = roles[i] != null ? roles[i].EntityDef : null;
        return defs;
    }

    private static void CreateLoopHUD(OUTL_GameLoopRunner runner)
    {
        GameObject canvasGo = GameObject.Find("OUTL_KernelHUD_Canvas");
        if (canvasGo == null)
        {
            canvasGo = new GameObject("OUTL_KernelHUD_Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();
        }
        GameObject panel = new GameObject("OUTL_KernelLoopPanel");
        panel.transform.SetParent(canvasGo.transform, false);
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(1f, 1f); rt.anchoredPosition = new Vector2(-24f, -24f); rt.sizeDelta = new Vector2(360f, 260f);
        OUTL_UICollectionBinder binder = panel.AddComponent<OUTL_UICollectionBinder>();
        binder.GameLoop = runner; binder.Source = OUTL_UICollectionSource.ChallengeProgress; binder.MaxRows = 8;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
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