using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_AIStateTableDebugView : MonoBehaviour
{
    public bool Show;
    public KeyCode ToggleKey = KeyCode.F3;
    public float RefreshInterval = 0.25f;
    public Rect WindowRect = new Rect(20f, 390f, 1230f, 360f);
    public int MaxRows = 32;

    private readonly List<OUTL_EntityRuntime> entities = new List<OUTL_EntityRuntime>(128);
    private readonly List<OUTL_AIActor> actors = new List<OUTL_AIActor>(64);
    private readonly OUTL_AIStateDebugRow[] rows = new OUTL_AIStateDebugRow[64];
    private float nextRefresh;

    private void Update()
    {
        if (Input.GetKeyDown(ToggleKey)) Show = !Show;
    }

    private void OnGUI()
    {
        if (!Show) return;
        if (Time.unscaledTime >= nextRefresh) RefreshRows();
        WindowRect = GUI.Window(GetInstanceID(), WindowRect, DrawWindow, "OUTL AI State Table");
    }

    private void RefreshRows()
    {
        nextRefresh = Time.unscaledTime + Mathf.Max(0.05f, RefreshInterval);
        actors.Clear();
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return;

        world.Registry.CopyAll(entities);
        int limit = Mathf.Min(Mathf.Max(1, MaxRows), rows.Length);
        for (int i = 0; i < entities.Count && actors.Count < limit; i++)
        {
            OUTL_EntityRuntime runtime = entities[i];
            if (runtime == null || runtime.Adapter == null) continue;
            OUTL_AIActor ai = runtime.Adapter.GetComponent<OUTL_AIActor>();
            if (ai == null || !ai.ExposeDebugState) continue;
            actors.Add(ai);
            OUTL_AIStateDebugRow row = rows[actors.Count - 1];
            ai.FillDebugRow(ref row);
            rows[actors.Count - 1] = row;
        }
    }

    private void DrawWindow(int id)
    {
        GUILayout.BeginVertical();
        DrawHeader();
        for (int i = 0; i < actors.Count; i++)
            DrawRow(rows[i]);
        GUILayout.EndVertical();
        GUI.DragWindow();
    }

    private static void DrawHeader()
    {
        GUILayout.BeginHorizontal();
        Label("Entity", 120);
        Label("State", 100);
        Label("Goal", 92);
        Label("Stimulus", 130);
        Label("Target", 60);
        Label("Weapon", 72);
        Label("Profile", 120);
        Label("HP", 48);
        Label("Fear", 50);
        Label("Susp", 50);
        Label("Agg", 50);
        Label("Mor", 50);
        Label("Dist", 54);
        Label("Vis", 40);
        Label("Danger", 56);
        Label("Food", 48);
        Label("Next", 100);
        Label("LastEvent", 150);
        GUILayout.EndHorizontal();
    }

    private static void DrawRow(OUTL_AIStateDebugRow row)
    {
        Color old = GUI.color;
        GUI.color = row.DebugColor;
        GUILayout.BeginHorizontal();
        Label(row.Entity, 120);
        Label(row.State.ToString(), 100);
        Label(row.Goal, 92);
        Label(row.Stimulus, 130);
        Label(row.Target, 60);
        Label(row.Weapon, 72);
        Label(row.AttackProfile, 120);
        Label(row.Health.ToString("0"), 48);
        Label(row.Fear.ToString("0.00"), 50);
        Label(row.Suspicion.ToString("0.00"), 50);
        Label(row.Aggression.ToString("0.00"), 50);
        Label(row.Morale.ToString("0.00"), 50);
        Label(row.Distance.ToString("0.0"), 54);
        Label(row.Visibility ? "yes" : "no", 40);
        Label(row.Danger.ToString("0.00"), 56);
        Label(row.Food.ToString("0.00"), 48);
        Label(row.NextAction, 100);
        Label(row.LastEvent, 150);
        GUILayout.EndHorizontal();
        GUI.color = old;
    }

    private static void Label(string text, int width)
    {
        GUILayout.Label(text, GUILayout.Width(width));
    }
}
