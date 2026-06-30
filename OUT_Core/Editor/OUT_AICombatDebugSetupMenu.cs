#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OUT_AICombatDebugSetupMenu
{
    private const string MenuRoot = "OUT CORE (Legacy)/AI/Debug/";

    [MenuItem(MenuRoot + "Create Combat Test Target", priority = 1)]
    public static void CreateCombatTestTarget()
    {
        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Undo.RegisterCreatedObjectUndo(target, "Create OUT Combat Test Target");
        target.name = "OUT_CombatTest_Target";
        target.transform.position = GetSceneDropPosition();
        target.transform.localScale = new Vector3(1f, 1f, 1f);
        TryAssignLayer(target, "Player");

        OUT_HealthSimple health = Undo.AddComponent<OUT_HealthSimple>(target);
        SetInt(health, "maxHealth", 250);
        SetInt(health, "startingHealth", 250);
        SetBool(health, "resetHealthOnEnable", true);

        Selection.activeGameObject = target;
        EditorGUIUtility.PingObject(target);
        MarkSceneDirty();
    }

    [MenuItem(MenuRoot + "Fix Selected Soldiers For Combat Test", priority = 2)]
    public static void FixSelectedSoldiersForCombatTest()
    {
        OUT_AIActorBrain[] brains = GetSelectedBrains();
        if (brains == null || brains.Length == 0)
        {
            Debug.LogWarning("Select soldier or squad root first.");
            return;
        }

        for (int i = 0; i < brains.Length; i++)
            FixSoldier(brains[i].gameObject);

        Debug.Log("Fixed OUT soldier combat test setup: " + brains.Length);
        MarkSceneDirty();
    }

    [MenuItem(MenuRoot + "Add Patrol Route To Selected Soldiers", priority = 3)]
    public static void AddPatrolRouteToSelectedSoldiers()
    {
        OUT_AIActorBrain[] brains = GetSelectedBrains();
        if (brains == null || brains.Length == 0)
        {
            Debug.LogWarning("Select soldier or squad root first.");
            return;
        }

        for (int i = 0; i < brains.Length; i++)
            AddPatrol(brains[i].gameObject, i);

        Debug.Log("Added OUT patrol routes to soldiers: " + brains.Length);
        MarkSceneDirty();
    }

    private static void FixSoldier(GameObject soldier)
    {
        OUT_AIPerception perception = soldier.GetComponent<OUT_AIPerception>();
        if (perception != null)
        {
            SetBool(perception, "requireDamageableTarget", true);
            SetInt(perception, "targetMask", GetLayerMaskOrEverything("Player"));
        }

        OUT_WeaponController weapon = soldier.GetComponent<OUT_WeaponController>();
        if (weapon != null)
        {
            Transform fireOrigin = soldier.transform.Find("FireOrigin");
            if (fireOrigin != null)
                SetObject(weapon, "fireOrigin", fireOrigin);
            SetInt(weapon, "aimMode", 3);
            ConfigureWeaponProfile(weapon, "primary", false);
            ConfigureWeaponProfile(weapon, "secondary", true);
        }

        OUT_SoldierAttackEvaluator evaluator = soldier.GetComponent<OUT_SoldierAttackEvaluator>();
        if (evaluator != null)
        {
            SetInt(evaluator, "visibilityMask", ~0);
            SetFloat(evaluator, "friendlyFireRadius", 0.45f);
            SetFloat(evaluator, "maxViewDistance", 80f);
        }
    }

    private static void AddPatrol(GameObject soldier, int index)
    {
        OUT_SoldierScheduleResolver soldierResolver = soldier.GetComponent<OUT_SoldierScheduleResolver>();
        OUT_AIPatrolRoute route = GetOrAdd<OUT_AIPatrolRoute>(soldier);
        OUT_AIPatrolScheduleResolver patrolResolver = GetOrAdd<OUT_AIPatrolScheduleResolver>(soldier);
        OUT_AIActorBrain brain = soldier.GetComponent<OUT_AIActorBrain>();

        Transform pointsRoot = soldier.transform.Find("PatrolPoints");
        if (pointsRoot == null)
        {
            GameObject root = new GameObject("PatrolPoints");
            Undo.RegisterCreatedObjectUndo(root, "Create OUT Patrol Points");
            root.transform.SetParent(soldier.transform, false);
            pointsRoot = root.transform;
        }

        float phase = index * 1.35f;
        Transform[] points = new Transform[4];
        points[0] = GetOrCreatePoint(pointsRoot, "P0", new Vector3(Mathf.Cos(phase) * 3f, 0f, Mathf.Sin(phase) * 3f));
        points[1] = GetOrCreatePoint(pointsRoot, "P1", new Vector3(Mathf.Cos(phase + 1.57f) * 5f, 0f, Mathf.Sin(phase + 1.57f) * 5f));
        points[2] = GetOrCreatePoint(pointsRoot, "P2", new Vector3(Mathf.Cos(phase + 3.14f) * 3f, 0f, Mathf.Sin(phase + 3.14f) * 3f));
        points[3] = GetOrCreatePoint(pointsRoot, "P3", new Vector3(Mathf.Cos(phase + 4.71f) * 5f, 0f, Mathf.Sin(phase + 4.71f) * 5f));

        SetTransformArray(route, "points", points);
        SetInt(route, "patrolMode", 0);
        SetInt(route, "startMode", 1);
        SetFloat(route, "waitTimeMin", 0.35f);
        SetFloat(route, "waitTimeMax", 1.15f);

        SetObject(patrolResolver, "fallbackResolverBehaviour", soldierResolver);
        SetObject(patrolResolver, "patrolProviderBehaviour", route);
        SetBool(patrolResolver, "patrolWhenNoEnemy", true);
        SetFloat(patrolResolver, "reachDistance", 1.1f);

        if (brain != null)
            SetObject(brain, "scheduleResolver", patrolResolver);
    }

    private static Transform GetOrCreatePoint(Transform parent, string name, Vector3 localPosition)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            existing.localPosition = localPosition;
            return existing;
        }

        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create OUT Patrol Point");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        return go.transform;
    }

    private static OUT_AIActorBrain[] GetSelectedBrains()
    {
        if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
            return new OUT_AIActorBrain[0];

        System.Collections.Generic.List<OUT_AIActorBrain> result = new System.Collections.Generic.List<OUT_AIActorBrain>();
        for (int i = 0; i < Selection.gameObjects.Length; i++)
        {
            OUT_AIActorBrain[] children = Selection.gameObjects[i].GetComponentsInChildren<OUT_AIActorBrain>(true);
            for (int j = 0; j < children.Length; j++)
            {
                if (children[j] != null && !result.Contains(children[j]))
                    result.Add(children[j]);
            }
        }
        return result.ToArray();
    }

    private static Vector3 GetSceneDropPosition()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null && sceneView.camera != null)
            return sceneView.camera.transform.position + sceneView.camera.transform.forward * 8f;

        return new Vector3(0f, 1f, 8f);
    }

    private static void TryAssignLayer(GameObject go, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0)
            go.layer = layer;
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

    private static void ConfigureWeaponProfile(OUT_WeaponController weapon, string profileName, bool secondary)
    {
        SerializedObject so = new SerializedObject(weapon);
        SerializedProperty profile = so.FindProperty(profileName);
        if (profile == null)
            return;

        SetRelative(profile, "AttackMode", 1);
        SetRelative(profile, "Damage", secondary ? 18 : 8);
        SetRelative(profile, "FireInterval", secondary ? 1.2f : 0.12f);
        SetRelative(profile, "ShotsPerBurst", secondary ? 1 : 3);
        SetRelative(profile, "BurstShotInterval", secondary ? 0f : 0.055f);
        SetRelative(profile, "ClipSize", secondary ? 8 : 30);
        SetRelative(profile, "AmmoInClip", secondary ? 8 : 30);
        SetRelative(profile, "ReserveAmmo", secondary ? 16 : 120);
        SetRelative(profile, "MaxDistance", secondary ? 72f : 96f);
        SetRelative(profile, "HitMask", ~0);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(weapon);
    }

    private static void SetRelative(SerializedProperty parent, string relativeName, int value)
    {
        SerializedProperty p = parent.FindPropertyRelative(relativeName);
        if (p != null)
            p.intValue = value;
    }

    private static void SetRelative(SerializedProperty parent, string relativeName, float value)
    {
        SerializedProperty p = parent.FindPropertyRelative(relativeName);
        if (p != null)
            p.floatValue = value;
    }

    private static void SetTransformArray(Component component, string propertyName, Transform[] values)
    {
        SerializedObject so = new SerializedObject(component);
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null || !property.isArray)
            return;

        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(component);
    }

    private static void SetBool(Component component, string propertyName, bool value) => SetProperty(component, propertyName, value);
    private static void SetInt(Component component, string propertyName, int value) => SetProperty(component, propertyName, value);
    private static void SetFloat(Component component, string propertyName, float value) => SetProperty(component, propertyName, value);
    private static void SetObject(Component component, string propertyName, Object value) => SetProperty(component, propertyName, value);

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
            case SerializedPropertyType.Integer:
            case SerializedPropertyType.Enum:
            case SerializedPropertyType.LayerMask:
                property.intValue = value is int i ? i : 0;
                break;
            case SerializedPropertyType.Float:
                property.floatValue = value is float f ? f : 0f;
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
