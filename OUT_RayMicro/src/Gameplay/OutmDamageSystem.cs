using System.Numerics;
using OUT_RayMicro.Core;

namespace OUT_RayMicro.Gameplay;

public readonly struct OutmDamageResult
{
    public readonly int Incoming;
    public readonly int ArmorSaved;
    public readonly int HealthDamage;
    public readonly int HealthAfter;
    public readonly int ArmorAfter;
    public readonly OutmArmorTier ArmorTierAfter;

    public OutmDamageResult(int incoming, int armorSaved, int healthDamage, int healthAfter, int armorAfter, OutmArmorTier armorTierAfter)
    {
        Incoming = incoming;
        ArmorSaved = armorSaved;
        HealthDamage = healthDamage;
        HealthAfter = healthAfter;
        ArmorAfter = armorAfter;
        ArmorTierAfter = armorTierAfter;
    }
}

public static class OutmDamageSystem
{
    public static OutmDamageResult ApplyQuakeDamage(OutmWorld world, int incomingDamage, string reason)
    {
        incomingDamage = Math.Max(0, incomingDamage);
        OutmPlayerVitals vitals = world.PlayerVitals;

        int armorSaved = 0;
        float absorb = OutmArmorRules.AbsorbFraction(vitals.ArmorTier);

        if (incomingDamage > 0 && vitals.Armor > 0 && absorb > 0.0f)
        {
            armorSaved = (int)MathF.Ceiling(incomingDamage * absorb);
            if (armorSaved >= vitals.Armor)
            {
                armorSaved = vitals.Armor;
                vitals.Armor = 0;
                vitals.ArmorTier = OutmArmorTier.None;
                vitals.MaxArmor = 0;
            }
            else
            {
                vitals.Armor -= armorSaved;
            }
        }

        int healthDamage = Math.Max(0, incomingDamage - armorSaved);
        vitals.Health = Math.Max(0, vitals.Health - healthDamage);
        world.PlayerVitals = vitals;

        var result = new OutmDamageResult(incomingDamage, armorSaved, healthDamage, vitals.Health, vitals.Armor, vitals.ArmorTier);
        world.Emit(new OutmEvent(
            OutmEventType.DamageApplied,
            EntityId.None,
            EntityId.None,
            Vector3.Zero,
            incomingDamage,
            $"{reason}: dmg {incomingDamage}, armor {armorSaved}, hp {healthDamage}"));

        return result;
    }

    public static bool TryPickupQuakeArmor(OutmWorld world, OutmArmorTier tier, string reason)
    {
        int pickupArmor = OutmArmorRules.Capacity(tier);
        if (pickupArmor <= 0)
            return false;

        OutmPlayerVitals vitals = world.PlayerVitals;

        // Quake-style replacement: compare effective protection, not just raw armor points.
        int currentScore = OutmArmorRules.EffectiveProtectionScore(vitals.ArmorTier, vitals.Armor);
        int pickupScore = OutmArmorRules.EffectiveProtectionScore(tier, pickupArmor);
        if (currentScore >= pickupScore)
        {
            world.PushLog($"armor ignored: {OutmArmorRules.Code(tier)} is weaker than current {OutmArmorRules.Code(vitals.ArmorTier)}");
            return false;
        }

        vitals.ArmorTier = tier;
        vitals.Armor = pickupArmor;
        vitals.MaxArmor = pickupArmor;
        world.PlayerVitals = vitals;

        world.Emit(new OutmEvent(
            OutmEventType.ArmorPicked,
            EntityId.None,
            EntityId.None,
            Vector3.Zero,
            pickupArmor,
            $"{reason}: {OutmArmorRules.Code(tier)} {pickupArmor} absorb {OutmArmorRules.AbsorbFraction(tier):0.0}"));

        return true;
    }

    public static OutmArmorTier NextDebugArmorTier(OutmArmorTier current)
    {
        return current switch
        {
            OutmArmorTier.None => OutmArmorTier.Green,
            OutmArmorTier.Green => OutmArmorTier.Yellow,
            OutmArmorTier.Yellow => OutmArmorTier.Red,
            _ => OutmArmorTier.Green
        };
    }
}
