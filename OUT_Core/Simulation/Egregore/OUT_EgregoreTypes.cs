using System;
using UnityEngine;

public enum OUT_EgregoreContainmentShape
{
    Sphere = 0,
    Box = 1
}

public enum OUT_EgregoreDominantForce
{
    Neutral = 0,
    Threat = 1,
    Fear = 2,
    Violence = 3,
    Hunger = 4,
    Greed = 5,
    Desire = 6,
    Sacred = 7,
    Shelter = 8,
    Corruption = 9,
    Social = 10
}

[Serializable]
public struct OUT_EgregoreState
{
    [Range(0f, 1f)] public float Threat;
    [Range(0f, 1f)] public float Fear;
    [Range(0f, 1f)] public float Violence;
    [Range(0f, 1f)] public float Hunger;
    [Range(0f, 1f)] public float Greed;
    [Range(0f, 1f)] public float Desire;
    [Range(0f, 1f)] public float Sacred;
    [Range(0f, 1f)] public float Shelter;
    [Range(0f, 1f)] public float Corruption;
    [Range(0f, 1f)] public float Social;

    public void Add(OUT_SignalChannelFlags channels, float intensity, OUT_EgregoreWeights weights)
    {
        float value = Mathf.Clamp01(intensity);

        if ((channels & OUT_SignalChannelFlags.Danger) != 0) Threat += value * weights.DangerToThreat;
        if ((channels & OUT_SignalChannelFlags.Fear) != 0) Fear += value * weights.FearToFear;
        if ((channels & OUT_SignalChannelFlags.Aggression) != 0) Violence += value * weights.AggressionToViolence;
        if ((channels & OUT_SignalChannelFlags.Death) != 0)
        {
            Fear += value * weights.DeathToFear;
            Corruption += value * weights.DeathToCorruption;
        }
        if ((channels & OUT_SignalChannelFlags.Pain) != 0)
        {
            Threat += value * weights.PainToThreat;
            Violence += value * weights.PainToViolence;
        }
        if ((channels & OUT_SignalChannelFlags.Food) != 0) Hunger += value * weights.FoodToHunger;
        if ((channels & OUT_SignalChannelFlags.Reward) != 0) Greed += value * weights.RewardToGreed;
        if ((channels & OUT_SignalChannelFlags.Treasure) != 0) Greed += value * weights.TreasureToGreed;
        if ((channels & OUT_SignalChannelFlags.Attraction) != 0) Desire += value * weights.AttractionToDesire;
        if ((channels & OUT_SignalChannelFlags.Shelter) != 0) Shelter += value * weights.ShelterToShelter;
        if ((channels & OUT_SignalChannelFlags.Sacred) != 0) Sacred += value * weights.SacredToSacred;
        if ((channels & OUT_SignalChannelFlags.Aversion) != 0) Corruption += value * weights.AversionToCorruption;
        if ((channels & OUT_SignalChannelFlags.Fire) != 0)
        {
            Threat += value * weights.FireToThreat;
            Corruption += value * weights.FireToCorruption;
        }
        if ((channels & OUT_SignalChannelFlags.Social) != 0) Social += value * weights.SocialToSocial;
        if ((channels & OUT_SignalChannelFlags.Command) != 0) Social += value * weights.CommandToSocial;

        Clamp();
    }

    public void Decay(float amount)
    {
        if (amount <= 0f)
            return;

        Threat = Mathf.MoveTowards(Threat, 0f, amount);
        Fear = Mathf.MoveTowards(Fear, 0f, amount);
        Violence = Mathf.MoveTowards(Violence, 0f, amount);
        Hunger = Mathf.MoveTowards(Hunger, 0f, amount);
        Greed = Mathf.MoveTowards(Greed, 0f, amount);
        Desire = Mathf.MoveTowards(Desire, 0f, amount);
        Sacred = Mathf.MoveTowards(Sacred, 0f, amount * 0.35f);
        Shelter = Mathf.MoveTowards(Shelter, 0f, amount * 0.5f);
        Corruption = Mathf.MoveTowards(Corruption, 0f, amount * 0.25f);
        Social = Mathf.MoveTowards(Social, 0f, amount);
    }

    public void Clamp()
    {
        Threat = Mathf.Clamp01(Threat);
        Fear = Mathf.Clamp01(Fear);
        Violence = Mathf.Clamp01(Violence);
        Hunger = Mathf.Clamp01(Hunger);
        Greed = Mathf.Clamp01(Greed);
        Desire = Mathf.Clamp01(Desire);
        Sacred = Mathf.Clamp01(Sacred);
        Shelter = Mathf.Clamp01(Shelter);
        Corruption = Mathf.Clamp01(Corruption);
        Social = Mathf.Clamp01(Social);
    }

    public OUT_EgregoreDominantForce GetDominantForce()
    {
        OUT_EgregoreDominantForce force = OUT_EgregoreDominantForce.Neutral;
        float best = 0.001f;

        Check(OUT_EgregoreDominantForce.Threat, Threat, ref force, ref best);
        Check(OUT_EgregoreDominantForce.Fear, Fear, ref force, ref best);
        Check(OUT_EgregoreDominantForce.Violence, Violence, ref force, ref best);
        Check(OUT_EgregoreDominantForce.Hunger, Hunger, ref force, ref best);
        Check(OUT_EgregoreDominantForce.Greed, Greed, ref force, ref best);
        Check(OUT_EgregoreDominantForce.Desire, Desire, ref force, ref best);
        Check(OUT_EgregoreDominantForce.Sacred, Sacred, ref force, ref best);
        Check(OUT_EgregoreDominantForce.Shelter, Shelter, ref force, ref best);
        Check(OUT_EgregoreDominantForce.Corruption, Corruption, ref force, ref best);
        Check(OUT_EgregoreDominantForce.Social, Social, ref force, ref best);

        return force;
    }

    private static void Check(OUT_EgregoreDominantForce candidate, float value, ref OUT_EgregoreDominantForce force, ref float best)
    {
        if (value <= best)
            return;

        best = value;
        force = candidate;
    }
}

[Serializable]
public struct OUT_EgregoreWeights
{
    [Range(0f, 3f)] public float DangerToThreat;
    [Range(0f, 3f)] public float FearToFear;
    [Range(0f, 3f)] public float AggressionToViolence;
    [Range(0f, 3f)] public float DeathToFear;
    [Range(0f, 3f)] public float DeathToCorruption;
    [Range(0f, 3f)] public float PainToThreat;
    [Range(0f, 3f)] public float PainToViolence;
    [Range(0f, 3f)] public float FoodToHunger;
    [Range(0f, 3f)] public float RewardToGreed;
    [Range(0f, 3f)] public float TreasureToGreed;
    [Range(0f, 3f)] public float AttractionToDesire;
    [Range(0f, 3f)] public float ShelterToShelter;
    [Range(0f, 3f)] public float SacredToSacred;
    [Range(0f, 3f)] public float AversionToCorruption;
    [Range(0f, 3f)] public float FireToThreat;
    [Range(0f, 3f)] public float FireToCorruption;
    [Range(0f, 3f)] public float SocialToSocial;
    [Range(0f, 3f)] public float CommandToSocial;

    public static OUT_EgregoreWeights Default
    {
        get
        {
            return new OUT_EgregoreWeights
            {
                DangerToThreat = 1f,
                FearToFear = 1f,
                AggressionToViolence = 1f,
                DeathToFear = 0.8f,
                DeathToCorruption = 0.7f,
                PainToThreat = 0.6f,
                PainToViolence = 0.45f,
                FoodToHunger = 0.7f,
                RewardToGreed = 0.65f,
                TreasureToGreed = 1f,
                AttractionToDesire = 1f,
                ShelterToShelter = 0.8f,
                SacredToSacred = 1f,
                AversionToCorruption = 1f,
                FireToThreat = 0.8f,
                FireToCorruption = 0.45f,
                SocialToSocial = 0.7f,
                CommandToSocial = 0.6f
            };
        }
    }
}
