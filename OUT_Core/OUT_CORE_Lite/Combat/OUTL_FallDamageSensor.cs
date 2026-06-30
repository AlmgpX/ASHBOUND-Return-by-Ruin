using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_FallDamageSensor : MonoBehaviour, OUTL_ITickable, OUTL_IPoolReset
{
    [Header("OUTL")]
    public OUTL_EntityAdapter Entity;
    public bool AutoRegister = true;
    public OUTL_TickLane TickLane = OUTL_TickLane.Full;
    [Min(0.01f)] public float TickInterval = 0.02f;
    [Tooltip("Fall physics is only exact for materialized actors. Mid/Far/Dormant actors are abstract and should not spend casts.")]
    public OUTL_RuntimeTier MinimumActiveTier = OUTL_RuntimeTier.Near;

    [Header("Motion Source")]
    public CharacterController CharacterController;
    public Rigidbody Rigidbody;
    public Collider GroundProbeCollider;
    public bool AutoResolveSources = true;

    [Header("Ground Probe")]
    public LayerMask GroundMask = ~0;
    public QueryTriggerInteraction GroundTriggerInteraction = QueryTriggerInteraction.Ignore;
    public float GroundProbeExtraDistance = 0.16f;
    public float GroundProbeRadius = 0.22f;

    [Header("GoldSRC Fall Damage")]
    public bool EnableFallDamage = true;
    public bool UseGoldSrcUnits = true;
    public float GoldSrcUnitsPerUnityUnit = 32f;
    [Tooltip("GoldSrc-style safe fall velocity. 580 HU/s roughly matches the classic safe threshold.")]
    public float SafeFallSpeedHU = 580f;
    [Tooltip("GoldSrc-style fatal fall velocity. 1024 HU/s is the classic hard landing region.")]
    public float FatalFallSpeedHU = 1024f;
    public float MaxFallDamage = 100f;
    public string DamageKey = "fall";

    [Header("Runtime")]
    public bool IsGrounded;
    public bool WasGrounded;
    public bool IsFalling;
    public float PeakFallSpeedHU;
    public float LastFallSpeedHU;
    public float LastFallDamage;

    private bool registered;
    private Vector3 lastPosition;
    private bool hasLastPosition;
    private readonly RaycastHit[] groundHits = new RaycastHit[8];

    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled && EnableFallDamage && IsRuntimeTierActive(); } }
    public OUTL_TickLane OUTL_TickLane { get { return TickLane; } }
    public float OUTL_TickInterval { get { return Mathf.Max(0.01f, TickInterval); } }

    private void Awake()
    {
        Resolve();
        ResetRuntimeState();
    }

    private void OnEnable()
    {
        Resolve();
        ResetRuntimeState();
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
        Resolve();
        float dt = deltaTime > 0f ? deltaTime : Time.deltaTime;
        if (dt <= 0f) return;

        WasGrounded = IsGrounded;
        IsGrounded = ReadGrounded();
        float verticalVelocity = ReadVerticalVelocity(dt);
        float fallSpeedHU = ToGoldSrcSpeed(Mathf.Max(0f, -verticalVelocity));

        if (!IsGrounded)
        {
            if (fallSpeedHU > 0.01f)
            {
                IsFalling = true;
                if (fallSpeedHU > PeakFallSpeedHU) PeakFallSpeedHU = fallSpeedHU;
            }
            return;
        }

        if ((!WasGrounded || IsFalling) && PeakFallSpeedHU > 0.01f)
            Land(PeakFallSpeedHU);
        else
            PeakFallSpeedHU = 0f;

        IsFalling = false;
    }

    public void OUTL_OnPoolSpawn()
    {
        ResetRuntimeState();
    }

    public void OUTL_OnPoolRelease()
    {
        ResetRuntimeState();
    }

    private void Resolve()
    {
        if (!AutoResolveSources) return;
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (CharacterController == null) CharacterController = GetComponent<CharacterController>();
        if (Rigidbody == null) Rigidbody = GetComponent<Rigidbody>();
        if (GroundProbeCollider == null) GroundProbeCollider = GetComponentInChildren<Collider>();
    }

    private bool IsRuntimeTierActive()
    {
        if (Entity == null || Entity.Runtime == null) return true;
        return Entity.Runtime.Tier >= MinimumActiveTier;
    }

    private void ResetRuntimeState()
    {
        IsGrounded = ReadGrounded();
        WasGrounded = IsGrounded;
        IsFalling = false;
        PeakFallSpeedHU = 0f;
        LastFallSpeedHU = 0f;
        LastFallDamage = 0f;
        lastPosition = transform.position;
        hasLastPosition = true;
    }

    private bool ReadGrounded()
    {
        if (CharacterController != null && CharacterController.enabled && CharacterController.isGrounded)
            return true;

        Vector3 origin;
        float distance;
        float radius;
        BuildGroundProbe(out origin, out distance, out radius);
        int count = Physics.SphereCastNonAlloc(origin, radius, Vector3.down, groundHits, distance, GroundMask, GroundTriggerInteraction);
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = groundHits[i];
            groundHits[i] = default(RaycastHit);
            Collider c = hit.collider;
            if (c == null || c.transform.IsChildOf(transform)) continue;
            return true;
        }

        return false;
    }

    private void BuildGroundProbe(out Vector3 origin, out float distance, out float radius)
    {
        radius = Mathf.Max(0.02f, GroundProbeRadius);
        distance = Mathf.Max(0.02f, GroundProbeExtraDistance);

        if (CharacterController != null)
        {
            radius = Mathf.Max(0.02f, CharacterController.radius * 0.85f);
            origin = transform.TransformPoint(CharacterController.center) + Vector3.up * Mathf.Max(0.01f, CharacterController.height * 0.5f - radius);
            distance += Mathf.Max(0.01f, CharacterController.height - radius * 2f);
            return;
        }

        if (GroundProbeCollider != null)
        {
            Bounds b = GroundProbeCollider.bounds;
            radius = Mathf.Max(0.02f, Mathf.Min(b.extents.x, b.extents.z, GroundProbeRadius));
            origin = b.center + Vector3.up * Mathf.Max(0.01f, b.extents.y - radius);
            distance += Mathf.Max(0.01f, b.extents.y * 2f - radius * 2f);
            return;
        }

        origin = transform.position + Vector3.up * 0.5f;
        distance += 0.55f;
    }

    private float ReadVerticalVelocity(float dt)
    {
        if (Rigidbody != null)
            return Rigidbody.velocity.y;

        if (CharacterController != null)
            return CharacterController.velocity.y;

        Vector3 pos = transform.position;
        if (!hasLastPosition)
        {
            lastPosition = pos;
            hasLastPosition = true;
            return 0f;
        }

        float velocity = (pos.y - lastPosition.y) / Mathf.Max(0.0001f, dt);
        lastPosition = pos;
        return velocity;
    }

    private void Land(float fallSpeedHU)
    {
        LastFallSpeedHU = fallSpeedHU;
        LastFallDamage = CalculateFallDamage(fallSpeedHU);
        PeakFallSpeedHU = 0f;

        if (LastFallDamage <= 0f || Entity == null || !Entity.Id.IsValid)
            return;

        OUTL_Combat.ApplyDamage(OUTL_EntityId.None, Entity.Id, LastFallDamage, transform.position, DamageKey);
    }

    private float CalculateFallDamage(float fallSpeedHU)
    {
        if (!EnableFallDamage) return 0f;
        float safe = Mathf.Max(0f, SafeFallSpeedHU);
        if (fallSpeedHU <= safe) return 0f;
        float fatal = Mathf.Max(safe + 1f, FatalFallSpeedHU);
        if (fallSpeedHU >= fatal) return Mathf.Max(0f, MaxFallDamage);
        float t = Mathf.InverseLerp(safe, fatal, fallSpeedHU);
        return Mathf.Clamp(t * Mathf.Max(0f, MaxFallDamage), 0f, Mathf.Max(0f, MaxFallDamage));
    }

    private float ToGoldSrcSpeed(float unitySpeed)
    {
        return UseGoldSrcUnits ? unitySpeed * Mathf.Max(1f, GoldSrcUnitsPerUnityUnit) : unitySpeed;
    }
}
