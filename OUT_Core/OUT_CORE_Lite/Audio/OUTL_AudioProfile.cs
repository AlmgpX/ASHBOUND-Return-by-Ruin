using UnityEngine;

[CreateAssetMenu(menuName = "OUT CORE Lite/Audio/Audio Profile", fileName = "OUTL_AudioProfile")]
public class OUTL_AudioProfile : ScriptableObject
{
    public AudioClip[] Clips;
    [Range(0f, 1f)] public float Volume = 1f;
    [Range(-3f, 3f)] public float Pitch = 1f;
    public float RandomPitch = 0.05f;
    public float SpatialBlend = 1f;
    public float MinDistance = 1f;
    public float MaxDistance = 32f;
    public bool UseUnscaledTime;

    public AudioClip Pick()
    {
        if (Clips == null || Clips.Length == 0) return null;
        if (Clips.Length == 1) return Clips[0];
        return Clips[Random.Range(0, Clips.Length)];
    }

    public void Play(Vector3 position)
    {
        AudioClip clip = Pick();
        if (clip == null) return;
        float pitch = Mathf.Clamp(Pitch + Random.Range(-RandomPitch, RandomPitch), -3f, 3f);
        OUTL_PoolSystem.PlayClipShared(clip, position, Volume, pitch, SpatialBlend, MinDistance, MaxDistance, UseUnscaledTime);
    }
}
