using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public sealed class OUT_ConsoleCommandContext
{
    public OUT_ConsoleService Service;
    public string RawLine;
    public string CommandName;
    public string[] Args;

    public int Count => Args != null ? Args.Length : 0;

    public string Arg(int index, string fallback = "")
    {
        if (Args == null || index < 0 || index >= Args.Length)
            return fallback;

        return Args[index];
    }

    public string JoinArgs(int startIndex = 0)
    {
        if (Args == null || startIndex >= Args.Length)
            return string.Empty;

        StringBuilder builder = new StringBuilder();
        for (int i = startIndex; i < Args.Length; i++)
        {
            if (i > startIndex)
                builder.Append(' ');
            builder.Append(Args[i]);
        }
        return builder.ToString();
    }
}

public sealed class OUT_ConsoleCommand
{
    public readonly string Name;
    public readonly string Help;
    public readonly string Usage;
    public readonly bool Protected;
    public readonly Action<OUT_ConsoleCommandContext> Execute;

    public OUT_ConsoleCommand(string name, string help, string usage, bool isProtected, Action<OUT_ConsoleCommandContext> execute)
    {
        Name = name;
        Help = help ?? string.Empty;
        Usage = usage ?? string.Empty;
        Protected = isProtected;
        Execute = execute;
    }
}

[DefaultExecutionOrder(-8600)]
public class OUT_ConsoleCommandRegistry : MonoBehaviour
{
    private readonly Dictionary<string, OUT_ConsoleCommand> commands = new Dictionary<string, OUT_ConsoleCommand>(StringComparer.OrdinalIgnoreCase);
    private readonly List<OUT_ConsoleCommand> sortedCache = new List<OUT_ConsoleCommand>(128);
    private bool cacheDirty = true;

    public bool Register(string name, Action<OUT_ConsoleCommandContext> execute, string help = "", string usage = "", bool isProtected = false)
    {
        if (string.IsNullOrWhiteSpace(name) || execute == null)
            return false;

        name = name.Trim();
        if (commands.ContainsKey(name))
            return false;

        commands.Add(name, new OUT_ConsoleCommand(name, help, usage, isProtected, execute));
        cacheDirty = true;
        return true;
    }

    public bool TryGet(string name, out OUT_ConsoleCommand command)
    {
        if (name == null)
        {
            command = null;
            return false;
        }

        return commands.TryGetValue(name.Trim(), out command);
    }

    public List<OUT_ConsoleCommand> GetSortedSnapshot()
    {
        if (!cacheDirty)
            return sortedCache;

        sortedCache.Clear();
        foreach (KeyValuePair<string, OUT_ConsoleCommand> pair in commands)
            sortedCache.Add(pair.Value);

        sortedCache.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        cacheDirty = false;
        return sortedCache;
    }
}
