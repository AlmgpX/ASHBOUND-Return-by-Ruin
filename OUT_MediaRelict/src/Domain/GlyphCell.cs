namespace MediaRelic.Domain;

public readonly record struct GlyphCell(
    char Rune,
    byte R,
    byte G,
    byte B,
    byte BgR = 3,
    byte BgG = 5,
    byte BgB = 8);

public sealed class GlyphFrame
{
    public int Width { get; }
    public int Height { get; }
    public GlyphCell[] Cells { get; }

    public GlyphFrame(int width, int height, GlyphCell[] cells)
    {
        Width = width;
        Height = height;
        Cells = cells;
    }

    public GlyphCell At(int x, int y)
    {
        return Cells[y * Width + x];
    }

    public static GlyphFrame Empty(int width, int height)
    {
        var cells = new GlyphCell[width * height];

        for (var i = 0; i < cells.Length; i++)
            cells[i] = new GlyphCell(' ', 120, 120, 120, 3, 5, 8);

        return new GlyphFrame(width, height, cells);
    }
}
