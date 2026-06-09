using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace OUT_ASHBOUND;

public static class OUT_SaveSystem
{
    public static void Save(OUT_State state, string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);

        var dto = new OUT_SaveDto
        {
            Turn = state.Turn,
            Loops = state.Loops,
            Memory = state.Memory,
            Residue = state.Residue,
            Shards = state.Shards,
            ShardsRequired = state.ShardsRequired,
            Mode = state.Mode.ToString(),
            PlayerId = state.PlayerId,
            RuinX = state.RuinNode.X,
            RuinY = state.RuinNode.Y,
            Log = state.LogLines.ToList(),
            Objects = state.Table.All.Select(OUT_ObjectSave.FromRuntime).ToList()
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(path, JsonSerializer.Serialize(dto, options));
    }

    public static OUT_State? TryLoad(OUT_Content content, string path)
    {
        if (!File.Exists(path)) return null;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var dto = JsonSerializer.Deserialize<OUT_SaveDto>(File.ReadAllText(path), options);
        if (dto == null) return null;

        var state = new OUT_State(content)
        {
            Turn = dto.Turn,
            Loops = dto.Loops,
            Memory = dto.Memory,
            Residue = dto.Residue,
            Shards = dto.Shards,
            ShardsRequired = dto.ShardsRequired <= 0 ? 3 : dto.ShardsRequired,
            Mode = Enum.TryParse<OUT_Mode>(dto.Mode, out var mode) ? mode : OUT_Mode.World,
            PlayerId = dto.PlayerId,
            RuinNode = new OUT_Pos(dto.RuinX, dto.RuinY),
            LogLines = dto.Log ?? new List<string>()
        };

        state.Table = new OUT_Table();
        foreach (OUT_ObjectSave saved in dto.Objects)
        {
            OUT_EntityDef def = content.Entity(saved.DefKey);
            var runtime = new OUT_RuntimeObject(saved.Id, def, new OUT_Pos(saved.X, saved.Y), Enum.TryParse<OUT_Scope>(saved.Scope, out var scope) ? scope : OUT_Scope.World)
            {
                WorldPos = new OUT_Pos(saved.WorldX, saved.WorldY),
                TargetName = saved.TargetName ?? string.Empty,
                ClassName = saved.ClassName ?? def.ClassName
            };

            runtime.Stats.Hp = Math.Clamp(saved.Hp, 0, runtime.Stats.MaxHp);
            runtime.Stats.Stamina = Math.Clamp(saved.Stamina, 0, runtime.Stats.MaxStamina);

            foreach (var pair in saved.Bag ?? new Dictionary<string, int>())
                runtime.OUT_Put(pair.Key, pair.Value);

            state.Table.AddExisting(runtime);
        }

        return state;
    }
}

public sealed class OUT_SaveDto
{
    public int Turn { get; set; }
    public int Loops { get; set; }
    public int Memory { get; set; }
    public int Residue { get; set; }
    public int Shards { get; set; }
    public int ShardsRequired { get; set; }
    public string Mode { get; set; } = nameof(OUT_Mode.World);
    public int PlayerId { get; set; }
    public int RuinX { get; set; }
    public int RuinY { get; set; }
    public List<string> Log { get; set; } = new();
    public List<OUT_ObjectSave> Objects { get; set; } = new();
}

public sealed class OUT_ObjectSave
{
    public int Id { get; set; }
    public string DefKey { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int WorldX { get; set; }
    public int WorldY { get; set; }
    public string Scope { get; set; } = nameof(OUT_Scope.World);
    public int Hp { get; set; }
    public int Stamina { get; set; }
    public string TargetName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public Dictionary<string, int> Bag { get; set; } = new();

    public static OUT_ObjectSave FromRuntime(OUT_RuntimeObject obj)
    {
        return new OUT_ObjectSave
        {
            Id = obj.Id,
            DefKey = obj.Def.Key,
            X = obj.Pos.X,
            Y = obj.Pos.Y,
            WorldX = obj.WorldPos.X,
            WorldY = obj.WorldPos.Y,
            Scope = obj.Scope.ToString(),
            Hp = obj.Stats.Hp,
            Stamina = obj.Stats.Stamina,
            TargetName = obj.TargetName,
            ClassName = obj.ClassName,
            Bag = new Dictionary<string, int>(obj.Bag)
        };
    }
}
