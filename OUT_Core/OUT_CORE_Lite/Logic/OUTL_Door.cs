using UnityEngine;

[DisallowMultipleComponent]
public class OUTL_Door : MonoBehaviour, OUTL_ICommandReceiver, OUTL_ITickable, OUTL_IComponentSaveParticipant
{
    public OUTL_EntityAdapter Entity;
    public Transform DoorRoot;
    public Vector3 ClosedLocalPosition;
    public Vector3 OpenLocalPosition = new Vector3(0f, 2.5f, 0f);
    public Vector3 ClosedLocalEuler;
    public Vector3 OpenLocalEuler;
    public float Speed = 3f;
    public bool StartsOpen;
    public bool IsOpen;

    [Header("OUTL Tick")]
    public bool AutoRegister = true;
    public OUTL_TickLane TickLane = OUTL_TickLane.Logic;
    [Min(0.001f)] public float TickInterval = 0.01f;

    [Header("Door Logic")]
    public bool ToggleMode;
    public bool AutoClose = true;
    public float WaitOpenSeconds = 3f;
    public bool UseWorldTime = true;
    public bool Moving;
    [Tooltip("Canonical shooter door mode: while the slab is moving, repeated USE/toggle commands are ignored until the move finishes.")]
    public bool IgnoreCommandsWhileMoving = true;
    [Tooltip("Clamp a single large scheduler gap so a recovering frame cannot teleport a moving door through actors.")]
    public float MaxMoveDeltaTime = 0.12f;

    [Header("Blocking / Crush")]
    public bool CheckBlockers = true;
    public LayerMask BlockMask = ~0;
    public Vector3 BlockPadding = new Vector3(0.02f, 0.02f, 0.02f);
    public QueryTriggerInteraction BlockTriggerInteraction = QueryTriggerInteraction.Ignore;
    public float CrushDamage = 2f;
    public string CrushDamageKey = "crush.door";
    public float CrushDamageInterval = 0.5f;
    public bool ReverseOnBlock = true;
    [Tooltip("When true, world/level geometry is ignored as a blocker; only characters, OUTL entities and rigidbodies can stop/crush.")]
    public bool OnlyBlockActorsAndPhysics = true;

    [Header("Audio")]
    public AudioSource AudioSource;
    public AudioClip OpenSound;
    public AudioClip CloseSound;
    public AudioClip MovingLoop;
    public AudioClip StopSound;
    [Range(0f, 1f)] public float Volume = 0.85f;

    private bool registered;
    private float waitUntil;
    private float lastMoveTime = -1f;
    private float lastCrushTime = -999f;
    private readonly Collider[] blockBuffer = new Collider[24];

    public string OUTL_SaveKey { get { return "OUTL_Door"; } }
    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled && DoorRoot != null; } }
    public OUTL_TickLane OUTL_TickLane { get { return TickLane; } }
    public float OUTL_TickInterval { get { return Mathf.Max(0.001f, TickInterval); } }

    private void Awake()
    {
        ResolveReferences();
        IsOpen = StartsOpen;
        ApplyImmediate();
        PushStateFlags();
    }

    private void OnEnable()
    {
        if (AutoRegister) Register();
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

    [ContextMenu("OUT Open")]
    public void Open()
    {
        SetOpen(true, true);
    }

    [ContextMenu("OUT Close")]
    public void Close()
    {
        SetOpen(false, true);
    }

    [ContextMenu("OUT Toggle")]
    public void Toggle()
    {
        SetOpen(!IsOpen, true);
    }

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        if (DoorRoot == null || deltaTime <= 0f) return;

        if (!Moving && IsOpen && AutoClose && !ToggleMode && WaitOpenSeconds >= 0f && time >= waitUntil)
            SetOpen(false, true);

        Vector3 targetPos = IsOpen ? OpenLocalPosition : ClosedLocalPosition;
        Quaternion targetRot = Quaternion.Euler(IsOpen ? OpenLocalEuler : ClosedLocalEuler);
        float moveDeltaTime = ReadMoveDeltaTime(time, deltaTime);
        float step = Mathf.Max(0.001f, Speed) * moveDeltaTime;
        Vector3 currentPos = DoorRoot.localPosition;
        Vector3 nextPos = Vector3.MoveTowards(currentPos, targetPos, step);
        Quaternion nextRot = Quaternion.RotateTowards(DoorRoot.localRotation, targetRot, step * 120f);
        Vector3 sweepDelta = ToWorldDelta(nextPos - currentPos);

        if (CheckBlockers && Moving && IsBlocked(time, sweepDelta))
            return;

        DoorRoot.localPosition = nextPos;
        DoorRoot.localRotation = nextRot;

        bool atPos = (DoorRoot.localPosition - targetPos).sqrMagnitude <= 0.000001f;
        bool atRot = Quaternion.Angle(DoorRoot.localRotation, targetRot) <= 0.01f;
        if (Moving && atPos && atRot)
            FinishMove(time);
    }

    public bool OUTL_CanReceive(in OUTL_Command command, OUTL_World world)
    {
        if (IgnoreCommandsWhileMoving && Moving)
            return false;
        return command.Type == OUTL_CommandType.Use || command.Type == OUTL_CommandType.Open || command.Type == OUTL_CommandType.Close || command.Type == OUTL_CommandType.Activate || command.Type == OUTL_CommandType.Deactivate || command.Type == OUTL_CommandType.SendSignal || command.Type == OUTL_CommandType.Custom;
    }

    public void OUTL_Receive(in OUTL_Command command, OUTL_World world)
    {
        if (IgnoreCommandsWhileMoving && Moving)
            return;

        if (command.Type == OUTL_CommandType.Close || command.Type == OUTL_CommandType.Deactivate)
            SetOpen(false, true);
        else if (command.Type == OUTL_CommandType.Open || command.Type == OUTL_CommandType.Activate)
            SetOpen(true, true);
        else
            Toggle();
    }

    public void OUTL_Capture(OUTL_ComponentSaveWriter writer)
    {
        if (writer == null) return;
        writer.SetInt("open", IsOpen ? 1 : 0);
        writer.SetInt("moving", Moving ? 1 : 0);
        float remaining = waitUntil == float.PositiveInfinity ? -1f : Mathf.Max(0f, waitUntil - ReadWorldTime());
        writer.SetFloat("waitRemaining", remaining);
        if (DoorRoot != null)
        {
            Vector3 p = DoorRoot.localPosition;
            Quaternion r = DoorRoot.localRotation;
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
        IsOpen = reader.GetInt("open", StartsOpen ? 1 : 0) != 0;
        Moving = reader.GetInt("moving", 0) != 0;
        float remaining = reader.GetFloat("waitRemaining", 0f);
        waitUntil = remaining < 0f ? float.PositiveInfinity : ReadWorldTime() + remaining;
        if (DoorRoot != null)
        {
            DoorRoot.localPosition = new Vector3(
                reader.GetFloat("px", IsOpen ? OpenLocalPosition.x : ClosedLocalPosition.x),
                reader.GetFloat("py", IsOpen ? OpenLocalPosition.y : ClosedLocalPosition.y),
                reader.GetFloat("pz", IsOpen ? OpenLocalPosition.z : ClosedLocalPosition.z));
            DoorRoot.localRotation = new Quaternion(
                reader.GetFloat("rx", 0f),
                reader.GetFloat("ry", 0f),
                reader.GetFloat("rz", 0f),
                reader.GetFloat("rw", 1f));
        }
        PushStateFlags();
        if (!Moving) StopMovingSound();
    }

    private void SetOpen(bool value, bool playSound)
    {
        if (IsOpen == value && !Moving) return;
        IsOpen = value;
        Moving = true;
        lastMoveTime = ReadWorldTime();
        waitUntil = ReadWorldTime() + Mathf.Max(0f, WaitOpenSeconds);
        PushStateFlags();
        if (playSound) PlayStartSound(value);
        PlayMovingSound();
    }

    private void FinishMove(float time)
    {
        Moving = false;
        lastMoveTime = -1f;
        StopMovingSound();
        PlayStopSound();
        waitUntil = time + Mathf.Max(0f, WaitOpenSeconds);
        PushStateFlags();
    }

    private bool IsBlocked(float time, Vector3 sweepDelta)
    {
        Collider own = DoorRoot.GetComponentInChildren<Collider>();
        if (own == null) return false;
        Bounds b = own.bounds;
        Vector3 absDelta = new Vector3(Mathf.Abs(sweepDelta.x), Mathf.Abs(sweepDelta.y), Mathf.Abs(sweepDelta.z));
        int count = Physics.OverlapBoxNonAlloc(b.center + sweepDelta * 0.5f, b.extents + absDelta * 0.5f + BlockPadding, blockBuffer, Quaternion.identity, BlockMask, BlockTriggerInteraction);
        Collider blocker = null;
        for (int i = 0; i < count; i++)
        {
            Collider c = blockBuffer[i];
            blockBuffer[i] = null;
            if (c == null || c == own || c.transform.IsChildOf(DoorRoot) || c.transform.IsChildOf(transform)) continue;
            if (OnlyBlockActorsAndPhysics && !IsRelevantBlocker(c)) continue;
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
            SetOpen(!IsOpen, true);

        return true;
    }

    private bool IsRelevantBlocker(Collider c)
    {
        if (c == null) return false;
        if (c.GetComponentInParent<CharacterController>() != null) return true;
        Rigidbody rb = c.attachedRigidbody != null ? c.attachedRigidbody : c.GetComponentInParent<Rigidbody>();
        if (rb != null) return true;
        OUTL_EntityAdapter adapter;
        return OUTL_Combat.TryGetEntityFromCollider(c, out adapter);
    }

    private void ApplyImmediate()
    {
        if (DoorRoot == null) return;
        DoorRoot.localPosition = IsOpen ? OpenLocalPosition : ClosedLocalPosition;
        DoorRoot.localRotation = Quaternion.Euler(IsOpen ? OpenLocalEuler : ClosedLocalEuler);
        Moving = false;
        lastMoveTime = -1f;
    }

    private void PushStateFlags()
    {
        if (Entity == null || Entity.Runtime == null) return;
        Entity.Runtime.State.SetFlag("Open", IsOpen);
        Entity.Runtime.State.SetFlag("On", IsOpen);
        Entity.Runtime.State.SetFlag("Moving", Moving);
    }

    private void ResolveReferences()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (DoorRoot == null) DoorRoot = transform;
        if (AudioSource == null) AudioSource = GetComponent<AudioSource>();
    }

    private void PlayStartSound(bool opening)
    {
        AudioClip clip = opening ? OpenSound : CloseSound;
        PlayOneShot(clip);
    }

    private void PlayMovingSound()
    {
        if (AudioSource == null || MovingLoop == null) return;
        AudioSource.clip = MovingLoop;
        AudioSource.volume = Volume;
        AudioSource.loop = true;
        if (!AudioSource.isPlaying) AudioSource.Play();
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
        else OUTL_PoolSystem.PlayClipShared(clip, transform.position, Volume);
    }

    private static float ReadWorldTime()
    {
        OUTL_World world = OUTL_World.Instance;
        return world != null ? world.WorldTime : Time.time;
    }

    private float ReadMoveDeltaTime(float time, float schedulerDeltaTime)
    {
        float dt;
        if (UseWorldTime)
        {
            if (lastMoveTime < 0f)
            {
                lastMoveTime = time;
                dt = schedulerDeltaTime;
            }
            else
            {
                dt = time - lastMoveTime;
                lastMoveTime = time;
                if (dt <= 0f) dt = schedulerDeltaTime;
            }
        }
        else
        {
            dt = schedulerDeltaTime > 0f ? schedulerDeltaTime : Time.deltaTime;
        }

        if (dt <= 0f) dt = Time.deltaTime;
        float max = Mathf.Max(0.001f, MaxMoveDeltaTime);
        return Mathf.Clamp(dt, 0.001f, max);
    }

    private Vector3 ToWorldDelta(Vector3 localDelta)
    {
        Transform parent = DoorRoot != null ? DoorRoot.parent : null;
        return parent != null ? parent.TransformVector(localDelta) : localDelta;
    }
}
