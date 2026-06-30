using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_GeneticCellAutoSceneBuilder : MonoBehaviour
{
    public OUTL_GeneticCellAutoPreset Preset;
    public bool CreateWorldIfMissing = true;
    public bool CreateDiagnostics = true;
    public bool CreateRuntimePresetIfMissing = true;
    public bool BuildOnStart = false;
    public Vector3 Origin = Vector3.zero;

    [Header("Generated Materials")]
    public Material CellMaterial;
    public Material FoodMaterial;
    public Material PredatorMaterial;
    public Material ObstacleMaterial;
    public Material EmptyMaterial;

    private void Start()
    {
        if (BuildOnStart) OUT_BuildScene();
    }

    [ContextMenu("OUT Build Genetic Cell Automaton Stress Scene")]
    public void OUT_BuildScene()
    {
        OUTL_World world = EnsureWorld();
        OUTL_GeneticCellAutoPreset preset = Preset;
        if (preset == null && CreateRuntimePresetIfMissing)
            preset = OUTL_GeneticCellAutoPreset.CreateRuntimeDenseStress();
        if (preset != null) preset.Sanitize();

        GameObject root = FindOrCreate("OUTL_GeneticCellAutoStress", Origin);
        OUTL_GeneticCellAutomaton sim = root.GetComponent<OUTL_GeneticCellAutomaton>();
        if (sim == null) sim = root.AddComponent<OUTL_GeneticCellAutomaton>();
        sim.Preset = preset;
        sim.CellMaterial = CellMaterial != null ? CellMaterial : CreateMaterial("OUTL_CellAuto_Cell_Mat", new Color(0.1f, 0.85f, 0.35f, 1f));
        sim.FoodMaterial = FoodMaterial != null ? FoodMaterial : CreateMaterial("OUTL_CellAuto_Food_Mat", new Color(1f, 0.78f, 0.18f, 1f));
        sim.PredatorMaterial = PredatorMaterial != null ? PredatorMaterial : CreateMaterial("OUTL_CellAuto_Predator_Mat", new Color(1f, 0.12f, 0.08f, 1f));
        sim.ObstacleMaterial = ObstacleMaterial != null ? ObstacleMaterial : CreateMaterial("OUTL_CellAuto_Obstacle_Mat", new Color(0.18f, 0.18f, 0.22f, 1f));
        sim.EmptyMaterial = EmptyMaterial != null ? EmptyMaterial : CreateMaterial("OUTL_CellAuto_Empty_Mat", new Color(0.05f, 0.07f, 0.09f, 0.25f));
        sim.GenerateOnStart = false;
        sim.BuildVisualsOnStart = true;
        sim.AutoRegister = true;
        sim.OUT_Generate();
        sim.OUT_RebuildVisuals();
        sim.OUT_Register();

        if (CreateDiagnostics)
        {
            GameObject diag = FindOrCreate("OUTL_StressDiagnostics", Origin + new Vector3(0f, 0f, -4f));
            OUTL_CoreLiteDiagnostics d = diag.GetComponent<OUTL_CoreLiteDiagnostics>();
            if (d == null) d = diag.AddComponent<OUTL_CoreLiteDiagnostics>();
            d.RunOnStart = false;
            d.DrawOnGUI = true;
            d.LogReportToConsole = true;
            d.RunDiagnostics();
        }

        OUTL_DebugLog.Log(OUTL_DebugChannel.Perf, "[CELL_AUTO_BUILDER] built stress scene preset=" + (preset != null ? preset.PresetId : "null") + " world=" + (world != null ? "ok" : "null"), true);
    }

    private OUTL_World EnsureWorld()
    {
        OUTL_World world = OUTL_World.Instance;
        if (world != null || !CreateWorldIfMissing) return world;

        GameObject go = FindOrCreate("OUTL_Runtime", Vector3.zero);
        world = go.GetComponent<OUTL_World>();
        if (world == null) world = go.AddComponent<OUTL_World>();
        world.UpdateMode = OUTL_WorldUpdateMode.CustomFixedStep;
        world.TimeSource = OUTL_WorldTimeSource.UnscaledUnityTime;
        world.SimulationStep = 0.05f;
        world.CustomTickInterval = 0.05f;
        world.RandomTickInterval = 0.25f;
        world.RandomTickBudget = 256;
        world.AutoFindAdaptersOnStart = true;
        world.DebugStats = true;
        return world;
    }

    private static GameObject FindOrCreate(string name, Vector3 position)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) go = new GameObject(name);
        go.transform.position = position;
        return go;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        Shader shader = FindSafeShader();
        Material material = new Material(shader);
        material.name = name;
        material.hideFlags = HideFlags.DontSave;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        material.color = color;
        return material;
    }

    private static Shader FindSafeShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader != null) return shader;
        shader = Shader.Find("HDRP/Lit");
        if (shader != null) return shader;
        shader = Shader.Find("Standard");
        if (shader != null) return shader;
        shader = Shader.Find("Unlit/Color");
        if (shader != null) return shader;
        shader = Shader.Find("Sprites/Default");
        return shader;
    }
}
