using System.Numerics;
using Raylib_cs;
using OUT_RayMicro.Core;
using OUT_RayMicro.Physics;

namespace OUT_RayMicro.World;

public struct OutmDoorRecord
{
    public EntityId Entity;
    public OutmBodyHandle Body;
    public string Id;
    public Vector3 Center;
    public Vector3 Size;
    public Color Color;
    public bool Open;
    public string SurfaceId;
}

public sealed class OutmDoorStore
{
    private readonly List<OutmDoorRecord> doors = new(32);
    private readonly Dictionary<string, int> byId = new(StringComparer.OrdinalIgnoreCase);

    public int Count => doors.Count;
    public IReadOnlyList<OutmDoorRecord> Doors => doors;

    public void Add(EntityId entity, string id, Vector3 center, Vector3 size, Color color, bool open, string surfaceId)
    {
        if (string.IsNullOrWhiteSpace(id)) id = $"door.{doors.Count}";
        var record = new OutmDoorRecord
        {
            Entity = entity,
            Body = OutmBodyHandle.None,
            Id = id,
            Center = center,
            Size = size,
            Color = color,
            Open = open,
            SurfaceId = string.IsNullOrWhiteSpace(surfaceId) ? "surface.wood" : surfaceId
        };
        byId[id] = doors.Count;
        doors.Add(record);
    }

    public bool TryBindBody(string id, OutmBodyHandle body)
    {
        if (!byId.TryGetValue(id, out int index)) return false;
        OutmDoorRecord door = doors[index];
        door.Body = body;
        doors[index] = door;
        return true;
    }

    public bool TryGet(string id, out OutmDoorRecord door)
    {
        if (!string.IsNullOrWhiteSpace(id) && byId.TryGetValue(id, out int index))
        {
            door = doors[index];
            return true;
        }
        door = default;
        return false;
    }

    public bool TrySetOpen(string id, bool open)
    {
        if (!byId.TryGetValue(id, out int index)) return false;
        OutmDoorRecord door = doors[index];
        door.Open = open;
        doors[index] = door;
        return true;
    }

    public bool TryToggle(string id, out bool open)
    {
        if (!byId.TryGetValue(id, out int index))
        {
            open = false;
            return false;
        }
        OutmDoorRecord door = doors[index];
        door.Open = !door.Open;
        doors[index] = door;
        open = door.Open;
        return true;
    }

    public void SyncToDemoMap(OutmDemoMap map)
    {
        for (int i = 0; i < doors.Count; i++)
            map.TrySetDoorOpen(doors[i].Id, doors[i].Open);
    }
}
