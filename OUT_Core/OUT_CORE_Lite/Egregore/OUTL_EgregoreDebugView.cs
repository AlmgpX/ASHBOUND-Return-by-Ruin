using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_EgregoreDebugView : MonoBehaviour
{
    public bool Show;
    public KeyCode ToggleKey = KeyCode.F6;
    public float RefreshInterval = 0.5f;
    public Rect WindowRect = new Rect(20f, 760f, 980f, 220f);
    public int MaxRows = 16;
    public OUTL_EgregoreComponent[] Sources;

    private readonly List<OUTL_EgregoreComponent> components = new List<OUTL_EgregoreComponent>(16);
    private float nextRefresh;

    private void Update()
    {
        if (Input.GetKeyDown(ToggleKey)) Show = !Show;
    }

    private void OnGUI()
    {
        if (!Show) return;
        if (Time.unscaledTime >= nextRefresh) Refresh();
        WindowRect = GUI.Window(GetInstanceID(), WindowRect, DrawWindow, "OUTL Egregore Debug");
    }

    private void Refresh()
    {
        nextRefresh = Time.unscaledTime + Mathf.Max(0.05f, RefreshInterval);
        components.Clear();
        if (Sources == null) return;
        int max = Mathf.Min(Mathf.Max(1, MaxRows), Sources.Length);
        for (int i = 0; i < Sources.Length && components.Count < max; i++)
            if (Sources[i] != null)
                components.Add(Sources[i]);
    }

    private void DrawWindow(int id)
    {
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        Label("Id", 150);
        Label("Scope", 70);
        Label("Mood", 82);
        Label("Phase", 132);
        Label("Dom", 98);
        Label("Shadow", 116);
        Label("Viol", 48);
        Label("Fear", 48);
        Label("Alert", 48);
        Label("Host", 48);
        Label("Pros", 48);
        Label("Res", 48);
        Label("Corr", 48);
        Label("Ent", 48);
        Label("Sectors", 58);
        Label("Entities", 58);
        Label("Stim", 44);
        Label("Mem", 44);
        Label("Last", 150);
        GUILayout.EndHorizontal();

        for (int i = 0; i < components.Count; i++)
        {
            OUTL_EgregoreRuntime r = components[i] != null ? components[i].Runtime : null;
            if (r == null) continue;
            GUILayout.BeginHorizontal();
            Label(r.EgregoreId, 150);
            Label(r.Scope.ToString(), 70);
            Label(r.DominantMood.ToString(), 82);
            Label(r.CurrentCyclePhase.ToString(), 132);
            Label(r.DominantArchetype.ToString(), 98);
            Label(r.ShadowArchetype.ToString(), 116);
            Label(r.Violence.ToString("0.00"), 48);
            Label(r.Fear.ToString("0.00"), 48);
            Label(r.Alertness.ToString("0.00"), 48);
            Label(r.Hostility.ToString("0.00"), 48);
            Label(r.Prosperity.ToString("0.00"), 48);
            Label(r.ResourcePressure.ToString("0.00"), 48);
            Label(r.Corruption.ToString("0.00"), 48);
            Label(r.Entropy.ToString("0.00"), 48);
            Label(r.OwnedSectorCount.ToString(), 58);
            Label(r.LastSectorEntityCount.ToString(), 58);
            Label(r.LastSectorStimulusCount.ToString(), 44);
            Label(r.MemoryTraceCount.ToString(), 44);
            Label(string.IsNullOrEmpty(r.LastEffect) ? "-" : r.LastEffect, 150);
            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();
        GUI.DragWindow();
    }

    private static void Label(string text, int width)
    {
        GUILayout.Label(text, GUILayout.Width(width));
    }
}
