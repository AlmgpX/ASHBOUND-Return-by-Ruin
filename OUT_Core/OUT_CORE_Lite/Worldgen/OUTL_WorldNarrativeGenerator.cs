using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public struct OUTL_WorldTile
{
    public int X;
    public int Y;
    public float Height;
    public float Moisture;
    public float Heat;
    public float Drainage;
    public float Slope;
    public bool HasRiver;
    public bool HasLake;
    public OUTL_WorldZoneType Zone;
    public int Prosperity;
    public int Danger;
    public int Sanctity;
    public readonly List<string> Resources;
    public readonly List<string> Plants;
    public readonly List<string> Events;

    public OUTL_WorldTile(int x, int y)
    {
        X = x;
        Y = y;
        Height = 0f;
        Moisture = 0f;
        Heat = 0f;
        Drainage = 0f;
        Slope = 0f;
        HasRiver = false;
        HasLake = false;
        Zone = OUTL_WorldZoneType.Plains;
        Prosperity = 0;
        Danger = 0;
        Sanctity = 0;
        Resources = new List<string>(4);
        Plants = new List<string>(4);
        Events = new List<string>(8);
    }
}

public sealed class OUTL_WorldNarrativeResult
{
    public string WorldName;
    public string EraName;
    public string Language;
    public int Seed;
    public int Width;
    public int Height;
    public int Years;
    public OUTL_WorldTile[,] Tiles;
    public readonly List<string> Timeline = new List<string>(256);
    public readonly Dictionary<OUTL_WorldZoneType, int> ZoneCounts = new Dictionary<OUTL_WorldZoneType, int>();
}

public static class OUTL_WorldNarrativeGenerator
{
    public static OUTL_WorldNarrativeResult Generate(OUTL_WorldNarrativeConfig config)
    {
        if (config == null) throw new ArgumentNullException("config");

        int width = Mathf.Max(4, config.Width);
        int height = Mathf.Max(4, config.Height);
        int years = Mathf.Max(1, config.Years);
        System.Random rng = new System.Random(config.Seed);

        OUTL_WorldNarrativeResult result = new OUTL_WorldNarrativeResult();
        result.Language = string.IsNullOrEmpty(config.Language) ? "ru" : config.Language;
        result.WorldName = L(config.WorldName, result.Language, "Безымянный мир");
        result.EraName = L(config.EraName, result.Language, "Первая эпоха");
        result.Seed = config.Seed;
        result.Width = width;
        result.Height = height;
        result.Years = years;
        result.Tiles = new OUTL_WorldTile[width, height];

        Vector2 hOffset = new Vector2(rng.Next(-10000, 10000), rng.Next(-10000, 10000));
        Vector2 mOffset = new Vector2(rng.Next(-10000, 10000), rng.Next(-10000, 10000));
        Vector2 tOffset = new Vector2(rng.Next(-10000, 10000), rng.Next(-10000, 10000));

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                OUTL_WorldTile tile = new OUTL_WorldTile(x, y);
                tile.Height = Fractal01(x, y, config.HeightScale, config.Octaves, config.Persistence, config.Lacunarity, hOffset);
                tile.Moisture = Fractal01(x, y, config.MoistureScale, config.Octaves, config.Persistence, config.Lacunarity, mOffset);
                float rawHeat = Fractal01(x, y, config.HeatScale, config.Octaves, config.Persistence, config.Lacunarity, tOffset);
                tile.Heat = ApplyClimateHeat(config, rawHeat, tile.Height, y, height);
                result.Tiles[x, y] = tile;
            }
        }

        ComputeSlopeAndDrainage(config, result);
        if (config.GenerateRivers) GenerateRivers(config, result, rng);
        GenerateLakes(config, result, rng);
        RecomputeMoistureNearWater(config, result);
        AssignZones(config, result, rng);
        SeedResourcesAndPlants(config, result, rng);
        SimulateYears(config, result, rng);
        return result;
    }

    public static string BuildReport(OUTL_WorldNarrativeConfig config, OUTL_WorldNarrativeResult result)
    {
        string lang = result != null && !string.IsNullOrEmpty(result.Language) ? result.Language : (config != null ? config.Language : "ru");
        StringBuilder sb = new StringBuilder(32768);
        sb.AppendLine("# Отчёт OUT CORE Lite: нарративный мир");
        sb.AppendLine();
        sb.AppendLine("Мир: " + result.WorldName);
        sb.AppendLine("Эпоха: " + result.EraName);
        sb.AppendLine("Seed: " + result.Seed);
        sb.AppendLine("Карта: " + result.Width + "x" + result.Height);
        sb.AppendLine("Смоделировано лет: " + result.Years);
        sb.AppendLine();

        sb.AppendLine("## Сводка зон");
        foreach (var kv in result.ZoneCounts)
            sb.AppendLine("- " + ZoneName(kv.Key, lang) + ": " + kv.Value);
        sb.AppendLine();

        sb.AppendLine("## Карта мира");
        sb.AppendLine("Легенда: ~ океан, = берег, . равнины, F лес, h холмы, M горы, S болото, D пустыня, W пустошь, * священное место, R руины, | река, O озеро, T тундра, s степь");
        for (int y = result.Height - 1; y >= 0; y--)
        {
            for (int x = 0; x < result.Width; x++)
                sb.Append(ZoneChar(result.Tiles[x, y]));
            sb.AppendLine();
        }
        sb.AppendLine();

        sb.AppendLine("## Хроника мира");
        for (int i = 0; i < result.Timeline.Count; i++)
            sb.AppendLine("- " + result.Timeline[i]);
        sb.AppendLine();

        sb.AppendLine("## Примечательные области");
        AppendTopTiles(sb, result, "опасность", t => t.Danger, 10, lang);
        AppendTopTiles(sb, result, "богатство", t => t.Prosperity, 10, lang);
        AppendTopTiles(sb, result, "сакральность", t => t.Sanctity, 10, lang);

        if (config != null && config.IncludeTileDump)
        {
            sb.AppendLine("## Полный дамп тайлов");
            for (int y = 0; y < result.Height; y++)
            {
                for (int x = 0; x < result.Width; x++)
                {
                    OUTL_WorldTile t = result.Tiles[x, y];
                    sb.AppendLine("Тайл " + x + "," + y + " зона=" + ZoneName(t.Zone, lang) + " h=" + t.Height.ToString("0.00") + " m=" + t.Moisture.ToString("0.00") + " heat=" + t.Heat.ToString("0.00") + " slope=" + t.Slope.ToString("0.00") + " drainage=" + t.Drainage.ToString("0.00") + " prosperity=" + t.Prosperity + " danger=" + t.Danger + " sanctity=" + t.Sanctity + " resources=" + Join(t.Resources) + " plants=" + Join(t.Plants));
                }
            }
        }

        return sb.ToString();
    }

    public static string GenerateAndWrite(OUTL_WorldNarrativeConfig config)
    {
        OUTL_WorldNarrativeResult result = Generate(config);
        string report = BuildReport(config, result);
        if (config != null && config.WriteToPersistentDataPath)
        {
            string folder = Path.Combine(Application.persistentDataPath, config.OutputFolder);
            Directory.CreateDirectory(folder);
            string safeName = string.IsNullOrEmpty(result.WorldName) ? "World" : result.WorldName.Replace('/', '_').Replace('\\', '_');
            string path = Path.Combine(folder, safeName + "_seed_" + config.Seed + ".md");
            File.WriteAllText(path, report, Encoding.UTF8);
            return path;
        }
        return string.Empty;
    }

    private static float ApplyClimateHeat(OUTL_WorldNarrativeConfig config, float rawHeat, float height, int y, int mapHeight)
    {
        float heat = rawHeat;
        if (config.UseLatitudeHeat)
        {
            float yn = mapHeight <= 1 ? 0.5f : y / (float)(mapHeight - 1);
            float latitude = Mathf.Abs(yn - Mathf.Clamp01(config.EquatorY)) * 2f;
            heat -= latitude * Mathf.Clamp01(config.PolarCooling);
        }
        heat -= height * Mathf.Clamp01(config.AltitudeCooling);
        return Mathf.Clamp01(heat);
    }

    private static void ComputeSlopeAndDrainage(OUTL_WorldNarrativeConfig config, OUTL_WorldNarrativeResult result)
    {
        for (int y = 0; y < result.Height; y++)
        {
            for (int x = 0; x < result.Width; x++)
            {
                OUTL_WorldTile tile = result.Tiles[x, y];
                float maxDiff = 0f;
                for (int oy = -1; oy <= 1; oy++)
                {
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        if (ox == 0 && oy == 0) continue;
                        int nx = x + ox;
                        int ny = y + oy;
                        if (!Inside(result, nx, ny)) continue;
                        float diff = Mathf.Abs(tile.Height - result.Tiles[nx, ny].Height);
                        if (diff > maxDiff) maxDiff = diff;
                    }
                }
                tile.Slope = Mathf.Clamp01(maxDiff * 4f);
                tile.Drainage = Mathf.Clamp01(tile.Height * config.DrainageFromHeight + tile.Slope * config.DrainageFromSlope);
                result.Tiles[x, y] = tile;
            }
        }
    }

    private static void GenerateRivers(OUTL_WorldNarrativeConfig config, OUTL_WorldNarrativeResult result, System.Random rng)
    {
        int count = Mathf.Max(0, config.RiverCount);
        for (int r = 0; r < count; r++)
        {
            int sx = 0;
            int sy = 0;
            float best = -1f;
            for (int attempt = 0; attempt < 64; attempt++)
            {
                int x = rng.Next(0, result.Width);
                int y = rng.Next(0, result.Height);
                OUTL_WorldTile t = result.Tiles[x, y];
                float score = t.Height + t.Moisture * 0.2f - t.Heat * 0.1f;
                if (t.Height >= config.RiverSourceMinHeight && score > best)
                {
                    best = score;
                    sx = x;
                    sy = y;
                }
            }
            if (best < 0f) continue;
            CarveRiver(config, result, sx, sy, rng);
        }
    }

    private static void CarveRiver(OUTL_WorldNarrativeConfig config, OUTL_WorldNarrativeResult result, int x, int y, System.Random rng)
    {
        int cx = x;
        int cy = y;
        for (int step = 0; step < config.RiverMaxSteps; step++)
        {
            if (!Inside(result, cx, cy)) break;
            OUTL_WorldTile tile = result.Tiles[cx, cy];
            tile.HasRiver = true;
            tile.Moisture = Mathf.Clamp01(tile.Moisture + config.RiverMoistureBoost);
            tile.Danger += tile.Height > config.HillsHeight ? 1 : 0;
            result.Tiles[cx, cy] = tile;

            if (tile.Height <= config.CoastHeight || tile.Zone == OUTL_WorldZoneType.Ocean) break;

            int bx = cx;
            int by = cy;
            float bestHeight = tile.Height;
            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    if (Mathf.Abs(ox) + Mathf.Abs(oy) != 1) continue;
                    int nx = cx + ox;
                    int ny = cy + oy;
                    if (!Inside(result, nx, ny)) continue;
                    float h = result.Tiles[nx, ny].Height + (float)rng.NextDouble() * 0.015f;
                    if (h < bestHeight)
                    {
                        bestHeight = h;
                        bx = nx;
                        by = ny;
                    }
                }
            }

            if (bx == cx && by == cy)
            {
                int nx = Mathf.Clamp(cx + rng.Next(-1, 2), 0, result.Width - 1);
                int ny = Mathf.Clamp(cy + rng.Next(-1, 2), 0, result.Height - 1);
                if (nx == cx && ny == cy) break;
                bx = nx;
                by = ny;
            }
            cx = bx;
            cy = by;
        }
    }

    private static void GenerateLakes(OUTL_WorldNarrativeConfig config, OUTL_WorldNarrativeResult result, System.Random rng)
    {
        for (int y = 0; y < result.Height; y++)
        {
            for (int x = 0; x < result.Width; x++)
            {
                OUTL_WorldTile tile = result.Tiles[x, y];
                if (tile.Height > config.LakeMaxHeight || tile.Height <= config.OceanHeight) continue;
                if (tile.Moisture < 0.62f || tile.Drainage > 0.45f) continue;
                if (rng.NextDouble() > config.LakeChance) continue;
                tile.HasLake = true;
                tile.Moisture = Mathf.Clamp01(tile.Moisture + 0.2f);
                result.Tiles[x, y] = tile;
            }
        }
    }

    private static void RecomputeMoistureNearWater(OUTL_WorldNarrativeConfig config, OUTL_WorldNarrativeResult result)
    {
        for (int y = 0; y < result.Height; y++)
        {
            for (int x = 0; x < result.Width; x++)
            {
                OUTL_WorldTile tile = result.Tiles[x, y];
                if (tile.HasRiver || tile.HasLake || tile.Height <= config.OceanHeight) continue;
                float boost = 0f;
                for (int oy = -2; oy <= 2; oy++)
                {
                    for (int ox = -2; ox <= 2; ox++)
                    {
                        int nx = x + ox;
                        int ny = y + oy;
                        if (!Inside(result, nx, ny)) continue;
                        OUTL_WorldTile n = result.Tiles[nx, ny];
                        if (!n.HasRiver && !n.HasLake && n.Height > config.OceanHeight) continue;
                        float d = Mathf.Max(1f, Mathf.Abs(ox) + Mathf.Abs(oy));
                        boost = Mathf.Max(boost, config.RiverMoistureBoost / d);
                    }
                }
                if (boost > 0f)
                {
                    tile.Moisture = Mathf.Clamp01(tile.Moisture + boost);
                    result.Tiles[x, y] = tile;
                }
            }
        }
    }

    private static void AssignZones(OUTL_WorldNarrativeConfig config, OUTL_WorldNarrativeResult result, System.Random rng)
    {
        result.ZoneCounts.Clear();
        for (int y = 0; y < result.Height; y++)
        {
            for (int x = 0; x < result.Width; x++)
            {
                OUTL_WorldTile tile = result.Tiles[x, y];
                tile.Zone = PickZone(config, tile, rng);
                tile.Prosperity = Mathf.RoundToInt((tile.Moisture * 5f + (1f - Mathf.Abs(tile.Heat - 0.48f)) * 4f + (1f - tile.Drainage) * 2f + (tile.HasRiver ? 2f : 0f) + (tile.HasLake ? 1f : 0f)) - 5f);
                tile.Danger = tile.Zone == OUTL_WorldZoneType.Wasteland || tile.Zone == OUTL_WorldZoneType.Ruins || tile.Zone == OUTL_WorldZoneType.Mountains ? 2 : 0;
                tile.Sanctity = tile.Zone == OUTL_WorldZoneType.Sacred ? 5 : 0;
                result.Tiles[x, y] = tile;
                Increment(result.ZoneCounts, tile.Zone);
            }
        }
    }

    private static void SeedResourcesAndPlants(OUTL_WorldNarrativeConfig config, OUTL_WorldNarrativeResult result, System.Random rng)
    {
        for (int y = 0; y < result.Height; y++)
        {
            for (int x = 0; x < result.Width; x++)
            {
                OUTL_WorldTile tile = result.Tiles[x, y];
                if (config.Resources != null)
                {
                    for (int i = 0; i < config.Resources.Length; i++)
                    {
                        OUTL_WorldResourceDef r = config.Resources[i];
                        if (r == null || !Matches(tile, r.AllowedZones, r.MinHeight, r.MaxHeight, r.MinMoisture, r.MaxMoisture, r.MinHeat, r.MaxHeat, r.MinDrainage, r.MaxDrainage)) continue;
                        if (rng.NextDouble() <= r.BaseChance) tile.Resources.Add(r.Id);
                    }
                }
                if (config.Plants != null)
                {
                    for (int i = 0; i < config.Plants.Length; i++)
                    {
                        OUTL_WorldPlantDef p = config.Plants[i];
                        if (p == null || !Matches(tile, p.AllowedZones, p.MinHeight, p.MaxHeight, p.MinMoisture, p.MaxMoisture, p.MinHeat, p.MaxHeat, p.MinDrainage, p.MaxDrainage)) continue;
                        if (rng.NextDouble() <= p.BaseChance) tile.Plants.Add(p.Id);
                    }
                }
                result.Tiles[x, y] = tile;
            }
        }
    }

    private static void SimulateYears(OUTL_WorldNarrativeConfig config, OUTL_WorldNarrativeResult result, System.Random rng)
    {
        string lang = result.Language;
        result.Timeline.Add("Год 0: мир «" + result.WorldName + "» входит в эпоху «" + result.EraName + "». Ландшафт уже задан: высоты, вода, ресурсы и будущие поводы для катастроф.");
        for (int year = 1; year <= result.Years; year++)
        {
            if (config.Events == null || config.Events.Length == 0) continue;
            int attempts = Mathf.Max(1, (result.Width * result.Height) / 96);
            for (int a = 0; a < attempts; a++)
            {
                OUTL_WorldEventDef e = config.Events[rng.Next(0, config.Events.Length)];
                if (e == null || year < e.MinYear || year > e.MaxYear) continue;
                int x = rng.Next(0, result.Width);
                int y = rng.Next(0, result.Height);
                OUTL_WorldTile tile = result.Tiles[x, y];
                if (!Matches(tile, e.AllowedZones, e.MinHeight, e.MaxHeight, e.MinMoisture, e.MaxMoisture, e.MinHeat, e.MaxHeat, e.MinDrainage, e.MaxDrainage)) continue;
                float chance = e.BaseChancePerYear * (1f + Mathf.Max(0, tile.Prosperity) * 0.03f + Mathf.Max(0, tile.Danger) * 0.04f);
                if (rng.NextDouble() > chance) continue;
                tile.Prosperity += e.ProsperityDelta;
                tile.Danger += e.DangerDelta;
                tile.Sanctity += e.SanctityDelta;
                string line = PickLine(e.Lines, rng, lang, L(e.DisplayName, lang, e.Id));
                string record = "Год " + year + ": " + line + " [" + ZoneName(tile.Zone, lang) + " " + x + "," + y + "]";
                tile.Events.Add(record);
                result.Timeline.Add(record);
                result.Tiles[x, y] = tile;
            }
        }
        if (result.Timeline.Count == 1) result.Timeline.Add("Год " + result.Years + ": в хроники не попало ничего крупного. Это не значит, что ничего не произошло; просто архивариусы опять были бесполезны.");
    }

    private static OUTL_WorldZoneType PickZone(OUTL_WorldNarrativeConfig c, OUTL_WorldTile tile, System.Random rng)
    {
        if (tile.Height < c.OceanHeight) return OUTL_WorldZoneType.Ocean;
        if (tile.HasLake) return OUTL_WorldZoneType.Lake;
        if (tile.HasRiver) return OUTL_WorldZoneType.River;
        if (tile.Height < c.CoastHeight) return OUTL_WorldZoneType.Coast;
        if (rng.NextDouble() < c.RuinsChance) return OUTL_WorldZoneType.Ruins;
        if (rng.NextDouble() < c.SacredChance) return OUTL_WorldZoneType.Sacred;
        if (rng.NextDouble() < c.WastelandChance) return OUTL_WorldZoneType.Wasteland;
        if (tile.Heat <= c.TundraHeat) return OUTL_WorldZoneType.Tundra;
        if (tile.Height >= c.MountainHeight) return OUTL_WorldZoneType.Mountains;
        if (tile.Height >= c.HillsHeight) return OUTL_WorldZoneType.Hills;
        if (tile.Moisture >= c.SwampMoisture && tile.Drainage <= 0.45f) return OUTL_WorldZoneType.Swamp;
        if (tile.Heat >= c.DesertHeat && tile.Moisture <= c.DesertMoisture) return OUTL_WorldZoneType.Desert;
        if (tile.Moisture <= c.SteppeMoisture) return OUTL_WorldZoneType.Steppe;
        if (tile.Moisture >= c.ForestMoisture) return OUTL_WorldZoneType.Forest;
        return OUTL_WorldZoneType.Plains;
    }

    private static float Fractal01(float x, float y, float scale, int octaves, float persistence, float lacunarity, Vector2 offset)
    {
        scale = Mathf.Max(0.001f, scale);
        octaves = Mathf.Max(1, octaves);
        float amplitude = 1f;
        float frequency = 1f;
        float value = 0f;
        float norm = 0f;
        for (int i = 0; i < octaves; i++)
        {
            float sx = (x + offset.x) / scale * frequency;
            float sy = (y + offset.y) / scale * frequency;
            value += Mathf.PerlinNoise(sx, sy) * amplitude;
            norm += amplitude;
            amplitude *= Mathf.Clamp01(persistence);
            frequency *= Mathf.Max(1.01f, lacunarity);
        }
        return Mathf.Clamp01(value / Mathf.Max(0.0001f, norm));
    }

    private static bool Matches(OUTL_WorldTile tile, OUTL_WorldZoneType[] zones, float minH, float maxH, float minM, float maxM, float minHeat, float maxHeat, float minDrainage, float maxDrainage)
    {
        if (zones != null && zones.Length > 0)
        {
            bool ok = false;
            for (int i = 0; i < zones.Length; i++) if (zones[i] == tile.Zone) { ok = true; break; }
            if (!ok) return false;
        }
        return tile.Height >= minH && tile.Height <= maxH && tile.Moisture >= minM && tile.Moisture <= maxM && tile.Heat >= minHeat && tile.Heat <= maxHeat && tile.Drainage >= minDrainage && tile.Drainage <= maxDrainage;
    }

    private static bool Inside(OUTL_WorldNarrativeResult result, int x, int y)
    {
        return x >= 0 && y >= 0 && x < result.Width && y < result.Height;
    }

    private static void Increment(Dictionary<OUTL_WorldZoneType, int> dict, OUTL_WorldZoneType zone)
    {
        int v;
        dict.TryGetValue(zone, out v);
        dict[zone] = v + 1;
    }

    private static string PickLine(OUTL_LocalizedText[] lines, System.Random rng, string language, string fallback)
    {
        if (lines == null || lines.Length == 0) return fallback;
        return L(lines[rng.Next(0, lines.Length)], language, fallback);
    }

    private static string L(OUTL_LocalizedText text, string language, string fallback)
    {
        return text != null ? text.Get(string.IsNullOrEmpty(language) ? "ru" : language, fallback) : fallback;
    }

    private static string ZoneName(OUTL_WorldZoneType zone, string language)
    {
        switch (zone)
        {
            case OUTL_WorldZoneType.Ocean: return "океан";
            case OUTL_WorldZoneType.Coast: return "берег";
            case OUTL_WorldZoneType.Plains: return "равнины";
            case OUTL_WorldZoneType.Forest: return "лес";
            case OUTL_WorldZoneType.Hills: return "холмы";
            case OUTL_WorldZoneType.Mountains: return "горы";
            case OUTL_WorldZoneType.Swamp: return "болото";
            case OUTL_WorldZoneType.Desert: return "пустыня";
            case OUTL_WorldZoneType.Wasteland: return "пустошь";
            case OUTL_WorldZoneType.Sacred: return "сакральное место";
            case OUTL_WorldZoneType.Ruins: return "руины";
            case OUTL_WorldZoneType.River: return "река";
            case OUTL_WorldZoneType.Lake: return "озеро";
            case OUTL_WorldZoneType.Tundra: return "тундра";
            case OUTL_WorldZoneType.Steppe: return "степь";
        }
        return zone.ToString();
    }

    private static char ZoneChar(OUTL_WorldTile tile)
    {
        switch (tile.Zone)
        {
            case OUTL_WorldZoneType.Ocean: return '~';
            case OUTL_WorldZoneType.Coast: return '=';
            case OUTL_WorldZoneType.Plains: return '.';
            case OUTL_WorldZoneType.Forest: return 'F';
            case OUTL_WorldZoneType.Hills: return 'h';
            case OUTL_WorldZoneType.Mountains: return 'M';
            case OUTL_WorldZoneType.Swamp: return 'S';
            case OUTL_WorldZoneType.Desert: return 'D';
            case OUTL_WorldZoneType.Wasteland: return 'W';
            case OUTL_WorldZoneType.Sacred: return '*';
            case OUTL_WorldZoneType.Ruins: return 'R';
            case OUTL_WorldZoneType.River: return '|';
            case OUTL_WorldZoneType.Lake: return 'O';
            case OUTL_WorldZoneType.Tundra: return 'T';
            case OUTL_WorldZoneType.Steppe: return 's';
        }
        return '?';
    }

    private delegate int TileScore(OUTL_WorldTile tile);

    private static void AppendTopTiles(StringBuilder sb, OUTL_WorldNarrativeResult result, string label, TileScore score, int count, string lang)
    {
        List<OUTL_WorldTile> tiles = new List<OUTL_WorldTile>(result.Width * result.Height);
        for (int y = 0; y < result.Height; y++) for (int x = 0; x < result.Width; x++) tiles.Add(result.Tiles[x, y]);
        tiles.Sort((a, b) => score(b).CompareTo(score(a)));
        sb.AppendLine("### Максимум: " + label);
        int n = Mathf.Min(count, tiles.Count);
        for (int i = 0; i < n; i++)
        {
            OUTL_WorldTile t = tiles[i];
            sb.AppendLine("- " + label + " " + score(t) + " в " + t.X + "," + t.Y + " зона=" + ZoneName(t.Zone, lang) + " ресурсы=" + Join(t.Resources) + " растения=" + Join(t.Plants));
        }
        sb.AppendLine();
    }

    private static string Join(List<string> list)
    {
        if (list == null || list.Count == 0) return "-";
        return string.Join(",", list.ToArray());
    }
}
