using System.Numerics;
using OUT_RayMicro.World;

namespace OUT_RayMicro.Physics;

public enum OutmPhysicsShapeKind : byte
{
    Box,
    MeshProxy
}

public enum OutmPhysicsBodyKind : byte
{
    StaticWorld,
    Door,
    MeshCollider
}

public struct OutmPhysicsBody
{
    public int BodyId;
    public string Id;
    public OutmPhysicsBodyKind BodyKind;
    public OutmPhysicsShapeKind ShapeKind;
    public Vector3 Center;
    public Vector3 Size;
    public string SurfaceId;
    public bool Active;

    public Vector3 Min => Center - Size * 0.5f;
    public Vector3 Max => Center + Size * 0.5f;
}

public struct OutmPhysicsTrigger
{
    public int TriggerId;
    public string Id;
    public string Kind;
    public string Target;
    public Vector3 Center;
    public Vector3 Size;

    public Vector3 Min => Center - Size * 0.5f;
    public Vector3 Max => Center + Size * 0.5f;
}

public sealed class OutmPhysicsScene
{
    public readonly List<OutmPhysicsBody> Bodies = new(256);
    public readonly List<OutmPhysicsTrigger> Triggers = new(128);

    public void AddBody(string id, OutmPhysicsBodyKind bodyKind, OutmPhysicsShapeKind shapeKind, Vector3 center, Vector3 size, string surfaceId, bool active = true)
    {
        Bodies.Add(new OutmPhysicsBody
        {
            BodyId = Bodies.Count,
            Id = string.IsNullOrWhiteSpace(id) ? $"body.{Bodies.Count}" : id,
            BodyKind = bodyKind,
            ShapeKind = shapeKind,
            Center = center,
            Size = SanitizeSize(size),
            SurfaceId = string.IsNullOrWhiteSpace(surfaceId) ? "surface.stone" : surfaceId,
            Active = active
        });
    }

    public void AddTrigger(string id, string kind, string target, Vector3 center, Vector3 size)
    {
        Triggers.Add(new OutmPhysicsTrigger
        {
            TriggerId = Triggers.Count,
            Id = string.IsNullOrWhiteSpace(id) ? $"trigger.{Triggers.Count}" : id,
            Kind = string.IsNullOrWhiteSpace(kind) ? "trigger" : kind,
            Target = target,
            Center = center,
            Size = SanitizeSize(size)
        });
    }

    public void SetDoorActive(string doorId, bool active)
    {
        for (int i = 0; i < Bodies.Count; i++)
        {
            OutmPhysicsBody body = Bodies[i];
            if (body.BodyKind != OutmPhysicsBodyKind.Door || !string.Equals(body.Id, doorId, StringComparison.OrdinalIgnoreCase))
                continue;

            body.Active = active;
            Bodies[i] = body;
            return;
        }
    }

    private static Vector3 SanitizeSize(Vector3 size)
    {
        return new Vector3(
            MathF.Max(0.01f, MathF.Abs(size.X)),
            MathF.Max(0.01f, MathF.Abs(size.Y)),
            MathF.Max(0.01f, MathF.Abs(size.Z)));
    }
}

public static class OutmPhysicsSceneBuilder
{
    public static OutmPhysicsScene Build(OutmMapDef def, OutmDemoMap map)
    {
        var scene = new OutmPhysicsScene();

        for (int i = 0; i < map.Boxes.Count; i++)
        {
            OutmBox box = map.Boxes[i];
            if (!box.Solid)
                continue;

            scene.AddBody(box.Id, OutmPhysicsBodyKind.StaticWorld, OutmPhysicsShapeKind.Box, box.Center, box.Size, box.SurfaceId);
        }

        for (int i = 0; i < map.Doors.Count; i++)
        {
            OutmDoorRuntime door = map.Doors[i];
            scene.AddBody(door.Id, OutmPhysicsBodyKind.Door, OutmPhysicsShapeKind.Box, door.Center, door.Size, door.SurfaceId, !door.Open);
        }

        for (int i = 0; i < map.Triggers.Count; i++)
        {
            OutmTriggerRuntime trigger = map.Triggers[i];
            scene.AddTrigger(trigger.Id, trigger.Kind, trigger.Target, trigger.Center, trigger.Size);
        }

        for (int i = 0; i < def.Meshes.Length; i++)
        {
            OutmMeshRefDef mesh = def.Meshes[i];
            if (string.Equals(mesh.Collision, "none", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(mesh.Collision))
                continue;

            Vector3 position = OutmMapDef.ToVector3(mesh.Position, Vector3.Zero);
            Vector3 scale = OutmMapDef.ToVector3(mesh.Scale, Vector3.One);
            OutmPhysicsShapeKind shapeKind = string.Equals(mesh.Collision, "mesh", StringComparison.OrdinalIgnoreCase)
                ? OutmPhysicsShapeKind.MeshProxy
                : OutmPhysicsShapeKind.Box;

            scene.AddBody(mesh.Id, OutmPhysicsBodyKind.MeshCollider, shapeKind, position + Vector3.UnitY * (MathF.Abs(scale.Y) * 0.5f), scale, mesh.Surface);
        }

        return scene;
    }
}
