using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class OUTL_NavMeshMover : MonoBehaviour, OUTL_ITickable, OUTL_IPoolReset
{
    public OUTL_EntityAdapter Entity;
    public NavMeshAgent Agent;
    public CharacterController CharacterController;
    public bool AutoFindAgent = true;
    public bool AutoFindCharacterController = true;
    public bool UseTransformFallback = true;
    public bool UseCharacterControllerFallback = true;
    public float FallbackSpeed = 3f;
    public float RotationSpeed = 720f;
    public float RepathInterval = 0.25f;
    public float StopDistance = 1.4f;
    public bool SnapToNavMeshOnEnable = true;
    public float NavMeshSpawnSampleDistance = 4f;

    [Header("Debug / Authority")]
    public string CurrentMovementAuthority = "none";
    public Vector3 LastRequestedDestination;
    public float LastDestinationSetTime;
    public float LastStopTime;
    public int DestinationSetCount;
    public int StopRequestCount;

    [Header("OUTL Tick")]
    public bool UseOUTLTick = true;
    public OUTL_TickLane TickLane = OUTL_TickLane.Logic;
    public float TickInterval = 0.04f;
    public bool TickWhenPaused = false;
    [Tooltip("If true, disables NavMeshAgent automatic transform updates and moves transform from OUTL ticks.")]
    public bool ManualAgentTransformUpdate = false;
    [Tooltip("If true, Update() only performs visual smoothing/agent sync work. Heavy movement still runs from OUTL tick.")]
    public bool AllowVisualUpdate = true;
    [Tooltip("If true, Full/Near runtime tiers apply movement from Unity Update for frame-responsive close NPCs while Mid/Far/Dormant stay scheduler-budgeted.")]
    public bool UseUnityUpdateForFullNear = true;
    [Tooltip("Legacy compatibility only. Keep false for canonical OUTL runtime; movement simulation should run from OUTL_Scheduler.")]
    public bool AllowLegacyUpdateTick = false;

    [Header("Gravity")]
    public bool AffectedByGravity = true;
    public bool Flying;
    public bool Swimming;
    public LayerMask GroundMask = ~0;
    public float GroundProbeDistance = 0.35f;
    public float GroundProbeRadius = 0.25f;
    public float GroundSnapDistance = 0.35f;
    public float GroundSkin = 0.02f;
    public float GroundStickSpeed = 2f;
    public bool StableGroundFallback = true;
    public bool DisableGravityWhenNoGround = true;
    public QueryTriggerInteraction GroundTriggerInteraction = QueryTriggerInteraction.Ignore;

    private Vector3 currentDestination;
    private bool hasDestination;
    private float nextRepath;
    private float verticalVelocity;
    private bool registered;
    private bool lastUseOUTLTick;
    private bool lastManualAgentTransformUpdate;
    private bool agentRotationSuppressed;
    private float agentRotationSuppressedUntil;
    private CapsuleCollider cachedCapsule;
    private Collider cachedCollider;

    public bool HasDestination { get { return hasDestination; } }
    public bool OUTL_IsTickEnabled { get { return UseOUTLTick && isActiveAndEnabled && !ShouldUseUnityUpdateMove() && !IsDead() && (TickWhenPaused || OUTL_World.Instance == null || !OUTL_World.Instance.IsPaused); } }
    public OUTL_TickLane OUTL_TickLane { get { return TickLane; } }
    public float OUTL_TickInterval { get { return Mathf.Max(0.001f, TickInterval); } }

    private void Awake()
    {
        ResolveAgent();
        ApplyAgentMode();
        lastUseOUTLTick = UseOUTLTick;
        lastManualAgentTransformUpdate = ManualAgentTransformUpdate;
    }

    private void OnEnable()
    {
        ResolveAgent();
        ApplyAgentMode();
        if (SnapToNavMeshOnEnable) TryPlaceAgentOnNavMesh();
        SyncTickRegistration();
    }

    private void OnDisable()
    {
        UnregisterTick();
    }

    private void Update()
    {
        SyncRuntimeModeChanges();
        RefreshAgentRotationSuppression();

        if (UseOUTLTick)
        {
            if (ShouldUseUnityUpdateMove())
            {
                TickMove(Time.deltaTime);
                if (AllowVisualUpdate) VisualUpdate();
            }
            else if (AllowVisualUpdate) VisualUpdate();
            return;
        }

        if (AllowLegacyUpdateTick)
            TickMove(ReadDeltaTime());
        else if (AllowVisualUpdate)
            VisualUpdate();
    }

    public void SetOUTLTickMode(bool enabled)
    {
        if (UseOUTLTick == enabled)
        {
            SyncTickRegistration();
            return;
        }

        UseOUTLTick = enabled;
        SyncTickRegistration();
        lastUseOUTLTick = UseOUTLTick;
    }

    public void RefreshRuntimeMode()
    {
        ResolveAgent();
        ApplyAgentMode();
        SyncTickRegistration();
    }

    public void SetDestination(Vector3 destination)
    {
        SetDestination(destination, "legacy");
    }

    public void SetDestination(Vector3 destination, string authority)
    {
        currentDestination = destination;
        hasDestination = true;
        LastRequestedDestination = destination;
        LastDestinationSetTime = OUTL_World.Instance != null ? OUTL_World.Instance.WorldTime : Time.time;
        DestinationSetCount++;
        CurrentMovementAuthority = string.IsNullOrEmpty(authority) ? "unknown" : authority;
    }

    public void Stop()
    {
        Stop("legacy");
    }

    public void Stop(string authority)
    {
        hasDestination = false;
        LastStopTime = OUTL_World.Instance != null ? OUTL_World.Instance.WorldTime : Time.time;
        StopRequestCount++;
        CurrentMovementAuthority = string.IsNullOrEmpty(authority) ? "stopped" : "stopped:" + authority;
        if (Agent != null && Agent.enabled && Agent.isOnNavMesh)
        {
            Agent.isStopped = true;
            Agent.ResetPath();
        }
    }

    public void SyncAgentToTransform()
    {
        ResolveAgent();
        if (Agent == null || !Agent.enabled) return;
        if (!TryPlaceAgentOnNavMesh() && Agent.isOnNavMesh) Agent.Warp(transform.position);
        if (ManualAgentTransformUpdate) Agent.nextPosition = transform.position;
        nextRepath = 0f;
    }

    public bool TryPlaceAgentOnNavMesh()
    {
        ResolveAgent();
        if (Agent == null || !Agent.enabled || !gameObject.activeInHierarchy) return false;
        if (Agent.isOnNavMesh)
        {
            bool warped = Agent.Warp(transform.position);
            if (ManualAgentTransformUpdate) Agent.nextPosition = transform.position;
            return warped;
        }

        NavMeshHit hit;
        if (!NavMesh.SamplePosition(transform.position, out hit, Mathf.Max(0.1f, NavMeshSpawnSampleDistance), Agent.areaMask))
            return false;

        transform.position = hit.position;
        bool placed = Agent.Warp(hit.position);
        if (placed && ManualAgentTransformUpdate) Agent.nextPosition = hit.position;
        return placed;
    }

    public void ResumeAfterMaterialization()
    {
        ResolveAgent();
        ApplyAgentMode();
        TryPlaceAgentOnNavMesh();
        verticalVelocity = 0f;
        nextRepath = 0f;
        SyncTickRegistration();
    }

    public void SuppressAgentRotation(float seconds)
    {
        if (Agent == null || ManualAgentTransformUpdate) return;
        float until = Time.time + Mathf.Max(0.02f, seconds);
        if (!agentRotationSuppressed || until > agentRotationSuppressedUntil)
            agentRotationSuppressedUntil = until;
        agentRotationSuppressed = true;
        if (Agent.enabled) Agent.updateRotation = false;
    }

    public bool IsAtDestination(Vector3 destination)
    {
        Vector3 flat = destination - transform.position;
        flat.y = 0f;
        return flat.sqrMagnitude <= StopDistance * StopDistance;
    }

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        if (ShouldUseUnityUpdateMove()) return;
        TickMove(deltaTime);
    }

    public void TickMove(float deltaTime)
    {
        if (deltaTime <= 0f) return;
        RefreshAgentRotationSuppression();
        if (IsDead())
        {
            Stop("dead");
            return;
        }

        if (Agent != null && Agent.enabled && Agent.isOnNavMesh)
        {
            TickAgent(deltaTime);
            return;
        }

        TickTransformFallback(deltaTime);
    }

    private void TickAgent(float deltaTime)
    {
        Agent.stoppingDistance = StopDistance;
        Agent.updateUpAxis = !Flying && !Swimming;

        float time = OUTL_World.Instance != null ? OUTL_World.Instance.WorldTime : Time.time;
        if (hasDestination && time >= nextRepath)
        {
            nextRepath = time + Mathf.Max(0.02f, RepathInterval);
            Agent.isStopped = false;
            Agent.SetDestination(currentDestination);
        }
        else if (!hasDestination)
        {
            Agent.isStopped = true;
        }

        if (ManualAgentTransformUpdate)
        {
            Vector3 next = Agent.nextPosition;
            transform.position = next;
            Vector3 desired = Agent.desiredVelocity;
            desired.y = 0f;
            if (desired.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(desired.normalized), RotationSpeed * deltaTime);
        }
    }

    private void TickTransformFallback(float deltaTime)
    {
        if (!UseTransformFallback) return;

        Vector3 step = Vector3.zero;
        if (hasDestination)
        {
            Vector3 dir = currentDestination - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > StopDistance * StopDistance)
            {
                step += dir.normalized * FallbackSpeed * deltaTime;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir.normalized), RotationSpeed * deltaTime);
            }
        }

        ApplyFallbackGravity(ref step, deltaTime);
        MoveFallbackStep(step);
    }

    private void VisualUpdate()
    {
        if (!ManualAgentTransformUpdate) return;
        if (Agent == null || !Agent.enabled || !Agent.isOnNavMesh) return;
        Agent.nextPosition = transform.position;
    }

    private float ReadDeltaTime()
    {
        OUTL_World world = OUTL_World.Instance;
        if (world != null) return world.IsPaused ? 0f : world.DeltaTime;
        return Time.deltaTime;
    }

    private bool ShouldUseUnityUpdateMove()
    {
        if (!UseUnityUpdateForFullNear || !isActiveAndEnabled) return false;
        if (OUTL_World.Instance != null && OUTL_World.Instance.IsPaused) return false;
        ResolveAgent();
        if (Entity == null || Entity.Runtime == null) return false;
        OUTL_RuntimeTier tier = Entity.Runtime.Tier;
        return tier == OUTL_RuntimeTier.Full || tier == OUTL_RuntimeTier.Near;
    }

    private bool IsDead()
    {
        OUTL_EntityRuntime runtime = Entity != null ? Entity.Runtime : null;
        return runtime != null && (runtime.Dead || runtime.LifeState == OUTL_LifeState.Dead || runtime.State.GetFlag(OUTL_StateId.Dead) || runtime.Stats.Get(OUTL_StatId.Health, 1f) <= 0f);
    }

    private void ResolveAgent()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (Agent == null && AutoFindAgent) Agent = GetComponent<NavMeshAgent>();
        if (CharacterController == null && AutoFindCharacterController) CharacterController = GetComponent<CharacterController>();
        if (cachedCapsule == null) cachedCapsule = GetComponent<CapsuleCollider>();
        if (cachedCollider == null) cachedCollider = GetComponent<Collider>();
    }

    private void ApplyFallbackGravity(ref Vector3 step, float deltaTime)
    {
        if (!AffectedByGravity || Flying || Swimming)
        {
            verticalVelocity = 0f;
            return;
        }

        RaycastHit hit;
        bool hasGround = TryProbeGround(deltaTime, out hit);
        if (StableGroundFallback && hasGround && verticalVelocity <= 0f)
        {
            float targetY = hit.point.y + ResolveFeetOffset() + Mathf.Max(0f, GroundSkin);
            float deltaToGround = targetY - transform.position.y;
            if (deltaToGround >= -Mathf.Max(0.01f, GroundSnapDistance))
            {
                step.y = Mathf.Max(step.y, deltaToGround);
                verticalVelocity = -Mathf.Max(0f, GroundStickSpeed);
                return;
            }
        }

        if (!hasGround && DisableGravityWhenNoGround && !hasDestination)
        {
            verticalVelocity = 0f;
            step.y = 0f;
            return;
        }

        verticalVelocity -= Mathf.Max(0f, OUTL_Cheats.UnityGravity) * deltaTime;
        step.y += verticalVelocity * deltaTime;
    }

    private bool TryProbeGround(float deltaTime, out RaycastHit hit)
    {
        float radius = ResolveProbeRadius();
        float feetOffset = ResolveFeetOffset();
        float extraFall = Mathf.Max(0f, -verticalVelocity * deltaTime);
        Vector3 origin = transform.position + Vector3.up * (feetOffset + Mathf.Max(radius + 0.02f, GroundProbeDistance));
        float distance = Mathf.Max(0.05f, GroundProbeDistance + GroundSnapDistance + extraFall);
        OUTL_Profile.Frame.Raycasts++;
        return Physics.SphereCast(origin, radius, Vector3.down, out hit, distance, GroundMask, GroundTriggerInteraction);
    }

    private float ResolveProbeRadius()
    {
        if (CharacterController != null) return Mathf.Max(0.05f, CharacterController.radius * 0.9f);
        CapsuleCollider capsule = cachedCapsule;
        if (capsule != null) return Mathf.Max(0.05f, capsule.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z) * 0.9f);
        Collider collider = cachedCollider;
        if (collider != null) return Mathf.Max(0.05f, Mathf.Min(collider.bounds.extents.x, collider.bounds.extents.z) * 0.9f);
        return Mathf.Max(0.05f, GroundProbeRadius);
    }

    private float ResolveFeetOffset()
    {
        if (CharacterController != null) return Mathf.Max(0f, CharacterController.center.y - CharacterController.height * 0.5f);
        CapsuleCollider capsule = cachedCapsule;
        if (capsule != null) return Mathf.Max(0f, capsule.center.y - capsule.height * 0.5f);
        Collider collider = cachedCollider;
        if (collider != null) return Mathf.Max(0f, transform.position.y - collider.bounds.min.y);
        return 0f;
    }

    private void MoveFallbackStep(Vector3 step)
    {
        if (UseCharacterControllerFallback && CharacterController != null && CharacterController.enabled)
        {
            CollisionFlags flags = CharacterController.Move(step);
            if ((flags & CollisionFlags.Below) != 0 && verticalVelocity < 0f)
                verticalVelocity = -Mathf.Max(0f, GroundStickSpeed);
            return;
        }

        transform.position += step;
    }

    private void ApplyAgentMode()
    {
        if (Agent == null) return;
        if (ManualAgentTransformUpdate)
        {
            Agent.updatePosition = false;
            Agent.updateRotation = false;
        }
        else
        {
            Agent.updatePosition = true;
            Agent.updateRotation = !agentRotationSuppressed;
        }
    }

    private void RefreshAgentRotationSuppression()
    {
        if (!agentRotationSuppressed) return;
        if (ManualAgentTransformUpdate)
        {
            agentRotationSuppressed = false;
            ApplyAgentMode();
            return;
        }

        if (Time.time <= agentRotationSuppressedUntil) return;
        agentRotationSuppressed = false;
        ApplyAgentMode();
    }

    private void SyncRuntimeModeChanges()
    {
        if (lastUseOUTLTick != UseOUTLTick)
        {
            SyncTickRegistration();
            lastUseOUTLTick = UseOUTLTick;
        }

        if (lastManualAgentTransformUpdate != ManualAgentTransformUpdate)
        {
            ApplyAgentMode();
            lastManualAgentTransformUpdate = ManualAgentTransformUpdate;
        }
    }

    private void SyncTickRegistration()
    {
        if (UseOUTLTick) RegisterTick();
        else UnregisterTick();
    }

    private void RegisterTick()
    {
        if (!UseOUTLTick || registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Register(this);
        registered = true;
    }

    private void UnregisterTick()
    {
        if (!registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Unregister(this);
        registered = false;
    }

    public void OUTL_OnPoolSpawn()
    {
        ResolveAgent();
        ApplyAgentMode();
        verticalVelocity = 0f;
        nextRepath = 0f;
        SyncTickRegistration();
    }

    public void OUTL_OnPoolRelease()
    {
        Stop("pool_release");
        verticalVelocity = 0f;
        nextRepath = 0f;
        UnregisterTick();
    }
}
