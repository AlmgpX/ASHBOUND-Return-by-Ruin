using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class OUTL_InteractPromptConnector : MonoBehaviour
{
    [Header("Source")]
    public OUTL_BasicPlayerController Player;
    public bool AutoFindPlayer = true;
    [Min(0.1f)] public float AutoFindInterval = 1.0f;

    [Header("Legacy Compatibility")]
    [Tooltip("Deprecated. Language is now resolved only by OUTL_LanguageService.")]
    public bool AutoLanguage = true;
    [Tooltip("Deprecated fallback hint for old presets. Use OUTL_LanguageService.CurrentLanguage instead.")]
    public string Language = "ru";

    [Header("UI")]
    public GameObject Root;
    public Text NameText;
    public Text DescriptionText;
    public Text VerbText;
    public Text KeyText;
    public Image IconImage;
    public Image BackgroundImage;

    [Header("Text")]
    public string FallbackUseKey = "E";
    public bool HideWhenNoInteractable = true;
    public bool ShowVerb = true;
    public bool ShowDescription = true;

    private OUTL_Interactable lastInteractable;
    private string lastFallbackTargetName;
    private string lastLanguage;
    private bool dirty = true;
    private float nextAutoFindTime;
    private readonly List<OUTL_EntityRuntime> entityBuffer = new List<OUTL_EntityRuntime>(64);

    private void Awake()
    {
        if (Root == null) Root = gameObject;
        if (Player == null && AutoFindPlayer) Player = FindPlayer(true);
        OUTL_LanguageService.OnLanguageChangedGlobal += OnLanguageChanged;
        Refresh(true);
    }

    private void OnDestroy()
    {
        OUTL_LanguageService.OnLanguageChangedGlobal -= OnLanguageChanged;
    }

    private void Update()
    {
        Refresh(false);
    }

    private void OnLanguageChanged(string language)
    {
        dirty = true;
        Refresh(true);
    }

    public void Refresh(bool force)
    {
        if (Player == null && AutoFindPlayer) Player = FindPlayer(false);
        string language = OUTL_LanguageService.Instance != null ? OUTL_LanguageService.Instance.CurrentLanguage : Language;
        OUTL_Interactable interactable = Player != null ? Player.CurrentInteractable : null;
        string fallbackTargetName = interactable == null && Player != null ? Player.GetCurrentUseDisplayName() : string.Empty;
        if (!force && !dirty && interactable == lastInteractable && fallbackTargetName == lastFallbackTargetName && language == lastLanguage) return;
        dirty = false;
        lastInteractable = interactable;
        lastFallbackTargetName = fallbackTargetName;
        lastLanguage = language;

        bool visible = interactable != null || !string.IsNullOrEmpty(fallbackTargetName) || !HideWhenNoInteractable;
        if (Root != null && Root.activeSelf != visible) Root.SetActive(visible);
        if (!visible) return;

        if (interactable == null)
        {
            SetText(NameText, fallbackTargetName);
            SetText(DescriptionText, string.Empty);
            SetText(VerbText, !string.IsNullOrEmpty(fallbackTargetName) && ShowVerb ? OUTL_LanguageService.GetText("interact.verb.use", "Use") : string.Empty);
            SetText(KeyText, !string.IsNullOrEmpty(fallbackTargetName) ? FallbackUseKey : string.Empty);
            SetIcon(null, false);
            return;
        }

        SetText(NameText, interactable.GetDisplayName());
        SetText(DescriptionText, ShowDescription ? interactable.GetDescription() : string.Empty);
        SetText(VerbText, ShowVerb ? interactable.GetVerb() : string.Empty);
        SetText(KeyText, FallbackUseKey);
        SetIcon(interactable.PromptIcon, interactable.PromptIcon != null);
    }

    private static void SetText(Text text, string value)
    {
        if (text != null) text.text = value;
    }

    private void SetIcon(Sprite sprite, bool visible)
    {
        if (IconImage == null) return;
        IconImage.enabled = visible;
        if (sprite != null) IconImage.sprite = sprite;
    }

    private OUTL_BasicPlayerController FindPlayer(bool force)
    {
        float now = Time.unscaledTime;
        if (!force && now < nextAutoFindTime) return null;
        nextAutoFindTime = now + Mathf.Max(0.1f, AutoFindInterval);

        OUTL_World world = OUTL_World.Instance;
        if (world == null) return null;
        world.Registry.CopyAll(entityBuffer);
        for (int i = 0; i < entityBuffer.Count; i++)
        {
            OUTL_EntityRuntime runtime = entityBuffer[i];
            OUTL_EntityAdapter adapter = runtime != null ? runtime.Adapter : null;
            if (adapter == null || !runtime.HasTag("Player")) continue;
            OUTL_BasicPlayerController controller = adapter.GetComponent<OUTL_BasicPlayerController>();
            if (controller != null)
            {
                entityBuffer.Clear();
                return controller;
            }
        }
        entityBuffer.Clear();
        return null;
    }
}
