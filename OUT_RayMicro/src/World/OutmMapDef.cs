using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Raylib_cs;
using OUT_RayMicro.Core;
using OUT_RayMicro.Gameplay;
using OUT_RayMicro.Runtime;

namespace OUT_RayMicro.World;

public sealed class OutmMapDef
{
    public string Id { get; set; } = "map.test_room";
    public string DisplayName { get; set; } = "Test Room";
    public float[] PlayerStart { get; set; } = { 0.0f, 1.2f, 7.0f };
    public OutmBoxDef[] Boxes { get; set; } = Array.Empty<OutmBoxDef>();
    public OutmDoorDef[] Doors { get; set; } = Array.Empty<OutmDoorDef>();
    public OutmTriggerDef[] Triggers { get; set; } = Array.Empty<OutmTriggerDef>();
    public OutmPickupDef[] Pickups { get; set; } = Array.Empty<OutmPickupDef>();
    public OutmMeshRefDef[] Meshes { get; set; } = Array.Empty<OutmMeshRefDef>();

    public Vector3 PlayerStartVector => ToVector3(PlayerStart, new Vector3(0, 1.2f, 7));

    public static Vector3 ToVector3(float[]? value, Vector3 fallback)
    {
        if (value == null || value.Length < 3)
            return fallback;

        return new Vector3(value[0], value[1], value[2]);
    }

    public static Color ToColor(int[]? value, Color fallback)
    {
        if (value == null || value.Length < 3)
            return fallback;

        byte r = ClampByte(value[0]);
        byte g = ClampByte(value[1]);
        byte b = ClampByte(value[2]);
        byte a = value.Length >= 4 ? ClampByte(value[3]) : (byte)255;
        return new Color(r, g, b, a);
    }

    private static byte ClampByte(int value) => (byte)Math.Clamp(value, 0, 255);
}

public sealed class OutmBoxDef
{
    public string Id { get; set; } = "box";
    public float[] Center { get; set; } = { 0, 0, 0 };
    public float[] Size { get; set; } = { 1, 1, 1 };
    public int[] Color { get; set; } = { 96, 96, 96, 255 };
    public bool Solid { get; set; } = true;
    public string Surface { get; set; } = "surface.stone";
}

public sealed class OutmDoorDef
{
    public string Id { get; set; } = "door";
    public float[] Center { get; set; } = { 0, 2, -8.85f };
    public float[] Size { get; set; } = { 2.1f, 4.0f, 0.35f };
    public int[] Color { get; set; } = { 120, 62, 48, 255 };
    public bool StartsOpen { get; set; }
    public string Surface { get; set; } = "surface.wood";
}

public sealed class OutmTriggerDef
{
    public string Id { get; set; } = "trigger";
    public string Kind { get; set; } = "door_toggle";
    public string Target { get; set; } = "door.main";
    public float[] Center { get; set; } = { 0, 1, -7.2f };
    public float[] Size { get; set; } = { 2.2f, 2.0f, 0.8f };
}

public sealed class OutmPickupDef
{
    public string Id { get; set; } = "pickup";
    public OutmPickupKind Kind { get; set; } = OutmPickupKind.Health;
    public float[] Position { get; set; } = { 0, 0.5f, 0 };
    public float Radius { get; set; } = 0.65f;
    public int Amount { get; set; } = 25;
    public OutmArmorTier ArmorTier { get; set; } = OutmArmorTier.Green;
    public string Surface { get; set; } = "surface.stone";
}

public sealed class OutmMeshRefDef
{
    public string Id { get; set; } = "mesh";
    public string Path { get; set; } = "";
    public string MaterialManifest { get; set; } = "";
    public float[] Position { get; set; } = { 0, 0, 0 };
    public float[] Rotation { get; set; } = { 0, 0, 0 };
    public float[] Scale { get; set; } = { 1, 1, 1 };
    public string Collision { get; set; } = "none";
    public string Surface { get; set; } = "surface.stone";
}

public static class OutmMapLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static OutmMapDef LoadOrDefault(string relativePath)
    {
        string path = OutmAssetPaths.ResolveData(relativePath);
        if (!File.Exists(path))
        {
            OutmCrashLog.Write($"outmap missing, using fallback: {path}");
            return CreateFallbackDef();
        }

        try
        {
            OutmCrashLog.Write($"outmap load: {path}");
            OutmMapDef? def = JsonSerializer.Deserialize<OutmMapDef>(File.ReadAllText(path), Options);
            return def ?? CreateFallbackDef();
        }
        catch (Exception ex)
        {
            OutmCrashLog.Write($"outmap failed, using fallback: {path}\n{ex}");
            return CreateFallbackDef();
        }
    }

    public static OutmDemoMap BuildDemoMap(OutmMapDef def)
    {
        var map = new OutmDemoMap
        {
            Id = def.Id,
            DisplayName = def.DisplayName,
            PlayerStart = def.PlayerStartVector
        };

        foreach (OutmBoxDef box in def.Boxes)
        {
            map.Boxes.Add(new OutmBox(
                box.Id,
                OutmMapDef.ToVector3(box.Center, Vector3.Zero),
                OutmMapDef.ToVector3(box.Size, Vector3.One),
                OutmMapDef.ToColor(box.Color, new Color(96, 96, 96, 255)),
                box.Solid,
                box.Surface));
        }

        foreach (OutmDoorDef door in def.Doors)
        {
            map.Doors.Add(new OutmDoorRuntime(
                door.Id,
                OutmMapDef.ToVector3(door.Center, new Vector3(0, 2, -8.85f)),
                OutmMapDef.ToVector3(door.Size, new Vector3(2.1f, 4, 0.35f)),
                OutmMapDef.ToColor(door.Color, new Color(120, 62, 48, 255)),
                door.StartsOpen,
                door.Surface));
        }

        foreach (OutmTriggerDef trigger in def.Triggers)
        {
            map.Triggers.Add(new OutmTriggerRuntime(
                trigger.Id,
                trigger.Kind,
                trigger.Target,
                OutmMapDef.ToVector3(trigger.Center, new Vector3(0, 1, -7.2f)),
                OutmMapDef.ToVector3(trigger.Size, new Vector3(2.2f, 2, 0.8f))));
        }

        return map;
    }

    private static OutmMapDef CreateFallbackDef()
    {
        return new OutmMapDef
        {
            Id = "map.fallback_room",
            DisplayName = "Fallback Room",
            PlayerStart = new[] { 0.0f, 1.2f, 7.0f },
            Boxes = new[]
            {
                new OutmBoxDef { Id = "floor", Center = new[] { 0f, -0.1f, 0f }, Size = new[] { 18f, 0.2f, 18f }, Color = new[] { 42, 43, 45, 255 }, Solid = true, Surface = "surface.stone" },
                new OutmBoxDef { Id = "crate", Center = new[] { -5f, 0.5f, 2f }, Size = new[] { 1.6f, 1f, 1.6f }, Color = new[] { 120, 85, 62, 255 }, Solid = true, Surface = "surface.wood" }
            },
            Doors = new[]
            {
                new OutmDoorDef { Id = "door.main", Center = new[] { 0f, 2f, -8.85f }, Size = new[] { 2.1f, 4f, 0.35f }, Color = new[] { 120, 62, 48, 255 }, StartsOpen = false, Surface = "surface.wood" }
            },
            Triggers = new[]
            {
                new OutmTriggerDef { Id = "trigger.door.main", Kind = "door_toggle", Target = "door.main", Center = new[] { 0f, 1f, -7.2f }, Size = new[] { 2.2f, 2f, 0.8f } }
            },
            Pickups = new[]
            {
                new OutmPickupDef { Id = "pickup.health.demo", Kind = OutmPickupKind.Health, Position = new[] { 2.5f, 0.45f, 2.0f }, Radius = 0.75f, Amount = 25, Surface = "surface.stone" }
            }
        };
    }
}
