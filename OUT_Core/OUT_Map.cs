using System;
using System.Collections.Generic;
using System.Linq;

namespace OUT_ASHBOUND;

public sealed class OUT_Map
{
    private readonly string[] rows;
    private readonly Dictionary<char, OUT_TileDef> tiles;

    public int Width => rows.Length == 0 ? 0 : rows[0].Length;
    public int Height => rows.Length;

    private OUT_Map(string[] rows, List<OUT_TileDef> tiles)
    {
        this.rows = rows;
        // Guard against duplicate tile character definitions (e.g. 'X' already present in base tiles
        // and added again for local maps). When duplicates exist, prefer the first occurrence.
        this.tiles = tiles
            .Where(t => !string.IsNullOrEmpty(t.Ch))
            .GroupBy(t => t.Ch[0])
            .ToDictionary(g => g.Key, g => g.First());
    }

    public static OUT_Map FromRows(string[] rows, List<OUT_TileDef> tiles) => new(rows, tiles);

    public static OUT_Map GenerateLocal(List<OUT_TileDef> baseTiles, int seed)
    {
        var rng = new Random(seed);
        var rows = new string[16];
        for (int y = 0; y < rows.Length; y++)
        {
            var line = new char[32];
            for (int x = 0; x < line.Length; x++)
            {
                if (x == 0 || y == 0 || x == line.Length - 1 || y == rows.Length - 1) line[x] = '#';
                else line[x] = rng.NextDouble() < 0.12 ? 'T' : '.';
            }
            rows[y] = new string(line);
        }

        var start = rows[8].ToCharArray();
        start[16] = '.';
        rows[8] = new string(start);

        var exit = rows[14].ToCharArray();
        exit[16] = 'X';
        rows[14] = new string(exit);

        var tiles = baseTiles.Concat(new[]
        {
            new OUT_TileDef { Key = "exit", Name = "Exit", Ch = "X", Glyph = "X", Color = "Yellow", Cost = 1, Walkable = true }
        }).ToList();

        return new OUT_Map(rows, tiles);
    }

    public bool InBounds(OUT_Pos p) => p.X >= 0 && p.Y >= 0 && p.Y < rows.Length && p.X < rows[p.Y].Length;

    public char CharAt(OUT_Pos p) => InBounds(p) ? rows[p.Y][p.X] : ' ';

    public OUT_TileDef TileAt(OUT_Pos p)
    {
        if (!InBounds(p)) return tiles['#'];
        char c = rows[p.Y][p.X];
        return tiles.TryGetValue(c, out var tile) ? tile : tiles['.'];
    }
}
