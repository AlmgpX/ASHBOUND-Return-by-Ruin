using System.Numerics;
using Raylib_cs;

namespace OUT_RayMicro.Input;

[Flags]
public enum OutmButtons : uint
{
    None = 0,
    Jump = 1 << 0,
    Crouch = 1 << 1,
    Sprint = 1 << 2,
    Use = 1 << 3,
    FirePrimary = 1 << 4,
    FireSecondary = 1 << 5,
    Melee = 1 << 6,
    Overlay = 1 << 7,
    DebugDamage = 1 << 8,
    DebugArmor = 1 << 9,
    DebugSave = 1 << 10,
    DebugLoad = 1 << 11,
    LevelDesign = 1 << 12,
    LevelDesignNext = 1 << 13,
    Pause = 1 << 14,
    Fullscreen = 1 << 15
}

public readonly struct OutmInputFrame
{
    public readonly int Sequence;
    public readonly float DeltaTime;
    public readonly Vector2 Move;
    public readonly Vector2 LookDelta;
    public readonly OutmButtons Down;
    public readonly OutmButtons Pressed;
    public readonly OutmButtons Released;

    public OutmInputFrame(int sequence, float deltaTime, Vector2 move, Vector2 lookDelta, OutmButtons down, OutmButtons pressed, OutmButtons released)
    {
        Sequence = sequence;
        DeltaTime = deltaTime;
        Move = move;
        LookDelta = lookDelta;
        Down = down;
        Pressed = pressed;
        Released = released;
    }

    public bool IsDown(OutmButtons button) => (Down & button) != 0;
    public bool IsPressed(OutmButtons button) => (Pressed & button) != 0;
    public bool IsReleased(OutmButtons button) => (Released & button) != 0;
}

public sealed class OutmInputSampler
{
    private OutmButtons previousDown;
    private int sequence;

    public OutmInputFrame Sample(float dt)
    {
        OutmButtons down = ReadButtonsDown();
        OutmButtons pressed = down & ~previousDown;
        OutmButtons released = previousDown & ~down;
        previousDown = down;

        Vector2 move = ReadMove();
        Vector2 look = Raylib.GetMouseDelta();

        return new OutmInputFrame(sequence++, dt, move, look, down, pressed, released);
    }

    private static Vector2 ReadMove()
    {
        Vector2 move = Vector2.Zero;
        if (Raylib.IsKeyDown(KeyboardKey.W)) move.Y += 1.0f;
        if (Raylib.IsKeyDown(KeyboardKey.S)) move.Y -= 1.0f;
        if (Raylib.IsKeyDown(KeyboardKey.D)) move.X += 1.0f;
        if (Raylib.IsKeyDown(KeyboardKey.A)) move.X -= 1.0f;

        if (move.LengthSquared() > 1.0f)
            move = Vector2.Normalize(move);

        return move;
    }

    private static OutmButtons ReadButtonsDown()
    {
        OutmButtons buttons = OutmButtons.None;

        if (Raylib.IsKeyDown(KeyboardKey.Space)) buttons |= OutmButtons.Jump;
        if (Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.C)) buttons |= OutmButtons.Crouch;
        if (Raylib.IsKeyDown(KeyboardKey.LeftShift)) buttons |= OutmButtons.Sprint;
        if (Raylib.IsKeyDown(KeyboardKey.E)) buttons |= OutmButtons.Use;
        if (Raylib.IsMouseButtonDown(MouseButton.Left)) buttons |= OutmButtons.FirePrimary;
        if (Raylib.IsMouseButtonDown(MouseButton.Right)) buttons |= OutmButtons.FireSecondary;
        if (Raylib.IsKeyDown(KeyboardKey.V)) buttons |= OutmButtons.Melee;
        if (Raylib.IsKeyDown(KeyboardKey.F1)) buttons |= OutmButtons.Overlay;
        if (Raylib.IsKeyDown(KeyboardKey.F2)) buttons |= OutmButtons.DebugDamage;
        if (Raylib.IsKeyDown(KeyboardKey.F3)) buttons |= OutmButtons.DebugArmor;
        if (Raylib.IsKeyDown(KeyboardKey.F5)) buttons |= OutmButtons.DebugSave;
        if (Raylib.IsKeyDown(KeyboardKey.F6)) buttons |= OutmButtons.LevelDesign;
        if (Raylib.IsKeyDown(KeyboardKey.F7)) buttons |= OutmButtons.LevelDesignNext;
        if (Raylib.IsKeyDown(KeyboardKey.F9)) buttons |= OutmButtons.DebugLoad;
        if (Raylib.IsKeyDown(KeyboardKey.Escape)) buttons |= OutmButtons.Pause;
        if (Raylib.IsKeyDown(KeyboardKey.F11)) buttons |= OutmButtons.Fullscreen;

        return buttons;
    }
}
