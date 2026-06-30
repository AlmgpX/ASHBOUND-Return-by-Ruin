#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OUT_CoreSceneTemplateMenu
{
    private const string MenuRoot = "OUT CORE (Legacy)/Scene/";

    [MenuItem(MenuRoot + "Setup Open Scene Template", priority = 1)]
    public static void SetupOpenSceneTemplate()
    {
        CreateOrUpdateTemplate(false);
    }

    [MenuItem(MenuRoot + "Create New Template Scene", priority = 2)]
    public static void CreateNewTemplateScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateOrUpdateTemplate(true);
    }

    [MenuItem(MenuRoot + "Select OUT_CORE_SCENE_ROOT", priority = 50)]
    public static void SelectRoot()
    {
        GameObject root = GameObject.Find("OUT_CORE_SCENE_ROOT");
        if (root == null)
        {
            Debug.LogWarning("Legacy OUT CORE scene root not found. Canonical setup is OUT CORE Lite/Scene Setup/Setup Open Scene - Production.");
            return;
        }

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
    }

    private static void CreateOrUpdateTemplate(bool newScene)
    {
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Setup OUT CORE Scene Template");
        int undoGroup = Undo.GetCurrentGroup();

        GameObject root = GetOrCreateRoot("OUT_CORE_SCENE_ROOT");
        GameObject runtime = GetOrCreateChild(root.transform, "OUT_Runtime");
        GameObject systems = GetOrCreateChild(root.transform, "OUT_Systems");
        GameObject world = GetOrCreateChild(root.transform, "OUT_World");
        GameObject egregores = GetOrCreateChild(root.transform, "OUT_Egregores");
        GameObject ai = GetOrCreateChild(root.transform, "OUT_AI");
        GameObject player = GetOrCreateChild(root.transform, "OUT_Player");
        GameObject debug = GetOrCreateChild(root.transform, "OUT_Debug");

        SetupRuntime(runtime.transform);
        SetupWorld(world.transform);
        SetupEgregores(egregores.transform);
        SetupAI(ai.transform);
        SetupPlayer(player.transform);
        SetupDebug(debug.transform);
        SetupSystems(systems.transform);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(undoGroup);

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        Debug.Log(newScene
            ? "OUT CORE template scene created. Да, теперь даже пустота имеет менеджмент."
            : "OUT CORE template added/updated in the open scene.");
    }

    private static void SetupRuntime(Transform parent)
    {
        CreateRuntimeService(parent, "OUT_ConsoleService", "OUT_ConsoleService");
        CreateRuntimeService(parent, "OUT_ConsoleOverlay", "OUT_ConsoleOverlay");
        CreateRuntimeService(parent, "OUT_SaveConsoleCommands", "OUT_SaveConsoleCommands");
        CreateRuntimeService(parent, "OUT_SimulationService", "OUT_SimulationService");
        CreateRuntimeService(parent, "OUT_WorldSaveService", "OUT_WorldSaveService");
        CreateRuntimeService(parent, "OUT_SignalBus", "OUT_SignalBus");
        CreateRuntimeService(parent, "OUT_SceneStimulusService", "OUT_SceneStimulusService");
        CreateRuntimeService(parent, "OUT_RuntimePoolService", "OUT_RuntimePoolService");
        CreateRuntimeService(parent, "OUT_AIDebugLogService", "OUT_AIDebugLogService");
    }

    private static void SetupSystems(Transform parent)
    {
        GetOrCreateChild(parent, "README_Systems_Placeholder");
    }

    private static void SetupDebug(Transform parent)
    {
        GetOrCreateChild(parent, "Debug_DrawComponents");
    }

    private static void SetupWorld(Transform parent)
    {
        GameObject ground = FindChild(parent, "Ground");
        if (ground == null)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Undo.RegisterCreatedObjectUndo(ground, "Create Ground");
            ground.name = "Ground";
            ground.transform.SetParent(parent, false);
            ground.transform.localScale = new Vector3(5f, 1f, 5f);
        }

        GameObject cratesRoot = GetOrCreateChild(parent, "Test_Crates");
        for (int i = 0; i < 3; i++)
        {
            GameObject crate = FindChild(cratesRoot.transform, "Crate_0" + (i + 1));
            if (crate == null)
            {
                crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Undo.RegisterCreatedObjectUndo(crate, "Create Test Crate");
                crate.name = "Crate_0" + (i + 1);
                crate.transform.SetParent(cratesRoot.transform, false);
                crate.transform.localPosition = new Vector3(-2f + i * 2f, 0.5f, 4f);
            }
            AddComponentByName(crate, "OUT_SaveableEntity");
            ConfigureSaveable(crate, "world/crate_0" + (i + 1), false, true, true);
        }

        GameObject food = FindChild(parent, "Test_Food");
        if (food == null)
        {
            food = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Undo.RegisterCreatedObjectUndo(food, "Create Test Food");
            food.name = "Test_Food";
            food.transform.SetParent(parent, false);
            food.transform.localPosition = new Vector3(3f, 0.45f, 2f);
            food.transform.localScale = Vector3.one * 0.55f;
        }
        AddComponentByName(food, "OUT_SaveableEntity");
        ConfigureSaveable(food, "world/food/test_food", false, true, true);
        Component foodEmitter = AddComponentByName(food, "OUT_SignalEmitter");
        ConfigureSignalEmitter(foodEmitter, SignalMask("Food", "Reward"), 0.6f, 10f, "food", false, false);

        GameObject sacredTree = FindChild(parent, "Test_SacredTree");
        if (sacredTree == null)
        {
            sacredTree = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Undo.RegisterCreatedObjectUndo(sacredTree, "Create Test Sacred Tree");
            sacredTree.name = "Test_SacredTree";
            sacredTree.transform.SetParent(parent, false);
            sacredTree.transform.localPosition = new Vector3(-4f, 1f, 2.5f);
            sacredTree.transform.localScale = new Vector3(0.45f, 1.2f, 0.45f);
        }
        AddComponentByName(sacredTree, "OUT_SaveableEntity");
        ConfigureSaveable(sacredTree, "world/tree/sacred_test_tree", false, true, true);
        Component treeEmitter = AddComponentByName(sacredTree, "OUT_SignalEmitter");
        ConfigureSignalEmitter(treeEmitter, SignalMask("Sacred", "Death", "Aversion"), 1f, 8f, "sacred_tree_cut", false, false);
    }

    private static void SetupEgregores(Transform parent)
    {
        GameObject zone = GetOrCreateChild(parent, "EG_TestZone");
        zone.transform.localPosition = Vector3.zero;

        AddComponentByName(zone, "OUT_SaveableEntity");
        ConfigureSaveable(zone, "egregore/test_zone", false, false, true);

        Component egregoreZone = AddComponentByName(zone, "OUT_EgregoreZone");
        SetSerializedString(egregoreZone, "egregoreId", "test_zone");
        SetSerializedString(egregoreZone, "displayName", "Test Zone Egregore");
        SetSerializedFloat(egregoreZone, "radius", 35f);
        SetSerializedBool(egregoreZone, "saveToTextFile", true);
        SetSerializedFloat(egregoreZone, "saveInterval", 20f);

        AddComponentByName(zone, "OUT_EgregoreZoneSaveState");
        AddComponentByName(zone, "OUT_EgregoreEventLedger");
    }

    private static void SetupAI(Transform parent)
    {
        GameObject graph = GetOrCreateChild(parent, "OUT_WORLD_GRAPH_Test");
        AddComponentByName(graph, "OUT_AIGraph");
        AddComponentByName(graph, "OUT_AIGraphAuthoring");
        AddComponentByName(graph, "OUT_SceneSensoryField");
        AddComponentByName(graph, "OUT_AICrowdService");

        GetOrCreateChild(parent, "SoldierSquad_01");
        GetOrCreateChild(parent, "Cockroach_System");
    }

    private static void SetupPlayer(Transform parent)
    {
        GameObject player = GetOrCreateChild(parent, "OUT_HL1PlayerController");
        player.transform.localPosition = new Vector3(0f, 1.1f, -6f);

        if (player.GetComponent<CharacterController>() == null)
            Undo.AddComponent<CharacterController>(player);

        AddComponentByName(player, "OUT_SaveableEntity");
        ConfigureSaveable(player, "player/main", false, true, true);
        AddComponentByName(player, "OUT_HL1PlayerController");
        AddComponentByName(player, "OUT_PlayerCrushDamage");
        AddComponentByName(player, "OUT_HL1PlayerMovementShake");

        GameObject cameraRoot = GetOrCreateChild(player.transform, "CameraRoot");
        GameObject cam = FindChild(cameraRoot.transform, "HL_cam");
        if (cam == null)
        {
            cam = new GameObject("HL_cam");
            Undo.RegisterCreatedObjectUndo(cam, "Create Player Camera");
            cam.transform.SetParent(cameraRoot.transform, false);
            cam.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            cam.AddComponent<Camera>();
            cam.AddComponent<AudioListener>();
        }
        AddComponentByName(cam, "OUT_CameraShakeReceiver");
    }

    private static void CreateRuntimeService(Transform parent, string objectName, string componentTypeName)
    {
        GameObject go = GetOrCreateChild(parent, objectName);
        AddComponentByName(go, componentTypeName);
    }

    private static GameObject GetOrCreateRoot(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
            return existing;

        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        return go;
    }

    private static GameObject GetOrCreateChild(Transform parent, string name)
    {
        GameObject existing = FindChild(parent, name);
        if (existing != null)
            return existing;

        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject FindChild(Transform parent, string name)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name)
                return child.gameObject;
        }
        return null;
    }

    private static Component AddComponentByName(GameObject go, string typeName)
    {
        if (go == null || string.IsNullOrWhiteSpace(typeName))
            return null;

        Type type = FindType(typeName);
        if (type == null)
        {
            Debug.LogWarning("OUT CORE template: type not found: " + typeName + ". Skipped. Видимо, класс ещё не приехал на этот праздник.");
            return null;
        }

        if (!typeof(Component).IsAssignableFrom(type))
        {
            Debug.LogWarning("OUT CORE template: type is not a Component: " + typeName);
            return null;
        }

        Component existing = go.GetComponent(type);
        if (existing != null)
            return existing;

        return Undo.AddComponent(go, type);
    }

    private static Type FindType(string typeName)
    {
        Type direct = Type.GetType(typeName);
        if (direct != null)
            return direct;

        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            Type type = assemblies[i].GetType(typeName);
            if (type != null)
                return type;
        }

        return assemblies
            .SelectMany(a => SafeGetTypes(a))
            .FirstOrDefault(t => t != null && t.Name == typeName);
    }

    private static Type[] SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null).ToArray();
        }
        catch
        {
            return Array.Empty<Type>();
        }
    }

    private static int SignalMask(params string[] names)
    {
        Type enumType = FindType("OUT_SignalChannelFlags");
        if (enumType == null || !enumType.IsEnum)
            return 0;

        int value = 0;
        for (int i = 0; i < names.Length; i++)
        {
            try
            {
                object parsed = Enum.Parse(enumType, names[i]);
                value |= Convert.ToInt32(parsed);
            }
            catch
            {
                // deliberately ignored
            }
        }
        return value;
    }

    private static void ConfigureSaveable(GameObject go, string saveId, bool autoPath, bool saveTransform, bool saveActive)
    {
        Component saveable = go != null ? go.GetComponent(FindType("OUT_SaveableEntity")) : null;
        if (saveable == null)
            return;

        SerializedObject so = new SerializedObject(saveable);
        SetProperty(so, "saveId", saveId);
        SetProperty(so, "autoGenerateFromHierarchyPath", autoPath);
        SetProperty(so, "saveTransform", saveTransform);
        SetProperty(so, "saveActiveState", saveActive);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(saveable);
    }

    private static void ConfigureSignalEmitter(Component emitter, int channels, float intensity, float radius, string label, bool emitOnEnable, bool emitRepeatedly)
    {
        if (emitter == null)
            return;

        SerializedObject so = new SerializedObject(emitter);
        SetProperty(so, "channels", channels);
        SetProperty(so, "intensity", intensity);
        SetProperty(so, "radius", radius);
        SetProperty(so, "label", label);
        SetProperty(so, "emitOnEnable", emitOnEnable);
        SetProperty(so, "emitRepeatedly", emitRepeatedly);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(emitter);
    }

    private static void SetSerializedString(Component component, string propertyName, string value)
    {
        if (component == null)
            return;

        SerializedObject so = new SerializedObject(component);
        SetProperty(so, propertyName, value);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(component);
    }

    private static void SetSerializedBool(Component component, string propertyName, bool value)
    {
        if (component == null)
            return;

        SerializedObject so = new SerializedObject(component);
        SetProperty(so, propertyName, value);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(component);
    }

    private static void SetSerializedFloat(Component component, string propertyName, float value)
    {
        if (component == null)
            return;

        SerializedObject so = new SerializedObject(component);
        SetProperty(so, propertyName, value);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(component);
    }

    private static void SetProperty(SerializedObject so, string propertyName, object value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
            return;

        switch (property.propertyType)
        {
            case SerializedPropertyType.Boolean:
                property.boolValue = value is bool b && b;
                break;
            case SerializedPropertyType.Integer:
            case SerializedPropertyType.Enum:
                property.intValue = Convert.ToInt32(value);
                break;
            case SerializedPropertyType.Float:
                property.floatValue = Convert.ToSingle(value);
                break;
            case SerializedPropertyType.String:
                property.stringValue = value != null ? value.ToString() : string.Empty;
                break;
        }
    }
}
#endif
