using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class OUTL_StatHUDConnector : MonoBehaviour, OUTL_IEventListener
{
    [Header("Source")]
    public OUTL_EntityAdapter Entity;
    public OUTL_StatId Stat = OUTL_StatId.Health;
    public float MaxValue = 100f;
    public bool AutoFindPlayer = true;
    [Min(0.1f)] public float AutoFindInterval = 1.0f;
    public bool ListenToEvents = true;
    public float PollInterval = 0.08f;

    [Header("UI")]
    public Text ValueText;
    public Text LabelText;
    public Image FillImage;
    public Image StatusImage;
    public Image BackgroundImage;
    public string Label = "HP";
    public string Format = "0";
    public bool ShowCurrentAndMax = false;

    [Header("Threshold Visuals")]
    public OUTL_StatHUDThreshold[] Thresholds;
    public Color DefaultColor = Color.white;
    public Sprite DefaultSprite;

    private float nextPollTime;
    private float nextAutoFindTime;
    private float lastValue = float.MinValue;
    private float lastNormalized = -1f;
    private readonly List<OUTL_EntityRuntime> entityBuffer = new List<OUTL_EntityRuntime>(64);

    private void Awake()
    {
        if (Entity == null && AutoFindPlayer) Entity = FindPlayerEntity(true);
        Refresh(true);
    }

    private void OnEnable()
    {
        if (OUTL_World.Instance != null && ListenToEvents) OUTL_World.Instance.Events.Register(this);
        Refresh(true);
    }

    private void OnDisable()
    {
        if (OUTL_World.Instance != null && ListenToEvents) OUTL_World.Instance.Events.Unregister(this);
    }

    private void Update()
    {
        if (Time.unscaledTime < nextPollTime) return;
        nextPollTime = Time.unscaledTime + Mathf.Max(0.01f, PollInterval);
        Refresh(false);
    }

    public void OUTL_OnEvent(in OUTL_Event evt, OUTL_World world)
    {
        if (Entity == null || !Entity.Id.IsValid || evt.Target != Entity.Id) return;
        if (evt.Type == OUTL_EventType.Damaged || evt.Type == OUTL_EventType.Healed || evt.Type == OUTL_EventType.CommandExecuted)
            Refresh(true);
    }

    public void Refresh(bool force)
    {
        if (Entity == null && AutoFindPlayer) Entity = FindPlayerEntity(force);
        if (Entity == null || Entity.Runtime == null)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        float value = Entity.Runtime.Stats.Get(Stat, 0f);
        float max = Mathf.Max(0.0001f, MaxValue);
        float normalized = Mathf.Clamp01(value / max);

        if (!force && Mathf.Approximately(value, lastValue) && Mathf.Approximately(normalized, lastNormalized)) return;
        lastValue = value;
        lastNormalized = normalized;

        if (LabelText != null) LabelText.text = Label;
        if (ValueText != null)
            ValueText.text = ShowCurrentAndMax ? value.ToString(Format) + " / " + MaxValue.ToString(Format) : value.ToString(Format);
        if (FillImage != null) FillImage.fillAmount = normalized;

        ApplyThreshold(normalized);
    }

    private void ApplyThreshold(float normalized)
    {
        OUTL_StatHUDThreshold best = null;
        if (Thresholds != null)
        {
            for (int i = 0; i < Thresholds.Length; i++)
            {
                OUTL_StatHUDThreshold t = Thresholds[i];
                if (t == null) continue;
                if (normalized <= t.MaxNormalized)
                {
                    best = t;
                    break;
                }
            }
        }

        Color color = best != null ? best.Color : DefaultColor;
        Sprite sprite = best != null && best.Sprite != null ? best.Sprite : DefaultSprite;

        if (FillImage != null) FillImage.color = color;
        if (StatusImage != null)
        {
            StatusImage.color = color;
            if (sprite != null) StatusImage.sprite = sprite;
        }
        if (BackgroundImage != null && best != null && best.BackgroundColor.a > 0f) BackgroundImage.color = best.BackgroundColor;
    }

    private void SetVisible(bool value)
    {
        if (ValueText != null) ValueText.enabled = value;
        if (LabelText != null) LabelText.enabled = value;
        if (FillImage != null) FillImage.enabled = value;
        if (StatusImage != null) StatusImage.enabled = value;
        if (BackgroundImage != null) BackgroundImage.enabled = value;
    }

    private OUTL_EntityAdapter FindPlayerEntity(bool force)
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
            if (runtime != null && runtime.Adapter != null && runtime.HasTag("Player"))
            {
                OUTL_EntityAdapter adapter = runtime.Adapter;
                entityBuffer.Clear();
                return adapter;
            }
        }
        entityBuffer.Clear();
        return null;
    }
}

[System.Serializable]
public class OUTL_StatHUDThreshold
{
    [Range(0f, 1f)] public float MaxNormalized = 1f;
    public Color Color = Color.white;
    public Color BackgroundColor = new Color(0f, 0f, 0f, 0f);
    public Sprite Sprite;
}
