using System.Numerics;
using Raylib_cs;
using OUT_RayMicro.Content;
using OUT_RayMicro.Core;
using OUT_RayMicro.Input;
using OUT_RayMicro.Physics;
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
    private readonly OutmWeaponDef revolver;
    private float fireCooldown;

    public OutmWeaponSystem(OutmWeaponDef revolver)
    {
        this.revolver = revolver;
    }

    public int ActiveProjectileCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < projectiles.Length; i++)
            {
                if (projectiles[i].Active)
                    count++;
            }
            return count;
        }
    }

    public OutmProjectileSaveSnapshot[] CaptureProjectileSnapshot()
    {
        var snapshot = new OutmProjectileSaveSnapshot[ActiveProjectileCount];
        int cursor = 0;
        for (int i = 0; i < projectiles.Length; i++)
        {
            OutmProjectile projectile = projectiles[i];
            if (!projectile.Active)
                continue;

            snapshot[cursor++] = new OutmProjectileSaveSnapshot
            {
                Position = OutmVector3Save.From(projectile.Position),
                Velocity = OutmVector3Save.From(projectile.Velocity),
                Life = projectile.Life,
                Bounces = projectile.Bounces
            };
        }

        return snapshot;
    }

    public void RestoreProjectileSnapshot(OutmProjectileSaveSnapshot[] snapshot)
    {
        ClearProjectiles();
        int count = Math.Min(projectiles.Length, snapshot.Length);
        for (int i = 0; i < count; i++)
        {
            OutmProjectileSaveSnapshot saved = snapshot[i];
            projectiles[i] = new OutmProjectile
            {
                Active = saved.Life > 0.0f,
                Position = saved.Position.ToVector3(),
                Velocity = saved.Velocity.ToVector3(),
                Life = MathF.Max(0.0f, saved.Life),
                Bounces = Math.Max(0, saved.Bounces)
            };
        }
    }

    public void ClearProjectiles()
    {
        Array.Clear(projectiles, 0, projectiles.Length);
    }

    public void Update(in OutmInputFrame input, Vector3 muzzle, Vector3 forward, IOutmCollisionWorld collision, OutmDemoMap map, OutmSurfaceRegistry surfaces, OutmWorld world)
    {
        float dt = Math.Clamp(input.DeltaTime, 0.0f, 0.05f);
        fireCooldown = MathF.Max(0, fireCooldown - dt);

        if (input.IsDown(OutmButtons.FirePrimary) && fireCooldown <= 0)
        {
            Fire(muzzle, forward, world);
            fireCooldown = revolver.Cooldown;
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

            if (collision.CollidesSphere(next, revolver.ProjectileRadius))
            {
                Vector3 travel = next - previous;
                OutmRayHit hit = travel.LengthSquared() > 0.000001f
                    ? collision.Raycast(previous, travel, travel.Length() + revolver.ProjectileRadius)
                    : OutmRayHit.None;
                Vector3 normal = hit.Hit ? hit.Normal : EstimateCollisionNormal(previous, next, collision, revolver.ProjectileRadius);
                string surfaceId = hit.Hit ? hit.SurfaceId : OutmSurfaceId.Stone.Value;
                OutmSurfaceDef surface = surfaces.Get(surfaceId);

                if (p.Bounces < revolver.MaxBounces)
                {
                    p.Position = previous + normal * revolver.ProjectileRadius;
                    p.Velocity = Vector3.Reflect(p.Velocity, normal) * revolver.BounceEnergy;
                    p.Bounces++;
                    world.Emit(new OutmEvent(OutmEventType.ProjectileBounce, EntityId.None, EntityId.None, p.Position, p.Bounces, $"{surface.ImpactTag}: ricochet"));
                }
                else
                {
                    p.Active = false;
                    world.Emit(new OutmEvent(OutmEventType.ProjectileHit, EntityId.None, EntityId.None, previous, 0, $"{surface.ImpactTag}: stopped"));
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
            Vector3 tail = projectiles[i].Velocity.LengthSquared() > 0.0001f
                ? projectiles[i].Position - Vector3.Normalize(projectiles[i].Velocity) * 0.45f
                : projectiles[i].Position;
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
                Velocity = Vector3.Normalize(forward) * revolver.ProjectileSpeed,
                Life = revolver.ProjectileLife,
                Bounces = 0
            };

            world.Emit(new OutmEvent(OutmEventType.Fired, EntityId.None, EntityId.None, muzzle, 0, revolver.Id));
            return;
        }

        world.PushLog("Projectile pool full. The machine refuses your drama.");
    }

    private static Vector3 EstimateCollisionNormal(Vector3 previous, Vector3 next, IOutmCollisionWorld collision, float radius)
    {
        Vector3 testX = new(next.X, previous.Y, previous.Z);
        if (collision.CollidesSphere(testX, radius))
            return new Vector3(next.X > previous.X ? -1 : 1, 0, 0);

        Vector3 testZ = new(previous.X, previous.Y, next.Z);
        if (collision.CollidesSphere(testZ, radius))
            return new Vector3(0, 0, next.Z > previous.Z ? -1 : 1);

        return Vector3.UnitY;
    }
}
