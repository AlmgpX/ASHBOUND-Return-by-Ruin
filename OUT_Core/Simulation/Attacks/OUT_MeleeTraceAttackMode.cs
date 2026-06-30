using OUTPool = OutCore.pool.OUT;
using UnityEngine;

public class OUT_MeleeTraceAttackMode : IOutAttackMode
{
    private readonly RaycastHit[] _hits = new RaycastHit[32];

    public void Execute(in OUT_AttackContext context)
    {
        Vector3 direction = context.Direction.sqrMagnitude > 0.0001f
            ? context.Direction.normalized
            : Vector3.forward;

        if (!TryGetBestHit(context, direction, out RaycastHit bestHit))
            return;

        IOutDamageable damageable = bestHit.collider.GetComponentInParent<IOutDamageable>();
        if (damageable != null)
        {
            OUT_DamageContext damageContext = new OUT_DamageContext(
                context.Instigator,
                context.Inflictor != null ? context.Inflictor : context.Instigator,
                bestHit.point,
                bestHit.normal,
                context.Damage,
                context.DamageKind == OUT_DamageKind.Generic ? OUT_DamageKind.Melee : context.DamageKind,
                OUT_HitZone.Generic,
                direction,
                context.Impulse);

            OUT_DamageResolver.TryApply(damageable, in damageContext);
        }

        SpawnImpact(context, bestHit.point, Quaternion.LookRotation(bestHit.normal));
    }

    private bool TryGetBestHit(in OUT_AttackContext context, Vector3 direction, out RaycastHit bestHit)
    {
        bestHit = default;

        int hitCount;
        float radius = Mathf.Max(0f, context.TraceRadius);

        if (radius > 0.001f)
        {
            hitCount = Physics.SphereCastNonAlloc(
                context.Origin,
                radius,
                direction,
                _hits,
                context.Distance,
                context.HitMask,
                context.TriggerInteraction);
        }
        else
        {
            hitCount = Physics.RaycastNonAlloc(
                context.Origin,
                direction,
                _hits,
                context.Distance,
                context.HitMask,
                context.TriggerInteraction);
        }

        if (hitCount <= 0)
            return false;

        float closestDistance = float.MaxValue;
        bool found = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _hits[i];
            if (hit.collider == null)
                continue;

            if (context.IgnoreOwnerRoot && context.OwnerRoot != null && hit.transform.root == context.OwnerRoot)
                continue;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                bestHit = hit;
                found = true;
            }
        }

        return found;
    }

    private void SpawnImpact(in OUT_AttackContext context, Vector3 position, Quaternion rotation)
    {
        GameObject prefab = context.ImpactVfx != null ? context.ImpactVfx : context.FallbackImpactVfx;
        if (prefab == null)
            return;

        OUTPool.Instantiate(prefab, position, rotation);
    }
}
