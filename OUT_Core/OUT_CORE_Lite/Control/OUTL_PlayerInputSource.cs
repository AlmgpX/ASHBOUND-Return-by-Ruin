using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_PlayerInputSource : MonoBehaviour, OUTL_IActorInputSource
{
    public Camera ViewCamera;
    public string HorizontalAxis = "Horizontal";
    public string VerticalAxis = "Vertical";
    public string MouseXAxis = "Mouse X";
    public string MouseYAxis = "Mouse Y";
    public string MouseWheelAxis = "Mouse ScrollWheel";
    public string JumpButton = "Jump";
    public KeyCode CrouchKey = KeyCode.LeftControl;
    public KeyCode AltCrouchKey = KeyCode.C;
    public KeyCode SprintKey = KeyCode.LeftShift;
    public KeyCode UseKey = KeyCode.E;
    public KeyCode PrimaryFireKey = KeyCode.Mouse0;
    public KeyCode SecondaryFireKey = KeyCode.Mouse1;
    public KeyCode ReloadKey = KeyCode.R;
    public KeyCode MeleeKey = KeyCode.V;
    public KeyCode LedgeDropKey = KeyCode.LeftControl;
    public KeyCode PrimarySlotKey = KeyCode.Alpha1;
    public KeyCode SecondarySlotKey = KeyCode.Alpha2;
    public KeyCode MeleeSlotKey = KeyCode.Alpha3;
    public float MouseSensitivity = 1f;
    public float AimDistance = 64f;
    public bool IgnoreWhenConsoleCapturesInput = true;
    public bool LockCursorOnStart = true;
    public bool EscapeUnlocksCursor = true;
    public bool ClickRelocksCursor = true;
    public bool DebugLocalInput;
    public float DebugInterval = 1f;

    private OUTL_ActorInputFrame latestFrame;
    private int latestFrameNumber = -1;
    private int localUpdateInputCount;
    private float lastMouseDeltaMagnitude;
    private float lastDebugTime;

    private void Awake()
    {
        if (ViewCamera == null) ViewCamera = GetComponentInChildren<Camera>();
    }

    private void OnEnable()
    {
        if (LockCursorOnStart && !OUTL_DevConsole.IsInputCaptured)
            SetCursorLocked(true);
    }

    private void Update()
    {
        UpdateCursorLock();
        SampleLatest(Time.time, Time.deltaTime);
        if (DebugLocalInput) LogDebugOncePerSecond();
    }

    public bool TryBuildInput(OUTL_World world, OUTL_EntityAdapter entity, float time, float deltaTime, ref OUTL_ActorInputFrame frame)
    {
        if (IgnoreWhenConsoleCapturesInput && OUTL_DevConsole.IsInputCaptured) return false;
        if (latestFrameNumber != Time.frameCount) SampleLatest(time, deltaTime);
        frame = latestFrame;
        frame.Timestamp = time;
        frame.DeltaTime = Mathf.Max(0f, deltaTime);
        return frame.HasAnyAction;
    }

    public void RefreshAimAfterImmediateLook(ref OUTL_ActorInputFrame frame)
    {
        if (ViewCamera == null) ViewCamera = GetComponentInChildren<Camera>();
        Transform aimTransform = ViewCamera != null ? ViewCamera.transform : transform;
        frame.AimWorldPoint = aimTransform.position + aimTransform.forward * Mathf.Max(1f, AimDistance);
        frame.HasAimWorldPoint = true;
        frame.DesiredYaw = transform.eulerAngles.y;
        frame.DesiredPitch = ViewCamera != null ? NormalizePitch(ViewCamera.transform.localEulerAngles.x) : 0f;
        frame.HasDesiredView = true;
    }

    private void SampleLatest(float time, float deltaTime)
    {
        latestFrame = OUTL_ActorInputFrame.Empty(time);
        latestFrame.DeltaTime = Mathf.Max(0f, deltaTime);
        latestFrameNumber = Time.frameCount;
        if (IgnoreWhenConsoleCapturesInput && OUTL_DevConsole.IsInputCaptured) return;
        if (ViewCamera == null) ViewCamera = GetComponentInChildren<Camera>();

        latestFrame.Move = new Vector2(Mathf.Clamp(Input.GetAxisRaw(HorizontalAxis), -1f, 1f), Mathf.Clamp(Input.GetAxisRaw(VerticalAxis), -1f, 1f));
        latestFrame.Look = new Vector2(Input.GetAxisRaw(MouseXAxis) * MouseSensitivity, Input.GetAxisRaw(MouseYAxis) * MouseSensitivity);
        lastMouseDeltaMagnitude = latestFrame.Look.magnitude;
        latestFrame.JumpHeld = Input.GetButton(JumpButton);
        latestFrame.JumpPressed = Input.GetButtonDown(JumpButton);
        latestFrame.CrouchHeld = Input.GetKey(CrouchKey) || Input.GetKey(AltCrouchKey);
        latestFrame.SprintHeld = Input.GetKey(SprintKey);
        latestFrame.FirePrimaryHeld = Input.GetKey(PrimaryFireKey);
        latestFrame.FirePrimaryPressed = Input.GetKeyDown(PrimaryFireKey);
        latestFrame.FireSecondaryPressed = Input.GetKeyDown(SecondaryFireKey);
        latestFrame.MeleePressed = Input.GetKeyDown(MeleeKey);
        latestFrame.FireAuthorized = latestFrame.FirePrimaryHeld || latestFrame.FirePrimaryPressed || latestFrame.FireSecondaryPressed || latestFrame.MeleePressed;
        latestFrame.AimConfidence = 1f;
        latestFrame.MaxAllowedFireAngle = 180f;
        latestFrame.ReloadPressed = Input.GetKeyDown(ReloadKey);
        latestFrame.UsePressed = Input.GetKeyDown(UseKey);
        latestFrame.LedgeDropPressed = Input.GetKeyDown(LedgeDropKey);

        float wheel = string.IsNullOrEmpty(MouseWheelAxis) ? 0f : Input.GetAxisRaw(MouseWheelAxis);
        if (wheel > 0.05f) latestFrame.WeaponCycle = 1;
        else if (wheel < -0.05f) latestFrame.WeaponCycle = -1;

        if (Input.GetKeyDown(PrimarySlotKey)) latestFrame.WeaponSlot = (int)OUTL_EquipmentSlot.Primary;
        else if (Input.GetKeyDown(SecondarySlotKey)) latestFrame.WeaponSlot = (int)OUTL_EquipmentSlot.Secondary;
        else if (Input.GetKeyDown(MeleeSlotKey)) latestFrame.WeaponSlot = (int)OUTL_EquipmentSlot.Melee;

        RefreshAimAfterImmediateLook(ref latestFrame);
        if (latestFrame.HasAnyAction) localUpdateInputCount++;
    }

    private void UpdateCursorLock()
    {
        if (OUTL_DevConsole.IsInputCaptured) return;
        if (EscapeUnlocksCursor && Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorLocked(false);
            return;
        }

        if (ClickRelocksCursor && Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            SetCursorLocked(true);
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void LogDebugOncePerSecond()
    {
        float now = Time.unscaledTime;
        if (now - lastDebugTime < Mathf.Max(0.1f, DebugInterval)) return;
        lastDebugTime = now;
        float yaw = transform.eulerAngles.y;
        float pitch = ViewCamera != null ? NormalizePitch(ViewCamera.transform.localEulerAngles.x) : 0f;
        Debug.Log("[OUTL LocalInput] updates=" + localUpdateInputCount + " mouse=" + lastMouseDeltaMagnitude.ToString("0.###") + " yaw=" + yaw.ToString("0.0") + " pitch=" + pitch.ToString("0.0") + " cursor=" + Cursor.lockState + " captured=" + OUTL_DevConsole.IsInputCaptured, this);
        localUpdateInputCount = 0;
    }

    private static float NormalizePitch(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}
