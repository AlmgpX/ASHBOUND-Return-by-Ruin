using UnityEngine;

public readonly struct OUT_PerceptionStimulus
{
    public readonly Vector3 Origin;
    public readonly float Radius;
    public readonly OUT_SoundTypeFlags SoundType;
    public readonly OUT_InterestKind InterestKind;
    public readonly GameObject SourceObject;

    public OUT_PerceptionStimulus(
        Vector3 origin,
        float radius,
        OUT_SoundTypeFlags soundType,
        OUT_InterestKind interestKind,
        GameObject sourceObject = null)
    {
        Origin = origin;
        Radius = radius;
        SoundType = soundType;
        InterestKind = interestKind;
        SourceObject = sourceObject;
    }

    public bool IsValid => Radius > 0f;
}