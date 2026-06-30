using UnityEngine;

public struct OUTL_CoverQuery
{
    public OUTL_EntityAdapter Seeker;
    public Vector3 SeekerPosition;
    public Vector3 ThreatPosition;
    public float SearchRadius;
    public int SectorId;
    public OUTL_WeaponRole WeaponRole;
    public LayerMask VisibilityMask;
    public bool RequireBlocksThreat;
    public float Time;
}

public struct OUTL_CoverQueryResult
{
    public OUTL_CoverPoint Point;
    public Vector3 StandPoint;
    public Vector3 PeekPoint;
    public float Score;
    public bool IsValid { get { return Point != null; } }
}
