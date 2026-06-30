#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class OUTL_TacticalAIWorkbenchEditor
{
    private const string MenuRoot = "OUT CORE Lite/AI/";
    private const string LegacySampleMenuRoot = "OUT CORE Lite/Legacy Demo/AI/";
    private const string SampleRoot = "Assets/OUT/OUT_Core/OUT_CORE_Lite/Samples/TacticalAI";

    // [MenuItem(LegacySampleMenuRoot + "Create Tactical AI Sample")]
    public static void CreateTacticalAISample()
    {
        EnsureSampleFolder();
        OUTL_AttackProfile attack = EnsureAttackProfile();
        OUTL_AimProfile aim = EnsureAimProfile();
        OUTL_TacticalProfile tactical = EnsureTacticalProfile(aim);
        tactical.PrimaryAbility = null;
        tactical.LeapAbility = null;

        GameObject root = new GameObject("OUTL_TacticalAI_Sample");
        Undo.RegisterCreatedObjectUndo(root, "Create OUTL Tactical AI Sample");

        OUTL_World world = Object.FindObjectOfType<OUTL_World>();
        if (world == null)
        {
            GameObject worldGo = new GameObject("OUTL_World");
            Undo.RegisterCreatedObjectUndo(worldGo, "Create OUTL World");
            worldGo.transform.SetParent(root.transform);
            world = Undo.AddComponent<OUTL_World>(worldGo);
        }

        OUTL_SquadBlackboard blackboard = root.AddComponent<OUTL_SquadBlackboard>();
        OUTL_CoverPoint coverA = CreateCoverPoint(root.transform, "OUTL_Cover_A", new Vector3(-2f, 0f, 6f));
        OUTL_CoverPoint coverB = CreateCoverPoint(root.transform, "OUTL_Cover_B", new Vector3(2f, 0f, 6f));
        coverB.SectorId = 1;

        GameObject actor = CreateTacticalActor(root.transform, "OUTL_Tactical_Actor", new Vector3(0f, 0f, 0f), attack, aim, tactical, blackboard, OUTL_SquadRole.Rifle);
        GameObject target = CreateDamageableActor(root.transform, "OUTL_Target_Actor", new Vector3(0f, 0f, 10f));
        OUTL_AIActor ai = actor.GetComponent<OUTL_AIActor>();
        OUTL_EntityAdapter targetEntity = target.GetComponent<OUTL_EntityAdapter>();
        if (ai != null && targetEntity != null)
        {
            ai.CurrentTarget = targetEntity.Id;
            ai.LastKnownTargetPosition = target.transform.position;
        }

        Selection.activeGameObject = root;
        EditorUtility.SetDirty(root);
        Debug.Log("Created OUTL Tactical AI sample. It uses BotInputDriver -> ActorControlBridge -> Nav/Attack sinks.", root);
    }

    // [MenuItem(LegacySampleMenuRoot + "Create Humanoid Soldier Sample")]
    public static void CreateHumanoidSoldierSample()
    {
        EnsureSampleFolder();
        OUTL_AttackProfile attack = EnsureAttackProfile();
        OUTL_AimProfile aim = EnsureAimProfile();
        OUTL_TacticalProfile tactical = EnsureTacticalProfile(aim);
        tactical.PrimaryAbility = null;
        tactical.LeapAbility = null;
        tactical.PreferredRange = 18f;
        tactical.MinSafeRange = 4f;
        EditorUtility.SetDirty(tactical);

        GameObject root = new GameObject("OUTL_HumanoidSoldier_Sample");
        Undo.RegisterCreatedObjectUndo(root, "Create OUTL Humanoid Soldier Sample");
        OUTL_SquadBlackboard blackboard = Undo.AddComponent<OUTL_SquadBlackboard>(root);
        CreateCoverPoint(root.transform, "OUTL_Soldier_Cover_A", new Vector3(-2f, 0f, 6f));
        CreateTacticalActor(root.transform, "OUTL_Soldier_Actor", Vector3.zero, attack, aim, tactical, blackboard, OUTL_SquadRole.Rifle);
        CreateDamageableActor(root.transform, "OUTL_Soldier_Target", new Vector3(0f, 0f, 12f));
        Selection.activeGameObject = root;
        Debug.Log("Created OUTL humanoid soldier sample: ranged actor, aim sink, fire authorization and cover-ready tactical profile.", root);
    }

    // [MenuItem(LegacySampleMenuRoot + "Create Quake Demon Leap Sample")]
    public static void CreateQuakeDemonLeapSample()
    {
        CreateLeapCreatureSample("OUTL_QuakeDemonLeap_Sample", "OUTL_LeapCreature_Actor", "OUTL_QuakeDemon_Leap.asset", OUTL_SquadRole.Creature, 4f, 18f, 16f, 1.8f);
    }

    // [MenuItem(LegacySampleMenuRoot + "Create Spider Pounce Sample")]
    public static void CreateSpiderPounceSample()
    {
        CreateLeapCreatureSample("OUTL_SpiderPounce_Sample", "OUTL_PounceCreature_Actor", "OUTL_SpiderPounce_Leap.asset", OUTL_SquadRole.Creature, 2.5f, 11f, 12f, 1.2f);
    }

    // [MenuItem(MenuRoot + "Create Cover Point")]
    public static void CreateCoverPoint()
    {
        GameObject go = new GameObject("OUTL_CoverPoint");
        Undo.RegisterCreatedObjectUndo(go, "Create OUTL Cover Point");
        BoxCollider box = Undo.AddComponent<BoxCollider>(go);
        box.size = new Vector3(2f, 1.6f, 0.25f);
        OUTL_CoverPoint cover = Undo.AddComponent<OUTL_CoverPoint>(go);
        Selection.activeGameObject = go;
        EditorUtility.SetDirty(cover);
    }

    // [MenuItem(MenuRoot + "Create Squad Blackboard")]
    public static void CreateSquadBlackboard()
    {
        GameObject go = new GameObject("OUTL_SquadBlackboard");
        Undo.RegisterCreatedObjectUndo(go, "Create OUTL Squad Blackboard");
        Undo.AddComponent<OUTL_SquadBlackboard>(go);
        Selection.activeGameObject = go;
    }

    // [MenuItem(MenuRoot + "Validate Tactical AI Setup")]
    public static void ValidateTacticalAISetup()
    {
        OUTL_TacticalPlanner[] planners = Object.FindObjectsOfType<OUTL_TacticalPlanner>(true);
        int warnings = 0;
        for (int i = 0; i < planners.Length; i++)
        {
            OUTL_TacticalPlanner planner = planners[i];
            if (planner == null) continue;
            OUTL_EntityAdapter entity = planner.GetComponent<OUTL_EntityAdapter>();
            OUTL_ActorControlBridge bridge = planner.GetComponent<OUTL_ActorControlBridge>();
            OUTL_BotInputDriver bot = planner.GetComponent<OUTL_BotInputDriver>();
            OUTL_AttackDriver attack = planner.GetComponent<OUTL_AttackDriver>();
            OUTL_NavMeshMover nav = planner.GetComponent<OUTL_NavMeshMover>();
            OUTL_AimInputSink aimSink = planner.GetComponent<OUTL_AimInputSink>();
            OUTL_AbilityInputSink abilitySink = planner.GetComponent<OUTL_AbilityInputSink>();
            if (entity == null) { Debug.LogWarning("Tactical AI missing OUTL_EntityAdapter.", planner); warnings++; }
            if (bridge == null) { Debug.LogWarning("Tactical AI missing OUTL_ActorControlBridge.", planner); warnings++; }
            if (bot == null) { Debug.LogWarning("Tactical AI missing OUTL_BotInputDriver.", planner); warnings++; }
            if (attack == null) { Debug.LogWarning("Tactical AI missing OUTL_AttackDriver.", planner); warnings++; }
            if (nav == null) { Debug.LogWarning("Tactical AI has no OUTL_NavMeshMover; mark stationary intentionally or add one.", planner); warnings++; }
            if (planner.Profile == null) { Debug.LogWarning("Tactical AI missing OUTL_TacticalProfile.", planner); warnings++; }
            if (planner.AimPlanner == null && planner.GetComponent<OUTL_AimPlanner>() == null) { Debug.LogWarning("Tactical AI missing OUTL_AimPlanner.", planner); warnings++; }
            if (aimSink == null) { Debug.LogWarning("Tactical AI missing OUTL_AimInputSink; aim smoothing will not run before weapon fire.", planner); warnings++; }
            if (planner.Profile != null && (planner.Profile.PrimaryAbility != null || planner.Profile.LeapAbility != null) && abilitySink == null)
            {
                Debug.LogWarning("Tactical AI profile has ability but actor has no OUTL_AbilityInputSink.", planner);
                warnings++;
            }
            if (bridge != null && bridge.InputSinkBehaviours != null && !HasPhasedWeaponAfterMovement(bridge.InputSinkBehaviours))
            {
                Debug.LogWarning("ActorControlBridge sink list should contain Movement/Aim before Weapon.", bridge);
                warnings++;
            }
            if (attack != null && attack.Primary != null && attack.Primary.Mode == OUTL_AttackMode.Projectile && (attack.Primary.ProjectilePrefab == null || attack.Primary.ProjectilePrefab.GetComponent<OUTL_Projectile>() == null))
            {
                Debug.LogWarning("Projectile tactical profile needs ProjectilePrefab with OUTL_Projectile already authored.", attack.Primary);
                warnings++;
            }
        }

        int forbiddenTypes = CountForbiddenAITypes();
        if (forbiddenTypes > 0)
        {
            warnings += forbiddenTypes;
            Debug.LogWarning("OUTL Tactical AI found content-specific AI type names. Use generic actor/profile/ability composition instead. count=" + forbiddenTypes);
        }

        Debug.Log("OUTL Tactical AI validation complete. planners=" + planners.Length + " warnings=" + warnings + ".");
    }

    private static GameObject CreateTacticalActor(Transform parent, string name, Vector3 position, OUTL_AttackProfile attackProfile, OUTL_AimProfile aimProfile, OUTL_TacticalProfile tacticalProfile, OUTL_SquadBlackboard blackboard, OUTL_SquadRole role)
    {
        GameObject go = CreateDamageableActor(parent, name, position);
        OUTL_EntityAdapter entity = go.GetComponent<OUTL_EntityAdapter>();
        entity.ClassNameOverride = "actor_tactical";
        OUTL_AttackDriver attack = Undo.AddComponent<OUTL_AttackDriver>(go);
        attack.Source = entity;
        attack.Primary = attackProfile;
        attack.Muzzle = EnsureMuzzle(go.transform);

        OUTL_AIActor ai = Undo.AddComponent<OUTL_AIActor>(go);
        ai.Entity = entity;
        ai.Profile = ScriptableObject.CreateInstance<OUTL_AIProfile>();
        ai.Profile.ViewDistance = 30f;
        ai.Profile.AttackDistance = 18f;
        ai.AttackDriver = attack;
        ai.UseActorInputContract = true;
        ai.PreferRangedCombat = true;

        OUTL_NavMeshMover nav = Undo.AddComponent<OUTL_NavMeshMover>(go);
        nav.UseOUTLTick = true;
        ai.NavMover = nav;

        OUTL_AimPlanner aim = Undo.AddComponent<OUTL_AimPlanner>(go);
        aim.Entity = entity;
        aim.AttackDriver = attack;
        aim.Profile = aimProfile;

        OUTL_AIArsenalSelector arsenal = Undo.AddComponent<OUTL_AIArsenalSelector>(go);
        arsenal.Entity = entity;
        arsenal.AttackDriver = attack;

        OUTL_TacticalPlanner planner = Undo.AddComponent<OUTL_TacticalPlanner>(go);
        planner.Entity = entity;
        planner.AIActor = ai;
        planner.Profile = tacticalProfile;
        planner.Arsenal = arsenal;
        planner.AimPlanner = aim;
        planner.SquadBlackboard = blackboard;

        OUTL_BotInputDriver bot = Undo.AddComponent<OUTL_BotInputDriver>(go);
        bot.Entity = entity;
        bot.AIActor = ai;
        bot.TacticalPlanner = planner;
        bot.AimPlanner = aim;
        bot.Arsenal = arsenal;

        OUTL_NavMoverInputSink navSink = Undo.AddComponent<OUTL_NavMoverInputSink>(go);
        navSink.Entity = entity;
        navSink.NavMover = nav;
        OUTL_AimInputSink aimSink = Undo.AddComponent<OUTL_AimInputSink>(go);
        aimSink.Entity = entity;
        aimSink.AngularSpeed = aimProfile != null ? Mathf.Max(1f, aimProfile.AimAngularSpeed) : 240f;
        OUTL_AttackDriverInputSink attackSink = Undo.AddComponent<OUTL_AttackDriverInputSink>(go);
        attackSink.Entity = entity;
        attackSink.AttackDriver = attack;
        OUTL_AbilityInputSink abilitySink = Undo.AddComponent<OUTL_AbilityInputSink>(go);
        abilitySink.Entity = entity;
        abilitySink.NavMover = nav;
        abilitySink.PrimaryAbility = tacticalProfile != null ? (tacticalProfile.LeapAbility != null ? tacticalProfile.LeapAbility : tacticalProfile.PrimaryAbility) : null;
        abilitySink.AllowTransformFallback = true;
        planner.AbilitySink = abilitySink;

        OUTL_ActorControlBridge bridge = Undo.AddComponent<OUTL_ActorControlBridge>(go);
        bridge.Entity = entity;
        bridge.InputSourceBehaviour = bot;
        bridge.InputSinkBehaviours = new Behaviour[] { navSink, aimSink, attackSink, abilitySink };

        OUTL_SquadMember member = Undo.AddComponent<OUTL_SquadMember>(go);
        member.Entity = entity;
        member.Actor = ai;
        member.Blackboard = blackboard;
        member.RoleKind = role;
        if (blackboard != null) blackboard.Register(member);

        return go;
    }

    private static GameObject CreateDamageableActor(Transform parent, string name, Vector3 position)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Undo.RegisterCreatedObjectUndo(go, "Create OUTL Actor");
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = position;

        OUTL_EntityAdapter entity = Undo.AddComponent<OUTL_EntityAdapter>(go);
        entity.ClassNameOverride = "actor_generic";
        entity.TargetName = name.ToLowerInvariant();
        OUTL_Vitals vitals = Undo.AddComponent<OUTL_Vitals>(go);
        vitals.Entity = entity;
        vitals.InitializeMissingStats = true;
        vitals.DefaultHealth = 100f;
        vitals.DefaultMaxHealth = 100f;
        Undo.AddComponent<OUTL_DamageReceiver>(go).Entity = entity;
        Undo.AddComponent<OUTL_DeathRuntime>(go).Entity = entity;
        Undo.AddComponent<OUTL_DeathHandler>(go).Entity = entity;
        OUTL_Hitbox hitbox = Undo.AddComponent<OUTL_Hitbox>(go);
        hitbox.Entity = entity;
        return go;
    }

    private static OUTL_CoverPoint CreateCoverPoint(Transform parent, string name, Vector3 position)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create OUTL Cover Point");
        go.transform.SetParent(parent);
        go.transform.position = position;
        BoxCollider box = Undo.AddComponent<BoxCollider>(go);
        box.size = new Vector3(2f, 1.6f, 0.25f);
        OUTL_CoverPoint cover = Undo.AddComponent<OUTL_CoverPoint>(go);
        cover.CoverKind = OUTL_CoverKind.High;
        return cover;
    }

    private static Transform EnsureMuzzle(Transform parent)
    {
        Transform existing = parent.Find("Muzzle");
        if (existing != null) return existing;
        GameObject go = new GameObject("Muzzle");
        Undo.RegisterCreatedObjectUndo(go, "Create OUTL Muzzle");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 1.25f, 0.45f);
        go.transform.localRotation = Quaternion.identity;
        return go.transform;
    }

    private static OUTL_AttackProfile EnsureAttackProfile()
    {
        EnsureSampleFolder();
        string path = SampleRoot + "/OUTL_TacticalAI_Hitscan.asset";
        OUTL_AttackProfile profile = AssetDatabase.LoadAssetAtPath<OUTL_AttackProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<OUTL_AttackProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }
        profile.AttackId = "tactical.primary";
        profile.Mode = OUTL_AttackMode.Hitscan;
        profile.Damage = 12f;
        profile.Range = 35f;
        profile.Cooldown = 0.25f;
        profile.HitDamageKey = "tactical";
        profile.HitMask = ~0;
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    private static OUTL_AimProfile EnsureAimProfile()
    {
        EnsureSampleFolder();
        string path = SampleRoot + "/OUTL_TacticalAI_Aim.asset";
        OUTL_AimProfile profile = AssetDatabase.LoadAssetAtPath<OUTL_AimProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<OUTL_AimProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }
        profile.ReactionDelayMin = 0.15f;
        profile.ReactionDelayMax = 0.45f;
        profile.FireDelayMin = 0.05f;
        profile.FireDelayMax = 0.25f;
        profile.AimHoldSeconds = 0.1f;
        profile.UseFriendlyFire = true;
        profile.RequireLineOfSight = true;
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    private static OUTL_TacticalProfile EnsureTacticalProfile(OUTL_AimProfile aim)
    {
        EnsureSampleFolder();
        string path = SampleRoot + "/OUTL_TacticalAI_Profile.asset";
        OUTL_TacticalProfile profile = AssetDatabase.LoadAssetAtPath<OUTL_TacticalProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<OUTL_TacticalProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }
        profile.AimProfile = aim;
        profile.UseCover = true;
        profile.AllowSuppress = true;
        profile.PreferredRange = 18f;
        profile.MinSafeRange = 4f;
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    private static void CreateLeapCreatureSample(string rootName, string actorName, string leapAssetName, OUTL_SquadRole role, float minDistance, float maxDistance, float speed, float arc)
    {
        EnsureSampleFolder();
        OUTL_AttackProfile attack = EnsureAttackProfile();
        OUTL_AimProfile aim = EnsureAimProfile();
        OUTL_LeapAbilityProfile leap = EnsureLeapProfile(leapAssetName, minDistance, maxDistance, speed, arc);
        OUTL_TacticalProfile tactical = EnsureTacticalProfile(aim);
        tactical.PrimaryAbility = leap;
        tactical.LeapAbility = leap;
        tactical.PreferredRange = Mathf.Max(2f, minDistance + 1f);
        tactical.MinSafeRange = 1f;
        EditorUtility.SetDirty(tactical);
        AssetDatabase.SaveAssets();

        GameObject root = new GameObject(rootName);
        Undo.RegisterCreatedObjectUndo(root, "Create OUTL Leap Creature Sample");
        OUTL_SquadBlackboard blackboard = Undo.AddComponent<OUTL_SquadBlackboard>(root);
        GameObject actor = CreateTacticalActor(root.transform, actorName, Vector3.zero, attack, aim, tactical, blackboard, role);
        CharacterController motor = Undo.AddComponent<CharacterController>(actor);
        motor.height = 2f;
        motor.radius = 0.35f;
        OUTL_AbilityInputSink ability = actor.GetComponent<OUTL_AbilityInputSink>();
        if (ability != null) ability.CharacterController = motor;
        CreateDamageableActor(root.transform, "OUTL_Leap_Target", new Vector3(0f, 0f, Mathf.Clamp(maxDistance - 1f, minDistance + 0.5f, maxDistance)));
        Selection.activeGameObject = root;
        Debug.Log("Created OUTL generic leap creature sample. It is a profile/ability composition, not a creature-specific AI class.", root);
    }

    private static OUTL_LeapAbilityProfile EnsureLeapProfile(string assetName, float minDistance, float maxDistance, float speed, float arc)
    {
        EnsureSampleFolder();
        string path = SampleRoot + "/" + assetName;
        OUTL_LeapAbilityProfile profile = AssetDatabase.LoadAssetAtPath<OUTL_LeapAbilityProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<OUTL_LeapAbilityProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }
        profile.AbilityId = "ability.leap";
        profile.AbilitySlot = 0;
        profile.MinRange = Mathf.Max(0f, minDistance);
        profile.MaxRange = Mathf.Max(profile.MinRange + 0.1f, maxDistance);
        profile.PreferWhenTargetDistanceMin = profile.MinRange;
        profile.PreferWhenTargetDistanceMax = profile.MaxRange;
        profile.Cooldown = 2.5f;
        profile.WindupTime = 0.15f;
        profile.RecoveryTime = 0.45f;
        profile.RequiresLineOfSight = true;
        profile.RequiresGround = false;
        profile.LeapSpeed = Mathf.Max(0.1f, speed);
        profile.LeapArcHeight = Mathf.Max(0f, arc);
        profile.LeapDuration = 0.45f;
        profile.ImpactRadius = 1.25f;
        profile.ImpactDamage = 20f;
        profile.UseCharacterMotor = true;
        profile.UsePhysicsImpulse = false;
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    private static bool HasPhasedWeaponAfterMovement(Behaviour[] behaviours)
    {
        bool sawMovement = false;
        bool sawWeapon = false;
        int lastPhase = -1;
        for (int i = 0; i < behaviours.Length; i++)
        {
            OUTL_IActorInputSink sink = behaviours[i] as OUTL_IActorInputSink;
            if (sink == null) continue;
            OUTL_IActorInputPhasedSink phased = sink as OUTL_IActorInputPhasedSink;
            OUTL_ActorInputPhase phase = phased != null ? phased.Phase : OUTL_ActorInputPhase.Interaction;
            if ((int)phase < lastPhase) return false;
            lastPhase = (int)phase;
            if (phase == OUTL_ActorInputPhase.Movement) sawMovement = true;
            if (phase == OUTL_ActorInputPhase.Weapon) sawWeapon = true;
        }
        return sawMovement && sawWeapon;
    }

    private static int CountForbiddenAITypes()
    {
        int count = 0;
        TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom<MonoBehaviour>();
        for (int i = 0; i < types.Count; i++)
        {
            System.Type type = types[i];
            string name = type != null ? type.Name : string.Empty;
            if (name == "BotEntity" || name == "SpiderAI" || name == "DemonAI")
            {
                count++;
                Debug.LogWarning("Forbidden content-specific AI type found: " + name);
            }
        }
        return count;
    }

    private static void EnsureSampleFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/OUT/OUT_Core/OUT_CORE_Lite/Samples"))
            AssetDatabase.CreateFolder("Assets/OUT/OUT_Core/OUT_CORE_Lite", "Samples");
        if (!AssetDatabase.IsValidFolder(SampleRoot))
            AssetDatabase.CreateFolder("Assets/OUT/OUT_Core/OUT_CORE_Lite/Samples", "TacticalAI");
    }
}
#endif
