using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_GameLoopGoldenTester : MonoBehaviour
{
    public OUTL_GameLoopRunner Runner;
    public OUTL_RewardDef TestReward;
    public OUTL_ChallengeDef TestChallenge;
    public OUTL_EntityAdapter TestOwner;
    public bool RunOnStart;
    public int SyntheticTargetCount = 1;
    public int SyntheticRewardPoints = 77;
    public int SyntheticRewardXP = 13;

    private void Start()
    {
        if (RunOnStart) RunNow();
    }

    [ContextMenu("OUT Run Game Loop Golden Test")]
    public void RunNow()
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null) { Fail("OUTL_World missing"); return; }

        ResolveRefs();
        EnsureSyntheticAssets();

        if (TestOwner == null) TestOwner = CreateOwner(world);
        EnsureOwnerTags(world, TestOwner);

        if (Runner == null) Runner = gameObject.AddComponent<OUTL_GameLoopRunner>();
        if (Runner.Owner == null) Runner.Owner = TestOwner;
        if (Runner.GameLoop == null) Runner.GameLoop = CreateLoop(TestChallenge, TestReward);

        float beforeScore = TestOwner.Runtime != null ? TestOwner.Runtime.Stats.Get(OUTL_LoopKeys.Score, 0f) : 0f;
        int beforeProgress = Runner.GetProgress(TestChallenge.ChallengeId);

        Runner.StartLoop();
        OUTL_Event evt = new OUTL_Event(TestChallenge.ListenEvent, TestOwner.Id, TestOwner.Id)
        {
            Key = TestChallenge.ListenKey,
            Point = TestOwner.transform.position
        };
        world.Events.Emit(evt);
        world.Events.Flush();

        int afterProgress = Runner.GetProgress(TestChallenge.ChallengeId);
        float afterScore = TestOwner.Runtime != null ? TestOwner.Runtime.Stats.Get(OUTL_LoopKeys.Score, 0f) : 0f;

        if (afterProgress <= beforeProgress) { Fail("challenge did not progress"); return; }
        if (TestChallenge.TargetCount <= 1 && !Runner.IsCompleted(TestChallenge.ChallengeId)) { Fail("challenge did not complete"); return; }
        if (TestReward != null && afterScore < beforeScore + TestReward.Points) { Fail("reward points not granted"); return; }

        Debug.Log("[OUTL GameLoopGolden] PASS progress=" + beforeProgress + "->" + afterProgress + " score=" + beforeScore + "->" + afterScore, this);
    }

    private void ResolveRefs()
    {
        if (Runner == null) Runner = FindObjectOfType<OUTL_GameLoopRunner>();
        if (TestOwner == null) TestOwner = FindObjectOfType<OUTL_EntityAdapter>();
    }

    private void EnsureSyntheticAssets()
    {
        if (TestReward == null)
        {
            TestReward = ScriptableObject.CreateInstance<OUTL_RewardDef>();
            TestReward.RewardId = "golden.reward.synthetic";
            TestReward.DisplayName = "Golden Synthetic Reward";
            TestReward.CurrencyId = "score";
            TestReward.Points = Mathf.Max(1, SyntheticRewardPoints);
            TestReward.XP = Mathf.Max(0, SyntheticRewardXP);
        }

        if (TestChallenge == null)
        {
            TestChallenge = ScriptableObject.CreateInstance<OUTL_ChallengeDef>();
            TestChallenge.ChallengeId = "golden.challenge.synthetic";
            TestChallenge.DisplayName = "Golden Synthetic Challenge";
            TestChallenge.ListenEvent = OUTL_EventType.Custom;
            TestChallenge.ListenKey = "Golden";
            TestChallenge.RequiredSourceTags = new[] { "Role.Controlled" };
            TestChallenge.RequiredTargetTags = new[] { "Role.Opposition" };
            TestChallenge.TargetCount = Mathf.Max(1, SyntheticTargetCount);
            TestChallenge.Rewards = new[] { TestReward };
            TestChallenge.CompleteOnlyOnce = true;
        }
    }

    private OUTL_GameLoopDef CreateLoop(OUTL_ChallengeDef challenge, OUTL_RewardDef reward)
    {
        OUTL_GameLoopDef loop = ScriptableObject.CreateInstance<OUTL_GameLoopDef>();
        loop.LoopId = "golden.loop.synthetic";
        loop.DisplayName = "Golden Synthetic Loop";
        loop.StartupChallenges = new[] { challenge };
        loop.RewardTable = reward != null ? new[] { reward } : null;
        loop.AutoStart = true;
        loop.ResetChallengesOnStart = true;
        return loop;
    }

    private OUTL_EntityAdapter CreateOwner(OUTL_World world)
    {
        GameObject go = new GameObject("OUTL_GameLoopGolden_Owner");
        OUTL_EntityAdapter e = go.AddComponent<OUTL_EntityAdapter>();
        e.ClassNameOverride = "golden_owner";
        e.TargetName = "golden_owner";
        e.StableId = "golden_owner";
        EnsureOwnerTags(world, e);
        e.RegisterNow(world);
        return e;
    }

    private void EnsureOwnerTags(OUTL_World world, OUTL_EntityAdapter owner)
    {
        if (owner == null) return;
        if (owner.Def == null)
        {
            OUTL_EntityDef def = ScriptableObject.CreateInstance<OUTL_EntityDef>();
            def.ClassName = "golden_owner";
            def.DisplayName = "Golden Owner";
            def.Tags = new[] { "Actor", "Player", "Enemy", "Objective", "Role.Controlled", "Role.Opposition", "Role.Objective", "Role.Targetable" };
            def.BaseStats = new[]
            {
                new OUTL_StatEntry { Key = "Health", Value = 100f },
                new OUTL_StatEntry { Key = OUTL_LoopKeys.Score, Value = 0f },
                new OUTL_StatEntry { Key = OUTL_LoopKeys.XP, Value = 0f }
            };
            owner.Def = def;
        }
        if (world != null)
        {
            if (owner.Runtime == null) owner.RegisterNow(world);
            else owner.RebindRuntime(world);
        }
    }

    private void Fail(string msg)
    {
        Debug.LogError("[OUTL GameLoopGolden] FAIL: " + msg, this);
    }
}
