#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class OUTL_EgregorePresetEditor
{
    private const string MenuPath = "OUT CORE Lite/Legacy Demo/Egregore/Create Sample Egregore Defs";
    private const string TargetFolder = "Assets/OUT/OUT_Core/OUT_CORE_Lite/Templates/Egregore";

    // [MenuItem(MenuPath)]
    public static void CreateSampleDefs()
    {
        EnsureFolder(TargetFolder);
        CreateOrUpdate("OUTL_Egregore_ForestSpirit.asset", "forest_spirit", "ForestSpirit", OUTL_EgregoreScope.Local, 2.0f, 96f, 0.45f, 0.35f, 0.15f, 0.05f, 0.25f, 0.45f, 0.35f);
        CreateOrUpdate("OUTL_Egregore_CitySpirit.asset", "city_spirit", "CitySpirit", OUTL_EgregoreScope.Regional, 5.0f, 160f, 0.35f, 0.20f, 0.25f, 0.02f, 0.35f, 0.55f, 0.20f);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateOrUpdate(string fileName, string id, string displayName, OUTL_EgregoreScope scope, float interval, float radius, float violence, float fear, float prosperity, float corruption, float alertness, float hostility, float resource)
    {
        string path = TargetFolder + "/" + fileName;
        OUTL_EgregoreDef def = AssetDatabase.LoadAssetAtPath<OUTL_EgregoreDef>(path);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<OUTL_EgregoreDef>();
            AssetDatabase.CreateAsset(def, path);
        }

        def.EgregoreId = id;
        def.DisplayName = displayName;
        def.Scope = scope;
        def.UpdateInterval = interval;
        def.InfluenceRadius = radius;
        def.ViolenceWeight = violence;
        def.FearWeight = fear;
        def.ProsperityWeight = prosperity;
        def.CorruptionWeight = corruption;
        def.AlertnessWeight = alertness;
        def.HostilityWeight = hostility;
        def.ResourceWeight = resource;
        EditorUtility.SetDirty(def);
    }

    private static void EnsureFolder(string assetFolder)
    {
        string[] parts = assetFolder.Split('/');
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
