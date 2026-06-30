using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_GameLoopRunner : MonoBehaviour, OUTL_IEventListener, OUTL_IComponentSaveParticipant
{
    public OUTL_EntityAdapter Owner;
    public OUTL_GameLoopDef GameLoop;
    public bool AutoStart = true;
    public bool AutoRegisterEvents = true;

    private readonly List<OUTL_ChallengeDef> active = new List<OUTL_ChallengeDef>(32);
    private readonly List<string> ids = new List<string>(32);
    private readonly List<int> counts = new List<int>(32);
    private readonly List<string> completed = new List<string>(32);
    private bool registered;
    private bool started;

    public string OUTL_SaveKey { get { return "OUTL_GameLoopRunner"; } }

    private void Awake()
    {
        if (Owner == null) Owner = GetComponent<OUTL_EntityAdapter>();
    }

    private void OnEnable()
    {
        RegisterEvents();
        if (AutoStart && GameLoop != null && GameLoop.AutoStart) StartLoop();
    }

    private void OnDisable()
    {
        if (registered && OUTL_World.Instance != null) OUTL_World.Instance.Events.Unregister(this);
        registered = false;
    }

    [ContextMenu("OUT Start Game Loop")]
    public void StartLoop()
    {
        if (GameLoop == null) return;
        started = true;
        active.Clear();
        if (GameLoop.StartupChallenges != null)
            for (int i = 0; i < GameLoop.StartupChallenges.Length; i++)
                if (GameLoop.StartupChallenges[i] != null) active.Add(GameLoop.StartupChallenges[i]);
        if (GameLoop.ResetChallengesOnStart) ResetProgress();
        OUTL_World world = OUTL_World.Instance;
        OUTL_EntityId ownerId = Owner != null ? Owner.Id : OUTL_EntityId.None;
        if (Owner != null && Owner.Runtime != null) Owner.Runtime.State.SetString(OUTL_LoopKeys.SessionState, "Running");
        if (world != null)
        {
            world.Effects.ApplyAll(GameLoop.OnSessionStart, ownerId, ownerId, transform.position);
            world.Events.Emit(new OUTL_Event(OUTL_EventType.Custom, ownerId, ownerId) { Key = OUTL_LoopKeys.SessionStarted, Point = transform.position });
        }
    }

    public void OUTL_OnEvent(in OUTL_Event evt, OUTL_World world)
    {
        if (!started || world == null) return;
        for (int i = 0; i < active.Count; i++) Process(active[i], evt, world);
    }

    public void AddChallenge(OUTL_ChallengeDef challenge)
    {
        if (challenge != null && !active.Contains(challenge)) active.Add(challenge);
    }

    public int GetProgress(string challengeId)
    {
        int index = ids.IndexOf(challengeId);
        return index >= 0 ? counts[index] : 0;
    }

    public bool IsCompleted(string challengeId)
    {
        return completed.Contains(challengeId);
    }

    [ContextMenu("OUT Win Game Loop")]
    public void WinLoop() { EndLoop(true); }

    [ContextMenu("OUT Fail Game Loop")]
    public void FailLoop() { EndLoop(false); }

    public void OUTL_Capture(OUTL_ComponentSaveWriter writer)
    {
        writer.SetFlag("started", started);
        for (int i = 0; i < ids.Count; i++) writer.SetInt(ids[i], counts[i]);
        for (int i = 0; i < completed.Count; i++) writer.SetFlag("done." + completed[i], true);
    }

    public void OUTL_Restore(OUTL_ComponentSaveReader reader)
    {
        started = reader.GetFlag("started", started);
        if (GameLoop == null || GameLoop.StartupChallenges == null) return;
        active.Clear();
        for (int i = 0; i < GameLoop.StartupChallenges.Length; i++)
        {
            OUTL_ChallengeDef c = GameLoop.StartupChallenges[i];
            if (c == null) continue;
            active.Add(c);
            SetProgress(c.ChallengeId, reader.GetInt(c.ChallengeId, GetProgress(c.ChallengeId)));
            if (reader.GetFlag("done." + c.ChallengeId)) MarkCompleted(c.ChallengeId);
        }
    }

    private void Process(OUTL_ChallengeDef challenge, in OUTL_Event evt, OUTL_World world)
    {
        if (challenge == null) return;
        if (challenge.ListenEvent != OUTL_EventType.None && challenge.ListenEvent != evt.Type) return;
        if (!string.IsNullOrEmpty(challenge.ListenKey) && challenge.ListenKey != evt.Key) return;
        if (!TagsMatch(world, evt.Source, challenge.RequiredSourceTags)) return;
        if (!TagsMatch(world, evt.Target, challenge.RequiredTargetTags)) return;
        if (challenge.CompleteOnlyOnce && IsCompleted(challenge.ChallengeId)) return;
        if (!OUTL_Rules.CheckAll(challenge.Conditions, evt.Source, evt.Target, world)) return;
        int count = GetProgress(challenge.ChallengeId) + 1;
        SetProgress(challenge.ChallengeId, count);
        OUTL_EntityId ownerId = Owner != null ? Owner.Id : evt.Source;
        world.Effects.ApplyAll(challenge.OnProgressEffects, evt.Source, ownerId, evt.Point);
        world.Events.Emit(new OUTL_Event(OUTL_EventType.Custom, evt.Source, ownerId) { Key = OUTL_LoopKeys.ChallengeProgress, IntValue = count, Point = evt.Point });
        if (count < Mathf.Max(1, challenge.TargetCount)) return;
        MarkCompleted(challenge.ChallengeId);
        world.Effects.ApplyAll(challenge.OnCompleteEffects, evt.Source, ownerId, evt.Point);
        if (challenge.Rewards != null)
            for (int i = 0; i < challenge.Rewards.Length; i++) OUTL_RewardUtility.Grant(challenge.Rewards[i], evt.Source, ownerId, evt.Point);
        world.Events.Emit(new OUTL_Event(OUTL_EventType.Custom, evt.Source, ownerId) { Key = OUTL_LoopKeys.ChallengeCompleted, IntValue = count, Point = evt.Point });
    }

    private void EndLoop(bool win)
    {
        OUTL_World world = OUTL_World.Instance;
        OUTL_EntityId ownerId = Owner != null ? Owner.Id : OUTL_EntityId.None;
        if (Owner != null && Owner.Runtime != null) Owner.Runtime.State.SetString(OUTL_LoopKeys.SessionState, win ? "Won" : "Failed");
        if (world != null && GameLoop != null)
        {
            world.Effects.ApplyAll(win ? GameLoop.OnSessionWin : GameLoop.OnSessionFail, ownerId, ownerId, transform.position);
            world.Events.Emit(new OUTL_Event(OUTL_EventType.Custom, ownerId, ownerId) { Key = win ? OUTL_LoopKeys.SessionWon : OUTL_LoopKeys.SessionFailed, Point = transform.position });
        }
        started = false;
    }

    private void ResetProgress()
    {
        ids.Clear();
        counts.Clear();
        completed.Clear();
        for (int i = 0; i < active.Count; i++) if (active[i] != null) SetProgress(active[i].ChallengeId, 0);
    }

    private void SetProgress(string id, int count)
    {
        int index = ids.IndexOf(id);
        if (index < 0) { ids.Add(id); counts.Add(count); }
        else counts[index] = count;
    }

    private void MarkCompleted(string id)
    {
        if (!completed.Contains(id)) completed.Add(id);
    }

    private static bool TagsMatch(OUTL_World world, OUTL_EntityId entityId, string[] requiredTags)
    {
        if (requiredTags == null || requiredTags.Length == 0) return true;
        if (world == null || !entityId.IsValid) return false;
        OUTL_EntityRuntime runtime;
        if (!world.Registry.TryGet(entityId, out runtime) || runtime == null) return false;
        for (int i = 0; i < requiredTags.Length; i++)
        {
            string tag = requiredTags[i];
            if (!string.IsNullOrEmpty(tag) && !runtime.HasTag(tag)) return false;
        }
        return true;
    }

    private void RegisterEvents()
    {
        if (registered || !AutoRegisterEvents || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Events.Register(this);
        registered = true;
    }
}
