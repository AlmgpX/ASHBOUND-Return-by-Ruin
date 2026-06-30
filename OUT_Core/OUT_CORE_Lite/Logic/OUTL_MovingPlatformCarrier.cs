using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_MovingPlatformCarrier : MonoBehaviour, OUTL_ITickable
{
    public OUTL_EntityAdapter Entity;
    public Transform PlatformRoot;
    public bool AutoRegister = true;
    public OUTL_TickLane TickLane = OUTL_TickLane.Logic;
    [Min(0.001f)] public float TickInterval = 0.01f;

    [Header("Passenger Detection")]
    public LayerMask PassengerMask = ~0;
    public Vector3 LocalBoxCenter = new Vector3(0f, 0.72f, 0f);
    public Vector3 LocalBoxSize = new Vector3(3f, 0.4f, 3f);
    public QueryTriggerInteraction TriggerInteraction = QueryTriggerInteraction.Ignore;
    public bool CarryCharacterControllers = true;
    public bool CarryRigidbodies = true;
    public bool CarryTransformsFallback;
    public bool IgnoreChildren = true;

    [Header("Motion Transfer")]
    public bool TransferRotationYaw;
    public float MaxCarryDelta = 4f;

    [Header("Debug")]
    public bool DebugLog;

    private readonly Collider[] passengers = new Collider[64];
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private bool initialized;
    private bool registered;

    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled && PlatformRoot != null; } }
    public OUTL_TickLane OUTL_TickLane { get { return TickLane; } }
    public float OUTL_TickInterval { get { return Mathf.Max(0.001f, TickInterval); } }

    private void Awake()
    {
        ResolveReferences();
        CapturePose();
    }

    private void OnEnable()
    {
        CapturePose();
        if (AutoRegister) Register();
    }

    private void OnDisable()
    {
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

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        if (PlatformRoot == null) return;
        if (!initialized) { CapturePose(); return; }

        Vector3 currentPosition = PlatformRoot.position;
        Quaternion currentRotation = PlatformRoot.rotation;
        Vector3 delta = currentPosition - lastPosition;
        if (delta.magnitude > MaxCarryDelta) delta = Vector3.zero;

        Quaternion rotDelta = currentRotation * Quaternion.Inverse(lastRotation);
        Vector3 yawEuler = rotDelta.eulerAngles;
        if (yawEuler.y > 180f) yawEuler.y -= 360f;
        Quaternion yawDelta = Quaternion.Euler(0f, yawEuler.y, 0f);

        if (delta.sqrMagnitude > 0.0000001f || (TransferRotationYaw && Mathf.Abs(yawEuler.y) > 0.001f))
            CarryPassengers(delta, yawDelta);

        lastPosition = currentPosition;
        lastRotation = currentRotation;
    }

    private void CarryPassengers(Vector3 delta, Quaternion yawDelta)
    {
        Vector3 center = PlatformRoot.TransformPoint(LocalBoxCenter);
        Vector3 half = Vector3.Scale(LocalBoxSize * 0.5f, PlatformRoot.lossyScale);
        int count = Physics.OverlapBoxNonAlloc(center, half, passengers, PlatformRoot.rotation, PassengerMask, TriggerInteraction);
        for (int i = 0; i < count; i++)
        {
            Collider c = passengers[i];
            passengers[i] = null;
            if (c == null) continue;
            if (IgnoreChildren && c.transform.IsChildOf(PlatformRoot)) continue;

            CharacterController cc = c.GetComponentInParent<CharacterController>();
            if (CarryCharacterControllers && cc != null && cc.enabled)
            {
                cc.Move(delta);
                if (TransferRotationYaw)
                    cc.transform.rotation = yawDelta * cc.transform.rotation;
                continue;
            }

            Rigidbody rb = c.attachedRigidbody;
            if (CarryRigidbodies && rb != null && !rb.isKinematic)
            {
                rb.MovePosition(rb.position + delta);
                if (TransferRotationYaw)
                    rb.MoveRotation(yawDelta * rb.rotation);
                continue;
            }

            if (CarryTransformsFallback)
            {
                Transform t = c.attachedRigidbody != null ? c.attachedRigidbody.transform : c.transform;
                t.position += delta;
                if (TransferRotationYaw) t.rotation = yawDelta * t.rotation;
            }
        }
    }

    private void CapturePose()
    {
        ResolveReferences();
        if (PlatformRoot == null) return;
        lastPosition = PlatformRoot.position;
        lastRotation = PlatformRoot.rotation;
        initialized = true;
    }

    private void ResolveReferences()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (PlatformRoot == null) PlatformRoot = transform;
    }

    private void OnDrawGizmosSelected()
    {
        Transform root = PlatformRoot != null ? PlatformRoot : transform;
        Gizmos.color = Color.green;
        Gizmos.matrix = root.localToWorldMatrix;
        Gizmos.DrawWireCube(LocalBoxCenter, LocalBoxSize);
    }
}
