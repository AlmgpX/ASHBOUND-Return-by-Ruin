using Cinemachine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class OUTL_PlayerFeedback : MonoBehaviour, OUTL_IEventListener
{
    public OUTL_EntityAdapter Entity;
    public OUTL_FPS_Controller Controller;
    public OUTL_CharacterAnimationBridge AnimationBridge;

    [Header("Audio Sources")]
    public AudioSource BodySource;
    public AudioSource PainSource;
    public AudioSource StrongPainSource;
    public AudioSource ArmorSource;
    public AudioSource EnergySource;
    public AudioSource VoiceSource;

    [Header("Pain Audio")]
    public AudioClip[] PainClips;
    public AudioClip[] StrongPainClips;
    public AudioClip[] ArmorHitClips;
    public AudioClip[] EnergyLowClips;
    public float PainCooldown = 0.08f;
    public float StrongPainThreshold = 25f;
    public Vector2 PainPitch = new Vector2(0.95f, 1.05f);
    public Vector2 PainVolume = new Vector2(0.85f, 1f);

    [Header("Movement Impulses")]
    public CinemachineImpulseSource WalkImpulseSource;
    public CinemachineImpulseSource RunImpulseSource;
    public CinemachineImpulseSource JumpImpulseSource;
    public CinemachineImpulseSource LandingImpulseSource;
    public CinemachineImpulseSource DamageImpulseSource;
    public CinemachineImpulseSource ExplosionImpulseSource;
    public float WalkImpulse = 0.08f;
    public float RunImpulse = 0.16f;
    public float JumpImpulse = 0.25f;
    public float LandingImpulseMin = 0.25f;
    public float LandingImpulseMax = 1.6f;
    public float DamageImpulseMin = 0.2f;
    public float DamageImpulseMax = 2.0f;

    [Header("HUD Feedback")]
    public Image DamageOverlay;
    public CanvasGroup DamageOverlayGroup;
    public Color DamageColor = new Color(1f, 0f, 0f, 0.55f);
    public float DamageOverlayMaxAlpha = 0.65f;
    public float DamageOverlayFadeSpeed = 2.7f;
    public Image HealOverlay;
    public CanvasGroup HealOverlayGroup;
    public Color HealColor = new Color(0.1f, 1f, 0.35f, 0.35f);
    public float HealOverlayMaxAlpha = 0.35f;
    public float HealOverlayFadeSpeed = 1.8f;
    public TMP_Text LastEventText;
    public float LastEventTextTime = 1.4f;

    private float damageAlpha;
    private float healAlpha;
    private float nextPainTime;
    private float eventTextHideTime;

    private void Awake()
    {
        Resolve();
    }

    private void OnEnable()
    {
        Resolve();
        if (OUTL_World.Instance != null)
        {
            OUTL_World.Instance.Events.Register(this, OUTL_EventType.Damaged);
            OUTL_World.Instance.Events.Register(this, OUTL_EventType.Healed);
            OUTL_World.Instance.Events.Register(this, OUTL_EventType.Killed);
        }
    }

    private void OnDisable()
    {
        if (OUTL_World.Instance != null) OUTL_World.Instance.Events.Unregister(this);
    }

    private void Update()
    {
        if (damageAlpha > 0f) damageAlpha = Mathf.MoveTowards(damageAlpha, 0f, DamageOverlayFadeSpeed * Time.unscaledDeltaTime);
        if (healAlpha > 0f) healAlpha = Mathf.MoveTowards(healAlpha, 0f, HealOverlayFadeSpeed * Time.unscaledDeltaTime);
        ApplyOverlay(DamageOverlay, DamageOverlayGroup, DamageColor, damageAlpha);
        ApplyOverlay(HealOverlay, HealOverlayGroup, HealColor, healAlpha);
        if (LastEventText != null && eventTextHideTime > 0f && Time.unscaledTime >= eventTextHideTime)
        {
            LastEventText.text = string.Empty;
            eventTextHideTime = 0f;
        }
    }

    public void OUTL_OnEvent(in OUTL_Event evt, OUTL_World world)
    {
        if (Entity == null || evt.Target != Entity.Id) return;
        if (evt.Type == OUTL_EventType.Damaged) OnDamaged(evt.FloatValue, evt.Key, evt.Point);
        else if (evt.Type == OUTL_EventType.Healed) OnHealed(evt.FloatValue);
        else if (evt.Type == OUTL_EventType.Killed) OnKilled(evt.Key);
    }

    public void OnFootstep(bool running, float speed01)
    {
        GenerateImpulse(running ? RunImpulseSource : WalkImpulseSource, (running ? RunImpulse : WalkImpulse) * Mathf.Clamp01(speed01));
    }

    public void OnJump(float speed01)
    {
        GenerateImpulse(JumpImpulseSource, JumpImpulse * Mathf.Clamp01(0.5f + speed01));
    }

    public void OnLanding(float fallSpeed, float damage)
    {
        float t = Mathf.Clamp01(Mathf.Abs(fallSpeed) / 18f);
        GenerateImpulse(LandingImpulseSource, Mathf.Lerp(LandingImpulseMin, LandingImpulseMax, t));
        if (damage > 0f) FlashDamage(damage);
    }

    public void OnExplosion(Vector3 position, float strength)
    {
        GenerateImpulse(ExplosionImpulseSource != null ? ExplosionImpulseSource : DamageImpulseSource, Mathf.Max(0f, strength));
        SetEventText("BLAST " + strength.ToString("0.0"));
    }

    private void OnDamaged(float damage, string key, Vector3 point)
    {
        FlashDamage(damage);
        float strength = Mathf.Lerp(DamageImpulseMin, DamageImpulseMax, Mathf.Clamp01(damage / 50f));
        GenerateImpulse(DamageImpulseSource, strength);
        if (AnimationBridge != null) AnimationBridge.NotifyHurt();

        if (Entity != null && Entity.Runtime != null && Entity.Runtime.State.GetFloat("Player.LastArmorAbsorbed", 0f) > 0.01f)
            PlayRandom(ArmorSource != null ? ArmorSource : PainSource, ArmorHitClips, 1f);

        if (Time.unscaledTime >= nextPainTime)
        {
            nextPainTime = Time.unscaledTime + Mathf.Max(0.01f, PainCooldown);
            bool strong = damage >= StrongPainThreshold;
            PlayRandom(strong ? StrongPainSource : PainSource, strong ? StrongPainClips : PainClips, 1f);
        }

        SetEventText("DAMAGE " + damage.ToString("0") + (string.IsNullOrEmpty(key) ? string.Empty : " / " + key));
    }

    private void OnHealed(float amount)
    {
        healAlpha = Mathf.Min(HealOverlayMaxAlpha, healAlpha + HealOverlayMaxAlpha * 0.65f);
        SetEventText(amount > 0f ? "HEAL +" + amount.ToString("0") : "HEAL");
    }

    private void OnKilled(string key)
    {
        damageAlpha = DamageOverlayMaxAlpha;
        SetEventText("DEAD" + (string.IsNullOrEmpty(key) ? string.Empty : " / " + key));
    }

    private void FlashDamage(float damage)
    {
        float t = Mathf.Clamp01(Mathf.Max(1f, damage) / 50f);
        damageAlpha = Mathf.Min(DamageOverlayMaxAlpha, Mathf.Max(damageAlpha, Mathf.Lerp(0.12f, DamageOverlayMaxAlpha, t)));
    }

    private void GenerateImpulse(CinemachineImpulseSource source, float strength)
    {
        if (source == null || strength <= 0f) return;
        Vector3 velocity = source.m_DefaultVelocity;
        if (velocity.sqrMagnitude < 0.0001f) velocity = Vector3.down;
        source.GenerateImpulse(velocity.normalized * strength);
    }

    private void PlayRandom(AudioSource source, AudioClip[] clips, float volumeMul)
    {
        if (source == null || clips == null || clips.Length == 0) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;
        source.pitch = Random.Range(PainPitch.x, PainPitch.y);
        source.PlayOneShot(clip, Random.Range(PainVolume.x, PainVolume.y) * volumeMul);
    }

    private void ApplyOverlay(Image image, CanvasGroup group, Color baseColor, float alpha)
    {
        if (image != null)
        {
            Color c = baseColor;
            c.a = alpha;
            image.color = c;
        }
        if (group != null) group.alpha = alpha;
    }

    private void SetEventText(string text)
    {
        if (LastEventText == null) return;
        LastEventText.text = text;
        eventTextHideTime = Time.unscaledTime + Mathf.Max(0.1f, LastEventTextTime);
    }

    private void Resolve()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (Controller == null) Controller = GetComponent<OUTL_FPS_Controller>();
        if (AnimationBridge == null) AnimationBridge = GetComponentInChildren<OUTL_CharacterAnimationBridge>(true);
        if (BodySource == null) BodySource = GetComponent<AudioSource>();
        if (PainSource == null) PainSource = BodySource;
        if (StrongPainSource == null) StrongPainSource = PainSource;
    }
}
