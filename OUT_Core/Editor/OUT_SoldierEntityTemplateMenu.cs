#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OUT_SoldierEntityTemplateMenu
{
    private const string MenuRoot = "OUT CORE (Legacy)/AI/";

    [MenuItem(MenuRoot + "Create Soldier Entity", priority = 10)]
    public static void CreateSoldierEntity()
    {
        Transform parent = GetDefaultSoldierParent();
        GameObject soldier = CreateSoldier(parent, "OUT_Soldier", OUT_SoldierRole.Rifleman, Vector3.zero, GetCommanderFromSelectionOrParent(parent));
        Selection.activeGameObject = soldier;
        EditorGUIUtility.PingObject(soldier);
        MarkSceneDirty();
    }

    [MenuItem(MenuRoot + "Create Soldier Squad x4", priority = 11)]
    public static void CreateSoldierSquadX4()
    {
        Transform aiRoot = GetDefaultSoldierParent();
        GameObject squadRoot = GetOrCreateChild(aiRoot, "SoldierSquad_01");
        OUT_SoldierSquadCommander commander = GetOrAdd<OUT_SoldierSquadCommander>(squadRoot);
        SetInt(commander, "maxMembers", 4);
        SetFloat(commander, "frontSpacing", 6f);
        SetFloat(commander, "sideSpacing", 4f);
        SetFloat(commander, "retreatDistance", 12f);
        SetFloat(commander, "regroupRadius", 8f);

        CreateSoldier(squadRoot.transform, "Soldier_01_Commander", OUT_SoldierRole.Commander, new Vector3(-3f, 0f, 0f), commander);
        CreateSoldier(squadRoot.transform, "Soldier_02_Rifleman", OUT_SoldierRole.Rifleman, new Vector3(3f, 0f, 0f), commander);
        CreateSoldier(squadRoot.transform, "Soldier_03_Rifleman", OUT_SoldierRole.Rifleman, new Vector3(-2f, 0f, -4f), commander);
        CreateSoldier(squadRoot.transform, "Soldier_04_Demolitions", OUT_SoldierRole.Demolitions, new Vector3(2f, 0f, -4f), commander);

        Selection.activeGameObject = squadRoot;
        EditorGUIUtility.PingObject(squadRoot);
        MarkSceneDirty();

        Debug.Log("OUT CORE soldier squad template created. Four little armed abstractions, because apparently one haunted MonoBehaviour wasn't enough.");
    }

    [MenuItem(MenuRoot + "Add OUT Health To Selected Target", priority = 30)]
    public static void AddHealthToSelectedTarget()
    {
        GameObject target = Selection.activeGameObject;
        if (target == null)
        {
            Debug.LogWarning("Select a GameObject first. Even Unity cannot bless pure nothingness with health.");
            return;
        }

        if (target.GetComponent<Collider>() == null && target.GetComponentInChildren<Collider>() == null)
        {
            CapsuleCollider collider = Undo.AddComponent<CapsuleCollider>(target);
            collider.center = new Vector3(0f, 1f, 0f);
            collider.height = 2f;
            collider.radius = 0.35f;
        }

        OUT_HealthSimple health = GetOrAdd<OUT_HealthSimple>(target);
        SetInt(health, "maxHealth", 100);
        SetInt(health, "startingHealth", 100);
        SetBool(health, "resetHealthOnEnable", true);

        EditorUtility.SetDirty(target);
        MarkSceneDirty();
    }

    [MenuItem(MenuRoot + "Select Soldier Squad Root", priority = 50)]
    public static void SelectSoldierSquadRoot()
    {
        GameObject root = GameObject.Find("SoldierSquad_01");
        if (root == null)
        {
            Debug.LogWarning("SoldierSquad_01 not found. This is a legacy OUT CORE authoring tool.");
            return;
        }

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
    }

    private static GameObject CreateSoldier(Transform parent, string name, OUT_SoldierRole role, Vector3 localPosition, OUT_SoldierSquadCommander commander)
    {
        GameObject soldier = FindChild(parent, name);
        if (soldier == null)
        {
            soldier = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(soldier, "Create OUT Soldier");
            soldier.transform.SetParent(parent, false);
        }

        soldier.transform.localPosition = localPosition;
        soldier.transform.localRotation = Quaternion.identity;

        CharacterController controller = GetOrAdd<CharacterController>(soldier);
        controller.center = new Vector3(0f, 1f, 0f);
        controller.height = 2f;
        controller.radius = 0.35f;
        controller.stepOffset = 0.35f;

        AudioSource audio = GetOrAdd<AudioSource>(soldier);
        audio.playOnAwake = false;
        audio.spatialBlend = 1f;

        GameObject eye = GetOrCreateChild(soldier.transform, "EyePoint");
        eye.transform.localPosition = new Vector3(0f, 1.62f, 0.08f);

        GameObject fireOrigin = GetOrCreateChild(soldier.transform, "FireOrigin");
        fireOrigin.transform.localPosition = new Vector3(0.22f, 1.35f, 0.55f);

        OUT_SaveableEntity saveable = GetOrAdd<OUT_SaveableEntity>(soldier);
        SetString(saveable, "saveId", "ai/soldiers/" + name.ToLowerInvariant());
        SetBool(saveable, "autoGenerateFromHierarchyPath", false);
        SetBool(saveable, "saveTransform", true);
        SetBool(saveable, "saveActiveState", true);

        OUT_HealthSimple health = GetOrAdd<OUT_HealthSimple>(soldier);
        SetInt(health, "maxHealth", role == OUT_SoldierRole.Commander ? 130 : 100);
        SetInt(health, "startingHealth", role == OUT_SoldierRole.Commander ? 130 : 100);
        SetBool(health, "resetHealthOnEnable", true);

        OUT_AIMemoryProfile memoryProfile = GetOrAdd<OUT_AIMemoryProfile>(soldier);
        SetInt(memoryProfile, "maxMemories", 12);
        SetFloat(memoryProfile, "enemyMemorySeconds", 6f);
        SetFloat(memoryProfile, "interestMemorySeconds", 4f);
        SetFloat(memoryProfile, "dangerMemorySeconds", 8f);

        OUT_AIMemoryBuffer memoryBuffer = GetOrAdd<OUT_AIMemoryBuffer>(soldier);
        SetObject(memoryBuffer, "profile", memoryProfile);

        OUT_AIEntityMemory entityMemory = GetOrAdd<OUT_AIEntityMemory>(soldier);
        OUT_EgregoreInfluenceAgent egregoreInfluence = GetOrAdd<OUT_EgregoreInfluenceAgent>(soldier);
        SetObject(egregoreInfluence, "entityMemory", entityMemory);
        SetObject(egregoreInfluence, "aiMemoryBuffer", memoryBuffer);

        OUT_AIPerception perception = GetOrAdd<OUT_AIPerception>(soldier);
        SetObject(perception, "eyePoint", eye.transform);
        SetFloat(perception, "sightDistance", 35f);
        SetFloat(perception, "fieldOfView", 120f);
        SetBool(perception, "requireDamageableTarget", false);
        SetLayerMask(perception, "targetMask", GetPlayerLayerMaskOrEverything());

        OUT_AIHearingSensor hearing = GetOrAdd<OUT_AIHearingSensor>(soldier);
        SetObject(hearing, "memoryBuffer", memoryBuffer);

        OUT_AILocomotion_CharacterController locomotion = GetOrAdd<OUT_AILocomotion_CharacterController>(soldier);
        SetFloat(locomotion, "moveSpeed", role == OUT_SoldierRole.Commander ? 3.25f : 3.75f);
        SetFloat(locomotion, "separationRadius", 1.05f);
        SetFloat(locomotion, "separationStrength", 1.35f);

        OUT_AICrowdAgent crowdAgent = GetOrAdd<OUT_AICrowdAgent>(soldier);
        OUT_AIRoutePlanner_LocalGraph routePlanner = GetOrAdd<OUT_AIRoutePlanner_LocalGraph>(soldier);
        SetObject(routePlanner, "crowdAgent", crowdAgent);

        OUT_WeaponController weapon = GetOrAdd<OUT_WeaponController>(soldier);
        SetObject(weapon, "fireOrigin", fireOrigin.transform);
        SetObject(weapon, "audioSource", audio);
        SetInt(weapon, "aimMode", 3);
        ConfigureWeaponProfile(weapon, "primary", role);
        ConfigureWeaponProfile(weapon, "secondary", role);

        OUT_SoldierSquadAgent squadAgent = GetOrAdd<OUT_SoldierSquadAgent>(soldier);
        SetObject(squadAgent, "commander", commander);
        SetInt(squadAgent, "role", (int)role);
        SetFloat(squadAgent, "preferredMinRange", role == OUT_SoldierRole.Shotgunner ? 4f : 8f);
        SetFloat(squadAgent, "preferredMaxRange", role == OUT_SoldierRole.Shotgunner ? 16f : 28f);
        SetObject(squadAgent, "health", health);

        OUT_SoldierAttackEvaluator attackEvaluator = GetOrAdd<OUT_SoldierAttackEvaluator>(soldier);
        SetObject(attackEvaluator, "squadAgent", squadAgent);
        SetObject(attackEvaluator, "weapon", weapon);
        SetObject(attackEvaluator, "health", health);

        OUT_SoldierAttackExecutor attackExecutor = GetOrAdd<OUT_SoldierAttackExecutor>(soldier);
        SetObject(attackExecutor, "weapon", weapon);
        SetObject(attackExecutor, "attackEvaluator", attackEvaluator);

        OUT_AITaskRunner taskRunner = GetOrAdd<OUT_AITaskRunner>(soldier);
        SetObject(taskRunner, "attackExecutor", attackExecutor);

        OUT_SoldierScheduleResolver scheduleResolver = GetOrAdd<OUT_SoldierScheduleResolver>(soldier);
        SetObject(scheduleResolver, "squadAgent", squadAgent);
        SetObject(scheduleResolver, "weapon", weapon);
        SetObject(scheduleResolver, "routePlannerBehaviour", routePlanner);

        OUT_AIActorBrain brain = GetOrAdd<OUT_AIActorBrain>(soldier);
        SetObject(brain, "perception", perception);
        SetObject(brain, "hearingSensor", hearing);
        SetObject(brain, "memoryBuffer", memoryBuffer);
        SetObject(brain, "scheduleResolver", scheduleResolver);
        SetObject(brain, "taskRunner", taskRunner);
        SetObject(brain, "attackEvaluator", attackEvaluator);
        SetFloat(brain, "thinkInterval", 0.12f);
        SetBool(brain, "useRandomThinkInterval", true);
        SetFloat(brain, "thinkIntervalMin", 0.08f);
        SetFloat(brain, "thinkIntervalMax", 0.18f);

        if (commander != null)
            squadAgent.AssignCommander(commander);

        EditorUtility.SetDirty(soldier);
        return soldier;
    }

    private static void ConfigureWeaponProfile(OUT_WeaponController weapon, string profileName, OUT_SoldierRole role)
    {
        SerializedObject so = new SerializedObject(weapon);
        SerializedProperty profile = so.FindProperty(profileName);
        if (profile == null)
            return;

        bool secondary = profileName == "secondary";
        bool demolitions = role == OUT_SoldierRole.Demolitions;

        SetRelative(profile, "AttackMode", 1);
        SetRelative(profile, "Damage", secondary ? (demolitions ? 55 : 18) : 8);
        SetRelative(profile, "Impulse", secondary ? 8f : 4f);
        SetRelative(profile, "FireInterval", secondary ? 1.2f : 0.12f);
        SetRelative(profile, "ShotsPerBurst", secondary ? 1 : 3);
        SetRelative(profile, "BurstShotInterval", secondary ? 0f : 0.055f);
        SetRelative(profile, "ClipSize", secondary ? (demolitions ? 4 : 8) : 30);
        SetRelative(profile, "AmmoInClip", secondary ? (demolitions ? 4 : 8) : 30);
        SetRelative(profile, "ReserveAmmo", secondary ? (demolitions ? 8 : 16) : 120);
        SetRelative(profile, "AmmoPerShot", 1);
        SetRelative(profile, "ReloadDuration", secondary ? 2.2f : 1.6f);
        SetRelative(profile, "MaxDistance", secondary ? 72f : 96f);
        SetRelative(profile, "PelletCount", role == OUT_SoldierRole.Shotgunner && !secondary ? 6 : 1);

        SerializedProperty spread = profile.FindPropertyRelative("Spread");
        if (spread != null)
            spread.vector2Value = role == OUT_SoldierRole.Shotgunner && !secondary ? new Vector2(0.08f, 0.08f) : new Vector2(0.012f, 0.012f);

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(weapon);
    }

    private static void SetRelative(SerializedProperty parent, string relativeName, int value)
    {
        SerializedProperty p = parent.FindPropertyRelative(relativeName);
        if (p == null)
            return;
        p.intValue = value;
    }

    private static void SetRelative(SerializedProperty parent, string relativeName, float value)
    {
        SerializedProperty p = parent.FindPropertyRelative(relativeName);
        if (p == null)
            return;
        p.floatValue = value;
    }

    private static int GetPlayerLayerMaskOrEverything()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
            return 1 << playerLayer;

        return ~0;
    }

    private static OUT_SoldierSquadCommander GetCommanderFromSelectionOrParent(Transform fallbackParent)
    {
        if (Selection.activeGameObject != null)
        {
            OUT_SoldierSquadCommander selectedCommander = Selection.activeGameObject.GetComponentInParent<OUT_SoldierSquadCommander>();
            if (selectedCommander != null)
                return selectedCommander;
        }

        return fallbackParent != null ? fallbackParent.GetComponentInChildren<OUT_SoldierSquadCommander>() : null;
    }

    private static Transform GetDefaultSoldierParent()
    {
        if (Selection.activeTransform != null && Selection.activeTransform.GetComponent<OUT_SoldierSquadCommander>() != null)
            return Selection.activeTransform;

        GameObject root = GameObject.Find("OUT_CORE_SCENE_ROOT");
        if (root == null)
            root = new GameObject("OUT_CORE_SCENE_ROOT");

        GameObject ai = GetOrCreateChild(root.transform, "OUT_AI");
        GameObject squad = GetOrCreateChild(ai.transform, "SoldierSquad_01");
        return squad.transform;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T existing = go.GetComponent<T>();
        if (existing != null)
            return existing;

        return Undo.AddComponent<T>(go);
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

    private static void SetString(Component component, string propertyName, string value) => SetProperty(component, propertyName, value);
    private static void SetBool(Component component, string propertyName, bool value) => SetProperty(component, propertyName, value);
    private static void SetInt(Component component, string propertyName, int value) => SetProperty(component, propertyName, value);
    private static void SetFloat(Component component, string propertyName, float value) => SetProperty(component, propertyName, value);
    private static void SetLayerMask(Component component, string propertyName, int value) => SetProperty(component, propertyName, value);
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
                property.boolValue = value is bool boolValue && boolValue;
                break;
            case SerializedPropertyType.Integer:
            case SerializedPropertyType.Enum:
            case SerializedPropertyType.LayerMask:
                property.intValue = value is int intValue ? intValue : 0;
                break;
            case SerializedPropertyType.Float:
                property.floatValue = value is float floatValue ? floatValue : 0f;
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
