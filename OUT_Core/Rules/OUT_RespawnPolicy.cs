using System;

public enum OUT_RespawnMode
{
    None = 0,
    Immediate = 1,
    Delayed = 2,
    Recharge = 3,
    RefillOnly = 4
}

[Serializable]
public readonly struct OUT_RespawnPolicy
{
    public readonly OUT_RespawnMode Mode;
    public readonly float DelaySeconds;
    public readonly bool ResetState;
    public readonly bool ReenableObject;

    public OUT_RespawnPolicy(
        OUT_RespawnMode mode,
        float delaySeconds,
        bool resetState,
        bool reenableObject)
    {
        Mode = mode;
        DelaySeconds = delaySeconds;
        ResetState = resetState;
        ReenableObject = reenableObject;
    }

    public static OUT_RespawnPolicy None()
    {
        return new OUT_RespawnPolicy(OUT_RespawnMode.None, 0f, false, false);
    }

    public static OUT_RespawnPolicy Immediate(bool resetState = true, bool reenableObject = true)
    {
        return new OUT_RespawnPolicy(OUT_RespawnMode.Immediate, 0f, resetState, reenableObject);
    }

    public static OUT_RespawnPolicy Delayed(float delaySeconds, bool resetState = true, bool reenableObject = true)
    {
        return new OUT_RespawnPolicy(OUT_RespawnMode.Delayed, delaySeconds, resetState, reenableObject);
    }

    public static OUT_RespawnPolicy Recharge(float delaySeconds, bool resetState = false, bool reenableObject = true)
    {
        return new OUT_RespawnPolicy(OUT_RespawnMode.Recharge, delaySeconds, resetState, reenableObject);
    }

    public static OUT_RespawnPolicy RefillOnly(float delaySeconds)
    {
        return new OUT_RespawnPolicy(OUT_RespawnMode.RefillOnly, delaySeconds, false, false);
    }
}