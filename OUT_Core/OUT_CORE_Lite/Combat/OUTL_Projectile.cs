using UnityEngine;

[DisallowMultipleComponent]
public class OUTL_Projectile : MonoBehaviour, OUTL_IPoolReset, OUTL_ITickable, OUTL_ILaunchableProjectile
{
    public OUTL_EntityId Source;
    public OUTL_AttackProfile Profile;
    public Vector3 Direction;
    public Vector3 Velocity;
    public bool DestroyOnHit = true;
    public bool AutoRegisterOnLaunch = true;
    public OUTL_TickLane TickLane = OUTL_TickLane.Logic;
    public float TickInterval = 0.01f;

    private float deathTime;
    private bool launched;
    private bool released;
    private bool registered;
    private int bounceCount;
    private Collider lastHitCollider;
    private int lastHitFrame = -1;

    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled && launched && !released; } }
    public OUTL_TickLane OUTL_TickLane { get { return TickLane; } }
    public float OUTL_TickInterval { get { return Mathf.Max(0.01f, TickInterval); } }

    public void Launch(OUTL_EntityId source, OUTL_AttackProfile profile, Vector3 direction)
    {
        Source = source;
        Profile = profile;
        Direction = direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
        if (Direction.sqrMagnitude <= 0.001f) Direction = Vector3.forward;
        Velocity = Direction.normalized * (profile != null ? Mathf.Max(0.01f, profile.ProjectileSpeed) : 28f);
        deathTime = ReadWorldTime() + (profile != null ? Mathf.Max(0.1f, profile.ProjectileLifetime) : 8f);
        bounceCount = 0;
        launched = true;
        released = false;
        if (AutoRegisterOnLaunch) RegisterTick();
    }

    public void OUTL_Launch(OUTL_EntityId source, OUTL_AttackProfile profile, Vector3 direction)
    {
        Launch(source, profile, direction);
    }

    private void OnEnable()
    {
        ResetTransientState(false);
    }

    private void OnDisable()
    {
        UnregisterTick();
    }

    public void OUTL_OnPoolSpawn()
    {
        ResetTransientState(false);
    }

    public void OUTL_OnPoolRelease()
    {
        UnregisterTick();
        ResetTransientState(true);
    }

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        if (!launched || released) return;
        if (Profile == null)
        {
            ReleaseSelf();
            return;
        }

        float dt = deltaTime > 0f ? deltaTime : ReadDeltaTime();
        if (dt <= 0f) return;

        if (Profile.ProjectileUsesGravity)
            Velocity += Vector3.down * Mathf.Max(0f, Profile.ProjectileGravity) * dt;

        Vector3 oldPos = transform.position;
        Vector3 step = Velocity * dt;
        Vector3 dir = step.sqrMagnitude > 0.0001f ? step.normalized : Direction;
        if (dir.sqrMagnitude <= 0.001f) dir = transform.forward.sqrMagnitude > 0.001f ? transform.forward : Vector3.forward;
        float dist = step.magnitude;

        RaycastHit hit;
        if (dist > 0f)
        {
            OUTL_Profile.Frame.Raycasts++;
            if (Physics.Raycast(oldPos, dir, out hit, dist + 0.05f, Profile.HitMask, QueryTriggerInteraction.Ignore))
            {
                HandleHit(hit.collider, hit.point, hit.normal, false);
                return;
            }
        }

        transform.position = oldPos + step;
        if (Velocity.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(Velocity.normalized);

        float now = world != null ? time : ReadWorldTime();
        if (now >= deathTime)
        {
            if (Profile.ProjectileDetonateOnLifetimeEnd || Profile.HasExplosion)
                Explode(transform.position, -Direction);
            else
                ReleaseSelf();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!launched || released || Profile == null) return;
        if (Profile.ProjectileIgnoreTriggers && other != null && other.isTrigger) return;
        HandleHit(other, transform.position, -Direction, true);
    }

    private void HandleHit(Collider collider, Vector3 point, Vector3 normal, bool triggerHit)
    {
        if (released || Profile == null) return;
        if (collider == null) return;
        if (Profile.ProjectileIgnoreTriggers && collider.isTrigger) return;
        if (lastHitCollider == collider && lastHitFrame == Time.frameCount) return;
        lastHitCollider = collider;
        lastHitFrame = Time.frameCount;

        OUTL_EntityAdapter target;
        float multiplier;
        string suffix;
        bool hitEntity = OUTL_Hitbox.Resolve(collider, out target, out multiplier, out suffix);
        if (hitEntity && target.Id == Source) return;

        if (hitEntity)
        {
            if (Profile.ProjectileDetonateOnEntityHit || Profile.HasExplosion)
            {
                Explode(point, normal);
                return;
            }

            string key = OUTL_Hitbox.BuildDamageKey(Profile.HitDamageKey, suffix);
            OUTL_Combat.ApplyDamage(Source, target.Id, Profile.Damage * Mathf.Max(0f, multiplier), point, key, Profile.ExtraHitEffects);
            SpawnImpact(point, normal);
            if (DestroyOnHit) ReleaseSelf();
            return;
        }

        if (Profile.ProjectileDetonateOnWorldHit)
        {
            Explode(point, normal);
            return;
        }

        if (Profile.ProjectileBounceOnWorldHit && CanBounce())
        {
            Bounce(point, normal);
            return;
        }

        SpawnImpact(point, normal);
        if (DestroyOnHit) ReleaseSelf();
    }

    private bool CanBounce()
    {
        return Profile != null && Profile.ProjectileBounceOnWorldHit && (Profile.ProjectileMaxBounces < 0 || bounceCount < Profile.ProjectileMaxBounces);
    }

    private void Bounce(Vector3 point, Vector3 normal)
    {
        bounceCount++;
        Vector3 n = normal.sqrMagnitude > 0.001f ? normal.normalized : Vector3.up;
        Velocity = Vector3.Reflect(Velocity, n) * Mathf.Max(0f, Profile.ProjectileBounceDamping);
        Vector3 tangent = Vector3.ProjectOnPlane(Velocity, n) * Mathf.Max(0f, Profile.ProjectileFrictionDamping);
        Vector3 vertical = Vector3.Project(Velocity, n);
        Velocity = tangent + vertical;
        Direction = Velocity.sqrMagnitude > 0.001f ? Velocity.normalized : Direction;
        transform.position = point + n * 0.04f;
        if (Velocity.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(Velocity.normalized);
    }

    private void Explode(Vector3 point, Vector3 normal)
    {
        if (released) return;

        if (Profile != null && Profile.HasExplosion)
            OUTL_Combat.ApplyExplosion(Source, Profile, point);
        else if (Profile != null)
        {
            OUTL_EntityAdapter target;
            float multiplier;
            string suffix;
            RaycastHit hit;
            OUTL_Profile.Frame.Raycasts++;
            if (Physics.Raycast(point + Direction * -0.1f, Direction, out hit, 0.2f, Profile.HitMask, QueryTriggerInteraction.Ignore) && OUTL_Hitbox.Resolve(hit.collider, out target, out multiplier, out suffix) && target.Id != Source)
            {
                string key = OUTL_Hitbox.BuildDamageKey(Profile.HitDamageKey, suffix);
                OUTL_Combat.ApplyDamage(Source, target.Id, Profile.Damage * Mathf.Max(0f, multiplier), point, key, Profile.ExtraHitEffects);
            }
        }

        SpawnImpact(point, normal);
        ReleaseSelf();
    }

    private void SpawnImpact(Vector3 point, Vector3 normal)
    {
        if (Profile != null && Profile.ImpactVFX != null)
            OUTL_PoolSystem.SpawnShared(Profile.ImpactVFX, point, normal.sqrMagnitude > 0.001f ? Quaternion.LookRotation(normal) : Quaternion.identity);

        if (Profile != null && Profile.ImpactSound != null)
            OUTL_PoolSystem.PlayClipShared(Profile.ImpactSound, point);
    }

    private void ReleaseSelf()
    {
        if (released) return;
        UnregisterTick();
        released = true;
        launched = false;
        Source = OUTL_EntityId.None;
        Profile = null;
        Velocity = Vector3.zero;
        Direction = Vector3.zero;
        OUTL_PoolSystem.ReleaseShared(gameObject);
    }

    private void RegisterTick()
    {
        if (registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Register(this);
        registered = true;
    }

    private void UnregisterTick()
    {
        if (!registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Unregister(this);
        registered = false;
    }

    private void ResetTransientState(bool markReleased)
    {
        launched = false;
        released = markReleased;
        Source = OUTL_EntityId.None;
        Profile = null;
        Direction = Vector3.zero;
        Velocity = Vector3.zero;
        bounceCount = 0;
        deathTime = 0f;
        lastHitCollider = null;
        lastHitFrame = -1;
    }

    private static float ReadWorldTime()
    {
        OUTL_World world = OUTL_World.Instance;
        return world != null ? world.WorldTime : Time.time;
    }

    private static float ReadDeltaTime()
    {
        OUTL_World world = OUTL_World.Instance;
        if (world != null) return world.IsPaused ? 0f : world.DeltaTime;
        return Time.deltaTime;
    }
}
