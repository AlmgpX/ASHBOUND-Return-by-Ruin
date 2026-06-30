using UnityEngine;

[System.Serializable]
public class OUT_AISchedule
{
    public string Name;
    public OUT_AIConditionFlags InterruptMask;
    public OUT_AITask[] Tasks;

    public OUT_AISchedule()
    {
        Name = "Unnamed";
        InterruptMask = OUT_AIConditionFlags.None;
        Tasks = new OUT_AITask[0];
    }

    public OUT_AISchedule(string name, OUT_AIConditionFlags interruptMask, params OUT_AITask[] tasks)
    {
        Name = name;
        InterruptMask = interruptMask;
        Tasks = tasks ?? new OUT_AITask[0];
    }

    public bool IsValid => Tasks != null && Tasks.Length > 0;
}
