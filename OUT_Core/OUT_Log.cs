namespace OUT_ASHBOUND;

public static class OUT_Log
{
    public static void Add(OUT_State state, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (text.Length > 112) text = text[..112];
        state.LogLines.Add(text);
        while (state.LogLines.Count > 9) state.LogLines.RemoveAt(0);
    }
}
