using System.Numerics;
using Raylib_cs;
using OUT_RayMicro.World;

namespace OUT_RayMicro.Runtime;

public sealed class OutmCameraMotor
{
    public Vector3 Position;
    public float Yaw;
    public float Pitch;
    public float MoveSpeed = 6.0f;
    public float SprintMultiplier = 1.65f;
    public float MouseSensitivity = 0.0023f;
    public float Radius = 0.35f;

    public OutmCameraMotor(Vector3 start)
    {
        Position = start;
        Yaw = MathF.PI;
        Pitch = 0.0f;
    }

    public Vector3 Forward
    {
        get
        {
            float cp = MathF.Cos(Pitch);
            return Vector3.Normalize(new Vector3(MathF.Sin(Yaw) * cp, MathF.Sin(Pitch), MathF.Cos(Yaw) * cp));
        }
    }

    public Vector3 FlatForward
    {
        get
        {
            var f = new Vector3(MathF.Sin(Yaw), 0, MathF.Cos(Yaw));
            return Vector3.Normalize(f);
        }
    }

    public Vector3 Right
    {
        get
        {
            var f = FlatForward;
            return new Vector3(f.Z, 0, -f.X);
        }
    }

    public void Update(float dt, OutmDemoMap map)
    {
        Vector2 mouse = Raylib.GetMouseDelta();
        Yaw -= mouse.X * MouseSensitivity;
        Pitch -= mouse.Y * MouseSensitivity;
        Pitch = Math.Clamp(Pitch, -1.45f, 1.45f);

        Vector3 wish = Vector3.Zero;
        if (Raylib.IsKeyDown(KeyboardKey.W)) wish += FlatForward;
        if (Raylib.IsKeyDown(KeyboardKey.S)) wish -= FlatForward;
        if (Raylib.IsKeyDown(KeyboardKey.D)) wish += Right;
        if (Raylib.IsKeyDown(KeyboardKey.A)) wish -= Right;

        if (wish.LengthSquared() > 0.0001f)
            wish = Vector3.Normalize(wish);

        float speed = MoveSpeed * (Raylib.IsKeyDown(KeyboardKey.LeftShift) ? SprintMultiplier : 1.0f);
        Position = map.MoveWithCollision(Position, wish * speed * dt, Radius);
    }

    public Camera3D ToRayCamera()
    {
        return new Camera3D
        {
            Position = Position,
            Target = Position + Forward,
            Up = Vector3.UnitY,
            FovY = 72.0f,
            Projection = CameraProjection.Perspective
        };
    }
}
