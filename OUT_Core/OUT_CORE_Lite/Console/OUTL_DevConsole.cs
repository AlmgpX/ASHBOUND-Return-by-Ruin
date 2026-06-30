using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class OUTL_DevConsole : MonoBehaviour
{
    public static bool IsOpen { get; private set; }
    public static bool IsInputCaptured { get { return IsOpen; } }
    public static bool IsGamePausedByConsole { get; private set; }

    public KeyCode ToggleKey = KeyCode.BackQuote;
    public KeyCode[] AlternateToggleKeys = new[] { KeyCode.F1, KeyCode.Backslash, KeyCode.Insert };
    public int MaxLines = 28;
    public bool OpenOnStart;
    public bool PauseGameWhenOpen = true;
    public bool UnlockCursorWhenOpen = true;
    public bool RestoreCursorLockOnClose = true;
    public int MaxHistory = 64;

    [Header("Pick")]
    public float PickDistance = 1000f;
    public LayerMask PickMask = ~0;
    public QueryTriggerInteraction PickTriggers = QueryTriggerInteraction.Collide;
    public bool PickUsesCenterByDefault = true;
    public bool PickSkipsPlayerByDefault = true;
    public Camera PickCamera;
    public OUTL_BasicPlayerController PlayerController;
    public OUTL_PlayerInputSource PlayerInputSource;
    public bool UsePlayerViewCameraFallback = true;
    public string PlayerTargetName = "player";
    public string PlayerClassName = "player";

    [Header("Cheat Attack Profiles")]
    public OUTL_AttackProfile ImpulsePrimary;
    public OUTL_AttackProfile ImpulseSecondary;
    public OUTL_AttackProfile ImpulseMelee;

    private readonly List<string> lines = new List<string>(96);
    private readonly List<string> history = new List<string>(64);
    private readonly List<OUTL_EntityRuntime> entBuffer = new List<OUTL_EntityRuntime>(32);
    private readonly string[] commandHints =
    {
        "help", "outl.help", "outl.stats", "outl.perf", "outl.log", "outl.combatlog", "outl.logdump", "outl.unitylog",
        "ent_fire", "ent_find", "ent_dump",
        "outl.inspect", "outl.ai", "outl.aitrace", "outl.aiwatch", "outl.aiclearwatch",
        "outl.diary", "outl.diary_path", "outl.diary_clear",
        "outl.pick", "outl.pick_mouse", "outl.pick_self", "pick", "pick_mouse", "pick_center", "pick_self",
        "outl.send", "outl.kill", "outl.damage", "outl.save", "outl.load",
        "outl.tick", "outl.entitytick", "outl.tickbudget",
        "SV_Debug_Health", "sv_debug_health", "outl.debug.health", "SV_Debug_Health_Offset", "sv_debug_health_offset",
        "SV_Debug_Inventory", "sv_debug_inventory", "outl.debug.inventory",
        "outl.debug.hud", "outl.debug.container", "outl.debug.selected", "outl.debug.map", "sv_debug_map", "outl.debug.ledger", "outl.debug.stimuli",
        "sv_cheats", "sv_gravity", "sv_tick", "sv_tick_logic", "sv_tick_ai", "sv_tick_quest", "sv_tick_custom",
        "sv_tick_random", "sv_tick_stimulus", "sv_tick_materialize", "sv_tick_encounter", "sv_simstep",
        "sv_timescale", "sv_random_budget", "sv_npc_budget", "sv_route_budget", "sv_path_budget",
        "sv_npc_interrupt_budget", "sv_stimulus_budget", "god", "noclip", "impulse", "restart", "map_restart"
    };

    private string input = string.Empty;
    private Vector2 scroll;
    private int historyIndex = -1;
    private float previousUnityTimeScale = 1f;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;
    private bool closeRequestedThisFrame;

    private void OnEnable()
    {
        OUTL_DebugLog.OnLine += Log;
        Log("OUTL console ready. Type outl.help");
        if (OpenOnStart) SetOpen(true);
    }

    private void OnDisable()
    {
        OUTL_DebugLog.OnLine -= Log;
        if (IsOpen) SetOpen(false);
    }

    private void Update()
    {
        if (!IsOpen && WasTogglePressed()) SetOpen(true);
    }

    private void OnGUI()
    {
        if (!IsOpen) return;
        Event e = Event.current;
        if (e.type == EventType.KeyDown)
        {
            if (IsToggleKey(e.keyCode) || e.keyCode == KeyCode.Escape) { closeRequestedThisFrame = true; e.Use(); }
            else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) { RunInput(); e.Use(); }
            else if (e.keyCode == KeyCode.UpArrow) { HistoryUp(); e.Use(); }
            else if (e.keyCode == KeyCode.DownArrow) { HistoryDown(); e.Use(); }
            else if (e.keyCode == KeyCode.Tab) { CompleteInput(); e.Use(); }
        }
        if (closeRequestedThisFrame) { closeRequestedThisFrame = false; SetOpen(false); return; }

        int width = Screen.width;
        int height = Mathf.Min(400, Mathf.Max(220, Screen.height / 2));
        GUI.Box(new Rect(0, 0, width, height), "OUT CORE Lite Console  |  TAB autocomplete  |  UP history  |  ` / F1 close");
        GUILayout.BeginArea(new Rect(8, 24, width - 16, height - 32));
        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(height - 84));
        for (int i = 0; i < lines.Count; i++) GUILayout.Label(lines[i]);
        GUILayout.EndScrollView();
        GUILayout.BeginHorizontal();
        GUILayout.Label(">", GUILayout.Width(16));
        GUI.SetNextControlName("OUTLConsoleInput");
        input = GUILayout.TextField(input, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("Run", GUILayout.Width(70))) RunInput();
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        GUI.FocusControl("OUTLConsoleInput");
    }

    public void Log(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        lines.Add(text);
        while (lines.Count > MaxLines) lines.RemoveAt(0);
        scroll.y = 999999f;
    }

    private void SetOpen(bool value)
    {
        if (IsOpen == value) return;
        IsOpen = value;
        if (value)
        {
            previousUnityTimeScale = Time.timeScale;
            previousCursorLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            if (PauseGameWhenOpen) { Time.timeScale = 0f; IsGamePausedByConsole = true; }
            if (UnlockCursorWhenOpen) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            historyIndex = -1;
        }
        else
        {
            if (IsGamePausedByConsole) { Time.timeScale = previousUnityTimeScale <= 0f ? 1f : previousUnityTimeScale; IsGamePausedByConsole = false; }
            if (RestoreCursorLockOnClose) { Cursor.lockState = previousCursorLock; Cursor.visible = previousCursorVisible; }
        }
    }

    private void RunInput()
    {
        string cmd = input.Trim();
        input = string.Empty;
        historyIndex = -1;
        if (string.IsNullOrEmpty(cmd)) return;
        history.Add(cmd);
        while (history.Count > MaxHistory) history.RemoveAt(0);
        Log("> " + cmd);
        Execute(cmd);
    }

    private void HistoryUp()
    {
        if (history.Count == 0) return;
        if (historyIndex < 0) historyIndex = history.Count - 1;
        else historyIndex = Mathf.Max(0, historyIndex - 1);
        input = history[historyIndex];
    }

    private void HistoryDown()
    {
        if (history.Count == 0 || historyIndex < 0) return;
        historyIndex++;
        if (historyIndex >= history.Count) { historyIndex = -1; input = string.Empty; return; }
        input = history[historyIndex];
    }

    private void CompleteInput()
    {
        string raw = input.TrimStart();
        if (string.IsNullOrEmpty(raw)) { PrintHints(string.Empty); return; }
        string[] parts = raw.Split(' ');
        if (parts.Length > 1 && !raw.EndsWith(" ")) return;
        string prefix = parts[0].ToLowerInvariant();
        string match = null;
        int count = 0;
        for (int i = 0; i < commandHints.Length; i++)
        {
            if (!commandHints[i].StartsWith(prefix)) continue;
            if (match == null) match = commandHints[i];
            count++;
        }
        if (count == 1 && match != null) input = match + " ";
        else PrintHints(prefix);
    }

    private void PrintHints(string prefix)
    {
        string line = "commands:";
        for (int i = 0; i < commandHints.Length; i++)
            if (string.IsNullOrEmpty(prefix) || commandHints[i].StartsWith(prefix)) line += " " + commandHints[i];
        Log(line);
    }

    private void Execute(string commandLine)
    {
        string[] parts = Split(commandLine);
        if (parts.Length == 0) return;
        string cmd = parts[0].ToLowerInvariant();
        OUTL_World world = OUTL_World.Instance;

        if (cmd == "outl.help" || cmd == "help")
        {
            Log("outl.stats | outl.perf | outl.log [channel 0/1] | outl.logdump [n] | outl.unitylog 0/1");
            Log("outl.combatlog 0/1 toggles HIT/KILL/FIRE combat tracing");
            Log("ent_fire <targetname> <command> [floatValue] [delay] | ent_find <name> | ent_dump <id>");
            Log("outl.inspect <id> | outl.ai <id> | outl.aitrace 0/1 | outl.aiwatch <id|0>");
            Log("outl.diary <id> | outl.diary_path <id> | outl.diary_clear <id>");
            Log("pick/outl.pick = center ray, skips player | pick_self = player | pick_mouse = mouse ray");
            Log("outl.tick [logic|ai|quest|random] [seconds] | outl.entitytick <id> <seconds> | outl.tickbudget <n>");
            Log("sv_tick | sv_tick_logic/ai/quest/custom/random/stimulus/materialize/encounter [seconds] | sv_simstep [seconds]");
            Log("sv_timescale [value] | sv_random_budget/npc_budget/route_budget/path_budget/npc_interrupt_budget/stimulus_budget [count]");
            Log("SV_Debug_Health [0|1|2] toggles health labels | SV_Debug_Health_Offset <meters>");
            Log("SV_Debug_Inventory/outl.debug.hud [0|1|2] [entityId] toggles inventory/container/stats debug window");
            Log("outl.debug.selected [entityId|0] selects overlay focus | outl.debug.map/sv_debug_map [0|1|2] [range] shows entities/ledger/stimuli");
            Log("sv_cheats 0/1 | sv_gravity 981 | god | noclip | impulse 101 | restart/map_restart");
            return;
        }

        if (cmd == "outl.log") { HandleLogCommand(parts); return; }
        if (cmd == "outl.combatlog") { HandleCombatLogCommand(parts); return; }
        if (cmd == "outl.logdump") { PrintLogDump(parts.Length >= 2 ? ParseInt(parts[1]) : 32); return; }
        if (cmd == "outl.unitylog") { if (parts.Length >= 2) OUTL_DebugLog.MirrorToUnityConsole = ParseInt(parts[1]) != 0; Log("outl.unitylog = " + (OUTL_DebugLog.MirrorToUnityConsole ? "1" : "0")); return; }

        if (cmd == "sv_cheats") { if (parts.Length >= 2) OUTL_Cheats.SvCheats = ParseInt(parts[1]) != 0; Log("sv_cheats = " + (OUTL_Cheats.SvCheats ? "1" : "0")); return; }
        if (cmd == "sv_gravity")
        {
            if (parts.Length >= 2) OUTL_Cheats.SvGravity = Mathf.Max(0f, ParseFloat(parts[1]));
            if (world != null) world.ApplyGlobalGravity();
            else Physics.gravity = Vector3.down * OUTL_Cheats.UnityGravity;
            Log("sv_gravity = " + OUTL_Cheats.SvGravity + "  UnityY = -" + OUTL_Cheats.UnityGravity);
            return;
        }
        if (cmd == "sv_debug_health" || cmd == "outl.debug.health") { HandleDebugHealthCommand(parts); return; }
        if (cmd == "sv_debug_health_offset") { HandleDebugHealthOffsetCommand(parts); return; }
        if (cmd == "sv_debug_inventory" || cmd == "outl.debug.inventory" || cmd == "outl.debug.hud" || cmd == "outl.debug.container") { HandleDebugInventoryCommand(parts); return; }
        if (cmd == "outl.debug.selected") { HandleDebugSelectedCommand(parts); return; }
        if (cmd == "outl.debug.map" || cmd == "sv_debug_map" || cmd == "outl.debug.ledger" || cmd == "outl.debug.stimuli") { HandleDebugMapCommand(parts); return; }
        if (cmd == "god") { if (!RequireCheats()) return; OUTL_EntityAdapter player = FindPlayerEntity(); OUTL_Cheats.SetGod(!OUTL_Cheats.GodMode, player != null ? player.Id : OUTL_EntityId.None); Log("godmode " + (OUTL_Cheats.GodMode ? "ON" : "OFF")); return; }
        if (cmd == "noclip") { if (!RequireCheats()) return; OUTL_EntityAdapter player = FindPlayerEntity(); OUTL_Cheats.SetNoClip(!OUTL_Cheats.NoClip, player != null ? player.Id : OUTL_EntityId.None); Log("noclip " + (OUTL_Cheats.NoClip ? "ON" : "OFF")); return; }
        if (cmd == "impulse") { if (parts.Length >= 2 && parts[1] == "101") { if (!RequireCheats()) return; GiveImpulse101(); return; } Log("unknown impulse"); return; }
        if (cmd == "restart" || cmd == "map_restart") { Scene active = SceneManager.GetActiveScene(); Time.timeScale = previousUnityTimeScale <= 0f ? 1f : previousUnityTimeScale; IsGamePausedByConsole = false; SceneManager.LoadScene(active.buildIndex); return; }

        if (world == null) { Log("No OUTL_World in scene."); return; }

        if (HandleSvWorldCommand(cmd, parts, world)) return;

        if (cmd == "ent_fire") { HandleEntFire(parts, world); return; }
        if (cmd == "ent_find") { HandleEntFind(parts, world); return; }
        if (cmd == "ent_dump") { HandleEntDump(parts, world); return; }

        if (cmd == "outl.stats") { Log("entities=" + world.Registry.Count + " tickables=" + world.Scheduler.TickableCount + " random=" + world.Scheduler.RandomTickableCount + " queued=" + world.Commands.QueuedCount + " events=" + world.Events.PendingCount + " quests=" + world.Quests.QuestCount + " sv_cheats=" + (OUTL_Cheats.SvCheats ? 1 : 0) + " god=" + (OUTL_Cheats.GodMode ? 1 : 0) + " noclip=" + (OUTL_Cheats.NoClip ? 1 : 0) + " sv_gravity=" + OUTL_Cheats.SvGravity + " npcBudget=" + world.MaxNpcBehaviorTicksPerFrame + "/" + world.MaxNpcRouteUpdatesPerFrame + "/" + world.MaxNpcPathRequestsPerFrame + "/" + world.MaxNpcStimulusInterruptsPerFrame + " debug_health=" + OUTL_DebugSettings.DebugHealthMode + " debug_inventory=" + OUTL_DebugSettings.DebugInventoryMode + " debug_map=" + OUTL_DebugSettings.DebugMapMode + " aitrace=" + (OUTL_DebugLog.AITrace ? 1 : 0) + " aiwatch=" + OUTL_DebugLog.AIWatch.Value); return; }
        if (cmd == "outl.perf") { PrintPerf(); return; }
        if (cmd == "outl.aitrace") { if (parts.Length >= 2) OUTL_DebugLog.SetChannelEnabled(OUTL_DebugChannel.AI, ParseInt(parts[1]) != 0); Log("outl.aitrace = " + (OUTL_DebugLog.AITrace ? "1" : "0")); return; }
        if (cmd == "outl.aiwatch") { if (parts.Length >= 2) { int id = ParseInt(parts[1]); OUTL_DebugLog.AIWatch = id > 0 ? new OUTL_EntityId(id) : OUTL_EntityId.None; } Log("outl.aiwatch = " + (OUTL_DebugLog.AIWatch.IsValid ? OUTL_DebugLog.AIWatch.Value.ToString() : "all")); return; }
        if (cmd == "outl.aiclearwatch") { OUTL_DebugLog.AIWatch = OUTL_EntityId.None; Log("outl.aiwatch = all"); return; }
        if (cmd == "outl.pick" || cmd == "pick" || cmd == "pick_center") { PickEntityId(false, false); return; }
        if (cmd == "outl.pick_mouse" || cmd == "pick_mouse") { PickEntityId(true, false); return; }
        if (cmd == "outl.pick_self" || cmd == "pick_self") { PickSelf(); return; }

        if (cmd == "outl.diary" || cmd == "outl.diary_path" || cmd == "outl.diary_clear")
        {
            if (parts.Length < 2) { Log(cmd + " <id>"); return; }
            OUTL_EntityRuntime e;
            if (!world.Registry.TryGet(new OUTL_EntityId(ParseInt(parts[1])), out e) || e == null || e.Adapter == null) { Log("entity not found"); return; }
            OUTL_EntityDiary diary = e.Adapter.GetComponent<OUTL_EntityDiary>();
            if (diary == null) { Log("no OUTL_EntityDiary on entity " + e.Id); return; }
            if (cmd == "outl.diary_path") { Log(diary.FilePath); return; }
            if (cmd == "outl.diary_clear") { diary.ClearMemory(); Log("diary memory cleared " + e.Id); return; }
            Log("diary " + e.Id + " path=" + diary.FilePath);
            string dump = diary.DumpMemory();
            string[] dumpLines = dump.Split('\n');
            for (int i = 0; i < dumpLines.Length; i++) if (!string.IsNullOrEmpty(dumpLines[i])) Log(dumpLines[i]);
            return;
        }

        if (cmd == "outl.tick")
        {
            if (parts.Length < 3) { Log("logic=" + world.LogicTickInterval + " ai=" + world.AITickInterval + " quest=" + world.QuestTickInterval + " random=" + world.RandomTickInterval + " budget=" + world.RandomTickBudget); return; }
            float value = Mathf.Max(0.001f, ParseFloat(parts[2]));
            string lane = parts[1].ToLowerInvariant();
            if (lane == "logic") world.LogicTickInterval = value;
            else if (lane == "ai") world.AITickInterval = value;
            else if (lane == "quest") world.QuestTickInterval = value;
            else if (lane == "random") world.RandomTickInterval = value;
            else { Log("bad tick lane"); return; }
            Log("outl.tick " + lane + " = " + value);
            return;
        }

        if (cmd == "outl.tickbudget" && parts.Length >= 2) { world.RandomTickBudget = Mathf.Max(0, ParseInt(parts[1])); Log("RandomTickBudget = " + world.RandomTickBudget); return; }
        if (cmd == "outl.entitytick" && parts.Length >= 3) { OUTL_EntityRuntime e; if (!world.Registry.TryGet(new OUTL_EntityId(ParseInt(parts[1])), out e) || e == null || e.Adapter == null) { Log("entity not found"); return; } e.Adapter.TickInterval = Mathf.Max(0.01f, ParseFloat(parts[2])); Log("entity " + e.Id + " TickInterval = " + e.Adapter.TickInterval); return; }
        if (cmd == "outl.inspect" && parts.Length >= 2) { OUTL_EntityRuntime e; if (!world.Registry.TryGet(new OUTL_EntityId(ParseInt(parts[1])), out e) || e == null) { Log("entity not found"); return; } Log("id=" + e.Id + " def=" + (e.Def != null ? e.Def.name : "null") + " class=" + e.ClassName + " targetname=" + e.TargetName + " hp=" + e.Stats.Get(OUTL_StatId.Health, 0f) + " armor=" + e.Stats.Get(OUTL_StatId.Armor, 0f) + " faction=" + (e.Faction != null ? e.Faction.FactionId : "null")); return; }
        if (cmd == "outl.ai" && parts.Length >= 2) { OUTL_EntityRuntime e; if (!world.Registry.TryGet(new OUTL_EntityId(ParseInt(parts[1])), out e) || e == null || e.Adapter == null) { Log("entity not found"); return; } OUTL_AIActor ai = e.Adapter.GetComponent<OUTL_AIActor>(); if (ai == null) { Log("no OUTL_AIActor"); return; } Log("ai " + e.Id + " " + ai.DescribeThinking()); return; }
        if (cmd == "outl.kill" && parts.Length >= 2) { OUTL_EntityId id = new OUTL_EntityId(ParseInt(parts[1])); world.Events.Emit(new OUTL_Event(OUTL_EventType.Killed, OUTL_EntityId.None, id)); Log("killed " + id); return; }
        if (cmd == "outl.damage" && parts.Length >= 3) { OUTL_EntityId id = new OUTL_EntityId(ParseInt(parts[1])); float amount = ParseFloat(parts[2]); OUTL_Combat.ApplyDamage(OUTL_EntityId.None, id, amount, Vector3.zero, "console"); Log("damage " + id + " " + amount); return; }
        if (cmd == "outl.send" && parts.Length >= 4) { OUTL_CommandType type; if (!TryParseCommand(parts[1], out type)) { Log("bad command type"); return; } OUTL_EntityId source = new OUTL_EntityId(ParseInt(parts[2])); OUTL_EntityId target = new OUTL_EntityId(ParseInt(parts[3])); bool ok = world.Commands.Send(new OUTL_Command(type, source, target)); Log("send " + type + " -> " + ok); return; }
        if (cmd == "outl.save") { world.Save.SaveToFile(parts.Length >= 2 ? parts[1] : null); Log("saved"); return; }
        if (cmd == "outl.load") { world.Save.LoadFromFile(parts.Length >= 2 ? parts[1] : null); Log("loaded"); return; }

        Log("unknown command: " + commandLine);
    }

    private void HandleEntFire(string[] parts, OUTL_World world)
    {
        if (parts.Length < 3) { Log("usage: ent_fire <targetname> <command> [floatValue] [delay]"); return; }
        OUTL_CommandType type;
        if (!TryParseCommand(parts[2], out type)) { Log("bad command type: " + parts[2]); return; }
        float value = parts.Length >= 4 ? ParseFloat(parts[3]) : 0f;
        float delay = parts.Length >= 5 ? Mathf.Max(0f, ParseFloat(parts[4])) : 0f;
        OUTL_Command command = new OUTL_Command(type, OUTL_EntityId.None, OUTL_EntityId.None)
        {
            Key = "ent_fire",
            FloatValue = value,
            Point = Vector3.zero,
            Context = this
        };
        world.Commands.QueueToTargetName(parts[1], command, delay);
        Log("ent_fire " + parts[1] + " " + type + " value=" + value + " delay=" + delay + " queued=" + world.Commands.QueuedCount);
    }

    private void HandleEntFind(string[] parts, OUTL_World world)
    {
        if (parts.Length < 2) { Log("usage: ent_find <targetname|classname>"); return; }
        string name = parts[1];
        world.Registry.CopyByTargetName(name, entBuffer);
        int targetMatches = entBuffer.Count;
        for (int i = 0; i < entBuffer.Count; i++) Log(BuildEntityLine(entBuffer[i]));
        entBuffer.Clear();
        world.Registry.CopyByClassName(name, entBuffer);
        int classMatches = entBuffer.Count;
        for (int i = 0; i < entBuffer.Count; i++) Log(BuildEntityLine(entBuffer[i]));
        entBuffer.Clear();
        Log("ent_find " + name + " targetname=" + targetMatches + " classname=" + classMatches);
    }

    private void HandleEntDump(string[] parts, OUTL_World world)
    {
        if (parts.Length < 2) { Log("usage: ent_dump <id>"); return; }
        OUTL_EntityRuntime e;
        if (!world.Registry.TryGet(new OUTL_EntityId(ParseInt(parts[1])), out e) || e == null) { Log("entity not found"); return; }
        Log("id=" + e.Id + " stable=" + e.StableId + " class=" + e.ClassName + " targetname=" + e.TargetName);
        Log("target=" + e.Target + " killtarget=" + e.KillTarget + " def=" + (e.Def != null ? e.Def.name : "null") + " persistent=" + (e.SavePersistent ? 1 : 0));
        Log("pos=" + (e.Adapter != null ? e.Adapter.transform.position.ToString() : "null") + " tier=" + e.Tier + " hp=" + e.Stats.Get(OUTL_StatId.Health, 0f));
    }

    private string BuildEntityLine(OUTL_EntityRuntime e)
    {
        if (e == null) return "null entity";
        return "id=" + e.Id.Value + " class=" + e.ClassName + " targetname=" + e.TargetName + " name=" + (e.Adapter != null ? e.Adapter.name : "null");
    }

    private void HandleDebugHealthCommand(string[] parts)
    {
        if (parts.Length >= 2)
            OUTL_DebugSettings.SetDebugHealthMode(ParseInt(parts[1]));
        Log("SV_Debug_Health = " + OUTL_DebugSettings.DebugHealthMode + " distance=" + OUTL_DebugSettings.DebugHealthMaxDistance.ToString("0.#") + "m offset=" + OUTL_DebugSettings.DebugHealthVerticalOffset.ToString("0.##") + "m");
    }

    private void HandleDebugHealthOffsetCommand(string[] parts)
    {
        if (parts.Length >= 2)
            OUTL_DebugSettings.DebugHealthVerticalOffset = Mathf.Clamp(ParseFloat(parts[1]), -5f, 10f);
        Log("SV_Debug_Health_Offset = " + OUTL_DebugSettings.DebugHealthVerticalOffset.ToString("0.##") + "m");
    }

    private void HandleDebugInventoryCommand(string[] parts)
    {
        if (parts.Length >= 2)
            OUTL_DebugSettings.SetDebugInventoryMode(ParseInt(parts[1]));
        if (parts.Length >= 3)
        {
            int id = ParseInt(parts[2]);
            OUTL_DebugSettings.DebugInventoryEntityId = id > 0 ? new OUTL_EntityId(id) : OUTL_EntityId.None;
        }
        Log("SV_Debug_Inventory = " + OUTL_DebugSettings.DebugInventoryMode + " entity=" + (OUTL_DebugSettings.DebugInventoryEntityId.IsValid ? OUTL_DebugSettings.DebugInventoryEntityId.Value.ToString() : "auto"));
    }

    private void HandleDebugSelectedCommand(string[] parts)
    {
        if (parts.Length >= 2)
        {
            int id = ParseInt(parts[1]);
            OUTL_DebugSettings.DebugInventoryEntityId = id > 0 ? new OUTL_EntityId(id) : OUTL_EntityId.None;
            if (id > 0 && OUTL_DebugSettings.DebugInventoryMode == 0) OUTL_DebugSettings.SetDebugInventoryMode(2);
        }
        Log("outl.debug.selected = " + (OUTL_DebugSettings.DebugInventoryEntityId.IsValid ? OUTL_DebugSettings.DebugInventoryEntityId.Value.ToString() : "auto") + " hud=" + OUTL_DebugSettings.DebugInventoryMode);
    }

    private void HandleDebugMapCommand(string[] parts)
    {
        if (parts.Length >= 2)
            OUTL_DebugSettings.SetDebugMapMode(ParseInt(parts[1]));
        if (parts.Length >= 3)
            OUTL_DebugSettings.DebugMapRange = Mathf.Clamp(ParseFloat(parts[2]), 10f, 5000f);
        Log("outl.debug.map = " + OUTL_DebugSettings.DebugMapMode + " range=" + OUTL_DebugSettings.DebugMapRange.ToString("0.#") + "m layers=entities/ledger/stimuli");
    }

    private void HandleLogCommand(string[] parts)
    {
        if (parts.Length < 2)
        {
            Log(OUTL_DebugLog.BuildChannelSummary());
            Log("usage: outl.log <general|ai|stimulus|diary|events|combat|loot|save|perf> <0|1>");
            return;
        }

        if (parts.Length >= 3)
        {
            bool enabled = ParseInt(parts[2]) != 0;
            if (!OUTL_DebugLog.SetChannelEnabled(parts[1], enabled)) { Log("bad log channel: " + parts[1]); return; }
            Log("outl.log " + parts[1].ToLowerInvariant() + " = " + (enabled ? "1" : "0"));
            return;
        }

        Log(OUTL_DebugLog.BuildChannelSummary());
    }

    private bool HandleSvWorldCommand(string cmd, string[] parts, OUTL_World world)
    {
        if (cmd == "sv_tick") { PrintSvTick(world); return true; }
        if (cmd == "sv_tick_logic") { HandleFloatCommand(parts, "sv_tick_logic", world.LogicTickInterval, delegate(float v) { world.LogicTickInterval = v; }, 0.001f, 10f); return true; }
        if (cmd == "sv_tick_ai") { HandleFloatCommand(parts, "sv_tick_ai", world.AITickInterval, delegate(float v) { world.AITickInterval = v; }, 0.001f, 10f); return true; }
        if (cmd == "sv_tick_quest") { HandleFloatCommand(parts, "sv_tick_quest", world.QuestTickInterval, delegate(float v) { world.QuestTickInterval = v; }, 0.001f, 60f); return true; }
        if (cmd == "sv_tick_custom") { HandleFloatCommand(parts, "sv_tick_custom", world.CustomTickInterval, delegate(float v) { world.CustomTickInterval = v; }, 0.001f, 60f); return true; }
        if (cmd == "sv_tick_random") { HandleFloatCommand(parts, "sv_tick_random", world.RandomTickInterval, delegate(float v) { world.RandomTickInterval = v; }, 0.001f, 60f); return true; }
        if (cmd == "sv_tick_stimulus") { HandleFloatCommand(parts, "sv_tick_stimulus", world.StimulusTickInterval, delegate(float v) { world.StimulusTickInterval = v; }, 0.001f, 60f); return true; }
        if (cmd == "sv_tick_materialize") { HandleFloatCommand(parts, "sv_tick_materialize", world.MaterializationTickInterval, delegate(float v) { world.MaterializationTickInterval = v; }, 0.001f, 60f); return true; }
        if (cmd == "sv_tick_encounter") { HandleFloatCommand(parts, "sv_tick_encounter", world.AbstractEncounterTickInterval, delegate(float v) { world.AbstractEncounterTickInterval = v; }, 0.001f, 60f); return true; }
        if (cmd == "sv_simstep") { HandleFloatCommand(parts, "sv_simstep", world.SimulationStep, delegate(float v) { world.SimulationStep = v; }, 0.001f, 1f); return true; }
        if (cmd == "sv_timescale") { HandleFloatCommand(parts, "sv_timescale", world.TimeScale, delegate(float v) { world.TimeScale = v; }, 0f, 8f); return true; }
        if (cmd == "sv_random_budget") { HandleIntCommand(parts, "sv_random_budget", world.RandomTickBudget, delegate(int v) { world.RandomTickBudget = v; }, 0, 100000); return true; }
        if (cmd == "sv_npc_budget") { HandleIntCommand(parts, "sv_npc_budget", world.MaxNpcBehaviorTicksPerFrame, delegate(int v) { world.MaxNpcBehaviorTicksPerFrame = v; }, 0, 100000); return true; }
        if (cmd == "sv_route_budget") { HandleIntCommand(parts, "sv_route_budget", world.MaxNpcRouteUpdatesPerFrame, delegate(int v) { world.MaxNpcRouteUpdatesPerFrame = v; }, 0, 100000); return true; }
        if (cmd == "sv_path_budget") { HandleIntCommand(parts, "sv_path_budget", world.MaxNpcPathRequestsPerFrame, delegate(int v) { world.MaxNpcPathRequestsPerFrame = v; }, 0, 100000); return true; }
        if (cmd == "sv_npc_interrupt_budget") { HandleIntCommand(parts, "sv_npc_interrupt_budget", world.MaxNpcStimulusInterruptsPerFrame, delegate(int v) { world.MaxNpcStimulusInterruptsPerFrame = v; }, 0, 100000); return true; }
        if (cmd == "sv_stimulus_budget") { HandleIntCommand(parts, "sv_stimulus_budget", world.MaxStimuliProcessedPerFrame, delegate(int v) { world.MaxStimuliProcessedPerFrame = v; }, 0, 100000); return true; }
        return false;
    }

    private void PrintSvTick(OUTL_World world)
    {
        Log("sv_tick logic=" + world.LogicTickInterval + " ai=" + world.AITickInterval + " quest=" + world.QuestTickInterval + " custom=" + world.CustomTickInterval + " random=" + world.RandomTickInterval + " stimulus=" + world.StimulusTickInterval);
        Log("sv_tick materialize=" + world.MaterializationTickInterval + " encounter=" + world.AbstractEncounterTickInterval + " simstep=" + world.SimulationStep + " timescale=" + world.TimeScale);
        Log("sv_budget random=" + world.RandomTickBudget + " npc=" + world.MaxNpcBehaviorTicksPerFrame + " route=" + world.MaxNpcRouteUpdatesPerFrame + " path=" + world.MaxNpcPathRequestsPerFrame + " npcInterrupt=" + world.MaxNpcStimulusInterruptsPerFrame + " stimulus=" + world.MaxStimuliProcessedPerFrame);
    }

    private void HandleFloatCommand(string[] parts, string label, float current, System.Action<float> setter, float min, float max)
    {
        if (parts.Length >= 2)
        {
            current = Mathf.Clamp(ParseFloat(parts[1]), min, max);
            if (setter != null) setter(current);
        }
        Log(label + " = " + current);
    }

    private void HandleIntCommand(string[] parts, string label, int current, System.Action<int> setter, int min, int max)
    {
        if (parts.Length >= 2)
        {
            current = Mathf.Clamp(ParseInt(parts[1]), min, max);
            if (setter != null) setter(current);
        }
        Log(label + " = " + current);
    }

    private void HandleCombatLogCommand(string[] parts)
    {
        if (parts.Length >= 2)
            OUTL_DebugLog.SetChannelEnabled(OUTL_DebugChannel.Combat, ParseInt(parts[1]) != 0);
        Log("outl.combatlog = " + (OUTL_DebugLog.ShouldTraceCombat() ? "1" : "0") + "  use outl.logdump 32 for recent combat lines");
    }

    private void PrintLogDump(int count)
    {
        string dump = OUTL_DebugLog.DumpRecent(Mathf.Clamp(count, 1, 256));
        string[] dumpLines = dump.Split('\n');
        for (int i = 0; i < dumpLines.Length; i++) if (!string.IsNullOrEmpty(dumpLines[i])) Log(dumpLines[i]);
    }

    private void PrintPerf()
    {
        OUTL_FrameStats s = OUTL_Profile.LastFrame;
        Log("perf entities=" + s.Entities + " tickables=" + s.Tickables + " randomTickables=" + s.RandomTickables);
        Log("ticks full=" + s.FullTicks + " logic=" + s.LogicTicks + " ai=" + s.AITicks + " quest=" + s.QuestTicks + " random=" + s.RandomTicks);
        Log("events emitted=" + s.EventsEmitted + " flushed=" + s.EventsFlushed + " commands=" + s.CommandsSent + "/" + s.CommandsHandled + " queued=" + s.QueuedCommands + " effects=" + s.EffectsApplied);
        Log("physics raycasts=" + s.Raycasts + " overlaps=" + s.Overlaps + " pool spawn/release/miss=" + s.PoolSpawns + "/" + s.PoolReleases + "/" + s.PoolMisses + " despawns=" + s.Despawns);
        Log("save capture/restore entities=" + s.SaveEntities + "/" + s.RestoreEntities);
    }

    private void PickEntityId(bool useMousePosition, bool allowPlayer)
    {
        Camera cam = ResolvePickCamera();
        if (cam == null) { Log("pick failed: assign PickCamera or PlayerController.ViewCamera."); return; }
        OUTL_EntityAdapter player = FindPlayerEntity();
        OUTL_EntityId playerId = player != null ? player.Id : OUTL_EntityId.None;
        Vector3 screenPoint = useMousePosition ? Input.mousePosition : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        if (screenPoint.x < 0f || screenPoint.x > Screen.width || screenPoint.y < 0f || screenPoint.y > Screen.height) screenPoint = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Ray ray = cam.ScreenPointToRay(screenPoint);
        OUTL_Profile.Frame.Raycasts++;
        RaycastHit[] hits = Physics.RaycastAll(ray, PickDistance, PickMask, PickTriggers);
        if (hits == null || hits.Length == 0) { Log("pick: no physics hit from " + (useMousePosition ? "mouse" : "center")); return; }
        OUTL_EntityAdapter bestEntity = null;
        RaycastHit bestHit = default(RaycastHit);
        float bestDistance = float.MaxValue;
        string firstHitName = string.Empty;
        float firstHitDistance = float.MaxValue;
        bool skippedPlayer = false;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null) continue;
            if (hit.distance < firstHitDistance) { firstHitDistance = hit.distance; firstHitName = hit.collider.name; }
            OUTL_EntityAdapter entity = hit.collider.GetComponentInParent<OUTL_EntityAdapter>();
            if (entity == null || !entity.Id.IsValid) continue;
            if (!allowPlayer && PickSkipsPlayerByDefault && playerId.IsValid && entity.Id == playerId) { skippedPlayer = true; continue; }
            if (hit.distance < bestDistance) { bestDistance = hit.distance; bestEntity = entity; bestHit = hit; }
        }
        if (bestEntity == null) { Log(skippedPlayer ? "pick: only player entity was hit. Aim past your own collider or use pick_self." : "pick: hit " + firstHitName + " but no OUTL_EntityAdapter in parents"); return; }
        GUIUtility.systemCopyBuffer = bestEntity.Id.Value.ToString();
        string defName = bestEntity.Runtime != null && bestEntity.Runtime.Def != null ? bestEntity.Runtime.Def.name : "null";
        Log("pick id=" + bestEntity.Id.Value + " name=" + bestEntity.name + " def=" + defName + " targetname=" + (bestEntity.Runtime != null ? bestEntity.Runtime.TargetName : "") + " dist=" + bestDistance.ToString("0.00") + " hit=" + bestHit.collider.name + " copied-to-clipboard");
    }

    private void PickSelf()
    {
        OUTL_EntityAdapter player = FindPlayerEntity();
        if (player == null || !player.Id.IsValid) { Log("pick_self: player entity not found"); return; }
        GUIUtility.systemCopyBuffer = player.Id.Value.ToString();
        string defName = player.Runtime != null && player.Runtime.Def != null ? player.Runtime.Def.name : "null";
        Log("pick_self id=" + player.Id.Value + " name=" + player.name + " def=" + defName + " copied-to-clipboard");
    }

    private bool RequireCheats() { if (OUTL_Cheats.SvCheats) return true; Log("sv_cheats is 0"); return false; }

    private void GiveImpulse101()
    {
        OUTL_EntityAdapter player = FindPlayerEntity();
        if (player == null || player.Runtime == null) { Log("impulse 101 failed: player entity not found"); return; }
        player.Runtime.Stats.Set(OUTL_StatId.Health, 100f);
        player.Runtime.Stats.Set(OUTL_StatId.Armor, 100f);
        OUTL_AttackDriver attack = player.GetComponent<OUTL_AttackDriver>();
        if (attack == null)
        {
            Log("impulse 101: player has no OUTL_AttackDriver. Use Workbench repair/editor setup for the armed actor stack.");
            return;
        }
        attack.Source = player;
        Camera cam = ResolvePickCamera();
        if (attack.Muzzle == null && cam != null) attack.Muzzle = cam.transform;
        if (attack.AimCamera == null && cam != null) attack.AimCamera = cam;
        if (ImpulsePrimary != null) attack.Primary = ImpulsePrimary;
        if (ImpulseSecondary != null) attack.Secondary = ImpulseSecondary;
        if (ImpulseMelee != null) attack.Melee = ImpulseMelee;
        if (ImpulsePrimary == null && ImpulseSecondary == null && ImpulseMelee == null)
            Log("impulse 101: health=100 armor=100. No attack profiles assigned; author profiles in inspector/workbench.");
        else
            Log("impulse 101: health=100 armor=100 authored attack profiles assigned");
    }

    private Camera ResolvePickCamera()
    {
        if (PickCamera != null) return PickCamera;
        if (!UsePlayerViewCameraFallback) return null;
        OUTL_PlayerInputSource inputSource = ResolvePlayerInputSource();
        if (inputSource != null && inputSource.ViewCamera != null)
        {
            PickCamera = inputSource.ViewCamera;
            return PickCamera;
        }
        OUTL_BasicPlayerController controller = ResolvePlayerController();
        if (controller != null && controller.ViewCamera != null)
        {
            PickCamera = controller.ViewCamera;
            return PickCamera;
        }
        return null;
    }

    private OUTL_PlayerInputSource ResolvePlayerInputSource()
    {
        if (PlayerInputSource != null) return PlayerInputSource;
        OUTL_EntityAdapter player = FindPlayerEntity();
        if (player == null) return null;
        PlayerInputSource = player.GetComponent<OUTL_PlayerInputSource>();
        return PlayerInputSource;
    }

    private OUTL_BasicPlayerController ResolvePlayerController()
    {
        if (PlayerController != null) return PlayerController;
        OUTL_EntityAdapter player = FindPlayerEntity();
        if (player == null) return null;
        PlayerController = player.GetComponent<OUTL_BasicPlayerController>();
        return PlayerController;
    }

    private OUTL_EntityAdapter FindPlayerEntity()
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return null;
        OUTL_EntityRuntime named = null;
        if (!string.IsNullOrEmpty(PlayerTargetName))
            named = world.Registry.FindFirstByTargetName(PlayerTargetName);
        if (named == null && !string.IsNullOrEmpty(PlayerClassName))
            named = world.Registry.FindFirstByClassName(PlayerClassName);
        if (named != null && named.Adapter != null) return named.Adapter;

        world.Registry.CopyAll(entBuffer);
        for (int i = 0; i < entBuffer.Count; i++)
        {
            OUTL_EntityRuntime runtime = entBuffer[i];
            if (runtime == null || runtime.Adapter == null) continue;
            if (runtime.Adapter.GetComponent<OUTL_PlayerInputSource>() != null)
            {
                OUTL_EntityAdapter adapter = runtime.Adapter;
                entBuffer.Clear();
                return adapter;
            }
            if (runtime.Adapter.GetComponent<OUTL_BasicPlayerController>() != null)
            {
                OUTL_EntityAdapter adapter = runtime.Adapter;
                entBuffer.Clear();
                return adapter;
            }
        }
        entBuffer.Clear();
        return null;
    }

    private bool WasTogglePressed()
    {
        if (ToggleKey != KeyCode.None && Input.GetKeyDown(ToggleKey)) return true;
        if (AlternateToggleKeys == null) return false;
        for (int i = 0; i < AlternateToggleKeys.Length; i++)
            if (AlternateToggleKeys[i] != KeyCode.None && Input.GetKeyDown(AlternateToggleKeys[i]))
                return true;
        return false;
    }

    private bool IsToggleKey(KeyCode key)
    {
        if (key == ToggleKey) return true;
        if (AlternateToggleKeys == null) return false;
        for (int i = 0; i < AlternateToggleKeys.Length; i++)
            if (key == AlternateToggleKeys[i])
                return true;
        return false;
    }

    private static string[] Split(string text) { return text.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries); }
    private static int ParseInt(string text) { int v; return int.TryParse(text, out v) ? v : 0; }
    private static float ParseFloat(string text) { float v; return float.TryParse(text, out v) ? v : 0f; }
    private static bool TryParseCommand(string text, out OUTL_CommandType type) { return System.Enum.TryParse(text, true, out type); }
}
