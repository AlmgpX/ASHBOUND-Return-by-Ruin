using UnityEngine;

[DisallowMultipleComponent]
public class OUT_CrushDamageByImpact : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody sourceBody;

    [Header("Threshold")]
    [SerializeField][Min(0.01f)] private float minimumImpactVelocity = 4f;
    [SerializeField][Min(0.01f)] private float minimumMomentum = 6f;
    [SerializeField][Min(0f)] private float minimumDownwardVelocity = 0f;

    [Header("Damage")]
    [SerializeField][Min(1)] private int damageAmount = 1;
    [SerializeField] private OUT_HitZone hitZone = OUT_HitZone.Generic;
    [SerializeField][Min(0f)] private float impulseScale = 1f;

    [Header("Filtering")]
    [SerializeField] private LayerMask targetMask = ~0;
    [SerializeField] private bool ignoreOwnRoot = true;
    [SerializeField] private bool requireBodyMass = true;
    [SerializeField] private GameObject instigator;
    [SerializeField] private GameObject inflictor;

    private void Reset()
    {
        if (sourceBody == null)
            sourceBody = GetComponentInParent<Rigidbody>();

        if (inflictor == null)
            inflictor = gameObject;
    }

    private void Awake()
    {
        if (sourceBody == null)
            sourceBody = GetComponentInParent<Rigidbody>();

        if (inflictor == null)
            inflictor = gameObject;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return;

        Collider target = collision.collider;
        if (!IsAllowedTarget(target))
            return;

        float relativeVelocity = collision.relativeVelocity.magnitude;
        if (relativeVelocity < minimumImpactVelocity)
            return;

        float mass = sourceBody != null ? Mathf.Max(0.01f, sourceBody.mass) : (requireBodyMass ? 0f : 1f);
        if (requireBodyMass && mass <= 0f)
            return;

        float downwardVelocity = sourceBody != null ? -sourceBody.velocity.y : 0f;
        if (minimumDownwardVelocity > 0f && downwardVelocity < minimumDownwardVelocity)
            return;

        float momentum = mass * relativeVelocity;
        if (momentum < minimumMomentum)
            return;

        ContactPoint contact = collision.GetContact(0);
        Vector3 hitDirection = collision.relativeVelocity.sqrMagnitude > 0.0001f
            ? collision.relativeVelocity.normalized
            : -contact.normal;

        OUT_DamageContext context = new OUT_DamageContext(
            instigator: instigator,
            inflictor: inflictor != null ? inflictor : gameObject,
            hitPoint: contact.point,
            hitNormal: contact.normal,
            damageAmount: damageAmount,
            damageKind: OUT_DamageKind.Crush,
            hitZone: hitZone,
            hitDirection: hitDirection,
            impulse: momentum * Mathf.Max(0f, impulseScale));

        OUT_DamageUtility.TryApplyDamage(target, context);
    }

    private bool IsAllowedTarget(Collider other)
    {
        if (((1 << other.gameObject.layer) & targetMask) == 0)
            return false;

        if (ignoreOwnRoot && other.transform.root == transform.root)
            return false;

        return true;
    }
}
