using System.Numerics;
using OUT_RayMicro.World;

namespace OUT_RayMicro.Physics;

public sealed class OutmCorePhysicsWorld : IOutmCollisionWorld
{
    private readonly OutmDemoMap map;
    private readonly OutmPhysicsScene scene;
    private readonly OutmPhysicsRuntime runtime;
    private readonly IOutmPhysicsBackendBridge backend;
    private readonly Dictionary<string, OutmBodyHandle> solidBodies = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, OutmBodyHandle> sensorBodies = new(StringComparer.OrdinalIgnoreCase);

    public OutmCorePhysicsWorld(OutmMapDef def, OutmDemoMap map)
    {
        this.map = map;
        scene = OutmPhysicsSceneBuilder.Build(def, map);
        runtime = BuildRuntime(scene);
        backend = OutmPhysicsBackendFactory.CreateDefault();
        runtime.FlushDirtyProxies();
        runtime.BuildPairs();
        backend.BuildFromRuntime(runtime);
    }

    public OutmCollisionBackendKind BackendKind => OutmCollisionBackendKind.Jolt;
    public OutmPhysicsBackendRole BackendRole => backend.Role;
    public int BodyCount => runtime.BodyCount;
    public int ShapeCount => runtime.ShapeCount;
    public int ProxyCount => runtime.ProxyCount;
    public int PairCount => runtime.PairCount;

    public bool TryGetSolidBody(string id, out OutmBodyHandle body) => solidBodies.TryGetValue(id, out body);
    public bool TryGetSensorBody(string id, out OutmBodyHandle body) => sensorBodies.TryGetValue(id, out body);

    public void Step(float dt)
    {
        SyncSolidsFromComponents();
        runtime.FlushDirtyProxies();
        runtime.BuildPairs();
        backend.Step(dt);
    }

    public bool CollidesSphere(Vector3 center, float radius)
    {
        runtime.FlushDirtyProxies();
        return runtime.SphereOverlap(center, radius, out _);
    }

    public OutmRayHit Raycast(Vector3 origin, Vector3 direction, float maxDistance)
    {
        if (direction.LengthSquared() < 0.0001f || maxDistance <= 0.0f)
            return OutmRayHit.None;

        direction = Vector3.Normalize(direction);
        const float step = 0.08f;
        float distance = 0.0f;
        Vector3 previous = origin;
        runtime.FlushDirtyProxies();

        while (distance <= maxDistance)
        {
            Vector3 point = origin + direction * distance;
            for (int i = 0; i < runtime.ProxyCount; i++)
            {
                OutmBroadphaseProxy proxy = runtime.Proxies.Items[i];
                if (!proxy.Active)
                    continue;

                OutmBody body = runtime.Bodies.Items[proxy.BodyId];
                if ((body.Flags & OutmBodyFlags.Sensor) != 0)
                    continue;

                if (PointInside(point, proxy.Min, proxy.Max))
                {
                    string surface = runtime.Shapes.Items[body.ShapeId].SurfaceId;
                    return new OutmRayHit(true, previous, EstimateNormal(previous, point), distance, proxy.BodyId, surface);
                }
            }
            previous = point;
            distance += step;
        }

        return OutmRayHit.None;
    }

    public bool OverlapBox(Vector3 center, Vector3 size)
    {
        runtime.FlushDirtyProxies();
        return runtime.BoxOverlap(center, size);
    }

    public bool QuerySensor(Vector3 position, out OutmSensorProbe sensor)
    {
        runtime.FlushDirtyProxies();
        if (!runtime.PointInSensor(position, out OutmBodyHandle bodyHandle))
        {
            sensor = OutmSensorProbe.None;
            return false;
        }

        if (!runtime.TryGetBody(bodyHandle, out OutmBody body) || body.SourceKind != OutmPhysicsSourceKind.AuthoringSensor || (uint)body.SourceIndex >= (uint)scene.Triggers.Count)
        {
            sensor = OutmSensorProbe.None;
            return false;
        }

        OutmPhysicsTrigger src = scene.Triggers[body.SourceIndex];
        sensor = new OutmSensorProbe(true, src.Id, src.Kind, src.Target, src.TriggerId);
        return true;
    }

    public OutmCharacterMove MoveCharacter(Vector3 position, Vector3 velocity, float radius, float floorHeight, float dt)
    {
        Vector3 delta = velocity * Math.Clamp(dt, 0.0f, 0.05f);
        Vector3 next = position;
        Vector3 tryX = next + new Vector3(delta.X, 0.0f, 0.0f);
        if (!CollidesSphere(tryX, radius)) next = tryX;
        else velocity.X = 0.0f;

        Vector3 tryZ = next + new Vector3(0.0f, 0.0f, delta.Z);
        if (!CollidesSphere(tryZ, radius)) next = tryZ;
        else velocity.Z = 0.0f;

        next.Y = MathF.Max(floorHeight, next.Y + delta.Y);
        if (next.Y <= floorHeight + 0.001f && velocity.Y < 0.0f) velocity.Y = 0.0f;
        bool grounded = next.Y <= floorHeight + 0.001f && velocity.Y <= 0.05f;
        return new OutmCharacterMove(next, velocity, grounded, Vector3.UnitY);
    }

    private OutmPhysicsRuntime BuildRuntime(OutmPhysicsScene source)
    {
        int cap = Math.Max(4096, source.Bodies.Count + source.Triggers.Count + 128);
        var result = new OutmPhysicsRuntime(bodyCapacity: cap, shapeCapacity: cap * 2);

        for (int i = 0; i < source.Bodies.Count; i++)
        {
            OutmPhysicsBody src = source.Bodies[i];
            OutmShapeHandle shape = result.AddShape(src.ShapeKind, src.Size, src.SurfaceId);
            OutmBodyFlags flags = OutmBodyFlags.Static;
            if (src.Active) flags |= OutmBodyFlags.Active;
            if (src.BodyKind == OutmPhysicsBodyKind.Door) flags |= OutmBodyFlags.Kinematic | OutmBodyFlags.Door;
            OutmBodyHandle body = result.AddBody(shape, src.Center, flags, OutmPhysicsSourceKind.AuthoringBody, i);
            solidBodies[src.Id] = body;
        }

        for (int i = 0; i < source.Triggers.Count; i++)
        {
            OutmPhysicsTrigger src = source.Triggers[i];
            OutmShapeHandle shape = result.AddShape(OutmPhysicsShapeKind.Box, src.Size, "surface.sensor");
            OutmBodyHandle body = result.AddBody(shape, src.Center, OutmBodyFlags.Active | OutmBodyFlags.Sensor | OutmBodyFlags.Static, OutmPhysicsSourceKind.AuthoringSensor, i);
            sensorBodies[src.Id] = body;
        }

        return result;
    }

    private void SyncSolidsFromComponents()
    {
        for (int i = 0; i < map.Doors.Count; i++)
        {
            OutmDoorRuntime item = map.Doors[i];
            if (solidBodies.TryGetValue(item.Id, out OutmBodyHandle body))
                runtime.SetBodyActive(body, !item.Open);
        }
    }

    private static bool PointInside(Vector3 p, Vector3 min, Vector3 max)
    {
        return p.X >= min.X && p.X <= max.X && p.Y >= min.Y && p.Y <= max.Y && p.Z >= min.Z && p.Z <= max.Z;
    }

    private static Vector3 EstimateNormal(Vector3 previous, Vector3 current)
    {
        Vector3 d = current - previous;
        float ax = MathF.Abs(d.X);
        float ay = MathF.Abs(d.Y);
        float az = MathF.Abs(d.Z);
        if (ax >= ay && ax >= az) return new Vector3(d.X > 0.0f ? -1.0f : 1.0f, 0.0f, 0.0f);
        if (ay >= ax && ay >= az) return new Vector3(0.0f, d.Y > 0.0f ? -1.0f : 1.0f, 0.0f);
        return new Vector3(0.0f, 0.0f, d.Z > 0.0f ? -1.0f : 1.0f);
    }
}
