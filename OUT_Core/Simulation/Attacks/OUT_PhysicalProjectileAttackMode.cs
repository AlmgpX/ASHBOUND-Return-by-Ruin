using OUTPool = OutCore.pool.OUT;
using UnityEngine;

public class OUT_PhysicalProjectileAttackMode : IOutAttackMode
{
    public void Execute(in OUT_AttackContext context)
    {
        if (context.ProjectilePrefab == null)
            return;

        int pelletCount = Mathf.Max(1, context.PelletCount);
        Vector3 baseDirection = context.Direction.sqrMagnitude > 0.0001f
            ? context.Direction.normalized
            : Vector3.forward;

        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 shotDirection = ApplySpread(baseDirection, context.Spread);
            Quaternion rotation = Quaternion.LookRotation(shotDirection);

            GameObject projectileObject = OUTPool.Instantiate(context.ProjectilePrefab, context.Origin, rotation);

            if (projectileObject == null)
                continue;

            OUT_ProjectileBase projectile = projectileObject.GetComponent<OUT_ProjectileBase>();
            if (projectile == null)
            {
                projectileObject.SetActive(false);
                continue;
            }

            OUT_AttackContext shotContext = context;
            shotContext.Direction = shotDirection;
            projectile.Launch(in shotContext);
        }
    }

    private Vector3 ApplySpread(Vector3 direction, Vector2 spread)
    {
        if (spread == Vector2.zero)
            return direction;

        Vector3 right = Vector3.Cross(direction, Vector3.up);
        if (right.sqrMagnitude < 0.0001f)
            right = Vector3.right;
        right.Normalize();

        Vector3 up = Vector3.Cross(right, direction).normalized;

        float x = Random.Range(-spread.x, spread.x);
        float y = Random.Range(-spread.y, spread.y);

        return (direction + right * x + up * y).normalized;
    }
}
