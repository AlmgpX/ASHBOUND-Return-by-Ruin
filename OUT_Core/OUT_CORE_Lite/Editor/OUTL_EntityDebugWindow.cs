#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class OUTL_EntityDebugWindow : EditorWindow
{
    private readonly List<OUTL_EntityDef> entityDefs = new List<OUTL_EntityDef>(256);
    private readonly List<OUTL_EntityAdapter> sceneAdapters = new List<OUTL_EntityAdapter>(512);
    private readonly Dictionary<OUTL_EntityDef, int> runtimeCountByDef = new Dictionary<OUTL_EntityDef, int>();
    private readonly Dictionary<string, int> runtimeCountByClass = new Dictionary<string, int>();
    private readonly HashSet<OUTL_EntityDef> usedDefs = new HashSet<OUTL_EntityDef>();

    private Vector2 scroll;
    private string filter = string.Empty;
    private bool showDefs = true;
    private bool showRuntime = true;
    private bool showWarnings = true;
    private bool showStats = false;
    private bool editDefs = true;
    private bool editAdapters = true;
    private bool includeInactive = true;
    private bool autoRefreshInPlayMode = true;
    private double nextAutoRefresh;

    // [MenuItem("OUT CORE Lite/Debug/Entity Debug Window")]
    public static void Open()
    {
        OUTL_EntityDebugWindow window = GetWindow<OUTL_EntityDebugWindow>();
        window.titleContent = new GUIContent("OUTL Entities");
        window.Refresh();
        window.Show();
    }

    private void OnEnable()
    {
        Refresh();
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        Refresh();
        Repaint();
    }

    private void OnEditorUpdate()
    {
        if (!autoRefreshInPlayMode || !EditorApplication.isPlaying) return;
        if (EditorApplication.timeSinceStartup < nextAutoRefresh) return;
        nextAutoRefresh = EditorApplication.timeSinceStartup + 0.75f;
        RefreshSceneOnly();
        Repaint();
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawSummary();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        if (showWarnings) DrawWarnings();
        if (showDefs) DrawEntityDefs();
        if (showRuntime) DrawRuntimeEntities();
        EditorGUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70))) Refresh();
        if (GUILayout.Button("Select World", EditorStyles.toolbarButton, GUILayout.Width(90))) SelectWorld();
        GUILayout.Space(8);
        GUILayout.Label("Filter", GUILayout.Width(36));
        filter = GUILayout.TextField(filter, EditorStyles.toolbarTextField, GUILayout.MinWidth(160));
        GUILayout.FlexibleSpace();
        includeInactive = GUILayout.Toggle(includeInactive, "Inactive", EditorStyles.toolbarButton, GUILayout.Width(70));
        autoRefreshInPlayMode = GUILayout.Toggle(autoRefreshInPlayMode, "Auto Play", EditorStyles.toolbarButton, GUILayout.Width(76));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        showDefs = GUILayout.Toggle(showDefs, "EntityDef Assets", EditorStyles.toolbarButton, GUILayout.Width(120));
        showRuntime = GUILayout.Toggle(showRuntime, "Runtime/Scene", EditorStyles.toolbarButton, GUILayout.Width(110));
        showWarnings = GUILayout.Toggle(showWarnings, "Warnings", EditorStyles.toolbarButton, GUILayout.Width(86));
        showStats = GUILayout.Toggle(showStats, "Stats", EditorStyles.toolbarButton, GUILayout.Width(60));
        editDefs = GUILayout.Toggle(editDefs, "Edit Defs", EditorStyles.toolbarButton, GUILayout.Width(72));
        editAdapters = GUILayout.Toggle(editAdapters, "Edit Scene", EditorStyles.toolbarButton, GUILayout.Width(78));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSummary()
    {
        EditorGUILayout.HelpBox(
            "EntityDef assets: " + entityDefs.Count +
            " | Scene adapters: " + sceneAdapters.Count +
            " | Runtime classes: " + runtimeCountByClass.Count +
            " | OUTL_World: " + (OUTL_World.Instance != null ? "LIVE" : "not running"),
            MessageType.Info);
    }

    private void DrawWarnings()
    {
        int noDef = 0;
        int noRuntime = 0;
        int noPrefab = 0;
        int unused = 0;
        for (int i = 0; i < sceneAdapters.Count; i++)
        {
            OUTL_EntityAdapter a = sceneAdapters[i];
            if (a == null) continue;
            if (a.Def == null) noDef++;
            if (EditorApplication.isPlaying && a.Runtime == null) noRuntime++;
        }
        for (int i = 0; i < entityDefs.Count; i++)
        {
            OUTL_EntityDef d = entityDefs[i];
            if (d == null) continue;
            if (d.Prefab == null) noPrefab++;
            if (!usedDefs.Contains(d)) unused++;
        }

        if (noDef == 0 && noRuntime == 0 && noPrefab == 0)
        {
            EditorGUILayout.HelpBox("Warnings: clean enough. Suspicious, frankly.", MessageType.None);
            return;
        }

        if (noDef > 0) EditorGUILayout.HelpBox("Scene adapters without EntityDef: " + noDef, MessageType.Warning);
        if (noRuntime > 0) EditorGUILayout.HelpBox("Playing adapters without Runtime registration: " + noRuntime, MessageType.Warning);
        if (noPrefab > 0) EditorGUILayout.HelpBox("EntityDef assets without Prefab: " + noPrefab + " (fine for abstract/runtime-only defs, bad for spawnable content)", MessageType.None);
        if (unused > 0) EditorGUILayout.HelpBox("EntityDef assets not used by current scene/runtime: " + unused, MessageType.None);
    }

    private void DrawEntityDefs()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("EntityDef Assets", EditorStyles.boldLabel);

        for (int i = 0; i < entityDefs.Count; i++)
        {
            OUTL_EntityDef def = entityDefs[i];
            if (def == null || !MatchesFilter(def)) continue;

            int runtimeCount;
            runtimeCountByDef.TryGetValue(def, out runtimeCount);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(def.ClassName, EditorStyles.boldLabel, GUILayout.Width(180));
            EditorGUILayout.LabelField(def.DisplayName, GUILayout.MinWidth(120));
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("runtime: " + runtimeCount, GUILayout.Width(80));
            if (GUILayout.Button("Select", GUILayout.Width(58))) Selection.activeObject = def;
            if (GUILayout.Button("Ping", GUILayout.Width(46))) EditorGUIUtility.PingObject(def);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Asset", AssetDatabase.GetAssetPath(def));
            if (editDefs) DrawEditableEntityDef(def);
            else
            {
                EditorGUILayout.LabelField("Tags", Join(def.Tags));
                EditorGUILayout.ObjectField("Prefab", def.Prefab, typeof(GameObject), false);
                DrawObjectArray("Actions", def.Actions);
                DrawObjectArray("Modules", def.Modules);
                if (showStats) DrawStats(def.BaseStats);
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawRuntimeEntities()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Runtime / Scene Entity Classes", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");
        foreach (KeyValuePair<string, int> kv in runtimeCountByClass)
            if (MatchesFilter(kv.Key)) EditorGUILayout.LabelField(kv.Key, kv.Value.ToString());
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Scene OUTL_EntityAdapter objects", EditorStyles.boldLabel);

        for (int i = 0; i < sceneAdapters.Count; i++)
        {
            OUTL_EntityAdapter adapter = sceneAdapters[i];
            if (adapter == null || !MatchesFilter(adapter)) continue;

            OUTL_EntityDef def = adapter.Def;
            string className = def != null ? def.ClassName : "<NO DEF>";
            string runtimeId = adapter.Runtime != null ? adapter.Id.Value.ToString() : "no runtime";
            string tier = adapter.Runtime != null ? adapter.Runtime.Tier.ToString() : "-";
            string faction = adapter.Runtime != null && adapter.Runtime.Faction != null ? adapter.Runtime.Faction.FactionId : "-";

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(runtimeId, GUILayout.Width(80));
            EditorGUILayout.LabelField(className, EditorStyles.boldLabel, GUILayout.Width(170));
            EditorGUILayout.LabelField(adapter.name, GUILayout.MinWidth(140));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Select", GUILayout.Width(58))) Selection.activeGameObject = adapter.gameObject;
            if (GUILayout.Button("Ping", GUILayout.Width(46))) EditorGUIUtility.PingObject(adapter.gameObject);
            EditorGUILayout.EndHorizontal();

            if (editAdapters) DrawEditableAdapter(adapter);
            else
            {
                EditorGUILayout.LabelField("Tier", tier);
                EditorGUILayout.LabelField("Faction", faction);
                EditorGUILayout.ObjectField("Def", def, typeof(OUTL_EntityDef), false);
            }
            if (adapter.Runtime != null)
            {
                EditorGUILayout.LabelField("Tags", Join(adapter.Runtime.Tags));
                if (showStats)
                {
                    EditorGUILayout.LabelField("Health", adapter.Runtime.Stats.Get(OUTL_StatId.Health, 0f).ToString("0.##"));
                    EditorGUILayout.LabelField("Damage", adapter.Runtime.Stats.Get(OUTL_StatId.Damage, 0f).ToString("0.##"));
                    EditorGUILayout.LabelField("Speed", adapter.Runtime.Stats.Get(OUTL_StatId.Speed, 0f).ToString("0.##"));
                }
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawEditableEntityDef(OUTL_EntityDef def)
    {
        if (def == null) return;
        SerializedObject so = new SerializedObject(def);
        so.Update();
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(so.FindProperty("ClassName"));
        EditorGUILayout.PropertyField(so.FindProperty("DisplayName"));
        EditorGUILayout.PropertyField(so.FindProperty("Tags"), true);
        EditorGUILayout.PropertyField(so.FindProperty("BaseStats"), true);
        EditorGUILayout.PropertyField(so.FindProperty("Actions"), true);
        EditorGUILayout.PropertyField(so.FindProperty("Modules"), true);
        EditorGUILayout.PropertyField(so.FindProperty("Prefab"));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(def, "Edit OUTL EntityDef");
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(def);
            RefreshSceneOnly();
        }
        else so.ApplyModifiedProperties();
    }

    private void DrawEditableAdapter(OUTL_EntityAdapter adapter)
    {
        if (adapter == null) return;
        SerializedObject so = new SerializedObject(adapter);
        so.Update();
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(so.FindProperty("Def"));
        EditorGUILayout.PropertyField(so.FindProperty("Faction"));
        EditorGUILayout.PropertyField(so.FindProperty("ClassNameOverride"));
        EditorGUILayout.PropertyField(so.FindProperty("TargetName"));
        EditorGUILayout.PropertyField(so.FindProperty("Target"));
        EditorGUILayout.PropertyField(so.FindProperty("KillTarget"));
        EditorGUILayout.PropertyField(so.FindProperty("StableId"));
        EditorGUILayout.PropertyField(so.FindProperty("SavePersistent"));
        EditorGUILayout.PropertyField(so.FindProperty("RestoreSpawnIfMissing"));
        EditorGUILayout.PropertyField(so.FindProperty("Tier"));
        EditorGUILayout.PropertyField(so.FindProperty("TickLane"));
        EditorGUILayout.PropertyField(so.FindProperty("TickInterval"));
        EditorGUILayout.PropertyField(so.FindProperty("RegisterRandomTick"));
        EditorGUILayout.PropertyField(so.FindProperty("RandomTickInterval"));
        EditorGUILayout.PropertyField(so.FindProperty("RegisterInSectors"));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(adapter, "Edit OUTL EntityAdapter");
            so.ApplyModifiedProperties();
            adapter.MarkAddressDirty();
            if (EditorApplication.isPlaying && OUTL_World.Instance != null)
                adapter.RebindRuntime(OUTL_World.Instance);
            EditorUtility.SetDirty(adapter);
            RefreshSceneOnly();
        }
        else so.ApplyModifiedProperties();
    }

    private void DrawObjectArray(string label, Object[] objects)
    {
        int count = objects != null ? objects.Length : 0;
        EditorGUILayout.LabelField(label, count.ToString());
        if (objects == null) return;
        EditorGUI.indentLevel++;
        for (int i = 0; i < objects.Length; i++)
            EditorGUILayout.ObjectField("[" + i + "]", objects[i], typeof(Object), false);
        EditorGUI.indentLevel--;
    }

    private void DrawStats(OUTL_StatEntry[] stats)
    {
        EditorGUILayout.LabelField("BaseStats", stats != null ? stats.Length.ToString() : "0");
        if (stats == null) return;
        EditorGUI.indentLevel++;
        for (int i = 0; i < stats.Length; i++)
            EditorGUILayout.LabelField(stats[i].Key, stats[i].Value.ToString("0.###"));
        EditorGUI.indentLevel--;
    }

    private void Refresh()
    {
        RefreshDefs();
        RefreshSceneOnly();
    }

    private void RefreshDefs()
    {
        entityDefs.Clear();
        string[] guids = AssetDatabase.FindAssets("t:OUTL_EntityDef");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            OUTL_EntityDef def = AssetDatabase.LoadAssetAtPath<OUTL_EntityDef>(path);
            if (def != null) entityDefs.Add(def);
        }
        entityDefs.Sort((a, b) => string.Compare(a.ClassName, b.ClassName, System.StringComparison.OrdinalIgnoreCase));
    }

    private void RefreshSceneOnly()
    {
        sceneAdapters.Clear();
        runtimeCountByDef.Clear();
        runtimeCountByClass.Clear();
        usedDefs.Clear();

        OUTL_EntityAdapter[] adapters = Resources.FindObjectsOfTypeAll<OUTL_EntityAdapter>();
        for (int i = 0; i < adapters.Length; i++)
        {
            OUTL_EntityAdapter a = adapters[i];
            if (a == null) continue;
            if (EditorUtility.IsPersistent(a)) continue;
            if (!includeInactive && !a.gameObject.activeInHierarchy) continue;
            sceneAdapters.Add(a);

            OUTL_EntityDef def = a.Def;
            if (def != null)
            {
                usedDefs.Add(def);
                AddCount(runtimeCountByDef, def);
                AddCount(runtimeCountByClass, string.IsNullOrEmpty(def.ClassName) ? "<empty>" : def.ClassName);
            }
            else
            {
                AddCount(runtimeCountByClass, "<NO DEF>");
            }
        }

        sceneAdapters.Sort((a, b) => string.Compare(GetAdapterSortName(a), GetAdapterSortName(b), System.StringComparison.OrdinalIgnoreCase));
    }

    private static string GetAdapterSortName(OUTL_EntityAdapter adapter)
    {
        if (adapter == null) return string.Empty;
        string c = adapter.Def != null ? adapter.Def.ClassName : "zz_no_def";
        return c + "/" + adapter.name;
    }

    private static void AddCount<TKey>(Dictionary<TKey, int> dict, TKey key)
    {
        int v;
        dict.TryGetValue(key, out v);
        dict[key] = v + 1;
    }

    private bool MatchesFilter(OUTL_EntityDef def)
    {
        if (string.IsNullOrEmpty(filter)) return true;
        string f = filter.ToLowerInvariant();
        if (def.name.ToLowerInvariant().Contains(f)) return true;
        if (!string.IsNullOrEmpty(def.ClassName) && def.ClassName.ToLowerInvariant().Contains(f)) return true;
        if (!string.IsNullOrEmpty(def.DisplayName) && def.DisplayName.ToLowerInvariant().Contains(f)) return true;
        if (def.Tags != null)
            for (int i = 0; i < def.Tags.Length; i++)
                if (!string.IsNullOrEmpty(def.Tags[i]) && def.Tags[i].ToLowerInvariant().Contains(f)) return true;
        return false;
    }

    private bool MatchesFilter(OUTL_EntityAdapter adapter)
    {
        if (string.IsNullOrEmpty(filter)) return true;
        string f = filter.ToLowerInvariant();
        if (adapter.name.ToLowerInvariant().Contains(f)) return true;
        if (adapter.Def != null && MatchesFilter(adapter.Def)) return true;
        if (adapter.Runtime != null && adapter.Runtime.Tags != null)
            for (int i = 0; i < adapter.Runtime.Tags.Length; i++)
                if (!string.IsNullOrEmpty(adapter.Runtime.Tags[i]) && adapter.Runtime.Tags[i].ToLowerInvariant().Contains(f)) return true;
        return false;
    }

    private bool MatchesFilter(string value)
    {
        if (string.IsNullOrEmpty(filter)) return true;
        return !string.IsNullOrEmpty(value) && value.ToLowerInvariant().Contains(filter.ToLowerInvariant());
    }

    private static string Join(string[] values)
    {
        if (values == null || values.Length == 0) return "-";
        return string.Join(", ", values);
    }

    private void SelectWorld()
    {
        OUTL_World world = FindObjectOfType<OUTL_World>();
        if (world != null) Selection.activeGameObject = world.gameObject;
    }
}
#endif
