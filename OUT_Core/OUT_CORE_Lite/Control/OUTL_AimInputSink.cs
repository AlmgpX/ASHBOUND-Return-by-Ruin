using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_AimInputSink : MonoBehaviour, OUTL_IActorInputPhasedSink
{
    public OUTL_EntityAdapter Entity;
    public OUTL_NavMeshMover NavMover;
    public Transform AimRoot;
    public float AngularSpeed = 240f;
    public bool SuppressNavAgentRotation = true;
    public float NavAgentRotationSuppressionSeconds = 0.08f;
    public bool UseFrameMaxAngleForLock = true;
    public string LastAimReason = "";

    public OUTL_ActorInputPhase Phase { get { return OUTL_ActorInputPhase.Aim; } }

    private void Awake()
    {
        Resolve();
    }

    public void OUTL_ApplyInput(in OUTL_ActorInputFrame frame, OUTL_World world)
    {
        Resolve();
        if (IsDead()) return;
        if (!frame.HasDesiredView && !frame.HasAimWorldPoint) return;

        Transform root = AimRoot != null ? AimRoot : transform;
        float dt = ReadDeltaTime(frame, world);
        if (dt <= 0f) return;

        float desiredYaw = frame.HasDesiredView ? frame.DesiredYaw : BuildYaw(root, frame.AimWorldPoint);
        Quaternion desired = Quaternion.Euler(0f, desiredYaw, 0f);
        float speed = Mathf.Max(1f, AngularSpeed);
        if (SuppressNavAgentRotation && NavMover != null)
            NavMover.SuppressAgentRotation(NavAgentRotationSuppressionSeconds);
        root.rotation = Quaternion.RotateTowards(root.rotation, desired, speed * dt);

        float yawDelta = Quaternion.Angle(root.rotation, desired);
        float allowed = frame.MaxAllowedFireAngle > 0f ? frame.MaxAllowedFireAngle : 3f;
        LastAimReason = yawDelta <= allowed ? "locked" : "turning";
    }

    private static float BuildYaw(Transform root, Vector3 aimPoint)
    {
        Vector3 toAim = aimPoint - root.position;
        toAim.y = 0f;
        if (toAim.sqrMagnitude <= 0.0001f) return root.eulerAngles.y;
        return Quaternion.LookRotation(toAim.normalized).eulerAngles.y;
    }

    private bool IsDead()
    {
        OUTL_EntityRuntime runtime = Entity != null ? Entity.Runtime : null;
        return runtime != null && (runtime.Dead || runtime.LifeState == OUTL_LifeState.Dead || runtime.State.GetFlag(OUTL_StateId.Dead) || runtime.Stats.Get(OUTL_StatId.Health, 1f) <= 0f);
    }

    private static float ReadDeltaTime(in OUTL_ActorInputFrame frame, OUTL_World world)
    {
        if (frame.DeltaTime > 0f) return frame.DeltaTime;
        return Mathf.Max(0f, world != null ? world.DeltaTime : Time.deltaTime);
    }

    private void Resolve()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (NavMover == null) NavMover = GetComponent<OUTL_NavMeshMover>();
        if (AimRoot == null) AimRoot = transform;
    }
}
