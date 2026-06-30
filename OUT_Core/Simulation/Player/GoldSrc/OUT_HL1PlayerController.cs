using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class OUT_HL1PlayerController : MonoBehaviour
{
    public enum SpeedMode
    {
        HalfLifeRunDefault = 0,
        HoldToSprint = 1,
        HoldToWalk = 2
    }

    [Header("GoldSrc Scale")]
    [SerializeField] private bool useGoldSrcUnits = true;
    [SerializeField] [Min(1f)] private float goldSrcUnitsPerUnityUnit = 32f;

    [Header("View")]
    [SerializeField] private Transform viewRoot;
    [SerializeField] private bool lockCursor = true;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minPitch = -89f;
    [SerializeField] private float maxPitch = 89f;
    [SerializeField] private float standingViewHeight = 1.62f;
    [SerializeField] private float crouchViewHeight = 0.92f;
    [SerializeField] private float viewHeightLerpSpeed = 16f;

    [Header("Input")]
    [SerializeField] private string horizontalAxis = "Horizontal";
    [SerializeField] private string verticalAxis = "Vertical";
    [SerializeField] private string mouseXAxis = "Mouse X";
    [SerializeField] private string mouseYAxis = "Mouse Y";
    [SerializeField] private string jumpButton = "Jump";
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode altCrouchKey = KeyCode.C;
    [SerializeField] private KeyCode speedKey = KeyCode.LeftShift;
    [SerializeField] private bool respectProgressSystemPause = false;

    [Header("GoldSrc Movement")]
    [SerializeField] private SpeedMode speedMode = SpeedMode.HoldToSprint;
    [SerializeField] private float forwardSpeed = 320f;
    [SerializeField] private float sideSpeed = 320f;
    [SerializeField] private float backSpeed = 320f;
    [SerializeField] private float walkSpeed = 150f;
    [SerializeField] private float sprintSpeed = 320f;
    [SerializeField] private float maxGroundSpeed = 320f;
    [SerializeField] private float crouchSpeedMultiplier = 0.333f;
    [SerializeField] private float groundAcceleration = 10f;
    [SerializeField] private float airAcceleration = 10f;
    [SerializeField] private float airWishSpeedCap = 30f;
    [SerializeField] private float friction = 4f;
    [SerializeField] private float stopSpeed = 100f;
    [SerializeField] private float gravity = 800f;
    [SerializeField] private float jumpSpeed = 270f;
    [SerializeField] private float overbounce = 1.001f;
    [SerializeField] private bool skipFrictionOnJumpFrame = true;
    [SerializeField] [Min(0f)] private float jumpGroundLockout = 0.08f;

    [Header("Crouch / Hull")]
    [SerializeField] private float standingHeight = 1.8f;
    [SerializeField] private float crouchHeight = 1.0f;
    [SerializeField] private float hullRadius = 0.32f;
    [SerializeField] private float crouchLerpSpeed = 18f;
    [SerializeField] private LayerMask uncrouchBlockMask = ~0;
    [SerializeField] private QueryTriggerInteraction uncrouchTriggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Ground / Steps / Slopes")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private QueryTriggerInteraction groundTriggerInteraction = QueryTriggerInteraction.Ignore;
    [SerializeField] private float groundProbeExtraDistance = 0.18f;
    [SerializeField] private float stepOffset = 0.35f;
    [SerializeField] private float slopeLimit = 45f;
    [SerializeField] private bool projectMoveOnGroundPlane = true;

    [Header("Ladders")]
    [SerializeField] private bool enableLadders = true;
    [SerializeField] private LayerMask ladderMask = 0;
    [SerializeField] private string ladderTag = "Ladder";
    [SerializeField] private float ladderSpeed = 200f;
    [SerializeField] private float ladderDetachJumpPush = 180f;

    [Header("Water Stub")]
    [SerializeField] private bool enableWaterMovement = false;
    [SerializeField] private LayerMask waterMask = 0;
    [SerializeField] private float waterSpeedMultiplier = 0.55f;
    [SerializeField] private float waterGravityMultiplier = 0.35f;

    [Header("Surface Audio")]
    [SerializeField] private OUT_SurfaceData defaultSurfaceData;
    [SerializeField] private OUT_SurfaceData currentSurfaceData;
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private AudioSource jumpSource;
    [SerializeField] private AudioSource landingSource;
    [SerializeField] private float walkStepDistance = 2.25f;
    [SerializeField] private float runStepDistance = 2.9f;
    [SerializeField] private float crouchStepDistance = 1.65f;
    [SerializeField] private float minFootstepSpeed = 0.8f;
    [SerializeField] private bool spawnFootprints = false;
    [SerializeField] private float footprintLift = 0.015f;

    [Header("Fall Damage")]
    [SerializeField] private bool useFallDamage = true;
    [SerializeField] private OUT_Health_Main health;
    [SerializeField] private float safeFallSpeed = 580f;
    [SerializeField] private float fatalFallSpeed = 1024f;
    [SerializeField] private float fallDamageScale = 100f;

    [Header("Debug")]
    [SerializeField] private bool showDebug;

    private CharacterController controller;
    private AudioSource fallbackAudioSource;

    private Vector3 velocity;
    private Vector3 groundNormal = Vector3.up;
    private RaycastHit groundHit;
    private bool hasGroundHit;
    private bool wasGrounded;
    private bool isGrounded;
    private bool jumpQueued;
    private bool jumpLatchedUntilRelease;
    private bool isCrouching;
    private bool wantsCrouch;
    private bool isOnLadder;
    private bool touchingLadder;
    private Vector3 ladderNormal;
    private bool inWater;

    private float yaw;
    private float pitch;
    private float accumulatedStepDistance;
    private float previousVerticalVelocity;
    private float lastGroundedTime;
    private float lastJumpTime = -999f;

    private float inputForward;
    private float inputSide;
    private bool speedHeld;
    private bool jumpHeld;
    private bool jumpPressedThisFrame;

    private readonly Collider[] uncrouchBuffer = new Collider[24];

    public Vector3 Velocity => velocity;
    public bool IsGrounded => isGrounded;
    public bool IsCrouching => isCrouching;
    public bool IsOnLadder => isOnLadder;
    public OUT_SurfaceData CurrentSurfaceData => currentSurfaceData;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        fallbackAudioSource = GetComponent<AudioSource>();

        if (footstepSource == null)
            footstepSource = fallbackAudioSource;
        if (jumpSource == null)
            jumpSource = fallbackAudioSource;
        if (landingSource == null)
            landingSource = fallbackAudioSource;

        if (health == null)
            health = GetComponent<OUT_Health_Main>() ?? GetComponentInChildren<OUT_Health_Main>(true) ?? GetComponentInParent<OUT_Health_Main>();

        if (viewRoot == null && Camera.main != null)
            viewRoot = Camera.main.transform;

        controller.radius = hullRadius;
        ApplyControllerShape(standingHeight, true);
        controller.slopeLimit = slopeLimit;
        controller.stepOffset = stepOffset;

        yaw = transform.eulerAngles.y;
        pitch = viewRoot != null ? NormalizePitch(viewRoot.localEulerAngles.x) : 0f;
    }

    private void OnEnable()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        jumpQueued = false;
        jumpLatchedUntilRelease = false;
    }

    private void Update()
    {
        if (IsPaused())
            return;

        ReadLookInput();
        ReadMovementInput();
        ReadButtonInput();
        SmoothCrouchAndView(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (IsPaused())
            return;

        float dt = Time.fixedDeltaTime;
        if (dt <= 0f)
            return;

        bool jumpRequestThisTick = jumpQueued;
        jumpQueued = false;

        ProbeGround();
        UpdateWaterState();
        UpdateLadderState();

        Vector3 wishDir;
        float wishSpeed;
        BuildWishVelocity(out wishDir, out wishSpeed);

        wasGrounded = isGrounded;
        isGrounded = ShouldTreatAsGrounded();

        if (isOnLadder)
            LadderMove(jumpRequestThisTick);
        else if (isGrounded)
            GroundMove(wishDir, wishSpeed, jumpRequestThisTick, dt);
        else
            AirMove(wishDir, wishSpeed, dt);

        previousVerticalVelocity = velocity.y;
        CollisionFlags flags = controller.Move(velocity * dt);
        ClipVelocityFromControllerFlags(flags);

        bool groundedAfterMove = ShouldTreatAsGroundedAfterMove(flags);
        bool landed = !wasGrounded && groundedAfterMove;
        isGrounded = groundedAfterMove;

        ProgressFootsteps(dt);

        if (landed)
            OnLanded(previousVerticalVelocity);

        touchingLadder = false;
        jumpPressedThisFrame = false;
    }

    private bool IsPaused()
    {
        return respectProgressSystemPause && ProgressSystem.OnPause;
    }

    private void ReadLookInput()
    {
        float mx = Input.GetAxisRaw(mouseXAxis) * mouseSensitivity;
        float my = Input.GetAxisRaw(mouseYAxis) * mouseSensitivity;

        yaw += mx;
        pitch -= my;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (viewRoot != null)
            viewRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void ReadMovementInput()
    {
        inputForward = Mathf.Clamp(Input.GetAxisRaw(verticalAxis), -1f, 1f);
        inputSide = Mathf.Clamp(Input.GetAxisRaw(horizontalAxis), -1f, 1f);
        speedHeld = Input.GetKey(speedKey);
    }

    private void ReadButtonInput()
    {
        jumpHeld = Input.GetButton(jumpButton);
        jumpPressedThisFrame = false;

        if (!jumpHeld)
        {
            jumpLatchedUntilRelease = false;
        }
        else if (!jumpLatchedUntilRelease)
        {
            jumpPressedThisFrame = true;
            jumpQueued = true;
            jumpLatchedUntilRelease = true;
        }

        wantsCrouch = Input.GetKey(crouchKey) || Input.GetKey(altCrouchKey);
    }

    private void BuildWishVelocity(out Vector3 wishDir, out float wishSpeed)
    {
        float forwardMove = inputForward >= 0f ? inputForward * ScaleSpeed(forwardSpeed) : inputForward * ScaleSpeed(backSpeed);
        float sideMove = inputSide * ScaleSpeed(sideSpeed);

        Vector3 forwardVector = transform.forward;
        Vector3 rightVector = transform.right;

        wishDir = forwardVector * forwardMove + rightVector * sideMove;
        wishDir.y = 0f;

        wishSpeed = wishDir.magnitude;
        if (wishSpeed > 0.0001f)
            wishDir /= wishSpeed;
        else
            wishDir = Vector3.zero;

        float maxSpeed = GetCurrentMaxSpeed();
        if (wishSpeed > maxSpeed)
            wishSpeed = maxSpeed;

        if (isCrouching)
            wishSpeed *= crouchSpeedMultiplier;

        if (inWater)
            wishSpeed *= waterSpeedMultiplier;
    }

    private float GetCurrentMaxSpeed()
    {
        switch (speedMode)
        {
            case SpeedMode.HoldToSprint:
                return ScaleSpeed(speedHeld ? sprintSpeed : walkSpeed);
            case SpeedMode.HoldToWalk:
                return ScaleSpeed(speedHeld ? walkSpeed : maxGroundSpeed);
            case SpeedMode.HalfLifeRunDefault:
            default:
                return ScaleSpeed(speedHeld ? walkSpeed : maxGroundSpeed);
        }
    }

    private void GroundMove(Vector3 wishDir, float wishSpeed, bool jumpRequest, float dt)
    {
        bool jumpNow = jumpRequest && CanStartGroundJump();

        if (!(jumpNow && skipFrictionOnJumpFrame))
            ApplyFriction(dt);

        if (projectMoveOnGroundPlane && hasGroundHit && wishDir.sqrMagnitude > 0.0001f)
            wishDir = Vector3.ProjectOnPlane(wishDir, groundNormal).normalized;

        Accelerate(wishDir, wishSpeed, groundAcceleration, dt);

        if (jumpNow)
        {
            velocity.y = ScaleSpeed(jumpSpeed);
            isGrounded = false;
            lastJumpTime = Time.time;
            PlayJumpSound();
        }
        else
        {
            if (velocity.y < 0f)
                velocity.y = -ScaleSpeed(18f);
            lastGroundedTime = Time.time;
        }
    }

    private bool CanStartGroundJump()
    {
        if (Time.time - lastJumpTime < jumpGroundLockout)
            return false;

        return isGrounded;
    }

    private void AirMove(Vector3 wishDir, float wishSpeed, float dt)
    {
        float wishSpeedCapped = Mathf.Min(wishSpeed, ScaleSpeed(airWishSpeedCap));
        Accelerate(wishDir, wishSpeedCapped, airAcceleration, dt);

        float gravityScale = inWater ? waterGravityMultiplier : 1f;
        velocity.y -= ScaleSpeed(gravity) * gravityScale * dt;
    }

    private void LadderMove(bool jumpRequest)
    {
        float climb = 0f;

        if (Mathf.Abs(inputForward) > 0.01f)
            climb += inputForward;
        if (jumpHeld)
            climb += 1f;
        if (wantsCrouch)
            climb -= 1f;

        Vector3 alongLadder = Vector3.ProjectOnPlane(transform.right * inputSide, ladderNormal);
        Vector3 climbVelocity = Vector3.up * climb + alongLadder;

        if (climbVelocity.sqrMagnitude > 1f)
            climbVelocity.Normalize();

        velocity = climbVelocity * ScaleSpeed(ladderSpeed);

        if (jumpRequest)
        {
            isOnLadder = false;
            touchingLadder = false;
            velocity = ladderNormal * ScaleSpeed(ladderDetachJumpPush);
            velocity.y = ScaleSpeed(jumpSpeed * 0.65f);
            lastJumpTime = Time.time;
        }
    }

    private bool ShouldTreatAsGrounded()
    {
        if (Time.time - lastJumpTime < jumpGroundLockout)
            return false;

        if (velocity.y > 0.05f)
            return false;

        bool validGroundProbe = hasGroundHit && Vector3.Angle(groundNormal, Vector3.up) <= slopeLimit + 0.5f;
        return controller.isGrounded || validGroundProbe;
    }

    private bool ShouldTreatAsGroundedAfterMove(CollisionFlags flags)
    {
        if (Time.time - lastJumpTime < jumpGroundLockout)
            return false;

        if (velocity.y > 0.05f)
            return false;

        if ((flags & CollisionFlags.Below) != 0)
            return true;

        bool validGroundProbe = hasGroundHit && Vector3.Angle(groundNormal, Vector3.up) <= slopeLimit + 0.5f;
        return controller.isGrounded || validGroundProbe;
    }

    private void ApplyFriction(float dt)
    {
        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
        float speed = horizontal.magnitude;
        if (speed < 0.0001f)
            return;

        float surfaceFriction = 1f;
        if (currentSurfaceData != null && currentSurfaceData.physicMaterial != null)
            surfaceFriction = Mathf.Max(0.05f, currentSurfaceData.physicMaterial.dynamicFriction);

        float control = speed < ScaleSpeed(stopSpeed) ? ScaleSpeed(stopSpeed) : speed;
        float drop = control * friction * surfaceFriction * dt;
        float newSpeed = Mathf.Max(0f, speed - drop);

        if (newSpeed != speed)
        {
            newSpeed /= speed;
            velocity.x *= newSpeed;
            velocity.z *= newSpeed;
        }
    }

    private void Accelerate(Vector3 wishDir, float wishSpeed, float accelerationValue, float dt)
    {
        if (wishDir.sqrMagnitude < 0.0001f || wishSpeed <= 0f)
            return;

        float currentSpeed = Vector3.Dot(velocity, wishDir);
        float addSpeed = wishSpeed - currentSpeed;
        if (addSpeed <= 0f)
            return;

        float accelSpeed = accelerationValue * dt * wishSpeed;
        if (accelSpeed > addSpeed)
            accelSpeed = addSpeed;

        velocity += accelSpeed * wishDir;
    }

    private void ClipVelocityFromControllerFlags(CollisionFlags flags)
    {
        if ((flags & CollisionFlags.Above) != 0 && velocity.y > 0f)
            velocity.y = 0f;

        if ((flags & CollisionFlags.Sides) != 0 && hasGroundHit && Vector3.Angle(groundNormal, Vector3.up) > slopeLimit)
            velocity = ClipVelocity(velocity, groundNormal, overbounce);
    }

    private Vector3 ClipVelocity(Vector3 inputVelocity, Vector3 normal, float bounce)
    {
        float backoff = Vector3.Dot(inputVelocity, normal) * bounce;
        Vector3 output = inputVelocity - normal * backoff;

        if (Mathf.Abs(output.x) < 0.0001f) output.x = 0f;
        if (Mathf.Abs(output.y) < 0.0001f) output.y = 0f;
        if (Mathf.Abs(output.z) < 0.0001f) output.z = 0f;

        return output;
    }

    private void SmoothCrouchAndView(float dt)
    {
        bool targetCrouch = wantsCrouch;
        if (!targetCrouch && isCrouching && !CanUncrouch())
            targetCrouch = true;

        isCrouching = targetCrouch;

        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        float currentHeight = Mathf.MoveTowards(controller.height, targetHeight, crouchLerpSpeed * dt);
        ApplyControllerShape(currentHeight, false);

        if (viewRoot != null)
        {
            float targetViewY = isCrouching ? crouchViewHeight : standingViewHeight;
            Vector3 local = viewRoot.localPosition;
            local.y = Mathf.Lerp(local.y, targetViewY, 1f - Mathf.Exp(-viewHeightLerpSpeed * dt));
            viewRoot.localPosition = local;
        }
    }

    private bool CanUncrouch()
    {
        float radius = Mathf.Max(0.01f, hullRadius * 0.95f);
        Vector3 bottom = transform.position + Vector3.up * (radius + 0.035f);
        Vector3 top = transform.position + Vector3.up * (standingHeight - radius - 0.035f);

        int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, uncrouchBuffer, uncrouchBlockMask, uncrouchTriggerInteraction);
        Transform ownRoot = transform.root;

        for (int i = 0; i < count; i++)
        {
            Collider col = uncrouchBuffer[i];
            if (col == null)
                continue;

            if (col.transform.root == ownRoot)
                continue;

            return false;
        }

        return true;
    }

    private void ApplyControllerShape(float height, bool force)
    {
        if (controller == null)
            return;

        height = Mathf.Max(height, hullRadius * 2f + 0.02f);
        if (!force && Mathf.Abs(controller.height - height) < 0.001f)
            return;

        controller.radius = hullRadius;
        controller.height = height;
        controller.center = Vector3.up * (height * 0.5f);
    }

    private void ProbeGround()
    {
        hasGroundHit = false;
        groundNormal = Vector3.up;
        currentSurfaceData = defaultSurfaceData;

        float radius = Mathf.Max(0.01f, controller.radius * 0.9f);
        Vector3 origin = transform.position + Vector3.up * (controller.radius + 0.04f);
        float distance = controller.radius + groundProbeExtraDistance + 0.08f;

        if (Physics.SphereCast(origin, radius, Vector3.down, out groundHit, distance, groundMask, groundTriggerInteraction))
        {
            hasGroundHit = true;
            groundNormal = groundHit.normal;
            ResolveSurface(groundHit.collider);
        }
    }

    private void ResolveSurface(Collider col)
    {
        if (col == null)
            return;

        OUT_SurfaceManager manager = col.GetComponent<OUT_SurfaceManager>();
        if (manager == null)
            manager = col.GetComponentInParent<OUT_SurfaceManager>();

        if (manager != null && manager.surfaceData != null)
            currentSurfaceData = manager.surfaceData;
    }

    private void UpdateWaterState()
    {
        if (!enableWaterMovement)
        {
            inWater = false;
            return;
        }

        Vector3 point = transform.position + Vector3.up * (controller.height * 0.5f);
        inWater = Physics.CheckSphere(point, controller.radius * 0.75f, waterMask, QueryTriggerInteraction.Collide);
    }

    private void UpdateLadderState()
    {
        if (!enableLadders)
        {
            isOnLadder = false;
            return;
        }

        if (touchingLadder)
        {
            isOnLadder = true;
            return;
        }

        if (isOnLadder)
            isOnLadder = false;
    }

    private void ProgressFootsteps(float dt)
    {
        if (!isGrounded || isOnLadder)
            return;

        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
        float speed = horizontal.magnitude;
        if (speed < minFootstepSpeed)
            return;

        accumulatedStepDistance += speed * dt;
        float interval = isCrouching ? crouchStepDistance : speed > ScaleSpeed(walkSpeed + 20f) ? runStepDistance : walkStepDistance;

        if (accumulatedStepDistance >= interval)
        {
            accumulatedStepDistance = 0f;
            PlayFootstepSound();
            TrySpawnFootprint();
        }
    }

    private void PlayFootstepSound()
    {
        OUT_SurfaceData surface = currentSurfaceData != null ? currentSurfaceData : defaultSurfaceData;
        if (surface == null || surface.footstepSounds == null || surface.footstepSounds.Length == 0 || footstepSource == null)
            return;

        AudioClip clip = surface.footstepSounds[Random.Range(0, surface.footstepSounds.Length)];
        PlaySurfaceClip(footstepSource, clip, surface.MinSoundPitch, surface.MaxSoundPitch, surface.MinSoundVolume, surface.MaxSoundVolume);
    }

    private void PlayJumpSound()
    {
        OUT_SurfaceData surface = currentSurfaceData != null ? currentSurfaceData : defaultSurfaceData;
        if (surface == null || surface.JumpSounds == null || surface.JumpSounds.Length == 0 || jumpSource == null)
            return;

        AudioClip clip = surface.JumpSounds[Random.Range(0, surface.JumpSounds.Length)];
        PlaySurfaceClip(jumpSource, clip, surface.MinSoundPitch, surface.MaxSoundPitch, surface.MinSoundVolume, surface.MaxSoundVolume);
    }

    private void OnLanded(float verticalVelocityBeforeLanding)
    {
        OUT_SurfaceData surface = currentSurfaceData != null ? currentSurfaceData : defaultSurfaceData;
        if (surface != null && surface.LandingSounds != null && surface.LandingSounds.Length > 0 && landingSource != null)
        {
            AudioClip clip = surface.LandingSounds[Random.Range(0, surface.LandingSounds.Length)];
            PlaySurfaceClip(landingSource, clip, surface.MinSoundPitch, surface.MaxSoundPitch, surface.MinSoundVolume, surface.MaxSoundVolume);
        }

        if (!useFallDamage)
            return;

        float impactSpeed = Mathf.Abs(verticalVelocityBeforeLanding) * goldSrcUnitsPerUnityUnit;
        if (!useGoldSrcUnits)
            impactSpeed = Mathf.Abs(verticalVelocityBeforeLanding);

        if (impactSpeed <= safeFallSpeed)
            return;

        float t = Mathf.InverseLerp(safeFallSpeed, fatalFallSpeed, impactSpeed);
        int damage = Mathf.Max(1, Mathf.RoundToInt(t * fallDamageScale));

        if (health != null)
            health.TakeDamage(damage, damage > 35 ? OUT_Health_Main.DamagePainLevel.Strong : OUT_Health_Main.DamagePainLevel.Light);
    }

    private void PlaySurfaceClip(AudioSource source, AudioClip clip, float minPitch, float maxPitch, float minVolume, float maxVolume)
    {
        if (source == null || clip == null)
            return;

        source.pitch = Random.Range(minPitch, maxPitch);
        source.PlayOneShot(clip, Random.Range(minVolume, maxVolume));
    }

    private void TrySpawnFootprint()
    {
        if (!spawnFootprints || currentSurfaceData == null || currentSurfaceData.footprintPrefab == null || !hasGroundHit)
            return;

        float angle = Vector3.Angle(groundNormal, Vector3.up);
        if (angle > currentSurfaceData.maxAngleForFootprints)
            return;

        Vector3 pos = groundHit.point + groundNormal * footprintLift;
        Quaternion rot = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, groundNormal), groundNormal);
        Instantiate(currentSurfaceData.footprintPrefab, pos, rot);
    }

    private float ScaleSpeed(float goldSrcValue)
    {
        return useGoldSrcUnits ? goldSrcValue / Mathf.Max(1f, goldSrcUnitsPerUnityUnit) : goldSrcValue;
    }

    private float NormalizePitch(float eulerX)
    {
        if (eulerX > 180f)
            eulerX -= 360f;
        return eulerX;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!enableLadders)
            return;

        bool maskMatch = (ladderMask.value & (1 << other.gameObject.layer)) != 0;
        bool tagMatch = !string.IsNullOrEmpty(ladderTag) && other.CompareTag(ladderTag);

        if (!maskMatch && !tagMatch)
            return;

        touchingLadder = true;
        Vector3 toPlayer = transform.position - other.bounds.center;
        toPlayer.y = 0f;
        ladderNormal = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : -transform.forward;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!enableLadders || hit.collider == null)
            return;

        bool maskMatch = (ladderMask.value & (1 << hit.collider.gameObject.layer)) != 0;
        bool tagMatch = !string.IsNullOrEmpty(ladderTag) && hit.collider.CompareTag(ladderTag);

        if (!maskMatch && !tagMatch)
            return;

        touchingLadder = true;
        ladderNormal = hit.normal;
    }

    private void OnGUI()
    {
        if (!showDebug)
            return;

        GUILayout.BeginArea(new Rect(12, 80, 460, 260), GUI.skin.box);
        GUILayout.Label("OUT HL1 Controller");
        GUILayout.Label($"input f:{inputForward:0.00} s:{inputSide:0.00} speedHeld:{speedHeld}");
        GUILayout.Label($"jumpHeld:{jumpHeld} jumpPressed:{jumpPressedThisFrame} jumpQueued:{jumpQueued} latched:{jumpLatchedUntilRelease}");
        GUILayout.Label($"vel: {velocity} speed:{new Vector3(velocity.x, 0, velocity.z).magnitude:0.00}");
        GUILayout.Label($"grounded:{isGrounded} crouch:{isCrouching} wantsCrouch:{wantsCrouch} ladder:{isOnLadder} water:{inWater}");
        GUILayout.Label($"surface:{(currentSurfaceData != null ? currentSurfaceData.name : "none")}");
        GUILayout.EndArea();
    }
}
