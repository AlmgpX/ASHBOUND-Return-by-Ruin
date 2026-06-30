using UnityEngine;

public static class OUT_DamageResolver
{
    public static bool TryApply(IOutDamageable target, in OUT_DamageContext context)
    {
        if (target == null)
            return false;

        if (!target.CanTakeDamage(context))
            return false;

        target.ApplyDamage(context);
        return true;
    }

    public static OUT_DamageContext WithDamage(in OUT_DamageContext source, int newDamageAmount)
    {
        return new OUT_DamageContext(
            source.Instigator,
            source.Inflictor,
            source.HitPoint,
            source.HitNormal,
            newDamageAmount,
            source.DamageKind,
            source.HitZone);
    }

    public static int ApplyHitZoneMultiplier(int baseDamage, OUT_HitZone hitZone)
    {
        switch (hitZone)
        {
            case OUT_HitZone.Head:
                return Mathf.RoundToInt(baseDamage * 2.0f);

            case OUT_HitZone.LeftLeg:
            case OUT_HitZone.RightLeg:
                return Mathf.RoundToInt(baseDamage * 0.75f);

            case OUT_HitZone.LeftArm:
            case OUT_HitZone.RightArm:
                return Mathf.RoundToInt(baseDamage * 0.85f);

            default:
                return baseDamage;
        }
    }

    public static int ApplyDistanceFalloff(int baseDamage, float distance, float fullDamageDistance, float zeroDamageDistance)
    {
        if (distance <= fullDamageDistance)
            return baseDamage;

        if (distance >= zeroDamageDistance)
            return 0;

        float t = Mathf.InverseLerp(fullDamageDistance, zeroDamageDistance, distance);
        float factor = 1f - t;
        return Mathf.RoundToInt(baseDamage * factor);
    }
}