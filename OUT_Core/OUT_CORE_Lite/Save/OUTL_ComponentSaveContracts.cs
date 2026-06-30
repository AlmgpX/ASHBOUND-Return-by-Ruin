using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OUTL_ComponentSavePayload
{
    public string Key;
    public List<OUTL_FloatPair> Floats = new List<OUTL_FloatPair>();
    public List<OUTL_IntPair> Ints = new List<OUTL_IntPair>();
    public List<OUTL_StringPair> Strings = new List<OUTL_StringPair>();
    public List<string> Flags = new List<string>();
}

public interface OUTL_IComponentSaveParticipant
{
    string OUTL_SaveKey { get; }
    void OUTL_Capture(OUTL_ComponentSaveWriter writer);
    void OUTL_Restore(OUTL_ComponentSaveReader reader);
}

public sealed class OUTL_ComponentSaveWriter
{
    private readonly OUTL_ComponentSavePayload payload;

    public OUTL_ComponentSaveWriter(OUTL_ComponentSavePayload payload)
    {
        this.payload = payload;
    }

    public OUTL_ComponentSavePayload Payload { get { return payload; } }

    public bool HasData
    {
        get
        {
            return payload != null &&
                   ((payload.Floats != null && payload.Floats.Count > 0) ||
                    (payload.Ints != null && payload.Ints.Count > 0) ||
                    (payload.Strings != null && payload.Strings.Count > 0) ||
                    (payload.Flags != null && payload.Flags.Count > 0));
        }
    }

    public void SetFloat(string key, float value)
    {
        if (payload == null || string.IsNullOrEmpty(key)) return;
        payload.Floats.Add(new OUTL_FloatPair { Key = key, Value = value });
    }

    public void SetInt(string key, int value)
    {
        if (payload == null || string.IsNullOrEmpty(key)) return;
        payload.Ints.Add(new OUTL_IntPair { Key = key, Value = value });
    }

    public void SetString(string key, string value)
    {
        if (payload == null || string.IsNullOrEmpty(key)) return;
        payload.Strings.Add(new OUTL_StringPair { Key = key, Value = value ?? string.Empty });
    }

    public void SetFlag(string key, bool value)
    {
        if (payload == null || string.IsNullOrEmpty(key) || !value) return;
        payload.Flags.Add(key);
    }
}

public sealed class OUTL_ComponentSaveReader
{
    private readonly OUTL_ComponentSavePayload payload;

    public OUTL_ComponentSaveReader(OUTL_ComponentSavePayload payload)
    {
        this.payload = payload;
    }

    public bool HasPayload { get { return payload != null; } }

    public float GetFloat(string key, float fallback = 0f)
    {
        if (payload == null || payload.Floats == null || string.IsNullOrEmpty(key)) return fallback;
        for (int i = payload.Floats.Count - 1; i >= 0; i--)
            if (payload.Floats[i].Key == key) return payload.Floats[i].Value;
        return fallback;
    }

    public int GetInt(string key, int fallback = 0)
    {
        if (payload == null || payload.Ints == null || string.IsNullOrEmpty(key)) return fallback;
        for (int i = payload.Ints.Count - 1; i >= 0; i--)
            if (payload.Ints[i].Key == key) return payload.Ints[i].Value;
        return fallback;
    }

    public string GetString(string key, string fallback = "")
    {
        if (payload == null || payload.Strings == null || string.IsNullOrEmpty(key)) return fallback;
        for (int i = payload.Strings.Count - 1; i >= 0; i--)
            if (payload.Strings[i].Key == key) return payload.Strings[i].Value;
        return fallback;
    }

    public bool GetFlag(string key, bool fallback = false)
    {
        if (payload == null || payload.Flags == null || string.IsNullOrEmpty(key)) return fallback;
        for (int i = 0; i < payload.Flags.Count; i++)
            if (payload.Flags[i] == key) return true;
        return fallback;
    }
}

public static class OUTL_ComponentSaveUtility
{
    private const string LegacySaveStatePrefix = "OUTL_ISaveState:";

    public static OUTL_ComponentSavePayload Capture(OUTL_IComponentSaveParticipant participant)
    {
        if (participant == null || string.IsNullOrEmpty(participant.OUTL_SaveKey)) return null;
        OUTL_ComponentSavePayload payload = new OUTL_ComponentSavePayload { Key = participant.OUTL_SaveKey };
        OUTL_ComponentSaveWriter writer = new OUTL_ComponentSaveWriter(payload);
        participant.OUTL_Capture(writer);
        return writer.HasData ? payload : null;
    }

    public static int CaptureComponents(OUTL_EntityAdapter adapter, List<OUTL_ComponentSavePayload> output)
    {
        if (adapter == null || output == null) return 0;
        int count = 0;
        MonoBehaviour[] behaviours = adapter.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null) continue;

            OUTL_IComponentSaveParticipant participant = behaviour as OUTL_IComponentSaveParticipant;
            if (participant != null)
            {
                OUTL_ComponentSavePayload payload = Capture(participant);
                if (payload != null)
                {
                    output.Add(payload);
                    count++;
                }
                continue;
            }

            OUTL_ISaveState legacy = behaviour as OUTL_ISaveState;
            if (legacy != null)
            {
                OUTL_ComponentSavePayload payload = CaptureLegacySaveState(legacy, behaviour.GetType());
                if (payload != null)
                {
                    output.Add(payload);
                    count++;
                }
            }
        }
        return count;
    }

    public static int RestoreComponents(OUTL_EntityAdapter adapter, List<OUTL_ComponentSavePayload> payloads)
    {
        if (adapter == null || payloads == null || payloads.Count == 0) return 0;
        int count = 0;
        MonoBehaviour[] behaviours = adapter.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null) continue;

            OUTL_IComponentSaveParticipant participant = behaviour as OUTL_IComponentSaveParticipant;
            if (participant != null)
            {
                OUTL_ComponentSavePayload payload = FindPayload(payloads, participant.OUTL_SaveKey);
                if (payload != null)
                {
                    participant.OUTL_Restore(new OUTL_ComponentSaveReader(payload));
                    count++;
                }
                continue;
            }

            OUTL_ISaveState legacy = behaviour as OUTL_ISaveState;
            if (legacy != null)
            {
                OUTL_ComponentSavePayload payload = FindPayload(payloads, BuildLegacySaveStateKey(behaviour.GetType()));
                if (payload != null)
                {
                    RestoreLegacySaveState(legacy, payload);
                    count++;
                }
            }
        }
        return count;
    }

    public static OUTL_ComponentSavePayload FindPayload(List<OUTL_ComponentSavePayload> payloads, string key)
    {
        if (payloads == null || string.IsNullOrEmpty(key)) return null;
        for (int i = 0; i < payloads.Count; i++)
            if (payloads[i] != null && payloads[i].Key == key) return payloads[i];
        return null;
    }

    private static OUTL_ComponentSavePayload CaptureLegacySaveState(OUTL_ISaveState saveState, Type type)
    {
        if (saveState == null || type == null) return null;
        OUTL_SaveData data = new OUTL_SaveData();
        saveState.OUTL_Capture(data);
        if (data.Values == null || data.Values.Count == 0) return null;

        OUTL_ComponentSavePayload payload = new OUTL_ComponentSavePayload { Key = BuildLegacySaveStateKey(type) };
        foreach (KeyValuePair<string, string> pair in data.Values)
            payload.Strings.Add(new OUTL_StringPair { Key = pair.Key, Value = pair.Value });
        return payload.Strings.Count > 0 ? payload : null;
    }

    private static void RestoreLegacySaveState(OUTL_ISaveState saveState, OUTL_ComponentSavePayload payload)
    {
        if (saveState == null || payload == null) return;
        OUTL_SaveData data = new OUTL_SaveData();
        if (payload.Strings != null)
        {
            for (int i = 0; i < payload.Strings.Count; i++)
                data.Set(payload.Strings[i].Key, payload.Strings[i].Value);
        }
        saveState.OUTL_Restore(data);
    }

    private static string BuildLegacySaveStateKey(Type type)
    {
        return LegacySaveStatePrefix + (type != null ? type.FullName : "Unknown");
    }
}
