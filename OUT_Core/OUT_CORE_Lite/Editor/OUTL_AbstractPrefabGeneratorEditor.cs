#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public static class OUTL_AbstractPrefabGeneratorEditor
{
    private const string RootFolder = "Assets/OUT/OUT_Core/OUT_CORE_Lite/Templates/Foundation";
    private const string PrefabFolder = RootFolder + "/Prefabs";
    private const string DefFolder = RootFolder + "/Defs";
    private const string CombatFolder = RootFolder + "/Combat";
    private const string AIFolder = RootFolder + "/AI";
    private const string NPCFolder = RootFolder + "/NPC";
    private const string ItemFolder = RootFolder + "/Items";
    private const string LootFolder = RootFolder + "/Loot";

    public static void GenerateAllFoundationPrefabs()
    {
        EnsureFolders();

        FoundationContext ctx = new FoundationContext();
        ctx.NeutralFaction = EnsureFaction("OUTL_Faction_Neutral.asset", "faction.neutral", "Neutral");
        ctx.ControlledFaction = EnsureFaction("OUTL_Faction_Controlled.asset", "faction.controlled", "Controlled");
        ctx.GroupAFaction = EnsureFaction("OUTL_Faction_GroupA.asset", "faction.group_a", "Group A");
        ctx.GroupBFaction = EnsureFaction("OUTL_Faction_GroupB.asset", "faction.group_b", "Group B");
        ConfigureFactionRelations(ctx);

        ctx.GenericProjectilePrefab = CreateProjectilePrefab(ctx);
        ctx.HitscanAttack = EnsureAttackProfile("OUTL_Attack_Generic_Hitscan.asset", "attack.generic.hitscan", OUTL_AttackMode.Hitscan, "kinetic", 18f, 55f, 0.16f, 0.12f);
        ctx.ProjectileAttack = EnsureAttackProfile("OUTL_Attack_Generic_Projectile.asset", "attack.generic.projectile", OUTL_AttackMode.Projectile, "projectile", 16f, 42f, 0.55f, 0.14f);
        ctx.ProjectileAttack.ProjectilePrefab = ctx.GenericProjectilePrefab;
        ctx.ProjectileAttack.ProjectileSpeed = 28f;
        ctx.ProjectileAttack.ProjectileLifetime = 6f;
        ctx.ProjectileAttack.AimMode = OUTL_AimMode.PredictLinear;
        ctx.ProjectileAttack.UseTargetVelocityPrediction = true;
        ctx.ProjectileAttack.PredictionStrength = 0.65f;
        ctx.ProjectileAttack.MaxPredictionTime = 1f;
        ctx.MeleeAttack = EnsureAttackProfile("OUTL_Attack_Generic_Melee.asset", "attack.generic.melee", OUTL_AttackMode.Melee, "melee", 12f, 1.9f, 0.6f, 0.75f);
        ctx.MeleeAttack.MeleeArcDegrees = 145f;
        ctx.MeleeAttack.MeleeMinRadius = 0.45f;
        ctx.MeleeAttack.MeleeHeight = 1.45f;
        ctx.MeleeAttack.MeleeForwardBias = 0.6f;

        ctx.PrimaryWeapon = EnsureEquipmentItem("OUTL_Item_Generic_PrimaryWeapon.asset", "item.weapon.primary_generic", "Generic Primary Weapon", OUTL_EquipmentSlot.Primary, ctx.HitscanAttack);
        ctx.SecondaryWeapon = EnsureEquipmentItem("OUTL_Item_Generic_ProjectileWeapon.asset", "item.weapon.projectile_generic", "Generic Projectile Weapon", OUTL_EquipmentSlot.Secondary, ctx.ProjectileAttack);
        ctx.MeleeWeapon = EnsureEquipmentItem("OUTL_Item_Generic_MeleeWeapon.asset", "item.weapon.melee_generic", "Generic Melee Weapon", OUTL_EquipmentSlot.Melee, ctx.MeleeAttack);
        ctx.GenericPickupItem = EnsureItem("OUTL_Item_Generic_Pickup.asset", "item.generic_pickup", "Generic Pickup", 16);

        ctx.AimProfile = EnsureAimProfile("OUTL_Aim_Generic_Combat.asset", "aim.generic.combat");
        ctx.PrimaryUseProfile = EnsureWeaponUseProfile("OUTL_WeaponUse_Generic_Primary.asset", OUTL_WeaponRole.Rifle, OUTL_EquipmentSlot.Primary, ctx.HitscanAttack, 20f, 3f, 55f);
        ctx.SecondaryUseProfile = EnsureWeaponUseProfile("OUTL_WeaponUse_Generic_Projectile.asset", OUTL_WeaponRole.Heavy, OUTL_EquipmentSlot.Secondary, ctx.ProjectileAttack, 18f, 4f, 42f);
        ctx.MeleeUseProfile = EnsureWeaponUseProfile("OUTL_WeaponUse_Generic_Melee.asset", OUTL_WeaponRole.Melee, OUTL_EquipmentSlot.Melee, ctx.MeleeAttack, 1.2f, 0f, 2.2f);
        ctx.LeapAbility = EnsureLeapAbilityProfile("OUTL_Ability_Generic_Leap.asset", "ability.generic.leap");

        ctx.PerceptionProfile = EnsurePerceptionProfile();
        ctx.StateTable = EnsureStateTable(ctx);
        ctx.RangedAIProfile = EnsureAIProfile("OUTL_AI_Generic_Ranged.asset", "ai.generic.ranged", true);
        ctx.MeleeAIProfile = EnsureAIProfile("OUTL_AI_Generic_Melee.asset", "ai.generic.melee", false);
        ctx.CreatureAIProfile = EnsureAIProfile("OUTL_AI_Generic_Creature.asset", "ai.generic.creature", false);
        ctx.RangedTacticalProfile = EnsureTacticalProfile("OUTL_Tactical_Generic_Ranged.asset", ctx.AimProfile, null, null, 22f, 4f, true);
        ctx.MeleeTacticalProfile = EnsureTacticalProfile("OUTL_Tactical_Generic_Melee.asset", ctx.AimProfile, null, null, 1.6f, 0f, false);
        ctx.CreatureTacticalProfile = EnsureTacticalProfile("OUTL_Tactical_Generic_Creature.asset", ctx.AimProfile, ctx.LeapAbility, ctx.LeapAbility, 2.4f, 0f, false);
        ctx.NavigationProfile = EnsureNavigationProfile("OUTL_Nav_Generic_Grounded.asset", "nav.generic.grounded");
        ctx.RangedBehavior = EnsureBehaviorModel("OUTL_NPCBehavior_Generic_Ranged.asset", "npc_behavior.generic.ranged", "combat_ranged", ctx.NavigationProfile, false);
        ctx.MeleeBehavior = EnsureBehaviorModel("OUTL_NPCBehavior_Generic_Melee.asset", "npc_behavior.generic.melee", "combat_melee", ctx.NavigationProfile, false);
        ctx.CreatureBehavior = EnsureBehaviorModel("OUTL_NPCBehavior_Generic_Creature.asset", "npc_behavior.generic.creature", "creature", ctx.NavigationProfile, true);

        ctx.GenericPickupPrefab = CreatePickupPrefab(ctx);
        ctx.GenericLootTable = EnsureLootTable(ctx);

        CreateDamageableActorPrefab(ctx);
        CreateControlledActorPrefab(ctx);
        CreateArmedRangedActorPrefab(ctx);
        CreateArmedMeleeActorPrefab(ctx);
        CreateRangedNpcPrefab(ctx);
        CreateMeleeNpcPrefab(ctx);
        CreateCreaturePrefab(ctx);
        CreateProjectileTurretPrefab(ctx);
        CreateDestructibleObjectPrefab(ctx);
        CreateInteractableObjectPrefab(ctx);

        EditorUtility.SetDirty(ctx.ProjectileAttack);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("OUTL abstract foundation prefabs generated in " + RootFolder + ". Duplicate these templates into game content and assign game-specific art/audio/balance there.");
    }

    public static void ValidateFoundationPrefabs()
    {
        string[] required =
        {
            PrefabFolder + "/OUTL_Abstract_Actor_Damageable.prefab",
            PrefabFolder + "/OUTL_Abstract_Actor_Controlled.prefab",
            PrefabFolder + "/OUTL_Abstract_Actor_ArmedRanged.prefab",
            PrefabFolder + "/OUTL_Abstract_Actor_ArmedMelee.prefab",
            PrefabFolder + "/OUTL_Abstract_NPC_Ranged.prefab",
            PrefabFolder + "/OUTL_Abstract_NPC_Melee.prefab",
            PrefabFolder + "/OUTL_Abstract_Creature.prefab",
            PrefabFolder + "/OUTL_Abstract_Turret_Projectile.prefab",
            PrefabFolder + "/OUTL_Abstract_Object_Destructible.prefab",
            PrefabFolder + "/OUTL_Abstract_Object_Interactable.prefab",
            PrefabFolder + "/OUTL_Abstract_ItemPickup.prefab",
            PrefabFolder + "/OUTL_Abstract_Projectile.prefab"
        };

        int missing = 0;
        for (int i = 0; i < required.Length; i++)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(required[i]) == null)
            {
                missing++;
                Debug.LogWarning("OUTL foundation prefab missing: " + required[i]);
            }
        }

        OUTL_AttackProfile projectile = AssetDatabase.LoadAssetAtPath<OUTL_AttackProfile>(CombatFolder + "/OUTL_Attack_Generic_Projectile.asset");
        if (projectile == null || projectile.ProjectilePrefab == null || projectile.ProjectilePrefab.GetComponent<OUTL_Projectile>() == null)
        {
            missing++;
            Debug.LogWarning("OUTL foundation projectile attack must reference a prefab with OUTL_Projectile.");
        }

        GameObject controlled = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/OUTL_Abstract_Actor_Controlled.prefab");
        if (controlled == null || controlled.GetComponent<OUTL_ActorControlBridge>() == null || controlled.GetComponent<OUTL_CharacterControllerInputSink>() == null || controlled.GetComponent<OUTL_BasicPlayerController>() != null)
        {
            missing++;
            Debug.LogWarning("OUTL controlled foundation actor must use ActorControlBridge + CharacterControllerInputSink and must not use OUTL_BasicPlayerController.");
        }

        if (missing == 0) Debug.Log("OUTL abstract foundation prefabs validated successfully.");
    }

    private static void CreateDamageableActorPrefab(FoundationContext ctx)
    {
        OUTL_EntityDef def = EnsureEntityDef("OUTL_Def_Actor_Damageable.asset", "actor.damageable", "Damageable Actor", new[] { "Actor", "Damageable", "Role.Targetable" }, 100f, 0f, 2.5f);
        GameObject root = CreateCapsuleRoot("OUTL_Abstract_Actor_Damageable");
        OUTL_EntityAdapter entity = ConfigureDamageable(root, def, ctx.NeutralFaction, 100f, true, true, OUTL_HitboxZone.Torso);
        AddHeadHitbox(root, entity);
        SavePrefabAndLink(def, root, PrefabFolder + "/OUTL_Abstract_Actor_Damageable.prefab");
    }

    private static void CreateControlledActorPrefab(FoundationContext ctx)
    {
        OUTL_EntityDef def = EnsureEntityDef("OUTL_Def_Actor_Controlled.asset", "actor.controlled", "Controlled Actor", new[] { "Actor", "Controlled", "Role.Targetable" }, 100f, 18f, 4.5f);
        GameObject root = new GameObject("OUTL_Abstract_Actor_Controlled");
        OUTL_EntityAdapter entity = ConfigureDamageable(root, def, ctx.ControlledFaction, 100f, false, false, OUTL_HitboxZone.Torso);
        CharacterController cc = root.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.32f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        AddVisualCapsule(root.transform, "Visual_Capsule", new Vector3(0f, 0.9f, 0f), new Vector3(0.7f, 0.9f, 0.7f), false);
        AddHeadHitbox(root, entity);

        GameObject cameraObject = new GameObject("ViewCamera");
        cameraObject.transform.SetParent(root.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 1.62f, 0f);
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        Transform muzzle = CreateMuzzle(cameraObject.transform, new Vector3(0.16f, -0.12f, 0.45f));

        OUTL_AttackDriver attack = ConfigureAttackDriver(root, entity, muzzle, ctx.HitscanAttack, ctx.ProjectileAttack, ctx.MeleeAttack);
        OUTL_InventoryRuntime inventory = root.AddComponent<OUTL_InventoryRuntime>();
        inventory.Entity = entity;
        inventory.KnownItems = new OUTL_ItemDef[] { ctx.PrimaryWeapon, ctx.SecondaryWeapon, ctx.MeleeWeapon, ctx.GenericPickupItem };
        OUTL_EquipmentRuntime equipment = ConfigureEquipment(root, entity, ctx, true);

        OUTL_PlayerInputSource input = root.AddComponent<OUTL_PlayerInputSource>();
        input.ViewCamera = camera;
        input.MouseSensitivity = 2f;
        input.LockCursorOnStart = true;
        input.EscapeUnlocksCursor = true;
        input.ClickRelocksCursor = true;

        OUTL_CharacterControllerInputSink movement = root.AddComponent<OUTL_CharacterControllerInputSink>();
        movement.Entity = entity;
        movement.Controller = cc;
        movement.YawRoot = root.transform;
        movement.ViewPitchRoot = cameraObject.transform;
        movement.UseGoldSrcUnits = true;
        movement.ForwardSpeed = 320f;
        movement.SideSpeed = 320f;
        movement.BackSpeed = 320f;
        movement.WalkSpeed = 150f;
        movement.RunSpeed = 320f;
        movement.HoldShiftToWalk = true;
        movement.GroundAcceleration = 10f;
        movement.AirAcceleration = 10f;
        movement.AirWishSpeedCap = 30f;
        movement.Friction = 4f;
        movement.StopSpeed = 100f;
        movement.JumpSpeed = 270f;
        movement.Gravity = 981f;
        movement.GravityMultiplier = 1.65f;
        movement.FallingGravityMultiplier = 1.75f;
        movement.LowJumpGravityMultiplier = 2.25f;
        movement.StandingHeight = 1.8f;
        movement.CrouchHeight = 1.0f;
        movement.StandingViewHeight = 1.62f;
        movement.CrouchViewHeight = 0.92f;
        movement.CrouchNoiseMultiplier = 0.35f;
        movement.CrouchStimulusRadiusMultiplier = 0.45f;

        OUTL_AttackDriverInputSink attackSink = root.AddComponent<OUTL_AttackDriverInputSink>();
        attackSink.Entity = entity;
        attackSink.AttackDriver = attack;

        OUTL_InteractionInputSink interactionSink = root.AddComponent<OUTL_InteractionInputSink>();
        interactionSink.Entity = entity;
        interactionSink.ViewCamera = camera;
        interactionSink.ViewRoot = cameraObject.transform;
        interactionSink.UseDistance = 3f;

        OUTL_ActorControlBridge bridge = root.AddComponent<OUTL_ActorControlBridge>();
        bridge.Entity = entity;
        bridge.InputSourceBehaviour = input;
        bridge.InputSinkBehaviours = new Behaviour[] { movement, attackSink, interactionSink };
        bridge.TickLane = OUTL_TickLane.Full;
        bridge.TickInterval = 0.02f;
        bridge.LocalPlayerUpdateMode = OUTL_ActorInputUpdateMode.FullAndNearActors;
        bridge.ApplyLocalPlayerEveryFrame = true;
        bridge.ApplyNearActorsEveryFrame = true;
        bridge.UseUnityUpdateForLocalInput = true;

        SavePrefabAndLink(def, root, PrefabFolder + "/OUTL_Abstract_Actor_Controlled.prefab");
    }

    private static void CreateArmedRangedActorPrefab(FoundationContext ctx)
    {
        OUTL_EntityDef def = EnsureEntityDef("OUTL_Def_Actor_ArmedRanged.asset", "actor.armed_ranged", "Armed Ranged Actor", new[] { "Actor", "Armed", "Ranged", "Role.Targetable" }, 90f, 18f, 3.2f);
        GameObject root = CreateCapsuleRoot("OUTL_Abstract_Actor_ArmedRanged");
        OUTL_EntityAdapter entity = ConfigureDamageable(root, def, ctx.NeutralFaction, 90f, true, true, OUTL_HitboxZone.Torso);
        Transform muzzle = CreateMuzzle(root.transform, new Vector3(0f, 1.25f, 0.5f));
        ConfigureAttackDriver(root, entity, muzzle, ctx.HitscanAttack, ctx.ProjectileAttack, ctx.MeleeAttack);
        ConfigureEquipment(root, entity, ctx, true);
        AddHeadHitbox(root, entity);
        SavePrefabAndLink(def, root, PrefabFolder + "/OUTL_Abstract_Actor_ArmedRanged.prefab");
    }

    private static void CreateArmedMeleeActorPrefab(FoundationContext ctx)
    {
        OUTL_EntityDef def = EnsureEntityDef("OUTL_Def_Actor_ArmedMelee.asset", "actor.armed_melee", "Armed Melee Actor", new[] { "Actor", "Armed", "Melee", "Role.Targetable" }, 110f, 12f, 3.4f);
        GameObject root = CreateCapsuleRoot("OUTL_Abstract_Actor_ArmedMelee");
        OUTL_EntityAdapter entity = ConfigureDamageable(root, def, ctx.NeutralFaction, 110f, true, true, OUTL_HitboxZone.Torso);
        Transform muzzle = CreateMuzzle(root.transform, new Vector3(0f, 1.25f, 0.45f));
        ConfigureAttackDriver(root, entity, muzzle, null, null, ctx.MeleeAttack);
        ConfigureEquipment(root, entity, ctx, false);
        AddHeadHitbox(root, entity);
        SavePrefabAndLink(def, root, PrefabFolder + "/OUTL_Abstract_Actor_ArmedMelee.prefab");
    }

    private static void CreateRangedNpcPrefab(FoundationContext ctx)
    {
        OUTL_EntityDef def = EnsureEntityDef("OUTL_Def_NPC_Ranged.asset", "actor.npc.ranged", "Ranged NPC Actor", new[] { "Actor", "NPC", "Ranged", "Role.Targetable" }, 80f, 16f, 3.2f);
        GameObject root = CreateCapsuleRoot("OUTL_Abstract_NPC_Ranged");
        OUTL_EntityAdapter entity = ConfigureDamageable(root, def, ctx.GroupAFaction, 80f, true, true, OUTL_HitboxZone.Torso);
        Transform muzzle = CreateMuzzle(root.transform, new Vector3(0f, 1.35f, 0.5f));
        OUTL_AttackDriver attack = ConfigureAttackDriver(root, entity, muzzle, ctx.ProjectileAttack, ctx.HitscanAttack, ctx.MeleeAttack);
        ConfigureEquipment(root, entity, ctx, true);
        AddHeadHitbox(root, entity);
        AddAIStack(root, entity, attack, ctx.RangedAIProfile, ctx.PerceptionProfile, ctx.StateTable, ctx.RangedBehavior, ctx.NavigationProfile, true, false);
        SavePrefabAndLink(def, root, PrefabFolder + "/OUTL_Abstract_NPC_Ranged.prefab");
    }

    private static void CreateMeleeNpcPrefab(FoundationContext ctx)
    {
        OUTL_EntityDef def = EnsureEntityDef("OUTL_Def_NPC_Melee.asset", "actor.npc.melee", "Melee NPC Actor", new[] { "Actor", "NPC", "Melee", "Role.Targetable" }, 105f, 14f, 3.6f);
        GameObject root = CreateCapsuleRoot("OUTL_Abstract_NPC_Melee");
        OUTL_EntityAdapter entity = ConfigureDamageable(root, def, ctx.GroupAFaction, 105f, true, true, OUTL_HitboxZone.Torso);
        Transform muzzle = CreateMuzzle(root.transform, new Vector3(0f, 1.25f, 0.45f));
        OUTL_AttackDriver attack = ConfigureAttackDriver(root, entity, muzzle, null, null, ctx.MeleeAttack);
        ConfigureEquipment(root, entity, ctx, false);
        AddHeadHitbox(root, entity);
        AddAIStack(root, entity, attack, ctx.MeleeAIProfile, ctx.PerceptionProfile, ctx.StateTable, ctx.MeleeBehavior, ctx.NavigationProfile, false, false);
        SavePrefabAndLink(def, root, PrefabFolder + "/OUTL_Abstract_NPC_Melee.prefab");
    }

    private static void CreateCreaturePrefab(FoundationContext ctx)
    {
        OUTL_EntityDef def = EnsureEntityDef("OUTL_Def_Creature.asset", "actor.creature", "Creature Actor", new[] { "Actor", "Creature", "Melee", "Role.Targetable" }, 70f, 10f, 3.8f);
        GameObject root = CreateCapsuleRoot("OUTL_Abstract_Creature");
        OUTL_EntityAdapter entity = ConfigureDamageable(root, def, ctx.NeutralFaction, 70f, true, true, OUTL_HitboxZone.Torso);
        root.transform.localScale = new Vector3(0.85f, 0.85f, 1.15f);
        Transform muzzle = CreateMuzzle(root.transform, new Vector3(0f, 0.9f, 0.55f));
        OUTL_AttackDriver attack = ConfigureAttackDriver(root, entity, muzzle, null, null, ctx.MeleeAttack);
        AddWeakPointHitbox(root, entity);
        AddAIStack(root, entity, attack, ctx.CreatureAIProfile, ctx.PerceptionProfile, ctx.StateTable, ctx.CreatureBehavior, ctx.NavigationProfile, false, true);
        SavePrefabAndLink(def, root, PrefabFolder + "/OUTL_Abstract_Creature.prefab");
    }

    private static void CreateProjectileTurretPrefab(FoundationContext ctx)
    {
        OUTL_EntityDef def = EnsureEntityDef("OUTL_Def_Turret_Projectile.asset", "emitter.turret.projectile", "Projectile Turret", new[] { "Entity", "CombatEmitter", "Turret", "Role.Targetable" }, 120f, 16f, 0f);
        GameObject root = new GameObject("OUTL_Abstract_Turret_Projectile");
        OUTL_EntityAdapter entity = ConfigureDamageable(root, def, ctx.GroupAFaction, 120f, true, true, OUTL_HitboxZone.Armor);

        BoxCollider body = root.AddComponent<BoxCollider>();
        body.center = new Vector3(0f, 0.45f, 0f);
        body.size = new Vector3(1.1f, 0.9f, 1.1f);
        AddVisualPrimitive(root.transform, PrimitiveType.Cylinder, "Base", new Vector3(0f, 0.25f, 0f), new Vector3(0.8f, 0.25f, 0.8f), false);
        GameObject pivot = AddVisualPrimitive(root.transform, PrimitiveType.Cube, "AimPivot", new Vector3(0f, 0.85f, 0f), new Vector3(0.45f, 0.35f, 0.7f), false);
        Transform muzzle = CreateMuzzle(pivot.transform, new Vector3(0f, 0f, 0.55f));

        SphereCollider sensor = root.AddComponent<SphereCollider>();
        sensor.isTrigger = true;
        sensor.radius = 12f;

        OUTL_ProjectileCombatEmitter turret = root.AddComponent<OUTL_ProjectileCombatEmitter>();
        turret.Source = entity;
        turret.ProjectileAttack = ctx.ProjectileAttack;
        turret.ProjectilePrefab = ctx.GenericProjectilePrefab;
        turret.Muzzle = muzzle;
        turret.AimPivot = pivot.transform;
        turret.TargetTags = new[] { "Role.Targetable" };
        turret.FireInterval = 0.45f;
        turret.ProjectileSpeedOverride = -1f;
        turret.AimLeadStrength = 0.65f;
        turret.RequireLineOfSight = true;
        turret.AutoRegister = true;

        SavePrefabAndLink(def, root, PrefabFolder + "/OUTL_Abstract_Turret_Projectile.prefab");
    }

    private static void CreateDestructibleObjectPrefab(FoundationContext ctx)
    {
        OUTL_EntityDef def = EnsureEntityDef("OUTL_Def_Object_Destructible.asset", "object.destructible", "Destructible Object", new[] { "Object", "Damageable", "Destructible", "Role.Targetable" }, 60f, 0f, 0f);
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "OUTL_Abstract_Object_Destructible";
        root.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
        OUTL_EntityAdapter entity = ConfigureDamageable(root, def, ctx.NeutralFaction, 60f, true, false, OUTL_HitboxZone.Generic);
        OUTL_LootDropper loot = root.AddComponent<OUTL_LootDropper>();
        loot.Entity = entity;
        loot.LootTable = ctx.GenericLootTable;
        loot.InventoryPickupPrefab = ctx.GenericPickupPrefab;
        loot.DropInventoryItems = false;
        loot.DropOnKilled = true;
        SavePrefabAndLink(def, root, PrefabFolder + "/OUTL_Abstract_Object_Destructible.prefab");
    }

    private static void CreateInteractableObjectPrefab(FoundationContext ctx)
    {
        OUTL_EntityDef def = EnsureEntityDef("OUTL_Def_Object_Interactable.asset", "object.interactable", "Interactable Object", new[] { "Object", "Interactable" }, 30f, 0f, 0f);
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "OUTL_Abstract_Object_Interactable";
        root.transform.localScale = new Vector3(1f, 1f, 1f);
        OUTL_EntityAdapter entity = ConfigureEntity(root, def, ctx.NeutralFaction, false, false);
        OUTL_Interactable interactable = root.AddComponent<OUTL_Interactable>();
        interactable.Entity = entity;
        interactable.DisplayNameKey = "interact.object.name";
        interactable.DescriptionKey = "interact.object.description";
        interactable.DisplayName = "Use";
        interactable.DescriptionEn = "Use";
        interactable.Command = OUTL_CommandType.Use;
        interactable.SendToSelf = true;
        interactable.AllowLegacyDirectTargets = false;
        interactable.InvokeLegacyUnityEvents = false;
        SavePrefabAndLink(def, root, PrefabFolder + "/OUTL_Abstract_Object_Interactable.prefab");
    }

    private static GameObject CreatePickupPrefab(FoundationContext ctx)
    {
        OUTL_EntityDef def = EnsureEntityDef("OUTL_Def_ItemPickup.asset", "pickup.item", "Item Pickup", new[] { "Pickup", "Item", "Resource" }, 1f, 0f, 0f);
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "OUTL_Abstract_ItemPickup";
        root.transform.localScale = Vector3.one * 0.35f;
        SphereCollider collider = root.GetComponent<SphereCollider>();
        if (collider != null) collider.isTrigger = true;
        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        OUTL_EntityAdapter entity = ConfigureEntity(root, def, ctx.NeutralFaction, false, true);
        OUTL_ItemPickup pickup = root.AddComponent<OUTL_ItemPickup>();
        pickup.Entity = entity;
        pickup.Item = ctx.GenericPickupItem;
        pickup.Count = 1;
        pickup.UseCommand = true;
        pickup.AutoDespawnOnPickup = true;
        OUTL_Interactable interactable = root.AddComponent<OUTL_Interactable>();
        interactable.Entity = entity;
        interactable.DisplayName = "Pickup";
        interactable.DescriptionEn = "Pickup item";
        interactable.Command = OUTL_CommandType.Pickup;
        interactable.SendToSelf = true;
        return SavePrefabAndLink(def, root, PrefabFolder + "/OUTL_Abstract_ItemPickup.prefab");
    }

    private static GameObject CreateProjectilePrefab(FoundationContext ctx)
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "OUTL_Abstract_Projectile";
        root.transform.localScale = Vector3.one * 0.18f;
        Collider collider = root.GetComponent<Collider>();
        if (collider != null) collider.isTrigger = true;
        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        OUTL_Projectile projectile = root.AddComponent<OUTL_Projectile>();
        projectile.DestroyOnHit = true;
        projectile.AutoRegisterOnLaunch = true;
        return SavePrefab(root, PrefabFolder + "/OUTL_Abstract_Projectile.prefab");
    }

    private static OUTL_EntityAdapter ConfigureDamageable(GameObject root, OUTL_EntityDef def, OUTL_FactionDef faction, float health, bool queueDespawn, bool disableAI, OUTL_HitboxZone zone)
    {
        OUTL_EntityAdapter entity = ConfigureEntity(root, def, faction, true, true);
        OUTL_DamageReceiver receiver = root.AddComponent<OUTL_DamageReceiver>();
        receiver.Entity = entity;
        OUTL_Vitals vitals = root.AddComponent<OUTL_Vitals>();
        vitals.Entity = entity;
        vitals.InitializeMissingStats = true;
        vitals.DefaultHealth = Mathf.Max(1f, health);
        vitals.DefaultMaxHealth = Mathf.Max(1f, health);
        OUTL_DeathHandler death = root.AddComponent<OUTL_DeathHandler>();
        death.Entity = entity;
        death.DisableAI = disableAI;
        death.DisableColliders = false;
        death.DisableRenderers = false;
        death.QueueDespawn = queueDespawn;
        death.DespawnDelay = queueDespawn ? 4f : 0f;
        OUTL_Hitbox hitbox = root.AddComponent<OUTL_Hitbox>();
        hitbox.Entity = entity;
        hitbox.Zone = zone;
        hitbox.DamageMultiplier = 1f;
        OUTL_DeathRuntime deathRuntime = root.AddComponent<OUTL_DeathRuntime>();
        deathRuntime.Entity = entity;
        return entity;
    }

    private static OUTL_EntityAdapter ConfigureEntity(GameObject root, OUTL_EntityDef def, OUTL_FactionDef faction, bool tick, bool sectors)
    {
        OUTL_EntityAdapter entity = root.AddComponent<OUTL_EntityAdapter>();
        entity.Def = def;
        entity.Faction = faction;
        entity.ClassNameOverride = def != null ? def.ClassName : "entity.generic";
        entity.TargetName = "";
        entity.Target = "";
        entity.KillTarget = "";
        entity.StableId = "";
        entity.SavePersistent = false;
        entity.RestoreSpawnIfMissing = false;
        entity.Tier = OUTL_RuntimeTier.Full;
        entity.RegisterOnEnable = true;
        entity.RegisterTick = tick;
        entity.RegisterInSectors = sectors;
        entity.IncludeChildCommandReceivers = true;
        entity.TickLane = OUTL_TickLane.Logic;
        entity.TickInterval = 0.25f;
        entity.RebuildCommandReceiverCache();
        return entity;
    }

    private static OUTL_AttackDriver ConfigureAttackDriver(GameObject root, OUTL_EntityAdapter entity, Transform muzzle, OUTL_AttackProfile primary, OUTL_AttackProfile secondary, OUTL_AttackProfile melee)
    {
        OUTL_AttackDriver attack = root.AddComponent<OUTL_AttackDriver>();
        attack.Source = entity;
        attack.Muzzle = muzzle != null ? muzzle : root.transform;
        attack.Primary = primary;
        attack.Secondary = secondary;
        attack.Melee = melee;
        attack.SmartMeleeWhenFireAtPrimary = true;
        attack.RespectCooldownOnFireAt = true;
        attack.BlockWhenSourceDead = true;
        attack.MeleeHeight = 1.45f;
        attack.MeleeForwardBias = 0.6f;
        return attack;
    }

    private static OUTL_EquipmentRuntime ConfigureEquipment(GameObject root, OUTL_EntityAdapter entity, FoundationContext ctx, bool includeRanged)
    {
        OUTL_EquipmentRuntime equipment = root.AddComponent<OUTL_EquipmentRuntime>();
        equipment.Entity = entity;
        equipment.RequireInventoryForEquip = false;
        equipment.ReturnUnequippedToInventory = false;
        equipment.AutoEquipKnownItemsOnStart = true;
        equipment.AutoEquipOnlyEmptySlots = true;
        equipment.KnownItems = includeRanged ? new[] { ctx.PrimaryWeapon, ctx.SecondaryWeapon, ctx.MeleeWeapon } : new[] { ctx.MeleeWeapon };
        return equipment;
    }

    private static void AddAIStack(GameObject root, OUTL_EntityAdapter entity, OUTL_AttackDriver attack, OUTL_AIProfile aiProfile, OUTL_AIPerceptionProfile perception, OUTL_AIStateTable stateTable, OUTL_NPCBehaviorModel behavior, OUTL_NPCNavigationProfile navProfile, bool ranged, bool creature)
    {
        CapsuleCollider capsule = root.GetComponent<CapsuleCollider>();
        if (capsule != null) UnityEngine.Object.DestroyImmediate(capsule);
        CharacterController motor = root.GetComponent<CharacterController>();
        if (motor == null) motor = root.AddComponent<CharacterController>();
        motor.height = navProfile != null ? navProfile.NavAgentHeight : 1.8f;
        motor.radius = navProfile != null ? navProfile.NavAgentRadius : 0.35f;
        motor.center = new Vector3(0f, motor.height * 0.5f, 0f);
        motor.stepOffset = 0.35f;
        motor.slopeLimit = 45f;

        OUTL_FallDamageSensor fallDamage = root.GetComponent<OUTL_FallDamageSensor>();
        if (fallDamage == null) fallDamage = root.AddComponent<OUTL_FallDamageSensor>();
        fallDamage.Entity = entity;
        fallDamage.CharacterController = motor;
        fallDamage.TickLane = OUTL_TickLane.Full;
        fallDamage.TickInterval = 0.02f;

        NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
        agent.speed = navProfile != null ? navProfile.WalkSpeed : 3f;
        agent.height = navProfile != null ? navProfile.NavAgentHeight : 1.8f;
        agent.radius = navProfile != null ? navProfile.NavAgentRadius : 0.35f;
        agent.baseOffset = 0f;

        OUTL_NavMeshMover mover = root.AddComponent<OUTL_NavMeshMover>();
        mover.Entity = entity;
        mover.Agent = agent;
        mover.CharacterController = motor;
        mover.UseOUTLTick = true;
        mover.AllowLegacyUpdateTick = false;
        mover.UseUnityUpdateForFullNear = true;
        mover.UseCharacterControllerFallback = true;
        mover.StableGroundFallback = true;
        mover.DisableGravityWhenNoGround = true;
        mover.FallbackSpeed = agent.speed;
        mover.StopDistance = ranged ? 4f : 1.4f;

        OUTL_AIActor ai = root.AddComponent<OUTL_AIActor>();
        ai.Profile = aiProfile;
        ai.PerceptionProfile = perception;
        ai.StateTable = stateTable;
        ai.Entity = entity;
        ai.MoveRoot = root.transform;
        ai.NavMover = mover;
        ai.AttackDriver = attack;
        ai.RequireLineOfSightToAcquireTarget = true;
        ai.RequireLineOfSightToKeepTarget = true;
        ai.UseStimulusInterrupts = true;
        ai.CreatureUsesFoodStimulus = creature;
        ai.FleeFromDanger = true;
        ai.PreferRangedCombat = ranged;
        ai.FleeWhenTargetTooClose = ranged;
        ai.PreferredRange = ranged ? 22f : 1.8f;
        ai.MinSafeRange = ranged ? 3f : 0.8f;
        ai.SwitchCooldown = 0.5f;
        ai.ExposeDebugState = true;
        ai.UseActorInputContract = true;

        OUTL_HearingSensor hearing = root.AddComponent<OUTL_HearingSensor>();
        hearing.Actor = ai;
        hearing.HearingMultiplier = 1f;
        hearing.MinPriority = 0.05f;

        OUTL_EntityDiary diary = root.AddComponent<OUTL_EntityDiary>();
        diary.Entity = entity;
        diary.WriteToFile = false;
        ai.Diary = diary;

        OUTL_NPCBehaviorController npc = root.AddComponent<OUTL_NPCBehaviorController>();
        npc.Entity = entity;
        npc.AIActor = ai;
        npc.NavMover = mover;
        npc.AttackDriver = attack;
        npc.Model = behavior;
        npc.NavigationProfileOverride = navProfile;
        npc.UseSharedRouteCache = true;
        npc.UseLocalRouteCache = false;

        OUTL_AIArsenalSelector arsenal = root.AddComponent<OUTL_AIArsenalSelector>();
        arsenal.Entity = entity;
        arsenal.AttackDriver = attack;
        arsenal.Primary = ranged ? EnsureWeaponUseProfile("OUTL_WeaponUse_Generic_Primary.asset", OUTL_WeaponRole.Rifle, OUTL_EquipmentSlot.Primary, attack != null ? attack.Primary : null, 20f, 3f, 55f) : null;
        arsenal.Secondary = ranged ? EnsureWeaponUseProfile("OUTL_WeaponUse_Generic_Projectile.asset", OUTL_WeaponRole.Heavy, OUTL_EquipmentSlot.Secondary, attack != null ? attack.Secondary : null, 18f, 4f, 42f) : null;
        arsenal.Melee = EnsureWeaponUseProfile("OUTL_WeaponUse_Generic_Melee.asset", OUTL_WeaponRole.Melee, OUTL_EquipmentSlot.Melee, attack != null ? attack.Melee : null, 1.2f, 0f, 2.2f);

        OUTL_AimPlanner aimPlanner = root.AddComponent<OUTL_AimPlanner>();
        aimPlanner.Entity = entity;
        aimPlanner.AttackDriver = attack;
        aimPlanner.Profile = EnsureAimProfile("OUTL_Aim_Generic_Combat.asset", "aim.generic.combat");

        OUTL_AbilityInputSink abilitySink = null;
        OUTL_LeapAbilityProfile leap = null;
        if (creature)
        {
            leap = EnsureLeapAbilityProfile("OUTL_Ability_Generic_Leap.asset", "ability.generic.leap");
            abilitySink = root.AddComponent<OUTL_AbilityInputSink>();
            abilitySink.Entity = entity;
            abilitySink.CharacterController = motor;
            abilitySink.NavMover = mover;
            abilitySink.PrimaryAbility = leap;
            abilitySink.AllowTransformFallback = true;
            abilitySink.UseUnityUpdateForFullNear = true;
        }

        OUTL_TacticalPlanner tactical = root.AddComponent<OUTL_TacticalPlanner>();
        tactical.Entity = entity;
        tactical.AIActor = ai;
        tactical.Arsenal = arsenal;
        tactical.AimPlanner = aimPlanner;
        tactical.AbilitySink = abilitySink;
        tactical.Profile = EnsureTacticalProfile(creature ? "OUTL_Tactical_Generic_Creature.asset" : (ranged ? "OUTL_Tactical_Generic_Ranged.asset" : "OUTL_Tactical_Generic_Melee.asset"), aimPlanner.Profile, creature ? leap : null, creature ? leap : null, ranged ? 22f : 1.8f, ranged ? 3f : 0f, ranged);
        tactical.AutoRegister = false;
        ai.TacticalPlanner = tactical;

        OUTL_BotInputDriver bot = root.AddComponent<OUTL_BotInputDriver>();
        bot.Entity = entity;
        bot.AIActor = ai;
        bot.TacticalPlanner = tactical;
        bot.AimPlanner = aimPlanner;
        bot.Arsenal = arsenal;
        bot.MoveRoot = root.transform;
        bot.StopDistance = ranged ? 4f : 1.2f;

        OUTL_NavMoverInputSink navSink = root.AddComponent<OUTL_NavMoverInputSink>();
        navSink.Entity = entity;
        navSink.NavMover = mover;
        navSink.CharacterController = motor;
        navSink.MoveRoot = root.transform;
        navSink.MovementAuthority = "actor_input";
        navSink.StopOnlyOwnedDestination = true;
        navSink.StopOwnedDestinationOnZeroInput = true;
        navSink.ZeroInputStopDelay = 0.06f;
        navSink.MinDestinationChangeDistance = 0.35f;
        navSink.MinDestinationRefreshInterval = 0.12f;

        OUTL_AimInputSink aimSink = root.AddComponent<OUTL_AimInputSink>();
        aimSink.Entity = entity;
        aimSink.AimRoot = root.transform;

        OUTL_AttackDriverInputSink attackSink = root.AddComponent<OUTL_AttackDriverInputSink>();
        attackSink.Entity = entity;
        attackSink.AttackDriver = attack;

        OUTL_ActorControlBridge bridge = root.AddComponent<OUTL_ActorControlBridge>();
        bridge.Entity = entity;
        bridge.InputSourceBehaviour = bot;
        bridge.InputSinkBehaviours = creature && abilitySink != null
            ? new Behaviour[] { navSink, aimSink, attackSink, abilitySink }
            : new Behaviour[] { navSink, aimSink, attackSink };
        bridge.TickLane = OUTL_TickLane.AI;
        bridge.TickInterval = 0.05f;
        bridge.LocalPlayerUpdateMode = OUTL_ActorInputUpdateMode.FullAndNearActors;
        bridge.ApplyLocalPlayerEveryFrame = false;
        bridge.ApplyNearActorsEveryFrame = true;
        bridge.UseUnityUpdateForLocalInput = true;
    }

    private static OUTL_EntityDef EnsureEntityDef(string fileName, string className, string displayName, string[] tags, float health, float damage, float speed)
    {
        string path = DefFolder + "/" + fileName;
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
            new OUTL_StatEntry { Key = "MaxHealth", Value = Mathf.Max(1f, health) },
            new OUTL_StatEntry { Key = "Damage", Value = damage },
            new OUTL_StatEntry { Key = "Speed", Value = speed },
            new OUTL_StatEntry { Key = "Armor", Value = 0f }
        };
        EditorUtility.SetDirty(def);
        return def;
    }

    private static OUTL_ItemDef EnsureItem(string fileName, string className, string displayName, int maxStack)
    {
        string path = ItemFolder + "/" + fileName;
        OUTL_ItemDef item = AssetDatabase.LoadAssetAtPath<OUTL_ItemDef>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<OUTL_ItemDef>();
            AssetDatabase.CreateAsset(item, path);
        }

        item.ClassName = className;
        item.DisplayName = displayName;
        item.Tags = new[] { "Item", "Resource" };
        item.MaxStack = Mathf.Max(1, maxStack);
        item.Equippable = false;
        item.BaseStats = new[] { new OUTL_StatEntry { Key = "Value", Value = 1f } };
        EditorUtility.SetDirty(item);
        return item;
    }

    private static OUTL_EquipmentItemDef EnsureEquipmentItem(string fileName, string className, string displayName, OUTL_EquipmentSlot slot, OUTL_AttackProfile profile)
    {
        string path = ItemFolder + "/" + fileName;
        OUTL_EquipmentItemDef item = AssetDatabase.LoadAssetAtPath<OUTL_EquipmentItemDef>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<OUTL_EquipmentItemDef>();
            AssetDatabase.CreateAsset(item, path);
        }

        item.ClassName = className;
        item.DisplayName = displayName;
        item.Tags = new[] { "Item", "Weapon", slot.ToString() };
        item.MaxStack = 1;
        item.Equippable = true;
        item.Slot = slot;
        item.AttackProfile = profile;
        EditorUtility.SetDirty(item);
        return item;
    }

    private static OUTL_AimProfile EnsureAimProfile(string fileName, string profileId)
    {
        string path = AIFolder + "/" + fileName;
        OUTL_AimProfile profile = AssetDatabase.LoadAssetAtPath<OUTL_AimProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<OUTL_AimProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        profile.AimAngularSpeed = 260f;
        profile.AimErrorNear = 1.5f;
        profile.AimErrorFar = 5f;
        profile.AimSettleTimeMin = 0.06f;
        profile.AimSettleTimeMax = 0.22f;
        profile.ReactionDelayMin = 0.10f;
        profile.ReactionDelayMax = 0.35f;
        profile.FireDelayMin = 0.04f;
        profile.FireDelayMax = 0.16f;
        profile.MaxFireAngleError = 7f;
        profile.AimHeight = 1.1f;
        profile.RequireLineOfSight = true;
        profile.UseFriendlyFire = true;
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static OUTL_WeaponUseProfile EnsureWeaponUseProfile(string fileName, OUTL_WeaponRole role, OUTL_EquipmentSlot slot, OUTL_AttackProfile attack, float preferredRange, float minSafeRange, float maxRange)
    {
        string path = AIFolder + "/" + fileName;
        OUTL_WeaponUseProfile profile = AssetDatabase.LoadAssetAtPath<OUTL_WeaponUseProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<OUTL_WeaponUseProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        profile.Role = role;
        profile.Slot = slot;
        profile.AttackProfileOverride = attack;
        profile.PreferredRange = Mathf.Max(0.1f, preferredRange);
        profile.MinSafeRange = Mathf.Max(0f, minSafeRange);
        profile.MaxRange = Mathf.Max(profile.PreferredRange, maxRange);
        profile.RequiresLineOfSight = true;
        profile.BlocksOnFriendlyFire = true;
        profile.AllowSuppression = slot != OUTL_EquipmentSlot.Melee;
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static OUTL_LeapAbilityProfile EnsureLeapAbilityProfile(string fileName, string abilityId)
    {
        string path = AIFolder + "/" + fileName;
        OUTL_LeapAbilityProfile profile = AssetDatabase.LoadAssetAtPath<OUTL_LeapAbilityProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<OUTL_LeapAbilityProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        profile.AbilityId = abilityId;
        profile.AbilitySlot = 0;
        profile.Cooldown = 2.4f;
        profile.MinRange = 2f;
        profile.MaxRange = 10f;
        profile.RequiresGround = false;
        profile.RequiresLineOfSight = true;
        profile.WindupTime = 0.08f;
        profile.RecoveryTime = 0.35f;
        profile.LeapSpeed = 12f;
        profile.LeapArcHeight = 1.8f;
        profile.LeapDuration = 0.6f;
        profile.ImpactRadius = 1.15f;
        profile.ImpactDamage = 16f;
        profile.PreferWhenTargetDistanceMin = 3f;
        profile.PreferWhenTargetDistanceMax = 9f;
        profile.UseCharacterMotor = false;
        profile.UsePhysicsImpulse = false;
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static OUTL_TacticalProfile EnsureTacticalProfile(string fileName, OUTL_AimProfile aim, OUTL_AbilityProfile primaryAbility, OUTL_LeapAbilityProfile leapAbility, float preferredRange, float minSafeRange, bool ranged)
    {
        string path = AIFolder + "/" + fileName;
        OUTL_TacticalProfile profile = AssetDatabase.LoadAssetAtPath<OUTL_TacticalProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<OUTL_TacticalProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        profile.NearThinkInterval = 0.12f;
        profile.MidThinkInterval = 0.35f;
        profile.FarThinkInterval = 1.25f;
        profile.FullTacticalOnlyNear = true;
        profile.PreferredRange = Mathf.Max(0.1f, preferredRange);
        profile.MinSafeRange = Mathf.Max(0f, minSafeRange);
        profile.MeleeFallbackRange = ranged ? 2.2f : 1.8f;
        profile.StopDistance = ranged ? 4f : 1.15f;
        profile.UseCover = ranged;
        profile.CoverSearchRadius = 18f;
        profile.FireOnlyWhenVisible = true;
        profile.HoldFireOnFriendlyRisk = true;
        profile.PrimaryAbility = primaryAbility;
        profile.LeapAbility = leapAbility;
        profile.AimProfile = aim;
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static OUTL_AttackProfile EnsureAttackProfile(string fileName, string attackId, OUTL_AttackMode mode, string damageKey, float damage, float range, float cooldown, float radius)
    {
        string path = CombatFolder + "/" + fileName;
        OUTL_AttackProfile profile = AssetDatabase.LoadAssetAtPath<OUTL_AttackProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<OUTL_AttackProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        profile.AttackId = attackId;
        profile.Mode = mode;
        profile.HitDamageKey = damageKey;
        profile.Damage = damage;
        profile.Range = range;
        profile.Cooldown = cooldown;
        profile.Radius = radius;
        profile.HitMask = ~0;
        profile.ProjectileIgnoreTriggers = true;
        profile.ProjectileDetonateOnEntityHit = true;
        profile.ProjectileDetonateOnWorldHit = true;
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static OUTL_AIProfile EnsureAIProfile(string fileName, string profileId, bool ranged)
    {
        string path = AIFolder + "/" + fileName;
        OUTL_AIProfile profile = AssetDatabase.LoadAssetAtPath<OUTL_AIProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<OUTL_AIProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        profile.ProfileId = profileId;
        profile.UseFactionHostility = true;
        profile.EnemyTags = new[] { "Role.Targetable" };
        profile.FriendTags = new[] { "Actor" };
        profile.ViewDistance = ranged ? 44f : 30f;
        profile.AttackDistance = ranged ? 26f : 2.1f;
        profile.MoveSpeed = ranged ? 3.1f : 3.6f;
        profile.ThinkIntervalNear = 0.14f;
        profile.ThinkIntervalMid = 0.45f;
        profile.ThinkIntervalFar = 1.5f;
        profile.LowHealthThreshold = 20f;
        profile.IdleSchedule = EnsureAISchedule(fileName.Replace(".asset", "_Idle.asset"), profileId + ".idle", new[]
        {
            Task(OUTL_AITaskType.Wait, 0.35f, 0f),
            Task(OUTL_AITaskType.FindTarget, 0.05f, 0f)
        });
        profile.SearchSchedule = EnsureAISchedule(fileName.Replace(".asset", "_Search.asset"), profileId + ".search", new[]
        {
            Task(OUTL_AITaskType.InvestigateStimulus, 0.45f, 1.4f),
            Task(OUTL_AITaskType.Wait, 0.25f, 0f),
            Task(OUTL_AITaskType.FindTarget, 0.05f, 0f)
        });
        profile.CombatSchedule = ranged
            ? EnsureAISchedule(fileName.Replace(".asset", "_Combat.asset"), profileId + ".combat", new[]
            {
                Task(OUTL_AITaskType.FindCover, 0.05f, 16f),
                Task(OUTL_AITaskType.MoveToCover, 0.35f, 1.2f),
                Task(OUTL_AITaskType.FaceTarget, 0.05f, 0f),
                Task(OUTL_AITaskType.AttackTarget, 0.05f, 0f),
                Task(OUTL_AITaskType.Wait, 0.25f, 0f)
            })
            : EnsureAISchedule(fileName.Replace(".asset", "_Combat.asset"), profileId + ".combat", new[]
            {
                Task(OUTL_AITaskType.MoveToTarget, 0.15f, 1.6f),
                Task(OUTL_AITaskType.FaceTarget, 0.05f, 0f),
                Task(OUTL_AITaskType.AttackTarget, 0.05f, 0f),
                Task(OUTL_AITaskType.Wait, 0.18f, 0f)
            });
        profile.FleeSchedule = EnsureAISchedule(fileName.Replace(".asset", "_Flee.asset"), profileId + ".flee", new[]
        {
            Task(OUTL_AITaskType.FleeFromTarget, 0.55f, 8f),
            Task(OUTL_AITaskType.Wait, 0.25f, 0f)
        });
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static OUTL_AIScheduleLite EnsureAISchedule(string fileName, string scheduleId, OUTL_AITaskDef[] tasks)
    {
        string path = AIFolder + "/" + fileName;
        OUTL_AIScheduleLite schedule = AssetDatabase.LoadAssetAtPath<OUTL_AIScheduleLite>(path);
        if (schedule == null)
        {
            schedule = ScriptableObject.CreateInstance<OUTL_AIScheduleLite>();
            AssetDatabase.CreateAsset(schedule, path);
        }

        schedule.ScheduleId = scheduleId;
        schedule.Loop = true;
        schedule.Tasks = tasks;
        EditorUtility.SetDirty(schedule);
        return schedule;
    }

    private static OUTL_AITaskDef Task(OUTL_AITaskType type, float duration, float distance)
    {
        return new OUTL_AITaskDef
        {
            Type = type,
            Duration = duration,
            Distance = distance,
            SpeedMultiplier = 1f,
            Mask = ~0
        };
    }

    private static OUTL_AIPerceptionProfile EnsurePerceptionProfile()
    {
        string path = AIFolder + "/OUTL_AIPerception_Generic.asset";
        OUTL_AIPerceptionProfile profile = AssetDatabase.LoadAssetAtPath<OUTL_AIPerceptionProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<OUTL_AIPerceptionProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        profile.SightConeAngle = 120f;
        profile.SightDistance = 40f;
        profile.RequireLineOfSight = true;
        profile.SightBlockMask = ~0;
        profile.HearingRadius = 18f;
        profile.DangerRadius = 20f;
        profile.FoodRadius = 14f;
        profile.MemoryDuration = 8f;
        profile.UseFactionFilter = true;
        profile.UseProfileEnemyTags = true;
        profile.DangerTags = new[] { "Danger", "Combat" };
        profile.FoodTags = new[] { "Food", "Resource" };
        profile.TargetPriority = 1f;
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static OUTL_AIStateTable EnsureStateTable(FoundationContext ctx)
    {
        string path = AIFolder + "/OUTL_AIStateTable_GenericCombat.asset";
        OUTL_AIStateTable table = AssetDatabase.LoadAssetAtPath<OUTL_AIStateTable>(path);
        if (table == null)
        {
            table = ScriptableObject.CreateInstance<OUTL_AIStateTable>();
            AssetDatabase.CreateAsset(table, path);
        }

        table.Rows = new[]
        {
            StateRow(OUTL_AIStateId.Idle, "no active stimulus", "stimulus or order", "SightEnemy, HeardNoise, TookDamage", "Wait", "none", "stop", null, "idle"),
            StateRow(OUTL_AIStateId.Patrol, "patrol schedule", "stimulus or route end", "SightEnemy, HeardNoise, TookDamage", "Patrol", "route point", "move route", null, "move"),
            StateRow(OUTL_AIStateId.Investigate, "heard noise or interest point", "reached point or target found", "SightEnemy, TookDamage", "Investigate", "last stimulus", "move to stimulus", null, "search"),
            StateRow(OUTL_AIStateId.Search, "lost target", "memory expired or target found", "SightEnemy, TookDamage", "Search", "last known target", "move/search", null, "search"),
            StateRow(OUTL_AIStateId.TakeCover, "danger or hit", "safe or target lost", "LowHealth, Dead", "MoveToCover", "threat", "cover", null, "cover"),
            StateRow(OUTL_AIStateId.AttackRanged, "visible target in preferred range", "target lost or too close", "LowHealth, TookDamage", "AttackTarget", "enemy", "hold range", ctx.ProjectileAttack, "attack_ranged"),
            StateRow(OUTL_AIStateId.AttackMelee, "target in melee range", "target lost", "LowHealth", "AttackTarget", "enemy", "close distance", ctx.MeleeAttack, "attack_melee"),
            StateRow(OUTL_AIStateId.SwitchWeapon, "range/profile mismatch", "profile selected", "Dead", "SwitchWeapon", "enemy", "hold", null, "switch"),
            StateRow(OUTL_AIStateId.EatOrUseResource, "food/resource stimulus", "resource reached or danger", "SightEnemy, SightDanger, TookDamage", "UseResource", "resource", "move to resource", null, "use"),
            StateRow(OUTL_AIStateId.Flee, "danger or low health", "safe or memory expired", "Dead", "FleeFromTarget", "threat", "move away", null, "flee"),
            StateRow(OUTL_AIStateId.Dead, "killed", "none", "none", "Stop", "none", "stop", null, "dead")
        };
        EditorUtility.SetDirty(table);
        return table;
    }

    private static OUTL_AIStateTableRow StateRow(OUTL_AIStateId state, string entry, string exit, string interrupts, string command, string target, string movement, OUTL_AttackProfile attack, string animation)
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
            AttackProfile = attack,
            AnimationHint = animation,
            DebugColor = OUTL_AIStateTable.DefaultColor(state),
            Notes = "Generated abstract foundation row."
        };
    }

    private static OUTL_NPCNavigationProfile EnsureNavigationProfile(string fileName, string profileId)
    {
        string path = NPCFolder + "/" + fileName;
        OUTL_NPCNavigationProfile nav = AssetDatabase.LoadAssetAtPath<OUTL_NPCNavigationProfile>(path);
        if (nav == null)
        {
            nav = ScriptableObject.CreateInstance<OUTL_NPCNavigationProfile>();
            AssetDatabase.CreateAsset(nav, path);
        }

        nav.ProfileId = profileId;
        nav.WalkSpeed = 2.8f;
        nav.RunSpeed = 4.4f;
        nav.RoadPreference = 0.5f;
        nav.AvoidDangerWeight = 1f;
        nav.FactionTerritoryWeight = 1f;
        nav.CanUseAbstractTravel = true;
        nav.CanTeleportWhenDormant = false;
        nav.MaxPathRequestRate = 0.25f;
        nav.RepathCooldown = 2f;
        nav.StuckTimeout = 6f;
        nav.NavAgentRadius = 0.35f;
        nav.NavAgentHeight = 1.8f;
        nav.MaterializeTransformOnNear = true;
        nav.UpdateTransformWhileAbstract = true;
        nav.Sanitize();
        EditorUtility.SetDirty(nav);
        return nav;
    }

    private static OUTL_NPCBehaviorModel EnsureBehaviorModel(string fileName, string modelId, string role, OUTL_NPCNavigationProfile nav, bool creature)
    {
        string path = NPCFolder + "/" + fileName;
        OUTL_NPCBehaviorModel model = AssetDatabase.LoadAssetAtPath<OUTL_NPCBehaviorModel>(path);
        if (model == null)
        {
            model = ScriptableObject.CreateInstance<OUTL_NPCBehaviorModel>();
            AssetDatabase.CreateAsset(model, path);
        }

        model.ModelId = modelId;
        model.Archetype = creature ? OUTL_NPCBehaviorArchetype.Wildlife : OUTL_NPCBehaviorArchetype.Generic;
        model.NavigationProfile = nav;
        model.Schedule = EnsureNPCSchedule(fileName.Replace(".asset", "_Schedule.asset"), modelId + ".schedule", creature);
        model.InterruptPolicies = new[]
        {
            new OUTL_NPCStimulusInterruptPolicy { StimulusTypes = new[] { OUTL_StimulusType.TookDamage, OUTL_StimulusType.SightEnemy, OUTL_StimulusType.HeardCombat }, MinimumPriority = 0.35f, InterruptAction = OUTL_NPCScheduleActionType.Combat, MaxDuration = 10f },
            new OUTL_NPCStimulusInterruptPolicy { StimulusTypes = new[] { OUTL_StimulusType.HeardNoise, OUTL_StimulusType.LostTarget }, MinimumPriority = 0.2f, InterruptAction = OUTL_NPCScheduleActionType.Investigate, MaxDuration = 8f },
            new OUTL_NPCStimulusInterruptPolicy { StimulusTypes = new[] { OUTL_StimulusType.LowHealth, OUTL_StimulusType.SightDanger }, MinimumPriority = 0.3f, InterruptAction = OUTL_NPCScheduleActionType.Flee, MaxDuration = 8f }
        };
        model.UseAIActorForNearTactics = true;
        model.ResumeScheduleAfterInterrupt = true;
        model.StimulusRadius = creature ? 30f : 24f;
        model.StimulusMinimumPriority = 0.15f;
        model.StimulusBudget = 8;
        model.Role = role;
        EditorUtility.SetDirty(model);
        return model;
    }

    private static OUTL_NPCScheduleDef EnsureNPCSchedule(string fileName, string scheduleId, bool creature)
    {
        string path = NPCFolder + "/" + fileName;
        OUTL_NPCScheduleDef schedule = AssetDatabase.LoadAssetAtPath<OUTL_NPCScheduleDef>(path);
        if (schedule == null)
        {
            schedule = ScriptableObject.CreateInstance<OUTL_NPCScheduleDef>();
            AssetDatabase.CreateAsset(schedule, path);
        }

        schedule.ScheduleId = scheduleId;
        schedule.Entries = creature
            ? new[]
            {
                ScheduleEntry("wander", 0f, 0.55f, OUTL_NPCScheduleActionType.Wander),
                ScheduleEntry("feed_or_idle", 0.55f, 0.85f, OUTL_NPCScheduleActionType.Eat),
                ScheduleEntry("rest", 0.85f, 1f, OUTL_NPCScheduleActionType.Idle)
            }
            : new[]
            {
                ScheduleEntry("idle", 0f, 0.25f, OUTL_NPCScheduleActionType.Idle),
                ScheduleEntry("patrol", 0.25f, 0.75f, OUTL_NPCScheduleActionType.Patrol),
                ScheduleEntry("investigate", 0.75f, 1f, OUTL_NPCScheduleActionType.Investigate)
            };
        EditorUtility.SetDirty(schedule);
        return schedule;
    }

    private static OUTL_NPCScheduleEntry ScheduleEntry(string id, float start, float end, OUTL_NPCScheduleActionType action)
    {
        return new OUTL_NPCScheduleEntry
        {
            EntryId = id,
            StartTimeNormalized = start,
            EndTimeNormalized = end,
            Action = action,
            TargetMode = OUTL_NPCScheduleTargetMode.None,
            CanBeInterrupted = true,
            MinDuration = 1f,
            Priority = 1f,
            RouteKey = "route.generic." + id
        };
    }

    private static OUTL_LootTableDef EnsureLootTable(FoundationContext ctx)
    {
        string path = LootFolder + "/OUTL_Loot_Generic_Drop.asset";
        OUTL_LootTableDef table = AssetDatabase.LoadAssetAtPath<OUTL_LootTableDef>(path);
        if (table == null)
        {
            table = ScriptableObject.CreateInstance<OUTL_LootTableDef>();
            AssetDatabase.CreateAsset(table, path);
        }

        table.TableId = "loot.generic.drop";
        table.RollEachEntry = true;
        table.MaxDrops = 4;
        table.Entries = new[]
        {
            new OUTL_LootTableEntry
            {
                Label = "generic_pickup",
                Item = ctx.GenericPickupItem,
                PickupPrefab = ctx.GenericPickupPrefab,
                Chance = 1f,
                Weight = 1f,
                MinCount = 1,
                MaxCount = 3,
                SpawnOneObjectPerCount = false,
                ScatterRadius = 0.45f,
                SpawnOffset = Vector3.up * 0.35f
            }
        };
        EditorUtility.SetDirty(table);
        return table;
    }

    private static OUTL_FactionDef EnsureFaction(string fileName, string id, string displayName)
    {
        string path = DefFolder + "/" + fileName;
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

    private static void ConfigureFactionRelations(FoundationContext ctx)
    {
        ctx.GroupAFaction.Relations = new[]
        {
            new OUTL_FactionRelation { Faction = ctx.GroupBFaction, Relation = -1f },
            new OUTL_FactionRelation { Faction = ctx.ControlledFaction, Relation = -1f },
            new OUTL_FactionRelation { Faction = ctx.NeutralFaction, Relation = 0f }
        };
        ctx.GroupBFaction.Relations = new[]
        {
            new OUTL_FactionRelation { Faction = ctx.GroupAFaction, Relation = -1f },
            new OUTL_FactionRelation { Faction = ctx.ControlledFaction, Relation = 0.25f },
            new OUTL_FactionRelation { Faction = ctx.NeutralFaction, Relation = 0f }
        };
        ctx.ControlledFaction.Relations = new[]
        {
            new OUTL_FactionRelation { Faction = ctx.GroupAFaction, Relation = -1f },
            new OUTL_FactionRelation { Faction = ctx.GroupBFaction, Relation = 0.25f },
            new OUTL_FactionRelation { Faction = ctx.NeutralFaction, Relation = 0f }
        };
        ctx.NeutralFaction.Relations = new OUTL_FactionRelation[0];
        EditorUtility.SetDirty(ctx.GroupAFaction);
        EditorUtility.SetDirty(ctx.GroupBFaction);
        EditorUtility.SetDirty(ctx.ControlledFaction);
        EditorUtility.SetDirty(ctx.NeutralFaction);
    }

    private static GameObject CreateCapsuleRoot(string name)
    {
        GameObject root = new GameObject(name);
        root.name = name;
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.height = 1.8f;
        collider.radius = 0.35f;
        collider.center = new Vector3(0f, 0.9f, 0f);
        AddVisualCapsule(root.transform, "Visual_Capsule", new Vector3(0f, 0.9f, 0f), new Vector3(0.7f, 0.9f, 0.7f), false);
        return root;
    }

    private static GameObject AddVisualCapsule(Transform parent, string name, Vector3 localPosition, Vector3 localScale, bool keepCollider)
    {
        return AddVisualPrimitive(parent, PrimitiveType.Capsule, name, localPosition, localScale, keepCollider);
    }

    private static GameObject AddVisualPrimitive(Transform parent, PrimitiveType type, string name, Vector3 localPosition, Vector3 localScale, bool keepCollider)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = localScale;
        if (!keepCollider)
        {
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        }
        return go;
    }

    private static Transform CreateMuzzle(Transform parent, Vector3 localPosition)
    {
        GameObject muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(parent, false);
        muzzle.transform.localPosition = localPosition;
        muzzle.transform.localRotation = Quaternion.identity;
        return muzzle.transform;
    }

    private static void AddHeadHitbox(GameObject root, OUTL_EntityAdapter entity)
    {
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Hitbox_Head";
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.75f, 0f);
        head.transform.localScale = Vector3.one * 0.32f;
        OUTL_Hitbox hitbox = head.AddComponent<OUTL_Hitbox>();
        hitbox.Entity = entity;
        hitbox.Zone = OUTL_HitboxZone.Head;
        hitbox.DamageMultiplier = 2f;
    }

    private static void AddWeakPointHitbox(GameObject root, OUTL_EntityAdapter entity)
    {
        GameObject weak = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        weak.name = "Hitbox_WeakPoint";
        weak.transform.SetParent(root.transform, false);
        weak.transform.localPosition = new Vector3(0f, 1.1f, -0.35f);
        weak.transform.localScale = Vector3.one * 0.24f;
        OUTL_Hitbox hitbox = weak.AddComponent<OUTL_Hitbox>();
        hitbox.Entity = entity;
        hitbox.Zone = OUTL_HitboxZone.WeakPoint;
        hitbox.DamageMultiplier = 1.75f;
    }

    private static GameObject SavePrefabAndLink(OUTL_EntityDef def, GameObject root, string path)
    {
        GameObject prefab = SavePrefab(root, path);
        if (def != null)
        {
            def.Prefab = prefab;
            EditorUtility.SetDirty(def);
        }
        return prefab;
    }

    private static GameObject SavePrefab(GameObject root, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    private static void EnsureFolders()
    {
        EnsureFolder(RootFolder);
        EnsureFolder(PrefabFolder);
        EnsureFolder(DefFolder);
        EnsureFolder(CombatFolder);
        EnsureFolder(AIFolder);
        EnsureFolder(NPCFolder);
        EnsureFolder(ItemFolder);
        EnsureFolder(LootFolder);
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

    private sealed class FoundationContext
    {
        public OUTL_FactionDef NeutralFaction;
        public OUTL_FactionDef ControlledFaction;
        public OUTL_FactionDef GroupAFaction;
        public OUTL_FactionDef GroupBFaction;
        public OUTL_AttackProfile HitscanAttack;
        public OUTL_AttackProfile ProjectileAttack;
        public OUTL_AttackProfile MeleeAttack;
        public OUTL_EquipmentItemDef PrimaryWeapon;
        public OUTL_EquipmentItemDef SecondaryWeapon;
        public OUTL_EquipmentItemDef MeleeWeapon;
        public OUTL_ItemDef GenericPickupItem;
        public OUTL_AimProfile AimProfile;
        public OUTL_WeaponUseProfile PrimaryUseProfile;
        public OUTL_WeaponUseProfile SecondaryUseProfile;
        public OUTL_WeaponUseProfile MeleeUseProfile;
        public OUTL_LeapAbilityProfile LeapAbility;
        public GameObject GenericProjectilePrefab;
        public GameObject GenericPickupPrefab;
        public OUTL_LootTableDef GenericLootTable;
        public OUTL_AIProfile RangedAIProfile;
        public OUTL_AIProfile MeleeAIProfile;
        public OUTL_AIProfile CreatureAIProfile;
        public OUTL_TacticalProfile RangedTacticalProfile;
        public OUTL_TacticalProfile MeleeTacticalProfile;
        public OUTL_TacticalProfile CreatureTacticalProfile;
        public OUTL_AIPerceptionProfile PerceptionProfile;
        public OUTL_AIStateTable StateTable;
        public OUTL_NPCNavigationProfile NavigationProfile;
        public OUTL_NPCBehaviorModel RangedBehavior;
        public OUTL_NPCBehaviorModel MeleeBehavior;
        public OUTL_NPCBehaviorModel CreatureBehavior;
    }
}
#endif
