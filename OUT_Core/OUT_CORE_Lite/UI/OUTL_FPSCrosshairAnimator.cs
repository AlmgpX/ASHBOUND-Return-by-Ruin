using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class OUTL_FPSCrosshairAnimator : MonoBehaviour
{
    [Header("Source")]
    public OUTL_FPS_Controller Controller;
    public bool AutoFindController = true;
    [Min(0.1f)] public float AutoFindInterval = 1.0f;

    [Header("UI")]
    public Image TargetImage;
    public TMP_Text FallbackText;

    [Header("Sprites")]
    public Sprite IdleSprite;
    public Sprite[] ActiveFrames;
    [Min(1f)] public float FrameRate = 10f;

    [Header("Look")]
    public Color InactiveColor = new Color(0.42f, 0.42f, 0.42f, 0.62f);
    public Color ActiveColor = new Color(1f, 1f, 1f, 0.95f);
    public float FadeSpeed = 4f;
    public string IdleText = "+";
    public string ActiveText = "+";
    public bool HideFallbackTextWhenSpriteVisible = true;

    private int frameIndex;
    private float frameTimer;
    private float nextAutoFindTime;

    private void Awake()
    {
        Resolve();
        ApplyIdleImmediate();
    }

    private void OnEnable()
    {
        Resolve();
        ApplyIdleImmediate();
    }

    private void Update()
    {
        ResolveRuntime();
        bool active = Controller != null && Controller.HasUsableFocus;
        float dt = Time.unscaledDeltaTime > 0f ? Time.unscaledDeltaTime : Time.deltaTime;

        if (active)
            AnimateActive(dt);
        else
            AnimateIdle(dt);
    }

    private void Resolve()
    {
        if (TargetImage == null) TargetImage = GetComponent<Image>();
        if (FallbackText == null) FallbackText = GetComponent<TMP_Text>();
        ResolveRuntime(true);
    }

    private void ResolveRuntime(bool force = false)
    {
        if (Controller != null || !AutoFindController) return;
        float now = Time.unscaledTime;
        if (!force && now < nextAutoFindTime) return;
        nextAutoFindTime = now + Mathf.Max(0.1f, AutoFindInterval);
        Controller = FindObjectOfType<OUTL_FPS_Controller>();
    }

    private void AnimateActive(float dt)
    {
        SetFallbackText(ActiveText);

        bool spriteVisible = false;
        if (TargetImage != null)
        {
            if (ActiveFrames != null && ActiveFrames.Length > 0)
            {
                frameTimer += dt;
                float step = 1f / Mathf.Max(1f, FrameRate);
                while (frameTimer >= step)
                {
                    frameTimer -= step;
                    frameIndex = (frameIndex + 1) % ActiveFrames.Length;
                }

                Sprite sprite = ActiveFrames[frameIndex];
                if (sprite != null && TargetImage.sprite != sprite) TargetImage.sprite = sprite;
            }

            Color targetColor = Color.Lerp(TargetImage.color, ActiveColor, Mathf.Clamp01(dt * FadeSpeed * 6.3022f));
            if (TargetImage.color != targetColor) TargetImage.color = targetColor;
            spriteVisible = TargetImage.sprite != null && TargetImage.color.a > 0.01f;
        }

        if (FallbackText != null)
        {
            bool enabled = !HideFallbackTextWhenSpriteVisible || !spriteVisible;
            if (FallbackText.enabled != enabled) FallbackText.enabled = enabled;
            Color targetColor = Color.Lerp(FallbackText.color, ActiveColor, Mathf.Clamp01(dt * FadeSpeed * 6.3022f));
            if (FallbackText.color != targetColor) FallbackText.color = targetColor;
        }
    }

    private void AnimateIdle(float dt)
    {
        frameTimer = 0f;
        frameIndex = 0;
        if (TargetImage != null)
        {
            if (IdleSprite != null && TargetImage.sprite != IdleSprite) TargetImage.sprite = IdleSprite;
            Color targetColor = Color.Lerp(TargetImage.color, InactiveColor, Mathf.Clamp01(dt * FadeSpeed));
            if (TargetImage.color != targetColor) TargetImage.color = targetColor;
        }

        if (FallbackText != null)
        {
            bool enabled = TargetImage == null || !HideFallbackTextWhenSpriteVisible || TargetImage.sprite == null;
            if (FallbackText.enabled != enabled) FallbackText.enabled = enabled;
            SetFallbackText(IdleText);
            Color targetColor = Color.Lerp(FallbackText.color, InactiveColor, Mathf.Clamp01(dt * FadeSpeed));
            if (FallbackText.color != targetColor) FallbackText.color = targetColor;
        }
    }

    private void ApplyIdleImmediate()
    {
        frameIndex = 0;
        frameTimer = 0f;
        if (TargetImage != null)
        {
            if (IdleSprite != null) TargetImage.sprite = IdleSprite;
            TargetImage.color = InactiveColor;
        }

        if (FallbackText != null)
        {
            SetFallbackText(IdleText);
            FallbackText.color = InactiveColor;
            FallbackText.enabled = TargetImage == null || !HideFallbackTextWhenSpriteVisible || TargetImage.sprite == null;
        }
    }

    private void SetFallbackText(string value)
    {
        if (FallbackText == null) return;
        if (FallbackText.text == value) return;
        FallbackText.text = value;
    }
}
