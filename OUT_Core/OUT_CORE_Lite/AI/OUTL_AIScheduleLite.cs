using System;
using UnityEngine;

public enum OUTL_AITaskType : byte
{
    None = 0,
    Wait = 1,
    Stop = 2,
    FindTarget = 3,
    MoveToTarget = 4,
    AttackTarget = 5,
    MoveToPoint = 6,
    FleeFromTarget = 7,
    FaceTarget = 8,
    SendCommandToTarget = 9,
    ApplyEffects = 10,
    SetStateFlag = 11,
    Patrol = 12,
    InvestigateStimulus = 13,
    FindCover = 14,
    MoveToCover = 15,
    FollowSquadOrder = 16
}

[CreateAssetMenu(menuName = "OUT CORE Lite/AI Schedule Lite", fileName = "OUTL_AIScheduleLite")]
public class OUTL_AIScheduleLite : ScriptableObject
{
    public string ScheduleId = "schedule";
    public bool Loop = true;
    public bool RestartWhenSelected = false;
    public OUTL_AITaskDef[] Tasks;
}

[Serializable]
public class OUTL_AITaskDef
{
    public OUTL_AITaskType Type = OUTL_AITaskType.Wait;
    public float Duration = 0.25f;
    public float Distance = 2f;
    public float SpeedMultiplier = 1f;
    public OUTL_CommandType Command = OUTL_CommandType.None;
    public string StateKey;
    public bool StateValue;
    public LayerMask Mask = ~0;
    public OUTL_EffectDef[] Effects;
}
