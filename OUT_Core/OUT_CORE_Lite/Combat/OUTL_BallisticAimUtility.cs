using UnityEngine;

public static class OUTL_BallisticAimUtility
{
    public static Vector3 BuildAimDirection(Vector3 origin, Vector3 targetPoint, OUTL_AttackProfile profile, Vector3 targetVelocity)
    {
        return BuildAimDirection(origin, targetPoint, profile, targetVelocity, -1f, true);
    }

    public static Vector3 BuildAimDirection(Vector3 origin, Vector3 targetPoint, OUTL_AttackProfile profile, Vector3 targetVelocity, float projectileSpeedOverride)
    {
        return BuildAimDirection(origin, targetPoint, profile, targetVelocity, projectileSpeedOverride, true);
    }

    public static Vector3 BuildAimDirection(Vector3 origin, Vector3 targetPoint, OUTL_AttackProfile profile, Vector3 targetVelocity, float projectileSpeedOverride, bool applySpread)
    {
        if (profile == null)
            return SafeDirection(targetPoint - origin, Vector3.forward);

        float projectileSpeed = projectileSpeedOverride > 0f ? projectileSpeedOverride : profile.ProjectileSpeed;
        projectileSpeed = Mathf.Max(0.01f, projectileSpeed);

        Vector3 aimPoint = targetPoint;
        if (profile.UseTargetVelocityPrediction && profile.AimMode != OUTL_AimMode.Direct)
        {
            float distance = Vector3.Distance(origin, targetPoint);
            float t = Mathf.Min(profile.MaxPredictionTime, distance / projectileSpeed);
            aimPoint += targetVelocity * t * Mathf.Max(0f, profile.PredictionStrength);
        }

        Vector3 dir;
        if (profile.AimMode == OUTL_AimMode.BallisticLowArc || profile.AimMode == OUTL_AimMode.BallisticHighArc)
        {
            if (!SolveBallisticArc(origin, aimPoint, projectileSpeed, profile.ProjectileGravity, profile.AimMode == OUTL_AimMode.BallisticHighArc, out dir))
                dir = SafeDirection(aimPoint - origin, Vector3.forward);
        }
        else
        {
            dir = SafeDirection(aimPoint - origin, Vector3.forward);
        }

        if (!applySpread) return SafeDirection(dir, Vector3.forward);
        return ApplySpread(dir, profile.HorizontalSpreadDegrees, profile.VerticalSpreadDegrees, Vector3.Distance(origin, aimPoint), profile.MinSpreadDistance);
    }

    public static bool SolveBallisticArc(Vector3 origin, Vector3 target, float speed, float gravity, bool highArc, out Vector3 direction)
    {
        direction = Vector3.forward;
        speed = Mathf.Max(0.01f, speed);
        gravity = Mathf.Max(0.01f, gravity);

        Vector3 diff = target - origin;
        Vector3 diffXZ = new Vector3(diff.x, 0f, diff.z);
        float x = diffXZ.magnitude;
        float y = diff.y;
        if (x < 0.001f)
        {
            direction = y >= 0f ? Vector3.up : Vector3.down;
            return true;
        }

        float speed2 = speed * speed;
        float speed4 = speed2 * speed2;
        float root = speed4 - gravity * (gravity * x * x + 2f * y * speed2);
        if (root < 0f) return false;

        float sqrt = Mathf.Sqrt(root);
        float tan = highArc ? (speed2 + sqrt) / (gravity * x) : (speed2 - sqrt) / (gravity * x);
        float cos = 1f / Mathf.Sqrt(1f + tan * tan);
        float sin = tan * cos;

        Vector3 flat = diffXZ.normalized;
        direction = flat * cos + Vector3.up * sin;
        return direction.sqrMagnitude > 0.001f;
    }

    private static Vector3 ApplySpread(Vector3 direction, float horizontalDegrees, float verticalDegrees, float distance, float minSpreadDistance)
    {
        direction = SafeDirection(direction, Vector3.forward);
        if (distance < minSpreadDistance) return direction;
        if (horizontalDegrees <= 0f && verticalDegrees <= 0f) return direction;

        Quaternion baseRotation = Quaternion.LookRotation(direction);
        int salt = BuildSpreadSalt(direction, distance);
        float yaw = OUTL_HumanRandom.ValueSigned(0xBA11A5A1u, salt, 11) * horizontalDegrees;
        float pitch = OUTL_HumanRandom.ValueSigned(0xBA11A5A2u, salt, 23) * verticalDegrees;
        return (baseRotation * Quaternion.Euler(pitch, yaw, 0f)) * Vector3.forward;
    }

    private static int BuildSpreadSalt(Vector3 direction, float distance)
    {
        OUTL_World world = OUTL_World.Instance;
        int timeBucket = world != null ? Mathf.FloorToInt(world.WorldTime * 1000f) : Time.frameCount;
        unchecked
        {
            int x = Mathf.RoundToInt(direction.x * 10000f);
            int y = Mathf.RoundToInt(direction.y * 10000f);
            int z = Mathf.RoundToInt(direction.z * 10000f);
            int d = Mathf.RoundToInt(distance * 100f);
            int h = timeBucket;
            h = (h * 397) ^ x;
            h = (h * 397) ^ y;
            h = (h * 397) ^ z;
            h = (h * 397) ^ d;
            return h;
        }
    }

    private static Vector3 SafeDirection(Vector3 v, Vector3 fallback)
    {
        if (v.sqrMagnitude > 0.001f) return v.normalized;
        return fallback.sqrMagnitude > 0.001f ? fallback.normalized : Vector3.forward;
    }
}
