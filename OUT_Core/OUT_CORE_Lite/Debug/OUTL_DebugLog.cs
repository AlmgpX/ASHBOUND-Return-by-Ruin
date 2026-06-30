using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum OUTL_DebugChannel : byte
{
    General = 0,
    AI = 1,
    Stimulus = 2,
    Diary = 3,
    Events = 4,
    Combat = 5,
    Loot = 6,
    Save = 7,
    Perf = 8
}

public static class OUTL_DebugLog
{
    private const int ChannelCount = 9;
    private const int RingCapacity = 256;

    public static event Action<string> OnLine;
    public static bool AITrace;
    public static bool StimulusTrace;
    public static bool DiaryTrace;
    public static bool EventTrace;
    public static bool CombatTrace = true;
    public static bool LootTrace = true;
    public static bool SaveTrace = true;
    public static bool PerfTrace = true;
    public static bool MirrorToUnityConsole = true;
    public static OUTL_EntityId AIWatch = OUTL_EntityId.None;

    private static readonly bool[] channelEnabled = new bool[ChannelCount];
    private static readonly string[] ring = new string[RingCapacity];
    private static int ringWriteIndex;
    private static bool ringWrapped;

    static OUTL_DebugLog()
    {
        channelEnabled[(int)OUTL_DebugChannel.General] = true;
        channelEnabled[(int)OUTL_DebugChannel.AI] = true;
        channelEnabled[(int)OUTL_DebugChannel.Stimulus] = true;
        channelEnabled[(int)OUTL_DebugChannel.Diary] = true;
        channelEnabled[(int)OUTL_DebugChannel.Events] = true;
        channelEnabled[(int)OUTL_DebugChannel.Combat] = true;
        channelEnabled[(int)OUTL_DebugChannel.Loot] = true;
        channelEnabled[(int)OUTL_DebugChannel.Save] = true;
        channelEnabled[(int)OUTL_DebugChannel.Perf] = true;
    }

    public static void Log(string line)
    {
        Log(OUTL_DebugChannel.General, line, true);
    }

    public static void Log(OUTL_DebugChannel channel, string line)
    {
        Log(channel, line, false);
    }

    public static void Log(OUTL_DebugChannel channel, string line, bool force)
    {
        if (string.IsNullOrEmpty(line)) return;
        if (!force && !IsChannelEnabled(channel)) return;

        string stamped = Stamp(channel, line);
        AddRing(stamped);

        if (MirrorToUnityConsole) Debug.Log(stamped);
        Action<string> handler = OnLine;
        if (handler != null) handler(stamped);
    }

    public static bool IsChannelEnabled(OUTL_DebugChannel channel)
    {
        int index = (int)channel;
        if (index < 0 || index >= channelEnabled.Length) return true;
        return channelEnabled[index];
    }

    public static bool SetChannelEnabled(string channelName, bool enabled)
    {
        OUTL_DebugChannel channel;
        if (!TryParseChannel(channelName, out channel)) return false;
        SetChannelEnabled(channel, enabled);
        return true;
    }

    public static void SetChannelEnabled(OUTL_DebugChannel channel, bool enabled)
    {
        int index = (int)channel;
        if (index < 0 || index >= channelEnabled.Length) return;
        channelEnabled[index] = enabled;

        if (channel == OUTL_DebugChannel.AI) AITrace = enabled;
        else if (channel == OUTL_DebugChannel.Stimulus) StimulusTrace = enabled;
        else if (channel == OUTL_DebugChannel.Diary) DiaryTrace = enabled;
        else if (channel == OUTL_DebugChannel.Events) EventTrace = enabled;
        else if (channel == OUTL_DebugChannel.Combat) CombatTrace = enabled;
        else if (channel == OUTL_DebugChannel.Loot) LootTrace = enabled;
        else if (channel == OUTL_DebugChannel.Save) SaveTrace = enabled;
        else if (channel == OUTL_DebugChannel.Perf) PerfTrace = enabled;
    }

    public static string BuildChannelSummary()
    {
        StringBuilder sb = new StringBuilder(128);
        for (int i = 0; i < channelEnabled.Length; i++)
        {
            OUTL_DebugChannel channel = (OUTL_DebugChannel)i;
            if (i > 0) sb.Append(' ');
            sb.Append(channel.ToString().ToLowerInvariant()).Append('=').Append(IsChannelEnabled(channel) ? '1' : '0');
        }
        sb.Append(" unity=").Append(MirrorToUnityConsole ? '1' : '0');
        sb.Append(" aiwatch=").Append(AIWatch.IsValid ? AIWatch.Value.ToString() : "all");
        return sb.ToString();
    }

    public static string DumpRecent(int maxLines)
    {
        int max = Mathf.Clamp(maxLines, 1, RingCapacity);
        StringBuilder sb = new StringBuilder(max * 80);
        int count = ringWrapped ? RingCapacity : ringWriteIndex;
        int start = Mathf.Max(0, count - max);

        for (int n = start; n < count; n++)
        {
            int index = ringWrapped ? (ringWriteIndex + n) % RingCapacity : n;
            string line = ring[index];
            if (!string.IsNullOrEmpty(line)) sb.AppendLine(line);
        }

        return sb.Length > 0 ? sb.ToString() : "log empty";
    }

    public static bool ShouldTraceAI(OUTL_EntityId id)
    {
        if (!AITrace || !IsChannelEnabled(OUTL_DebugChannel.AI)) return false;
        return !AIWatch.IsValid || AIWatch == id;
    }

    public static void TraceAI(OUTL_EntityId id, string message)
    {
        if (!ShouldTraceAI(id)) return;
        Log(OUTL_DebugChannel.AI, "[AI " + id.Value + "] " + message, true);
    }

    public static void TraceStimulus(string message)
    {
        if (!StimulusTrace || !IsChannelEnabled(OUTL_DebugChannel.Stimulus)) return;
        Log(OUTL_DebugChannel.Stimulus, "[STIMULUS] " + message, true);
    }

    public static void TraceDiary(OUTL_EntityId id, string message)
    {
        if (!DiaryTrace || !IsChannelEnabled(OUTL_DebugChannel.Diary)) return;
        string prefix = id.IsValid ? "[DIARY " + id.Value + "] " : "[DIARY] ";
        Log(OUTL_DebugChannel.Diary, prefix + message, true);
    }

    public static bool ShouldTraceCombat()
    {
        return CombatTrace && IsChannelEnabled(OUTL_DebugChannel.Combat);
    }

    public static void TraceCombat(string message)
    {
        if (!ShouldTraceCombat()) return;
        Log(OUTL_DebugChannel.Combat, "[COMBAT] " + message, true);
    }

    private static string Stamp(OUTL_DebugChannel channel, string line)
    {
        OUTL_World world = OUTL_World.Instance;
        float t = world != null ? world.WorldTime : Time.unscaledTime;
        return "[" + channel + "][t=" + t.ToString("0.000") + "] " + line;
    }

    private static void AddRing(string line)
    {
        ring[ringWriteIndex] = line;
        ringWriteIndex++;
        if (ringWriteIndex >= ring.Length)
        {
            ringWriteIndex = 0;
            ringWrapped = true;
        }
    }

    private static bool TryParseChannel(string text, out OUTL_DebugChannel channel)
    {
        channel = OUTL_DebugChannel.General;
        if (string.IsNullOrEmpty(text)) return false;
        return Enum.TryParse(text, true, out channel);
    }
}

public static class OUTL_DebugSettings
{
    public static int DebugHealthMode;
    public static float DebugHealthMaxDistance = 80f;
    public static float DebugHealthVerticalOffset = 0.22f;
    public static bool DebugHealthShowDead = true;
    public static int DebugInventoryMode;
    public static OUTL_EntityId DebugInventoryEntityId = OUTL_EntityId.None;
    public static int DebugMapMode;
    public static float DebugMapRange = 120f;

    public static int SetDebugHealthMode(int mode)
    {
        DebugHealthMode = mode < 0 ? 0 : (mode > 2 ? 2 : mode);
        return DebugHealthMode;
    }

    public static int ToggleDebugHealthMode()
    {
        return SetDebugHealthMode(DebugHealthMode == 0 ? 1 : 0);
    }

    public static int SetDebugInventoryMode(int mode)
    {
        DebugInventoryMode = mode < 0 ? 0 : (mode > 2 ? 2 : mode);
        return DebugInventoryMode;
    }

    public static int ToggleDebugInventoryMode()
    {
        return SetDebugInventoryMode(DebugInventoryMode == 0 ? 1 : 0);
    }

    public static int SetDebugMapMode(int mode)
    {
        DebugMapMode = mode < 0 ? 0 : (mode > 2 ? 2 : mode);
        return DebugMapMode;
    }

    public static int ToggleDebugMapMode()
    {
        return SetDebugMapMode(DebugMapMode == 0 ? 1 : 0);
    }
}

public struct OUTL_DebugHealthRow
{
    public float Health;
    public float MaxHealth;
    public float Fraction;
    public bool Dead;
    public string DisplayName;
    public string ClassName;
    public string StableOrId;
    public string LifeState;
    public string Tier;
    public string Faction;
    public string Authority;
    public string MovementAuthority;
    public string Behavior;
    public string AnchorSource;
}

public static class OUTL_DebugHealthOverlay
{
    private static readonly List<OUTL_EntityRuntime> entities = new List<OUTL_EntityRuntime>(512);
    private static readonly List<Renderer> renderers = new List<Renderer>(16);
    private static readonly List<Collider> colliders = new List<Collider>(16);
    private static readonly GUIContent content = new GUIContent();

    public static void Draw(OUTL_World world)
    {
        int mode = OUTL_DebugSettings.DebugHealthMode;
        if (mode <= 0 || world == null) return;
        Camera camera = Camera.main != null ? Camera.main : Camera.current;
        if (camera == null) return;

        world.Registry.CopyAll(entities);
        float maxDistance = Mathf.Max(1f, OUTL_DebugSettings.DebugHealthMaxDistance);
        float maxDistanceSqr = maxDistance * maxDistance;
        Vector3 cameraPosition = camera.transform.position;

        for (int i = 0; i < entities.Count; i++)
        {
            OUTL_EntityRuntime runtime = entities[i];
            if (runtime == null || runtime.Adapter == null) continue;
            OUTL_EntityAdapter adapter = runtime.Adapter;
            if (!adapter.gameObject.activeInHierarchy) continue;
            if ((adapter.transform.position - cameraPosition).sqrMagnitude > maxDistanceSqr) continue;

            OUTL_DebugHealthRow info;
            if (!TryBuildRow(runtime, out info)) continue;
            if (info.Dead && !OUTL_DebugSettings.DebugHealthShowDead) continue;

            string anchorSource;
            Vector3 screen = camera.WorldToScreenPoint(ResolveTopPoint(adapter, out anchorSource));
            if (screen.z <= 0f) continue;
            info.AnchorSource = anchorSource;
            DrawLabel(screen.x, Screen.height - screen.y, info, mode);
        }
    }

    public static bool TryBuildRow(OUTL_EntityRuntime runtime, out OUTL_DebugHealthRow info)
    {
        info = new OUTL_DebugHealthRow();
        if (runtime == null) return false;

        OUTL_EntityAdapter adapter = runtime.Adapter;
        OUTL_Vitals vitals = adapter != null ? adapter.GetComponent<OUTL_Vitals>() : null;
        string healthKey = vitals != null && !string.IsNullOrEmpty(vitals.HealthKey) ? vitals.HealthKey : "Health";
        string maxHealthKey = vitals != null && !string.IsNullOrEmpty(vitals.MaxHealthKey) ? vitals.MaxHealthKey : "MaxHealth";

        float hp = runtime.Stats.Get(healthKey, float.NaN);
        if (float.IsNaN(hp) && healthKey != "Health") hp = runtime.Stats.Get(OUTL_StatId.Health, float.NaN);
        if (float.IsNaN(hp)) hp = ReadBaseStat(runtime.Def, healthKey, float.NaN);
        if (float.IsNaN(hp) && healthKey != "Health") hp = ReadBaseStat(runtime.Def, "Health", float.NaN);

        float max = runtime.Stats.Get(maxHealthKey, float.NaN);
        if (float.IsNaN(max) && maxHealthKey != "MaxHealth") max = runtime.Stats.Get("MaxHealth", float.NaN);
        if (float.IsNaN(max)) max = ReadBaseStat(runtime.Def, maxHealthKey, float.NaN);
        if (float.IsNaN(max) && maxHealthKey != "MaxHealth") max = ReadBaseStat(runtime.Def, "MaxHealth", float.NaN);
        if (float.IsNaN(max) || max <= 0f) max = ReadBaseStat(runtime.Def, healthKey, float.NaN);
        if ((float.IsNaN(max) || max <= 0f) && healthKey != "Health") max = ReadBaseStat(runtime.Def, "Health", float.NaN);
        if (float.IsNaN(hp) && vitals != null) hp = vitals.DefaultHealth;
        if ((float.IsNaN(max) || max <= 0f) && vitals != null) max = Mathf.Max(vitals.DefaultMaxHealth, vitals.DefaultHealth);
        if (float.IsNaN(hp)) return false;
        if (float.IsNaN(max) || max <= 0f) max = Mathf.Max(1f, hp);

        info.Health = hp;
        info.MaxHealth = max;
        info.Fraction = Mathf.Clamp01(max > 0f ? hp / max : 0f);
        info.Dead = runtime.Dead || runtime.LifeState == OUTL_LifeState.Dead || runtime.LifeState == OUTL_LifeState.DormantDead || hp <= 0f;
        info.DisplayName = BuildDisplayName(runtime);
        info.ClassName = string.IsNullOrEmpty(runtime.ClassName) ? (adapter != null ? adapter.name : "entity") : runtime.ClassName;
        info.StableOrId = !string.IsNullOrEmpty(runtime.StableId) ? runtime.StableId : "#" + runtime.Id.Value;
        info.LifeState = info.Dead ? "DEAD" : runtime.LifeState.ToString();
        info.Tier = runtime.Tier.ToString();
        info.Faction = runtime.Faction != null ? Short(runtime.Faction.FactionId, 18) : "-";
        info.Authority = BuildAuthority(adapter);
        info.MovementAuthority = BuildMovementAuthority(adapter);
        info.Behavior = BuildBehavior(adapter);
        return true;
    }

    private static float ReadBaseStat(OUTL_EntityDef def, string key, float fallback)
    {
        if (def == null || string.IsNullOrEmpty(key) || def.BaseStats == null) return fallback;
        for (int i = 0; i < def.BaseStats.Length; i++)
            if (def.BaseStats[i].Key == key)
                return def.BaseStats[i].Value;
        return fallback;
    }

    private static string BuildDisplayName(OUTL_EntityRuntime runtime)
    {
        if (runtime == null) return "entity";
        if (runtime.Adapter != null)
        {
            OUTL_CharacterIdentity identity = runtime.Adapter.GetComponent<OUTL_CharacterIdentity>();
            if (identity != null)
            {
                identity.EnsureGenerated();
                if (!string.IsNullOrEmpty(identity.DisplayName)) return identity.DisplayName;
            }
        }
        if (runtime.Def != null && !string.IsNullOrEmpty(runtime.Def.DisplayName)) return runtime.Def.DisplayName;
        if (!string.IsNullOrEmpty(runtime.ClassName)) return runtime.ClassName;
        return runtime.Adapter != null ? runtime.Adapter.name : "entity";
    }

    private static string BuildAuthority(OUTL_EntityAdapter adapter)
    {
        if (adapter == null) return "-";
        if (OUTL_NetworkAuthority.IsOffline()) return "offline";
        if (OUTL_NetworkAuthority.IsServerOrHost()) return "server";
        return OUTL_NetworkAuthority.CanAuthoritativeSimulate(adapter) ? "client-local" : "replica";
    }

    private static string BuildMovementAuthority(OUTL_EntityAdapter adapter)
    {
        if (adapter == null) return "-";
        OUTL_NavMeshMover mover = adapter.GetComponent<OUTL_NavMeshMover>();
        if (mover != null && !string.IsNullOrEmpty(mover.CurrentMovementAuthority)) return mover.CurrentMovementAuthority;

        OUTL_ActorControlBridge bridge = adapter.GetComponent<OUTL_ActorControlBridge>();
        if (bridge != null)
        {
            string source = bridge.InputSourceBehaviour != null ? bridge.InputSourceBehaviour.GetType().Name : "auto";
            return "bridge:" + source;
        }

        if (adapter.GetComponent<OUTL_CharacterControllerInputSink>() != null) return "input_cc";
        if (adapter.GetComponent<OUTL_BasicPlayerController>() != null) return "player_motor";
        if (adapter.GetComponent<CharacterController>() != null) return "character_controller";
        return "-";
    }

    private static string BuildBehavior(OUTL_EntityAdapter adapter)
    {
        if (adapter == null) return "-";
        string line = "";
        OUTL_DriveRuntime drive = adapter.GetComponent<OUTL_DriveRuntime>();
        if (drive != null && !string.IsNullOrEmpty(drive.CurrentActionId)) line = "drive:" + drive.CurrentActionId;
        OUTL_NPCBehaviorController npc = adapter.GetComponent<OUTL_NPCBehaviorController>();
        if (npc != null && npc.Runtime != null)
        {
            if (!string.IsNullOrEmpty(line)) line += " ";
            line += "npc:" + npc.Runtime.CurrentAction + " travel=" + npc.Runtime.Travel.Mode + " p=" + npc.Runtime.Travel.RouteProgress.ToString("0.00") + " target=" + CompactVec(npc.Runtime.CurrentTargetPosition);
        }
        OUTL_AIActor ai = adapter.GetComponent<OUTL_AIActor>();
        if (ai != null)
        {
            if (!string.IsNullOrEmpty(line)) line += " ";
            line += "ai:" + ai.CurrentIntent;
        }
        return !string.IsNullOrEmpty(line) ? line : "-";
    }

    private static Vector3 ResolveTopPoint(OUTL_EntityAdapter adapter, out string source)
    {
        source = "Fallback";
        if (adapter == null) return Vector3.zero;
        OUTL_ActorShapeRuntime shape = adapter.GetComponent<OUTL_ActorShapeRuntime>();
        if (shape != null && shape.ShapeProfile != null)
        {
            OUTL_ActorShapeProfileDef profile = shape.ShapeProfile;
            source = "ShapeProfile";
            Vector3 local = profile.CenterOffset + Vector3.up * Mathf.Max(0.01f, profile.BodyHeight * 0.5f + OUTL_DebugSettings.DebugHealthVerticalOffset);
            return adapter.transform.TransformPoint(local);
        }

        Bounds bounds;
        string boundsSource;
        if (TryBuildBounds(adapter, out bounds, out boundsSource))
        {
            source = boundsSource;
            return new Vector3(bounds.center.x, bounds.max.y + OUTL_DebugSettings.DebugHealthVerticalOffset, bounds.center.z);
        }
        return adapter.transform.position + Vector3.up * 2f;
    }

    private static bool TryBuildBounds(OUTL_EntityAdapter adapter, out Bounds bounds, out string source)
    {
        bounds = new Bounds(adapter.transform.position, Vector3.one);
        source = "Fallback";
        bool hasBounds = false;
        bool usedRenderer = false;
        bool usedCollider = false;
        renderers.Clear();
        adapter.GetComponentsInChildren(true, renderers);
        for (int i = 0; i < renderers.Count; i++)
        {
            Renderer r = renderers[i];
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
            if (!hasBounds) { bounds = r.bounds; hasBounds = true; }
            else bounds.Encapsulate(r.bounds);
            usedRenderer = true;
        }

        colliders.Clear();
        adapter.GetComponentsInChildren(true, colliders);
        for (int i = 0; i < colliders.Count; i++)
        {
            Collider c = colliders[i];
            if (c == null || !c.enabled || !c.gameObject.activeInHierarchy) continue;
            if (!hasBounds) { bounds = c.bounds; hasBounds = true; }
            else bounds.Encapsulate(c.bounds);
            usedCollider = true;
        }
        if (usedRenderer && usedCollider) source = "RendererColliderBounds";
        else if (usedRenderer) source = "RendererBounds";
        else if (usedCollider) source = "ColliderBounds";
        return hasBounds;
    }

    private static void DrawLabel(float centerX, float topY, OUTL_DebugHealthRow info, int mode)
    {
        string line1 = info.DisplayName + (info.Dead ? " DEAD" : "") + " " + Format(info.Health) + "/" + Format(info.MaxHealth);
        string line2 = mode >= 2 ? info.ClassName + " " + info.StableOrId + " " + info.LifeState + " " + info.Tier + " f=" + info.Faction + " net=" + info.Authority + " move=" + info.MovementAuthority + " anchor=" + info.AnchorSource : string.Empty;
        string line3 = mode >= 2 && !string.IsNullOrEmpty(info.Behavior) && info.Behavior != "-" ? info.Behavior : string.Empty;
        float width = Mathf.Max(92f, EstimateWidth(line1));
        if (!string.IsNullOrEmpty(line2)) width = Mathf.Max(width, EstimateWidth(line2));
        if (!string.IsNullOrEmpty(line3)) width = Mathf.Max(width, EstimateWidth(line3));
        float height = 26f + (!string.IsNullOrEmpty(line2) ? 15f : 0f) + (!string.IsNullOrEmpty(line3) ? 15f : 0f);
        float x = Mathf.Round(centerX - width * 0.5f);
        float y = Mathf.Round(topY - height);

        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(x - 4f, y - 3f, width + 8f, height + 5f), Texture2D.whiteTexture);
        GUI.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        GUI.DrawTexture(new Rect(x, y, width, 5f), Texture2D.whiteTexture);
        GUI.color = info.Dead ? new Color(0.45f, 0.45f, 0.45f, 1f) : HealthColor(info.Fraction);
        GUI.DrawTexture(new Rect(x, y, width * info.Fraction, 5f), Texture2D.whiteTexture);
        GUI.color = info.Dead ? new Color(0.72f, 0.72f, 0.72f, 1f) : Color.white;
        DrawLine(x, y + 7f, width, line1);
        if (!string.IsNullOrEmpty(line2)) DrawLine(x, y + 22f, width, line2);
        if (!string.IsNullOrEmpty(line3)) DrawLine(x, y + 37f, width, line3);
        GUI.color = oldColor;
    }

    private static void DrawLine(float x, float y, float width, string text)
    {
        content.text = text;
        GUI.Label(new Rect(x, y, width, 16f), content);
    }

    private static Color HealthColor(float fraction)
    {
        if (fraction <= 0.25f) return new Color(1f, 0.2f, 0.15f, 1f);
        if (fraction <= 0.55f) return new Color(1f, 0.78f, 0.15f, 1f);
        return new Color(0.25f, 0.95f, 0.35f, 1f);
    }

    private static float EstimateWidth(string text)
    {
        return Mathf.Clamp((string.IsNullOrEmpty(text) ? 1 : text.Length) * 7f + 10f, 92f, 460f);
    }

    private static string Format(float value)
    {
        return Mathf.Abs(value - Mathf.Round(value)) < 0.01f ? Mathf.RoundToInt(value).ToString() : value.ToString("0.0");
    }

    private static string Short(string text, int max)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= max) return string.IsNullOrEmpty(text) ? "-" : text;
        return text.Substring(0, Mathf.Max(1, max - 1)) + ".";
    }

    private static string CompactVec(Vector3 v)
    {
        return "(" + v.x.ToString("0.#") + "," + v.y.ToString("0.#") + "," + v.z.ToString("0.#") + ")";
    }

}

public static class OUTL_DebugMapOverlay
{
    private static readonly List<OUTL_EntityRuntime> entities = new List<OUTL_EntityRuntime>(512);
    private static readonly List<OUTL_WorldCellSummary> summaries = new List<OUTL_WorldCellSummary>(256);
    private static readonly List<OUTL_Stimulus> stimuli = new List<OUTL_Stimulus>(64);
    private static Rect rect = new Rect(500f, 150f, 360f, 360f);
    private static OUTL_World drawWorld;
    private static int drawMode;
    private static Vector3 focusPosition;

    public static void Draw(OUTL_World world)
    {
        int mode = OUTL_DebugSettings.DebugMapMode;
        if (mode <= 0 || world == null) return;
        drawWorld = world;
        drawMode = mode;
        focusPosition = ResolveFocusPosition(world);
        rect = GUI.Window(0x0A7712, rect, DrawWindow, "OUTL Debug Map");
    }

    private static void DrawWindow(int id)
    {
        OUTL_World world = drawWorld;
        if (world == null) return;
        float size = Mathf.Min(rect.width - 20f, rect.height - 62f);
        Rect mapRect = new Rect(10f, 38f, size, size);
        GUI.Label(new Rect(10f, 18f, rect.width - 20f, 18f), "range=" + OUTL_DebugSettings.DebugMapRange.ToString("0") + "m entities=" + world.Registry.Count);
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.62f);
        GUI.DrawTexture(mapRect, Texture2D.whiteTexture);
        DrawLedgerCells(world, mapRect);
        GUI.color = new Color(0.22f, 0.22f, 0.22f, 1f);
        GUI.DrawTexture(new Rect(mapRect.center.x, mapRect.y, 1f, mapRect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(mapRect.x, mapRect.center.y, mapRect.width, 1f), Texture2D.whiteTexture);

        float range = Mathf.Max(5f, OUTL_DebugSettings.DebugMapRange);
        GUI.color = Color.white;
        GUI.Label(new Rect(10f, mapRect.yMax + 4f, rect.width - 20f, 18f), "ledger=" + world.WorldLedger.CellCount + " stimuli=" + OUTL_StimulusBus.StoredCount + " focus=" + CompactVec(focusPosition));

        world.Registry.CopyAll(entities);
        for (int i = 0; i < entities.Count; i++)
        {
            OUTL_EntityRuntime runtime = entities[i];
            if (runtime == null || runtime.Adapter == null || !runtime.Adapter.gameObject.activeInHierarchy) continue;
            Vector3 p = runtime.Adapter.transform.position;
            Vector3 d = p - focusPosition;
            if (Mathf.Abs(d.x) > range || Mathf.Abs(d.z) > range) continue;
            float x = mapRect.center.x + (d.x / range) * mapRect.width * 0.5f;
            float y = mapRect.center.y - (d.z / range) * mapRect.height * 0.5f;
            DrawBlip(new Rect(x - 3f, y - 3f, 6f, 6f), ColorFor(runtime), LabelFor(runtime, drawMode));
        }
        if (drawMode >= 2) DrawStimuli(mapRect, range);
        GUI.color = old;
        GUI.DragWindow();
    }

    private static void DrawLedgerCells(OUTL_World world, Rect mapRect)
    {
        if (world == null || world.WorldLedger == null) return;
        int count = world.WorldLedger.CopyCellSummaries(summaries);
        if (count <= 0) return;
        float range = Mathf.Max(5f, OUTL_DebugSettings.DebugMapRange);
        float cellSize = Mathf.Max(1f, world.WorldLedger.ActivityCellSize);
        float pixelSize = Mathf.Max(2f, cellSize / range * mapRect.width * 0.5f);
        for (int i = 0; i < summaries.Count; i++)
        {
            OUTL_WorldCellSummary summary = summaries[i];
            Vector3 center = CellCenter(summary.Cell, cellSize);
            Vector3 d = center - focusPosition;
            if (Mathf.Abs(d.x) > range + cellSize || Mathf.Abs(d.z) > range + cellSize) continue;
            float x = mapRect.center.x + (d.x / range) * mapRect.width * 0.5f;
            float y = mapRect.center.y - (d.z / range) * mapRect.height * 0.5f;
            Rect cellRect = new Rect(x - pixelSize * 0.5f, y - pixelSize * 0.5f, pixelSize, pixelSize);
            GUI.color = ColorForSummary(summary);
            GUI.DrawTexture(cellRect, Texture2D.whiteTexture);
            if (drawMode >= 2)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.85f);
                GUI.Label(new Rect(cellRect.x + 2f, cellRect.y + 2f, 190f, 16f), CellLabel(summary));
            }
        }
    }

    private static void DrawStimuli(Rect mapRect, float range)
    {
        int count = OUTL_StimulusBus.QueryRadius(focusPosition, range, stimuli, 64);
        for (int i = 0; i < count; i++)
        {
            OUTL_Stimulus stimulus = stimuli[i];
            Vector3 d = stimulus.Position - focusPosition;
            if (Mathf.Abs(d.x) > range || Mathf.Abs(d.z) > range) continue;
            float x = mapRect.center.x + (d.x / range) * mapRect.width * 0.5f;
            float y = mapRect.center.y - (d.z / range) * mapRect.height * 0.5f;
            string label = "stim " + stimulus.Type + (string.IsNullOrEmpty(stimulus.Key) ? "" : ":" + Short(stimulus.Key, 14));
            DrawBlip(new Rect(x - 2f, y - 2f, 4f, 4f), ColorForStimulus(stimulus.Type, stimulus.Priority), label);
        }
    }

    private static void DrawBlip(Rect r, Color color, string label)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        if (!string.IsNullOrEmpty(label))
        {
            GUI.color = Color.white;
            GUI.Label(new Rect(r.x + 7f, r.y - 5f, 170f, 16f), label);
        }
        GUI.color = old;
    }

    private static Vector3 ResolveFocusPosition(OUTL_World world)
    {
        OUTL_EntityRuntime runtime;
        if (OUTL_DebugSettings.DebugInventoryEntityId.IsValid && world.Registry.TryGet(OUTL_DebugSettings.DebugInventoryEntityId, out runtime) && runtime != null && runtime.Adapter != null)
            return runtime.Adapter.transform.position;
        runtime = world.Registry.FindFirstByTargetName("player");
        if (runtime == null) runtime = world.Registry.FindFirstByClassName("player");
        if (runtime != null && runtime.Adapter != null) return runtime.Adapter.transform.position;
        Camera camera = Camera.main;
        return camera != null ? camera.transform.position : Vector3.zero;
    }

    private static Vector3 CellCenter(OUTL_WorldCellKey cell, float cellSize)
    {
        return new Vector3((cell.X + 0.5f) * cellSize, 0f, (cell.Z + 0.5f) * cellSize);
    }

    private static Color ColorForSummary(OUTL_WorldCellSummary summary)
    {
        float danger = Mathf.Clamp01(Mathf.Max(summary.Danger, summary.EgregoreFear, summary.EgregoreViolence, summary.SpawnPressure));
        float resource = Mathf.Clamp01(Mathf.Max(summary.Food, summary.EgregoreProsperity));
        float corruption = Mathf.Clamp01(Mathf.Max(summary.EgregoreCorruption, summary.BehaviorPressure));
        if (danger >= resource && danger >= corruption && danger > 0.05f) return new Color(1f, 0.12f, 0.08f, 0.22f + danger * 0.30f);
        if (corruption >= resource && corruption > 0.05f) return new Color(0.75f, 0.12f, 1f, 0.20f + corruption * 0.28f);
        if (resource > 0.05f) return new Color(0.1f, 0.9f, 0.35f, 0.16f + resource * 0.22f);
        return new Color(0.25f, 0.45f, 0.95f, summary.EntityCount > 0 ? 0.18f : 0.08f);
    }

    private static Color ColorForStimulus(OUTL_StimulusType type, float priority)
    {
        float a = Mathf.Clamp01(0.45f + priority * 0.45f);
        switch (type)
        {
            case OUTL_StimulusType.HeardCombat:
            case OUTL_StimulusType.Combat:
            case OUTL_StimulusType.TookDamage:
            case OUTL_StimulusType.Damage:
            case OUTL_StimulusType.Death:
                return new Color(1f, 0.1f, 0.05f, a);
            case OUTL_StimulusType.HeardNoise:
            case OUTL_StimulusType.Sound:
            case OUTL_StimulusType.Alert:
            case OUTL_StimulusType.Suspicion:
                return new Color(1f, 0.85f, 0.15f, a);
            case OUTL_StimulusType.Resource:
            case OUTL_StimulusType.SightFood:
            case OUTL_StimulusType.Social:
                return new Color(0.25f, 1f, 0.35f, a);
            case OUTL_StimulusType.SightDanger:
            case OUTL_StimulusType.Fear:
            case OUTL_StimulusType.Fire:
            case OUTL_StimulusType.Egregore:
                return new Color(0.9f, 0.2f, 1f, a);
            default:
                return new Color(0.35f, 0.75f, 1f, a);
        }
    }

    private static string CellLabel(OUTL_WorldCellSummary summary)
    {
        return summary.Cell.X + "," + summary.Cell.Z + " e=" + summary.EntityCount + " npc=" + summary.NpcCount + " d=" + summary.Danger.ToString("0.0") + " f=" + summary.Food.ToString("0.0") + " " + Short(summary.EgregoreCyclePhase.ToString(), 14);
    }

    private static Color ColorFor(OUTL_EntityRuntime runtime)
    {
        if (runtime == null || runtime.Adapter == null) return Color.gray;
        OUTL_EntityAdapter adapter = runtime.Adapter;
        if (runtime.HasTag("Player") || runtime.ClassName.IndexOf("player", StringComparison.OrdinalIgnoreCase) >= 0) return new Color(0.2f, 0.75f, 1f, 1f);
        if (adapter.GetComponent<OUTL_ContainerRuntime>() != null) return new Color(1f, 0.82f, 0.25f, 1f);
        if (adapter.GetComponent<OUTL_ItemPickup>() != null) return new Color(0.35f, 1f, 0.45f, 1f);
        if (adapter.GetComponent<OUTL_Door>() != null || adapter.GetComponent<OUTL_Button>() != null || adapter.GetComponent<OUTL_LogicGate>() != null || adapter.GetComponent<OUTL_LogicRelay>() != null) return new Color(1f, 0.55f, 0.18f, 1f);
        if (adapter.GetComponent<OUTL_AIActor>() != null || adapter.GetComponent<OUTL_NPCBehaviorController>() != null) return new Color(1f, 0.28f, 0.28f, 1f);
        if (runtime.Dead || runtime.LifeState == OUTL_LifeState.Dead) return Color.gray;
        return Color.white;
    }

    private static string LabelFor(OUTL_EntityRuntime runtime, int mode)
    {
        if (runtime == null) return "";
        if (mode < 2) return "";
        string label = "#" + runtime.Id.Value;
        if (!string.IsNullOrEmpty(runtime.ClassName)) label += " " + runtime.ClassName;
        OUTL_EntityAdapter adapter = runtime.Adapter;
        if (adapter == null) return label;

        OUTL_NPCBehaviorController npc = adapter.GetComponent<OUTL_NPCBehaviorController>();
        if (npc != null && npc.Runtime != null)
            label += " " + npc.Runtime.CurrentAction + " r=" + npc.Runtime.Travel.RouteProgress.ToString("0.00");

        OUTL_NavMeshMover nav = adapter.GetComponent<OUTL_NavMeshMover>();
        if (nav != null)
            label += " mv=" + Short(nav.CurrentMovementAuthority, 18);

        OUTL_AIActor ai = adapter.GetComponent<OUTL_AIActor>();
        if (ai != null && !string.IsNullOrEmpty(ai.CurrentIntent))
            label += " ai=" + Short(ai.CurrentIntent, 18);

        OUTL_ContainerRuntime container = adapter.GetComponent<OUTL_ContainerRuntime>();
        if (container != null)
            label += container.IsOpen ? " opened" : " closed";

        OUTL_ItemPickup pickup = adapter.GetComponent<OUTL_ItemPickup>();
        if (pickup != null)
            label += pickup.IsPickedUp ? " picked" : " pickup";
        return label;
    }

    private static string Short(string text, int max)
    {
        if (string.IsNullOrEmpty(text)) return "-";
        if (text.Length <= max) return text;
        return text.Substring(0, Mathf.Max(1, max - 1)) + ".";
    }

    private static string CompactVec(Vector3 v)
    {
        return "(" + v.x.ToString("0.#") + "," + v.z.ToString("0.#") + ")";
    }
}

public static class OUTL_DebugInventoryOverlay
{
    private static readonly List<OUTL_EntityRuntime> entities = new List<OUTL_EntityRuntime>(512);
    private static readonly List<OUTL_InventoryItemSnapshot> inventory = new List<OUTL_InventoryItemSnapshot>(32);
    private static readonly List<OUTL_FloatPair> stats = new List<OUTL_FloatPair>(32);
    private static readonly List<OUTL_FloatPair> stateFloats = new List<OUTL_FloatPair>(32);
    private static readonly List<string> flags = new List<string>(32);
    private static readonly List<OUTL_IntPair> ints = new List<OUTL_IntPair>(32);
    private static readonly List<OUTL_StringPair> strings = new List<OUTL_StringPair>(32);
    private static readonly StringBuilder text = new StringBuilder(256);
    private static Rect rect = new Rect(18f, 150f, 460f, 620f);
    private static Vector2 scroll;
    private static OUTL_World drawWorld;
    private static int drawMode;

    public static void Draw(OUTL_World world)
    {
        int mode = OUTL_DebugSettings.DebugInventoryMode;
        if (mode <= 0 || world == null) return;
        drawWorld = world;
        drawMode = mode;
        rect = GUI.Window(0x0A7711, rect, DrawWindow, "OUTL Inventory / Container / Stats");
    }

    private static void DrawWindow(int id)
    {
        OUTL_World world = drawWorld;
        int mode = drawMode;
        if (world == null) return;

        scroll = GUILayout.BeginScrollView(scroll);
        OUTL_EntityRuntime focus = ResolveFocus(world);

        GUILayout.Label("World", GUI.skin.box);
        GUILayout.Label("entities=" + world.Registry.Count + " events=" + world.Events.PendingCount + " queued=" + world.Commands.QueuedCount + " debugInv=" + mode);

        GUILayout.Space(4f);
        GUILayout.Label("Focus Entity", GUI.skin.box);
        if (focus == null) GUILayout.Label("<none>");
        else
        {
            GUILayout.Label(DescribeEntity(focus));
            DrawHealthSummary(focus);
            DrawFocusComponents(focus, mode);
            DrawInventory(world, focus.Id, "Inventory");
            if (mode >= 2) DrawRuntimeDetails(focus);
        }

        GUILayout.Space(4f);
        DrawNearbyContainers(world, focus, mode);

        GUILayout.Space(4f);
        DrawNearbyPickups(world, focus);

        GUILayout.EndScrollView();
        GUI.DragWindow();
    }

    private static OUTL_EntityRuntime ResolveFocus(OUTL_World world)
    {
        if (world == null) return null;
        OUTL_EntityRuntime runtime;
        if (OUTL_DebugSettings.DebugInventoryEntityId.IsValid && world.Registry.TryGet(OUTL_DebugSettings.DebugInventoryEntityId, out runtime))
            return runtime;

        runtime = world.Registry.FindFirstByTargetName("player");
        if (runtime != null) return runtime;
        runtime = world.Registry.FindFirstByClassName("player");
        if (runtime != null) return runtime;

        world.Registry.CopyAll(entities);
        for (int i = 0; i < entities.Count; i++)
            if (entities[i] != null && entities[i].HasTag("Player"))
                return entities[i];
        return entities.Count > 0 ? entities[0] : null;
    }

    private static void DrawInventory(OUTL_World world, OUTL_EntityId owner, string title)
    {
        GUILayout.Label(title);
        int count = world.Inventory.CopyItems(owner, inventory);
        if (count <= 0)
        {
            GUILayout.Label("  <empty>");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            OUTL_InventoryItemSnapshot item = inventory[i];
            if (item == null || item.Item == null) continue;
            GUILayout.Label("  " + DisplayItem(item.Item) + " x" + item.Count);
        }
    }

    private static void DrawRuntimeDetails(OUTL_EntityRuntime runtime)
    {
        if (runtime == null) return;
        GUILayout.Label("Stats");
        runtime.Stats.CopyTo(stats);
        if (stats.Count == 0) GUILayout.Label("  <none>");
        for (int i = 0; i < stats.Count; i++)
            GUILayout.Label("  " + stats[i].Key + " = " + stats[i].Value.ToString("0.##"));

        GUILayout.Label("State");
        runtime.State.CopyFlags(flags);
        runtime.State.CopyFloats(stateFloats);
        runtime.State.CopyInts(ints);
        runtime.State.CopyStrings(strings);
        GUILayout.Label("  flags=" + JoinFlags());
        for (int i = 0; i < stateFloats.Count; i++) GUILayout.Label("  float " + stateFloats[i].Key + " = " + stateFloats[i].Value.ToString("0.##"));
        for (int i = 0; i < ints.Count; i++) GUILayout.Label("  int " + ints[i].Key + " = " + ints[i].Value);
        for (int i = 0; i < strings.Count; i++) GUILayout.Label("  str " + strings[i].Key + " = " + strings[i].Value);
    }

    private static void DrawHealthSummary(OUTL_EntityRuntime runtime)
    {
        if (runtime == null) return;
        float hp = ReadStatOrBase(runtime, "Health", float.NaN);
        float max = ReadStatOrBase(runtime, "MaxHealth", float.NaN);
        if (float.IsNaN(max) || max <= 0f) max = ReadStatOrBase(runtime, "Health", float.NaN);
        OUTL_Vitals vitals = runtime.Adapter != null ? runtime.Adapter.GetComponent<OUTL_Vitals>() : null;
        if (float.IsNaN(hp) && vitals != null) hp = vitals.DefaultHealth;
        if ((float.IsNaN(max) || max <= 0f) && vitals != null) max = Mathf.Max(vitals.DefaultMaxHealth, vitals.DefaultHealth);

        string hpText = float.IsNaN(hp) ? "hp=<none>" : "hp=" + FormatFloat(hp) + "/" + FormatFloat(float.IsNaN(max) ? Mathf.Max(1f, hp) : max);
        string faction = runtime.Faction != null ? runtime.Faction.FactionId : "-";
        string stable = !string.IsNullOrEmpty(runtime.StableId) ? runtime.StableId : "-";
        GUILayout.Label("  " + hpText + " life=" + runtime.LifeState + " dead=" + runtime.Dead + " faction=" + faction + " stable=" + stable + " targetname=" + runtime.TargetName);
    }

    private static void DrawFocusComponents(OUTL_EntityRuntime runtime, int mode)
    {
        if (runtime == null || runtime.Adapter == null) return;
        OUTL_ContainerRuntime container = runtime.Adapter.GetComponent<OUTL_ContainerRuntime>();
        if (container != null) DrawContainerDetails(container, mode, "Focus Container");

        OUTL_ItemPickup pickup = runtime.Adapter.GetComponent<OUTL_ItemPickup>();
        if (pickup != null)
            GUILayout.Label("Focus Pickup: item=" + DisplayItem(pickup.Item) + " x" + pickup.Count + " picked=" + pickup.IsPickedUp + " key=" + pickup.PickupKey);

        OUTL_NavMeshMover mover = runtime.Adapter.GetComponent<OUTL_NavMeshMover>();
        if (mover != null) GUILayout.Label("Movement: authority=" + mover.CurrentMovementAuthority + " hasDestination=" + mover.HasDestination + " stop=" + mover.StopDistance.ToString("0.##"));

        OUTL_NPCBehaviorController npc = runtime.Adapter.GetComponent<OUTL_NPCBehaviorController>();
        if (npc != null && npc.Runtime != null)
            GUILayout.Label("NPC: action=" + npc.Runtime.CurrentAction + " entry=" + npc.Runtime.CurrentEntryId + " travel=" + npc.Runtime.Travel.Mode + " route=" + npc.Runtime.Travel.RouteKey);
    }

    private static void DrawNearbyContainers(OUTL_World world, OUTL_EntityRuntime focus, int mode)
    {
        GUILayout.Label("Containers", GUI.skin.box);
        Vector3 focusPosition = focus != null && focus.Adapter != null ? focus.Adapter.transform.position : Vector3.zero;
        world.Registry.CopyAll(entities);
        int shown = 0;
        for (int i = 0; i < entities.Count && shown < 12; i++)
        {
            OUTL_EntityRuntime runtime = entities[i];
            if (runtime == null || runtime.Adapter == null) continue;
            OUTL_ContainerRuntime container = runtime.Adapter.GetComponent<OUTL_ContainerRuntime>();
            if (container == null) continue;
            float distance = focus != null && focus.Adapter != null ? Vector3.Distance(focusPosition, runtime.Adapter.transform.position) : 0f;
            GUILayout.Label("  " + DescribeEntity(runtime) + " d=" + distance.ToString("0.0") + "m open=" + container.IsOpen + " locked=" + container.IsLocked + " looted=" + container.Looted + " spawned=" + container.LastSpawnedCount);
            if (mode >= 2) DrawContainerDetails(container, mode, "    details");
            shown++;
        }
        if (shown == 0) GUILayout.Label("  <none>");
    }

    private static void DrawNearbyPickups(OUTL_World world, OUTL_EntityRuntime focus)
    {
        GUILayout.Label("Pickups", GUI.skin.box);
        Vector3 focusPosition = focus != null && focus.Adapter != null ? focus.Adapter.transform.position : Vector3.zero;
        world.Registry.CopyAll(entities);
        int shown = 0;
        for (int i = 0; i < entities.Count && shown < 12; i++)
        {
            OUTL_EntityRuntime runtime = entities[i];
            if (runtime == null || runtime.Adapter == null) continue;
            OUTL_ItemPickup pickup = runtime.Adapter.GetComponent<OUTL_ItemPickup>();
            if (pickup == null) continue;
            float distance = focus != null && focus.Adapter != null ? Vector3.Distance(focusPosition, runtime.Adapter.transform.position) : 0f;
            GUILayout.Label("  " + DescribeEntity(runtime) + " d=" + distance.ToString("0.0") + "m item=" + DisplayItem(pickup.Item) + " x" + pickup.Count + " picked=" + pickup.IsPickedUp);
            shown++;
        }
        if (shown == 0) GUILayout.Label("  <none>");
    }

    private static string DescribeEntity(OUTL_EntityRuntime runtime)
    {
        if (runtime == null) return "<none>";
        string name = runtime.Def != null && !string.IsNullOrEmpty(runtime.Def.DisplayName) ? runtime.Def.DisplayName : (!string.IsNullOrEmpty(runtime.ClassName) ? runtime.ClassName : "entity");
        string stable = !string.IsNullOrEmpty(runtime.StableId) ? " stable=" + runtime.StableId : "";
        string target = !string.IsNullOrEmpty(runtime.TargetName) ? " target=" + runtime.TargetName : "";
        return "#" + runtime.Id.Value + " " + name + " class=" + runtime.ClassName + stable + target + " tier=" + runtime.Tier + " life=" + runtime.LifeState;
    }

    private static string DisplayItem(OUTL_ItemDef item)
    {
        if (item == null) return "<null>";
        if (!string.IsNullOrEmpty(item.DisplayName)) return item.DisplayName;
        if (!string.IsNullOrEmpty(item.ClassName)) return item.ClassName;
        return item.name;
    }

    private static string JoinFlags()
    {
        if (flags.Count == 0) return "<none>";
        text.Length = 0;
        for (int i = 0; i < flags.Count; i++)
        {
            if (i > 0) text.Append(", ");
            text.Append(flags[i]);
        }
        return text.ToString();
    }

    private static void DrawContainerDetails(OUTL_ContainerRuntime container, int mode, string title)
    {
        if (container == null) return;
        OUTL_ContainerDef def = container.Def;
        GUILayout.Label(title + ": open=" + container.IsOpen + " locked=" + container.IsLocked + " looted=" + container.Looted + " seed=" + container.RolledSeed + " spawned=" + container.LastSpawnedCount);
        if (def == null)
        {
            if (mode >= 2) GUILayout.Label("    def=<none>");
            return;
        }

        GUILayout.Label("    def=" + def.ContainerId + " openKey=" + def.OpenKey + " lootKey=" + def.LootKey + " startsLocked=" + def.StartsLocked);
        DrawLootTable(def.LootTable, "    ");
    }

    private static void DrawLootTable(OUTL_LootTableDef table, string indent)
    {
        if (table == null)
        {
            GUILayout.Label(indent + "loot=<none>");
            return;
        }

        int entryCount = table.Entries != null ? table.Entries.Length : 0;
        GUILayout.Label(indent + "loot=" + table.TableId + " rollEach=" + table.RollEachEntry + " maxDrops=" + table.MaxDrops + " entries=" + entryCount);
        if (table.Entries == null) return;
        int maxShown = Mathf.Min(table.Entries.Length, 8);
        for (int i = 0; i < maxShown; i++)
            GUILayout.Label(indent + "  " + DescribeLootEntry(i, table.Entries[i]));
        if (table.Entries.Length > maxShown) GUILayout.Label(indent + "  ... +" + (table.Entries.Length - maxShown) + " more");
    }

    private static string DescribeLootEntry(int index, OUTL_LootTableEntry entry)
    {
        if (entry == null) return "[" + index + "] <null>";
        string item = entry.Item != null ? DisplayItem(entry.Item) : (entry.EntityDef != null ? entry.EntityDef.DisplayName : "<no item/entity>");
        string prefab = entry.PickupPrefab != null ? entry.PickupPrefab.name : "-";
        return "[" + index + "] " + entry.Label + " item=" + item + " count=" + entry.MinCount + "-" + entry.MaxCount + " chance=" + entry.Chance.ToString("0.##") + " weight=" + entry.Weight.ToString("0.##") + " prefab=" + prefab + " tag=" + entry.ContextTag;
    }

    private static float ReadStatOrBase(OUTL_EntityRuntime runtime, string key, float fallback)
    {
        if (runtime == null || string.IsNullOrEmpty(key)) return fallback;
        float value = runtime.Stats.Get(key, float.NaN);
        if (!float.IsNaN(value)) return value;
        if (runtime.Def != null && runtime.Def.BaseStats != null)
        {
            for (int i = 0; i < runtime.Def.BaseStats.Length; i++)
                if (runtime.Def.BaseStats[i].Key == key)
                    return runtime.Def.BaseStats[i].Value;
        }
        return fallback;
    }

    private static string FormatFloat(float value)
    {
        if (float.IsNaN(value)) return "-";
        return Mathf.Abs(value - Mathf.Round(value)) < 0.01f ? Mathf.RoundToInt(value).ToString() : value.ToString("0.0");
    }
}
