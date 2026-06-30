#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class OUT_MeshMergerWindow : EditorWindow
{
    private Transform root;
    private bool includeInactive;
    private bool disableSourceRenderers = true;

    private bool keepSourceColliders = true;
    private bool addCombinedMeshCollider;
    private bool disableSourceCollidersAfterMerge;

    private bool saveMeshAsset = true;
    private string outputFolder = "Assets/OUT_MergedMeshes";

    [MenuItem("OUT/Tools/Mesh Merger")]
    private static void Open()
    {
        GetWindow<OUT_MeshMergerWindow>("OUT Mesh Merger");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("OUT Mesh Merger", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        root = (Transform)EditorGUILayout.ObjectField("Root", root, typeof(Transform), true);

        if (GUILayout.Button("Use Selection"))
        {
            if (Selection.activeTransform != null)
                root = Selection.activeTransform;
        }

        EditorGUILayout.Space(8);

        includeInactive = EditorGUILayout.ToggleLeft("Include inactive children", includeInactive);
        disableSourceRenderers = EditorGUILayout.ToggleLeft("Disable source MeshRenderers after merge", disableSourceRenderers);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Collisions", EditorStyles.boldLabel);

        keepSourceColliders = EditorGUILayout.ToggleLeft("Keep original Colliders enabled, safest", keepSourceColliders);
        addCombinedMeshCollider = EditorGUILayout.ToggleLeft("Add one combined MeshCollider from render mesh", addCombinedMeshCollider);

        using (new EditorGUI.DisabledScope(!addCombinedMeshCollider))
        {
            disableSourceCollidersAfterMerge = EditorGUILayout.ToggleLeft(
                "Disable source Colliders after creating combined MeshCollider",
                disableSourceCollidersAfterMerge
            );
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);

        saveMeshAsset = EditorGUILayout.ToggleLeft("Save combined mesh as asset", saveMeshAsset);
        outputFolder = EditorGUILayout.TextField("Output folder", outputFolder);

        EditorGUILayout.Space(12);

        using (new EditorGUI.DisabledScope(root == null))
        {
            if (GUILayout.Button("MERGE SELECTED ROOT", GUILayout.Height(34)))
                Merge();
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "Best practice: merge per room / chunk / sector, not the whole map into one cursed mega-mesh. " +
            "Huge meshes hurt culling and make debugging miserable.",
            MessageType.Info
        );
    }

    private void Merge()
    {
        if (root == null)
        {
            Debug.LogError("OUT Mesh Merger: Root is null.");
            return;
        }

        var meshFilters = root.GetComponentsInChildren<MeshFilter>(includeInactive);
        var sourceRenderers = new List<MeshRenderer>();
        var materials = new List<Material>();
        var buckets = new List<List<CombineInstance>>();

        int skippedNoRenderer = 0;
        int skippedNoMesh = 0;
        int skippedUnreadable = 0;
        int includedSubMeshes = 0;

        Matrix4x4 rootWorldToLocal = root.worldToLocalMatrix;

        foreach (var mf in meshFilters)
        {
            if (mf == null)
                continue;

            if (mf.gameObject.name.Contains("_MERGED", System.StringComparison.OrdinalIgnoreCase))
                continue;

            var mr = mf.GetComponent<MeshRenderer>();
            if (mr == null)
            {
                skippedNoRenderer++;
                continue;
            }

            var mesh = mf.sharedMesh;
            if (mesh == null)
            {
                skippedNoMesh++;
                continue;
            }

            if (!mesh.isReadable)
            {
                skippedUnreadable++;
                Debug.LogWarning(
                    $"OUT Mesh Merger: skipped unreadable mesh '{mesh.name}' on '{mf.name}'. " +
                    "Enable Read/Write in import settings."
                );
                continue;
            }

            sourceRenderers.Add(mr);

            var sharedMaterials = mr.sharedMaterials;
            Matrix4x4 matrix = rootWorldToLocal * mf.transform.localToWorldMatrix;

            int subMeshCount = mesh.subMeshCount;
            for (int sub = 0; sub < subMeshCount; sub++)
            {
                Material mat = null;

                if (sharedMaterials != null && sharedMaterials.Length > 0)
                    mat = sharedMaterials[Mathf.Min(sub, sharedMaterials.Length - 1)];

                int matIndex = GetMaterialIndex(materials, buckets, mat);

                buckets[matIndex].Add(new CombineInstance
                {
                    mesh = mesh,
                    subMeshIndex = sub,
                    transform = matrix
                });

                includedSubMeshes++;
            }
        }

        if (includedSubMeshes == 0)
        {
            Debug.LogError("OUT Mesh Merger: nothing to merge. No valid readable MeshFilter + MeshRenderer pairs found.");
            return;
        }

        EnsureAssetFolder(outputFolder);

        var tempMeshes = new List<Mesh>();
        var finalCombines = new List<CombineInstance>();

        for (int i = 0; i < buckets.Count; i++)
        {
            var list = buckets[i];
            if (list.Count == 0)
                continue;

            var temp = new Mesh
            {
                name = $"{root.name}_mat_{i}_temp",
                indexFormat = IndexFormat.UInt32
            };

            temp.CombineMeshes(list.ToArray(), mergeSubMeshes: true, useMatrices: true, hasLightmapData: false);
            temp.RecalculateBounds();
            tempMeshes.Add(temp);

            finalCombines.Add(new CombineInstance
            {
                mesh = temp,
                subMeshIndex = 0,
                transform = Matrix4x4.identity
            });
        }

        var finalMesh = new Mesh
        {
            name = $"{root.name}_COMBINED",
            indexFormat = IndexFormat.UInt32
        };

        finalMesh.CombineMeshes(finalCombines.ToArray(), mergeSubMeshes: false, useMatrices: false, hasLightmapData: false);
        finalMesh.RecalculateBounds();

        string assetPath = null;

        if (saveMeshAsset)
        {
            assetPath = AssetDatabase.GenerateUniqueAssetPath($"{outputFolder}/{root.name}_COMBINED.asset");
            AssetDatabase.CreateAsset(finalMesh, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        foreach (var temp in tempMeshes)
            DestroyImmediate(temp);

        var mergedGo = new GameObject($"{root.name}_MERGED_RENDER");
        Undo.RegisterCreatedObjectUndo(mergedGo, "Create merged mesh object");

        mergedGo.transform.SetParent(root, false);
        mergedGo.transform.localPosition = Vector3.zero;
        mergedGo.transform.localRotation = Quaternion.identity;
        mergedGo.transform.localScale = Vector3.one;

        var outMf = mergedGo.AddComponent<MeshFilter>();
        var outMr = mergedGo.AddComponent<MeshRenderer>();

        outMf.sharedMesh = finalMesh;
        outMr.sharedMaterials = materials.ToArray();

        GameObjectUtility.SetStaticEditorFlags(
            mergedGo,
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.ContributeGI |
            StaticEditorFlags.ReflectionProbeStatic
        );

        if (addCombinedMeshCollider)
        {
            var mc = mergedGo.AddComponent<MeshCollider>();
            mc.sharedMesh = finalMesh;
            mc.convex = false;
        }

        if (disableSourceRenderers)
        {
            foreach (var mr in sourceRenderers)
            {
                if (mr == null)
                    continue;

                Undo.RecordObject(mr, "Disable source renderer");
                mr.enabled = false;
                EditorUtility.SetDirty(mr);
            }
        }

        if (!keepSourceColliders || (addCombinedMeshCollider && disableSourceCollidersAfterMerge))
        {
            var colliders = root.GetComponentsInChildren<Collider>(includeInactive);

            foreach (var col in colliders)
            {
                if (col == null)
                    continue;

                if (col.transform == mergedGo.transform)
                    continue;

                Undo.RecordObject(col, "Disable source collider");
                col.enabled = false;
                EditorUtility.SetDirty(col);
            }
        }

        Selection.activeGameObject = mergedGo;

        Debug.Log(
            $"OUT Mesh Merger: merged '{root.name}' into '{mergedGo.name}'. " +
            $"Submeshes: {includedSubMeshes}, materials: {materials.Count}, " +
            $"skipped unreadable: {skippedUnreadable}, skipped no renderer: {skippedNoRenderer}, skipped no mesh: {skippedNoMesh}, " +
            $"asset: {(assetPath ?? "scene-only mesh") }"
        );
    }

    private static int GetMaterialIndex(List<Material> materials, List<List<CombineInstance>> buckets, Material mat)
    {
        for (int i = 0; i < materials.Count; i++)
        {
            if (ReferenceEquals(materials[i], mat))
                return i;
        }

        materials.Add(mat);
        buckets.Add(new List<CombineInstance>());
        return materials.Count - 1;
    }

    private static void EnsureAssetFolder(string folder)
    {
        folder = folder.Replace("\\", "/").Trim('/');

        if (string.IsNullOrWhiteSpace(folder))
            folder = "Assets/OUT_MergedMeshes";

        if (!folder.StartsWith("Assets"))
            folder = "Assets/" + folder;

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
