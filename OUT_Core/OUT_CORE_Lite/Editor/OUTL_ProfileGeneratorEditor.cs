#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class OUTL_ProfileGeneratorEditor
{
    private const string MenuRoot = "OUT CORE Lite/Legacy Demo/Profiles/";
    private const string Folder = "Assets/OUT/OUT_Core/OUT_CORE_Lite/Templates/Profiles";

    // [MenuItem(MenuRoot + "Create AI Profile Pack")]
    public static void CreateAIProfilePack()
    {
        EnsureFolder(Folder);

        OUTL_AIScheduleLite idle = CreateSchedule("OUTL_AI_Schedule_Idle", "idle", new[]
        {
            Task(OUTL_AITaskType.Stop, 0.05f, 0f),
            Task(OUTL_AITaskType.Wait, 0.8f, 0f),
            Task(OUTL_AITaskType.FindTarget, 0.05f, 0f)
        });

        OUTL_AIScheduleLite combat = CreateSchedule("OUTL_AI_Schedule_Combat_ChaseAttack", "combat.chase_attack", new[]
        {
            Task(OUTL_AITaskType.FaceTarget, 0.05f, 0f),
            Task(OUTL_AITaskType.MoveToTarget, 0.15f, 1.6f),
            Task(OUTL_AITaskType.AttackTarget, 0.05f, 0f),
            Task(OUTL_AITaskType.Wait, 0.2f, 0f)
        });

        OUTL_AIScheduleLite search = CreateSchedule("OUTL_AI_Schedule_Search_LastKnown", "search.last_known", new[]
        {
            Task(OUTL_AITaskType.InvestigateStimulus, 0.5f, 1.2f),
            Task(OUTL_AITaskType.Wait, 0.5f, 0f),
            Task(OUTL_AITaskType.FindTarget, 0.05f, 0f)
        });

        OUTL_AIScheduleLite flee = CreateSchedule("OUTL_AI_Schedule_Flee", "flee", new[]
        {
            Task(OUTL_AITaskType.FleeFromTarget, 0.5f, 8f),
            Task(OUTL_AITaskType.Wait, 0.2f, 0f),
            Task(OUTL_AITaskType.FindTarget, 0.05f, 0f)
        });

        OUTL_AIProfile profile = CreateProfile("ai.goldsrc_lite.default", idle, combat, search, flee);
        AssetDatabase.CreateAsset(profile, AssetDatabase.GenerateUniqueAssetPath(Folder + "/OUTL_AI_Profile_GoldSrcLite.asset"));
        AssetDatabase.SaveAssets();
        Selection.activeObject = profile;
    }

    // [MenuItem(MenuRoot + "Create Basic Patrol Interest AI")]
    public static void CreateBasicPatrolInterestAI()
    {
        EnsureFolder(Folder);

        OUTL_AIScheduleLite idlePatrol = CreateSchedule("OUTL_AI_Schedule_BasicPatrol_Idle", "idle.patrol_wait_scan", new[]
        {
            Task(OUTL_AITaskType.Patrol, 0.1f, 1.2f),
            Task(OUTL_AITaskType.Wait, 1.25f, 0f),
            Task(OUTL_AITaskType.FindTarget, 0.05f, 0f)
        });

        OUTL_AIScheduleLite investigate = CreateSchedule("OUTL_AI_Schedule_BasicPatrol_Investigate", "search.investigate_interest", new[]
        {
            Task(OUTL_AITaskType.InvestigateStimulus, 0.5f, 1.35f),
            Task(OUTL_AITaskType.Wait, 1.0f, 0f),
            Task(OUTL_AITaskType.FindTarget, 0.05f, 0f)
        });

        OUTL_AIScheduleLite combat = CreateSchedule("OUTL_AI_Schedule_BasicPatrol_Combat", "combat.basic", new[]
        {
            Task(OUTL_AITaskType.FindCover, 0.05f, 14f),
            Task(OUTL_AITaskType.MoveToCover, 0.6f, 1.2f),
            Task(OUTL_AITaskType.FaceTarget, 0.05f, 0f),
            Task(OUTL_AITaskType.AttackTarget, 0.05f, 0f),
            Task(OUTL_AITaskType.Wait, 0.35f, 0f)
        });

        OUTL_AIScheduleLite flee = CreateSchedule("OUTL_AI_Schedule_BasicPatrol_Flee", "flee.basic", new[]
        {
            Task(OUTL_AITaskType.FleeFromTarget, 0.5f, 8f),
            Task(OUTL_AITaskType.Wait, 0.3f, 0f),
            Task(OUTL_AITaskType.FindTarget, 0.05f, 0f)
        });

        OUTL_AIProfile profile = CreateProfile("ai.lite.basic_patrol_interest", idlePatrol, combat, investigate, flee);
        profile.ViewDistance = 32f;
        profile.AttackDistance = 2f;
        profile.MoveSpeed = 3.2f;
        profile.ThinkIntervalNear = 0.12f;
        profile.ThinkIntervalMid = 0.4f;
        profile.ThinkIntervalFar = 1.5f;
        AssetDatabase.CreateAsset(profile, AssetDatabase.GenerateUniqueAssetPath(Folder + "/OUTL_AI_Profile_BasicPatrolInterest.asset"));
        AssetDatabase.SaveAssets();
        Selection.activeObject = profile;
    }

    // [MenuItem(MenuRoot + "Create Ready Enemy Ranged Melee Cover")]
    public static void CreateReadyEnemyRangedMeleeCover()
    {
        EnsureFolder(Folder);

        OUTL_AttackProfile ranged = ScriptableObject.CreateInstance<OUTL_AttackProfile>();
        ranged.AttackId = "enemy.ranged.hitscan";
        ranged.Mode = OUTL_AttackMode.Hitscan;
        ranged.Damage = 8f;
        ranged.Range = 42f;
        ranged.Radius = 0.12f;
        ranged.Cooldown = 0.45f;
        ranged.HitMask = ~0;
        AssetDatabase.CreateAsset(ranged, AssetDatabase.GenerateUniqueAssetPath(Folder + "/OUTL_Attack_Enemy_RangedHitscan.asset"));

        OUTL_AttackProfile melee = ScriptableObject.CreateInstance<OUTL_AttackProfile>();
        melee.AttackId = "enemy.melee.close";
        melee.Mode = OUTL_AttackMode.Melee;
        melee.Damage = 14f;
        melee.Range = 1.55f;
        melee.Radius = 0.75f;
        melee.Cooldown = 0.8f;
        melee.HitMask = ~0;
        AssetDatabase.CreateAsset(melee, AssetDatabase.GenerateUniqueAssetPath(Folder + "/OUTL_Attack_Enemy_Melee.asset"));

        OUTL_AIScheduleLite idlePatrol = CreateSchedule("OUTL_AI_Schedule_ReadyEnemy_IdlePatrol", "ready_enemy.idle_patrol", new[]
        {
            Task(OUTL_AITaskType.Patrol, 0.1f, 1.2f),
            Task(OUTL_AITaskType.Wait, 0.8f, 0f),
            Task(OUTL_AITaskType.FindTarget, 0.05f, 0f)
        });

        OUTL_AIScheduleLite investigate = CreateSchedule("OUTL_AI_Schedule_ReadyEnemy_Investigate", "ready_enemy.investigate", new[]
        {
            Task(OUTL_AITaskType.InvestigateStimulus, 0.5f, 1.35f),
            Task(OUTL_AITaskType.Wait, 0.65f, 0f),
            Task(OUTL_AITaskType.FindTarget, 0.05f, 0f)
        });

        OUTL_AIScheduleLite combat = CreateSchedule("OUTL_AI_Schedule_ReadyEnemy_RangedMeleeCover", "ready_enemy.ranged_melee_cover", new[]
        {
            Task(OUTL_AITaskType.FindCover, 0.05f, 18f),
            Task(OUTL_AITaskType.MoveToCover, 0.45f, 1.1f),
            Task(OUTL_AITaskType.FaceTarget, 0.05f, 0f),
            Task(OUTL_AITaskType.AttackTarget, 0.05f, 0f),
            Task(OUTL_AITaskType.Wait, 0.25f, 0f),
            Task(OUTL_AITaskType.FindTarget, 0.05f, 0f)
        });

        OUTL_AIScheduleLite flee = CreateSchedule("OUTL_AI_Schedule_ReadyEnemy_Flee", "ready_enemy.flee", new[]
        {
            Task(OUTL_AITaskType.FindCover, 0.05f, 18f),
            Task(OUTL_AITaskType.MoveToCover, 0.5f, 1.0f),
            Task(OUTL_AITaskType.FleeFromTarget, 0.4f, 7f),
            Task(OUTL_AITaskType.Wait, 0.45f, 0f)
        });

        OUTL_AIProfile profile = CreateProfile("ai.lite.ready_enemy.ranged_melee_cover", idlePatrol, combat, investigate, flee);
        profile.ViewDistance = 38f;
        profile.AttackDistance = 28f;
        profile.MoveSpeed = 3.4f;
        profile.ThinkIntervalNear = 0.1f;
        profile.ThinkIntervalMid = 0.35f;
        profile.ThinkIntervalFar = 1.25f;
        profile.LowHealthThreshold = 18f;
        AssetDatabase.CreateAsset(profile, AssetDatabase.GenerateUniqueAssetPath(Folder + "/OUTL_AI_Profile_ReadyEnemy_RangedMeleeCover.asset"));

        AssetDatabase.SaveAssets();
        Selection.activeObject = profile;
        Debug.Log("OUTL ready enemy preset created. Assign profile to OUTL_AIActor, ranged to OUTL_AttackDriver.Primary, melee to OUTL_AttackDriver.Melee.");
    }

    // [MenuItem(MenuRoot + "Create Dev Console In Scene")]
    public static void CreateDevConsole()
    {
        GameObject root = GameObject.Find("OUTL_Runtime");
        if (root == null)
        {
            root = new GameObject("OUTL_Runtime");
            Undo.RegisterCreatedObjectUndo(root, "Create OUTL Runtime");
        }
        if (root.GetComponent<OUTL_World>() == null) Undo.AddComponent<OUTL_World>(root);
        if (root.GetComponent<OUTL_DevConsole>() == null) Undo.AddComponent<OUTL_DevConsole>(root);
        Selection.activeGameObject = root;
    }

    private static OUTL_AIProfile CreateProfile(string id, OUTL_AIScheduleLite idle, OUTL_AIScheduleLite combat, OUTL_AIScheduleLite search, OUTL_AIScheduleLite flee)
    {
        OUTL_AIProfile profile = ScriptableObject.CreateInstance<OUTL_AIProfile>();
        profile.ProfileId = id;
        profile.UseFactionHostility = true;
        profile.EnemyTags = new[] { "Player" };
        profile.ViewDistance = 45f;
        profile.AttackDistance = 1.8f;
        profile.MoveSpeed = 3.6f;
        profile.ThinkIntervalNear = 0.1f;
        profile.ThinkIntervalMid = 0.35f;
        profile.ThinkIntervalFar = 1.25f;
        profile.LowHealthThreshold = 12f;
        profile.IdleSchedule = idle;
        profile.CombatSchedule = combat;
        profile.SearchSchedule = search;
        profile.FleeSchedule = flee;
        return profile;
    }

    private static OUTL_AITaskDef Task(OUTL_AITaskType type, float duration, float distance)
    {
        return new OUTL_AITaskDef { Type = type, Duration = duration, Distance = distance, SpeedMultiplier = 1f, Mask = ~0 };
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
