using System;
using UnityEngine;

public enum OUTL_NPCTravelMode
{
    None = 0,
    Exact = 1,
    Abstract = 2,
    RouteFailed = 3
}

[Serializable]
public sealed class OUTL_NPCTravelState
{
    public OUTL_NPCTravelMode Mode = OUTL_NPCTravelMode.None;
    public string RouteKey = "";
    public int StartSectorId;
    public int TargetSectorId;
    public Vector3 StartPosition;
    public Vector3 TargetPosition;
    public Vector3 AbstractPosition;
    [Range(0f, 1f)] public float RouteProgress;
    public float EstimatedArrivalTime;
    public float LastRouteUpdateTime;
    public float RouteFailedUntil;

    public void Reset()
    {
        Mode = OUTL_NPCTravelMode.None;
        RouteKey = "";
        StartSectorId = 0;
        TargetSectorId = 0;
        StartPosition = Vector3.zero;
        TargetPosition = Vector3.zero;
        AbstractPosition = Vector3.zero;
        RouteProgress = 0f;
        EstimatedArrivalTime = 0f;
        LastRouteUpdateTime = 0f;
        RouteFailedUntil = 0f;
    }
}
