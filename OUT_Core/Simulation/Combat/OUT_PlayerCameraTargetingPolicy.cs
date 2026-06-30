using UnityEngine;

[DisallowMultipleComponent]
public class OUT_PlayerCameraTargetingPolicy : MonoBehaviour, IOutWeaponTargetingPolicy
{
    [Header("References")]
    [SerializeField] private Camera aimCamera;

    [Header("Raycast")]
    [SerializeField] private LayerMask aimMask = ~0;
    [SerializeField][Min(0.1f)] private float maxDistance = 2048f;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Fallback")]
    [SerializeField] private bool useCameraMainFallback = true;

    public bool TryBuildAimContext(GameObject instigator, Transform fireOrigin, out OUT_WeaponAimContext context)
    {
        context = default;

        Camera cam = aimCamera != null ? aimCamera : (useCameraMainFallback ? Camera.main : null);
        if (cam == null || fireOrigin == null)
            return false;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        Vector3 aimPoint;
        GameObject target = null;
        Vector3 targetVelocity = Vector3.zero;
        bool hasDirectHit = false;
        RaycastHit directHit = default;

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, aimMask, triggerInteraction))
        {
            hasDirectHit = true;
            directHit = hit;
            aimPoint = hit.point;

            if (hit.collider != null)
            {
                target = hit.collider.transform.root.gameObject;
                Rigidbody rb = hit.collider.attachedRigidbody;
                if (rb != null)
                    targetVelocity = rb.velocity;
            }
        }
        else
        {
            aimPoint = ray.origin + ray.direction * maxDistance;
        }

        Vector3 aimDirection = aimPoint - fireOrigin.position;
        if (aimDirection.sqrMagnitude <= 0.0001f)
            aimDirection = fireOrigin.forward;

        aimDirection.Normalize();

        context = new OUT_WeaponAimContext
        {
            Instigator = instigator,
            FireOrigin = fireOrigin,
            AimCamera = cam,
            Target = target,
            Origin = fireOrigin.position,
            AimPoint = aimPoint,
            AimDirection = aimDirection,
            TargetVelocity = targetVelocity,
            DistanceToAimPoint = Vector3.Distance(fireOrigin.position, aimPoint),
            HasTarget = target != null,
            HasDirectHit = hasDirectHit,
            DirectHit = directHit
        };

        return true;
    }
}