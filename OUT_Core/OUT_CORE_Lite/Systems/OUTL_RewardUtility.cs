using UnityEngine;

public static class OUTL_LoopKeys
{
    public const string Score = "Score";
    public const string XP = "XP";
    public const string SessionState = "Loop.SessionState";
    public const string RewardGranted = "RewardGranted";
    public const string ChallengeProgress = "ChallengeProgress";
    public const string ChallengeCompleted = "ChallengeCompleted";
    public const string SessionStarted = "SessionStarted";
    public const string SessionWon = "SessionWon";
    public const string SessionFailed = "SessionFailed";
}

public static class OUTL_RewardUtility
{
    public static void Grant(OUTL_RewardDef reward, OUTL_EntityId source, OUTL_EntityId target, Vector3 point)
    {
        if (reward == null || OUTL_World.Instance == null) return;
        OUTL_World world = OUTL_World.Instance;
        OUTL_EntityRuntime runtime;
        world.Registry.TryGet(target, out runtime);
        if (runtime != null)
        {
            if (reward.Points != 0) runtime.Stats.Add(OUTL_LoopKeys.Score, reward.Points);
            if (reward.XP != 0) runtime.Stats.Add(OUTL_LoopKeys.XP, reward.XP);
        }
        if (reward.Items != null)
            for (int i = 0; i < reward.Items.Length; i++)
                if (reward.Items[i] != null) world.Inventory.AddItem(target, reward.Items[i], 1);
        world.Effects.ApplyAll(reward.Effects, source, target, point);
        world.Events.Emit(new OUTL_Event(OUTL_EventType.Custom, source, target) { Key = OUTL_LoopKeys.RewardGranted, IntValue = reward.Points, FloatValue = reward.XP, Point = point });
    }
}
