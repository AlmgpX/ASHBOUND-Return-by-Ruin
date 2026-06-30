using UnityEngine;

public struct OUT_AttackContext
{
    public GameObject Instigator;
    public GameObject Inflictor;
    public Transform OwnerRoot;

    public Vector3 Origin;
    public Vector3 Direction;
    public float Distance;

    public LayerMask HitMask;
    public QueryTriggerInteraction TriggerInteraction;

    public int Damage;
    public float Impulse;
    public OUT_DamageKind DamageKind;

    public int PelletCount;
    public Vector2 Spread;
    public float TraceRadius;

    public GameObject ProjectilePrefab;
    public float ProjectileSpeed;

    public GameObject ImpactVfx;
    public GameObject FallbackImpactVfx;
    public GameObject TracerVfx;

    public bool IgnoreOwnerRoot;
}