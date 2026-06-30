using System.Text;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_FPSHudTMP : MonoBehaviour
{
    public OUTL_EntityAdapter Target;
    public OUTL_Vitals Vitals;
    public OUTL_FPS_Controller Controller;
    public TMP_Text HealthText;
    public TMP_Text ArmorText;
    public TMP_Text EnergyText;
    public TMP_Text UsePromptText;
    public string HealthKey = "Health";
    public string MaxHealthKey = "MaxHealth";
    public string ArmorKey = "Armor";
    public string MaxArmorKey = "MaxArmor";
    public string EnergyKey = "Energy";
    public string MaxEnergyKey = "MaxEnergy";
    public string Prefix = "HP";
    public string ArmorPrefix = "AR";
    public string EnergyPrefix = "EN";
    public bool ShowPercentBar = true;
    public int BarWidth = 18;
    public float UpdateInterval = 0.05f;
    public bool AutoFindTarget = true;
    [Min(0.1f)] public float AutoFindInterval = 1.0f;

    [Header("Glyph HUD")]
    public bool UseGlyphHud = true;
    public bool UseRichTextColors = true;
    public bool UseMonospaceTag = true;
    public string MonospaceWidth = "1.1em";
    public string GlyphSeparator = " ";
    public bool ShowNumbersAfterGlyphs = true;

    [Header("Health Glyphs")]
    [Range(1, 32)] public int HealthGlyphCount = 10;
    public string HealthFullGlyph = "♥";
    public string HealthEmptyGlyph = "♡";
    public string HealthColor = "#FF3030";
    public string HealthEmptyColor = "#552020";

    [Header("Armor Glyphs")]
    [Range(1, 32)] public int ArmorGlyphCount = 10;
    public string ArmorFullGlyph = "▣";
    public string ArmorEmptyGlyph = "□";
    public string ArmorColor = "#66AAFF";
    public string ArmorEmptyColor = "#22334A";

    [Header("Energy Glyphs")]
    [Range(1, 32)] public int EnergyGlyphCount = 10;
    public string EnergyFullGlyph = "♦";
    public string EnergyEmptyGlyph = "◇";
    public string EnergyColor = "#66FF99";
    public string EnergyEmptyColor = "#224A33";

    private readonly StringBuilder textBuilder = new StringBuilder(256);

    private float nextUpdateTime;
    private float nextAutoFindTime;
    private bool forceTextRefresh = true;

    private int lastHealthValue = int.MinValue;
    private int lastHealthMax = int.MinValue;
    private int lastArmorValue = int.MinValue;
    private int lastArmorMax = int.MinValue;
    private int lastEnergyValue = int.MinValue;
    private int lastEnergyMax = int.MinValue;

    private OUTL_Interactable lastPromptInteractable;
    private OUTL_EntityAdapter lastPromptTarget;

    private void Awake()
    {
        forceTextRefresh = true;
        Resolve();
        UpdateNow();
    }

    private void OnEnable()
    {
        forceTextRefresh = true;
        Resolve();
        UpdateNow();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        forceTextRefresh = true;
    }
#endif

    private void Update()
    {
        if (Time.unscaledTime < nextUpdateTime) return;
        nextUpdateTime = Time.unscaledTime + Mathf.Max(0.01f, UpdateInterval);
        UpdateNow();
    }

    [ContextMenu("OUT Refresh HUD")]
    public void UpdateNow()
    {
        Resolve();
        bool force = forceTextRefresh;
        forceTextRefresh = false;

        if (Target == null || Target.Runtime == null)
        {
            SetTextIfChanged(HealthText, Prefix + " -- / --");
            SetTextIfChanged(ArmorText, ArmorPrefix + " --");
            SetTextIfChanged(EnergyText, EnergyPrefix + " --");
            SetTextIfChanged(UsePromptText, string.Empty);
            ResetStatCaches();
            lastPromptInteractable = null;
            lastPromptTarget = null;
            return;
        }

        SetStatText(
            HealthText,
            Prefix,
            HealthKey,
            MaxHealthKey,
            HealthGlyphCount,
            HealthFullGlyph,
            HealthEmptyGlyph,
            HealthColor,
            HealthEmptyColor,
            ref lastHealthValue,
            ref lastHealthMax,
            true,
            force);

        SetStatText(
            ArmorText,
            ArmorPrefix,
            ArmorKey,
            MaxArmorKey,
            ArmorGlyphCount,
            ArmorFullGlyph,
            ArmorEmptyGlyph,
            ArmorColor,
            ArmorEmptyColor,
            ref lastArmorValue,
            ref lastArmorMax,
            false,
            force);

        SetStatText(
            EnergyText,
            EnergyPrefix,
            EnergyKey,
            MaxEnergyKey,
            EnergyGlyphCount,
            EnergyFullGlyph,
            EnergyEmptyGlyph,
            EnergyColor,
            EnergyEmptyColor,
            ref lastEnergyValue,
            ref lastEnergyMax,
            false,
            force);

        SetPromptText(force);
    }

    private void SetStatText(
        TMP_Text text,
        string prefix,
        string key,
        string maxKey,
        int glyphCount,
        string fullGlyph,
        string emptyGlyph,
        string fullColor,
        string emptyColor,
        ref int lastValue,
        ref int lastMax,
        bool includeBar,
        bool force)
    {
        if (text == null || Target == null || Target.Runtime == null) return;

        float rawValue = Target.Runtime.Stats.Get(key, 0f);
        float rawMax = Target.Runtime.Stats.Get(maxKey, 0f);
        if (rawMax <= 0f) rawMax = Mathf.Max(1f, rawValue);
        rawValue = Mathf.Clamp(rawValue, 0f, rawMax);

        int value = Mathf.CeilToInt(rawValue);
        int max = Mathf.CeilToInt(rawMax);

        if (!force && value == lastValue && max == lastMax)
            return;

        lastValue = value;
        lastMax = max;

        string line = UseGlyphHud
            ? BuildGlyphLine(prefix, rawValue, rawMax, glyphCount, fullGlyph, emptyGlyph, fullColor, emptyColor)
            : BuildNumericLine(prefix, value, max, includeBar ? rawValue / rawMax : -1f);

        SetTextIfChanged(text, line);
    }

    private void SetPromptText(bool force)
    {
        if (UsePromptText == null) return;
        if (Controller == null)
        {
            lastPromptInteractable = null;
            lastPromptTarget = null;
            SetTextIfChanged(UsePromptText, string.Empty);
            return;
        }

        OUTL_Interactable interactable = Controller.CurrentInteractable;
        OUTL_EntityAdapter target = Controller.CurrentCommandTarget;

        if (!force && interactable == lastPromptInteractable && target == lastPromptTarget)
            return;

        lastPromptInteractable = interactable;
        lastPromptTarget = target;

        if (interactable != null)
        {
            string name = !string.IsNullOrEmpty(interactable.DisplayName) ? interactable.DisplayName : interactable.name;
            string desc = !string.IsNullOrEmpty(interactable.DescriptionEn) ? interactable.DescriptionEn : interactable.DescriptionRu;

            textBuilder.Length = 0;
            textBuilder.Append("[E] ");
            textBuilder.Append(name);
            if (!string.IsNullOrEmpty(desc))
            {
                textBuilder.Append('\n');
                textBuilder.Append(desc);
            }
            SetTextIfChanged(UsePromptText, textBuilder.ToString());
            return;
        }

        if (target != null)
        {
            textBuilder.Length = 0;
            textBuilder.Append("[E] ");
            textBuilder.Append(!string.IsNullOrEmpty(target.TargetName) ? target.TargetName : target.name);
            SetTextIfChanged(UsePromptText, textBuilder.ToString());
            return;
        }

        SetTextIfChanged(UsePromptText, string.Empty);
    }

    private void Resolve()
    {
        if (Target == null && Vitals != null) Target = Vitals.Entity;
        if (Target == null && AutoFindTarget)
        {
            float now = Time.unscaledTime;
            if (now >= nextAutoFindTime)
            {
                nextAutoFindTime = now + Mathf.Max(0.1f, AutoFindInterval);
                Target = FindObjectOfType<OUTL_EntityAdapter>();
                forceTextRefresh = true;
            }
        }
        if (Vitals == null && Target != null) Vitals = Target.GetComponent<OUTL_Vitals>();
        if (Controller == null && Target != null) Controller = Target.GetComponent<OUTL_FPS_Controller>();
        if (Vitals != null)
        {
            if (!string.IsNullOrEmpty(Vitals.HealthKey)) HealthKey = Vitals.HealthKey;
            if (!string.IsNullOrEmpty(Vitals.MaxHealthKey)) MaxHealthKey = Vitals.MaxHealthKey;
        }
    }

    private string BuildGlyphLine(
        string label,
        float value,
        float max,
        int glyphCount,
        string fullGlyph,
        string emptyGlyph,
        string fullColor,
        string emptyColor)
    {
        glyphCount = Mathf.Clamp(glyphCount, 1, 32);
        float normalized = max > 0f ? Mathf.Clamp01(value / max) : 0f;
        int filled = Mathf.Clamp(Mathf.CeilToInt(normalized * glyphCount), 0, glyphCount);

        textBuilder.Length = 0;

        if (UseMonospaceTag)
        {
            textBuilder.Append("<mspace=");
            textBuilder.Append(string.IsNullOrEmpty(MonospaceWidth) ? "1.1em" : MonospaceWidth);
            textBuilder.Append('>');
        }

        textBuilder.Append(label);
        textBuilder.Append("  ");

        for (int i = 0; i < glyphCount; i++)
        {
            bool full = i < filled;
            string glyph = full ? fullGlyph : emptyGlyph;

            if (string.IsNullOrEmpty(glyph)) glyph = full ? "#" : "-";

            if (UseRichTextColors)
            {
                textBuilder.Append("<color=");
                textBuilder.Append(full ? fullColor : emptyColor);
                textBuilder.Append('>');
            }

            textBuilder.Append(glyph);

            if (UseRichTextColors)
                textBuilder.Append("</color>");

            if (i < glyphCount - 1)
                textBuilder.Append(GlyphSeparator);
        }

        if (ShowNumbersAfterGlyphs)
        {
            textBuilder.Append("  ");
            textBuilder.Append(Mathf.CeilToInt(value));
            textBuilder.Append('/');
            textBuilder.Append(Mathf.CeilToInt(max));
        }

        if (UseMonospaceTag)
            textBuilder.Append("</mspace>");

        return textBuilder.ToString();
    }

    private string BuildNumericLine(string prefix, int value, int max, float normalizedBar)
    {
        textBuilder.Length = 0;
        textBuilder.Append(prefix);
        textBuilder.Append(' ');
        textBuilder.Append(value);
        textBuilder.Append(" / ");
        textBuilder.Append(max);

        if (ShowPercentBar && normalizedBar >= 0f)
        {
            textBuilder.Append("  ");
            AppendBar(normalizedBar);
        }

        return textBuilder.ToString();
    }

    private void AppendBar(float value)
    {
        int width = Mathf.Clamp(BarWidth, 4, 48);
        int filled = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(value) * width), 0, width);
        textBuilder.Append('[');
        for (int i = 0; i < width; i++) textBuilder.Append(i < filled ? '#' : '-');
        textBuilder.Append(']');
    }

    private void SetTextIfChanged(TMP_Text text, string value)
    {
        if (text == null) return;
        if (text.text == value) return;
        text.text = value;
    }

    private void ResetStatCaches()
    {
        lastHealthValue = int.MinValue;
        lastHealthMax = int.MinValue;
        lastArmorValue = int.MinValue;
        lastArmorMax = int.MinValue;
        lastEnergyValue = int.MinValue;
        lastEnergyMax = int.MinValue;
    }
}
