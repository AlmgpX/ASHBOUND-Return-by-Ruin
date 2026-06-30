using UnityEngine;

[CreateAssetMenu(menuName = "OUT CORE Lite/AI/Squad Def", fileName = "OUTL_SquadDef")]
public sealed class OUTL_SquadDef : ScriptableObject
{
    public string SquadId = "squad";
    public float SharedTargetMemory = 5f;
    public float CoverReservationSeconds = 4f;
    public int MaxMembers = 8;
    public bool ShareCoverReservations = true;
    public bool ShareSuppressionOrders = true;
}
