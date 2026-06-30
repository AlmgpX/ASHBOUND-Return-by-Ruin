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

    public void Draw(OutmWorld world, OutmCameraMotor camera, OutmDemoMap map, OutmChunkStore chunks, OutmMapRuntimeStores mapRuntime)
    {
        int w = Raylib.GetScreenWidth();
        int h = Raylib.GetScreenHeight();

        Text("+", w / 2 - 4, h / 2 - 8, 20, Color.White);
        DrawVitalsHud(world.PlayerVitals, 18, h - 116);

        if (!Visible)
            return;

        Raylib.DrawRectangle(10, 10, 650, 376, new Color(0, 0, 0, 170));
        Raylib.DrawRectangleLines(10, 10, 650, 376, OverlayCyan);
        Text("OUT CORE // MAP ENTITY SLICE", 22, 20, 18, OverlayCyan);
        Text("WASD move  SPACE jump  CTRL/C crouch", 22, 46, 14, OverlayText);
        Text("E use  LMB fire  F1 overlay  F2 damage  F3 armor", 22, 64, 14, OverlayText);
        Text("F5 quicksave  F9 quickload", 22, 82, 14, OverlayText);
        Text(OutmFontSystem.IsLoaded ? "FONT hud_unicode.ttf OK" : "FONT MISSING: data/fonts/hud_unicode.ttf", 22, 102, 14, OutmFontSystem.IsLoaded ? ManaAccent : Color.Orange);
        Text($"MAP {map.DisplayName}  boxes {map.Boxes.Count}  doors {map.Doors.Count}  triggers {map.Triggers.Count}", 22, 126, 14, OverlayText);
        Text($"STORES static {mapRuntime.StaticWorldEntities}  doors {mapRuntime.DoorEntities}  triggers {mapRuntime.TriggerEntities}  pickups {mapRuntime.PickupEntities}", 22, 148, 14, OverlayText);
        Text($"CHUNK {chunks.FocusChunk}  active {chunks.ActiveCount}  resident {chunks.ResidentCount}  sleeping {chunks.SleepingCount}  known {chunks.KnownCount}", 22, 170, 14, ManaAccent);
        Text($"FOCUS ENTITIES {mapRuntime.Chunks.CountInChunk(chunks.FocusChunk)}", 22, 192, 14, ManaAccent);
        Text($"POS {camera.Position.X:0.00}, {camera.Position.Y:0.00}, {camera.Position.Z:0.00}", 22, 214, 14, Color.Yellow);
        Text($"VEL {camera.HorizontalSpeed:0.00}  {(camera.Grounded ? "GROUND" : "AIR")}  {(camera.IsCrouching ? "CROUCH" : "STAND")}", 22, 234, 14, camera.Grounded ? ManaAccent : ArmorColor(world.PlayerVitals.ArmorTier));
        Text(DoorStatus(map), 22, 254, 14, DoorStatusColor(map));

        for (int i = 0; i < 8; i++)
        {
            string line = world.GetLogLineFromNewest(i);
            if (!string.IsNullOrWhiteSpace(line))
                Text(line, 22, 282 + i * 14, 12, OverlayText);
        }
    }

    private static string DoorStatus(OutmDemoMap map)
    {
        if (map.Doors.Count == 0)
            return "DOORS none";

        OutmDoorRuntime door = map.Doors[0];
        return $"USE/E DOOR {door.Id} {(door.Open ? "OPEN" : "CLOSED")}";
    }

    private static Color DoorStatusColor(OutmDemoMap map)
    {
        if (map.Doors.Count == 0)
            return OverlayText;

        return map.Doors[0].Open ? Color.Green : Color.Orange;
    }

    private static void DrawVitalsHud(OutmPlayerVitals vitals, int x, int y)
    {
        Raylib.DrawRectangle(x - 8, y - 8, 418, 98, HudBack);
        Raylib.DrawRectangleLines(x - 8, y - 8, 418, 98, HudFrame);
        Text("VITALS", x, y - 2, 14, OverlayCyan);

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

        Text(label, x, y, 15, OverlayText);

        for (int i = 0; i < slots; i++)
        {
            Color color = i < filled ? full : empty;
            Text(glyph, x + 38 + i * 24, y - 3, 23, color);
        }

        if (accent.HasValue)
            Text("✦", x + 38 + slots * 24 + 6, y - 1, 18, accent.Value);

        Text($"{value:000}", x + 318, y, 15, OverlayText);
    }

    private static void Text(string text, int x, int y, int fontSize, Color color)
    {
        OutmFontSystem.DrawText(text, x, y, fontSize, color);
    }
}
