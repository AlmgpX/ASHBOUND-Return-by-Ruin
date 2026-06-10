using System;

namespace OUT_ASHBOUND;

public static class OUT_WorldGenerator
{
    public static OUT_Map Generate(OUT_WorldContent content, int seed)
    {
        int width = content.Map.Length > 0 ? content.Map[0].Length : 48;
        int height = content.Map.Length > 0 ? content.Map.Length : 16;
        var rng = new Random(seed);
        var rows = new string[height];

        for (int y = 0; y < height; y++)
        {
            var line = new char[width];
            for (int x = 0; x < width; x++)
            {
                if (x == 0 || y == 0 || x == width - 1 || y == height - 1) line[x] = '^';
                else line[x] = '.';
            }
            rows[y] = new string(line);
        }

        // Forest blobs.
        for (int blob = 0; blob < 10; blob++)
        {
            int cx = rng.Next(3, width - 3);
            int cy = rng.Next(2, height - 2);
            int radius = rng.Next(2, 5);
            PaintBlob(rows, cx, cy, radius, 'T', rng, 0.72);
        }

        // Ruin clusters.
        for (int blob = 0; blob < 4; blob++)
        {
            int cx = rng.Next(5, width - 5);
            int cy = rng.Next(3, height - 3);
            PaintRect(rows, cx - 2, cy - 1, 5, 3, '#');
        }

        // River.
        int riverY = height / 2 + rng.Next(-2, 3);
        for (int x = 1; x < width - 1; x++)
        {
            riverY += rng.Next(-1, 2);
            riverY = Math.Clamp(riverY, 2, height - 3);
            Set(rows, x, riverY, '~');
            if (rng.NextDouble() < 0.35) Set(rows, x, riverY + 1, '~');
        }

        // Road from start toward gate.
        OUT_Pos start = content.Start;
        OUT_Pos gate = content.Locations.Find(l => l.Kind == "gate")?.Pos ?? new OUT_Pos(width - 5, height / 2);
        int yRoad = start.Y;
        for (int x = Math.Min(start.X, gate.X); x <= Math.Max(start.X, gate.X); x++) Set(rows, x, yRoad, '=');
        int xRoad = gate.X;
        for (int y = Math.Min(start.Y, gate.Y); y <= Math.Max(start.Y, gate.Y); y++) Set(rows, xRoad, y, '=');

        // Restore required locations as walkable/visible cells underneath icons.
        foreach (var loc in content.Locations)
        {
            if (loc.Kind == "gate") Set(rows, loc.Pos.X, loc.Pos.Y, '=');
            else if (loc.Kind == "ruin_node") Set(rows, loc.Pos.X, loc.Pos.Y, '#');
            else Set(rows, loc.Pos.X, loc.Pos.Y, '.');
        }

        return OUT_Map.FromRows(rows, content.Tiles);
    }

    private static void PaintBlob(string[] rows, int cx, int cy, int radius, char ch, Random rng, double chance)
    {
        for (int y = cy - radius; y <= cy + radius; y++)
        for (int x = cx - radius; x <= cx + radius; x++)
        {
            int dx = x - cx;
            int dy = y - cy;
            if (dx * dx + dy * dy <= radius * radius && rng.NextDouble() < chance) Set(rows, x, y, ch);
        }
    }

    private static void PaintRect(string[] rows, int x0, int y0, int w, int h, char ch)
    {
        for (int y = y0; y < y0 + h; y++)
        for (int x = x0; x < x0 + w; x++)
            Set(rows, x, y, ch);
    }

    private static void Set(string[] rows, int x, int y, char ch)
    {
        if (y < 0 || y >= rows.Length) return;
        if (x < 0 || x >= rows[y].Length) return;
        var line = rows[y].ToCharArray();
        line[x] = ch;
        rows[y] = new string(line);
    }
}
