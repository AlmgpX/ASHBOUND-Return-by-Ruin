using System.IO;
using System.Text;
using UnityEngine;

public sealed class OUTL_WorldNarrativeWriteResult
{
    public OUTL_WorldNarrativeResult World;
    public string ReportPath;
    public string TextureFolder;
}

public static class OUTL_WorldNarrativeWriter
{
    public static OUTL_WorldNarrativeWriteResult GenerateWriteAndExport(OUTL_WorldNarrativeConfig config)
    {
        if (config == null) return null;

        OUTL_WorldNarrativeResult world = OUTL_WorldNarrativeGenerator.Generate(config);
        OUTL_WorldNarrativeLayerApplier.ApplyArchetypeAndVisibilityLayers(config, world);

        string report = OUTL_WorldNarrativeGenerator.BuildReport(config, world);
        report += BuildArchetypeAndVisibilityAppendix(config, world);

        string baseFolder = Path.Combine(Application.persistentDataPath, config.OutputFolder);
        Directory.CreateDirectory(baseFolder);

        string safeName = string.IsNullOrEmpty(world.WorldName) ? "World" : world.WorldName.Replace('/', '_').Replace('\\', '_');
        string reportPath = Path.Combine(baseFolder, safeName + "_seed_" + config.Seed + ".md");
        File.WriteAllText(reportPath, report, Encoding.UTF8);

        string textureFolder = string.Empty;
        if (config.ExportLayerTextures)
        {
#if UNITY_EDITOR
            textureFolder = OUTL_WorldNarrativeTextureExporter.ExportAll(config, world, baseFolder);
#else
            Debug.LogWarning("OUTL world narrative texture export is editor-only; runtime writer skipped layer textures.");
#endif
        }

        return new OUTL_WorldNarrativeWriteResult { World = world, ReportPath = reportPath, TextureFolder = textureFolder };
    }

    private static string BuildArchetypeAndVisibilityAppendix(OUTL_WorldNarrativeConfig config, OUTL_WorldNarrativeResult result)
    {
        StringBuilder sb = new StringBuilder(8192);
        sb.AppendLine();
        sb.AppendLine("## Архетипический слой");
        sb.AppendLine("Архетип здесь не 'магическая личность тайла', а полиморфная сила интерпретации: она влияет на смысл зоны, видимость, склонность к событиям и будущий выбор контента. Юнг бы, вероятно, вздохнул, но хотя бы не увидел очередной enum без души.");
        sb.AppendLine();

        int[] counts = new int[32];
        float visibilitySum = 0f;
        int total = Mathf.Max(1, result.Width * result.Height);

        for (int y = 0; y < result.Height; y++)
        {
            for (int x = 0; x < result.Width; x++)
            {
                OUTL_WorldTile tile = result.Tiles[x, y];
                OUTL_WorldArchetypeType archetype = OUTL_WorldArchetypeUtility.GetDominantArchetype(config, tile);
                int idx = Mathf.Clamp((int)archetype, 0, counts.Length - 1);
                counts[idx]++;
                visibilitySum += OUTL_WorldArchetypeUtility.GetVisibility(config, tile);
            }
        }

        sb.AppendLine("Средняя видимость мира: " + (visibilitySum / total).ToString("0.00"));
        sb.AppendLine();
        sb.AppendLine("### Распределение архетипов");
        for (int i = 0; i < counts.Length; i++)
        {
            if (counts[i] <= 0) continue;
            OUTL_WorldArchetypeType type = (OUTL_WorldArchetypeType)i;
            sb.AppendLine("- " + OUTL_WorldArchetypeUtility.ArchetypeNameRu(type) + ": " + counts[i]);
        }
        sb.AppendLine();
        sb.AppendLine("### Самые видимые области");
        AppendTopVisibility(sb, config, result, true);
        sb.AppendLine("### Самые скрытые области");
        AppendTopVisibility(sb, config, result, false);
        return sb.ToString();
    }

    private static void AppendTopVisibility(StringBuilder sb, OUTL_WorldNarrativeConfig config, OUTL_WorldNarrativeResult result, bool high)
    {
        OUTL_WorldTile[] best = new OUTL_WorldTile[10];
        float[] values = new float[10];
        for (int i = 0; i < values.Length; i++) values[i] = high ? float.MinValue : float.MaxValue;

        for (int y = 0; y < result.Height; y++)
        {
            for (int x = 0; x < result.Width; x++)
            {
                OUTL_WorldTile tile = result.Tiles[x, y];
                float v = OUTL_WorldArchetypeUtility.GetVisibility(config, tile);
                for (int i = 0; i < values.Length; i++)
                {
                    bool better = high ? v > values[i] : v < values[i];
                    if (!better) continue;
                    for (int j = values.Length - 1; j > i; j--)
                    {
                        values[j] = values[j - 1];
                        best[j] = best[j - 1];
                    }
                    values[i] = v;
                    best[i] = tile;
                    break;
                }
            }
        }

        for (int i = 0; i < values.Length; i++)
        {
            OUTL_WorldTile t = best[i];
            OUTL_WorldArchetypeType a = OUTL_WorldArchetypeUtility.GetDominantArchetype(config, t);
            sb.AppendLine("- " + t.X + "," + t.Y + " видимость=" + values[i].ToString("0.00") + " архетип=" + OUTL_WorldArchetypeUtility.ArchetypeNameRu(a) + " зона=" + t.Zone + " danger=" + t.Danger + " prosperity=" + t.Prosperity + " sanctity=" + t.Sanctity);
        }
        sb.AppendLine();
    }
}
