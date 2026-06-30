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
    Footstep,
    DamageApplied,
    ArmorPicked,
    Died,
    Debug
}

public enum OutmArmorTier : byte
{
    None,
    Green,
    Yellow,
    Red
}

public static class OutmArmorRules
{
    public static int Capacity(OutmArmorTier tier)
    {
        return tier switch
        {
            OutmArmorTier.Green => 100,
            OutmArmorTier.Yellow => 150,
            OutmArmorTier.Red => 200,
            _ => 0
        };
    }

    public static float AbsorbFraction(OutmArmorTier tier)
    {
        return tier switch
        {
            OutmArmorTier.Green => 0.30f,
            OutmArmorTier.Yellow => 0.60f,
            OutmArmorTier.Red => 0.80f,
            _ => 0.0f
        };
    }

    public static string Code(OutmArmorTier tier)
    {
        return tier switch
        {
            OutmArmorTier.Green => "GA",
            OutmArmorTier.Yellow => "YA",
            OutmArmorTier.Red => "RA",
            _ => "--"
        };
    }

    public static int EffectiveProtectionScore(OutmArmorTier tier, int armor)
    {
        return (int)MathF.Round(armor * AbsorbFraction(tier) * 1000.0f);
    }
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

public struct OutmPlayerVitals
{
    public int Health;
    public int Armor;
    public int Mana;
    public int MaxHealth;
    public int MaxArmor;
    public int MaxMana;
    public OutmArmorTier ArmorTier;
    public bool IsDead;

    public static OutmPlayerVitals Default => new()
    {
        Health = 100,
        Armor = 100,
        Mana = 80,
        MaxHealth = 100,
        MaxArmor = OutmArmorRules.Capacity(OutmArmorTier.Green),
        MaxMana = 100,
        ArmorTier = OutmArmorTier.Green,
        IsDead = false
    };
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
    public readonly OutmEntityStore Entities = new(512);
    public readonly OutmTransformStore Transforms = new(512);
    public EntityId PlayerEntity = EntityId.None;
    public OutmPlayerVitals PlayerVitals = OutmPlayerVitals.Default;
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
