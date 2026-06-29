using System.Numerics;
using Raylib_cs;
using OUT_RayMicro.World;

namespace OUT_RayMicro.Runtime;

public sealed class OutmCameraMotor
{
    public Vector3 Position;
    public Vector3 Velocity;
    public float Yaw;
    public float Pitch;

    public float GroundMaxSpeed = 8.2f;
    public float AirMaxSpeed = 7.2f;
    public float GroundAcceleration = 58.0f;
    public float AirAcceleration = 16.0f;
    public float GroundFriction = 8.5f;
    public float StopSpeed = 2.0f;
    public float JumpSpeed = 6.2f;
    public float Gravity = 18.0f;
    public float SprintMultiplier = 1.18f;
    public float MouseSensitivity = 0.0023f;
    public float Radius = 0.35f;
    public float EyeHeight = 1.2f;
    public float CoyoteTime = 0.105f;
    public float JumpBufferTime = 0.115f;

    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool grounded = true;

    public bool Grounded => grounded;
    public float HorizontalSpeed => new Vector2(Velocity.X, Velocity.Z).Length();

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
            // Raylib/System.Numerics handedness makes the old vector feel inverted. This is the actual player-right strafe.
            return new Vector3(-f.Z, 0, f.X);
        }
    }

    public void Update(float dt, OutmDemoMap map)
    {
        dt = Math.Clamp(dt, 0.0f, 0.05f);
        UpdateLook();

        if (Raylib.IsKeyPressed(KeyboardKey.Space))
            jumpBufferTimer = JumpBufferTime;
        else
            jumpBufferTimer = MathF.Max(0.0f, jumpBufferTimer - dt);

        grounded = Position.Y <= EyeHeight + 0.001f && Velocity.Y <= 0.05f;
        if (grounded)
            coyoteTimer = CoyoteTime;
        else
            coyoteTimer = MathF.Max(0.0f, coyoteTimer - dt);

        Vector3 wishDir = ReadWishDirection();
        float maxSpeed = (grounded ? GroundMaxSpeed : AirMaxSpeed) * (Raylib.IsKeyDown(KeyboardKey.LeftShift) ? SprintMultiplier : 1.0f);

        if (grounded)
        {
            ApplyFriction(dt);
            Accelerate(wishDir, maxSpeed, GroundAcceleration, dt);
        }
        else
        {
            Accelerate(wishDir, maxSpeed, AirAcceleration, dt);
        }

        if (jumpBufferTimer > 0.0f && coyoteTimer > 0.0f)
        {
            Velocity.Y = JumpSpeed;
            grounded = false;
            coyoteTimer = 0.0f;
            jumpBufferTimer = 0.0f;
        }

        Velocity.Y -= Gravity * dt;

        Vector3 delta = Velocity * dt;
        Vector3 before = Position;
        Position = map.MoveWithCollision(Position, delta, Radius);

        if (MathF.Abs(Position.X - before.X) < 0.00001f && MathF.Abs(delta.X) > 0.00001f)
            Velocity.X = 0.0f;
        if (MathF.Abs(Position.Z - before.Z) < 0.00001f && MathF.Abs(delta.Z) > 0.00001f)
            Velocity.Z = 0.0f;

        if (Position.Y <= EyeHeight + 0.001f)
        {
            Position.Y = EyeHeight;
            if (Velocity.Y < 0.0f)
                Velocity.Y = 0.0f;
        }
    }

    private void UpdateLook()
    {
        Vector2 mouse = Raylib.GetMouseDelta();
        Yaw -= mouse.X * MouseSensitivity;
        Pitch -= mouse.Y * MouseSensitivity;
        Pitch = Math.Clamp(Pitch, -1.45f, 1.45f);
    }

    private Vector3 ReadWishDirection()
    {
        Vector3 wish = Vector3.Zero;
        if (Raylib.IsKeyDown(KeyboardKey.W)) wish += FlatForward;
        if (Raylib.IsKeyDown(KeyboardKey.S)) wish -= FlatForward;
        if (Raylib.IsKeyDown(KeyboardKey.D)) wish += Right;
        if (Raylib.IsKeyDown(KeyboardKey.A)) wish -= Right;

        return wish.LengthSquared() > 0.0001f ? Vector3.Normalize(wish) : Vector3.Zero;
    }

    private void ApplyFriction(float dt)
    {
        Vector3 lateral = new(Velocity.X, 0, Velocity.Z);
        float speed = lateral.Length();
        if (speed < 0.0001f)
            return;

        float control = MathF.Max(speed, StopSpeed);
        float drop = control * GroundFriction * dt;
        float newSpeed = MathF.Max(0.0f, speed - drop);
        float scale = newSpeed / speed;
        Velocity.X *= scale;
        Velocity.Z *= scale;
    }

    private void Accelerate(Vector3 wishDir, float wishSpeed, float accel, float dt)
    {
        if (wishDir.LengthSquared() < 0.0001f)
            return;

        Vector3 lateral = new(Velocity.X, 0, Velocity.Z);
        float currentSpeed = Vector3.Dot(lateral, wishDir);
        float addSpeed = wishSpeed - currentSpeed;
        if (addSpeed <= 0.0f)
            return;

        float accelSpeed = accel * wishSpeed * dt;
        if (accelSpeed > addSpeed)
            accelSpeed = addSpeed;

        Velocity.X += wishDir.X * accelSpeed;
        Velocity.Z += wishDir.Z * accelSpeed;
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
