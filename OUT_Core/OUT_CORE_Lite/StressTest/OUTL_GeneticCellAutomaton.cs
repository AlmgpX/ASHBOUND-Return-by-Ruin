using System;
using System.IO;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_GeneticCellAutomaton : MonoBehaviour, OUTL_ITickable
{
    private const byte Empty = 0;
    private const byte Cell = 1;
    private const byte Food = 2;
    private const byte Predator = 3;
    private const byte Obstacle = 4;
    private const int SaveMagic = 0x4F434153; // OCAS
    private const int SaveVersion = 1;

    public OUTL_GeneticCellAutoPreset Preset;
    public bool AutoRegister = true;
    public bool GenerateOnStart = true;
    public bool BuildVisualsOnStart = true;
    public bool UseInstancedVisuals = true;
    public Material CellMaterial;
    public Material FoodMaterial;
    public Material PredatorMaterial;
    public Material ObstacleMaterial;
    public Material EmptyMaterial;
    public Material GridMaterial;
    public Transform VisualRoot;

    [Header("Runtime Debug")]
    public bool ShowStatsGUI = true;
    public bool DebugLogStepStats = false;

    private OUTL_GeneticCellAutoPreset runtimePreset;
    private byte[] type;
    private byte[] nextType;
    private ushort[] energy;
    private ushort[] nextEnergy;
    private uint[] genome;
    private uint[] nextGenome;
    private GameObject[] visuals;
    private int width;
    private int height;
    private int count;
    private int stepIndex;
    private int liveCells;
    private int foodCount;
    private int predatorCount;
    private int obstacleCount;
    private bool registered;
    private string savePath;

    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled && type != null && runtimePreset != null; } }
    public OUTL_TickLane OUTL_TickLane { get { return runtimePreset != null ? runtimePreset.TickLane : OUTL_TickLane.Custom; } }
    public float OUTL_TickInterval { get { return runtimePreset != null ? runtimePreset.TickInterval : 0.05f; } }

    private void Awake()
    {
        EnsurePreset();
        ResolveSavePath();
    }

    private void Start()
    {
        EnsurePreset();
        if (GenerateOnStart) OUT_Generate();
        if (BuildVisualsOnStart) OUT_RebuildVisuals();
        if (AutoRegister) OUT_Register();
    }

    private void OnEnable()
    {
        if (AutoRegister) OUT_Register();
    }

    private void OnDisable()
    {
        OUT_Unregister();
    }

    private void OnDestroy()
    {
        OUT_Unregister();
    }

    [ContextMenu("OUT Generate")]
    public void OUT_Generate()
    {
        EnsurePreset();
        Allocate(runtimePreset.Width, runtimePreset.Height);
        stepIndex = 0;

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = Index(x, z);
                float field = SpawnField01(x, z);
                uint h = OUTL_HumanRandom.Hash(runtimePreset.Seed, x, z, 31);
                float r = (h & 0x00FFFFFFu) / 16777215f;

                byte t = Empty;
                if (r < runtimePreset.InitialObstacleChance * Mathf.Lerp(0.65f, 1.65f, field)) t = Obstacle;
                else if (r < runtimePreset.InitialObstacleChance + runtimePreset.InitialPredatorChance * Mathf.Lerp(0.5f, 1.5f, 1f - field)) t = Predator;
                else if (r < runtimePreset.InitialObstacleChance + runtimePreset.InitialPredatorChance + runtimePreset.InitialCellChance * Mathf.Lerp(0.75f, 1.75f, field)) t = Cell;
                else if (r < runtimePreset.InitialObstacleChance + runtimePreset.InitialPredatorChance + runtimePreset.InitialCellChance + runtimePreset.InitialFoodChance * Mathf.Lerp(0.7f, 1.6f, 1f - Mathf.Abs(field - 0.5f) * 2f)) t = Food;

                type[i] = t;
                energy[i] = InitialEnergy(t, x, z);
                genome[i] = InitialGenome(t, x, z);
            }
        }

        Recount();
        PushAllVisuals();
        LogStats("generated");
    }

    [ContextMenu("OUT Step Once")]
    public void OUT_StepOnce()
    {
        EnsurePreset();
        if (type == null) OUT_Generate();
        StepSimulation();
        PushAllVisuals();
    }

    [ContextMenu("OUT Rebuild Visuals")]
    public void OUT_RebuildVisuals()
    {
        EnsurePreset();
        if (type == null) OUT_Generate();
        BuildVisuals();
        PushAllVisuals();
    }

    [ContextMenu("OUT Save")]
    public void OUT_Save()
    {
        EnsurePreset();
        if (type == null) return;
        ResolveSavePath();
        try
        {
            using (FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (BinaryWriter w = new BinaryWriter(fs))
            {
                w.Write(SaveMagic);
                w.Write(SaveVersion);
                w.Write(width);
                w.Write(height);
                w.Write(stepIndex);
                w.Write(runtimePreset.Seed);
                for (int i = 0; i < count; i++)
                {
                    w.Write(type[i]);
                    w.Write(energy[i]);
                    w.Write(genome[i]);
                }
            }
            OUTL_DebugLog.Log(OUTL_DebugChannel.Save, "[CELL_AUTO] saved " + savePath, true);
        }
        catch (Exception ex)
        {
            OUTL_DebugLog.Log(OUTL_DebugChannel.Save, "[CELL_AUTO] save failed " + ex.Message, true);
        }
    }

    [ContextMenu("OUT Load")]
    public void OUT_Load()
    {
        EnsurePreset();
        ResolveSavePath();
        if (!File.Exists(savePath))
        {
            OUTL_DebugLog.Log(OUTL_DebugChannel.Save, "[CELL_AUTO] save not found " + savePath, true);
            return;
        }

        try
        {
            using (FileStream fs = new FileStream(savePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BinaryReader r = new BinaryReader(fs))
            {
                if (r.ReadInt32() != SaveMagic) throw new InvalidDataException("bad magic");
                int version = r.ReadInt32();
                if (version != SaveVersion) throw new InvalidDataException("bad version " + version);
                int w = r.ReadInt32();
                int h = r.ReadInt32();
                Allocate(w, h);
                stepIndex = r.ReadInt32();
                runtimePreset.Seed = r.ReadUInt32();
                for (int i = 0; i < count; i++)
                {
                    type[i] = r.ReadByte();
                    energy[i] = r.ReadUInt16();
                    genome[i] = r.ReadUInt32();
                }
            }
            Recount();
            PushAllVisuals();
            OUTL_DebugLog.Log(OUTL_DebugChannel.Save, "[CELL_AUTO] loaded " + savePath, true);
        }
        catch (Exception ex)
        {
            OUTL_DebugLog.Log(OUTL_DebugChannel.Save, "[CELL_AUTO] load failed " + ex.Message, true);
        }
    }

    public void OUT_Register()
    {
        if (registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Register(this);
        registered = true;
    }

    public void OUT_Unregister()
    {
        if (!registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Unregister(this);
        registered = false;
    }

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        if (runtimePreset == null || type == null) return;
        int steps = Mathf.Max(1, runtimePreset.StepsPerTick);
        for (int i = 0; i < steps; i++) StepSimulation();
        PushAllVisuals();

        if (runtimePreset.AutoSave && stepIndex > 0 && stepIndex % runtimePreset.AutoSaveEverySteps == 0) OUT_Save();
        if ((runtimePreset.LogMilestones || DebugLogStepStats) && stepIndex > 0 && stepIndex % runtimePreset.LogEverySteps == 0) LogStats("step");
    }

    private void StepSimulation()
    {
        if (count <= 0) return;

        Array.Copy(type, nextType, count);
        Array.Copy(energy, nextEnergy, count);
        Array.Copy(genome, nextGenome, count);

        int budget = Mathf.Min(count, runtimePreset.MaxCellsProcessedPerStep);
        int offset = runtimePreset.DeterministicScanJitter ? (int)(OUTL_HumanRandom.Hash(runtimePreset.Seed, stepIndex) % (uint)count) : 0;

        for (int n = 0; n < budget; n++)
        {
            int i = (offset + n) % count;
            byte t = type[i];
            if (t == Empty) StepEmpty(i);
            else if (t == Food) StepFood(i);
            else if (t == Cell) StepCell(i);
            else if (t == Predator) StepPredator(i);
        }

        SwapBuffers();
        stepIndex++;
        Recount();
    }

    private void StepEmpty(int i)
    {
        if (nextType[i] != Empty) return;
        int x, z;
        ToXZ(i, out x, out z);
        float chance = runtimePreset.SpontaneousFoodChance * Mathf.Lerp(0.4f, 2.0f, SpawnField01(x, z));
        if (Chance(i, 11, chance))
        {
            nextType[i] = Food;
            nextEnergy[i] = (ushort)Mathf.Clamp(runtimePreset.FoodEnergy, 1, ushort.MaxValue);
            nextGenome[i] = MutateGenome(OUTL_HumanRandom.Hash(runtimePreset.Seed, x, z, stepIndex), i, runtimePreset.CellMutationChance * 0.25f);
        }
    }

    private void StepFood(int i)
    {
        if (Chance(i, 12, runtimePreset.FoodDecayChance))
        {
            nextType[i] = Empty;
            nextEnergy[i] = 0;
            nextGenome[i] = 0;
            return;
        }

        if (!Chance(i, 13, runtimePreset.FoodSpreadChance)) return;
        int target = PickNeighbor(i, genome[i], false, Food);
        if (target >= 0 && nextType[target] == Empty)
        {
            nextType[target] = Food;
            nextEnergy[target] = (ushort)Mathf.Clamp(runtimePreset.FoodEnergy, 1, ushort.MaxValue);
            nextGenome[target] = MutateGenome(genome[i], target, 0.0025f);
        }
    }

    private void StepCell(int i)
    {
        int e = energy[i] - runtimePreset.CellBaseMetabolism;
        if (e <= 0)
        {
            Kill(i);
            return;
        }

        int food = FindNeighborOfType(i, Food, genome[i]);
        if (food >= 0)
        {
            int foodBonus = (int)(genome[i] & 15u);
            e = Mathf.Min(runtimePreset.MaxEnergy, e + runtimePreset.FoodEnergy + foodBonus);
            MoveOrCopy(i, food, Cell, e, MutateGenome(genome[i], food, runtimePreset.CellMutationChance), true);
            return;
        }

        int predator = FindNeighborOfType(i, Predator, genome[i] ^ 0xBADC0DEu);
        if (predator >= 0 && Chance(i, 14, GeneAggression(genome[i]) * runtimePreset.CellAggressionDamageChance))
        {
            int damage = 8 + (int)(genome[i] & 31u);
            nextEnergy[predator] = (ushort)Mathf.Max(0, (int)nextEnergy[predator] - damage);
        }

        if (e >= runtimePreset.CellReproduceEnergy)
        {
            int empty = PickNeighbor(i, genome[i] ^ 0xA53A9u, true, Cell);
            if (empty >= 0 && nextType[empty] == Empty)
            {
                nextType[empty] = Cell;
                nextEnergy[empty] = (ushort)Mathf.Clamp(runtimePreset.CellReproduceCost, 1, runtimePreset.MaxEnergy);
                nextGenome[empty] = MutateGenome(genome[i], empty, runtimePreset.CellMutationChance);
                e -= runtimePreset.CellReproduceCost;
            }
        }

        if (Chance(i, 15, GeneMobility(genome[i]) * runtimePreset.CellMoveChance))
        {
            int empty = PickNeighbor(i, genome[i], true, Cell);
            if (empty >= 0 && nextType[empty] == Empty)
            {
                MoveOrCopy(i, empty, Cell, e, genome[i], true);
                return;
            }
        }

        nextEnergy[i] = (ushort)Mathf.Clamp(e, 0, runtimePreset.MaxEnergy);
        nextGenome[i] = genome[i];
    }

    private void StepPredator(int i)
    {
        int e = energy[i] - runtimePreset.PredatorBaseMetabolism;
        if (e <= 0)
        {
            Kill(i);
            return;
        }

        int prey = FindNeighborOfType(i, Cell, genome[i]);
        if (prey >= 0)
        {
            int preyBonus = (int)((genome[i] >> 8) & 31u);
            e = Mathf.Min(runtimePreset.MaxEnergy, e + runtimePreset.PredatorEatEnergy + preyBonus);
            MoveOrCopy(i, prey, Predator, e, MutateGenome(genome[i], prey, runtimePreset.PredatorMutationChance), true);
            return;
        }

        if (e >= runtimePreset.PredatorReproduceEnergy)
        {
            int empty = PickNeighbor(i, genome[i] ^ 0xF00D5u, true, Predator);
            if (empty >= 0 && nextType[empty] == Empty)
            {
                nextType[empty] = Predator;
                nextEnergy[empty] = (ushort)Mathf.Clamp(runtimePreset.PredatorReproduceCost, 1, runtimePreset.MaxEnergy);
                nextGenome[empty] = MutateGenome(genome[i], empty, runtimePreset.PredatorMutationChance);
                e -= runtimePreset.PredatorReproduceCost;
            }
        }

        if (Chance(i, 16, GeneMobility(genome[i]) * runtimePreset.PredatorMoveChance))
        {
            int empty = PickNeighbor(i, genome[i], true, Predator);
            if (empty >= 0 && nextType[empty] == Empty)
            {
                MoveOrCopy(i, empty, Predator, e, genome[i], true);
                return;
            }
        }

        nextEnergy[i] = (ushort)Mathf.Clamp(e, 0, runtimePreset.MaxEnergy);
        nextGenome[i] = genome[i];
    }

    private void MoveOrCopy(int from, int to, byte newType, int newEnergy, uint newGenome, bool clearSource)
    {
        if (to < 0 || to >= count) return;
        nextType[to] = newType;
        nextEnergy[to] = (ushort)Mathf.Clamp(newEnergy, 0, runtimePreset.MaxEnergy);
        nextGenome[to] = newGenome;
        if (clearSource)
        {
            nextType[from] = Empty;
            nextEnergy[from] = 0;
            nextGenome[from] = 0;
        }
    }

    private void Kill(int i)
    {
        nextType[i] = Empty;
        nextEnergy[i] = 0;
        nextGenome[i] = 0;
    }

    private int FindNeighborOfType(int index, byte wanted, uint gene)
    {
        int start = (int)(OUTL_HumanRandom.Hash(runtimePreset.Seed ^ gene, index, stepIndex) & 7u);
        for (int k = 0; k < 8; k++)
        {
            int n = Neighbor(index, (start + k) & 7);
            if (n >= 0 && type[n] == wanted && nextType[n] == wanted) return n;
        }
        return -1;
    }

    private int PickNeighbor(int index, uint gene, bool emptyOnly, byte actor)
    {
        int start = (int)(OUTL_HumanRandom.Hash(runtimePreset.Seed ^ gene, index, stepIndex, actor) & 7u);
        int best = -1;
        float bestScore = -999f;
        for (int k = 0; k < 8; k++)
        {
            int n = Neighbor(index, (start + k) & 7);
            if (n < 0) continue;
            if (emptyOnly && (type[n] != Empty || nextType[n] != Empty)) continue;
            if (type[n] == Obstacle || nextType[n] == Obstacle) continue;
            int x, z;
            ToXZ(n, out x, out z);
            float field = SpawnField01(x, z);
            float score = field + OUTL_HumanRandom.Value01(runtimePreset.Seed ^ gene, n, stepIndex) * 0.35f;
            if (score > bestScore)
            {
                bestScore = score;
                best = n;
            }
        }
        return best;
    }

    private int Neighbor(int index, int dir)
    {
        int x, z;
        ToXZ(index, out x, out z);
        switch (dir & 7)
        {
            case 0: z--; break;
            case 1: x++; z--; break;
            case 2: x++; break;
            case 3: x++; z++; break;
            case 4: z++; break;
            case 5: x--; z++; break;
            case 6: x--; break;
            default: x--; z--; break;
        }

        if (runtimePreset.WrapEdges)
        {
            if (x < 0) x += width;
            else if (x >= width) x -= width;
            if (z < 0) z += height;
            else if (z >= height) z -= height;
            return Index(x, z);
        }

        if (x < 0 || z < 0 || x >= width || z >= height) return -1;
        return Index(x, z);
    }

    private void Allocate(int w, int h)
    {
        width = Mathf.Clamp(w, 8, 512);
        height = Mathf.Clamp(h, 8, 512);
        count = width * height;
        type = new byte[count];
        nextType = new byte[count];
        energy = new ushort[count];
        nextEnergy = new ushort[count];
        genome = new uint[count];
        nextGenome = new uint[count];
        if (visuals != null && visuals.Length != count) ClearVisuals();
    }

    private void SwapBuffers()
    {
        byte[] tt = type; type = nextType; nextType = tt;
        ushort[] ee = energy; energy = nextEnergy; nextEnergy = ee;
        uint[] gg = genome; genome = nextGenome; nextGenome = gg;
    }

    private void Recount()
    {
        liveCells = 0;
        foodCount = 0;
        predatorCount = 0;
        obstacleCount = 0;
        for (int i = 0; i < count; i++)
        {
            if (type[i] == Cell) liveCells++;
            else if (type[i] == Food) foodCount++;
            else if (type[i] == Predator) predatorCount++;
            else if (type[i] == Obstacle) obstacleCount++;
        }
    }

    private void BuildVisuals()
    {
        if (count <= 0) return;
        if (VisualRoot == null)
        {
            GameObject root = new GameObject("OUTL_GeneticCellAuto_Visuals");
            root.transform.SetParent(transform, false);
            VisualRoot = root.transform;
        }

        if (visuals == null || visuals.Length != count)
        {
            ClearVisuals();
            visuals = new GameObject[count];
            for (int i = 0; i < count; i++)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "CA_" + i;
                go.transform.SetParent(VisualRoot, false);
                Collider col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);
                visuals[i] = go;
            }
        }
    }

    private void ClearVisuals()
    {
        if (visuals != null)
        {
            for (int i = 0; i < visuals.Length; i++)
            {
                if (visuals[i] == null) continue;
                if (Application.isPlaying) Destroy(visuals[i]);
                else DestroyImmediate(visuals[i]);
            }
        }
        visuals = null;
    }

    private void PushAllVisuals()
    {
        if (!BuildVisualsOnStart && visuals == null) return;
        if (visuals == null || visuals.Length != count) BuildVisuals();
        if (visuals == null) return;

        float size = runtimePreset.CubeSize;
        float ox = -(width - 1) * 0.5f * size;
        float oz = -(height - 1) * 0.5f * size;

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = Index(x, z);
                GameObject go = visuals[i];
                if (go == null) continue;
                byte t = type[i];
                bool visible = t != Empty || runtimePreset.DrawEmptyCells;
                if (go.activeSelf != visible) go.SetActive(visible);
                if (!visible) continue;

                float h = HeightFor(t);
                go.transform.localPosition = new Vector3(ox + x * size, h * 0.5f, oz + z * size);
                go.transform.localScale = new Vector3(size * 0.96f, h, size * 0.96f);
                Renderer r = go.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = MaterialFor(t);
            }
        }
    }

    private Material MaterialFor(byte t)
    {
        if (t == Cell) return CellMaterial != null ? CellMaterial : null;
        if (t == Food) return FoodMaterial != null ? FoodMaterial : null;
        if (t == Predator) return PredatorMaterial != null ? PredatorMaterial : null;
        if (t == Obstacle) return ObstacleMaterial != null ? ObstacleMaterial : null;
        return EmptyMaterial != null ? EmptyMaterial : null;
    }

    private float HeightFor(byte t)
    {
        if (t == Cell) return runtimePreset.CellHeight;
        if (t == Food) return runtimePreset.FoodHeight;
        if (t == Predator) return runtimePreset.PredatorHeight;
        if (t == Obstacle) return runtimePreset.ObstacleHeight;
        return 0.04f;
    }

    private float SpawnField01(int x, int z)
    {
        if (runtimePreset.SpawnMode == OUTL_CellAutoSpawnMode.HumanBiased)
            return OUTL_HumanRandom.HumanHeightSample(runtimePreset.Seed, x, z, 4, 0.5f, runtimePreset.MatrixBias);
        if (runtimePreset.SpawnMode == OUTL_CellAutoSpawnMode.MixedCycleField)
            return OUTL_CycleMatrix.StructuredCycleHeight(runtimePreset.Seed, x, z, runtimePreset.WorldYear, runtimePreset.MatrixMode, runtimePreset.CycleBias, runtimePreset.MatrixBias);
        return OUTL_NumericMatrix.StructuredHeight(runtimePreset.Seed, x, z, runtimePreset.MatrixMode, 4, 0.5f, runtimePreset.MatrixBias);
    }

    private ushort InitialEnergy(byte t, int x, int z)
    {
        if (t == Cell) return (ushort)Mathf.Clamp(runtimePreset.StartCellEnergy + (int)(OUTL_HumanRandom.Hash(runtimePreset.Seed, x, z, 7) & 31u), 1, runtimePreset.MaxEnergy);
        if (t == Predator) return (ushort)Mathf.Clamp(runtimePreset.StartPredatorEnergy + (int)(OUTL_HumanRandom.Hash(runtimePreset.Seed, x, z, 8) & 31u), 1, runtimePreset.MaxEnergy);
        if (t == Food) return (ushort)Mathf.Clamp(runtimePreset.FoodEnergy, 1, runtimePreset.MaxEnergy);
        return 0;
    }

    private uint InitialGenome(byte t, int x, int z)
    {
        if (t == Empty || t == Obstacle) return 0;
        return OUTL_HumanRandom.Hash(runtimePreset.Seed ^ (uint)t, x, z, runtimePreset.WorldYear);
    }

    private uint MutateGenome(uint g, int salt, float chance)
    {
        chance = Mathf.Clamp01(chance);
        uint h = OUTL_HumanRandom.Hash(g ^ runtimePreset.Seed, salt, stepIndex);
        if (((h >> 8) & 0x00FFFFFFu) / 16777215f > chance) return g;
        int bit = (int)(h & 31u);
        return g ^ (1u << bit);
    }

    private float GeneMobility(uint g)
    {
        return 0.25f + ((g & 255u) / 255f) * 0.75f;
    }

    private float GeneAggression(uint g)
    {
        return 0.10f + (((g >> 8) & 255u) / 255f) * 0.90f;
    }

    private bool Chance(int index, int salt, float probability)
    {
        return OUTL_HumanRandom.Value01(runtimePreset.Seed ^ genome[index], index, stepIndex + salt) < Mathf.Clamp01(probability);
    }

    private int Index(int x, int z)
    {
        return x + z * width;
    }

    private void ToXZ(int index, out int x, out int z)
    {
        z = index / width;
        x = index - z * width;
    }

    private void EnsurePreset()
    {
        if (runtimePreset != null) return;
        runtimePreset = Preset != null ? Preset : OUTL_GeneticCellAutoPreset.CreateRuntimeDefault();
        runtimePreset.Sanitize();
    }

    private void ResolveSavePath()
    {
        EnsurePreset();
        string file = string.IsNullOrEmpty(runtimePreset.SaveFileName) ? "OUTL_GeneticCellAutoStress.bin" : runtimePreset.SaveFileName;
        savePath = Path.Combine(Application.persistentDataPath, "OUTL_Stress", file);
        string dir = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
    }

    private void LogStats(string reason)
    {
        OUTL_DebugLog.Log(OUTL_DebugChannel.Perf, "[CELL_AUTO] " + reason + " step=" + stepIndex + " size=" + width + "x" + height + " cells=" + liveCells + " food=" + foodCount + " predators=" + predatorCount + " obstacles=" + obstacleCount, true);
    }

    private void OnGUI()
    {
        if (!ShowStatsGUI || runtimePreset == null) return;
        GUI.Label(new Rect(12, 176, 1200, 22), "OUTL CellAuto step=" + stepIndex + " size=" + width + "x" + height + " cells=" + liveCells + " food=" + foodCount + " predators=" + predatorCount + " obstacles=" + obstacleCount + " seed=" + runtimePreset.Seed + " matrix=" + runtimePreset.MatrixMode);
    }
}
