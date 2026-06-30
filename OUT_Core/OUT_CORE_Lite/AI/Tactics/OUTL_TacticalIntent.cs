using System;
using UnityEngine;

public enum OUTL_TacticalIntentId : byte
{
    None = 0,
    Idle = 1,
    Guard = 2,
    Patrol = 3,
    Work = 4,
    Travel = 5,
    Investigate = 6,
    Search = 7,
    Aim = 8,
    HoldFire = 9,
    AttackRanged = 10,
    AttackMelee = 11,
    Suppress = 12,
    FindCover = 13,
    TakeCover = 14,
    Retreat = 15,
    Flee = 16,
    SwitchWeapon = 17,
    Reload = 18,
    EatOrUseResource = 19,
    AbilityAttack = 20,
    LeapAttack = 21,
    Dead = 22
}

public enum OUTL_TacticalMoveMode : byte
{
    None = 0,
    Hold = 1,
    MoveTo = 2,
    KeepRange = 3,
    RetreatFromTarget = 4,
    TakeCover = 5
}

[Serializable]
public struct OUTL_TacticalDecision
{
    public OUTL_TacticalIntentId Intent;
    public OUTL_TacticalMoveMode MoveMode;
    public OUTL_EntityId Target;
    public Vector3 MoveTarget;
    public Vector3 AimPoint;
    public bool HasMoveTarget;
    public bool HasAimPoint;
    public bool WantsFire;
    public bool WantsSuppress;
    public bool WantsReload;
    public bool WantsSwitchWeapon;
    public bool WantsCover;
    public OUTL_EquipmentSlot WeaponSlot;
    public OUTL_AttackProfile AttackProfile;
    public OUTL_AbilityProfile AbilityProfile;
    public bool WantsAbility;
    public int AbilitySlot;
    public Vector3 AbilityTargetPoint;
    public bool HasAbilityTargetPoint;
    public OUTL_CoverPoint Cover;
    public string Reason;
    public float Score;
    public float PreferredRange;
    public float MinSafeRange;
    public float DecisionTime;

    public bool IsValid { get { return Intent != OUTL_TacticalIntentId.None; } }

    public static OUTL_TacticalDecision Idle(float time, string reason)
    {
        return new OUTL_TacticalDecision
        {
            Intent = OUTL_TacticalIntentId.Idle,
            MoveMode = OUTL_TacticalMoveMode.Hold,
            WeaponSlot = OUTL_EquipmentSlot.Primary,
            Reason = reason,
            DecisionTime = time
        };
    }

    public static OUTL_TacticalDecision Dead(float time)
    {
        return new OUTL_TacticalDecision
        {
            Intent = OUTL_TacticalIntentId.Dead,
            MoveMode = OUTL_TacticalMoveMode.Hold,
            WeaponSlot = OUTL_EquipmentSlot.Primary,
            Reason = "dead",
            DecisionTime = time
        };
    }
}
