using System.Collections.Generic;
using UnityEngine;

public enum OUTL_PathMoverMode : byte
{
    Once = 0,
    Loop = 1,
    PingPong = 2
}

public enum OUTL_PathMoverState : byte
{
    Idle = 0,
    Moving = 1,
    Waiting = 2,
    Paused = 3
}

[DisallowMultipleComponent]
public sealed class OUTL_PathMover : MonoBehaviour, OUTL_ITickable, OUTL_ICommandReceiver, OUTL_IComponentSaveParticipant
{
    [Header("OUTL")]
    public OUTL_EntityAdapter Entity;
    public bool AutoRegister = true;
    public OUTL_TickLane TickLane = OUTL_TickLane.Logic;
    [Min(0.001f)] public float TickInterval = 0.01f;

    [Header("Path")]
    public Transform MoverRoot;
    public OUTL_PathNode FirstNode;
    public OUTL_PathNode CurrentNode;
    public OUTL_PathMoverMode Mode = OUTL_PathMoverMode.Loop;
    public bool StartOnEnable;
    public bool TeleportToFirstNodeOnStart = true;
    public bool UseNodeRotation;
    public bool RotateToPathDirection = true;
    public bool ReverseDirection;

    [Header("Motion")]
    [Min(0.001f)] public float Speed = 3f;
    public float RotationSpeed = 360f;
    public float DefaultWait = 0f;
    public bool UseWorldTime = true;
    public bool PauseOnNodeUntilCommand;

    [Header("Blocking / Damage")]
    public bool CheckBlockers = true;
    public LayerMask BlockMask = ~0;
    public Vector3 BlockPadding = new Vector3(0.02f, 0.02f, 0.02f);
    public QueryTriggerInteraction BlockTriggerInteraction = QueryTriggerInteraction.Ignore;
    public float CrushDamage = 2f;
    public string CrushDamageKey = "crush.platform";
    public float CrushDamageInterval = 0.5f;
    public bool StopOnBlock;
    public bool ReverseOnBlock;

    [Header("Audio")]
    public AudioSource AudioSource;
    public AudioClip MovingLoop;
    public AudioClip StartSound;
    public AudioClip StopSound;
    [Range(0f, 1f)] public float Volume = 0.85f;

    [Header("State")]
    public OUTL_PathMoverState State = OUTL_PathMoverState.Idle;
    public bool MovingForward = true;

    [Header("Debug")]
    public bool DebugLog;

    private bool registered;
    private OUTL_PathNode targetNode;
    private float waitUntil;
    private float lastCrushTime = -999f;
    private readonly Collider[] blockBuffer = new Collider[32];

    public string OUTL_SaveKey { get { return "OUTL_PathMover"; } }
    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled && MoverRoot != null; } }
    public OUTL_TickLane OUTL_TickLane { get { return TickLane; } }
    public float OUTL_TickInterval { get { return Mathf.Max(0.001f, TickInterval); } }
    public bool IsMoving { get { return State == OUTL_PathMoverState.Moving; } }

    private void Awake()
    {
        ResolveReferences();
        if (CurrentNode == null) CurrentNode = FirstNode;
        if (TeleportToFirstNodeOnStart && CurrentNode != null)
            SnapToNode(CurrentNode);
        PushStateFlags();
    }

    private void OnEnable()
    {
        if (AutoRegister) Register();
        if (StartOnEnable) StartMove();
    }

    private void OnDisable()
    {
        StopMovingSound();
        Unregister();
    }

    private void OnDestroy()
    {
        Unregister();
    }

    [ContextMenu("OUT Register")]
    public void Register()
    {
        if (registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Register(this);
        registered = true;
    }

    [ContextMenu("OUT Unregister")]
    public void Unregister()
    {
        if (!registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Unregister(this);
        registered = false;
    }

    [ContextMenu("OUT Start")]
    public void StartMove()
    {
        if (CurrentNode == null) CurrentNode = FirstNode;
        if (CurrentNode == null) return;
        State = OUTL_PathMoverState.Moving;
        targetNode = ResolveNextNode(CurrentNode);
        if (targetNode == null)
        {
            State = OUTL_PathMoverState.Idle;
            PushStateFlags();
            return;
        }
        PlayStartAndMovingSound();
        PushStateFlags();
    }

    [ContextMenu("OUT Stop")]
    public void StopMove()
    {
        State = OUTL_PathMoverState.Idle;
        targetNode = null;
        StopMovingSound();
        PlayStopSound();
        PushStateFlags();
    }

    [ContextMenu("OUT Pause")]
    public void PauseMove()
    {
        if (State != OUTL_PathMoverState.Moving) return;
        State = OUTL_PathMoverState.Paused;
        StopMovingSound();
        PushStateFlags();
    }

    [ContextMenu("OUT Resume")]
    public void ResumeMove()
    {
        if (State != OUTL_PathMoverState.Paused && State != OUTL_PathMoverState.Waiting) return;
        if (targetNode == null && CurrentNode != null) targetNode = ResolveNextNode(CurrentNode);
        if (targetNode == null) return;
        State = OUTL_PathMoverState.Moving;
        PlayStartAndMovingSound();
        PushStateFlags();
    }

    public bool OUTL_CanReceive(in OUTL_Command command, OUTL_World world)
    {
        return command.Type == OUTL_CommandType.Use || command.Type == OUTL_CommandType.Activate || command.Type == OUTL_CommandType.Open || command.Type == OUTL_CommandType.Deactivate || command.Type == OUTL_CommandType.Close || command.Type == OUTL_CommandType.SendSignal || command.Type == OUTL_CommandType.Custom;
    }

    public void OUTL_Receive(in OUTL_Command command, OUTL_World world)
    {
        if (command.Type == OUTL_CommandType.Deactivate || command.Type == OUTL_CommandType.Close)
        {
            PauseMove();
            return;
        }

        if (State == OUTL_PathMoverState.Moving) PauseMove();
        else if (State == OUTL_PathMoverState.Paused || State == OUTL_PathMoverState.Waiting) ResumeMove();
        else StartMove();
    }

    public void OUTL_Capture(OUTL_ComponentSaveWriter writer)
    {
        if (writer == null) return;
        writer.SetInt("state", (int)State);
        writer.SetInt("forward", MovingForward ? 1 : 0);
        writer.SetString("currentNode", GetNodeId(CurrentNode));
        writer.SetString("targetNode", GetNodeId(targetNode));
        float remaining = waitUntil == float.PositiveInfinity ? -1f : Mathf.Max(0f, waitUntil - ReadWorldTime());
        writer.SetFloat("waitRemaining", remaining);
        if (MoverRoot != null)
        {
            Vector3 p = MoverRoot.position;
            Quaternion r = MoverRoot.rotation;
            writer.SetFloat("px", p.x);
            writer.SetFloat("py", p.y);
            writer.SetFloat("pz", p.z);
            writer.SetFloat("rx", r.x);
            writer.SetFloat("ry", r.y);
            writer.SetFloat("rz", r.z);
            writer.SetFloat("rw", r.w);
        }
    }

    public void OUTL_Restore(OUTL_ComponentSaveReader reader)
    {
        if (reader == null) return;
        ResolveReferences();
        State = (OUTL_PathMoverState)Mathf.Clamp(reader.GetInt("state", (int)OUTL_PathMoverState.Idle), 0, (int)OUTL_PathMoverState.Paused);
        MovingForward = reader.GetInt("forward", 1) != 0;
        CurrentNode = FindNode(reader.GetString("currentNode", GetNodeId(FirstNode))) ?? FirstNode;
        targetNode = FindNode(reader.GetString("targetNode", string.Empty));
        float remaining = reader.GetFloat("waitRemaining", 0f);
        waitUntil = remaining < 0f ? float.PositiveInfinity : ReadWorldTime() + remaining;
        if (MoverRoot != null)
        {
            Vector3 p = MoverRoot.position;
            Quaternion r = MoverRoot.rotation;
            MoverRoot.position = new Vector3(
                reader.GetFloat("px", p.x),
                reader.GetFloat("py", p.y),
                reader.GetFloat("pz", p.z));
            MoverRoot.rotation = new Quaternion(
                reader.GetFloat("rx", r.x),
                reader.GetFloat("ry", r.y),
                reader.GetFloat("rz", r.z),
                reader.GetFloat("rw", r.w));
        }
        if (State == OUTL_PathMoverState.Moving && targetNode == null && CurrentNode != null)
            targetNode = ResolveNextNode(CurrentNode);
        if (State != OUTL_PathMoverState.Moving) StopMovingSound();
        PushStateFlags();
    }

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        if (MoverRoot == null) return;
        if (deltaTime <= 0f) return;

        if (State == OUTL_PathMoverState.Waiting)
        {
            if (PauseOnNodeUntilCommand) return;
            if (time < waitUntil) return;
            ResumeMove();
            return;
        }

        if (State != OUTL_PathMoverState.Moving) return;
        if (targetNode == null)
        {
            targetNode = ResolveNextNode(CurrentNode);
            if (targetNode == null) { StopMove(); return; }
        }

        if (CheckBlockers && IsBlocked(time))
            return;

        float speed = targetNode.OverrideSpeed ? Mathf.Max(0.001f, targetNode.Speed) : Mathf.Max(0.001f, Speed);
        Vector3 target = targetNode.Position;
        Vector3 next = Vector3.MoveTowards(MoverRoot.position, target, speed * deltaTime);
        Vector3 delta = next - MoverRoot.position;
        MoverRoot.position = next;

        if (targetNode.UseNodeRotation || UseNodeRotation)
            MoverRoot.rotation = Quaternion.RotateTowards(MoverRoot.rotation, targetNode.Rotation, RotationSpeed * deltaTime);
        else if (RotateToPathDirection && delta.sqrMagnitude > 0.000001f)
            MoverRoot.rotation = Quaternion.RotateTowards(MoverRoot.rotation, Quaternion.LookRotation(delta.normalized, Vector3.up), RotationSpeed * deltaTime);

        if ((MoverRoot.position - target).sqrMagnitude <= 0.000001f)
            ArriveAtNode(world, time, targetNode);
    }

    private void ArriveAtNode(OUTL_World world, float time, OUTL_PathNode node)
    {
        CurrentNode = node;
        node.FireArrival(Entity != null ? Entity.Id : OUTL_EntityId.None);

        float wait = node.OverrideWait ? node.Wait : DefaultWait;
        bool pause = node.PauseUntilTriggered || PauseOnNodeUntilCommand;
        targetNode = ResolveNextNode(CurrentNode);

        if (targetNode == null)
        {
            StopMove();
            return;
        }

        if (pause || wait > 0f)
        {
            State = OUTL_PathMoverState.Waiting;
            waitUntil = pause ? float.PositiveInfinity : time + wait;
            StopMovingSound();
            PlayStopSound();
            PushStateFlags();
            return;
        }
    }

    private OUTL_PathNode ResolveNextNode(OUTL_PathNode node)
    {
        if (node == null) return null;
        OUTL_PathNode next = MovingForward ? node.Next : node.Previous;
        if (next != null) return next;

        if (Mode == OUTL_PathMoverMode.Loop)
            return MovingForward ? FirstNode : FindLastNode();

        if (Mode == OUTL_PathMoverMode.PingPong)
        {
            MovingForward = !MovingForward;
            return MovingForward ? node.Next : node.Previous;
        }

        return null;
    }

    private OUTL_PathNode FindLastNode()
    {
        OUTL_PathNode n = FirstNode;
        if (n == null) return null;
        int guard = 0;
        while (n.Next != null && guard++ < 4096)
            n = n.Next;
        return n;
    }

    private OUTL_PathNode FindNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId) || FirstNode == null) return null;
        Queue<OUTL_PathNode> pending = new Queue<OUTL_PathNode>();
        HashSet<OUTL_PathNode> visited = new HashSet<OUTL_PathNode>();
        pending.Enqueue(FirstNode);
        while (pending.Count > 0)
        {
            OUTL_PathNode node = pending.Dequeue();
            if (node == null || !visited.Add(node)) continue;
            if (GetNodeId(node) == nodeId) return node;
            if (node.Next != null) pending.Enqueue(node.Next);
            if (node.Previous != null) pending.Enqueue(node.Previous);
        }
        return null;
    }

    private static string GetNodeId(OUTL_PathNode node)
    {
        return node != null ? node.GetStableNodeId() : string.Empty;
    }

    private bool IsBlocked(float time)
    {
        Collider own = MoverRoot.GetComponentInChildren<Collider>();
        if (own == null) return false;
        Bounds b = own.bounds;
        int count = Physics.OverlapBoxNonAlloc(b.center, b.extents + BlockPadding, blockBuffer, own.transform.rotation, BlockMask, BlockTriggerInteraction);
        Collider blocker = null;
        for (int i = 0; i < count; i++)
        {
            Collider c = blockBuffer[i];
            blockBuffer[i] = null;
            if (c == null || c == own || c.transform.IsChildOf(MoverRoot) || c.transform.IsChildOf(transform)) continue;
            blocker = c;
            break;
        }
        if (blocker == null) return false;

        if (CrushDamage > 0f && time >= lastCrushTime + CrushDamageInterval)
        {
            lastCrushTime = time;
            OUTL_EntityAdapter target;
            if (OUTL_Combat.TryGetEntityFromCollider(blocker, out target))
                OUTL_Combat.ApplyDamage(Entity != null ? Entity.Id : OUTL_EntityId.None, target.Id, CrushDamage, blocker.bounds.center, CrushDamageKey);
        }

        if (ReverseOnBlock)
        {
            MovingForward = !MovingForward;
            targetNode = ResolveNextNode(CurrentNode);
        }

        if (StopOnBlock) PauseMove();
        return StopOnBlock;
    }

    public void SnapToNode(OUTL_PathNode node)
    {
        if (node == null || MoverRoot == null) return;
        MoverRoot.position = node.Position;
        if (node.UseNodeRotation || UseNodeRotation) MoverRoot.rotation = node.Rotation;
    }

    private void PushStateFlags()
    {
        if (Entity == null || Entity.Runtime == null) return;
        Entity.Runtime.State.SetFlag("Moving", State == OUTL_PathMoverState.Moving);
        Entity.Runtime.State.SetFlag("On", State == OUTL_PathMoverState.Moving || State == OUTL_PathMoverState.Waiting);
        Entity.Runtime.State.SetFloat("PathMoverState", (float)State);
    }

    private void ResolveReferences()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (MoverRoot == null) MoverRoot = transform;
        if (AudioSource == null) AudioSource = GetComponent<AudioSource>();
    }

    private void PlayStartAndMovingSound()
    {
        PlayOneShot(StartSound);
        if (AudioSource != null && MovingLoop != null)
        {
            AudioSource.volume = Volume;
            AudioSource.clip = MovingLoop;
            AudioSource.loop = true;
            if (!AudioSource.isPlaying) AudioSource.Play();
        }
    }

    private void StopMovingSound()
    {
        if (AudioSource != null && AudioSource.isPlaying && AudioSource.clip == MovingLoop) AudioSource.Stop();
    }

    private void PlayStopSound()
    {
        PlayOneShot(StopSound);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null) return;
        if (AudioSource != null) AudioSource.PlayOneShot(clip, Volume);
        else OUTL_PoolSystem.PlayClipShared(clip, MoverRoot != null ? MoverRoot.position : transform.position, Volume);
    }

    private static float ReadWorldTime()
    {
        OUTL_World world = OUTL_World.Instance;
        return world != null ? world.WorldTime : Time.time;
    }
}
