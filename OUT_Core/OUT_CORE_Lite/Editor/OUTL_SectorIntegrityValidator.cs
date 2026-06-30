#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public sealed class OUTL_SectorIntegrityValidator : EditorWindow
{
    private readonly List<string> messages = new List<string>(256);
    private OUTL_SectorIntegrityStats stats;
    private string report = "Not validated yet.";
    private Vector2 scroll;

    // [MenuItem("OUT CORE Lite/Workbench/Sector Integrity Validator")]
    public static void Open()
    {
        GetWindow<OUTL_SectorIntegrityValidator>("OUTL Sector Integrity");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("OUT CORE Lite Sector Integrity Validator", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Validate")) Validate(false);
        if (GUILayout.Button("Rebuild Sector Index Safe")) Validate(true);
        if (GUILayout.Button("Copy Report")) EditorGUIUtility.systemCopyBuffer = report;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("registry", stats.RegistryEntityCount.ToString());
        EditorGUILayout.LabelField("sector indexed", stats.SectorEntityCount.ToString());
        EditorGUILayout.LabelField("cells", stats.CellCount.ToString());
        EditorGUILayout.LabelField("worst sector", stats.WorstSectorEntityCount.ToString());
        EditorGUILayout.LabelField("issues", (stats.MissingFromSector + stats.MissingFromRegistry + stats.DuplicateSectorEntries + stats.StaleSectorAddress).ToString());

        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.TextArea(report, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private void Validate(bool rebuildFirst)
    {
        OUTL_World world = Object.FindObjectOfType<OUTL_World>();
        messages.Clear();
        if (world == null)
        {
            report = "No OUTL_World found in open scene.";
            stats = default(OUTL_SectorIntegrityStats);
            return;
        }

        if (rebuildFirst) world.Sectors.RebuildSectorIndexSafe();
        int issueCount = world.Sectors.ValidateIntegrity(messages, out stats);
        report = BuildReport(world, issueCount, rebuildFirst);
        Repaint();
    }

    private string BuildReport(OUTL_World world, int issueCount, bool rebuilt)
    {
        StringBuilder sb = new StringBuilder(512 + messages.Count * 80);
        sb.AppendLine("OUT CORE Lite Sector Integrity");
        sb.AppendLine("World: " + (world != null ? world.name : "none"));
        sb.AppendLine("Rebuilt first: " + rebuilt);
        sb.AppendLine("Issues: " + issueCount);
        sb.AppendLine("Registry entities: " + stats.RegistryEntityCount);
        sb.AppendLine("Sector entities: " + stats.SectorEntityCount);
        sb.AppendLine("Cells: " + stats.CellCount);
        sb.AppendLine("Worst sector count: " + stats.WorstSectorEntityCount);
        sb.AppendLine("Missing from sector: " + stats.MissingFromSector);
        sb.AppendLine("Missing from registry: " + stats.MissingFromRegistry);
        sb.AppendLine("Duplicate sector entries: " + stats.DuplicateSectorEntries);
        sb.AppendLine("Stale sector addresses: " + stats.StaleSectorAddress);
        sb.AppendLine();

        for (int i = 0; i < messages.Count; i++)
            sb.AppendLine(messages[i]);

        if (messages.Count == 0) sb.AppendLine("Sector index is consistent.");
        return sb.ToString();
    }
}
#endif
