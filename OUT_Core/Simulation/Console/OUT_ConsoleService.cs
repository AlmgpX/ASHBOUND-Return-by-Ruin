using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-8900)]
[DisallowMultipleComponent]
public class OUT_ConsoleService : MonoBehaviour
{
    public static OUT_ConsoleService Instance { get; private set; }

    [Header("Lifecycle")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool registerBuiltInsOnAwake = true;

    [Header("References")]
    [SerializeField] private OUT_ConsoleLog consoleLog;
    [SerializeField] private OUT_ConsoleCommandRegistry commandRegistry;
    [SerializeField] private OUT_CVarRegistry cvarRegistry;
    [SerializeField] private OUT_ConsoleConfig config;

    private readonly List<string> history = new List<string>(128);
    private readonly List<string> tokens = new List<string>(16);
    private readonly StringBuilder builder = new StringBuilder(1024);

    public OUT_ConsoleLog Log => consoleLog;
    public OUT_ConsoleCommandRegistry Commands => commandRegistry;
    public OUT_CVarRegistry CVars => cvarRegistry;
    public IReadOnlyList<string> History => history;
    public bool CheatsAllowed => cvarRegistry != null && cvarRegistry.GetBool("sv_cheats", false);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        EnsureComponents();
        RegisterCoreCVars();

        if (registerBuiltInsOnAwake)
            RegisterBuiltInCommands();
    }

    private void EnsureComponents()
    {
        if (consoleLog == null) consoleLog = GetComponent<OUT_ConsoleLog>() ?? gameObject.AddComponent<OUT_ConsoleLog>();
        if (commandRegistry == null) commandRegistry = GetComponent<OUT_ConsoleCommandRegistry>() ?? gameObject.AddComponent<OUT_ConsoleCommandRegistry>();
        if (cvarRegistry == null) cvarRegistry = GetComponent<OUT_CVarRegistry>() ?? gameObject.AddComponent<OUT_CVarRegistry>();
        if (config == null) config = GetComponent<OUT_ConsoleConfig>() ?? gameObject.AddComponent<OUT_ConsoleConfig>();
    }

    private void RegisterCoreCVars()
    {
        cvarRegistry.RegisterBool("sv_cheats", false, OUT_CVarFlags.Archive, "Allows protected development commands.");
        cvarRegistry.RegisterFloat("host_timescale", 1f, OUT_CVarFlags.Protected, "Runtime Time.timeScale multiplier.", c => Time.timeScale = Mathf.Max(0f, c.FloatValue));
        cvarRegistry.RegisterBool("con_echo", true, OUT_CVarFlags.Archive, "Echo typed commands to console log.");
        cvarRegistry.RegisterBool("ai_debug_log", false, OUT_CVarFlags.Archive, "Toggle OUT_AIDebugLogService capture when present.", ApplyAIDebugLogCVar);
    }

    private void ApplyAIDebugLogCVar(OUT_CVar cvar)
    {
        OUT_AIDebugLogService service = FindObjectOfType<OUT_AIDebugLogService>();
        if (service != null) service.CaptureLogs = cvar.BoolValue;
    }

    private void RegisterBuiltInCommands()
    {
        commandRegistry.Register("help", CmdHelp, "Lists commands or describes one command.", "help [command]");
        commandRegistry.Register("clear", c => consoleLog.Clear(), "Clears console log.", "clear");
        commandRegistry.Register("echo", c => consoleLog.Add(c.JoinArgs(0)), "Prints text.", "echo <text>");
        commandRegistry.Register("cvar.list", CmdCVarList, "Lists console variables.", "cvar.list");
        commandRegistry.Register("cvar.get", CmdCVarGet, "Prints a cvar value.", "cvar.get <name>");
        commandRegistry.Register("cvar.set", CmdCVarSet, "Sets a cvar value.", "cvar.set <name> <value>");
        commandRegistry.Register("exec", CmdExec, "Loads console config from persistent data folder.", "exec [file]");
        commandRegistry.Register("cfg.save", CmdSaveConfig, "Saves archived cvars.", "cfg.save [file]");
        commandRegistry.Register("scene.reload", c => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex), "Reloads current scene.", "scene.reload", true);
        commandRegistry.Register("scene.load", CmdLoadScene, "Loads scene by name.", "scene.load <sceneName>", true);
        commandRegistry.Register("time.scale", CmdTimeScale, "Sets Time.timeScale.", "time.scale <value>", true);
        commandRegistry.Register("ai.log.save", CmdAILogSave, "Saves AI debug log.", "ai.log.save");
        commandRegistry.Register("ai.log.clear", CmdAILogClear, "Clears AI debug log.", "ai.log.clear");
        commandRegistry.Register("ai.inspect", CmdAIInspect, "Prints AI brain state and tactical intent.", "ai.inspect <objectName>");
        commandRegistry.Register("ai.intent", CmdAIIntent, "Prints soldier tactical resolver intent.", "ai.intent <objectName>");
        commandRegistry.Register("impulse", CmdImpulse, "Half-Life style impulse command gateway.", "impulse <number>", true);
        commandRegistry.Register("impulse101", c => RunImpulse101(), "Half-Life style developer gift command.", "impulse101", true);
    }

    public void ExecuteLine(string line, bool echo = true)
    {
        if (string.IsNullOrWhiteSpace(line)) return;

        line = line.Trim();
        AddHistory(line);

        if (echo && cvarRegistry.GetBool("con_echo", true))
            consoleLog.Add("> " + line, OUT_ConsoleLog.Level.System);

        tokens.Clear();
        Tokenize(line, tokens);
        if (tokens.Count == 0) return;

        string name = tokens[0];
        string[] args = new string[Mathf.Max(0, tokens.Count - 1)];
        for (int i = 1; i < tokens.Count; i++) args[i - 1] = tokens[i];

        if (commandRegistry.TryGet(name, out OUT_ConsoleCommand command))
        {
            if (command.Protected && !CheatsAllowed)
            {
                consoleLog.Add($"command '{name}' is protected. Set sv_cheats 1 first.", OUT_ConsoleLog.Level.Warning);
                return;
            }

            try
            {
                command.Execute(new OUT_ConsoleCommandContext { Service = this, RawLine = line, CommandName = name, Args = args });
            }
            catch (Exception ex)
            {
                consoleLog.Add($"command '{name}' failed: {ex.Message}", OUT_ConsoleLog.Level.Error);
            }
            return;
        }

        if (cvarRegistry.Exists(name))
        {
            if (args.Length == 0)
            {
                cvarRegistry.TryGet(name, out OUT_CVar cvar);
                consoleLog.Add($"{cvar.Name} = {cvar.Value}");
            }
            else
            {
                string value = JoinArray(args, 0);
                if (cvarRegistry.TrySet(name, value, CheatsAllowed, out string result)) consoleLog.Add(result);
                else consoleLog.Add(result, OUT_ConsoleLog.Level.Warning);
            }
            return;
        }

        consoleLog.Add($"unknown command or cvar: {name}", OUT_ConsoleLog.Level.Warning);
    }

    public void RegisterCommand(string name, Action<OUT_ConsoleCommandContext> execute, string help = "", string usage = "", bool isProtected = false)
    {
        commandRegistry.Register(name, execute, help, usage, isProtected);
    }

    private void CmdHelp(OUT_ConsoleCommandContext ctx)
    {
        if (ctx.Count > 0 && commandRegistry.TryGet(ctx.Arg(0), out OUT_ConsoleCommand command))
        {
            consoleLog.Add($"{command.Name}: {command.Help} usage: {command.Usage}");
            return;
        }

        builder.Length = 0;
        builder.AppendLine("commands:");
        foreach (OUT_ConsoleCommand cmd in commandRegistry.GetSortedSnapshot()) builder.Append("  ").Append(cmd.Name).Append(" - ").AppendLine(cmd.Help);
        consoleLog.Add(builder.ToString());
    }

    private void CmdCVarList(OUT_ConsoleCommandContext ctx)
    {
        builder.Length = 0;
        builder.AppendLine("cvars:");
        foreach (OUT_CVar cvar in cvarRegistry.GetSortedSnapshot()) builder.Append("  ").Append(cvar.Name).Append(" = ").Append(cvar.Value).Append("  ").AppendLine(cvar.Help);
        consoleLog.Add(builder.ToString());
    }

    private void CmdCVarGet(OUT_ConsoleCommandContext ctx)
    {
        if (ctx.Count < 1) { consoleLog.Add("usage: cvar.get <name>", OUT_ConsoleLog.Level.Warning); return; }
        if (cvarRegistry.TryGet(ctx.Arg(0), out OUT_CVar cvar)) consoleLog.Add($"{cvar.Name} = {cvar.Value}");
        else consoleLog.Add($"unknown cvar '{ctx.Arg(0)}'", OUT_ConsoleLog.Level.Warning);
    }

    private void CmdCVarSet(OUT_ConsoleCommandContext ctx)
    {
        if (ctx.Count < 2) { consoleLog.Add("usage: cvar.set <name> <value>", OUT_ConsoleLog.Level.Warning); return; }
        string value = ctx.JoinArgs(1);
        if (cvarRegistry.TrySet(ctx.Arg(0), value, CheatsAllowed, out string result)) consoleLog.Add(result);
        else consoleLog.Add(result, OUT_ConsoleLog.Level.Warning);
    }

    private void CmdExec(OUT_ConsoleCommandContext ctx)
    {
        config.LoadLinesIntoConsole(this, consoleLog, ctx.Count > 0 ? ctx.Arg(0) : null);
    }

    private void CmdSaveConfig(OUT_ConsoleCommandContext ctx)
    {
        config.SaveArchivedCVars(cvarRegistry, consoleLog, ctx.Count > 0 ? ctx.Arg(0) : null);
    }

    private void CmdLoadScene(OUT_ConsoleCommandContext ctx)
    {
        if (ctx.Count < 1) { consoleLog.Add("usage: scene.load <sceneName>", OUT_ConsoleLog.Level.Warning); return; }
        SceneManager.LoadScene(ctx.Arg(0));
    }

    private void CmdTimeScale(OUT_ConsoleCommandContext ctx)
    {
        if (ctx.Count < 1 || !float.TryParse(ctx.Arg(0), out float scale)) { consoleLog.Add("usage: time.scale <value>", OUT_ConsoleLog.Level.Warning); return; }
        Time.timeScale = Mathf.Max(0f, scale);
        cvarRegistry.TrySet("host_timescale", Time.timeScale.ToString("0.########"), true, out _);
        consoleLog.Add($"Time.timeScale = {Time.timeScale:0.###}");
    }

    private void CmdAILogSave(OUT_ConsoleCommandContext ctx)
    {
        OUT_AIDebugLogService service = FindObjectOfType<OUT_AIDebugLogService>();
        if (service == null) { consoleLog.Add("OUT_AIDebugLogService not found", OUT_ConsoleLog.Level.Warning); return; }
        service.SaveToFile();
        consoleLog.Add("AI log saved");
    }

    private void CmdAILogClear(OUT_ConsoleCommandContext ctx)
    {
        OUT_AIDebugLogService service = FindObjectOfType<OUT_AIDebugLogService>();
        if (service == null) { consoleLog.Add("OUT_AIDebugLogService not found", OUT_ConsoleLog.Level.Warning); return; }
        service.Clear();
        consoleLog.Add("AI log cleared");
    }

    private void CmdAIInspect(OUT_ConsoleCommandContext ctx)
    {
        if (!TryFindAIObject(ctx, out GameObject go)) return;

        OUT_AIActorBrain brain = go.GetComponentInChildren<OUT_AIActorBrain>();
        if (brain == null) { consoleLog.Add($"AI brain not found on {go.name}", OUT_ConsoleLog.Level.Warning); return; }

        OUT_SoldierScheduleResolver resolver = go.GetComponentInChildren<OUT_SoldierScheduleResolver>();
        string intent = resolver != null ? $" intent:{resolver.LastIntent} reason:{resolver.LastIntentReason}" : string.Empty;
        consoleLog.Add($"AI {go.name} state:{brain.CurrentState} conditions:{brain.Conditions} enemy:{(brain.Blackboard.Enemy != null ? brain.Blackboard.Enemy.name : "none")} interest:{brain.Blackboard.InterestStrength:0.00}{intent}");
    }

    private void CmdAIIntent(OUT_ConsoleCommandContext ctx)
    {
        if (!TryFindAIObject(ctx, out GameObject go)) return;

        OUT_SoldierScheduleResolver resolver = go.GetComponentInChildren<OUT_SoldierScheduleResolver>();
        if (resolver == null) { consoleLog.Add($"OUT_SoldierScheduleResolver not found on {go.name}", OUT_ConsoleLog.Level.Warning); return; }

        consoleLog.Add($"AI intent {go.name}: {resolver.LastIntent} reason:{resolver.LastIntentReason}");
    }

    private bool TryFindAIObject(OUT_ConsoleCommandContext ctx, out GameObject go)
    {
        go = null;
        if (ctx.Count < 1) { consoleLog.Add($"usage: {ctx.CommandName} <objectName>", OUT_ConsoleLog.Level.Warning); return false; }
        go = GameObject.Find(ctx.Arg(0));
        if (go == null) { consoleLog.Add($"object not found: {ctx.Arg(0)}", OUT_ConsoleLog.Level.Warning); return false; }
        return true;
    }

    private void CmdImpulse(OUT_ConsoleCommandContext ctx)
    {
        if (ctx.Count < 1 || !int.TryParse(ctx.Arg(0), out int code)) { consoleLog.Add("usage: impulse <number>", OUT_ConsoleLog.Level.Warning); return; }
        if (code == 101) { RunImpulse101(); return; }
        consoleLog.Add($"impulse {code}: no handler registered", OUT_ConsoleLog.Level.Warning);
    }

    private void RunImpulse101()
    {
        consoleLog.Add("impulse 101: developer gift hook fired. Register game-side handler for weapons/ammo.", OUT_ConsoleLog.Level.System);
        BroadcastMessage("OUT_OnImpulse101", SendMessageOptions.DontRequireReceiver);
    }

    private void AddHistory(string line)
    {
        if (history.Count == 0 || history[history.Count - 1] != line) history.Add(line);
        if (history.Count > 128) history.RemoveAt(0);
    }

    private static string JoinArray(string[] values, int start)
    {
        if (values == null || start >= values.Length) return string.Empty;
        StringBuilder b = new StringBuilder();
        for (int i = start; i < values.Length; i++) { if (i > start) b.Append(' '); b.Append(values[i]); }
        return b.ToString();
    }

    private static void Tokenize(string line, List<string> output)
    {
        bool quoted = false;
        StringBuilder token = new StringBuilder();
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"') { quoted = !quoted; continue; }
            if (!quoted && char.IsWhiteSpace(c))
            {
                if (token.Length > 0) { output.Add(token.ToString()); token.Length = 0; }
                continue;
            }
            token.Append(c);
        }
        if (token.Length > 0) output.Add(token.ToString());
    }
}
