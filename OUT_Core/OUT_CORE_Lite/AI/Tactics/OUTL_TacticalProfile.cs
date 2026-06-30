using UnityEngine;

[CreateAssetMenu(menuName = "OUT CORE Lite/AI/Tactical Profile", fileName = "OUTL_TacticalProfile")]
public sealed class OUTL_TacticalProfile : ScriptableObject
{
    [Header("Cadence")]
    public float NearThinkInterval = 0.12f;
    public float MidThinkInterval = 0.35f;
    public float FarThinkInterval = 1.25f;
    public bool FullTacticalOnlyNear = true;

    [Header("Ranges")]
    public float PreferredRange = 18f;
    public float MinSafeRange = 4f;
    public float MeleeFallbackRange = 2.2f;
    public float StopDistance = 1.25f;

    [Header("Cover")]
    public bool UseCover = true;
    public float CoverSearchRadius = 18f;
    [Range(0f, 1f)] public float DangerToSeekCover = 0.45f;
    [Range(0f, 1f)] public float FearToRetreat = 0.75f;
    public float CoverReservationSeconds = 4f;
    public LayerMask CoverVisibilityMask = ~0;

    [Header("Combat")]
    public bool AllowSuppress = true;
    [Range(0f, 1f)] public float AggressionToAttack = 0.25f;
    [Range(0f, 1f)] public float MoraleToSuppress = 0.35f;
    public bool FireOnlyWhenVisible = true;
    public bool HoldFireOnFriendlyRisk = true;
    public float LowHealthRetreatThreshold = 15f;

    [Header("Abilities")]
    public OUTL_AbilityProfile PrimaryAbility;
    public OUTL_LeapAbilityProfile LeapAbility;

    [Header("Contracts")]
    public OUTL_AimProfile AimProfile;
    public OUTL_FactionDisciplineProfile DisciplineProfile;
}
