using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public class OUT_RandomAudioSetPlayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;

    [Header("Clips")]
    [SerializeField] private AudioClip[] clips;

    [Header("Randomization")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);
    [SerializeField] private Vector2 volumeRange = new Vector2(0.95f, 1f);

    [Header("Playback")]
    [SerializeField] private bool playAsOneShot = true;
    [SerializeField] [Range(0f, 1f)] private float spatialBlend = 1f;
    [SerializeField] private AudioMixerGroup outputGroup;

    public bool HasClips => clips != null && clips.Length > 0;

    private void Reset()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public bool PlayRandom()
    {
        EnsureAudioSource();
        AudioClip clip = GetRandomClip();
        if (clip == null || audioSource == null)
            return false;

        ConfigureSource();
        audioSource.pitch = GetRandomPitch();
        float volume = GetRandomVolume();

        if (playAsOneShot)
            audioSource.PlayOneShot(clip, volume);
        else
        {
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.Play();
        }

        return true;
    }

    public bool PlayRandomAtPoint(Vector3 worldPosition)
    {
        AudioClip clip = GetRandomClip();
        if (clip == null)
            return false;

        float pitch = GetRandomPitch();
        float volume = GetRandomVolume();

        GameObject temp = new GameObject("OUT_RandomAudioOneShot");
        temp.transform.position = worldPosition;

        AudioSource source = temp.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = outputGroup;
        source.spatialBlend = spatialBlend;
        source.pitch = pitch;
        source.volume = volume;
        source.PlayOneShot(clip, volume);

        Object.Destroy(temp, Mathf.Max(clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch)), 0.05f) + 0.05f);
        return true;
    }

    public AudioClip GetRandomClip()
    {
        if (!HasClips)
            return null;

        return clips[Random.Range(0, clips.Length)];
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void ConfigureSource()
    {
        if (audioSource == null)
            return;

        audioSource.spatialBlend = spatialBlend;
        audioSource.outputAudioMixerGroup = outputGroup;
    }

    private float GetRandomPitch()
    {
        float min = Mathf.Min(pitchRange.x, pitchRange.y);
        float max = Mathf.Max(pitchRange.x, pitchRange.y);
        return Random.Range(min, max);
    }

    private float GetRandomVolume()
    {
        float min = Mathf.Min(volumeRange.x, volumeRange.y);
        float max = Mathf.Max(volumeRange.x, volumeRange.y);
        return Random.Range(min, max);
    }
}
