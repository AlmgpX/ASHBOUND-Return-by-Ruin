using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public enum OUTL_UIDataKind { Stat, StateFlag, StateFloat, StateInt, StateString, ClassName, TargetName, Faction, Tier, EquipmentSlot, DeadState, WorldDebug }

[Serializable]
public class OUTL_UIDataBinding
{
    public string Id = "Health";
    public OUTL_UIDataKind Kind = OUTL_UIDataKind.Stat;
    public string Key = "Health";
    public string Label = "HP";
    public string Format = "{0} {1}";
    public UnityEngine.Object TargetText;
    public bool HideWhenEmpty;
    public bool WarningWhenLessOrEqual;
    public float WarningLessOrEqual = 25f;
    public Color NormalColor = Color.white;
    public Color WarningColor = new Color(1f, 0.35f, 0.25f, 1f);
}

[DefaultExecutionOrder(1210)]
[DisallowMultipleComponent]
public sealed class OUTL_UIDataBinder : MonoBehaviour, OUTL_IEventListener
{
    public OUTL_EntityAdapter Entity;
    public string TargetName;
    public string FallbackClassName = "player";
    public string RequiredTag = "Player";
    public bool AutoFindEntity = true;
    public bool AutoFindTextTargets = true;
    public OUTL_UIDataBinding[] Bindings;
    [Min(0.01f)] public float RefreshInterval = 0.1f;

    private float nextRefresh;
    private bool registered;
    private readonly List<OUTL_EntityRuntime> entityBuffer = new List<OUTL_EntityRuntime>(64);
    private static readonly Dictionary<Type, PropertyInfo> textPropertyCache = new Dictionary<Type, PropertyInfo>(16);
    private static readonly Dictionary<Type, PropertyInfo> colorPropertyCache = new Dictionary<Type, PropertyInfo>(16);

    private void Reset() { Bindings = CreateDefaults(); }
    private void Awake()
    {
        if (Entity == null) Entity = GetComponentInParent<OUTL_EntityAdapter>();
        if (Bindings == null || Bindings.Length == 0) Bindings = CreateDefaults();
        if (AutoFindTextTargets) AutoBindTextTargets();
    }
    private void OnEnable() { RegisterEvents(); RefreshNow(); }
    private void OnDisable() { UnregisterEvents(); }
    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + Mathf.Max(0.01f, RefreshInterval);
        RefreshNow();
    }

    public void OUTL_OnEvent(in OUTL_Event evt, OUTL_World world)
    {
        if (Entity == null || !Entity.Id.IsValid) return;
        if (evt.Target == Entity.Id || evt.Source == Entity.Id) RefreshNow();
    }

    [ContextMenu("OUT Auto Bind Text Targets")]
    public void AutoBindTextTargets()
    {
        if (Bindings == null) return;
        Component[] all = GetComponentsInChildren<Component>(true);
        for (int i = 0; i < Bindings.Length; i++)
            if (Bindings[i] != null && Bindings[i].TargetText == null)
                Bindings[i].TargetText = FindText(all, Bindings[i]);
    }

    [ContextMenu("OUT Refresh UI Bindings")]
    public void RefreshNow()
    {
        if (Entity == null && AutoFindEntity) Entity = FindEntity();
        OUTL_EntityRuntime rt = Entity != null ? Entity.Runtime : null;
        OUTL_World world = OUTL_World.Instance;
        if (Bindings == null) return;
        for (int i = 0; i < Bindings.Length; i++)
        {
            OUTL_UIDataBinding b = Bindings[i];
            if (b == null) continue;
            string value = Resolve(b, rt, world);
            SetText(b.TargetText, b.HideWhenEmpty && string.IsNullOrEmpty(value) ? string.Empty : SafeFormat(b, value));
            SetColor(b.TargetText, Warning(b, value) ? b.WarningColor : b.NormalColor);
        }
    }

    private string Resolve(OUTL_UIDataBinding b, OUTL_EntityRuntime rt, OUTL_World world)
    {
        if (b.Kind == OUTL_UIDataKind.WorldDebug)
            return world == null ? "no world" : "t=" + world.WorldTime.ToString("0.00") + " ent=" + world.Registry.Count + " cmd=" + world.Commands.QueuedCount + " evt=" + world.Events.PendingCount;
        if (rt == null) return string.Empty;
        switch (b.Kind)
        {
            case OUTL_UIDataKind.Stat: return F(rt.Stats.Get(b.Key, 0f));
            case OUTL_UIDataKind.StateFlag: return rt.State.GetFlag(b.Key) ? "1" : "0";
            case OUTL_UIDataKind.StateFloat: return F(rt.State.GetFloat(b.Key, 0f));
            case OUTL_UIDataKind.StateInt: return rt.State.GetInt(b.Key, 0).ToString();
            case OUTL_UIDataKind.StateString: return rt.State.GetString(b.Key, string.Empty);
            case OUTL_UIDataKind.ClassName: return rt.ClassName;
            case OUTL_UIDataKind.TargetName: return rt.TargetName;
            case OUTL_UIDataKind.Faction: return rt.Faction != null ? rt.Faction.FactionId : "none";
            case OUTL_UIDataKind.Tier: return rt.Tier.ToString();
            case OUTL_UIDataKind.EquipmentSlot: return rt.State.GetString(OUTL_Equipment.BuildSlotStateKey(b.Key), string.Empty);
            case OUTL_UIDataKind.DeadState: return rt.State.GetFlag(OUTL_StateId.Dead) || rt.Stats.Get(OUTL_StatId.Health, 0f) <= 0f ? "DEAD" : "ALIVE";
            default: return string.Empty;
        }
    }

    private OUTL_EntityAdapter FindEntity()
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return null;

        OUTL_EntityRuntime byTarget = world.Registry.FindFirstByTargetName(TargetName);
        if (byTarget != null && byTarget.Adapter != null) return byTarget.Adapter;

        OUTL_EntityRuntime byClass = world.Registry.FindFirstByClassName(FallbackClassName);
        if (byClass != null && byClass.Adapter != null) return byClass.Adapter;

        world.Registry.CopyAll(entityBuffer);
        for (int i = 0; i < entityBuffer.Count; i++)
        {
            OUTL_EntityRuntime runtime = entityBuffer[i];
            OUTL_EntityAdapter adapter = runtime != null ? runtime.Adapter : null;
            if (adapter == null) continue;
            if (runtime != null && !string.IsNullOrEmpty(RequiredTag) && runtime.HasTag(RequiredTag))
            {
                entityBuffer.Clear();
                return adapter;
            }
        }
        entityBuffer.Clear();
        return null;
    }

    private void RegisterEvents() { if (!registered && OUTL_World.Instance != null) { OUTL_World.Instance.Events.Register(this); registered = true; } }
    private void UnregisterEvents() { if (registered && OUTL_World.Instance != null) OUTL_World.Instance.Events.Unregister(this); registered = false; }

    private static OUTL_UIDataBinding[] CreateDefaults()
    {
        return new[] { B("Health", OUTL_UIDataKind.Stat, "Health", "HP", true), B("Armor", OUTL_UIDataKind.Stat, "Armor", "ARM", false), B("Primary", OUTL_UIDataKind.EquipmentSlot, "Primary", "PRI", false), B("Dead", OUTL_UIDataKind.DeadState, "Dead", "STATE", false) };
    }
    private static OUTL_UIDataBinding B(string id, OUTL_UIDataKind kind, string key, string label, bool warn) { return new OUTL_UIDataBinding { Id = id, Kind = kind, Key = key, Label = label, WarningWhenLessOrEqual = warn }; }

    private static Component FindText(Component[] all, OUTL_UIDataBinding b)
    {
        string id = N(b.Id), key = N(b.Key), label = N(b.Label);
        for (int i = 0; i < all.Length; i++)
        {
            Component c = all[i];
            if (!CanText(c)) continue;
            string n = N(c.name);
            if (n.Contains(id) || n.Contains(key) || n.Contains(label)) return c;
        }
        return null;
    }

    private static string SafeFormat(OUTL_UIDataBinding b, string v) { try { return string.Format(b.Format, b.Label, v, b.Key); } catch { return b.Label + " " + v; } }
    private static bool Warning(OUTL_UIDataBinding b, string v) { float f; return b.WarningWhenLessOrEqual && float.TryParse(v, out f) && f <= b.WarningLessOrEqual; }
    private static string F(float v) { return Mathf.Abs(v - Mathf.Round(v)) < 0.05f ? Mathf.RoundToInt(v).ToString() : v.ToString("0.0"); }
    private static string N(string s) { return string.IsNullOrEmpty(s) ? "" : s.ToLowerInvariant().Replace("_", "").Replace("-", "").Replace(" ", "").Replace("label", "").Replace("text", ""); }
    private static bool CanText(UnityEngine.Object o) { return o is Text || (o != null && o.GetType().GetProperty("text", BindingFlags.Instance | BindingFlags.Public) != null); }
    private static void SetText(UnityEngine.Object o, string v) { if (o is Text t) t.text = v; else SetProp(o, "text", v); }
    private static void SetColor(UnityEngine.Object o, Color v) { if (o is Text t) t.color = v; else SetProp(o, "color", v); }
    private static void SetProp(UnityEngine.Object o, string name, object v)
    {
        if (o == null) return;
        PropertyInfo p = GetCachedProperty(o.GetType(), name);
        if (p != null && p.CanWrite) p.SetValue(o, v, null);
    }

    private static PropertyInfo GetCachedProperty(Type type, string name)
    {
        if (type == null || string.IsNullOrEmpty(name)) return null;
        Dictionary<Type, PropertyInfo> cache = name == "text" ? textPropertyCache : colorPropertyCache;
        PropertyInfo prop;
        if (cache.TryGetValue(type, out prop)) return prop;
        prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        if (prop != null && !prop.CanWrite) prop = null;
        cache[type] = prop;
        return prop;
    }
}
