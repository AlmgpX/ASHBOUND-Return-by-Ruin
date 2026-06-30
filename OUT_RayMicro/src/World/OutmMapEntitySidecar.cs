using System.Text.Json;
using System.Text.Json.Serialization;
using OUT_RayMicro.Runtime;

namespace OUT_RayMicro.World;

public sealed class OutmMapEntityLump
{
    public OutmMapEntityDef[] Entities { get; set; } = Array.Empty<OutmMapEntityDef>();
}

public static class OutmMapEntitySidecar
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static OutmMapEntityDef[] LoadEntitiesOrEmpty(string relativePath)
    {
        string path = OutmAssetPaths.ResolveData(relativePath);
        if (!File.Exists(path))
            return Array.Empty<OutmMapEntityDef>();

        try
        {
            OutmMapEntityLump? lump = JsonSerializer.Deserialize<OutmMapEntityLump>(File.ReadAllText(path), Options);
            return lump?.Entities ?? Array.Empty<OutmMapEntityDef>();
        }
        catch (Exception ex)
        {
            OutmCrashLog.Write($"entity lump load failed: {path}\n{ex}");
            return Array.Empty<OutmMapEntityDef>();
        }
    }
}
