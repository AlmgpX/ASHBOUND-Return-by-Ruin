#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OUT_SoldierVisualTemplateMenu
{
    private const string MenuRoot = "OUT CORE (Legacy)/AI/Setup/";
    private const string DefaultSoldierFaction = "hecu";
    private const string DefaultPlayerFaction = "player";

    [MenuItem(MenuRoot + "Fix Selected Soldiers Simple", priority = 1)]
    public static void FixSelectedSoldiersSimple()
    {
        OUT_AIActorBrain[] brains = GetSelectedBrains();
        if (brains.Length == 0)
        {
            Debug.LogWarning("Select a soldier or SoldierSquad root first.");
            return;
        }

        OUT_EntityMindProfile defaultMind = FindMindProfile();
        for (int i = 0; i < brains.Length; i++)
            FixSoldier(brains[i].gameObject, defaultMind);

        Debug.Log("OUT CORE: fixed simple soldiers: " + brains.Length + ". Faction: " + DefaultSoldierFaction + " hostile to " + DefaultPlayerFaction + ".");
        MarkSceneDirty();
    }

    [MenuItem(MenuRoot + "Create Simple Visual For Selected Soldiers", priority = 2)]
    public static void CreateSimpleVisuals()
    {
        OUT_AIActorBrain[] brains = GetSelectedBrains();
        if (brains.Length == 0)
        {
            Debug.LogWarning("Select a soldier or SoldierSquad root first.");
            return;
        }

        for (int i = 0; i < brains.Length; i++)
            CreateSimpleVisual(brains[i].gameObject);

        Debug.Log("OUT CORE: created simple soldier visuals: " + brains.Length);
        MarkSceneDirty();
    }

    [MenuItem(MenuRoot + "Fix Selected Player Faction", priority = 3)]
    public static void FixSelectedPlayerFaction()
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogWarning("Select player root first.");
            return;
        }

        OUT_FactionAgent faction = GetOrAdd<OUT_FactionAgent>(Selection.activeGameObject);
        SetString(faction, "factionId", DefaultPlayerFaction);
        SetString(faction, "displayName", DefaultPlayerFaction);
        SetStringArray(faction, "hostileFactionIds", new[] { DefaultSoldierFaction });
        SetBool(faction, "treatUnlistedAsNeutral", true);
        SetBool(faction, "reciprocalHostility", true);

        Debug.Log("OUT CORE: fixed player faction. Player is hostile to " + DefaultSoldierFaction + ".");
        MarkSceneDirty();
    }

    private static void FixSoldier(GameObject soldier, OUT_EntityMindProfile defaultMind)
    {
        CreateSimpleVisual(soldier);

        OUT_SquadPatrolScheduleResolver obsoleteResolver = soldier.GetComponent<OUT_SquadPatrolScheduleResolver>();
        if (obsoleteResolver != null)
            Undo.DestroyObjectImmediate(obsoleteResolver);

        OUT_FactionAgent faction = GetOrAdd<OUT_FactionAgent>(soldier);
        SetString(faction, "factionId", DefaultSoldierFaction);
        SetString(faction, "displayName", soldier.name);
        SetStringArray(faction, "hostileFactionIds", new[] { DefaultPlayerFaction });
        SetStringArray(faction, "friendlyFactionIds", new[] { DefaultSoldierFaction });
        SetBool(faction, "treatUnlistedAsNeutral", true);
        SetBool(faction, "reciprocalHostility", true);

        OUT_EntityMind mind = GetOrAdd<OUT_EntityMind>(soldier);
        OUT_AIEntityMemory entityMemory = GetOrAdd<OUT_AIEntityMemory>(soldier);
        OUT_AIMemoryBuffer memoryBuffer = GetOrAdd<OUT_AIMemoryBuffer>(soldier);
        OUT_AIActorBrain brain = GetOrAdd<OUT_AIActorBrain>(soldier);
        SetObject(mind, "profile", defaultMind);
        SetObject(mind, "entityMemory", entityMemory);
        SetObject(mind, "brain", brain);
        SetObject(mind, "aiMemoryBuffer", memoryBuffer);
        SetObject(entityMemory, "profile", defaultMind);

        OUT_AIPerception perception = GetOrAdd<OUT_AIPerception>(soldier);
        SetBool(perception, "requireDamageableTarget", true);
        SetBool(perception, "requireHostileFaction", true);
        SetInt(perception, "targetMask", GetLayerMaskOrEverything("Player"));
        SetFloat(perception, "sightDistance", 45f);
        SetFloat(perception, "fieldOfView", 150f);

        OUT_SoldierScheduleResolver soldierResolver = GetOrAdd<OUT_SoldierScheduleResolver>(soldier);
        SetBool(soldierResolver, "exploreWhenIdle", false);

        OUT_AIPatrolScheduleResolver patrolResolver = GetOrAdd<OUT_AIPatrolScheduleResolver>(soldier);
        OUT_SoldierSquadAgent squadAgent = soldier.GetComponent<OUT_SoldierSquadAgent>();
        OUT_SoldierSquadCommander commander = squadAgent != null ? squadAgent.Commander : soldier.GetComponentInParent<OUT_SoldierSquadCommander>();

        SetObject(patrolResolver, "fallbackResolverBehaviour", soldierResolver);
        SetObject(patrolResolver, "patrolProviderBehaviour", commander);
        SetObject(patrolResolver, "squadAgent", squadAgent);
        SetObject(patrolResolver, "squadCommander", commander);
        SetBool(patrolResolver, "useSquadSlotsWhenProviderIsCommander", true);
        SetBool(patrolResolver, "patrolWhenNoEnemy", true);
        SetFloat(patrolResolver, "slotRepathDistance", 2.25f);
        SetFloat(patrolResolver, "waitNearSlot", 0.25f);
        SetObject(brain, "scheduleResolver", patrolResolver);

        if (commander != null)
        {
            SetFloat(commander, "frontSpacing", 3f);
            SetFloat(commander, "sideSpacing", 2f);
            SetFloat(commander, "retreatDistance", 6f);
            SetFloat(commander, "regroupRadius", 3f);
            SetBool(commander, "useForcedOrder", false);
        }
    }

    private static void CreateSimpleVisual(GameObject soldier)
    {
        GameObject visualRoot = GetOrCreateChild(soldier.transform, "VisualRoot");
        visualRoot.transform.localPosition = Vector3.zero;
        visualRoot.transform.localRotation = Quaternion.identity;

        GameObject body = GetOrCreatePrimitive(visualRoot.transform, "BodyCapsule", PrimitiveType.Capsule);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        body.transform.localScale = new Vector3(0.65f, 1f, 0.65f);
        RemoveCollider(body);

        GameObject head = GetOrCreatePrimitive(visualRoot.transform, "LookHead", PrimitiveType.Sphere);
        head.transform.localPosition = new Vector3(0f, 1.85f, 0.18f);
        head.transform.localScale = new Vector3(0.32f, 0.24f, 0.32f);
        RemoveCollider(head);

        GameObject nose = GetOrCreatePrimitive(head.transform, "ForwardNose", PrimitiveType.Cube);
        nose.transform.localPosition = new Vector3(0f, 0f, 0.55f);
        nose.transform.localScale = new Vector3(0.18f, 0.12f, 0.35f);
        RemoveCollider(nose);

        GameObject weaponSocket = GetOrCreateChild(visualRoot.transform, "WeaponSocket");
        weaponSocket.transform.localPosition = new Vector3(0.25f, 1.25f, 0.45f);
        weaponSocket.transform.localRotation = Quaternion.identity;

        GameObject gun = GetOrCreatePrimitive(weaponSocket.transform, "DebugRifle", PrimitiveType.Cube);
        gun.transform.localPosition = new Vector3(0f, 0f, 0.22f);
        gun.transform.localScale = new Vector3(0.12f, 0.12f, 0.65f);
        RemoveCollider(gun);

        GameObject muzzle = GetOrCreateChild(weaponSocket.transform, "MuzzleSocket");
        muzzle.transform.localPosition = new Vector3(0f, 0f, 0.62f);
        muzzle.transform.localRotation = Quaternion.identity;

        OUT_WeaponController weapon = soldier.GetComponent<OUT_WeaponController>();
        if (weapon != null)
            SetObject(weapon, "fireOrigin", muzzle.transform);

        OUT_AIPerception perception = soldier.GetComponent<OUT_AIPerception>();
        Transform eyePoint = soldier.transform.Find("EyePoint");
        if (perception != null && eyePoint != null)
            SetObject(perception, "eyePoint", eyePoint);
    }

    private static OUT_AIActorBrain[] GetSelectedBrains()
    {
        System.Collections.Generic.List<OUT_AIActorBrain> result = new System.Collections.Generic.List<OUT_AIActorBrain>();
        foreach (GameObject go in Selection.gameObjects)
        {
            OUT_AIActorBrain[] brains = go.GetComponentsInChildren<OUT_AIActorBrain>(true);
            for (int i = 0; i < brains.Length; i++)
            {
                if (brains[i] != null && !result.Contains(brains[i]))
                    result.Add(brains[i]);
            }
        }
        return result.ToArray();
    }

    private static OUT_EntityMindProfile FindMindProfile()
    {
        string[] guids = AssetDatabase.FindAssets("t:OUT_EntityMindProfile OUT_EntityMindProfile_BaseHumanoid");
        if (guids == null || guids.Length == 0)
            guids = AssetDatabase.FindAssets("t:OUT_EntityMindProfile");

        if (guids == null || guids.Length == 0)
            return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<OUT_EntityMindProfile>(path);
    }

    private static int GetLayerMaskOrEverything(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        return layer >= 0 ? 1 << layer : ~0;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T existing = go.GetComponent<T>();
        return existing != null ? existing : Undo.AddComponent<T>(go);
    }

    private static GameObject GetOrCreateChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing.gameObject;
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject GetOrCreatePrimitive(Transform parent, string name, PrimitiveType type)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing.gameObject;
        GameObject go = GameObject.CreatePrimitive(type);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.name = name;
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void RemoveCollider(GameObject go)
    {
        Collider c = go.GetComponent<Collider>();
        if (c != null)
            Object.DestroyImmediate(c);
    }

    private static void SetBool(Component component, string propertyName, bool value) => SetProperty(component, propertyName, value);
    private static void SetFloat(Component component, string propertyName, float value) => SetProperty(component, propertyName, value);
    private static void SetInt(Component component, string propertyName, int value) => SetProperty(component, propertyName, value);
    private static void SetString(Component component, string propertyName, string value) => SetProperty(component, propertyName, value);
    private static void SetObject(Component component, string propertyName, Object value) => SetProperty(component, propertyName, value);

    private static void SetStringArray(Component component, string propertyName, string[] values)
    {
        if (component == null)
            return;

        SerializedObject so = new SerializedObject(component);
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null || !property.isArray)
            return;

        property.arraySize = values != null ? values.Length : 0;
        for (int i = 0; i < property.arraySize; i++)
            property.GetArrayElementAtIndex(i).stringValue = values[i];

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(component);
    }

    private static void SetProperty(Component component, string propertyName, object value)
    {
        if (component == null)
            return;
        SerializedObject so = new SerializedObject(component);
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
            return;
        switch (property.propertyType)
        {
            case SerializedPropertyType.Boolean:
                property.boolValue = value is bool b && b;
                break;
            case SerializedPropertyType.Float:
                property.floatValue = value is float f ? f : 0f;
                break;
            case SerializedPropertyType.Integer:
            case SerializedPropertyType.Enum:
            case SerializedPropertyType.LayerMask:
                property.intValue = value is int i ? i : 0;
                break;
            case SerializedPropertyType.String:
                property.stringValue = value != null ? value.ToString() : string.Empty;
                break;
            case SerializedPropertyType.ObjectReference:
                property.objectReferenceValue = value as Object;
                break;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(component);
    }

    private static void MarkSceneDirty()
    {
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }
}
#endif
