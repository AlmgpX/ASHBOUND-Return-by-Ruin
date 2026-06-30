using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(OUTL_BasicPlayerController))]
public class OUTL_PlayerSurfaceMotorModifier : MonoBehaviour, OUTL_ITickable
{
    public OUTL_BasicPlayerController Controller;
    public OUTL_SurfaceProbe SurfaceProbe;
    public bool AutoRegister = true;
    public OUTL_TickLane TickLane = OUTL_TickLane.Logic;
    public float TickInterval = 0.05f;

    [Header("Jump")]
    public float PlayerJumpMultiplier = 0.78f;
    public bool ApplySurfaceJumpMultiplier = true;
    public bool ApplySurfaceFrictionMultiplier = true;
    public bool ApplySurfaceSpeedMultiplier = true;

    [Header("Base Values")]
    public bool CaptureBaseOnAwake = true;
    public float BaseJumpSpeed = 270f;
    public float BaseFriction = 4f;
    public float BaseForwardSpeed = 320f;
    public float BaseSideSpeed = 320f;
    public float BaseBackSpeed = 320f;
    public float BaseRunSpeed = 320f;
    public float BaseWalkSpeed = 150f;

    private OUTL_SurfaceProfile currentProfile;
    private bool registered;

    public OUTL_SurfaceProfile CurrentProfile { get { return currentProfile; } }
    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled && Controller != null; } }
    public OUTL_TickLane OUTL_TickLane { get { return TickLane; } }
    public float OUTL_TickInterval { get { return Mathf.Max(0.01f, TickInterval); } }

    private void Awake()
    {
        if (Controller == null) Controller = GetComponent<OUTL_BasicPlayerController>();
        if (SurfaceProbe == null) SurfaceProbe = GetComponent<OUTL_SurfaceProbe>();
        if (SurfaceProbe == null && (ApplySurfaceJumpMultiplier || ApplySurfaceFrictionMultiplier || ApplySurfaceSpeedMultiplier))
            Debug.LogWarning("OUTL_PlayerSurfaceMotorModifier needs a preauthored OUTL_SurfaceProbe for surface multipliers.", this);
        if (CaptureBaseOnAwake && Controller != null) CaptureBaseValues();
    }

    private void OnEnable()
    {
        if (AutoRegister) Register();
    }

    private void OnDisable()
    {
        Unregister();
    }

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        ApplyModifiers();
    }

    public void Register()
    {
        if (registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Register(this);
        registered = true;
    }

    public void Unregister()
    {
        if (!registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Unregister(this);
        registered = false;
    }

    public void CaptureBaseValues()
    {
        if (Controller == null) return;
        BaseJumpSpeed = Controller.JumpSpeed;
        BaseFriction = Controller.Friction;
        BaseForwardSpeed = Controller.ForwardSpeed;
        BaseSideSpeed = Controller.SideSpeed;
        BaseBackSpeed = Controller.BackSpeed;
        BaseRunSpeed = Controller.RunSpeed;
        BaseWalkSpeed = Controller.WalkSpeed;
    }

    public void ApplyModifiers()
    {
        if (Controller == null) return;

        currentProfile = null;
        if (SurfaceProbe != null && SurfaceProbe.ProbeBelow())
            currentProfile = SurfaceProbe.CurrentProfile;

        float jump = Mathf.Max(0f, PlayerJumpMultiplier);
        float friction = 1f;
        float speed = 1f;

        if (currentProfile != null)
        {
            if (ApplySurfaceJumpMultiplier) jump *= Mathf.Max(0f, currentProfile.JumpMultiplier);
            if (ApplySurfaceFrictionMultiplier) friction *= Mathf.Max(0f, currentProfile.FrictionMultiplier);
            if (ApplySurfaceSpeedMultiplier) speed *= Mathf.Max(0f, currentProfile.MoveSpeedMultiplier);
        }

        Controller.JumpSpeed = BaseJumpSpeed * jump;
        Controller.Friction = BaseFriction * friction;
        Controller.ForwardSpeed = BaseForwardSpeed * speed;
        Controller.SideSpeed = BaseSideSpeed * speed;
        Controller.BackSpeed = BaseBackSpeed * speed;
        Controller.RunSpeed = BaseRunSpeed * speed;
        Controller.WalkSpeed = BaseWalkSpeed * speed;
    }
}
