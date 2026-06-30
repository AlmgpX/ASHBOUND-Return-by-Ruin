using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum OUTL_DebugHealthAnchorSource
{
    None = 0,
    ExplicitAnchor = 1,
    ShapeProfile = 2,
    CharacterController = 3,
    NavMeshAgent = 4,
    RendererBounds = 5,
    ColliderBounds = 6,
    Fallback = 7
}

[DisallowMultipleComponent]
public sealed class OUTL_DebugHealthAnchor : MonoBehaviour
{
    public Transform Anchor;
    public Vector3 LocalOffset = new Vector3(0f, 1.8f, 0f);

    public Vector3 WorldPosition
    {
        get
        {
            Transform t = Anchor != null ? Anchor : transform;
            return t.TransformPoint(LocalOffset);
        }
    }
}

public static class OUTL_DebugHealthSettings
{
    public static int HealthMode;
    public static float VerticalOffset = 0.18f;
    public static float MaxDistance = 80f;
    public static bool ShowDead = true;
    public static bool ShowAnchorCrossInMode2 = true;

    public static bool Enabled { get { return HealthMode > 0; } }

    public static void SetHealthMode(int mode)
    {
        HealthMode = Mathf.Clamp(mode, 0, 2);
    }

    public static void ToggleHealthMode()
    {
        HealthMode = HealthMode > 0 ? 0 : 1;
    }
}

[DefaultExecutionOrder(9800)]
[DisallowMultipleComponent]
public sealed class OUTL_DebugHealthLabels : MonoBehaviour
{
    private readonly List<OUTL_EntityRuntime> entityBuffer = new List<OUTL_EntityRuntime>(256);
    private readonly List<OUTL_EntityRuntime> cameraEntityBuffer = new List<OUTL_EntityRuntime>(64);
    private GUIStyle labelStyle;
    private GUIStyle smallStyle;
    private Texture2D whiteTexture;

    public float LabelWidth = 190f;
    public float LabelHeight = 36f;
    public float BarWidth = 72f;
    public float BarHeight = 5f;
    public bool AutoDisableIfNoWorld;

    public static OUTL_DebugHealthLabels EnsureInScene()
    {
        OUTL_DebugHealthLabels labels = FindObjectOfType<OUTL_DebugHealthLabels>();
        if (labels != null) return labels;
        GameObject go = new GameObject("OUTL_DebugHealthLabels");
        labels = go.AddComponent<OUTL_DebugHealthLabels>();
        return labels;
    }

    public static void SetMode(int mode)
    {
        OUTL_DebugHealthSettings.SetHealthMode(mode);
        if (OUTL_DebugHealthSettings.Enabled) EnsureInScene();
    }

    public static void Toggle()
    {
        OUTL_DebugHealthSettings.ToggleHealthMode();
        if (OUTL_DebugHealthSettings.Enabled) EnsureInScene();
    }

    private void Awake()
    {
        EnsureStyles();
    }

    private void OnGUI()
    {
        if (!OUTL_DebugHealthSettings.Enabled) return;
        OUTL_World world = OUTL_World.Instance;
        if (world == null)
        {
            if (AutoDisableIfNoWorld) enabled = false;
            return;
        }

        Camera cam = ResolveDebugCamera(world);
        if (cam == null) return;

        EnsureStyles();
        entityBuffer.Clear();
        world.Registry.CopyAll(entityBuffer);

        float maxDistance = Mathf.Max(0.1f, OUTL_DebugHealthSettings.MaxDistance);
        float maxSqr = maxDistance * maxDistance;
        Vector3 camPos = cam.transform.position;

        for (int i = 0; i < entityBuffer.Count; i++)
        {
            OUTL_EntityRuntime runtime = entityBuffer[i];
            if (!ShouldDraw(runtime)) continue;
            OUTL_EntityAdapter adapter = runtime.Adapter;
            if ((adapter.transform.position - camPos).sqrMagnitude > maxSqr) continue;

            OUTL_DebugHealthAnchorSource source;
            Vector3 anchor = ResolveHealthLabelAnchor(adapter, out source);
            Vector3 screen = cam.WorldToScreenPoint(anchor);
            if (screen.z <= 0f) continue;
            if (screen.x < -LabelWidth || screen.x > Screen.width + LabelWidth || screen.y < -LabelHeight || screen.y > Screen.height + LabelHeight) continue;

            float hp;
            float maxHp;
            if (!TryGetHealth(runtime, out hp, out maxHp)) continue;

            DrawHealthLabel(runtime, screen, hp, maxHp, source);
        }
    }

    public static Vector3 ResolveHealthLabelAnchor(OUTL_EntityAdapter entity, out OUTL_DebugHealthAnchorSource source)
    {
        source = OUTL_DebugHealthAnchorSource.None;
        if (entity == null) return Vector3.zero;

        OUTL_DebugHealthAnchor explicitAnchor = entity.GetComponentInChildren<OUTL_DebugHealthAnchor>();
        if (explicitAnchor != null)
        {
            source = OUTL_DebugHealthAnchorSource.ExplicitAnchor;
            return explicitAnchor.WorldPosition;
        }

        OUTL_ActorShapeRuntime shape = entity.GetComponent<OUTL_ActorShapeRuntime>();
        if (shape != null && shape.ShapeProfile != null)
        {
            source = OUTL_DebugHealthAnchorSource.ShapeProfile;
            OUTL_ActorShapeProfileDef profile = shape.ShapeProfile;
            return entity.transform.TransformPoint(profile.CenterOffset + Vector3.up * (profile.BodyHeight * 0.5f + OUTL_DebugHealthSettings.VerticalOffset));
        }

        CharacterController controller = entity.GetComponent<CharacterController>();
        if (controller != null)
        {
            source = OUTL_DebugHealthAnchorSource.CharacterController;
            return entity.transform.TransformPoint(controller.center + Vector3.up * (controller.height * 0.5f + OUTL_DebugHealthSettings.VerticalOffset));
        }

        NavMeshAgent agent = entity.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            source = OUTL_DebugHealthAnchorSource.NavMeshAgent;
            return entity.transform.position + Vector3.up * (Mathf.Max(0.01f, agent.height) + OUTL_DebugHealthSettings.VerticalOffset);
        }

        Bounds bounds;
        if (TryGetCombinedRendererBounds(entity.gameObject, out bounds))
        {
            source = OUTL_DebugHealthAnchorSource.RendererBounds;
            Vector3 p = bounds.center;
            p.y = bounds.max.y + OUTL_DebugHealthSettings.VerticalOffset;
            return p;
        }

        if (TryGetCombinedColliderBounds(entity.gameObject, out bounds))
        {
            source = OUTL_DebugHealthAnchorSource.ColliderBounds;
            Vector3 p = bounds.center;
            p.y = bounds.max.y + OUTL_DebugHealthSettings.VerticalOffset;
            return p;
        }

        source = OUTL_DebugHealthAnchorSource.Fallback;
        return entity.transform.position + Vector3.up * (1.8f + OUTL_DebugHealthSettings.VerticalOffset);
    }

    private static bool TryGetCombinedRendererBounds(GameObject root, out Bounds bounds)
    {
        bounds = default(Bounds);
        if (root == null) return false;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(false);
        bool has = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || !r.enabled) continue;
            if (!has) { bounds = r.bounds; has = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return has;
    }

    private static bool TryGetCombinedColliderBounds(GameObject root, out Bounds bounds)
    {
        bounds = default(Bounds);
        if (root == null) return false;
        Collider[] colliders = root.GetComponentsInChildren<Collider>(false);
        bool has = false;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider c = colliders[i];
            if (c == null || !c.enabled) continue;
            if (!has) { bounds = c.bounds; has = true; }
            else bounds.Encapsulate(c.bounds);
        }
        return has;
    }

    private static bool ShouldDraw(OUTL_EntityRuntime runtime)
    {
        if (runtime == null || runtime.Adapter == null || !runtime.Adapter.isActiveAndEnabled) return false;
        if (!OUTL_DebugHealthSettings.ShowDead && (runtime.Dead || runtime.LifeState == OUTL_LifeState.Dead)) return false;
        float hp;
        float maxHp;
        return TryGetHealth(runtime, out hp, out maxHp);
    }

    private static bool TryGetHealth(OUTL_EntityRuntime runtime, out float hp, out float maxHp)
    {
        hp = 0f;
        maxHp = 0f;
        if (runtime == null) return false;

        hp = runtime.Stats.Get(OUTL_StatId.Health, float.NaN);
        maxHp = runtime.Stats.Get("MaxHealth", float.NaN);

        OUTL_Vitals vitals = runtime.Adapter != null ? runtime.Adapter.GetComponent<OUTL_Vitals>() : null;
        if (float.IsNaN(hp) && vitals != null) hp = runtime.Stats.Get(vitals.HealthKey, vitals.DefaultHealth);
        if (float.IsNaN(maxHp) && vitals != null) maxHp = runtime.Stats.Get(vitals.MaxHealthKey, vitals.DefaultMaxHealth);

        if (float.IsNaN(hp)) hp = 0f;
        if (float.IsNaN(maxHp)) maxHp = vitals != null ? Mathf.Max(1f, vitals.DefaultMaxHealth) : Mathf.Max(1f, hp);
        if (maxHp <= 0f && hp <= 0f && vitals == null) return false;
        if (maxHp <= 0f) maxHp = Mathf.Max(1f, hp);
        return true;
    }

    private Camera ResolveDebugCamera(OUTL_World world)
    {
        if (world != null)
        {
            cameraEntityBuffer.Clear();
            world.Registry.CopyAll(cameraEntityBuffer);
            for (int i = 0; i < cameraEntityBuffer.Count; i++)
            {
                OUTL_EntityRuntime runtime = cameraEntityBuffer[i];
                if (runtime == null || runtime.Adapter == null) continue;
                OUTL_PlayerInputSource source = runtime.Adapter.GetComponent<OUTL_PlayerInputSource>();
                if (source != null && source.ViewCamera != null && source.ViewCamera.enabled) return source.ViewCamera;
            }
        }

        if (Camera.main != null && Camera.main.enabled) return Camera.main;
        Camera[] cameras = FindObjectsOfType<Camera>();
        for (int i = 0; i < cameras.Length; i++)
            if (cameras[i] != null && cameras[i].enabled)
                return cameras[i];
        return null;
    }

    private void DrawHealthLabel(OUTL_EntityRuntime runtime, Vector3 screen, float hp, float maxHp, OUTL_DebugHealthAnchorSource source)
    {
        float width = LabelWidth;
        float height = OUTL_DebugHealthSettings.HealthMode >= 2 ? LabelHeight + 30f : LabelHeight;
        float guiX = screen.x - width * 0.5f;
        float guiY = Screen.height - screen.y - height;
        Rect rect = new Rect(guiX, guiY, width, height);

        float t = Mathf.Clamp01(maxHp > 0f ? hp / maxHp : 0f);
        string name = !string.IsNullOrEmpty(runtime.ClassName) ? runtime.ClassName : (runtime.Adapter != null ? runtime.Adapter.name : "entity");
        string dead = runtime.Dead || runtime.LifeState == OUTL_LifeState.Dead ? " DEAD" : string.Empty;

        GUI.Label(rect, name + dead + "  " + Mathf.CeilToInt(hp) + "/" + Mathf.CeilToInt(maxHp), labelStyle);

        Rect barBack = new Rect(screen.x - BarWidth * 0.5f, guiY + 18f, BarWidth, BarHeight);
        DrawRect(barBack, new Color(0f, 0f, 0f, 0.75f));
        Rect bar = new Rect(barBack.x + 1f, barBack.y + 1f, Mathf.Max(0f, (BarWidth - 2f) * t), Mathf.Max(1f, BarHeight - 2f));
        DrawRect(bar, Color.Lerp(new Color(0.9f, 0.1f, 0.1f, 0.95f), new Color(0.1f, 0.9f, 0.25f, 0.95f), t));

        if (OUTL_DebugHealthSettings.HealthMode >= 2)
        {
            string id = runtime.Id.IsValid ? runtime.Id.Value.ToString() : "none";
            string faction = runtime.Faction != null ? runtime.Faction.FactionId : "none";
            string stable = string.IsNullOrEmpty(runtime.StableId) ? "-" : runtime.StableId;
            GUI.Label(new Rect(guiX, guiY + 28f, width, 40f), "id=" + id + " tier=" + runtime.Tier + " src=" + source + "\nstable=" + stable + " faction=" + faction, smallStyle);

            if (OUTL_DebugHealthSettings.ShowAnchorCrossInMode2)
            {
                DrawRect(new Rect(screen.x - 3f, Screen.height - screen.y - 1f, 6f, 2f), Color.yellow);
                DrawRect(new Rect(screen.x - 1f, Screen.height - screen.y - 3f, 2f, 6f), Color.yellow);
            }
        }
    }

    private void EnsureStyles()
    {
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.UpperCenter;
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontSize = 12;
            labelStyle.fontStyle = FontStyle.Bold;
        }
        if (smallStyle == null)
        {
            smallStyle = new GUIStyle(GUI.skin.label);
            smallStyle.alignment = TextAnchor.UpperCenter;
            smallStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
            smallStyle.fontSize = 10;
        }
        if (whiteTexture == null)
        {
            whiteTexture = Texture2D.whiteTexture;
        }
    }

    private void DrawRect(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, whiteTexture);
        GUI.color = old;
    }
}
