using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(-8600)]
[DisallowMultipleComponent]
[AddComponentMenu("OUT CORE Lite/Logic/Outpost Clearance Controller")]
public sealed class OUTL_OutpostClearanceController : MonoBehaviour, OUTL_ITickable, OUTL_IEventListener, OUTL_IComponentSaveParticipant
{
    public OUTL_EntityAdapter Entity;
    public string OutpostId = "outpost";
    [Min(1f)] public float Radius = 150f;
    [Min(0.1f)] public float CheckInterval = 0.5f;
    public bool IncludeUnassignedMarkersInsideRadius = true;
    public bool PreventEmptyCompletion = true;

    [Header("State")]
    public int InitialTargetCount;
    public int RemainingTargetCount;
    public int DestroyedTargetCount;
    public bool IsCleared;

    [Header("Presentation")]
    public GameObject HostileIndicator;
    public GameObject ClearedIndicator;
    public GameObject SiegeModeObject;
    public AudioClip VictorySound;
    public UnityEvent OnCleared;
    public OUTL_OutputLink[] Outputs;

    private readonly List<TargetRef> targets = new List<TargetRef>(64);
    private readonly HashSet<string> collectedIds = new HashSet<string>();
    private readonly HashSet<string> destroyed = new HashSet<string>();
    private bool registered;
    private Transform player;

    public string OUTL_SaveKey { get { return "OUTL_OutpostClearanceController"; } }
    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled && !IsCleared; } }
    public OUTL_TickLane OUTL_TickLane { get { return OUTL_TickLane.Logic; } }
    public float OUTL_TickInterval { get { return Mathf.Max(0.1f, CheckInterval); } }

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
    }

    private void Start()
    {
        CollectTargets();
        Register();
        Evaluate();
    }

    private void OnDisable()
    {
        OUTL_World world = OUTL_World.Instance;
        if (world != null)
        {
            world.Events.Unregister(this);
            world.Scheduler.Unregister(this);
        }
        registered = false;
    }

    [ContextMenu("OUTL Collect Outpost Targets")]
    public void CollectTargets()
    {
        targets.Clear();
        collectedIds.Clear();
        destroyed.Clear();
        float radiusSqr = Radius * Radius;

        OUTL_AbstractSpawnMarker[] markers = FindObjectsOfType<OUTL_AbstractSpawnMarker>(true);
        for (int i = 0; i < markers.Length; i++)
        {
            OUTL_AbstractSpawnMarker marker = markers[i];
            if (marker == null || !marker.CountsForOutpostClearance || string.IsNullOrEmpty(marker.StableId)) continue;
            bool explicitMember = !string.IsNullOrEmpty(OutpostId) && marker.OutpostId == OutpostId;
            bool radialMember = IncludeUnassignedMarkersInsideRadius && string.IsNullOrEmpty(marker.OutpostId) &&
                                (marker.transform.position - transform.position).sqrMagnitude <= radiusSqr;
            if ((explicitMember || radialMember) && collectedIds.Add(marker.StableId))
                targets.Add(new TargetRef { StableId = marker.StableId, Marker = marker });
        }

        OUTL_OutpostClearanceTarget[] sceneTargets = FindObjectsOfType<OUTL_OutpostClearanceTarget>(true);
        for (int i = 0; i < sceneTargets.Length; i++)
        {
            OUTL_OutpostClearanceTarget target = sceneTargets[i];
            if (target == null || !target.CountsForClearance || string.IsNullOrEmpty(target.StableId)) continue;
            bool explicitMember = !string.IsNullOrEmpty(OutpostId) && target.OutpostId == OutpostId;
            bool radialMember = IncludeUnassignedMarkersInsideRadius && string.IsNullOrEmpty(target.OutpostId) &&
                                (target.transform.position - transform.position).sqrMagnitude <= radiusSqr;
            if ((explicitMember || radialMember) && collectedIds.Add(target.StableId))
                targets.Add(new TargetRef { StableId = target.StableId, SceneTarget = target });
        }

        InitialTargetCount = targets.Count;
        RemainingTargetCount = InitialTargetCount;
        DestroyedTargetCount = 0;
    }

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        Evaluate();
        UpdateSiegeMode(world);
    }

    public void OUTL_OnEvent(in OUTL_Event evt, OUTL_World world)
    {
        if (IsCleared || world == null) return;

        if (evt.Type == OUTL_EventType.Killed)
        {
            OUTL_EntityRuntime runtime;
            if (world.Registry.TryGet(evt.Target, out runtime) && runtime != null && !string.IsNullOrEmpty(runtime.StableId))
                destroyed.Add(runtime.StableId);
        }
        else if (evt.Type == OUTL_EventType.Custom && !string.IsNullOrEmpty(evt.Key))
        {
            destroyed.Add(evt.Key);
        }
    }

    public void Evaluate()
    {
        if (IsCleared) return;
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return;

        int remaining = 0;
        for (int i = 0; i < targets.Count; i++)
            if (IsTargetAlive(world, targets[i])) remaining++;

        RemainingTargetCount = remaining;
        DestroyedTargetCount = Mathf.Max(0, InitialTargetCount - remaining);
        if (remaining == 0 && (!PreventEmptyCompletion || InitialTargetCount > 0))
            Complete(world);
    }

    public void OUTL_Capture(OUTL_ComponentSaveWriter writer)
    {
        writer.SetFlag("cleared", IsCleared);
        writer.SetInt("initial", InitialTargetCount);
        writer.SetInt("remaining", RemainingTargetCount);
    }

    public void OUTL_Restore(OUTL_ComponentSaveReader reader)
    {
        InitialTargetCount = reader.GetInt("initial", InitialTargetCount);
        RemainingTargetCount = reader.GetInt("remaining", RemainingTargetCount);
        IsCleared = reader.GetFlag("cleared", false);
        ApplyPresentation();
    }

    private bool IsTargetAlive(OUTL_World world, TargetRef target)
    {
        if (target == null || string.IsNullOrEmpty(target.StableId) || destroyed.Contains(target.StableId)) return false;

        OUTL_EntitySaveRecord record;
        if (world.Materialization.TryGetAbstractRecord(target.StableId, out record))
            return record != null && !record.Dead && record.LifeState != OUTL_LifeState.Dead && record.LifeState != OUTL_LifeState.DormantDead;

        OUTL_EntityRuntime runtime = world.Registry.FindByStableId(target.StableId);
        if (runtime != null)
            return !runtime.Dead && runtime.LifeState != OUTL_LifeState.Dead && runtime.LifeState != OUTL_LifeState.DormantDead;

        if (target.SceneTarget != null)
            return !target.SceneTarget.IsDestroyed;

        return false;
    }

    private void Complete(OUTL_World world)
    {
        if (IsCleared) return;
        IsCleared = true;
        RemainingTargetCount = 0;
        DestroyedTargetCount = InitialTargetCount;
        ApplyPresentation();

        if (VictorySound != null) OUTL_PoolSystem.PlayClipShared(VictorySound, transform.position);
        if (OnCleared != null) OnCleared.Invoke();
        OUTL_EntityId source = Entity != null ? Entity.Id : OUTL_EntityId.None;
        OUTL_OutputDispatcher.Fire(world, source, this, transform.position, Outputs, "OnCleared", OutpostId, InitialTargetCount, true);
        world.Events.Emit(new OUTL_Event(OUTL_EventType.Custom, source, source)
        {
            Key = "outpost.cleared:" + OutpostId,
            IntValue = InitialTargetCount,
            Point = transform.position
        });
        world.Scheduler.Unregister(this);
    }

    private void ApplyPresentation()
    {
        if (HostileIndicator != null) HostileIndicator.SetActive(!IsCleared);
        if (ClearedIndicator != null) ClearedIndicator.SetActive(IsCleared);
        if (SiegeModeObject != null && IsCleared) SiegeModeObject.SetActive(false);
    }

    private void UpdateSiegeMode(OUTL_World world)
    {
        if (SiegeModeObject == null || IsCleared || world == null) return;
        if (player == null)
        {
            OUTL_EntityRuntime playerRuntime = world.Registry.FindFirstByTargetName("player");
            if (playerRuntime != null && playerRuntime.Adapter != null) player = playerRuntime.Adapter.transform;
        }
        if (player == null) return;
        bool inside = (player.position - transform.position).sqrMagnitude <= Radius * Radius;
        if (SiegeModeObject.activeSelf != inside) SiegeModeObject.SetActive(inside);
    }

    private void Register()
    {
        OUTL_World world = OUTL_World.Instance;
        if (registered || world == null) return;
        world.Events.Register(this, OUTL_EventType.Killed);
        world.Events.Register(this, OUTL_EventType.Custom);
        world.Scheduler.Register(this);
        registered = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsCleared ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, Radius);
    }

    [Serializable]
    private sealed class TargetRef
    {
        public string StableId;
        public OUTL_AbstractSpawnMarker Marker;
        public OUTL_OutpostClearanceTarget SceneTarget;
    }
}
