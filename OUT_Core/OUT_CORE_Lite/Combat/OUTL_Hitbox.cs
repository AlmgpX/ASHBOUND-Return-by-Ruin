using UnityEngine;

public enum OUTL_HitboxZone : byte
{
    Generic = 0,
    Head = 1,
    Torso = 2,
    Arm = 3,
    Leg = 4,
    WeakPoint = 5,
    Armor = 6
}

[DisallowMultipleComponent]
public sealed class OUTL_Hitbox : MonoBehaviour
{
    public OUTL_EntityAdapter Entity;
    public OUTL_HitboxZone Zone = OUTL_HitboxZone.Generic;
    public string DamageKeySuffix;
    [Min(0f)] public float DamageMultiplier = 1f;
    public bool UseZoneAsSuffix = true;

    private void Awake()
    {
        ResolveEntity();
    }

    private void OnValidate()
    {
        DamageMultiplier = Mathf.Max(0f, DamageMultiplier);
    }

    public void ResolveEntity()
    {
        if (Entity == null) Entity = GetComponentInParent<OUTL_EntityAdapter>();
    }

    public string GetSuffix()
    {
        if (!string.IsNullOrEmpty(DamageKeySuffix)) return DamageKeySuffix;
        if (!UseZoneAsSuffix || Zone == OUTL_HitboxZone.Generic) return string.Empty;
        return Zone.ToString().ToLowerInvariant();
    }

    public static bool Resolve(Collider collider, out OUTL_EntityAdapter target, out float multiplier, out string suffix)
    {
        target = null;
        multiplier = 1f;
        suffix = string.Empty;

        if (collider == null) return false;

        OUTL_Hitbox hitbox = collider.GetComponent<OUTL_Hitbox>();
        if (hitbox == null) hitbox = collider.GetComponentInParent<OUTL_Hitbox>();

        if (hitbox != null)
        {
            hitbox.ResolveEntity();
            target = hitbox.Entity;
            multiplier = Mathf.Max(0f, hitbox.DamageMultiplier);
            suffix = hitbox.GetSuffix();
            return target != null && target.Runtime != null;
        }

        if (!OUTL_Combat.TryGetEntityFromCollider(collider, out target)) return false;
        multiplier = 1f;
        suffix = string.Empty;
        return true;
    }

    public static string BuildDamageKey(string baseKey, string suffix)
    {
        bool hasBase = !string.IsNullOrEmpty(baseKey);
        bool hasSuffix = !string.IsNullOrEmpty(suffix);
        if (hasBase && hasSuffix) return baseKey + "." + suffix;
        if (hasBase) return baseKey;
        if (hasSuffix) return suffix;
        return string.Empty;
    }
}
