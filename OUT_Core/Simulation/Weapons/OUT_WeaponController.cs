using System.Collections;
using OUTPool = OutCore.pool.OUT;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class OUT_WeaponController : MonoBehaviour
{
    public enum AimMode
    {
        FireOriginForward = 0,
        CameraRay = 1,
        ExplicitWorldPoint = 2,
        ExplicitTarget = 3,
        ExplicitTargetTransform = 3
    }

    public enum AttackModeKind
    {
        Auto = 0,
        Hitscan = 1,
        PhysicalProjectile = 2,
        MeleeTrace = 3
    }

    [System.Serializable]
    public class FireProfile
    {
        [Header("Attack Core")]
        [Tooltip("Auto chooses Physical Projectile when Projectile Prefab is assigned, Melee Trace for melee damage, otherwise Hitscan.")]
        public AttackModeKind AttackMode = AttackModeKind.Auto;

        [Header("Damage")]
        public OUT_DamageKind DamageKind = OUT_DamageKind.Bullet;
        public int Damage = 8;
        public float Impulse = 4f;

        [Header("Timing")]
        [Min(0.01f)] public float FireInterval = 0.1f;
        [Min(1)] public int ShotsPerBurst = 1;
        [Min(0f)] public float BurstShotInterval = 0.05f;

        [Header("Ammo")]
        [Min(0)] public int ClipSize = 50;
        [Min(0)] public int AmmoInClip = 50;
        [Min(0)] public int ReserveAmmo = 150;
        [Min(1)] public int AmmoPerShot = 1;
        [Min(0.01f)] public float ReloadDuration = 1.5f;
        public bool InfiniteClip = false;
        public bool InfiniteReserve = false;
        public bool AutoReloadOnEmpty = true;

        [Header("Trace / Spread")]
        [Min(0.1f)] public float MaxDistance = 2048f;
        public LayerMask HitMask = ~0;
        public QueryTriggerInteraction TriggerInteraction = QueryTriggerInteraction.Ignore;
        [Min(1)] public int PelletCount = 1;
        public Vector2 Spread = Vector2.zero;
        [Min(0f)] public float TraceRadius = 0f;

        [Header("Projectile")]
        public GameObject ProjectilePrefab;
        [Min(0f)] public float ProjectileSpeed = 120f;

        [Header("VFX")]
        public GameObject ImpactVfx;
        public GameObject FallbackImpactVfx;
        public GameObject TracerVfx;
        public GameObject MuzzleFlashVfx;

        [Header("Audio")]
        public AudioClip[] FireClips;
        public AudioClip DryFireClip;
        public AudioClip ReloadStartClip;
        public AudioClip ReloadEndClip;

        [Header("Animation")]
        public string FireTrigger = "Fire";
        public string ReloadTrigger = "Reload";

        [System.NonSerialized] private IOutAttackMode _runtimeAttackMode;
        [System.NonSerialized] private AttackModeKind _runtimeAttackModeKind;

        public IOutAttackMode RuntimeAttackMode
        {
            get
            {
                AttackModeKind resolvedKind = ResolveAttackModeKind();
                if (_runtimeAttackMode != null && _runtimeAttackModeKind == resolvedKind)
                    return _runtimeAttackMode;

                _runtimeAttackModeKind = resolvedKind;
                _runtimeAttackMode = CreateAttackMode(resolvedKind);
                return _runtimeAttackMode;
            }
        }

        public bool HasAmmoInClip
        {
            get { return InfiniteClip || AmmoInClip >= Mathf.Max(1, AmmoPerShot); }
        }

        public bool CanReload
        {
            get
            {
                if (InfiniteClip)
                    return false;

                if (AmmoInClip >= ClipSize)
                    return false;

                if (InfiniteReserve)
                    return true;

                return ReserveAmmo > 0;
            }
        }

        private AttackModeKind ResolveAttackModeKind()
        {
            if (AttackMode != AttackModeKind.Auto)
                return AttackMode;

            if (ProjectilePrefab != null)
                return AttackModeKind.PhysicalProjectile;

            if (DamageKind == OUT_DamageKind.Melee)
                return AttackModeKind.MeleeTrace;

            return AttackModeKind.Hitscan;
        }

        private IOutAttackMode CreateAttackMode(AttackModeKind kind)
        {
            switch (kind)
            {
                case AttackModeKind.PhysicalProjectile:
                    return new OUT_PhysicalProjectileAttackMode();

                case AttackModeKind.MeleeTrace:
                    return new OUT_MeleeTraceAttackMode();

                case AttackModeKind.Hitscan:
                default:
                    return new OUT_HitscanAttackMode();
            }
        }
    }

    [Header("References")]
    [SerializeField] private Transform fireOrigin;
    [SerializeField] private Camera aimCamera;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform ownerRootOverride;

    [Header("Aim")]
    [SerializeField] private AimMode aimMode = AimMode.FireOriginForward;
    [SerializeField] private LayerMask cameraAimMask = ~0;
    [SerializeField] private QueryTriggerInteraction cameraAimTriggers = QueryTriggerInteraction.Ignore;
    [SerializeField][Min(0.1f)] private float cameraAimDistance = 4096f;
    [SerializeField] private bool ignoreOwnerRoot = true;

    [Header("Optional Turret")]
    [SerializeField] private OUT_TurretAimingRig turretRig;
    [SerializeField] private bool requireTurretAlignmentToFire = true;

    [Header("Primary")]
    [SerializeField] private FireProfile primary = new FireProfile();

    [Header("Secondary")]
    [SerializeField] private FireProfile secondary = new FireProfile();

    [Header("Events")]
    [SerializeField] private UnityEvent onPrimaryFired;
    [SerializeField] private UnityEvent onSecondaryFired;
    [SerializeField] private UnityEvent onDryFire;
    [SerializeField] private UnityEvent onReloadStarted;
    [SerializeField] private UnityEvent onReloadFinished;

    private float _nextPrimaryFireTime;
    private float _nextSecondaryFireTime;
    private bool _isReloadingPrimary;
    private bool _isReloadingSecondary;
    private Coroutine _primaryBurstRoutine;
    private Coroutine _secondaryBurstRoutine;

    private Transform _explicitTarget;
    private Vector3 _explicitAimPoint;
    private bool _hasExplicitAimPoint;

    public FireProfile Primary => primary;
    public FireProfile Secondary => secondary;

    public bool IsReloadingPrimary => _isReloadingPrimary;
    public bool IsReloadingSecondary => _isReloadingSecondary;

    private void Awake()
    {
        if (fireOrigin == null)
            fireOrigin = transform;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void SetExplicitAimPoint(Vector3 worldPoint)
    {
        _explicitAimPoint = worldPoint;
        _hasExplicitAimPoint = true;
    }

    public void ClearExplicitAimPoint()
    {
        _hasExplicitAimPoint = false;
    }

    public void SetExplicitTarget(Transform target)
    {
        _explicitTarget = target;
    }

    public void ClearExplicitTarget()
    {
        _explicitTarget = null;
    }

    public bool TryFirePrimary()
    {
        return TryFire(primary, true);
    }

    public bool TryFireSecondary()
    {
        return TryFire(secondary, false);
    }

    public bool TryReloadPrimary()
    {
        return TryReload(primary, true);
    }

    public bool TryReloadSecondary()
    {
        return TryReload(secondary, false);
    }

    public void StopActiveBursts()
    {
        if (_primaryBurstRoutine != null)
        {
            StopCoroutine(_primaryBurstRoutine);
            _primaryBurstRoutine = null;
        }

        if (_secondaryBurstRoutine != null)
        {
            StopCoroutine(_secondaryBurstRoutine);
            _secondaryBurstRoutine = null;
        }
    }

    private bool TryFire(FireProfile profile, bool isPrimary)
    {
        if (profile == null || profile.RuntimeAttackMode == null)
            return false;

        if (isPrimary)
        {
            if (_isReloadingPrimary || Time.time < _nextPrimaryFireTime || _primaryBurstRoutine != null)
                return false;
        }
        else
        {
            if (_isReloadingSecondary || Time.time < _nextSecondaryFireTime || _secondaryBurstRoutine != null)
                return false;
        }

        if (!profile.HasAmmoInClip)
        {
            PlayDryFire(profile);
            if (profile.AutoReloadOnEmpty)
                TryReload(profile, isPrimary);
            return false;
        }

        if (!TryBuildAttackContext(profile, out OUT_AttackContext context))
            return false;

        if (isPrimary)
        {
            _nextPrimaryFireTime = Time.time + Mathf.Max(0.01f, profile.FireInterval);
            _primaryBurstRoutine = StartCoroutine(FireBurstRoutine(profile, context, true));
        }
        else
        {
            _nextSecondaryFireTime = Time.time + Mathf.Max(0.01f, profile.FireInterval);
            _secondaryBurstRoutine = StartCoroutine(FireBurstRoutine(profile, context, false));
        }

        return true;
    }

    private IEnumerator FireBurstRoutine(FireProfile profile, OUT_AttackContext baseContext, bool isPrimary)
    {
        int shots = Mathf.Max(1, profile.ShotsPerBurst);

        for (int i = 0; i < shots; i++)
        {
            if (!profile.HasAmmoInClip)
            {
                PlayDryFire(profile);
                break;
            }

            if (!TryBuildAttackContext(profile, out OUT_AttackContext context))
                break;

            ConsumeAmmo(profile);
            SpawnMuzzleFlash(profile, context.Origin, context.Direction);
            PlayRandom(profile.FireClips);
            TriggerAnimator(profile.FireTrigger);

            profile.RuntimeAttackMode.Execute(context);

            if (isPrimary) onPrimaryFired?.Invoke();
            else onSecondaryFired?.Invoke();

            if (i < shots - 1 && profile.BurstShotInterval > 0f)
                yield return new WaitForSeconds(profile.BurstShotInterval);
        }

        if (isPrimary)
            _primaryBurstRoutine = null;
        else
            _secondaryBurstRoutine = null;

        if (!profile.HasAmmoInClip && profile.AutoReloadOnEmpty)
            TryReload(profile, isPrimary);
    }

    private bool TryReload(FireProfile profile, bool isPrimary)
    {
        if (profile == null || !profile.CanReload)
            return false;

        if (isPrimary)
        {
            if (_isReloadingPrimary)
                return false;

            StartCoroutine(ReloadRoutine(profile, true));
        }
        else
        {
            if (_isReloadingSecondary)
                return false;

            StartCoroutine(ReloadRoutine(profile, false));
        }

        return true;
    }

    private IEnumerator ReloadRoutine(FireProfile profile, bool isPrimary)
    {
        if (isPrimary) _isReloadingPrimary = true;
        else _isReloadingSecondary = true;

        PlaySingle(profile.ReloadStartClip);
        TriggerAnimator(profile.ReloadTrigger);
        onReloadStarted?.Invoke();

        yield return new WaitForSeconds(Mathf.Max(0.01f, profile.ReloadDuration));

        int needed = Mathf.Max(0, profile.ClipSize - profile.AmmoInClip);
        if (needed > 0)
        {
            if (profile.InfiniteReserve)
            {
                profile.AmmoInClip = profile.ClipSize;
            }
            else
            {
                int taken = Mathf.Min(needed, profile.ReserveAmmo);
                profile.ReserveAmmo -= taken;
                profile.AmmoInClip += taken;
            }
        }

        PlaySingle(profile.ReloadEndClip);
        onReloadFinished?.Invoke();

        if (isPrimary) _isReloadingPrimary = false;
        else _isReloadingSecondary = false;
    }

    private bool TryBuildAttackContext(FireProfile profile, out OUT_AttackContext context)
    {
        context = default;

        Transform ownerRoot = ownerRootOverride != null ? ownerRootOverride : transform.root;
        Transform actualOrigin = fireOrigin != null ? fireOrigin : transform;

        Vector3 desiredAimPoint = ResolveDesiredAimPoint(actualOrigin, profile.MaxDistance);
        if (turretRig != null)
        {
            turretRig.SetDesiredAimPoint(desiredAimPoint);

            if (requireTurretAlignmentToFire && !turretRig.IsAligned(desiredAimPoint))
                return false;

            actualOrigin = turretRig.GetFireOrigin() != null ? turretRig.GetFireOrigin() : actualOrigin;
        }

        Vector3 direction;
        if (turretRig != null)
            direction = turretRig.GetFireDirection();
        else
            direction = desiredAimPoint - actualOrigin.position;

        if (direction.sqrMagnitude <= 0.0001f)
            direction = actualOrigin.forward;

        direction.Normalize();

        context = new OUT_AttackContext
        {
            Instigator = gameObject,
            Inflictor = gameObject,
            OwnerRoot = ownerRoot,

            Origin = actualOrigin.position,
            Direction = direction,
            Distance = Mathf.Max(0.1f, profile.MaxDistance),

            HitMask = profile.HitMask,
            TriggerInteraction = profile.TriggerInteraction,

            Damage = Mathf.Max(0, profile.Damage),
            Impulse = Mathf.Max(0f, profile.Impulse),
            DamageKind = profile.DamageKind,

            PelletCount = Mathf.Max(1, profile.PelletCount),
            Spread = profile.Spread,
            TraceRadius = Mathf.Max(0f, profile.TraceRadius),

            ProjectilePrefab = profile.ProjectilePrefab,
            ProjectileSpeed = Mathf.Max(0f, profile.ProjectileSpeed),

            ImpactVfx = profile.ImpactVfx,
            FallbackImpactVfx = profile.FallbackImpactVfx,
            TracerVfx = profile.TracerVfx,

            IgnoreOwnerRoot = ignoreOwnerRoot
        };

        return true;
    }

    private Vector3 ResolveDesiredAimPoint(Transform actualOrigin, float maxDistance)
    {
        switch (aimMode)
        {
            case AimMode.ExplicitTarget:
                if (_explicitTarget != null)
                    return GetTransformAimPoint(_explicitTarget);
                break;

            case AimMode.ExplicitWorldPoint:
                if (_hasExplicitAimPoint)
                    return _explicitAimPoint;
                break;

            case AimMode.CameraRay:
                Camera cam = aimCamera != null ? aimCamera : Camera.main;
                if (cam != null)
                {
                    Ray ray = new Ray(cam.transform.position, cam.transform.forward);
                    if (Physics.Raycast(ray, out RaycastHit hit, cameraAimDistance, cameraAimMask, cameraAimTriggers))
                        return hit.point;

                    return ray.origin + ray.direction * cameraAimDistance;
                }
                break;
        }

        return actualOrigin.position + actualOrigin.forward * Mathf.Max(1f, maxDistance);
    }

    private Vector3 GetTransformAimPoint(Transform target)
    {
        Collider col = target.GetComponentInChildren<Collider>();
        if (col != null)
            return col.bounds.center;

        return target.position;
    }

    private void ConsumeAmmo(FireProfile profile)
    {
        if (profile.InfiniteClip)
            return;

        profile.AmmoInClip = Mathf.Max(0, profile.AmmoInClip - Mathf.Max(1, profile.AmmoPerShot));
    }

    private void SpawnMuzzleFlash(FireProfile profile, Vector3 origin, Vector3 direction)
    {
        if (profile.MuzzleFlashVfx == null)
            return;

        Quaternion rot = direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction)
            : Quaternion.identity;

        OUTPool.Instantiate(profile.MuzzleFlashVfx, origin, rot);
    }

    private void PlayDryFire(FireProfile profile)
    {
        PlaySingle(profile.DryFireClip);
        onDryFire?.Invoke();
    }

    private void PlaySingle(AudioClip clip)
    {
        if (clip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clip);
    }

    private void PlayRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0 || audioSource == null)
            return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void TriggerAnimator(string triggerName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(triggerName))
            return;

        animator.SetTrigger(triggerName);
    }
}
