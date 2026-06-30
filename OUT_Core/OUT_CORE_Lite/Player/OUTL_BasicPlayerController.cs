using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class OUTL_BasicPlayerController : MonoBehaviour
{
    public OUTL_EntityAdapter Entity;
    public OUTL_AttackDriver AttackDriver;
    public OUTL_EquipmentRuntime EquipmentRuntime;
    public Camera ViewCamera;
    public CharacterController CharacterController;
    public OUTL_CharacterAnimationBridge AnimationBridge;

    [Header("Data Profile")]
    public OUTL_PlayerMotorProfile MotorProfile;
    public bool ApplyMotorProfileOnAwake = true;
    public bool WriteMotorStateToEntity = true;

    [Header("GoldSrc Scale")]
    public bool UseGoldSrcUnits = true;
    public float GoldSrcUnitsPerUnityUnit = 32f;

    [Header("View")]
    public float MouseSensitivity = 2f;
    public bool LockCursor = true;
    public float MinPitch = -89f;
    public float MaxPitch = 89f;
    public float StandingViewHeight = 1.62f;
    public float CrouchViewHeight = 0.92f;
    public float ViewHeightLerpSpeed = 16f;

    [Header("Input")]
    public string HorizontalAxis = "Horizontal";
    public string VerticalAxis = "Vertical";
    public string MouseXAxis = "Mouse X";
    public string MouseYAxis = "Mouse Y";
    public string JumpButton = "Jump";
    public KeyCode CrouchKey = KeyCode.LeftControl;
    public KeyCode AltCrouchKey = KeyCode.C;
    public KeyCode SpeedKey = KeyCode.LeftShift;

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
    [Tooltip("Fallback only. Runtime gravity normally comes from sv_gravity.")]
    public float Gravity = 981f;
    [Header("OUTL Gravity Tuning")]
    [Tooltip("Multiplies OUTL_Cheats.UnityGravity. sv_gravity still remains the base source.")]
    public float GravityMultiplier = 1.65f;
    [Tooltip("Extra gravity while rising. Keep near 1 for classic jump start.")]
    public float RisingGravityMultiplier = 1.0f;
    [Tooltip("Extra gravity while falling. Use this to remove moon-jump float without lowering jump height too much.")]
    public float FallingGravityMultiplier = 1.75f;
    [Tooltip("Extra gravity when jump is released before apex. Gives controllable short-hop behavior.")]
    public float LowJumpGravityMultiplier = 2.25f;
    public bool ApplyLowJumpGravityWhenJumpReleased = true;
    public float MaxFallSpeed = 54f;
    public float JumpSpeed = 270f;
    public bool SkipFrictionOnJumpFrame = true;
    public float JumpGroundLockout = 0.12f;
    public float GroundStickSpeed = 8f;

    [Header("Noclip")]
    public float NoClipSpeed = 12f;
    public float NoClipFastMultiplier = 4f;
    public float NoClipSlowMultiplier = 0.25f;

    [Header("Crouch / Hull")]
    public float StandingHeight = 1.8f;
    public float CrouchHeight = 1.0f;
    public float HullRadius = 0.32f;
    public float CrouchLerpSpeed = 18f;
    public LayerMask UncrouchBlockMask = ~0;
    public QueryTriggerInteraction UncrouchTriggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Ground")]
    public LayerMask GroundMask = ~0;
    public QueryTriggerInteraction GroundTriggerInteraction = QueryTriggerInteraction.Ignore;
    public float GroundProbeExtraDistance = 0.20f;
    public float GroundSnapDistance = 0.24f;
    public float StepOffset = 0.35f;
    public float SlopeLimit = 45f;
    public float StableGroundUpSpeed = 2.5f;
    public bool ProjectMoveOnGroundPlane = true;
    public bool CancelDownhillSlideOnWalkableGround = true;
    public float SlopeSlideStartAngle = 50f;

    [Header("Interaction")]
    public float UseDistance = 3f;
    public LayerMask UseMask = ~0;
    public KeyCode UseKey = KeyCode.E;
    public KeyCode PrimaryFireKey = KeyCode.Mouse0;
    public KeyCode SecondaryFireKey = KeyCode.Mouse1;
    public KeyCode MeleeKey = KeyCode.V;

    [Header("Weapon Slots")]
    public bool EnableWeaponSlotKeys = true;
    public bool EnableMouseWheelWeaponCycle = true;
    public OUTL_EquipmentSlot ActiveWeaponDefaultSlot = OUTL_EquipmentSlot.Primary;
    public KeyCode PrimarySlotKey = KeyCode.Alpha1;
    public KeyCode SecondarySlotKey = KeyCode.Alpha2;
    public KeyCode MeleeSlotKey = KeyCode.Alpha3;
    public KeyCode UtilitySlotKey = KeyCode.Alpha4;

    [Header("Audio")]
    public AudioClip[] FootstepClips;
    public AudioClip[] JumpClips;
    public AudioClip[] LandingClips;
    public float WalkStepDistance = 2.25f;
    public float RunStepDistance = 2.9f;
    public float CrouchStepDistance = 1.65f;
    public float MinFootstepSpeed = 0.8f;
    public float LandingSoundMinFallSpeed = 5f;

    [Header("Fall Damage")]
    public bool EnableFallDamage = true;
    public float FallDamageMinSpeed = 18f;
    public float FallDamageFatalSpeed = 32f;
    public float FallDamageScale = 7f;
    public float FallDamageMaxDamage = 100f;
    public string FallDamageKey = "fall";

    private static readonly OUTL_EquipmentSlot[] WeaponCycleSlots =
    {
        OUTL_EquipmentSlot.Primary,
        OUTL_EquipmentSlot.Secondary,
        OUTL_EquipmentSlot.Melee
    };

    private readonly Collider[] uncrouchBuffer = new Collider[24];
    private float yaw;
    private float pitch;
    private float inputForward;
    private float inputSide;
    private bool speedHeld;
    private bool jumpHeld;
    private bool jumpQueued;
    private bool jumpLatchedUntilRelease;
    private bool wantsCrouch;
    private bool isCrouching;
    private bool isGrounded;
    private bool wasGrounded;
    private bool hasGroundHit;
    private Vector3 groundNormal = Vector3.up;
    private float groundSlopeAngle;
    private RaycastHit groundHit;
    private Vector3 velocity;
    private OUTL_EquipmentSlot activeWeaponSlot;
    private float lastJumpTime = -999f;
    private float previousVerticalVelocity;
    private float maxObservedFallSpeed;
    private float lastFallSpeed;
    private float lastFallDamage;
    private float accumulatedStepDistance;
    private OUTL_Interactable currentInteractable;
    private OUTL_EntityAdapter currentCommandTarget;
    private bool wasNoClip;

    public OUTL_Interactable CurrentInteractable { get { return currentInteractable; } }
    public OUTL_EntityAdapter CurrentCommandTarget { get { return currentCommandTarget; } }
    public Vector3 Velocity { get { return velocity; } }
    public bool IsGrounded { get { return isGrounded; } }
    public bool IsCrouching { get { return isCrouching; } }
    public float HorizontalSpeed { get { Vector3 v = velocity; v.y = 0f; return v.magnitude; } }
    public Vector3 GroundNormal { get { return groundNormal; } }
    public float GroundSlopeAngle { get { return groundSlopeAngle; } }
    public float ViewPitch { get { return pitch; } }
    public float ViewYaw { get { return yaw; } }
    public float LastFallSpeed { get { return lastFallSpeed; } }
    public float LastFallDamage { get { return lastFallDamage; } }
    public OUTL_EquipmentSlot ActiveWeaponSlot { get { return activeWeaponSlot; } }

    public void ApplyMotorProfile()
    {
        if (MotorProfile != null) MotorProfile.ApplyTo(this);
        if (CharacterController != null)
        {
            CharacterController.slopeLimit = SlopeLimit;
            CharacterController.stepOffset = StepOffset;
            ApplyControllerShape(isCrouching ? CrouchHeight : StandingHeight);
        }
        activeWeaponSlot = ActiveWeaponDefaultSlot;
        WriteMotorState();
    }

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (AttackDriver == null) AttackDriver = GetComponent<OUTL_AttackDriver>();
        if (EquipmentRuntime == null) EquipmentRuntime = GetComponent<OUTL_EquipmentRuntime>();
        if (CharacterController == null) CharacterController = GetComponent<CharacterController>();
        if (ViewCamera == null) ViewCamera = GetComponentInChildren<Camera>();
        if (AnimationBridge == null) AnimationBridge = GetComponentInChildren<OUTL_CharacterAnimationBridge>(true);
        if (ApplyMotorProfileOnAwake && MotorProfile != null) MotorProfile.ApplyTo(this);

        CharacterController.radius = HullRadius;
        CharacterController.slopeLimit = SlopeLimit;
        CharacterController.stepOffset = StepOffset;
        ApplyControllerShape(StandingHeight);
        activeWeaponSlot = ActiveWeaponDefaultSlot;

        yaw = transform.eulerAngles.y;
        pitch = ViewCamera != null ? NormalizePitch(ViewCamera.transform.localEulerAngles.x) : 0f;
    }

    private void OnEnable()
    {
        if (LockCursor && !OUTL_DevConsole.IsOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        jumpQueued = false;
        jumpLatchedUntilRelease = false;
    }

    private void Update()
    {
        bool noClip = Entity != null && OUTL_Cheats.IsNoClipEntity(Entity.Id);
        if (noClip)
        {
            NoclipUpdate(Time.unscaledDeltaTime);
            if (AnimationBridge != null) AnimationBridge.PushLocomotion(this);
            return;
        }

        if (wasNoClip)
        {
            wasNoClip = false;
            if (CharacterController != null) CharacterController.enabled = true;
            velocity = Vector3.zero;
        }

        if (OUTL_DevConsole.IsInputCaptured)
        {
            inputForward = 0f;
            inputSide = 0f;
            jumpQueued = false;
            return;
        }

        ReadLookInput();
        ReadMovementInput();
        ReadButtonInput();
        SmoothCrouchAndView(Time.deltaTime);
        ScanInteractable();
        HandleWeaponSlotInput();
        HandleActionInput();
        if (AnimationBridge != null) AnimationBridge.PushLocomotion(this);
    }

    private void FixedUpdate()
    {
        if (OUTL_DevConsole.IsGamePausedByConsole) return;
        if (Entity != null && OUTL_Cheats.IsNoClipEntity(Entity.Id)) return;

        float dt = Time.fixedDeltaTime;
        if (dt <= 0f || CharacterController == null) return;

        bool jumpRequest = jumpQueued;
        jumpQueued = false;

        ProbeGroundForNormalOnly();
        Vector3 wishDir;
        float wishSpeed;
        BuildWishVelocity(out wishDir, out wishSpeed);

        wasGrounded = isGrounded;
        isGrounded = CanUseGroundBeforeMove();

        if (isGrounded)
            GroundMove(wishDir, wishSpeed, jumpRequest, dt);
        else
        {
            AirMove(wishDir, wishSpeed, dt);
            TrackFallSpeed();
        }

        previousVerticalVelocity = velocity.y;
        CollisionFlags flags = CharacterController.Move(velocity * dt);
        ClipVelocityFromControllerFlags(flags);

        bool groundedAfterMove = (flags & CollisionFlags.Below) != 0;
        bool landed = !wasGrounded && groundedAfterMove;
        isGrounded = groundedAfterMove;
        if (!isGrounded) TrackFallSpeed();

        ProgressFootsteps(dt);
        if (landed) OnLanded(previousVerticalVelocity);
        WriteMotorState();
    }

    private void NoclipUpdate(float dt)
    {
        wasNoClip = true;
        if (CharacterController != null && CharacterController.enabled) CharacterController.enabled = false;
        if (OUTL_DevConsole.IsInputCaptured) return;

        ReadLookInput();
        float f = Input.GetAxisRaw(VerticalAxis);
        float s = Input.GetAxisRaw(HorizontalAxis);
        float u = 0f;
        if (Input.GetButton(JumpButton) || Input.GetKey(KeyCode.Space)) u += 1f;
        if (Input.GetKey(CrouchKey) || Input.GetKey(AltCrouchKey)) u -= 1f;

        float mult = Input.GetKey(SpeedKey) ? NoClipFastMultiplier : 1f;
        if (Input.GetKey(KeyCode.LeftAlt)) mult *= NoClipSlowMultiplier;

        Transform basis = ViewCamera != null ? ViewCamera.transform : transform;
        Vector3 move = basis.forward * f + basis.right * s + Vector3.up * u;
        if (move.sqrMagnitude > 1f) move.Normalize();
        transform.position += move * NoClipSpeed * mult * dt;
        velocity = move * NoClipSpeed * mult;
        isGrounded = false;
        isCrouching = false;
    }

    private void ReadLookInput()
    {
        if (ViewCamera == null || OUTL_DevConsole.IsInputCaptured) return;
        float mx = Input.GetAxisRaw(MouseXAxis) * MouseSensitivity;
        float my = Input.GetAxisRaw(MouseYAxis) * MouseSensitivity;
        yaw += mx;
        pitch = Mathf.Clamp(pitch - my, MinPitch, MaxPitch);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        ViewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void ReadMovementInput()
    {
        inputForward = Mathf.Clamp(Input.GetAxisRaw(VerticalAxis), -1f, 1f);
        inputSide = Mathf.Clamp(Input.GetAxisRaw(HorizontalAxis), -1f, 1f);
        speedHeld = Input.GetKey(SpeedKey);
    }

    private void ReadButtonInput()
    {
        jumpHeld = Input.GetButton(JumpButton);
        if (!jumpHeld)
        {
            jumpLatchedUntilRelease = false;
        }
        else if (!jumpLatchedUntilRelease)
        {
            jumpQueued = true;
            jumpLatchedUntilRelease = true;
        }

        wantsCrouch = Input.GetKey(CrouchKey) || Input.GetKey(AltCrouchKey);
    }

    private void BuildWishVelocity(out Vector3 wishDir, out float wishSpeed)
    {
        float forwardMove = inputForward >= 0f ? inputForward * ScaleSpeed(ForwardSpeed) : inputForward * ScaleSpeed(BackSpeed);
        float sideMove = inputSide * ScaleSpeed(SideSpeed);

        wishDir = transform.forward * forwardMove + transform.right * sideMove;
        wishDir.y = 0f;
        wishSpeed = wishDir.magnitude;

        if (wishSpeed > 0.0001f) wishDir /= wishSpeed;
        else wishDir = Vector3.zero;

        float maxSpeed = ScaleSpeed(speedHeld && HoldShiftToWalk ? WalkSpeed : RunSpeed);
        if (!HoldShiftToWalk && speedHeld) maxSpeed = ScaleSpeed(RunSpeed);
        if (wishSpeed > maxSpeed) wishSpeed = maxSpeed;
        if (isCrouching) wishSpeed *= CrouchSpeedMultiplier;
    }

    private void GroundMove(Vector3 wishDir, float wishSpeed, bool jumpRequest, float dt)
    {
        bool jumpNow = jumpRequest && CanStartGroundJump();
        if (!(jumpNow && SkipFrictionOnJumpFrame)) ApplyFriction(dt);

        if (ProjectMoveOnGroundPlane && hasGroundHit && wishDir.sqrMagnitude > 0.0001f)
            wishDir = BuildGroundWishDir(wishDir);

        Accelerate(wishDir, wishSpeed, GroundAcceleration, dt);

        if (jumpNow)
        {
            velocity.y = ScaleSpeed(JumpSpeed);
            isGrounded = false;
            lastJumpTime = Time.time;
            PlayRandomClip(JumpClips, transform.position);
            if (AnimationBridge != null) AnimationBridge.NotifyJump();
        }
        else if (velocity.y <= Mathf.Max(0.01f, StableGroundUpSpeed))
        {
            ApplyGroundContactVelocity();
        }
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
        if (projected.sqrMagnitude <= 0.0001f)
            return wishDir;
        return projected.normalized;
    }

    private void ApplyGroundContactVelocity()
    {
        float stick = ScaleSpeed(Mathf.Max(0f, GroundStickSpeed));
        if (stick <= 0f)
        {
            velocity.y = Mathf.Min(velocity.y, 0f);
            return;
        }

        if (CancelDownhillSlideOnWalkableGround && hasGroundHit && groundSlopeAngle <= Mathf.Max(SlopeLimit, SlopeSlideStartAngle))
            velocity.y = -stick;
        else if (velocity.y < 0f)
            velocity.y = -stick;
    }

    private void AirMove(Vector3 wishDir, float wishSpeed, float dt)
    {
        float cappedWishSpeed = Mathf.Min(wishSpeed, ScaleSpeed(AirWishSpeedCap));
        Accelerate(wishDir, cappedWishSpeed, AirAcceleration, dt);
        velocity.y -= BuildEffectiveGravity() * dt;
        float maxFall = Mathf.Max(1f, MaxFallSpeed);
        if (velocity.y < -maxFall) velocity.y = -maxFall;
    }

    private float BuildEffectiveGravity()
    {
        float g = OUTL_Cheats.UnityGravity;
        if (g <= 0f) g = ScaleSpeed(Gravity);
        g *= Mathf.Max(0f, GravityMultiplier);
        if (velocity.y > 0f)
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

    private void Accelerate(Vector3 wishDir, float wishSpeed, float accel, float dt)
    {
        if (wishSpeed <= 0f || wishDir.sqrMagnitude <= 0.0001f) return;
        float currentSpeed = Vector3.Dot(velocity, wishDir);
        float addSpeed = wishSpeed - currentSpeed;
        if (addSpeed <= 0f) return;
        float accelSpeed = accel * dt * wishSpeed;
        if (accelSpeed > addSpeed) accelSpeed = addSpeed;
        velocity += wishDir * accelSpeed;
    }

    private void ApplyFriction(float dt)
    {
        Vector3 lateral = velocity;
        lateral.y = 0f;
        float speed = lateral.magnitude;
        if (speed < 0.0001f) return;
        float control = speed < ScaleSpeed(StopSpeed) ? ScaleSpeed(StopSpeed) : speed;
        float drop = control * Friction * dt;
        float newSpeed = Mathf.Max(speed - drop, 0f);
        if (newSpeed != speed)
        {
            newSpeed /= speed;
            velocity.x *= newSpeed;
            velocity.z *= newSpeed;
        }
    }

    private void ProbeGroundForNormalOnly()
    {
        hasGroundHit = false;
        groundNormal = Vector3.up;
        groundSlopeAngle = 0f;

        Vector3 bottomSphereCenter = transform.position + Vector3.up * (CharacterController.radius + 0.02f);
        float distance = Mathf.Max(0.01f, Mathf.Max(GroundProbeExtraDistance, GroundSnapDistance));
        if (Physics.SphereCast(bottomSphereCenter, CharacterController.radius * 0.92f, Vector3.down, out groundHit, distance, GroundMask, GroundTriggerInteraction))
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
        if (velocity.y > Mathf.Max(0.01f, StableGroundUpSpeed)) return false;
        return CharacterController.isGrounded || hasGroundHit;
    }

    private void ClipVelocityFromControllerFlags(CollisionFlags flags)
    {
        if ((flags & CollisionFlags.Above) != 0 && velocity.y > 0f) velocity.y = 0f;
        if ((flags & CollisionFlags.Below) != 0 && velocity.y < 0f) ApplyGroundContactVelocity();
    }

    private void SmoothCrouchAndView(float dt)
    {
        bool shouldCrouch = wantsCrouch || !CanStandUp();
        isCrouching = shouldCrouch;
        float targetHeight = shouldCrouch ? CrouchHeight : StandingHeight;
        float nextHeight = Mathf.Lerp(CharacterController.height, targetHeight, Mathf.Clamp01(dt * CrouchLerpSpeed));
        ApplyControllerShape(nextHeight);

        if (ViewCamera != null)
        {
            Vector3 lp = ViewCamera.transform.localPosition;
            lp.y = Mathf.Lerp(lp.y, shouldCrouch ? CrouchViewHeight : StandingViewHeight, Mathf.Clamp01(dt * ViewHeightLerpSpeed));
            ViewCamera.transform.localPosition = lp;
        }
    }

    private bool CanStandUp()
    {
        if (!isCrouching && !wantsCrouch) return true;
        Vector3 basePos = transform.position + Vector3.up * HullRadius;
        Vector3 topPos = transform.position + Vector3.up * (StandingHeight - HullRadius);
        int count = Physics.OverlapCapsuleNonAlloc(basePos, topPos, HullRadius * 0.95f, uncrouchBuffer, UncrouchBlockMask, UncrouchTriggerInteraction);
        for (int i = 0; i < count; i++)
        {
            Collider c = uncrouchBuffer[i];
            if (c != null && c.gameObject != gameObject && !c.transform.IsChildOf(transform)) return false;
        }
        return true;
    }

    private void ApplyControllerShape(float height)
    {
        CharacterController.height = Mathf.Max(height, HullRadius * 2f + 0.02f);
        CharacterController.radius = HullRadius;
        CharacterController.center = new Vector3(0f, CharacterController.height * 0.5f, 0f);
    }

    private void ProgressFootsteps(float dt)
    {
        Vector3 lateral = velocity;
        lateral.y = 0f;
        float speed = lateral.magnitude;
        if (!isGrounded || speed < MinFootstepSpeed) return;
        accumulatedStepDistance += speed * dt;
        float stepDistance = isCrouching ? CrouchStepDistance : (speed > ScaleSpeed(WalkSpeed + 20f) ? RunStepDistance : WalkStepDistance);
        if (accumulatedStepDistance >= stepDistance)
        {
            accumulatedStepDistance = 0f;
            PlayRandomClip(FootstepClips, hasGroundHit ? groundHit.point : transform.position);
        }
    }

    private void TrackFallSpeed()
    {
        if (velocity.y < 0f)
            maxObservedFallSpeed = Mathf.Max(maxObservedFallSpeed, -velocity.y);
    }

    private void OnLanded(float previousY)
    {
        lastFallSpeed = Mathf.Max(maxObservedFallSpeed, previousY < 0f ? -previousY : 0f);
        lastFallDamage = 0f;
        maxObservedFallSpeed = 0f;

        if (lastFallSpeed >= LandingSoundMinFallSpeed)
            PlayRandomClip(LandingClips, transform.position);

        lastFallDamage = CalculateFallDamage(lastFallSpeed);
        if (lastFallDamage > 0f && Entity != null && Entity.Id.IsValid)
            OUTL_Combat.ApplyDamage(OUTL_EntityId.None, Entity.Id, lastFallDamage, transform.position, FallDamageKey);

        if (AnimationBridge != null) AnimationBridge.NotifyLand(lastFallSpeed, lastFallDamage);
        WriteMotorState();
    }

    private float CalculateFallDamage(float fallSpeed)
    {
        if (!EnableFallDamage) return 0f;
        float min = Mathf.Max(0f, FallDamageMinSpeed);
        if (fallSpeed <= min) return 0f;
        float fatal = Mathf.Max(min + 0.01f, FallDamageFatalSpeed);
        if (fallSpeed >= fatal) return Mathf.Max(0f, FallDamageMaxDamage);
        float damage = (fallSpeed - min) * Mathf.Max(0f, FallDamageScale);
        return Mathf.Clamp(damage, 0f, Mathf.Max(0f, FallDamageMaxDamage));
    }

    private void PlayRandomClip(AudioClip[] clips, Vector3 position)
    {
        if (clips == null || clips.Length == 0) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip != null) OUTL_PoolSystem.PlayClipShared(clip, position);
    }

    private void ScanInteractable()
    {
        currentInteractable = null;
        currentCommandTarget = null;
        if (ViewCamera == null) return;
        Ray ray = new Ray(ViewCamera.transform.position, ViewCamera.transform.forward);
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, UseDistance, UseMask, QueryTriggerInteraction.Collide)) return;
        currentInteractable = hit.collider.GetComponentInParent<OUTL_Interactable>();
        if (currentInteractable != null) return;

        OUTL_EntityAdapter adapter = hit.collider.GetComponentInParent<OUTL_EntityAdapter>();
        if (CanUseEntity(adapter)) currentCommandTarget = adapter;
    }

    private void HandleWeaponSlotInput()
    {
        if (EnableWeaponSlotKeys)
        {
            if (Input.GetKeyDown(PrimarySlotKey)) SelectWeaponSlot(OUTL_EquipmentSlot.Primary);
            if (Input.GetKeyDown(SecondarySlotKey)) SelectWeaponSlot(OUTL_EquipmentSlot.Secondary);
            if (Input.GetKeyDown(MeleeSlotKey)) SelectWeaponSlot(OUTL_EquipmentSlot.Melee);
            if (Input.GetKeyDown(UtilitySlotKey)) SelectWeaponSlot(OUTL_EquipmentSlot.Utility);
        }

        if (!EnableMouseWheelWeaponCycle) return;
        float wheel = Input.GetAxisRaw("Mouse ScrollWheel");
        if (wheel > 0.05f) CycleWeaponSlot(1);
        else if (wheel < -0.05f) CycleWeaponSlot(-1);
    }

    public void SelectWeaponSlot(OUTL_EquipmentSlot slot)
    {
        if (activeWeaponSlot == slot) return;
        activeWeaponSlot = slot;
        if (AnimationBridge != null) AnimationBridge.NotifyWeaponChanged(slot);
        WriteMotorState();
    }

    private void CycleWeaponSlot(int direction)
    {
        int index = 0;
        for (int i = 0; i < WeaponCycleSlots.Length; i++)
        {
            if (WeaponCycleSlots[i] == activeWeaponSlot) { index = i; break; }
        }

        int step = direction >= 0 ? 1 : -1;
        for (int tries = 0; tries < WeaponCycleSlots.Length; tries++)
        {
            index = (index + step + WeaponCycleSlots.Length) % WeaponCycleSlots.Length;
            OUTL_EquipmentSlot candidate = WeaponCycleSlots[index];
            if (HasUsableWeaponSlot(candidate))
            {
                SelectWeaponSlot(candidate);
                return;
            }
        }
    }

    private bool HasUsableWeaponSlot(OUTL_EquipmentSlot slot)
    {
        if (AttackDriver == null) return false;
        switch (slot)
        {
            case OUTL_EquipmentSlot.Primary: return AttackDriver.Primary != null;
            case OUTL_EquipmentSlot.Secondary: return AttackDriver.Secondary != null;
            case OUTL_EquipmentSlot.Melee: return AttackDriver.Melee != null;
        }
        return false;
    }

    public string GetCurrentUseDisplayName()
    {
        if (currentInteractable != null) return currentInteractable.GetDisplayName();
        if (currentCommandTarget == null || currentCommandTarget.Runtime == null) return string.Empty;
        OUTL_EntityRuntime runtime = currentCommandTarget.Runtime;
        if (runtime.Def != null && !string.IsNullOrEmpty(runtime.Def.DisplayName)) return runtime.Def.DisplayName;
        if (!string.IsNullOrEmpty(runtime.ClassName)) return runtime.ClassName;
        return currentCommandTarget.name;
    }

    private static bool CanUseEntity(OUTL_EntityAdapter adapter)
    {
        if (adapter == null || adapter.Runtime == null || !adapter.Id.IsValid) return false;
        OUTL_World world = OUTL_World.Instance;
        OUTL_Command command = new OUTL_Command(OUTL_CommandType.Use, OUTL_EntityId.None, adapter.Id);
        OUTL_ICommandReceiver[] receivers = adapter.CommandReceivers;
        for (int i = 0; i < receivers.Length; i++)
            if (receivers[i] != null && receivers[i].OUTL_CanReceive(command, world))
                return true;
        return false;
    }

    private void HandleActionInput()
    {
        if (Input.GetKeyDown(UseKey))
        {
            OUTL_EntityId source = Entity != null ? Entity.Id : OUTL_EntityId.None;
            if (currentInteractable != null)
                currentInteractable.Use(source);
            else if (currentCommandTarget != null && OUTL_World.Instance != null)
                OUTL_World.Instance.Commands.Send(new OUTL_Command(OUTL_CommandType.Use, source, currentCommandTarget.Id) { Point = currentCommandTarget.transform.position });
        }

        if (AttackDriver != null)
        {
            if (Input.GetKeyDown(PrimaryFireKey)) FireSelectedWeapon();
            if (Input.GetKeyDown(SecondaryFireKey)) FireWeaponSlot(OUTL_EquipmentSlot.Secondary);
            if (Input.GetKeyDown(MeleeKey)) FireWeaponSlot(OUTL_EquipmentSlot.Melee);
        }
    }

    private bool FireSelectedWeapon()
    {
        return FireWeaponSlot(activeWeaponSlot);
    }

    private bool FireWeaponSlot(OUTL_EquipmentSlot slot)
    {
        if (AttackDriver == null) return false;
        bool fired = false;
        switch (slot)
        {
            case OUTL_EquipmentSlot.Primary: fired = AttackDriver.FirePrimary(); break;
            case OUTL_EquipmentSlot.Secondary: fired = AttackDriver.FireSecondary(); break;
            case OUTL_EquipmentSlot.Melee: fired = AttackDriver.FireMelee(); break;
        }

        if (fired)
        {
            activeWeaponSlot = slot;
            if (AnimationBridge != null) AnimationBridge.NotifyAttack(slot);
            WriteMotorState();
        }
        return fired;
    }

    private void WriteMotorState()
    {
        if (!WriteMotorStateToEntity || Entity == null || Entity.Runtime == null) return;
        Entity.Runtime.State.SetFlag("Player.Grounded", isGrounded);
        Entity.Runtime.State.SetFlag("Player.Crouching", isCrouching);
        Entity.Runtime.State.SetFloat("Player.Speed", HorizontalSpeed);
        Entity.Runtime.State.SetFloat("Player.VerticalSpeed", velocity.y);
        Entity.Runtime.State.SetFloat("Player.Slope", groundSlopeAngle);
        Entity.Runtime.State.SetFloat("Player.FallSpeed", lastFallSpeed);
        Entity.Runtime.State.SetFloat("Player.FallDamage", lastFallDamage);
        Entity.Runtime.State.SetString("Player.ActiveWeaponSlot", activeWeaponSlot.ToString());
    }

    private float ScaleSpeed(float value)
    {
        return UseGoldSrcUnits ? value / Mathf.Max(1f, GoldSrcUnitsPerUnityUnit) : value;
    }

    private static float NormalizePitch(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}
