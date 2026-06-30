using System.Numerics;
using Raylib_cs;

namespace OUT_RayMicro.World;

public readonly struct OutmBox
{
    public readonly string Id;
    public readonly Vector3 Center;
    public readonly Vector3 Size;
    public readonly Color Color;
    public readonly bool Solid;

    public OutmBox(string id, Vector3 center, Vector3 size, Color color, bool solid = true)
    {
        Id = id;
        Center = center;
        Size = size;
        Color = color;
        Solid = solid;
    }

    public OutmBox(Vector3 center, Vector3 size, Color color, bool solid = true)
        : this("box", center, size, color, solid)
    {
    }

    public Vector3 Min => Center - Size * 0.5f;
    public Vector3 Max => Center + Size * 0.5f;
}

public struct OutmDoorRuntime
{
    public string Id;
    public Vector3 Center;
    public Vector3 Size;
    public Color Color;
    public bool Open;

    public OutmDoorRuntime(string id, Vector3 center, Vector3 size, Color color, bool open)
    {
        Id = id;
        Center = center;
        Size = size;
        Color = color;
        Open = open;
    }

    public Vector3 Min => Center - Size * 0.5f;
    public Vector3 Max => Center + Size * 0.5f;
}

public readonly struct OutmTriggerRuntime
{
    public readonly string Id;
    public readonly string Kind;
    public readonly string Target;
    public readonly Vector3 Center;
    public readonly Vector3 Size;

    public OutmTriggerRuntime(string id, string kind, string target, Vector3 center, Vector3 size)
    {
        Id = id;
        Kind = kind;
        Target = target;
        Center = center;
        Size = size;
    }

    public Vector3 Min => Center - Size * 0.5f;
    public Vector3 Max => Center + Size * 0.5f;
}

public sealed class OutmDemoMap
{
    public readonly List<OutmBox> Boxes = new(64);
    public readonly List<OutmDoorRuntime> Doors = new(8);
    public readonly List<OutmTriggerRuntime> Triggers = new(16);
    public string Id = "map.runtime";
    public string DisplayName = "Runtime Map";
    public Vector3 PlayerStart = new(0, 1.2f, 7);

    private static readonly Color TriggerColor = new(80, 220, 220, 255);

    public static OutmDemoMap CreateQuakeRoom()
    {
        return OutmMapLoader.BuildDemoMap(OutmMapLoader.LoadOrDefault("maps/test_room.outmap.json"));
    }

    public void Draw()
    {
        foreach (var box in Boxes)
        {
            Raylib.DrawCubeV(box.Center, box.Size, box.Color);
            Raylib.DrawCubeWiresV(box.Center, box.Size, new Color(12, 14, 18, 210));
        }

        for (int i = 0; i < Doors.Count; i++)
        {
            OutmDoorRuntime door = Doors[i];
            if (door.Open)
                continue;

            Raylib.DrawCubeV(door.Center, door.Size, door.Color);
            Raylib.DrawCubeWiresV(door.Center, door.Size, Color.Orange);
        }

        foreach (OutmTriggerRuntime trigger in Triggers)
            Raylib.DrawCubeWiresV(trigger.Center, trigger.Size, TriggerColor);
    }

    public bool TryGetEnteredTrigger(Vector3 position, out OutmTriggerRuntime trigger)
    {
        for (int i = 0; i < Triggers.Count; i++)
        {
            trigger = Triggers[i];
            if (PointInsideBox(position, trigger.Min, trigger.Max))
                return true;
        }

        trigger = default;
        return false;
    }

    public bool IntersectsTrigger(Vector3 position)
    {
        return TryGetEnteredTrigger(position, out _);
    }

    public bool TryToggleDoor(string doorId)
    {
        for (int i = 0; i < Doors.Count; i++)
        {
            OutmDoorRuntime door = Doors[i];
            if (!string.Equals(door.Id, doorId, StringComparison.OrdinalIgnoreCase))
                continue;

            door.Open = !door.Open;
            Doors[i] = door;
            return door.Open;
        }

        return false;
    }

    public bool TrySetDoorOpen(string doorId, bool open)
    {
        for (int i = 0; i < Doors.Count; i++)
        {
            OutmDoorRuntime door = Doors[i];
            if (!string.Equals(door.Id, doorId, StringComparison.OrdinalIgnoreCase))
                continue;

            door.Open = open;
            Doors[i] = door;
            return true;
        }

        return false;
    }

    public bool Collides(Vector3 point, float radius)
    {
        foreach (OutmDoorRuntime door in Doors)
        {
            if (door.Open)
                continue;

            if (SphereAgainstBoxXZ(point, radius, door.Min, door.Max) && point.Y < door.Max.Y + 1.8f)
                return true;
        }

        foreach (var box in Boxes)
        {
            if (!box.Solid || box.Size.Y < 0.35f)
                continue;

            if (SphereAgainstBoxXZ(point, radius, box.Min, box.Max) && point.Y < box.Max.Y + 1.8f)
                return true;
        }

        return false;
    }

    public Vector3 MoveWithCollision(Vector3 position, Vector3 delta, float radius, float floorHeight)
    {
        var next = position;
        var tryX = next + new Vector3(delta.X, 0, 0);
        if (!Collides(tryX, radius)) next = tryX;

        var tryZ = next + new Vector3(0, 0, delta.Z);
        if (!Collides(tryZ, radius)) next = tryZ;

        next.Y = MathF.Max(floorHeight, next.Y + delta.Y);
        return next;
    }

    private static bool PointInsideBox(Vector3 point, Vector3 min, Vector3 max)
    {
        return point.X >= min.X && point.X <= max.X &&
               point.Y >= min.Y && point.Y <= max.Y &&
               point.Z >= min.Z && point.Z <= max.Z;
    }

    private static bool SphereAgainstBoxXZ(Vector3 point, float radius, Vector3 min, Vector3 max)
    {
        float closestX = Math.Clamp(point.X, min.X, max.X);
        float closestZ = Math.Clamp(point.Z, min.Z, max.Z);
        float dx = point.X - closestX;
        float dz = point.Z - closestZ;
        return dx * dx + dz * dz < radius * radius;
    }
}
