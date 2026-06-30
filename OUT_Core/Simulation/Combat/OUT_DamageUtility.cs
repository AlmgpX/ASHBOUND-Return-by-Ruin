using UnityEngine;

public static class OUT_DamageUtility
{
    public static bool TryApplyDamage(GameObject target, in OUT_DamageContext context)
    {
        if (target == null)
            return false;

        IOutDamageable damageable = target.GetComponent<IOutDamageable>();
        if (damageable == null)
            damageable = target.GetComponentInParent<IOutDamageable>();

        if (damageable == null)
            return false;

        if (!damageable.CanTakeDamage(context))
            return false;

        damageable.ApplyDamage(context);
        return true;
    }

    public static bool TryApplyDamage(Component target, in OUT_DamageContext context)
    {
        if (target == null)
            return false;

        IOutDamageable damageable = target.GetComponent<IOutDamageable>();
        if (damageable == null)
            damageable = target.GetComponentInParent<IOutDamageable>();

        if (damageable == null && target.TryGetComponent<Rigidbody>(out Rigidbody body) && body != null)
            damageable = body.GetComponentInParent<IOutDamageable>();

        if (damageable == null)
            return false;

        if (!damageable.CanTakeDamage(context))
            return false;

        damageable.ApplyDamage(context);
        return true;
    }

    public static bool TryApplyDamage(Collider target, in OUT_DamageContext context)
    {
        if (target == null)
            return false;

        if (TryApplyDamage((Component)target, context))
            return true;

        if (target.attachedRigidbody != null)
            return TryApplyDamage(target.attachedRigidbody, context);

        return false;
    }
}
