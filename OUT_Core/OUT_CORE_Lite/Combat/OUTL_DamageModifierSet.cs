using System;
using UnityEngine;

[Serializable]
public struct OUTL_DamageModifier
{
    public string Key;
    public float Multiplier;
    public float FlatReduction;
}

[DisallowMultipleComponent]
public class OUTL_DamageModifierSet : MonoBehaviour
{
    public float GlobalMultiplier = 1f;
    public float ArmorStatScale = 0.01f;
    public OUTL_DamageModifier[] Modifiers;

    public float ModifyDamage(string damageKey, float inputDamage, OUTL_EntityRuntime runtime)
    {
        float damage = inputDamage * Mathf.Max(0f, GlobalMultiplier);

        if (runtime != null)
        {
            float armor = runtime.Stats.Get(OUTL_StatId.Armor, 0f);
            if (armor > 0f)
                damage *= Mathf.Clamp01(1f - armor * Mathf.Max(0f, ArmorStatScale));
        }

        if (Modifiers != null && !string.IsNullOrEmpty(damageKey))
        {
            for (int i = 0; i < Modifiers.Length; i++)
            {
                if (Modifiers[i].Key != damageKey) continue;
                damage = damage * Mathf.Max(0f, Modifiers[i].Multiplier) - Mathf.Max(0f, Modifiers[i].FlatReduction);
                break;
            }
        }

        return Mathf.Max(0f, damage);
    }
}
