using Unity.Profiling;

public struct OUTL_FrameStats
{
    public int Entities;
    public int Tickables;
    public int RandomTickables;
    public int LogicTicks;
    public int AITicks;
    public int QuestTicks;
    public int FullTicks;
    public int RandomTicks;
    public int EventsEmitted;
    public int EventsFlushed;
    public int CommandsSent;
    public int CommandsHandled;
    public int QueuedCommands;
    public int EffectsApplied;
    public int Raycasts;
    public int Overlaps;
    public int PoolSpawns;
    public int PoolReleases;
    public int PoolMisses;
    public int Despawns;
    public int SaveEntities;
    public int RestoreEntities;
}

public static class OUTL_Profile
{
    public static readonly ProfilerMarker WorldUpdate = new ProfilerMarker("OUTL.World.Update");
    public static readonly ProfilerMarker SchedulerTickLane = new ProfilerMarker("OUTL.Scheduler.TickLane");
    public static readonly ProfilerMarker SchedulerRandomTick = new ProfilerMarker("OUTL.Scheduler.RandomTick");
    public static readonly ProfilerMarker SectorRegisterOrUpdate = new ProfilerMarker("OUTL.Sectors.RegisterOrUpdate");
    public static readonly ProfilerMarker SectorQuery = new ProfilerMarker("OUTL.Sectors.Query");
    public static readonly ProfilerMarker EventFlush = new ProfilerMarker("OUTL.EventBus.Flush");
    public static readonly ProfilerMarker CommandSend = new ProfilerMarker("OUTL.Commands.Send");
    public static readonly ProfilerMarker EffectsApplyAll = new ProfilerMarker("OUTL.Effects.ApplyAll");
    public static readonly ProfilerMarker EffectApply = new ProfilerMarker("OUTL.Effect.Apply");
    public static readonly ProfilerMarker AITick = new ProfilerMarker("OUTL.AI.Tick");
    public static readonly ProfilerMarker Perception = new ProfilerMarker("OUTL.AI.Perception");
    public static readonly ProfilerMarker PoolSpawn = new ProfilerMarker("OUTL.Pool.Spawn");
    public static readonly ProfilerMarker PoolRelease = new ProfilerMarker("OUTL.Pool.Release");
    public static readonly ProfilerMarker SaveCapture = new ProfilerMarker("OUTL.Save.Capture");
    public static readonly ProfilerMarker SaveRestore = new ProfilerMarker("OUTL.Save.Restore");

    public static OUTL_FrameStats Frame;
    public static OUTL_FrameStats LastFrame;

    public static void BeginFrame(int entities, int tickables, int randomTickables)
    {
        LastFrame = Frame;
        Frame = default(OUTL_FrameStats);
        Frame.Entities = entities;
        Frame.Tickables = tickables;
        Frame.RandomTickables = randomTickables;
    }

    public static void AddTick(OUTL_TickLane lane)
    {
        switch (lane)
        {
            case OUTL_TickLane.Full: Frame.FullTicks++; break;
            case OUTL_TickLane.Logic: Frame.LogicTicks++; break;
            case OUTL_TickLane.AI: Frame.AITicks++; break;
            case OUTL_TickLane.Quest: Frame.QuestTicks++; break;
        }
    }
}
