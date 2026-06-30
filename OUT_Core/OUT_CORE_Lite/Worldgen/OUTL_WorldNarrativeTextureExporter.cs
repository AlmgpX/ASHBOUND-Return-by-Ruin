using System.IO;
using UnityEngine;

#if UNITY_EDITOR
public static class OUTL_WorldNarrativeTextureExporter
{
    public static string ExportAll(OUTL_WorldNarrativeConfig config, OUTL_WorldNarrativeResult result, string baseFolder)
    {
        if (config == null || result == null || !config.ExportLayerTextures) return string.Empty;
        int width = Mathf.Max(1, config.TextureWidth);
        int height = Mathf.Max(1, config.TextureHeight);
        string folder = Path.Combine(baseFolder, config.TextureFolder);
        Directory.CreateDirectory(folder);
        string prefix = string.IsNullOrEmpty(config.TexturePrefix) ? "world" : config.TexturePrefix;

        if (config.ExportHeightTexture) Save(folder, prefix + "_height.png", CreateScalar(result, width, height, t => t.Height));
        if (config.ExportMoistureTexture) Save(folder, prefix + "_moisture.png", CreateScalar(result, width, height, t => t.Moisture));
        if (config.ExportHeatTexture) Save(folder, prefix + "_heat.png", CreateScalar(result, width, height, t => t.Heat));
        if (config.ExportDrainageTexture) Save(folder, prefix + "_drainage.png", CreateScalar(result, width, height, t => t.Drainage));
        if (config.ExportDangerTexture) Save(folder, prefix + "_danger.png", CreateScalar(result, width, height, t => Mathf.Clamp01((t.Danger + 4f) / 16f)));
        if (config.ExportProsperityTexture) Save(folder, prefix + "_prosperity.png", CreateScalar(result, width, height, t => Mathf.Clamp01((t.Prosperity + 6f) / 18f)));
        if (config.ExportSanctityTexture) Save(folder, prefix + "_sanctity.png", CreateScalar(result, width, height, t => Mathf.Clamp01(t.Sanctity / 12f)));
        if (config.ExportVisibilityTexture) Save(folder, prefix + "_visibility.png", CreateScalar(result, width, height, t => OUTL_WorldArchetypeUtility.GetVisibility(config, t)));
        if (config.ExportZoneTexture) Save(folder, prefix + "_zones.png", CreateColor(result, width, height, t => ZoneColor(t.Zone)));
        if (config.ExportResourceTexture) Save(folder, prefix + "_resources.png", CreateScalar(result, width, height, t => Mathf.Clamp01(t.Resources != null ? t.Resources.Count / 6f : 0f)));
        if (config.ExportPlantTexture) Save(folder, prefix + "_plants.png", CreateScalar(result, width, height, t => Mathf.Clamp01(t.Plants != null ? t.Plants.Count / 6f : 0f)));
        if (config.ExportArchetypeTexture) Save(folder, prefix + "_archetypes.png", CreateColor(result, width, height, t => OUTL_WorldArchetypeUtility.ArchetypeColor(OUTL_WorldArchetypeUtility.GetDominantArchetype(config, t))));

        return folder;
    }

    private delegate float TileScalar(OUTL_WorldTile tile);
    private delegate Color32 TileColor32(OUTL_WorldTile tile);

    private static Texture2D CreateScalar(OUTL_WorldNarrativeResult result, int width, int height, TileScalar scalar)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[width * height];
        int index = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                OUTL_WorldTile tile = Sample(result, x, y, width, height);
                byte v = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(scalar(tile)) * 255f), 0, 255);
                pixels[index++] = new Color32(v, v, v, 255);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, false);
        return tex;
    }

    private static Texture2D CreateColor(OUTL_WorldNarrativeResult result, int width, int height, TileColor32 color)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[width * height];
        int index = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                OUTL_WorldTile tile = Sample(result, x, y, width, height);
                pixels[index++] = color(tile);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, false);
        return tex;
    }

    private static OUTL_WorldTile Sample(OUTL_WorldNarrativeResult result, int px, int py, int texW, int texH)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt(px / Mathf.Max(1f, texW - 1f) * (result.Width - 1)), 0, result.Width - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(py / Mathf.Max(1f, texH - 1f) * (result.Height - 1)), 0, result.Height - 1);
        return result.Tiles[x, y];
    }

    private static void Save(string folder, string filename, Texture2D texture)
    {
        if (texture == null) return;
        File.WriteAllBytes(Path.Combine(folder, filename), texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }

    private static Color32 ZoneColor(OUTL_WorldZoneType zone)
    {
        switch (zone)
        {
            case OUTL_WorldZoneType.Ocean: return new Color32(20, 55, 130, 255);
            case OUTL_WorldZoneType.Coast: return new Color32(210, 190, 120, 255);
            case OUTL_WorldZoneType.Plains: return new Color32(105, 170, 80, 255);
            case OUTL_WorldZoneType.Forest: return new Color32(35, 105, 45, 255);
            case OUTL_WorldZoneType.Hills: return new Color32(125, 120, 75, 255);
            case OUTL_WorldZoneType.Mountains: return new Color32(145, 145, 145, 255);
            case OUTL_WorldZoneType.Swamp: return new Color32(45, 80, 60, 255);
            case OUTL_WorldZoneType.Desert: return new Color32(215, 180, 90, 255);
            case OUTL_WorldZoneType.Wasteland: return new Color32(95, 75, 70, 255);
            case OUTL_WorldZoneType.Sacred: return new Color32(230, 230, 255, 255);
            case OUTL_WorldZoneType.Ruins: return new Color32(85, 80, 95, 255);
            case OUTL_WorldZoneType.River: return new Color32(45, 120, 220, 255);
            case OUTL_WorldZoneType.Lake: return new Color32(45, 95, 190, 255);
            case OUTL_WorldZoneType.Tundra: return new Color32(190, 210, 210, 255);
            case OUTL_WorldZoneType.Steppe: return new Color32(160, 155, 75, 255);
        }
        return new Color32(255, 0, 255, 255);
    }
}
#endif
