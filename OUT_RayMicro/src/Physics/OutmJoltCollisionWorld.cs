using System.Numerics;
using OUT_RayMicro.World;

namespace OUT_RayMicro.Physics;

public sealed class OutmJoltCollisionWorld : IOutmCollisionWorld
{
    private readonly OutmDemoMap map;
    private readonly OutmPhysicsScene scene;
    private readonly OutmPhysicsRuntime runtime;
    private readonly Dictionary<string, OutmBodyHandle> doorBodies = new(StringComparer.OrdinalIgnoreCase);

    public OutmJoltCollisionWorld(OutmMapDef def, OutmDemoMap map)
    {
        this.map = map;
        scene = OutmPhysicsSceneBuilder.Build(def, map);
        runtime = BuildRuntime(scene);
        runtime.FlushDirtyProxies();
        runtime.BuildPairs();
    }

    public OutmCollisionBackendKind BackendKind => OutmCollisionBackendKind.Jolt;
    public int BodyCount => runtime.BodyCount;
    public int ShapeCount => runtime.ShapeCount;
    public int ProxyCount => runtime.ProxyCount;
    public int PairCount => runtime.PairCount;
    public int ContactCount => runtime.ContactCount;
    public int SensorOverlapCount => runtime.TriggerOverlapCount;

    public void Step(float dt)
    {
        SyncDoorBodies();
        runtime.FlushDirtyProxies();
        runtime.BuildPairs();
        // Native Jolt stepping will replace the current proxy-only broadphase internals.
        // Gameplay already sees Body/Shape/Proxy buffers through IOutmCollisionWorld, not authoring buckets.
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

                if (PointInsideBox(point, proxy.Min, proxy.Max))
                {
                    Vector3 normal = EstimateNormal(previous, point);
                    return new OutmRayHit(true, previous, normal, distance, proxy.BodyId);
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
        if (!runtime.PointInSensor(position, out OutmBodyHandle sensorBody))
        {
            sensor = OutmSensorProbe.None;
            return false;
        }

        if (!runtime.TryGetBody(sensorBody, out OutmBody body) || body.SourceKind != OutmPhysicsSourceKind.AuthoringSensor)
        {
            sensor = OutmSensorProbe.None;
            return false;
        }

        if ((uint)body.SourceIndex >= (uint)scene.Triggers.Count)
        {
            sensor = OutmSensorProbe.None;
            return false;
        }

        OutmPhysicsTrigger trigger = scene.Triggers[body.SourceIndex];
        sensor = new OutmSensorProbe(true, trigger.Id, trigger.Kind, trigger.Target, trigger.TriggerId);
        return true;
    }

    public OutmCharacterMove MoveCharacter(Vector3 position, Vector3 velocity, float radius, float floorHeight, float dt)
    {
        Vector3 delta = velocity * Math.Clamp(dt, 0.0f, 0.05f);
        Vector3 next = position;

        Vector3 tryX = next + new Vector3(delta.X, 0.0f, 0.0f);
        if (!CollidesSphere(tryX, radius))
            next = tryX;
        else
            velocity.X = 0.0f;

        Vector3 tryZ = next + new Vector3(0.0f, 0.0f, delta.Z);
        if (!CollidesSphere(tryZ, radius))
            next = tryZ;
        else
            velocity.Z = 0.0f;

        next.Y = MathF.Max(floorHeight, next.Y + delta.Y);
        if (next.Y <= floorHeight + 0.001f && velocity.Y < 0.0f)
            velocity.Y = 0.0f;

        bool grounded = next.Y <= floorHeight + 0.001f && velocity.Y <= 0.05f;
        return new OutmCharacterMove(next, velocity, grounded, Vector3.UnitY);
    }

    private OutmPhysicsRuntime BuildRuntime(OutmPhysicsScene sourceScene)
    {
        int bodyCount = Math.Max(4096, sourceScene.Bodies.Count + sourceScene.Triggers.Count + 128);
        var result = new OutmPhysicsRuntime(bodyCapacity: bodyCount, shapeCapacity: bodyCount * 2);

        for (int i = 0; i < sourceScene.Bodies.Count; i++)
        {
            OutmPhysicsBody src = sourceScene.Bodies[i];
            OutmShapeHandle shape = result.AddShape(src.ShapeKind, src.Size, src.SurfaceId);
            OutmBodyFlags flags = OutmBodyFlags.Static;
            if (src.Active)
                flags |= OutmBodyFlags.Active;
            if (src.BodyKind == OutmPhysicsBodyKind.Door)
                flags |= OutmBodyFlags.Door | OutmBodyFlags.Kinematic;

            OutmBodyHandle body = result.AddBody(shape, src.Center, flags, OutmPhysicsSourceKind.AuthoringBody, i);
            if (src.BodyKind == OutmPhysicsBodyKind.Door)
                doorBodies[src.Id] = body;
        }

        for (int i = 0; i < sourceScene.Triggers.Count; i++)
        {
            OutmPhysicsTrigger src = sourceScene.Triggers[i];
            OutmShapeHandle shape = result.AddShape(OutmPhysicsShapeKind.Box, src.Size, "surface.sensor");
            result.AddBody(shape, src.Center, OutmBodyFlags.Active | OutmBodyFlags.Sensor | OutmBodyFlags.Static, OutmPhysicsSourceKind.AuthoringSensor, i);
        }

        return result;
    }

    private void SyncDoorBodies()
    {
        for (int i = 0; i < map.Doors.Count; i++)
        {
            OutmDoorRuntime door = map.Doors[i];
            if (doorBodies.TryGetValue(door.Id, out OutmBodyHandle body))
                runtime.SetBodyActive(body, !door.Open);
        }
    }

    private static bool PointInsideBox(Vector3 point, Vector3 min, Vector3 max)
    {
        return point.X >= min.X && point.X <= max.X && point.Y >= min.Y && point.Y <= max.Y && point.Z >= min.Z && point.Z <= max.Z;
    }

    private static Vector3 EstimateNormal(Vector3 previous, Vector3 current)
    {
        Vector3 delta = current - previous;
        float ax = MathF.Abs(delta.X);
        float ay = MathF.Abs(delta.Y);
        float az = MathF.Abs(delta.Z);

        if (ax >= ay && ax >= az)
            return new Vector3(delta.X > 0.0f ? -1.0f : 1.0f, 0.0f, 0.0f);
        if (ay >= ax && ay >= az)
            return new Vector3(0.0f, delta.Y > 0.0f ? -1.0f : 1.0f, 0.0f);

        return new Vector3(0.0f, 0.0f, delta.Z > 0.0f ? -1.0f : 1.0f);
    }
}
