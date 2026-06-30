using System.Numerics;

namespace OUT_RayMicro.Physics;

[Flags]
public enum OutmBodyFlags : ushort
{
    None = 0,
    Active = 1 << 0,
    Static = 1 << 1,
    Sensor = 1 << 2,
    Door = 1 << 3,
    Dirty = 1 << 4
}

public struct OutmBody
{
    public int BodyId;
    public int ShapeId;
    public int ProxyId;
    public int SourceKind;
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

public struct OutmDirtyBody
{
    public int BodyId;
}

public sealed class OutmPhysicsRuntime
{
    public OutmBody[] Bodies;
    public OutmShape[] Shapes;
    public OutmBroadphaseProxy[] Proxies;
    public OutmActivePair[] Pairs;
    public OutmContactManifold[] Contacts;
    public OutmTriggerOverlap[] Overlaps;
    public OutmDirtyBody[] DirtyBodies;

    public int BodyCount;
    public int ShapeCount;
    public int ProxyCount;
    public int PairCount;
    public int ContactCount;
    public int OverlapCount;
    public int DirtyCount;

    public OutmPhysicsRuntime(int bodyCapacity, int shapeCapacity, int pairCapacity)
    {
        int bodies = Math.Max(16, bodyCapacity);
        int shapes = Math.Max(16, shapeCapacity);
        int pairs = Math.Max(32, pairCapacity);

        Bodies = new OutmBody[bodies];
        Shapes = new OutmShape[shapes];
        Proxies = new OutmBroadphaseProxy[bodies];
        Pairs = new OutmActivePair[pairs];
        Contacts = new OutmContactManifold[pairs];
        Overlaps = new OutmTriggerOverlap[pairs];
        DirtyBodies = new OutmDirtyBody[bodies];
    }

    public int AddShape(OutmPhysicsShapeKind kind, Vector3 size, string surfaceId)
    {
        EnsureShapeCapacity(ShapeCount + 1);
        int id = ShapeCount++;
        Shapes[id] = new OutmShape
        {
            ShapeId = id,
            Kind = kind,
            Size = SanitizeSize(size),
            SurfaceId = string.IsNullOrWhiteSpace(surfaceId) ? "surface.stone" : surfaceId
        };
        return id;
    }

    public int AddBody(int shapeId, Vector3 position, OutmBodyFlags flags, int sourceKind, int sourceIndex)
    {
        EnsureBodyCapacity(BodyCount + 1);
        int bodyId = BodyCount;
        int proxyId = ProxyCount;
        BodyCount++;
        ProxyCount++;

        Bodies[bodyId] = new OutmBody
        {
            BodyId = bodyId,
            ShapeId = shapeId,
            ProxyId = proxyId,
            SourceKind = sourceKind,
            SourceIndex = sourceIndex,
            Position = position,
            Flags = flags | OutmBodyFlags.Dirty
        };

        Proxies[proxyId] = new OutmBroadphaseProxy
        {
            ProxyId = proxyId,
            BodyId = bodyId,
            ShapeId = shapeId,
            Active = (flags & OutmBodyFlags.Active) != 0
        };

        MarkDirty(bodyId);
        return bodyId;
    }

    public void SetBodyActive(int bodyId, bool active)
    {
        if ((uint)bodyId >= (uint)BodyCount)
            return;

        OutmBody body = Bodies[bodyId];
        if (active)
            body.Flags |= OutmBodyFlags.Active;
        else
            body.Flags &= ~OutmBodyFlags.Active;

        body.Flags |= OutmBodyFlags.Dirty;
        Bodies[bodyId] = body;
        MarkDirty(bodyId);
    }

    public void SetBodyPosition(int bodyId, Vector3 position)
    {
        if ((uint)bodyId >= (uint)BodyCount)
            return;

        OutmBody body = Bodies[bodyId];
        body.Position = position;
        body.Flags |= OutmBodyFlags.Dirty;
        Bodies[bodyId] = body;
        MarkDirty(bodyId);
    }

    public void FlushDirtyProxies()
    {
        for (int i = 0; i < DirtyCount; i++)
        {
            int bodyId = DirtyBodies[i].BodyId;
            if ((uint)bodyId >= (uint)BodyCount)
                continue;

            OutmBody body = Bodies[bodyId];
            OutmShape shape = Shapes[body.ShapeId];
            Vector3 half = shape.Size * 0.5f;
            int proxyId = body.ProxyId;

            Proxies[proxyId] = new OutmBroadphaseProxy
            {
                ProxyId = proxyId,
                BodyId = bodyId,
                ShapeId = body.ShapeId,
                Min = body.Position - half,
                Max = body.Position + half,
                Active = (body.Flags & OutmBodyFlags.Active) != 0
            };

            body.Flags &= ~OutmBodyFlags.Dirty;
            Bodies[bodyId] = body;
        }

        DirtyCount = 0;
    }

    public void BuildPairs()
    {
        PairCount = 0;
        ContactCount = 0;
        OverlapCount = 0;

        for (int a = 0; a < ProxyCount; a++)
        {
            OutmBroadphaseProxy pa = Proxies[a];
            if (!pa.Active)
                continue;

            for (int b = a + 1; b < ProxyCount; b++)
            {
                OutmBroadphaseProxy pb = Proxies[b];
                if (!pb.Active || !AabbOverlap(pa.Min, pa.Max, pb.Min, pb.Max))
                    continue;

                AddPair(pa.BodyId, pb.BodyId);
            }
        }
    }

    public bool SphereOverlap(Vector3 center, float radius, out int bodyId)
    {
        radius = MathF.Max(0.001f, radius);
        for (int i = 0; i < ProxyCount; i++)
        {
            OutmBroadphaseProxy proxy = Proxies[i];
            if (!proxy.Active)
                continue;

            if (SphereAgainstAabb(center, radius, proxy.Min, proxy.Max))
            {
                bodyId = proxy.BodyId;
                return true;
            }
        }

        bodyId = -1;
        return false;
    }

    public bool PointInSensor(Vector3 point, out int bodyId)
    {
        for (int i = 0; i < BodyCount; i++)
        {
            OutmBody body = Bodies[i];
            if ((body.Flags & (OutmBodyFlags.Active | OutmBodyFlags.Sensor)) != (OutmBodyFlags.Active | OutmBodyFlags.Sensor))
                continue;

            OutmBroadphaseProxy proxy = Proxies[body.ProxyId];
            if (PointInsideAabb(point, proxy.Min, proxy.Max))
            {
                bodyId = body.BodyId;
                return true;
            }
        }

        bodyId = -1;
        return false;
    }

    public bool BoxOverlap(Vector3 center, Vector3 size)
    {
        Vector3 half = SanitizeSize(size) * 0.5f;
        Vector3 min = center - half;
        Vector3 max = center + half;

        for (int i = 0; i < ProxyCount; i++)
        {
            OutmBroadphaseProxy proxy = Proxies[i];
            if (proxy.Active && AabbOverlap(min, max, proxy.Min, proxy.Max))
                return true;
        }

        return false;
    }

    private void AddPair(int a, int b)
    {
        EnsurePairCapacity(PairCount + 1);
        Pairs[PairCount++] = new OutmActivePair { A = a, B = b };

        bool aSensor = (Bodies[a].Flags & OutmBodyFlags.Sensor) != 0;
        bool bSensor = (Bodies[b].Flags & OutmBodyFlags.Sensor) != 0;
        if (aSensor || bSensor)
        {
            EnsurePairCapacity(OverlapCount + 1);
            Overlaps[OverlapCount++] = new OutmTriggerOverlap
            {
                SensorBodyId = aSensor ? a : b,
                OtherBodyId = aSensor ? b : a
            };
            return;
        }

        EnsurePairCapacity(ContactCount + 1);
        Contacts[ContactCount++] = new OutmContactManifold
        {
            BodyA = a,
            BodyB = b,
            Point = (Bodies[a].Position + Bodies[b].Position) * 0.5f,
            Normal = Vector3.UnitY,
            Penetration = 0.0f
        };
    }

    private void MarkDirty(int bodyId)
    {
        EnsureDirtyCapacity(DirtyCount + 1);
        DirtyBodies[DirtyCount++] = new OutmDirtyBody { BodyId = bodyId };
    }

    private void EnsureBodyCapacity(int required)
    {
        if (required <= Bodies.Length)
            return;

        int next = Bodies.Length * 2;
        while (next < required) next *= 2;
        Array.Resize(ref Bodies, next);
        Array.Resize(ref Proxies, next);
        Array.Resize(ref DirtyBodies, next);
    }

    private void EnsureShapeCapacity(int required)
    {
        if (required <= Shapes.Length)
            return;

        int next = Shapes.Length * 2;
        while (next < required) next *= 2;
        Array.Resize(ref Shapes, next);
    }

    private void EnsurePairCapacity(int required)
    {
        if (required <= Pairs.Length)
            return;

        int next = Pairs.Length * 2;
        while (next < required) next *= 2;
        Array.Resize(ref Pairs, next);
        Array.Resize(ref Contacts, next);
        Array.Resize(ref Overlaps, next);
    }

    private void EnsureDirtyCapacity(int required)
    {
        if (required <= DirtyBodies.Length)
            return;

        int next = DirtyBodies.Length * 2;
        while (next < required) next *= 2;
        Array.Resize(ref DirtyBodies, next);
    }

    private static Vector3 SanitizeSize(Vector3 size)
    {
        return new Vector3(MathF.Max(0.01f, MathF.Abs(size.X)), MathF.Max(0.01f, MathF.Abs(size.Y)), MathF.Max(0.01f, MathF.Abs(size.Z)));
    }

    private static bool AabbOverlap(Vector3 minA, Vector3 maxA, Vector3 minB, Vector3 maxB)
    {
        return minA.X <= maxB.X && maxA.X >= minB.X && minA.Y <= maxB.Y && maxA.Y >= minB.Y && minA.Z <= maxB.Z && maxA.Z >= minB.Z;
    }

    private static bool PointInsideAabb(Vector3 point, Vector3 min, Vector3 max)
    {
        return point.X >= min.X && point.X <= max.X && point.Y >= min.Y && point.Y <= max.Y && point.Z >= min.Z && point.Z <= max.Z;
    }

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
