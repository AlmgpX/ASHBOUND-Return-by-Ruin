#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public sealed class OUTL_SectorIntegrityWindow : EditorWindow
{
    private readonly List<string> messages = new List<string>(256);
    private readonly List<OUTL_EntityRuntime> registryRows = new List<OUTL_EntityRuntime>(1024);
    private readonly List<OUTL_SectorCellStats> cellStats = new List<OUTL_SectorCellStats>(256);
    private OUTL_SectorIntegrityStats stats;
    private string report = "Not scanned yet.";
    private Vector2 scroll;

    // [MenuItem("OUT CORE Lite/Workbench/Sector Integrity Window")]
    public static void Open()
    {
        GetWindow<OUTL_SectorIntegrityWindow>("OUTL Sector Integrity");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("OUT CORE Lite Sector Integrity Window", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Scan Open Scene Runtime State")) Scan(false);
        if (GUILayout.Button("Rebuild + Scan")) Scan(true);
        if (GUILayout.Button("Copy Report")) EditorGUIUtility.systemCopyBuffer = report;
        if (GUILayout.Button("Export Report")) ExportReport();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("Registry entities", stats.RegistryEntityCount.ToString());
        EditorGUILayout.LabelField("Sector entities", stats.SectorEntityCount.ToString());
        EditorGUILayout.LabelField("Cells", stats.CellCount.ToString());
        EditorGUILayout.LabelField("Missing from sector", stats.MissingFromSector.ToString());
        EditorGUILayout.LabelField("Missing from registry", stats.MissingFromRegistry.ToString());
        EditorGUILayout.LabelField("Duplicate ids", stats.DuplicateSectorEntries.ToString());
        EditorGUILayout.LabelField("Stale sector address", stats.StaleSectorAddress.ToString());
        EditorGUILayout.LabelField("Worst sector by entities", stats.WorstSectorEntityCount.ToString());

        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.TextArea(report, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private void Scan(bool rebuildFirst)
    {
        OUTL_World world = Object.FindObjectOfType<OUTL_World>();
        messages.Clear();
        cellStats.Clear();
        registryRows.Clear();
        stats = default(OUTL_SectorIntegrityStats);

        if (world == null)
        {
            report = "No OUTL_World found in open scene.";
            Repaint();
            return;
        }

        if (!Application.isPlaying)
            messages.Add("Edit Mode scan: runtime registry/sector state may be empty until Play Mode.");

        if (rebuildFirst && Application.isPlaying) world.Sectors.RebuildSectorIndexSafe();
        int issueCount = Application.isPlaying ? world.Sectors.ValidateIntegrity(messages, out stats) : 0;
        world.Sectors.CopyCellStats(cellStats);
        world.Registry.CopyAll(registryRows);
        AppendAdapterRuntimeChecks();
        report = BuildReport(world, issueCount, rebuildFirst);
        Repaint();
    }

    private void AppendAdapterRuntimeChecks()
    {
        OUTL_EntityAdapter[] adapters = Object.FindObjectsOfType<OUTL_EntityAdapter>(true);
        int activeAdapterWithoutRuntime = 0;
        for (int i = 0; i < adapters.Length; i++)
        {
            OUTL_EntityAdapter adapter = adapters[i];
            if (adapter == null || !adapter.isActiveAndEnabled) continue;
            if (adapter.Runtime == null)
            {
                activeAdapterWithoutRuntime++;
                messages.Add("active adapter without runtime: " + adapter.name);
            }
        }

        int runtimeWithoutAdapter = 0;
        for (int i = 0; i < registryRows.Count; i++)
        {
            OUTL_EntityRuntime runtime = registryRows[i];
            if (runtime == null || runtime.Adapter != null) continue;
            runtimeWithoutAdapter++;
            messages.Add("runtime without adapter id=" + runtime.Id);
        }

        if (activeAdapterWithoutRuntime == 0) messages.Add("active adapter without runtime: none");
        if (runtimeWithoutAdapter == 0) messages.Add("runtime without adapter: none");
    }

    private string BuildReport(OUTL_World world, int issueCount, bool rebuilt)
    {
        OUTL_SectorCellStats worstStimulus = default(OUTL_SectorCellStats);
        OUTL_SectorCellStats worstEntities = default(OUTL_SectorCellStats);
        for (int i = 0; i < cellStats.Count; i++)
        {
            OUTL_SectorCellStats cell = cellStats[i];
            if (cell.EntityCount > worstEntities.EntityCount) worstEntities = cell;
            if (cell.StimulusCount > worstStimulus.StimulusCount) worstStimulus = cell;
        }

        StringBuilder sb = new StringBuilder(1024 + messages.Count * 80);
        sb.AppendLine("OUT CORE Lite Sector Integrity Window");
        sb.AppendLine("World: " + (world != null ? world.name : "none"));
        sb.AppendLine("Play Mode: " + Application.isPlaying);
        sb.AppendLine("Rebuilt first: " + rebuilt);
        sb.AppendLine("Issues: " + issueCount);
        sb.AppendLine("Registry but not SectorGrid: " + stats.MissingFromSector);
        sb.AppendLine("SectorGrid but not Registry: " + stats.MissingFromRegistry);
        sb.AppendLine("Duplicate ids: " + stats.DuplicateSectorEntries);
        sb.AppendLine("Stale sector address: " + stats.StaleSectorAddress);
        sb.AppendLine("Worst sector by entity count: cell=" + worstEntities.CellId + " count=" + worstEntities.EntityCount + " x=" + worstEntities.X + " z=" + worstEntities.Z);
        sb.AppendLine("Worst sector by stimulus count: cell=" + worstStimulus.CellId + " count=" + worstStimulus.StimulusCount + " x=" + worstStimulus.X + " z=" + worstStimulus.Z);
        sb.AppendLine();
        for (int i = 0; i < messages.Count; i++) sb.AppendLine(messages[i]);
        if (messages.Count == 0) sb.AppendLine("Sector index is consistent.");
        return sb.ToString();
    }

    private void ExportReport()
    {
        string path = EditorUtility.SaveFilePanel("Export OUTL sector integrity report", Application.dataPath, "OUTL_SectorIntegrityReport.txt", "txt");
        if (string.IsNullOrEmpty(path)) return;
        File.WriteAllText(path, report);
    }
}
#endif
