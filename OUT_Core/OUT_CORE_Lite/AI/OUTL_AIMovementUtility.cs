using UnityEngine;

public static class OUTL_AIMovementUtility
{
    public static bool MoveTowards(Transform root, OUTL_AIProfile profile, OUTL_NavMeshMover navMover, bool useNavMeshMover, bool moveEnabled, Vector3 point, float stopDistance, float speedMultiplier, float deltaTime)
    {
        if (!moveEnabled || root == null) return true;
        float stopSqr = stopDistance * stopDistance;
        Vector3 to = point - root.position;
        to.y = 0f;
        if (to.sqrMagnitude <= stopSqr)
        {
            Stop(navMover, "ai_actor");
            return true;
        }

        if (useNavMeshMover && navMover != null)
        {
            navMover.SetDestination(point, "ai_actor");
            return false;
        }

        Vector3 dir = to.normalized;
        float speed = profile != null ? profile.MoveSpeed : 3f;
        root.position += dir * speed * Mathf.Max(0.01f, speedMultiplier) * deltaTime;
        Face(root, point, deltaTime);
        return false;
    }

    public static void Face(Transform root, Vector3 point, float deltaTime)
    {
        if (root == null) return;
        Vector3 dir = point - root.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            root.rotation = Quaternion.RotateTowards(root.rotation, Quaternion.LookRotation(dir.normalized), 720f * deltaTime);
    }

    public static void Stop(OUTL_NavMeshMover navMover)
    {
        Stop(navMover, "ai_actor");
    }

    public static void Stop(OUTL_NavMeshMover navMover, string authority)
    {
        if (navMover != null) navMover.Stop(authority);
    }
}
