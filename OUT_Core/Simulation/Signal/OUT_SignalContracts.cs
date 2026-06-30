using System;
using UnityEngine;

[Flags]
public enum OUT_SignalChannelFlags
{
    None = 0,
    Danger = 1 << 0,
    Fear = 1 << 1,
    Aggression = 1 << 2,
    Curiosity = 1 << 3,
    Suspicion = 1 << 4,
    Noise = 1 << 5,
    Death = 1 << 6,
    Pain = 1 << 7,
    Command = 1 << 8,
    Help = 1 << 9,
    Food = 1 << 10,
    Fire = 1 << 11,

    // Value / drive channels. Added after the old bits, so existing serialized masks stay valid.
    Reward = 1 << 12,
    Attraction = 1 << 13,
    Treasure = 1 << 14,
    Shelter = 1 << 15,
    Sacred = 1 << 16,
    Aversion = 1 << 17,
    Social = 1 << 18,

    Custom1 = 1 << 28,
    Custom2 = 1 << 29,
    Custom3 = 1 << 30,
    Custom4 = 1 << 31,

    All = ~0
}

public enum OUT_SignalDirection
{
    Forward = 0,
    Backward = 1,
    Echo = 2
}

public enum OUT_MemoryEventKind
{
    Generic = 0,
    SawEnemy = 1,
    LostEnemy = 2,
    HeardNoise = 3,
    SawCorpse = 4,
    TookDamage = 5,
    SawDanger = 6,
    ReceivedSignal = 7,
    SentSignal = 8,
    ScheduleChanged = 9,
    SawValuable = 10,
    FeltAttraction = 11,
    FoundFood = 12,
    FoundShelter = 13,
    Custom = 100
}

public readonly struct OUT_Signal
{
    public readonly int Id;
    public readonly GameObject Source;
    public readonly GameObject Subject;
    public readonly Vector3 Origin;
    public readonly OUT_SignalChannelFlags Channels;
    public readonly OUT_SignalDirection Direction;
    public readonly float Intensity;
    public readonly float Radius;
    public readonly float Timestamp;
    public readonly int Payload;
    public readonly string Label;

    public OUT_Signal(
        int id,
        GameObject source,
        GameObject subject,
        Vector3 origin,
        OUT_SignalChannelFlags channels,
        OUT_SignalDirection direction,
        float intensity,
        float radius,
        float timestamp,
        int payload = 0,
        string label = null)
    {
        Id = id;
        Source = source;
        Subject = subject;
        Origin = origin;
        Channels = channels;
        Direction = direction;
        Intensity = Mathf.Clamp01(intensity);
        Radius = Mathf.Max(0f, radius);
        Timestamp = timestamp;
        Payload = payload;
        Label = label ?? string.Empty;
    }

    public OUT_Signal WithDirection(OUT_SignalDirection direction, int id, float timestamp)
    {
        return new OUT_Signal(id, Source, Subject, Origin, Channels, direction, Intensity, Radius, timestamp, Payload, Label);
    }
}

public readonly struct OUT_MemoryEvent
{
    public readonly OUT_MemoryEventKind Kind;
    public readonly OUT_SignalChannelFlags Channels;
    public readonly GameObject Source;
    public readonly GameObject Subject;
    public readonly Vector3 Position;
    public readonly float Timestamp;
    public readonly float Intensity;
    public readonly int Payload;
    public readonly string Label;

    public OUT_MemoryEvent(
        OUT_MemoryEventKind kind,
        OUT_SignalChannelFlags channels,
        GameObject source,
        GameObject subject,
        Vector3 position,
        float timestamp,
        float intensity,
        int payload = 0,
        string label = null)
    {
        Kind = kind;
        Channels = channels;
        Source = source;
        Subject = subject;
        Position = position;
        Timestamp = timestamp;
        Intensity = Mathf.Clamp01(intensity);
        Payload = payload;
        Label = label ?? string.Empty;
    }

    public static OUT_MemoryEvent FromSignal(in OUT_Signal signal)
    {
        return new OUT_MemoryEvent(
            OUT_MemoryEventKind.ReceivedSignal,
            signal.Channels,
            signal.Source,
            signal.Subject,
            signal.Origin,
            signal.Timestamp,
            signal.Intensity,
            signal.Payload,
            signal.Label);
    }
}

public interface IOutSignalReceiver
{
    GameObject SignalOwner { get; }
    Vector3 SignalPosition { get; }
    bool CanReceiveSignal(in OUT_Signal signal, float distance, float attenuatedIntensity);
    void ReceiveSignal(in OUT_Signal signal, float distance, float attenuatedIntensity);
}

public interface IOutSignalEmitter
{
    OUT_Signal BuildSignal(OUT_SignalDirection direction = OUT_SignalDirection.Forward);
}
