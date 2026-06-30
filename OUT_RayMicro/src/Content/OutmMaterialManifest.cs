using System.Text.Json;
using OUT_RayMicro.Runtime;

namespace OUT_RayMicro.Content;

public sealed class OutmMaterialManifest
{
    public string Id { get; set; } = "materials.unknown";
    public OutmMaterialDef[] Materials { get; set; } = Array.Empty<OutmMaterialDef>();
}

public sealed class OutmMaterialDef
{
    public string Id { get; set; } = "mat.unknown";
    public string BlenderName { get; set; } = "";
    public string Surface { get; set; } = "surface.stone";
    public float[] BaseColor { get; set; } = { 1, 1, 1, 1 };
    public float Metallic { get; set; }
    public float Roughness { get; set; } = 0.75f;
    public string BaseColorTexture { get; set; } = "";
    public string NormalTexture { get; set; } = "";
    public string EmissiveTexture { get; set; } = "";
}

public static class OutmMaterialManifestLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static OutmMaterialManifest LoadOrEmpty(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return new OutmMaterialManifest();

        string path = OutmAssetPaths.ResolveData(relativePath);
        if (!File.Exists(path))
            return new OutmMaterialManifest { Id = relativePath };

        try
        {
            OutmMaterialManifest? manifest = JsonSerializer.Deserialize<OutmMaterialManifest>(File.ReadAllText(path), Options);
            return manifest ?? new OutmMaterialManifest { Id = relativePath };
        }
        catch (Exception ex)
        {
            OutmCrashLog.Write($"material manifest failed: {path}\n{ex}");
            return new OutmMaterialManifest { Id = relativePath };
        }
    }
}
