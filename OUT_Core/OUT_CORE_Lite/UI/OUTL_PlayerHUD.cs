using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(1200)]
[DisallowMultipleComponent]
public sealed class OUTL_PlayerHUD : MonoBehaviour, OUTL_IEventListener
{
    [Header("Source")]
    public OUTL_EntityAdapter Entity;
    public string FallbackTargetName = "demo_player";
    public string FallbackClassName = "player";
    public string FallbackRequiredTag = "Player";
    public bool AutoFindEntity = true;

    [Header("UI References - Text or TMP_Text")]
    public Object HealthText;
    public Object StatsText;
    public Object StateText;
    public Object DebugText;
    public Object CrosshairText;

    [Header("Auto Create")]
    public bool AutoCreateCanvas = false;
    public Canvas CanvasRoot;
    public Vector2 RootOffset = new Vector2(24f, -24f);
    public Font BuiltinFont;
    public int FontSize = 22;
    public Color TextColor = Color.white;
    public Color WarningColor = new Color(1f, 0.35f, 0.25f, 1f);

    [Header("Refresh")]
    [Min(0.01f)] public float RefreshInterval = 0.1f;
    public bool ShowStats = true;
    public bool ShowState = true;
    public bool ShowDebug = true;
    public bool ShowCrosshair = true;
    public string HealthLabel = "HP";
    public string ArmorLabel = "ARM";
    public string StaminaLabel = "STM";
    public string ManaLabel = "MANA";

    private float nextRefresh;
    private bool registeredEvents;
    private bool missingCanvasWarned;
    private readonly List<OUTL_EntityRuntime> entityBuffer = new List<OUTL_EntityRuntime>(64);
    private static readonly Dictionary<System.Type, PropertyInfo> textPropertyCache = new Dictionary<System.Type, PropertyInfo>(16);
    private static readonly Dictionary<System.Type, PropertyInfo> colorPropertyCache = new Dictionary<System.Type, PropertyInfo>(16);

    private void Awake()
    {
        if (Entity == null) Entity = GetComponentInParent<OUTL_EntityAdapter>();
        if (AutoCreateCanvas) EnsureCanvasAndText();
    }

    private void OnEnable()
    {
        RegisterEvents();
        Refresh(true);
    }

    private void OnDisable()
    {
        UnregisterEvents();
    }

    private void Update()
    {
        float now = Time.unscaledTime;
        if (now < nextRefresh) return;
        nextRefresh = now + Mathf.Max(0.01f, RefreshInterval);
        Refresh(false);
    }

    public void OUTL_OnEvent(in OUTL_Event evt, OUTL_World world)
    {
        if (Entity == null || !Entity.Id.IsValid) return;
        if (evt.Target == Entity.Id || evt.Source == Entity.Id)
            Refresh(true);
    }

    [ContextMenu("OUT Refresh HUD Now")]
    public void RefreshNow()
    {
        Refresh(true);
    }

    [ContextMenu("OUT Auto Create HUD UI")]
    public void EnsureCanvasAndText()
    {
        if (CanvasRoot == null)
            CanvasRoot = GetComponentInChildren<Canvas>(true);
        if (CanvasRoot == null)
        {
            WarnMissingCanvas();
            return;
        }

        if (HealthText == null) HealthText = FindTextObject("HealthText");
        if (StatsText == null) StatsText = FindTextObject("StatsText");
        if (StateText == null) StateText = FindTextObject("StateText");
        if (DebugText == null) DebugText = FindTextObject("DebugText");
        if (CrosshairText == null) CrosshairText = FindTextObject("CrosshairText");
    }

    private void Refresh(bool force)
    {
        if (Entity == null && AutoFindEntity) Entity = FindEntity();
        if (Entity == null || Entity.Runtime == null)
        {
            SetText(HealthText, "NO OUTL PLAYER ENTITY");
            SetText(StatsText, "assign Entity or set TargetName/tag");
            SetText(StateText, string.Empty);
            SetText(DebugText, string.Empty);
            SetText(CrosshairText, ShowCrosshair ? "+" : string.Empty);
            return;
        }

        OUTL_EntityRuntime rt = Entity.Runtime;
        float hp = rt.Stats.Get(OUTL_StatId.Health, 0f);
        float armor = rt.Stats.Get(OUTL_StatId.Armor, 0f);
        float stamina = rt.Stats.Get(OUTL_StatId.Stamina, 0f);
        float mana = rt.Stats.Get(OUTL_StatId.Mana, 0f);
        float dmg = rt.Stats.Get(OUTL_StatId.Damage, 0f);
        float speed = rt.Stats.Get(OUTL_StatId.Speed, 0f);

        SetText(HealthText, HealthLabel + " " + Format(hp));
        SetTextColor(HealthText, hp > 25f ? TextColor : WarningColor);

        if (ShowStats)
            SetText(StatsText, ArmorLabel + " " + Format(armor) + "   " + StaminaLabel + " " + Format(stamina) + "   " + ManaLabel + " " + Format(mana) + "   DMG " + Format(dmg) + "   SPD " + Format(speed));
        else SetText(StatsText, string.Empty);

        if (ShowState)
            SetText(StateText, "id " + rt.Id + " | " + rt.ClassName + " | " + rt.TargetName + " | tier " + rt.Tier + " | lane " + Entity.TickLane + " | tick " + Entity.TickInterval.ToString("0.###"));
        else SetText(StateText, string.Empty);

        if (ShowDebug)
        {
            OUTL_World world = OUTL_World.Instance;
            if (world != null)
                SetText(DebugText, "t " + world.WorldTime.ToString("0.00") + " dt " + world.DeltaTime.ToString("0.000") + " paused " + world.IsPaused + " entities " + world.Registry.Count + " queued " + world.Commands.QueuedCount + " events " + world.Events.PendingCount);
            else SetText(DebugText, "no OUTL_World");
        }
        else SetText(DebugText, string.Empty);

        SetText(CrosshairText, ShowCrosshair ? "+" : string.Empty);
    }

    private OUTL_EntityAdapter FindEntity()
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return null;

        OUTL_EntityRuntime byTarget = world.Registry.FindFirstByTargetName(FallbackTargetName);
        if (byTarget != null && byTarget.Adapter != null) return byTarget.Adapter;

        OUTL_EntityRuntime byClass = world.Registry.FindFirstByClassName(FallbackClassName);
        if (byClass != null && byClass.Adapter != null) return byClass.Adapter;

        world.Registry.CopyAll(entityBuffer);
        for (int i = 0; i < entityBuffer.Count; i++)
        {
            OUTL_EntityRuntime runtime = entityBuffer[i];
            OUTL_EntityAdapter a = runtime != null ? runtime.Adapter : null;
            if (a == null) continue;
            if (runtime != null && !string.IsNullOrEmpty(FallbackRequiredTag) && runtime.HasTag(FallbackRequiredTag))
            {
                entityBuffer.Clear();
                return a;
            }
        }
        entityBuffer.Clear();
        return null;
    }

    private Object FindTextObject(string name)
    {
        if (CanvasRoot == null || string.IsNullOrEmpty(name)) return null;
        Text[] texts = CanvasRoot.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
            if (texts[i] != null && texts[i].name == name)
                return texts[i];
        return null;
    }

    private void WarnMissingCanvas()
    {
        if (missingCanvasWarned) return;
        missingCanvasWarned = true;
        Debug.LogWarning("OUTL_PlayerHUD requires a preauthored CanvasRoot. Runtime UI construction is disabled by OUT CORE Lite canon.", this);
    }

    private void RegisterEvents()
    {
        if (registeredEvents) return;
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return;
        world.Events.Register(this, OUTL_EventType.Damaged);
        world.Events.Register(this, OUTL_EventType.Healed);
        world.Events.Register(this, OUTL_EventType.Killed);
        registeredEvents = true;
    }

    private void UnregisterEvents()
    {
        if (!registeredEvents) return;
        OUTL_World world = OUTL_World.Instance;
        if (world != null) world.Events.Unregister(this);
        registeredEvents = false;
    }

    private static string Format(float value)
    {
        return Mathf.Abs(value - Mathf.Round(value)) < 0.05f ? Mathf.RoundToInt(value).ToString() : value.ToString("0.0");
    }

    private static void SetText(Object target, string value)
    {
        if (target == null) return;
        Text uiText = target as Text;
        if (uiText != null)
        {
            uiText.text = value;
            return;
        }

        PropertyInfo prop = GetCachedProperty(target.GetType(), "text");
        if (prop != null && prop.PropertyType == typeof(string) && prop.CanWrite)
            prop.SetValue(target, value, null);
    }

    private static void SetTextColor(Object target, Color color)
    {
        if (target == null) return;
        Text uiText = target as Text;
        if (uiText != null)
        {
            uiText.color = color;
            return;
        }

        PropertyInfo prop = GetCachedProperty(target.GetType(), "color");
        if (prop != null && prop.PropertyType == typeof(Color) && prop.CanWrite)
            prop.SetValue(target, color, null);
    }

    private static PropertyInfo GetCachedProperty(System.Type type, string name)
    {
        if (type == null || string.IsNullOrEmpty(name)) return null;
        Dictionary<System.Type, PropertyInfo> cache = name == "text" ? textPropertyCache : colorPropertyCache;
        PropertyInfo prop;
        if (cache.TryGetValue(type, out prop)) return prop;
        prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        if (prop != null && !prop.CanWrite) prop = null;
        cache[type] = prop;
        return prop;
    }
}
