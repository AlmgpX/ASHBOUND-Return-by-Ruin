using System;
using UnityEngine;

[Serializable]
public sealed class OUTL_NPCStimulusInterruptPolicy
{
    public OUTL_StimulusType[] StimulusTypes;

    // Editor/setup convenience bridge. Real runtime matching still uses StimulusTypes.
    public OUTL_StimulusType StimulusType
    {
        get { return StimulusTypes != null && StimulusTypes.Length > 0 ? StimulusTypes[0] : OUTL_StimulusType.None; }
        set { StimulusTypes = value == OUTL_StimulusType.None ? null : new[] { value }; }
    }

    public float MinimumPriority = 0.5f;
    public OUTL_NPCScheduleActionType InterruptAction = OUTL_NPCScheduleActionType.Investigate;
    public float Cooldown = 2f;
    public float MaxDuration = 8f;
    public bool CanInterruptCombat = true;
    public bool CanInterruptWork = true;
    public bool CanInterruptTravel = true;
    public bool ResumePreviousScheduleAfterCompletion = true;
    public bool WriteMemory = true;

    private float nextAllowedTime;

    public bool Matches(OUTL_Stimulus stimulus, OUTL_NPCScheduleActionType currentAction, float time)
    {
        if (time < nextAllowedTime) return false;
        if (stimulus.Priority < MinimumPriority) return false;
        if (!CanInterruptCombat && currentAction == OUTL_NPCScheduleActionType.Combat) return false;
        if (!CanInterruptWork && (currentAction == OUTL_NPCScheduleActionType.Work || currentAction == OUTL_NPCScheduleActionType.Trade)) return false;
        if (!CanInterruptTravel && (currentAction == OUTL_NPCScheduleActionType.TravelTo || currentAction == OUTL_NPCScheduleActionType.ReturnHome)) return false;
        if (StimulusTypes == null || StimulusTypes.Length == 0) return true;
        for (int i = 0; i < StimulusTypes.Length; i++)
            if (StimulusTypes[i] == stimulus.Type)
                return true;
        return false;
    }

    public void MarkUsed(float time)
    {
        nextAllowedTime = time + Mathf.Max(0f, Cooldown);
    }
}
