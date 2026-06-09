using System;
using System.Collections.Generic;

namespace OUT_ASHBOUND;

public enum OUT_Mode { World, Local, Talk }
public enum OUT_Scope { World, Local }
public enum OUT_CommandType { Move, Wait, Interact, Talk, Attack }
public enum OUT_EventType { Message, ShardTaken, GateOpened }

public readonly record struct OUT_Pos(int X, int Y)
{
    public static readonly OUT_Pos Zero = new(0, 0);
    public static readonly OUT_Pos Up = new(0, -1);
    public static readonly OUT_Pos Down = new(0, 1);
    public static readonly OUT_Pos Left = new(-1, 0);
    public static readonly OUT_Pos Right = new(1, 0);
    public static readonly OUT_Pos[] Cardinals = { Up, Down, Left, Right };
    public static OUT_Pos operator +(OUT_Pos a, OUT_Pos b) => new(a.X + b.X, a.Y + b.Y);
    public static int Distance(OUT_Pos a, OUT_Pos b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
}

public readonly record struct OUT_Command(OUT_CommandType Type, int Source, int Target, OUT_Pos Direction)
{
    public static OUT_Command Move(int source, OUT_Pos dir) => new(OUT_CommandType.Move, source, 0, dir);
    public static OUT_Command Wait(int source) => new(OUT_CommandType.Wait, source, 0, OUT_Pos.Zero);
    public static OUT_Command Interact(int source) => new(OUT_CommandType.Interact, source, 0, OUT_Pos.Zero);
    public static OUT_Command Talk(int source) => new(OUT_CommandType.Talk, source, 0, OUT_Pos.Zero);
    public static OUT_Command Attack(int source, int target) => new(OUT_CommandType.Attack, source, target, OUT_Pos.Zero);
}

public readonly record struct OUT_Event(OUT_EventType Type, string Text)
{
    public static OUT_Event Message(string text) => new(OUT_EventType.Message, text);
    public static OUT_Event Shard(string text) => new(OUT_EventType.ShardTaken, text);
    public static OUT_Event Gate(string text) => new(OUT_EventType.GateOpened, text);
}

public sealed class OUT_Stats
{
    public int MaxHp { get; }
    public int MaxStamina { get; }
    public int Attack { get; }
    public int Hp { get; set; }
    public int Stamina { get; set; }

    public OUT_Stats(int hp, int stamina, int attack)
    {
        MaxHp = Math.Max(1, hp);
        Hp = MaxHp;
        MaxStamina = Math.Max(0, stamina);
        Stamina = MaxStamina;
        Attack = Math.Max(0, attack);
    }
}

public sealed record OUT_EntityVector(string C, string S, int H, int A, int I);
