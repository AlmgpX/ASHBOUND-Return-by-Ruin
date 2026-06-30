using UnityEngine;

[System.Serializable]
public struct OUT_AfflictionState
{
    public OUT_AfflictionKind Kind;

    [Min(0)] public int Stacks;
    [Min(0f)] public float Intensity;
    [Min(0f)] public float RemainingTime;

    public bool IsInfinite;
    public bool BlocksUse;

    public bool IsActive =>
        Kind != OUT_AfflictionKind.None &&
        (IsInfinite || RemainingTime > 0f) &&
        (Stacks > 0 || Intensity > 0f);

    public static OUT_AfflictionState Create(
        OUT_AfflictionKind kind,
        float duration,
        float intensity = 1f,
        int stacks = 1,
        bool isInfinite = false,
        bool blocksUse = false)
    {
        OUT_AfflictionState state = new OUT_AfflictionState
        {
            Kind = kind,
            Stacks = Mathf.Max(1, stacks),
            Intensity = Mathf.Max(0f, intensity),
            RemainingTime = Mathf.Max(0f, duration),
            IsInfinite = isInfinite,
            BlocksUse = blocksUse
        };

        return state;
    }

    public void Tick(float deltaTime)
    {
        if (!IsActive)
            return;

        if (IsInfinite)
            return;

        RemainingTime = Mathf.Max(0f, RemainingTime - Mathf.Max(0f, deltaTime));

        if (RemainingTime <= 0f)
            Clear();
    }

    public void Refresh(
        float duration,
        float intensity = 1f,
        int addStacks = 1,
        bool makeInfinite = false,
        bool blocksUse = false)
    {
        if (Kind == OUT_AfflictionKind.None)
            return;

        Stacks = Mathf.Max(1, Stacks + Mathf.Max(0, addStacks));
        Intensity = Mathf.Max(Intensity, Mathf.Max(0f, intensity));

        if (makeInfinite)
        {
            IsInfinite = true;
            RemainingTime = 0f;
        }
        else
        {
            RemainingTime = Mathf.Max(RemainingTime, Mathf.Max(0f, duration));
        }

        BlocksUse |= blocksUse;
    }

    public void Clear()
    {
        Kind = OUT_AfflictionKind.None;
        Stacks = 0;
        Intensity = 0f;
        RemainingTime = 0f;
        IsInfinite = false;
        BlocksUse = false;
    }
}