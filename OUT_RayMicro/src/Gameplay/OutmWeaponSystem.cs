using System.Numerics;
using Raylib_cs;
using OUT_RayMicro.Core;
using OUT_RayMicro.World;

namespace OUT_RayMicro.Gameplay;

public struct OutmProjectile
{
    public bool Active;
    public Vector3 Position;
    public Vector3 Velocity;
    public float Life;
    public int Bounces;
}

public sealed class OutmWeaponSystem
{
    private readonly OutmProjectile[] projectiles = new OutmProjectile[128];
    private float fireCooldown;

    public void Update(float dt, Vector3 muzzle, Vector3 forward, OutmDemoMap map, OutmWorld world)
    {
        fireCooldown = MathF.Max(0, fireCooldown - dt);

        if (Raylib.IsMouseButtonDown(MouseButton.Left) && fireCooldown <= 0)
        {
            Fire(muzzle, forward, world);
            fireCooldown = 0.22f;
        }

        for (int i = 0; i < projectiles.Length; i++)
        {
            if (!projectiles[i].Active)
                continue;

            var p = projectiles[i];
            p.Life -= dt;
            if (p.Life <= 0)
            {
                p.Active = false;
                projectiles[i] = p;
                continue;
            }

            Vector3 previous = p.Position;
            Vector3 next = p.Position + p.Velocity * dt;

            if (map.Collides(next, 0.08f))
            {
                Vector3 normal = EstimateCollisionNormal(previous, next, map);
                if (p.Bounces < 2)
                {
                    p.Position = previous + normal * 0.08f;
                    p.Velocity = Vector3.Reflect(p.Velocity, normal) * 0.72f;
                    p.Bounces++;
                    world.Emit(new OutmEvent(OutmEventType.ProjectileBounce, EntityId.None, EntityId.None, p.Position, p.Bounces, "bullet ricochet"));
                }
                else
                {
                    p.Active = false;
                    world.Emit(new OutmEvent(OutmEventType.ProjectileHit, EntityId.None, EntityId.None, previous, 0, "bullet stopped"));
                }
            }
            else
            {
                p.Position = next;
            }

            projectiles[i] = p;
        }
    }

    public void Draw()
    {
        for (int i = 0; i < projectiles.Length; i++)
        {
            if (!projectiles[i].Active)
                continue;

            Raylib.DrawSphere(projectiles[i].Position, 0.055f, Color.Yellow);
            Vector3 tail = projectiles[i].Position - Vector3.Normalize(projectiles[i].Velocity) * 0.45f;
            Raylib.DrawLine3D(tail, projectiles[i].Position, Color.Orange);
        }
    }

    private void Fire(Vector3 muzzle, Vector3 forward, OutmWorld world)
    {
        for (int i = 0; i < projectiles.Length; i++)
        {
            if (projectiles[i].Active)
                continue;

            projectiles[i] = new OutmProjectile
            {
                Active = true,
                Position = muzzle + forward * 0.35f,
                Velocity = Vector3.Normalize(forward) * 42.0f,
                Life = 4.0f,
                Bounces = 0
            };

            world.Emit(new OutmEvent(OutmEventType.Fired, EntityId.None, EntityId.None, muzzle, 0, "revolver projectile"));
            return;
        }

        world.PushLog("Projectile pool full. The machine refuses your drama.");
    }

    private static Vector3 EstimateCollisionNormal(Vector3 previous, Vector3 next, OutmDemoMap map)
    {
        Vector3 testX = new(next.X, previous.Y, previous.Z);
        if (map.Collides(testX, 0.08f))
            return new Vector3(next.X > previous.X ? -1 : 1, 0, 0);

        Vector3 testZ = new(previous.X, previous.Y, next.Z);
        if (map.Collides(testZ, 0.08f))
            return new Vector3(0, 0, next.Z > previous.Z ? -1 : 1);

        return Vector3.UnitY;
    }
}
