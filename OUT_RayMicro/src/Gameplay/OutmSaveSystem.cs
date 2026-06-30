using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using OUT_RayMicro.Core;
using OUT_RayMicro.Runtime;
using OUT_RayMicro.World;

namespace OUT_RayMicro.Gameplay;

public sealed class OutmSaveSnapshot
{
    public int Version { get; set; } = 1;
    public string MapId { get; set; } = "";
    public int Tick { get; set; }
    public float Time { get; set; }
    public OutmPlayerSaveSnapshot Player { get; set; } = new();
    public OutmDoorSaveSnapshot[] Doors { get; set; } = Array.Empty<OutmDoorSaveSnapshot>();
}

public sealed class OutmPlayerSaveSnapshot
{
    public OutmVector3Save Position { get; set; }
    public OutmVector3Save Velocity { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public int Health { get; set; }
    public int Armor { get; set; }
    public int Mana { get; set; }
    public int MaxHealth { get; set; }
    public int MaxArmor { get; set; }
    public int MaxMana { get; set; }
    public OutmArmorTier ArmorTier { get; set; }
    public bool InputLocked { get; set; }
}

public sealed class OutmDoorSaveSnapshot
{
    public string Id { get; set; } = "";
    public bool Open { get; set; }
}

public readonly struct OutmVector3Save
{
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }

    public OutmVector3Save(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Vector3 ToVector3() => new(X, Y, Z);
    public static OutmVector3Save From(Vector3 value) => new(value.X, value.Y, value.Z);
}

public static class OutmSaveSystem
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static OutmSaveSnapshot Capture(OutmWorld world, OutmDemoMap map, Vector3 playerPosition, Vector3 playerVelocity, float yaw, float pitch)
    {
        OutmPlayerVitals vitals = world.PlayerVitals;
        var doors = new OutmDoorSaveSnapshot[map.Doors.Count];
        for (int i = 0; i < map.Doors.Count; i++)
        {
            OutmDoorRuntime door = map.Doors[i];
            doors[i] = new OutmDoorSaveSnapshot
            {
                Id = door.Id,
                Open = door.Open
            };
        }

        return new OutmSaveSnapshot
        {
            Version = 1,
            MapId = map.Id,
            Tick = world.Tick,
            Time = world.Time,
            Player = new OutmPlayerSaveSnapshot
            {
                Position = OutmVector3Save.From(playerPosition),
                Velocity = OutmVector3Save.From(playerVelocity),
                Yaw = yaw,
                Pitch = pitch,
                Health = vitals.Health,
                Armor = vitals.Armor,
                Mana = vitals.Mana,
                MaxHealth = vitals.MaxHealth,
                MaxArmor = vitals.MaxArmor,
                MaxMana = vitals.MaxMana,
                ArmorTier = vitals.ArmorTier,
                InputLocked = vitals.IsDead
            },
            Doors = doors
        };
    }

    public static void ApplyWorldState(OutmWorld world, OutmDemoMap map, OutmSaveSnapshot snapshot)
    {
        OutmPlayerSaveSnapshot player = snapshot.Player;
        world.PlayerVitals = new OutmPlayerVitals
        {
            Health = player.Health,
            Armor = player.Armor,
            Mana = player.Mana,
            MaxHealth = player.MaxHealth,
            MaxArmor = player.MaxArmor,
            MaxMana = player.MaxMana,
            ArmorTier = player.ArmorTier,
            IsDead = player.InputLocked
        };

        world.Time = snapshot.Time;
        world.Tick = snapshot.Tick;
        world.Transforms.Set(world.PlayerEntity, player.Position.ToVector3(), new Vector3(0.0f, player.Yaw, player.Pitch));

        for (int i = 0; i < snapshot.Doors.Length; i++)
            map.TrySetDoorOpen(snapshot.Doors[i].Id, snapshot.Doors[i].Open);
    }

    public static void SaveToDisk(OutmSaveSnapshot snapshot, string relativePath = "saves/quicksave.outsave.json")
    {
        string path = OutmAssetPaths.ResolveData(relativePath);
        string? folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(folder))
            Directory.CreateDirectory(folder);

        File.WriteAllText(path, JsonSerializer.Serialize(snapshot, Options));
        OutmCrashLog.Write($"save written: {path}");
    }

    public static bool TryLoadFromDisk(out OutmSaveSnapshot snapshot, string relativePath = "saves/quicksave.outsave.json")
    {
        string path = OutmAssetPaths.ResolveData(relativePath);
        if (!File.Exists(path))
        {
            snapshot = new OutmSaveSnapshot();
            return false;
        }

        try
        {
            OutmSaveSnapshot? loaded = JsonSerializer.Deserialize<OutmSaveSnapshot>(File.ReadAllText(path), Options);
            snapshot = loaded ?? new OutmSaveSnapshot();
            return loaded != null;
        }
        catch (Exception ex)
        {
            OutmCrashLog.Write($"save load failed: {path}\n{ex}");
            snapshot = new OutmSaveSnapshot();
            return false;
        }
    }
}
