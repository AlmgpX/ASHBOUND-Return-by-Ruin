#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class OUT_ZombieTemplateEditor
{
    private const string MenuRoot = "OUT CORE (Legacy)/Zombies/";
    private const string DefaultFolder = "Assets/OUT/OUT_Core/Templates/Zombies";

    [MenuItem(MenuRoot + "Create Basic Zombie Profile")]
    public static void CreateBasicZombieProfile()
    {
        EnsureFolder(DefaultFolder);
        string path = AssetDatabase.GenerateUniqueAssetPath(DefaultFolder + "/OUT_BasicZombie_Profile.asset");
        OUT_ZombieHordeProfile profile = ScriptableObject.CreateInstance<OUT_ZombieHordeProfile>();
        ApplyDefaultProfileValues(profile);
        AssetDatabase.CreateAsset(profile, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = profile;
        EditorGUIUtility.PingObject(profile);
    }

    [MenuItem(MenuRoot + "Create Basic Zombie Scene Object")]
    public static void CreateBasicZombieSceneObject()
    {
        OUT_ZombieHordeProfile profile = GetOrCreateDefaultProfile();
        GameObject zombie = CreateZombieGameObject(profile);
        Undo.RegisterCreatedObjectUndo(zombie, "Create OUT Basic Zombie");
        Selection.activeGameObject = zombie;
        EditorGUIUtility.PingObject(zombie);
    }

    [MenuItem(MenuRoot + "Create Basic Zombie Prefab Template")]
    public static void CreateBasicZombiePrefabTemplate()
    {
        EnsureFolder(DefaultFolder);
        OUT_ZombieHordeProfile profile = GetOrCreateDefaultProfile();
        GameObject zombie = CreateZombieGameObject(profile);
        string prefabPath = AssetDatabase.GenerateUniqueAssetPath(DefaultFolder + "/OUT_BasicZombie.prefab");
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(zombie, prefabPath);
        Object.DestroyImmediate(zombie);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
    }

    [MenuItem(MenuRoot + "Create Horde Runtime Setup")]
    public static void CreateHordeRuntimeSetup()
    {
        GameObject root = GameObject.Find("OUT_ZombieHorde_Runtime");
        if (root == null)
        {
            root = new GameObject("OUT_ZombieHorde_Runtime");
            Undo.RegisterCreatedObjectUndo(root, "Create OUT Zombie Horde Runtime");
        }

        EnsureComponent<OUT_ZombieTargetHub>(root);
        EnsureComponent<OUT_ZombieHordeSystem>(root);

        if (OUT_RuntimePoolService.Instance == null && Object.FindObjectOfType<OUT_RuntimePoolService>() == null)
            root.AddComponent<OUT_RuntimePoolService>();

        if (OUT_SimulationService.Instance == null && Object.FindObjectOfType<OUT_SimulationService>() == null)
            root.AddComponent<OUT_SimulationService>();

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
    }

    [MenuItem(MenuRoot + "Create Zombie Target From Selection")]
    public static void CreateZombieTargetFromSelection()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("OUT Zombie Target", "Select a GameObject first. Even Unity needs one concrete thing to poke.", "OK");
            return;
        }

        OUT_ZombieTarget target = go.GetComponent<OUT_ZombieTarget>();
        if (target == null)
        {
            Undo.AddComponent<OUT_ZombieTarget>(go);
            target = go.GetComponent<OUT_ZombieTarget>();
        }

        target.IsActiveTarget = true;
        target.Priority = Mathf.Max(1f, target.Priority);
        EditorUtility.SetDirty(target);
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
    }

    private static GameObject CreateZombieGameObject(OUT_ZombieHordeProfile profile)
    {
        GameObject root = new GameObject("OUT_BasicZombie");
        root.tag = "Untagged";
        root.layer = 0;

        CharacterController controller = root.AddComponent<CharacterController>();
        controller.height = 1.85f;
        controller.radius = 0.32f;
        controller.center = new Vector3(0f, 0.92f, 0f);
        controller.stepOffset = 0.25f;
        controller.slopeLimit = 45f;
        controller.skinWidth = 0.04f;

        AudioSource audio = root.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.loop = false;
        audio.spatialBlend = 1f;
        audio.rolloffMode = AudioRolloffMode.Logarithmic;
        audio.minDistance = 2f;
        audio.maxDistance = 28f;

        OUT_EntityAdapter entity = root.AddComponent<OUT_EntityAdapter>();
        OUT_ZombieHordeAgent agent = root.AddComponent<OUT_ZombieHordeAgent>();
        agent.Profile = profile;
        agent.CharacterController = controller;
        agent.BodyCollider = controller;
        agent.AudioSource = audio;
        agent.UseAnimator = true;
        agent.UseAudio = true;
        agent.UseGibsOnDeath = true;
        agent.RegisterOnEnable = true;

        GameObject visualRoot = new GameObject("VisualRoot");
        visualRoot.transform.SetParent(root.transform, false);
        visualRoot.transform.localPosition = Vector3.zero;
        visualRoot.transform.localRotation = Quaternion.identity;
        visualRoot.transform.localScale = Vector3.one;
        agent.VisualRoot = visualRoot.transform;

        GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        placeholder.name = "Zombie_Body_PLACEHOLDER_replace_with_model";
        placeholder.transform.SetParent(visualRoot.transform, false);
        placeholder.transform.localPosition = new Vector3(0f, 0.92f, 0f);
        placeholder.transform.localRotation = Quaternion.identity;
        placeholder.transform.localScale = new Vector3(0.65f, 0.92f, 0.65f);

        Collider placeholderCollider = placeholder.GetComponent<Collider>();
        if (placeholderCollider != null)
            Object.DestroyImmediate(placeholderCollider);

        GameObject attackPoint = new GameObject("AttackPoint_optional");
        attackPoint.transform.SetParent(root.transform, false);
        attackPoint.transform.localPosition = new Vector3(0f, 1.25f, 0.65f);

        GameObject audioRoot = new GameObject("AudioClips_DROP_HERE_optional");
        audioRoot.transform.SetParent(root.transform, false);

        GameObject vfxRoot = new GameObject("VFX_DROP_Gib_Hit_prefabs_into_Profile");
        vfxRoot.transform.SetParent(root.transform, false);

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(agent);
        EditorUtility.SetDirty(entity);
        return root;
    }

    private static OUT_ZombieHordeProfile GetOrCreateDefaultProfile()
    {
        EnsureFolder(DefaultFolder);

        string[] guids = AssetDatabase.FindAssets("t:OUT_ZombieHordeProfile OUT_BasicZombie_Profile", new[] { DefaultFolder });
        if (guids != null && guids.Length > 0)
        {
            string existingPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            OUT_ZombieHordeProfile existing = AssetDatabase.LoadAssetAtPath<OUT_ZombieHordeProfile>(existingPath);
            if (existing != null)
                return existing;
        }

        string path = AssetDatabase.GenerateUniqueAssetPath(DefaultFolder + "/OUT_BasicZombie_Profile.asset");
        OUT_ZombieHordeProfile profile = ScriptableObject.CreateInstance<OUT_ZombieHordeProfile>();
        ApplyDefaultProfileValues(profile);
        AssetDatabase.CreateAsset(profile, path);
        AssetDatabase.SaveAssets();
        return profile;
    }

    private static void ApplyDefaultProfileValues(OUT_ZombieHordeProfile profile)
    {
        if (profile == null)
            return;

        profile.WalkSpeed = 1.65f;
        profile.RunSpeed = 3.7f;
        profile.RotationSpeed = 520f;
        profile.StopDistance = 1.15f;
        profile.AttackRange = 1.35f;
        profile.AttackInterval = 0.85f;
        profile.AttackDamage = 8;
        profile.MaxHealth = 35;
        profile.HideInsteadOfDestroy = true;

        profile.NearDistance = 45f;
        profile.MidDistance = 160f;
        profile.FarDistance = 420f;
        profile.NearThinkInterval = 0.05f;
        profile.MidThinkInterval = 0.25f;
        profile.FarThinkInterval = 1f;

        profile.AudioMaxDistance = 42f;
        profile.MoanMinInterval = 3f;
        profile.MoanMaxInterval = 9f;
        profile.MoanChance = 0.18f;

        profile.AnimatorSpeedFloat = "Speed";
        profile.AnimatorAttackTrigger = "Attack";
        profile.AnimatorHitTrigger = "Hit";
        profile.AnimatorDeathTrigger = "Death";
        profile.AnimatorAliveBool = "Alive";

        EditorUtility.SetDirty(profile);
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        if (component == null)
            component = Undo.AddComponent<T>(go);
        return component;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
