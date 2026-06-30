using System;

[Flags]
public enum OUT_AIConditionFlags
{
    None = 0,

    SeeEnemy = 1 << 0,
    NewEnemy = 1 << 1,
    EnemyOccluded = 1 << 2,
    EnemyTooFar = 1 << 3,
    EnemyFacingMe = 1 << 4,
    EnemyDead = 1 << 5,

    CanRangeAttack1 = 1 << 6,
    CanRangeAttack2 = 1 << 7,
    CanMeleeAttack1 = 1 << 8,
    CanMeleeAttack2 = 1 << 9,

    HearDanger = 1 << 10,
    HearCombat = 1 << 11,
    HearWorld = 1 << 12,
    HearPlayer = 1 << 13,

    NoAmmoLoaded = 1 << 14,
    LightDamage = 1 << 15,
    HeavyDamage = 1 << 16,
    NoFriendlyFire = 1 << 17,

    MoveFailed = 1 << 18,
    InCover = 1 << 19,
    HasRoute = 1 << 20,
    HasEnemyLKP = 1 << 21,
    ScriptLocked = 1 << 22
}
