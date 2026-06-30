using UnityEngine;

public struct OUT_AIRouteRequest
{
    public Vector3 Destination;
    public Vector3 ThreatPosition;

    public float MinDistance;
    public float MaxDistance;

    public bool RequireCover;
    public bool AllowTriangulation;
    public bool RefreshIfStale;
}