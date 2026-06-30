public struct OUT_SimTickContext
{
    public int Tick;
    public float Time;
    public float DeltaTime;
    public float TimeScale;
    public uint RandomSeed;
    public OUT_SimulationTier Tier;
    public bool IsCatchUpTick;

    public OUT_SimTickContext(int tick, float time, float deltaTime, float timeScale, uint randomSeed, OUT_SimulationTier tier, bool isCatchUpTick)
    {
        Tick = tick;
        Time = time;
        DeltaTime = deltaTime;
        TimeScale = timeScale;
        RandomSeed = randomSeed;
        Tier = tier;
        IsCatchUpTick = isCatchUpTick;
    }
}
