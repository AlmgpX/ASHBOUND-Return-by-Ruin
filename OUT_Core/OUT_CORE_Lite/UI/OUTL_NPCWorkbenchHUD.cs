using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(1310)]
[DisallowMultipleComponent]
public sealed class OUTL_NPCWorkbenchHUD : MonoBehaviour
{
    public bool Show;
    public KeyCode ToggleKey = KeyCode.F3;
    public Rect WindowRect = new Rect(900f, 80f, 520f, 560f);
    public bool AutoRefresh = true;
    public float RefreshInterval = 0.5f;
    public bool ShowOnlyAI = true;

    private readonly List<OUTL_AIActor> aiActors = new List<OUTL_AIActor>(64);
    private readonly List<OUTL_EntityAdapter> entities = new List<OUTL_EntityAdapter>(128);
    private readonly List<OUTL_EntityRuntime> entityBuffer = new List<OUTL_EntityRuntime>(128);
    private Vector2 scroll;
    private float nextRefresh;

    private void Update()
    {
        if (Input.GetKeyDown(ToggleKey)) Show = !Show;
        if (!AutoRefresh) return;
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + Mathf.Max(0.05f, RefreshInterval);
        RefreshLists();
    }

    private void OnGUI()
    {
        if (!Show) return;
        WindowRect = GUI.Window(GetInstanceID(), WindowRect, DrawWindow, "OUTL NPC Workbench");
    }

    [ContextMenu("OUT Refresh NPC Workbench")]
    public void RefreshLists()
    {
        aiActors.Clear();
        entities.Clear();

        OUTL_World world = OUTL_World.Instance;
        if (world == null) return;
        world.Registry.CopyAll(entityBuffer);
        for (int i = 0; i < entityBuffer.Count; i++)
        {
            OUTL_EntityRuntime runtime = entityBuffer[i];
            OUTL_EntityAdapter e = runtime != null ? runtime.Adapter : null;
            if (e == null) continue;
            OUTL_AIActor ai = e.GetComponent<OUTL_AIActor>();
            if (ai != null) aiActors.Add(ai);
            if (ShowOnlyAI && ai == null) continue;
            entities.Add(e);
        }
        entityBuffer.Clear();
    }

    private void DrawWindow(int id)
    {
        if (GUILayout.Button("Refresh")) RefreshLists();
        ShowOnlyAI = GUILayout.Toggle(ShowOnlyAI, "Show only entities with OUTL_AIActor");
        OUTL_World world = OUTL_World.Instance;
        GUILayout.Label(world != null ? ("world t=" + world.WorldTime.ToString("0.00") + " entities=" + world.Registry.Count) : "NO OUTL_WORLD");
        GUILayout.Label("F3 toggles this window.");

        scroll = GUILayout.BeginScrollView(scroll);

        GUILayout.Label("AI Actors: " + aiActors.Count, GUI.skin.box);
        for (int i = 0; i < aiActors.Count; i++) DrawAI(aiActors[i]);

        GUILayout.Space(8f);
        GUILayout.Label("Entity Summary: " + entities.Count, GUI.skin.box);
        for (int i = 0; i < entities.Count; i++) DrawEntity(entities[i]);

        GUILayout.EndScrollView();
        GUI.DragWindow();
    }

    private void DrawAI(OUTL_AIActor ai)
    {
        if (ai == null) return;
        OUTL_EntityAdapter e = ai.Entity != null ? ai.Entity : ai.GetComponent<OUTL_EntityAdapter>();
        OUTL_EntityRuntime rt = e != null ? e.Runtime : null;
        string name = ai.name;
        string faction = e != null && e.Faction != null ? e.Faction.FactionId : "<none>";
        string profile = ai.Profile != null ? ai.Profile.ProfileId : "<no profile>";
        string hp = rt != null ? rt.Stats.Get(OUTL_StatId.Health, 0f).ToString("0.#") : "?";
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label(name + " | faction=" + faction + " | hp=" + hp);
        GUILayout.Label("profile=" + profile);
        GUILayout.Label(ai.DescribeThinking());
        OUTL_AttackDriver attack = ai.AttackDriver != null ? ai.AttackDriver : ai.GetComponent<OUTL_AttackDriver>();
        if (attack != null)
        {
            GUILayout.Label("primary=" + AttackName(attack.Primary) + " melee=" + AttackName(attack.Melee));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Force Primary") && attack.Primary != null) attack.FirePrimary();
            if (GUILayout.Button("Force Melee") && attack.Melee != null) attack.FireMelee();
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
    }

    private void DrawEntity(OUTL_EntityAdapter e)
    {
        if (e == null) return;
        OUTL_EntityRuntime rt = e.Runtime;
        string faction = e.Faction != null ? e.Faction.FactionId : "<none>";
        string className = rt != null ? rt.ClassName : e.ClassNameOverride;
        string targetName = rt != null ? rt.TargetName : e.TargetName;
        string hp = rt != null ? rt.Stats.Get(OUTL_StatId.Health, 0f).ToString("0.#") : "?";
        GUILayout.Label(e.name + " | " + className + " | target=" + targetName + " | faction=" + faction + " | hp=" + hp);
    }

    private static string AttackName(OUTL_AttackProfile profile)
    {
        return profile != null ? profile.AttackId + "/" + profile.Mode : "<none>";
    }
}
