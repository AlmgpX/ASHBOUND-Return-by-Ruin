using System;
using UnityEngine;

[Serializable]
public sealed class OUTL_NPCBehaviorRuntime
{
    public string CurrentScheduleId = "";
    public string CurrentEntryId = "";
    public OUTL_NPCScheduleActionType CurrentAction = OUTL_NPCScheduleActionType.Idle;
    public int CurrentTargetSector;
    public Vector3 CurrentTargetPosition;
    public string CurrentRouteKey = "";
    [Range(0f, 1f)] public float RouteProgress;
    public float EstimatedArrivalTime;
    public OUTL_RuntimeTier CurrentTier = OUTL_RuntimeTier.Full;
    public float LastBehaviorTick;
    public Vector3 LastExactPosition;
    public Vector3 AbstractPosition;
    public OUTL_StimulusType LastStimulus = OUTL_StimulusType.None;
    public string LastStimulusKey = "";
    public float LastStimulusTime;
    public OUTL_NPCScheduleActionType CurrentInterrupt = OUTL_NPCScheduleActionType.Idle;
    public string PreviousScheduleEntry = "";
    public OUTL_NPCScheduleActionType PreviousAction = OUTL_NPCScheduleActionType.Idle;
    public int HomeSector;
    public string Faction = "";
    public string Role = "";
    public OUTL_BehaviorModeId CurrentBehaviorMode = OUTL_BehaviorModeId.Normal;
    public string BehaviorModeSource = "";
    public OUTL_EgregoreCyclePhase LocalEgregorePhase = OUTL_EgregoreCyclePhase.StableWorld;
    public float LocalDanger;
    public float LocalSafety = 0.5f;
    public int SaveVersion = 1;
    public bool HasActiveInterrupt;
    public float InterruptEndTime;
    public readonly OUTL_NPCTravelState Travel = new OUTL_NPCTravelState();

    public void ClearTransient()
    {
        HasActiveInterrupt = false;
        InterruptEndTime = 0f;
        CurrentInterrupt = OUTL_NPCScheduleActionType.Idle;
        LastStimulus = OUTL_StimulusType.None;
        LastStimulusKey = "";
    }
}
