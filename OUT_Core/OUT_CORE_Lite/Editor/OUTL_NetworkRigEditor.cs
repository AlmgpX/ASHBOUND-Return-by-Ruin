#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class OUTL_NetworkRigEditor
{
    private const string MenuRoot = "OUT CORE Lite/Network/";

    // [MenuItem(MenuRoot + "Create Open World Network Rig")]
    public static void CreateOpenWorldNetworkRig()
    {
        CreateMirrorLanTestRig();
    }

    // [MenuItem(MenuRoot + "Create Mirror LAN Test Rig")]
    public static void CreateMirrorLanTestRig()
    {
        GameObject runtime = EnsureRuntime();
        EnsureWorldAccessMenu(runtime);

        GameObject manager = GameObject.Find("OUTL_Mirror_NetworkManager");
        if (manager == null)
        {
            manager = new GameObject("OUTL_Mirror_NetworkManager");
            Undo.RegisterCreatedObjectUndo(manager, "Create OUTL Mirror NetworkManager");
        }

        bool mirrorInstalled = TryAddMirrorManager(manager);
        GameObject playerPrefab = EnsureNetworkPlayerPrefab(mirrorInstalled);
        if (mirrorInstalled) TryAssignPlayerPrefab(manager, playerPrefab);

        Selection.activeGameObject = runtime;
        EditorGUIUtility.PingObject(runtime);
        Debug.Log(mirrorInstalled
            ? "OUTL Open World Network Rig created. Press Play, F1, then Open Current World To Friends. Same OUTL_World is used for single-player and host mode."
            : "OUTL network rig placeholder created. Install Mirror and add OUTL_MIRROR define, then run this menu again. Single-player OUTL_World still works.");
    }

    // [MenuItem(MenuRoot + "Add OUTL_MIRROR Define")]
    public static void AddMirrorDefine()
    {
        BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
        string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
        if (!symbols.Contains("OUTL_MIRROR"))
        {
            symbols = string.IsNullOrEmpty(symbols) ? "OUTL_MIRROR" : symbols + ";OUTL_MIRROR";
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, symbols);
        }
        Debug.Log("OUTL_MIRROR define is enabled for " + group);
    }

    private static GameObject EnsureRuntime()
    {
        GameObject runtime = GameObject.Find("OUTL_Runtime");
        if (runtime == null)
        {
            runtime = new GameObject("OUTL_Runtime");
            Undo.RegisterCreatedObjectUndo(runtime, "Create OUTL Runtime");
        }
        if (runtime.GetComponent<OUTL_World>() == null) runtime.AddComponent<OUTL_World>();
        if (runtime.GetComponent<OUTL_QuickSaveInput>() == null) runtime.AddComponent<OUTL_QuickSaveInput>();
        OUTL_NetworkSession session = runtime.GetComponent<OUTL_NetworkSession>();
        if (session == null) session = runtime.AddComponent<OUTL_NetworkSession>();
        session.World = runtime.GetComponent<OUTL_World>();
        return runtime;
    }

    private static void EnsureWorldAccessMenu(GameObject runtime)
    {
        OUTL_NetworkSession session = runtime.GetComponent<OUTL_NetworkSession>();
        OUTL_NetworkLanMenu menu = runtime.GetComponent<OUTL_NetworkLanMenu>();
        if (menu == null) menu = runtime.AddComponent<OUTL_NetworkLanMenu>();
        menu.Session = session;
        if (runtime.GetComponent<OUTL_NetworkQuickStart>() == null) runtime.AddComponent<OUTL_NetworkQuickStart>();
    }

    private static bool TryAddMirrorManager(GameObject manager)
    {
        Type networkManagerType = FindType("Mirror.NetworkManager");
        Type transportType = FindType("Mirror.KcpTransport") ?? FindType("Mirror.TelepathyTransport");
        if (networkManagerType == null) return false;

        Component networkManager = manager.GetComponent(networkManagerType);
        if (networkManager == null) networkManager = manager.AddComponent(networkManagerType);

        if (transportType != null && manager.GetComponent(transportType) == null)
            manager.AddComponent(transportType);

        return true;
    }

    private static GameObject EnsureNetworkPlayerPrefab(bool mirrorInstalled)
    {
        const string folder = "Assets/OUT/OUT_Core/OUT_CORE_Lite/Templates/Network";
        EnsureFolder(folder);
        string path = folder + "/OUTL_Network_Player.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null) return prefab;

        GameObject go = new GameObject("OUTL_Network_Player");
        CharacterController cc = go.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.9f, 0f);

        OUTL_EntityAdapter entity = go.AddComponent<OUTL_EntityAdapter>();
        entity.ClassNameOverride = "player_network";
        entity.TargetName = "player_network";
        entity.StableId = "network_player_prefab";
        entity.TickLane = OUTL_TickLane.Full;
        entity.TickInterval = 0.01f;

        OUTL_NetworkIdentityLite id = go.AddComponent<OUTL_NetworkIdentityLite>();
        id.Entity = entity;
        id.ReplicateTransform = true;
        id.ReplicateStats = true;

        OUTL_MirrorEntityBridge bridge = go.AddComponent<OUTL_MirrorEntityBridge>();
        go.AddComponent<OUTL_DamageReceiver>().Entity = entity;
        OUTL_PlayerHUD hud = go.AddComponent<OUTL_PlayerHUD>();
        hud.Entity = entity;
        hud.AutoCreateCanvas = true;

        Type networkIdentityType = FindType("Mirror.NetworkIdentity");
        if (mirrorInstalled && networkIdentityType != null && go.GetComponent(networkIdentityType) == null)
            go.AddComponent(networkIdentityType);

        GameObject cam = new GameObject("ViewCamera");
        cam.transform.SetParent(go.transform, false);
        cam.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        Camera camera = cam.AddComponent<Camera>();
        cam.AddComponent<AudioListener>();

        OUTL_BasicPlayerController controller = go.AddComponent<OUTL_BasicPlayerController>();
        controller.Entity = entity;
        controller.CharacterController = cc;
        controller.ViewCamera = camera;

        OUTL_AttackDriver attack = go.AddComponent<OUTL_AttackDriver>();
        attack.Source = entity;
        attack.AimCamera = camera;
        attack.Muzzle = cam.transform;
        controller.AttackDriver = attack;

        TrySetLocalOnlyBehaviours(bridge, controller, hud, camera, cam.GetComponent<AudioListener>());

        prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        UnityEngine.Object.DestroyImmediate(go);
        AssetDatabase.SaveAssets();
        return prefab;
    }

    private static void TryAssignPlayerPrefab(GameObject manager, GameObject prefab)
    {
        if (manager == null || prefab == null) return;
        Type networkManagerType = FindType("Mirror.NetworkManager");
        if (networkManagerType == null) return;
        Component nm = manager.GetComponent(networkManagerType);
        if (nm == null) return;

        SerializedObject so = new SerializedObject(nm);
        string[] propertyNames = { "playerPrefab", "_playerPrefab" };
        for (int i = 0; i < propertyNames.Length; i++)
        {
            SerializedProperty prop = so.FindProperty(propertyNames[i]);
            if (prop != null && prop.propertyType == SerializedPropertyType.ObjectReference)
            {
                prop.objectReferenceValue = prefab;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(nm);
                return;
            }
        }

        FieldInfo field = networkManagerType.GetField("playerPrefab", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null) field.SetValue(nm, prefab);
        EditorUtility.SetDirty(nm);
    }

    private static void TrySetLocalOnlyBehaviours(OUTL_MirrorEntityBridge bridge, params Behaviour[] behaviours)
    {
        if (bridge == null) return;
        SerializedObject so = new SerializedObject(bridge);
        SerializedProperty prop = so.FindProperty("LocalOnlyBehaviours");
        if (prop == null || !prop.isArray) return;
        prop.arraySize = behaviours.Length;
        for (int i = 0; i < behaviours.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = behaviours[i];
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(bridge);
    }

    private static Type FindType(string fullName)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            Type t = assemblies[i].GetType(fullName);
            if (t != null) return t;
        }
        return null;
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
#endif