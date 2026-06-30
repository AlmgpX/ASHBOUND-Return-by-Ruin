using UnityEngine;

public enum OUTL_AimMode : byte
{
    Direct = 0,
    PredictLinear = 1,
    BallisticLowArc = 2,
    BallisticHighArc = 3
}

public enum OUTL_ExplosionFalloff : byte
{
    None = 0,
    Linear = 1,
    Smooth = 2
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Combat/Attack Profile", fileName = "OUTL_AttackProfile")]
public class OUTL_AttackProfile : ScriptableObject
{
    public string AttackId = "attack";
    public OUTL_AttackMode Mode = OUTL_AttackMode.Hitscan;
    public float Damage = 10f;
    public string HitDamageKey = "hit";
    public float Range = 40f;
    public float Radius = 0.25f;
    public float Cooldown = 0.25f;

    [Header("Melee Shape")]
    public float MeleeArcDegrees = 120f;
    public float MeleeMinRadius = 0.45f;
    public float MeleeHeight = 1.35f;
    public float MeleeForwardBias = 0.55f;
    public bool MeleeCanHitTriggers = false;

    [Header("Projectile")]
    public float ProjectileSpeed = 28f;
    public float ProjectileLifetime = 8f;
    public bool ProjectileUsesGravity = false;
    public float ProjectileGravity = 9.81f;
    [Min(1)] public int ProjectilesPerShot = 1;

    [Header("Projectile Impact Rules")]
    public bool ProjectileIgnoreTriggers = true;
    public bool ProjectileDetonateOnEntityHit = true;
    public bool ProjectileDetonateOnWorldHit = true;
    public bool ProjectileDetonateOnLifetimeEnd = false;
    public bool ProjectileBounceOnWorldHit = false;
    public int ProjectileMaxBounces = 0;
    [Range(0f, 1.5f)] public float ProjectileBounceDamping = 0.65f;
    [Range(0f, 1.5f)] public float ProjectileFrictionDamping = 0.92f;

    [Header("Explosion")]
    public bool UseExplosion = false;
    public float ExplosionRadius = 0f;
    public float ExplosionDamage = 0f;
    public OUTL_ExplosionFalloff ExplosionFalloff = OUTL_ExplosionFalloff.Smooth;
    public LayerMask ExplosionHitMask = ~0;
    public bool ExplosionRequireLineOfSight = true;
    public LayerMask ExplosionObstacleMask = ~0;
    public string ExplosionDamageKey = "explosion";

    [Header("Aim / AI")]
    public OUTL_AimMode AimMode = OUTL_AimMode.Direct;
    public bool UseTargetVelocityPrediction = true;
    public float PredictionStrength = 1f;
    public float MaxPredictionTime = 1.5f;
    public float HorizontalSpreadDegrees = 0f;
    public float VerticalSpreadDegrees = 0f;
    public float MinSpreadDistance = 0f;

    public LayerMask HitMask = ~0;
    public GameObject ProjectilePrefab;
    public GameObject MuzzleVFX;
    public GameObject ImpactVFX;
    public AudioClip FireSound;
    public AudioClip ImpactSound;
    public OUTL_EffectDef[] ExtraHitEffects;

    public bool HasExplosion
    {
        get { return UseExplosion && ExplosionRadius > 0f && ExplosionDamage > 0f; }
    }
}
