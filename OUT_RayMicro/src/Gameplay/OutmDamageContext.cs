using System.Numerics;
using OUT_RayMicro.Core;

namespace OUT_RayMicro.Gameplay;

public enum OutmDamageKind : byte
{
    Generic,
    Bullet,
    Explosion,
    Melee,
    Fire,
    Electric,
    Crush,
    Fall
}

public enum OutmHitZone : byte
{
    Generic,
    Head,
    Chest,
    Stomach,
    LeftArm,
    RightArm,
    LeftLeg,
    RightLeg
}

public readonly struct OutmDamageContext
{
    public readonly EntityId Instigator;
    public readonly EntityId Inflictor;
    public readonly EntityId Target;
    public readonly Vector3 HitPoint;
    public readonly Vector3 HitNormal;
    public readonly Vector3 HitDirection;
    public readonly int DamageAmount;
    public readonly float Impulse;
    public readonly OutmDamageKind DamageKind;
    public readonly OutmHitZone HitZone;
    public readonly string Reason;

    public OutmDamageContext(
        EntityId instigator,
        EntityId inflictor,
        EntityId target,
        Vector3 hitPoint,
        Vector3 hitNormal,
        int damageAmount,
        OutmDamageKind damageKind,
        OutmHitZone hitZone = OutmHitZone.Generic,
        Vector3 hitDirection = default,
        float impulse = 0.0f,
        string reason = "")
    {
        Instigator = instigator;
        Inflictor = inflictor;
        Target = target;
        HitPoint = hitPoint;
        HitNormal = hitNormal.LengthSquared() > 0.0001f ? Vector3.Normalize(hitNormal) : Vector3.UnitY;
        HitDirection = hitDirection.LengthSquared() > 0.0001f ? Vector3.Normalize(hitDirection) : Vector3.Zero;
        DamageAmount = Math.Max(0, damageAmount);
        DamageKind = damageKind;
        HitZone = hitZone;
        Impulse = MathF.Max(0.0f, impulse);
        Reason = reason;
    }

    public static OutmDamageContext PlayerDebug(EntityId target, int damageAmount, string reason)
    {
        return new OutmDamageContext(
            EntityId.None,
            EntityId.None,
            target,
            Vector3.Zero,
            Vector3.UnitY,
            damageAmount,
            OutmDamageKind.Generic,
            OutmHitZone.Generic,
            Vector3.Zero,
            0.0f,
            reason);
    }
}
