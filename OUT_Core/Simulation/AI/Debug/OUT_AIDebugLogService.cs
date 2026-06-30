using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

[DefaultExecutionOrder(-9000)]
public class OUT_AIDebugLogService : MonoBehaviour
{
    public enum AIEventKind
    {
        Brain = 0,
        Perception = 1,
        Hearing = 2,
        Memory = 3,
        Schedule = 4,
        Task = 5,
        Route = 6,
        Combat = 7,
        Warning = 8
    }

    [Serializable]
    public struct Entry
    {
        public float Time;
        public string Actor;
        public AIEventKind Kind;
        public string Message;
    }

    public static OUT_AIDebugLogService Instance { get; private set; }

    [Header("Global")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool autoCreateIfMissing = true;
    [SerializeField] private bool captureLogs = true;
    [SerializeField] private bool alsoLogToUnityConsole = false;

    [Header("Storage")]
    [SerializeField] [Min(32)] private int maxEntries = 512;
    [SerializeField] private bool saveOnQuit = false;
    [SerializeField] private string fileName = "out_ai_debug_log.txt";

    [Header("Overlay")]
    [SerializeField] private bool showOverlay = true;
    [SerializeField] [Min(4)] private int visibleLines = 18;
    [SerializeField] private KeyCode toggleKey = KeyCode.F8;
    [SerializeField] private Vector2 overlayPosition = new Vector2(12f, 12f);
    [SerializeField] private Vector2 overlaySize = new Vector2(980f, 420f);
    [SerializeField] private bool pauseOverlayUpdatesWhenHidden = false;

    private readonly List<Entry> _entries = new List<Entry>(512);
    private readonly StringBuilder _builder = new StringBuilder(8192);
    private Vector2 _scroll;

    public IReadOnlyList<Entry> Entries => _entries;
    public bool CaptureLogs
    {
        get => captureLogs;
        set => captureLogs = value;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (toggleKey == KeyCode.F9)
            toggleKey = KeyCode.F8;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            showOverlay = !showOverlay;
    }

    private void OnApplicationQuit()
    {
        if (saveOnQuit)
            SaveToFile();
    }

    private void OnGUI()
    {
        if (!showOverlay)
            return;

        Rect rect = new Rect(overlayPosition.x, overlayPosition.y, overlaySize.x, overlaySize.y);
        GUILayout.BeginArea(rect, GUI.skin.box);
        GUILayout.BeginHorizontal();
        GUILayout.Label($"OUT AI LOG  entries:{_entries.Count}  toggle:{toggleKey}  file:{fileName}");
        if (GUILayout.Button("Clear", GUILayout.Width(80f)))
            Clear();
        if (GUILayout.Button("Save", GUILayout.Width(80f)))
            SaveToFile();
        GUILayout.EndHorizontal();

        _builder.Length = 0;
        int start = Mathf.Max(0, _entries.Count - Mathf.Max(1, visibleLines));
        for (int i = start; i < _entries.Count; i++)
        {
            Entry e = _entries[i];
            _builder.Append('[').Append(e.Time.ToString("0.00")).Append("] ");
            _builder.Append(e.Kind).Append(" | ");
            _builder.Append(e.Actor).Append(" | ");
            _builder.AppendLine(e.Message);
        }

        _scroll = GUILayout.BeginScrollView(_scroll);
        GUILayout.TextArea(_builder.ToString(), GUILayout.ExpandHeight(true));
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    public static void Log(GameObject actor, AIEventKind kind, string message)
    {
        OUT_AIDebugLogService service = GetOrCreate();
        if (service == null || !service.captureLogs)
            return;

        service.Add(actor != null ? actor.name : "<null>", kind, message);
    }

    public static void Log(Component actor, AIEventKind kind, string message)
    {
        Log(actor != null ? actor.gameObject : null, kind, message);
    }

    private static OUT_AIDebugLogService GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        OUT_AIDebugLogService existing = FindObjectOfType<OUT_AIDebugLogService>();
        if (existing != null)
            return existing;

        GameObject go = new GameObject("OUT_AI_DebugLogService");
        OUT_AIDebugLogService created = go.AddComponent<OUT_AIDebugLogService>();
        return created.autoCreateIfMissing ? created : null;
    }

    private void Add(string actor, AIEventKind kind, string message)
    {
        if (_entries.Count >= Mathf.Max(32, maxEntries))
            _entries.RemoveAt(0);

        Entry entry = new Entry
        {
            Time = Time.time,
            Actor = actor,
            Kind = kind,
            Message = message ?? string.Empty
        };

        _entries.Add(entry);

        if (alsoLogToUnityConsole)
            Debug.Log($"[OUT AI] [{entry.Kind}] {entry.Actor}: {entry.Message}");
    }

    [ContextMenu("Save To File")]
    public void SaveToFile()
    {
        string path = GetFilePath();
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        using (StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8))
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry e = _entries[i];
                writer.Write('[');
                writer.Write(e.Time.ToString("0.000"));
                writer.Write("]\t");
                writer.Write(e.Kind);
                writer.Write("\t");
                writer.Write(e.Actor);
                writer.Write("\t");
                writer.WriteLine(e.Message);
            }
        }

        Debug.Log($"OUT_AIDebugLogService saved {_entries.Count} entries to: {path}");
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        _entries.Clear();
    }

    public string GetFilePath()
    {
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "out_ai_debug_log.txt";

        if (Path.IsPathRooted(fileName))
            return fileName;

        return Path.Combine(Application.persistentDataPath, fileName);
    }
}
