using System.Numerics;

namespace OUT_RayMicro.Core;

public readonly struct EntityId
{
    public readonly int Index;
    public readonly int Generation;

    public EntityId(int index, int generation)
    {
        Index = index;
        Generation = generation;
    }

    public bool IsValid => Index >= 0;
    public override string ToString() => IsValid ? $"E{Index}:{Generation}" : "None";
    public static readonly EntityId None = new(-1, 0);
}

public enum OutmEventType : ushort
{
    None,
    Fired,
    ProjectileHit,
    ProjectileBounce,
    TriggerEntered,
    DoorToggled,
    Debug
}

public struct OutmEvent
{
    public OutmEventType Type;
    public EntityId Source;
    public EntityId Target;
    public Vector3 Point;
    public float Value;
    public string Text;

    public OutmEvent(OutmEventType type, EntityId source, EntityId target, Vector3 point, float value, string text = "")
    {
        Type = type;
        Source = source;
        Target = target;
        Point = point;
        Value = value;
        Text = text;
    }
}

public sealed class OutmEventQueue
{
    private readonly OutmEvent[] events;
    private int head;
    private int tail;
    private int count;

    public OutmEventQueue(int capacity)
    {
        events = new OutmEvent[Math.Max(8, capacity)];
    }

    public void Enqueue(in OutmEvent evt)
    {
        if (count == events.Length)
        {
            // Drop oldest. Debuggability beats allocation. Civilization may continue limping.
            head = (head + 1) % events.Length;
            count--;
        }

        events[tail] = evt;
        tail = (tail + 1) % events.Length;
        count++;
    }

    public bool TryDequeue(out OutmEvent evt)
    {
        if (count == 0)
        {
            evt = default;
            return false;
        }

        evt = events[head];
        head = (head + 1) % events.Length;
        count--;
        return true;
    }
}

public sealed class OutmWorld
{
    private readonly string[] log;
    private int logCursor;

    public readonly OutmEventQueue Events = new(512);
    public float Time;
    public int Tick;

    public OutmWorld(int logLines = 16)
    {
        log = new string[Math.Max(4, logLines)];
        for (int i = 0; i < log.Length; i++) log[i] = "";
    }

    public void BeginFrame(float dt)
    {
        Time += dt;
        Tick++;
    }

    public void Emit(in OutmEvent evt)
    {
        Events.Enqueue(evt);
        PushLog($"{Tick:00000} {evt.Type} {evt.Text}");
    }

    public void PushLog(string line)
    {
        log[logCursor] = line;
        logCursor = (logCursor + 1) % log.Length;
    }

    public string GetLogLineFromNewest(int newestOffset)
    {
        int index = logCursor - 1 - newestOffset;
        while (index < 0) index += log.Length;
        return log[index % log.Length];
    }
}
