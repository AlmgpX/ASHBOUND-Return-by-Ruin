using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(1300)]
[DisallowMultipleComponent]
public sealed class OUTL_WorkbenchHUD : MonoBehaviour, OUTL_IEventListener
{
    public bool Show = true;
    public KeyCode ToggleKey = KeyCode.F2;
    public Rect WindowRect = new Rect(430f, 80f, 460f, 560f);
    public OUTL_EntityAdapter Player;
    public OUTL_TestChest Chest;
    public OUTL_ItemDef TestItem;
    public OUTL_ActionDef TestUseAction;
    public OUTL_DropTable TestDropTable;
    public string PlayerTargetName = "player";
    public string PlayerClassName = "player";
    public string PlayerRequiredTag = "Player";
    public string TargetNameCommand = "wb_chest";
    public OUTL_CommandType Command = OUTL_CommandType.Use;
    public string CommandKey = "Workbench";
    public float CommandFloat;
    public int CommandInt;
    public bool AutoFind = true;
    public int EventLogLimit = 12;

    private readonly List<string> events = new List<string>(24);
    private readonly List<OUTL_FloatPair> stats = new List<OUTL_FloatPair>(32);
    private readonly List<string> flags = new List<string>(32);
    private readonly List<OUTL_IntPair> ints = new List<OUTL_IntPair>(32);
    private readonly List<OUTL_StringPair> strings = new List<OUTL_StringPair>(32);
    private readonly List<OUTL_EntityRuntime> entityBuffer = new List<OUTL_EntityRuntime>(128);
    private Vector2 scroll;
    private bool registered;
    private static readonly OUTL_CommandType[] CommandButtons =
    {
        OUTL_CommandType.Use,
        OUTL_CommandType.Open,
        OUTL_CommandType.Close,
        OUTL_CommandType.Activate,
        OUTL_CommandType.Deactivate,
        OUTL_CommandType.Damage,
        OUTL_CommandType.Heal,
        OUTL_CommandType.AddItem,
        OUTL_CommandType.RemoveItem,
        OUTL_CommandType.SendSignal,
        OUTL_CommandType.Custom
    };

    private void Awake()
    {
        AutoResolve();
    }

    private void OnEnable()
    {
        RegisterEvents();
    }

    private void OnDisable()
    {
        UnregisterEvents();
    }

    private void Update()
    {
        if (Input.GetKeyDown(ToggleKey)) Show = !Show;
        if (AutoFind && (Player == null || Chest == null)) AutoResolve();
    }

    private void OnGUI()
    {
        if (!Show) return;
        WindowRect = GUI.Window(GetInstanceID(), WindowRect, DrawWindow, "OUTL Workbench");
    }

    public void OUTL_OnEvent(in OUTL_Event evt, OUTL_World world)
    {
        string line = evt.Type + " src=" + evt.Source + " dst=" + evt.Target + " key=" + evt.Key + " i=" + evt.IntValue + " f=" + evt.FloatValue.ToString("0.##");
        events.Insert(0, line);
        while (events.Count > Mathf.Max(1, EventLogLimit)) events.RemoveAt(events.Count - 1);
    }

    [ContextMenu("OUT Auto Resolve Workbench")]
    public void AutoResolve()
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return;
        if (Player == null)
        {
            OUTL_EntityRuntime byTarget = world.Registry.FindFirstByTargetName(PlayerTargetName);
            if (byTarget != null && byTarget.Adapter != null) Player = byTarget.Adapter;
        }
        if (Player == null)
        {
            OUTL_EntityRuntime byClass = world.Registry.FindFirstByClassName(PlayerClassName);
            if (byClass != null && byClass.Adapter != null) Player = byClass.Adapter;
        }
        world.Registry.CopyAll(entityBuffer);
        for (int i = 0; i < entityBuffer.Count; i++)
        {
            OUTL_EntityRuntime runtime = entityBuffer[i];
            OUTL_EntityAdapter e = runtime != null ? runtime.Adapter : null;
            if (e == null) continue;
            if (Player == null && runtime != null && !string.IsNullOrEmpty(PlayerRequiredTag) && runtime.HasTag(PlayerRequiredTag)) Player = e;
            if (Chest == null)
            {
                OUTL_TestChest chest = e.GetComponent<OUTL_TestChest>();
                if (chest != null) Chest = chest;
            }
        }
        entityBuffer.Clear();
        if (Chest != null && !string.IsNullOrEmpty(Chest.Entity != null ? Chest.Entity.TargetName : string.Empty)) TargetNameCommand = Chest.Entity.TargetName;
        if (Chest != null && TestDropTable == null) TestDropTable = Chest.DropTable;
    }

    private void DrawWindow(int id)
    {
        scroll = GUILayout.BeginScrollView(scroll);
        OUTL_World world = OUTL_World.Instance;
        GUILayout.Label("World", GUI.skin.box);
        GUILayout.Label(world != null ? ("time=" + world.WorldTime.ToString("0.00") + " paused=" + world.IsPaused + " entities=" + world.Registry.Count + " queued=" + world.Commands.QueuedCount + " events=" + world.Events.PendingCount) : "NO OUTL_WORLD");

        GUILayout.Space(6f);
        GUILayout.Label("References", GUI.skin.box);
        Player = (OUTL_EntityAdapter)ObjectField("Player", Player, typeof(OUTL_EntityAdapter));
        Chest = (OUTL_TestChest)ObjectField("Chest", Chest, typeof(OUTL_TestChest));
        TestItem = (OUTL_ItemDef)ObjectField("Test Item", TestItem, typeof(OUTL_ItemDef));
        TestUseAction = (OUTL_ActionDef)ObjectField("Use Action", TestUseAction, typeof(OUTL_ActionDef));
        TestDropTable = (OUTL_DropTable)ObjectField("Drop Table", TestDropTable, typeof(OUTL_DropTable));
        if (GUILayout.Button("Auto Resolve")) AutoResolve();

        GUILayout.Space(6f);
        GUILayout.Label("Command Console", GUI.skin.box);
        GUILayout.Label("TargetName");
        TargetNameCommand = GUILayout.TextField(TargetNameCommand);
        DrawCommandButtons();
        GUILayout.Label("Key");
        CommandKey = GUILayout.TextField(CommandKey);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Float", GUILayout.Width(45f));
        float nextFloat;
        if (float.TryParse(GUILayout.TextField(CommandFloat.ToString()), out nextFloat)) CommandFloat = nextFloat;
        GUILayout.Label("Int", GUILayout.Width(30f));
        int nextInt;
        if (int.TryParse(GUILayout.TextField(CommandInt.ToString()), out nextInt)) CommandInt = nextInt;
        GUILayout.EndHorizontal();
        if (GUILayout.Button("Send Command To TargetName")) SendCommand();

        GUILayout.Space(6f);
        GUILayout.Label("Chest", GUI.skin.box);
        if (Chest != null)
        {
            GUILayout.Label("open=" + Chest.IsOpen + " target=" + (Chest.Entity != null ? Chest.Entity.TargetName : "<none>"));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Open")) Chest.OpenNow();
            if (GUILayout.Button("Close")) Chest.CloseNow();
            if (GUILayout.Button("Drop")) Chest.DropNow();
            GUILayout.EndHorizontal();
        }
        else GUILayout.Label("No OUTL_TestChest assigned/found.");

        GUILayout.Space(6f);
        GUILayout.Label("Inventory / Item Effects", GUI.skin.box);
        if (world != null && Player != null && TestItem != null)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Item")) world.Inventory.AddItem(Player.Id, TestItem, 1);
            if (GUILayout.Button("Remove Item")) world.Inventory.RemoveItem(Player.Id, TestItem, 1);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Use Item Action")) UseItemAction();
        }
        else GUILayout.Label("Assign Player + TestItem to test inventory.");

        GUILayout.Space(6f);
        GUILayout.Label("Save / Load", GUI.skin.box);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Quick Save")) SaveNow();
        if (GUILayout.Button("Quick Load")) LoadNow();
        GUILayout.EndHorizontal();

        GUILayout.Space(6f);
        DrawRuntime(Player, "Player Runtime");
        DrawRuntime(Chest != null ? Chest.Entity : null, "Chest Runtime");

        GUILayout.Space(6f);
        GUILayout.Label("Recent Events", GUI.skin.box);
        for (int i = 0; i < events.Count; i++) GUILayout.Label(events[i]);

        GUILayout.EndScrollView();
        GUI.DragWindow();
    }

    private void DrawCommandButtons()
    {
        GUILayout.Label("Command = " + Command);
        for (int i = 0; i < CommandButtons.Length; i += 3)
        {
            GUILayout.BeginHorizontal();
            for (int j = 0; j < 3 && i + j < CommandButtons.Length; j++)
            {
                OUTL_CommandType type = CommandButtons[i + j];
                bool selected = Command == type;
                GUI.enabled = !selected;
                if (GUILayout.Button(type.ToString())) Command = type;
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
        }
    }

    private void SendCommand()
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null || string.IsNullOrEmpty(TargetNameCommand)) return;
        OUTL_EntityId source = Player != null ? Player.Id : OUTL_EntityId.None;
        OUTL_Command cmd = new OUTL_Command(Command, source, OUTL_EntityId.None) { Key = CommandKey, FloatValue = CommandFloat, IntValue = CommandInt, Point = Player != null ? Player.transform.position : transform.position, Context = this };
        int count = world.Commands.SendToTargetName(TargetNameCommand, cmd);
        events.Insert(0, "SEND " + Command + " -> " + TargetNameCommand + " receivers=" + count);
    }

    private void UseItemAction()
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null || Player == null || TestItem == null) return;
        OUTL_ActionDef action = TestUseAction != null ? TestUseAction : TestItem.OnUse;
        if (action == null) { events.Insert(0, "No item action assigned."); return; }
        if (!OUTL_Rules.CheckAll(action.Conditions, Player.Id, Player.Id, world)) { events.Insert(0, "Item action conditions failed."); return; }
        world.Effects.ApplyAll(action.Effects, Player.Id, Player.Id, Player.transform.position);
        events.Insert(0, "USED ITEM ACTION " + action.ActionId);
    }

    private void SaveNow()
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return;
        string path = System.IO.Path.Combine(Application.persistentDataPath, "outl_workbench_save.json");
        world.Save.SaveToFile(path);
        events.Insert(0, "SAVE " + path);
    }

    private void LoadNow()
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return;
        string path = System.IO.Path.Combine(Application.persistentDataPath, "outl_workbench_save.json");
        bool ok = world.Save.LoadFromFile(path);
        events.Insert(0, "LOAD " + ok + " " + path);
    }

    private void DrawRuntime(OUTL_EntityAdapter entity, string title)
    {
        GUILayout.Label(title, GUI.skin.box);
        if (entity == null || entity.Runtime == null) { GUILayout.Label("<none>"); return; }
        OUTL_EntityRuntime rt = entity.Runtime;
        GUILayout.Label("id=" + rt.Id + " class=" + rt.ClassName + " target=" + rt.TargetName + " tier=" + rt.Tier);
        rt.Stats.CopyTo(stats);
        GUILayout.Label("Stats:");
        for (int i = 0; i < stats.Count; i++) GUILayout.Label("  " + stats[i].Key + " = " + stats[i].Value.ToString("0.##"));
        rt.State.CopyFlags(flags);
        rt.State.CopyInts(ints);
        rt.State.CopyStrings(strings);
        GUILayout.Label("Flags: " + string.Join(", ", flags.ToArray()));
        for (int i = 0; i < ints.Count; i++) GUILayout.Label("Int " + ints[i].Key + " = " + ints[i].Value);
        for (int i = 0; i < strings.Count; i++) GUILayout.Label("Str " + strings[i].Key + " = " + strings[i].Value);
    }

    private void RegisterEvents()
    {
        if (registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Events.Register(this);
        registered = true;
    }

    private void UnregisterEvents()
    {
        if (!registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Events.Unregister(this);
        registered = false;
    }

    private static Object ObjectField(string label, Object value, System.Type type)
    {
#if UNITY_EDITOR
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(80f));
        Object result = UnityEditor.EditorGUILayout.ObjectField(value, type, true);
        GUILayout.EndHorizontal();
        return result;
#else
        GUILayout.Label(label + ": " + (value != null ? value.name : "<none>"));
        return value;
#endif
    }
}
