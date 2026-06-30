using OUTPool = OutCore.pool.OUT;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class OUT_ProjectileBase : MonoBehaviour, IOutPoolResettable, OUTL_IPoolReset
{
    [Header("Ballistics")]
    [SerializeField] private float defaultSpeed = 120f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private bool alignToVelocity = true;
    [SerializeField] private CollisionDetectionMode collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

    [Header("Damage Defaults")]
    [SerializeField] private int defaultDamage = 10;
    [SerializeField] private float defaultImpactImpulse = 10f;
    [SerializeField] private OUT_DamageKind defaultDamageKind = OUT_DamageKind.Bullet;

    [Header("Impact")]
    [SerializeField] private GameObject defaultImpactVfx;
    [SerializeField] private GameObject defaultFallbackImpactVfx;
    [SerializeField] private bool ignoreOwnerRootOnImpact = true;
    [SerializeField] private bool destroyOnImpact = true;
    [SerializeField] private int maxBounceCount = 0;

    private Rigidbody _rigidbody;
    private float _lifeTimer;
    private int _bounceCount;
    private bool _isActive;

    private GameObject _instigator;
    private Transform _ownerRoot;

    private int _runtimeDamage;
    private float _runtimeImpulse;
    private OUT_DamageKind _runtimeDamageKind;
    private GameObject _runtimeImpactVfx;
    private GameObject _runtimeFallbackImpactVfx;
    private bool _runtimeIgnoreOwnerRoot;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.collisionDetectionMode = collisionDetectionMode;
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Update()
    {
        if (!_isActive)
            return;

        _lifeTimer -= Time.deltaTime;
        if (_lifeTimer <= 0f)
        {
            ReturnToPool();
            return;
        }

        if (alignToVelocity)
        {
            Vector3 velocity = _rigidbody.velocity;
            if (velocity.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(velocity.normalized);
        }
    }

    public void Launch(in OUT_AttackContext attackContext)
    {
        _instigator = attackContext.Instigator;
        _ownerRoot = attackContext.OwnerRoot;

        _runtimeDamage = attackContext.Damage > 0 ? attackContext.Damage : defaultDamage;
        _runtimeImpulse = attackContext.Impulse > 0f ? attackContext.Impulse : defaultImpactImpulse;
        _runtimeDamageKind = attackContext.DamageKind == OUT_DamageKind.Generic ? defaultDamageKind : attackContext.DamageKind;

        _runtimeImpactVfx = attackContext.ImpactVfx != null ? attackContext.ImpactVfx : defaultImpactVfx;
        _runtimeFallbackImpactVfx = attackContext.FallbackImpactVfx != null ? attackContext.FallbackImpactVfx : defaultFallbackImpactVfx;
        _runtimeIgnoreOwnerRoot = attackContext.IgnoreOwnerRoot || ignoreOwnerRootOnImpact;

        _isActive = true;
        _lifeTimer = lifetime;
        _bounceCount = 0;

        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody>();

        _rigidbody.velocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.WakeUp();

        Vector3 direction = attackContext.Direction.sqrMagnitude > 0.0001f
            ? attackContext.Direction.normalized
            : transform.forward;

        float speed = attackContext.ProjectileSpeed > 0f ? attackContext.ProjectileSpeed : defaultSpeed;
        Vector3 velocity = direction * speed;
        _rigidbody.velocity = velocity;

        if (alignToVelocity && velocity.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(velocity.normalized);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!_isActive)
            return;

        if (collision == null || collision.collider == null)
            return;

        Transform otherRoot = collision.transform.root;
        if (_runtimeIgnoreOwnerRoot && _ownerRoot != null && otherRoot == _ownerRoot)
            return;

        ContactPoint contact = collision.contactCount > 0 ? collision.GetContact(0) : default;

        Vector3 hitPoint = collision.contactCount > 0 ? contact.point : transform.position;
        Vector3 hitNormal = collision.contactCount > 0 ? contact.normal : -_rigidbody.velocity.normalized;
        Vector3 hitDirection = _rigidbody.velocity.sqrMagnitude > 0.0001f
            ? _rigidbody.velocity.normalized
            : transform.forward;

        IOutDamageable damageable = collision.collider.GetComponentInParent<IOutDamageable>();
        if (damageable != null)
        {
            OUT_DamageContext damageContext = new OUT_DamageContext(
                _instigator,
                gameObject,
                hitPoint,
                hitNormal,
                _runtimeDamage,
                _runtimeDamageKind,
                OUT_HitZone.Generic,
                hitDirection,
                _runtimeImpulse);

            OUT_DamageResolver.TryApply(damageable, in damageContext);
        }

        SpawnImpact(hitPoint, Quaternion.LookRotation(hitNormal));

        if (destroyOnImpact || _bounceCount >= maxBounceCount)
        {
            ReturnToPool();
            return;
        }

        _bounceCount++;
    }

    private void SpawnImpact(Vector3 position, Quaternion rotation)
    {
        GameObject prefab = _runtimeImpactVfx != null ? _runtimeImpactVfx : _runtimeFallbackImpactVfx;
        if (prefab == null)
            return;

        OUTPool.Instantiate(prefab, position, rotation);
    }

    public void ReturnToPool()
    {
        _isActive = false;

        if (OUTPool.IsManaged(gameObject))
            OUTPool.Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    public void OnTakenFromPool()
    {
        _isActive = false;
        _lifeTimer = lifetime;
        _bounceCount = 0;
        _instigator = null;
        _ownerRoot = null;
        _runtimeDamage = defaultDamage;
        _runtimeImpulse = defaultImpactImpulse;
        _runtimeDamageKind = defaultDamageKind;
        _runtimeImpactVfx = defaultImpactVfx;
        _runtimeFallbackImpactVfx = defaultFallbackImpactVfx;
        _runtimeIgnoreOwnerRoot = ignoreOwnerRootOnImpact;

        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody>();

        _rigidbody.velocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.Sleep();
    }

    public void OnReturnedToPool()
    {
        _isActive = false;
        _lifeTimer = lifetime;
        _bounceCount = 0;
        _instigator = null;
        _ownerRoot = null;
        _runtimeDamage = defaultDamage;
        _runtimeImpulse = defaultImpactImpulse;
        _runtimeDamageKind = defaultDamageKind;
        _runtimeImpactVfx = defaultImpactVfx;
        _runtimeFallbackImpactVfx = defaultFallbackImpactVfx;
        _runtimeIgnoreOwnerRoot = ignoreOwnerRootOnImpact;

        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody>();

        _rigidbody.velocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.Sleep();
    }

    public void OUTL_OnPoolSpawn()
    {
        OnTakenFromPool();
    }

    public void OUTL_OnPoolRelease()
    {
        OnReturnedToPool();
    }
}
