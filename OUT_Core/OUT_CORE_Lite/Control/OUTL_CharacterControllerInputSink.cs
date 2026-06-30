using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class OUTL_CharacterControllerInputSink : MonoBehaviour, OUTL_IActorInputPhasedSink, OUTL_IPoolReset
{
    public OUTL_EntityAdapter Entity;
    public CharacterController Controller;
    public Transform YawRoot;
    public Transform ViewPitchRoot;

    [Header("GoldSrc Scale")]
    public bool UseGoldSrcUnits = true;
    public float GoldSrcUnitsPerUnityUnit = 32f;

    [Header("View")]
    public float MinPitch = -89f;
    public float MaxPitch = 89f;
    public bool RotateYawFromLook = true;
    public bool RotateViewPitchFromLook = true;
    public float StandingViewHeight = 1.62f;
    public float CrouchViewHeight = 0.92f;
    public float ViewHeightLerpSpeed = 16f;

    [Header("GoldSrc Movement")]
    public float ForwardSpeed = 320f;
    public float SideSpeed = 320f;
    public float BackSpeed = 320f;
    public float WalkSpeed = 150f;
    public float RunSpeed = 320f;
    public bool HoldShiftToWalk = true;
    public float CrouchSpeedMultiplier = 0.333f;
    public float GroundAcceleration = 10f;
    public float AirAcceleration = 10f;
    public float AirWishSpeedCap = 30f;
    public float Friction = 4f;
    public float StopSpeed = 100f;
    public float JumpSpeed = 270f;
    public bool AutoBunnyHop = true;
    public bool SkipFrictionOnJumpFrame = true;
    public float JumpGroundLockout = 0.12f;

    [Header("Gravity")]
    [Tooltip("Fallback gravity in GoldSrc units. sv_gravity is the normal source.")]
    public float Gravity = 981f;
    public bool AllowFallbackGravityWhenSvGravityZero;
    public float GravityMultiplier = 1.65f;
    public float RisingGravityMultiplier = 1.0f;
    public float FallingGravityMultiplier = 1.75f;
    public float LowJumpGravityMultiplier = 2.25f;
    public bool ApplyLowJumpGravityWhenJumpReleased = true;
    public float MaxFallSpeed = 54f;
    public float GroundStickSpeed = 8f;
    public float StableGroundUpSpeed = 2.5f;

    [Header("Crouch / Hull")]
    public float StandingHeight = 1.8f;
    public float CrouchHeight = 1.0f;
    public float HullRadius = 0.32f;
    public float CrouchLerpSpeed = 18f;
    public LayerMask UncrouchBlockMask = ~0;
    public QueryTriggerInteraction UncrouchTriggerInteraction = QueryTriggerInteraction.Ignore;
    public float CrouchNoiseMultiplier = 0.35f;
    public float CrouchStimulusRadiusMultiplier = 0.45f;

    [Header("Ground")]
    public LayerMask GroundMask = ~0;
    public QueryTriggerInteraction GroundTriggerInteraction = QueryTriggerInteraction.Ignore;
    public float GroundProbeExtraDistance = 0.20f;
    public float GroundSnapDistance = 0.24f;
    public float StepOffset = 0.35f;
    public float SlopeLimit = 45f;
    public bool ProjectMoveOnGroundPlane = true;
    public bool CancelDownhillSlideOnWalkableGround = true;
    public float SlopeSlideStartAngle = 50f;

    [Header("Noclip")]
    public bool SupportNoClip = true;
    public float NoClipSpeed = 12f;
    public float NoClipFastMultiplier = 4f;

    [Header("Runtime State")]
    public bool WriteMotorStateToEntity = true;
    public Vector3 Velocity;
    public bool IsGrounded;
    public bool IsCrouching;
    public float CurrentNoiseMultiplier = 1f;
    public float CurrentStimulusRadiusMultiplier = 1f;
    public float LastFallSpeed;

    [Header("Legacy Serialized Fields")]
    [HideInInspector] public float MoveSpeed = 4.5f;
    [HideInInspector] public float SprintMultiplier = 1.35f;
    [HideInInspector] public float CrouchMultiplier = 0.55f;
    [HideInInspector] public float GroundStickVelocity = -2f;

    public OUTL_ActorInputPhase Phase { get { return OUTL_ActorInputPhase.Movement; } }

    private readonly Collider[] uncrouchBuffer = new Collider[24];
    private float pitch;
    private bool wasGrounded;
    private bool hasGroundHit;
    private Vector3 groundNormal = Vector3.up;
    private float groundSlopeAngle;
    private RaycastHit groundHit;
    private float lastJumpTime = -999f;
    private float previousVerticalVelocity;
    private float maxObservedFallSpeed;
    private bool wasNoClip;

    private void Awake()
    {
        Resolve();
        ApplyControllerDefaults();
        if (ViewPitchRoot != null) pitch = NormalizePitch(ViewPitchRoot.localEulerAngles.x);
    }

    private void OnValidate()
    {
        GoldSrcUnitsPerUnityUnit = Mathf.Max(1f, GoldSrcUnitsPerUnityUnit);
        Gravity = Mathf.Abs(Gravity);
        StandingHeight = Mathf.Max(0.5f, StandingHeight);
        CrouchHeight = Mathf.Clamp(CrouchHeight, HullRadius * 2f + 0.02f, StandingHeight);
        HullRadius = Mathf.Max(0.05f, HullRadius);
        GroundProbeExtraDistance = Mathf.Max(0.01f, GroundProbeExtraDistance);
        GroundSnapDistance = Mathf.Max(0f, GroundSnapDistance);
    }

    public void OUTL_ApplyInput(in OUTL_ActorInputFrame frame, OUTL_World world)
    {
        Resolve();
        if (Controller == null || IsDead()) return;

        float dt = ReadDeltaTime(frame, world);
        if (dt <= 0f) return;

        if (SupportNoClip && Entity != null && OUTL_Cheats.IsNoClipEntity(Entity.Id))
        {
            NoclipMove(frame, dt);
            WriteMotorState();
            return;
        }

        if (wasNoClip)
        {
            wasNoClip = false;
            Controller.enabled = true;
            Velocity = Vector3.zero;
        }

        Transform yaw = YawRoot != null ? YawRoot : transform;
        ApplyLook(frame, yaw);
        SmoothCrouchAndView(frame.CrouchHeld, dt);
        ProbeGroundForNormalOnly();

        Vector3 wishDir;
        float wishSpeed;
        BuildWishVelocity(frame, yaw, out wishDir, out wishSpeed);

        wasGrounded = IsGrounded;
        IsGrounded = CanUseGroundBeforeMove();
        bool jumpRequest = frame.JumpPressed || (AutoBunnyHop && frame.JumpHeld);

        if (IsGrounded)
            GroundMove(wishDir, wishSpeed, jumpRequest, frame.JumpHeld, dt);
        else
        {
            AirMove(wishDir, wishSpeed, frame.JumpHeld, dt);
            TrackFallSpeed();
        }

        previousVerticalVelocity = Velocity.y;
        CollisionFlags flags = Controller.Move(Velocity * dt);
        ClipVelocityFromControllerFlags(flags);

        bool groundedAfterMove = (flags & CollisionFlags.Below) != 0;
        bool landed = !wasGrounded && groundedAfterMove;
        IsGrounded = groundedAfterMove || (IsGrounded && Velocity.y <= 0f);
        if (!IsGrounded) TrackFallSpeed();
        if (landed) OnLanded(previousVerticalVelocity);

        UpdateNoise(frame);
        WriteMotorState();
    }

    public void OUTL_OnPoolSpawn()
    {
        Velocity = Vector3.zero;
        IsGrounded = false;
        IsCrouching = false;
        LastFallSpeed = 0f;
        maxObservedFallSpeed = 0f;
        wasNoClip = false;
        Resolve();
        ApplyControllerDefaults();
        if (ViewPitchRoot != null) pitch = NormalizePitch(ViewPitchRoot.localEulerAngles.x);
    }

    public void OUTL_OnPoolRelease()
    {
        Velocity = Vector3.zero;
        IsGrounded = false;
        IsCrouching = false;
        LastFallSpeed = 0f;
        maxObservedFallSpeed = 0f;
        wasNoClip = false;
        pitch = 0f;
    }

    private void GroundMove(Vector3 wishDir, float wishSpeed, bool jumpRequest, bool jumpHeld, float dt)
    {
        bool jumpNow = jumpRequest && CanStartGroundJump();
        if (!(jumpNow && SkipFrictionOnJumpFrame)) ApplyFriction(dt);

        if (ProjectMoveOnGroundPlane && hasGroundHit && wishDir.sqrMagnitude > 0.0001f)
            wishDir = BuildGroundWishDir(wishDir);

        Accelerate(wishDir, wishSpeed, GroundAcceleration, dt);

        if (jumpNow)
        {
            Velocity.y = ScaleSpeed(JumpSpeed);
            IsGrounded = false;
            lastJumpTime = Time.time;
            return;
        }

        if (Velocity.y <= Mathf.Max(0.01f, StableGroundUpSpeed))
            ApplyGroundContactVelocity();
    }

    private void AirMove(Vector3 wishDir, float wishSpeed, bool jumpHeld, float dt)
    {
        float cappedWishSpeed = Mathf.Min(wishSpeed, ScaleSpeed(AirWishSpeedCap));
        Accelerate(wishDir, cappedWishSpeed, AirAcceleration, dt);
        Velocity.y -= BuildEffectiveGravity(jumpHeld) * dt;
        float maxFall = Mathf.Max(1f, MaxFallSpeed);
        if (Velocity.y < -maxFall) Velocity.y = -maxFall;
    }

    private void BuildWishVelocity(in OUTL_ActorInputFrame frame, Transform yaw, out Vector3 wishDir, out float wishSpeed)
    {
        Vector2 moveInput = Vector2.ClampMagnitude(frame.Move, 1f);
        float forwardMove = moveInput.y >= 0f ? moveInput.y * ScaleSpeed(ForwardSpeed) : moveInput.y * ScaleSpeed(BackSpeed);
        float sideMove = moveInput.x * ScaleSpeed(SideSpeed);

        wishDir = yaw.forward * forwardMove + yaw.right * sideMove;
        wishDir.y = 0f;
        wishSpeed = wishDir.magnitude;

        if (wishSpeed > 0.0001f) wishDir /= wishSpeed;
        else wishDir = Vector3.zero;

        float maxSpeed = ScaleSpeed(frame.SprintHeld && HoldShiftToWalk ? WalkSpeed : RunSpeed);
        if (!HoldShiftToWalk && frame.SprintHeld) maxSpeed = ScaleSpeed(RunSpeed) * Mathf.Max(1f, SprintMultiplier);
        if (wishSpeed > maxSpeed) wishSpeed = maxSpeed;
        if (IsCrouching) wishSpeed *= Mathf.Max(0.01f, CrouchSpeedMultiplier);
    }

    private void ApplyLook(in OUTL_ActorInputFrame frame, Transform yaw)
    {
        if (RotateYawFromLook && Mathf.Abs(frame.Look.x) > 0.0001f)
            yaw.Rotate(0f, frame.Look.x, 0f, Space.Self);

        if (!RotateViewPitchFromLook || ViewPitchRoot == null || Mathf.Abs(frame.Look.y) <= 0.0001f) return;
        pitch = Mathf.Clamp(pitch - frame.Look.y, MinPitch, MaxPitch);
        ViewPitchRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void SmoothCrouchAndView(bool wantsCrouch, float dt)
    {
        bool shouldCrouch = wantsCrouch || !CanStandUp();
        IsCrouching = shouldCrouch;
        float targetHeight = shouldCrouch ? CrouchHeight : StandingHeight;
        float nextHeight = Mathf.Lerp(Controller.height, targetHeight, Mathf.Clamp01(dt * Mathf.Max(0.01f, CrouchLerpSpeed)));
        ApplyControllerShape(nextHeight);

        if (ViewPitchRoot != null)
        {
            Vector3 lp = ViewPitchRoot.localPosition;
            float targetY = shouldCrouch ? CrouchViewHeight : StandingViewHeight;
            lp.y = Mathf.Lerp(lp.y, targetY, Mathf.Clamp01(dt * Mathf.Max(0.01f, ViewHeightLerpSpeed)));
            ViewPitchRoot.localPosition = lp;
        }
    }

    private bool CanStandUp()
    {
        if (!IsCrouching) return true;
        Vector3 basePos = transform.position + Vector3.up * HullRadius;
        Vector3 topPos = transform.position + Vector3.up * (StandingHeight - HullRadius);
        int count = Physics.OverlapCapsuleNonAlloc(basePos, topPos, HullRadius * 0.95f, uncrouchBuffer, UncrouchBlockMask, UncrouchTriggerInteraction);
        for (int i = 0; i < count; i++)
        {
            Collider c = uncrouchBuffer[i];
            uncrouchBuffer[i] = null;
            if (c != null && c.gameObject != gameObject && !c.transform.IsChildOf(transform)) return false;
        }
        return true;
    }

    private void ApplyFriction(float dt)
    {
        Vector3 lateral = Velocity;
        lateral.y = 0f;
        float speed = lateral.magnitude;
        if (speed < 0.0001f) return;
        float control = speed < ScaleSpeed(StopSpeed) ? ScaleSpeed(StopSpeed) : speed;
        float drop = control * Mathf.Max(0f, Friction) * dt;
        float newSpeed = Mathf.Max(speed - drop, 0f);
        if (newSpeed == speed) return;
        newSpeed /= speed;
        Velocity.x *= newSpeed;
        Velocity.z *= newSpeed;
    }

    private void Accelerate(Vector3 wishDir, float wishSpeed, float accel, float dt)
    {
        if (wishSpeed <= 0f || wishDir.sqrMagnitude <= 0.0001f) return;
        float currentSpeed = Vector3.Dot(Velocity, wishDir);
        float addSpeed = wishSpeed - currentSpeed;
        if (addSpeed <= 0f) return;
        float accelSpeed = Mathf.Max(0f, accel) * dt * wishSpeed;
        if (accelSpeed > addSpeed) accelSpeed = addSpeed;
        Velocity += wishDir * accelSpeed;
    }

    private float BuildEffectiveGravity(bool jumpHeld)
    {
        float g = Mathf.Max(0f, OUTL_Cheats.UnityGravity);
        if (g <= 0f && AllowFallbackGravityWhenSvGravityZero)
            g = ScaleSpeed(Gravity);
        g *= Mathf.Max(0f, GravityMultiplier);
        if (Velocity.y > 0f)
        {
            g *= Mathf.Max(0f, RisingGravityMultiplier);
            if (ApplyLowJumpGravityWhenJumpReleased && !jumpHeld)
                g *= Mathf.Max(0f, LowJumpGravityMultiplier);
        }
        else
        {
            g *= Mathf.Max(0f, FallingGravityMultiplier);
        }
        return g;
    }

    private void ProbeGroundForNormalOnly()
    {
        hasGroundHit = false;
        groundNormal = Vector3.up;
        groundSlopeAngle = 0f;
        if (Controller == null) return;

        Vector3 bottomSphereCenter = transform.position + Vector3.up * (Controller.radius + 0.02f);
        float distance = Mathf.Max(0.01f, Mathf.Max(GroundProbeExtraDistance, GroundSnapDistance));
        OUTL_Profile.Frame.Raycasts++;
        if (Physics.SphereCast(bottomSphereCenter, Controller.radius * 0.92f, Vector3.down, out groundHit, distance, GroundMask, GroundTriggerInteraction))
        {
            float slope = Vector3.Angle(groundHit.normal, Vector3.up);
            if (slope <= SlopeLimit + 0.5f)
            {
                hasGroundHit = true;
                groundNormal = groundHit.normal;
                groundSlopeAngle = slope;
            }
        }
    }

    private bool CanUseGroundBeforeMove()
    {
        if (Time.time - lastJumpTime < JumpGroundLockout) return false;
        if (Velocity.y > Mathf.Max(0.01f, StableGroundUpSpeed)) return false;
        return Controller.isGrounded || hasGroundHit;
    }

    private bool CanStartGroundJump()
    {
        return Time.time - lastJumpTime >= JumpGroundLockout;
    }

    private Vector3 BuildGroundWishDir(Vector3 wishDir)
    {
        Vector3 projected = Vector3.ProjectOnPlane(wishDir, groundNormal);
        if (CancelDownhillSlideOnWalkableGround && groundSlopeAngle <= Mathf.Max(SlopeLimit, SlopeSlideStartAngle))
            projected.y = 0f;
        if (projected.sqrMagnitude <= 0.0001f) return wishDir;
        return projected.normalized;
    }

    private void ApplyGroundContactVelocity()
    {
        float stick = ScaleSpeed(Mathf.Max(0f, GroundStickSpeed));
        if (stick <= 0f)
        {
            Velocity.y = Mathf.Min(Velocity.y, 0f);
            return;
        }

        if (CancelDownhillSlideOnWalkableGround && hasGroundHit && groundSlopeAngle <= Mathf.Max(SlopeLimit, SlopeSlideStartAngle))
            Velocity.y = -stick;
        else if (Velocity.y < 0f)
            Velocity.y = -stick;
    }

    private void ClipVelocityFromControllerFlags(CollisionFlags flags)
    {
        if ((flags & CollisionFlags.Above) != 0 && Velocity.y > 0f) Velocity.y = 0f;
        if ((flags & CollisionFlags.Below) != 0 && Velocity.y < 0f) ApplyGroundContactVelocity();
    }

    private void TrackFallSpeed()
    {
        if (Velocity.y < 0f) maxObservedFallSpeed = Mathf.Max(maxObservedFallSpeed, -Velocity.y);
    }

    private void OnLanded(float previousY)
    {
        LastFallSpeed = Mathf.Max(maxObservedFallSpeed, previousY < 0f ? -previousY : 0f);
        maxObservedFallSpeed = 0f;
    }

    private void NoclipMove(in OUTL_ActorInputFrame frame, float dt)
    {
        wasNoClip = true;
        if (Controller.enabled) Controller.enabled = false;
        Transform basis = ViewPitchRoot != null ? ViewPitchRoot : (YawRoot != null ? YawRoot : transform);
        Vector3 move = basis.forward * frame.Move.y + basis.right * frame.Move.x;
        if (frame.JumpHeld) move += Vector3.up;
        if (frame.CrouchHeld) move += Vector3.down;
        if (move.sqrMagnitude > 1f) move.Normalize();
        float speed = NoClipSpeed * (frame.SprintHeld ? Mathf.Max(1f, NoClipFastMultiplier) : 1f);
        transform.position += move * speed * dt;
        Velocity = move * speed;
        IsGrounded = false;
        IsCrouching = false;
    }

    private void UpdateNoise(in OUTL_ActorInputFrame frame)
    {
        CurrentNoiseMultiplier = IsCrouching ? Mathf.Max(0f, CrouchNoiseMultiplier) : 1f;
        CurrentStimulusRadiusMultiplier = IsCrouching ? Mathf.Max(0f, CrouchStimulusRadiusMultiplier) : 1f;
        if (!HoldShiftToWalk && frame.SprintHeld && !IsCrouching)
            CurrentNoiseMultiplier = Mathf.Max(CurrentNoiseMultiplier, SprintMultiplier);
    }

    private void WriteMotorState()
    {
        if (!WriteMotorStateToEntity || Entity == null || Entity.Runtime == null) return;
        Entity.Runtime.State.SetFlag("Player.Grounded", IsGrounded);
        Entity.Runtime.State.SetFlag("Player.Crouching", IsCrouching);
        Entity.Runtime.State.SetFloat("Player.Speed", HorizontalSpeed());
        Entity.Runtime.State.SetFloat("Player.VerticalSpeed", Velocity.y);
        Entity.Runtime.State.SetFloat("Player.Slope", groundSlopeAngle);
        Entity.Runtime.State.SetFloat("Player.FallSpeed", LastFallSpeed);
        Entity.Runtime.State.SetFloat("Player.NoiseMultiplier", CurrentNoiseMultiplier);
        Entity.Runtime.State.SetFloat("Player.StimulusRadiusMultiplier", CurrentStimulusRadiusMultiplier);
    }

    private float HorizontalSpeed()
    {
        Vector3 v = Velocity;
        v.y = 0f;
        return v.magnitude;
    }

    private void ApplyControllerDefaults()
    {
        if (Controller == null) return;
        Controller.slopeLimit = SlopeLimit;
        Controller.stepOffset = StepOffset;
        ApplyControllerShape(IsCrouching ? CrouchHeight : StandingHeight);
    }

    private void ApplyControllerShape(float height)
    {
        if (Controller == null) return;
        Controller.height = Mathf.Max(height, HullRadius * 2f + 0.02f);
        Controller.radius = HullRadius;
        Controller.center = new Vector3(0f, Controller.height * 0.5f, 0f);
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

    private float ScaleSpeed(float value)
    {
        return UseGoldSrcUnits ? value / Mathf.Max(1f, GoldSrcUnitsPerUnityUnit) : value;
    }

    private void Resolve()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (Controller == null) Controller = GetComponent<CharacterController>();
        if (YawRoot == null) YawRoot = transform;
    }

    private static float NormalizePitch(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}
