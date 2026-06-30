using UnityEngine;

public enum OUTL_WeaponRole : byte
{
    Any = 0,
    Melee = 1,
    Sidearm = 2,
    SMG = 3,
    Rifle = 4,
    Bow = 5,
    Sniper = 6,
    Grenade = 7,
    Heavy = 8,
    Utility = 9
}

[CreateAssetMenu(menuName = "OUT CORE Lite/AI/Weapon Use Profile", fileName = "OUTL_WeaponUseProfile")]
public sealed class OUTL_WeaponUseProfile : ScriptableObject
{
    public OUTL_WeaponRole Role = OUTL_WeaponRole.Rifle;
    public OUTL_EquipmentSlot Slot = OUTL_EquipmentSlot.Primary;
    public OUTL_AttackProfile AttackProfileOverride;
    public float PreferredRange = 18f;
    public float MinSafeRange = 3f;
    public float MaxRange = 45f;
    public bool RequiresLineOfSight = true;
    public bool BlocksOnFriendlyFire = true;
    public bool AllowSuppression = true;
    public float BurstDuration = 0.35f;
    public float BurstCooldown = 0.75f;
    [Range(0f, 1f)] public float SuppressionChance = 0.35f;
}
