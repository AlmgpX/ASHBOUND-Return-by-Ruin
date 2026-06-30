using UnityEngine;

[CreateAssetMenu(menuName = "OUT CORE Lite/AI/Faction Discipline Profile", fileName = "OUTL_FactionDisciplineProfile")]
public sealed class OUTL_FactionDisciplineProfile : ScriptableObject
{
    [Range(0f, 1f)] public float FriendlyFireTolerance = 0.05f;
    [Range(0f, 1f)] public float WarningThreshold = 0.25f;
    [Range(0f, 1f)] public float RetaliationThreshold = 0.85f;
    [Range(0f, 1f)] public float Discipline = 0.8f;
    [Range(0f, 2f)] public float PanicFriendlyFireMultiplier = 1.25f;
    public bool LowDisciplineCanSuppressThroughRisk = false;
}
