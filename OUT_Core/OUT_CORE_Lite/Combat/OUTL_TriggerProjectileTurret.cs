using UnityEngine;

[DisallowMultipleComponent]
public class OUTL_TriggerProjectileTurret : MonoBehaviour, OUTL_ITickable
{
    public OUTL_EntityAdapter Source;
    public OUTL_AttackProfile ProjectileAttack;
    public GameObject ProjectilePrefab;
    public Transform Muzzle;
    public Transform AimPivot;
    public bool RequireEntityTarget = true;
    public string[] TargetTags = new[] { "Role.Targetable" };
    public LayerMask TargetMask = ~0;
    public LayerMask LineOfSightMask = ~0;
    public bool RequireLineOfSight = true;
    public float FireInterval = 0.5f;
    public float ProjectileSpeedOverride = -1f;
    public float AimLeadStrength = 0.85f;
    public float MaxLeadTime = 1.25f;
    public float HorizontalSpreadDegrees = 0f;
    public float VerticalSpreadDegrees = 0f;
    public bool RotatePivotToTarget = true;
    public float PivotTurnSpeed = 720f;
    public bool DebugDraw;

    [Header("OUTL Tick")]
    public bool AutoRegister = true;
    public OUTL_TickLane TickLane = OUTL_TickLane.Logic;
    public float TickInterval = 0.05f;

    private readonly Collider[] targets = new Collider[64];
    private readonly OUTL_EntityAdapter[] targetEntities = new OUTL_EntityAdapter[64];
    private readonly Vector3[] lastPositions = new Vector3[64];
    private readonly Vector3[] velocities = new Vector3[64];
    private readonly bool[] hasSample = new bool[64];
    private int targetCount;
    private float nextFireTime;
    private bool registered;

    private void Awake() { ResolveReferences(); }
    private void OnEnable() { ResolveReferences(); ClearTargets(); nextFireTime = 0f; if (AutoRegister) Register(); }
    private void OnDisable() { Unregister(); ClearTargets(); }

    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled; } }
    public OUTL_TickLane OUTL_TickLane { get { return TickLane; } }
    public float OUTL_TickInterval { get { return Mathf.Max(0.01f, TickInterval); } }

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

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        float dt = deltaTime > 0f ? deltaTime : ReadDeltaTime();
        float now = world != null ? time : ReadWorldTime();
        PruneTargets(dt);
        if (targetCount <= 0 || ProjectileAttack == null) return;
        int targetIndex = FindBestTargetIndex();
        if (targetIndex < 0) return;
        Collider targetCollider = targets[targetIndex];
        Vector3 targetPoint = GetTargetPoint(targetCollider);
        Vector3 velocity = velocities[targetIndex] * Mathf.Clamp01(AimLeadStrength);
        Vector3 origin = Muzzle != null ? Muzzle.position : transform.position;
        if (RequireLineOfSight && !HasLineOfSight(origin, targetCollider, targetPoint)) return;
        Vector3 cleanDirection = BuildDirection(origin, targetPoint, velocity, false);
        if (RotatePivotToTarget && AimPivot != null && cleanDirection.sqrMagnitude > 0.001f)
            AimPivot.rotation = Quaternion.RotateTowards(AimPivot.rotation, Quaternion.LookRotation(cleanDirection), PivotTurnSpeed * dt);
        if (now < nextFireTime) return;
        nextFireTime = now + Mathf.Max(0.01f, FireInterval);
        Fire(BuildDirection(origin, targetPoint, velocity, true));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (!LayerMatches(other.gameObject.layer, TargetMask)) return;
        OUTL_EntityAdapter entity;
        if (!TryResolveValidTarget(other, out entity)) return;
        AddTarget(other, entity);
    }

    private void OnTriggerExit(Collider other) { RemoveTarget(other); }

    private void AddTarget(Collider target, OUTL_EntityAdapter entity)
    {
        if (target == null) return;
        if (RequireEntityTarget && entity == null) return;
        if (entity != null)
        {
            for (int i = 0; i < targetCount; i++) if (targetEntities[i] != null && targetEntities[i].Id == entity.Id) return;
        }
        else
        {
            for (int i = 0; i < targetCount; i++) if (targets[i] == target) return;
        }
        if (targetCount >= targets.Length) return;
        targets[targetCount] = target;
        targetEntities[targetCount] = entity;
        lastPositions[targetCount] = GetTargetPoint(target);
        velocities[targetCount] = Vector3.zero;
        hasSample[targetCount] = false;
        targetCount++;
    }

    private void RemoveTarget(Collider target)
    {
        for (int i = 0; i < targetCount; i++) if (targets[i] == target) { RemoveAt(i); return; }
    }

    private void ClearTargets()
    {
        for (int i = 0; i < targetCount; i++)
        {
            targets[i] = null;
            targetEntities[i] = null;
            hasSample[i] = false;
            velocities[i] = Vector3.zero;
            lastPositions[i] = Vector3.zero;
        }
        targetCount = 0;
    }

    private void PruneTargets(float dt)
    {
        for (int i = targetCount - 1; i >= 0; i--)
        {
            Collider c = targets[i];
            if (c == null || !c.gameObject.activeInHierarchy) { RemoveAt(i); continue; }
            OUTL_EntityAdapter entity;
            if (!TryResolveValidTarget(c, out entity)) { RemoveAt(i); continue; }
            targetEntities[i] = entity;
            Vector3 pos = GetTargetPoint(c);
            float safeDt = Mathf.Max(0.02f, dt);
            if (hasSample[i]) velocities[i] = Vector3.Lerp(velocities[i], (pos - lastPositions[i]) / safeDt, 0.25f);
            lastPositions[i] = pos;
            hasSample[i] = true;
        }
    }

    private void RemoveAt(int index)
    {
        int last = targetCount - 1;
        targets[index] = targets[last];
        targetEntities[index] = targetEntities[last];
        lastPositions[index] = lastPositions[last];
        velocities[index] = velocities[last];
        hasSample[index] = hasSample[last];
        targets[last] = null;
        targetEntities[last] = null;
        lastPositions[last] = Vector3.zero;
        velocities[last] = Vector3.zero;
        hasSample[last] = false;
        targetCount--;
    }

    private int FindBestTargetIndex()
    {
        Vector3 origin = Muzzle != null ? Muzzle.position : transform.position;
        int best = -1;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < targetCount; i++)
        {
            Collider c = targets[i];
            if (c == null) continue;
            float sqr = (GetTargetPoint(c) - origin).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = i; }
        }
        return best;
    }

    private bool TryResolveValidTarget(Collider c, out OUTL_EntityAdapter entity)
    {
        entity = null;
        if (c == null) return false;
        if (!LayerMatches(c.gameObject.layer, TargetMask)) return false;
        if (!OUTL_Combat.TryGetEntityFromCollider(c, out entity)) return !RequireEntityTarget;
        if (Source != null && entity.Id == Source.Id) return false;
        if (TargetTags == null || TargetTags.Length == 0) return true;
        if (entity.Runtime == null) return false;
        for (int i = 0; i < TargetTags.Length; i++)
            if (!string.IsNullOrEmpty(TargetTags[i]) && entity.Runtime.HasTag(TargetTags[i])) return true;
        return false;
    }

    private Vector3 GetTargetPoint(Collider c)
    {
        if (c == null) return transform.position;
        return c.bounds.center;
    }

    private bool HasLineOfSight(Vector3 origin, Collider targetCollider, Vector3 targetPoint)
    {
        Vector3 dir = targetPoint - origin;
        float dist = dir.magnitude;
        if (dist <= 0.05f) return true;
        RaycastHit hit;
        OUTL_Profile.Frame.Raycasts++;
        if (!Physics.Raycast(origin, dir / dist, out hit, dist, LineOfSightMask, QueryTriggerInteraction.Ignore)) return true;
        if (hit.collider == targetCollider) return true;
        OUTL_EntityAdapter hitEntity = hit.collider != null ? hit.collider.GetComponentInParent<OUTL_EntityAdapter>() : null;
        OUTL_EntityAdapter targetEntity = targetCollider != null ? targetCollider.GetComponentInParent<OUTL_EntityAdapter>() : null;
        return hitEntity != null && targetEntity != null && hitEntity.Id == targetEntity.Id;
    }

    private Vector3 BuildDirection(Vector3 origin, Vector3 targetPoint, Vector3 targetVelocity, bool applySpread)
    {
        Vector3 dir = OUTL_BallisticAimUtility.BuildAimDirection(origin, targetPoint, ProjectileAttack, targetVelocity, ProjectileSpeedOverride, applySpread);
        if (applySpread && (HorizontalSpreadDegrees > 0f || VerticalSpreadDegrees > 0f))
        {
            Quaternion baseRotation = SafeLookRotation(dir, transform.forward);
            int sourceId = Source != null && Source.Id.IsValid ? Source.Id.Value : 0;
            int salt = Mathf.FloorToInt(ReadWorldTime() * 1000f) ^ sourceId;
            float pitch = OUTL_HumanRandom.ValueSigned(0x7A471C1u, salt, targetCount + 17) * VerticalSpreadDegrees;
            float yaw = OUTL_HumanRandom.ValueSigned(0x7A471C2u, salt, targetCount + 29) * HorizontalSpreadDegrees;
            dir = baseRotation * Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;
        }
        return SafeDirection(dir, transform.forward);
    }

    private void Fire(Vector3 direction)
    {
        GameObject prefab = ProjectilePrefab != null ? ProjectilePrefab : (ProjectileAttack != null ? ProjectileAttack.ProjectilePrefab : null);
        if (prefab == null) return;
        Vector3 origin = Muzzle != null ? Muzzle.position : transform.position;
        direction = SafeDirection(direction, Muzzle != null ? Muzzle.forward : transform.forward);
        Quaternion rotation = SafeLookRotation(direction, transform.forward);
        GameObject go = OUTL_PoolSystem.SpawnShared(prefab, origin, rotation);
        if (go == null) return;
        OUTL_Projectile projectile = go.GetComponent<OUTL_Projectile>();
        if (projectile == null)
        {
            Debug.LogWarning("Projectile prefab must contain OUTL_Projectile for pooled turret spawn. Prefab=" + prefab.name, prefab);
            OUTL_PoolSystem.ReleaseShared(go);
            return;
        }
        projectile.Launch(Source != null ? Source.Id : OUTL_EntityId.None, ProjectileAttack, direction);
        if (ProjectileAttack != null && ProjectileAttack.MuzzleVFX != null) OUTL_PoolSystem.SpawnShared(ProjectileAttack.MuzzleVFX, origin, rotation);
        if (ProjectileAttack != null && ProjectileAttack.FireSound != null) OUTL_PoolSystem.PlayClipShared(ProjectileAttack.FireSound, origin);
        if (DebugDraw) Debug.DrawRay(origin, direction * 8f, Color.red, FireInterval);
    }

    private static Vector3 SafeDirection(Vector3 direction, Vector3 fallback)
    {
        if (direction.sqrMagnitude > 0.001f) return direction.normalized;
        return fallback.sqrMagnitude > 0.001f ? fallback.normalized : Vector3.forward;
    }

    private static Quaternion SafeLookRotation(Vector3 direction, Vector3 fallback)
    {
        return Quaternion.LookRotation(SafeDirection(direction, fallback));
    }

    private void ResolveReferences()
    {
        if (Source == null) Source = GetComponentInParent<OUTL_EntityAdapter>();
        if (Muzzle == null) Muzzle = transform;
        if (AimPivot == null) AimPivot = Muzzle;
    }

    private static bool LayerMatches(int layer, LayerMask mask) { return (mask.value & (1 << layer)) != 0; }
    private static float ReadWorldTime() { OUTL_World world = OUTL_World.Instance; return world != null ? world.WorldTime : Time.time; }
    private static float ReadDeltaTime() { OUTL_World world = OUTL_World.Instance; return world != null ? (world.IsPaused ? 0f : world.DeltaTime) : Time.deltaTime; }
}
