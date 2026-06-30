using System.Numerics;
using OUT_RayMicro.Core;

namespace OUT_RayMicro.Physics;

[Flags]
public enum OutmBodyFlags : ushort
{
    None = 0,
    Active = 1 << 0,
    Static = 1 << 1,
    Sensor = 1 << 2,
    Door = 1 << 3,
    Dynamic = 1 << 4,
    Kinematic = 1 << 5,
    Dirty = 1 << 6
}

public enum OutmPhysicsSourceKind : byte
{
    None,
    AuthoringBody,
    AuthoringSensor,
    RuntimeDynamic,
    RuntimeKinematic
}

public readonly struct OutmBodyHandle
{
    public readonly int Id;
    public bool IsValid => Id >= 0;
    public OutmBodyHandle(int id) { Id = id; }
    public override string ToString() => IsValid ? $"B{Id}" : "None";
    public static readonly OutmBodyHandle None = new(-1);
}

public readonly struct OutmShapeHandle
{
    public readonly int Id;
    public bool IsValid => Id >= 0;
    public OutmShapeHandle(int id) { Id = id; }
    public override string ToString() => IsValid ? $"S{Id}" : "None";
    public static readonly OutmShapeHandle None = new(-1);
}

public struct OutmBody
{
    public int BodyId;
    public int ShapeId;
    public int ProxyId;
    public OutmPhysicsSourceKind SourceKind;
    public int SourceIndex;
    public Vector3 Position;
    public OutmBodyFlags Flags;
}

public struct OutmShape
{
    public int ShapeId;
    public OutmPhysicsShapeKind Kind;
    public Vector3 Size;
    public string SurfaceId;
    public ushort CollisionFlags;
}

public struct OutmBroadphaseProxy
{
    public int ProxyId;
    public int BodyId;
    public int ShapeId;
    public Vector3 Min;
    public Vector3 Max;
    public bool Active;
}

public struct OutmActivePair
{
    public int A;
    public int B;
}

public struct OutmContactManifold
{
    public int BodyA;
    public int BodyB;
    public Vector3 Point;
    public Vector3 Normal;
    public float Penetration;
}

public struct OutmTriggerOverlap
{
    public int SensorBodyId;
    public int OtherBodyId;
}

public sealed class OutmPhysicsRuntime
{
    public readonly OutBuffer<OutmBody> Bodies;
    public readonly OutBuffer<OutmShape> Shapes;
    public readonly OutBuffer<OutmBroadphaseProxy> Proxies;
    public readonly OutBuffer<OutmActivePair> ActivePairs;
    public readonly OutBuffer<OutmContactManifold> Contacts;
    public readonly OutBuffer<OutmTriggerOverlap> TriggerOverlaps;
    public readonly OutBuffer<int> DirtyBodies;
    public readonly OutBuffer<int> DynamicBodies;
    public readonly OutBuffer<int> KinematicBodies;

    public int BodyCount => Bodies.Count;
    public int ShapeCount => Shapes.Count;
    public int ProxyCount => Proxies.Count;
    public int PairCount => ActivePairs.Count;
    public int ContactCount => Contacts.Count;
    public int TriggerOverlapCount => TriggerOverlaps.Count;

    public OutmPhysicsRuntime(int bodyCapacity = 4096, int shapeCapacity = 8192, int proxyCapacity = 8192, int pairCapacity = 16384, int contactCapacity = 4096, int triggerOverlapCapacity = 4096, int dirtyCapacity = 2048, int dynamicCapacity = 2048, int kinematicCapacity = 1024)
    {
        Bodies = new OutBuffer<OutmBody>(bodyCapacity);
        Shapes = new OutBuffer<OutmShape>(shapeCapacity);
        Proxies = new OutBuffer<OutmBroadphaseProxy>(proxyCapacity);
        ActivePairs = new OutBuffer<OutmActivePair>(pairCapacity);
        Contacts = new OutBuffer<OutmContactManifold>(contactCapacity);
        TriggerOverlaps = new OutBuffer<OutmTriggerOverlap>(triggerOverlapCapacity);
        DirtyBodies = new OutBuffer<int>(dirtyCapacity);
        DynamicBodies = new OutBuffer<int>(dynamicCapacity);
        KinematicBodies = new OutBuffer<int>(kinematicCapacity);
    }

    public OutmShapeHandle AddShape(OutmPhysicsShapeKind kind, Vector3 size, string surfaceId, ushort collisionFlags = 0)
    {
        int id = Shapes.Add(new OutmShape { ShapeId = Shapes.Count, Kind = kind, Size = SanitizeSize(size), SurfaceId = string.IsNullOrWhiteSpace(surfaceId) ? "surface.stone" : surfaceId, CollisionFlags = collisionFlags });
        return new OutmShapeHandle(id);
    }

    public OutmBodyHandle AddBody(OutmShapeHandle shape, Vector3 position, OutmBodyFlags flags, OutmPhysicsSourceKind sourceKind, int sourceIndex)
    {
        if (!shape.IsValid)
            return OutmBodyHandle.None;

        int proxyId = Proxies.Count;
        int bodyId = Bodies.Add(new OutmBody { BodyId = Bodies.Count, ShapeId = shape.Id, ProxyId = proxyId, SourceKind = sourceKind, SourceIndex = sourceIndex, Position = position, Flags = flags | OutmBodyFlags.Dirty });
        Proxies.Add(new OutmBroadphaseProxy { ProxyId = proxyId, BodyId = bodyId, ShapeId = shape.Id, Active = (flags & OutmBodyFlags.Active) != 0 });

        if ((flags & OutmBodyFlags.Dynamic) != 0) DynamicBodies.Add(bodyId);
        if ((flags & OutmBodyFlags.Kinematic) != 0) KinematicBodies.Add(bodyId);

        MarkDirty(bodyId);
        return new OutmBodyHandle(bodyId);
    }

    public void SetBodyActive(OutmBodyHandle handle, bool active)
    {
        if (!IsValidBody(handle)) return;
        ref OutmBody body = ref Bodies[handle.Id];
        bool currentlyActive = (body.Flags & OutmBodyFlags.Active) != 0;
        if (currentlyActive == active) return;
        if (active) body.Flags |= OutmBodyFlags.Active;
        else body.Flags &= ~OutmBodyFlags.Active;
        body.Flags |= OutmBodyFlags.Dirty;
        MarkDirty(handle.Id);
    }

    public void SetBodyPosition(OutmBodyHandle handle, Vector3 position)
    {
        if (!IsValidBody(handle)) return;
        ref OutmBody body = ref Bodies[handle.Id];
        if (Vector3.DistanceSquared(body.Position, position) <= 0.000001f) return;
        body.Position = position;
        body.Flags |= OutmBodyFlags.Dirty;
        MarkDirty(handle.Id);
    }

    public void FlushDirtyProxies()
    {
        for (int i = 0; i < DirtyBodies.Count; i++)
        {
            int bodyId = DirtyBodies.Items[i];
            if ((uint)bodyId >= (uint)Bodies.Count) continue;
            ref OutmBody body = ref Bodies[bodyId];
            ref OutmShape shape = ref Shapes[body.ShapeId];
            Vector3 half = shape.Size * 0.5f;
            int proxyId = body.ProxyId;
            Proxies[proxyId] = new OutmBroadphaseProxy { ProxyId = proxyId, BodyId = bodyId, ShapeId = body.ShapeId, Min = body.Position - half, Max = body.Position + half, Active = (body.Flags & OutmBodyFlags.Active) != 0 };
            body.Flags &= ~OutmBodyFlags.Dirty;
        }
        DirtyBodies.Clear();
    }

    public void BuildPairs()
    {
        ActivePairs.Clear();
        Contacts.Clear();
        TriggerOverlaps.Clear();
        for (int a = 0; a < Proxies.Count; a++)
        {
            OutmBroadphaseProxy pa = Proxies.Items[a];
            if (!pa.Active) continue;
            for (int b = a + 1; b < Proxies.Count; b++)
            {
                OutmBroadphaseProxy pb = Proxies.Items[b];
                if (!pb.Active || !AabbOverlap(pa.Min, pa.Max, pb.Min, pb.Max)) continue;
                AddPair(pa.BodyId, pb.BodyId);
            }
        }
    }

    public bool SphereOverlap(Vector3 center, float radius, out OutmBodyHandle body)
    {
        radius = MathF.Max(0.001f, radius);
        for (int i = 0; i < Proxies.Count; i++)
        {
            OutmBroadphaseProxy proxy = Proxies.Items[i];
            if (!proxy.Active) continue;
            ref OutmBody candidate = ref Bodies[proxy.BodyId];
            if ((candidate.Flags & OutmBodyFlags.Sensor) != 0) continue;
            if (SphereAgainstAabb(center, radius, proxy.Min, proxy.Max)) { body = new OutmBodyHandle(proxy.BodyId); return true; }
        }
        body = OutmBodyHandle.None;
        return false;
    }

    public bool PointInSensor(Vector3 point, out OutmBodyHandle body)
    {
        for (int i = 0; i < Bodies.Count; i++)
        {
            ref OutmBody candidate = ref Bodies[i];
            if ((candidate.Flags & (OutmBodyFlags.Active | OutmBodyFlags.Sensor)) != (OutmBodyFlags.Active | OutmBodyFlags.Sensor)) continue;
            OutmBroadphaseProxy proxy = Proxies.Items[candidate.ProxyId];
            if (PointInsideAabb(point, proxy.Min, proxy.Max)) { body = new OutmBodyHandle(candidate.BodyId); return true; }
        }
        body = OutmBodyHandle.None;
        return false;
    }

    public bool BoxOverlap(Vector3 center, Vector3 size)
    {
        Vector3 half = SanitizeSize(size) * 0.5f;
        Vector3 min = center - half;
        Vector3 max = center + half;
        for (int i = 0; i < Proxies.Count; i++)
        {
            OutmBroadphaseProxy proxy = Proxies.Items[i];
            if (!proxy.Active) continue;
            ref OutmBody body = ref Bodies[proxy.BodyId];
            if ((body.Flags & OutmBodyFlags.Sensor) == 0 && AabbOverlap(min, max, proxy.Min, proxy.Max)) return true;
        }
        return false;
    }

    public bool TryGetBody(OutmBodyHandle handle, out OutmBody body)
    {
        if (!IsValidBody(handle)) { body = default; return false; }
        body = Bodies.Items[handle.Id];
        return true;
    }

    private void AddPair(int a, int b)
    {
        ActivePairs.Add(new OutmActivePair { A = a, B = b });
        bool aSensor = (Bodies.Items[a].Flags & OutmBodyFlags.Sensor) != 0;
        bool bSensor = (Bodies.Items[b].Flags & OutmBodyFlags.Sensor) != 0;
        if (aSensor || bSensor)
        {
            TriggerOverlaps.Add(new OutmTriggerOverlap { SensorBodyId = aSensor ? a : b, OtherBodyId = aSensor ? b : a });
            return;
        }
        Contacts.Add(new OutmContactManifold { BodyA = a, BodyB = b, Point = (Bodies.Items[a].Position + Bodies.Items[b].Position) * 0.5f, Normal = Vector3.UnitY, Penetration = 0.0f });
    }

    private void MarkDirty(int bodyId) => DirtyBodies.Add(bodyId);
    private bool IsValidBody(OutmBodyHandle handle) => (uint)handle.Id < (uint)Bodies.Count;
    private static Vector3 SanitizeSize(Vector3 size) => new(MathF.Max(0.01f, MathF.Abs(size.X)), MathF.Max(0.01f, MathF.Abs(size.Y)), MathF.Max(0.01f, MathF.Abs(size.Z)));
    private static bool AabbOverlap(Vector3 minA, Vector3 maxA, Vector3 minB, Vector3 maxB) => minA.X <= maxB.X && maxA.X >= minB.X && minA.Y <= maxB.Y && maxA.Y >= minB.Y && minA.Z <= maxB.Z && maxA.Z >= minB.Z;
    private static bool PointInsideAabb(Vector3 point, Vector3 min, Vector3 max) => point.X >= min.X && point.X <= max.X && point.Y >= min.Y && point.Y <= max.Y && point.Z >= min.Z && point.Z <= max.Z;
    private static bool SphereAgainstAabb(Vector3 point, float radius, Vector3 min, Vector3 max)
    {
        float x = Math.Clamp(point.X, min.X, max.X);
        float y = Math.Clamp(point.Y, min.Y, max.Y);
        float z = Math.Clamp(point.Z, min.Z, max.Z);
        float dx = point.X - x;
        float dy = point.Y - y;
        float dz = point.Z - z;
        return dx * dx + dy * dy + dz * dz < radius * radius;
    }
}
