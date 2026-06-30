public interface IOutSimSystem
{
    string SimSystemName { get; }
    OUT_SimulationTier Tiers { get; }
    void Tick(OUT_SimWorld world, in OUT_SimTickContext context);
}

public interface IOutRandomWorldTickReceiver
{
    void RandomWorldTick(OUT_SimWorld world, in OUT_SimTickContext context, uint randomValue);
}
