using System.Collections.Generic;
using System.Text;
using UnityEngine;

[DefaultExecutionOrder(-8700)]
public class OUT_ConsoleLog : MonoBehaviour
{
    public enum Level
    {
        Info = 0,
        Warning = 1,
        Error = 2,
        System = 3
    }

    public struct Entry
    {
        public float Time;
        public Level LogLevel;
        public string Message;
    }

    [SerializeField] [Min(64)] private int maxEntries = 512;
    [SerializeField] private bool mirrorUnityLogs = false;

    private readonly List<Entry> entries = new List<Entry>(512);
    private readonly StringBuilder builder = new StringBuilder(8192);

    public IReadOnlyList<Entry> Entries => entries;

    private void OnEnable()
    {
        if (mirrorUnityLogs)
            Application.logMessageReceived += HandleUnityLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleUnityLog;
    }

    public void Add(string message, Level level = Level.Info)
    {
        if (entries.Count >= Mathf.Max(64, maxEntries))
            entries.RemoveAt(0);

        entries.Add(new Entry
        {
            Time = Time.time,
            LogLevel = level,
            Message = message ?? string.Empty
        });
    }

    public void Clear()
    {
        entries.Clear();
    }

    public string BuildText(int maxLines)
    {
        builder.Length = 0;
        int start = Mathf.Max(0, entries.Count - Mathf.Max(1, maxLines));

        for (int i = start; i < entries.Count; i++)
        {
            Entry e = entries[i];
            builder.Append('[').Append(e.Time.ToString("0.00")).Append("] ");
            builder.Append(e.LogLevel).Append(" | ");
            builder.AppendLine(e.Message);
        }

        return builder.ToString();
    }

    private void HandleUnityLog(string condition, string stackTrace, LogType type)
    {
        Level level = Level.Info;
        if (type == LogType.Warning)
            level = Level.Warning;
        else if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            level = Level.Error;

        Add(condition, level);
    }
}
