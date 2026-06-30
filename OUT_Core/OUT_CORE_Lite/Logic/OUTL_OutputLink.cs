using System;
using UnityEngine;

[Serializable]
public sealed class OUTL_OutputLink
{
    [Header("Source Event")]
    public string EventName = "OnTrigger";

    [Header("Target Address")]
    public string TargetName;

    [Header("Target Input")]
    public OUTL_CommandType Command = OUTL_CommandType.Activate;
    public string Key;

    [Header("Payload")]
    public float FloatValue;
    public int IntValue;

    [Header("Timing")]
    [Min(0f)] public float Delay;

    [Header("Lifecycle")]
    public bool Once;
    public bool Disabled;

    [NonSerialized] public bool Fired;

    public bool CanFire(string eventName)
    {
        if (Disabled) return false;
        if (Once && Fired) return false;
        if (string.IsNullOrEmpty(TargetName)) return false;
        if (Command == OUTL_CommandType.None) return false;
        if (string.IsNullOrEmpty(EventName)) return true;
        return string.Equals(EventName, eventName, StringComparison.Ordinal);
    }
}

public static class OUTL_OutputDispatcher
{
    public static int Fire(OUTL_World world, OUTL_EntityId source, Component context, Vector3 point, OUTL_OutputLink[] outputs, string eventName, string fallbackKey = null, int intValueOverride = 0, bool useIntValueOverride = false)
    {
        if (world == null || outputs == null) return 0;

        int fired = 0;
        for (int i = 0; i < outputs.Length; i++)
        {
            OUTL_OutputLink output = outputs[i];
            if (output == null || !output.CanFire(eventName)) continue;

            OUTL_Command command = new OUTL_Command(output.Command, source, OUTL_EntityId.None)
            {
                Key = string.IsNullOrEmpty(output.Key) ? (fallbackKey ?? eventName) : output.Key,
                FloatValue = output.FloatValue,
                IntValue = useIntValueOverride ? intValueOverride : output.IntValue,
                Point = point,
                Context = context
            };

            int sent = 1;
            if (output.Delay > 0f) world.Commands.QueueToTargetName(output.TargetName, command, output.Delay);
            else sent = world.Commands.SendToTargetName(output.TargetName, command);

            if (sent <= 0) continue;
            output.Fired = true;
            fired += sent;
        }
        return fired;
    }

    public static void ResetOnceFlags(OUTL_OutputLink[] outputs)
    {
        if (outputs == null) return;
        for (int i = 0; i < outputs.Length; i++)
            if (outputs[i] != null) outputs[i].Fired = false;
    }
}

public static class OUTL_OutputSaveUtility
{
    public static void Capture(OUTL_ComponentSaveWriter writer, string prefix, OUTL_OutputLink[] outputs)
    {
        if (writer == null || outputs == null || string.IsNullOrEmpty(prefix)) return;
        writer.SetInt(prefix + ".count", outputs.Length);
        for (int i = 0; i < outputs.Length; i++)
            if (outputs[i] != null && outputs[i].Fired)
                writer.SetFlag(prefix + "." + i + ".fired", true);
    }

    public static void Restore(OUTL_ComponentSaveReader reader, string prefix, OUTL_OutputLink[] outputs)
    {
        if (reader == null || outputs == null || string.IsNullOrEmpty(prefix)) return;
        for (int i = 0; i < outputs.Length; i++)
            if (outputs[i] != null)
                outputs[i].Fired = reader.GetFlag(prefix + "." + i + ".fired", false);
    }
}
