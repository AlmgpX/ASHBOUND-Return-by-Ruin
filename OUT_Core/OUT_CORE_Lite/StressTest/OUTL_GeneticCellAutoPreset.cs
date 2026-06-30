using UnityEngine;

public enum OUTL_CellAutoSpawnMode : byte
{
    MatrixField = 0,
    HumanBiased = 1,
    MixedCycleField = 2
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Stress/Genetic Cell Automaton Preset", fileName = "OUTL_GeneticCellAutoPreset")]
public sealed class OUTL_GeneticCellAutoPreset : ScriptableObject
{
    [Header("World")]
    public string PresetId = "outl.stress.genetic_cell_auto.default";
    public int Width = 96;
    public int Height = 96;
    public uint Seed = 37973u;
    public int WorldYear = 2025;
    public OUTL_CellAutoSpawnMode SpawnMode = OUTL_CellAutoSpawnMode.MixedCycleField;
    public OUTL_NumericMatrixMode MatrixMode = OUTL_NumericMatrixMode.AhmesRoot9;
    [Range(0f, 1f)] public float MatrixBias = 0.45f;
    [Range(0f, 1f)] public float CycleBias = 0.35f;

    [Header("Initial Density")]
    [Range(0f, 1f)] public float InitialCellChance = 0.055f;
    [Range(0f, 1f)] public float InitialFoodChance = 0.12f;
    [Range(0f, 1f)] public float InitialPredatorChance = 0.012f;
    [Range(0f, 1f)] public float InitialObstacleChance = 0.075f;

    [Header("Simulation")]
    public OUTL_TickLane TickLane = OUTL_TickLane.Custom;
    [Min(0.001f)] public float TickInterval = 0.05f;
    [Min(1)] public int StepsPerTick = 1;
    [Min(1)] public int MaxCellsProcessedPerStep = 200000;
    public bool WrapEdges = true;
    public bool DeterministicScanJitter = true;

    [Header("Ecology")]
    [Range(0f, 1f)] public float SpontaneousFoodChance = 0.0015f;
    [Range(0f, 1f)] public float FoodSpreadChance = 0.025f;
    [Range(0f, 1f)] public float FoodDecayChance = 0.002f;
    public int FoodEnergy = 36;
    public int MaxEnergy = 255;

    [Header("Cells")]
    public int StartCellEnergy = 96;
    public int CellBaseMetabolism = 2;
    public int CellReproduceEnergy = 150;
    public int CellReproduceCost = 64;
    [Range(0f, 1f)] public float CellMoveChance = 0.55f;
    [Range(0f, 1f)] public float CellMutationChance = 0.018f;
    [Range(0f, 1f)] public float CellAggressionDamageChance = 0.18f;

    [Header("Predators")]
    public int StartPredatorEnergy = 140;
    public int PredatorBaseMetabolism = 4;
    public int PredatorReproduceEnergy = 205;
    public int PredatorReproduceCost = 86;
    public int PredatorEatEnergy = 90;
    [Range(0f, 1f)] public float PredatorMoveChance = 0.85f;
    [Range(0f, 1f)] public float PredatorMutationChance = 0.025f;

    [Header("Stress / Persistence")]
    public bool AutoSave = false;
    [Min(1)] public int AutoSaveEverySteps = 1500;
    public string SaveFileName = "OUTL_GeneticCellAutoStress.bin";
    public bool LogMilestones = true;
    [Min(1)] public int LogEverySteps = 250;

    [Header("Rendering")]
    public float CubeSize = 0.92f;
    public float CellHeight = 0.55f;
    public float FoodHeight = 0.24f;
    public float PredatorHeight = 0.85f;
    public float ObstacleHeight = 0.72f;
    public bool DrawEmptyCells = false;
    public bool DrawGridPlane = true;

    public void Sanitize()
    {
        Width = Mathf.Clamp(Width, 8, 512);
        Height = Mathf.Clamp(Height, 8, 512);
        TickInterval = Mathf.Max(0.001f, TickInterval);
        StepsPerTick = Mathf.Max(1, StepsPerTick);
        MaxCellsProcessedPerStep = Mathf.Max(1, MaxCellsProcessedPerStep);
        FoodEnergy = Mathf.Clamp(FoodEnergy, 1, 255);
        MaxEnergy = Mathf.Clamp(MaxEnergy, 16, 4096);
        StartCellEnergy = Mathf.Clamp(StartCellEnergy, 1, MaxEnergy);
        StartPredatorEnergy = Mathf.Clamp(StartPredatorEnergy, 1, MaxEnergy);
        CellBaseMetabolism = Mathf.Clamp(CellBaseMetabolism, 0, 64);
        PredatorBaseMetabolism = Mathf.Clamp(PredatorBaseMetabolism, 0, 64);
        CellReproduceEnergy = Mathf.Clamp(CellReproduceEnergy, 1, MaxEnergy);
        PredatorReproduceEnergy = Mathf.Clamp(PredatorReproduceEnergy, 1, MaxEnergy);
        CellReproduceCost = Mathf.Clamp(CellReproduceCost, 1, MaxEnergy);
        PredatorReproduceCost = Mathf.Clamp(PredatorReproduceCost, 1, MaxEnergy);
        PredatorEatEnergy = Mathf.Clamp(PredatorEatEnergy, 1, MaxEnergy);
        AutoSaveEverySteps = Mathf.Max(1, AutoSaveEverySteps);
        LogEverySteps = Mathf.Max(1, LogEverySteps);
        CubeSize = Mathf.Clamp(CubeSize, 0.05f, 4f);
        CellHeight = Mathf.Max(0.02f, CellHeight);
        FoodHeight = Mathf.Max(0.02f, FoodHeight);
        PredatorHeight = Mathf.Max(0.02f, PredatorHeight);
        ObstacleHeight = Mathf.Max(0.02f, ObstacleHeight);
    }

    public static OUTL_GeneticCellAutoPreset CreateRuntimeDefault()
    {
        OUTL_GeneticCellAutoPreset preset = CreateInstance<OUTL_GeneticCellAutoPreset>();
        preset.name = "OUTL_GeneticCellAutoPreset_RuntimeDefault";
        preset.hideFlags = HideFlags.DontSave;
        preset.Sanitize();
        return preset;
    }

    public static OUTL_GeneticCellAutoPreset CreateRuntimeDenseStress()
    {
        OUTL_GeneticCellAutoPreset preset = CreateRuntimeDefault();
        preset.PresetId = "outl.stress.genetic_cell_auto.dense";
        preset.Width = 160;
        preset.Height = 160;
        preset.InitialCellChance = 0.09f;
        preset.InitialFoodChance = 0.16f;
        preset.InitialPredatorChance = 0.018f;
        preset.InitialObstacleChance = 0.08f;
        preset.StepsPerTick = 1;
        preset.TickInterval = 0.035f;
        preset.LogEverySteps = 500;
        preset.Sanitize();
        return preset;
    }
}
