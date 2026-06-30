using UnityEngine;

public enum OUT_DamageKind
{
    Generic = 0,
    Bullet = 1,
    Explosion = 2,
    Melee = 3,
    Fire = 4,
    Electric = 5,
    Crush = 6,
    Fall = 7
}

public enum OUT_HitZone
{
    Generic = 0,
    Head = 1,
    Chest = 2,
    Stomach = 3,
    LeftArm = 4,
    RightArm = 5,
    LeftLeg = 6,
    RightLeg = 7
}

public readonly struct OUT_DamageContext
{
    public readonly GameObject Instigator;
    public readonly GameObject Inflictor;
    public readonly Vector3 HitPoint;
    public readonly Vector3 HitNormal;
    public readonly int DamageAmount;
    public readonly OUT_DamageKind DamageKind;
    public readonly OUT_HitZone HitZone;

    public readonly Vector3 HitDirection;
    public readonly float Impulse;

    public OUT_DamageContext(
        GameObject instigator,
        GameObject inflictor,
        Vector3 hitPoint,
        Vector3 hitNormal,
        int damageAmount,
        OUT_DamageKind damageKind,
        OUT_HitZone hitZone = OUT_HitZone.Generic,
        Vector3 hitDirection = default,
        float impulse = 0f)
    {
        Instigator = instigator;
        Inflictor = inflictor;
        HitPoint = hitPoint;
        HitNormal = hitNormal;
        DamageAmount = damageAmount;
        DamageKind = damageKind;
        HitZone = hitZone;
        HitDirection = hitDirection;
        Impulse = impulse;
    }
}