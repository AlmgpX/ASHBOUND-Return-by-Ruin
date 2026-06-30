using UnityEngine;

[DisallowMultipleComponent]
public class OUTL_WorldNarrativeAuthor : MonoBehaviour
{
    public OUTL_WorldNarrativeConfig Config;
    public bool GenerateOnStart;
    public bool UseWriterAndExportTextures = true;
    [TextArea(8, 24)] public string LastReportPreview;
    public string LastOutputPath;
    public string LastTextureFolder;

    private OUTL_WorldNarrativeResult lastResult;
    public OUTL_WorldNarrativeResult LastResult { get { return lastResult; } }

    private void Start()
    {
        if (GenerateOnStart) Generate();
    }

    [ContextMenu("Generate World Narrative")]
    public void Generate()
    {
        if (Config == null)
        {
            Debug.LogWarning("OUTL_WorldNarrativeAuthor: не назначен Config.");
            return;
        }

        if (UseWriterAndExportTextures)
        {
            OUTL_WorldNarrativeWriteResult write = OUTL_WorldNarrativeWriter.GenerateWriteAndExport(Config);
            if (write == null)
            {
                Debug.LogWarning("OUTL_WorldNarrativeAuthor: writer failed.");
                return;
            }
            lastResult = write.World;
            LastOutputPath = write.ReportPath;
            LastTextureFolder = write.TextureFolder;
            string report = OUTL_WorldNarrativeGenerator.BuildReport(Config, lastResult);
            LastReportPreview = report.Length > 4000 ? report.Substring(0, 4000) + "\n..." : report;
            Debug.Log("OUTL world narrative written: " + LastOutputPath + " textures: " + LastTextureFolder);
            return;
        }

        lastResult = OUTL_WorldNarrativeGenerator.Generate(Config);
        string rawReport = OUTL_WorldNarrativeGenerator.BuildReport(Config, lastResult);
        LastReportPreview = rawReport.Length > 4000 ? rawReport.Substring(0, 4000) + "\n..." : rawReport;
        LastOutputPath = OUTL_WorldNarrativeGenerator.GenerateAndWrite(Config);
        LastTextureFolder = string.Empty;
        if (!string.IsNullOrEmpty(LastOutputPath)) Debug.Log("OUTL world narrative written: " + LastOutputPath);
        else Debug.Log(LastReportPreview);
    }
}
