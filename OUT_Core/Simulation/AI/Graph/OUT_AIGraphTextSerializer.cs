using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public static class OUT_AIGraphTextSerializer
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static void SaveRuntimeGraph(OUT_AIGraph graph, string filePath)
    {
        if (graph == null)
        {
            Debug.LogError("OUT_AIGraphTextSerializer: graph is null.");
            return;
        }

        string directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        StringBuilder sb = new StringBuilder(1024 * 32);
        OUT_AIGraph.OUT_RuntimeNode[] nodes = graph.Nodes;
        int count = nodes != null ? nodes.Length : 0;

        sb.AppendLine("# OUT AI Graph Text Bake v1");
        sb.AppendLine("# Human-readable. Decimal separator is dot. Do not localize this file, because machines are already confused enough.");
        sb.Append("node_count=").Append(count).AppendLine();
        sb.AppendLine();

        for (int i = 0; i < count; i++)
        {
            OUT_AIGraph.OUT_RuntimeNode node = nodes[i];
            if (node == null)
                continue;

            sb.Append("[node ").Append(i).AppendLine("]");
            sb.Append("name=").AppendLine(Escape(node.Name));
            sb.Append("enabled=").Append(node.Enabled ? 1 : 0).AppendLine();
            sb.Append("cover_hint=").Append(node.IsCoverHint ? 1 : 0).AppendLine();
            AppendVector(sb, "position", node.Position);

            sb.Append("links=");
            if (node.Links != null)
            {
                for (int j = 0; j < node.Links.Length; j++)
                {
                    if (j > 0)
                        sb.Append(',');
                    sb.Append(node.Links[j]);
                }
            }
            sb.AppendLine();

            AppendSensory(sb, node.Sensory);
            sb.AppendLine();
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
        Debug.Log($"OUT_AIGraphTextSerializer: saved graph text data to {filePath}");
    }

    private static void AppendSensory(StringBuilder sb, OUT_SceneSensorySample sample)
    {
        sb.Append("has_ground=").Append(sample.HasGround ? 1 : 0).AppendLine();
        AppendFloat(sb, "ground_height", sample.GroundHeight);
        AppendFloat(sb, "luminance", sample.Luminance);
        AppendFloat(sb, "sky_luminance", sample.SkyLuminance);
        AppendFloat(sb, "ground_luminance", sample.GroundLuminance);
        AppendFloat(sb, "occlusion", sample.Occlusion);
        AppendFloat(sb, "cover", sample.Cover);
        AppendFloat(sb, "ground_safety", sample.GroundSafety);
        AppendFloat(sb, "area_cost", sample.AreaCost);
        AppendFloat(sb, "noise", sample.Noise);
        AppendFloat(sb, "danger", sample.Danger);
        AppendFloat(sb, "food", sample.Food);
        AppendFloat(sb, "fire", sample.Fire);
    }

    private static void AppendVector(StringBuilder sb, string name, Vector3 value)
    {
        sb.Append(name).Append('=')
            .Append(value.x.ToString("0.######", Invariant)).Append(',')
            .Append(value.y.ToString("0.######", Invariant)).Append(',')
            .Append(value.z.ToString("0.######", Invariant)).AppendLine();
    }

    private static void AppendFloat(StringBuilder sb, string name, float value)
    {
        sb.Append(name).Append('=').Append(value.ToString("0.######", Invariant)).AppendLine();
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r");
    }
}
