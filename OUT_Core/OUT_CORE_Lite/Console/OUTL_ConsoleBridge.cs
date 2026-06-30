using UnityEngine;

[DisallowMultipleComponent]
public class OUTL_ConsoleBridge : MonoBehaviour
{
    public OUTL_EntityDef[] SpawnCatalog;

    private void Start()
    {
        if (OUT_ConsoleService.Instance == null) return;
        OUT_ConsoleService.Instance.RegisterCommand("outl.stats", CmdStats, "Prints OUT CORE Lite stats.", "outl.stats");
        OUT_ConsoleService.Instance.RegisterCommand("outl.spawn", CmdSpawn, "Spawns OUTL entity from the assigned SpawnCatalog.", "outl.spawn <entityDefNameOrClassName>", true);
        OUT_ConsoleService.Instance.RegisterCommand("outl.inspect", CmdInspect, "Inspects OUTL entity by id.", "outl.inspect <id>");
        OUT_ConsoleService.Instance.RegisterCommand("outl.send", CmdSend, "Sends OUTL command by ids.", "outl.send <cmd> <sourceId> <targetId>", true);
        OUT_ConsoleService.Instance.RegisterCommand("outl.quest", CmdQuest, "Sets OUTL quest stage.", "outl.quest <questId> <stage>", true);
        OUT_ConsoleService.Instance.RegisterCommand("outl.save", CmdSave, "Saves OUTL world.", "outl.save [path]", true);
        OUT_ConsoleService.Instance.RegisterCommand("outl.load", CmdLoad, "Loads OUTL world.", "outl.load [path]", true);
        OUT_ConsoleService.Instance.RegisterCommand("SV_Debug_Health", CmdDebugHealth, "Toggles OUTL world-space health labels.", "SV_Debug_Health [0|1|2]");
        OUT_ConsoleService.Instance.RegisterCommand("sv_debug_health", CmdDebugHealth, "Toggles OUTL world-space health labels.", "sv_debug_health [0|1|2]");
        OUT_ConsoleService.Instance.RegisterCommand("outl.debug.health", CmdDebugHealth, "Toggles OUTL world-space health labels.", "outl.debug.health [0|1|2]");
        OUT_ConsoleService.Instance.RegisterCommand("SV_Debug_Health_Offset", CmdDebugHealthOffset, "Sets OUTL health label vertical offset.", "SV_Debug_Health_Offset <meters>");
        OUT_ConsoleService.Instance.RegisterCommand("sv_debug_health_offset", CmdDebugHealthOffset, "Sets OUTL health label vertical offset.", "sv_debug_health_offset <meters>");
        OUT_ConsoleService.Instance.RegisterCommand("SV_Debug_Inventory", CmdDebugInventory, "Toggles OUTL inventory/container/stats debug window.", "SV_Debug_Inventory [0|1|2] [entityId]");
        OUT_ConsoleService.Instance.RegisterCommand("sv_debug_inventory", CmdDebugInventory, "Toggles OUTL inventory/container/stats debug window.", "sv_debug_inventory [0|1|2] [entityId]");
        OUT_ConsoleService.Instance.RegisterCommand("outl.debug.inventory", CmdDebugInventory, "Toggles OUTL inventory/container/stats debug window.", "outl.debug.inventory [0|1|2] [entityId]");
        OUT_ConsoleService.Instance.RegisterCommand("outl.debug.hud", CmdDebugInventory, "Toggles OUTL runtime debug HUD.", "outl.debug.hud [0|1|2] [entityId]");
        OUT_ConsoleService.Instance.RegisterCommand("outl.debug.container", CmdDebugInventory, "Shows container section in OUTL runtime debug HUD.", "outl.debug.container [0|1|2]");
        OUT_ConsoleService.Instance.RegisterCommand("outl.debug.selected", CmdDebugSelected, "Selects OUTL debug HUD focus entity.", "outl.debug.selected <entityId|0>");
        OUT_ConsoleService.Instance.RegisterCommand("outl.debug.map", CmdDebugMap, "Toggles OUTL top-down debug map.", "outl.debug.map [0|1|2] [range]");
        OUT_ConsoleService.Instance.RegisterCommand("sv_debug_map", CmdDebugMap, "Toggles OUTL top-down debug map.", "sv_debug_map [0|1|2] [range]");
        OUT_ConsoleService.Instance.RegisterCommand("outl.debug.ledger", CmdDebugMap, "Shows OUTL world ledger on the debug map.", "outl.debug.ledger [0|1|2] [range]");
        OUT_ConsoleService.Instance.RegisterCommand("outl.debug.stimuli", CmdDebugMap, "Shows OUTL stimuli on the debug map.", "outl.debug.stimuli [0|1|2] [range]");
    }

    private void CmdStats(OUT_ConsoleCommandContext ctx)
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null) { ctx.Service.Log.Add("OUTL_World not found", OUT_ConsoleLog.Level.Warning); return; }
        ctx.Service.Log.Add("OUTL stats: entities=" + world.Registry.Count + " tickables=" + world.Scheduler.TickableCount + " events=" + world.Events.PendingCount + " quests=" + world.Quests.QuestCount);
    }

    private void CmdSpawn(OUT_ConsoleCommandContext ctx)
    {
        if (ctx.Count < 1) { ctx.Service.Log.Add("usage: outl.spawn <entityDefNameOrClassName>", OUT_ConsoleLog.Level.Warning); return; }
        OUTL_World world = OUTL_World.Instance;
        if (world == null) { ctx.Service.Log.Add("OUTL_World not found", OUT_ConsoleLog.Level.Warning); return; }
        OUTL_EntityDef def = ResolveSpawnDef(ctx.Arg(0));
        if (def == null) { ctx.Service.Log.Add("OUTL_EntityDef not found in SpawnCatalog: " + ctx.Arg(0), OUT_ConsoleLog.Level.Warning); return; }
        OUTL_EntityRuntime e = world.Spawn(def, Vector3.zero, Quaternion.identity);
        ctx.Service.Log.Add("spawned OUTL " + def.name + " id=" + (e != null ? e.Id.ToString() : "none"));
    }

    private OUTL_EntityDef ResolveSpawnDef(string key)
    {
        if (string.IsNullOrEmpty(key) || SpawnCatalog == null) return null;
        for (int i = 0; i < SpawnCatalog.Length; i++)
        {
            OUTL_EntityDef def = SpawnCatalog[i];
            if (def == null) continue;
            if (string.Equals(def.name, key, System.StringComparison.OrdinalIgnoreCase)) return def;
            if (string.Equals(def.ClassName, key, System.StringComparison.OrdinalIgnoreCase)) return def;
        }
        return null;
    }

    private void CmdInspect(OUT_ConsoleCommandContext ctx)
    {
        if (ctx.Count < 1 || !int.TryParse(ctx.Arg(0), out int id)) { ctx.Service.Log.Add("usage: outl.inspect <id>", OUT_ConsoleLog.Level.Warning); return; }
        OUTL_World world = OUTL_World.Instance;
        if (world == null) { ctx.Service.Log.Add("OUTL_World not found", OUT_ConsoleLog.Level.Warning); return; }
        OUTL_EntityRuntime e;
        if (!world.Registry.TryGet(new OUTL_EntityId(id), out e)) { ctx.Service.Log.Add("OUTL entity not found: " + id, OUT_ConsoleLog.Level.Warning); return; }
        ctx.Service.Log.Add("OUTL entity " + id + " def=" + (e.Def != null ? e.Def.name : "none") + " faction=" + (e.Faction != null ? e.Faction.name : "none") + " tier=" + e.Tier + " health=" + e.Stats.Get("Health", -1f));
    }

    private void CmdSend(OUT_ConsoleCommandContext ctx)
    {
        if (ctx.Count < 3) { ctx.Service.Log.Add("usage: outl.send <cmd> <sourceId> <targetId>", OUT_ConsoleLog.Level.Warning); return; }
        OUTL_World world = OUTL_World.Instance;
        if (world == null) { ctx.Service.Log.Add("OUTL_World not found", OUT_ConsoleLog.Level.Warning); return; }
        OUTL_CommandType type;
        if (!System.Enum.TryParse(ctx.Arg(0), true, out type)) { ctx.Service.Log.Add("unknown OUTL command: " + ctx.Arg(0), OUT_ConsoleLog.Level.Warning); return; }
        if (!int.TryParse(ctx.Arg(1), out int source) || !int.TryParse(ctx.Arg(2), out int target)) { ctx.Service.Log.Add("source/target must be ints", OUT_ConsoleLog.Level.Warning); return; }
        bool ok = world.Commands.Send(new OUTL_Command(type, new OUTL_EntityId(source), new OUTL_EntityId(target)));
        ctx.Service.Log.Add("outl.send " + type + " => " + ok);
    }

    private void CmdQuest(OUT_ConsoleCommandContext ctx)
    {
        if (ctx.Count < 2 || !int.TryParse(ctx.Arg(1), out int stage)) { ctx.Service.Log.Add("usage: outl.quest <questId> <stage>", OUT_ConsoleLog.Level.Warning); return; }
        OUTL_World world = OUTL_World.Instance;
        if (world == null) { ctx.Service.Log.Add("OUTL_World not found", OUT_ConsoleLog.Level.Warning); return; }
        world.Quests.SetStage(ctx.Arg(0), stage);
        ctx.Service.Log.Add("OUTL quest " + ctx.Arg(0) + " stage=" + stage);
    }

    private void CmdSave(OUT_ConsoleCommandContext ctx)
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null) { ctx.Service.Log.Add("OUTL_World not found", OUT_ConsoleLog.Level.Warning); return; }
        string path = ctx.Count > 0 ? ctx.Arg(0) : null;
        world.Save.SaveToFile(path);
        ctx.Service.Log.Add("OUTL saved to " + (string.IsNullOrEmpty(path) ? world.Save.DefaultPath : path));
    }

    private void CmdLoad(OUT_ConsoleCommandContext ctx)
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null) { ctx.Service.Log.Add("OUTL_World not found", OUT_ConsoleLog.Level.Warning); return; }
        string path = ctx.Count > 0 ? ctx.Arg(0) : null;
        bool ok = world.Save.LoadFromFile(path);
        ctx.Service.Log.Add("OUTL load => " + ok);
    }

    private void CmdDebugHealth(OUT_ConsoleCommandContext ctx)
    {
        if (ctx.Count > 0)
        {
            int mode;
            if (!int.TryParse(ctx.Arg(0), out mode)) mode = 0;
            OUTL_DebugSettings.SetDebugHealthMode(mode);
        }
        ctx.Service.Log.Add("SV_Debug_Health = " + OUTL_DebugSettings.DebugHealthMode + " distance=" + OUTL_DebugSettings.DebugHealthMaxDistance.ToString("0.#") + "m offset=" + OUTL_DebugSettings.DebugHealthVerticalOffset.ToString("0.##") + "m");
    }

    private void CmdDebugHealthOffset(OUT_ConsoleCommandContext ctx)
    {
        if (ctx.Count > 0)
        {
            float offset;
            if (float.TryParse(ctx.Arg(0), out offset)) OUTL_DebugSettings.DebugHealthVerticalOffset = Mathf.Clamp(offset, -5f, 10f);
        }
        ctx.Service.Log.Add("SV_Debug_Health_Offset = " + OUTL_DebugSettings.DebugHealthVerticalOffset.ToString("0.##") + "m");
    }

    private void CmdDebugInventory(OUT_ConsoleCommandContext ctx)
    {
        if (ctx.Count > 0)
        {
            int mode;
            if (!int.TryParse(ctx.Arg(0), out mode)) mode = 0;
            OUTL_DebugSettings.SetDebugInventoryMode(mode);
        }
        else if (string.Equals(ctx.CommandName, "outl.debug.container", System.StringComparison.OrdinalIgnoreCase))
        {
            OUTL_DebugSettings.SetDebugInventoryMode(2);
        }
        if (ctx.Count > 1)
        {
            int id;
            OUTL_DebugSettings.DebugInventoryEntityId = int.TryParse(ctx.Arg(1), out id) && id > 0 ? new OUTL_EntityId(id) : OUTL_EntityId.None;
        }
        ctx.Service.Log.Add("SV_Debug_Inventory = " + OUTL_DebugSettings.DebugInventoryMode + " entity=" + (OUTL_DebugSettings.DebugInventoryEntityId.IsValid ? OUTL_DebugSettings.DebugInventoryEntityId.Value.ToString() : "auto"));
    }

    private void CmdDebugSelected(OUT_ConsoleCommandContext ctx)
    {
        if (ctx.Count > 0)
        {
            int id;
            OUTL_DebugSettings.DebugInventoryEntityId = int.TryParse(ctx.Arg(0), out id) && id > 0 ? new OUTL_EntityId(id) : OUTL_EntityId.None;
            if (OUTL_DebugSettings.DebugInventoryEntityId.IsValid && OUTL_DebugSettings.DebugInventoryMode == 0) OUTL_DebugSettings.SetDebugInventoryMode(2);
        }
        ctx.Service.Log.Add("outl.debug.selected = " + (OUTL_DebugSettings.DebugInventoryEntityId.IsValid ? OUTL_DebugSettings.DebugInventoryEntityId.Value.ToString() : "auto") + " hud=" + OUTL_DebugSettings.DebugInventoryMode);
    }

    private void CmdDebugMap(OUT_ConsoleCommandContext ctx)
    {
        if (ctx.Count > 0)
        {
            int mode;
            if (!int.TryParse(ctx.Arg(0), out mode)) mode = 0;
            OUTL_DebugSettings.SetDebugMapMode(mode);
        }
        else if (string.Equals(ctx.CommandName, "outl.debug.ledger", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(ctx.CommandName, "outl.debug.stimuli", System.StringComparison.OrdinalIgnoreCase))
        {
            OUTL_DebugSettings.SetDebugMapMode(2);
        }
        if (ctx.Count > 1)
        {
            float range;
            if (float.TryParse(ctx.Arg(1), out range)) OUTL_DebugSettings.DebugMapRange = Mathf.Clamp(range, 10f, 5000f);
        }
        ctx.Service.Log.Add("outl.debug.map = " + OUTL_DebugSettings.DebugMapMode + " range=" + OUTL_DebugSettings.DebugMapRange.ToString("0.#") + "m layers=entities/ledger/stimuli");
    }
}
