using System;
using UnityEngine;

[Serializable]
public struct OUTL_ActorInputFrame
{
    public Vector2 Move;
    public Vector2 Look;
    public bool JumpPressed;
    public bool JumpHeld;
    public bool CrouchHeld;
    public bool SprintHeld;
    public bool FirePrimaryHeld;
    public bool FirePrimaryPressed;
    public bool FireSecondaryPressed;
    public bool MeleePressed;
    public bool ReloadPressed;
    public bool UsePressed;
    public bool LedgeDropPressed;
    public bool FireAuthorized;
    public float AimConfidence;
    public float MaxAllowedFireAngle;
    public int WeaponSlot;
    public int WeaponCycle;
    public Vector3 AimWorldPoint;
    public bool HasAimWorldPoint;
    public float DesiredYaw;
    public float DesiredPitch;
    public bool HasDesiredView;
    public bool AbilityPrimaryPressed;
    public bool AbilityPrimaryHeld;
    public bool AbilitySecondaryPressed;
    public int AbilitySlot;
    public Vector3 AbilityTargetPoint;
    public bool HasAbilityTargetPoint;
    public float Timestamp;
    public float DeltaTime;

    public static OUTL_ActorInputFrame Empty(float time)
    {
        OUTL_ActorInputFrame frame = default(OUTL_ActorInputFrame);
        frame.WeaponSlot = -1;
        frame.WeaponCycle = 0;
        frame.AbilitySlot = -1;
        frame.MaxAllowedFireAngle = 0f;
        frame.Timestamp = time;
        frame.DeltaTime = 0f;
        return frame;
    }

    public bool HasAnyAction
    {
        get
        {
            return Move.sqrMagnitude > 0.0001f
                || Look.sqrMagnitude > 0.0001f
                || JumpPressed
                || JumpHeld
                || CrouchHeld
                || SprintHeld
                || FirePrimaryHeld
                || FirePrimaryPressed
                || FireSecondaryPressed
                || MeleePressed
                || ReloadPressed
                || UsePressed
                || LedgeDropPressed
                || WeaponSlot >= 0
                || WeaponCycle != 0
                || HasAimWorldPoint
                || HasDesiredView
                || AbilityPrimaryPressed
                || AbilityPrimaryHeld
                || AbilitySecondaryPressed
                || AbilitySlot >= 0
                || HasAbilityTargetPoint;
        }
    }

    public void ClearActions()
    {
        Move = Vector2.zero;
        Look = Vector2.zero;
        JumpPressed = false;
        JumpHeld = false;
        CrouchHeld = false;
        SprintHeld = false;
        FirePrimaryHeld = false;
        FirePrimaryPressed = false;
        FireSecondaryPressed = false;
        MeleePressed = false;
        ReloadPressed = false;
        UsePressed = false;
        LedgeDropPressed = false;
        FireAuthorized = false;
        AimConfidence = 0f;
        MaxAllowedFireAngle = 0f;
        WeaponSlot = -1;
        WeaponCycle = 0;
        HasAimWorldPoint = false;
        HasDesiredView = false;
        AbilityPrimaryPressed = false;
        AbilityPrimaryHeld = false;
        AbilitySecondaryPressed = false;
        AbilitySlot = -1;
        HasAbilityTargetPoint = false;
    }
}
