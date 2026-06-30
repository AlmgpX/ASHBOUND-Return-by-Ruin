namespace OUT_RayMicro.Runtime;

public sealed class OutmFixedStep
{
    public const float DefaultFixedDelta = 1.0f / 60.0f;
    public const int MaxTicksPerRenderFrame = 5;

    private float accumulator;
    private int tick;

    public float FixedDelta { get; }
    public int Tick => tick;
    public float InterpolationAlpha => accumulator / FixedDelta;

    public OutmFixedStep(float fixedDelta = DefaultFixedDelta)
    {
        FixedDelta = fixedDelta;
    }

    public void AddFrameTime(float dt)
    {
        accumulator += Math.Clamp(dt, 0.0f, 0.10f);
    }

    public bool TryConsumeTick(out int currentTick)
    {
        if (accumulator < FixedDelta)
        {
            currentTick = tick;
            return false;
        }

        accumulator -= FixedDelta;
        currentTick = tick++;
        return true;
    }

    public void ClampAfterSpiralLimit()
    {
        accumulator = Math.Min(accumulator, FixedDelta);
    }

    public void ClearAccumulator()
    {
        accumulator = 0.0f;
    }
}
