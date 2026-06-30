using System;
using UnityEngine;

[Serializable]
public struct OUTL_AIWeaponSelection
{
    public bool IsValid;
    public OUTL_EquipmentSlot Slot;
    public OUTL_AttackProfile AttackProfile;
    public OUTL_WeaponUseProfile UseProfile;
    public float PreferredRange;
    public float MinSafeRange;
    public float MaxRange;
    public string Reason;
}

[DisallowMultipleComponent]
public sealed class OUTL_AIArsenalSelector : MonoBehaviour
{
    public OUTL_EntityAdapter Entity;
    public OUTL_AttackDriver AttackDriver;
    public OUTL_WeaponUseProfile Primary;
    public OUTL_WeaponUseProfile Secondary;
    public OUTL_WeaponUseProfile Melee;
    public OUTL_EquipmentSlot CurrentSlot = OUTL_EquipmentSlot.Primary;
    public float SwitchCooldown = 0.5f;

    public OUTL_AIWeaponSelection CurrentSelection;
    private float lastSwitchTime = -999f;

    private void Awake()
    {
        Resolve();
    }

    public bool TrySelect(OUTL_TacticalProfile tactical, OUTL_EntityRuntime target, float distance, float time, bool targetVisible, OUTL_TacticalIntentId requestedIntent, out OUTL_AIWeaponSelection selection)
    {
        Resolve();
        selection = default(OUTL_AIWeaponSelection);
        if (AttackDriver == null) return false;

        OUTL_AIWeaponSelection melee = BuildSelection(OUTL_EquipmentSlot.Melee, AttackDriver.Melee, Melee, "melee");
        OUTL_AIWeaponSelection primary = BuildSelection(OUTL_EquipmentSlot.Primary, AttackDriver.Primary, Primary, "primary");
        OUTL_AIWeaponSelection secondary = BuildSelection(OUTL_EquipmentSlot.Secondary, AttackDriver.Secondary, Secondary, "secondary");

        if (requestedIntent == OUTL_TacticalIntentId.AttackMelee && melee.IsValid)
            return AcceptSelection(melee, time, out selection);

        float meleeRange = melee.IsValid ? Mathf.Max(0.5f, melee.MaxRange) : (tactical != null ? tactical.MeleeFallbackRange : 2f);
        if (distance <= meleeRange && melee.IsValid)
            return AcceptSelection(melee, time, out selection);

        if (targetVisible)
        {
            if (IsInRange(primary, distance)) return AcceptSelection(primary, time, out selection);
            if (IsInRange(secondary, distance)) return AcceptSelection(secondary, time, out selection);
        }

        if (primary.IsValid) return AcceptSelection(primary, time, out selection);
        if (secondary.IsValid) return AcceptSelection(secondary, time, out selection);
        if (melee.IsValid) return AcceptSelection(melee, time, out selection);
        return false;
    }

    private bool AcceptSelection(OUTL_AIWeaponSelection wanted, float time, out OUTL_AIWeaponSelection selection)
    {
        selection = wanted;
        if (!wanted.IsValid) return false;
        if (CurrentSelection.IsValid && CurrentSlot != wanted.Slot && time - lastSwitchTime < Mathf.Max(0f, SwitchCooldown))
        {
            selection = CurrentSelection;
            selection.Reason = "switch_cooldown";
            return selection.IsValid;
        }

        if (CurrentSlot != wanted.Slot) lastSwitchTime = time;
        CurrentSlot = wanted.Slot;
        CurrentSelection = wanted;
        return true;
    }

    private OUTL_AIWeaponSelection BuildSelection(OUTL_EquipmentSlot slot, OUTL_AttackProfile driverProfile, OUTL_WeaponUseProfile use, string reason)
    {
        OUTL_AttackProfile profile = use != null && use.AttackProfileOverride != null ? use.AttackProfileOverride : driverProfile;
        if (profile == null) return default(OUTL_AIWeaponSelection);

        float maxRange = use != null && use.MaxRange > 0f ? use.MaxRange : Mathf.Max(0.1f, profile.Range + profile.Radius);
        float preferred = use != null && use.PreferredRange > 0f ? use.PreferredRange : Mathf.Clamp(maxRange * 0.65f, 0.75f, maxRange);
        float minSafe = use != null ? Mathf.Max(0f, use.MinSafeRange) : (slot == OUTL_EquipmentSlot.Melee ? 0f : Mathf.Min(3f, preferred * 0.35f));

        return new OUTL_AIWeaponSelection
        {
            IsValid = true,
            Slot = slot,
            AttackProfile = profile,
            UseProfile = use,
            PreferredRange = preferred,
            MinSafeRange = minSafe,
            MaxRange = maxRange,
            Reason = reason
        };
    }

    private static bool IsInRange(OUTL_AIWeaponSelection selection, float distance)
    {
        if (!selection.IsValid) return false;
        return distance >= selection.MinSafeRange && distance <= Mathf.Max(selection.MinSafeRange, selection.MaxRange);
    }

    private void Resolve()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (AttackDriver == null) AttackDriver = GetComponent<OUTL_AttackDriver>();
    }
}
