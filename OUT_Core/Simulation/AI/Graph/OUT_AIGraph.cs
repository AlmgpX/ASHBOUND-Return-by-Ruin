using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class OUT_AIGraph : MonoBehaviour
{
    [System.Serializable]
    public class OUT_RuntimeNode
    {
        public string Name;
        public Vector3 Position;
        public int[] Links;
        public bool Enabled = true;
        public bool IsCoverHint = false;
        public OUT_SceneSensorySample Sensory;
    }

    [Header("Runtime Graph")]
    [SerializeField] private OUT_RuntimeNode[] nodes;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private bool drawLinks = true;
    [SerializeField] private bool drawSensoryOverlay = false;
    [SerializeField] private float gizmoNodeRadius = 0.18f;
    [SerializeField] private OUT_SensoryChannelFlags sensoryDebugChannel = OUT_SensoryChannelFlags.Luminance;
    [SerializeField] [Range(0.05f, 1f)] private float sensoryDebugAlpha = 0.45f;
    [SerializeField] private bool drawNodeLabels = true;
    [SerializeField] private bool showNodeIndex = true;
    [SerializeField] private bool showLinkCount = true;
    [SerializeField] private bool showSelectedSensoryValue = true;
    [SerializeField] private bool showFullSensoryBreakdown = false;

    public int NodeCount => nodes != null ? nodes.Length : 0;
    public bool HasNodes => NodeCount > 0;

    public OUT_RuntimeNode[] Nodes => nodes;

    public void ApplyNodes(OUT_RuntimeNode[] bakedNodes)
    {
        nodes = bakedNodes ?? new OUT_RuntimeNode[0];
    }

    public bool IsNodeUsable(int index)
    {
        return nodes != null && index >= 0 && index < nodes.Length && nodes[index] != null && nodes[index].Enabled;
    }

    public Vector3 GetNodePosition(int index)
    {
        if (!IsNodeUsable(index))
            return transform.position;

        return nodes[index].Position;
    }

    public int[] GetNodeLinks(int index)
    {
        if (!IsNodeUsable(index))
            return null;

        return nodes[index].Links;
    }

    public bool IsNodeCoverHint(int index)
    {
        return IsNodeUsable(index) && nodes[index].IsCoverHint;
    }

    public OUT_SceneSensorySample GetNodeSensory(int index)
    {
        if (!IsNodeUsable(index))
        {
            OUT_SceneSensorySample empty = default;
            empty.Clear();
            return empty;
        }

        return nodes[index].Sensory;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || nodes == null || nodes.Length == 0)
            return;

        for (int i = 0; i < nodes.Length; i++)
        {
            OUT_RuntimeNode node = nodes[i];
            if (node == null || !node.Enabled)
                continue;

            if (drawSensoryOverlay)
                Gizmos.color = GetSensoryDebugColor(node.Sensory.GetChannelValue(GetSingleSensoryChannel(sensoryDebugChannel)));
            else
                Gizmos.color = node.IsCoverHint ? Color.yellow : Color.cyan;

            Gizmos.DrawWireSphere(node.Position, gizmoNodeRadius);

            if (!drawLinks || node.Links == null)
                continue;

            Gizmos.color = Color.gray;

            for (int j = 0; j < node.Links.Length; j++)
            {
                int link = node.Links[j];
                if (!IsNodeUsable(link))
                    continue;

                Gizmos.DrawLine(node.Position, nodes[link].Position);
            }
        }

#if UNITY_EDITOR
        if (drawNodeLabels)
            DrawEditorLabels();
#endif
    }

#if UNITY_EDITOR
    private void DrawEditorLabels()
    {
        for (int i = 0; i < nodes.Length; i++)
        {
            OUT_RuntimeNode node = nodes[i];
            if (node == null || !node.Enabled)
                continue;

            System.Text.StringBuilder sb = new System.Text.StringBuilder(64);

            if (showNodeIndex)
                sb.Append('[').Append(i).Append("] ");

            sb.Append(string.IsNullOrWhiteSpace(node.Name) ? "Node" : node.Name);

            if (showLinkCount)
                sb.Append("  links:").Append(node.Links != null ? node.Links.Length : 0);

            if (node.IsCoverHint)
                sb.Append("  COVER");

            if (showSelectedSensoryValue)
            {
                OUT_SensoryChannelFlags channel = GetSingleSensoryChannel(sensoryDebugChannel);
                float value = node.Sensory.GetChannelValue(channel);
                sb.Append('\n').Append(channel).Append(": ").Append(value.ToString("0.00"));
            }

            if (showFullSensoryBreakdown)
            {
                sb.Append("\nL:").Append(node.Sensory.Luminance.ToString("0.00"));
                sb.Append(" Sky:").Append(node.Sensory.SkyLuminance.ToString("0.00"));
                sb.Append(" Ground:").Append(node.Sensory.GroundLuminance.ToString("0.00"));
                sb.Append(" O:").Append(node.Sensory.Occlusion.ToString("0.00"));
                sb.Append(" C:").Append(node.Sensory.Cover.ToString("0.00"));
                sb.Append(" G:").Append(node.Sensory.GroundSafety.ToString("0.00"));
                sb.Append(" A:").Append(node.Sensory.AreaCost.ToString("0.00"));
            }

            Handles.color = Color.white;
            Handles.Label(node.Position + Vector3.up * 0.18f, sb.ToString());
        }
    }
#endif

    private OUT_SensoryChannelFlags GetSingleSensoryChannel(OUT_SensoryChannelFlags flags)
    {
        if ((flags & OUT_SensoryChannelFlags.Luminance) != 0) return OUT_SensoryChannelFlags.Luminance;
        if ((flags & OUT_SensoryChannelFlags.SkyLuminance) != 0) return OUT_SensoryChannelFlags.SkyLuminance;
        if ((flags & OUT_SensoryChannelFlags.GroundLuminance) != 0) return OUT_SensoryChannelFlags.GroundLuminance;
        if ((flags & OUT_SensoryChannelFlags.Occlusion) != 0) return OUT_SensoryChannelFlags.Occlusion;
        if ((flags & OUT_SensoryChannelFlags.Cover) != 0) return OUT_SensoryChannelFlags.Cover;
        if ((flags & OUT_SensoryChannelFlags.GroundSafety) != 0) return OUT_SensoryChannelFlags.GroundSafety;
        if ((flags & OUT_SensoryChannelFlags.AreaCost) != 0) return OUT_SensoryChannelFlags.AreaCost;
        if ((flags & OUT_SensoryChannelFlags.Noise) != 0) return OUT_SensoryChannelFlags.Noise;
        if ((flags & OUT_SensoryChannelFlags.Danger) != 0) return OUT_SensoryChannelFlags.Danger;
        if ((flags & OUT_SensoryChannelFlags.Food) != 0) return OUT_SensoryChannelFlags.Food;
        if ((flags & OUT_SensoryChannelFlags.Fire) != 0) return OUT_SensoryChannelFlags.Fire;

        return OUT_SensoryChannelFlags.Luminance;
    }

    private Color GetSensoryDebugColor(float value)
    {
        value = Mathf.Clamp01(value);
        Color a = new Color(0f, 0f, 0f, sensoryDebugAlpha);
        Color b = new Color(1f, 1f, 1f, sensoryDebugAlpha);
        return Color.Lerp(a, b, value);
    }
}
