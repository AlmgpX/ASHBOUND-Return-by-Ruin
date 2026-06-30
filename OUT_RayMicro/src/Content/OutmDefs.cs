using System.Text.Json;
using System.Text.Json.Serialization;
using OUT_RayMicro.Runtime;

namespace OUT_RayMicro.Content;

public sealed class OutmContentRegistry
{
    private readonly Dictionary<string, OutmWeaponDef> weapons = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, OutmItemDef> items = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, OutmActorDef> actors = new(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public IReadOnlyDictionary<string, OutmWeaponDef> Weapons => weapons;
    public IReadOnlyDictionary<string, OutmItemDef> Items => items;
    public IReadOnlyDictionary<string, OutmActorDef> Actors => actors;

    public static OutmContentRegistry LoadDefault()
    {
        var registry = new OutmContentRegistry();
        registry.LoadFolder("defs/weapons", registry.weapons);
        registry.LoadFolder("defs/items", registry.items);
        registry.LoadFolder("defs/actors", registry.actors);
        registry.EnsureFallbacks();
        return registry;
    }

    public OutmWeaponDef GetWeapon(string id)
    {
        return weapons.TryGetValue(id, out OutmWeaponDef? def) ? def : OutmWeaponDef.FallbackRevolver;
    }

    private void LoadFolder<T>(string relativeFolder, Dictionary<string, T> target) where T : OutmDef
    {
        string folder = OutmAssetPaths.ResolveData(relativeFolder);
        if (!Directory.Exists(folder))
            return;

        foreach (string path in Directory.GetFiles(folder, "*.json", SearchOption.TopDirectoryOnly))
        {
            T? def = JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions);
            if (def == null || string.IsNullOrWhiteSpace(def.Id))
                continue;

            target[def.Id] = def;
        }
    }

    private void EnsureFallbacks()
    {
        weapons.TryAdd(OutmWeaponDef.FallbackRevolver.Id, OutmWeaponDef.FallbackRevolver);
    }
}

public abstract class OutmDef
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
}

public sealed class OutmWeaponDef : OutmDef
{
    public float Cooldown { get; set; } = 0.24f;
    public float ProjectileSpeed { get; set; } = 38.0f;
    public float ProjectileLife { get; set; } = 4.0f;
    public float ProjectileRadius { get; set; } = 0.08f;
    public int MaxBounces { get; set; } = 2;
    public float BounceEnergy { get; set; } = 0.72f;
    public string FireSound { get; set; } = "shot";

    public static OutmWeaponDef FallbackRevolver => new()
    {
        Id = "weapon.revolver",
        DisplayName = "Revolver",
        Cooldown = 0.24f,
        ProjectileSpeed = 38.0f,
        ProjectileLife = 4.0f,
        ProjectileRadius = 0.08f,
        MaxBounces = 2,
        BounceEnergy = 0.72f,
        FireSound = "shot"
    };
}

public sealed class OutmItemDef : OutmDef
{
    public string Kind { get; set; } = "generic";
    public int Value { get; set; }
    public string Effect { get; set; } = "none";
}

public sealed class OutmActorDef : OutmDef
{
    public string Faction { get; set; } = "neutral";
    public int Health { get; set; } = 100;
    public string WeaponId { get; set; } = "";
}
