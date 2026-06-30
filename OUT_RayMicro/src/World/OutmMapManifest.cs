using System.Text.Json;
using System.Text.Json.Serialization;
using OUT_RayMicro.Runtime;

namespace OUT_RayMicro.World;

public sealed class OutmMapManifest
{
    public string DefaultMap { get; set; } = "map.test_room";
    public OutmMapManifestEntry[] Maps { get; set; } = Array.Empty<OutmMapManifestEntry>();

    public OutmMapManifestEntry FindDefault()
    {
        for (int i = 0; i < Maps.Length; i++)
        {
            if (string.Equals(Maps[i].Id, DefaultMap, StringComparison.OrdinalIgnoreCase))
                return Maps[i];
        }

        if (Maps.Length > 0)
            return Maps[0];

        return new OutmMapManifestEntry
        {
            Id = "map.test_room",
            DisplayName = "Test Room OUTMAP",
            Path = "maps/test_room.outmap.json"
        };
    }
}

public sealed class OutmMapManifestEntry
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Path { get; set; } = "";
}

public static class OutmMapManifestLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static OutmMapManifest LoadOrDefault(string relativePath = "maps/maps.json")
    {
        string path = OutmAssetPaths.ResolveData(relativePath);
        if (!File.Exists(path))
        {
            OutmCrashLog.Write($"map manifest missing, using fallback: {path}");
            return CreateFallback();
        }

        try
        {
            OutmCrashLog.Write($"map manifest load: {path}");
            OutmMapManifest? manifest = JsonSerializer.Deserialize<OutmMapManifest>(File.ReadAllText(path), Options);
            return manifest ?? CreateFallback();
        }
        catch (Exception ex)
        {
            OutmCrashLog.Write($"map manifest failed, using fallback: {path}\n{ex}");
            return CreateFallback();
        }
    }

    private static OutmMapManifest CreateFallback()
    {
        return new OutmMapManifest
        {
            DefaultMap = "map.test_room",
            Maps = new[]
            {
                new OutmMapManifestEntry
                {
                    Id = "map.test_room",
                    DisplayName = "Test Room OUTMAP",
                    Path = "maps/test_room.outmap.json"
                }
            }
        };
    }
}
