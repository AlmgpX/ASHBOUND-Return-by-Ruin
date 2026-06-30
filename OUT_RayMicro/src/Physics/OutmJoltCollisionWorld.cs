using System.Numerics;
using OUT_RayMicro.World;

namespace OUT_RayMicro.Physics;

public sealed class OutmJoltCollisionWorld : IOutmCollisionWorld
{
    private readonly OutmDemoMap map;
    private readonly OutmPhysicsScene scene;

    public OutmJoltCollisionWorld(OutmMapDef def, OutmDemoMap map)
    {
        this.map = map;
        scene = OutmPhysicsSceneBuilder.Build(def, map);
    }

    public OutmCollisionBackendKind BackendKind => OutmCollisionBackendKind.Jolt;
    public int BodyCount => scene.Bodies.Count;
    public int SensorCount => scene.Triggers.Count;

    public void Step(float dt)
    {
        SyncDoorBodies();
        // Native Jolt stepping will be inserted here. The gameplay side already talks to this backend contract.
    }

    public bool CollidesSphere(Vector3 center, float radius)
    {
        SyncDoorBodies();
        radius = MathF.Max(0.001f, radius);

        for (int i = 0; i < scene.Bodies.Count; i++)
        {
            OutmPhysicsBody body = scene.Bodies[i];
            if (!body.Active)
                continue;

            if (SphereAgainstBox(center, radius, body.Min, body.Max))
                return true;
        }

        return false;
    }

    public OutmRayHit Raycast(Vector3 origin, Vector3 direction, float maxDistance)
    {
        if (direction.LengthSquared() < 0.0001f || maxDistance <= 0.0f)
            return OutmRayHit.None;

        direction = Vector3.Normalize(direction);
        const float step = 0.08f;
        float distance = 0.0f;
        Vector3 previous = origin;

        while (distance <= maxDistance)
        {
            Vector3 point = origin + direction * distance;
            for (int i = 0; i < scene.Bodies.Count; i++)
            {
                OutmPhysicsBody body = scene.Bodies[i];
                if (!body.Active)
                    continue;

                if (PointInsideBox(point, body.Min, body.Max))
                {
                    Vector3 normal = EstimateNormal(previous, point);
                    return new OutmRayHit(true, previous, normal, distance, body.BodyId);
                }
            }

            previous = point;
            distance += step;
        }

        return OutmRayHit.None;
    }

    public bool OverlapBox(Vector3 center, Vector3 size)
    {
        Vector3 min = center - size * 0.5f;
        Vector3 max = center + size * 0.5f;

        for (int i = 0; i < scene.Bodies.Count; i++)
        {
            OutmPhysicsBody body = scene.Bodies[i];
            if (!body.Active)
                continue;

            if (BoxesOverlap(min, max, body.Min, body.Max))
                return true;
        }

        return false;
    }

    public bool QuerySensor(Vector3 position, out OutmSensorProbe sensor)
    {
        for (int i = 0; i < scene.Triggers.Count; i++)
        {
            OutmPhysicsTrigger trigger = scene.Triggers[i];
            if (!PointInsideBox(position, trigger.Min, trigger.Max))
                continue;

            sensor = new OutmSensorProbe(true, trigger.Id, trigger.Kind, trigger.Target, trigger.TriggerId);
            return true;
        }

        sensor = OutmSensorProbe.None;
        return false;
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

    private void SyncDoorBodies()
    {
        for (int i = 0; i < map.Doors.Count; i++)
        {
            OutmDoorRuntime door = map.Doors[i];
            scene.SetDoorActive(door.Id, !door.Open);
        }
    }

    private static bool SphereAgainstBox(Vector3 point, float radius, Vector3 min, Vector3 max)
    {
        float closestX = Math.Clamp(point.X, min.X, max.X);
        float closestY = Math.Clamp(point.Y, min.Y, max.Y);
        float closestZ = Math.Clamp(point.Z, min.Z, max.Z);
        float dx = point.X - closestX;
        float dy = point.Y - closestY;
        float dz = point.Z - closestZ;
        return dx * dx + dy * dy + dz * dz < radius * radius;
    }

    private static bool PointInsideBox(Vector3 point, Vector3 min, Vector3 max)
    {
        return point.X >= min.X && point.X <= max.X &&
               point.Y >= min.Y && point.Y <= max.Y &&
               point.Z >= min.Z && point.Z <= max.Z;
    }

    private static bool BoxesOverlap(Vector3 minA, Vector3 maxA, Vector3 minB, Vector3 maxB)
    {
        return minA.X <= maxB.X && maxA.X >= minB.X &&
               minA.Y <= maxB.Y && maxA.Y >= minB.Y &&
               minA.Z <= maxB.Z && maxA.Z >= minB.Z;
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
