using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(OUT_HealthSimple))]
public class OUT_BreakableBase : MonoBehaviour, IOutTriggerReceiver
{
    [Header("References")]
    [SerializeField] private OUT_HealthSimple health;

    [Header("Break By Trigger")]
    [SerializeField] private bool allowTriggerBreak = true;
    [SerializeField][Min(1)] private int triggerDamageAmount = 1;
    [SerializeField] private OUT_DamageKind triggerDamageKind = OUT_DamageKind.Generic;

    [Header("State")]
    [SerializeField] private bool broken;

    [Header("Events")]
    [SerializeField] private UnityEvent onBroken;

    public bool IsBroken => broken || (health != null && health.IsDead);
    public OUT_HealthSimple Health => health;

    private void Reset()
    {
        if (health == null)
            health = GetComponent<OUT_HealthSimple>();
    }

    private void Awake()
    {
        if (health == null)
            health = GetComponent<OUT_HealthSimple>();
    }

    private void OnEnable()
    {
        if (health == null)
            health = GetComponent<OUT_HealthSimple>();

        if (health != null)
            health.Died += OnDied;
    }

    private void OnDisable()
    {
        if (health != null)
            health.Died -= OnDied;
    }

    public bool CanReceiveTrigger(in OUT_TriggerContext context)
    {
        return allowTriggerBreak && !IsBroken && health != null && health.IsAlive;
    }

    public void ReceiveTrigger(in OUT_TriggerContext context)
    {
        if (!CanReceiveTrigger(context))
            return;

        OUT_DamageContext damage = new OUT_DamageContext(
            instigator: context.Instigator,
            inflictor: context.Sender,
            hitPoint: transform.position,
            hitNormal: Vector3.up,
            damageAmount: triggerDamageAmount,
            damageKind: triggerDamageKind,
            hitDirection: context.Direction);

        health.ApplyDamage(damage);
    }

    public void BreakNow(GameObject instigator = null, GameObject inflictor = null, OUT_DamageKind damageKind = OUT_DamageKind.Generic)
    {
        if (health == null || !health.IsAlive)
            return;

        OUT_DamageContext damage = new OUT_DamageContext(
            instigator: instigator,
            inflictor: inflictor != null ? inflictor : gameObject,
            hitPoint: transform.position,
            hitNormal: Vector3.up,
            damageAmount: Mathf.Max(1, health.CurrentHealth),
            damageKind: damageKind);

        health.ApplyDamage(damage);
    }

    private void OnDied(OUT_DamageContext context)
    {
        if (broken)
            return;

        broken = true;
        onBroken?.Invoke();
    }
}
