#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public static class OUTL_NPCPresetWorkbenchEditor
{
    private const string MenuRoot = "OUT CORE Lite/Legacy Demo/Workbench/";
    private const string Folder = "Assets/OUT/OUT_Core/OUT_CORE_Lite/Templates/NPC_Presets";

    // [MenuItem(MenuRoot + "Create Quake Lite NPC Combat Slice")]
    public static void CreateQuakeLiteNPCCombatSlice()
    {
        CreateNPCFactionCombatPack();
    }

    // [MenuItem(MenuRoot + "Create NPC Faction Combat Pack")]
    public static void CreateNPCFactionCombatPack()
    {
        EnsureFolder(Folder);
        EnsureRuntime();

        OUTL_FactionDef player = GetOrCreateFaction("OUTL_Faction_Player", "player", "Player");
        OUTL_FactionDef friend = GetOrCreateFaction("OUTL_Faction_Friendly", "friendly", "Friendly");
        OUTL_FactionDef enemy = GetOrCreateFaction("OUTL_Faction_Hostile", "hostile", "Hostile");
        OUTL_FactionDef neutral = GetOrCreateFaction("OUTL_Faction_Neutral", "neutral", "Neutral");
        ConfigureRelations(player, friend, enemy, neutral);
        ConfigureRelations(friend, player, enemy, neutral);
        ConfigureRelations(enemy, neutral, player, friend);
        ConfigureNeutral(neutral, player, friend, enemy);
        EnsurePlayerCombatTarget(player);

        OUTL_AttackProfile meleeLight = GetOrCreateAttack("OUTL_Attack_NPC_MeleeLight", "npc.melee.light", OUTL_AttackMode.Melee, 9f, 1.65f, 0.75f, 0.75f, 0f, false);
        OUTL_AttackProfile meleeHeavy = GetOrCreateAttack("OUTL_Attack_NPC_MeleeHeavy", "npc.melee.heavy", OUTL_AttackMode.Melee, 18f, 1.9f, 0.9f, 1.15f, 0f, false);
        OUTL_AttackProfile rangedHitscan = GetOrCreateAttack("OUTL_Attack_NPC_RangedHitscan", "npc.ranged.hitscan", OUTL_AttackMode.Hitscan, 8f, 42f, 0.12f, 0.42f, 0f, false);
        OUTL_AttackProfile rangedProjectile = GetOrCreateAttack("OUTL_Attack_NPC_RangedProjectile", "npc.ranged.projectile", OUTL_AttackMode.Projectile, 14f, 38f, 0.16f, 0.95f, 25f, true);
        OUTL_AttackProfile supportPulse = GetOrCreateAttack("OUTL_Attack_NPC_SupportPulse", "npc.support.pulse", OUTL_AttackMode.Hitscan, 4f, 30f, 0.2f, 0.7f, 0f, false);

        OUTL_AIProfile meleeProfile = GetOrCreateAIProfile("OUTL_AI_Profile_NPC_MeleeAggressor", "ai.npc.melee_aggressor", 32f, 1.8f, 3.8f, CreateIdleSchedule("Melee"), CreateMeleeCombatSchedule(), CreateSearchSchedule("Melee"), CreateFleeSchedule("Melee"));
        OUTL_AIProfile rangedProfile = GetOrCreateAIProfile("OUTL_AI_Profile_NPC_RangedCover", "ai.npc.ranged_cover", 45f, 28f, 3.25f, CreateIdleSchedule("Ranged"), CreateRangedCombatSchedule(), CreateSearchSchedule("Ranged"), CreateFleeSchedule("Ranged"));
        OUTL_AIProfile skirmisherProfile = GetOrCreateAIProfile("OUTL_AI_Profile_NPC_Skirmisher", "ai.npc.skirmisher", 38f, 18f, 4.2f, CreateIdleSchedule("Skirmisher"), CreateSkirmisherCombatSchedule(), CreateSearchSchedule("Skirmisher"), CreateFleeSchedule("Skirmisher"));
        OUTL_AIProfile guardProfile = GetOrCreateAIProfile("OUTL_AI_Profile_NPC_Guard", "ai.npc.guard", 34f, 22f, 2.7f, CreateIdleSchedule("Guard"), CreateGuardCombatSchedule(), CreateSearchSchedule("Guard"), CreateFleeSchedule("Guard"));

        OUTL_EntityDef enemyMeleeDef = GetOrCreateEntityDef("OUTL_Def_NPC_Hostile_Melee", "npc_hostile_melee", "Hostile Melee NPC", new[] { "Actor", "NPC", "Enemy", "Melee", "Role.Opposition" }, 70f, 12f, 3.8f);
        OUTL_EntityDef enemyRangedDef = GetOrCreateEntityDef("OUTL_Def_NPC_Hostile_Ranged", "npc_hostile_ranged", "Hostile Ranged NPC", new[] { "Actor", "NPC", "Enemy", "Ranged", "Role.Opposition" }, 55f, 8f, 3.2f);
        OUTL_EntityDef enemySkirmDef = GetOrCreateEntityDef("OUTL_Def_NPC_Hostile_Skirmisher", "npc_hostile_skirmisher", "Hostile Skirmisher NPC", new[] { "Actor", "NPC", "Enemy", "Ranged", "Melee", "Role.Opposition" }, 62f, 10f, 4.2f);
        OUTL_EntityDef friendMeleeDef = GetOrCreateEntityDef("OUTL_Def_NPC_Friendly_Melee", "npc_friendly_melee", "Friendly Melee NPC", new[] { "Actor", "NPC", "Friendly", "Melee" }, 75f, 11f, 3.6f);
        OUTL_EntityDef friendRangedDef = GetOrCreateEntityDef("OUTL_Def_NPC_Friendly_Ranged", "npc_friendly_ranged", "Friendly Ranged NPC", new[] { "Actor", "NPC", "Friendly", "Ranged" }, 60f, 8f, 3.1f);
        OUTL_EntityDef neutralGuardDef = GetOrCreateEntityDef("OUTL_Def_NPC_Neutral_Guard", "npc_neutral_guard", "Neutral Guard NPC", new[] { "Actor", "NPC", "Neutral", "Guard" }, 80f, 10f, 2.7f);

        SpawnNPC("OUTL_NPC_Hostile_Melee", enemyMeleeDef, enemy, meleeProfile, meleeLight, meleeHeavy, new Vector3(3f, 1f, 2f));
        SpawnNPC("OUTL_NPC_Hostile_Ranged", enemyRangedDef, enemy, rangedProfile, rangedHitscan, meleeLight, new Vector3(6f, 1f, 3f));
        SpawnNPC("OUTL_NPC_Hostile_Projectile", enemySkirmDef, enemy, skirmisherProfile, rangedProjectile, meleeLight, new Vector3(9f, 1f, 3f));
        SpawnNPC("OUTL_NPC_Friendly_Melee", friendMeleeDef, friend, meleeProfile, meleeLight, meleeHeavy, new Vector3(-3f, 1f, 2f));
        SpawnNPC("OUTL_NPC_Friendly_Ranged", friendRangedDef, friend, rangedProfile, supportPulse, meleeLight, new Vector3(-6f, 1f, 3f));
        SpawnNPC("OUTL_NPC_Neutral_Guard", neutralGuardDef, neutral, guardProfile, rangedHitscan, meleeLight, new Vector3(0f, 1f, 6f));

        GameObject runtime = GameObject.Find("OUTL_Runtime");
        if (runtime != null && runtime.GetComponent<OUTL_NPCWorkbenchHUD>() == null) runtime.AddComponent<OUTL_NPCWorkbenchHUD>();

        AssetDatabase.SaveAssets();
        Debug.Log("OUTL NPC combat slice created. Press Play: hostile NPCs have factions, vitals, death handlers, melee/ranged attacks, fallback movement and chunk-tiered AI budget.");
    }

    // [MenuItem(MenuRoot + "Repair Selected NPC Preset Combat Stack")]
    public static void RepairSelectedNPCCombatStack()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null)
        {
            Debug.LogWarning("OUTL: select an NPC GameObject to repair.");
            return;
        }

        EnsureFolder(Folder);
        EnsureRuntime();
        OUTL_FactionDef player = GetOrCreateFaction("OUTL_Faction_Player", "player", "Player");
        OUTL_FactionDef enemy = GetOrCreateFaction("OUTL_Faction_Hostile", "hostile", "Hostile");
        OUTL_FactionDef neutral = GetOrCreateFaction("OUTL_Faction_Neutral", "neutral", "Neutral");
        ConfigureRelations(enemy, neutral, player, neutral);

        OUTL_AttackProfile melee = GetOrCreateAttack("OUTL_Attack_NPC_MeleeLight", "npc.melee.light", OUTL_AttackMode.Melee, 9f, 1.75f, 0.85f, 0.55f, 0f, false);
        OUTL_AIProfile profile = GetOrCreateAIProfile("OUTL_AI_Profile_NPC_MeleeAggressor", "ai.npc.melee_aggressor", 32f, 1.85f, 3.8f, CreateIdleSchedule("Melee"), CreateMeleeCombatSchedule(), CreateSearchSchedule("Melee"), CreateFleeSchedule("Melee"));
        OUTL_EntityDef def = GetOrCreateEntityDef("OUTL_Def_NPC_Hostile_Melee", "npc_hostile_melee", "Hostile Melee NPC", new[] { "Actor", "NPC", "Enemy", "Melee", "Role.Opposition" }, 70f, 12f, 3.8f);
        ConfigureNPCStack(go, def, enemy, profile, melee, melee);
        Selection.activeGameObject = go;
        AssetDatabase.SaveAssets();
        Debug.Log("OUTL selected NPC combat stack repaired: " + go.name);
    }

    private static void EnsureRuntime()
    {
        GameObject runtime = GameObject.Find("OUTL_Runtime");
        if (runtime == null)
        {
            runtime = new GameObject("OUTL_Runtime");
            Undo.RegisterCreatedObjectUndo(runtime, "Create OUTL Runtime");
        }
        if (runtime.GetComponent<OUTL_World>() == null) runtime.AddComponent<OUTL_World>();
        if (runtime.GetComponent<OUTL_DevConsole>() == null) runtime.AddComponent<OUTL_DevConsole>();
        if (runtime.GetComponent<OUTL_PoolSystem>() == null) runtime.AddComponent<OUTL_PoolSystem>();
        if (runtime.GetComponent<OUTL_SaveSpawnResolverRegistry>() == null) runtime.AddComponent<OUTL_SaveSpawnResolverRegistry>();
        if (runtime.GetComponent<OUTL_GameLoopRunner>() == null) runtime.AddComponent<OUTL_GameLoopRunner>();
        if (runtime.GetComponent<OUTL_GameLoopGoldenTester>() == null) runtime.AddComponent<OUTL_GameLoopGoldenTester>();
        OUTL_ChunkProcessingDriver chunk = runtime.GetComponent<OUTL_ChunkProcessingDriver>();
        if (chunk == null) chunk = runtime.AddComponent<OUTL_ChunkProcessingDriver>();
        chunk.BuiltInPreset = OUTL_ProcessingBuiltInPreset.StreamingWorld;
        chunk.ApplyPresetOnEnable = true;
        chunk.OverrideDriverTickInterval = true;
        chunk.DriverTickInterval = 0.25f;
        chunk.OverrideEntitiesPerTick = true;
        chunk.EntitiesPerTick = 256;
        chunk.CacheRegistrySnapshot = true;
        chunk.FullRefreshInterval = 1.5f;
        OUTL_ChunkDebugView debug = runtime.GetComponent<OUTL_ChunkDebugView>();
        if (debug == null) debug = runtime.AddComponent<OUTL_ChunkDebugView>();
        debug.Driver = chunk;
        OUTL_GoldenTestRunner golden = runtime.GetComponent<OUTL_GoldenTestRunner>();
        if (golden == null) golden = runtime.AddComponent<OUTL_GoldenTestRunner>();
        golden.ChunkDriver = chunk;
        if (runtime.GetComponent<OUTL_NPCWorkbenchHUD>() == null) runtime.AddComponent<OUTL_NPCWorkbenchHUD>();
    }

    private static GameObject EnsurePlayerCombatTarget(OUTL_FactionDef playerFaction)
    {
        OUTL_BasicPlayerController existingController = Object.FindObjectOfType<OUTL_BasicPlayerController>(true);
        GameObject go = existingController != null ? existingController.gameObject : null;
        if (go == null)
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "OUTL_Combat_Player";
            go.transform.position = new Vector3(0f, 1f, -4f);
            Undo.RegisterCreatedObjectUndo(go, "Create OUTL Combat Player");
        }

        OUTL_EntityDef playerDef = GetOrCreateEntityDef("OUTL_Def_Player_Combat", "player", "Combat Player", new[] { "Player", "Actor", "Human", "Controllable" }, 100f, 20f, 5f);
        OUTL_EntityAdapter entity = go.GetComponent<OUTL_EntityAdapter>();
        if (entity == null) entity = go.AddComponent<OUTL_EntityAdapter>();
        entity.Def = playerDef;
        entity.Faction = playerFaction;
        entity.ClassNameOverride = "player";
        entity.TargetName = string.IsNullOrEmpty(entity.TargetName) ? "player" : entity.TargetName;
        entity.StableId = string.IsNullOrEmpty(entity.StableId) ? "player_combat" : entity.StableId;
        entity.SavePersistent = true;
        entity.TickLane = OUTL_TickLane.Full;
        entity.TickInterval = 0.05f;
        entity.RegisterInSectors = true;
        entity.MarkAddressDirty();

        CharacterController character = go.GetComponent<CharacterController>();
        if (character == null) character = go.AddComponent<CharacterController>();
        character.height = 1.8f;
        character.radius = 0.32f;
        character.center = new Vector3(0f, 0.9f, 0f);

        Camera camera = go.GetComponentInChildren<Camera>(true);
        if (camera == null)
        {
            GameObject cam = new GameObject("OUTL_ViewCamera");
            cam.transform.SetParent(go.transform, false);
            cam.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            camera = cam.AddComponent<Camera>();
            if (Object.FindObjectOfType<AudioListener>() == null) cam.AddComponent<AudioListener>();
        }

        Transform muzzle = EnsureChild(camera != null ? camera.transform : go.transform, "Muzzle", new Vector3(0f, -0.08f, 0.45f));
        OUTL_AttackProfile primary = GetOrCreateAttack("OUTL_Attack_Player_CombatHitscan", "player.combat.hitscan", OUTL_AttackMode.Hitscan, 22f, 80f, 0.18f, 0.12f, 0f, false);
        OUTL_AttackProfile melee = GetOrCreateAttack("OUTL_Attack_Player_CombatMelee", "player.combat.melee", OUTL_AttackMode.Melee, 28f, 1.9f, 0.8f, 0.45f, 0f, false);
        OUTL_AttackDriver attack = go.GetComponent<OUTL_AttackDriver>();
        if (attack == null) attack = go.AddComponent<OUTL_AttackDriver>();
        attack.Source = entity;
        attack.AimCamera = camera;
        attack.Muzzle = muzzle;
        attack.Primary = primary;
        attack.Melee = melee;
        attack.UseCapsuleMeleeVolume = true;

        OUTL_DamageReceiver damage = go.GetComponent<OUTL_DamageReceiver>();
        if (damage == null) damage = go.AddComponent<OUTL_DamageReceiver>();
        damage.Entity = entity;

        OUTL_Hitbox hitbox = go.GetComponent<OUTL_Hitbox>();
        if (hitbox == null) hitbox = go.AddComponent<OUTL_Hitbox>();
        hitbox.Entity = entity;
        hitbox.Zone = OUTL_HitboxZone.Torso;

        OUTL_Vitals actorVitals = go.GetComponent<OUTL_Vitals>();
        if (actorVitals == null) actorVitals = go.AddComponent<OUTL_Vitals>();
        actorVitals.Entity = entity;
        actorVitals.InitializeMissingStats = true;
        actorVitals.DefaultHealth = 100f;
        actorVitals.DefaultMaxHealth = 100f;

        OUTL_DeathHandler death = go.GetComponent<OUTL_DeathHandler>();
        if (death == null) death = go.AddComponent<OUTL_DeathHandler>();
        death.Entity = entity;
        death.QueueDespawn = false;
        death.DisableColliders = false;
        death.DisableRenderers = false;

        OUTL_VitalsBootstrap vitals = go.GetComponent<OUTL_VitalsBootstrap>();
        if (vitals == null) vitals = go.AddComponent<OUTL_VitalsBootstrap>();
        vitals.Entity = entity;
        vitals.AddVitalsIfMissing = true;
        vitals.AddDeathHandlerIfMissing = true;
        vitals.AddUIBinderIfPlayer = true;
        vitals.Ensure();

        OUTL_BasicPlayerController controller = go.GetComponent<OUTL_BasicPlayerController>();
        if (controller == null) controller = go.AddComponent<OUTL_BasicPlayerController>();
        controller.Entity = entity;
        controller.CharacterController = character;
        controller.AttackDriver = attack;
        controller.ViewCamera = camera;

        OUTL_BasicHUD hud = go.GetComponent<OUTL_BasicHUD>();
        if (hud == null) hud = go.AddComponent<OUTL_BasicHUD>();
        hud.Player = entity;
        hud.Controller = controller;
        hud.AutoCreateUI = true;
        hud.AutoAddDataBinder = true;
        hud.EnsureUI();
        hud.EnsureBinder();

        OUTL_UICollectionBinder equipment = go.GetComponent<OUTL_UICollectionBinder>();
        if (equipment == null) equipment = go.AddComponent<OUTL_UICollectionBinder>();
        equipment.Entity = entity;
        equipment.Source = OUTL_UICollectionSource.EquipmentSlots;
        equipment.Root = hud.EquipmentRoot;
        equipment.AutoCreateRows = true;
        equipment.MaxRows = 6;

        entity.RebuildCommandReceiverCache();
        Colorize(go, new Color(0.15f, 0.35f, 0.9f, 1f));
        EditorUtility.SetDirty(go);
        EditorUtility.SetDirty(entity);
        return go;
    }

    private static GameObject SpawnNPC(string name, OUTL_EntityDef def, OUTL_FactionDef faction, OUTL_AIProfile profile, OUTL_AttackProfile primary, OUTL_AttackProfile melee, Vector3 position)
    {
        GameObject existing = GameObject.Find(name);
        GameObject go = existing != null ? existing : GameObject.CreatePrimitive(PrimitiveType.Capsule);
        if (existing == null)
        {
            go.name = name;
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.position = position;
        }

        ConfigureNPCStack(go, def, faction, profile, primary, melee);
        return go;
    }

    private static void ConfigureNPCStack(GameObject go, OUTL_EntityDef def, OUTL_FactionDef faction, OUTL_AIProfile profile, OUTL_AttackProfile primary, OUTL_AttackProfile melee)
    {
        if (go == null) return;
        string stableName = go.name.ToLowerInvariant();

        OUTL_EntityAdapter entity = go.GetComponent<OUTL_EntityAdapter>();
        if (entity == null) entity = go.AddComponent<OUTL_EntityAdapter>();
        entity.Def = def;
        entity.Faction = faction;
        entity.ClassNameOverride = def != null ? def.ClassName : "npc_hostile";
        if (string.IsNullOrEmpty(entity.TargetName)) entity.TargetName = stableName;
        if (string.IsNullOrEmpty(entity.StableId)) entity.StableId = stableName;
        entity.SavePersistent = true;
        entity.TickLane = OUTL_TickLane.AI;
        entity.TickInterval = 0.15f;
        entity.RegisterInSectors = true;
        entity.MarkAddressDirty();

        OUTL_DamageReceiver receiver = go.GetComponent<OUTL_DamageReceiver>();
        if (receiver == null) receiver = go.AddComponent<OUTL_DamageReceiver>();
        receiver.Entity = entity;

        OUTL_Hitbox hitbox = go.GetComponent<OUTL_Hitbox>();
        if (hitbox == null) hitbox = go.AddComponent<OUTL_Hitbox>();
        hitbox.Entity = entity;
        hitbox.Zone = OUTL_HitboxZone.Torso;

        OUTL_Vitals actorVitals = go.GetComponent<OUTL_Vitals>();
        if (actorVitals == null) actorVitals = go.AddComponent<OUTL_Vitals>();
        actorVitals.Entity = entity;
        actorVitals.InitializeMissingStats = true;
        actorVitals.DefaultHealth = 100f;
        actorVitals.DefaultMaxHealth = 100f;

        OUTL_DeathHandler death = go.GetComponent<OUTL_DeathHandler>();
        if (death == null) death = go.AddComponent<OUTL_DeathHandler>();
        death.Entity = entity;
        death.DisableAI = true;
        death.DisableColliders = true;
        death.DisableRenderers = false;
        death.QueueDespawn = true;
        death.DespawnDelay = 4f;

        OUTL_VitalsBootstrap vitals = go.GetComponent<OUTL_VitalsBootstrap>();
        if (vitals == null) vitals = go.AddComponent<OUTL_VitalsBootstrap>();
        vitals.Entity = entity;
        vitals.AddVitalsIfMissing = true;
        vitals.AddDeathHandlerIfMissing = true;
        vitals.AddUIBinderIfPlayer = false;
        vitals.Ensure();

        NavMeshAgent nav = go.GetComponent<NavMeshAgent>();
        if (nav == null) nav = go.AddComponent<NavMeshAgent>();
        nav.speed = profile != null ? profile.MoveSpeed : 3.2f;
        nav.angularSpeed = 540f;
        nav.acceleration = 18f;
        nav.stoppingDistance = profile != null ? Mathf.Max(0.9f, profile.AttackDistance * 0.72f) : 1.2f;
        nav.autoBraking = false;

        OUTL_NavMeshMover mover = go.GetComponent<OUTL_NavMeshMover>();
        if (mover == null) mover = go.AddComponent<OUTL_NavMeshMover>();
        mover.Agent = nav;
        mover.AutoFindAgent = true;
        mover.UseOUTLTick = true;
        mover.TickLane = OUTL_TickLane.Logic;
        mover.TickInterval = 0.06f;
        mover.RepathInterval = 0.18f;
        mover.FallbackSpeed = profile != null ? profile.MoveSpeed : 3.2f;
        mover.StopDistance = profile != null ? Mathf.Min(1.45f, Mathf.Max(0.85f, profile.AttackDistance * 0.65f)) : 1.1f;

        Transform muzzle = EnsureChild(go.transform, "Muzzle", new Vector3(0f, 1.35f, 0.55f));
        OUTL_AttackDriver attack = go.GetComponent<OUTL_AttackDriver>();
        if (attack == null) attack = go.AddComponent<OUTL_AttackDriver>();
        attack.Source = entity;
        attack.Muzzle = muzzle;
        attack.Primary = primary != null ? primary : melee;
        attack.Melee = melee != null ? melee : primary;
        attack.SmartMeleeWhenFireAtPrimary = true;
        attack.RespectCooldownOnFireAt = true;
        attack.UseCapsuleMeleeVolume = true;
        attack.MeleeHeight = 1.45f;
        attack.MeleeForwardBias = 0.65f;

        OUTL_AIActor ai = go.GetComponent<OUTL_AIActor>();
        if (ai == null) ai = go.AddComponent<OUTL_AIActor>();
        ai.Profile = profile;
        ai.Entity = entity;
        ai.MoveRoot = go.transform;
        ai.NavMover = mover;
        ai.AttackDriver = attack;
        ai.UseNavMeshMover = true;
        ai.UseAttackDriver = true;
        ai.MoveToTarget = true;
        ai.RequireLineOfSightToAcquireTarget = false;
        ai.RequireLineOfSightToKeepTarget = false;
        ai.EyeHeight = 1.35f;
        ai.TargetEyeHeight = 1.1f;

        OUTL_HearingSensor hearing = go.GetComponent<OUTL_HearingSensor>();
        if (hearing == null) hearing = go.AddComponent<OUTL_HearingSensor>();
        hearing.Actor = ai;

        OUTL_EntityDiary diary = go.GetComponent<OUTL_EntityDiary>();
        if (diary == null) diary = go.AddComponent<OUTL_EntityDiary>();
        diary.Entity = entity;
        diary.WriteToFile = false;
        ai.Diary = diary;

        entity.RebuildCommandReceiverCache();
        Colorize(go, faction != null && faction.FactionId == "friendly" ? new Color(0.1f, 0.75f, 0.18f, 1f) : new Color(0.85f, 0.08f, 0.04f, 1f));
        EditorUtility.SetDirty(go);
        EditorUtility.SetDirty(entity);
        EditorUtility.SetDirty(attack);
        EditorUtility.SetDirty(ai);
    }

    private static OUTL_FactionDef GetOrCreateFaction(string assetName, string id, string displayName)
    {
        string path = Folder + "/" + assetName + ".asset";
        OUTL_FactionDef faction = AssetDatabase.LoadAssetAtPath<OUTL_FactionDef>(path);
        if (faction == null)
        {
            faction = ScriptableObject.CreateInstance<OUTL_FactionDef>();
            AssetDatabase.CreateAsset(faction, path);
        }
        faction.FactionId = id;
        faction.DisplayName = displayName;
        EditorUtility.SetDirty(faction);
        return faction;
    }

    private static void ConfigureRelations(OUTL_FactionDef faction, OUTL_FactionDef ally, OUTL_FactionDef enemyA, OUTL_FactionDef neutral)
    {
        faction.Relations = new[]
        {
            new OUTL_FactionRelation { Faction = faction, Relation = 1f },
            new OUTL_FactionRelation { Faction = ally, Relation = 0.75f },
            new OUTL_FactionRelation { Faction = enemyA, Relation = -1f },
            new OUTL_FactionRelation { Faction = neutral, Relation = 0f }
        };
        EditorUtility.SetDirty(faction);
    }

    private static void ConfigureNeutral(OUTL_FactionDef neutral, OUTL_FactionDef a, OUTL_FactionDef b, OUTL_FactionDef c)
    {
        neutral.Relations = new[]
        {
            new OUTL_FactionRelation { Faction = neutral, Relation = 1f },
            new OUTL_FactionRelation { Faction = a, Relation = 0f },
            new OUTL_FactionRelation { Faction = b, Relation = 0f },
            new OUTL_FactionRelation { Faction = c, Relation = 0f }
        };
        EditorUtility.SetDirty(neutral);
    }

    private static OUTL_EntityDef GetOrCreateEntityDef(string assetName, string className, string displayName, string[] tags, float health, float damage, float speed)
    {
        string path = Folder + "/" + assetName + ".asset";
        OUTL_EntityDef def = AssetDatabase.LoadAssetAtPath<OUTL_EntityDef>(path);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<OUTL_EntityDef>();
            AssetDatabase.CreateAsset(def, path);
        }
        def.ClassName = className;
        def.DisplayName = displayName;
        def.Tags = tags;
        def.BaseStats = new[]
        {
            new OUTL_StatEntry { Key = "Health", Value = health },
            new OUTL_StatEntry { Key = "MaxHealth", Value = health },
            new OUTL_StatEntry { Key = "Damage", Value = damage },
            new OUTL_StatEntry { Key = "Speed", Value = speed },
            new OUTL_StatEntry { Key = "Armor", Value = 0f }
        };
        EditorUtility.SetDirty(def);
        return def;
    }

    private static OUTL_AttackProfile GetOrCreateAttack(string assetName, string id, OUTL_AttackMode mode, float damage, float range, float radius, float cooldown, float projectileSpeed, bool projectileGravity)
    {
        string path = Folder + "/" + assetName + ".asset";
        OUTL_AttackProfile profile = AssetDatabase.LoadAssetAtPath<OUTL_AttackProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<OUTL_AttackProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }
        profile.AttackId = id;
        profile.Mode = mode;
        profile.Damage = damage;
        profile.Range = range;
        profile.Radius = radius;
        profile.Cooldown = cooldown;
        profile.ProjectileSpeed = projectileSpeed > 0f ? projectileSpeed : profile.ProjectileSpeed;
        profile.ProjectileUsesGravity = projectileGravity;
        profile.ProjectileGravity = 9.81f;
        profile.ProjectileLifetime = 6f;
        profile.AimMode = mode == OUTL_AttackMode.Projectile ? OUTL_AimMode.BallisticLowArc : OUTL_AimMode.PredictLinear;
        profile.UseTargetVelocityPrediction = true;
        profile.PredictionStrength = mode == OUTL_AttackMode.Projectile ? 0.7f : 0.45f;
        profile.MaxPredictionTime = 1.1f;
        profile.HorizontalSpreadDegrees = mode == OUTL_AttackMode.Melee ? 4f : 1.5f;
        profile.VerticalSpreadDegrees = mode == OUTL_AttackMode.Melee ? 2f : 0.8f;
        profile.HitDamageKey = mode == OUTL_AttackMode.Melee ? "melee" : "hit";
        profile.HitMask = ~0;
        if (mode == OUTL_AttackMode.Melee)
        {
            profile.MeleeArcDegrees = 145f;
            profile.MeleeMinRadius = Mathf.Max(0.55f, radius);
            profile.MeleeHeight = 1.45f;
            profile.MeleeForwardBias = 0.65f;
            profile.MeleeCanHitTriggers = false;
        }
        if (mode == OUTL_AttackMode.Projectile && profile.ProjectilePrefab == null)
            profile.ProjectilePrefab = CreateProjectilePrefab(assetName + "_ProjectilePrefab");
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static GameObject CreateProjectilePrefab(string assetName)
    {
        string path = Folder + "/" + assetName + ".prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = assetName;
        go.transform.localScale = Vector3.one * 0.18f;
        Collider col = go.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        go.AddComponent<OUTL_Projectile>();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        UnityEngine.Object.DestroyImmediate(go);
        return prefab;
    }

    private static OUTL_AIProfile GetOrCreateAIProfile(string assetName, string id, float view, float attack, float speed, OUTL_AIScheduleLite idle, OUTL_AIScheduleLite combat, OUTL_AIScheduleLite search, OUTL_AIScheduleLite flee)
    {
        string path = Folder + "/" + assetName + ".asset";
        OUTL_AIProfile profile = AssetDatabase.LoadAssetAtPath<OUTL_AIProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<OUTL_AIProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }
        profile.ProfileId = id;
        profile.UseFactionHostility = true;
        profile.EnemyTags = new[] { "Player", "Enemy", "Friendly" };
        profile.ViewDistance = view;
        profile.AttackDistance = attack;
        profile.MoveSpeed = speed;
        profile.ThinkIntervalNear = 0.1f;
        profile.ThinkIntervalMid = 0.35f;
        profile.ThinkIntervalFar = 1.25f;
        profile.LowHealthThreshold = 15f;
        profile.IdleSchedule = idle;
        profile.CombatSchedule = combat;
        profile.SearchSchedule = search;
        profile.FleeSchedule = flee;
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static OUTL_AIScheduleLite CreateIdleSchedule(string suffix)
    {
        return GetOrCreateSchedule("OUTL_AI_Idle_" + suffix, "idle." + suffix.ToLowerInvariant(), new[] { Task(OUTL_AITaskType.Wait, 0.6f, 0f), Task(OUTL_AITaskType.FindTarget, 0.05f, 0f) });
    }

    private static OUTL_AIScheduleLite CreateSearchSchedule(string suffix)
    {
        return GetOrCreateSchedule("OUTL_AI_Search_" + suffix, "search." + suffix.ToLowerInvariant(), new[] { Task(OUTL_AITaskType.InvestigateStimulus, 0.45f, 1.4f), Task(OUTL_AITaskType.Wait, 0.4f, 0f), Task(OUTL_AITaskType.FindTarget, 0.05f, 0f) });
    }

    private static OUTL_AIScheduleLite CreateFleeSchedule(string suffix)
    {
        return GetOrCreateSchedule("OUTL_AI_Flee_" + suffix, "flee." + suffix.ToLowerInvariant(), new[] { Task(OUTL_AITaskType.FleeFromTarget, 0.6f, 7f), Task(OUTL_AITaskType.Wait, 0.35f, 0f), Task(OUTL_AITaskType.FindTarget, 0.05f, 0f) });
    }

    private static OUTL_AIScheduleLite CreateMeleeCombatSchedule()
    {
        return GetOrCreateSchedule("OUTL_AI_Combat_Melee", "combat.melee", new[] { Task(OUTL_AITaskType.MoveToTarget, 0.2f, 1.4f), Task(OUTL_AITaskType.FaceTarget, 0.05f, 0f), Task(OUTL_AITaskType.AttackTarget, 0.05f, 0f), Task(OUTL_AITaskType.Wait, 0.25f, 0f) });
    }

    private static OUTL_AIScheduleLite CreateRangedCombatSchedule()
    {
        return GetOrCreateSchedule("OUTL_AI_Combat_RangedCover", "combat.ranged_cover", new[] { Task(OUTL_AITaskType.FindCover, 0.05f, 16f), Task(OUTL_AITaskType.MoveToCover, 0.45f, 1.1f), Task(OUTL_AITaskType.FaceTarget, 0.05f, 0f), Task(OUTL_AITaskType.AttackTarget, 0.05f, 0f), Task(OUTL_AITaskType.Wait, 0.35f, 0f) });
    }

    private static OUTL_AIScheduleLite CreateSkirmisherCombatSchedule()
    {
        return GetOrCreateSchedule("OUTL_AI_Combat_Skirmisher", "combat.skirmisher", new[] { Task(OUTL_AITaskType.MoveToTarget, 0.18f, 9f), Task(OUTL_AITaskType.FaceTarget, 0.05f, 0f), Task(OUTL_AITaskType.AttackTarget, 0.05f, 0f), Task(OUTL_AITaskType.FleeFromTarget, 0.25f, 7f), Task(OUTL_AITaskType.Wait, 0.25f, 0f) });
    }

    private static OUTL_AIScheduleLite CreateGuardCombatSchedule()
    {
        return GetOrCreateSchedule("OUTL_AI_Combat_Guard", "combat.guard", new[] { Task(OUTL_AITaskType.FaceTarget, 0.05f, 0f), Task(OUTL_AITaskType.AttackTarget, 0.05f, 0f), Task(OUTL_AITaskType.Wait, 0.5f, 0f), Task(OUTL_AITaskType.FindTarget, 0.05f, 0f) });
    }

    private static OUTL_AIScheduleLite GetOrCreateSchedule(string assetName, string id, OUTL_AITaskDef[] tasks)
    {
        string path = Folder + "/" + assetName + ".asset";
        OUTL_AIScheduleLite schedule = AssetDatabase.LoadAssetAtPath<OUTL_AIScheduleLite>(path);
        if (schedule == null)
        {
            schedule = ScriptableObject.CreateInstance<OUTL_AIScheduleLite>();
            AssetDatabase.CreateAsset(schedule, path);
        }
        schedule.ScheduleId = id;
        schedule.Loop = true;
        schedule.Tasks = tasks;
        EditorUtility.SetDirty(schedule);
        return schedule;
    }

    private static OUTL_AITaskDef Task(OUTL_AITaskType type, float duration, float distance)
    {
        return new OUTL_AITaskDef { Type = type, Duration = duration, Distance = distance, SpeedMultiplier = 1f, Mask = ~0 };
    }

    private static Transform EnsureChild(Transform parent, string name, Vector3 localPosition)
    {
        if (parent == null) return null;
        Transform existing = parent.Find(name);
        if (existing != null) return existing;
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create OUTL " + name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;
        return go.transform;
    }

    private static void Colorize(GameObject go, Color color)
    {
        if (go == null) return;
        Renderer renderer = go.GetComponentInChildren<Renderer>();
        if (renderer == null) return;
        Material material = renderer.sharedMaterial;
        if (material == null || AssetDatabase.Contains(material))
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");
            if (shader == null) return;
            material = new Material(shader);
            renderer.sharedMaterial = material;
        }
        material.color = color;
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
