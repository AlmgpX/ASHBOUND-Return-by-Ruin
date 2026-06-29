using System.Numerics;
using OUT_RayMicro.World;

namespace OUT_RayMicro.Physics;

public sealed class OutmDemoCollisionWorld : IOutmCollisionWorld
{
    private readonly OutmDemoMap map;

    public OutmDemoCollisionWorld(OutmDemoMap map)
    {
        this.map = map;
    }

    public OutmCollisionBackendKind BackendKind => OutmCollisionBackendKind.Custom;

    public void Step(float dt)
    {
        // Static demo world. Jolt backend will actually step simulation later.
    }

    public bool CollidesSphere(Vector3 center, float radius)
    {
        return map.Collides(center, radius);
    }

    public OutmRayHit Raycast(Vector3 origin, Vector3 direction, float maxDistance)
    {
        if (direction.LengthSquared() < 0.0001f || maxDistance <= 0.0f)
            return OutmRayHit.None;

        direction = Vector3.Normalize(direction);
        const float step = 0.10f;
        float distance = 0.0f;
        Vector3 previous = origin;

        while (distance <= maxDistance)
        {
            Vector3 point = origin + direction * distance;
            if (map.Collides(point, 0.08f))
            {
                Vector3 normal = EstimateNormal(previous, point);
                return new OutmRayHit(true, previous, normal, distance, 0);
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
        Vector3[] probes =
        {
            center,
            new(min.X, center.Y, center.Z),
            new(max.X, center.Y, center.Z),
            new(center.X, center.Y, min.Z),
            new(center.X, center.Y, max.Z),
            new(min.X, center.Y, min.Z),
            new(max.X, center.Y, min.Z),
            new(min.X, center.Y, max.Z),
            new(max.X, center.Y, max.Z)
        };

        for (int i = 0; i < probes.Length; i++)
        {
            if (map.Collides(probes[i], 0.05f))
                return true;
        }

        return false;
    }

    public OutmCharacterMove MoveCharacter(Vector3 position, Vector3 velocity, float radius, float floorHeight, float dt)
    {
        Vector3 delta = velocity * Math.Clamp(dt, 0.0f, 0.05f);
        Vector3 before = position;
        Vector3 after = map.MoveWithCollision(position, delta, radius, floorHeight);

        Vector3 resultVelocity = velocity;
        if (MathF.Abs(after.X - before.X) < 0.00001f && MathF.Abs(delta.X) > 0.00001f)
            resultVelocity.X = 0.0f;
        if (MathF.Abs(after.Z - before.Z) < 0.00001f && MathF.Abs(delta.Z) > 0.00001f)
            resultVelocity.Z = 0.0f;
        if (after.Y <= floorHeight + 0.001f && resultVelocity.Y < 0.0f)
            resultVelocity.Y = 0.0f;

        bool grounded = after.Y <= floorHeight + 0.001f && resultVelocity.Y <= 0.05f;
        return new OutmCharacterMove(after, resultVelocity, grounded, Vector3.UnitY);
    }

    private static Vector3 EstimateNormal(Vector3 previous, Vector3 current)
    {
        Vector3 delta = current - previous;
        if (MathF.Abs(delta.X) > MathF.Abs(delta.Z))
            return new Vector3(delta.X > 0.0f ? -1.0f : 1.0f, 0.0f, 0.0f);

        return new Vector3(0.0f, 0.0f, delta.Z > 0.0f ? -1.0f : 1.0f);
    }
}
