using UnityEngine;

[System.Serializable]
public struct OUT_WeaponFireContext
{
    public GameObject Instigator;
    public Transform InstigatorRoot;

    public Transform FireOrigin;
    public Vector3 Origin;

    public GameObject Target;
    public Vector3 AimPoint;
    public Vector3 AimDirection;
    public Vector3 TargetVelocity;

    public LayerMask HitMask;
    public QueryTriggerInteraction TriggerInteraction;

    public int Damage;
    public int Pellets;

    public float MaxDistance;
    public float SpreadDegrees;

    public float ProjectileSpeed;
    public float ProjectileLifetime;

    public float MeleeRadius;
    public float BashForce;

    public bool IgnoreInstigatorRoot;
    public bool UsePrediction;

    public GameObject ProjectilePrefab;
    public GameObject MuzzleFlashPrefab;
    public Transform MuzzleFlashPoint;
    public GameObject ImpactVFX;

    public OUT_Q1DamageKind DamageKind;

    public bool IsValid
    {
        get
        {
            return FireOrigin != null && AimDirection.sqrMagnitude > 0.0001f;
        }
    }
}