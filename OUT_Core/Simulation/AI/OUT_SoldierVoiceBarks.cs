using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class OUT_SoldierVoiceBarks : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;

    [Header("Rules")]
    [SerializeField][Range(0f, 1f)] private float defaultChance = 1f;
    [SerializeField][Min(0f)] private float globalCooldown = 1.2f;
    [SerializeField][Min(0f)] private float perEventCooldown = 3.0f;
    [SerializeField] private bool blockIfAudioSourceIsPlaying = false;

    [Header("Enemy")]
    [SerializeField] private AudioClip[] enemySpotted;
    [SerializeField] private AudioClip[] lostEnemy;

    [Header("Orders / Movement")]
    [SerializeField] private AudioClip[] takeCover;
    [SerializeField] private AudioClip[] fallback;
    [SerializeField] private AudioClip[] regroup;
    [SerializeField] private AudioClip[] advance;

    [Header("Combat")]
    [SerializeField] private AudioClip[] suppressFire;
    [SerializeField] private AudioClip[] explosiveAttack;
    [SerializeField] private AudioClip[] meleeAttack;

    [Header("Weapon")]
    [SerializeField] private AudioClip[] reload;
    [SerializeField] private AudioClip[] dryFire;

    [Header("Damage / Death")]
    [SerializeField] private AudioClip[] lightDamage;
    [SerializeField] private AudioClip[] heavyDamage;
    [SerializeField] private AudioClip[] death;

    [Header("Events")]
    [SerializeField] private UnityEvent onAnyBark;
    [SerializeField] private UnityEvent onEnemySpotted;
    [SerializeField] private UnityEvent onTakeCover;
    [SerializeField] private UnityEvent onReload;
    [SerializeField] private UnityEvent onExplosiveAttack;

    private float _nextAnyBarkTime;
    private float _nextEnemySpottedTime;
    private float _nextLostEnemyTime;
    private float _nextTakeCoverTime;
    private float _nextFallbackTime;
    private float _nextRegroupTime;
    private float _nextAdvanceTime;
    private float _nextSuppressFireTime;
    private float _nextExplosiveAttackTime;
    private float _nextMeleeAttackTime;
    private float _nextReloadTime;
    private float _nextDryFireTime;
    private float _nextLightDamageTime;
    private float _nextHeavyDamageTime;
    private float _nextDeathTime;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public bool PlayEnemySpotted(float chance = -1f)
    {
        bool played = TryPlay(enemySpotted, ref _nextEnemySpottedTime, chance);
        if (played)
            onEnemySpotted?.Invoke();
        return played;
    }

    public bool PlayLostEnemy(float chance = -1f)
    {
        return TryPlay(lostEnemy, ref _nextLostEnemyTime, chance);
    }

    public bool PlayTakeCover(float chance = -1f)
    {
        bool played = TryPlay(takeCover, ref _nextTakeCoverTime, chance);
        if (played)
            onTakeCover?.Invoke();
        return played;
    }

    public bool PlayFallback(float chance = -1f)
    {
        return TryPlay(fallback, ref _nextFallbackTime, chance);
    }

    public bool PlayRegroup(float chance = -1f)
    {
        return TryPlay(regroup, ref _nextRegroupTime, chance);
    }

    public bool PlayAdvance(float chance = -1f)
    {
        return TryPlay(advance, ref _nextAdvanceTime, chance);
    }

    public bool PlaySuppressFire(float chance = -1f)
    {
        return TryPlay(suppressFire, ref _nextSuppressFireTime, chance);
    }

    public bool PlayExplosiveAttack(float chance = -1f)
    {
        bool played = TryPlay(explosiveAttack, ref _nextExplosiveAttackTime, chance);
        if (played)
            onExplosiveAttack?.Invoke();
        return played;
    }

    public bool PlayMeleeAttack(float chance = -1f)
    {
        return TryPlay(meleeAttack, ref _nextMeleeAttackTime, chance);
    }

    public bool PlayReload(float chance = -1f)
    {
        bool played = TryPlay(reload, ref _nextReloadTime, chance);
        if (played)
            onReload?.Invoke();
        return played;
    }

    public bool PlayDryFire(float chance = -1f)
    {
        return TryPlay(dryFire, ref _nextDryFireTime, chance);
    }

    public bool PlayLightDamage(float chance = -1f)
    {
        return TryPlay(lightDamage, ref _nextLightDamageTime, chance);
    }

    public bool PlayHeavyDamage(float chance = -1f)
    {
        return TryPlay(heavyDamage, ref _nextHeavyDamageTime, chance);
    }

    public bool PlayDeath(float chance = -1f)
    {
        return TryPlay(death, ref _nextDeathTime, chance);
    }

    private bool TryPlay(AudioClip[] clips, ref float nextEventTime, float chance)
    {
        if (audioSource == null || clips == null || clips.Length == 0)
            return false;

        if (Time.time < _nextAnyBarkTime || Time.time < nextEventTime)
            return false;

        if (blockIfAudioSourceIsPlaying && audioSource.isPlaying)
            return false;

        float finalChance = chance >= 0f ? chance : defaultChance;
        if (finalChance < 1f && Random.value > finalChance)
            return false;

        AudioClip clip = PickRandom(clips);
        if (clip == null)
            return false;

        audioSource.PlayOneShot(clip);
        _nextAnyBarkTime = Time.time + globalCooldown;
        nextEventTime = Time.time + perEventCooldown;
        onAnyBark?.Invoke();
        return true;
    }

    private AudioClip PickRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
            return null;

        for (int i = 0; i < 8; i++)
        {
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip != null)
                return clip;
        }

        return null;
    }
}
