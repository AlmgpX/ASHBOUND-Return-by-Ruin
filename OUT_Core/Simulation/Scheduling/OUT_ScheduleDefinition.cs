using System;
using UnityEngine;

[Serializable]
public sealed class OUT_ScheduleDefinition
{
    [SerializeField] private string id;
    [SerializeField] private string debugName;
    [SerializeField] private OUT_ConditionFlags interruptMask;
    [SerializeField] private OUT_TaskDefinition[] tasks;

    public string Id => id;
    public string DebugName => string.IsNullOrWhiteSpace(debugName) ? id : debugName;
    public OUT_ConditionFlags InterruptMask => interruptMask;
    public OUT_TaskDefinition[] Tasks => tasks ?? Array.Empty<OUT_TaskDefinition>();

    public bool CanInterrupt(OUT_ConditionState currentConditions)
    {
        return interruptMask != OUT_ConditionFlags.None && currentConditions.Any(interruptMask);
    }
}