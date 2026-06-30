using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public sealed class OUTL_RigidbodyProjectile : MonoBehaviour, OUTL_ILaunchableProjectile, OUTL_ITickable, OUTL_IPoolReset
{
    public Rigidbody Body;
    public Collider BodyCollider;
    public OUTL_TickLane TickLane = OUTL_TickLane.Logic;
    public float LifetimeCheckInterval = 0.05f;
    public bool AlignToVelocity = true;
    public bool ReleaseWhenTooSlow;
    public float MinimumSpeed = 0.35f;

    private OUTL_EntityId source;
    private OUTL_AttackProfile profile;
    private float releaseTime;
    private int bounceCount;
    private bool launched;
    private bool released;
    private bool registered;
    private Collider lastCollider;
    private int lastCollisionFrame = -1;

    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled && launched && !released; } }
    public OUTL_TickLane OUTL_TickLane { get { return TickLane; } }
    public float OUTL_TickInterval { get { return Mathf.Max(0.01f, LifetimeCheckInterval); } }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnDisable()
    {
        Unregister();
    }

    public void OUTL_Launch(OUTL_EntityId sourceId, OUTL_AttackProfile attackProfile, Vector3 direction)
    {
        ResolveReferences();
        source = sourceId;
        profile = attackProfile;
        released = false;
        launched = profile != null;
        bounceCount = 0;
        lastCollider = null;
        lastCollisionFrame = -1;

        Vector3 safeDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
        if (safeDirection.sqrMagnitude <= 0.001f) safeDirection = Vector3.forward;
        float speed = profile != null ? Mathf.Max(0.01f, profile.ProjectileSpeed) : 1f;
        releaseTime = ReadTime() + (profile != null ? Mathf.Max(0.05f, profile.ProjectileLifetime) : 1f);

        if (Body != null)
        {
            Body.velocity = safeDirection * speed;
            Body.angularVelocity = Vector3.zero;
            Body.useGravity = profile != null && profile.ProjectileUsesGravity;
            Body.isKinematic = false;
            Body.WakeUp();
        }
        if (AlignToVelocity) transform.rotation = Quaternion.LookRotation(safeDirection);
        Register();
    }

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        if (!launched || released || profile == null)
        {
            ReleaseSelf(false);
            return;
        }

        if (AlignToVelocity && Body != null && Body.velocity.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(Body.velocity.normalized);

        if (time >= releaseTime)
        {
            ReleaseSelf(profile.ProjectileDetonateOnLifetimeEnd || profile.HasExplosion);
            return;
        }

        if (ReleaseWhenTooSlow && Body != null && Body.velocity.sqrMagnitude < MinimumSpeed * MinimumSpeed)
            ReleaseSelf(profile.ProjectileDetonateOnLifetimeEnd);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!launched || released || profile == null || collision == null || collision.collider == null) return;
        ContactPoint contact = collision.contactCount > 0 ? collision.GetContact(0) : default(ContactPoint);
        Vector3 point = collision.contactCount > 0 ? contact.point : transform.position;
        Vector3 normal = collision.contactCount > 0 ? contact.normal : -transform.forward;
        HandleHit(collision.collider, point, normal);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!launched || released || profile == null || other == null) return;
        if (profile.ProjectileIgnoreTriggers && other.isTrigger) return;
        Vector3 point = other.ClosestPoint(transform.position);
        Vector3 normal = transform.position - point;
        if (normal.sqrMagnitude <= 0.001f) normal = -transform.forward;
        HandleHit(other, point, normal.normalized);
    }

    private void HandleHit(Collider collider, Vector3 point, Vector3 normal)
    {
        if (collider == lastCollider && lastCollisionFrame == Time.frameCount) return;
        lastCollider = collider;
        lastCollisionFrame = Time.frameCount;

        OUTL_EntityAdapter target;
        float multiplier;
        string suffix;
        bool hitEntity = OUTL_Hitbox.Resolve(collider, out target, out multiplier, out suffix);
        if (hitEntity && target.Id == source) return;

        if (hitEntity)
        {
            if (profile.ProjectileDetonateOnEntityHit || profile.HasExplosion)
            {
                ReleaseSelf(true, point, normal);
                return;
            }

            string key = OUTL_Hitbox.BuildDamageKey(profile.HitDamageKey, suffix);
            OUTL_Combat.ApplyDamage(source, target.Id, profile.Damage * Mathf.Max(0f, multiplier), point, key, profile.ExtraHitEffects);
            SpawnImpact(point, normal);
            ReleaseSelf(false);
            return;
        }

        if (profile.ProjectileDetonateOnWorldHit)
        {
            ReleaseSelf(true, point, normal);
            return;
        }

        if (profile.ProjectileBounceOnWorldHit && (profile.ProjectileMaxBounces < 0 || bounceCount < profile.ProjectileMaxBounces))
        {
            bounceCount++;
            if (Body != null)
            {
                Body.velocity *= Mathf.Max(0f, profile.ProjectileBounceDamping);
                Body.angularVelocity *= Mathf.Max(0f, profile.ProjectileFrictionDamping);
            }
            return;
        }

        SpawnImpact(point, normal);
        ReleaseSelf(false);
    }

    private void ReleaseSelf(bool detonate)
    {
        ReleaseSelf(detonate, transform.position, -transform.forward);
    }

    private void ReleaseSelf(bool detonate, Vector3 point, Vector3 normal)
    {
        if (released) return;
        if (detonate && profile != null && profile.HasExplosion)
            OUTL_Combat.ApplyExplosion(source, profile, point);
        SpawnImpact(point, normal);
        released = true;
        launched = false;
        Unregister();
        OUTL_PoolSystem.ReleaseShared(gameObject);
    }

    private void SpawnImpact(Vector3 point, Vector3 normal)
    {
        if (profile == null) return;
        if (profile.ImpactVFX != null)
            OUTL_PoolSystem.SpawnShared(profile.ImpactVFX, point, normal.sqrMagnitude > 0.001f ? Quaternion.LookRotation(normal) : Quaternion.identity);
        if (profile.ImpactSound != null)
            OUTL_PoolSystem.PlayClipShared(profile.ImpactSound, point);
    }

    private void Register()
    {
        if (registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Register(this);
        registered = true;
    }

    private void Unregister()
    {
        if (!registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Unregister(this);
        registered = false;
    }

    private void ResolveReferences()
    {
        if (Body == null) Body = GetComponent<Rigidbody>();
        if (BodyCollider == null) BodyCollider = GetComponent<Collider>();
    }

    public void OUTL_OnPoolSpawn()
    {
        ResetState();
    }

    public void OUTL_OnPoolRelease()
    {
        Unregister();
        ResetState();
    }

    private void ResetState()
    {
        ResolveReferences();
        source = OUTL_EntityId.None;
        profile = null;
        releaseTime = 0f;
        bounceCount = 0;
        launched = false;
        released = false;
        lastCollider = null;
        lastCollisionFrame = -1;
        if (Body != null)
        {
            Body.velocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Body.Sleep();
        }
    }

    private static float ReadTime()
    {
        OUTL_World world = OUTL_World.Instance;
        return world != null ? world.WorldTime : Time.time;
    }
}
