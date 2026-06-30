using System.Collections.Generic;
using UnityEngine;

public struct OUTL_NPCBehaviorBudgetSnapshot
{
    public int Registered;
    public int TickedThisFrame;
    public int SkippedByTier;
    public int SkippedByBudget;
    public int RouteUpdatesUsed;
    public int PathRequestsUsed;
    public int StimulusInterruptsUsed;
}

public sealed class OUTL_NPCBehaviorDispatcher : OUTL_ITickable
{
    private const float DispatcherInterval = 0.05f;
    private static readonly OUTL_NPCBehaviorDispatcher instance = new OUTL_NPCBehaviorDispatcher();
    private static readonly List<Entry> entries = new List<Entry>(1024);
    private static readonly Dictionary<OUTL_NPCBehaviorController, int> indices = new Dictionary<OUTL_NPCBehaviorController, int>(1024);
    private static int cursor;
    private static OUTL_NPCBehaviorDispatcher activeBudget;

    private readonly OUTL_NPCWorldRouteCache sharedRouteCache = new OUTL_NPCWorldRouteCache { MaxRoutes = 512 };
    private int routeUpdatesRemaining;
    private int pathRequestsRemaining;
    private int stimulusInterruptsRemaining;
    private OUTL_NPCBehaviorBudgetSnapshot lastSnapshot;
    private bool registeredWithScheduler;

    private struct Entry
    {
        public OUTL_NPCBehaviorController Controller;
        public float NextTickTime;
    }

    public static OUTL_NPCWorldRouteCache SharedRouteCache { get { return instance.sharedRouteCache; } }
    public static int RegisteredCount { get { return entries.Count; } }
    public static int RouteUpdatesRemaining { get { return activeBudget != null ? activeBudget.routeUpdatesRemaining : int.MaxValue; } }
    public static int PathRequestsRemaining { get { return activeBudget != null ? activeBudget.pathRequestsRemaining : int.MaxValue; } }
    public static int StimulusInterruptsRemaining { get { return activeBudget != null ? activeBudget.stimulusInterruptsRemaining : int.MaxValue; } }
    public static OUTL_NPCBehaviorBudgetSnapshot LastSnapshot { get { return instance.lastSnapshot; } }

    public bool OUTL_IsTickEnabled { get { return entries.Count > 0; } }
    public OUTL_TickLane OUTL_TickLane { get { return OUTL_TickLane.AI; } }
    public float OUTL_TickInterval { get { return DispatcherInterval; } }

    public static void Register(OUTL_NPCBehaviorController controller)
    {
        if (controller == null || indices.ContainsKey(controller)) return;
        OUTL_World world = OUTL_World.Instance;
        float now = world != null ? world.WorldTime : Time.time;
        indices[controller] = entries.Count;
        entries.Add(new Entry { Controller = controller, NextTickTime = now + StablePhase(controller) * Mathf.Max(0.01f, controller.OUTL_NPCRequestedTickInterval) });
        instance.EnsureSchedulerRegistration(world);
    }

    public static void Unregister(OUTL_NPCBehaviorController controller)
    {
        int index;
        if (controller == null || !indices.TryGetValue(controller, out index)) return;
        int last = entries.Count - 1;
        indices.Remove(controller);
        if (index != last)
        {
            Entry moved = entries[last];
            entries[index] = moved;
            if (moved.Controller != null) indices[moved.Controller] = index;
        }
        entries.RemoveAt(last);
        if (cursor > entries.Count) cursor = entries.Count;
    }

    public static bool TryConsumeRouteUpdate()
    {
        if (activeBudget == null) return true;
        if (activeBudget.routeUpdatesRemaining <= 0) return false;
        activeBudget.routeUpdatesRemaining--;
        return true;
    }

    public static bool TryConsumePathRequest()
    {
        if (activeBudget == null) return true;
        if (activeBudget.pathRequestsRemaining <= 0) return false;
        activeBudget.pathRequestsRemaining--;
        return true;
    }

    public static bool TryConsumeStimulusInterrupt()
    {
        if (activeBudget == null) return true;
        if (activeBudget.stimulusInterruptsRemaining <= 0) return false;
        activeBudget.stimulusInterruptsRemaining--;
        return true;
    }

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        if (world == null || entries.Count == 0) return;
        routeUpdatesRemaining = Mathf.Max(0, world.MaxNpcRouteUpdatesPerFrame);
        pathRequestsRemaining = Mathf.Max(0, world.MaxNpcPathRequestsPerFrame);
        stimulusInterruptsRemaining = Mathf.Max(0, world.MaxNpcStimulusInterruptsPerFrame);
        int startRoute = routeUpdatesRemaining;
        int startPath = pathRequestsRemaining;
        int startStimulus = stimulusInterruptsRemaining;
        lastSnapshot = new OUTL_NPCBehaviorBudgetSnapshot { Registered = entries.Count };

        int behaviorBudget = Mathf.Min(Mathf.Max(0, world.MaxNpcBehaviorTicksPerFrame), entries.Count);
        if (behaviorBudget <= 0)
        {
            lastSnapshot.SkippedByBudget = entries.Count;
            return;
        }

        activeBudget = this;
        int visited = 0;
        int processed = 0;
        while (visited < entries.Count && processed < behaviorBudget && entries.Count > 0)
        {
            if (cursor >= entries.Count) cursor = 0;
            int index = cursor++;
            Entry entry = entries[index];
            OUTL_NPCBehaviorController controller = entry.Controller;
            if (controller == null)
            {
                RemoveAt(index);
                cursor = Mathf.Min(index, entries.Count);
                visited++;
                continue;
            }

            if (time < entry.NextTickTime)
            {
                lastSnapshot.SkippedByTier++;
                visited++;
                continue;
            }

            entry.NextTickTime = time + Mathf.Max(0.01f, controller.OUTL_NPCRequestedTickInterval);
            entries[index] = entry;
            controller.OUTL_RunBudgetedTick(world, time, deltaTime);
            processed++;
            lastSnapshot.TickedThisFrame++;
            visited++;
        }

        if (processed >= behaviorBudget) lastSnapshot.SkippedByBudget = CountDue(time);
        lastSnapshot.RouteUpdatesUsed = Mathf.Max(0, startRoute - routeUpdatesRemaining);
        lastSnapshot.PathRequestsUsed = Mathf.Max(0, startPath - pathRequestsRemaining);
        lastSnapshot.StimulusInterruptsUsed = Mathf.Max(0, startStimulus - stimulusInterruptsRemaining);
        activeBudget = null;
    }

    private void EnsureSchedulerRegistration(OUTL_World world)
    {
        if (world == null || registeredWithScheduler) return;
        world.Scheduler.Register(this);
        registeredWithScheduler = true;
    }

    private static void RemoveAt(int index)
    {
        if (index < 0 || index >= entries.Count) return;
        Entry removed = entries[index];
        if (removed.Controller != null) indices.Remove(removed.Controller);
        int last = entries.Count - 1;
        if (index != last)
        {
            Entry moved = entries[last];
            entries[index] = moved;
            if (moved.Controller != null) indices[moved.Controller] = index;
        }
        entries.RemoveAt(last);
    }

    private static float StablePhase(OUTL_NPCBehaviorController controller)
    {
        if (controller == null) return 0f;
        unchecked
        {
            int seed = 0;
            OUTL_EntityAdapter entity = controller.Entity;
            if (entity != null)
            {
                if (entity.Id.IsValid) seed = entity.Id.Value;
                else if (!string.IsNullOrEmpty(entity.StableId)) seed = StableStringHash(entity.StableId);
                else if (!string.IsNullOrEmpty(entity.TargetName)) seed = StableStringHash(entity.TargetName);
                else if (!string.IsNullOrEmpty(entity.ClassNameOverride)) seed = StableStringHash(entity.ClassNameOverride);
            }
            uint h = (uint)(seed * 1103515245 + 12345);
            return (h & 0x00FFFFFFu) / 16777215f;
        }
    }

    private static int StableStringHash(string value)
    {
        if (string.IsNullOrEmpty(value)) return 0;
        unchecked
        {
            int hash = 23;
            for (int i = 0; i < value.Length; i++) hash = hash * 31 + value[i];
            return hash;
        }
    }

    private static int CountDue(float time)
    {
        int due = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            OUTL_NPCBehaviorController controller = entries[i].Controller;
            if (controller != null && time >= entries[i].NextTickTime) due++;
        }
        return due;
    }
}
