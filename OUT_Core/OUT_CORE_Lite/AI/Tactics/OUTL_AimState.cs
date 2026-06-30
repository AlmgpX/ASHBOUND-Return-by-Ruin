using System;
using UnityEngine;

public enum OUTL_AimCommand : byte
{
    None = 0,
    AimOnly = 1,
    HoldFire = 2,
    FireSingle = 3,
    FireBurst = 4,
    Suppress = 5,
    Reload = 6,
    SwitchWeapon = 7
}

public enum OUTL_AimReasonCode : byte
{
    None = 0,
    NoTarget = 1,
    Reload = 2,
    SwitchWeapon = 3,
    NoLineOfFire = 4,
    FriendlyFire = 5,
    AimHold = 6,
    ReactionOrHold = 7,
    Fire = 8,
    Suppress = 9
}

[Serializable]
public struct OUTL_AimState
{
    public OUTL_AimCommand Command;
    public OUTL_AimReasonCode ReasonCode;
    public Vector3 AimPoint;
    public bool HasAimPoint;
    public float DesiredYaw;
    public float DesiredPitch;
    public bool AimLocked;
    public float TargetAcquiredTime;
    public float MaxAllowedFireAngle;
    public bool FireAuthorized;
    public float ErrorDegrees;
    public float Confidence;
    public float StableTime;
    public float FireAllowedTime;
    public bool FriendlyFireBlocked;
    public OUTL_FriendlyFireEvaluation FriendlyFire;
    public string Reason;
}
