using UnityEngine;

[DisallowMultipleComponent]
public class OUT_HitscanDamageCaster : MonoBehaviour
{
    [Header("Origin")]
    [SerializeField] private Transform castOrigin;
    [SerializeField] private bool useForwardDirection = true;

    [Header("Hitscan")]
    [SerializeField][Min(0.01f)] private float range = 100f;
    [SerializeField][Min(0f)] private float radius = 0f;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Damage")]
    [SerializeField][Min(1)] private int damageAmount = 10;
    [SerializeField] private OUT_DamageKind damageKind = OUT_DamageKind.Bullet;
    [SerializeField] private OUT_HitZone defaultHitZone = OUT_HitZone.Generic;
    [SerializeField][Min(0f)] private float impulse = 0f;
    [SerializeField] private GameObject instigator;
    [SerializeField] private GameObject inflictor;

    [Header("Debug")]
    [SerializeField] private bool drawDebugRay = false;
    [SerializeField] private float debugRayDuration = 0.1f;

    public bool Fire() => Fire(GetOrigin(), GetDirection());

    public bool Fire(Vector3 origin, Vector3 direction)
    {
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        Ray ray = new Ray(origin, direction);

        bool hitSomething;
        RaycastHit hit;

        if (radius > 0f)
            hitSomething = Physics.SphereCast(ray, radius, out hit, range, hitMask, triggerInteraction);
        else
            hitSomething = Physics.Raycast(ray, out hit, range, hitMask, triggerInteraction);

        if (drawDebugRay)
        {
            Vector3 end = hitSomething ? hit.point : origin + direction * range;
            Debug.DrawLine(origin, end, hitSomething ? Color.red : Color.white, debugRayDuration);
        }

        if (!hitSomething)
            return false;

        OUT_DamageContext context = new OUT_DamageContext(
            instigator: instigator,
            inflictor: inflictor != null ? inflictor : gameObject,
            hitPoint: hit.point,
            hitNormal: hit.normal,
            damageAmount: damageAmount,
            damageKind: damageKind,
            hitZone: ResolveHitZone(hit.collider),
            hitDirection: direction,
            impulse: impulse);

        return OUT_DamageUtility.TryApplyDamage(hit.collider, context);
    }

    private Vector3 GetOrigin()
    {
        if (castOrigin != null)
            return castOrigin.position;

        return transform.position;
    }

    private Vector3 GetDirection()
    {
        if (castOrigin != null && useForwardDirection)
            return castOrigin.forward;

        return transform.forward;
    }

    private OUT_HitZone ResolveHitZone(Collider target)
    {
        return defaultHitZone;
    }
}
