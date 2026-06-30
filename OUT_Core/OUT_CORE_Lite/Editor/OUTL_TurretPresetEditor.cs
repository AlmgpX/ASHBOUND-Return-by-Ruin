#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class OUTL_TurretPresetEditor
{
    private const string MenuRoot = "OUT CORE Lite/Legacy Demo/Scene/Combat/";
    private const string Folder = "Assets/OUT/OUT_Core/OUT_CORE_Lite/Templates/Create";

    // [MenuItem(MenuRoot + "Create Projectile Combat Emitter")]
    public static void CreateBasicProjectileTurret()
    {
        EnsureFolder(Folder);

        OUTL_EntityDef def = ScriptableObject.CreateInstance<OUTL_EntityDef>();
        def.ClassName = "role.combat_emitter.projectile";
        def.DisplayName = "Projectile Combat Emitter";
        def.Tags = new[] { "Entity", "Role.CombatEmitter", "Role.CommandSource", "CombatEmitter" };
        def.BaseStats = new[] { new OUTL_StatEntry { Key = "Health", Value = 100f }, new OUTL_StatEntry { Key = "Damage", Value = 12f } };
        AssetDatabase.CreateAsset(def, AssetDatabase.GenerateUniqueAssetPath(Folder + "/OUTL_CombatEmitter_Projectile_Def.asset"));

        OUTL_AttackProfile attack = ScriptableObject.CreateInstance<OUTL_AttackProfile>();
        attack.AttackId = "attack.combat_emitter.projectile";
        attack.Mode = OUTL_AttackMode.Projectile;
        attack.Damage = 12f;
        attack.Range = 35f;
        attack.Radius = 0.14f;
        attack.Cooldown = 0.35f;
        attack.ProjectileSpeed = 26f;
        attack.ProjectileLifetime = 5f;
        attack.ProjectileUsesGravity = false;
        attack.AimMode = OUTL_AimMode.PredictLinear;
        attack.UseTargetVelocityPrediction = true;
        attack.PredictionStrength = 0.65f;
        attack.MaxPredictionTime = 1f;
        attack.HorizontalSpreadDegrees = 2f;
        attack.VerticalSpreadDegrees = 1f;
        attack.HitDamageKey = "projectile";
        attack.HitMask = ~0;
        AssetDatabase.CreateAsset(attack, AssetDatabase.GenerateUniqueAssetPath(Folder + "/OUTL_Attack_CombatEmitterProjectile.asset"));

        GameObject projectileTemp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileTemp.name = "OUTL_Generic_Projectile";
        projectileTemp.transform.localScale = Vector3.one * 0.18f;
        OUTL_Projectile projectile = projectileTemp.AddComponent<OUTL_Projectile>();
        projectile.DestroyOnHit = true;
        string projectilePath = AssetDatabase.GenerateUniqueAssetPath(Folder + "/OUTL_Generic_Projectile.prefab");
        GameObject projectilePrefab = PrefabUtility.SaveAsPrefabAsset(projectileTemp, projectilePath);
        Object.DestroyImmediate(projectileTemp);
        attack.ProjectilePrefab = projectilePrefab;
        EditorUtility.SetDirty(attack);

        GameObject root = new GameObject("OUTL_CombatEmitter_Projectile");
        Undo.RegisterCreatedObjectUndo(root, "Create OUTL Projectile Combat Emitter");

        GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObj.name = "Base";
        baseObj.transform.SetParent(root.transform, false);
        baseObj.transform.localScale = new Vector3(0.8f, 0.25f, 0.8f);

        GameObject pivot = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pivot.name = "AimPivot";
        pivot.transform.SetParent(root.transform, false);
        pivot.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        pivot.transform.localScale = new Vector3(0.45f, 0.35f, 0.7f);

        GameObject muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(pivot.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 0f, 0.55f);

        SphereCollider trigger = root.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 12f;

        OUTL_EntityAdapter adapter = root.AddComponent<OUTL_EntityAdapter>();
        adapter.Def = def;
        adapter.RegisterTick = false;
        OUTL_DamageReceiver receiver = root.AddComponent<OUTL_DamageReceiver>();
        receiver.Entity = adapter;

        OUTL_ProjectileCombatEmitter turret = root.AddComponent<OUTL_ProjectileCombatEmitter>();
        turret.Source = adapter;
        turret.ProjectileAttack = attack;
        turret.ProjectilePrefab = projectilePrefab;
        turret.Muzzle = muzzle.transform;
        turret.AimPivot = pivot.transform;
        turret.TargetTags = new[] { "Role.Targetable" };
        turret.FireInterval = 0.35f;
        turret.ProjectileSpeedOverride = -1f;
        turret.AimLeadStrength = 0.65f;
        turret.HorizontalSpreadDegrees = 2f;
        turret.VerticalSpreadDegrees = 1f;

        AssetDatabase.SaveAssets();
        Selection.activeGameObject = root;
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
