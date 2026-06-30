using UnityEngine;

[DisallowMultipleComponent]
public class OUTL_AttackDriver : MonoBehaviour, OUTL_IPoolReset
{
    public OUTL_EntityAdapter Source;
    public Transform Muzzle;
    public Camera AimCamera;
    public OUTL_AttackProfile Primary;
    public OUTL_AttackProfile Secondary;
    public OUTL_AttackProfile Melee;

    [Header("Runtime Gates")]
    public bool BlockedByVitals;
    public bool BlockWhenSourceDead = true;

    [Header("AI / FireAt")]
    public bool SmartMeleeWhenFireAtPrimary = true;
    public bool RespectCooldownOnFireAt = true;
    public float SmartMeleeExtraRange = 0.15f;

    [Header("Melee Volume")]
    public bool UseCapsuleMeleeVolume = true;
    public float MeleeMinRadius = 0.45f;
    public float MeleeHeight = 1.35f;
    public float MeleeForwardBias = 0.55f;

    private readonly Collider[] overlapBuffer = new Collider[64];
    private readonly int[] hitEntityIds = new int[64];
    private int hitEntityCount;
    private float nextPrimary;
    private float nextSecondary;
    private float nextMelee;
    private float nextExternal;
    private Vector3 lastForcedTargetPoint;
    private Vector3 forcedTargetVelocity;
    private bool hasForcedTargetSample;
    private int shotSequence;

    private void Awake()
    {
        ResolveReferences();
    }

    public bool FirePrimary() { return Fire(Primary, ref nextPrimary); }
    public bool FireSecondary() { return Fire(Secondary, ref nextSecondary); }
    public bool FireMelee() { return Fire(Melee, ref nextMelee); }

    public bool FireAt(OUTL_AttackProfile profile, Vector3 targetPoint)
    {
        return FireAt(profile, targetPoint, Vector3.zero);
    }

    public bool FireAt(OUTL_AttackProfile profile, Vector3 targetPoint, Vector3 targetVelocity)
    {
        forcedTargetVelocity = targetVelocity;
        if (targetVelocity.sqrMagnitude <= 0.001f && targetPoint != Vector3.zero)
        {
            if (hasForcedTargetSample)
            {
                float dt = Mathf.Max(0.02f, ReadDeltaTime());
                forcedTargetVelocity = (targetPoint - lastForcedTargetPoint) / dt;
            }
            lastForcedTargetPoint = targetPoint;
            hasForcedTargetSample = true;
        }

        if (SmartMeleeWhenFireAtPrimary && profile == Primary && Melee != null)
        {
            Vector3 origin = GetOrigin();
            float meleeRange = Mathf.Max(0.05f, Melee.Range + Melee.Radius + SmartMeleeExtraRange);
            if ((targetPoint - origin).sqrMagnitude <= meleeRange * meleeRange)
                profile = Melee;
        }

        if (!RespectCooldownOnFireAt)
        {
            float dummy = 0f;
            return FireInternal(profile, ref dummy, true, targetPoint);
        }

        if (profile == Primary) return FireInternal(profile, ref nextPrimary, false, targetPoint);
        if (profile == Secondary) return FireInternal(profile, ref nextSecondary, false, targetPoint);
        if (profile == Melee) return FireInternal(profile, ref nextMelee, false, targetPoint);
        return FireInternal(profile, ref nextExternal, false, targetPoint);
    }

    private bool Fire(OUTL_AttackProfile profile, ref float cooldownTime)
    {
        forcedTargetVelocity = Vector3.zero;
        return FireInternal(profile, ref cooldownTime, false, Vector3.zero);
    }

    private bool FireInternal(OUTL_AttackProfile profile, ref float cooldownTime, bool ignoreCooldown, Vector3 forcedTargetPoint)
    {
        if (profile == null || IsBlocked()) return false;
        float now = ReadWorldTime();
        if (!ignoreCooldown && now < cooldownTime) return false;
        if (!ignoreCooldown) cooldownTime = now + Mathf.Max(0f, profile.Cooldown);

        Vector3 origin = GetOrigin();
        Vector3 direction = SafeDirection(GetDirection(profile, origin, forcedTargetPoint, profile.ProjectilesPerShot <= 1));

        Spawn(profile.MuzzleVFX, origin, SafeLookRotation(direction));
        if (profile.FireSound != null) OUTL_PoolSystem.PlayClipShared(profile.FireSound, origin);
        TraceFire(profile, origin, direction, forcedTargetPoint);

        switch (profile.Mode)
        {
            case OUTL_AttackMode.Projectile:
                SpawnProjectile(profile, origin, direction);
                return true;
            case OUTL_AttackMode.Melee:
                MeleeAttack(profile, origin, direction);
                return true;
            case OUTL_AttackMode.Direct:
                DirectAttack(profile, forcedTargetPoint);
                return true;
            case OUTL_AttackMode.Hitscan:
            default:
                HitscanAttack(profile, origin, direction);
                return true;
        }
    }

    private bool IsBlocked()
    {
        if (BlockedByVitals) return true;
        if (!BlockWhenSourceDead || Source == null || Source.Runtime == null) return false;
        return Source.Runtime.State.GetFlag(OUTL_StateId.Dead) || Source.Runtime.Stats.Get(OUTL_StatId.Health, 1f) <= 0f;
    }

    private Vector3 GetOrigin()
    {
        if (Muzzle != null) return Muzzle.position;
        return transform.position + Vector3.up;
    }

    private Vector3 GetDirection(OUTL_AttackProfile profile, Vector3 origin, Vector3 forcedTargetPoint, bool applySpread)
    {
        if (forcedTargetPoint != Vector3.zero)
            return OUTL_BallisticAimUtility.BuildAimDirection(origin, forcedTargetPoint, profile, forcedTargetVelocity, -1f, applySpread);

        if (AimCamera != null)
            return OUTL_BallisticAimUtility.BuildAimDirection(origin, origin + AimCamera.transform.forward * Mathf.Max(1f, profile != null ? profile.Range : 40f), profile, Vector3.zero, -1f, applySpread);

        Vector3 forward = Muzzle != null ? Muzzle.forward : transform.forward;
        return OUTL_BallisticAimUtility.BuildAimDirection(origin, origin + forward * Mathf.Max(1f, profile != null ? profile.Range : 40f), profile, Vector3.zero, -1f, applySpread);
    }

    private void HitscanAttack(OUTL_AttackProfile profile, Vector3 origin, Vector3 direction)
    {
        RaycastHit hit;
        OUTL_Profile.Frame.Raycasts++;
        if (!Physics.Raycast(origin, direction, out hit, profile.Range, profile.HitMask, QueryTriggerInteraction.Ignore)) return;
        ApplyHit(profile, hit.collider, hit.point, hit.normal);
    }

    private void MeleeAttack(OUTL_AttackProfile profile, Vector3 origin, Vector3 direction)
    {
        hitEntityCount = 0;
        Vector3 flatDirection = direction;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude <= 0.001f) flatDirection = transform.forward;
        flatDirection.Normalize();

        int count;
        float radius = Mathf.Max(0.05f, profile.Radius, profile.MeleeMinRadius, MeleeMinRadius);
        float reach = Mathf.Max(0.1f, profile.Range);
        float height = Mathf.Max(0.36f, profile.MeleeHeight > 0f ? profile.MeleeHeight : MeleeHeight);
        float forwardBias = Mathf.Max(0.1f, profile.MeleeForwardBias > 0f ? profile.MeleeForwardBias : MeleeForwardBias);
        QueryTriggerInteraction triggerPolicy = profile.MeleeCanHitTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
        OUTL_Profile.Frame.Overlaps++;
        if (UseCapsuleMeleeVolume)
        {
            Vector3 baseCenter = transform.position + flatDirection * Mathf.Max(0.1f, reach * forwardBias);
            Vector3 p0 = baseCenter + Vector3.up * 0.35f;
            Vector3 p1 = baseCenter + Vector3.up * height;
            count = Physics.OverlapCapsuleNonAlloc(p0, p1, radius, overlapBuffer, profile.HitMask, triggerPolicy);
        }
        else
        {
            Vector3 center = origin + flatDirection * Mathf.Max(0.1f, reach * 0.5f);
            count = Physics.OverlapSphereNonAlloc(center, radius, overlapBuffer, profile.HitMask, triggerPolicy);
        }

        for (int i = 0; i < count; i++)
        {
            OUTL_EntityAdapter target;
            float multiplier;
            string suffix;
            if (!OUTL_Hitbox.Resolve(overlapBuffer[i], out target, out multiplier, out suffix)) continue;
            if (Source != null && target.Id == Source.Id) continue;
            if (!IsInsideMeleeArc(profile, target, origin, flatDirection, reach, radius)) continue;
            if (WasEntityHit(target.Id.Value)) continue;
            AddHitEntity(target.Id.Value);
            ApplyHit(profile, overlapBuffer[i], target.transform.position + Vector3.up, -flatDirection);
        }
    }

    private void DirectAttack(OUTL_AttackProfile profile, Vector3 targetPoint)
    {
        Vector3 origin = GetOrigin();
        Vector3 center = targetPoint != Vector3.zero ? targetPoint : origin + transform.forward * profile.Range;
        OUTL_Profile.Frame.Overlaps++;
        int count = Physics.OverlapSphereNonAlloc(center, Mathf.Max(0.05f, profile.Radius), overlapBuffer, profile.HitMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            OUTL_EntityAdapter target;
            float multiplier;
            string suffix;
            if (!OUTL_Hitbox.Resolve(overlapBuffer[i], out target, out multiplier, out suffix)) continue;
            if (Source != null && target.Id == Source.Id) continue;
            ApplyHit(profile, overlapBuffer[i], target.transform.position + Vector3.up, Vector3.up);
            return;
        }
    }

    private bool WasEntityHit(int entityId)
    {
        for (int i = 0; i < hitEntityCount; i++)
            if (hitEntityIds[i] == entityId)
                return true;
        return false;
    }

    private void AddHitEntity(int entityId)
    {
        if (hitEntityCount >= hitEntityIds.Length) return;
        hitEntityIds[hitEntityCount++] = entityId;
    }

    private static bool IsInsideMeleeArc(OUTL_AttackProfile profile, OUTL_EntityAdapter target, Vector3 origin, Vector3 forward, float reach, float radius)
    {
        if (profile == null || target == null) return false;
        Vector3 targetPoint = target.transform.position + Vector3.up;
        Collider c = target.GetComponentInChildren<Collider>();
        if (c != null) targetPoint = c.bounds.center;
        Vector3 toTarget = targetPoint - origin;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;
        if (distance <= Mathf.Max(0.1f, radius)) return true;
        if (distance > reach + radius) return false;
        if (toTarget.sqrMagnitude <= 0.001f) return true;
        float arc = Mathf.Clamp(profile.MeleeArcDegrees <= 0f ? 360f : profile.MeleeArcDegrees, 1f, 360f);
        if (arc >= 359f) return true;
        float dot = Vector3.Dot(forward.normalized, toTarget / distance);
        float minDot = Mathf.Cos(arc * 0.5f * Mathf.Deg2Rad);
        return dot >= minDot;
    }

    private void ApplyHit(OUTL_AttackProfile profile, Collider collider, Vector3 point, Vector3 normal)
    {
        OUTL_EntityAdapter target;
        float multiplier;
        string suffix;
        if (OUTL_Hitbox.Resolve(collider, out target, out multiplier, out suffix))
        {
            OUTL_EntityId sourceId = Source != null ? Source.Id : OUTL_EntityId.None;
            string key = OUTL_Hitbox.BuildDamageKey(profile != null ? profile.HitDamageKey : string.Empty, suffix);
            OUTL_Combat.ApplyDamage(sourceId, target.Id, profile.Damage * Mathf.Max(0f, multiplier), point, key, profile.ExtraHitEffects);
        }

        Spawn(profile.ImpactVFX, point, normal.sqrMagnitude > 0.001f ? SafeLookRotation(normal) : Quaternion.identity);
        if (profile.ImpactSound != null) OUTL_PoolSystem.PlayClipShared(profile.ImpactSound, point);
    }

    private void SpawnProjectile(OUTL_AttackProfile profile, Vector3 origin, Vector3 direction)
    {
        GameObject prefab = profile.ProjectilePrefab;
        if (prefab == null) return;
        int count = Mathf.Max(1, profile.ProjectilesPerShot);
        int sequence = ++shotSequence;
        for (int i = 0; i < count; i++)
        {
            Vector3 projectileDirection = count > 1 ? ApplyPatternSpread(direction, profile, sequence, i) : direction;
            GameObject go = OUTL_PoolSystem.SpawnShared(prefab, origin, SafeLookRotation(projectileDirection));
            if (go == null) continue;
            OUTL_ILaunchableProjectile projectile = go.GetComponent<OUTL_ILaunchableProjectile>();
            if (projectile == null)
            {
                Debug.LogWarning("Projectile prefab must implement OUTL_ILaunchableProjectile for pooled runtime spawn. Prefab=" + prefab.name, prefab);
                OUTL_PoolSystem.ReleaseShared(go);
                continue;
            }
            projectile.OUTL_Launch(Source != null ? Source.Id : OUTL_EntityId.None, profile, projectileDirection);
        }
    }

    private Vector3 ApplyPatternSpread(Vector3 direction, OUTL_AttackProfile profile, int sequence, int projectileIndex)
    {
        if (profile == null) return direction;
        int source = Source != null && Source.Id.IsValid ? Source.Id.Value : GetInstanceID();
        int salt = unchecked(source * 486187739 + sequence * 16777619 + projectileIndex * 31);
        float yaw = OUTL_HumanRandom.ValueSigned(0x53484F54u, salt, projectileIndex + 11) * Mathf.Max(0f, profile.HorizontalSpreadDegrees);
        float pitch = OUTL_HumanRandom.ValueSigned(0x50454C4Cu, salt, projectileIndex + 29) * Mathf.Max(0f, profile.VerticalSpreadDegrees);
        return (SafeLookRotation(direction) * Quaternion.Euler(pitch, yaw, 0f)) * Vector3.forward;
    }

    private static void Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab != null) OUTL_PoolSystem.SpawnShared(prefab, position, rotation);
    }

    private void ResolveReferences()
    {
        if (Source == null) Source = GetComponent<OUTL_EntityAdapter>();
        if (Muzzle == null) Muzzle = transform;
        if (AimCamera == null)
        {
            OUTL_BasicPlayerController player = GetComponent<OUTL_BasicPlayerController>();
            if (player != null) AimCamera = player.ViewCamera;
        }
    }

    private void ResetRuntimeState()
    {
        hitEntityCount = 0;
        nextPrimary = 0f;
        nextSecondary = 0f;
        nextMelee = 0f;
        nextExternal = 0f;
        lastForcedTargetPoint = Vector3.zero;
        forcedTargetVelocity = Vector3.zero;
        hasForcedTargetSample = false;
        shotSequence = 0;
        BlockedByVitals = false;
    }

    private static Vector3 SafeDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f) return Vector3.forward;
        return direction.normalized;
    }

    private static Quaternion SafeLookRotation(Vector3 direction)
    {
        direction = SafeDirection(direction);
        return Quaternion.LookRotation(direction);
    }

    private static float ReadWorldTime()
    {
        OUTL_World world = OUTL_World.Instance;
        return world != null ? world.WorldTime : Time.time;
    }

    private static float ReadDeltaTime()
    {
        OUTL_World world = OUTL_World.Instance;
        if (world != null) return Mathf.Max(0.001f, world.DeltaTime);
        return Mathf.Max(0.001f, Time.deltaTime);
    }

    private void TraceFire(OUTL_AttackProfile profile, Vector3 origin, Vector3 direction, Vector3 forcedTargetPoint)
    {
        if (!OUTL_DebugLog.ShouldTraceCombat() || profile == null) return;
        string source = DescribeSource();
        string targetPoint = forcedTargetPoint != Vector3.zero ? " targetPoint=" + FormatPoint(forcedTargetPoint) : string.Empty;
        string attackId = string.IsNullOrEmpty(profile.AttackId) ? profile.name : profile.AttackId;
        OUTL_DebugLog.TraceCombat("FIRE " + source + " attack=" + attackId + " mode=" + profile.Mode + " dmg=" + profile.Damage.ToString("0.##") + " origin=" + FormatPoint(origin) + " dir=" + FormatPoint(direction) + targetPoint);
    }

    private string DescribeSource()
    {
        if (Source == null) return "unbound";
        OUTL_EntityRuntime runtime = Source.Runtime;
        if (runtime == null) return Source.Id.IsValid ? ("entity#" + Source.Id.Value) : "unbound";
        string cls = string.IsNullOrEmpty(runtime.ClassName) ? "-" : runtime.ClassName;
        string targetName = string.IsNullOrEmpty(runtime.TargetName) ? "-" : runtime.TargetName;
        return "entity#" + runtime.Id.Value + "(" + cls + "/" + targetName + ")";
    }

    private static string FormatPoint(Vector3 point)
    {
        return "(" + point.x.ToString("0.##") + "," + point.y.ToString("0.##") + "," + point.z.ToString("0.##") + ")";
    }

    public void OUTL_OnPoolSpawn()
    {
        ResolveReferences();
        ResetRuntimeState();
    }

    public void OUTL_OnPoolRelease()
    {
        ResetRuntimeState();
    }
}
