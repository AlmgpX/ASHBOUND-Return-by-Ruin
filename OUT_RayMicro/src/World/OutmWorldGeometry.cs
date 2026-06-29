using System.Numerics;
using Raylib_cs;

namespace OUT_RayMicro.World;

public readonly struct OutmBox
{
    public readonly Vector3 Center;
    public readonly Vector3 Size;
    public readonly Color Color;
    public readonly bool Solid;

    public OutmBox(Vector3 center, Vector3 size, Color color, bool solid = true)
    {
        Center = center;
        Size = size;
        Color = color;
        Solid = solid;
    }

    public Vector3 Min => Center - Size * 0.5f;
    public Vector3 Max => Center + Size * 0.5f;
}

public sealed class OutmDemoMap
{
    public readonly List<OutmBox> Boxes = new(64);
    public Vector3 PlayerStart = new(0, 1.2f, 7);
    public Vector3 TriggerCenter = new(0, 1, -7.2f);
    public Vector3 TriggerSize = new(2.2f, 2f, 0.8f);
    public bool DoorOpen;

    private static readonly Color TriggerColor = new(80, 220, 220, 255);

    public static OutmDemoMap CreateQuakeRoom()
    {
        var map = new OutmDemoMap();
        var wall = new Color(74, 84, 96, 255);
        var floor = new Color(42, 43, 45, 255);
        var trim = new Color(120, 85, 62, 255);
        var stone = new Color(92, 98, 110, 255);

        map.Boxes.Add(new OutmBox(new Vector3(0, -0.1f, 0), new Vector3(18, 0.2f, 18), floor, solid: true));
        map.Boxes.Add(new OutmBox(new Vector3(0, 4.2f, 0), new Vector3(18, 0.2f, 18), new Color(27, 30, 37, 255), solid: false));
        map.Boxes.Add(new OutmBox(new Vector3(-9, 2, 0), new Vector3(0.4f, 4, 18), wall));
        map.Boxes.Add(new OutmBox(new Vector3(9, 2, 0), new Vector3(0.4f, 4, 18), wall));
        map.Boxes.Add(new OutmBox(new Vector3(0, 2, 9), new Vector3(18, 4, 0.4f), wall));
        map.Boxes.Add(new OutmBox(new Vector3(-4.5f, 2, -9), new Vector3(9, 4, 0.4f), wall));
        map.Boxes.Add(new OutmBox(new Vector3(4.5f, 2, -9), new Vector3(9, 4, 0.4f), wall));
        map.Boxes.Add(new OutmBox(new Vector3(0, 0.6f, -2.5f), new Vector3(3.0f, 1.2f, 2.0f), stone));
        map.Boxes.Add(new OutmBox(new Vector3(-5.0f, 0.5f, 2.0f), new Vector3(1.6f, 1.0f, 1.6f), trim));
        map.Boxes.Add(new OutmBox(new Vector3(5.0f, 0.5f, 2.0f), new Vector3(1.6f, 1.0f, 1.6f), trim));

        return map;
    }

    public void Draw()
    {
        foreach (var box in Boxes)
        {
            Raylib.DrawCubeV(box.Center, box.Size, box.Color);
            Raylib.DrawCubeWiresV(box.Center, box.Size, new Color(12, 14, 18, 210));
        }

        if (!DoorOpen)
        {
            Raylib.DrawCubeV(new Vector3(0, 2, -8.85f), new Vector3(2.1f, 4, 0.35f), new Color(120, 62, 48, 255));
            Raylib.DrawCubeWiresV(new Vector3(0, 2, -8.85f), new Vector3(2.1f, 4, 0.35f), Color.Orange);
        }

        Raylib.DrawCubeWiresV(TriggerCenter, TriggerSize, TriggerColor);
    }

    public bool IntersectsTrigger(Vector3 position)
    {
        Vector3 min = TriggerCenter - TriggerSize * 0.5f;
        Vector3 max = TriggerCenter + TriggerSize * 0.5f;
        return position.X >= min.X && position.X <= max.X &&
               position.Y >= min.Y && position.Y <= max.Y &&
               position.Z >= min.Z && position.Z <= max.Z;
    }

    public bool Collides(Vector3 point, float radius)
    {
        if (point.X < -8.55f + radius || point.X > 8.55f - radius || point.Z < -8.55f + radius || point.Z > 8.55f - radius)
            return true;

        if (!DoorOpen && point.Z < -8.5f + radius && MathF.Abs(point.X) < 1.2f + radius)
            return true;

        foreach (var box in Boxes)
        {
            if (!box.Solid || box.Size.Y < 0.35f)
                continue;

            Vector3 min = box.Min;
            Vector3 max = box.Max;
            float closestX = Math.Clamp(point.X, min.X, max.X);
            float closestZ = Math.Clamp(point.Z, min.Z, max.Z);
            float dx = point.X - closestX;
            float dz = point.Z - closestZ;
            if (dx * dx + dz * dz < radius * radius && point.Y < max.Y + 1.8f)
                return true;
        }

        return false;
    }

    public Vector3 MoveWithCollision(Vector3 position, Vector3 delta, float radius)
    {
        var next = position;
        var tryX = next + new Vector3(delta.X, 0, 0);
        if (!Collides(tryX, radius)) next = tryX;

        var tryZ = next + new Vector3(0, 0, delta.Z);
        if (!Collides(tryZ, radius)) next = tryZ;

        next.Y = MathF.Max(1.2f, next.Y + delta.Y);
        return next;
    }
}
