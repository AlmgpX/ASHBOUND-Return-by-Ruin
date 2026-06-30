using UnityEngine;

public enum OUTL_EnemyState : byte
{
    Wander = 0,
    Alert = 1,
    Combat = 2,
    ReturnHome = 3,
    Dead = 4
}

[CreateAssetMenu(menuName = "OUT CORE Lite/AI/Enemy Archetype Profile", fileName = "OUTL_EnemyArchetype")]
public sealed class OUTL_EnemyArchetypeProfile : ScriptableObject
{
    public string ArchetypeId = "enemy.guard";

    [Header("Perception")]
    public float SightDistance = 48f;
    [Range(1f, 360f)] public float SightAngle = 120f;
    public float EyeHeight = 1.55f;
    public LayerMask LineOfSightMask = ~0;
    public float TargetMemorySeconds = 6f;
    public float AcquireInterval = 0.25f;

    [Header("Outpost")]
    public float WanderRadius = 14f;
    public float WanderPointIntervalMin = 3f;
    public float WanderPointIntervalMax = 8f;
    public float HomeLeashDistance = 65f;
    public float ReturnStopDistance = 3f;

    [Header("Combat")]
    public OUTL_AttackProfile PrimaryAttack;
    public OUTL_AttackProfile SecondaryAttack;
    public float PreferredRange = 18f;
    public float MinimumRange = 3f;
    public float SecondaryMinimumRange = 10f;
    public float SecondaryMaximumRange = 35f;
    [Range(0f, 1f)] public float SecondaryChance = 0.25f;
    public float TurnDegreesPerSecond = 540f;
    public float CombatStimulusRadius = 34f;

    [Header("Scheduler intervals")]
    public float FullInterval = 0.05f;
    public float NearInterval = 0.10f;
    public float MidInterval = 1.0f;
    public float FarInterval = 8f;
    public float DormantInterval = 60f;

    public float GetInterval(OUTL_RuntimeTier tier)
    {
        switch (tier)
        {
            case OUTL_RuntimeTier.Full: return Mathf.Max(0.01f, FullInterval);
            case OUTL_RuntimeTier.Near: return Mathf.Max(0.01f, NearInterval);
            case OUTL_RuntimeTier.Mid: return Mathf.Max(0.05f, MidInterval);
            case OUTL_RuntimeTier.Far: return Mathf.Max(0.1f, FarInterval);
            default: return Mathf.Max(0.25f, DormantInterval);
        }
    }
}
