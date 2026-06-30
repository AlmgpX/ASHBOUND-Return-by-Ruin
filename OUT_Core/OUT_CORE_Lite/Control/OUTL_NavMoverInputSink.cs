using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_NavMoverInputSink : MonoBehaviour, OUTL_IActorInputPhasedSink, OUTL_IActorInputClearableSink
{
    public OUTL_EntityAdapter Entity;
    public OUTL_NavMeshMover NavMover;
    public CharacterController CharacterController;
    public Transform MoveRoot;
    public float InputStepDistance = 2.5f;
    public float CharacterControllerSpeed = 3.5f;
    public bool FaceAimYaw = true;
    public bool SuppressNavAgentRotationWhenFacingAim = true;
    public float NavAgentRotationSuppressionSeconds = 0.08f;
    public string MovementAuthority = "actor_input";
    public bool StopOnlyOwnedDestination = true;
    public bool StopOwnedDestinationOnZeroInput = true;
    public float ZeroInputStopDelay = 0.04f;
    public float MinDestinationChangeDistance = 0.35f;
    public float MinDestinationRefreshInterval = 0.12f;
    [Tooltip("While actor input owns the mover, use a tight stop distance for incremental input destinations so the NavMeshAgent does not brake every few steps.")]
    public bool OverrideOwnedStopDistance = true;
    public float OwnedStopDistance = 0.12f;
    public string LastInputDebug;

    public OUTL_ActorInputPhase Phase { get { return OUTL_ActorInputPhase.Movement; } }

    private bool hasInputDestination;
    private Vector3 lastInputDestination;
    private float lastDestinationRequestTime = -999f;
    private float lastNonZeroInputTime = -999f;
    private bool capturedStopDistance;
    private float capturedStopDistanceValue;

    private void Awake()
    {
        Resolve();
    }

    private void OnDisable()
    {
        RestoreOwnedStopDistanceIfCaptured();
    }

    public void OUTL_ApplyInput(in OUTL_ActorInputFrame frame, OUTL_World world)
    {
        Resolve();
        Transform root = MoveRoot != null ? MoveRoot : transform;
        float dt = ReadDeltaTime(frame, world);
        float time = world != null ? world.WorldTime : Time.time;
        if (IsDead())
        {
            ClearOwnedInputDestination("dead");
            return;
        }

        if (FaceAimYaw && frame.HasAimWorldPoint && !frame.HasDesiredView)
        {
            Vector3 toAim = frame.AimWorldPoint - root.position;
            toAim.y = 0f;
            if (toAim.sqrMagnitude > 0.001f)
            {
                if (SuppressNavAgentRotationWhenFacingAim && NavMover != null)
                    NavMover.SuppressAgentRotation(NavAgentRotationSuppressionSeconds);
                root.rotation = Quaternion.RotateTowards(root.rotation, Quaternion.LookRotation(toAim.normalized), 720f * dt);
            }
        }

        Vector3 move = BuildWorldMove(root, frame.Move);
        if (move.sqrMagnitude <= 0.0001f)
        {
            StopOwnedInputDestinationIfNeeded(time);
            return;
        }

        if (NavMover != null)
        {
            lastNonZeroInputTime = time;
            CaptureAndSetOwnedStopDistance();
            Vector3 destination = root.position + move.normalized * Mathf.Max(0.1f, InputStepDistance);
            if (ShouldRefreshInputDestination(destination, time))
            {
                NavMover.SetDestination(destination, MovementAuthority);
                hasInputDestination = true;
                lastInputDestination = destination;
                lastDestinationRequestTime = time;
                LastInputDebug = "destination " + destination.ToString("F2");
            }
            else
            {
                LastInputDebug = "hold destination";
            }
            return;
        }

        if (CharacterController != null && CharacterController.enabled)
        {
            CharacterController.Move(move.normalized * CharacterControllerSpeed * dt);
        }
    }

    public void OUTL_ClearInput(OUTL_World world, float time)
    {
        Resolve();
        ClearOwnedInputDestination("clear_input");
    }

    private void StopOwnedInputDestinationIfNeeded(float time)
    {
        if (NavMover == null || !StopOwnedDestinationOnZeroInput)
        {
            LastInputDebug = "zero input";
            return;
        }

        if (!hasInputDestination)
        {
            LastInputDebug = "zero input no owned destination";
            return;
        }

        if (StopOnlyOwnedDestination && NavMover.CurrentMovementAuthority != MovementAuthority)
        {
            hasInputDestination = false;
            RestoreOwnedStopDistanceIfCaptured();
            LastInputDebug = "released destination; mover owned by " + NavMover.CurrentMovementAuthority;
            return;
        }

        if (time - lastNonZeroInputTime < Mathf.Max(0f, ZeroInputStopDelay))
        {
            LastInputDebug = "zero input grace";
            return;
        }

        ClearOwnedInputDestination("zero input");
    }

    private void ClearOwnedInputDestination(string reason)
    {
        if (NavMover == null)
        {
            hasInputDestination = false;
            RestoreOwnedStopDistanceIfCaptured();
            LastInputDebug = reason;
            return;
        }

        bool ownsMover = NavMover.CurrentMovementAuthority == MovementAuthority;
        if (hasInputDestination || ownsMover)
        {
            if (!StopOnlyOwnedDestination || ownsMover)
                NavMover.Stop(MovementAuthority);
            hasInputDestination = false;
        }

        RestoreOwnedStopDistanceIfCaptured();
        LastInputDebug = reason;
    }

    private void CaptureAndSetOwnedStopDistance()
    {
        if (!OverrideOwnedStopDistance || NavMover == null) return;
        if (!capturedStopDistance)
        {
            capturedStopDistanceValue = NavMover.StopDistance;
            capturedStopDistance = true;
        }
        NavMover.StopDistance = Mathf.Max(0.01f, OwnedStopDistance);
    }

    private void RestoreOwnedStopDistanceIfCaptured()
    {
        if (!capturedStopDistance) return;
        if (NavMover != null) NavMover.StopDistance = capturedStopDistanceValue;
        capturedStopDistance = false;
    }

    private bool ShouldRefreshInputDestination(Vector3 destination, float time)
    {
        if (!hasInputDestination) return true;
        float minDistance = Mathf.Max(0f, MinDestinationChangeDistance);
        if ((destination - lastInputDestination).sqrMagnitude >= minDistance * minDistance) return true;
        return time - lastDestinationRequestTime >= Mathf.Max(0.01f, MinDestinationRefreshInterval);
    }

    private static Vector3 BuildWorldMove(Transform root, Vector2 input)
    {
        Vector3 move = root.right * input.x + root.forward * input.y;
        move.y = 0f;
        if (move.sqrMagnitude > 1f) move.Normalize();
        return move;
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
        if (CharacterController == null) CharacterController = GetComponent<CharacterController>();
        if (MoveRoot == null) MoveRoot = transform;
    }
}
