using UnityEngine;

[CreateAssetMenu(menuName = "OUT CORE Lite/AI/Aim Profile", fileName = "OUTL_AimProfile")]
public sealed class OUTL_AimProfile : ScriptableObject
{
    public float AimAngularSpeed = 240f;
    public float AimErrorNear = 1.5f;
    public float AimErrorFar = 6f;
    public float AimSettleTimeMin = 0.08f;
    public float AimSettleTimeMax = 0.30f;
    [Range(0f, 1f)] public float HoldAimChance = 0.1f;
    [Range(0f, 1f)] public float FakeAimChance = 0.05f;
    public float BurstJitterMin = 0f;
    public float BurstJitterMax = 0.15f;
    public float ReactionDelayMin = 0.15f;
    public float ReactionDelayMax = 0.45f;
    public float FireDelayMin = 0.05f;
    public float FireDelayMax = 0.25f;
    public float AimHoldSeconds = 0.12f;
    public float MaxFireAngleError = 8f;
    public float AimHeight = 1.1f;
    public float AimRadius = 0.25f;
    public bool RequireLineOfSight = true;
    public bool UseFriendlyFire = true;
    public LayerMask LineOfFireMask = ~0;
    public OUTL_FactionDisciplineProfile DisciplineProfile;
}
