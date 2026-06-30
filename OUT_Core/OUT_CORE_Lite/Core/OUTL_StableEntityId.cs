using System;
using UnityEngine;

[DisallowMultipleComponent]
[ExecuteAlways]
public class OUTL_StableEntityId : MonoBehaviour
{
    [SerializeField] private string stableId;
    [SerializeField] private string prefix = "scene";
    [SerializeField] private bool generateIfEmpty = true;

    public string StableId
    {
        get
        {
            EnsureId();
            return stableId;
        }
    }

    public void SetStableId(string id)
    {
        stableId = string.IsNullOrWhiteSpace(id) ? BuildNewId() : id;
    }

    public void EnsureId()
    {
        if (!generateIfEmpty) return;
        if (!string.IsNullOrWhiteSpace(stableId)) return;
        stableId = BuildNewId();
    }

    private string BuildNewId()
    {
        string sceneName = gameObject.scene.IsValid() ? gameObject.scene.name : "noscn";
        if (string.IsNullOrWhiteSpace(sceneName)) sceneName = "unsaved";
        string cleanPrefix = string.IsNullOrWhiteSpace(prefix) ? "scene" : prefix.Trim();
        return cleanPrefix + "." + sceneName + "." + Guid.NewGuid().ToString("N");
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!generateIfEmpty) return;
        if (!string.IsNullOrWhiteSpace(stableId)) return;
        stableId = BuildNewId();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
