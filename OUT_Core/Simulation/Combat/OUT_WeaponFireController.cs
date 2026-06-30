using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class OUT_WeaponFireController : MonoBehaviour
{
    [System.Serializable]
    public class OUT_FireSlot
    {
        [Header("Mode")]
        public bool Enabled = true;
        [Tooltip("—юда кидаешь компонент, который реализует IOutWeaponAttackMode.")]
        public MonoBehaviour AttackModeBehaviour;

        [Header("Damage")]
        public OUT_Q1DamageKind DamageKind = OUT_Q1DamageKind.Bullet;
        public int Damage = 10;

        [Header("Rate")]
        [Min(0.01f)] public float FireInterval = 0.12f;
        [Min(1)] public int ShotsPerBurst = 1;
        [Min(0f)] public float BurstShotInterval = 0.05f;

        [Header("Spread / Distance")]
        [Min(1)] public int Pellets = 1;
        [Min(0f)] public float SpreadDegrees = 0f;
        [Min(0.1f)] public float MaxDistance = 2048f;

        [Header("Projectile")]
        public GameObject ProjectilePrefab;
        [Min(0f)] public float ProjectileSpeed = 60f;
        [Min(0f)] public float ProjectileLifetime = 4f;

        [Header("Melee")]
        [Min(0f)] public float MeleeRadius = 0.35f;
        [Min(0f)] public float BashForce = 0f;

        [Header("VFX")]
        public GameObject MuzzleFlashPrefab;
        public GameObject ImpactVFX;

        [Header("Trace")]
        public LayerMask HitMask = ~0;
        public QueryTriggerInteraction TriggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Aim")]
        public bool UsePrediction = false;

        public IOutWeaponAttackMode AttackMode
        {
            get { return AttackModeBehaviour as IOutWeaponAttackMode; }
        }
    }

    private class OUT_FireRuntime
    {
        public float NextFireTime;
        public Coroutine ActiveBurstRoutine;
    }

    [Header("References")]
    [SerializeField] private MonoBehaviour targetingPolicyBehaviour;
    [SerializeField] private Transform fireOrigin;
    [SerializeField] private Transform muzzleFlashPoint;
    [SerializeField] private Transform instigatorRootOverride;
    [SerializeField] private GameObject instigatorOverride;

    [Header("Rules")]
    [SerializeField] private bool ignoreInstigatorRoot = true;

    [Header("Primary")]
    [SerializeField] private OUT_FireSlot primary = new OUT_FireSlot();

    [Header("Secondary")]
    [SerializeField] private OUT_FireSlot secondary = new OUT_FireSlot();

    private readonly OUT_FireRuntime _primaryRuntime = new OUT_FireRuntime();
    private readonly OUT_FireRuntime _secondaryRuntime = new OUT_FireRuntime();

    private IOutWeaponTargetingPolicy TargetingPolicy
    {
        get { return targetingPolicyBehaviour as IOutWeaponTargetingPolicy; }
    }

    private void Awake()
    {
        if (fireOrigin == null)
            fireOrigin = transform;
    }

    public bool TryFirePrimary()
    {
        return TryFireSlot(primary, _primaryRuntime);
    }

    public bool TryFireSecondary()
    {
        return TryFireSlot(secondary, _secondaryRuntime);
    }

    public void StopActiveBursts()
    {
        StopBurst(_primaryRuntime);
        StopBurst(_secondaryRuntime);
    }

    private bool TryFireSlot(OUT_FireSlot slot, OUT_FireRuntime runtime)
    {
        if (slot == null || !slot.Enabled)
            return false;

        if (slot.AttackMode == null)
            return false;

        if (TargetingPolicy == null)
            return false;

        if (Time.time < runtime.NextFireTime)
            return false;

        if (runtime.ActiveBurstRoutine != null)
            return false;

        runtime.NextFireTime = Time.time + Mathf.Max(0.01f, slot.FireInterval);
        runtime.ActiveBurstRoutine = StartCoroutine(FireBurstRoutine(slot, runtime));
        return true;
    }

    private IEnumerator FireBurstRoutine(OUT_FireSlot slot, OUT_FireRuntime runtime)
    {
        int shots = Mathf.Max(1, slot.ShotsPerBurst);

        for (int i = 0; i < shots; i++)
        {
            if (TargetingPolicy == null || fireOrigin == null)
                break;

            GameObject instigator = instigatorOverride != null ? instigatorOverride : gameObject;
            Transform instigatorRoot = instigatorRootOverride != null ? instigatorRootOverride : transform.root;

            if (!TargetingPolicy.TryBuildAimContext(instigator, fireOrigin, out OUT_WeaponAimContext aimContext))
                break;

            OUT_WeaponFireContext fireContext = BuildFireContext(slot, aimContext, instigator, instigatorRoot);

            if (slot.AttackMode != null && slot.AttackMode.CanFire(fireContext))
                slot.AttackMode.Fire(fireContext);

            if (i < shots - 1 && slot.BurstShotInterval > 0f)
                yield return new WaitForSeconds(slot.BurstShotInterval);
        }

        runtime.ActiveBurstRoutine = null;
    }

    private OUT_WeaponFireContext BuildFireContext(
        OUT_FireSlot slot,
        OUT_WeaponAimContext aimContext,
        GameObject instigator,
        Transform instigatorRoot)
    {
        return new OUT_WeaponFireContext
        {
            Instigator = instigator,
            InstigatorRoot = instigatorRoot,

            FireOrigin = fireOrigin,
            Origin = fireOrigin.position,

            Target = aimContext.Target,
            AimPoint = aimContext.AimPoint,
            AimDirection = aimContext.AimDirection,
            TargetVelocity = aimContext.TargetVelocity,

            HitMask = slot.HitMask,
            TriggerInteraction = slot.TriggerInteraction,

            Damage = Mathf.Max(0, slot.Damage),
            Pellets = Mathf.Max(1, slot.Pellets),

            MaxDistance = Mathf.Max(0.1f, slot.MaxDistance),
            SpreadDegrees = Mathf.Max(0f, slot.SpreadDegrees),

            ProjectileSpeed = Mathf.Max(0f, slot.ProjectileSpeed),
            ProjectileLifetime = Mathf.Max(0f, slot.ProjectileLifetime),

            MeleeRadius = Mathf.Max(0f, slot.MeleeRadius),
            BashForce = Mathf.Max(0f, slot.BashForce),

            IgnoreInstigatorRoot = ignoreInstigatorRoot,
            UsePrediction = slot.UsePrediction,

            ProjectilePrefab = slot.ProjectilePrefab,
            MuzzleFlashPrefab = slot.MuzzleFlashPrefab,
            MuzzleFlashPoint = muzzleFlashPoint != null ? muzzleFlashPoint : fireOrigin,
            ImpactVFX = slot.ImpactVFX,

            DamageKind = slot.DamageKind
        };
    }

    private void StopBurst(OUT_FireRuntime runtime)
    {
        if (runtime.ActiveBurstRoutine == null)
            return;

        StopCoroutine(runtime.ActiveBurstRoutine);
        runtime.ActiveBurstRoutine = null;
    }
}