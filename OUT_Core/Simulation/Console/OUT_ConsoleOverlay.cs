using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[DefaultExecutionOrder(-8400)]
[DisallowMultipleComponent]
public class OUT_ConsoleOverlay : MonoBehaviour
{
    public static bool AnyOpen { get; private set; }

    [Header("References")]
    [SerializeField] private OUT_ConsoleService service;

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote;
    [SerializeField] private KeyCode fallbackToggleKey = KeyCode.F1;
    [SerializeField] private bool closeOnEscape = false;
    [SerializeField] private bool consumeKeyboardWhenOpen = true;
    [SerializeField] private bool pauseGameWhenOpen = false;

    [Header("Layout")]
    [SerializeField] private bool isOpen = false;
    [SerializeField] private int visibleLines = 24;
    [SerializeField] private Rect windowRect = new Rect(10f, 10f, 980f, 520f);
    [SerializeField] private float inputLineHeight = 24f;

    [Header("Caret")]
    [SerializeField] private bool drawCaret = true;
    [SerializeField] private float caretBlinkRate = 0.55f;
    [SerializeField] private Color caretColor = Color.white;
    [SerializeField] private Color inputBackgroundColor = new Color(0f, 0f, 0f, 0.55f);

    [Header("Autocomplete")]
    [SerializeField] private bool enableAutocomplete = true;
    [SerializeField] private int maxAutocompleteResultsToPrint = 18;

    private string input = string.Empty;
    private Vector2 scroll;
    private int historyIndex = -1;
    private float previousTimeScale = 1f;
    private int cursorIndex;
    private int lastToggleFrame = -1;
    private Rect lastInputRect;
    private readonly List<string> autocompleteMatches = new List<string>(64);
    private readonly StringBuilder builder = new StringBuilder(512);

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (service == null)
            service = GetComponent<OUT_ConsoleService>();

        if (service == null)
            service = OUT_ConsoleService.Instance;
    }

    private void OnEnable()
    {
        RefreshAnyOpenFlag();
    }

    private void OnDisable()
    {
        if (isOpen)
            SetOpen(false);
        RefreshAnyOpenFlag();
    }

    private void Update()
    {
        if (service == null)
            service = OUT_ConsoleService.Instance;

        if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(fallbackToggleKey))
            ToggleOpen();
    }

    private void OnGUI()
    {
        Event e = Event.current;

        if (IsToggleEvent(e))
        {
            ToggleOpen();
            e.Use();
            return;
        }

        if (!isOpen || service == null || service.Log == null)
            return;

        windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow, "OUT CORE Console");

        if (consumeKeyboardWhenOpen && e != null && e.type == EventType.KeyDown && !e.isMouse)
            e.Use();
    }

    private void DrawWindow(int id)
    {
        HandleConsoleKeyEvent(Event.current);

        GUILayout.BeginVertical();

        scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
        GUILayout.TextArea(service.Log.BuildText(visibleLines), GUILayout.ExpandHeight(true));
        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        DrawInputLineGUILayout();

        if (GUILayout.Button("Run", GUILayout.Width(70f), GUILayout.Height(inputLineHeight)))
            Submit();

        if (GUILayout.Button("Clear", GUILayout.Width(70f), GUILayout.Height(inputLineHeight)))
            service.Log.Clear();

        GUILayout.EndHorizontal();
        GUILayout.Label($"toggle: {toggleKey}/{fallbackToggleKey} | Enter: run | Tab: autocomplete | Up/Down: history | Esc: {(closeOnEscape ? "close" : "ignored")} | F5 save | F9 load");
        GUILayout.EndVertical();

        GUI.DragWindow(new Rect(0, 0, windowRect.width, 24));
    }

    private void DrawInputLineGUILayout()
    {
        Rect rect = GUILayoutUtility.GetRect(10f, inputLineHeight, GUILayout.ExpandWidth(true));
        lastInputRect = rect;

        Color previousColor = GUI.color;
        GUI.color = inputBackgroundColor;
        GUI.Box(rect, GUIContent.none);
        GUI.color = previousColor;

        string prefix = "> ";
        Rect textRect = new Rect(rect.x + 6f, rect.y + 3f, rect.width - 12f, rect.height - 6f);
        GUI.Label(textRect, prefix + input, GUI.skin.label);

        if (drawCaret && ShouldShowCaret())
            DrawCaret(textRect, prefix);
    }

    private bool ShouldShowCaret()
    {
        float rate = Mathf.Max(0.05f, caretBlinkRate);
        return (Time.realtimeSinceStartup % rate) < rate * 0.5f;
    }

    private void DrawCaret(Rect textRect, string prefix)
    {
        cursorIndex = Mathf.Clamp(cursorIndex, 0, input.Length);

        GUIStyle style = GUI.skin.label;
        string beforeCaret = prefix + input.Substring(0, cursorIndex);
        Vector2 size = style.CalcSize(new GUIContent(beforeCaret));
        float x = Mathf.Min(textRect.x + size.x, textRect.xMax - 4f);
        Rect caretRect = new Rect(x, textRect.y + 2f, 2f, textRect.height - 4f);

        Color old = GUI.color;
        GUI.color = caretColor;
        GUI.DrawTexture(caretRect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    private void HandleConsoleKeyEvent(Event e)
    {
        if (e == null)
            return;

        if (e.type == EventType.MouseDown && lastInputRect.Contains(e.mousePosition))
        {
            MoveCursorFromMouse(e.mousePosition.x);
            e.Use();
            return;
        }

        if (e.type != EventType.KeyDown)
            return;

        if (IsToggleEvent(e))
        {
            SetOpen(false);
            e.Use();
            return;
        }

        if (e.keyCode == KeyCode.Escape)
        {
            if (closeOnEscape)
                SetOpen(false);

            e.Use();
            return;
        }

        if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
        {
            Submit();
            e.Use();
            return;
        }

        if (e.keyCode == KeyCode.Tab)
        {
            CompleteInput();
            e.Use();
            return;
        }

        if (e.keyCode == KeyCode.UpArrow)
        {
            BrowseHistory(-1);
            e.Use();
            return;
        }

        if (e.keyCode == KeyCode.DownArrow)
        {
            BrowseHistory(1);
            e.Use();
            return;
        }

        if (e.keyCode == KeyCode.LeftArrow)
        {
            cursorIndex = Mathf.Max(0, cursorIndex - 1);
            e.Use();
            return;
        }

        if (e.keyCode == KeyCode.RightArrow)
        {
            cursorIndex = Mathf.Min(input.Length, cursorIndex + 1);
            e.Use();
            return;
        }

        if (e.keyCode == KeyCode.Home)
        {
            cursorIndex = 0;
            e.Use();
            return;
        }

        if (e.keyCode == KeyCode.End)
        {
            cursorIndex = input.Length;
            e.Use();
            return;
        }

        if (e.keyCode == KeyCode.Backspace)
        {
            if (cursorIndex > 0 && input.Length > 0)
            {
                input = input.Remove(cursorIndex - 1, 1);
                cursorIndex--;
            }
            e.Use();
            return;
        }

        if (e.keyCode == KeyCode.Delete)
        {
            if (cursorIndex < input.Length)
                input = input.Remove(cursorIndex, 1);
            e.Use();
            return;
        }

        if ((e.control || e.command) && e.keyCode == KeyCode.V)
        {
            InsertText(GUIUtility.systemCopyBuffer ?? string.Empty);
            e.Use();
            return;
        }

        if ((e.control || e.command) && e.keyCode == KeyCode.C)
        {
            GUIUtility.systemCopyBuffer = input;
            e.Use();
            return;
        }

        if ((e.control || e.command) && e.keyCode == KeyCode.A)
        {
            cursorIndex = input.Length;
            e.Use();
            return;
        }

        char c = e.character;
        if (!char.IsControl(c))
        {
            InsertText(c.ToString());
            e.Use();
        }
    }

    private void MoveCursorFromMouse(float mouseX)
    {
        string prefix = "> ";
        float localX = Mathf.Max(0f, mouseX - lastInputRect.x - 6f);
        GUIStyle style = GUI.skin.label;

        cursorIndex = input.Length;
        for (int i = 0; i <= input.Length; i++)
        {
            string test = prefix + input.Substring(0, i);
            float width = style.CalcSize(new GUIContent(test)).x;
            if (width >= localX)
            {
                cursorIndex = i;
                break;
            }
        }
    }

    private bool IsToggleEvent(Event e)
    {
        if (e == null || e.type != EventType.KeyDown)
            return false;

        if (e.keyCode == toggleKey || e.keyCode == fallbackToggleKey)
            return true;

        return e.character == '`' || e.character == '~' || e.character == 'ё' || e.character == 'Ё';
    }

    private void InsertText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        text = text.Replace("\n", string.Empty).Replace("\r", string.Empty);
        if (text.Length == 0)
            return;

        cursorIndex = Mathf.Clamp(cursorIndex, 0, input.Length);
        input = input.Insert(cursorIndex, text);
        cursorIndex += text.Length;
        historyIndex = -1;
    }

    private void Submit()
    {
        if (string.IsNullOrWhiteSpace(input))
            return;

        service.ExecuteLine(input, true);
        input = string.Empty;
        cursorIndex = 0;
        historyIndex = -1;
        scroll.y = float.MaxValue;
    }

    private void BrowseHistory(int direction)
    {
        if (service == null || service.History.Count == 0)
            return;

        if (historyIndex < 0)
            historyIndex = service.History.Count;

        historyIndex = Mathf.Clamp(historyIndex + direction, 0, service.History.Count - 1);
        input = service.History[historyIndex];
        cursorIndex = input.Length;
    }

    private void CompleteInput()
    {
        if (!enableAutocomplete || service == null)
            return;

        string prefix = GetCurrentToken(input, out int tokenStart, out int tokenEnd);
        if (string.IsNullOrWhiteSpace(prefix))
            return;

        autocompleteMatches.Clear();
        CollectAutocompleteMatches(prefix, autocompleteMatches);

        if (autocompleteMatches.Count == 0)
        {
            service.Log.Add($"no autocomplete matches for '{prefix}'", OUT_ConsoleLog.Level.Warning);
            return;
        }

        autocompleteMatches.Sort(StringComparer.OrdinalIgnoreCase);

        string common = GetCommonPrefix(autocompleteMatches);
        if (common.Length > prefix.Length)
        {
            ReplaceCurrentToken(common, tokenStart, tokenEnd);
            return;
        }

        if (autocompleteMatches.Count == 1)
        {
            ReplaceCurrentToken(autocompleteMatches[0], tokenStart, tokenEnd);
            return;
        }

        PrintAutocompleteMatches(prefix, autocompleteMatches);
    }

    private void CollectAutocompleteMatches(string prefix, List<string> result)
    {
        if (service.Commands != null)
        {
            List<OUT_ConsoleCommand> commands = service.Commands.GetSortedSnapshot();
            for (int i = 0; i < commands.Count; i++)
            {
                string name = commands[i].Name;
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    result.Add(name);
            }
        }

        if (service.CVars != null)
        {
            List<OUT_CVar> cvars = service.CVars.GetSortedSnapshot();
            for (int i = 0; i < cvars.Count; i++)
            {
                string name = cvars[i].Name;
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    result.Add(name);
            }
        }
    }

    private string GetCurrentToken(string value, out int start, out int end)
    {
        value ??= string.Empty;
        end = Mathf.Clamp(cursorIndex, 0, value.Length);
        start = end;

        while (start > 0 && !char.IsWhiteSpace(value[start - 1]))
            start--;

        while (end < value.Length && !char.IsWhiteSpace(value[end]))
            end++;

        return value.Substring(start, end - start);
    }

    private void ReplaceCurrentToken(string replacement, int start, int end)
    {
        string before = input.Substring(0, start);
        string after = input.Substring(end);
        input = before + replacement + after;
        cursorIndex = before.Length + replacement.Length;

        if (start == 0 && !input.EndsWith(" "))
        {
            input += " ";
            cursorIndex = input.Length;
        }
    }

    private string GetCommonPrefix(List<string> values)
    {
        if (values == null || values.Count == 0)
            return string.Empty;

        string prefix = values[0];
        for (int i = 1; i < values.Count; i++)
        {
            string value = values[i];
            int length = Mathf.Min(prefix.Length, value.Length);
            int j = 0;

            while (j < length && char.ToLowerInvariant(prefix[j]) == char.ToLowerInvariant(value[j]))
                j++;

            prefix = prefix.Substring(0, j);
            if (prefix.Length == 0)
                break;
        }

        return prefix;
    }

    private void PrintAutocompleteMatches(string prefix, List<string> matches)
    {
        builder.Length = 0;
        builder.Append("matches for '").Append(prefix).Append("': ");

        int count = Mathf.Min(matches.Count, Mathf.Max(1, maxAutocompleteResultsToPrint));
        for (int i = 0; i < count; i++)
        {
            if (i > 0)
                builder.Append(", ");
            builder.Append(matches[i]);
        }

        if (matches.Count > count)
            builder.Append(" ... +").Append(matches.Count - count);

        service.Log.Add(builder.ToString(), OUT_ConsoleLog.Level.System);
    }

    private void ToggleOpen()
    {
        if (lastToggleFrame == Time.frameCount)
            return;

        lastToggleFrame = Time.frameCount;
        SetOpen(!isOpen);
    }

    private void SetOpen(bool open)
    {
        if (isOpen == open)
            return;

        isOpen = open;
        RefreshAnyOpenFlag();

        if (isOpen)
        {
            cursorIndex = input.Length;
            historyIndex = -1;
        }

        if (pauseGameWhenOpen)
        {
            if (isOpen)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = previousTimeScale;
            }
        }
    }

    private static void RefreshAnyOpenFlag()
    {
        OUT_ConsoleOverlay[] overlays = FindObjectsOfType<OUT_ConsoleOverlay>();
        for (int i = 0; i < overlays.Length; i++)
        {
            if (overlays[i] != null && overlays[i].isActiveAndEnabled && overlays[i].isOpen)
            {
                AnyOpen = true;
                return;
            }
        }
        AnyOpen = false;
    }
}
