using UnityEngine;

[DisallowMultipleComponent]
public class OUT_AIGraphNode : MonoBehaviour
{
    [SerializeField] private string nodeName;
    [SerializeField] private bool nodeEnabled = true;
    [SerializeField] private bool isCoverHint = false;
    [SerializeField] private bool generatedFromNavMesh = false;
    [SerializeField] private OUT_AIGraphNode[] links;
    [SerializeField, HideInInspector] private OUT_SceneSensorySample bakedSensory;

    public string NodeName => string.IsNullOrWhiteSpace(nodeName) ? gameObject.name : nodeName;
    public bool NodeEnabled => nodeEnabled;
    public bool IsCoverHint => isCoverHint;
    public bool GeneratedFromNavMesh => generatedFromNavMesh;
    public OUT_AIGraphNode[] Links => links;
    public OUT_SceneSensorySample BakedSensory => bakedSensory;

    public void SetGeneratedFromNavMesh(bool value)
    {
        generatedFromNavMesh = value;
    }

    public void SetLinks(OUT_AIGraphNode[] newLinks)
    {
        links = newLinks;
    }

    public void SetBakedSensory(in OUT_SceneSensorySample sample)
    {
        bakedSensory = sample;
    }

    public void SetNodeName(string value)
    {
        nodeName = value;
    }

    public void SetCoverHint(bool value)
    {
        isCoverHint = value;
    }

    public void SetNodeEnabled(bool value)
    {
        nodeEnabled = value;
    }

    private void OnDrawGizmos()
    {
        if (!nodeEnabled)
            Gizmos.color = Color.gray;
        else
            Gizmos.color = isCoverHint ? Color.yellow : Color.cyan;

        Gizmos.DrawWireSphere(transform.position, 0.12f);
    }
}
