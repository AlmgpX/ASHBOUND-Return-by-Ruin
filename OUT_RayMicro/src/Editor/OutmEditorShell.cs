using Raylib_cs;
using OUT_RayMicro.Core;
using OUT_RayMicro.Runtime;
using OUT_RayMicro.World;

namespace OUT_RayMicro.Editor;

public sealed class OutmEditorShell
{
    public bool Visible = true;

    private static readonly Color OverlayCyan = new(80, 220, 220, 255);
    private static readonly Color OverlayText = new(205, 212, 220, 255);
    private static readonly Color HudBack = new(0, 0, 0, 150);
    private static readonly Color HudFrame = new(90, 210, 220, 210);
    private static readonly Color HeartFull = new(235, 48, 74, 255);
    private static readonly Color HeartEmpty = new(82, 30, 42, 230);
    private static readonly Color ArmorFull = new(235, 206, 92, 255);
    private static readonly Color ArmorEmpty = new(72, 68, 48, 220);
    private static readonly Color ManaFull = new(60, 92, 255, 255);
    private static readonly Color ManaEmpty = new(28, 38, 92, 220);
    private static readonly Color ManaAccent = new(80, 255, 160, 255);

    public void Update(OutmWorld world)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.F1))
        {
            Visible = !Visible;
            world.PushLog(Visible ? "editor overlay on" : "editor overlay off");
        }
    }

    public void Draw(OutmWorld world, OutmCameraMotor camera, OutmDemoMap map)
    {
        int w = Raylib.GetScreenWidth();
        int h = Raylib.GetScreenHeight();

        Raylib.DrawText("+", w / 2 - 4, h / 2 - 8, 20, Color.White);
        DrawVitalsHud(world.PlayerVitals, 18, h - 116);

        if (!Visible)
            return;

        Raylib.DrawRectangle(10, 10, 440, 230, new Color(0, 0, 0, 170));
        Raylib.DrawRectangleLines(10, 10, 440, 230, OverlayCyan);
        Raylib.DrawText("OUT RAYMICRO // M0-M1 SEED", 22, 20, 18, OverlayCyan);
        Raylib.DrawText("WASD move  MOUSE look  LMB fire  F1 overlay", 22, 46, 14, OverlayText);
        Raylib.DrawText($"POS {camera.Position.X:0.00}, {camera.Position.Y:0.00}, {camera.Position.Z:0.00}", 22, 70, 14, Color.Yellow);
        Raylib.DrawText($"VEL {camera.HorizontalSpeed:0.00}  {(camera.Grounded ? "GROUND" : "AIR")}", 22, 90, 14, camera.Grounded ? ManaAccent : ArmorFull);
        Raylib.DrawText($"DOOR {(map.DoorOpen ? "OPEN" : "CLOSED")}", 22, 110, 14, map.DoorOpen ? Color.Green : Color.Orange);

        for (int i = 0; i < 7; i++)
        {
            string line = world.GetLogLineFromNewest(i);
            if (!string.IsNullOrWhiteSpace(line))
                Raylib.DrawText(line, 22, 138 + i * 14, 12, OverlayText);
        }
    }

    private static void DrawVitalsHud(OutmPlayerVitals vitals, int x, int y)
    {
        Raylib.DrawRectangle(x - 8, y - 8, 390, 98, HudBack);
        Raylib.DrawRectangleLines(x - 8, y - 8, 390, 98, HudFrame);
        Raylib.DrawText("VITALS", x, y - 2, 14, OverlayCyan);

        DrawIconRow("HP", "♥", vitals.Health, vitals.MaxHealth, 10, x, y + 22, HeartFull, HeartEmpty);
        DrawIconRow("AR", "▰", vitals.Armor, vitals.MaxArmor, 10, x, y + 46, ArmorFull, ArmorEmpty);
        DrawIconRow("MN", "◆", vitals.Mana, vitals.MaxMana, 10, x, y + 70, ManaFull, ManaEmpty, ManaAccent);
    }

    private static void DrawIconRow(string label, string glyph, int value, int max, int slots, int x, int y, Color full, Color empty, Color? accent = null)
    {
        max = Math.Max(1, max);
        value = Math.Clamp(value, 0, max);
        int filled = (int)MathF.Ceiling(value / (float)max * slots);

        Raylib.DrawText(label, x, y, 15, OverlayText);

        for (int i = 0; i < slots; i++)
        {
            Color color = i < filled ? full : empty;
            Raylib.DrawText(glyph, x + 38 + i * 24, y - 3, 23, color);
        }

        if (accent.HasValue)
            Raylib.DrawText("✦", x + 38 + slots * 24 + 6, y - 1, 18, accent.Value);

        Raylib.DrawText($"{value:000}", x + 318, y, 15, OverlayText);
    }
}
