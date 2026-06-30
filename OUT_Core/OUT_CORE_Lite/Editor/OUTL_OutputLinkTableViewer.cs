#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public sealed class OUTL_OutputLinkTableViewer : EditorWindow
{
    private sealed class Row
    {
        public int Index;
        public OUTL_EntityAdapter Source;
        public Component Component;
        public string Field;
        public int OutputIndex;
        public OUTL_OutputLink Link;
        public int TargetCount;
        public string Status;
    }

    private readonly List<Row> rows = new List<Row>(512);
    private readonly List<Row> shown = new List<Row>(512);
    private readonly Dictionary<string, int> targetCounts = new Dictionary<string, int>(StringComparer.Ordinal);
    private readonly List<FieldInfo> fields = new List<FieldInfo>(8);
    private Vector2 scroll;
    private string search = string.Empty;
    private bool showOk = true;
    private bool showBroken = true;
    private bool showDisabled = true;
    private bool autoRefresh = true;
    private int selected = -1;

    // [MenuItem("OUT CORE Lite/Debug/Output Links Table")]
    public static void Open()
    {
        OUTL_OutputLinkTableViewer w = GetWindow<OUTL_OutputLinkTableViewer>("OUTL Output Links");
        w.minSize = new Vector2(1000f, 520f);
        w.RefreshTable();
    }

    // [MenuItem("OUT CORE Lite/Debug/Scene Graph Viewer + Output Table")]
    public static void OpenBoth()
    {
        OUTL_SceneGraphViewer.Open();
        Open();
    }

    private void OnEnable() { RefreshTable(); }
    private void OnHierarchyChange() { if (autoRefresh) RefreshTable(); else Repaint(); }
    private void OnInspectorUpdate() { if (autoRefresh) Repaint(); }

    private void OnGUI()
    {
        DrawToolbar();
        EditorGUILayout.HelpBox("Excel-style table of OUTL_OutputLink contracts in the open scene. Same data as Scene Graph Viewer: Source -> EventName -> TargetName -> CommandSystem. Double-click row to select source. Broken = empty or unresolved TargetName.", MessageType.Info);
        ApplyFilter();
        DrawStats();
        DrawHeader();
        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 0; i < shown.Count; i++) DrawRow(shown[i], i);
        EditorGUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70))) RefreshTable();
        if (GUILayout.Button("Graph", EditorStyles.toolbarButton, GUILayout.Width(58))) OUTL_SceneGraphViewer.Open();
        if (GUILayout.Button("Source", EditorStyles.toolbarButton, GUILayout.Width(62))) SelectSource(FindSelected());
        if (GUILayout.Button("Target", EditorStyles.toolbarButton, GUILayout.Width(62))) SelectTarget(FindSelected());
        showOk = GUILayout.Toggle(showOk, "OK", EditorStyles.toolbarButton, GUILayout.Width(38));
        showBroken = GUILayout.Toggle(showBroken, "Broken", EditorStyles.toolbarButton, GUILayout.Width(64));
        showDisabled = GUILayout.Toggle(showDisabled, "Disabled", EditorStyles.toolbarButton, GUILayout.Width(76));
        autoRefresh = GUILayout.Toggle(autoRefresh, "Auto", EditorStyles.toolbarButton, GUILayout.Width(52));
        GUILayout.Label("Search", GUILayout.Width(46));
        search = GUILayout.TextField(search, EditorStyles.toolbarTextField, GUILayout.MinWidth(180));
        GUILayout.FlexibleSpace();
        GUILayout.Label("Rows " + shown.Count + "/" + rows.Count, EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawStats()
    {
        int ok = 0, broken = 0, disabled = 0;
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].Status == "DISABLED") disabled++;
            else if (rows[i].Status == "OK") ok++;
            else broken++;
        }
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Total", rows.Count.ToString(), GUILayout.Width(120));
        EditorGUILayout.LabelField("OK", ok.ToString(), GUILayout.Width(100));
        EditorGUILayout.LabelField("Broken", broken.ToString(), GUILayout.Width(120));
        EditorGUILayout.LabelField("Disabled", disabled.ToString(), GUILayout.Width(130));
        EditorGUILayout.LabelField("TargetNames", targetCounts.Count.ToString(), GUILayout.Width(160));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        H("#", 34); H("Status", 80); H("Source", 170); H("Component", 150); H("Field", 92); H("Event", 110); H("TargetName", 160); H("Command", 108); H("Delay", 55); H("Key", 90); H("Float", 50); H("Int", 45); H("Once", 45); H("Dis", 42); H("Targets", 58);
        EditorGUILayout.EndHorizontal();
    }

    private static void H(string t, float w) { GUILayout.Label(t, EditorStyles.toolbarButton, GUILayout.Width(w)); }
    private static void L(string t, float w) { GUILayout.Label(t ?? string.Empty, EditorStyles.miniLabel, GUILayout.Width(w)); }

    private void DrawRow(Row r, int visibleIndex)
    {
        if (r == null || r.Link == null) return;
        Rect rect = EditorGUILayout.BeginHorizontal(visibleIndex % 2 == 0 ? "CN EntryBackEven" : "CN EntryBackOdd");
        L(r.Index.ToString(), 34);
        Color old = GUI.color;
        GUI.color = r.Status == "OK" ? new Color(0.45f, 1f, 0.55f) : (r.Status == "DISABLED" ? Color.gray : new Color(1f, 0.35f, 0.25f));
        GUILayout.Label(r.Status, EditorStyles.boldLabel, GUILayout.Width(80));
        GUI.color = old;
        if (GUILayout.Button(r.Source != null ? r.Source.name : "<none>", EditorStyles.miniButton, GUILayout.Width(170))) SelectSource(r);
        if (GUILayout.Button(r.Component != null ? r.Component.GetType().Name : "<null>", EditorStyles.miniButton, GUILayout.Width(150))) SelectSource(r);
        L(r.Field + "[" + r.OutputIndex + "]", 92);
        EditText(r, 0, r.Link.EventName, 110);
        EditText(r, 1, r.Link.TargetName, 160);
        EditCommand(r, 108);
        EditFloat(r, 0, r.Link.Delay, 55);
        EditText(r, 2, r.Link.Key, 90);
        EditFloat(r, 1, r.Link.FloatValue, 50);
        EditInt(r, r.Link.IntValue, 45);
        EditBool(r, 0, r.Link.Once, 45);
        EditBool(r, 1, r.Link.Disabled, 42);
        L(r.TargetCount.ToString(), 58);
        EditorGUILayout.EndHorizontal();
        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            selected = r.Index;
            if (Event.current.clickCount > 1) SelectSource(r);
            Event.current.Use();
        }
    }

    private void EditText(Row r, int kind, string value, float width)
    {
        EditorGUI.BeginChangeCheck();
        string next = GUILayout.TextField(value ?? string.Empty, GUILayout.Width(width));
        if (!EditorGUI.EndChangeCheck()) return;
        Undo.RecordObject(r.Component, "Edit OUTL OutputLink");
        if (kind == 0) r.Link.EventName = next;
        else if (kind == 1) r.Link.TargetName = next;
        else r.Link.Key = next;
        Mark(r.Component);
    }

    private void EditCommand(Row r, float width)
    {
        EditorGUI.BeginChangeCheck();
        OUTL_CommandType next = (OUTL_CommandType)EditorGUILayout.EnumPopup(r.Link.Command, GUILayout.Width(width));
        if (!EditorGUI.EndChangeCheck()) return;
        Undo.RecordObject(r.Component, "Edit OUTL OutputLink Command");
        r.Link.Command = next;
        Mark(r.Component);
    }

    private void EditFloat(Row r, int kind, float value, float width)
    {
        EditorGUI.BeginChangeCheck();
        float next = EditorGUILayout.FloatField(value, GUILayout.Width(width));
        if (!EditorGUI.EndChangeCheck()) return;
        Undo.RecordObject(r.Component, "Edit OUTL OutputLink Float");
        if (kind == 0) r.Link.Delay = Mathf.Max(0f, next);
        else r.Link.FloatValue = next;
        Mark(r.Component);
    }

    private void EditInt(Row r, int value, float width)
    {
        EditorGUI.BeginChangeCheck();
        int next = EditorGUILayout.IntField(value, GUILayout.Width(width));
        if (!EditorGUI.EndChangeCheck()) return;
        Undo.RecordObject(r.Component, "Edit OUTL OutputLink Int");
        r.Link.IntValue = next;
        Mark(r.Component);
    }

    private void EditBool(Row r, int kind, bool value, float width)
    {
        EditorGUI.BeginChangeCheck();
        bool next = GUILayout.Toggle(value, GUIContent.none, GUILayout.Width(width));
        if (!EditorGUI.EndChangeCheck()) return;
        Undo.RecordObject(r.Component, "Edit OUTL OutputLink Bool");
        if (kind == 0) r.Link.Once = next;
        else r.Link.Disabled = next;
        Mark(r.Component);
    }

    private void RefreshTable()
    {
        rows.Clear();
        targetCounts.Clear();
        OUTL_EntityAdapter[] entities = FindObjectsOfType<OUTL_EntityAdapter>(true);
        for (int i = 0; i < entities.Length; i++) if (entities[i] != null && !string.IsNullOrEmpty(entities[i].TargetName)) targetCounts[entities[i].TargetName] = targetCounts.ContainsKey(entities[i].TargetName) ? targetCounts[entities[i].TargetName] + 1 : 1;
        Array.Sort(entities, CompareAdapters);
        int index = 0;
        for (int i = 0; i < entities.Length; i++)
        {
            OUTL_EntityAdapter owner = entities[i];
            if (owner == null) continue;
            MonoBehaviour[] comps = owner.GetComponentsInChildren<MonoBehaviour>(true);
            for (int c = 0; c < comps.Length; c++)
            {
                MonoBehaviour comp = comps[c];
                if (comp == null) continue;
                fields.Clear();
                CollectOutputFields(comp.GetType(), fields);
                for (int f = 0; f < fields.Count; f++)
                {
                    OUTL_OutputLink[] links = fields[f].GetValue(comp) as OUTL_OutputLink[];
                    if (links == null) continue;
                    for (int o = 0; o < links.Length; o++) if (links[o] != null) rows.Add(BuildRow(index++, owner, comp, fields[f].Name, o, links[o]));
                }
            }
        }
        ApplyFilter();
        Repaint();
    }

    private Row BuildRow(int index, OUTL_EntityAdapter source, Component comp, string field, int outIndex, OUTL_OutputLink link)
    {
        string tn = link.TargetName ?? string.Empty;
        int count = 0;
        if (!string.IsNullOrEmpty(tn)) targetCounts.TryGetValue(tn, out count);
        string status = link.Disabled || link.Command == OUTL_CommandType.None ? "DISABLED" : (string.IsNullOrEmpty(tn) ? "EMPTY" : (count <= 0 ? "BROKEN" : "OK"));
        return new Row { Index = index, Source = source, Component = comp, Field = field, OutputIndex = outIndex, Link = link, TargetCount = count, Status = status };
    }

    private void ApplyFilter()
    {
        shown.Clear();
        string s = (search ?? string.Empty).Trim().ToLowerInvariant();
        for (int i = 0; i < rows.Count; i++)
        {
            Row r = rows[i];
            if (r.Status == "OK" && !showOk) continue;
            if ((r.Status == "BROKEN" || r.Status == "EMPTY") && !showBroken) continue;
            if (r.Status == "DISABLED" && !showDisabled) continue;
            if (!string.IsNullOrEmpty(s) && !Matches(r, s)) continue;
            shown.Add(r);
        }
    }

    private bool Matches(Row r, string s)
    {
        return Has(r.Status, s) || Has(r.Source != null ? r.Source.name : string.Empty, s) || Has(r.Source != null ? r.Source.TargetName : string.Empty, s) || Has(r.Component != null ? r.Component.GetType().Name : string.Empty, s) || Has(r.Field, s) || Has(r.Link.EventName, s) || Has(r.Link.TargetName, s) || Has(r.Link.Command.ToString(), s) || Has(r.Link.Key, s);
    }

    private static bool Has(string v, string s) { return !string.IsNullOrEmpty(v) && v.ToLowerInvariant().Contains(s); }

    private void SelectSource(Row r)
    {
        if (r == null) return;
        UnityEngine.Object obj = r.Component != null ? (UnityEngine.Object)r.Component : (r.Source != null ? r.Source.gameObject : null);
        if (obj == null) return;
        Selection.activeObject = obj;
        EditorGUIUtility.PingObject(obj);
    }

    private void SelectTarget(Row r)
    {
        if (r == null || r.Link == null || string.IsNullOrEmpty(r.Link.TargetName)) return;
        OUTL_EntityAdapter[] entities = FindObjectsOfType<OUTL_EntityAdapter>(true);
        for (int i = 0; i < entities.Length; i++) if (entities[i] != null && entities[i].TargetName == r.Link.TargetName) { Selection.activeObject = entities[i].gameObject; EditorGUIUtility.PingObject(entities[i].gameObject); return; }
    }

    private Row FindSelected() { for (int i = 0; i < rows.Count; i++) if (rows[i].Index == selected) return rows[i]; return shown.Count > 0 ? shown[0] : null; }
    private void Mark(UnityEngine.Object obj) { if (obj != null) EditorUtility.SetDirty(obj); RefreshTable(); }

    private static void CollectOutputFields(Type type, List<FieldInfo> result)
    {
        while (type != null && type != typeof(MonoBehaviour))
        {
            FieldInfo[] fs = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            for (int i = 0; i < fs.Length; i++) if (fs[i].FieldType == typeof(OUTL_OutputLink[])) result.Add(fs[i]);
            type = type.BaseType;
        }
    }

    private static int CompareAdapters(OUTL_EntityAdapter a, OUTL_EntityAdapter b) { return string.CompareOrdinal(a != null ? a.name : string.Empty, b != null ? b.name : string.Empty); }
}
#endif
