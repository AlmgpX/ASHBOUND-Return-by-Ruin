using System;
using UnityEngine;

[Serializable]
public struct OUT_SimulationTierStats
{
    public OUT_SimulationTier Tier;
    public int TickCount;
    public int LastFrameTicks;
    public float LastTickMs;
    public float AverageTickMs;
    public float MaxTickMs;

    public void RecordTick(float milliseconds)
    {
        TickCount++;
        LastFrameTicks++;
        LastTickMs = milliseconds;
        MaxTickMs = Mathf.Max(MaxTickMs, milliseconds);
        AverageTickMs = AverageTickMs <= 0f ? milliseconds : Mathf.Lerp(AverageTickMs, milliseconds, 0.08f);
    }

    public void BeginFrame()
    {
        LastFrameTicks = 0;
    }

    public void Reset()
    {
        TickCount = 0;
        LastFrameTicks = 0;
        LastTickMs = 0f;
        AverageTickMs = 0f;
        MaxTickMs = 0f;
    }
}

[Serializable]
public struct OUT_SimulationStats
{
    public int Frame;
    public int RegisteredSystems;
    public int RegisteredRandomReceivers;
    public int DroppedSteps;
    public int TotalSteps;
    public float InterpolationAlpha;
    public float Accumulator;
    public OUT_SimulationTierStats Full;
    public OUT_SimulationTierStats Reduced;
    public OUT_SimulationTierStats Abstract;
    public OUT_SimulationTierStats RandomWorld;

    public void Initialize()
    {
        Full.Tier = OUT_SimulationTier.Full;
        Reduced.Tier = OUT_SimulationTier.Reduced;
        Abstract.Tier = OUT_SimulationTier.Abstract;
        RandomWorld.Tier = OUT_SimulationTier.RandomWorld;
    }

    public void BeginFrame(int frame)
    {
        Frame = frame;
        Full.BeginFrame();
        Reduced.BeginFrame();
        Abstract.BeginFrame();
        RandomWorld.BeginFrame();
    }

    public void Record(OUT_SimulationTier tier, float milliseconds)
    {
        if ((tier & OUT_SimulationTier.Full) != 0)
            Full.RecordTick(milliseconds);
        else if ((tier & OUT_SimulationTier.Reduced) != 0)
            Reduced.RecordTick(milliseconds);
        else if ((tier & OUT_SimulationTier.Abstract) != 0)
            Abstract.RecordTick(milliseconds);
        else if ((tier & OUT_SimulationTier.RandomWorld) != 0)
            RandomWorld.RecordTick(milliseconds);
    }

    public void Reset()
    {
        DroppedSteps = 0;
        TotalSteps = 0;
        InterpolationAlpha = 0f;
        Accumulator = 0f;
        Full.Reset();
        Reduced.Reset();
        Abstract.Reset();
        RandomWorld.Reset();
        Initialize();
    }

    public string BuildSummary()
    {
        return "sim frame:" + Frame +
               " systems:" + RegisteredSystems +
               " randomReceivers:" + RegisteredRandomReceivers +
               " steps:" + TotalSteps +
               " dropped:" + DroppedSteps +
               " alpha:" + InterpolationAlpha.ToString("0.00") +
               " | full " + Full.TickCount + " avg:" + Full.AverageTickMs.ToString("0.000") + "ms" +
               " | reduced " + Reduced.TickCount + " avg:" + Reduced.AverageTickMs.ToString("0.000") + "ms" +
               " | abstract " + Abstract.TickCount + " avg:" + Abstract.AverageTickMs.ToString("0.000") + "ms" +
               " | random " + RandomWorld.TickCount + " avg:" + RandomWorld.AverageTickMs.ToString("0.000") + "ms";
    }
}
