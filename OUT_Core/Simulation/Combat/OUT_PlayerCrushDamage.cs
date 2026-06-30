using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class OUT_PlayerCrushDamage : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private OUT_HL1PlayerController playerController;

    [Header("Target Filter")]
    [SerializeField] private LayerMask crushableMask = ~0;
    [SerializeField] private bool requireDamageable = true;
    [SerializeField] private bool ignoreOwnRoot = true;
    [SerializeField] private bool ignoreSizeFilterForDamageableTargets = true;
    [SerializeField][Min(0f)] private float maxTargetHeight = 0.6f;
    [SerializeField][Min(0f)] private float maxTargetRadius = 1.25f;

    [Header("Damage")]
    [SerializeField][Min(1)] private int damageAmount = 999;
    [SerializeField][Min(0f)] private float impulse = 2f;
    [SerializeField][Min(0f)] private float minHorizontalSpeed = 0.35f;
    [SerializeField][Min(0f)] private float minDownwardSpeed = 0f;
    [SerializeField][Min(0f)] private float cooldownPerTarget = 0.15f;

    [Header("Overlap Crush / Small Targets")]
    [SerializeField] private bool enableOverlapCrush = true;
    [SerializeField] private bool scanTriggers = true;
    [SerializeField][Min(0.01f)] private float scanInterval = 0.03f;
    [SerializeField][Min(0.01f)] private float scanRadiusScale = 1.22f;
    [SerializeField][Min(0.01f)] private float scanBottomOffset = 0.01f;
    [SerializeField][Min(0.05f)] private float scanHeight = 1.15f;
    [SerializeField] private bool requirePlayerGroundedForOverlap = false;

    [Header("Collision")]
    [SerializeField] private bool temporarilyIgnoreCollision = true;
    [SerializeField][Min(0f)] private float ignoreCollisionDuration = 0.6f;

    [Header("Feedback")]
    [SerializeField] private bool emitCameraShake = true;
    [SerializeField][Min(0f)] private float crushShakeAmplitude = 0.08f;
    [SerializeField][Min(0.01f)] private float crushShakeFrequency = 18f;
    [SerializeField][Min(0.01f)] private float crushShakeDuration = 0.08f;

    [Header("Debug")]
    [SerializeField] private bool logCrushDebug;
    [SerializeField] private bool drawDebugGizmos;

    private const int RecentCapacity = 32;
    private const int OverlapCapacity = 64;
    private readonly Collider[] recentColliders = new Collider[RecentCapacity];
    private readonly float[] recentTimes = new float[RecentCapacity];
    private readonly Collider[] overlapBuffer = new Collider[OverlapCapacity];

    private float nextScanTime;

    private void Reset()
    {
        controller = GetComponent<CharacterController>();
        playerController = GetComponent<OUT_HL1PlayerController>();
        ignoreSizeFilterForDamageableTargets = true;
        scanRadiusScale = 1.22f;
        scanBottomOffset = 0.01f;
        scanHeight = 1.15f;
    }

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();
        if (playerController == null)
            playerController = GetComponent<OUT_HL1PlayerController>();
    }

    private void FixedUpdate()
    {
        if (!enableOverlapCrush)
            return;

        if (Time.time < nextScanTime)
            return;

        nextScanTime = Time.time + scanInterval;
        ScanOverlapCrush();
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit == null || hit.collider == null)
            return;

        TryCrush(hit.collider, hit.point, hit.normal, "controller-hit");
    }

    private void ScanOverlapCrush()
    {
        if (controller == null)
        {
            if (logCrushDebug)
                Debug.Log("[CRUSH] rejected: controller is null", this);
            return;
        }

        if (requirePlayerGroundedForOverlap && playerController != null && !playerController.IsGrounded)
        {
            if (logCrushDebug)
                Debug.Log("[CRUSH] rejected: player is not grounded", this);
            return;
        }

        if (!PassesSpeedGate())
        {
            if (logCrushDebug)
                Debug.Log("[CRUSH] rejected: speed gate", this);
            return;
        }

        GetScanCapsule(out Vector3 bottom, out Vector3 top, out float radius);
        QueryTriggerInteraction triggerMode = scanTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
        int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, overlapBuffer, crushableMask, triggerMode);

        if (logCrushDebug)
        {
            Debug.Log(
                $"[CRUSH] scan count={count} " +
                $"bottom={bottom} top={top} radius={radius} " +
                $"hSpeed={GetHorizontalSpeed():0.000} " +
                $"downSpeed={GetDownwardSpeed():0.000}",
                this);
        }

        for (int i = 0; i < count; i++)
        {
            Collider col = overlapBuffer[i];
            if (col == null)
                continue;

            if (logCrushDebug)
            {
                Debug.Log(
                    $"[CRUSH] candidate={col.name} " +
                    $"layer={LayerMask.LayerToName(col.gameObject.layer)} " +
                    $"trigger={col.isTrigger} " +
                    $"own={IsOwnCollider(col)} " +
                    $"parentDamageable={(col.GetComponentInParent<IOutDamageable>() != null)} " +
                    $"attachedRb={(col.attachedRigidbody != null ? col.attachedRigidbody.name : "null")}",
                    this);
            }

            Vector3 hitPoint = col.ClosestPoint(transform.position + Vector3.up * scanBottomOffset);
            Vector3 direction = col.bounds.center - transform.position;
            direction.y = 0f;
            Vector3 hitNormal = direction.sqrMagnitude > 0.0001f ? -direction.normalized : Vector3.up;
            TryCrush(col, hitPoint, hitNormal, "overlap-scan");
        }
    }

    private bool TryCrush(Collider targetCollider, Vector3 hitPoint, Vector3 hitNormal, string source)
    {
        if (targetCollider == null)
        {
            if (logCrushDebug)
                Debug.Log("[CRUSH] rejected: collider is null", this);
            return false;
        }

        if (((1 << targetCollider.gameObject.layer) & crushableMask) == 0)
        {
            if (logCrushDebug)
                Debug.Log("[CRUSH] rejected by layer: " + targetCollider.name + " layer=" + LayerMask.LayerToName(targetCollider.gameObject.layer), this);
            return false;
        }

        if (ignoreOwnRoot && IsOwnCollider(targetCollider))
        {
            if (logCrushDebug)
                Debug.Log("[CRUSH] rejected own collider: " + targetCollider.name, this);
            return false;
        }

        if (!PassesSpeedGate())
        {
            if (logCrushDebug)
                Debug.Log("[CRUSH] rejected by speed gate: " + targetCollider.name, this);
            return false;
        }

        if (IsOnCooldown(targetCollider))
        {
            if (logCrushDebug)
                Debug.Log("[CRUSH] rejected by cooldown: " + targetCollider.name, this);
            return false;
        }

        IOutDamageable damageable = FindDamageable(targetCollider);
        if (requireDamageable && damageable == null)
        {
            if (logCrushDebug)
                Debug.Log("[CRUSH] rejected, no IOutDamageable: " + targetCollider.name, this);
            return false;
        }

        if (!ignoreSizeFilterForDamageableTargets || damageable == null)
        {
            if (!PassesSizeFilter(targetCollider))
            {
                if (logCrushDebug)
                    Debug.Log("[CRUSH] rejected by size: " + targetCollider.name + " bounds=" + targetCollider.bounds.size, this);
                return false;
            }
        }

        Vector3 hitDirection = playerController != null && playerController.Velocity.sqrMagnitude > 0.0001f
            ? playerController.Velocity.normalized
            : (targetCollider.transform.position - transform.position).normalized;

        OUT_DamageContext context = new OUT_DamageContext(
            instigator: gameObject,
            inflictor: gameObject,
            hitPoint: hitPoint,
            hitNormal: hitNormal.sqrMagnitude > 0.0001f ? hitNormal : Vector3.up,
            damageAmount: damageAmount,
            damageKind: OUT_DamageKind.Crush,
            hitZone: OUT_HitZone.Generic,
            hitDirection: hitDirection,
            impulse: impulse);

        bool applied = damageable != null
            ? OUT_DamageResolver.TryApply(damageable, in context)
            : OUT_DamageUtility.TryApplyDamage(targetCollider, in context);

        if (!applied)
        {
            if (logCrushDebug)
                Debug.Log("[CRUSH] damage was not applied: " + targetCollider.name, this);
            return false;
        }

        MarkCooldown(targetCollider);

        if (temporarilyIgnoreCollision && controller != null && !targetCollider.isTrigger)
        {
            Physics.IgnoreCollision(controller, targetCollider, true);
            StartCoroutine(RestoreCollisionLater(targetCollider, ignoreCollisionDuration));
        }

        if (emitCameraShake && crushShakeAmplitude > 0f)
        {
            OUT_CameraShakeService.Shake(new OUT_CameraShakeRequest(
                instigator: gameObject,
                origin: hitPoint,
                amplitude: crushShakeAmplitude,
                frequency: crushShakeFrequency,
                duration: crushShakeDuration,
                radius: 0f,
                kind: OUT_CameraShakeKind.Generic,
                requireGrounded: false));
        }

        if (logCrushDebug)
            Debug.Log("[CRUSH] crushed " + targetCollider.name + " via " + source, this);

        return true;
    }

    private bool IsOwnCollider(Collider col)
    {
        if (col == null)
            return false;

        if (controller != null && col == controller)
            return true;

        Transform t = col.transform;
        return t == transform || t.IsChildOf(transform);
    }

    private IOutDamageable FindDamageable(Collider targetCollider)
    {
        if (targetCollider == null)
            return null;

        IOutDamageable damageable = targetCollider.GetComponent<IOutDamageable>();
        if (damageable != null)
            return damageable;

        damageable = targetCollider.GetComponentInParent<IOutDamageable>();
        if (damageable != null)
            return damageable;

        if (targetCollider.attachedRigidbody != null)
        {
            damageable = targetCollider.attachedRigidbody.GetComponent<IOutDamageable>();
            if (damageable != null)
                return damageable;

            damageable = targetCollider.attachedRigidbody.GetComponentInParent<IOutDamageable>();
            if (damageable != null)
                return damageable;
        }

        return targetCollider.GetComponentInChildren<IOutDamageable>(true);
    }

    private System.Collections.IEnumerator RestoreCollisionLater(Collider target, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (controller != null && target != null)
            Physics.IgnoreCollision(controller, target, false);
    }

    private bool PassesSizeFilter(Collider col)
    {
        Bounds b = col.bounds;
        if (maxTargetHeight > 0f && b.size.y > maxTargetHeight)
            return false;

        if (maxTargetRadius > 0f)
        {
            float horizontalSize = Mathf.Max(b.size.x, b.size.z);
            if (horizontalSize > maxTargetRadius * 2f)
                return false;
        }

        return true;
    }

    private bool PassesSpeedGate()
    {
        float horizontalSpeed = GetHorizontalSpeed();
        float downwardSpeed = GetDownwardSpeed();

        bool noSpeedGate = minHorizontalSpeed <= 0f && minDownwardSpeed <= 0f;
        if (noSpeedGate)
            return true;

        bool horizontalPass = minHorizontalSpeed > 0f && horizontalSpeed >= minHorizontalSpeed;
        bool downwardPass = minDownwardSpeed > 0f && downwardSpeed >= minDownwardSpeed;
        return horizontalPass || downwardPass;
    }

    private float GetHorizontalSpeed()
    {
        Vector3 v = playerController != null ? playerController.Velocity : Vector3.zero;
        v.y = 0f;
        return v.magnitude;
    }

    private float GetDownwardSpeed()
    {
        Vector3 v = playerController != null ? playerController.Velocity : Vector3.zero;
        return Mathf.Max(0f, -v.y);
    }

    private void GetScanCapsule(out Vector3 bottom, out Vector3 top, out float radius)
    {
        float controllerRadius = controller != null ? controller.radius : 0.32f;
        float controllerHeight = controller != null ? controller.height : 1.8f;

        Vector3 controllerCenter = controller != null
            ? transform.TransformPoint(controller.center)
            : transform.position + Vector3.up * 0.9f;

        radius = Mathf.Max(0.01f, controllerRadius * scanRadiusScale);

        float halfHeight = Mathf.Max(controllerHeight * 0.5f, controllerRadius);
        float soleY = controllerCenter.y - halfHeight;

        bottom = new Vector3(controllerCenter.x, soleY + scanBottomOffset, controllerCenter.z);
        top = bottom + Vector3.up * Mathf.Max(0.05f, scanHeight);
    }

    private bool IsOnCooldown(Collider col)
    {
        float now = Time.time;
        for (int i = 0; i < RecentCapacity; i++)
        {
            if (recentColliders[i] == col)
                return now - recentTimes[i] < cooldownPerTarget;
        }

        return false;
    }

    private void MarkCooldown(Collider col)
    {
        int slot = 0;
        float oldest = float.MaxValue;

        for (int i = 0; i < RecentCapacity; i++)
        {
            if (recentColliders[i] == col)
            {
                recentTimes[i] = Time.time;
                return;
            }

            if (recentColliders[i] == null)
            {
                slot = i;
                oldest = float.MinValue;
                break;
            }

            if (recentTimes[i] < oldest)
            {
                oldest = recentTimes[i];
                slot = i;
            }
        }

        recentColliders[slot] = col;
        recentTimes[slot] = Time.time;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos)
            return;

        if (controller == null)
            controller = GetComponent<CharacterController>();

        if (controller == null)
            return;

        GetScanCapsule(out Vector3 bottom, out Vector3 top, out float radius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(bottom, radius);
        Gizmos.DrawWireSphere(top, radius);
        Gizmos.DrawLine(bottom + Vector3.forward * radius, top + Vector3.forward * radius);
        Gizmos.DrawLine(bottom - Vector3.forward * radius, top - Vector3.forward * radius);
        Gizmos.DrawLine(bottom + Vector3.right * radius, top + Vector3.right * radius);
        Gizmos.DrawLine(bottom - Vector3.right * radius, top - Vector3.right * radius);
    }
}
