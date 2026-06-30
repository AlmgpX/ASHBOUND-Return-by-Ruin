using UnityEngine;

[CreateAssetMenu(menuName = "OUT CORE Lite/Abilities/Leap Ability Profile", fileName = "OUTL_LeapAbilityProfile")]
public sealed class OUTL_LeapAbilityProfile : OUTL_AbilityProfile
{
    public float LeapSpeed = 12f;
    public float LeapArcHeight = 2.5f;
    public float LeapDuration = 0.65f;
    public float ImpactRadius = 1.25f;
    public float ImpactDamage = 20f;
    public float PreferWhenTargetDistanceMin = 3f;
    public float PreferWhenTargetDistanceMax = 10f;
    [Range(0f, 1f)] public float OvershootChance = 0.1f;
    public float LandingRecovery = 0.35f;
    public bool UsePhysicsImpulse;
    public bool UseCharacterMotor = true;
}
