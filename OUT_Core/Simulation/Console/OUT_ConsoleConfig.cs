using System.IO;
using System.Text;
using UnityEngine;

[DefaultExecutionOrder(-8500)]
public class OUT_ConsoleConfig : MonoBehaviour
{
    [SerializeField] private string configFolder = "OUT";
    [SerializeField] private string defaultConfigFile = "console.cfg";

    public string DefaultConfigPath => Path.Combine(Application.persistentDataPath, configFolder, defaultConfigFile);

    public void SaveArchivedCVars(OUT_CVarRegistry registry, OUT_ConsoleLog log, string path = null)
    {
        if (registry == null)
            return;

        path = string.IsNullOrWhiteSpace(path) ? DefaultConfigPath : ResolvePath(path);
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        StringBuilder builder = new StringBuilder(4096);
        foreach (OUT_CVar cvar in registry.GetSortedSnapshot())
        {
            if ((cvar.Flags & OUT_CVarFlags.Archive) == 0)
                continue;

            builder.Append(cvar.Name).Append(' ');
            AppendEscaped(builder, cvar.Value);
            builder.AppendLine();
        }

        File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
        log?.Add($"saved config: {path}", OUT_ConsoleLog.Level.System);
    }

    public void LoadLinesIntoConsole(OUT_ConsoleService service, OUT_ConsoleLog log, string path)
    {
        if (service == null)
            return;

        path = string.IsNullOrWhiteSpace(path) ? DefaultConfigPath : ResolvePath(path);
        if (!File.Exists(path))
        {
            log?.Add($"config not found: {path}", OUT_ConsoleLog.Level.Warning);
            return;
        }

        string[] lines = File.ReadAllLines(path, Encoding.UTF8);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("//") || line.StartsWith("#"))
                continue;

            service.ExecuteLine(line, false);
        }

        log?.Add($"loaded config: {path}", OUT_ConsoleLog.Level.System);
    }

    private string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
            return path;

        return Path.Combine(Application.persistentDataPath, configFolder, path);
    }

    private void AppendEscaped(StringBuilder builder, string value)
    {
        if (value == null)
        {
            builder.Append("\"\"");
            return;
        }

        bool needsQuotes = value.IndexOfAny(new[] { ' ', '\t', '"' }) >= 0;
        if (!needsQuotes)
        {
            builder.Append(value);
            return;
        }

        builder.Append('"');
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c == '"' || c == '\\')
                builder.Append('\\');
            builder.Append(c);
        }
        builder.Append('"');
    }
}
