using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace OUT_ASHBOUND;

public sealed class OUT_Content
{
    public OUT_WorldContent World { get; set; } = new();
    public List<OUT_EntityDef> Entities { get; set; } = new();
    public List<OUT_RandomEvent> Events { get; set; } = new();
    public OUT_StoryGenome Story { get; set; } = new();

    private Dictionary<string, OUT_EntityDef> byKey = new();

    public static OUT_Content Load(string path)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
        var content = new OUT_Content
        {
            World = Read<OUT_WorldContent>(Path.Combine(path, "OUT_world.json"), options),
            Entities = Read<List<OUT_EntityDef>>(Path.Combine(path, "OUT_entities.json"), options),
            Events = Read<List<OUT_RandomEvent>>(Path.Combine(path, "OUT_events.json"), options),
            Story = Read<OUT_StoryGenome>(Path.Combine(path, "OUT_story.json"), options)
        };
        content.byKey = content.Entities.ToDictionary(e => e.Key, e => e);
        return content;
    }

    public OUT_EntityDef Entity(string key)
    {
        if (byKey.TryGetValue(key, out var def)) return def;
        throw new InvalidOperationException("OUT_Content missing entity def: " + key);
    }

    private static T Read<T>(string path, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), options)
            ?? throw new InvalidOperationException("OUT_Content failed to read " + path);
    }
}

public sealed class OUT_WorldContent
{
    public int Seed { get; set; } = 2040;
    public OUT_Pos Start { get; set; } = new(3, 8);
    public string[] Map { get; set; } = Array.Empty<string>();
    public List<OUT_TileDef> Tiles { get; set; } = new();
    public List<OUT_LocationDef> Locations { get; set; } = new();
}

public sealed class OUT_TileDef
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string Ch { get; set; } = ".";
    public string Glyph { get; set; } = ".";
    public string Color { get; set; } = "Gray";
    public int Cost { get; set; } = 1;
    public bool Walkable { get; set; } = true;
    public int RandomEventChance { get; set; }
}

public sealed class OUT_LocationDef
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public OUT_Pos Pos { get; set; }
    public string Glyph { get; set; } = "O";
    public string Color { get; set; } = "White";
    public List<OUT_SpawnDef>? Spawns { get; set; }
    public List<OUT_SpawnDef>? LocalSpawns { get; set; }
}

public sealed class OUT_SpawnDef
{
    public string Def { get; set; } = "";
    public int Count { get; set; } = 1;
}

public sealed class OUT_EntityDef
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string Glyph { get; set; } = "?";
    public string Color { get; set; } = "Gray";
    public string ClassName { get; set; } = "";
    public int Hp { get; set; } = 1;
    public int Stamina { get; set; } = 10;
    public int Attack { get; set; }
    public int Sight { get; set; } = 6;
    public bool Blocking { get; set; }
    public bool IsActor { get; set; }
    public bool IsPickup { get; set; }
    public bool CanTalk { get; set; }
}

public sealed class OUT_RandomEvent
{
    public string Key { get; set; } = "";
    public string Terrain { get; set; } = "any";
    public string Text { get; set; } = "";
    public int HpDelta { get; set; }
    public int ResidueDelta { get; set; }
    public string Item { get; set; } = "";
    public int ItemCount { get; set; }
}

public sealed class OUT_StoryGenome
{
    public string Title { get; set; } = "";
    public string Formula { get; set; } = "";
    public string EntityVector { get; set; } = "";
    public List<OUT_StoryBeat> Beats { get; set; } = new();
}

public sealed class OUT_StoryBeat
{
    public string Tag { get; set; } = "";
    public string Meaning { get; set; } = "";
}
