using UnityEngine;

[DisallowMultipleComponent]
public class OUT_DeathResponderSimple : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private OUT_HealthSimple health;
    [SerializeField] private Transform spawnPoint;

    [Header("Prefabs")]
    [SerializeField] private GameObject[] genericDeathPrefabs;
    [SerializeField] private GameObject[] crushDeathPrefabs;

    [Header("Audio Players")]
    [SerializeField] private OUT_RandomAudioSetPlayer genericDeathAudioPlayer;
    [SerializeField] private OUT_RandomAudioSetPlayer crushDeathAudioPlayer;

    [Header("Audio Fallback")]
    [SerializeField] private AudioClip[] genericDeathClips;
    [SerializeField] private AudioClip[] crushDeathClips;
    [SerializeField] [Range(0f, 1f)] private float audioVolume = 1f;

    [Header("Spawn")]
    [SerializeField] private bool useHitPointWhenValid = true;
    [SerializeField] private bool randomYaw = true;

    private void Awake()
    {
        if (health == null)
            health = GetComponent<OUT_HealthSimple>();
    }

    private void OnEnable()
    {
        if (health == null)
            health = GetComponent<OUT_HealthSimple>();

        if (health != null)
            health.Died += OnDied;
    }

    private void OnDisable()
    {
        if (health != null)
            health.Died -= OnDied;
    }

    private void OnDied(OUT_DamageContext context)
    {
        bool isCrush = context.DamageKind == OUT_DamageKind.Crush;
        Vector3 position = ResolveSpawnPosition(context);
        Quaternion rotation = randomYaw
            ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
            : (spawnPoint != null ? spawnPoint.rotation : transform.rotation);

        SpawnPrefab(isCrush ? crushDeathPrefabs : genericDeathPrefabs, position, rotation);
        PlayDeathAudio(isCrush, position);
    }

    private Vector3 ResolveSpawnPosition(OUT_DamageContext context)
    {
        if (useHitPointWhenValid && context.HitPoint != Vector3.zero)
            return context.HitPoint;

        if (spawnPoint != null)
            return spawnPoint.position;

        return transform.position;
    }

    private void SpawnPrefab(GameObject[] prefabs, Vector3 position, Quaternion rotation)
    {
        if (prefabs == null || prefabs.Length == 0)
            return;

        GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
        if (prefab == null)
            return;

        Object.Instantiate(prefab, position, rotation);
    }

    private void PlayDeathAudio(bool isCrush, Vector3 position)
    {
        OUT_RandomAudioSetPlayer player = isCrush ? crushDeathAudioPlayer : genericDeathAudioPlayer;
        if (player != null && player.PlayRandomAtPoint(position))
            return;

        PlayClip(isCrush ? crushDeathClips : genericDeathClips, position);
    }

    private void PlayClip(AudioClip[] clips, Vector3 position)
    {
        if (clips == null || clips.Length == 0)
            return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null)
            return;

        AudioSource.PlayClipAtPoint(clip, position, audioVolume);
    }
}
