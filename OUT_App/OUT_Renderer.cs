using System;
using System.Collections.Generic;
using System.Linq;

namespace OUT_ASHBOUND;

public sealed class OUT_Renderer
{
    private readonly Dictionary<string, ConsoleColor> colors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Gray"] = ConsoleColor.Gray,
        ["DarkGray"] = ConsoleColor.DarkGray,
        ["Green"] = ConsoleColor.Green,
        ["Blue"] = ConsoleColor.Blue,
        ["Yellow"] = ConsoleColor.Yellow,
        ["Red"] = ConsoleColor.Red,
        ["DarkRed"] = ConsoleColor.DarkRed,
        ["Magenta"] = ConsoleColor.Magenta,
        ["Cyan"] = ConsoleColor.Cyan,
        ["White"] = ConsoleColor.White,
        ["DarkYellow"] = ConsoleColor.DarkYellow
    };

    public void Render(OUT_State state)
    {
        Console.SetCursorPosition(0, 0);
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.WriteLine("OUT_ASHBOUND: Return by Ruin | OUT CORE mini data-driven engine");
        Console.ResetColor();

        OUT_Map map = state.Mode == OUT_Mode.World ? state.WorldMap : state.LocalMap;
        OUT_Scope scope = state.Mode == OUT_Mode.World ? OUT_Scope.World : OUT_Scope.Local;

        int rows = Math.Max(map.Height, 18);
        for (int y = 0; y < rows; y++)
        {
            if (y < map.Height)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var pos = new OUT_Pos(x, y);
                    var obj = state.Table.All.FirstOrDefault(a => a.Scope == scope && a.Pos == pos);
                    if (obj != null)
                    {
                        Color(obj.Def.Color);
                        Console.Write(obj.Def.Glyph);
                    }
                    else
                    {
                        var loc = scope == OUT_Scope.World ? state.Content.World.Locations.FirstOrDefault(l => l.Pos == pos) : null;
                        if (loc != null)
                        {
                            Color(loc.Color);
                            Console.Write(loc.Glyph);
                        }
                        else
                        {
                            var tile = map.TileAt(pos);
                            Color(tile.Color);
                            Console.Write(tile.Glyph);
                        }
                    }
                }
            }
            else Console.Write(new string(' ', map.Width));

            Console.ResetColor();
            Console.Write("   ");
            DrawSide(state, y);
            Console.WriteLine();
        }

        Console.WriteLine(new string('-', 116));
        foreach (string line in state.LogLines) Console.WriteLine(line.PadRight(116));
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("E=(C,S,H,A,I) | Story=Project(Update(World,Event,Genome)) | Q quit F5/F9 save/load TAB world/local");
        Console.ResetColor();
    }

    private void DrawSide(OUT_State state, int y)
    {
        var player = state.Player;
        if (player == null) return;

        string[] face =
        {
            "    .-''''-.    ",
            "  .'  .--.  '.  ",
            " /   / __ \\   ",
            "|   | /  \\ |  ",
            "|   | \\__/ |  ",
            "|    \\____/   ",
            "|   _/||||\\_ ",
            "|__/  ||||  \\" 
        };

        if (y < face.Length)
        {
            Color("DarkRed");
            Console.Write(face[y].PadRight(44));
            Console.ResetColor();
            return;
        }

        string text = y switch
        {
            9 => $"HP {player.Stats.Hp}/{player.Stats.MaxHp}",
            10 => $"ST {player.Stats.Stamina}/{player.Stats.MaxStamina}",
            11 => $"SHARDS {state.Shards}/{state.ShardsRequired}",
            12 => $"LOOPS {state.Loops}",
            13 => $"MEMORY {state.Memory}",
            14 => $"RESIDUE {state.Residue}",
            15 => $"MODE {state.Mode}",
            16 => "Legend @ you E/e NPC w/b mob * shard + item",
            17 => "Tiles . plain T forest ~ water ^ mountain # ruin",
            _ => ""
        };

        Console.Write(text.PadRight(50));

        if (state.ShowInventory && y > 18)
        {
            var item = player.Bag.Skip(y - 19).FirstOrDefault();
            if (!string.IsNullOrEmpty(item.Key)) Console.Write($" {item.Key} x{item.Value}");
        }
    }

    private void Color(string color)
    {
        Console.ForegroundColor = colors.TryGetValue(color, out var c) ? c : ConsoleColor.Gray;
    }
}
