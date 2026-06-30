using System.Numerics;

namespace OUT_RayMicro.Input;

public enum OutmCommandType : byte
{
    UserInput,
    Use,
    DebugDamage,
    DebugArmor
}

public readonly struct OutmUserCommand
{
    public readonly int Sequence;
    public readonly int Tick;
    public readonly float FixedDeltaTime;
    public readonly Vector2 Move;
    public readonly Vector2 LookDelta;
    public readonly OutmButtons Down;
    public readonly OutmButtons Pressed;
    public readonly OutmButtons Released;

    public OutmUserCommand(int sequence, int tick, float fixedDeltaTime, Vector2 move, Vector2 lookDelta, OutmButtons down, OutmButtons pressed, OutmButtons released)
    {
        Sequence = sequence;
        Tick = tick;
        FixedDeltaTime = fixedDeltaTime;
        Move = move;
        LookDelta = lookDelta;
        Down = down;
        Pressed = pressed;
        Released = released;
    }

    public OutmInputFrame ToInputFrame()
    {
        return new OutmInputFrame(Sequence, FixedDeltaTime, Move, LookDelta, Down, Pressed, Released);
    }
}

public readonly struct OutmCommand
{
    public readonly OutmCommandType Type;
    public readonly OutmUserCommand User;

    public OutmCommand(OutmCommandType type, OutmUserCommand user)
    {
        Type = type;
        User = user;
    }
}

public sealed class OutmCommandQueue
{
    private readonly OutmCommand[] commands;
    private int head;
    private int tail;
    private int count;

    public OutmCommandQueue(int capacity = 256)
    {
        commands = new OutmCommand[Math.Max(8, capacity)];
    }

    public void Enqueue(in OutmCommand command)
    {
        if (count == commands.Length)
        {
            head = (head + 1) % commands.Length;
            count--;
        }

        commands[tail] = command;
        tail = (tail + 1) % commands.Length;
        count++;
    }

    public bool TryDequeue(out OutmCommand command)
    {
        if (count == 0)
        {
            command = default;
            return false;
        }

        command = commands[head];
        head = (head + 1) % commands.Length;
        count--;
        return true;
    }

    public void Clear()
    {
        head = 0;
        tail = 0;
        count = 0;
    }
}
