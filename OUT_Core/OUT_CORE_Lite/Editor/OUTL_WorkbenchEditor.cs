#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class OUTL_WorkbenchEditor
{
    private const string MenuRoot = "OUT CORE Lite/Legacy Demo/Workbench/";
    private const string Folder = "Assets/OUT/OUT_Core/OUT_CORE_Lite/Templates/Workbench";

    // [MenuItem(MenuRoot + "Create Runtime Workbench")]
    public static void CreateRuntimeWorkbench()
    {
        EnsureFolder(Folder);

        GameObject runtime = GameObject.Find("OUTL_Runtime");
        if (runtime == null)
        {
            runtime = new GameObject("OUTL_Runtime");
            Undo.RegisterCreatedObjectUndo(runtime, "Create OUTL Runtime");
        }
        OUTL_World world = runtime.GetComponent<OUTL_World>();
        if (world == null) world = runtime.AddComponent<OUTL_World>();
        if (runtime.GetComponent<OUTL_QuickSaveInput>() == null) runtime.AddComponent<OUTL_QuickSaveInput>();
        OUTL_WorkbenchHUD hud = runtime.GetComponent<OUTL_WorkbenchHUD>();
        if (hud == null) hud = runtime.AddComponent<OUTL_WorkbenchHUD>();

        OUTL_EntityDef playerDef = CreateEntityDef("OUTL_WB_Player_Def", "player_workbench", "Workbench Player", new[] { "Actor", "Player" }, new OUTL_StatEntry[]
        {
            new OUTL_StatEntry { Key = "Health", Value = 100f },
            new OUTL_StatEntry { Key = "Armor", Value = 25f },
            new OUTL_StatEntry { Key = "Stamina", Value = 100f },
            new OUTL_StatEntry { Key = "Mana", Value = 50f },
            new OUTL_StatEntry { Key = "Damage", Value = 10f },
            new OUTL_StatEntry { Key = "Speed", Value = 5f }
        });

        OUTL_ItemDef item = CreateItemDef();
        OUTL_ActionDef useAction = CreateUseAction();
        item.OnUse = useAction;
        EditorUtility.SetDirty(item);

        OUTL_DropTable dropTable = CreateDropTable(item);
        GameObject player = CreatePlayer(playerDef);
        GameObject chest = CreateChest(dropTable);

        hud.Player = player.GetComponent<OUTL_EntityAdapter>();
        hud.Chest = chest.GetComponent<OUTL_TestChest>();
        hud.TestItem = item;
        hud.TestUseAction = useAction;
        hud.TestDropTable = dropTable;
        hud.TargetNameCommand = "wb_chest";
        hud.Command = OUTL_CommandType.Use;
        EditorUtility.SetDirty(hud);

        Selection.activeGameObject = runtime;
        EditorGUIUtility.PingObject(runtime);
        AssetDatabase.SaveAssets();
        Debug.Log("OUTL Runtime Workbench created. Press Play, F2, then test chest, commands, item effects, inventory and save/load.");
    }

    private static GameObject CreatePlayer(OUTL_EntityDef def)
    {
        GameObject existing = GameObject.Find("OUTL_WB_Player");
        if (existing != null) return existing;
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "OUTL_WB_Player";
        Undo.RegisterCreatedObjectUndo(go, "Create OUTL Workbench Player");
        go.transform.position = new Vector3(0f, 1f, -4f);
        OUTL_EntityAdapter entity = go.AddComponent<OUTL_EntityAdapter>();
        entity.Def = def;
        entity.ClassNameOverride = "player_workbench";
        entity.TargetName = "wb_player";
        entity.StableId = "wb_player";
        entity.TickLane = OUTL_TickLane.Full;
        entity.TickInterval = 0.01f;
        go.AddComponent<OUTL_DamageReceiver>().Entity = entity;
        OUTL_PlayerHUD playerHud = go.AddComponent<OUTL_PlayerHUD>();
        playerHud.Entity = entity;
        playerHud.FallbackTargetName = "wb_player";
        playerHud.AutoCreateCanvas = true;
        return go;
    }

    private static GameObject CreateChest(OUTL_DropTable dropTable)
    {
        GameObject existing = GameObject.Find("OUTL_WB_TestChest");
        if (existing != null) return existing;
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "OUTL_WB_TestChest";
        Undo.RegisterCreatedObjectUndo(go, "Create OUTL Workbench Chest");
        go.transform.position = new Vector3(0f, 0.5f, 1f);
        go.transform.localScale = new Vector3(1.4f, 0.8f, 1f);
        OUTL_EntityAdapter entity = go.AddComponent<OUTL_EntityAdapter>();
        entity.ClassNameOverride = "test_chest";
        entity.TargetName = "wb_chest";
        entity.StableId = "wb_chest";
        entity.TickLane = OUTL_TickLane.Logic;
        entity.TickInterval = 0.25f;
        OUTL_TestChest chest = go.AddComponent<OUTL_TestChest>();
        chest.Entity = entity;
        chest.DropTable = dropTable;
        chest.DropOnOpen = true;
        chest.DropOnlyOnce = true;
        return go;
    }

    private static OUTL_EntityDef CreateEntityDef(string assetName, string className, string displayName, string[] tags, OUTL_StatEntry[] stats)
    {
        string path = Folder + "/" + assetName + ".asset";
        OUTL_EntityDef existing = AssetDatabase.LoadAssetAtPath<OUTL_EntityDef>(path);
        if (existing != null) return existing;
        OUTL_EntityDef def = ScriptableObject.CreateInstance<OUTL_EntityDef>();
        def.ClassName = className;
        def.DisplayName = displayName;
        def.Tags = tags;
        def.BaseStats = stats;
        AssetDatabase.CreateAsset(def, path);
        return def;
    }

    private static OUTL_ItemDef CreateItemDef()
    {
        string path = Folder + "/OUTL_WB_Item_HealthShard.asset";
        OUTL_ItemDef existing = AssetDatabase.LoadAssetAtPath<OUTL_ItemDef>(path);
        if (existing != null) return existing;
        OUTL_ItemDef item = ScriptableObject.CreateInstance<OUTL_ItemDef>();
        item.ClassName = "item_health_shard";
        item.DisplayName = "Workbench Health Shard";
        item.Tags = new[] { "Item", "Consumable" };
        item.MaxStack = 16;
        item.Equippable = false;
        item.BaseStats = new[] { new OUTL_StatEntry { Key = "Value", Value = 1f } };
        AssetDatabase.CreateAsset(item, path);
        return item;
    }

    private static OUTL_ActionDef CreateUseAction()
    {
        string path = Folder + "/OUTL_WB_Action_UseHealthShard.asset";
        OUTL_ActionDef existing = AssetDatabase.LoadAssetAtPath<OUTL_ActionDef>(path);
        if (existing != null) return existing;
        OUTL_ActionDef action = ScriptableObject.CreateInstance<OUTL_ActionDef>();
        action.ActionId = "use_health_shard";
        action.TriggerCommand = OUTL_CommandType.Use;
        action.Effects = new[]
        {
            new OUTL_EffectDef { Type = OUTL_EffectType.Heal, Key = "workbench_heal", FloatValue = 15f, TargetSelf = true },
            new OUTL_EffectDef { Type = OUTL_EffectType.SetStateBool, Key = "UsedHealthShard", IntValue = 1, TargetSelf = true },
            new OUTL_EffectDef { Type = OUTL_EffectType.SendEvent, EventType = OUTL_EventType.Custom, Key = "UsedHealthShard", IntValue = 1, TargetSelf = true }
        };
        AssetDatabase.CreateAsset(action, path);
        return action;
    }

    private static OUTL_DropTable CreateDropTable(OUTL_ItemDef item)
    {
        string path = Folder + "/OUTL_WB_DropTable_Chest.asset";
        OUTL_DropTable existing = AssetDatabase.LoadAssetAtPath<OUTL_DropTable>(path);
        if (existing != null) return existing;
        OUTL_DropTable table = ScriptableObject.CreateInstance<OUTL_DropTable>();
        table.TableId = "wb_chest_table";
        table.Drops = new[]
        {
            new OUTL_DropEntry { Label = "health_shard", EntityDef = item, Chance = 1f, MinCount = 1, MaxCount = 3, ScatterRadius = 0.75f, SpawnOffset = Vector3.up * 0.7f }
        };
        AssetDatabase.CreateAsset(table, path);
        return table;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}

public static class OUTL_DebugMenuEditor
{
    // Duplicate legacy menu disabled. Canonical overlay menu is OUTL_DebugHealthLabelsEditor.
    public static void ToggleHealthLabels()
    {
        int mode = OUTL_DebugSettings.ToggleDebugHealthMode();
        Debug.Log("SV_Debug_Health = " + mode + " distance=" + OUTL_DebugSettings.DebugHealthMaxDistance.ToString("0.#") + "m");
    }

    // Duplicate legacy menu disabled.
    public static void ToggleHealthLabelsExtended()
    {
        int mode = OUTL_DebugSettings.SetDebugHealthMode(OUTL_DebugSettings.DebugHealthMode == 2 ? 0 : 2);
        Debug.Log("SV_Debug_Health = " + mode + " distance=" + OUTL_DebugSettings.DebugHealthMaxDistance.ToString("0.#") + "m offset=" + OUTL_DebugSettings.DebugHealthVerticalOffset.ToString("0.##") + "m");
    }

    // Duplicate legacy menu disabled.
    public static void DisableHealthLabels()
    {
        OUTL_DebugSettings.SetDebugHealthMode(0);
        Debug.Log("SV_Debug_Health = 0");
    }

    // Duplicate legacy validation menu disabled.
    private static bool ToggleHealthLabelsValidate()
    {
        Menu.SetChecked("OUT CORE Lite/Diagnostics/Runtime Overlays/Toggle Health Labels", OUTL_DebugSettings.DebugHealthMode > 0);
        return true;
    }

    [MenuItem("OUT CORE Lite/Diagnostics/Runtime Overlays/Toggle Inventory Window")]
    public static void ToggleInventoryWindow()
    {
        int mode = OUTL_DebugSettings.ToggleDebugInventoryMode();
        Debug.Log("SV_Debug_Inventory = " + mode + " entity=" + (OUTL_DebugSettings.DebugInventoryEntityId.IsValid ? OUTL_DebugSettings.DebugInventoryEntityId.Value.ToString() : "auto"));
    }

    [MenuItem("OUT CORE Lite/Diagnostics/Runtime Overlays/Toggle Inventory Window", true)]
    private static bool ToggleInventoryWindowValidate()
    {
        Menu.SetChecked("OUT CORE Lite/Diagnostics/Runtime Overlays/Toggle Inventory Window", OUTL_DebugSettings.DebugInventoryMode > 0);
        return true;
    }

    [MenuItem("OUT CORE Lite/Diagnostics/Runtime Overlays/Toggle World Debug Map")]
    public static void ToggleWorldDebugMap()
    {
        int mode = OUTL_DebugSettings.ToggleDebugMapMode();
        Debug.Log("outl.debug.map = " + mode + " range=" + OUTL_DebugSettings.DebugMapRange.ToString("0.#") + "m");
    }

    [MenuItem("OUT CORE Lite/Diagnostics/Runtime Overlays/Toggle World Debug Map", true)]
    private static bool ToggleWorldDebugMapValidate()
    {
        Menu.SetChecked("OUT CORE Lite/Diagnostics/Runtime Overlays/Toggle World Debug Map", OUTL_DebugSettings.DebugMapMode > 0);
        return true;
    }
}
#endif
