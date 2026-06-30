using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class OUTL_FPS_Controller : MonoBehaviour, OUTL_IActorInputPhasedSink, OUTL_IPoolReset
{
    public OUTL_EntityAdapter Entity;
    public OUTL_AttackDriver AttackDriver;
    public OUTL_EquipmentRuntime EquipmentRuntime;
    public OUTL_CharacterAnimationBridge AnimationBridge;
    public OUTL_PlayerFeedback Feedback;
    public CharacterController Controller;
    public Transform YawRoot;
    public Transform ViewPitchRoot;
    public Camera ViewCamera;

    [Header("OUTL Input")]
    public bool UseUnityInputFallback = true;
    public bool DisableFallbackWhenActorBridgePresent = true;
    public OUTL_ActorControlBridge ActorBridge;

    [Header("Unity Input Fallback")]
    public string HorizontalAxis = "Horizontal";
    public string VerticalAxis = "Vertical";
    public string MouseXAxis = "Mouse X";
    public string MouseYAxis = "Mouse Y";
    public string JumpButton = "Jump";
    public KeyCode CrouchKey = KeyCode.LeftControl;
    public KeyCode AltCrouchKey = KeyCode.C;
    public KeyCode SpeedKey = KeyCode.LeftShift;
    public KeyCode UseKey = KeyCode.E;
    public KeyCode PrimaryFireKey = KeyCode.Mouse0;
    public KeyCode SecondaryFireKey = KeyCode.Mouse1;
    public KeyCode MeleeKey = KeyCode.V;

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

    [Header("Visual Feel")]
    public bool EnableViewBob = true;
    public float WalkBobFrequency = 8f;
    public float RunBobFrequency = 12f;
    public float BobHorizontal = 0.035f;
    public float BobVertical = 0.055f;
    public float BobReturnSpeed = 12f;
    public float StrafeRollDegrees = 2.25f;
    public float VelocityRollDegrees = 1.25f;
    public float ViewRollLerpSpeed = 10f;
    public float LandingViewDip = 0.08f;
    public float LandingViewDipReturnSpeed = 10f;
    public float CameraFovRunAdd = 4f;
    public float CameraFovLerpSpeed = 8f;

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
    public float Gravity = 981f;
    public float GravityMultiplier = 1.65f;
    public float RisingGravityMultiplier = 1.0f;
    public float FallingGravityMultiplier = 1.75f;
    public float LowJumpGravityMultiplier = 2.25f;
    public bool ApplyLowJumpGravityWhenJumpReleased = true;
    public float MaxFallSpeed = 54f;
    public float JumpSpeed = 270f;
    public bool UseGoldSrcJumpHeight = true;
    public float NormalJumpVerticalHeightHU = 45f;
    public float GoldSrcGravityHU = 800f;
    public bool SkipFrictionOnJumpFrame = true;
    public float JumpGroundLockout = 0.12f;
    public float GroundStickSpeed = 8f;
    public float StableGroundUpSpeed = 2.5f;

    [Header("Input Forgiveness")]
    [Min(0f)] public float JumpBufferSeconds = 0.12f;
    [Min(0f)] public float GroundCoyoteSeconds = 0.10f;

    [Header("GoldSrc Long Jump")]
    public bool EnableLongJump = true;
    public bool RequireLongJumpModule = true;
    public bool HasLongJumpModule = false;
    public float LongJumpRequiredSpeedHU = 50f;
    public float LongJumpHorizontalSpeedHU = 560f;
    public float LongJumpVerticalHeightHU = 56f;
    public bool LongJumpRequiresCrouch = true;
    public bool LongJumpRequiresCrouchHeld = true;
    public bool LongJumpRequiresCrouchTransition = true;
    public float CrouchTransitionGraceSeconds = 0.40f;

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
    public bool ProjectMoveOnGroundPlane = true;
    public bool CancelDownhillSlideOnWalkableGround = true;
    public float SlopeSlideStartAngle = 50f;

    [Header("OUT SurfaceData")]
    public OUT_SurfaceData DefaultSurfaceData;
    public OUT_SurfaceData[] KnownSurfaces;
    public OUT_SurfaceData CurrentSurfaceData;
    public float MinimumFriction = 0.15f;
    public float MaximumFriction = 1.0f;
    public float DefaultFriction = 0.95f;
    public float SurfaceProbeDistance = 0.28f;
    public bool ApplySurfaceFriction = true;
    public bool ApplySurfaceHazards = true;

    [Header("Ladder")]
    public bool EnableLadders = true;
    public LayerMask LadderMask = 0;
    public string LadderTag = "Ladder";
    public float LadderDetectDistance = 0.65f;
    public float LadderClimbSpeedHU = 200f;
    public float LadderSideSpeedHU = 120f;
    public float LadderDetachJumpSpeedHU = 270f;
    public float LadderDetachPushSpeedHU = 180f;
    public bool AutoAttachLadderWhenMovingIntoIt = true;
    public bool UseKeyAttachesLadder = false;
    [Tooltip("Half-Life style: W climbs up by default, S climbs down; looking sharply down with W lets you descend.")]
    public bool HalfLifeLadderInput = true;
    [Tooltip("Small inward velocity used to keep the controller attached to the ladder plane without launching it.")]
    public float LadderStickSpeedHU = 24f;
    [Tooltip("How far the player probe may drift from the cached ladder before ladder mode is forcefully released.")]
    public float LadderReleaseDistance = 0.95f;
    [Range(0f, 0.95f)] public float LadderInputDeadZone = 0.08f;

    [Header("Ledge Grab")]
    public bool EnableLedgeGrab = true;

    [Tooltip("Surfaces that can be detected as ledges/walls for ledge grab.")]
    public LayerMask LedgeMask = ~0;

    [Tooltip("Solid geometry that blocks the player capsule when checking ledge hang/stand positions.")]
    public LayerMask LedgeBlockMask = ~0;
    public float LedgeForwardCheckDistance = 0.55f;
    public float LedgeChestHeight = 1.20f;
    public float LedgeHeadClearanceHeight = 1.75f;
    public float LedgeTopProbeDownDistance = 1.20f;
    public float LedgeHangOffsetBack = 0.28f;
    public float LedgeHangOffsetDown = 0.65f;
    public float LedgeStandForwardOffset = 0.35f;
    public float LedgeClimbDuration = 0.28f;
    public KeyCode LedgeDropKey = KeyCode.LeftControl;
    public bool AutoGrabLedges = true;

    [Header("Interaction")]
    public float UseDistance = 3f;
    public LayerMask UseMask = ~0;
    [Tooltip("Small forgiving probe around the center ray. Keeps USE stable on thin buttons and moving doors without allocating.")]
    public float UseProbeRadius = 0.055f;
    [Tooltip("Short grace against one-frame ray misses on mesh seams/edge hits.")]
    public float UseStickySeconds = 0.10f;
    public bool HideBusyInteractables = true;
    public OUTL_EquipmentSlot ActiveWeaponDefaultSlot = OUTL_EquipmentSlot.Primary;

    [Header("Audio")]
    public AudioSource FootstepSource;
    public AudioSource JumpSource;
    public AudioSource LandingSource;
    public AudioSource SurfaceHazardSource;
    public AudioClip[] FallbackFootstepClips;
    public AudioClip[] FallbackJumpClips;
    public AudioClip[] FallbackLandingClips;
    public float WalkStepDistance = 2.25f;
    public float RunStepDistance = 2.9f;
    public float CrouchStepDistance = 1.65f;
    public float MinFootstepSpeed = 0.8f;
    public float LandingSoundMinFallSpeed = 5f;
    public AnimationCurve FootstepVolumeCurve;

    [Header("Fall Damage")]
    public bool EnableFallDamage = true;
    public float FallDamageMinSpeed = 18f;
    public float FallDamageFatalSpeed = 32f;
    public float FallDamageScale = 7f;
    public float FallDamageMaxDamage = 100f;
    public string FallDamageKey = "fall";

    [Header("Runtime State")]
    public bool WriteMotorStateToEntity = true;
    public Vector3 Velocity;
    public bool IsGrounded;
    public bool IsCrouching;
    public bool IsOnLadder;
    public bool IsHangingFromLedge;
    public bool IsClimbingLedge;
    public float LastFallSpeed;
    public float LastFallDamage;

    private static readonly OUTL_EquipmentSlot[] WeaponCycleSlots =
    {
        OUTL_EquipmentSlot.Primary,
        OUTL_EquipmentSlot.Secondary,
        OUTL_EquipmentSlot.Melee
    };

    private readonly Collider[] uncrouchBuffer = new Collider[24];
    private readonly Collider[] ledgeClearanceBuffer = new Collider[24];
    private readonly OUT_SurfaceManager[] triggerSurfaces = new OUT_SurfaceManager[16];
    private readonly RaycastHit[] interactionHitBuffer = new RaycastHit[16];

    private float yaw;
    private float pitch;
    private float baseFov;
    private Vector3 baseViewLocalPosition;
    private float bobPhase;
    private float currentRoll;
    private float landingDip;
    private bool initializedView;
    private bool wasGrounded;
    private bool hasGroundHit;
    private Vector3 groundNormal = Vector3.up;
    private float groundSlopeAngle;
    private RaycastHit groundHit;
    private float currentSurfaceFriction = 1f;
    private float jumpPressedTime = -999f;
    private float lastGroundedTime = -999f;
    private bool bufferedJumpConsumed;
    private bool lastCrouchHeld;
    private float crouchPressedTime = -999f;
    private float lastJumpTime = -999f;
    private float previousVerticalVelocity;
    private float maxObservedFallSpeed;
    private float accumulatedStepDistance;
    private OUTL_Interactable currentInteractable;
    private OUTL_EntityAdapter currentCommandTarget;
    private OUTL_Interactable stickyInteractable;
    private OUTL_EntityAdapter stickyCommandTarget;
    private float stickyUseUntil;
    private OUTL_EquipmentSlot activeWeaponSlot;
    private bool wasNoClip;
    private int lastBridgeFrame = -1;
    private Vector3 ladderNormal;
    private Collider currentLadder;
    private Vector3 ledgeHangPosition;
    private Vector3 ledgeStandPosition;
    private Vector3 ledgeNormal;
    private Vector3 ledgeClimbStartPosition;
    private float ledgeClimbStartTime;
    private float nextSurfaceDamageTime;
    private float nextSurfaceAudioTime;

    public OUTL_ActorInputPhase Phase { get { return OUTL_ActorInputPhase.Movement; } }
    public OUTL_Interactable CurrentInteractable { get { return currentInteractable; } }
    public OUTL_EntityAdapter CurrentCommandTarget { get { return currentCommandTarget; } }
    public bool HasUsableFocus { get { return currentInteractable != null || currentCommandTarget != null; } }
    public float HorizontalSpeed { get { Vector3 v = Velocity; v.y = 0f; return v.magnitude; } }
    public float ViewPitch { get { return pitch; } }
    public float ViewYaw { get { return yaw; } }
    public float GroundSlopeAngle { get { return groundSlopeAngle; } }
    public Vector3 GroundNormal { get { return groundNormal; } }
    public OUTL_EquipmentSlot ActiveWeaponSlot { get { return activeWeaponSlot; } }

    private void Awake()
    {
        Resolve();
        ApplyControllerDefaults();
        CaptureViewDefaults();
        activeWeaponSlot = ActiveWeaponDefaultSlot;
    }

    private void OnEnable()
    {
        if (LockCursor && !OUTL_DevConsole.IsInputCaptured)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        jumpPressedTime = -999f;
        lastGroundedTime = -999f;
        bufferedJumpConsumed = true;
    }

    private void OnValidate()
    {
        GoldSrcUnitsPerUnityUnit = Mathf.Max(1f, GoldSrcUnitsPerUnityUnit);
        StandingHeight = Mathf.Max(0.5f, StandingHeight);
        HullRadius = Mathf.Max(0.05f, HullRadius);
        CrouchHeight = Mathf.Clamp(CrouchHeight, HullRadius * 2f + 0.02f, StandingHeight);
        SurfaceProbeDistance = Mathf.Max(0.01f, SurfaceProbeDistance);
        LedgeClimbDuration = Mathf.Max(0.01f, LedgeClimbDuration);
    }

    private void Update()
    {
        if (!UseUnityInputFallback) return;
        if (DisableFallbackWhenActorBridgePresent && ResolveBridge() != null && ActorBridge.isActiveAndEnabled) return;
        if (lastBridgeFrame == Time.frameCount) return;

        OUTL_ActorInputFrame frame = BuildUnityInputFrame();
        OUTL_ApplyInput(frame, OUTL_World.Instance);
    }

    public void OUTL_ApplyInput(in OUTL_ActorInputFrame frame, OUTL_World world)
    {
        lastBridgeFrame = Time.frameCount;
        Resolve();
        if (Controller == null || IsDead()) return;

        float dt = ReadDeltaTime(frame, world);
        if (dt <= 0f) return;

        if (OUTL_DevConsole.IsInputCaptured)
        {
            WriteMotorState();
            return;
        }

        if (Entity != null && OUTL_Cheats.IsNoClipEntity(Entity.Id))
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

        TrackBufferedInputs(frame);
        ApplyLook(frame);
        SmoothCrouchAndView(frame.CrouchHeld, dt);
        ScanInteractable();
        HandleWeaponSlotInput(frame);
        HandleActionInput(frame);
        TickSurfaceHazards(dt);

        if (IsClimbingLedge)
        {
            UpdateLedgeClimb(dt);
            UpdateViewFeel(frame, dt);
            WriteMotorState();
            return;
        }

        if (IsHangingFromLedge)
        {
            UpdateLedgeHang(frame);
            UpdateViewFeel(frame, dt);
            WriteMotorState();
            return;
        }

        bool wasGroundedAtFrameStart = IsGrounded;
        bool landedThisFrame = false;
        ProbeGround();
        wasGrounded = wasGroundedAtFrameStart;
        IsGrounded = CanUseGroundBeforeMove();
        if (!wasGroundedAtFrameStart && IsGrounded && maxObservedFallSpeed > 0.01f)
        {
            OnLanded(Velocity.y);
            landedThisFrame = true;
        }
        if (IsGrounded) lastGroundedTime = Time.time;

        if (EnableLadders)
        {
            if (IsOnLadder || ShouldAttachLadder(frame))
            {
                LadderMove(frame, dt);
                UpdateViewFeel(frame, dt);
                WriteMotorState();
                return;
            }
        }

        Vector3 wishDir;
        float wishSpeed;
        BuildWishVelocity(frame, out wishDir, out wishSpeed);

        bool canJump = HasBufferedJump() && CanStartGroundJump() && (IsGrounded || HasCoyoteGround());
        if (IsGrounded)
            GroundMove(wishDir, wishSpeed, canJump, frame.JumpHeld, dt);
        else
        {
            if (canJump) StartJump(frame.JumpHeld);
            else
            {
                AirMove(wishDir, wishSpeed, frame.JumpHeld, dt);
                TrackFallSpeed();
            }
        }

        if (!IsGrounded && EnableLedgeGrab && TryAutoGrabLedge(frame))
        {
            UpdateViewFeel(frame, dt);
            WriteMotorState();
            return;
        }

        previousVerticalVelocity = Velocity.y;
        CollisionFlags flags = Controller.Move(Velocity * dt);
        ClipVelocityFromControllerFlags(flags);

        bool groundedAfterMove = (flags & CollisionFlags.Below) != 0;
        bool landed = !wasGroundedAtFrameStart && !landedThisFrame && groundedAfterMove;
        IsGrounded = groundedAfterMove || (IsGrounded && Velocity.y <= 0f);
        if (IsGrounded) lastGroundedTime = Time.time;
        if (!IsGrounded) TrackFallSpeed();
        if (landed) OnLanded(previousVerticalVelocity);

        ProgressFootsteps(dt);
        UpdateViewFeel(frame, dt);
        if (AnimationBridge != null)
            AnimationBridge.PushLocomotionValues(transform, Velocity, ViewPitch, ViewYaw, groundSlopeAngle, LastFallSpeed, LastFallDamage, IsGrounded, IsCrouching, activeWeaponSlot);
        WriteMotorState();
    }

    public void OUTL_OnPoolSpawn()
    {
        ResetRuntimeState();
        Resolve();
        ApplyControllerDefaults();
        CaptureViewDefaults();
    }

    public void OUTL_OnPoolRelease()
    {
        ResetRuntimeState();
    }

    private void ResetRuntimeState()
    {
        Velocity = Vector3.zero;
        IsGrounded = false;
        IsCrouching = false;
        IsOnLadder = false;
        IsHangingFromLedge = false;
        IsClimbingLedge = false;
        LastFallSpeed = 0f;
        LastFallDamage = 0f;
        maxObservedFallSpeed = 0f;
        bufferedJumpConsumed = true;
        lastBridgeFrame = -1;
        currentLadder = null;
    }

    private void Resolve()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (AttackDriver == null) AttackDriver = GetComponent<OUTL_AttackDriver>();
        if (EquipmentRuntime == null) EquipmentRuntime = GetComponent<OUTL_EquipmentRuntime>();
        if (AnimationBridge == null) AnimationBridge = GetComponentInChildren<OUTL_CharacterAnimationBridge>(true);
        if (Feedback == null) Feedback = GetComponent<OUTL_PlayerFeedback>();
        if (Controller == null) Controller = GetComponent<CharacterController>();
        if (YawRoot == null) YawRoot = transform;
        if (ViewCamera == null) ViewCamera = GetComponentInChildren<Camera>(true);
        if (ViewPitchRoot == null && ViewCamera != null) ViewPitchRoot = ViewCamera.transform;
        if (FootstepSource == null) FootstepSource = GetComponent<AudioSource>();
        if (JumpSource == null) JumpSource = FootstepSource;
        if (LandingSource == null) LandingSource = FootstepSource;
        if (SurfaceHazardSource == null) SurfaceHazardSource = FootstepSource;
    }

    private OUTL_ActorControlBridge ResolveBridge()
    {
        if (ActorBridge == null) ActorBridge = GetComponent<OUTL_ActorControlBridge>();
        return ActorBridge;
    }

    private void ApplyControllerDefaults()
    {
        if (Controller == null) return;
        Controller.radius = HullRadius;
        Controller.slopeLimit = SlopeLimit;
        Controller.stepOffset = StepOffset;
        ApplyControllerShape(IsCrouching ? CrouchHeight : StandingHeight);
        yaw = YawRoot != null ? YawRoot.eulerAngles.y : transform.eulerAngles.y;
        pitch = ViewPitchRoot != null ? NormalizePitch(ViewPitchRoot.localEulerAngles.x) : 0f;
    }

    private void CaptureViewDefaults()
    {
        if (ViewPitchRoot != null)
        {
            baseViewLocalPosition = ViewPitchRoot.localPosition;
            baseViewLocalPosition.y = StandingViewHeight;
        }
        if (ViewCamera != null) baseFov = ViewCamera.fieldOfView;
        initializedView = true;
    }

    private OUTL_ActorInputFrame BuildUnityInputFrame()
    {
        OUTL_ActorInputFrame frame = OUTL_ActorInputFrame.Empty(Time.time);
        frame.DeltaTime = Time.deltaTime;
        if (OUTL_DevConsole.IsInputCaptured) return frame;

        frame.Move = new Vector2(
            Mathf.Clamp(Input.GetAxisRaw(HorizontalAxis), -1f, 1f),
            Mathf.Clamp(Input.GetAxisRaw(VerticalAxis), -1f, 1f));

        frame.Look = new Vector2(
            Input.GetAxisRaw(MouseXAxis) * MouseSensitivity,
            Input.GetAxisRaw(MouseYAxis) * MouseSensitivity);

        frame.JumpPressed = Input.GetButtonDown(JumpButton);
        frame.JumpHeld = Input.GetButton(JumpButton);

        frame.CrouchHeld = Input.GetKey(CrouchKey) || Input.GetKey(AltCrouchKey);
        frame.SprintHeld = Input.GetKey(SpeedKey);

        frame.UsePressed = Input.GetKeyDown(UseKey);

        frame.FirePrimaryPressed = Input.GetKeyDown(PrimaryFireKey);
        frame.FirePrimaryHeld = Input.GetKey(PrimaryFireKey);
        frame.FireSecondaryPressed = Input.GetKeyDown(SecondaryFireKey);

        frame.MeleePressed = Input.GetKeyDown(MeleeKey);
        frame.LedgeDropPressed = Input.GetKeyDown(LedgeDropKey);

        float wheel = Input.GetAxisRaw("Mouse ScrollWheel");
        if (wheel > 0.05f) frame.WeaponCycle = 1;
        else if (wheel < -0.05f) frame.WeaponCycle = -1;

        frame.FireAuthorized =
            frame.FirePrimaryPressed ||
            frame.FirePrimaryHeld ||
            frame.FireSecondaryPressed ||
            frame.MeleePressed;

        return frame;
    }

    private void TrackBufferedInputs(in OUTL_ActorInputFrame frame)
    {
        if (frame.JumpPressed)
        {
            jumpPressedTime = Time.time;
            bufferedJumpConsumed = false;
        }

        if (frame.CrouchHeld && !lastCrouchHeld) crouchPressedTime = Time.time;
        lastCrouchHeld = frame.CrouchHeld;
    }

    private float ReadDeltaTime(in OUTL_ActorInputFrame frame, OUTL_World world)
    {
        if (frame.DeltaTime > 0f) return frame.DeltaTime;
        if (world != null && world.DeltaTime > 0f) return world.DeltaTime;
        return Time.deltaTime;
    }

    private void ApplyLook(in OUTL_ActorInputFrame frame)
    {
        Transform yawRoot = YawRoot != null ? YawRoot : transform;
        yaw += frame.Look.x;
        pitch = Mathf.Clamp(pitch - frame.Look.y, MinPitch, MaxPitch);
        yawRoot.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private void SmoothCrouchAndView(bool crouchHeld, float dt)
    {
        bool shouldCrouch = crouchHeld || !CanStandUp();
        IsCrouching = shouldCrouch;
        float targetHeight = shouldCrouch ? CrouchHeight : StandingHeight;
        float nextHeight = Mathf.Lerp(Controller.height, targetHeight, Mathf.Clamp01(dt * CrouchLerpSpeed));
        ApplyControllerShape(nextHeight);

        if (!initializedView) CaptureViewDefaults();
        float targetViewHeight = shouldCrouch ? CrouchViewHeight : StandingViewHeight;
        baseViewLocalPosition.y = Mathf.Lerp(baseViewLocalPosition.y, targetViewHeight, Mathf.Clamp01(dt * ViewHeightLerpSpeed));
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
            if (IsBlockingCollider(c)) return false;
        }
        return true;
    }

    private void ApplyControllerShape(float height)
    {
        Controller.height = Mathf.Max(height, HullRadius * 2f + 0.02f);
        Controller.radius = HullRadius;
        Controller.center = new Vector3(0f, Controller.height * 0.5f, 0f);
    }

    private void ProbeGround()
    {
        hasGroundHit = false;
        groundNormal = Vector3.up;
        groundSlopeAngle = 0f;
        CurrentSurfaceData = DefaultSurfaceData;
        currentSurfaceFriction = DefaultFriction;

        float radius = Controller != null ? Controller.radius : HullRadius;
        Vector3 bottomSphereCenter = transform.position + Vector3.up * (radius + 0.02f);
        float distance = Mathf.Max(0.01f, Mathf.Max(GroundProbeExtraDistance, GroundSnapDistance) + SurfaceProbeDistance);
        if (Physics.SphereCast(bottomSphereCenter, radius * 0.92f, Vector3.down, out groundHit, distance, GroundMask, GroundTriggerInteraction))
        {
            float slope = Vector3.Angle(groundHit.normal, Vector3.up);
            if (slope <= SlopeLimit + 0.5f)
            {
                hasGroundHit = true;
                groundNormal = groundHit.normal;
                groundSlopeAngle = slope;
                ResolveSurfaceFromCollider(groundHit.collider);
            }
        }
    }

    private void ResolveSurfaceFromCollider(Collider c)
    {
        OUT_SurfaceData data = null;
        if (c != null)
        {
            OUT_SurfaceManager manager = c.GetComponent<OUT_SurfaceManager>();
            if (manager == null) manager = c.GetComponentInParent<OUT_SurfaceManager>();
            if (manager != null) data = manager.surfaceData;
            if (data == null) data = ResolveSurfaceByPhysicMaterial(c.sharedMaterial);
        }

        CurrentSurfaceData = data != null ? data : DefaultSurfaceData;
        if (CurrentSurfaceData != null && CurrentSurfaceData.physicMaterial != null)
            currentSurfaceFriction = Mathf.Clamp(CurrentSurfaceData.physicMaterial.dynamicFriction, MinimumFriction, MaximumFriction);
        else
            currentSurfaceFriction = DefaultFriction;
    }

    private OUT_SurfaceData ResolveSurfaceByPhysicMaterial(PhysicMaterial material)
    {
        if (material == null || KnownSurfaces == null) return null;
        for (int i = 0; i < KnownSurfaces.Length; i++)
        {
            OUT_SurfaceData data = KnownSurfaces[i];
            if (data != null && data.physicMaterial == material) return data;
        }
        return null;
    }

    private bool CanUseGroundBeforeMove()
    {
        if (Time.time - lastJumpTime < JumpGroundLockout) return false;
        if (Velocity.y > Mathf.Max(0.01f, StableGroundUpSpeed)) return false;
        return Controller.isGrounded || hasGroundHit;
    }

    private bool HasBufferedJump()
    {
        return !bufferedJumpConsumed && Time.time - jumpPressedTime <= JumpBufferSeconds;
    }

    private bool HasCoyoteGround()
    {
        return Time.time - lastGroundedTime <= GroundCoyoteSeconds;
    }

    private bool CanStartGroundJump()
    {
        return Time.time - lastJumpTime >= JumpGroundLockout;
    }

    private void BuildWishVelocity(in OUTL_ActorInputFrame frame, out Vector3 wishDir, out float wishSpeed)
    {
        Vector2 moveInput = Vector2.ClampMagnitude(frame.Move, 1f);
        Transform yawRoot = YawRoot != null ? YawRoot : transform;
        float forwardMove = moveInput.y >= 0f ? moveInput.y * ScaleSpeed(ForwardSpeed) : moveInput.y * ScaleSpeed(BackSpeed);
        float sideMove = moveInput.x * ScaleSpeed(SideSpeed);

        wishDir = yawRoot.forward * forwardMove + yawRoot.right * sideMove;
        wishDir.y = 0f;
        wishSpeed = wishDir.magnitude;
        if (wishSpeed > 0.0001f) wishDir /= wishSpeed;
        else wishDir = Vector3.zero;

        float maxSpeed = ScaleSpeed(frame.SprintHeld && HoldShiftToWalk ? WalkSpeed : RunSpeed);
        if (!HoldShiftToWalk && frame.SprintHeld) maxSpeed = ScaleSpeed(RunSpeed);
        if (wishSpeed > maxSpeed) wishSpeed = maxSpeed;
        if (IsCrouching) wishSpeed *= Mathf.Max(0.01f, CrouchSpeedMultiplier);
    }

    private void GroundMove(Vector3 wishDir, float wishSpeed, bool jumpNow, bool jumpHeld, float dt)
    {
        if (!(jumpNow && SkipFrictionOnJumpFrame)) ApplyFriction(dt);

        if (ProjectMoveOnGroundPlane && hasGroundHit && wishDir.sqrMagnitude > 0.0001f)
            wishDir = BuildGroundWishDir(wishDir);

        Accelerate(wishDir, wishSpeed, GroundAcceleration, dt);

        if (jumpNow)
        {
            StartJump(jumpHeld);
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

    private void StartJump(bool jumpHeld)
    {
        bufferedJumpConsumed = true;
        IsGrounded = false;
        lastJumpTime = Time.time;

        if (ShouldStartLongJump())
        {
            Vector3 f = YawRoot != null ? YawRoot.forward : transform.forward;
            f.y = 0f;
            if (f.sqrMagnitude < 0.0001f) f = transform.forward;
            f.Normalize();
            float horizontal = ScaleSpeed(LongJumpHorizontalSpeedHU);
            Velocity.x = f.x * horizontal;
            Velocity.z = f.z * horizontal;
            Velocity.y = ScaleSpeed(Mathf.Sqrt(2f * GoldSrcGravityHU * LongJumpVerticalHeightHU));
        }
        else
        {
            Velocity.y = UseGoldSrcJumpHeight
                ? ScaleSpeed(Mathf.Sqrt(2f * GoldSrcGravityHU * NormalJumpVerticalHeightHU))
                : ScaleSpeed(JumpSpeed);
        }

        PlaySurfaceClip(CurrentSurfaceData != null ? CurrentSurfaceData.JumpSounds : null, FallbackJumpClips, JumpSource, transform.position, 1f);
        if (Feedback != null) Feedback.OnJump(Mathf.Clamp01(HorizontalSpeed / Mathf.Max(0.01f, ScaleSpeed(RunSpeed))));
        if (AnimationBridge != null) AnimationBridge.NotifyJump();
    }

    private bool ShouldStartLongJump()
    {
        if (!EnableLongJump) return false;
        if (RequireLongJumpModule && !HasLongJumpModule) return false;
        if (LongJumpRequiresCrouch && !IsCrouching && Time.time - crouchPressedTime > CrouchTransitionGraceSeconds) return false;
        if (LongJumpRequiresCrouchHeld && !lastCrouchHeld) return false;
        if (LongJumpRequiresCrouchTransition && Time.time - crouchPressedTime > CrouchTransitionGraceSeconds) return false;
        return HorizontalSpeed * Mathf.Max(1f, GoldSrcUnitsPerUnityUnit) > LongJumpRequiredSpeedHU;
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

    private void Accelerate(Vector3 wishDir, float wishSpeed, float accel, float dt)
    {
        if (wishSpeed <= 0f || wishDir.sqrMagnitude <= 0.0001f) return;
        float currentSpeed = Vector3.Dot(Velocity, wishDir);
        float addSpeed = wishSpeed - currentSpeed;
        if (addSpeed <= 0f) return;
        float accelSpeed = accel * dt * wishSpeed;
        if (accelSpeed > addSpeed) accelSpeed = addSpeed;
        Velocity += wishDir * accelSpeed;
    }

    private void ApplyFriction(float dt)
    {
        Vector3 lateral = Velocity;
        lateral.y = 0f;
        float speed = lateral.magnitude;
        if (speed < 0.0001f) return;

        float surface = ApplySurfaceFriction ? Mathf.InverseLerp(MinimumFriction, MaximumFriction, currentSurfaceFriction) : 1f;
        float friction = Friction * Mathf.Lerp(0.35f, 1.35f, surface);
        float control = speed < ScaleSpeed(StopSpeed) ? ScaleSpeed(StopSpeed) : speed;
        float drop = control * friction * dt;
        float newSpeed = Mathf.Max(speed - drop, 0f);
        if (newSpeed == speed) return;
        newSpeed /= speed;
        Velocity.x *= newSpeed;
        Velocity.z *= newSpeed;
    }

    private float BuildEffectiveGravity(bool jumpHeld)
    {
        float g = OUTL_Cheats.UnityGravity;
        if (g <= 0f) g = ScaleSpeed(Gravity);
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

    private void ClipVelocityFromControllerFlags(CollisionFlags flags)
    {
        if ((flags & CollisionFlags.Above) != 0 && Velocity.y > 0f) Velocity.y = 0f;
        if ((flags & CollisionFlags.Below) != 0 && Velocity.y < 0f) ApplyGroundContactVelocity();
    }

    private bool ShouldAttachLadder(in OUTL_ActorInputFrame frame)
    {
        RaycastHit hit;
        if (!DetectLadder(out hit)) return false;
        Vector3 forward = YawRoot != null ? YawRoot.forward : transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.0001f) forward.Normalize();
        bool facingLadder = Vector3.Dot(forward, -hit.normal) > 0.45f;
        bool movingInto = AutoAttachLadderWhenMovingIntoIt && frame.Move.y > 0.1f && facingLadder;
        bool useAttach = UseKeyAttachesLadder && frame.UsePressed;
        if (!movingInto && !useAttach) return false;
        AttachLadder(hit);
        return true;
    }

    private bool DetectLadder(out RaycastHit hit)
    {
        Transform basis = YawRoot != null ? YawRoot : transform;
        Vector3 origin = transform.position + Vector3.up * Mathf.Clamp(StandingViewHeight * 0.75f, 0.7f, StandingHeight);
        Vector3 direction = basis.forward;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) direction = transform.forward;
        direction.Normalize();
        int mask = LadderMask.value != 0 ? LadderMask.value : ~0;
        if (!Physics.SphereCast(origin, HullRadius * 0.65f, direction, out hit, LadderDetectDistance, mask, QueryTriggerInteraction.Collide))
            return false;
        return IsLadderCollider(hit.collider) && Vector3.Angle(hit.normal, Vector3.up) > 55f;
    }

    private bool IsLadderCollider(Collider c)
    {
        if (c == null) return false;
        if (!string.IsNullOrEmpty(LadderTag) && c.CompareTag(LadderTag)) return true;
        if (!string.IsNullOrEmpty(LadderTag) && c.transform.root.CompareTag(LadderTag)) return true;
        return LadderMask.value != 0 && ((1 << c.gameObject.layer) & LadderMask.value) != 0;
    }

    private void AttachLadder(RaycastHit hit)
    {
        IsOnLadder = true;
        currentLadder = hit.collider;
        ladderNormal = hit.normal;
        Velocity = Vector3.zero;
        bufferedJumpConsumed = true;
    }

    private void LadderMove(in OUTL_ActorInputFrame frame, float dt)
    {
        RaycastHit hit;
        bool hasFreshHit = DetectLadder(out hit);
        if (!hasFreshHit && !IsStillNearCurrentLadder())
        {
            DetachLadder(Vector3.zero);
            return;
        }

        if (hasFreshHit && hit.collider != null)
        {
            currentLadder = hit.collider;
            ladderNormal = hit.normal;
        }

        if (frame.JumpPressed)
        {
            DetachLadder((-ladderNormal * ScaleSpeed(LadderDetachPushSpeedHU)) + (Vector3.up * ScaleSpeed(LadderDetachJumpSpeedHU)));
            bufferedJumpConsumed = true;
            lastJumpTime = Time.time;
            PlaySurfaceClip(CurrentSurfaceData != null ? CurrentSurfaceData.JumpSounds : null, FallbackJumpClips, JumpSource, transform.position, 1f);
            if (AnimationBridge != null) AnimationBridge.NotifyJump();
            return;
        }

        Vector3 side = Vector3.Cross(Vector3.up, ladderNormal);
        if (side.sqrMagnitude < 0.0001f) side = transform.right;
        side.Normalize();

        float forwardInput = Mathf.Abs(frame.Move.y) > LadderInputDeadZone ? frame.Move.y : 0f;
        float sideInput = Mathf.Abs(frame.Move.x) > LadderInputDeadZone ? frame.Move.x : 0f;
        float climbInput = BuildLadderClimbInput(forwardInput);
        Vector3 inwardStick = -ladderNormal * ScaleSpeed(LadderStickSpeedHU);
        Vector3 move = Vector3.up * (climbInput * ScaleSpeed(LadderClimbSpeedHU)) + side * (sideInput * ScaleSpeed(LadderSideSpeedHU)) + inwardStick;
        Velocity = move;
        Controller.Move(move * dt);
        IsGrounded = false;
    }

    private float BuildLadderClimbInput(float forwardInput)
    {
        if (!HalfLifeLadderInput || Mathf.Abs(forwardInput) <= LadderInputDeadZone)
            return forwardInput;

        if (forwardInput > 0f)
        {
            Transform view = ViewPitchRoot != null ? ViewPitchRoot : (ViewCamera != null ? ViewCamera.transform : transform);
            float lookY = view != null ? view.forward.y : 0f;
            if (lookY < -0.35f)
                return -Mathf.Clamp01(Mathf.InverseLerp(-0.35f, -0.9f, lookY));
            return 1f;
        }

        return -1f;
    }

    private bool IsStillNearCurrentLadder()
    {
        if (currentLadder == null || !currentLadder.enabled || !currentLadder.gameObject.activeInHierarchy)
            return false;

        Vector3 probe = transform.position + Vector3.up * Mathf.Clamp(StandingViewHeight * 0.65f, 0.55f, StandingHeight);
        float max = Mathf.Max(LadderDetectDistance, LadderReleaseDistance);
        return currentLadder.bounds.SqrDistance(probe) <= max * max;
    }

    private void DetachLadder(Vector3 inheritedVelocity)
    {
        IsOnLadder = false;
        currentLadder = null;
        ladderNormal = Vector3.zero;
        Velocity = inheritedVelocity;
    }

    private bool TryAutoGrabLedge(in OUTL_ActorInputFrame frame)
    {
        if (IsOnLadder || IsGrounded) return false;
        if (Velocity.y > ScaleSpeed(120f)) return false;
        if (!AutoGrabLedges && !frame.JumpHeld && !frame.UsePressed) return false;

        Vector3 hang;
        Vector3 stand;
        Vector3 normal;
        if (!TryFindLedge(out hang, out stand, out normal)) return false;

        IsHangingFromLedge = true;
        IsClimbingLedge = false;
        Velocity = Vector3.zero;
        ledgeHangPosition = hang;
        ledgeStandPosition = stand;
        ledgeNormal = normal;
        Controller.enabled = false;
        transform.position = ledgeHangPosition;
        Controller.enabled = true;
        bufferedJumpConsumed = true;
        return true;
    }

    private bool TryFindLedge(out Vector3 hang, out Vector3 stand, out Vector3 normal)
    {
        hang = Vector3.zero;
        stand = Vector3.zero;
        normal = Vector3.zero;

        Vector3 forward = YawRoot != null ? YawRoot.forward : transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) return false;
        forward.Normalize();

        RaycastHit wallHit;
        Vector3 chest = transform.position + Vector3.up * LedgeChestHeight;
        if (!Physics.Raycast(chest, forward, out wallHit, LedgeForwardCheckDistance, LedgeMask, QueryTriggerInteraction.Ignore))
            return false;
        if (Vector3.Angle(wallHit.normal, Vector3.up) < 55f) return false;

        Vector3 topOrigin = wallHit.point + forward * (HullRadius + LedgeStandForwardOffset) + Vector3.up * LedgeHeadClearanceHeight;
        RaycastHit topHit;
        if (!Physics.Raycast(topOrigin, Vector3.down, out topHit, LedgeTopProbeDownDistance, LedgeMask, QueryTriggerInteraction.Ignore))
            return false;
        if (Vector3.Angle(topHit.normal, Vector3.up) > SlopeLimit) return false;

        normal = wallHit.normal;
        stand = topHit.point + Vector3.up * 0.02f;
        hang = stand + normal * (HullRadius + LedgeHangOffsetBack) - Vector3.up * LedgeHangOffsetDown;
        return CapsuleClearAt(stand) && CapsuleClearAt(hang);
    }

    private void UpdateLedgeHang(in OUTL_ActorInputFrame frame)
    {
        Velocity = Vector3.zero;
        if (frame.CrouchHeld || frame.LedgeDropPressed)
        {
            IsHangingFromLedge = false;
            return;
        }

        if (frame.JumpPressed || frame.JumpHeld || frame.UsePressed)
        {
            IsClimbingLedge = true;
            IsHangingFromLedge = false;
            ledgeClimbStartTime = Time.time;
            ledgeClimbStartPosition = transform.position;
        }
    }

    private void UpdateLedgeClimb(float dt)
    {
        float t = Mathf.Clamp01((Time.time - ledgeClimbStartTime) / Mathf.Max(0.01f, LedgeClimbDuration));
        float eased = t * t * (3f - 2f * t);
        Controller.enabled = false;
        transform.position = Vector3.Lerp(ledgeClimbStartPosition, ledgeStandPosition, eased);
        Controller.enabled = true;
        Velocity = Vector3.zero;
        if (t >= 1f)
        {
            IsClimbingLedge = false;
            IsGrounded = true;
            lastGroundedTime = Time.time;
        }
    }

    private bool CapsuleClearAt(Vector3 position)
    {
        Vector3 basePos = position + Vector3.up * HullRadius;
        Vector3 topPos = position + Vector3.up * (StandingHeight - HullRadius);

        int count = Physics.OverlapCapsuleNonAlloc(
            basePos,
            topPos,
            HullRadius * 0.95f,
            ledgeClearanceBuffer,
            LedgeBlockMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider c = ledgeClearanceBuffer[i];
            ledgeClearanceBuffer[i] = null;
            if (IsBlockingCollider(c)) return false;
        }

        return true;
    }

    private bool IsBlockingCollider(Collider c)
    {
        return c != null && c.gameObject != gameObject && !c.transform.IsChildOf(transform);
    }

    private void NoclipMove(in OUTL_ActorInputFrame frame, float dt)
    {
        wasNoClip = true;
        if (Controller != null && Controller.enabled) Controller.enabled = false;
        ApplyLook(frame);
        Transform basis = ViewPitchRoot != null ? ViewPitchRoot : (ViewCamera != null ? ViewCamera.transform : transform);
        float up = frame.JumpHeld ? 1f : (frame.CrouchHeld ? -1f : 0f);
        Vector3 move = basis.forward * frame.Move.y + basis.right * frame.Move.x + Vector3.up * up;
        if (move.sqrMagnitude > 1f) move.Normalize();
        float mult = frame.SprintHeld ? 4f : 1f;
        transform.position += move * 12f * mult * dt;
        Velocity = move * 12f * mult;
        IsGrounded = false;
    }

    private void ProgressFootsteps(float dt)
    {
        float speed = HorizontalSpeed;
        if (!IsGrounded || speed < MinFootstepSpeed) return;
        accumulatedStepDistance += speed * dt;
        float stepDistance = IsCrouching ? CrouchStepDistance : (speed > ScaleSpeed(WalkSpeed + 20f) ? RunStepDistance : WalkStepDistance);
        if (accumulatedStepDistance < stepDistance) return;
        accumulatedStepDistance = 0f;

        AudioClip[] surfaceClips = CurrentSurfaceData != null ? CurrentSurfaceData.footstepSounds : null;
        float normalizedSpeed = Mathf.Clamp01(speed / Mathf.Max(0.01f, ScaleSpeed(RunSpeed)));
        float volumeMul = FootstepVolumeCurve != null ? FootstepVolumeCurve.Evaluate(normalizedSpeed) : 1f;
        PlaySurfaceClip(surfaceClips, FallbackFootstepClips, FootstepSource, hasGroundHit ? groundHit.point : transform.position, volumeMul);
        if (Feedback != null) Feedback.OnFootstep(speed > ScaleSpeed(WalkSpeed + 20f), normalizedSpeed);
    }

    private void OnLanded(float previousY)
    {
        LastFallSpeed = Mathf.Max(maxObservedFallSpeed, previousY < 0f ? -previousY : 0f);
        LastFallDamage = 0f;
        maxObservedFallSpeed = 0f;
        landingDip = Mathf.Min(LandingViewDip, LastFallSpeed * 0.006f);

        if (LastFallSpeed >= LandingSoundMinFallSpeed)
            PlaySurfaceClip(CurrentSurfaceData != null ? CurrentSurfaceData.LandingSounds : null, FallbackLandingClips, LandingSource, transform.position, 1f);

        LastFallDamage = CalculateFallDamage(LastFallSpeed);
        if (LastFallDamage > 0f && Entity != null && Entity.Id.IsValid)
            OUTL_Combat.ApplyDamage(OUTL_EntityId.None, Entity.Id, LastFallDamage, transform.position, FallDamageKey);

        if (Feedback != null) Feedback.OnLanding(LastFallSpeed, LastFallDamage);
        if (AnimationBridge != null) AnimationBridge.NotifyLand(LastFallSpeed, LastFallDamage);
    }

    private void TrackFallSpeed()
    {
        if (Velocity.y < 0f) maxObservedFallSpeed = Mathf.Max(maxObservedFallSpeed, -Velocity.y);
    }

    private float CalculateFallDamage(float fallSpeed)
    {
        if (!EnableFallDamage) return 0f;
        float min = Mathf.Max(0f, FallDamageMinSpeed);
        if (fallSpeed <= min) return 0f;
        float fatal = Mathf.Max(min + 0.01f, FallDamageFatalSpeed);
        if (fallSpeed >= fatal) return Mathf.Max(0f, FallDamageMaxDamage);
        return Mathf.Clamp((fallSpeed - min) * Mathf.Max(0f, FallDamageScale), 0f, Mathf.Max(0f, FallDamageMaxDamage));
    }

    private void PlaySurfaceClip(AudioClip[] primary, AudioClip[] fallback, AudioSource source, Vector3 position, float volumeMultiplier)
    {
        AudioClip[] clips = HasClips(primary) ? primary : fallback;
        if (!HasClips(clips)) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;

        float minPitch = CurrentSurfaceData != null ? CurrentSurfaceData.MinSoundPitch : 0.95f;
        float maxPitch = CurrentSurfaceData != null ? CurrentSurfaceData.MaxSoundPitch : 1.05f;
        float minVol = CurrentSurfaceData != null ? CurrentSurfaceData.MinSoundVolume : 0.85f;
        float maxVol = CurrentSurfaceData != null ? CurrentSurfaceData.MaxSoundVolume : 1f;
        float pitchValue = Random.Range(minPitch, maxPitch);
        float volume = Random.Range(minVol, maxVol) * Mathf.Max(0f, volumeMultiplier);

        if (source != null)
        {
            source.pitch = pitchValue;
            source.PlayOneShot(clip, volume);
        }
        else
        {
            OUTL_PoolSystem.PlayClipShared(clip, position, volume, pitchValue, 1f, 1f, 24f, false);
        }
    }

    private static bool HasClips(AudioClip[] clips)
    {
        return clips != null && clips.Length > 0;
    }

    private void TickSurfaceHazards(float dt)
    {
        if (!ApplySurfaceHazards) return;
        OUT_SurfaceData hazard = ResolveHazardSurface();
        if (hazard == null) return;

        if (Time.time >= nextSurfaceDamageTime && hazard.HazardDamagePerTick > 0)
        {
            nextSurfaceDamageTime = Time.time + Mathf.Max(0.01f, hazard.HazardDamageInterval);
            if (Entity != null && Entity.Id.IsValid)
                OUTL_Combat.ApplyDamage(OUTL_EntityId.None, Entity.Id, hazard.HazardDamagePerTick, transform.position, "surface." + hazard.name);
        }

        if (hazard.HazardFeedbackClips != null && hazard.HazardFeedbackClips.Length > 0 && Time.time >= nextSurfaceAudioTime)
        {
            nextSurfaceAudioTime = Time.time + Mathf.Max(0.01f, hazard.HazardAudioInterval);
            PlaySurfaceClip(hazard.HazardFeedbackClips, null, SurfaceHazardSource, transform.position, 1f);
        }
    }

    private OUT_SurfaceData ResolveHazardSurface()
    {
        if (CurrentSurfaceData != null && CurrentSurfaceData.IsHazard && CurrentSurfaceData.HazardDamageOnGroundContact)
            return CurrentSurfaceData;

        for (int i = 0; i < triggerSurfaces.Length; i++)
        {
            OUT_SurfaceManager manager = triggerSurfaces[i];
            if (manager == null || manager.surfaceData == null) continue;
            OUT_SurfaceData data = manager.surfaceData;
            if (data.IsHazard && data.HazardDamageOnTriggerContact) return data;
        }
        return null;
    }

    private void OnTriggerEnter(Collider other)
    {
        OUT_SurfaceManager manager = other != null ? other.GetComponent<OUT_SurfaceManager>() : null;
        if (manager == null && other != null) manager = other.GetComponentInParent<OUT_SurfaceManager>();
        if (manager == null) return;
        for (int i = 0; i < triggerSurfaces.Length; i++)
            if (triggerSurfaces[i] == manager) return;
        for (int i = 0; i < triggerSurfaces.Length; i++)
            if (triggerSurfaces[i] == null) { triggerSurfaces[i] = manager; return; }
    }

    private void OnTriggerExit(Collider other)
    {
        OUT_SurfaceManager manager = other != null ? other.GetComponent<OUT_SurfaceManager>() : null;
        if (manager == null && other != null) manager = other.GetComponentInParent<OUT_SurfaceManager>();
        if (manager == null) return;
        for (int i = 0; i < triggerSurfaces.Length; i++)
            if (triggerSurfaces[i] == manager) triggerSurfaces[i] = null;
    }

    private void UpdateViewFeel(in OUTL_ActorInputFrame frame, float dt)
    {
        if (ViewPitchRoot == null) return;
        if (!initializedView) CaptureViewDefaults();

        float speed01 = Mathf.Clamp01(HorizontalSpeed / Mathf.Max(0.01f, ScaleSpeed(RunSpeed)));
        Vector3 bob = Vector3.zero;
        if (EnableViewBob && IsGrounded && HorizontalSpeed > MinFootstepSpeed)
        {
            float freq = Mathf.Lerp(WalkBobFrequency, RunBobFrequency, speed01);
            bobPhase += dt * freq;
            bob.x = Mathf.Sin(bobPhase) * BobHorizontal * speed01;
            bob.y = Mathf.Abs(Mathf.Cos(bobPhase * 2f)) * BobVertical * speed01;
        }
        else
        {
            bobPhase = Mathf.Lerp(bobPhase, 0f, Mathf.Clamp01(dt * BobReturnSpeed));
        }

        landingDip = Mathf.Lerp(landingDip, 0f, Mathf.Clamp01(dt * LandingViewDipReturnSpeed));
        Vector3 targetPos = baseViewLocalPosition + bob - Vector3.up * landingDip;
        ViewPitchRoot.localPosition = Vector3.Lerp(ViewPitchRoot.localPosition, targetPos, Mathf.Clamp01(dt * BobReturnSpeed));

        float targetRoll = -frame.Move.x * StrafeRollDegrees;
        if (HorizontalSpeed > 0.01f)
            targetRoll += -Vector3.Dot((YawRoot != null ? YawRoot.right : transform.right), Velocity) / Mathf.Max(0.01f, ScaleSpeed(RunSpeed)) * VelocityRollDegrees;
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, Mathf.Clamp01(dt * ViewRollLerpSpeed));
        ViewPitchRoot.localRotation = Quaternion.Euler(pitch, 0f, currentRoll);

        if (ViewCamera != null && baseFov > 1f)
        {
            float targetFov = baseFov + CameraFovRunAdd * speed01;
            ViewCamera.fieldOfView = Mathf.Lerp(ViewCamera.fieldOfView, targetFov, Mathf.Clamp01(dt * CameraFovLerpSpeed));
        }
    }

    private void ScanInteractable()
    {
        currentInteractable = null;
        currentCommandTarget = null;
        Transform view = ViewPitchRoot != null ? ViewPitchRoot : (ViewCamera != null ? ViewCamera.transform : transform);
        OUTL_EntityId source = Entity != null ? Entity.Id : OUTL_EntityId.None;
        OUTL_Interactable interactable;
        OUTL_EntityAdapter adapter;

        if (TryFindUseTarget(view, source, out interactable, out adapter))
        {
            SetStickyUseTarget(interactable, adapter);
            return;
        }

        if (TryUseStickyTarget(source))
            return;
    }

    private bool TryFindUseTarget(Transform view, OUTL_EntityId source, out OUTL_Interactable interactable, out OUTL_EntityAdapter adapter)
    {
        interactable = null;
        adapter = null;
        if (view == null) return false;

        RaycastHit hit;
        if (Physics.Raycast(view.position, view.forward, out hit, UseDistance, UseMask, QueryTriggerInteraction.Collide))
        {
            if (TryResolveUseHit(hit, source, out interactable, out adapter))
                return true;
        }

        float radius = Mathf.Max(0f, UseProbeRadius);
        if (radius <= 0f) return false;

        int count = Physics.SphereCastNonAlloc(view.position, radius, view.forward, interactionHitBuffer, UseDistance, UseMask, QueryTriggerInteraction.Collide);
        float bestDistance = float.MaxValue;
        int bestIndex = -1;
        for (int i = 0; i < count; i++)
        {
            RaycastHit candidate = interactionHitBuffer[i];
            if (candidate.collider == null) continue;
            OUTL_Interactable hitInteractable;
            OUTL_EntityAdapter hitAdapter;
            if (!TryResolveUseHit(candidate, source, out hitInteractable, out hitAdapter)) continue;
            if (candidate.distance < bestDistance)
            {
                bestDistance = candidate.distance;
                bestIndex = i;
                interactable = hitInteractable;
                adapter = hitAdapter;
            }
        }

        for (int i = 0; i < count; i++) interactionHitBuffer[i] = default(RaycastHit);
        return bestIndex >= 0;
    }

    private bool TryResolveUseHit(RaycastHit hit, OUTL_EntityId source, out OUTL_Interactable interactable, out OUTL_EntityAdapter adapter)
    {
        interactable = null;
        adapter = null;
        if (hit.collider == null) return false;

        OUTL_Interactable foundInteractable = hit.collider.GetComponentInParent<OUTL_Interactable>();
        if (foundInteractable != null)
        {
            if (!HideBusyInteractables || foundInteractable.CanUse(source))
            {
                interactable = foundInteractable;
                return true;
            }

            return false;
        }

        OUTL_EntityAdapter foundAdapter = hit.collider.GetComponentInParent<OUTL_EntityAdapter>();
        if (CanUseEntity(foundAdapter))
        {
            adapter = foundAdapter;
            return true;
        }

        return false;
    }

    private void SetStickyUseTarget(OUTL_Interactable interactable, OUTL_EntityAdapter adapter)
    {
        currentInteractable = interactable;
        currentCommandTarget = adapter;
        stickyInteractable = interactable;
        stickyCommandTarget = adapter;
        stickyUseUntil = Time.unscaledTime + Mathf.Max(0f, UseStickySeconds);
    }

    private bool TryUseStickyTarget(OUTL_EntityId source)
    {
        if (Time.unscaledTime > stickyUseUntil) return false;

        if (stickyInteractable != null && stickyInteractable.isActiveAndEnabled && (!HideBusyInteractables || stickyInteractable.CanUse(source)) && IsWithinUseDistance(stickyInteractable.transform.position))
        {
            currentInteractable = stickyInteractable;
            return true;
        }

        if (CanUseEntity(stickyCommandTarget) && IsWithinUseDistance(stickyCommandTarget.transform.position))
        {
            currentCommandTarget = stickyCommandTarget;
            return true;
        }

        stickyInteractable = null;
        stickyCommandTarget = null;
        return false;
    }

    private bool IsWithinUseDistance(Vector3 point)
    {
        Transform view = ViewPitchRoot != null ? ViewPitchRoot : (ViewCamera != null ? ViewCamera.transform : transform);
        float max = Mathf.Max(0.01f, UseDistance + UseProbeRadius + 0.25f);
        return (point - view.position).sqrMagnitude <= max * max;
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

    private void HandleActionInput(in OUTL_ActorInputFrame frame)
    {
        if (frame.UsePressed)
        {
            OUTL_EntityId source = Entity != null ? Entity.Id : OUTL_EntityId.None;
            if (currentInteractable != null && (!HideBusyInteractables || currentInteractable.CanUse(source)))
                currentInteractable.Use(source);
            else if (currentCommandTarget != null && OUTL_World.Instance != null)
                OUTL_World.Instance.Commands.Send(new OUTL_Command(OUTL_CommandType.Use, source, currentCommandTarget.Id) { Point = currentCommandTarget.transform.position });
        }

        if (AttackDriver != null)
        {
            if (frame.FirePrimaryPressed) FireWeaponSlot(activeWeaponSlot);
            if (frame.FireSecondaryPressed) FireWeaponSlot(OUTL_EquipmentSlot.Secondary);
            if (frame.MeleePressed) FireWeaponSlot(OUTL_EquipmentSlot.Melee);
        }
    }

    private void HandleWeaponSlotInput(in OUTL_ActorInputFrame frame)
    {
        if (frame.WeaponSlot >= 0)
            SelectWeaponSlot((OUTL_EquipmentSlot)frame.WeaponSlot);

        float wheel = frame.WeaponCycle;
        if (wheel > 0.05f) CycleWeaponSlot(1);
        else if (wheel < -0.05f) CycleWeaponSlot(-1);
    }

    public void SelectWeaponSlot(OUTL_EquipmentSlot slot)
    {
        if (activeWeaponSlot == slot) return;
        activeWeaponSlot = slot;
        if (AnimationBridge != null) AnimationBridge.NotifyWeaponChanged(slot);
    }

    private void CycleWeaponSlot(int direction)
    {
        int index = 0;
        for (int i = 0; i < WeaponCycleSlots.Length; i++)
            if (WeaponCycleSlots[i] == activeWeaponSlot) { index = i; break; }
        int step = direction >= 0 ? 1 : -1;
        for (int tries = 0; tries < WeaponCycleSlots.Length; tries++)
        {
            index = (index + step + WeaponCycleSlots.Length) % WeaponCycleSlots.Length;
            if (HasUsableWeaponSlot(WeaponCycleSlots[index]))
            {
                SelectWeaponSlot(WeaponCycleSlots[index]);
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
        }
        return fired;
    }

    private bool IsDead()
    {
        if (Entity == null || Entity.Runtime == null) return false;
        return Entity.Runtime.Dead || Entity.Runtime.LifeState == OUTL_LifeState.Dead || Entity.Runtime.State.GetFlag(OUTL_StateId.Dead);
    }

    private void WriteMotorState()
    {
        if (!WriteMotorStateToEntity || Entity == null || Entity.Runtime == null) return;
        Entity.Runtime.State.SetFlag("Player.Grounded", IsGrounded);
        Entity.Runtime.State.SetFlag("Player.Crouching", IsCrouching);
        Entity.Runtime.State.SetFlag("Player.Ladder", IsOnLadder);
        Entity.Runtime.State.SetFlag("Player.LedgeHang", IsHangingFromLedge);
        Entity.Runtime.State.SetFloat("Player.Speed", HorizontalSpeed);
        Entity.Runtime.State.SetFloat("Player.VerticalSpeed", Velocity.y);
        Entity.Runtime.State.SetFloat("Player.Slope", groundSlopeAngle);
        Entity.Runtime.State.SetFloat("Player.FallSpeed", LastFallSpeed);
        Entity.Runtime.State.SetFloat("Player.FallDamage", LastFallDamage);
        Entity.Runtime.State.SetString("Player.Surface", CurrentSurfaceData != null ? CurrentSurfaceData.name : string.Empty);
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
