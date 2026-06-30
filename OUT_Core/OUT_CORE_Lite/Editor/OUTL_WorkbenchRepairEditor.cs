#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class OUTL_WorkbenchRepairEditor
{
    private const string MenuRoot = "OUT CORE Lite/Advanced/Repair/";
    private const string PlayerTemplateFolder = "Assets/OUT/OUT_Core/OUT_CORE_Lite/Templates/Player";
    private const string AITemplateFolder = "Assets/OUT/OUT_Core/OUT_CORE_Lite/Templates/AI";

    // [MenuItem(MenuRoot + "Repair Open Scene: Runtime + Chunks + Player HUD")]
    public static void RepairOpenScene()
    {
        GameObject runtime = EnsureRuntimeRoot();
        EnsureChunkRig(runtime);
        int repairedPlayers = RepairAllPlayers();
        EditorUtility.SetDirty(runtime);
        Debug.Log("OUTL repair complete. players=" + repairedPlayers + " runtime=" + runtime.name + ".");
    }

    // [MenuItem(MenuRoot + "Repair Selected Player HUD")]
    public static void RepairSelectedPlayerHUD()
    {
        GameObject go = Selection.activeGameObject;
        OUTL_BasicPlayerController controller = go != null ? go.GetComponentInParent<OUTL_BasicPlayerController>() : null;
        if (controller == null)
        {
            Debug.LogWarning("OUTL: select a player object with OUTL_BasicPlayerController.");
            return;
        }
        RepairPlayer(controller);
        Selection.activeGameObject = controller.gameObject;
    }

    [MenuItem(MenuRoot + "Repair Selected Actor Combat Stack")]
    public static void RepairSelectedActorCombatStack()
    {
        GameObject[] selected = Selection.gameObjects;
        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("OUTL: select one or more actor root GameObjects.");
            return;
        }

        int repaired = 0;
        for (int i = 0; i < selected.Length; i++)
        {
            if (selected[i] == null) continue;
            if (RepairActorCombatStack(selected[i]) != null) repaired++;
        }
        Debug.Log("OUTL actor combat stack repair complete. repaired=" + repaired + ".");
    }

    [MenuItem(MenuRoot + "Repair Selected Damageable Actor")]
    public static void RepairSelectedDamageableActor()
    {
        RepairSelectedActorCombatStack();
    }

    [MenuItem(MenuRoot + "Repair Selected Armed Actor")]
    public static void RepairSelectedArmedActor()
    {
        GameObject[] selected = Selection.gameObjects;
        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("OUTL: select one or more actor root GameObjects.");
            return;
        }

        OUTL_AttackProfile selectedProfile = FindSelectedAttackProfile();
        int repaired = 0;
        for (int i = 0; i < selected.Length; i++)
        {
            if (selected[i] == null) continue;
            OUTL_AttackDriver attack = RepairArmedActor(selected[i], selectedProfile);
            if (attack != null) repaired++;
        }
        Debug.Log("OUTL armed actor repair complete. repaired=" + repaired + ".");
    }

    [MenuItem(MenuRoot + "Repair Selected Ranged Combat Actor")]
    public static void RepairSelectedRangedCombatActor()
    {
        GameObject[] selected = Selection.gameObjects;
        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("OUTL: select one or more ranged actor root GameObjects.");
            return;
        }

        OUTL_AttackProfile selectedProfile = FindSelectedAttackProfile();
        int repaired = 0;
        for (int i = 0; i < selected.Length; i++)
        {
            if (selected[i] == null) continue;
            if (RepairRangedCombatActor(selected[i], selectedProfile) != null) repaired++;
        }
        Debug.Log("OUTL ranged combat actor repair complete. repaired=" + repaired + ".");
    }

    [MenuItem(MenuRoot + "Repair Selected Melee Combat Actor")]
    public static void RepairSelectedMeleeCombatActor()
    {
        GameObject[] selected = Selection.gameObjects;
        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("OUTL: select one or more melee actor root GameObjects.");
            return;
        }

        OUTL_AttackProfile selectedProfile = FindSelectedAttackProfile();
        int repaired = 0;
        for (int i = 0; i < selected.Length; i++)
        {
            if (selected[i] == null) continue;
            if (RepairMeleeCombatActor(selected[i], selectedProfile) != null) repaired++;
        }
        Debug.Log("OUTL melee combat actor repair complete. repaired=" + repaired + ".");
    }

    [MenuItem(MenuRoot + "Repair Selected NPC Combat Stack")]
    public static void RepairSelectedNPCCombatStack()
    {
        GameObject[] selected = Selection.gameObjects;
        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("OUTL: select one or more NPC actor root GameObjects.");
            return;
        }

        OUTL_AttackProfile selectedProfile = FindSelectedAttackProfile();
        int repaired = 0;
        for (int i = 0; i < selected.Length; i++)
        {
            if (selected[i] == null) continue;
            OUTL_AIActor ai = RepairNPCCombatStack(selected[i], selectedProfile);
            if (ai != null) repaired++;
        }
        Debug.Log("OUTL NPC combat stack repair complete. repaired=" + repaired + ".");
    }

    [MenuItem(MenuRoot + "Repair Selected AI Actor")]
    public static void RepairSelectedAIActor()
    {
        GameObject[] selected = Selection.gameObjects;
        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("OUTL: select one or more AI actor root GameObjects.");
            return;
        }

        OUTL_AttackProfile selectedProfile = FindSelectedAttackProfile();
        int repaired = 0;
        for (int i = 0; i < selected.Length; i++)
        {
            if (selected[i] == null) continue;
            if (RepairAIActor(selected[i], selectedProfile, false) != null) repaired++;
        }
        Debug.Log("OUTL AI actor repair complete. repaired=" + repaired + ".");
    }

    [MenuItem(MenuRoot + "Repair Selected Creature Actor")]
    public static void RepairSelectedCreatureActor()
    {
        GameObject[] selected = Selection.gameObjects;
        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("OUTL: select one or more creature actor root GameObjects.");
            return;
        }

        OUTL_AttackProfile selectedProfile = FindSelectedAttackProfile();
        int repaired = 0;
        for (int i = 0; i < selected.Length; i++)
        {
            if (selected[i] == null) continue;
            if (RepairAIActor(selected[i], selectedProfile, true) != null) repaired++;
        }
        Debug.Log("OUTL creature actor repair complete. repaired=" + repaired + ".");
    }

    // [MenuItem(MenuRoot + "Ensure Runtime Chunk Processing Rig")]
    public static void EnsureRuntimeChunkProcessingRig()
    {
        GameObject runtime = EnsureRuntimeRoot();
        EnsureChunkRig(runtime);
        Selection.activeGameObject = runtime;
    }

    private static GameObject EnsureRuntimeRoot()
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
        if (root.GetComponent<OUTL_SaveSpawnResolverRegistry>() == null) Undo.AddComponent<OUTL_SaveSpawnResolverRegistry>(root);
        if (root.GetComponent<OUTL_GameLoopRunner>() == null) Undo.AddComponent<OUTL_GameLoopRunner>(root);
        if (root.GetComponent<OUTL_GameLoopGoldenTester>() == null) Undo.AddComponent<OUTL_GameLoopGoldenTester>(root);
        return root;
    }

    private static void EnsureChunkRig(GameObject runtime)
    {
        if (runtime == null) return;
        OUTL_ChunkProcessingDriver driver = runtime.GetComponent<OUTL_ChunkProcessingDriver>();
        if (driver == null) driver = Undo.AddComponent<OUTL_ChunkProcessingDriver>(runtime);
        driver.BuiltInPreset = OUTL_ProcessingBuiltInPreset.StreamingWorld;
        driver.ApplyPresetOnEnable = true;
        driver.CacheRegistrySnapshot = true;
        driver.FullRefreshInterval = 2f;
        driver.EnforceCanonicalThreeByThree = true;
        driver.ChunkSize = Mathf.Max(32f, driver.ChunkSize <= 0f ? 64f : driver.ChunkSize);
        driver.FullRadius = 0;
        driver.NearRadius = 1;
        driver.MidRadius = 2;
        driver.FarRadius = 3;
        driver.OverrideEntitiesPerTick = true;
        driver.EntitiesPerTick = Mathf.Clamp(driver.EntitiesPerTick <= 0 ? 192 : driver.EntitiesPerTick, 32, 512);
        driver.OverrideDriverTickInterval = true;
        driver.DriverTickInterval = Mathf.Clamp(driver.DriverTickInterval <= 0f ? 0.30f : driver.DriverTickInterval, 0.10f, 1.0f);

        OUTL_ChunkDebugView debug = runtime.GetComponent<OUTL_ChunkDebugView>();
        if (debug == null) debug = Undo.AddComponent<OUTL_ChunkDebugView>(runtime);
        debug.Driver = driver;
        debug.ChunkSize = driver.ChunkSize;
        debug.ViewRadius = Mathf.Max(6, debug.ViewRadius);

        OUTL_AIStateTableDebugView aiDebug = runtime.GetComponent<OUTL_AIStateTableDebugView>();
        if (aiDebug == null) aiDebug = Undo.AddComponent<OUTL_AIStateTableDebugView>(runtime);

        OUTL_SectorGridDebugView sectorDebug = runtime.GetComponent<OUTL_SectorGridDebugView>();
        if (sectorDebug == null) sectorDebug = Undo.AddComponent<OUTL_SectorGridDebugView>(runtime);

        OUTL_GoldenTestRunner golden = runtime.GetComponent<OUTL_GoldenTestRunner>();
        if (golden == null) golden = Undo.AddComponent<OUTL_GoldenTestRunner>(runtime);
        golden.ChunkDriver = driver;

        EditorUtility.SetDirty(driver);
        EditorUtility.SetDirty(debug);
        EditorUtility.SetDirty(aiDebug);
        EditorUtility.SetDirty(sectorDebug);
        EditorUtility.SetDirty(golden);
    }

    private static int RepairAllPlayers()
    {
        OUTL_BasicPlayerController[] players = Object.FindObjectsOfType<OUTL_BasicPlayerController>(true);
        int count = 0;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null) continue;
            RepairPlayer(players[i]);
            count++;
        }
        return count;
    }

    private static void RepairPlayer(OUTL_BasicPlayerController controller)
    {
        if (controller == null) return;
        GameObject player = controller.gameObject;
        OUTL_EntityAdapter entity = player.GetComponent<OUTL_EntityAdapter>();
        if (entity == null) entity = Undo.AddComponent<OUTL_EntityAdapter>(player);
        if (string.IsNullOrEmpty(entity.ClassNameOverride) && entity.Def == null) entity.ClassNameOverride = "player";
        if (string.IsNullOrEmpty(entity.TargetName)) entity.TargetName = "player";
        if (string.IsNullOrEmpty(entity.StableId)) entity.StableId = "player_" + player.GetInstanceID();
        entity.SavePersistent = true;
        controller.Entity = entity;

        CharacterController character = player.GetComponent<CharacterController>();
        if (character == null) character = Undo.AddComponent<CharacterController>(player);
        controller.CharacterController = character;
        controller.MotorProfile = EnsureGoldSrcPlayerMotorProfile();
        controller.ApplyMotorProfile();

        Camera camera = EnsurePlayerCamera(player.transform);
        controller.ViewCamera = camera;

        OUTL_CharacterAnimationBridge animationBridge = EnsureAnimationBridge(player);
        controller.AnimationBridge = animationBridge;

        OUTL_AttackDriver attack = player.GetComponent<OUTL_AttackDriver>();
        if (attack == null) attack = Undo.AddComponent<OUTL_AttackDriver>(player);
        attack.Source = entity;
        attack.AimCamera = camera;
        attack.Muzzle = EnsureMuzzle(camera != null ? camera.transform : player.transform);
        controller.AttackDriver = attack;

        OUTL_DamageReceiver damageReceiver = player.GetComponent<OUTL_DamageReceiver>();
        if (damageReceiver == null) damageReceiver = Undo.AddComponent<OUTL_DamageReceiver>(player);
        damageReceiver.Entity = entity;
        RepairActorCombatStack(player);
        damageReceiver = player.GetComponent<OUTL_DamageReceiver>();

        OUTL_VitalsBootstrap vitalsBootstrap = player.GetComponent<OUTL_VitalsBootstrap>();
        if (vitalsBootstrap == null) vitalsBootstrap = Undo.AddComponent<OUTL_VitalsBootstrap>(player);
        vitalsBootstrap.Entity = entity;
        vitalsBootstrap.AddVitalsIfMissing = true;
        vitalsBootstrap.AddDeathHandlerIfMissing = true;
        vitalsBootstrap.AddUIBinderIfPlayer = true;
        vitalsBootstrap.Ensure();

        OUTL_EquipmentRuntime equipmentRuntime = player.GetComponent<OUTL_EquipmentRuntime>();
        if (equipmentRuntime == null) equipmentRuntime = Undo.AddComponent<OUTL_EquipmentRuntime>(player);
        equipmentRuntime.Entity = entity;

        OUTL_InventoryRuntime inventoryRuntime = player.GetComponent<OUTL_InventoryRuntime>();
        if (inventoryRuntime == null) inventoryRuntime = Undo.AddComponent<OUTL_InventoryRuntime>(player);
        inventoryRuntime.Entity = entity;

        EnsureStarterWeaponLoadout(equipmentRuntime, attack);

        OUTL_BasicHUD basicHud = player.GetComponent<OUTL_BasicHUD>();
        if (basicHud == null) basicHud = Undo.AddComponent<OUTL_BasicHUD>(player);
        basicHud.Player = entity;
        basicHud.Controller = controller;
        basicHud.AutoCreateUI = true;
        basicHud.AutoAddDataBinder = true;
        basicHud.EnsureUI();
        basicHud.EnsureBinder();

        OUTL_UIDataBinder binder = player.GetComponent<OUTL_UIDataBinder>();
        if (binder == null) binder = Undo.AddComponent<OUTL_UIDataBinder>(player);
        binder.Entity = entity;
        binder.RequiredTag = "Player";
        EnsurePlayerBindings(binder, basicHud);
        binder.AutoBindTextTargets();
        binder.RefreshNow();

        OUTL_UICollectionBinder equipment = player.GetComponent<OUTL_UICollectionBinder>();
        if (equipment == null) equipment = Undo.AddComponent<OUTL_UICollectionBinder>(player);
        equipment.Entity = entity;
        equipment.Source = OUTL_UICollectionSource.EquipmentSlots;
        equipment.Root = basicHud.EquipmentRoot;
        equipment.AutoCreateRows = true;
        equipment.MaxRows = 6;
        equipment.RefreshNow();

        EnsureEventSystem();
        EditorUtility.SetDirty(player);
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(entity);
        EditorUtility.SetDirty(basicHud);
        EditorUtility.SetDirty(binder);
        EditorUtility.SetDirty(attack);
        EditorUtility.SetDirty(animationBridge);
        EditorUtility.SetDirty(equipment);
        EditorUtility.SetDirty(equipmentRuntime);
        EditorUtility.SetDirty(inventoryRuntime);
    }

    private static OUTL_EntityAdapter RepairActorCombatStack(GameObject root)
    {
        if (root == null) return null;

        OUTL_EntityAdapter entity = root.GetComponent<OUTL_EntityAdapter>();
        if (entity == null) entity = Undo.AddComponent<OUTL_EntityAdapter>(root);
        if (string.IsNullOrEmpty(entity.ClassNameOverride) && entity.Def == null)
            entity.ClassNameOverride = "actor_generic";

        OUTL_DamageReceiver receiver = root.GetComponent<OUTL_DamageReceiver>();
        if (receiver == null) receiver = Undo.AddComponent<OUTL_DamageReceiver>(root);
        receiver.Entity = entity;

        OUTL_Vitals vitals = root.GetComponent<OUTL_Vitals>();
        if (vitals == null) vitals = Undo.AddComponent<OUTL_Vitals>(root);
        vitals.Entity = entity;
        vitals.InitializeMissingStats = true;
        vitals.DefaultHealth = Mathf.Max(1f, vitals.DefaultHealth);
        vitals.DefaultMaxHealth = Mathf.Max(vitals.DefaultHealth, vitals.DefaultMaxHealth);

        OUTL_DeathHandler death = root.GetComponent<OUTL_DeathHandler>();
        if (death == null) death = Undo.AddComponent<OUTL_DeathHandler>(root);
        death.Entity = entity;

        EnsureActorHealthDefaults(entity, vitals);
        EnsureGenericHitbox(root, entity);

        entity.RebuildCommandReceiverCache();
        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(entity);
        EditorUtility.SetDirty(receiver);
        EditorUtility.SetDirty(vitals);
        EditorUtility.SetDirty(death);
        return entity;
    }

    private static OUTL_AttackDriver RepairArmedActor(GameObject root, OUTL_AttackProfile selectedProfile)
    {
        OUTL_EntityAdapter entity = RepairActorCombatStack(root);
        if (entity == null) return null;

        OUTL_AttackDriver attack = root.GetComponent<OUTL_AttackDriver>();
        if (attack == null) attack = Undo.AddComponent<OUTL_AttackDriver>(root);
        attack.Source = entity;
        if (attack.Muzzle == null) attack.Muzzle = EnsureMuzzle(root.transform);
        if (!HasAnyAttackProfile(attack) && selectedProfile != null)
            attack.Primary = selectedProfile;

        if (!HasAnyAttackProfile(attack))
            Debug.LogWarning("OUTL: Armed actor '" + root.name + "' needs an existing OUTL_AttackProfile assigned to Primary, Secondary or Melee.", attack);

        ValidateAttackProfilesForRepair(attack);
        entity.RebuildCommandReceiverCache();
        EditorUtility.SetDirty(attack);
        EditorUtility.SetDirty(entity);
        return attack;
    }

    private static OUTL_AttackDriver RepairRangedCombatActor(GameObject root, OUTL_AttackProfile selectedProfile)
    {
        OUTL_AttackDriver attack = RepairArmedActor(root, selectedProfile);
        if (attack == null) return null;
        if (attack.Primary == null && selectedProfile != null && selectedProfile.Mode != OUTL_AttackMode.Melee)
            attack.Primary = selectedProfile;
        if (attack.Primary == null)
            Debug.LogWarning("OUTL: ranged actor '" + root.name + "' needs Primary OUTL_AttackProfile assigned.", attack);
        else if (attack.Primary.Mode == OUTL_AttackMode.Melee)
            Debug.LogWarning("OUTL: ranged actor '" + root.name + "' Primary profile is melee. Assign hitscan/projectile/direct profile for ranged combat.", attack.Primary);

        OUTL_AIActor ai = root.GetComponent<OUTL_AIActor>();
        if (ai != null)
        {
            ai.PreferRangedCombat = true;
            ai.MinSafeRange = Mathf.Max(0.5f, ai.MinSafeRange);
            ai.PreferredRange = attack.Primary != null ? Mathf.Max(1f, attack.Primary.Range * 0.65f) : Mathf.Max(1f, ai.PreferredRange);
            EditorUtility.SetDirty(ai);
        }

        ValidateAttackProfileForRepair(attack.Primary, attack);
        EditorUtility.SetDirty(attack);
        return attack;
    }

    private static OUTL_AttackDriver RepairMeleeCombatActor(GameObject root, OUTL_AttackProfile selectedProfile)
    {
        OUTL_AttackDriver attack = RepairArmedActor(root, null);
        if (attack == null) return null;
        if (attack.Melee == null && selectedProfile != null && selectedProfile.Mode == OUTL_AttackMode.Melee)
            attack.Melee = selectedProfile;
        if (attack.Melee == null)
            Debug.LogWarning("OUTL: melee actor '" + root.name + "' needs Melee OUTL_AttackProfile assigned.", attack);
        else
        {
            if (attack.Melee.Mode != OUTL_AttackMode.Melee)
                Debug.LogWarning("OUTL: melee actor '" + root.name + "' Melee slot should use OUTL_AttackMode.Melee.", attack.Melee);
            if (attack.Melee.Range <= 0f || attack.Melee.Radius <= 0f || attack.Melee.MeleeArcDegrees <= 0f)
                Debug.LogWarning("OUTL: melee profile '" + attack.Melee.name + "' should have positive range/radius/arc.", attack.Melee);
        }

        OUTL_AIActor ai = root.GetComponent<OUTL_AIActor>();
        if (ai != null)
        {
            ai.PreferRangedCombat = false;
            EditorUtility.SetDirty(ai);
        }

        EditorUtility.SetDirty(attack);
        return attack;
    }

    private static OUTL_AIActor RepairNPCCombatStack(GameObject root, OUTL_AttackProfile selectedProfile)
    {
        OUTL_AttackDriver attack = RepairArmedActor(root, selectedProfile);
        if (attack == null) return null;

        OUTL_EntityAdapter entity = root.GetComponent<OUTL_EntityAdapter>();
        OUTL_AIActor ai = root.GetComponent<OUTL_AIActor>();
        if (ai == null) ai = Undo.AddComponent<OUTL_AIActor>(root);
        ai.Entity = entity;
        ai.AttackDriver = attack;
        if (ai.MoveRoot == null) ai.MoveRoot = root.transform;

        OUTL_NavMeshMover mover = root.GetComponent<OUTL_NavMeshMover>();
        if (mover == null) mover = Undo.AddComponent<OUTL_NavMeshMover>(root);
        ai.NavMover = mover;
        ai.UseNavMeshMover = true;
        ai.Stationary = false;
        EnsureAIContracts(ai, false);
        if (attack.Primary != null && attack.Primary.Mode != OUTL_AttackMode.Melee)
        {
            ai.PreferRangedCombat = true;
            ai.PreferredRange = Mathf.Max(1f, attack.Primary.Range * 0.65f);
        }

        if (ai.Profile == null)
            Debug.LogWarning("OUTL: NPC actor '" + root.name + "' needs an OUTL_AIProfile asset assigned. Repair does not create AI profiles.", ai);
        if (entity != null && entity.Faction == null)
            Debug.LogWarning("OUTL: NPC actor '" + root.name + "' has no faction. Assign Faction or configure profile EnemyTags for target acquisition.", entity);
        if (!HasTargetAcquisitionPath(ai))
            Debug.LogWarning("OUTL: NPC actor '" + root.name + "' has no clear target acquisition path. Use faction hostility or profile EnemyTags.", ai);

        entity.RebuildCommandReceiverCache();
        EditorUtility.SetDirty(ai);
        EditorUtility.SetDirty(mover);
        EditorUtility.SetDirty(entity);
        return ai;
    }

    private static OUTL_AIActor RepairAIActor(GameObject root, OUTL_AttackProfile selectedProfile, bool creature)
    {
        OUTL_EntityAdapter entity = RepairActorCombatStack(root);
        if (entity == null) return null;

        OUTL_AttackDriver attack = root.GetComponent<OUTL_AttackDriver>();
        if (creature)
        {
            if (selectedProfile != null)
            {
                attack = RepairMeleeCombatActor(root, selectedProfile);
            }
            else if (attack != null)
            {
                attack.Source = entity;
                if (attack.Muzzle == null) attack.Muzzle = EnsureMuzzle(root.transform);
                ValidateAttackProfilesForRepair(attack);
                EditorUtility.SetDirty(attack);
            }
        }
        else
        {
            attack = RepairArmedActor(root, selectedProfile);
            if (attack == null) return null;
        }

        OUTL_AIActor ai = root.GetComponent<OUTL_AIActor>();
        if (ai == null) ai = Undo.AddComponent<OUTL_AIActor>(root);
        ai.Entity = entity;
        ai.AttackDriver = attack;
        if (ai.MoveRoot == null) ai.MoveRoot = root.transform;
        EnsureAIContracts(ai, creature);
        if (!creature && attack != null && attack.Primary != null && attack.Primary.Mode != OUTL_AttackMode.Melee)
        {
            ai.PreferRangedCombat = true;
            ai.PreferredRange = Mathf.Max(1f, attack.Primary.Range * 0.65f);
            ai.MinSafeRange = Mathf.Max(0.5f, ai.MinSafeRange);
        }

        OUTL_NavMeshMover mover = root.GetComponent<OUTL_NavMeshMover>();
        if (mover == null && !ai.Stationary) mover = Undo.AddComponent<OUTL_NavMeshMover>(root);
        ai.NavMover = mover;
        ai.UseNavMeshMover = mover != null && !ai.Stationary;

        if (ai.Profile == null)
            Debug.LogWarning("OUTL: AI actor '" + root.name + "' needs an OUTL_AIProfile asset assigned. Repair does not create AI schedules/profiles.", ai);
        if (!HasTargetAcquisitionPath(ai))
            Debug.LogWarning("OUTL: AI actor '" + root.name + "' needs faction hostility or profile EnemyTags for target acquisition.", ai);

        entity.RebuildCommandReceiverCache();
        EditorUtility.SetDirty(ai);
        if (mover != null) EditorUtility.SetDirty(mover);
        EditorUtility.SetDirty(entity);
        return ai;
    }

    private static void EnsureAIContracts(OUTL_AIActor ai, bool creature)
    {
        if (ai == null) return;
        if (ai.PerceptionProfile == null) ai.PerceptionProfile = EnsureDefaultPerceptionProfile();
        if (ai.StateTable == null) ai.StateTable = EnsureDefaultStateTable();
        ai.UseStimulusInterrupts = true;
        ai.ExposeDebugState = true;
        OUTL_StimulusSensor sensor = ai.GetComponent<OUTL_StimulusSensor>();
        if (sensor == null) sensor = Undo.AddComponent<OUTL_StimulusSensor>(ai.gameObject);
        sensor.Actor = ai;
        sensor.Entity = ai.Entity;
        sensor.Mode = creature ? OUTL_StimulusSensorMode.Threat : OUTL_StimulusSensorMode.Hearing;
        sensor.Radius = ai.PerceptionProfile != null ? Mathf.Max(1f, ai.PerceptionProfile.HearingRadius) : 16f;
        sensor.TickInterval = 0.35f;
        EditorUtility.SetDirty(sensor);
        if (creature)
        {
            ai.CreatureUsesFoodStimulus = true;
            ai.FleeFromDanger = true;
            ai.PreferRangedCombat = false;
        }
    }

    private static void EnsureActorHealthDefaults(OUTL_EntityAdapter entity, OUTL_Vitals vitals)
    {
        if (entity == null) return;
        if (entity.Def != null)
        {
            EnsureBaseStat(entity.Def, "Health", vitals != null ? Mathf.Max(1f, vitals.DefaultHealth) : 100f);
            EnsureBaseStat(entity.Def, "MaxHealth", vitals != null ? Mathf.Max(1f, vitals.DefaultMaxHealth) : 100f);
            EditorUtility.SetDirty(entity.Def);
        }
        else if (vitals != null)
        {
            vitals.InitializeMissingStats = true;
        }
    }

    private static void EnsureBaseStat(OUTL_EntityDef def, string key, float value)
    {
        if (def == null || string.IsNullOrEmpty(key)) return;
        OUTL_StatEntry[] stats = def.BaseStats;
        if (stats == null) stats = new OUTL_StatEntry[0];
        for (int i = 0; i < stats.Length; i++)
        {
            if (stats[i].Key == key)
            {
                if (stats[i].Value <= 0f) stats[i].Value = value;
                def.BaseStats = stats;
                return;
            }
        }

        System.Array.Resize(ref stats, stats.Length + 1);
        stats[stats.Length - 1] = new OUTL_StatEntry { Key = key, Value = value };
        def.BaseStats = stats;
    }

    private static void EnsureGenericHitbox(GameObject root, OUTL_EntityAdapter entity)
    {
        if (root == null) return;
        OUTL_Hitbox[] hitboxes = root.GetComponentsInChildren<OUTL_Hitbox>(true);
        if (hitboxes != null && hitboxes.Length > 0)
        {
            for (int i = 0; i < hitboxes.Length; i++)
            {
                if (hitboxes[i] == null) continue;
                if (hitboxes[i].Entity == null) hitboxes[i].Entity = entity;
                EditorUtility.SetDirty(hitboxes[i]);
            }
            return;
        }

        Collider collider = root.GetComponent<Collider>();
        if (collider == null) collider = root.GetComponentInChildren<Collider>(true);
        if (collider == null)
        {
            Debug.LogWarning("OUTL: Actor '" + root.name + "' needs at least one Collider. Repair cannot infer a safe collider shape.", root);
            return;
        }

        OUTL_Hitbox hitbox = Undo.AddComponent<OUTL_Hitbox>(collider.gameObject);
        hitbox.Entity = entity;
        hitbox.Zone = OUTL_HitboxZone.Generic;
        hitbox.DamageMultiplier = 1f;
        EditorUtility.SetDirty(hitbox);
    }

    private static OUTL_AttackProfile FindSelectedAttackProfile()
    {
        Object[] objects = Selection.objects;
        if (objects == null) return null;
        for (int i = 0; i < objects.Length; i++)
        {
            OUTL_AttackProfile profile = objects[i] as OUTL_AttackProfile;
            if (profile != null) return profile;
        }
        return null;
    }

    private static bool HasAnyAttackProfile(OUTL_AttackDriver attack)
    {
        return attack != null && (attack.Primary != null || attack.Secondary != null || attack.Melee != null);
    }

    private static void ValidateAttackProfilesForRepair(OUTL_AttackDriver attack)
    {
        if (attack == null) return;
        ValidateAttackProfileForRepair(attack.Primary, attack);
        ValidateAttackProfileForRepair(attack.Secondary, attack);
        ValidateAttackProfileForRepair(attack.Melee, attack);
    }

    private static void ValidateAttackProfileForRepair(OUTL_AttackProfile profile, Object context)
    {
        if (profile == null || profile.Mode != OUTL_AttackMode.Projectile) return;
        if (profile.ProjectilePrefab == null)
        {
            Debug.LogWarning("OUTL: projectile attack profile '" + profile.name + "' needs ProjectilePrefab assigned.", context);
            return;
        }
        if (profile.ProjectilePrefab.GetComponent<OUTL_Projectile>() == null)
            Debug.LogWarning("OUTL: projectile prefab '" + profile.ProjectilePrefab.name + "' must already contain OUTL_Projectile. Runtime component fallback is not allowed.", profile.ProjectilePrefab);
    }

    private static bool HasTargetAcquisitionPath(OUTL_AIActor ai)
    {
        if (ai == null || ai.Profile == null || ai.Entity == null) return false;
        if (ai.Profile.EnemyTags != null && ai.Profile.EnemyTags.Length > 0) return true;
        return ai.Profile.UseFactionHostility && ai.Entity.Faction != null;
    }

    private static OUTL_AIPerceptionProfile EnsureDefaultPerceptionProfile()
    {
        EnsureAITemplateFolder();
        string path = AITemplateFolder + "/OUTL_Generic_AIPerceptionProfile.asset";
        OUTL_AIPerceptionProfile profile = AssetDatabase.LoadAssetAtPath<OUTL_AIPerceptionProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<OUTL_AIPerceptionProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        profile.SightConeAngle = Mathf.Max(1f, profile.SightConeAngle <= 0f ? 120f : profile.SightConeAngle);
        profile.SightDistance = Mathf.Max(1f, profile.SightDistance <= 0f ? 30f : profile.SightDistance);
        profile.HearingRadius = Mathf.Max(1f, profile.HearingRadius <= 0f ? 16f : profile.HearingRadius);
        profile.DangerRadius = Mathf.Max(1f, profile.DangerRadius <= 0f ? 18f : profile.DangerRadius);
        profile.FoodRadius = Mathf.Max(1f, profile.FoodRadius <= 0f ? 12f : profile.FoodRadius);
        profile.MemoryDuration = Mathf.Max(0.5f, profile.MemoryDuration <= 0f ? 8f : profile.MemoryDuration);
        profile.RequireLineOfSight = true;
        profile.UseFactionFilter = true;
        profile.UseProfileEnemyTags = true;
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    private static OUTL_AIStateTable EnsureDefaultStateTable()
    {
        EnsureAITemplateFolder();
        string path = AITemplateFolder + "/OUTL_Generic_AIStateTable.asset";
        OUTL_AIStateTable table = AssetDatabase.LoadAssetAtPath<OUTL_AIStateTable>(path);
        if (table == null)
        {
            table = ScriptableObject.CreateInstance<OUTL_AIStateTable>();
            AssetDatabase.CreateAsset(table, path);
        }

        if (table.Rows == null || table.Rows.Length == 0)
        {
            table.Rows = new[]
            {
                StateRow(OUTL_AIStateId.Idle, "no strong stimulus", "stimulus or order", "SightEnemy, HeardNoise", "Wait", "none", "stop", "idle"),
                StateRow(OUTL_AIStateId.Patrol, "patrol route assigned", "stimulus or route end", "SightEnemy, HeardNoise, TookDamage", "Patrol", "route point", "move route", "move"),
                StateRow(OUTL_AIStateId.Work, "work schedule active", "stimulus or work complete", "SightEnemy, HeardNoise, TookDamage", "Work", "work point", "hold/use point", "work"),
                StateRow(OUTL_AIStateId.Investigate, "heard noise or food/danger point", "reached point or target found", "SightEnemy, TookDamage", "Investigate", "last stimulus", "move to stimulus", "search"),
                StateRow(OUTL_AIStateId.Search, "lost target", "memory expired or target found", "SightEnemy, TookDamage", "Search", "last known target", "move/search", "search"),
                StateRow(OUTL_AIStateId.Alert, "damage or suspicious stimulus", "resolved target/state", "LowHealth", "Face/ready", "source/target", "hold", "alert"),
                StateRow(OUTL_AIStateId.TakeCover, "cover found or hit", "safe or target lost", "LowHealth, Dead", "MoveToCover", "threat", "cover", "cover"),
                StateRow(OUTL_AIStateId.AttackRanged, "visible enemy in range", "target lost/too close/dead", "LowHealth, TookDamage", "AttackTarget", "enemy", "hold preferred range", "attack_ranged"),
                StateRow(OUTL_AIStateId.AttackMelee, "enemy in melee range", "target lost/dead", "LowHealth", "AttackTarget", "enemy", "close distance", "attack_melee"),
                StateRow(OUTL_AIStateId.SwitchWeapon, "range requires different profile", "profile selected", "Dead", "SwitchWeapon", "enemy", "hold", "switch"),
                StateRow(OUTL_AIStateId.EatOrUseResource, "food/resource stimulus", "resource reached/used", "SightEnemy, SightDanger, TookDamage", "UseResource", "food/resource", "move to resource", "use"),
                StateRow(OUTL_AIStateId.Flee, "danger or low health", "safe or schedule complete", "Dead", "FleeFromTarget", "threat", "move away", "flee"),
                StateRow(OUTL_AIStateId.Dead, "killed", "none", "none", "Stop", "none", "stop", "dead")
            };
        }

        for (int i = 0; i < table.Rows.Length; i++)
            if (table.Rows[i] != null)
                table.Rows[i].DebugColor = OUTL_AIStateTable.DefaultColor(table.Rows[i].State);

        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();
        return table;
    }

    private static OUTL_AIStateTableRow StateRow(OUTL_AIStateId state, string entry, string exit, string interrupts, string command, string target, string movement, string animation)
    {
        return new OUTL_AIStateTableRow
        {
            State = state,
            EntryConditions = entry,
            ExitConditions = exit,
            Interrupts = interrupts,
            MainCommand = command,
            TargetRule = target,
            MovementRule = movement,
            AnimationHint = animation,
            DebugColor = OUTL_AIStateTable.DefaultColor(state),
            Notes = ""
        };
    }

    private static OUTL_PlayerMotorProfile EnsureGoldSrcPlayerMotorProfile()
    {
        EnsurePlayerTemplateFolder();
        string path = PlayerTemplateFolder + "/OUTL_GoldSrc_PlayerMotorProfile.asset";
        OUTL_PlayerMotorProfile profile = AssetDatabase.LoadAssetAtPath<OUTL_PlayerMotorProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<OUTL_PlayerMotorProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        profile.name = "OUTL_GoldSrc_PlayerMotorProfile";
        profile.UseGoldSrcUnits = true;
        profile.GoldSrcUnitsPerUnityUnit = 32f;
        profile.ForwardSpeed = 320f;
        profile.SideSpeed = 320f;
        profile.BackSpeed = 320f;
        profile.WalkSpeed = 150f;
        profile.RunSpeed = 320f;
        profile.GroundAcceleration = 10f;
        profile.AirAcceleration = 10f;
        profile.AirWishSpeedCap = 30f;
        profile.Friction = 4f;
        profile.StopSpeed = 100f;
        profile.JumpSpeed = 270f;
        profile.GravityMultiplier = 1.65f;
        profile.FallingGravityMultiplier = 1.75f;
        profile.LowJumpGravityMultiplier = 2.25f;
        profile.MaxFallSpeed = 54f;
        profile.GroundProbeExtraDistance = 0.20f;
        profile.GroundSnapDistance = 0.24f;
        profile.GroundStickSpeed = 8f;
        profile.StableGroundUpSpeed = 2.5f;
        profile.SlopeLimit = 45f;
        profile.CancelDownhillSlideOnWalkableGround = true;
        profile.EnableFallDamage = true;
        profile.FallDamageMinSpeed = 18f;
        profile.FallDamageFatalSpeed = 32f;
        profile.FallDamageScale = 7f;
        profile.FallDamageMaxDamage = 100f;
        profile.FallDamageKey = "fall";
        profile.EnableWeaponSlotKeys = true;
        profile.EnableMouseWheelWeaponCycle = true;
        profile.ActiveWeaponDefaultSlot = OUTL_EquipmentSlot.Primary;
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    private static OUTL_CharacterAnimationBridge EnsureAnimationBridge(GameObject player)
    {
        OUTL_CharacterAnimationBridge bridge = player != null ? player.GetComponentInChildren<OUTL_CharacterAnimationBridge>(true) : null;
        if (bridge == null && player != null)
            bridge = Undo.AddComponent<OUTL_CharacterAnimationBridge>(player);
        if (bridge != null && bridge.Animator == null)
            bridge.Animator = player.GetComponentInChildren<Animator>(true);
        if (bridge != null && bridge.VisualRoot == null && bridge.Animator != null)
            bridge.VisualRoot = bridge.Animator.transform;
        return bridge;
    }

    private static void EnsureStarterWeaponLoadout(OUTL_EquipmentRuntime equipmentRuntime, OUTL_AttackDriver attack)
    {
        if (equipmentRuntime == null || attack == null) return;
        EnsurePlayerTemplateFolder();

        OUTL_AttackProfile primary = EnsureAttackProfile("OUTL_Player_Primary_Hitscan.asset", "player.primary", OUTL_AttackMode.Hitscan, 18f, 45f, 0.10f, 0.20f);
        OUTL_AttackProfile secondary = EnsureAttackProfile("OUTL_Player_Secondary_HeavyHitscan.asset", "player.secondary", OUTL_AttackMode.Hitscan, 45f, 36f, 0.45f, 0.35f);
        OUTL_AttackProfile melee = EnsureAttackProfile("OUTL_Player_Melee_Crowbar.asset", "player.melee", OUTL_AttackMode.Melee, 25f, 1.85f, 0.25f, 0.75f);
        melee.MeleeArcDegrees = 150f;
        melee.MeleeMinRadius = 0.45f;
        melee.MeleeHeight = 1.45f;
        melee.MeleeForwardBias = 0.65f;

        OUTL_EquipmentItemDef primaryItem = EnsureEquipmentItem("OUTL_Player_Primary_Weapon.asset", "weapon_player_primary", "Primary Weapon", OUTL_EquipmentSlot.Primary, primary);
        OUTL_EquipmentItemDef secondaryItem = EnsureEquipmentItem("OUTL_Player_Secondary_Weapon.asset", "weapon_player_secondary", "Secondary Weapon", OUTL_EquipmentSlot.Secondary, secondary);
        OUTL_EquipmentItemDef meleeItem = EnsureEquipmentItem("OUTL_Player_Melee_Weapon.asset", "weapon_player_melee", "Melee Weapon", OUTL_EquipmentSlot.Melee, melee);

        attack.Primary = primary;
        attack.Secondary = secondary;
        attack.Melee = melee;
        equipmentRuntime.RequireInventoryForEquip = false;
        equipmentRuntime.ReturnUnequippedToInventory = false;
        equipmentRuntime.AutoEquipKnownItemsOnStart = true;
        equipmentRuntime.AutoEquipOnlyEmptySlots = true;
        equipmentRuntime.KnownItems = new[] { primaryItem, secondaryItem, meleeItem };

        EditorUtility.SetDirty(primary);
        EditorUtility.SetDirty(secondary);
        EditorUtility.SetDirty(melee);
        EditorUtility.SetDirty(primaryItem);
        EditorUtility.SetDirty(secondaryItem);
        EditorUtility.SetDirty(meleeItem);
        AssetDatabase.SaveAssets();
    }

    private static OUTL_AttackProfile EnsureAttackProfile(string fileName, string attackId, OUTL_AttackMode mode, float damage, float range, float cooldown, float radius)
    {
        string path = PlayerTemplateFolder + "/" + fileName;
        OUTL_AttackProfile profile = AssetDatabase.LoadAssetAtPath<OUTL_AttackProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<OUTL_AttackProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }
        profile.AttackId = attackId;
        profile.Mode = mode;
        profile.Damage = damage;
        profile.Range = range;
        profile.Cooldown = cooldown;
        profile.Radius = radius;
        profile.HitDamageKey = attackId;
        profile.HitMask = ~0;
        profile.ProjectileIgnoreTriggers = true;
        return profile;
    }

    private static OUTL_EquipmentItemDef EnsureEquipmentItem(string fileName, string className, string displayName, OUTL_EquipmentSlot slot, OUTL_AttackProfile profile)
    {
        string path = PlayerTemplateFolder + "/" + fileName;
        OUTL_EquipmentItemDef item = AssetDatabase.LoadAssetAtPath<OUTL_EquipmentItemDef>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<OUTL_EquipmentItemDef>();
            AssetDatabase.CreateAsset(item, path);
        }
        item.ClassName = className;
        item.DisplayName = displayName;
        item.MaxStack = 1;
        item.Equippable = true;
        item.Slot = slot;
        item.AttackProfile = profile;
        return item;
    }

    private static void EnsurePlayerTemplateFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/OUT/OUT_Core/OUT_CORE_Lite/Templates"))
            AssetDatabase.CreateFolder("Assets/OUT/OUT_Core/OUT_CORE_Lite", "Templates");
        if (!AssetDatabase.IsValidFolder(PlayerTemplateFolder))
            AssetDatabase.CreateFolder("Assets/OUT/OUT_Core/OUT_CORE_Lite/Templates", "Player");
    }

    private static void EnsureAITemplateFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/OUT/OUT_Core/OUT_CORE_Lite/Templates"))
            AssetDatabase.CreateFolder("Assets/OUT/OUT_Core/OUT_CORE_Lite", "Templates");
        if (!AssetDatabase.IsValidFolder(AITemplateFolder))
            AssetDatabase.CreateFolder("Assets/OUT/OUT_Core/OUT_CORE_Lite/Templates", "AI");
    }

    private static void EnsurePlayerBindings(OUTL_UIDataBinder binder, OUTL_BasicHUD basicHud)
    {
        if (binder == null) return;
        Text hp = basicHud != null ? basicHud.HealthText : null;
        binder.Bindings = new[]
        {
            new OUTL_UIDataBinding { Id = "Health", Kind = OUTL_UIDataKind.Stat, Key = "Health", Label = "HP", Format = "{0}: {1}", TargetText = hp, WarningWhenLessOrEqual = true, WarningLessOrEqual = 25f },
            new OUTL_UIDataBinding { Id = "Armor", Kind = OUTL_UIDataKind.Stat, Key = "Armor", Label = "ARM", Format = "{0}: {1}", TargetText = basicHud != null ? basicHud.ArmorText : null, WarningWhenLessOrEqual = false },
            new OUTL_UIDataBinding { Id = "State", Kind = OUTL_UIDataKind.DeadState, Key = "Dead", Label = "STATE", Format = "{0}: {1}", TargetText = basicHud != null ? basicHud.StateText : null, HideWhenEmpty = false },
            new OUTL_UIDataBinding { Id = "Primary", Kind = OUTL_UIDataKind.EquipmentSlot, Key = "Primary", Label = "PRI", Format = "{0}: {1}", TargetText = basicHud != null ? basicHud.PrimaryText : null },
            new OUTL_UIDataBinding { Id = "Secondary", Kind = OUTL_UIDataKind.EquipmentSlot, Key = "Secondary", Label = "SEC", Format = "{0}: {1}", TargetText = basicHud != null ? basicHud.SecondaryText : null },
            new OUTL_UIDataBinding { Id = "Melee", Kind = OUTL_UIDataKind.EquipmentSlot, Key = "Melee", Label = "MEL", Format = "{0}: {1}", TargetText = basicHud != null ? basicHud.MeleeText : null },
            new OUTL_UIDataBinding { Id = "Score", Kind = OUTL_UIDataKind.Stat, Key = OUTL_LoopKeys.Score, Label = "SCORE", Format = "{0}: {1}", TargetText = basicHud != null ? basicHud.ScoreText : null },
            new OUTL_UIDataBinding { Id = "XP", Kind = OUTL_UIDataKind.Stat, Key = OUTL_LoopKeys.XP, Label = "XP", Format = "{0}: {1}", TargetText = basicHud != null ? basicHud.XPText : null },
            new OUTL_UIDataBinding { Id = "Tier", Kind = OUTL_UIDataKind.Tier, Key = "Tier", Label = "TIER", Format = "{0}: {1}", TargetText = basicHud != null ? basicHud.TierText : null }
        };
    }

    private static Camera EnsurePlayerCamera(Transform player)
    {
        if (player == null) return null;
        Camera camera = player.GetComponentInChildren<Camera>(true);
        if (camera != null) return camera;
        GameObject go = new GameObject("OUTL_ViewCamera");
        Undo.RegisterCreatedObjectUndo(go, "Create OUTL View Camera");
        go.transform.SetParent(player, false);
        go.transform.localPosition = new Vector3(0f, 1.62f, 0f);
        go.transform.localRotation = Quaternion.identity;
        return go.AddComponent<Camera>();
    }

    private static Transform EnsureMuzzle(Transform parent)
    {
        if (parent == null) return null;
        Transform existing = parent.Find("Muzzle");
        if (existing != null) return existing;
        GameObject go = new GameObject("Muzzle");
        Undo.RegisterCreatedObjectUndo(go, "Create OUTL Muzzle");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, -0.08f, 0.45f);
        go.transform.localRotation = Quaternion.identity;
        return go.transform;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;
        GameObject go = new GameObject("EventSystem");
        Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }
}
#endif
