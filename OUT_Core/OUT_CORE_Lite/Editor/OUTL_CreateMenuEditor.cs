#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class OUTL_CreateMenuEditor
{
    private const string MenuRoot = "OUT CORE Lite/Legacy Demo/Create/";
    private const string WorkbenchRoot = "OUT CORE Lite/Advanced/Samples/";
    private const string Folder = "Assets/OUT/OUT_Core/OUT_CORE_Lite/Templates/Create";

    // [MenuItem(MenuRoot + "00 World/Create Runtime Root")]
    public static void CreateRuntimeRoot()
    {
        GameObject root = GameObject.Find("OUTL_Runtime");
        if (root == null)
        {
            root = new GameObject("OUTL_Runtime");
            Undo.RegisterCreatedObjectUndo(root, "Create OUTL Runtime Root");
        }
        if (root.GetComponent<OUTL_World>() == null) Undo.AddComponent<OUTL_World>(root);
        if (root.GetComponent<OUTL_DevConsole>() == null) Undo.AddComponent<OUTL_DevConsole>(root);
        if (root.GetComponent<OUTL_PoolSystem>() == null) Undo.AddComponent<OUTL_PoolSystem>(root);
        Selection.activeGameObject = root;
    }

    // [MenuItem(MenuRoot + "00 World/Create Basic Test Room")]
    public static void CreateBasicTestRoom()
    {
        CreateRuntimeRoot();
        EnsureLayer("Player");
        EnsureLayer("PhysicalProps");
        EnsureLayer("HandledProps");

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "OUTL_TestRoom_Floor";
        floor.transform.position = new Vector3(0f, -0.05f, 0f);
        floor.transform.localScale = new Vector3(18f, 0.1f, 18f);
        Undo.RegisterCreatedObjectUndo(floor, "Create OUTL Test Room Floor");

        CreateWall("OUTL_TestRoom_Wall_N", new Vector3(0f, 1.5f, 9f), new Vector3(18f, 3f, 0.25f));
        CreateWall("OUTL_TestRoom_Wall_S", new Vector3(0f, 1.5f, -9f), new Vector3(18f, 3f, 0.25f));
        CreateWall("OUTL_TestRoom_Wall_E", new Vector3(9f, 1.5f, 0f), new Vector3(0.25f, 3f, 18f));
        CreateWall("OUTL_TestRoom_Wall_W", new Vector3(-9f, 1.5f, 0f), new Vector3(0.25f, 3f, 18f));

        GameObject player = CreateBasicPlayerWithHUDObject(new Vector3(0f, 1f, -5f));
        GameObject enemy = CreateBasicEnemyObject(new Vector3(0f, 1f, 4f));
        GameObject npc = CreateBasicNPCObject(new Vector3(-3f, 1f, 3f));
        GameObject door = CreateBasicDoorObject(new Vector3(3f, 1.2f, 5f));
        GameObject button = CreateBasicButtonObject(new Vector3(1.6f, 0.35f, 2.2f));
        GameObject chest = CreateBasicChestObject(new Vector3(-3f, 0.45f, -1.5f));
        CreateBasicPatrolPointAt(new Vector3(2f, 0.25f, 1f));
        CreateBasicCoverPointAt(new Vector3(-2f, 0.5f, 1.5f));

        Selection.objects = new Object[] { player, enemy, npc, door, button, chest };
        AssetDatabase.SaveAssets();
    }

    // [MenuItem(MenuRoot + "10 Actors/Create Basic Player + HUD")]
    public static void CreateBasicPlayerWithHUD()
    {
        EnsureFolder(Folder);
        EnsureLayer("Player");
        Selection.activeGameObject = CreateBasicPlayerWithHUDObject(Vector3.up);
        AssetDatabase.SaveAssets();
    }

    // [MenuItem(MenuRoot + "10 Actors/Create Basic NPC")]
    public static void CreateBasicNPC()
    {
        EnsureFolder(Folder);
        Selection.activeGameObject = CreateBasicNPCObject(Vector3.up);
        AssetDatabase.SaveAssets();
    }

    // [MenuItem(MenuRoot + "10 Actors/Create Basic Enemy")]
    public static void CreateBasicEnemy()
    {
        EnsureFolder(Folder);
        Selection.activeGameObject = CreateBasicEnemyObject(Vector3.up);
        AssetDatabase.SaveAssets();
    }

    // [MenuItem(MenuRoot + "20 Objects/Create Basic Chest")]
    public static void CreateBasicChest()
    {
        EnsureFolder(Folder);
        Selection.activeGameObject = CreateBasicChestObject(Vector3.zero);
        AssetDatabase.SaveAssets();
    }

    // [MenuItem(MenuRoot + "30 Logic/Create Basic Door")]
    public static void CreateBasicDoor()
    {
        EnsureFolder(Folder);
        Selection.activeGameObject = CreateBasicDoorObject(Vector3.zero);
        AssetDatabase.SaveAssets();
    }

    // [MenuItem(MenuRoot + "30 Logic/Create Basic Button")]
    public static void CreateBasicButton()
    {
        EnsureFolder(Folder);
        Selection.activeGameObject = CreateBasicButtonObject(Vector3.zero);
        AssetDatabase.SaveAssets();
    }

    [MenuItem(WorkbenchRoot + "Create Door Button Logic Example")]
    public static void CreateDoorButtonLogicExample()
    {
        EnsureFolder(Folder);
        CreateRuntimeRoot();

        GameObject root = new GameObject("OUTL_DoorButtonLogic_Example");
        Undo.RegisterCreatedObjectUndo(root, "Create OUTL Door Button Logic Example");
        string suffix = Mathf.Abs(root.GetInstanceID()).ToString();
        string buttonATarget = "logic.example.button.a." + suffix;
        string buttonBTarget = "logic.example.button.b." + suffix;
        string gateTarget = "logic.example.gate.and." + suffix;
        string doorTarget = "logic.example.door." + suffix;

        GameObject door = CreateBasicDoorObject(new Vector3(0f, 1.2f, 4f));
        door.name = "OUTL_LogicExample_Door";
        ParentForWorkbench(root, door);
        ConfigureEntityAddress(door, "logic_door", doorTarget, doorTarget);
        OUTL_Door doorLogic = door.GetComponent<OUTL_Door>();
        if (doorLogic != null)
        {
            doorLogic.ToggleMode = false;
            doorLogic.AutoClose = false;
            doorLogic.OpenLocalPosition = door.transform.localPosition + Vector3.up * 2.5f;
            EditorUtility.SetDirty(doorLogic);
        }

        GameObject gate = CreateLogicGateObject(new Vector3(0f, 0.45f, 1.9f), gateTarget, doorTarget);
        ParentForWorkbench(root, gate);

        GameObject buttonA = CreateLogicExampleButton(new Vector3(-1.2f, 0.2f, 0f), "OUTL_LogicExample_Button_A", buttonATarget, gateTarget, 0);
        GameObject buttonB = CreateLogicExampleButton(new Vector3(1.2f, 0.2f, 0f), "OUTL_LogicExample_Button_B", buttonBTarget, gateTarget, 1);
        ParentForWorkbench(root, buttonA);
        ParentForWorkbench(root, buttonB);

        Selection.objects = new Object[] { root, buttonA, buttonB, gate, door };
        AssetDatabase.SaveAssets();
        Debug.Log("OUTL door/button/logic example created. Use both toggle buttons to open the door; release either to close it. Wiring uses OutputLink -> TargetName -> CommandSystem.");
    }

    [MenuItem(WorkbenchRoot + "Create Key Door Access Example")]
    public static void CreateKeyDoorAccessExample()
    {
        EnsureFolder(Folder);
        CreateRuntimeRoot();

        GameObject root = new GameObject("OUTL_KeyDoorAccess_Example");
        Undo.RegisterCreatedObjectUndo(root, "Create OUTL Key Door Access Example");
        string suffix = Mathf.Abs(root.GetInstanceID()).ToString();
        string keyId = "key.access.red." + suffix;
        string doorTarget = "access.example.door." + suffix;

        OUTL_ItemDef keyDef = CreateItemDef(keyId, "Red Access Key", new[] { "Item", "Key", "Access.Red" }, 1);

        GameObject source = CreateAccessSourceOnlyObject(new Vector3(-2.5f, 1f, -3.5f), keyDef, "access.example.source." + suffix);
        ParentForWorkbench(root, source);

        GameObject key = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        key.name = "OUTL_KeyDoorExample_RedKey";
        key.transform.position = new Vector3(-1.5f, 0.35f, -0.5f);
        key.transform.localScale = new Vector3(0.25f, 0.08f, 0.25f);
        Undo.RegisterCreatedObjectUndo(key, "Create OUTL Red Access Key");
        SetPreviewColor(key, new Color(1f, 0.12f, 0.08f, 1f));
        ParentForWorkbench(root, key);
        OUTL_EntityAdapter keyEntity = key.AddComponent<OUTL_EntityAdapter>();
        keyEntity.Def = keyDef;
        keyEntity.ClassNameOverride = keyId;
        keyEntity.TargetName = "access.example.key." + suffix;
        keyEntity.StableId = keyEntity.TargetName;
        OUTL_ItemPickup pickup = key.AddComponent<OUTL_ItemPickup>();
        pickup.Entity = keyEntity;
        pickup.Item = keyDef;
        pickup.Count = 1;
        pickup.PickupKey = keyId;
        OUTL_Interactable keyInteractable = key.AddComponent<OUTL_Interactable>();
        keyInteractable.Entity = keyEntity;
        keyInteractable.Command = OUTL_CommandType.Pickup;
        keyInteractable.DisplayName = "Red Access Key";
        keyInteractable.DescriptionEn = "Pick up the red key";
        keyInteractable.DescriptionRu = "Взять красный ключ";

        GameObject door = CreateAccessDoorObject(new Vector3(0f, 1.2f, 3f));
        door.name = "OUTL_KeyDoorExample_LockedDoor";
        ParentForWorkbench(root, door);
        ConfigureEntityAddress(door, "access_door", doorTarget, doorTarget);
        OUTL_Door doorLogic = door.GetComponent<OUTL_Door>();
        doorLogic.ToggleMode = false;
        doorLogic.AutoClose = false;
        doorLogic.OpenLocalPosition = door.transform.localPosition + Vector3.up * 2.5f;
        OUTL_AccessController access = door.AddComponent<OUTL_AccessController>();
        access.Entity = door.GetComponent<OUTL_EntityAdapter>();
        access.StartsLocked = true;
        access.IsLocked = true;
        access.UnlockPermanentlyOnGrant = true;
        access.ConsumePolicy = OUTL_AccessConsumePolicy.Never;
        access.Requirements = new[]
        {
            new OUTL_AccessRequirement
            {
                RequirementId = keyId,
                Condition = new OUTL_ConditionDef
                {
                    Op = OUTL_ConditionOp.HasItem,
                    Subject = OUTL_ConditionSubject.Source,
                    ItemDef = keyDef,
                    IntValue = 1
                }
            }
        };
        door.GetComponent<OUTL_EntityAdapter>().RebuildCommandReceiverCache();
        door.GetComponent<OUTL_EntityAdapter>().RebuildCommandGuardCache();

        Selection.objects = new Object[] { root, source, key, door };
        AssetDatabase.SaveAssets();
        Debug.Log("OUTL key-door example created. No player prefab is spawned: SOURCE_ONLY is just an inventory command source. Pick up the red key, then Use the locked door. Access is guarded at CommandSystem level; the key is retained Doom-style.");
    }

    // [MenuItem(MenuRoot + "50 AI/Create Basic Patrol Point")]
    public static void CreateBasicPatrolPoint()
    {
        Selection.activeGameObject = CreateBasicPatrolPointAt(Vector3.zero);
    }

    // [MenuItem(MenuRoot + "50 AI/Create Basic Cover Point")]
    public static void CreateBasicCoverPoint()
    {
        Selection.activeGameObject = CreateBasicCoverPointAt(Vector3.zero);
    }

    private static GameObject CreateBasicPlayerWithHUDObject(Vector3 position)
    {
        GameObject player = CreateBasicPlayerObject(position);
        OUTL_EntityAdapter entity = player.GetComponent<OUTL_EntityAdapter>();
        OUTL_BasicPlayerController controller = player.GetComponent<OUTL_BasicPlayerController>();
        Camera camera = player.GetComponentInChildren<Camera>();
        CreatePlayerHUD(player, entity, controller, camera);
        return player;
    }

    private static GameObject CreateBasicPlayerObject(Vector3 position)
    {
        EnsureFolder(Folder);
        int playerLayer = EnsureLayer("Player");
        OUTL_EntityDef def = CreateEntityDef("player_basic", "Basic Player", new[] { "Player", "Actor", "Human", "Controllable" }, 100f, 20f, 5.0f);

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "OUTL_Basic_Player";
        go.transform.position = position;
        if (playerLayer >= 0) SetLayerRecursively(go, playerLayer);
        Undo.RegisterCreatedObjectUndo(go, "Create OUTL Basic Player");

        OUTL_EntityAdapter adapter = go.AddComponent<OUTL_EntityAdapter>();
        adapter.Def = def;
        adapter.TickLane = OUTL_TickLane.Full;
        adapter.TickInterval = 0f;

        OUTL_DamageReceiver damage = go.AddComponent<OUTL_DamageReceiver>();
        damage.Entity = adapter;

        OUTL_Hitbox hitbox = go.AddComponent<OUTL_Hitbox>();
        hitbox.Entity = adapter;
        hitbox.Zone = OUTL_HitboxZone.Torso;

        OUTL_Vitals vitals = go.AddComponent<OUTL_Vitals>();
        vitals.Entity = adapter;
        vitals.InitializeMissingStats = true;
        vitals.DefaultHealth = 100f;
        vitals.DefaultMaxHealth = 100f;

        OUTL_DeathHandler death = go.AddComponent<OUTL_DeathHandler>();
        death.Entity = adapter;
        death.QueueDespawn = false;
        death.DisableColliders = false;
        death.DisableRenderers = false;

        CharacterController cc = go.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.32f;
        cc.center = new Vector3(0f, 0.9f, 0f);

        GameObject cam = new GameObject("ViewCamera");
        cam.transform.SetParent(go.transform, false);
        cam.transform.localPosition = new Vector3(0f, 1.62f, 0f);
        Camera camera = cam.AddComponent<Camera>();
        cam.AddComponent<AudioListener>();

        GameObject muzzle = new GameObject("Muzzle_Player");
        muzzle.transform.SetParent(cam.transform, false);
        muzzle.transform.localPosition = new Vector3(0.16f, -0.12f, 0.45f);

        OUTL_AttackDriver attack = go.AddComponent<OUTL_AttackDriver>();
        attack.Source = adapter;
        attack.Muzzle = muzzle.transform;
        attack.AimCamera = camera;
        attack.Primary = CreateAttack("OUTL_Attack_PlayerHitscan", OUTL_AttackMode.Hitscan, 20f, 80f, 0.18f, 0.12f, 1.0f, 0.6f);
        attack.Secondary = CreateAttack("OUTL_Attack_PlayerHeavyHitscan", OUTL_AttackMode.Hitscan, 45f, 100f, 0.55f, 0.15f, 2.0f, 1.0f);
        attack.Melee = CreateAttack("OUTL_Attack_PlayerMelee", OUTL_AttackMode.Melee, 25f, 1.75f, 0.45f, 0.75f, 10f, 3f);
        attack.MeleeHeight = 1.45f;
        attack.MeleeForwardBias = 0.55f;

        OUTL_BasicPlayerController controller = go.AddComponent<OUTL_BasicPlayerController>();
        controller.Entity = adapter;
        controller.AttackDriver = attack;
        controller.ViewCamera = camera;
        controller.CharacterController = cc;
        controller.UseMask = ~0;

        return go;
    }

    private static void CreatePlayerHUD(GameObject player, OUTL_EntityAdapter entity, OUTL_BasicPlayerController controller, Camera camera)
    {
        GameObject existing = GameObject.Find("OUTL_PlayerHUD_Canvas");
        if (existing != null) Object.DestroyImmediate(existing);

        GameObject canvasGO = new GameObject("OUTL_PlayerHUD_Canvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create OUTL Player HUD Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
        canvasGO.AddComponent<GraphicRaycaster>();

        EnsureEventSystem();

        GameObject hudRoot = CreateUIRect("HUDRoot", canvasGO.transform, new Vector2(0f, 0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject healthPanel = CreatePanel("HealthPanel", hudRoot.transform, new Vector2(24f, 24f), new Vector2(320f, 72f), new Color(0f, 0f, 0f, 0.55f));
        Text hpLabel = CreateText("HPLabel", healthPanel.transform, "HP", 20, TextAnchor.MiddleLeft, new Vector2(12f, -8f), new Vector2(70f, 28f));
        Text hpValue = CreateText("HPValue", healthPanel.transform, "100 / 100", 24, TextAnchor.MiddleRight, new Vector2(196f, -8f), new Vector2(110f, 28f));
        Image hpFill = CreateImage("HPFill", healthPanel.transform, new Color(0.1f, 0.85f, 0.25f, 0.85f), new Vector2(12f, -44f), new Vector2(294f, 14f));
        hpFill.type = Image.Type.Filled;
        hpFill.fillMethod = Image.FillMethod.Horizontal;
        hpFill.fillOrigin = 0;
        OUTL_StatHUDConnector hp = healthPanel.AddComponent<OUTL_StatHUDConnector>();
        hp.Entity = entity;
        hp.Stat = OUTL_StatId.Health;
        hp.MaxValue = 100f;
        hp.Label = "HP";
        hp.ValueText = hpValue;
        hp.LabelText = hpLabel;
        hp.FillImage = hpFill;
        hp.ShowCurrentAndMax = true;
        hp.Thresholds = new[]
        {
            new OUTL_StatHUDThreshold { MaxNormalized = 0.25f, Color = new Color(1f, 0.05f, 0.02f, 0.95f) },
            new OUTL_StatHUDThreshold { MaxNormalized = 0.5f, Color = new Color(1f, 0.75f, 0.05f, 0.95f) },
            new OUTL_StatHUDThreshold { MaxNormalized = 1f, Color = new Color(0.1f, 0.85f, 0.25f, 0.95f) }
        };

        GameObject armorPanel = CreatePanel("ArmorPanel", hudRoot.transform, new Vector2(24f, 104f), new Vector2(320f, 52f), new Color(0f, 0f, 0f, 0.42f));
        Text armorLabel = CreateText("ArmorLabel", armorPanel.transform, "ARM", 18, TextAnchor.MiddleLeft, new Vector2(12f, -6f), new Vector2(70f, 24f));
        Text armorValue = CreateText("ArmorValue", armorPanel.transform, "0 / 100", 20, TextAnchor.MiddleRight, new Vector2(196f, -6f), new Vector2(110f, 24f));
        Image armorFill = CreateImage("ArmorFill", armorPanel.transform, new Color(0.25f, 0.55f, 1f, 0.85f), new Vector2(12f, -34f), new Vector2(294f, 10f));
        armorFill.type = Image.Type.Filled;
        armorFill.fillMethod = Image.FillMethod.Horizontal;
        armorFill.fillOrigin = 0;
        OUTL_StatHUDConnector armor = armorPanel.AddComponent<OUTL_StatHUDConnector>();
        armor.Entity = entity;
        armor.Stat = OUTL_StatId.Armor;
        armor.MaxValue = 100f;
        armor.Label = "ARM";
        armor.ValueText = armorValue;
        armor.LabelText = armorLabel;
        armor.FillImage = armorFill;
        armor.ShowCurrentAndMax = true;
        armor.DefaultColor = new Color(0.25f, 0.55f, 1f, 0.95f);

        GameObject interactPanel = CreatePanel("InteractPrompt", hudRoot.transform, new Vector2(0f, 120f), new Vector2(520f, 110f), new Color(0f, 0f, 0f, 0.5f));
        RectTransform interactRect = interactPanel.GetComponent<RectTransform>();
        interactRect.anchorMin = new Vector2(0.5f, 0f);
        interactRect.anchorMax = new Vector2(0.5f, 0f);
        interactRect.pivot = new Vector2(0.5f, 0f);
        Text keyText = CreateText("KeyText", interactPanel.transform, "E", 32, TextAnchor.MiddleCenter, new Vector2(18f, -22f), new Vector2(60f, 60f));
        Text verbText = CreateText("VerbText", interactPanel.transform, "РСЃРїРѕР»СЊР·РѕРІР°С‚СЊ", 22, TextAnchor.MiddleLeft, new Vector2(88f, -16f), new Vector2(190f, 30f));
        Text nameText = CreateText("NameText", interactPanel.transform, "Object", 24, TextAnchor.MiddleLeft, new Vector2(88f, -48f), new Vector2(390f, 30f));
        Text descText = CreateText("DescriptionText", interactPanel.transform, "Description", 16, TextAnchor.MiddleLeft, new Vector2(88f, -76f), new Vector2(390f, 24f));
        Image icon = CreateImage("Icon", interactPanel.transform, new Color(1f, 1f, 1f, 0.85f), new Vector2(18f, -28f), new Vector2(48f, 48f));
        OUTL_InteractPromptConnector prompt = interactPanel.AddComponent<OUTL_InteractPromptConnector>();
        prompt.Player = controller;
        prompt.Root = interactPanel;
        prompt.NameText = nameText;
        prompt.DescriptionText = descText;
        prompt.VerbText = verbText;
        prompt.KeyText = keyText;
        prompt.IconImage = icon;
        prompt.AutoLanguage = true;
        prompt.Language = "ru";
        interactPanel.SetActive(false);

        GameObject painRoot = CreateUIRect("DamageHUD", hudRoot.transform, Vector2.zero, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image pain = CreateFullScreenImage("FullscreenPain", painRoot.transform, new Color(1f, 0f, 0f, 0f));
        Image front = CreateCompassImage("DamageFront", painRoot.transform, new Vector2(0.5f, 1f), new Vector2(0f, -70f));
        Image rear = CreateCompassImage("DamageRear", painRoot.transform, new Vector2(0.5f, 0f), new Vector2(0f, 70f));
        Image left = CreateCompassImage("DamageLeft", painRoot.transform, new Vector2(0f, 0.5f), new Vector2(70f, 0f));
        Image right = CreateCompassImage("DamageRight", painRoot.transform, new Vector2(1f, 0.5f), new Vector2(-70f, 0f));
        OUTL_DamageHUDConnector damageHUD = painRoot.AddComponent<OUTL_DamageHUDConnector>();
        damageHUD.PlayerEntity = entity;
        damageHUD.PlayerView = camera != null ? camera.transform : player.transform;
        damageHUD.HealthText = hpValue;
        damageHUD.FullscreenPainImage = pain;
        damageHUD.FrontImage = front;
        damageHUD.RearImage = rear;
        damageHUD.LeftImage = left;
        damageHUD.RightImage = right;
    }

    private static GameObject CreateBasicNPCObject(Vector3 position)
    {
        EnsureFolder(Folder);
        OUTL_FactionDef faction = GetOrCreateFaction("OUTL_Faction_Friendly", "friendly", "Friendly NPCs");
        OUTL_EntityDef def = CreateEntityDef("npc_basic", "Basic NPC", new[] { "Actor", "NPC", "Friendly" }, 80f, 6f, 3.0f);
        OUTL_AIProfile aiProfile = CreateSimpleAIProfile("OUTL_AI_Profile_BasicNPC", "npc.basic", false);
        return CreateActorObject("OUTL_Basic_NPC", def, faction, aiProfile, false, position);
    }

    private static GameObject CreateBasicEnemyObject(Vector3 position)
    {
        EnsureFolder(Folder);
        OUTL_FactionDef faction = GetOrCreateFaction("OUTL_Faction_Bandits", "bandits", "Bandits");
        OUTL_EntityDef def = CreateEntityDef("enemy_basic", "Basic Enemy", new[] { "Actor", "NPC", "Enemy" }, 70f, 12f, 3.4f);
        OUTL_AIProfile aiProfile = CreateSimpleAIProfile("OUTL_AI_Profile_BasicEnemy", "enemy.basic", true);
        return CreateActorObject("OUTL_Basic_Enemy", def, faction, aiProfile, true, position);
    }

    private static GameObject CreateBasicChestObject(Vector3 position)
    {
        EnsureFolder(Folder);
        OUTL_EntityDef def = CreateEntityDef("chest_basic", "Basic Chest", new[] { "Container", "Chest", "Interactable" }, 60f, 0f, 0f);
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "OUTL_Basic_Chest";
        go.transform.position = position;
        go.transform.localScale = new Vector3(1.2f, 0.8f, 0.8f);
        Undo.RegisterCreatedObjectUndo(go, "Create OUTL Basic Chest");
        OUTL_EntityAdapter adapter = go.AddComponent<OUTL_EntityAdapter>();
        adapter.Def = def;
        adapter.RegisterTick = false;
        OUTL_Interactable interactable = go.AddComponent<OUTL_Interactable>();
        interactable.Entity = adapter;
        interactable.DisplayName = "РЎСѓРЅРґСѓРє";
        interactable.DescriptionRu = "РћС‚РєСЂС‹С‚СЊ СЃСѓРЅРґСѓРє";
        interactable.DescriptionEn = "Open chest";
        OUTL_DamageReceiver damage = go.AddComponent<OUTL_DamageReceiver>();
        damage.Entity = adapter;
        OUTL_Hitbox hitbox = go.AddComponent<OUTL_Hitbox>();
        hitbox.Entity = adapter;
        hitbox.Zone = OUTL_HitboxZone.Generic;
        OUTL_Vitals vitals = go.AddComponent<OUTL_Vitals>();
        vitals.Entity = adapter;
        vitals.InitializeMissingStats = true;
        vitals.DefaultHealth = 60f;
        vitals.DefaultMaxHealth = 60f;
        OUTL_DeathHandler death = go.AddComponent<OUTL_DeathHandler>();
        death.Entity = adapter;
        death.QueueDespawn = false;
        return go;
    }

    private static GameObject CreateBasicDoorObject(Vector3 position)
    {
        EnsureFolder(Folder);
        OUTL_EntityDef def = CreateEntityDef("door_basic", "Basic Door", new[] { "Door", "Interactable" }, 100f, 0f, 0f);
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "OUTL_Basic_Door";
        go.transform.position = position;
        go.transform.localScale = new Vector3(1.4f, 2.4f, 0.25f);
        Undo.RegisterCreatedObjectUndo(go, "Create OUTL Basic Door");
        OUTL_EntityAdapter adapter = go.AddComponent<OUTL_EntityAdapter>();
        adapter.Def = def;
        OUTL_Door door = go.AddComponent<OUTL_Door>();
        door.Entity = adapter;
        door.DoorRoot = go.transform;
        door.ClosedLocalPosition = go.transform.localPosition;
        door.OpenLocalPosition = go.transform.localPosition + Vector3.up * 2.5f;
        OUTL_Interactable interactable = go.AddComponent<OUTL_Interactable>();
        interactable.Entity = adapter;
        interactable.DisplayName = "Р”РІРµСЂСЊ";
        interactable.DescriptionRu = "РћС‚РєСЂС‹С‚СЊ/Р·Р°РєСЂС‹С‚СЊ РґРІРµСЂСЊ";
        interactable.DescriptionEn = "Open/close door";
        return go;
    }

    private static GameObject CreateAccessSourceOnlyObject(Vector3 position, OUTL_ItemDef keyDef, string stableId)
    {
        OUTL_EntityDef def = CreateEntityDef("access_source_only", "Access Source Only", new[] { "Actor", "Player", "SourceOnly", "Test" }, 100f, 0f, 0f);
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "OUTL_KeyDoorExample_SOURCE_ONLY";
        go.transform.position = position;
        Undo.RegisterCreatedObjectUndo(go, "Create OUTL Access Source Only");
        SetPreviewColor(go, new Color(0.15f, 0.45f, 1f, 1f));

        OUTL_EntityAdapter adapter = go.AddComponent<OUTL_EntityAdapter>();
        adapter.Def = def;
        adapter.ClassNameOverride = "access.source_only";
        adapter.TargetName = stableId;
        adapter.StableId = stableId;
        adapter.SavePersistent = true;

        OUTL_InventoryRuntime inventory = go.AddComponent<OUTL_InventoryRuntime>();
        inventory.Entity = adapter;
        inventory.KnownItems = keyDef != null ? new[] { keyDef } : new OUTL_ItemDef[0];

        EditorUtility.SetDirty(adapter);
        EditorUtility.SetDirty(inventory);
        return go;
    }

    private static GameObject CreateAccessDoorObject(Vector3 position)
    {
        GameObject go = CreateBasicDoorObject(position);
        go.transform.localScale = new Vector3(1.8f, 2.6f, 0.28f);
        SetPreviewColor(go, new Color(1f, 0.55f, 0.12f, 1f));

        OUTL_Interactable interactable = go.GetComponent<OUTL_Interactable>();
        if (interactable != null)
        {
            interactable.DisplayName = "Locked Access Door";
            interactable.DescriptionEn = "Requires the red access key";
            interactable.DescriptionRu = "Нужен красный ключ доступа";
            interactable.Command = OUTL_CommandType.Use;
            EditorUtility.SetDirty(interactable);
        }

        OUTL_Door door = go.GetComponent<OUTL_Door>();
        if (door != null)
        {
            door.ToggleMode = false;
            door.AutoClose = false;
            door.CheckBlockers = false;
            EditorUtility.SetDirty(door);
        }

        return go;
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

    private static GameObject CreateBasicButtonObject(Vector3 position)
    {
        EnsureFolder(Folder);
        OUTL_EntityDef def = CreateEntityDef("button_basic", "Basic Button", new[] { "Button", "Interactable", "Logic" }, 20f, 0f, 0f);
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "OUTL_Basic_Button";
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.45f, 0.15f, 0.45f);
        Undo.RegisterCreatedObjectUndo(go, "Create OUTL Basic Button");
        OUTL_EntityAdapter adapter = go.AddComponent<OUTL_EntityAdapter>();
        adapter.Def = def;
        OUTL_Button button = go.AddComponent<OUTL_Button>();
        button.Entity = adapter;
        OUTL_Interactable interactable = go.AddComponent<OUTL_Interactable>();
        interactable.Entity = adapter;
        interactable.DisplayName = "РљРЅРѕРїРєР°";
        interactable.DescriptionRu = "РќР°Р¶Р°С‚СЊ РєРЅРѕРїРєСѓ";
        interactable.DescriptionEn = "Press button";
        return go;
    }

    private static GameObject CreateLogicExampleButton(Vector3 position, string name, string targetName, string gateTargetName, int inputIndex)
    {
        GameObject go = CreateBasicButtonObject(position);
        go.name = name;
        ConfigureEntityAddress(go, "logic_button", targetName, targetName);

        OUTL_Button button = go.GetComponent<OUTL_Button>();
        if (button != null)
        {
            button.Toggle = true;
            button.OverrideOutputIntWithState = false;
            button.Outputs = new[]
            {
                new OUTL_OutputLink { EventName = "OnPressed", TargetName = gateTargetName, Command = OUTL_CommandType.Activate, IntValue = inputIndex },
                new OUTL_OutputLink { EventName = "OnReleased", TargetName = gateTargetName, Command = OUTL_CommandType.Deactivate, IntValue = inputIndex }
            };
            EditorUtility.SetDirty(button);
        }

        OUTL_Interactable interactable = go.GetComponent<OUTL_Interactable>();
        if (interactable != null)
        {
            interactable.DisplayName = inputIndex == 0 ? "Logic Button A" : "Logic Button B";
            interactable.DescriptionEn = "Toggle logic input " + inputIndex;
            interactable.DescriptionRu = "Toggle logic input " + inputIndex;
            interactable.Command = OUTL_CommandType.Use;
            EditorUtility.SetDirty(interactable);
        }

        return go;
    }

    private static GameObject CreateLogicGateObject(Vector3 position, string targetName, string doorTargetName)
    {
        EnsureFolder(Folder);
        OUTL_EntityDef def = CreateEntityDef("logic_gate_and", "Logic AND Gate", new[] { "Logic", "Gate" }, 1f, 0f, 0f);
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "OUTL_LogicExample_AND_Gate";
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.7f, 0.3f, 0.7f);
        Undo.RegisterCreatedObjectUndo(go, "Create OUTL Logic Gate");

        OUTL_EntityAdapter adapter = go.AddComponent<OUTL_EntityAdapter>();
        adapter.Def = def;
        adapter.RegisterTick = false;
        adapter.RegisterRandomTick = false;
        ConfigureEntityAddress(go, "logic_gate", targetName, targetName);

        OUTL_LogicGate gate = go.AddComponent<OUTL_LogicGate>();
        gate.Entity = adapter;
        gate.Mode = OUTL_BooleanGateMode.And;
        gate.InputCount = 2;
        gate.Inputs = new bool[2];
        gate.Outputs = new[]
        {
            new OUTL_OutputLink { EventName = "OnTrue", TargetName = doorTargetName, Command = OUTL_CommandType.Open },
            new OUTL_OutputLink { EventName = "OnFalse", TargetName = doorTargetName, Command = OUTL_CommandType.Close }
        };
        adapter.RebuildCommandReceiverCache();
        EditorUtility.SetDirty(adapter);
        EditorUtility.SetDirty(gate);
        return go;
    }

    private static void ConfigureEntityAddress(GameObject go, string className, string targetName, string stableId)
    {
        if (go == null) return;
        OUTL_EntityAdapter adapter = go.GetComponent<OUTL_EntityAdapter>();
        if (adapter == null) return;
        adapter.ClassNameOverride = className;
        adapter.TargetName = targetName;
        adapter.StableId = stableId;
        adapter.RebuildCommandReceiverCache();
        adapter.MarkAddressDirty();
        EditorUtility.SetDirty(adapter);
    }

    private static void ParentForWorkbench(GameObject root, GameObject child)
    {
        if (root == null || child == null) return;
        Undo.SetTransformParent(child.transform, root.transform, "Parent OUTL Workbench Object");
    }

    private static GameObject CreateBasicPatrolPointAt(Vector3 position)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "OUTL_PatrolPoint";
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.25f;
        Undo.RegisterCreatedObjectUndo(go, "Create OUTL Patrol Point");
        Collider c = go.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);
        return go;
    }

    private static GameObject CreateBasicCoverPointAt(Vector3 position)
    {
        GameObject go = new GameObject("OUTL_CoverPoint");
        go.transform.position = position;
        Undo.RegisterCreatedObjectUndo(go, "Create OUTL Cover Point");
        go.AddComponent<OUTL_CoverPoint>();
        return go;
    }

    private static GameObject CreateActorObject(string name, OUTL_EntityDef def, OUTL_FactionDef faction, OUTL_AIProfile aiProfile, bool hostile, Vector3 position)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;
        go.transform.position = position;
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        OUTL_EntityAdapter adapter = go.AddComponent<OUTL_EntityAdapter>();
        adapter.Def = def;
        adapter.Faction = faction;
        adapter.TickLane = OUTL_TickLane.Logic;
        adapter.TickInterval = 0.25f;
        OUTL_DamageReceiver damage = go.AddComponent<OUTL_DamageReceiver>();
        damage.Entity = adapter;
        OUTL_Hitbox hitbox = go.AddComponent<OUTL_Hitbox>();
        hitbox.Entity = adapter;
        hitbox.Zone = OUTL_HitboxZone.Torso;
        OUTL_Vitals vitals = go.AddComponent<OUTL_Vitals>();
        vitals.Entity = adapter;
        vitals.InitializeMissingStats = true;
        vitals.DefaultHealth = def != null && def.BaseStats != null && def.BaseStats.Length > 0 ? def.BaseStats[0].Value : 80f;
        vitals.DefaultMaxHealth = Mathf.Max(1f, vitals.DefaultHealth);
        OUTL_DeathHandler death = go.AddComponent<OUTL_DeathHandler>();
        death.Entity = adapter;
        death.DisableAI = true;
        death.DisableColliders = true;
        death.DisableRenderers = false;
        death.QueueDespawn = true;

        OUTL_FallDamageSensor fallDamage = go.AddComponent<OUTL_FallDamageSensor>();
        fallDamage.Entity = adapter;
        fallDamage.GroundProbeCollider = go.GetComponent<Collider>();
        fallDamage.TickLane = OUTL_TickLane.Full;
        fallDamage.TickInterval = 0.02f;

        NavMeshAgent nav = go.AddComponent<NavMeshAgent>();
        nav.speed = hostile ? 3.4f : 3.0f;
        OUTL_NavMeshMover mover = go.AddComponent<OUTL_NavMeshMover>();
        mover.Agent = nav;
        mover.FallbackSpeed = nav.speed;

        GameObject muzzle = new GameObject("Muzzle_AI");
        muzzle.transform.SetParent(go.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 1.15f, 0.45f);

        OUTL_AttackDriver attack = go.AddComponent<OUTL_AttackDriver>();
        attack.Source = adapter;
        attack.Muzzle = muzzle.transform;
        attack.Primary = CreateAttack(hostile ? "OUTL_Attack_BasicEnemyMelee" : "OUTL_Attack_BasicNPCMelee", OUTL_AttackMode.Melee, hostile ? 12f : 6f, 1.9f, 0.75f, 0.85f, 8f, 2f);
        attack.Melee = attack.Primary;
        attack.MeleeHeight = 1.55f;
        attack.MeleeForwardBias = 0.55f;

        OUTL_AIActor ai = go.AddComponent<OUTL_AIActor>();
        ai.Profile = aiProfile;
        ai.Entity = adapter;
        ai.MoveRoot = go.transform;
        ai.NavMover = mover;
        ai.AttackDriver = attack;
        ai.TargetEyeHeight = 1.0f;
        ai.EyeHeight = 1.35f;

        OUTL_HearingSensor hearing = go.AddComponent<OUTL_HearingSensor>();
        hearing.Actor = ai;
        OUTL_EntityDiary diary = go.AddComponent<OUTL_EntityDiary>();
        diary.Entity = adapter;
        diary.WriteToFile = false;

        return go;
    }

    private static void CreateWall(string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position = position;
        wall.transform.localScale = scale;
        Undo.RegisterCreatedObjectUndo(wall, "Create OUTL Test Room Wall");
    }

    private static GameObject CreateUIRect(string name, Transform parent, Vector2 anchoredPosition, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 pivot)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot == Vector2.zero ? new Vector2(0.5f, 0.5f) : pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
        return go;
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject go = CreateUIRect(name, parent, anchoredPosition, new Vector2(0f, 1f), new Vector2(0f, 1f), size, new Vector2(0f, 1f));
        Image image = go.AddComponent<Image>();
        image.color = color;
        return go;
    }

    private static Text CreateText(string name, Transform parent, string text, int fontSize, TextAnchor anchor, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject go = CreateUIRect(name, parent, anchoredPosition, new Vector2(0f, 1f), new Vector2(0f, 1f), size, new Vector2(0f, 1f));
        Text t = go.AddComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.fontSize = fontSize;
        t.alignment = anchor;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    private static Image CreateImage(string name, Transform parent, Color color, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject go = CreateUIRect(name, parent, anchoredPosition, new Vector2(0f, 1f), new Vector2(0f, 1f), size, new Vector2(0f, 1f));
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Image CreateFullScreenImage(string name, Transform parent, Color color)
    {
        GameObject go = CreateUIRect(name, parent, Vector2.zero, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f));
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Image CreateCompassImage(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition)
    {
        GameObject go = CreateUIRect(name, parent, anchoredPosition, anchor, anchor, new Vector2(160f, 42f), new Vector2(0.5f, 0.5f));
        Image image = go.AddComponent<Image>();
        image.color = new Color(1f, 0f, 0f, 0f);
        image.raycastTarget = false;
        return image;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;
        GameObject go = new GameObject("EventSystem");
        Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    private static OUTL_EntityDef CreateEntityDef(string className, string displayName, string[] tags, float health, float damage, float speed)
    {
        OUTL_EntityDef def = ScriptableObject.CreateInstance<OUTL_EntityDef>();
        def.ClassName = className;
        def.DisplayName = displayName;
        def.Tags = tags;
        def.BaseStats = new[]
        {
            new OUTL_StatEntry { Key = "Health", Value = health },
            new OUTL_StatEntry { Key = "Damage", Value = damage },
            new OUTL_StatEntry { Key = "Speed", Value = speed },
            new OUTL_StatEntry { Key = "Armor", Value = 0f }
        };
        AssetDatabase.CreateAsset(def, AssetDatabase.GenerateUniqueAssetPath(Folder + "/OUTL_" + className + "_Def.asset"));
        return def;
    }

    private static OUTL_ItemDef CreateItemDef(string className, string displayName, string[] tags, int maxStack)
    {
        OUTL_ItemDef item = ScriptableObject.CreateInstance<OUTL_ItemDef>();
        item.ClassName = className;
        item.DisplayName = displayName;
        item.Tags = tags;
        item.MaxStack = Mathf.Max(1, maxStack);
        item.BaseStats = new[] { new OUTL_StatEntry { Key = "Value", Value = 1f } };
        AssetDatabase.CreateAsset(item, AssetDatabase.GenerateUniqueAssetPath(Folder + "/OUTL_" + className.Replace('.', '_') + ".asset"));
        return item;
    }

    private static OUTL_AIProfile CreateSimpleAIProfile(string assetName, string id, bool hostile)
    {
        OUTL_AIProfile ai = ScriptableObject.CreateInstance<OUTL_AIProfile>();
        ai.ProfileId = id;
        ai.UseFactionHostility = true;
        ai.EnemyTags = hostile ? new[] { "Player", "Friendly" } : new[] { "Enemy" };
        ai.ViewDistance = hostile ? 36f : 24f;
        ai.AttackDistance = hostile ? 1.9f : 1.7f;
        ai.MoveSpeed = hostile ? 3.4f : 3.0f;
        ai.ThinkIntervalNear = 0.15f;
        ai.ThinkIntervalMid = 0.5f;
        ai.ThinkIntervalFar = 1.5f;
        ai.IdleSchedule = CreateSchedule(assetName + "_Idle", id + ".idle", new[] { Task(OUTL_AITaskType.Wait, 0.55f, 0f), Task(OUTL_AITaskType.FindTarget, 0.05f, 0f) });
        ai.SearchSchedule = CreateSchedule(assetName + "_Search", id + ".search", new[] { Task(OUTL_AITaskType.InvestigateStimulus, 0.4f, 1.2f), Task(OUTL_AITaskType.Wait, 0.25f, 0f), Task(OUTL_AITaskType.FindTarget, 0.05f, 0f) });
        ai.CombatSchedule = CreateSchedule(assetName + "_Combat", id + ".combat", new[] { Task(OUTL_AITaskType.FaceTarget, 0.05f, 0f), Task(OUTL_AITaskType.MoveToTarget, 0.12f, 1.35f), Task(OUTL_AITaskType.AttackTarget, 0.05f, 0f), Task(OUTL_AITaskType.Wait, 0.25f, 0f) });
        AssetDatabase.CreateAsset(ai, AssetDatabase.GenerateUniqueAssetPath(Folder + "/" + assetName + ".asset"));
        return ai;
    }

    private static OUTL_AIScheduleLite CreateSchedule(string assetName, string id, OUTL_AITaskDef[] tasks)
    {
        OUTL_AIScheduleLite schedule = ScriptableObject.CreateInstance<OUTL_AIScheduleLite>();
        schedule.ScheduleId = id;
        schedule.Loop = true;
        schedule.Tasks = tasks;
        AssetDatabase.CreateAsset(schedule, AssetDatabase.GenerateUniqueAssetPath(Folder + "/" + assetName + ".asset"));
        return schedule;
    }

    private static OUTL_AITaskDef Task(OUTL_AITaskType type, float duration, float distance)
    {
        return new OUTL_AITaskDef { Type = type, Duration = duration, Distance = distance, SpeedMultiplier = 1f, Mask = ~0 };
    }

    private static OUTL_AttackProfile CreateAttack(string name, OUTL_AttackMode mode, float damage, float range, float cooldown, float radius, float horizontalSpread, float verticalSpread)
    {
        OUTL_AttackProfile profile = ScriptableObject.CreateInstance<OUTL_AttackProfile>();
        profile.AttackId = name;
        profile.Mode = mode;
        profile.Damage = damage;
        profile.Range = range;
        profile.Radius = radius;
        profile.Cooldown = cooldown;
        profile.HorizontalSpreadDegrees = horizontalSpread;
        profile.VerticalSpreadDegrees = verticalSpread;
        profile.HitMask = ~0;
        AssetDatabase.CreateAsset(profile, AssetDatabase.GenerateUniqueAssetPath(Folder + "/" + name + ".asset"));
        return profile;
    }

    private static OUTL_FactionDef GetOrCreateFaction(string assetName, string id, string displayName)
    {
        string path = Folder + "/" + assetName + ".asset";
        OUTL_FactionDef existing = AssetDatabase.LoadAssetAtPath<OUTL_FactionDef>(path);
        if (existing != null) return existing;
        OUTL_FactionDef faction = ScriptableObject.CreateInstance<OUTL_FactionDef>();
        faction.FactionId = id;
        faction.DisplayName = displayName;
        AssetDatabase.CreateAsset(faction, path);
        return faction;
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

    private static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing >= 0) return existing;
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(layer.stringValue)) continue;
            layer.stringValue = layerName;
            tagManager.ApplyModifiedProperties();
            return i;
        }
        Debug.LogWarning("OUTL: no free Unity layer slot for " + layerName + ". Create it manually in Project Settings > Tags and Layers.");
        return LayerMask.NameToLayer(layerName);
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        if (go == null || layer < 0) return;
        go.layer = layer;
        Transform t = go.transform;
        for (int i = 0; i < t.childCount; i++) SetLayerRecursively(t.GetChild(i).gameObject, layer);
    }
}
#endif
