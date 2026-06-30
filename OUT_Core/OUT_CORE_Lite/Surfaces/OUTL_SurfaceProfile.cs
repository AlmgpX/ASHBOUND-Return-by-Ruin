using UnityEngine;

[CreateAssetMenu(menuName = "OUT CORE Lite/Surfaces/Surface Profile", fileName = "OUTL_SurfaceProfile")]
public class OUTL_SurfaceProfile : ScriptableObject
{
    public string SurfaceId = "default";
    public string DisplayName = "Default";

    [Header("Movement")]
    public float MoveSpeedMultiplier = 1f;
    public float FrictionMultiplier = 1f;
    public float JumpMultiplier = 1f;
    public float GravityMultiplier = 1f;

    [Header("Medium")]
    public bool IsLiquid;
    public bool IsFlyingMedium;
    public bool IgnoreGravity;

    [Header("Damage Over Time")]
    public float DamagePerSecond;
    public string DamageKey = "surface";
    public OUTL_EffectDef[] TickEffects;

    [Header("Audio")]
    public AudioClip[] FootstepClips;
    public AudioClip[] JumpClips;
    public AudioClip[] LandingClips;
}
