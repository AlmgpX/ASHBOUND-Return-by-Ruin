using OUTPool = OutCore.pool.OUT;
using UnityEngine;

public class OUT_HitscanAttackMode : IOutAttackMode
{
    private readonly RaycastHit[] _raycastHits = new RaycastHit[32];

    public void Execute(in OUT_AttackContext context)
    {
        int pelletCount = Mathf.Max(1, context.PelletCount);
        Vector3 baseDirection = context.Direction.sqrMagnitude > 0.0001f
            ? context.Direction.normalized
            : Vector3.forward;

        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 shotDirection = ApplySpread(baseDirection, context.Spread);
            Vector3 traceEnd = context.Origin + shotDirection * context.Distance;

            if (TryGetBestHit(context, shotDirection, out RaycastHit bestHit))
            {
                traceEnd = bestHit.point;
                ApplyHit(context, shotDirection, bestHit);
            }

            SpawnTracer(context, context.Origin, traceEnd);
        }
    }

    private bool TryGetBestHit(in OUT_AttackContext context, Vector3 direction, out RaycastHit bestHit)
    {
        bestHit = default;

        int hitCount = Physics.RaycastNonAlloc(
            context.Origin,
            direction,
            _raycastHits,
            context.Distance,
            context.HitMask,
            context.TriggerInteraction);

        if (hitCount <= 0)
            return false;

        float closestDistance = float.MaxValue;
        bool found = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _raycastHits[i];
            if (hit.collider == null)
                continue;

            if (ShouldIgnoreHit(context, hit.transform))
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

    private bool ShouldIgnoreHit(in OUT_AttackContext context, Transform hitTransform)
    {
        if (!context.IgnoreOwnerRoot || hitTransform == null)
            return false;

        Transform hitEntity = OUT_AIPerception.ResolveEntityRoot(hitTransform);

        if (context.Instigator != null)
        {
            Transform instigatorEntity = OUT_AIPerception.ResolveEntityRoot(context.Instigator.transform);
            if (hitEntity != null && instigatorEntity != null && hitEntity == instigatorEntity)
                return true;
        }

        if (context.OwnerRoot != null)
        {
            Transform ownerEntity = OUT_AIPerception.ResolveEntityRoot(context.OwnerRoot);
            if (hitEntity != null && ownerEntity != null && hitEntity == ownerEntity)
                return true;
        }

        return false;
    }

    private void ApplyHit(in OUT_AttackContext context, Vector3 shotDirection, in RaycastHit hit)
    {
        IOutDamageable damageable = hit.collider.GetComponentInParent<IOutDamageable>();
        if (damageable != null)
        {
            OUT_DamageContext damageContext = new OUT_DamageContext(
                context.Instigator,
                context.Inflictor != null ? context.Inflictor : context.Instigator,
                hit.point,
                hit.normal,
                context.Damage,
                context.DamageKind == OUT_DamageKind.Generic ? OUT_DamageKind.Bullet : context.DamageKind,
                OUT_HitZone.Generic,
                shotDirection,
                context.Impulse);

            OUT_DamageResolver.TryApply(damageable, in damageContext);
        }

        SpawnImpact(context, hit.point, Quaternion.LookRotation(hit.normal));
    }

    private Vector3 ApplySpread(Vector3 direction, Vector2 spread)
    {
        if (spread == Vector2.zero)
            return direction;

        Vector3 right = Vector3.Cross(direction, Vector3.up);
        if (right.sqrMagnitude < 0.0001f)
            right = Vector3.right;
        right.Normalize();

        Vector3 up = Vector3.Cross(right, direction).normalized;

        float x = Random.Range(-spread.x, spread.x);
        float y = Random.Range(-spread.y, spread.y);

        Vector3 spreadDirection = (direction + right * x + up * y).normalized;
        return spreadDirection;
    }

    private void SpawnImpact(in OUT_AttackContext context, Vector3 position, Quaternion rotation)
    {
        GameObject prefab = context.ImpactVfx != null ? context.ImpactVfx : context.FallbackImpactVfx;
        if (prefab == null)
            return;

        OUTPool.Instantiate(prefab, position, rotation);
    }

    private void SpawnTracer(in OUT_AttackContext context, Vector3 start, Vector3 end)
    {
        if (context.TracerVfx == null)
            return;

        GameObject tracer = OUTPool.Instantiate(context.TracerVfx, start, Quaternion.identity);

        if (tracer == null)
            return;

        LineRenderer line = tracer.GetComponent<LineRenderer>();
        if (line != null)
        {
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
        }
        else
        {
            Vector3 dir = end - start;
            tracer.transform.SetPositionAndRotation(
                start,
                dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(dir.normalized) : Quaternion.identity);
        }
    }
}
