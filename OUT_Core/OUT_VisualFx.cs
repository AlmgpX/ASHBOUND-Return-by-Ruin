namespace OUT_ASHBOUND;

public sealed class OUT_VisualFx
{
    public OUT_Scope Scope { get; set; }
    public OUT_Pos Pos { get; set; }
    public string Glyph { get; set; } = "*";
    public string Color { get; set; } = "White";
    public int TtlFrames { get; set; } = 20;
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }

    public OUT_VisualFx() { }

    public OUT_VisualFx(OUT_Scope scope, OUT_Pos pos, string glyph, string color, int ttlFrames)
    {
        Scope = scope;
        Pos = pos;
        Glyph = glyph;
        Color = color;
        TtlFrames = ttlFrames;
    }
}

public static class OUT_Fx
{
    public static void Add(OUT_State state, OUT_Scope scope, OUT_Pos pos, string glyph, string color, int ttlFrames = 24)
    {
        state.VisualFx.Add(new OUT_VisualFx(scope, pos, glyph, color, ttlFrames));
    }

    public static void Blood(OUT_State state, OUT_Scope scope, OUT_Pos pos)
    {
        Add(state, scope, pos, "*", "Red", 32);
        Add(state, scope, pos, "%", "DarkRed", 40);
        Add(state, scope, pos, "'", "Red", 26);
    }

    public static void Guts(OUT_State state, OUT_Scope scope, OUT_Pos pos)
    {
        Add(state, scope, pos, "&", "DarkRed", 60);
        Add(state, scope, pos, "~", "Red", 48);
    }

    public static void Potion(OUT_State state, OUT_Scope scope, OUT_Pos pos)
    {
        Add(state, scope, pos, "+", "Green", 35);
        Add(state, scope, pos, "^", "Cyan", 25);
    }

    public static void Shot(OUT_State state, OUT_Scope scope, OUT_Pos pos)
    {
        Add(state, scope, pos, "-", "Yellow", 10);
    }

    public static void Tick(OUT_State state)
    {
        for (int i = state.VisualFx.Count - 1; i >= 0; i--)
        {
            state.VisualFx[i].TtlFrames--;
            if (state.VisualFx[i].TtlFrames <= 0) state.VisualFx.RemoveAt(i);
        }
    }
}
