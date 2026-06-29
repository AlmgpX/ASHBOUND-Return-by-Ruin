using System.Numerics;
using Raylib_cs;

namespace OUT_RayMicro.Runtime;

public static class OutmFontSystem
{
    public const string HudFontRelativePath = "fonts/hud_unicode.ttf";
    public static bool IsLoaded { get; private set; }
    public static string LoadedFontPath { get; private set; } = "";

    private static Font hudFont;
    private static readonly int[] hudCodepoints = BuildHudCodepoints();

    public static void Load()
    {
        string? path = FindHudFontPath();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            IsLoaded = false;
            LoadedFontPath = "";
            return;
        }

        hudFont = Raylib.LoadFontEx(path, 32, hudCodepoints, hudCodepoints.Length);
        IsLoaded = hudFont.Texture.Id != 0;
        LoadedFontPath = IsLoaded ? path : "";
    }

    public static void Unload()
    {
        if (!IsLoaded)
            return;

        Raylib.UnloadFont(hudFont);
        hudFont = default;
        IsLoaded = false;
        LoadedFontPath = "";
    }

    public static void DrawText(string text, int x, int y, int fontSize, Color color)
    {
        if (IsLoaded)
            Raylib.DrawTextEx(hudFont, text, new Vector2(x, y), fontSize, 1.0f, color);
        else
            Raylib.DrawText(text, x, y, fontSize, color);
    }

    private static string? FindHudFontPath()
    {
        string canonical = OutmAssetPaths.ResolveData(HudFontRelativePath);
        if (File.Exists(canonical))
            return canonical;

        string fontDir = OutmAssetPaths.ResolveData("fonts");
        if (!Directory.Exists(fontDir))
            return null;

        string[] preferredNames =
        {
            "hud_unicode.ttf",
            "unifont.ttf",
            "Unifont.ttf",
            "unifont.otf",
            "NotoSans-Regular.ttf",
            "NotoSansSymbols-Regular.ttf",
            "NotoSansSymbols2-Regular.ttf"
        };

        for (int i = 0; i < preferredNames.Length; i++)
        {
            string preferred = Path.Combine(fontDir, preferredNames[i]);
            if (File.Exists(preferred))
                return preferred;
        }

        string[] fonts = Directory.GetFiles(fontDir, "*.ttf", SearchOption.TopDirectoryOnly)
            .Concat(Directory.GetFiles(fontDir, "*.otf", SearchOption.TopDirectoryOnly))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return fonts.Length > 0 ? fonts[0] : null;
    }

    private static int[] BuildHudCodepoints()
    {
        var points = new List<int>(512);

        AddRange(points, 0x0020, 0x007E); // Basic Latin and punctuation.
        AddRange(points, 0x0400, 0x04FF); // Cyrillic.

        points.Add(0x00A0);
        points.Add(0x2665); // ♥
        points.Add(0x2764); // ❤
        points.Add(0x25B0); // ▰
        points.Add(0x25A0); // ■
        points.Add(0x25A1); // □
        points.Add(0x25C6); // ◆
        points.Add(0x25C7); // ◇
        points.Add(0x2726); // ✦
        points.Add(0x2022); // •
        points.Add(0x2190); // ←
        points.Add(0x2191); // ↑
        points.Add(0x2192); // →
        points.Add(0x2193); // ↓

        return points.Distinct().OrderBy(x => x).ToArray();
    }

    private static void AddRange(List<int> list, int first, int last)
    {
        for (int i = first; i <= last; i++)
            list.Add(i);
    }
}
