#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public static class OUTL_RangedEnemyPresetEditor
{
    private const string MenuRoot = "OUT CORE Lite/Legacy Demo/Scene/Actors/";
    private const string Folder = "Assets/OUT/OUT_Core/OUT_CORE_Lite/Templates/Create";

    // [MenuItem(MenuRoot + "Create Ranged Enemy / Archer")]
    public static void CreateBasicArcherEnemy()
    {
        EnsureFolder(Folder);

        OUTL_FactionDef faction = GetOrCreateFaction("OUTL_Faction_Bandits", "bandits", "Bandits");
        OUTL_EntityDef def = CreateEntityDef("enemy_archer_basic", "Basic Archer Enemy", new[] { "Actor", "NPC", "Enemy", "Archer", "Ranged" }, 55f, 10f, 3.1f);
        OUTL_AIProfile aiProfile = CreateRangedAIProfile("OUTL_AI_Profile_BasicArcher", "enemy.archer.basic");
        OUTL_AttackProfile arrow = CreateArrowAttackProfile();

        GameObject projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        projectilePrefab.name = "OUTL_Arrow_Projectile_RuntimePrefab";
        projectilePrefab.transform.localScale = new Vector3(0.06f, 0.06f, 0.65f);
        Rigidbody rb = projectilePrefab.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        Collider col = projectilePrefab.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        projectilePrefab.AddComponent<OUTL_Projectile>();
        string prefabPath = AssetDatabase.GenerateUniqueAssetPath(Folder + "/OUTL_Arrow_Projectile.prefab");
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(projectilePrefab, prefabPath);
        Object.DestroyImmediate(projectilePrefab);
        arrow.ProjectilePrefab = savedPrefab;
        EditorUtility.SetDirty(arrow);

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "OUTL_Basic_ArcherEnemy";
        Undo.RegisterCreatedObjectUndo(go, "Create OUTL Basic Archer Enemy");

        OUTL_EntityAdapter adapter = go.AddComponent<OUTL_EntityAdapter>();
        adapter.Def = def;
        adapter.Faction = faction;
        adapter.TickLane = OUTL_TickLane.Logic;
        adapter.TickInterval = 0.25f;
        OUTL_DamageReceiver receiver = go.AddComponent<OUTL_DamageReceiver>();
        receiver.Entity = adapter;
        OUTL_Hitbox hitbox = go.AddComponent<OUTL_Hitbox>();
        hitbox.Entity = adapter;
        hitbox.Zone = OUTL_HitboxZone.Torso;
        OUTL_Vitals vitals = go.AddComponent<OUTL_Vitals>();
        vitals.Entity = adapter;
        vitals.InitializeMissingStats = true;
        vitals.DefaultHealth = 55f;
        vitals.DefaultMaxHealth = 55f;
        OUTL_DeathHandler death = go.AddComponent<OUTL_DeathHandler>();
        death.Entity = adapter;
        death.DisableAI = true;
        death.DisableColliders = true;
        death.QueueDespawn = true;

        NavMeshAgent nav = go.AddComponent<NavMeshAgent>();
        nav.speed = 3.1f;
        OUTL_NavMeshMover mover = go.AddComponent<OUTL_NavMeshMover>();
        mover.Agent = nav;
        mover.FallbackSpeed = 3.1f;

        GameObject muzzle = new GameObject("Muzzle_Arrow");
        muzzle.transform.SetParent(go.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 1.35f, 0.45f);

        OUTL_AttackDriver attack = go.AddComponent<OUTL_AttackDriver>();
        attack.Source = adapter;
        attack.Muzzle = muzzle.transform;
        attack.Primary = arrow;
        attack.Melee = CreateMeleeFallback();
        attack.SmartMeleeWhenFireAtPrimary = true;
        attack.MeleeHeight = 1.55f;
        attack.MeleeForwardBias = 0.55f;

        OUTL_AIActor ai = go.AddComponent<OUTL_AIActor>();
        ai.Profile = aiProfile;
        ai.PerceptionProfile = CreatePerceptionProfile();
        ai.StateTable = CreateStateTable();
        ai.Entity = adapter;
        ai.MoveRoot = go.transform;
        ai.NavMover = mover;
        ai.AttackDriver = attack;
        ai.RequireLineOfSightToAcquireTarget = true;
        ai.RequireLineOfSightToKeepTarget = true;
        ai.PreferRangedCombat = true;
        ai.PreferredRange = 24f;
        ai.MinSafeRange = 3f;
        ai.UseStimulusInterrupts = true;
        ai.ExposeDebugState = true;

        OUTL_AIInterceptPlanner intercept = go.AddComponent<OUTL_AIInterceptPlanner>();
        intercept.Actor = ai;
        intercept.Enabled = true;
        intercept.InterceptChance = 0.25f;
        intercept.PredictionTime = 0.55f;
        intercept.RandomSideOffset = 2.0f;
        ai.InterceptPlanner = intercept;

        OUTL_HearingSensor hearing = go.AddComponent<OUTL_HearingSensor>();
        hearing.Actor = ai;
        OUTL_EntityDiary diary = go.AddComponent<OUTL_EntityDiary>();
        diary.Entity = adapter;
        diary.WriteToFile = false;

        AssetDatabase.SaveAssets();
        Selection.activeGameObject = go;
    }

    private static OUTL_AttackProfile CreateArrowAttackProfile()
    {
        OUTL_AttackProfile profile = ScriptableObject.CreateInstance<OUTL_AttackProfile>();
        profile.AttackId = "enemy.archer.arrow";
        profile.Mode = OUTL_AttackMode.Projectile;
        profile.Damage = 18f;
        profile.Range = 42f;
        profile.Radius = 0.12f;
        profile.Cooldown = 1.25f;
        profile.ProjectileSpeed = 24f;
        profile.ProjectileLifetime = 7f;
        profile.ProjectileUsesGravity = true;
        profile.ProjectileGravity = 9.81f;
        profile.AimMode = OUTL_AimMode.BallisticLowArc;
        profile.UseTargetVelocityPrediction = true;
        profile.PredictionStrength = 0.8f;
        profile.MaxPredictionTime = 1.2f;
        profile.HorizontalSpreadDegrees = 2.5f;
        profile.VerticalSpreadDegrees = 1.2f;
        profile.MinSpreadDistance = 4f;
        profile.HitMask = ~0;
        AssetDatabase.CreateAsset(profile, AssetDatabase.GenerateUniqueAssetPath(Folder + "/OUTL_Attack_BasicArrow.asset"));
        return profile;
    }

    private static OUTL_AttackProfile CreateMeleeFallback()
    {
        OUTL_AttackProfile profile = ScriptableObject.CreateInstance<OUTL_AttackProfile>();
        profile.AttackId = "enemy.archer.knife";
        profile.Mode = OUTL_AttackMode.Melee;
        profile.Damage = 8f;
        profile.Range = 1.7f;
        profile.Radius = 0.78f;
        profile.Cooldown = 0.85f;
        profile.HorizontalSpreadDegrees = 8f;
        profile.VerticalSpreadDegrees = 2f;
        profile.HitMask = ~0;
        AssetDatabase.CreateAsset(profile, AssetDatabase.GenerateUniqueAssetPath(Folder + "/OUTL_Attack_ArcherKnife.asset"));
        return profile;
    }

    private static OUTL_AIProfile CreateRangedAIProfile(string assetName, string id)
    {
        OUTL_AIProfile ai = ScriptableObject.CreateInstance<OUTL_AIProfile>();
        ai.ProfileId = id;
        ai.UseFactionHostility = true;
        ai.EnemyTags = new[] { "Player", "Friendly" };
        ai.ViewDistance = 46f;
        ai.AttackDistance = 28f;
        ai.MoveSpeed = 3.1f;
        ai.ThinkIntervalNear = 0.14f;
        ai.ThinkIntervalMid = 0.45f;
        ai.ThinkIntervalFar = 1.4f;
        ai.IdleSchedule = CreateSchedule(assetName + "_Idle", id + ".idle", new[] { Task(OUTL_AITaskType.Wait, 0.65f, 0f), Task(OUTL_AITaskType.FindTarget, 0.05f, 0f) });
        ai.SearchSchedule = CreateSchedule(assetName + "_Search", id + ".search", new[] { Task(OUTL_AITaskType.InvestigateStimulus, 0.5f, 1.4f), Task(OUTL_AITaskType.Wait, 0.45f, 0f), Task(OUTL_AITaskType.FindTarget, 0.05f, 0f) });
        ai.CombatSchedule = CreateSchedule(assetName + "_Combat", id + ".combat", new[] { Task(OUTL_AITaskType.FindCover, 0.05f, 18f), Task(OUTL_AITaskType.MoveToCover, 0.45f, 1.2f), Task(OUTL_AITaskType.FaceTarget, 0.05f, 0f), Task(OUTL_AITaskType.AttackTarget, 0.05f, 0f), Task(OUTL_AITaskType.Wait, 0.35f, 0f), Task(OUTL_AITaskType.FindTarget, 0.05f, 0f) });
        AssetDatabase.CreateAsset(ai, AssetDatabase.GenerateUniqueAssetPath(Folder + "/" + assetName + ".asset"));
        return ai;
    }

    private static OUTL_AIPerceptionProfile CreatePerceptionProfile()
    {
        OUTL_AIPerceptionProfile profile = ScriptableObject.CreateInstance<OUTL_AIPerceptionProfile>();
        profile.SightConeAngle = 125f;
        profile.SightDistance = 46f;
        profile.HearingRadius = 18f;
        profile.DangerRadius = 20f;
        profile.MemoryDuration = 8f;
        profile.RequireLineOfSight = true;
        profile.UseFactionFilter = true;
        profile.UseProfileEnemyTags = true;
        AssetDatabase.CreateAsset(profile, AssetDatabase.GenerateUniqueAssetPath(Folder + "/OUTL_AIPerception_RangedEnemy.asset"));
        return profile;
    }

    private static OUTL_AIStateTable CreateStateTable()
    {
        OUTL_AIStateTable table = ScriptableObject.CreateInstance<OUTL_AIStateTable>();
        table.Rows = new[]
        {
            Row(OUTL_AIStateId.Idle, "Wait", "idle"),
            Row(OUTL_AIStateId.Investigate, "InvestigateStimulus", "search"),
            Row(OUTL_AIStateId.Search, "SearchLastKnown", "search"),
            Row(OUTL_AIStateId.TakeCover, "MoveToCover", "cover"),
            Row(OUTL_AIStateId.AttackRanged, "AttackTarget", "attack_ranged"),
            Row(OUTL_AIStateId.AttackMelee, "AttackTarget", "attack_melee"),
            Row(OUTL_AIStateId.SwitchWeapon, "SwitchWeapon", "switch"),
            Row(OUTL_AIStateId.Flee, "FleeFromTarget", "flee"),
            Row(OUTL_AIStateId.Dead, "Stop", "dead")
        };
        AssetDatabase.CreateAsset(table, AssetDatabase.GenerateUniqueAssetPath(Folder + "/OUTL_AIStateTable_RangedEnemy.asset"));
        return table;
    }

    private static OUTL_AIStateTableRow Row(OUTL_AIStateId state, string command, string animation)
    {
        return new OUTL_AIStateTableRow
        {
            State = state,
            MainCommand = command,
            AnimationHint = animation,
            DebugColor = OUTL_AIStateTable.DefaultColor(state)
        };
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

    private static OUTL_EntityDef CreateEntityDef(string className, string displayName, string[] tags, float health, float damage, float speed)
    {
        OUTL_EntityDef def = ScriptableObject.CreateInstance<OUTL_EntityDef>();
        def.ClassName = className;
        def.DisplayName = displayName;
        def.Tags = tags;
        def.BaseStats = new[] { new OUTL_StatEntry { Key = "Health", Value = health }, new OUTL_StatEntry { Key = "Damage", Value = damage }, new OUTL_StatEntry { Key = "Speed", Value = speed } };
        AssetDatabase.CreateAsset(def, AssetDatabase.GenerateUniqueAssetPath(Folder + "/OUTL_" + className + "_Def.asset"));
        return def;
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
}
#endif
