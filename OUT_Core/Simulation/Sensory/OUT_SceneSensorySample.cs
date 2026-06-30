using UnityEngine;

[System.Serializable]
public struct OUT_SceneSensorySample
{
    public bool HasGround;
    public float GroundHeight;

    [Range(0f, 1f)] public float Luminance;
    [Range(0f, 1f)] public float SkyLuminance;
    [Range(0f, 1f)] public float GroundLuminance;
    [Range(0f, 1f)] public float Occlusion;
    [Range(0f, 1f)] public float Cover;
    [Range(0f, 1f)] public float GroundSafety;
    [Range(0f, 1f)] public float AreaCost;

    [Range(0f, 1f)] public float Noise;
    [Range(0f, 1f)] public float Danger;
    [Range(0f, 1f)] public float Food;
    [Range(0f, 1f)] public float Fire;

    public void Clear()
    {
        HasGround = false;
        GroundHeight = 0f;

        Luminance = 0f;
        SkyLuminance = 0f;
        GroundLuminance = 0f;
        Occlusion = 0f;
        Cover = 0f;
        GroundSafety = 0f;
        AreaCost = 0f;

        Noise = 0f;
        Danger = 0f;
        Food = 0f;
        Fire = 0f;
    }

    public void Clamp01()
    {
        Luminance = Mathf.Clamp01(Luminance);
        SkyLuminance = Mathf.Clamp01(SkyLuminance);
        GroundLuminance = Mathf.Clamp01(GroundLuminance);
        Occlusion = Mathf.Clamp01(Occlusion);
        Cover = Mathf.Clamp01(Cover);
        GroundSafety = Mathf.Clamp01(GroundSafety);
        AreaCost = Mathf.Clamp01(AreaCost);

        Noise = Mathf.Clamp01(Noise);
        Danger = Mathf.Clamp01(Danger);
        Food = Mathf.Clamp01(Food);
        Fire = Mathf.Clamp01(Fire);
    }

    public float GetChannelValue(OUT_SensoryChannelFlags channel)
    {
        switch (channel)
        {
            case OUT_SensoryChannelFlags.Luminance: return Luminance;
            case OUT_SensoryChannelFlags.SkyLuminance: return SkyLuminance;
            case OUT_SensoryChannelFlags.GroundLuminance: return GroundLuminance;
            case OUT_SensoryChannelFlags.Occlusion: return Occlusion;
            case OUT_SensoryChannelFlags.Cover: return Cover;
            case OUT_SensoryChannelFlags.GroundSafety: return GroundSafety;
            case OUT_SensoryChannelFlags.AreaCost: return AreaCost;
            case OUT_SensoryChannelFlags.Noise: return Noise;
            case OUT_SensoryChannelFlags.Danger: return Danger;
            case OUT_SensoryChannelFlags.Food: return Food;
            case OUT_SensoryChannelFlags.Fire: return Fire;
            default: return 0f;
        }
    }

    public void SetChannelValue(OUT_SensoryChannelFlags channel, float value)
    {
        value = Mathf.Clamp01(value);

        switch (channel)
        {
            case OUT_SensoryChannelFlags.Luminance: Luminance = value; break;
            case OUT_SensoryChannelFlags.SkyLuminance: SkyLuminance = value; break;
            case OUT_SensoryChannelFlags.GroundLuminance: GroundLuminance = value; break;
            case OUT_SensoryChannelFlags.Occlusion: Occlusion = value; break;
            case OUT_SensoryChannelFlags.Cover: Cover = value; break;
            case OUT_SensoryChannelFlags.GroundSafety: GroundSafety = value; break;
            case OUT_SensoryChannelFlags.AreaCost: AreaCost = value; break;
            case OUT_SensoryChannelFlags.Noise: Noise = value; break;
            case OUT_SensoryChannelFlags.Danger: Danger = value; break;
            case OUT_SensoryChannelFlags.Food: Food = value; break;
            case OUT_SensoryChannelFlags.Fire: Fire = value; break;
        }
    }

    public void SetMax(OUT_SensoryChannelFlags channel, float value)
    {
        value = Mathf.Clamp01(value);

        switch (channel)
        {
            case OUT_SensoryChannelFlags.Luminance: Luminance = Mathf.Max(Luminance, value); break;
            case OUT_SensoryChannelFlags.SkyLuminance: SkyLuminance = Mathf.Max(SkyLuminance, value); break;
            case OUT_SensoryChannelFlags.GroundLuminance: GroundLuminance = Mathf.Max(GroundLuminance, value); break;
            case OUT_SensoryChannelFlags.Occlusion: Occlusion = Mathf.Max(Occlusion, value); break;
            case OUT_SensoryChannelFlags.Cover: Cover = Mathf.Max(Cover, value); break;
            case OUT_SensoryChannelFlags.GroundSafety: GroundSafety = Mathf.Max(GroundSafety, value); break;
            case OUT_SensoryChannelFlags.AreaCost: AreaCost = Mathf.Max(AreaCost, value); break;
            case OUT_SensoryChannelFlags.Noise: Noise = Mathf.Max(Noise, value); break;
            case OUT_SensoryChannelFlags.Danger: Danger = Mathf.Max(Danger, value); break;
            case OUT_SensoryChannelFlags.Food: Food = Mathf.Max(Food, value); break;
            case OUT_SensoryChannelFlags.Fire: Fire = Mathf.Max(Fire, value); break;
        }
    }

    public void AddFrom(in OUT_SceneSensorySample other, OUT_SensoryChannelFlags channels)
    {
        if ((channels & OUT_SensoryChannelFlags.Luminance) != 0) Luminance = Mathf.Clamp01(Luminance + other.Luminance);
        if ((channels & OUT_SensoryChannelFlags.SkyLuminance) != 0) SkyLuminance = Mathf.Clamp01(SkyLuminance + other.SkyLuminance);
        if ((channels & OUT_SensoryChannelFlags.GroundLuminance) != 0) GroundLuminance = Mathf.Clamp01(GroundLuminance + other.GroundLuminance);
        if ((channels & OUT_SensoryChannelFlags.Occlusion) != 0) Occlusion = Mathf.Clamp01(Occlusion + other.Occlusion);
        if ((channels & OUT_SensoryChannelFlags.Cover) != 0) Cover = Mathf.Clamp01(Cover + other.Cover);
        if ((channels & OUT_SensoryChannelFlags.GroundSafety) != 0) GroundSafety = Mathf.Clamp01(GroundSafety + other.GroundSafety);
        if ((channels & OUT_SensoryChannelFlags.AreaCost) != 0) AreaCost = Mathf.Clamp01(AreaCost + other.AreaCost);
        if ((channels & OUT_SensoryChannelFlags.Noise) != 0) Noise = Mathf.Clamp01(Noise + other.Noise);
        if ((channels & OUT_SensoryChannelFlags.Danger) != 0) Danger = Mathf.Clamp01(Danger + other.Danger);
        if ((channels & OUT_SensoryChannelFlags.Food) != 0) Food = Mathf.Clamp01(Food + other.Food);
        if ((channels & OUT_SensoryChannelFlags.Fire) != 0) Fire = Mathf.Clamp01(Fire + other.Fire);
    }

    public void MaxFrom(in OUT_SceneSensorySample other, OUT_SensoryChannelFlags channels)
    {
        if ((channels & OUT_SensoryChannelFlags.Luminance) != 0) Luminance = Mathf.Max(Luminance, other.Luminance);
        if ((channels & OUT_SensoryChannelFlags.SkyLuminance) != 0) SkyLuminance = Mathf.Max(SkyLuminance, other.SkyLuminance);
        if ((channels & OUT_SensoryChannelFlags.GroundLuminance) != 0) GroundLuminance = Mathf.Max(GroundLuminance, other.GroundLuminance);
        if ((channels & OUT_SensoryChannelFlags.Occlusion) != 0) Occlusion = Mathf.Max(Occlusion, other.Occlusion);
        if ((channels & OUT_SensoryChannelFlags.Cover) != 0) Cover = Mathf.Max(Cover, other.Cover);
        if ((channels & OUT_SensoryChannelFlags.GroundSafety) != 0) GroundSafety = Mathf.Max(GroundSafety, other.GroundSafety);
        if ((channels & OUT_SensoryChannelFlags.AreaCost) != 0) AreaCost = Mathf.Max(AreaCost, other.AreaCost);
        if ((channels & OUT_SensoryChannelFlags.Noise) != 0) Noise = Mathf.Max(Noise, other.Noise);
        if ((channels & OUT_SensoryChannelFlags.Danger) != 0) Danger = Mathf.Max(Danger, other.Danger);
        if ((channels & OUT_SensoryChannelFlags.Food) != 0) Food = Mathf.Max(Food, other.Food);
        if ((channels & OUT_SensoryChannelFlags.Fire) != 0) Fire = Mathf.Max(Fire, other.Fire);
    }

    public bool HasAnyDynamicSignal()
    {
        return Noise > 0f || Danger > 0f || Food > 0f || Fire > 0f;
    }
}