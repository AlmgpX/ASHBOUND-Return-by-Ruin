using UnityEngine;

[DisallowMultipleComponent]
public class OUT_SaveableEntity : MonoBehaviour
{
    [SerializeField] private string saveId;
    [SerializeField] private bool autoGenerateFromHierarchyPath = true;
    [SerializeField] private bool saveTransform = true;
    [SerializeField] private bool saveActiveState = true;

    public string SaveId => ResolveSaveId();
    public bool SaveTransform => saveTransform;
    public bool SaveActiveState => saveActiveState;

    private void Reset()
    {
        saveId = string.Empty;
        autoGenerateFromHierarchyPath = true;
        saveTransform = true;
        saveActiveState = true;
    }

    private void OnValidate()
    {
        if (!autoGenerateFromHierarchyPath && !string.IsNullOrWhiteSpace(saveId))
            saveId = saveId.Trim();
    }

    public string ResolveSaveId()
    {
        if (!string.IsNullOrWhiteSpace(saveId))
            return saveId.Trim();

        if (!autoGenerateFromHierarchyPath)
            return gameObject.GetInstanceID().ToString();

        return BuildHierarchyPath(transform);
    }

    private static string BuildHierarchyPath(Transform t)
    {
        if (t == null)
            return "null";

        string path = t.name;
        Transform parent = t.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
}
