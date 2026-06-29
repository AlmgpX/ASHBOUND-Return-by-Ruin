using System.Numerics;

namespace OUT_RayMicro.Physics;

public enum OutmCollisionBackendKind : byte
{
    Custom,
    Jolt
}

public readonly struct OutmRayHit
{
    public readonly bool Hit;
    public readonly Vector3 Point;
    public readonly Vector3 Normal;
    public readonly float Distance;
    public readonly int BodyId;

    public OutmRayHit(bool hit, Vector3 point, Vector3 normal, float distance, int bodyId)
    {
        Hit = hit;
        Point = point;
        Normal = normal;
        Distance = distance;
        BodyId = bodyId;
    }

    public static readonly OutmRayHit None = new(false, Vector3.Zero, Vector3.UnitY, 0.0f, -1);
}

public readonly struct OutmCharacterMove
{
    public readonly Vector3 Position;
    public readonly Vector3 Velocity;
    public readonly bool Grounded;
    public readonly Vector3 GroundNormal;

    public OutmCharacterMove(Vector3 position, Vector3 velocity, bool grounded, Vector3 groundNormal)
    {
        Position = position;
        Velocity = velocity;
        Grounded = grounded;
        GroundNormal = groundNormal;
    }
}

public interface IOutmCollisionWorld
{
    OutmCollisionBackendKind BackendKind { get; }
    void Step(float dt);
    bool CollidesSphere(Vector3 center, float radius);
    OutmRayHit Raycast(Vector3 origin, Vector3 direction, float maxDistance);
    bool OverlapBox(Vector3 center, Vector3 size);
    OutmCharacterMove MoveCharacter(Vector3 position, Vector3 velocity, float radius, float floorHeight, float dt);
}
