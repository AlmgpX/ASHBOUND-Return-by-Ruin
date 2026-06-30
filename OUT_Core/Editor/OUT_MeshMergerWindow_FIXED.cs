#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class OUT_MeshMergerWindow_FIXED
{
    private const string OutputFolder = "Assets/OUT_MergedMeshes";

    [MenuItem("OUT/Tools/Mesh Merger/Merge Selected Root FAST")]
    public static void MergeSelectedRootFast()
    {
        Transform root = Selection.activeTransform;
        if (root == null)
        {
            Debug.LogError("OUT Mesh Merger: select a root object first.");
            return;
        }

        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        var materials = new List<Material>();
        var buckets = new List<List<CombineInstance>>();
        var sourceRenderers = new List<MeshRenderer>();
        Matrix4x4 rootWorldToLocal = root.worldToLocalMatrix;

        int found = renderers.Length;
        int included = 0;
        int noFilter = 0;
        int noMesh = 0;
        int unreadable = 0;
        int fixedReadable = 0;

        for (int r = 0; r < renderers.Length; r++)
        {
            MeshRenderer mr = renderers[r];
            if (mr == null)
                continue;

            if (mr.gameObject.name.IndexOf("_MERGED", StringComparison.OrdinalIgnoreCase) >= 0)
                continue;

            MeshFilter mf = mr.GetComponent<MeshFilter>();
            if (mf == null)
            {
                noFilter++;
                continue;
            }

            Mesh mesh = mf.sharedMesh;
            if (mesh == null)
            {
                noMesh++;
                continue;
            }

            if (!mesh.isReadable)
            {
                if (TryMakeReadable(mesh))
                {
                    fixedReadable++;
                    mesh = mf.sharedMesh;
                }
            }

            if (mesh == null || !mesh.isReadable)
            {
                unreadable++;
                Debug.LogWarning("OUT Mesh Merger: skipped unreadable mesh on " + mf.name);
                continue;
            }

            Material[] sharedMaterials = mr.sharedMaterials;
            Matrix4x4 matrix = rootWorldToLocal * mf.transform.localToWorldMatrix;
            sourceRenderers.Add(mr);

            for (int sub = 0; sub < mesh.subMeshCount; sub++)
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
                included++;
            }
        }

        if (included == 0)
        {
            Debug.LogError("OUT Mesh Merger: nothing to merge. Root=" + root.name +
                ", MeshRenderers=" + found +
                ", no MeshFilter=" + noFilter +
                ", no mesh=" + noMesh +
                ", unreadable=" + unreadable +
                ". Select the parent with real mesh children. Unity, naturally, chose pain.");
            return;
        }

        EnsureFolder(OutputFolder);

        var tempMeshes = new List<Mesh>();
        var finalCombines = new List<CombineInstance>();

        for (int i = 0; i < buckets.Count; i++)
        {
            if (buckets[i].Count == 0)
                continue;

            Mesh temp = new Mesh();
            temp.name = root.name + "_mat_" + i + "_temp";
            temp.indexFormat = IndexFormat.UInt32;
            temp.CombineMeshes(buckets[i].ToArray(), true, true, false);
            temp.RecalculateBounds();
            if (temp.normals == null || temp.normals.Length != temp.vertexCount)
                temp.RecalculateNormals();
            tempMeshes.Add(temp);

            finalCombines.Add(new CombineInstance
            {
                mesh = temp,
                subMeshIndex = 0,
                transform = Matrix4x4.identity
            });
        }

        Mesh finalMesh = new Mesh();
        finalMesh.name = root.name + "_COMBINED";
        finalMesh.indexFormat = IndexFormat.UInt32;
        finalMesh.CombineMeshes(finalCombines.ToArray(), false, false, false);
        finalMesh.RecalculateBounds();
        if (finalMesh.normals == null || finalMesh.normals.Length != finalMesh.vertexCount)
            finalMesh.RecalculateNormals();

        List<string> objMaterialNames = BuildObjMaterialNames(materials);

        string safeName = SanitizeFileName(root.name + "_COMBINED");
        string meshAssetPath = AssetDatabase.GenerateUniqueAssetPath(OutputFolder + "/" + safeName + ".asset");
        AssetDatabase.CreateAsset(finalMesh, meshAssetPath);
        AssetDatabase.SaveAssets();

        string objPath = AssetDatabase.GenerateUniqueAssetPath(OutputFolder + "/" + safeName + ".obj");
        string mtlPath = Path.ChangeExtension(objPath, ".mtl").Replace("\\", "/");
        ExportObj(finalMesh, materials, objMaterialNames, objPath, mtlPath);
        AssetDatabase.ImportAsset(mtlPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(objPath, ImportAssetOptions.ForceUpdate);
        ConfigureObjImporterForLightmapsAndMaterials(objPath, materials, objMaterialNames);

        Mesh importedObjMesh = LoadFirstMeshAtPath(objPath);

        for (int i = 0; i < tempMeshes.Count; i++)
            UnityEngine.Object.DestroyImmediate(tempMeshes[i]);

        GameObject merged = new GameObject(root.name + "_MERGED_RENDER");
        Undo.RegisterCreatedObjectUndo(merged, "Create merged mesh object");
        merged.transform.SetParent(root, false);
        merged.transform.localPosition = Vector3.zero;
        merged.transform.localRotation = Quaternion.identity;
        merged.transform.localScale = Vector3.one;

        MeshFilter outFilter = merged.AddComponent<MeshFilter>();
        MeshRenderer outRenderer = merged.AddComponent<MeshRenderer>();
        outFilter.sharedMesh = importedObjMesh != null ? importedObjMesh : finalMesh;
        outRenderer.sharedMaterials = materials.ToArray();

        GameObjectUtility.SetStaticEditorFlags(merged,
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.ContributeGI |
            StaticEditorFlags.ReflectionProbeStatic);

        for (int i = 0; i < sourceRenderers.Count; i++)
        {
            if (sourceRenderers[i] == null)
                continue;

            Undo.RecordObject(sourceRenderers[i], "Disable source renderer");
            sourceRenderers[i].enabled = false;
            EditorUtility.SetDirty(sourceRenderers[i]);
        }

        Selection.activeGameObject = merged;
        Debug.Log("OUT Mesh Merger: merged " + root.name +
            ". MeshRenderers=" + found +
            ", submeshes=" + included +
            ", materials=" + materials.Count +
            ", ReadWrite fixed=" + fixedReadable +
            ", unreadable skipped=" + unreadable +
            ", mesh asset=" + meshAssetPath +
            ", obj model=" + objPath +
            ", renderer uses=" + (importedObjMesh != null ? "imported OBJ mesh with generated lightmap UVs and original material remaps" : "fallback .asset mesh") +
            ". Original colliders are kept enabled.");
    }

    private static void ExportObj(Mesh mesh, List<Material> materials, List<string> objMaterialNames, string objAssetPath, string mtlAssetPath)
    {
        string absoluteObjPath = ToAbsolutePath(objAssetPath);
        string absoluteMtlPath = ToAbsolutePath(mtlAssetPath);
        Directory.CreateDirectory(Path.GetDirectoryName(absoluteObjPath));

        WriteMtl(materials, objMaterialNames, absoluteMtlPath);

        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector2[] uvs = mesh.uv;
        bool hasNormals = normals != null && normals.Length == vertices.Length;
        bool hasUvs = uvs != null && uvs.Length == vertices.Length;

        var sb = new StringBuilder(Mathf.Max(4096, vertices.Length * 64));
        sb.AppendLine("# OUT Mesh Merger OBJ export");
        sb.AppendLine("# Generated from Unity scene geometry. Material names are unique remap keys for original Unity materials.");
        sb.Append("mtllib ").Append(Path.GetFileName(mtlAssetPath)).AppendLine();
        sb.Append("o ").Append(SanitizeObjName(mesh.name)).AppendLine();

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = vertices[i];
            sb.Append("v ")
                .Append(Float(-v.x)).Append(' ')
                .Append(Float(v.y)).Append(' ')
                .Append(Float(v.z)).AppendLine();
        }

        if (hasUvs)
        {
            for (int i = 0; i < uvs.Length; i++)
            {
                Vector2 uv = uvs[i];
                sb.Append("vt ")
                    .Append(Float(uv.x)).Append(' ')
                    .Append(Float(uv.y)).AppendLine();
            }
        }

        if (hasNormals)
        {
            for (int i = 0; i < normals.Length; i++)
            {
                Vector3 n = normals[i];
                sb.Append("vn ")
                    .Append(Float(-n.x)).Append(' ')
                    .Append(Float(n.y)).Append(' ')
                    .Append(Float(n.z)).AppendLine();
            }
        }

        for (int sub = 0; sub < mesh.subMeshCount; sub++)
        {
            string matName = sub < objMaterialNames.Count ? objMaterialNames[sub] : "OUT_Default";
            sb.Append("g ").Append(SanitizeObjName(mesh.name)).Append("_sub_").Append(sub).AppendLine();
            sb.Append("usemtl ").Append(matName).AppendLine();

            int[] triangles = mesh.GetTriangles(sub);
            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                // X is flipped for OBJ export, so triangle winding is reversed.
                AppendFace(sb, triangles[i + 2] + 1, triangles[i + 1] + 1, triangles[i] + 1, hasUvs, hasNormals);
            }
        }

        File.WriteAllText(absoluteObjPath, sb.ToString(), Encoding.UTF8);
    }

    private static void WriteMtl(List<Material> materials, List<string> objMaterialNames, string absoluteMtlPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(absoluteMtlPath));
        var sb = new StringBuilder();
        sb.AppendLine("# OUT Mesh Merger MTL export");

        for (int i = 0; i < materials.Count; i++)
        {
            Material mat = materials[i];
            string name = i < objMaterialNames.Count ? objMaterialNames[i] : "OUT_Default";
            Color color = Color.white;
            if (mat != null && mat.HasProperty("_Color"))
                color = mat.color;

            sb.Append("newmtl ").Append(name).AppendLine();
            sb.Append("Kd ").Append(Float(color.r)).Append(' ').Append(Float(color.g)).Append(' ').Append(Float(color.b)).AppendLine();
            sb.Append("Ka 0 0 0").AppendLine();
            sb.Append("Ks 0 0 0").AppendLine();
            sb.Append("d ").Append(Float(color.a)).AppendLine();
            sb.AppendLine();
        }

        if (materials.Count == 0)
        {
            sb.AppendLine("newmtl OUT_Default");
            sb.AppendLine("Kd 1 1 1");
        }

        File.WriteAllText(absoluteMtlPath, sb.ToString(), Encoding.UTF8);
    }

    private static void AppendFace(StringBuilder sb, int a, int b, int c, bool hasUvs, bool hasNormals)
    {
        sb.Append("f ");
        AppendFaceIndex(sb, a, hasUvs, hasNormals);
        sb.Append(' ');
        AppendFaceIndex(sb, b, hasUvs, hasNormals);
        sb.Append(' ');
        AppendFaceIndex(sb, c, hasUvs, hasNormals);
        sb.AppendLine();
    }

    private static void AppendFaceIndex(StringBuilder sb, int index, bool hasUvs, bool hasNormals)
    {
        if (hasUvs && hasNormals)
        {
            sb.Append(index).Append('/').Append(index).Append('/').Append(index);
            return;
        }

        if (hasUvs)
        {
            sb.Append(index).Append('/').Append(index);
            return;
        }

        if (hasNormals)
        {
            sb.Append(index).Append("//").Append(index);
            return;
        }

        sb.Append(index);
    }

    private static void ConfigureObjImporterForLightmapsAndMaterials(string objPath, List<Material> materials, List<string> objMaterialNames)
    {
        ModelImporter importer = AssetImporter.GetAtPath(objPath) as ModelImporter;
        if (importer == null)
            return;

        importer.isReadable = true;
        importer.generateSecondaryUV = true;
        importer.secondaryUVAngleDistortion = 8f;
        importer.secondaryUVAreaDistortion = 15f;
        importer.secondaryUVHardAngle = 88f;
        importer.secondaryUVPackMargin = 4f;

        int remapCount = Mathf.Min(materials.Count, objMaterialNames.Count);
        for (int i = 0; i < remapCount; i++)
        {
            Material material = materials[i];
            if (material == null)
                continue;

            var sourceId = new AssetImporter.SourceAssetIdentifier(typeof(Material), objMaterialNames[i]);
            importer.AddRemap(sourceId, material);
        }

        importer.SaveAndReimport();
    }

    private static Mesh LoadFirstMeshAtPath(string assetPath)
    {
        UnityEngine.Object[] all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        for (int i = 0; i < all.Length; i++)
        {
            Mesh mesh = all[i] as Mesh;
            if (mesh != null)
                return mesh;
        }
        return null;
    }

    private static bool TryMakeReadable(Mesh mesh)
    {
        string path = AssetDatabase.GetAssetPath(mesh);
        if (string.IsNullOrEmpty(path))
            return false;

        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
            return false;

        if (importer.isReadable)
            return true;

        importer.isReadable = true;
        importer.SaveAndReimport();
        return true;
    }

    private static List<string> BuildObjMaterialNames(List<Material> materials)
    {
        var names = new List<string>(materials.Count);
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < materials.Count; i++)
        {
            Material mat = materials[i];
            string baseName = SanitizeObjName(mat != null ? mat.name : "OUT_Default");
            if (string.IsNullOrEmpty(baseName))
                baseName = "OUT_Default";

            string candidate = baseName;
            int suffix = 1;
            while (used.Contains(candidate))
            {
                candidate = baseName + "_" + suffix;
                suffix++;
            }

            used.Add(candidate);
            names.Add(candidate);
        }

        return names;
    }

    private static int GetMaterialIndex(List<Material> materials, List<List<CombineInstance>> buckets, Material mat)
    {
        for (int i = 0; i < materials.Count; i++)
            if (ReferenceEquals(materials[i], mat))
                return i;

        materials.Add(mat);
        buckets.Add(new List<CombineInstance>());
        return materials.Count - 1;
    }

    private static void EnsureFolder(string folder)
    {
        folder = folder.Replace("\\", "/").Trim('/');
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

    private static string ToAbsolutePath(string assetPath)
    {
        assetPath = assetPath.Replace("\\", "/");
        if (!assetPath.StartsWith("Assets/"))
            throw new ArgumentException("Path must be inside Assets: " + assetPath);

        string relative = assetPath.Substring("Assets/".Length).Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(Application.dataPath, relative);
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "OUT_MergedMesh";

        char[] invalid = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalid.Length; i++)
            name = name.Replace(invalid[i], '_');

        return name.Replace(' ', '_');
    }

    private static string SanitizeObjName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "OUT_Default";

        return SanitizeFileName(name).Replace('.', '_').Replace(':', '_').Replace(';', '_');
    }

    private static string Float(float value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }
}
#endif
