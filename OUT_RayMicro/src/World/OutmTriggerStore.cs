using System.Numerics;
using OUT_RayMicro.Core;
using OUT_RayMicro.Physics;

namespace OUT_RayMicro.World;

public struct OutmTriggerRecord
{
    public EntityId Entity;
    public OutmBodyHandle Body;
    public string Id;
    public string Kind;
    public string Target;
    public Vector3 Center;
    public Vector3 Size;

    public Vector3 Min => Center - Size * 0.5f;
    public Vector3 Max => Center + Size * 0.5f;
}

public sealed class OutmTriggerStore
{
    private readonly OutBuffer<OutmTriggerRecord> triggers = new(128);
    private readonly Dictionary<string, int> byId = new(StringComparer.OrdinalIgnoreCase);

    public int Count => triggers.Count;
    public OutBuffer<OutmTriggerRecord> Triggers => triggers;

    public void Add(EntityId entity, string id, string kind, string target, Vector3 center, Vector3 size)
    {
        if (string.IsNullOrWhiteSpace(id))
            id = $"trigger.{triggers.Count}";

        var record = new OutmTriggerRecord
        {
            Entity = entity,
            Body = OutmBodyHandle.None,
            Id = id,
            Kind = string.IsNullOrWhiteSpace(kind) ? "door_toggle" : kind,
            Target = target,
            Center = center,
            Size = size
        };

        byId[id] = triggers.Count;
        triggers.Add(record);
    }

    public bool TryBindBody(string id, OutmBodyHandle body)
    {
        if (!byId.TryGetValue(id, out int index))
            return false;

        OutmTriggerRecord trigger = triggers.Items[index];
        trigger.Body = body;
        triggers.Items[index] = trigger;
        return true;
    }

    public bool TryGet(string id, out OutmTriggerRecord trigger)
    {
        if (!string.IsNullOrWhiteSpace(id) && byId.TryGetValue(id, out int index))
        {
            trigger = triggers.Items[index];
            return true;
        }

        trigger = default;
        return false;
    }

    public bool TryGetEntered(Vector3 position, out OutmTriggerRecord trigger)
    {
        for (int i = 0; i < triggers.Count; i++)
        {
            trigger = triggers.Items[i];
            if (PointInsideBox(position, trigger.Min, trigger.Max))
                return true;
        }

        trigger = default;
        return false;
    }

    private static bool PointInsideBox(Vector3 point, Vector3 min, Vector3 max)
    {
        return point.X >= min.X && point.X <= max.X && point.Y >= min.Y && point.Y <= max.Y && point.Z >= min.Z && point.Z <= max.Z;
    }
}
