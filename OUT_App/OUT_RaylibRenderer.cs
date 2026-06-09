using System;
using System.Collections.Generic;
using System.Linq;
using Raylib_cs;

namespace OUT_ASHBOUND;

public sealed class OUT_RaylibRenderer
{
    private const int Tile = 18;
    private const int FontSize = 18;
    private const int MapX = 20;
    private const int MapY = 56;
    private const int PanelX = 920;
    private const int PanelY = 56;
    private const int LogY = 560;

    private readonly Dictionary<string, Color> colors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Gray"] = Color.Gray,
        ["DarkGray"] = Color.DarkGray,
        ["Green"] = Color.Green,
        ["Blue"] = Color.Blue,
        ["Yellow"] = Color.Yellow,
        ["Red"] = Color.Red,
        ["DarkRed"] = Color.Maroon,
        ["Magenta"] = Color.Magenta,
        ["Cyan"] = Color.SkyBlue,
        ["White"] = Color.RayWhite,
        ["DarkYellow"] = Color.Gold
    };

    public void Render(OUT_State state)
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(12, 12, 14, 255));

        DrawHeader(state);
        DrawMap(state);
        DrawPanel(state);
        DrawLog(state);

        Raylib.EndDrawing();
    }

    private void DrawHeader(OUT_State state)
    {
        Raylib.DrawText("OUT_ASHBOUND: RETURN BY RUIN", 20, 16, 24, Color.Maroon);
        Raylib.DrawText("OUT CORE mini | Raylib backend | data-driven", 430, 22, 16, Color.Gray);
        Raylib.DrawText("WASD move  TAB world/local  E use  T talk  I inventory  F5/F9 save/load  ESC quit", 20, 40, 14, Color.DarkGray);
    }

    private void DrawMap(OUT_State state)
    {
        OUT_Map map = state.Mode == OUT_Mode.World ? state.WorldMap : state.LocalMap;
        OUT_Scope scope = state.Mode == OUT_Mode.World ? OUT_Scope.World : OUT_Scope.Local;

        Raylib.DrawRectangleLines(MapX - 8, MapY - 8, map.Width * Tile + 16, map.Height * Tile + 16, Color.DarkGray);

        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                var pos = new OUT_Pos(x, y);
                int px = MapX + x * Tile;
                int py = MapY + y * Tile;

                var obj = state.Table.All.FirstOrDefault(a => a.Scope == scope && a.Pos == pos);
                if (obj != null)
                {
                    DrawGlyph(obj.Def.Glyph, px, py, GetColor(obj.Def.Color));
                    continue;
                }

                var loc = scope == OUT_Scope.World ? state.Content.World.Locations.FirstOrDefault(l => l.Pos == pos) : null;
                if (loc != null)
                {
                    DrawGlyph(loc.Glyph, px, py, GetColor(loc.Color));
                    continue;
                }

                var tile = map.TileAt(pos);
                DrawGlyph(tile.Glyph, px, py, GetColor(tile.Color));
            }
        }
    }

    private void DrawPanel(OUT_State state)
    {
        var player = state.Player;
        if (player == null) return;

        Raylib.DrawRectangle(PanelX - 14, PanelY - 8, 330, 470, new Color(20, 20, 24, 230));
        Raylib.DrawRectangleLines(PanelX - 14, PanelY - 8, 330, 470, Color.DarkGray);

        string[] face =
        {
            "    .-''''-.    ",
            "  .'  .--.  '.  ",
            " /   /____\\   ",
            "|   |  oo  |   |",
            "|   |  --  |   |",
            "|    \\____/   |",
            "|    _||||_    |",
            "|___/ |||| \\__|"
        };

        int y = PanelY;
        foreach (string line in face)
        {
            Raylib.DrawText(line, PanelX, y, 18, Color.Maroon);
            y += 18;
        }

        y += 12;
        DrawPanelLine("HP", player.Stats.Hp + "/" + player.Stats.MaxHp, y); y += 22;
        DrawPanelLine("STAMINA", player.Stats.Stamina + "/" + player.Stats.MaxStamina, y); y += 22;
        DrawPanelLine("SHARDS", state.Shards + "/" + state.ShardsRequired, y); y += 22;
        DrawPanelLine("LOOPS", state.Loops.ToString(), y); y += 22;
        DrawPanelLine("MEMORY", state.Memory.ToString(), y); y += 22;
        DrawPanelLine("RESIDUE", state.Residue.ToString(), y); y += 22;
        DrawPanelLine("MODE", state.Mode.ToString(), y); y += 32;

        Raylib.DrawText("Legend", PanelX, y, 18, Color.Gold); y += 24;
        Raylib.DrawText("@ you   E/e NPC   w/b mob", PanelX, y, 14, Color.Gray); y += 18;
        Raylib.DrawText("* shard + item  R node  Ω gate", PanelX, y, 14, Color.Gray); y += 18;
        Raylib.DrawText(". plain  ♣ forest  ≈ water", PanelX, y, 14, Color.Gray); y += 18;
        Raylib.DrawText("▲ mountain  ░ ruin  · road", PanelX, y, 14, Color.Gray); y += 26;

        if (state.ShowInventory)
        {
            Raylib.DrawText("Inventory", PanelX, y, 18, Color.Gold); y += 24;
            foreach (var item in player.Bag.Take(8))
            {
                Raylib.DrawText(item.Key + " x" + item.Value, PanelX, y, 14, Color.RayWhite);
                y += 18;
            }
        }
    }

    private void DrawLog(OUT_State state)
    {
        Raylib.DrawRectangle(20, LogY, 1220, 138, new Color(18, 18, 20, 235));
        Raylib.DrawRectangleLines(20, LogY, 1220, 138, Color.DarkGray);
        Raylib.DrawText("Event Log", 32, LogY + 10, 18, Color.Gold);

        int y = LogY + 36;
        foreach (string line in state.LogLines.TakeLast(5))
        {
            Raylib.DrawText(line, 32, y, 16, Color.RayWhite);
            y += 20;
        }

        Raylib.DrawText("E=(C,S,H,A,I) | Story=Project(Update(World,Event,Genome))", 32, LogY + 116, 14, Color.DarkGray);
    }

    private void DrawPanelLine(string key, string value, int y)
    {
        Raylib.DrawText(key, PanelX, y, 16, Color.Gray);
        Raylib.DrawText(value, PanelX + 120, y, 16, Color.RayWhite);
    }

    private void DrawGlyph(string glyph, int x, int y, Color color)
    {
        Raylib.DrawText(glyph, x, y, FontSize, color);
    }

    private Color GetColor(string key)
    {
        return colors.TryGetValue(key, out var color) ? color : Color.Gray;
    }
}
