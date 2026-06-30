using System.Collections.Generic;
using UnityEngine;

public enum OUTL_ActorInputUpdateMode
{
    SchedulerOnly = 0,
    LocalPlayerOnly = 1,
    FullAndNearActors = 2,
    AllActors = 3
}

[DisallowMultipleComponent]
public sealed class OUTL_ActorControlBridge : MonoBehaviour, OUTL_ITickable, OUTL_IPoolReset
{
    public OUTL_EntityAdapter Entity;
    public Behaviour InputSourceBehaviour;
    public Behaviour[] InputSinkBehaviours;
    public bool AutoResolve = true;
    public bool AutoRegister = true;
    public OUTL_TickLane TickLane = OUTL_TickLane.Logic;
    public float TickInterval = 0.02f;
    public float NearTickInterval = 0.05f;
    public float MidTickInterval = 0.20f;
    public float FarTickInterval = 2f;
    public float DormantTickInterval = 10f;
    public OUTL_ActorInputUpdateMode LocalPlayerUpdateMode = OUTL_ActorInputUpdateMode.FullAndNearActors;
    public bool ApplyLocalPlayerEveryFrame = true;
    public bool ApplyNearActorsEveryFrame = true;
    public bool UseUnityUpdateForLocalInput = true;
    public bool DebugLocalInput;
    public float DebugInterval = 1f;
    public bool TraceBlockedInput;
    public OUTL_ActorInputBuffer Buffer = new OUTL_ActorInputBuffer();
    public int LastAppliedPhaseOrderHash;
    public int LastAppliedSinkCount;
    public int LocalUnityUpdateApplyCount;
    public int SchedulerApplyCount;

    private readonly List<OUTL_IActorInputSink> resolvedSinks = new List<OUTL_IActorInputSink>(4);
    private OUTL_IActorInputSource resolvedSource;
    private bool registered;
    private int lastAppliedUnityFrame = -1;
    private float lastDebugTime;

    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled && Entity != null && Entity.Runtime != null && !ShouldApplyInUnityUpdate(); } }
    public OUTL_TickLane OUTL_TickLane { get { return TickLane; } }
    public float OUTL_TickInterval
    {
        get
        {
            OUTL_RuntimeTier tier = Entity != null && Entity.Runtime != null ? Entity.Runtime.Tier : OUTL_RuntimeTier.Full;
            switch (tier)
            {
                case OUTL_RuntimeTier.Dormant: return Mathf.Max(0.05f, DormantTickInterval);
                case OUTL_RuntimeTier.Far: return Mathf.Max(0.05f, FarTickInterval);
                case OUTL_RuntimeTier.Mid: return Mathf.Max(0.02f, MidTickInterval);
                case OUTL_RuntimeTier.Near: return Mathf.Max(0.01f, NearTickInterval);
                case OUTL_RuntimeTier.Full:
                default: return Mathf.Max(0.005f, TickInterval);
            }
        }
    }

    private void Awake()
    {
        Resolve();
    }

    private void OnEnable()
    {
        Resolve();
        if (AutoRegister) Register();
    }

    private void OnDisable()
    {
        Unregister();
    }

    private void Update()
    {
        if (!ShouldApplyInUnityUpdate()) return;
        OUTL_World world = OUTL_World.Instance;
        float time = world != null ? world.WorldTime : Time.time;
        ApplyInput(world, time, Time.deltaTime, true);
    }

    public void Register()
    {
        if (registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Register(this);
        registered = true;
    }

    public void Unregister()
    {
        if (!registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Unregister(this);
        registered = false;
    }

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        if (ShouldApplyInUnityUpdate()) return;
        ApplyInput(world, time, deltaTime, false);
    }

    public bool TrySetInputSource(Behaviour source)
    {
        if (source != null && !(source is OUTL_IActorInputSource))
        {
            Debug.LogWarning("OUTL_ActorControlBridge input override must implement OUTL_IActorInputSource.", source);
            return false;
        }

        InputSourceBehaviour = source;
        Resolve();
        return source == null || resolvedSource != null;
    }

    private void ApplyInput(OUTL_World world, float time, float deltaTime, bool unityUpdate)
    {
        if (unityUpdate && lastAppliedUnityFrame == Time.frameCount) return;
        if (AutoResolve && (resolvedSource == null || resolvedSinks.Count == 0)) Resolve();
        if (world == null || Entity == null || Entity.Runtime == null || resolvedSource == null)
        {
            ClearInputState(world, time);
            return;
        }

        if (!CanApplyInput(world, time))
        {
            ClearInputState(world, time);
            return;
        }

        OUTL_ActorInputFrame frame = OUTL_ActorInputFrame.Empty(time);
        if (!resolvedSource.TryBuildInput(world, Entity, time, deltaTime, ref frame))
        {
            ClearInputState(world, time);
            return;
        }

        frame.Timestamp = time;
        frame.DeltaTime = Mathf.Max(0f, deltaTime);
        if (Buffer != null) Buffer.Push(frame);
        LastAppliedPhaseOrderHash = 0;
        LastAppliedSinkCount = 0;
        bool refreshedAimAfterMovement = false;
        for (int i = 0; i < resolvedSinks.Count; i++)
            if (resolvedSinks[i] != null)
            {
                OUTL_ActorInputPhase phase = ReadPhase(resolvedSinks[i]);
                if (!refreshedAimAfterMovement && (int)phase > (int)OUTL_ActorInputPhase.Movement)
                {
                    RefreshAimAfterImmediateLook(ref frame);
                    refreshedAimAfterMovement = true;
                }

                LastAppliedPhaseOrderHash = LastAppliedPhaseOrderHash * 31 + (int)phase;
                LastAppliedSinkCount++;
                resolvedSinks[i].OUTL_ApplyInput(frame, world);
            }

        if (unityUpdate)
        {
            lastAppliedUnityFrame = Time.frameCount;
            LocalUnityUpdateApplyCount++;
        }
        else
        {
            SchedulerApplyCount++;
        }

        if (DebugLocalInput) LogDebugOncePerSecond(unityUpdate, frame);
    }

    private void ClearInputState(OUTL_World world, float time)
    {
        if (Buffer != null) Buffer.Clear(time);
        for (int i = 0; i < resolvedSinks.Count; i++)
        {
            OUTL_IActorInputClearableSink clearable = resolvedSinks[i] as OUTL_IActorInputClearableSink;
            if (clearable != null) clearable.OUTL_ClearInput(world, time);
        }
    }

    public void OUTL_OnPoolSpawn()
    {
        Resolve();
        Register();
    }

    public void OUTL_OnPoolRelease()
    {
        Unregister();
        if (Buffer != null) Buffer.Clear(0f);
    }

    private bool CanApplyInput(OUTL_World world, float time)
    {
        OUTL_EntityRuntime runtime = Entity.Runtime;
        if (runtime.Dead || runtime.LifeState == OUTL_LifeState.Dead || runtime.State.GetFlag(OUTL_StateId.Dead) || runtime.Stats.Get(OUTL_StatId.Health, 1f) <= 0f)
        {
            if (TraceBlockedInput) OUTL_DebugLog.TraceAI(Entity.Id, "actor input blocked: dead");
            return false;
        }

        if (!OUTL_NetworkAuthority.CanAuthoritativeSimulate(Entity))
        {
            if (TraceBlockedInput) OUTL_NetworkAuthority.TraceBlocked("actor_input", Entity);
            return false;
        }

        return true;
    }

    private bool ShouldApplyInUnityUpdate()
    {
        if (!UseUnityUpdateForLocalInput) return false;
        if (!isActiveAndEnabled) return false;
        if (AutoResolve && resolvedSource == null) Resolve();
        bool localPlayer = IsLocalPlayerSource();
        OUTL_RuntimeTier tier = Entity != null && Entity.Runtime != null ? Entity.Runtime.Tier : OUTL_RuntimeTier.Full;
        bool fullOrNear = tier == OUTL_RuntimeTier.Full || tier == OUTL_RuntimeTier.Near;

        switch (LocalPlayerUpdateMode)
        {
            case OUTL_ActorInputUpdateMode.SchedulerOnly:
                return false;
            case OUTL_ActorInputUpdateMode.LocalPlayerOnly:
                return localPlayer && ApplyLocalPlayerEveryFrame;
            case OUTL_ActorInputUpdateMode.AllActors:
                return true;
            case OUTL_ActorInputUpdateMode.FullAndNearActors:
            default:
                if (localPlayer && ApplyLocalPlayerEveryFrame) return true;
                return ApplyNearActorsEveryFrame && fullOrNear;
        }
    }

    private bool IsLocalPlayerSource()
    {
        return resolvedSource is OUTL_PlayerInputSource || InputSourceBehaviour is OUTL_PlayerInputSource;
    }

    private void RefreshAimAfterImmediateLook(ref OUTL_ActorInputFrame frame)
    {
        OUTL_PlayerInputSource player = resolvedSource as OUTL_PlayerInputSource;
        if (player == null) player = InputSourceBehaviour as OUTL_PlayerInputSource;
        if (player != null) player.RefreshAimAfterImmediateLook(ref frame);
    }

    private void LogDebugOncePerSecond(bool unityUpdate, in OUTL_ActorInputFrame frame)
    {
        float now = Time.unscaledTime;
        if (now - lastDebugTime < Mathf.Max(0.1f, DebugInterval)) return;
        lastDebugTime = now;
        Debug.Log("[OUTL ActorBridge] source=" + (InputSourceBehaviour != null ? InputSourceBehaviour.GetType().Name : "null") + " mode=" + LocalPlayerUpdateMode + " unityApplied=" + LocalUnityUpdateApplyCount + " schedulerApplied=" + SchedulerApplyCount + " mouse=" + frame.Look.magnitude.ToString("0.###") + " cursor=" + Cursor.lockState + " captured=" + OUTL_DevConsole.IsInputCaptured + " last=" + (unityUpdate ? "Update" : "Scheduler"), this);
        LocalUnityUpdateApplyCount = 0;
        SchedulerApplyCount = 0;
    }

    private void Resolve()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        resolvedSinks.Clear();
        resolvedSource = null;

        if (InputSourceBehaviour != null)
            resolvedSource = InputSourceBehaviour as OUTL_IActorInputSource;

        if (resolvedSource == null && AutoResolve)
        {
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] == this) continue;
                OUTL_IActorInputSource source = behaviours[i] as OUTL_IActorInputSource;
                if (source != null)
                {
                    resolvedSource = source;
                    InputSourceBehaviour = behaviours[i];
                    break;
                }
            }
        }

        if (InputSinkBehaviours != null)
        {
            for (int i = 0; i < InputSinkBehaviours.Length; i++)
            {
                OUTL_IActorInputSink sink = InputSinkBehaviours[i] as OUTL_IActorInputSink;
                if (sink != null) AddSinkSorted(sink);
            }
        }

        if (AutoResolve)
        {
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                OUTL_IActorInputSink sink = behaviours[i] as OUTL_IActorInputSink;
                if (sink != null) AddSinkSorted(sink);
            }
        }
    }

    private void AddSinkSorted(OUTL_IActorInputSink sink)
    {
        if (sink == null || resolvedSinks.Contains(sink)) return;
        OUTL_ActorInputPhase phase = ReadPhase(sink);
        int index = resolvedSinks.Count;
        for (int i = 0; i < resolvedSinks.Count; i++)
        {
            if (phase < ReadPhase(resolvedSinks[i]))
            {
                index = i;
                break;
            }
        }
        resolvedSinks.Insert(index, sink);
    }

    private static OUTL_ActorInputPhase ReadPhase(OUTL_IActorInputSink sink)
    {
        OUTL_IActorInputPhasedSink phased = sink as OUTL_IActorInputPhasedSink;
        return phased != null ? phased.Phase : OUTL_ActorInputPhase.Interaction;
    }
}
