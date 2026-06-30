using System;

[Flags]
public enum OUT_ConditionFlags
{
    None = 0,

    SeeEnemy = 1 << 0,
    HearDanger = 1 << 1,
    HearWorld = 1 << 2,
    HearPlayer = 1 << 3,
    HearCombat = 1 << 4,

    EnemyOccluded = 1 << 5,
    EnemyTooFar = 1 << 6,
    EnemyDead = 1 << 7,
    NewEnemy = 1 << 8,

    LightDamage = 1 << 9,
    HeavyDamage = 1 << 10,

    CanRangeAttack1 = 1 << 11,
    CanRangeAttack2 = 1 << 12,
    CanMeleeAttack1 = 1 << 13,
    CanMeleeAttack2 = 1 << 14,

    NoAmmoLoaded = 1 << 15,
    NoFire = 1 << 16
}
