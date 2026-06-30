using UnityEngine;

public enum OUTL_CoverKind : byte
{
    Low = 0,
    High = 1,
    Soft = 2,
    Hard = 3
}

public enum OUTL_CoverStance : byte
{
    Stand = 0,
    Crouch = 1,
    Prone = 2
}

[System.Flags]
public enum OUTL_CoverWeaponRoleMask
{
    Any = 1 << 0,
    Melee = 1 << 1,
    Sidearm = 1 << 2,
    SMG = 1 << 3,
    Rifle = 1 << 4,
    Bow = 1 << 5,
    Sniper = 1 << 6,
    Grenade = 1 << 7,
    Heavy = 1 << 8,
    Utility = 1 << 9
}

[DisallowMultipleComponent]
public class OUTL_CoverPoint : MonoBehaviour
{
    public bool Active = true;
    public float Radius = 0.75f;
    public float Height = 1.4f;
    public Vector3 PeekOffset = Vector3.up * 1.35f;
    public Vector3 PeekLeftOffset = new Vector3(-0.45f, 1.35f, 0f);
    public Vector3 PeekRightOffset = new Vector3(0.45f, 1.35f, 0f);
    public Vector3 CoverNormal = Vector3.forward;
    public int SectorId;
    public OUTL_CoverKind CoverKind = OUTL_CoverKind.High;
    public OUTL_CoverStance Stance = OUTL_CoverStance.Stand;
    public OUTL_CoverWeaponRoleMask AllowedRoles = OUTL_CoverWeaponRoleMask.Any | OUTL_CoverWeaponRoleMask.Rifle | OUTL_CoverWeaponRoleMask.Sidearm | OUTL_CoverWeaponRoleMask.SMG | OUTL_CoverWeaponRoleMask.Sniper;
    public float ExposureWeight = 1f;
    public float DangerWeight = 1f;
    public float OccupyDistance = 1.25f;
    public OUTL_EntityAdapter Occupant;
    public OUTL_CoverReservation Reservation;

    private void OnEnable()
    {
        OUTL_CoverRegistry.Register(this);
    }

    private void OnDisable()
    {
        OUTL_CoverRegistry.Unregister(this);
        if (Occupant != null) Occupant = null;
        Reservation = default(OUTL_CoverReservation);
    }

    public bool IsFreeFor(OUTL_EntityAdapter entity)
    {
        float time = OUTL_World.Instance != null ? OUTL_World.Instance.WorldTime : Time.time;
        return Active && (Occupant == null || Occupant == entity) && (!Reservation.IsActive(time) || (entity != null && Reservation.EntityId == entity.Id));
    }

    public bool AllowsRole(OUTL_WeaponRole role)
    {
        if ((AllowedRoles & OUTL_CoverWeaponRoleMask.Any) != 0) return true;
        OUTL_CoverWeaponRoleMask mask = RoleToMask(role);
        return (AllowedRoles & mask) != 0;
    }

    public bool Reserve(OUTL_EntityAdapter entity, float duration, string reason)
    {
        if (entity == null || !IsFreeFor(entity)) return false;
        float time = OUTL_World.Instance != null ? OUTL_World.Instance.WorldTime : Time.time;
        Occupant = entity;
        Reservation = new OUTL_CoverReservation(entity.Id, time + Mathf.Max(0.1f, duration), reason);
        return true;
    }

    public void Release(OUTL_EntityAdapter entity)
    {
        if (entity != null && Occupant != null && Occupant != entity) return;
        Occupant = null;
        Reservation = default(OUTL_CoverReservation);
    }

    public Vector3 StandPoint { get { return transform.position; } }
    public Vector3 PeekPoint { get { return transform.position + PeekOffset; } }
    public Vector3 LeftPeekPoint { get { return transform.position + PeekLeftOffset; } }
    public Vector3 RightPeekPoint { get { return transform.position + PeekRightOffset; } }

    private static OUTL_CoverWeaponRoleMask RoleToMask(OUTL_WeaponRole role)
    {
        switch (role)
        {
            case OUTL_WeaponRole.Melee: return OUTL_CoverWeaponRoleMask.Melee;
            case OUTL_WeaponRole.Sidearm: return OUTL_CoverWeaponRoleMask.Sidearm;
            case OUTL_WeaponRole.SMG: return OUTL_CoverWeaponRoleMask.SMG;
            case OUTL_WeaponRole.Rifle: return OUTL_CoverWeaponRoleMask.Rifle;
            case OUTL_WeaponRole.Bow: return OUTL_CoverWeaponRoleMask.Bow;
            case OUTL_WeaponRole.Sniper: return OUTL_CoverWeaponRoleMask.Sniper;
            case OUTL_WeaponRole.Grenade: return OUTL_CoverWeaponRoleMask.Grenade;
            case OUTL_WeaponRole.Heavy: return OUTL_CoverWeaponRoleMask.Heavy;
            case OUTL_WeaponRole.Utility: return OUTL_CoverWeaponRoleMask.Utility;
            case OUTL_WeaponRole.Any:
            default: return OUTL_CoverWeaponRoleMask.Any;
        }
    }
}
