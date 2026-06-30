using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class OUT_DamageOnTrigger : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField][Min(1)] private int damageAmount = 1;
    [SerializeField] private OUT_DamageKind damageKind = OUT_DamageKind.Generic;
    [SerializeField] private OUT_HitZone hitZone = OUT_HitZone.Generic;
    [SerializeField] private float impulse = 0f;

    [Header("Timing")]
    [SerializeField] private bool applyOnEnter = true;
    [SerializeField] private bool applyOnStay = false;
    [SerializeField][Min(0f)] private float tickInterval = 0.25f;

    [Header("Filtering")]
    [SerializeField] private LayerMask targetMask = ~0;
    [SerializeField] private bool ignoreOwnRoot = true;
    [SerializeField] private GameObject instigator;
    [SerializeField] private GameObject inflictor;

    private readonly Dictionary<int, float> _nextDamageTimes = new Dictionary<int, float>(32);

    private void Reset()
    {
        if (inflictor == null)
            inflictor = gameObject;
    }

    private void Awake()
    {
        if (inflictor == null)
            inflictor = gameObject;
    }

    private void OnDisable()
    {
        _nextDamageTimes.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (applyOnEnter)
            TryDamage(other, forceImmediate:true);
    }

    private void OnTriggerStay(Collider other)
    {
        if (applyOnStay)
            TryDamage(other, forceImmediate:false);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null)
            return;

        _nextDamageTimes.Remove(other.GetInstanceID());
    }

    private void TryDamage(Collider other, bool forceImmediate)
    {
        if (other == null)
            return;

        if (!IsAllowedTarget(other))
            return;

        int id = other.GetInstanceID();
        if (!forceImmediate)
        {
            if (_nextDamageTimes.TryGetValue(id, out float nextTime) && Time.time < nextTime)
                return;
        }

        Vector3 hitPoint = other.bounds.ClosestPoint(transform.position);
        Vector3 hitDirection = (other.transform.position - transform.position).normalized;
        if (hitDirection.sqrMagnitude < 0.0001f)
            hitDirection = transform.forward.sqrMagnitude > 0.001f ? transform.forward : Vector3.up;

        OUT_DamageContext context = new OUT_DamageContext(
            instigator: instigator,
            inflictor: inflictor != null ? inflictor : gameObject,
            hitPoint: hitPoint,
            hitNormal: -hitDirection,
            damageAmount: damageAmount,
            damageKind: damageKind,
            hitZone: hitZone,
            hitDirection: hitDirection,
            impulse: impulse);

        if (OUT_DamageUtility.TryApplyDamage(other, context))
            _nextDamageTimes[id] = Time.time + Mathf.Max(0f, tickInterval);
    }

    private bool IsAllowedTarget(Collider other)
    {
        if (((1 << other.gameObject.layer) & targetMask) == 0)
            return false;

        if (ignoreOwnRoot && other.transform.root == transform.root)
            return false;

        return true;
    }
}
