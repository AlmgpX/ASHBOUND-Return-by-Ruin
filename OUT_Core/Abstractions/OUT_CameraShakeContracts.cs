using UnityEngine;

public enum OUT_CameraShakeKind
{
    Generic = 0,
    Footstep = 1,
    Landing = 2,
    Explosion = 3,
    Damage = 4,
    Weapon = 5
}

public readonly struct OUT_CameraShakeRequest
{
    public readonly GameObject Instigator;
    public readonly Vector3 Origin;
    public readonly float Amplitude;
    public readonly float Frequency;
    public readonly float Duration;
    public readonly float Radius;
    public readonly OUT_CameraShakeKind Kind;
    public readonly bool RequireGrounded;

    public OUT_CameraShakeRequest(
        GameObject instigator,
        Vector3 origin,
        float amplitude,
        float frequency,
        float duration,
        float radius = 0f,
        OUT_CameraShakeKind kind = OUT_CameraShakeKind.Generic,
        bool requireGrounded = false)
    {
        Instigator = instigator;
        Origin = origin;
        Amplitude = amplitude;
        Frequency = frequency;
        Duration = duration;
        Radius = radius;
        Kind = kind;
        RequireGrounded = requireGrounded;
    }
}

public interface IOutCameraShakeReceiver
{
    bool CanReceiveShake(in OUT_CameraShakeRequest request);
    void ReceiveShake(in OUT_CameraShakeRequest request, float localAmplitude);
}

public interface IOutCameraShakeSource
{
    OUT_CameraShakeRequest BuildShakeRequest();
}
