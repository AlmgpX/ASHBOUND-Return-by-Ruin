using UnityEngine;

[DefaultExecutionOrder(-6300)]
[DisallowMultipleComponent]
public class OUT_SaveConsoleCommands : MonoBehaviour
{
    [Header("Lifecycle")]
    [SerializeField] private bool registerOnStart = true;

    [Header("Hotkeys")]
    [SerializeField] private bool enableHotkeys = true;
    [SerializeField] private KeyCode quickSaveKey = KeyCode.F5;
    [SerializeField] private KeyCode quickLoadKey = KeyCode.F9;
    [SerializeField] private bool allowHotkeysWhenConsoleOpen = true;
    [SerializeField] private bool logHotkeyOperations = true;

    private OUT_ConsoleService cachedConsole;

    private void Start()
    {
        if (registerOnStart)
            RegisterCommands();
    }

    private void Update()
    {
        if (!enableHotkeys)
            return;

        if (!allowHotkeysWhenConsoleOpen && OUT_ConsoleOverlay.AnyOpen)
            return;

        if (Input.GetKeyDown(quickSaveKey))
            QuickSaveFromHotkey();

        if (Input.GetKeyDown(quickLoadKey))
            QuickLoadFromHotkey();
    }

    public void RegisterCommands()
    {
        OUT_ConsoleService console = ResolveConsole();
        if (console == null)
            return;

        // Save/load are not cheats. Civilization barely survived, but at least F5 should not require sv_cheats.
        console.RegisterCommand("save.quick", CmdSaveQuick, "Saves current scene state into quick slot.", "save.quick");
        console.RegisterCommand("load.quick", CmdLoadQuick, "Loads current scene state from quick slot.", "load.quick");
        console.RegisterCommand("save.slot", CmdSaveSlot, "Saves current scene state into a named slot.", "save.slot <slot>");
        console.RegisterCommand("load.slot", CmdLoadSlot, "Loads current scene state from a named slot.", "load.slot <slot>");
        console.RegisterCommand("save.path", CmdSavePath, "Prints world save directory.", "save.path");
        console.RegisterCommand("egregore.list", CmdEgregoreList, "Lists egregore zones.", "egregore.list");
        console.RegisterCommand("egregore.save", CmdEgregoreSave, "Writes egregore text snapshots.", "egregore.save");
        console.RegisterCommand("egregore.clearledger", CmdEgregoreClearLedger, "Clears all egregore event ledgers.", "egregore.clearledger", true);
    }

    private OUT_ConsoleService ResolveConsole()
    {
        if (cachedConsole != null)
            return cachedConsole;

        cachedConsole = OUT_ConsoleService.Instance;
        if (cachedConsole == null)
            cachedConsole = FindObjectOfType<OUT_ConsoleService>();

        return cachedConsole;
    }

    private void QuickSaveFromHotkey()
    {
        string path = OUT_WorldSaveService.EnsureExists().Save();
        if (!logHotkeyOperations)
            return;

        OUT_ConsoleService console = ResolveConsole();
        if (console != null && console.Log != null)
            console.Log.Add("F5 quicksave: " + path, OUT_ConsoleLog.Level.System);
        else
            Debug.Log("F5 quicksave: " + path);
    }

    private void QuickLoadFromHotkey()
    {
        bool ok = OUT_WorldSaveService.EnsureExists().Load();
        if (!logHotkeyOperations)
            return;

        OUT_ConsoleService console = ResolveConsole();
        if (console != null && console.Log != null)
            console.Log.Add(ok ? "F9 quickload: loaded quick save" : "F9 quickload failed", ok ? OUT_ConsoleLog.Level.System : OUT_ConsoleLog.Level.Warning);
        else
            Debug.Log(ok ? "F9 quickload: loaded quick save" : "F9 quickload failed");
    }

    private void CmdSaveQuick(OUT_ConsoleCommandContext ctx)
    {
        string path = OUT_WorldSaveService.EnsureExists().Save();
        ctx.Service.Log.Add("saved: " + path);
    }

    private void CmdLoadQuick(OUT_ConsoleCommandContext ctx)
    {
        bool ok = OUT_WorldSaveService.EnsureExists().Load();
        ctx.Service.Log.Add(ok ? "loaded quick save" : "quick save load failed", ok ? OUT_ConsoleLog.Level.System : OUT_ConsoleLog.Level.Warning);
    }

    private void CmdSaveSlot(OUT_ConsoleCommandContext ctx)
    {
        if (ctx.Count < 1)
        {
            ctx.Service.Log.Add("usage: save.slot <slot>", OUT_ConsoleLog.Level.Warning);
            return;
        }

        string path = OUT_WorldSaveService.EnsureExists().Save(ctx.Arg(0));
        ctx.Service.Log.Add("saved: " + path);
    }

    private void CmdLoadSlot(OUT_ConsoleCommandContext ctx)
    {
        if (ctx.Count < 1)
        {
            ctx.Service.Log.Add("usage: load.slot <slot>", OUT_ConsoleLog.Level.Warning);
            return;
        }

        bool ok = OUT_WorldSaveService.EnsureExists().Load(ctx.Arg(0));
        ctx.Service.Log.Add(ok ? "loaded: " + ctx.Arg(0) : "load failed: " + ctx.Arg(0), ok ? OUT_ConsoleLog.Level.System : OUT_ConsoleLog.Level.Warning);
    }

    private void CmdSavePath(OUT_ConsoleCommandContext ctx)
    {
        ctx.Service.Log.Add(OUT_WorldSaveService.EnsureExists().GetSaveDirectory());
    }

    private void CmdEgregoreList(OUT_ConsoleCommandContext ctx)
    {
        OUT_EgregoreZone[] zones = FindObjectsOfType<OUT_EgregoreZone>(true);
        if (zones.Length == 0)
        {
            ctx.Service.Log.Add("no egregore zones found", OUT_ConsoleLog.Level.Warning);
            return;
        }

        for (int i = 0; i < zones.Length; i++)
        {
            OUT_EgregoreZone z = zones[i];
            OUT_EgregoreState s = z.State;
            ctx.Service.Log.Add(z.EgregoreId + " dominant:" + z.DominantForce + " threat:" + s.Threat.ToString("0.00") + " fear:" + s.Fear.ToString("0.00") + " sacred:" + s.Sacred.ToString("0.00") + " corruption:" + s.Corruption.ToString("0.00"));
        }
    }

    private void CmdEgregoreSave(OUT_ConsoleCommandContext ctx)
    {
        OUT_EgregoreZone[] zones = FindObjectsOfType<OUT_EgregoreZone>(true);
        for (int i = 0; i < zones.Length; i++)
            zones[i].SaveTextSnapshot();

        OUT_EgregoreEventLedger[] ledgers = FindObjectsOfType<OUT_EgregoreEventLedger>(true);
        for (int i = 0; i < ledgers.Length; i++)
            ledgers[i].SaveTextSnapshot();

        ctx.Service.Log.Add("egregore snapshots saved zones:" + zones.Length + " ledgers:" + ledgers.Length);
    }

    private void CmdEgregoreClearLedger(OUT_ConsoleCommandContext ctx)
    {
        OUT_EgregoreEventLedger[] ledgers = FindObjectsOfType<OUT_EgregoreEventLedger>(true);
        for (int i = 0; i < ledgers.Length; i++)
            ledgers[i].ClearLedger();

        ctx.Service.Log.Add("egregore ledgers cleared: " + ledgers.Length);
    }
}
