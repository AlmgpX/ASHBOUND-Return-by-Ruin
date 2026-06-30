#if UNITY_EDITOR
using System.Collections.Generic;
using Cinemachine;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class OUTL_FPSControllerPresetEditor
{
    private const string RootName = "OUTL_FPS_DEMO_ROOT";
    private const string RuntimeName = "OUTL_Runtime";
    private const string PresetFolder = "Assets/OUT/OUT_Core/OUT_CORE_Lite/Generated/FPSControllerPreset";
    private const string TickProfilePath = PresetFolder + "/OUTL_TickProfile_FPSDemo.asset";
    private const string LegacyCrosshairSpriteGuid = "4196c7ce35acd704aaf43e4e47b89c82";

    [MenuItem("OUT CORE Lite/Setup/Create OUTL FPS Controller Preset", priority = 20)]
    public static void CreateFPSControllerPreset()
    {
        GameObject root = EnsureRoot();
        OUTL_World world = EnsureRuntime(root.transform);
        GameObject player = CreateOrReplacePlayer(root.transform, new Vector3(0f, 1f, -6f));
        CreateOrReplaceTMPGui(root.transform, "OUTL_FPS_GUI",
            "OUTL FPS CONTROLLER\nWASD mouse | Space jump | Ctrl crouch | Shift walk\nE use | Ladders/ledges/long jump are in the movement demo");

        if (world != null) world.MaterializationFocusTargetName = "demo.player";
        Selection.activeGameObject = player;
        EditorGUIUtility.PingObject(player);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("OUT CORE Lite: OUTL_FPS_Controller preset created.", player);
    }

    [MenuItem("OUT CORE Lite/Setup/Create OUTL FPS Movement Test Demo", priority = 21)]
    public static void CreateFPSMovementTestDemo()
    {
        EnsureTag("Ladder");
        GameObject root = EnsureRoot();
        OUTL_World world = EnsureRuntime(root.transform);
        ClearChild(root.transform, "OUTL_FPS_TEST_GEOMETRY");
        GameObject geoRoot = new GameObject("OUTL_FPS_TEST_GEOMETRY");
        Undo.RegisterCreatedObjectUndo(geoRoot, "Create OUTL FPS Test Geometry");
        geoRoot.transform.SetParent(root.transform, false);

        GameObject player = CreateOrReplacePlayer(root.transform, new Vector3(0f, 1f, -8f));
        CreateGround(geoRoot.transform);
        CreateJumpBoxes(geoRoot.transform);
        CreateLongJumpLane(geoRoot.transform);
        CreateLadderRig(geoRoot.transform);
        CreateLedgeRig(geoRoot.transform);
        CreateCodeDoorRig(geoRoot.transform);
        CreateInteractableProps(geoRoot.transform);
        CreateOrReplaceTMPGui(root.transform, "OUTL_FPS_GUI",
            "OUTL FPS MOVEMENT DEMO\nWASD mouse | Space jump | Ctrl crouch | Shift walk | E use\nBoxes: jump/crouch/long jump. Ladder tag: Ladder. Ledge wall ahead.\nCode door: press panels 1 + 9 + 7, then the door opens through OUTL links.");

        if (world != null)
        {
            world.MaterializationFocusTargetName = "demo.player";
            OUTL_ChunkProcessingDriver chunks = world.GetComponent<OUTL_ChunkProcessingDriver>();
            if (chunks != null) chunks.Focus = player.transform;
        }

        Selection.activeGameObject = player;
        EditorGUIUtility.PingObject(player);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("OUT CORE Lite: OUTL_FPS_Controller movement test demo created.", root);
    }

    private static GameObject EnsureRoot()
    {
        GameObject root = GameObject.Find(RootName);
        if (root != null) return root;
        root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Create OUTL FPS Demo Root");
        return root;
    }

    private static OUTL_World EnsureRuntime(Transform parent)
    {
        OUTL_World world = Object.FindObjectOfType<OUTL_World>(true);
        if (world != null) return world;

        GameObject runtime = new GameObject(RuntimeName);
        Undo.RegisterCreatedObjectUndo(runtime, "Create OUTL Runtime");
        runtime.transform.SetParent(parent, false);

        world = Undo.AddComponent<OUTL_World>(runtime);
        world.TickProfile = EnsureTickProfile();
        world.ApplyTickProfileOnAwake = true;
        world.UpdateMode = OUTL_WorldUpdateMode.CustomFixedStep;
        world.SimulationStep = 0.02f;
        world.MaxSimulationStepsPerFrame = 4;
        world.AutoFindAdaptersOnStart = true;
        world.EnableAutomaticMaterialization = true;
        world.MaterializationTickInterval = 0.25f;
        world.MaterializeEnterDistance = 48f;
        world.DematerializeExitDistance = 80f;

        OUTL_PoolSystem pool = Undo.AddComponent<OUTL_PoolSystem>(runtime);
        pool.DefaultCapacity = 16;
        pool.MaxSize = 512;
        pool.CollectionChecks = false;

        OUTL_DevConsole console = Undo.AddComponent<OUTL_DevConsole>(runtime);
        console.ToggleKey = KeyCode.BackQuote;
        console.AlternateToggleKeys = new[] { KeyCode.F1, KeyCode.Backslash, KeyCode.Insert };
        console.PauseGameWhenOpen = true;
        console.UnlockCursorWhenOpen = true;
        console.RestoreCursorLockOnClose = true;

        Undo.AddComponent<OUTL_QuickSaveInput>(runtime);

        OUTL_ChunkProcessingDriver chunks = Undo.AddComponent<OUTL_ChunkProcessingDriver>(runtime);
        chunks.ChunkSize = 32f;
        chunks.EnforceCanonicalThreeByThree = true;
        chunks.FullRadius = 0;
        chunks.NearRadius = 1;
        chunks.MidRadius = 2;
        chunks.FarRadius = 3;
        chunks.OverrideDriverTickInterval = true;
        chunks.DriverTickInterval = 0.25f;
        chunks.OverrideEntitiesPerTick = true;
        chunks.EntitiesPerTick = 256;
        chunks.FocusTargetName = "demo.player";
        chunks.FocusClassName = "actor.player.fps";

        EditorUtility.SetDirty(runtime);
        return world;
    }

    private static OUTL_TickProfile EnsureTickProfile()
    {
        EnsureFolder(PresetFolder);
        OUTL_TickProfile profile = AssetDatabase.LoadAssetAtPath<OUTL_TickProfile>(TickProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<OUTL_TickProfile>();
            AssetDatabase.CreateAsset(profile, TickProfilePath);
        }

        profile.logicInterval = 0.02f;
        profile.chunkProcessingInterval = 0.25f;
        profile.npcFullInterval = 0.05f;
        profile.npcNearInterval = 0.25f;
        profile.npcMidInterval = 2f;
        profile.npcFarInterval = 10f;
        profile.npcDormantInterval = 60f;
        profile.Sanitize();
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static GameObject CreateOrReplacePlayer(Transform parent, Vector3 position)
    {
        ClearChild(parent, "OUTL_FPS_Player");

        GameObject player = new GameObject("OUTL_FPS_Player");
        Undo.RegisterCreatedObjectUndo(player, "Create OUTL FPS Player");
        player.transform.SetParent(parent, false);
        player.transform.position = position;
        player.tag = "Player";

        CharacterController controller = Undo.AddComponent<CharacterController>(player);
        controller.radius = 0.32f;
        controller.height = 1.8f;
        controller.center = new Vector3(0f, 0.9f, 0f);
        controller.stepOffset = 0.35f;
        controller.slopeLimit = 48f;
        controller.skinWidth = 0.045f;

        OUTL_EntityAdapter entity = Undo.AddComponent<OUTL_EntityAdapter>(player);
        entity.ClassNameOverride = "actor.player.fps";
        entity.TargetName = "demo.player";
        entity.StableId = "demo.player";
        entity.SavePersistent = true;
        entity.RegisterTick = true;
        entity.RegisterInSectors = true;

        GameObject view = new GameObject("ViewPitchRoot");
        Undo.RegisterCreatedObjectUndo(view, "Create OUTL FPS View");
        view.transform.SetParent(player.transform, false);
        view.transform.localPosition = new Vector3(0f, 1.62f, 0f);

        Camera camera = Undo.AddComponent<Camera>(view);
        camera.tag = "MainCamera";
        camera.fieldOfView = 75f;
        camera.nearClipPlane = 0.03f;
        Undo.AddComponent<AudioListener>(view);
        AudioSource bodySource = CreateAudioSource(player, "OUTL_Audio_Body", false);
        AudioSource footstepSource = CreateAudioSource(player, "OUTL_Audio_Footsteps", true);
        AudioSource jumpSource = CreateAudioSource(player, "OUTL_Audio_Jump", true);
        AudioSource landingSource = CreateAudioSource(player, "OUTL_Audio_Landing", true);
        AudioSource painSource = CreateAudioSource(player, "OUTL_Audio_Pain", false);
        AudioSource armorSource = CreateAudioSource(player, "OUTL_Audio_Armor", false);

        OUTL_PlayerInputSource input = Undo.AddComponent<OUTL_PlayerInputSource>(player);
        input.ViewCamera = camera;
        input.MouseSensitivity = 1f;

        OUTL_FPS_Controller fps = Undo.AddComponent<OUTL_FPS_Controller>(player);
        fps.Entity = entity;
        fps.Controller = controller;
        fps.YawRoot = player.transform;
        fps.ViewPitchRoot = view.transform;
        fps.ViewCamera = camera;
        fps.UseUnityInputFallback = true;
        fps.DisableFallbackWhenActorBridgePresent = true;
        fps.HasLongJumpModule = true;
        fps.RequireLongJumpModule = true;
        fps.UseDistance = 4f;
        fps.LadderTag = "Ladder";
        fps.EnableViewBob = true;
        fps.FootstepSource = footstepSource;
        fps.JumpSource = jumpSource;
        fps.LandingSource = landingSource;
        fps.SurfaceHazardSource = bodySource;

        OUTL_ActorControlBridge bridge = Undo.AddComponent<OUTL_ActorControlBridge>(player);
        bridge.Entity = entity;
        bridge.InputSourceBehaviour = input;
        bridge.InputSinkBehaviours = new Behaviour[] { fps };
        bridge.UseUnityUpdateForLocalInput = true;
        bridge.ApplyLocalPlayerEveryFrame = true;
        bridge.LocalPlayerUpdateMode = OUTL_ActorInputUpdateMode.FullAndNearActors;
        fps.ActorBridge = bridge;

        OUTL_Vitals vitals = Undo.AddComponent<OUTL_Vitals>(player);
        vitals.Entity = entity;
        vitals.HealthKey = "Health";
        vitals.MaxHealthKey = "MaxHealth";
        vitals.InitializeMissingStats = true;
        vitals.DefaultHealth = 100f;
        vitals.DefaultMaxHealth = 100f;

        OUTL_DamageReceiver damageReceiver = Undo.AddComponent<OUTL_DamageReceiver>(player);
        damageReceiver.Entity = entity;

        OUTL_PlayerArmorEnergy armorEnergy = Undo.AddComponent<OUTL_PlayerArmorEnergy>(player);
        armorEnergy.Entity = entity;
        armorEnergy.DefaultArmor = 25f;
        armorEnergy.DefaultMaxArmor = 100f;
        armorEnergy.DefaultEnergy = 100f;
        armorEnergy.DefaultMaxEnergy = 100f;

        OUTL_PlayerFeedback feedback = Undo.AddComponent<OUTL_PlayerFeedback>(player);
        feedback.Entity = entity;
        feedback.Controller = fps;
        feedback.BodySource = bodySource;
        feedback.PainSource = painSource;
        feedback.StrongPainSource = painSource;
        feedback.ArmorSource = armorSource;
        feedback.WalkImpulseSource = CreateImpulseSource(player, "OUTL_Impulse_Walk", new Vector3(0f, -0.18f, 0f), 0.06f);
        feedback.RunImpulseSource = CreateImpulseSource(player, "OUTL_Impulse_Run", new Vector3(0f, -0.28f, 0f), 0.07f);
        feedback.JumpImpulseSource = CreateImpulseSource(player, "OUTL_Impulse_Jump", new Vector3(0f, 0.45f, 0f), 0.12f);
        feedback.LandingImpulseSource = CreateImpulseSource(player, "OUTL_Impulse_Landing", new Vector3(0f, -0.85f, 0f), 0.15f);
        feedback.DamageImpulseSource = CreateImpulseSource(player, "OUTL_Impulse_Damage", new Vector3(0.35f, 0.2f, -0.15f), 0.12f);
        feedback.ExplosionImpulseSource = CreateImpulseSource(player, "OUTL_Impulse_Explosion", new Vector3(0f, 1.1f, -0.45f), 0.22f);
        fps.Feedback = feedback;

        entity.RebuildCommandReceiverCache();
        EditorUtility.SetDirty(entity);
        EditorUtility.SetDirty(fps);
        EditorUtility.SetDirty(bridge);
        EditorUtility.SetDirty(vitals);
        EditorUtility.SetDirty(damageReceiver);
        EditorUtility.SetDirty(armorEnergy);
        EditorUtility.SetDirty(feedback);
        return player;
    }

    private static AudioSource CreateAudioSource(GameObject owner, string name, bool spatial)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(owner.transform, false);
        AudioSource source = Undo.AddComponent<AudioSource>(go);
        source.playOnAwake = false;
        source.spatialBlend = spatial ? 1f : 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 1f;
        source.maxDistance = 24f;
        return source;
    }

    private static CinemachineImpulseSource CreateImpulseSource(GameObject owner, string name, Vector3 velocity, float duration)
    {
        CinemachineImpulseSource source = Undo.AddComponent<CinemachineImpulseSource>(owner);
        source.m_DefaultVelocity = velocity;
        source.m_ImpulseDefinition.m_ImpulseDuration = Mathf.Max(0.01f, duration);
        source.m_ImpulseDefinition.m_ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
        return source;
    }

    private static void CreateGround(Transform parent)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(ground, "Create OUTL FPS Demo Ground");
        ground.name = "OUTL_FPS_Ground";
        ground.transform.SetParent(parent, false);
        ground.transform.localPosition = new Vector3(0f, -0.05f, 10f);
        ground.transform.localScale = new Vector3(34f, 0.1f, 42f);
        SetPreviewColor(ground, new Color(0.18f, 0.2f, 0.18f, 1f));
    }

    private static void CreateJumpBoxes(Transform parent)
    {
        CreateBox(parent, "JumpBox_0_5m", new Vector3(-5f, 0.25f, -1f), new Vector3(1.5f, 0.5f, 1.5f), new Color(0.30f, 0.42f, 0.9f, 1f));
        CreateBox(parent, "JumpBox_1_0m", new Vector3(-5f, 0.5f, 2f), new Vector3(1.5f, 1f, 1.5f), new Color(0.25f, 0.55f, 0.95f, 1f));
        CreateBox(parent, "JumpBox_1_5m", new Vector3(-5f, 0.75f, 5.5f), new Vector3(1.5f, 1.5f, 1.5f), new Color(0.2f, 0.7f, 1f, 1f));
        CreateBox(parent, "CrouchTunnel", new Vector3(-1.5f, 1.25f, 2.5f), new Vector3(3.5f, 0.35f, 4f), new Color(0.45f, 0.25f, 0.9f, 1f));
    }

    private static void CreateLongJumpLane(Transform parent)
    {
        CreateBox(parent, "LongJump_Start", new Vector3(3.5f, 0.05f, -2f), new Vector3(2.5f, 0.1f, 2f), new Color(0.1f, 0.6f, 0.2f, 1f));
        CreateBox(parent, "LongJump_GapMarker", new Vector3(3.5f, 0.03f, 2f), new Vector3(2.5f, 0.06f, 3f), new Color(0.6f, 0.1f, 0.1f, 1f));
        CreateBox(parent, "LongJump_Landing", new Vector3(3.5f, 0.25f, 6f), new Vector3(2.5f, 0.5f, 2.5f), new Color(0.1f, 0.75f, 0.25f, 1f));
    }

    private static void CreateLadderRig(Transform parent)
    {
        GameObject wall = CreateBox(parent, "Ladder_BackWall", new Vector3(8f, 1.75f, 1f), new Vector3(0.35f, 3.5f, 2.5f), new Color(0.45f, 0.45f, 0.45f, 1f));
        GameObject ladder = CreateBox(parent, "Ladder_UseMe_Tagged", new Vector3(7.78f, 1.6f, 1f), new Vector3(0.08f, 3.2f, 1.1f), new Color(0.9f, 0.75f, 0.2f, 1f));
        ladder.tag = "Ladder";
        Collider wallCollider = wall.GetComponent<Collider>();
        if (wallCollider != null) wallCollider.isTrigger = false;
    }

    private static void CreateLedgeRig(Transform parent)
    {
        CreateBox(parent, "LedgeGrab_Wall", new Vector3(0f, 1.25f, 12f), new Vector3(4f, 2.5f, 0.45f), new Color(0.75f, 0.35f, 0.18f, 1f));
        CreateBox(parent, "LedgeGrab_TopPlatform", new Vector3(0f, 2.55f, 13.4f), new Vector3(4f, 0.25f, 2.5f), new Color(0.9f, 0.48f, 0.22f, 1f));
    }

    private static void CreateCodeDoorRig(Transform parent)
    {
        string gateTarget = "demo.code.gate.197";
        string doorTarget = "demo.code.door";

        GameObject door = CreateBox(parent, "OUTL_CodeDoor_197", new Vector3(-8f, 1.3f, 10f), new Vector3(2.2f, 2.6f, 0.28f), new Color(1f, 0.45f, 0.1f, 1f));
        OUTL_EntityAdapter doorEntity = Undo.AddComponent<OUTL_EntityAdapter>(door);
        doorEntity.ClassNameOverride = "object.door.code";
        doorEntity.TargetName = doorTarget;
        doorEntity.StableId = doorTarget;
        OUTL_Door doorLogic = Undo.AddComponent<OUTL_Door>(door);
        doorLogic.Entity = doorEntity;
        doorLogic.DoorRoot = door.transform;
        doorLogic.ClosedLocalPosition = door.transform.localPosition;
        doorLogic.OpenLocalPosition = door.transform.localPosition + Vector3.up * 2.8f;
        doorLogic.ToggleMode = false;
        doorLogic.AutoClose = false;
        doorLogic.CheckBlockers = true;
        doorLogic.IgnoreCommandsWhileMoving = true;
        doorLogic.OnlyBlockActorsAndPhysics = true;
        doorLogic.CrushDamage = 8f;
        doorEntity.RebuildCommandReceiverCache();

        GameObject gateGo = new GameObject("OUTL_CodeGate_AND_197");
        Undo.RegisterCreatedObjectUndo(gateGo, "Create OUTL Code Gate");
        gateGo.transform.SetParent(parent, false);
        gateGo.transform.localPosition = new Vector3(-8f, 0.2f, 8.2f);
        OUTL_EntityAdapter gateEntity = Undo.AddComponent<OUTL_EntityAdapter>(gateGo);
        gateEntity.ClassNameOverride = "logic.gate.code";
        gateEntity.TargetName = gateTarget;
        gateEntity.StableId = gateTarget;
        OUTL_LogicGate gate = Undo.AddComponent<OUTL_LogicGate>(gateGo);
        gate.Entity = gateEntity;
        gate.Mode = OUTL_BooleanGateMode.And;
        gate.InputCount = 3;
        gate.Inputs = new bool[3];
        gate.Outputs = new[]
        {
            new OUTL_OutputLink { EventName = "OnTrue", TargetName = doorTarget, Command = OUTL_CommandType.Open },
            new OUTL_OutputLink { EventName = "OnFalse", TargetName = doorTarget, Command = OUTL_CommandType.Close }
        };
        gateEntity.RebuildCommandReceiverCache();

        CreateCodeButton(parent, "CodePanel_1", "1", new Vector3(-9.2f, 1.1f, 8f), gateTarget, 0);
        CreateCodeButton(parent, "CodePanel_9", "9", new Vector3(-8f, 1.1f, 8f), gateTarget, 1);
        CreateCodeButton(parent, "CodePanel_7", "7", new Vector3(-6.8f, 1.1f, 8f), gateTarget, 2);
    }

    private static void CreateCodeButton(Transform parent, string objectName, string label, Vector3 position, string gateTarget, int inputIndex)
    {
        GameObject go = CreateBox(parent, objectName, position, new Vector3(0.65f, 0.65f, 0.15f), new Color(0.12f, 0.12f, 0.12f, 1f));
        OUTL_EntityAdapter entity = Undo.AddComponent<OUTL_EntityAdapter>(go);
        entity.ClassNameOverride = "logic.button.code";
        entity.TargetName = "demo.code.button." + label;
        entity.StableId = entity.TargetName;

        OUTL_Button button = Undo.AddComponent<OUTL_Button>(go);
        button.Entity = entity;
        button.Toggle = true;
        button.OverrideOutputIntWithState = false;
        button.PressedLocalOffset = new Vector3(0f, 0f, -0.06f);
        button.Outputs = new[]
        {
            new OUTL_OutputLink { EventName = "OnPressed", TargetName = gateTarget, Command = OUTL_CommandType.Activate, IntValue = inputIndex },
            new OUTL_OutputLink { EventName = "OnReleased", TargetName = gateTarget, Command = OUTL_CommandType.Deactivate, IntValue = inputIndex }
        };

        OUTL_Interactable interactable = Undo.AddComponent<OUTL_Interactable>(go);
        interactable.Entity = entity;
        interactable.DisplayName = "Code Panel " + label;
        interactable.DescriptionEn = "Toggle code digit " + label;
        interactable.DescriptionRu = "Кнопка кода " + label;
        interactable.Command = OUTL_CommandType.Use;
        entity.RebuildCommandReceiverCache();

        GameObject textGo = new GameObject("TMP_Label_" + label);
        Undo.RegisterCreatedObjectUndo(textGo, "Create Code Button TMP Label");
        textGo.transform.SetParent(go.transform, false);
        textGo.transform.localPosition = new Vector3(0f, 0f, -0.09f);
        textGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        TextMeshPro tmp = Undo.AddComponent<TextMeshPro>(textGo);
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 4f;
        tmp.color = Color.green;
        tmp.rectTransform.sizeDelta = new Vector2(1f, 1f);
    }

    private static void CreateInteractableProps(Transform parent)
    {
        CreateToggleProp(parent, "OUTL_Interactable_TestCube", new Vector3(6f, 0.5f, 7f), "Test Cube", "Toggle me with E");
        CreateToggleProp(parent, "OUTL_Interactable_Terminal", new Vector3(6f, 0.75f, 9f), "Training Terminal", "E toggles this OUTL_Button prop");
    }

    private static void CreateToggleProp(Transform parent, string name, Vector3 position, string displayName, string desc)
    {
        GameObject go = CreateBox(parent, name, position, new Vector3(0.9f, 0.9f, 0.9f), new Color(0.1f, 0.65f, 0.65f, 1f));
        OUTL_EntityAdapter entity = Undo.AddComponent<OUTL_EntityAdapter>(go);
        entity.ClassNameOverride = "object.interactable.demo";
        entity.TargetName = "demo.interactable." + name.ToLowerInvariant();
        entity.StableId = entity.TargetName;
        OUTL_Button button = Undo.AddComponent<OUTL_Button>(go);
        button.Entity = entity;
        button.Toggle = true;
        button.PressedLocalOffset = new Vector3(0f, -0.12f, 0f);
        OUTL_Interactable interactable = Undo.AddComponent<OUTL_Interactable>(go);
        interactable.Entity = entity;
        interactable.DisplayName = displayName;
        interactable.DescriptionEn = desc;
        interactable.DescriptionRu = desc;
        interactable.Command = OUTL_CommandType.Use;
        entity.RebuildCommandReceiverCache();
    }

    private static GameObject CreateBox(Transform parent, string name, Vector3 localPosition, Vector3 scale, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = scale;
        SetPreviewColor(go, color);
        return go;
    }

    private static void CreateOrReplaceTMPGui(Transform parent, string name, string text)
    {
        ClearChild(parent, name);

        GameObject canvasGo = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create OUTL FPS TMP GUI");
        canvasGo.transform.SetParent(parent, false);
        Canvas canvas = Undo.AddComponent<Canvas>(canvasGo);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = Undo.AddComponent<CanvasScaler>(canvasGo);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        Undo.AddComponent<GraphicRaycaster>(canvasGo);

        GameObject infoGo = new GameObject("TMP_Info");
        Undo.RegisterCreatedObjectUndo(infoGo, "Create OUTL FPS TMP Info");
        infoGo.transform.SetParent(canvasGo.transform, false);
        TextMeshProUGUI info = Undo.AddComponent<TextMeshProUGUI>(infoGo);
        info.text = text;
        info.fontSize = 24f;
        info.color = new Color(0.85f, 1f, 0.75f, 1f);
        info.alignment = TextAlignmentOptions.TopLeft;
        RectTransform infoRect = info.rectTransform;
        infoRect.anchorMin = new Vector2(0f, 1f);
        infoRect.anchorMax = new Vector2(0f, 1f);
        infoRect.pivot = new Vector2(0f, 1f);
        infoRect.anchoredPosition = new Vector2(24f, -24f);
        infoRect.sizeDelta = new Vector2(1100f, 220f);

        GameObject crossGo = new GameObject("TMP_Crosshair");
        Undo.RegisterCreatedObjectUndo(crossGo, "Create OUTL FPS TMP Crosshair");
        crossGo.transform.SetParent(canvasGo.transform, false);
        TextMeshProUGUI cross = Undo.AddComponent<TextMeshProUGUI>(crossGo);
        cross.text = "+";
        cross.fontSize = 28f;
        cross.color = new Color(1f, 1f, 1f, 0.7f);
        cross.alignment = TextAlignmentOptions.Center;
        RectTransform crossRect = cross.rectTransform;
        crossRect.anchorMin = new Vector2(0.5f, 0.5f);
        crossRect.anchorMax = new Vector2(0.5f, 0.5f);
        crossRect.pivot = new Vector2(0.5f, 0.5f);
        crossRect.anchoredPosition = Vector2.zero;
        crossRect.sizeDelta = new Vector2(64f, 64f);

        GameObject crossImageGo = new GameObject("IMG_CrosshairAnimated");
        Undo.RegisterCreatedObjectUndo(crossImageGo, "Create OUTL FPS Animated Crosshair");
        crossImageGo.transform.SetParent(canvasGo.transform, false);
        Image crossImage = Undo.AddComponent<Image>(crossImageGo);
        crossImage.raycastTarget = false;
        crossImage.preserveAspect = true;
        RectTransform crossImageRect = crossImage.rectTransform;
        crossImageRect.anchorMin = new Vector2(0.5f, 0.5f);
        crossImageRect.anchorMax = new Vector2(0.5f, 0.5f);
        crossImageRect.pivot = new Vector2(0.5f, 0.5f);
        crossImageRect.anchoredPosition = Vector2.zero;
        crossImageRect.sizeDelta = new Vector2(64f, 64f);
        Sprite idleCrosshair;
        Sprite[] crosshairFrames = LoadLegacyCrosshairFrames(out idleCrosshair);
        OUTL_FPSCrosshairAnimator crosshairAnimator = Undo.AddComponent<OUTL_FPSCrosshairAnimator>(crossImageGo);
        crosshairAnimator.TargetImage = crossImage;
        crosshairAnimator.FallbackText = cross;
        crosshairAnimator.IdleSprite = idleCrosshair;
        crosshairAnimator.ActiveFrames = crosshairFrames;
        crosshairAnimator.FrameRate = 10f;
        crosshairAnimator.FadeSpeed = 4f;
        crosshairAnimator.InactiveColor = new Color(0.4245283f, 0.4245283f, 0.4245283f, 0.6117647f);
        crosshairAnimator.ActiveColor = new Color(1f, 1f, 1f, 0.9490196f);
        if (idleCrosshair != null) crossImage.sprite = idleCrosshair;

        GameObject healthGo = new GameObject("TMP_Health");
        Undo.RegisterCreatedObjectUndo(healthGo, "Create OUTL FPS TMP Health");
        healthGo.transform.SetParent(canvasGo.transform, false);
        TextMeshProUGUI health = Undo.AddComponent<TextMeshProUGUI>(healthGo);
        health.text = "HP -- / --";
        health.fontSize = 30f;
        health.color = new Color(1f, 0.72f, 0.25f, 1f);
        health.alignment = TextAlignmentOptions.BottomLeft;
        RectTransform healthRect = health.rectTransform;
        healthRect.anchorMin = new Vector2(0f, 0f);
        healthRect.anchorMax = new Vector2(0f, 0f);
        healthRect.pivot = new Vector2(0f, 0f);
        healthRect.anchoredPosition = new Vector2(24f, 24f);
        healthRect.sizeDelta = new Vector2(720f, 64f);

        GameObject armorGo = new GameObject("TMP_Armor");
        Undo.RegisterCreatedObjectUndo(armorGo, "Create OUTL FPS TMP Armor");
        armorGo.transform.SetParent(canvasGo.transform, false);
        TextMeshProUGUI armor = Undo.AddComponent<TextMeshProUGUI>(armorGo);
        armor.text = "AR -- / --";
        armor.fontSize = 24f;
        armor.color = new Color(0.45f, 0.75f, 1f, 1f);
        armor.alignment = TextAlignmentOptions.BottomLeft;
        RectTransform armorRect = armor.rectTransform;
        armorRect.anchorMin = new Vector2(0f, 0f);
        armorRect.anchorMax = new Vector2(0f, 0f);
        armorRect.pivot = new Vector2(0f, 0f);
        armorRect.anchoredPosition = new Vector2(24f, 78f);
        armorRect.sizeDelta = new Vector2(320f, 44f);

        GameObject energyGo = new GameObject("TMP_Energy");
        Undo.RegisterCreatedObjectUndo(energyGo, "Create OUTL FPS TMP Energy");
        energyGo.transform.SetParent(canvasGo.transform, false);
        TextMeshProUGUI energy = Undo.AddComponent<TextMeshProUGUI>(energyGo);
        energy.text = "EN -- / --";
        energy.fontSize = 24f;
        energy.color = new Color(0.65f, 1f, 0.75f, 1f);
        energy.alignment = TextAlignmentOptions.BottomLeft;
        RectTransform energyRect = energy.rectTransform;
        energyRect.anchorMin = new Vector2(0f, 0f);
        energyRect.anchorMax = new Vector2(0f, 0f);
        energyRect.pivot = new Vector2(0f, 0f);
        energyRect.anchoredPosition = new Vector2(24f, 118f);
        energyRect.sizeDelta = new Vector2(320f, 44f);

        GameObject promptGo = new GameObject("TMP_UsePrompt");
        Undo.RegisterCreatedObjectUndo(promptGo, "Create OUTL FPS TMP Use Prompt");
        promptGo.transform.SetParent(canvasGo.transform, false);
        TextMeshProUGUI prompt = Undo.AddComponent<TextMeshProUGUI>(promptGo);
        prompt.text = string.Empty;
        prompt.fontSize = 26f;
        prompt.color = new Color(1f, 0.95f, 0.55f, 1f);
        prompt.alignment = TextAlignmentOptions.Center;
        RectTransform promptRect = prompt.rectTransform;
        promptRect.anchorMin = new Vector2(0.5f, 0.5f);
        promptRect.anchorMax = new Vector2(0.5f, 0.5f);
        promptRect.pivot = new Vector2(0.5f, 0.5f);
        promptRect.anchoredPosition = new Vector2(0f, -92f);
        promptRect.sizeDelta = new Vector2(740f, 96f);

        GameObject eventGo = new GameObject("TMP_LastFeedbackEvent");
        Undo.RegisterCreatedObjectUndo(eventGo, "Create OUTL FPS TMP Last Event");
        eventGo.transform.SetParent(canvasGo.transform, false);
        TextMeshProUGUI eventText = Undo.AddComponent<TextMeshProUGUI>(eventGo);
        eventText.text = string.Empty;
        eventText.fontSize = 22f;
        eventText.color = new Color(1f, 0.42f, 0.32f, 1f);
        eventText.alignment = TextAlignmentOptions.TopRight;
        RectTransform eventRect = eventText.rectTransform;
        eventRect.anchorMin = new Vector2(1f, 1f);
        eventRect.anchorMax = new Vector2(1f, 1f);
        eventRect.pivot = new Vector2(1f, 1f);
        eventRect.anchoredPosition = new Vector2(-24f, -24f);
        eventRect.sizeDelta = new Vector2(520f, 80f);

        Image damageOverlay = CreateOverlay(canvasGo.transform, "DamageOverlay", new Color(1f, 0f, 0f, 0f), out CanvasGroup damageGroup);
        Image healOverlay = CreateOverlay(canvasGo.transform, "HealOverlay", new Color(0.1f, 1f, 0.25f, 0f), out CanvasGroup healGroup);

        OUTL_FPSHudTMP hud = Undo.AddComponent<OUTL_FPSHudTMP>(canvasGo);
        GameObject player = GameObject.Find("OUTL_FPS_Player");
        if (player != null)
        {
            hud.Target = player.GetComponent<OUTL_EntityAdapter>();
            hud.Vitals = player.GetComponent<OUTL_Vitals>();
            hud.Controller = player.GetComponent<OUTL_FPS_Controller>();
            crosshairAnimator.Controller = hud.Controller;
            OUTL_PlayerFeedback feedback = player.GetComponent<OUTL_PlayerFeedback>();
            if (feedback != null)
            {
                feedback.DamageOverlay = damageOverlay;
                feedback.DamageOverlayGroup = damageGroup;
                feedback.HealOverlay = healOverlay;
                feedback.HealOverlayGroup = healGroup;
                feedback.LastEventText = eventText;
                EditorUtility.SetDirty(feedback);
            }
        }
        hud.HealthText = health;
        hud.ArmorText = armor;
        hud.EnergyText = energy;
        hud.UsePromptText = prompt;
        EditorUtility.SetDirty(crosshairAnimator);

        if (Object.FindObjectOfType<EventSystem>(true) == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
            Undo.AddComponent<EventSystem>(eventSystem);
            Undo.AddComponent<StandaloneInputModule>(eventSystem);
        }
    }

    private static Image CreateOverlay(Transform parent, string name, Color color, out CanvasGroup group)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create OUTL FPS " + name);
        go.transform.SetParent(parent, false);
        go.transform.SetAsFirstSibling();
        Image image = Undo.AddComponent<Image>(go);
        image.color = color;
        image.raycastTarget = false;
        group = Undo.AddComponent<CanvasGroup>(go);
        group.alpha = 0f;
        group.blocksRaycasts = false;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return image;
    }

    private static Sprite[] LoadLegacyCrosshairFrames(out Sprite idle)
    {
        idle = null;
        string path = AssetDatabase.GUIDToAssetPath(LegacyCrosshairSpriteGuid);
        if (string.IsNullOrEmpty(path))
            return new Sprite[0];

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        List<Sprite> sprites = new List<Sprite>(8);
        for (int i = 0; i < assets.Length; i++)
        {
            Sprite sprite = assets[i] as Sprite;
            if (sprite != null) sprites.Add(sprite);
        }

        if (sprites.Count == 0)
        {
            Sprite single = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (single != null) sprites.Add(single);
        }

        if (sprites.Count > 0)
            idle = sprites[Mathf.Min(3, sprites.Count - 1)];

        return sprites.ToArray();
    }

    private static void SetPreviewColor(GameObject go, Color color)
    {
        Renderer renderer = go != null ? go.GetComponent<Renderer>() : null;
        if (renderer == null) return;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return;
        Material material = new Material(shader);
        material.name = go.name + "_Mat";
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        renderer.sharedMaterial = material;
    }

    private static void ClearChild(Transform parent, string childName)
    {
        if (parent == null) return;
        Transform child = parent.Find(childName);
        if (child != null) Undo.DestroyObjectImmediate(child.gameObject);
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static void EnsureTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return;
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
    }
}
#endif
