using Raylib_cs;
using OUT_RayMicro.Core;
using OUT_RayMicro.Gameplay;
using OUT_RayMicro.Input;
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
    private static readonly Color ArmorEmpty = new(72, 68, 48, 220);
    private static readonly Color ManaFull = new(60, 92, 255, 255);
    private static readonly Color ManaEmpty = new(28, 38, 92, 220);
    private static readonly Color ManaAccent = new(80, 255, 160, 255);

    public void Update(OutmWorld world, in OutmInputFrame input)
    {
        if (input.IsPressed(OutmButtons.Overlay))
        {
            Visible = !Visible;
            world.PushLog(Visible ? "editor overlay on" : "editor overlay off");
        }

        if (input.IsPressed(OutmButtons.DebugDamage))
            OutmDamageSystem.ApplyQuakeDamage(world, 25, "debug hit");

        if (input.IsPressed(OutmButtons.DebugArmor))
        {
            OutmArmorTier tier = OutmDamageSystem.NextDebugArmorTier(world.PlayerVitals.ArmorTier);
            OutmDamageSystem.TryPickupQuakeArmor(world, tier, "debug armor pickup");
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

        Raylib.DrawRectangle(10, 10, 480, 266, new Color(0, 0, 0, 170));
        Raylib.DrawRectangleLines(10, 10, 480, 266, OverlayCyan);
        Raylib.DrawText("OUT RAYMICRO // M0-M1 SEED", 22, 20, 18, OverlayCyan);
        Raylib.DrawText("WASD move  SPACE jump  CTRL/C crouch", 22, 46, 14, OverlayText);
        Raylib.DrawText("LMB fire  F1 overlay  F2 damage  F3 armor", 22, 64, 14, OverlayText);
        Raylib.DrawText($"POS {camera.Position.X:0.00}, {camera.Position.Y:0.00}, {camera.Position.Z:0.00}", 22, 90, 14, Color.Yellow);
        Raylib.DrawText($"VEL {camera.HorizontalSpeed:0.00}  {(camera.Grounded ? "GROUND" : "AIR")}  {(camera.IsCrouching ? "CROUCH" : "STAND")}", 22, 110, 14, camera.Grounded ? ManaAccent : ArmorColor(world.PlayerVitals.ArmorTier));
        Raylib.DrawText($"DOOR {(map.DoorOpen ? "OPEN" : "CLOSED")}", 22, 130, 14, map.DoorOpen ? Color.Green : Color.Orange);

        for (int i = 0; i < 8; i++)
        {
            string line = world.GetLogLineFromNewest(i);
            if (!string.IsNullOrWhiteSpace(line))
                Raylib.DrawText(line, 22, 158 + i * 14, 12, OverlayText);
        }
    }

    private static void DrawVitalsHud(OutmPlayerVitals vitals, int x, int y)
    {
        Raylib.DrawRectangle(x - 8, y - 8, 418, 98, HudBack);
        Raylib.DrawRectangleLines(x - 8, y - 8, 418, 98, HudFrame);
        Raylib.DrawText("VITALS", x, y - 2, 14, OverlayCyan);

        DrawIconRow("HP", "♥", vitals.Health, vitals.MaxHealth, 10, x, y + 22, HeartFull, HeartEmpty);
        DrawIconRow(OutmArmorRules.Code(vitals.ArmorTier), "▰", vitals.Armor, Math.Max(1, vitals.MaxArmor), 10, x, y + 46, ArmorColor(vitals.ArmorTier), ArmorEmpty);
        DrawIconRow("MN", "◆", vitals.Mana, vitals.MaxMana, 10, x, y + 70, ManaFull, ManaEmpty, ManaAccent);
    }

    private static Color ArmorColor(OutmArmorTier tier)
    {
        return tier switch
        {
            OutmArmorTier.Green => new Color(80, 215, 92, 255),
            OutmArmorTier.Yellow => new Color(235, 206, 92, 255),
            OutmArmorTier.Red => new Color(235, 72, 58, 255),
            _ => new Color(92, 100, 112, 230)
        };
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
