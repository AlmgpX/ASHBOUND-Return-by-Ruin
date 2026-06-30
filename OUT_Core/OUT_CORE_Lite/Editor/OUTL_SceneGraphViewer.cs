#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public sealed class OUTL_SceneGraphViewer : EditorWindow
{
    private enum NodeKind { World, Entity, MissingTarget }
    private enum EdgeKind { WorldRegistration, OutputCommand, StateQuery, MissingOutput, Hierarchy }

    private sealed class Node
    {
        public string Id;
        public string Title;
        public string Subtitle;
        public string TargetName;
        public string ClassName;
        public string RuntimeLine;
        public Rect Rect;
        public NodeKind Kind;
        public GameObject GameObject;
        public UnityEngine.Object Context;
        public OUTL_EntityAdapter Adapter;
    }

    private sealed class Edge
    {
        public string FromId;
        public string ToId;
        public string Label;
        public EdgeKind Kind;
        public Component SourceComponent;
        public OUTL_OutputLink Output;
        public string TargetName;
        public bool Disabled;
    }

    private const float ToolbarHeight = 34f;
    private const float InspectorWidth = 380f;
    private const float NodeWidth = 260f;
    private const float NodeHeight = 110f;

    private readonly List<Node> nodes = new List<Node>(256);
    private readonly List<Edge> edges = new List<Edge>(512);
    private readonly Dictionary<string, Node> nodeById = new Dictionary<string, Node>(256);
    private readonly Dictionary<string, List<Node>> targetNameToNodes = new Dictionary<string, List<Node>>(StringComparer.Ordinal);
    private readonly List<FieldInfo> outputFieldBuffer = new List<FieldInfo>(8);
    private readonly List<OUTL_EntityRuntime> runtimeBuffer = new List<OUTL_EntityRuntime>(512);

    private Vector2 pan = new Vector2(80f, 80f);
    private float zoom = 1f;
    private bool showWorldLinks = true;
    private bool showOutputLinks = true;
    private bool showStateQueryLinks = true;
    private bool showMissingLinks = true;
    private bool showHierarchyLinks = true;
    private bool showDisabledOutputs;
    private bool autoRefresh = true;
    private bool autoLayoutOnRefresh = true;
    private string search = string.Empty;
    private Node selectedNode;
    private Node draggingNode;
    private Vector2 dragOffset;
    private Vector2 inspectorScroll;
    private bool panning;
    private Vector2 lastMouse;

    // [MenuItem("OUT CORE Lite/Debug/Scene Graph Viewer")]
    public static void Open()
    {
        OUTL_SceneGraphViewer window = GetWindow<OUTL_SceneGraphViewer>("OUTL Scene Graph");
        window.minSize = new Vector2(900f, 520f);
        window.RefreshGraph();
    }

    private void OnEnable()
    {
        Selection.selectionChanged += OnSelectionChanged;
        RefreshGraph();
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= OnSelectionChanged;
    }

    private void OnHierarchyChange()
    {
        if (autoRefresh) RefreshGraph();
        else Repaint();
    }

    private void OnInspectorUpdate()
    {
        if (autoRefresh) Repaint();
    }

    private void OnSelectionChanged()
    {
        GameObject go = Selection.activeGameObject;
        if (go != null)
        {
            OUTL_EntityAdapter adapter = go.GetComponentInParent<OUTL_EntityAdapter>();
            if (adapter != null)
            {
                Node node;
                if (nodeById.TryGetValue(EntityNodeId(adapter), out node)) selectedNode = node;
            }
        }
        Repaint();
    }

    private void OnGUI()
    {
        DrawToolbar();
        Rect canvasRect = new Rect(0f, ToolbarHeight, Mathf.Max(1f, position.width - InspectorWidth), Mathf.Max(1f, position.height - ToolbarHeight));
        Rect inspectorRect = new Rect(position.width - InspectorWidth, ToolbarHeight, InspectorWidth, position.height - ToolbarHeight);
        ProcessCanvasEvents(Event.current, canvasRect);
        DrawCanvas(canvasRect);
        DrawInspector(inspectorRect);
        if (GUI.changed) Repaint();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(ToolbarHeight));
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f))) RefreshGraph();
        if (GUILayout.Button("Auto Layout", EditorStyles.toolbarButton, GUILayout.Width(86f))) { AutoLayout(); Repaint(); }
        if (GUILayout.Button("Frame All", EditorStyles.toolbarButton, GUILayout.Width(72f))) FrameAll();
        GUILayout.Space(6f);
        showWorldLinks = GUILayout.Toggle(showWorldLinks, "World", EditorStyles.toolbarButton, GUILayout.Width(56f));
        showHierarchyLinks = GUILayout.Toggle(showHierarchyLinks, "Hierarchy", EditorStyles.toolbarButton, GUILayout.Width(76f));
        showOutputLinks = GUILayout.Toggle(showOutputLinks, "Outputs", EditorStyles.toolbarButton, GUILayout.Width(68f));
        showStateQueryLinks = GUILayout.Toggle(showStateQueryLinks, "Queries", EditorStyles.toolbarButton, GUILayout.Width(66f));
        showMissingLinks = GUILayout.Toggle(showMissingLinks, "Missing", EditorStyles.toolbarButton, GUILayout.Width(68f));
        showDisabledOutputs = GUILayout.Toggle(showDisabledOutputs, "Disabled", EditorStyles.toolbarButton, GUILayout.Width(72f));
        autoRefresh = GUILayout.Toggle(autoRefresh, "Auto", EditorStyles.toolbarButton, GUILayout.Width(52f));
        autoLayoutOnRefresh = GUILayout.Toggle(autoLayoutOnRefresh, "Layout", EditorStyles.toolbarButton, GUILayout.Width(58f));
        GUILayout.Space(6f);
        GUILayout.Label("Search", GUILayout.Width(46f));
        search = GUILayout.TextField(search, EditorStyles.toolbarTextField, GUILayout.MinWidth(120f));
        GUILayout.FlexibleSpace();
        GUILayout.Label("Nodes " + nodes.Count + "  Edges " + edges.Count + "  Zoom " + zoom.ToString("0.00"), EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawCanvas(Rect canvasRect)
    {
        GUI.Box(canvasRect, GUIContent.none);
        DrawGrid(canvasRect, 20f, new Color(0.18f, 0.18f, 0.18f, 0.55f));
        DrawGrid(canvasRect, 100f, new Color(0.28f, 0.28f, 0.28f, 0.65f));
        Handles.BeginGUI();
        DrawEdges(canvasRect);
        Handles.EndGUI();
        DrawNodes(canvasRect);
        DrawLegend(canvasRect);
    }

    private void DrawGrid(Rect canvasRect, float spacing, Color color)
    {
        Handles.BeginGUI();
        Handles.color = color;
        float scaled = Mathf.Max(5f, spacing * zoom);
        Vector2 offset = new Vector2((pan.x + canvasRect.x) % scaled, (pan.y + canvasRect.y) % scaled);
        for (float x = canvasRect.x + offset.x; x < canvasRect.xMax; x += scaled)
            Handles.DrawLine(new Vector3(x, canvasRect.y), new Vector3(x, canvasRect.yMax));
        for (float y = canvasRect.y + offset.y; y < canvasRect.yMax; y += scaled)
            Handles.DrawLine(new Vector3(canvasRect.x, y), new Vector3(canvasRect.xMax, y));
        Handles.color = Color.white;
        Handles.EndGUI();
    }

    private void DrawNodes(Rect canvasRect)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            Node node = nodes[i];
            if (!IsNodeVisible(node)) continue;
            Rect screen = GraphToScreen(node.Rect, canvasRect);
            if (!screen.Overlaps(canvasRect)) continue;
            DrawNode(node, screen);
        }
    }

    private void DrawNode(Node node, Rect r)
    {
        Color oldColor = GUI.color;
        GUI.color = GetNodeColor(node);
        GUI.Box(r, GUIContent.none, EditorStyles.helpBox);
        GUI.color = oldColor;

        Rect titleRect = new Rect(r.x + 8f, r.y + 6f, r.width - 16f, 18f);
        Rect subRect = new Rect(r.x + 8f, r.y + 26f, r.width - 16f, 18f);
        Rect runtimeRect = new Rect(r.x + 8f, r.y + 44f, r.width - 16f, 18f);
        Rect targetRect = new Rect(r.x + 8f, r.y + 66f, r.width - 16f, 16f);
        Rect classRect = new Rect(r.x + 8f, r.y + 84f, r.width - 16f, 16f);

        GUI.Label(titleRect, node.Title, EditorStyles.boldLabel);
        GUI.Label(subRect, node.Subtitle, EditorStyles.miniLabel);
        GUI.Label(runtimeRect, node.RuntimeLine, EditorStyles.miniLabel);
        GUI.Label(targetRect, string.IsNullOrEmpty(node.TargetName) ? "target: <none>" : "target: " + node.TargetName, EditorStyles.miniLabel);
        GUI.Label(classRect, string.IsNullOrEmpty(node.ClassName) ? "class: <none>" : "class: " + node.ClassName, EditorStyles.miniLabel);

        if (selectedNode == node)
        {
            Color prev = Handles.color;
            Handles.color = new Color(1f, 0.72f, 0.2f, 1f);
            Handles.DrawAAPolyLine(3f,
                new Vector3(r.xMin, r.yMin), new Vector3(r.xMax, r.yMin),
                new Vector3(r.xMax, r.yMax), new Vector3(r.xMin, r.yMax),
                new Vector3(r.xMin, r.yMin));
            Handles.color = prev;
        }
    }

    private void DrawEdges(Rect canvasRect)
    {
        for (int i = 0; i < edges.Count; i++)
        {
            Edge edge = edges[i];
            if (!IsEdgeVisible(edge)) continue;
            Node from;
            Node to;
            if (!nodeById.TryGetValue(edge.FromId, out from)) continue;
            if (!nodeById.TryGetValue(edge.ToId, out to)) continue;
            if (!IsNodeVisible(from) || !IsNodeVisible(to)) continue;

            Rect fromR = GraphToScreen(from.Rect, canvasRect);
            Rect toR = GraphToScreen(to.Rect, canvasRect);
            Vector3 start = new Vector3(fromR.xMax, fromR.center.y, 0f);
            Vector3 end = new Vector3(toR.xMin, toR.center.y, 0f);
            if (toR.center.x < fromR.center.x) end = new Vector3(toR.xMax, toR.center.y, 0f);

            Vector3 startTangent = start + Vector3.right * Mathf.Max(60f, Mathf.Abs(end.x - start.x) * 0.35f);
            Vector3 endTangent = end + Vector3.left * Mathf.Max(60f, Mathf.Abs(end.x - start.x) * 0.35f);
            Color color = GetEdgeColor(edge);
            if (edge.Disabled) color.a = 0.25f;
            Handles.DrawBezier(start, end, startTangent, endTangent, color, null, edge.Kind == EdgeKind.WorldRegistration || edge.Kind == EdgeKind.Hierarchy ? 1.2f : 2.3f);
            DrawArrow(end, start, color);

            if (!string.IsNullOrEmpty(edge.Label) && edge.Kind != EdgeKind.WorldRegistration && edge.Kind != EdgeKind.Hierarchy)
            {
                Vector3 mid = (start + end) * 0.5f;
                Rect labelRect = new Rect(mid.x - 85f, mid.y - 10f, 170f, 20f);
                GUI.color = new Color(0f, 0f, 0f, 0.78f);
                GUI.Box(labelRect, GUIContent.none);
                GUI.color = Color.white;
                GUI.Label(labelRect, edge.Label, EditorStyles.centeredGreyMiniLabel);
                GUI.color = Color.white;
            }
        }
    }

    private void DrawArrow(Vector3 end, Vector3 start, Color color)
    {
        Vector3 dir = (end - start).normalized;
        if (dir.sqrMagnitude < 0.001f) dir = Vector3.right;
        Vector3 normal = new Vector3(-dir.y, dir.x, 0f);
        Vector3 a = end - dir * 12f + normal * 5f;
        Vector3 b = end - dir * 12f - normal * 5f;
        Color prev = Handles.color;
        Handles.color = color;
        Handles.DrawAAConvexPolygon(end, a, b);
        Handles.color = prev;
    }

    private void DrawLegend(Rect canvasRect)
    {
        Rect r = new Rect(canvasRect.x + 10f, canvasRect.y + 10f, 310f, 90f);
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.Box(r, GUIContent.none);
        GUI.color = Color.white;
        GUI.Label(new Rect(r.x + 8f, r.y + 6f, r.width - 16f, 18f), "OUTL Graph: World / Hierarchy / Output / Query", EditorStyles.boldLabel);
        GUI.Label(new Rect(r.x + 8f, r.y + 28f, r.width - 16f, 18f), "Blue = command output, Yellow = state query, Red = missing target", EditorStyles.miniLabel);
        GUI.Label(new Rect(r.x + 8f, r.y + 46f, r.width - 16f, 18f), "Tier rings are in Scene View on OUTL_ChunkProcessingDriver", EditorStyles.miniLabel);
        GUI.Label(new Rect(r.x + 8f, r.y + 64f, r.width - 16f, 18f), "GoldSrc Addressing: ClassName / TargetName / Target / KillTarget", EditorStyles.miniLabel);
    }

    private void DrawInspector(Rect rect)
    {
        GUILayout.BeginArea(rect, EditorStyles.helpBox);
        inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll);
        EditorGUILayout.LabelField("OUTL Scene Graph", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("GoldSrc-style Addressing means: ClassName = type of entity, TargetName = addressable runtime name, Target = default output address, KillTarget = optional target to remove/disable by logic. Native flow is Source Event -> OUTL_OutputLink -> TargetName -> CommandSystem -> Receivers.", MessageType.Info);

        DrawCreationTools();
        EditorGUILayout.Space();

        if (selectedNode == null)
        {
            EditorGUILayout.LabelField("Selection", "<none>");
            EditorGUILayout.LabelField("Tips", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("LMB node: select / drag");
            EditorGUILayout.LabelField("MMB/RMB/Alt+LMB: pan");
            EditorGUILayout.LabelField("Wheel: zoom");
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
            return;
        }

        DrawSelectedNodeInspector(selectedNode);
        EditorGUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawCreationTools()
    {
        EditorGUILayout.LabelField("Create / Wire", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Button")) CreateBasicEntity("OUTL_Button", "func_button", typeof(OUTL_Button));
        if (GUILayout.Button("Door")) CreateBasicEntity("OUTL_Door", "func_door", typeof(OUTL_Door));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Relay")) CreateBasicEntity("OUTL_LogicRelay", "logic_relay", typeof(OUTL_LogicRelay));
        if (GUILayout.Button("MultiManager")) CreateBasicEntity("OUTL_MultiManager", "logic_multimanager", typeof(OUTL_MultiManager));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("MultiSource")) CreateBasicEntity("OUTL_MultiSource", "logic_multisource", typeof(OUTL_MultiSource));
        if (GUILayout.Button("KillCounter")) CreateBasicEntity("OUTL_KillCounter", "logic_kill_counter", typeof(OUTL_KillCounter));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSelectedNodeInspector(Node node)
    {
        EditorGUILayout.LabelField("Selected", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Title", node.Title);
        EditorGUILayout.LabelField("Kind", node.Kind.ToString());
        EditorGUILayout.LabelField("TargetName", string.IsNullOrEmpty(node.TargetName) ? "<none>" : node.TargetName);
        EditorGUILayout.LabelField("ClassName", string.IsNullOrEmpty(node.ClassName) ? "<none>" : node.ClassName);
        EditorGUILayout.LabelField("Runtime", node.RuntimeLine);

        if (node.Context != null)
        {
            EditorGUILayout.ObjectField("Context", node.Context, typeof(UnityEngine.Object), true);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select")) Selection.activeObject = node.Context;
            if (GUILayout.Button("Ping")) EditorGUIUtility.PingObject(node.Context);
            EditorGUILayout.EndHorizontal();
        }

        if (node.Adapter != null) DrawEntityEditor(node.Adapter);

        EditorGUILayout.Space();
        DrawOutputEditor(node.Adapter);
        EditorGUILayout.Space();
        DrawEdgesInspector(node);
    }

    private void DrawEntityEditor(OUTL_EntityAdapter adapter)
    {
        EditorGUILayout.LabelField("Entity Address", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        string className = EditorGUILayout.TextField("ClassName Override", adapter.ClassNameOverride);
        string targetName = EditorGUILayout.TextField("TargetName", adapter.TargetName);
        string target = EditorGUILayout.TextField("Target", adapter.Target);
        string killTarget = EditorGUILayout.TextField("KillTarget", adapter.KillTarget);
        string stableId = EditorGUILayout.TextField("StableId", adapter.StableId);
        OUTL_RuntimeTier tier = (OUTL_RuntimeTier)EditorGUILayout.EnumPopup("Tier", adapter.Tier);
        OUTL_TickLane lane = (OUTL_TickLane)EditorGUILayout.EnumPopup("Tick Lane", adapter.TickLane);
        float tick = EditorGUILayout.FloatField("Tick Interval", adapter.TickInterval);
        bool random = EditorGUILayout.Toggle("Random Tick", adapter.RegisterRandomTick);
        float randomInterval = EditorGUILayout.FloatField("Random Tick Interval", adapter.RandomTickInterval);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(adapter, "Edit OUTL Entity Address");
            adapter.ClassNameOverride = className;
            adapter.TargetName = targetName;
            adapter.Target = target;
            adapter.KillTarget = killTarget;
            adapter.StableId = stableId;
            adapter.Tier = tier;
            adapter.TickLane = lane;
            adapter.TickInterval = Mathf.Max(0.01f, tick);
            adapter.RegisterRandomTick = random;
            adapter.RandomTickInterval = Mathf.Max(0.01f, randomInterval);
            adapter.MarkAddressDirty();
            EditorUtility.SetDirty(adapter);
            RefreshGraph();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Auto TargetName"))
        {
            Undo.RecordObject(adapter, "Auto OUTL TargetName");
            adapter.TargetName = MakeSafeName(adapter.gameObject.name);
            adapter.MarkAddressDirty();
            EditorUtility.SetDirty(adapter);
            RefreshGraph();
        }
        if (GUILayout.Button("Auto StableId"))
        {
            Undo.RecordObject(adapter, "Auto OUTL StableId");
            adapter.StableId = MakeSafeName(adapter.gameObject.scene.name + "_" + adapter.gameObject.name + "_" + adapter.GetInstanceID());
            adapter.MarkAddressDirty();
            EditorUtility.SetDirty(adapter);
            RefreshGraph();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawOutputEditor(OUTL_EntityAdapter adapter)
    {
        if (adapter == null) return;
        EditorGUILayout.LabelField("Output Links", EditorStyles.boldLabel);
        MonoBehaviour[] components = adapter.GetComponentsInChildren<MonoBehaviour>(true);
        for (int c = 0; c < components.Length; c++)
        {
            MonoBehaviour component = components[c];
            if (component == null) continue;
            outputFieldBuffer.Clear();
            CollectOutputFields(component.GetType(), outputFieldBuffer);
            if (outputFieldBuffer.Count == 0) continue;

            SerializedObject so = new SerializedObject(component);
            EditorGUILayout.LabelField(component.GetType().Name, EditorStyles.boldLabel);
            for (int f = 0; f < outputFieldBuffer.Count; f++)
            {
                SerializedProperty prop = so.FindProperty(outputFieldBuffer[f].Name);
                if (prop != null) EditorGUILayout.PropertyField(prop, true);
            }
            if (so.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(component);
                RefreshGraph();
            }
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Output Component"))
        {
            if (adapter.GetComponent<OUTL_MultiManager>() == null)
            {
                Undo.AddComponent<OUTL_MultiManager>(adapter.gameObject).Entity = adapter;
                RefreshGraph();
            }
        }
        if (GUILayout.Button("Add KillCounter"))
        {
            if (adapter.GetComponent<OUTL_KillCounter>() == null)
            {
                OUTL_KillCounter kc = Undo.AddComponent<OUTL_KillCounter>(adapter.gameObject);
                kc.Entity = adapter;
                kc.RequiredKills = 10;
                RefreshGraph();
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawEdgesInspector(Node node)
    {
        EditorGUILayout.LabelField("Outgoing", EditorStyles.boldLabel);
        int outgoing = 0;
        for (int i = 0; i < edges.Count; i++)
        {
            Edge e = edges[i];
            if (e.FromId != node.Id) continue;
            outgoing++;
            EditorGUILayout.LabelField(GetEdgePrefix(e), e.Label + " -> " + GetNodeTitle(e.ToId));
        }
        if (outgoing == 0) EditorGUILayout.LabelField("<none>");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Incoming", EditorStyles.boldLabel);
        int incoming = 0;
        for (int i = 0; i < edges.Count; i++)
        {
            Edge e = edges[i];
            if (e.ToId != node.Id) continue;
            incoming++;
            EditorGUILayout.LabelField(GetEdgePrefix(e), GetNodeTitle(e.FromId) + " -> " + e.Label);
        }
        if (incoming == 0) EditorGUILayout.LabelField("<none>");
    }

    private void ProcessCanvasEvents(Event e, Rect canvasRect)
    {
        if (!canvasRect.Contains(e.mousePosition)) return;
        Vector2 graphMouse = ScreenToGraph(e.mousePosition, canvasRect);

        if (e.type == EventType.ScrollWheel)
        {
            float oldZoom = zoom;
            zoom = Mathf.Clamp(zoom - e.delta.y * 0.03f, 0.3f, 2.5f);
            Vector2 local = e.mousePosition - canvasRect.position;
            Vector2 before = (local - pan) / oldZoom;
            Vector2 after = (local - pan) / zoom;
            pan += (after - before) * zoom;
            e.Use();
            return;
        }

        bool wantsPan = e.button == 2 || e.button == 1 || (e.button == 0 && e.alt);
        if (e.type == EventType.MouseDown)
        {
            lastMouse = e.mousePosition;
            if (wantsPan)
            {
                panning = true;
                e.Use();
                return;
            }

            if (e.button == 0)
            {
                Node hit = FindNodeAt(graphMouse);
                selectedNode = hit;
                draggingNode = hit;
                if (hit != null)
                {
                    dragOffset = graphMouse - hit.Rect.position;
                    Selection.activeObject = hit.Context != null ? hit.Context : hit.GameObject;
                }
                e.Use();
            }
        }
        else if (e.type == EventType.MouseDrag)
        {
            Vector2 delta = e.mousePosition - lastMouse;
            lastMouse = e.mousePosition;
            if (panning)
            {
                pan += delta;
                e.Use();
            }
            else if (draggingNode != null && e.button == 0)
            {
                draggingNode.Rect.position = graphMouse - dragOffset;
                e.Use();
            }
        }
        else if (e.type == EventType.MouseUp)
        {
            panning = false;
            draggingNode = null;
        }
    }

    private Vector2 ScreenToGraph(Vector2 screenMouse, Rect canvasRect)
    {
        return (screenMouse - canvasRect.position - pan) / zoom;
    }

    private Rect GraphToScreen(Rect graphRect, Rect canvasRect)
    {
        return new Rect(canvasRect.x + pan.x + graphRect.x * zoom, canvasRect.y + pan.y + graphRect.y * zoom, graphRect.width * zoom, graphRect.height * zoom);
    }

    private Node FindNodeAt(Vector2 graphPosition)
    {
        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            Node node = nodes[i];
            if (!IsNodeVisible(node)) continue;
            if (node.Rect.Contains(graphPosition)) return node;
        }
        return null;
    }

    private void RefreshGraph()
    {
        nodes.Clear();
        edges.Clear();
        nodeById.Clear();
        targetNameToNodes.Clear();

        OUTL_World world = FindObjectOfType<OUTL_World>(true);
        Node worldNode = AddNode("world", world != null ? world.gameObject : null, world, null, NodeKind.World, "OUTL_World", world != null ? world.name : "No OUTL_World in scene", "OUTL_Runtime", "WorldRoot", "runtime root");

        OUTL_EntityAdapter[] entities = FindObjectsOfType<OUTL_EntityAdapter>(true);
        Array.Sort(entities, CompareAdapters);

        for (int i = 0; i < entities.Length; i++)
        {
            OUTL_EntityAdapter adapter = entities[i];
            Node entityNode = CreateEntityNode(adapter);
            if (entityNode == null) continue;
            edges.Add(new Edge { FromId = worldNode.Id, ToId = entityNode.Id, Label = "register", Kind = EdgeKind.WorldRegistration });
        }

        BuildHierarchyEdges(entities);
        BuildOutputEdges(entities);
        BuildStateQueryEdges(entities);
        if (autoLayoutOnRefresh) AutoLayout();
        Repaint();
    }

    private Node CreateEntityNode(OUTL_EntityAdapter adapter)
    {
        if (adapter == null) return null;
        string className = !string.IsNullOrEmpty(adapter.ClassNameOverride) ? adapter.ClassNameOverride : (adapter.Def != null ? adapter.Def.ClassName : string.Empty);
        string subtitle = (adapter.Id.IsValid ? "id " + adapter.Id.Value : "id editor") + " | " + adapter.name;
        string runtimeLine = "tier " + adapter.Tier + " | lane " + adapter.TickLane + " | tick " + adapter.TickInterval.ToString("0.###") + (adapter.RegisterRandomTick ? " | rnd " + adapter.RandomTickInterval.ToString("0.###") : "");
        Node node = AddNode(EntityNodeId(adapter), adapter.gameObject, adapter, adapter, NodeKind.Entity, adapter.name, subtitle, adapter.TargetName, className, runtimeLine);
        if (!string.IsNullOrEmpty(adapter.TargetName))
        {
            List<Node> list;
            if (!targetNameToNodes.TryGetValue(adapter.TargetName, out list))
            {
                list = new List<Node>(4);
                targetNameToNodes.Add(adapter.TargetName, list);
            }
            list.Add(node);
        }
        return node;
    }

    private void BuildHierarchyEdges(OUTL_EntityAdapter[] entities)
    {
        for (int i = 0; i < entities.Length; i++)
        {
            OUTL_EntityAdapter child = entities[i];
            if (child == null || child.transform.parent == null) continue;
            OUTL_EntityAdapter parent = child.transform.parent.GetComponentInParent<OUTL_EntityAdapter>();
            if (parent == null || parent == child) continue;
            edges.Add(new Edge { FromId = EntityNodeId(parent), ToId = EntityNodeId(child), Label = "child", Kind = EdgeKind.Hierarchy });
        }
    }

    private void BuildOutputEdges(OUTL_EntityAdapter[] entities)
    {
        for (int i = 0; i < entities.Length; i++)
        {
            OUTL_EntityAdapter owner = entities[i];
            if (owner == null) continue;
            string fromId = EntityNodeId(owner);
            MonoBehaviour[] components = owner.GetComponentsInChildren<MonoBehaviour>(true);
            for (int c = 0; c < components.Length; c++)
            {
                MonoBehaviour component = components[c];
                if (component == null) continue;
                outputFieldBuffer.Clear();
                CollectOutputFields(component.GetType(), outputFieldBuffer);
                for (int f = 0; f < outputFieldBuffer.Count; f++)
                {
                    OUTL_OutputLink[] outputs = outputFieldBuffer[f].GetValue(component) as OUTL_OutputLink[];
                    AddOutputEdgesFromArray(fromId, component, outputs);
                }
            }
        }
    }

    private void AddOutputEdgesFromArray(string fromId, Component component, OUTL_OutputLink[] outputs)
    {
        if (outputs == null) return;
        for (int i = 0; i < outputs.Length; i++)
        {
            OUTL_OutputLink output = outputs[i];
            if (output == null) continue;
            if (output.Disabled && !showDisabledOutputs) continue;
            string targetName = output.TargetName;
            if (string.IsNullOrEmpty(targetName))
            {
                string missingId = MissingNodeId("<empty target>", component.GetInstanceID() + ":" + i);
                EnsureMissingNode(missingId, "<empty target>", "Output has no TargetName");
                edges.Add(new Edge { FromId = fromId, ToId = missingId, Label = BuildOutputLabel(output), Kind = EdgeKind.MissingOutput, SourceComponent = component, Output = output, TargetName = targetName, Disabled = output.Disabled });
                continue;
            }

            List<Node> targets;
            if (!targetNameToNodes.TryGetValue(targetName, out targets) || targets.Count == 0)
            {
                string missingId = MissingNodeId(targetName, string.Empty);
                EnsureMissingNode(missingId, targetName, "Missing TargetName");
                edges.Add(new Edge { FromId = fromId, ToId = missingId, Label = BuildOutputLabel(output), Kind = EdgeKind.MissingOutput, SourceComponent = component, Output = output, TargetName = targetName, Disabled = output.Disabled });
                continue;
            }

            for (int t = 0; t < targets.Count; t++)
                edges.Add(new Edge { FromId = fromId, ToId = targets[t].Id, Label = BuildOutputLabel(output), Kind = EdgeKind.OutputCommand, SourceComponent = component, Output = output, TargetName = targetName, Disabled = output.Disabled });
        }
    }

    private void BuildStateQueryEdges(OUTL_EntityAdapter[] entities)
    {
        for (int i = 0; i < entities.Length; i++)
        {
            OUTL_EntityAdapter owner = entities[i];
            if (owner == null) continue;
            string ownerId = EntityNodeId(owner);
            OUTL_LogicRelay relay = owner.GetComponentInChildren<OUTL_LogicRelay>(true);
            if (relay != null) AddInputQueryEdges(ownerId, relay.Inputs, "relay " + relay.Gate);
            OUTL_MultiSource multiSource = owner.GetComponentInChildren<OUTL_MultiSource>(true);
            if (multiSource != null) AddInputQueryEdges(ownerId, multiSource.Inputs, "multi " + multiSource.Gate);
        }
    }

    private void AddInputQueryEdges(string ownerId, OUTL_MultiSourceInput[] inputs, string labelPrefix)
    {
        if (inputs == null) return;
        for (int i = 0; i < inputs.Length; i++)
        {
            OUTL_MultiSourceInput input = inputs[i];
            if (string.IsNullOrEmpty(input.TargetName)) continue;
            string label = labelPrefix + " | " + (string.IsNullOrEmpty(input.Flag) ? "On" : input.Flag) + (input.Invert ? " !" : string.Empty);
            List<Node> sources;
            if (!targetNameToNodes.TryGetValue(input.TargetName, out sources) || sources.Count == 0)
            {
                string missingId = MissingNodeId(input.TargetName, "query");
                EnsureMissingNode(missingId, input.TargetName, "Missing query TargetName");
                edges.Add(new Edge { FromId = missingId, ToId = ownerId, Label = label, Kind = EdgeKind.StateQuery, TargetName = input.TargetName });
                continue;
            }
            for (int s = 0; s < sources.Count; s++)
                edges.Add(new Edge { FromId = sources[s].Id, ToId = ownerId, Label = label, Kind = EdgeKind.StateQuery, TargetName = input.TargetName });
        }
    }

    private Node AddNode(string id, GameObject go, UnityEngine.Object context, OUTL_EntityAdapter adapter, NodeKind kind, string title, string subtitle, string targetName, string className, string runtimeLine)
    {
        Node existing;
        if (nodeById.TryGetValue(id, out existing)) return existing;
        Node node = new Node { Id = id, GameObject = go, Context = context, Adapter = adapter, Kind = kind, Title = string.IsNullOrEmpty(title) ? id : title, Subtitle = subtitle ?? string.Empty, TargetName = targetName ?? string.Empty, ClassName = className ?? string.Empty, RuntimeLine = runtimeLine ?? string.Empty, Rect = new Rect(0f, 0f, NodeWidth, NodeHeight) };
        nodes.Add(node);
        nodeById[id] = node;
        return node;
    }

    private Node EnsureMissingNode(string id, string targetName, string subtitle)
    {
        Node node;
        if (nodeById.TryGetValue(id, out node)) return node;
        return AddNode(id, null, null, null, NodeKind.MissingTarget, "Missing: " + targetName, subtitle, targetName, "MissingTarget", "broken link");
    }

    private void AutoLayout()
    {
        float worldX = 20f, sourceX = 340f, logicX = 680f, targetX = 1020f, missingX = 1360f, rowHeight = 142f;
        int sourceRow = 0, logicRow = 0, targetRow = 0, missingRow = 0;
        for (int i = 0; i < nodes.Count; i++)
        {
            Node node = nodes[i];
            if (node.Kind == NodeKind.World) { node.Rect.position = new Vector2(worldX, 40f); continue; }
            bool hasOutgoing = HasEdgeFrom(node.Id, EdgeKind.OutputCommand) || HasEdgeFrom(node.Id, EdgeKind.MissingOutput);
            bool hasIncomingOutput = HasEdgeTo(node.Id, EdgeKind.OutputCommand) || HasEdgeTo(node.Id, EdgeKind.MissingOutput);
            bool hasStateQuery = HasEdgeFrom(node.Id, EdgeKind.StateQuery) || HasEdgeTo(node.Id, EdgeKind.StateQuery);
            if (node.Kind == NodeKind.MissingTarget) node.Rect.position = new Vector2(missingX, 40f + missingRow++ * rowHeight);
            else if ((hasOutgoing && hasIncomingOutput) || hasStateQuery) node.Rect.position = new Vector2(logicX, 40f + logicRow++ * rowHeight);
            else if (hasOutgoing) node.Rect.position = new Vector2(sourceX, 40f + sourceRow++ * rowHeight);
            else node.Rect.position = new Vector2(targetX, 40f + targetRow++ * rowHeight);
        }
    }

    private void FrameAll()
    {
        if (nodes.Count == 0) return;
        Rect bounds = nodes[0].Rect;
        for (int i = 1; i < nodes.Count; i++) bounds = Union(bounds, nodes[i].Rect);
        float canvasWidth = Mathf.Max(1f, position.width - InspectorWidth);
        float canvasHeight = Mathf.Max(1f, position.height - ToolbarHeight);
        zoom = Mathf.Clamp(Mathf.Min(canvasWidth / Mathf.Max(1f, bounds.width + 160f), canvasHeight / Mathf.Max(1f, bounds.height + 160f)), 0.3f, 1.2f);
        pan = new Vector2(80f - bounds.xMin * zoom, 80f - bounds.yMin * zoom);
    }

    private static Rect Union(Rect a, Rect b) { return Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax)); }
    private bool HasEdgeFrom(string nodeId, EdgeKind kind) { for (int i = 0; i < edges.Count; i++) if (edges[i].FromId == nodeId && edges[i].Kind == kind) return true; return false; }
    private bool HasEdgeTo(string nodeId, EdgeKind kind) { for (int i = 0; i < edges.Count; i++) if (edges[i].ToId == nodeId && edges[i].Kind == kind) return true; return false; }

    private bool IsNodeVisible(Node node)
    {
        if (node == null) return false;
        if (node.Kind == NodeKind.MissingTarget && !showMissingLinks) return false;
        if (string.IsNullOrEmpty(search)) return true;
        string s = search.ToLowerInvariant();
        return ContainsLower(node.Title, s) || ContainsLower(node.Subtitle, s) || ContainsLower(node.TargetName, s) || ContainsLower(node.ClassName, s) || ContainsLower(node.RuntimeLine, s);
    }

    private bool IsEdgeVisible(Edge edge)
    {
        if (edge == null) return false;
        if (edge.Disabled && !showDisabledOutputs) return false;
        if (edge.Kind == EdgeKind.WorldRegistration) return showWorldLinks;
        if (edge.Kind == EdgeKind.Hierarchy) return showHierarchyLinks;
        if (edge.Kind == EdgeKind.OutputCommand) return showOutputLinks;
        if (edge.Kind == EdgeKind.StateQuery) return showStateQueryLinks;
        if (edge.Kind == EdgeKind.MissingOutput) return showMissingLinks;
        return true;
    }

    private static bool ContainsLower(string value, string lowerSearch) { return !string.IsNullOrEmpty(value) && value.ToLowerInvariant().Contains(lowerSearch); }

    private static Color GetNodeColor(Node node)
    {
        if (node == null) return Color.white;
        switch (node.Kind)
        {
            case NodeKind.World: return new Color(0.33f, 0.43f, 0.8f, 1f);
            case NodeKind.MissingTarget: return new Color(0.82f, 0.22f, 0.18f, 1f);
            case NodeKind.Entity:
            default:
                if (node.Adapter != null)
                {
                    switch (node.Adapter.Tier)
                    {
                        case OUTL_RuntimeTier.Full: return new Color(0.18f, 0.7f, 0.78f, 1f);
                        case OUTL_RuntimeTier.Near: return new Color(0.22f, 0.68f, 0.34f, 1f);
                        case OUTL_RuntimeTier.Mid: return new Color(0.65f, 0.58f, 0.25f, 1f);
                        case OUTL_RuntimeTier.Far: return new Color(0.65f, 0.36f, 0.22f, 1f);
                        case OUTL_RuntimeTier.Dormant: return new Color(0.32f, 0.32f, 0.32f, 1f);
                    }
                }
                return new Color(0.28f, 0.55f, 0.42f, 1f);
        }
    }

    private static Color GetEdgeColor(Edge edge)
    {
        if (edge == null) return Color.white;
        switch (edge.Kind)
        {
            case EdgeKind.WorldRegistration: return new Color(0.5f, 0.5f, 0.5f, 0.22f);
            case EdgeKind.Hierarchy: return new Color(0.55f, 0.55f, 0.85f, 0.35f);
            case EdgeKind.StateQuery: return new Color(1f, 0.78f, 0.25f, 0.9f);
            case EdgeKind.MissingOutput: return new Color(1f, 0.2f, 0.16f, 0.95f);
            case EdgeKind.OutputCommand:
            default: return new Color(0.2f, 0.78f, 1f, 0.95f);
        }
    }

    private static string GetEdgePrefix(Edge edge)
    {
        if (edge == null) return string.Empty;
        switch (edge.Kind)
        {
            case EdgeKind.WorldRegistration: return "world";
            case EdgeKind.Hierarchy: return "child";
            case EdgeKind.StateQuery: return "query";
            case EdgeKind.MissingOutput: return "missing";
            case EdgeKind.OutputCommand:
            default: return "output";
        }
    }

    private string GetNodeTitle(string id) { Node n; return nodeById.TryGetValue(id, out n) ? n.Title : id; }

    private static string BuildOutputLabel(OUTL_OutputLink output)
    {
        if (output == null) return "null";
        string label = (string.IsNullOrEmpty(output.EventName) ? "*" : output.EventName) + " -> " + output.Command;
        if (!string.IsNullOrEmpty(output.Key)) label += " [" + output.Key + "]";
        if (output.Delay > 0f) label += " +" + output.Delay.ToString("0.##") + "s";
        if (output.Once) label += " once";
        if (output.Disabled) label += " disabled";
        return label;
    }

    private static void CollectOutputFields(Type type, List<FieldInfo> result)
    {
        while (type != null && type != typeof(MonoBehaviour))
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            for (int i = 0; i < fields.Length; i++) if (fields[i].FieldType == typeof(OUTL_OutputLink[])) result.Add(fields[i]);
            type = type.BaseType;
        }
    }

    private void CreateBasicEntity(string baseName, string className, Type componentType)
    {
        GameObject go = new GameObject(baseName);
        Undo.RegisterCreatedObjectUndo(go, "Create OUTL Graph Entity");
        if (Selection.activeTransform != null) go.transform.position = Selection.activeTransform.position + Vector3.right;
        OUTL_EntityAdapter adapter = go.AddComponent<OUTL_EntityAdapter>();
        adapter.ClassNameOverride = className;
        adapter.TargetName = MakeSafeName(baseName + "_" + go.GetInstanceID());
        adapter.StableId = MakeSafeName(go.scene.name + "_" + baseName + "_" + go.GetInstanceID());

        if (componentType == typeof(OUTL_Door))
        {
            OUTL_Door door = go.AddComponent<OUTL_Door>();
            door.Entity = adapter;
            door.DoorRoot = go.transform;
        }
        else if (componentType == typeof(OUTL_Button))
        {
            OUTL_Button button = go.AddComponent<OUTL_Button>();
            button.Entity = adapter;
            button.Toggle = true;
            button.Outputs = new[] { new OUTL_OutputLink { EventName = "OnPressed", Command = OUTL_CommandType.Activate } };
        }
        else if (componentType == typeof(OUTL_LogicRelay))
        {
            OUTL_LogicRelay relay = go.AddComponent<OUTL_LogicRelay>();
            relay.Entity = adapter;
            relay.Outputs = new[] { new OUTL_OutputLink { EventName = "OnTrue", Command = OUTL_CommandType.Activate } };
        }
        else if (componentType == typeof(OUTL_MultiManager))
        {
            OUTL_MultiManager manager = go.AddComponent<OUTL_MultiManager>();
            manager.Entity = adapter;
            manager.Outputs = new[] { new OUTL_OutputLink { EventName = "OnTrigger", Command = OUTL_CommandType.Activate } };
        }
        else if (componentType == typeof(OUTL_MultiSource))
        {
            OUTL_MultiSource source = go.AddComponent<OUTL_MultiSource>();
            source.Entity = adapter;
            source.Outputs = new[] { new OUTL_OutputLink { EventName = "OnSatisfied", Command = OUTL_CommandType.Activate } };
        }
        else if (componentType == typeof(OUTL_KillCounter))
        {
            OUTL_KillCounter counter = go.AddComponent<OUTL_KillCounter>();
            counter.Entity = adapter;
            counter.RequiredKills = 10;
            counter.Outputs = new[] { new OUTL_OutputLink { EventName = "OnCompleted", Command = OUTL_CommandType.Activate } };
        }

        Selection.activeObject = adapter;
        EditorGUIUtility.PingObject(go);
        RefreshGraph();
    }

    private static string MakeSafeName(string value)
    {
        if (string.IsNullOrEmpty(value)) return "outl_entity";
        value = value.Trim().ToLowerInvariant();
        char[] chars = value.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
            if (!char.IsLetterOrDigit(chars[i])) chars[i] = '_';
        return new string(chars).Trim('_');
    }

    private static int CompareAdapters(OUTL_EntityAdapter a, OUTL_EntityAdapter b) { return string.CompareOrdinal(a != null ? a.name : string.Empty, b != null ? b.name : string.Empty); }
    private static string EntityNodeId(OUTL_EntityAdapter adapter) { return adapter != null ? "entity:" + adapter.GetInstanceID() : "entity:null"; }
    private static string MissingNodeId(string targetName, string salt) { return "missing:" + targetName + ":" + salt; }
}
#endif
