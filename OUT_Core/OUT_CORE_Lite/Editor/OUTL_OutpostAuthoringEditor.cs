#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class OUTL_OutpostAuthoringEditor
{
    private const string DefRoot = "Assets/OUT/OUT_Core/OUT_CORE_Lite/Generated/OccultistEnemyPack/Definitions/";

    [MenuItem("OUT CORE Lite/Add to Scene/Abstract Spawn Marker/From Selected EntityDef", priority = 40)]
    public static void AddSelectedDefMarker()
    {
        OUTL_EntityDef def = Selection.activeObject as OUTL_EntityDef;
        if (def == null)
        {
            Debug.LogWarning("Select an OUTL_EntityDef asset first.");
            return;
        }
        CreateMarker(def);
    }

    [MenuItem("OUT CORE Lite/Add to Scene/Abstract Spawn Marker/Occultist Shotgun", priority = 41)]
    public static void AddShotgun() { CreateMarker(LoadDef("OUTL_Entity_OccultistShotgun.asset")); }

    [MenuItem("OUT CORE Lite/Add to Scene/Abstract Spawn Marker/Occultist Rifle", priority = 42)]
    public static void AddRifle() { CreateMarker(LoadDef("OUTL_Entity_OccultistRifle.asset")); }

    [MenuItem("OUT CORE Lite/Add to Scene/Abstract Spawn Marker/Occultist SMG", priority = 43)]
    public static void AddSmg() { CreateMarker(LoadDef("OUTL_Entity_OccultistSMG.asset")); }

    [MenuItem("OUT CORE Lite/Add to Scene/Abstract Spawn Marker/Occultist Grenadier", priority = 44)]
    public static void AddGrenadier() { CreateMarker(LoadDef("OUTL_Entity_OccultistGrenadier.asset")); }

    [MenuItem("OUT CORE Lite/Add to Scene/Abstract Spawn Marker/Occultist Breacher", priority = 45)]
    public static void AddBreacher() { CreateMarker(LoadDef("OUTL_Entity_OccultistBreacher.asset")); }

    [MenuItem("OUT CORE Lite/Add to Scene/Outpost Clearance Controller", priority = 50)]
    public static void AddOutpostController()
    {
        GameObject go = new GameObject("OUTL_Outpost");
        Undo.RegisterCreatedObjectUndo(go, "Create OUTL Outpost");
        go.transform.position = AuthoringPosition();

        OUTL_EntityAdapter entity = Undo.AddComponent<OUTL_EntityAdapter>(go);
        entity.ClassNameOverride = "logic.outpost";
        entity.TargetName = "outpost";
        entity.StableId = "outpost." + Guid.NewGuid().ToString("N");
        entity.SavePersistent = true;
        entity.RestoreSpawnIfMissing = false;
        entity.RegisterTick = false;
        entity.RegisterRandomTick = false;
        entity.RegisterInSectors = false;

        OUTL_OutpostClearanceController controller = Undo.AddComponent<OUTL_OutpostClearanceController>(go);
        controller.Entity = entity;
        controller.OutpostId = entity.StableId;
        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(go.scene);
    }

    [MenuItem("OUT CORE Lite/Advanced/Migrate Selected GunshipBaseStatus", priority = 200)]
    public static void MigrateSelectedLegacyBase()
    {
        GameObject selected = Selection.activeGameObject;
        GunshipBaseStatus legacy = selected != null ? selected.GetComponent<GunshipBaseStatus>() : null;
        if (legacy == null)
        {
            Debug.LogWarning("Select a GameObject containing GunshipBaseStatus.");
            return;
        }

        OUTL_EntityAdapter entity = selected.GetComponent<OUTL_EntityAdapter>();
        if (entity == null) entity = Undo.AddComponent<OUTL_EntityAdapter>(selected);
        if (string.IsNullOrEmpty(entity.StableId)) entity.StableId = "outpost." + Guid.NewGuid().ToString("N");
        entity.ClassNameOverride = "logic.outpost";
        entity.TargetName = entity.StableId;
        entity.SavePersistent = true;
        entity.RestoreSpawnIfMissing = false;
        entity.RegisterTick = false;
        entity.RegisterRandomTick = false;
        entity.RegisterInSectors = false;

        OUTL_OutpostClearanceController controller = selected.GetComponent<OUTL_OutpostClearanceController>();
        if (controller == null) controller = Undo.AddComponent<OUTL_OutpostClearanceController>(selected);
        controller.Entity = entity;
        controller.OutpostId = entity.StableId;
        controller.Radius = Mathf.Max(1f, legacy.checkDistance);
        controller.CheckInterval = Mathf.Max(0.1f, legacy.checkFrequency);
        controller.HostileIndicator = legacy.RedBeam;
        controller.ClearedIndicator = legacy.GreenBeam;
        controller.SiegeModeObject = legacy.SiegeModeObject;
        controller.VictorySound = legacy.victorySound;
        controller.OnCleared = legacy.BaseStatusEvent;

        OUTL_LegacyGunshipBaseProgressBridge bridge = selected.GetComponent<OUTL_LegacyGunshipBaseProgressBridge>();
        if (bridge == null) bridge = Undo.AddComponent<OUTL_LegacyGunshipBaseProgressBridge>(selected);
        bridge.Controller = controller;

        int migratedTargets = 0;
        OUT_Entity_Breakable[] breakables = UnityEngine.Object.FindObjectsOfType<OUT_Entity_Breakable>(true);
        for (int i = 0; i < breakables.Length; i++)
            if (breakables[i] != null && (breakables[i].BaseStatus == legacy || IsInside(breakables[i].transform.position, legacy)))
                migratedTargets += AddTarget(breakables[i].gameObject, controller.OutpostId);

        OUT_ExplosionGeometry[] geometry = UnityEngine.Object.FindObjectsOfType<OUT_ExplosionGeometry>(true);
        for (int i = 0; i < geometry.Length; i++)
            if (geometry[i] != null && (geometry[i].BaseStatus == legacy || IsInside(geometry[i].transform.position, legacy)))
                migratedTargets += AddTarget(geometry[i].gameObject, controller.OutpostId);

        Undo.RecordObject(legacy, "Disable Legacy Base Status");
        legacy.enabled = false;
        controller.CollectTargets();
        EditorUtility.SetDirty(selected);
        EditorSceneManager.MarkSceneDirty(selected.scene);
        Selection.activeObject = controller;
        Debug.Log("OUT CORE Lite: migrated GunshipBaseStatus to OUTL outpost. Targets=" + migratedTargets, controller);
    }

    [MenuItem("OUT CORE Lite/Advanced/Assign Selected Targets To Selected Outpost", priority = 201)]
    public static void AssignSelectedToOutpost()
    {
        OUTL_OutpostClearanceController controller = null;
        for (int i = 0; i < Selection.gameObjects.Length; i++)
        {
            controller = Selection.gameObjects[i].GetComponent<OUTL_OutpostClearanceController>();
            if (controller != null) break;
        }
        if (controller == null)
        {
            Debug.LogWarning("Multi-select one OUTL outpost and the marker/target objects.");
            return;
        }

        int assigned = 0;
        for (int i = 0; i < Selection.gameObjects.Length; i++)
        {
            GameObject go = Selection.gameObjects[i];
            if (go == null || go == controller.gameObject) continue;
            OUTL_AbstractSpawnMarker marker = go.GetComponent<OUTL_AbstractSpawnMarker>();
            if (marker != null)
            {
                Undo.RecordObject(marker, "Assign OUTL Marker To Outpost");
                marker.OutpostId = controller.OutpostId;
                assigned++;
                continue;
            }
            assigned += AddTarget(go, controller.OutpostId);
        }
        controller.CollectTargets();
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        Debug.Log("OUT CORE Lite: assigned " + assigned + " targets to " + controller.OutpostId, controller);
    }

    private static void CreateMarker(OUTL_EntityDef def)
    {
        if (def == null) return;
        GameObject go = new GameObject("OUTL_Spawn_" + def.name.Replace("OUTL_Entity_", ""));
        Undo.RegisterCreatedObjectUndo(go, "Create OUTL Abstract Spawn Marker");
        go.transform.position = AuthoringPosition();
        OUTL_AbstractSpawnMarker marker = Undo.AddComponent<OUTL_AbstractSpawnMarker>(go);
        marker.EntityDef = def;

        OUTL_OutpostClearanceController selectedOutpost = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponentInParent<OUTL_OutpostClearanceController>()
            : null;
        if (selectedOutpost != null)
        {
            marker.OutpostId = selectedOutpost.OutpostId;
            go.transform.SetParent(selectedOutpost.transform, true);
        }

        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(go.scene);
    }

    private static int AddTarget(GameObject go, string outpostId)
    {
        if (go == null) return 0;
        OUTL_OutpostClearanceTarget target = go.GetComponent<OUTL_OutpostClearanceTarget>();
        if (target == null) target = Undo.AddComponent<OUTL_OutpostClearanceTarget>(go);
        Undo.RecordObject(target, "Assign OUTL Outpost Target");
        target.OutpostId = outpostId;
        target.CountsForClearance = true;
        EditorUtility.SetDirty(target);
        return 1;
    }

    private static bool IsInside(Vector3 position, GunshipBaseStatus legacy)
    {
        return legacy != null && (position - legacy.transform.position).sqrMagnitude <= legacy.checkDistance * legacy.checkDistance;
    }

    private static OUTL_EntityDef LoadDef(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<OUTL_EntityDef>(DefRoot + fileName);
    }

    private static Vector3 AuthoringPosition()
    {
        return SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
    }
}
#endif
