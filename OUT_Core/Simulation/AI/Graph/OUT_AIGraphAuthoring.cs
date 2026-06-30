using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class OUT_AIGraphAuthoring : MonoBehaviour
{
    private struct OUT_QuantizedVertexKey
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public OUT_QuantizedVertexKey(Vector3 position, float snap)
        {
            float safeSnap = Mathf.Max(0.001f, snap);
            X = Mathf.RoundToInt(position.x / safeSnap);
            Y = Mathf.RoundToInt(position.y / safeSnap);
            Z = Mathf.RoundToInt(position.z / safeSnap);
        }
    }

    [Header("References")]
    [SerializeField] private OUT_AIGraph targetGraph;
    [SerializeField] private OUT_SceneSensoryField sensoryField;
    [SerializeField] private Transform nodesRoot;

    [Header("NavMesh Import")]
    [SerializeField] private bool clearGeneratedNodesBeforeImport = true;
    [SerializeField] [Min(0.05f)] private float vertexMergeDistance = 0.5f;
    [SerializeField] [Min(0f)] private float importedNodeYOffset = 0.05f;
    [SerializeField] private bool rebuildRuntimeGraphAfterImport = true;

    [Header("Validation / Traversal")]
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] [Min(0.05f)] private float agentRadius = 0.35f;
    [SerializeField] [Min(0.2f)] private float agentHeight = 1.8f;
    [SerializeField] [Min(0.05f)] private float linkProbeStep = 0.75f;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
    [SerializeField] [Min(0f)] private float minLinkDistance = 0.05f;

    [Header("Neighbour Auto Linking")]
    [SerializeField] [Min(0.1f)] private float neighbourLinkRadius = 5f;
    [SerializeField] [Min(1)] private int maxNeighbourLinksPerNode = 6;
    [SerializeField] private bool rebuildRuntimeGraphAfterAutoLink = true;

    [Header("Optimization")]
    [SerializeField] private bool autoPruneInvalidLinksAfterImport = true;
    [SerializeField] private bool autoCollapseRedundantNodesAfterImport = true;
    [SerializeField] [Range(-1f, 1f)] private float redundantNodeMinDot = 0.985f;
    [SerializeField] private bool collapseGeneratedNodesOnly = true;

    [Header("Cover Hint Build")]
    [SerializeField] private bool autoBuildCoverHintsAfterImport = true;
    [SerializeField] private bool affectGeneratedNodesOnlyForCoverHints = true;
    [SerializeField] [Range(0f, 1f)] private float autoCoverMinCover = 0.45f;
    [SerializeField] [Range(0f, 1f)] private float autoCoverMinOcclusion = 0.25f;
    [SerializeField] [Range(0f, 1f)] private float autoCoverMinGroundSafety = 0.35f;

    [Header("Text Export")]
    [SerializeField] private string textExportRelativePath = "Assets/OUT/OUT_Runtime/Data/AI/out_ai_graph_bake.txt";

    [Header("Debug")]
    [SerializeField] private bool logImportStats = true;

    private readonly Collider[] _overlapBuffer = new Collider[32];

    [ContextMenu("Import Nodes From Unity NavMesh")]
    public void ImportNodesFromUnityNavMesh()
    {
        if (nodesRoot == null)
            nodesRoot = transform;

        if (clearGeneratedNodesBeforeImport)
            ClearGeneratedNodes();

        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        if (triangulation.vertices == null || triangulation.vertices.Length == 0)
        {
            Debug.LogWarning("OUT_AIGraphAuthoring: Unity NavMesh triangulation is empty.");
            return;
        }

        Dictionary<OUT_QuantizedVertexKey, OUT_AIGraphNode> nodeMap = new Dictionary<OUT_QuantizedVertexKey, OUT_AIGraphNode>();
        OUT_AIGraphNode[] vertexToNode = new OUT_AIGraphNode[triangulation.vertices.Length];
        List<OUT_AIGraphNode> createdNodes = new List<OUT_AIGraphNode>(triangulation.vertices.Length);

        for (int i = 0; i < triangulation.vertices.Length; i++)
        {
            Vector3 vertex = triangulation.vertices[i];
            OUT_QuantizedVertexKey key = new OUT_QuantizedVertexKey(vertex, vertexMergeDistance);

            if (!nodeMap.TryGetValue(key, out OUT_AIGraphNode node))
            {
                GameObject nodeObject = new GameObject($"GN_{createdNodes.Count:0000}");
                nodeObject.transform.SetParent(nodesRoot, false);
                nodeObject.transform.position = vertex + Vector3.up * importedNodeYOffset;

                node = nodeObject.AddComponent<OUT_AIGraphNode>();
                node.SetGeneratedFromNavMesh(true);
                node.SetNodeName(nodeObject.name);

                nodeMap.Add(key, node);
                createdNodes.Add(node);
            }

            vertexToNode[i] = node;
        }

        Dictionary<OUT_AIGraphNode, HashSet<OUT_AIGraphNode>> adjacency = new Dictionary<OUT_AIGraphNode, HashSet<OUT_AIGraphNode>>(createdNodes.Count);
        for (int i = 0; i < createdNodes.Count; i++)
            adjacency[createdNodes[i]] = new HashSet<OUT_AIGraphNode>();

        int[] indices = triangulation.indices;
        for (int i = 0; i + 2 < indices.Length; i += 3)
        {
            OUT_AIGraphNode a = vertexToNode[indices[i + 0]];
            OUT_AIGraphNode b = vertexToNode[indices[i + 1]];
            OUT_AIGraphNode c = vertexToNode[indices[i + 2]];

            Link(adjacency, a, b);
            Link(adjacency, b, c);
            Link(adjacency, c, a);
        }

        foreach (KeyValuePair<OUT_AIGraphNode, HashSet<OUT_AIGraphNode>> pair in adjacency)
        {
            OUT_AIGraphNode[] links = new OUT_AIGraphNode[pair.Value.Count];
            pair.Value.CopyTo(links);
            pair.Key.SetLinks(links);
        }

        if (autoPruneInvalidLinksAfterImport)
            AutoPruneInvalidLinks();

        if (autoCollapseRedundantNodesAfterImport)
            CollapseRedundantNodes();

        if (autoBuildCoverHintsAfterImport)
            BuildCoverHints();

        if (logImportStats)
            Debug.Log($"OUT_AIGraphAuthoring: imported {createdNodes.Count} graph nodes from Unity NavMesh.");

        if (rebuildRuntimeGraphAfterImport)
            BuildRuntimeGraph();
    }

    [ContextMenu("Auto Link Nearest Valid Neighbours")]
    public void AutoLinkNearestValidNeighbours()
    {
        OUT_AIGraphNode[] sceneNodes = GetSceneNodes();
        if (sceneNodes.Length == 0)
            return;

        Dictionary<OUT_AIGraphNode, HashSet<OUT_AIGraphNode>> adjacency = new Dictionary<OUT_AIGraphNode, HashSet<OUT_AIGraphNode>>(sceneNodes.Length);
        List<OUT_AIGraphNode> usable = new List<OUT_AIGraphNode>(sceneNodes.Length);

        for (int i = 0; i < sceneNodes.Length; i++)
        {
            OUT_AIGraphNode node = sceneNodes[i];
            if (node == null || !node.NodeEnabled)
                continue;

            usable.Add(node);
            adjacency[node] = new HashSet<OUT_AIGraphNode>();
        }

        float maxDistanceSqr = neighbourLinkRadius * neighbourLinkRadius;
        int created = 0;

        for (int i = 0; i < usable.Count; i++)
        {
            OUT_AIGraphNode node = usable[i];
            List<OUT_AIGraphNode> candidates = new List<OUT_AIGraphNode>(16);

            for (int j = 0; j < usable.Count; j++)
            {
                if (i == j)
                    continue;

                OUT_AIGraphNode other = usable[j];
                float sqr = (other.transform.position - node.transform.position).sqrMagnitude;
                if (sqr > maxDistanceSqr)
                    continue;

                candidates.Add(other);
            }

            candidates.Sort((a, b) =>
                (a.transform.position - node.transform.position).sqrMagnitude.CompareTo(
                (b.transform.position - node.transform.position).sqrMagnitude));

            int accepted = 0;
            for (int j = 0; j < candidates.Count && accepted < maxNeighbourLinksPerNode; j++)
            {
                OUT_AIGraphNode other = candidates[j];
                if (!CanTraverse(node.transform.position, other.transform.position))
                    continue;

                if (adjacency[node].Add(other))
                    created++;
                adjacency[other].Add(node);
                accepted++;
            }
        }

        foreach (KeyValuePair<OUT_AIGraphNode, HashSet<OUT_AIGraphNode>> pair in adjacency)
        {
            OUT_AIGraphNode[] links = new OUT_AIGraphNode[pair.Value.Count];
            pair.Value.CopyTo(links);
            pair.Key.SetLinks(links);
        }

        if (logImportStats)
            Debug.Log($"OUT_AIGraphAuthoring: auto-linked nearest valid neighbours. Nodes: {usable.Count}, directed candidates accepted: {created}.");

        if (rebuildRuntimeGraphAfterAutoLink)
            BuildRuntimeGraph();
    }

    [ContextMenu("Auto Prune Invalid Links")]
    public void AutoPruneInvalidLinks()
    {
        OUT_AIGraphNode[] sceneNodes = GetSceneNodes();
        if (sceneNodes.Length == 0)
            return;

        Dictionary<OUT_AIGraphNode, HashSet<OUT_AIGraphNode>> adjacency = new Dictionary<OUT_AIGraphNode, HashSet<OUT_AIGraphNode>>(sceneNodes.Length);
        for (int i = 0; i < sceneNodes.Length; i++)
            adjacency[sceneNodes[i]] = new HashSet<OUT_AIGraphNode>();

        int removed = 0;

        for (int i = 0; i < sceneNodes.Length; i++)
        {
            OUT_AIGraphNode node = sceneNodes[i];
            if (node == null || !node.NodeEnabled)
                continue;

            OUT_AIGraphNode[] links = node.Links;
            if (links == null)
                continue;

            for (int j = 0; j < links.Length; j++)
            {
                OUT_AIGraphNode other = links[j];
                if (other == null || other == node || !other.NodeEnabled)
                {
                    removed++;
                    continue;
                }

                if (Vector3.Distance(node.transform.position, other.transform.position) < minLinkDistance)
                {
                    removed++;
                    continue;
                }

                if (!CanTraverse(node.transform.position, other.transform.position))
                {
                    removed++;
                    continue;
                }

                adjacency[node].Add(other);
                adjacency[other].Add(node);
            }
        }

        foreach (KeyValuePair<OUT_AIGraphNode, HashSet<OUT_AIGraphNode>> pair in adjacency)
        {
            OUT_AIGraphNode[] links = new OUT_AIGraphNode[pair.Value.Count];
            pair.Value.CopyTo(links);
            pair.Key.SetLinks(links);
        }

        if (logImportStats)
            Debug.Log($"OUT_AIGraphAuthoring: pruned invalid links. Removed candidates: {removed}.");
    }

    [ContextMenu("Collapse Redundant Nodes")]
    public void CollapseRedundantNodes()
    {
        OUT_AIGraphNode[] sceneNodes = GetSceneNodes();
        if (sceneNodes.Length == 0)
            return;

        int collapsed = 0;
        bool changed;
        int safety = 0;

        do
        {
            changed = false;
            safety++;
            if (safety > 8)
                break;

            sceneNodes = GetSceneNodes();
            for (int i = 0; i < sceneNodes.Length; i++)
            {
                OUT_AIGraphNode node = sceneNodes[i];
                if (node == null || !node.NodeEnabled)
                    continue;

                if (collapseGeneratedNodesOnly && !node.GeneratedFromNavMesh)
                    continue;

                if (node.IsCoverHint)
                    continue;

                OUT_AIGraphNode[] links = GetValidLinks(node);
                if (links.Length != 2)
                    continue;

                OUT_AIGraphNode a = links[0];
                OUT_AIGraphNode b = links[1];
                if (a == null || b == null || a == b)
                    continue;

                Vector3 dirA = (a.transform.position - node.transform.position).normalized;
                Vector3 dirB = (b.transform.position - node.transform.position).normalized;
                float dot = Vector3.Dot(dirA, dirB);

                if (dot > -redundantNodeMinDot)
                    continue;

                if (!CanTraverse(a.transform.position, b.transform.position))
                    continue;

                ConnectBidirectional(a, b);
                RemoveBidirectional(a, node);
                RemoveBidirectional(b, node);

                node.SetLinks(new OUT_AIGraphNode[0]);
                node.SetNodeEnabled(false);

                collapsed++;
                changed = true;
                break;
            }
        }
        while (changed);

        if (logImportStats)
            Debug.Log($"OUT_AIGraphAuthoring: collapsed redundant nodes: {collapsed}.");
    }

    [ContextMenu("Build Cover Hints")]
    public void BuildCoverHints()
    {
        OUT_AIGraphNode[] sceneNodes = GetSceneNodes();
        if (sceneNodes.Length == 0)
            return;

        if (sensoryField == null)
            sensoryField = OUT_SceneSensoryField.Instance != null ? OUT_SceneSensoryField.Instance : FindObjectOfType<OUT_SceneSensoryField>();

        if (sensoryField != null && !sensoryField.HasBakedData)
            sensoryField.RebuildField();

        int marked = 0;

        for (int i = 0; i < sceneNodes.Length; i++)
        {
            OUT_AIGraphNode node = sceneNodes[i];
            if (node == null || !node.NodeEnabled)
                continue;

            if (affectGeneratedNodesOnlyForCoverHints && !node.GeneratedFromNavMesh)
                continue;

            OUT_SceneSensorySample sensory = default;
            sensory.Clear();

            if (sensoryField != null)
            {
                if (!sensoryField.TrySampleStatic(node.transform.position, out sensory))
                    sensory.Clear();
            }

            node.SetBakedSensory(sensory);

            bool isCover = sensory.Cover >= autoCoverMinCover
                && sensory.Occlusion >= autoCoverMinOcclusion
                && sensory.GroundSafety >= autoCoverMinGroundSafety;

            node.SetCoverHint(isCover);
            if (isCover)
                marked++;
        }

        if (logImportStats)
            Debug.Log($"OUT_AIGraphAuthoring: built cover hints. Marked: {marked}.");
    }

    [ContextMenu("Build Runtime Graph")]
    public void BuildRuntimeGraph()
    {
        if (targetGraph == null)
            targetGraph = GetComponent<OUT_AIGraph>();

        if (targetGraph == null)
        {
            Debug.LogError("OUT_AIGraphAuthoring: targetGraph is null.");
            return;
        }

        if (nodesRoot == null)
            nodesRoot = transform;

        if (sensoryField == null)
            sensoryField = OUT_SceneSensoryField.Instance != null ? OUT_SceneSensoryField.Instance : FindObjectOfType<OUT_SceneSensoryField>();

        if (sensoryField != null && !sensoryField.HasBakedData)
            sensoryField.RebuildField();

        OUT_AIGraphNode[] allSceneNodes = GetSceneNodes();
        List<OUT_AIGraphNode> enabledNodes = new List<OUT_AIGraphNode>(allSceneNodes.Length);

        for (int i = 0; i < allSceneNodes.Length; i++)
        {
            if (allSceneNodes[i] != null && allSceneNodes[i].NodeEnabled)
                enabledNodes.Add(allSceneNodes[i]);
        }

        if (enabledNodes.Count == 0)
        {
            targetGraph.ApplyNodes(new OUT_AIGraph.OUT_RuntimeNode[0]);
            Debug.LogWarning("OUT_AIGraphAuthoring: no enabled OUT_AIGraphNode components found.");
            return;
        }

        Dictionary<OUT_AIGraphNode, int> indexMap = new Dictionary<OUT_AIGraphNode, int>(enabledNodes.Count);
        List<OUT_AIGraph.OUT_RuntimeNode> runtimeNodes = new List<OUT_AIGraph.OUT_RuntimeNode>(enabledNodes.Count);

        for (int i = 0; i < enabledNodes.Count; i++)
        {
            indexMap[enabledNodes[i]] = i;
            runtimeNodes.Add(null);
        }

        for (int i = 0; i < enabledNodes.Count; i++)
        {
            OUT_AIGraphNode sceneNode = enabledNodes[i];
            OUT_SceneSensorySample sensory = default;
            sensory.Clear();

            if (sensoryField != null)
            {
                if (!sensoryField.TrySampleStatic(sceneNode.transform.position, out sensory))
                    sensory = sceneNode.BakedSensory;
            }
            else
            {
                sensory = sceneNode.BakedSensory;
            }

            sceneNode.SetBakedSensory(sensory);

            List<int> links = new List<int>();
            OUT_AIGraphNode[] nodeLinks = sceneNode.Links;
            if (nodeLinks != null)
            {
                for (int j = 0; j < nodeLinks.Length; j++)
                {
                    OUT_AIGraphNode linkedNode = nodeLinks[j];
                    if (linkedNode == null || !linkedNode.NodeEnabled)
                        continue;

                    if (!indexMap.TryGetValue(linkedNode, out int linkedIndex))
                        continue;

                    if (linkedIndex == i)
                        continue;

                    if (!links.Contains(linkedIndex))
                        links.Add(linkedIndex);
                }
            }

            runtimeNodes[i] = new OUT_AIGraph.OUT_RuntimeNode
            {
                Name = sceneNode.NodeName,
                Position = sceneNode.transform.position,
                Links = links.ToArray(),
                Enabled = sceneNode.NodeEnabled,
                IsCoverHint = sceneNode.IsCoverHint,
                Sensory = sensory
            };
        }

        targetGraph.ApplyNodes(runtimeNodes.ToArray());

        if (logImportStats)
            Debug.Log($"OUT_AIGraphAuthoring: built runtime graph with {runtimeNodes.Count} enabled nodes.");
    }

    [ContextMenu("Export Runtime Graph To Text File")]
    public void ExportRuntimeGraphToTextFile()
    {
        if (targetGraph == null)
            targetGraph = GetComponent<OUT_AIGraph>();

        if (targetGraph == null)
        {
            Debug.LogError("OUT_AIGraphAuthoring: targetGraph is null, cannot export.");
            return;
        }

        if (!targetGraph.HasNodes)
            BuildRuntimeGraph();

        string path = textExportRelativePath;
        if (string.IsNullOrWhiteSpace(path))
            path = "Assets/OUT/OUT_Runtime/Data/AI/out_ai_graph_bake.txt";

        if (!Path.IsPathRooted(path))
            path = path.Replace('\\', '/');

        OUT_AIGraphTextSerializer.SaveRuntimeGraph(targetGraph, path);
    }

    [ContextMenu("Clear Generated Nodes")]
    public void ClearGeneratedNodes()
    {
        if (nodesRoot == null)
            nodesRoot = transform;

        List<GameObject> toDestroy = new List<GameObject>();
        OUT_AIGraphNode[] sceneNodes = nodesRoot.GetComponentsInChildren<OUT_AIGraphNode>(true);

        for (int i = 0; i < sceneNodes.Length; i++)
        {
            if (sceneNodes[i] != null && sceneNodes[i].GeneratedFromNavMesh)
                toDestroy.Add(sceneNodes[i].gameObject);
        }

        for (int i = 0; i < toDestroy.Count; i++)
        {
            if (Application.isPlaying)
                Destroy(toDestroy[i]);
            else
                DestroyImmediate(toDestroy[i]);
        }
    }

    [ContextMenu("Import NavMesh And Build Graph")]
    public void ImportNavMeshAndBuildGraph()
    {
        ImportNodesFromUnityNavMesh();
        BuildRuntimeGraph();
    }

    private OUT_AIGraphNode[] GetSceneNodes()
    {
        if (nodesRoot == null)
            nodesRoot = transform;

        return nodesRoot.GetComponentsInChildren<OUT_AIGraphNode>(true);
    }

    private OUT_AIGraphNode[] GetValidLinks(OUT_AIGraphNode node)
    {
        OUT_AIGraphNode[] links = node.Links;
        if (links == null || links.Length == 0)
            return new OUT_AIGraphNode[0];

        List<OUT_AIGraphNode> result = new List<OUT_AIGraphNode>(links.Length);
        for (int i = 0; i < links.Length; i++)
        {
            OUT_AIGraphNode other = links[i];
            if (other == null || !other.NodeEnabled || other == node)
                continue;

            if (!result.Contains(other))
                result.Add(other);
        }

        return result.ToArray();
    }

    private void ConnectBidirectional(OUT_AIGraphNode a, OUT_AIGraphNode b)
    {
        AddLinkIfMissing(a, b);
        AddLinkIfMissing(b, a);
    }

    private void RemoveBidirectional(OUT_AIGraphNode a, OUT_AIGraphNode b)
    {
        RemoveLinkIfPresent(a, b);
        RemoveLinkIfPresent(b, a);
    }

    private void AddLinkIfMissing(OUT_AIGraphNode owner, OUT_AIGraphNode target)
    {
        if (owner == null || target == null || owner == target)
            return;

        List<OUT_AIGraphNode> links = new List<OUT_AIGraphNode>();
        OUT_AIGraphNode[] current = owner.Links;
        if (current != null)
        {
            for (int i = 0; i < current.Length; i++)
            {
                if (current[i] != null && current[i] != owner && !links.Contains(current[i]))
                    links.Add(current[i]);
            }
        }

        if (!links.Contains(target))
            links.Add(target);

        owner.SetLinks(links.ToArray());
    }

    private void RemoveLinkIfPresent(OUT_AIGraphNode owner, OUT_AIGraphNode target)
    {
        if (owner == null)
            return;

        List<OUT_AIGraphNode> links = new List<OUT_AIGraphNode>();
        OUT_AIGraphNode[] current = owner.Links;
        if (current != null)
        {
            for (int i = 0; i < current.Length; i++)
            {
                OUT_AIGraphNode candidate = current[i];
                if (candidate == null || candidate == target || candidate == owner)
                    continue;

                if (!links.Contains(candidate))
                    links.Add(candidate);
            }
        }

        owner.SetLinks(links.ToArray());
    }

    private bool CanTraverse(Vector3 start, Vector3 end)
    {
        Vector3 delta = end - start;
        delta.y = 0f;

        float distance = delta.magnitude;
        if (distance < minLinkDistance)
            return false;

        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / Mathf.Max(0.1f, linkProbeStep)));

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector3 sample = Vector3.Lerp(start, end, t);
            if (!IsPositionFree(sample))
                return false;
        }

        return true;
    }

    private bool IsPositionFree(Vector3 position)
    {
        GetCapsulePoints(position, out Vector3 p1, out Vector3 p2);

        int hitCount = Physics.OverlapCapsuleNonAlloc(
            p1,
            p2,
            agentRadius,
            _overlapBuffer,
            obstacleMask,
            triggerInteraction);

        Transform ownRoot = transform.root;

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = _overlapBuffer[i];
            if (col == null)
                continue;

            if (col.transform.root == ownRoot)
                continue;

            return false;
        }

        return true;
    }

    private void GetCapsulePoints(Vector3 center, out Vector3 p1, out Vector3 p2)
    {
        float halfHeight = Mathf.Max(agentRadius + 0.01f, agentHeight * 0.5f - agentRadius);
        Vector3 up = Vector3.up * halfHeight;

        p1 = center + up;
        p2 = center - up;
    }

    private void Link(Dictionary<OUT_AIGraphNode, HashSet<OUT_AIGraphNode>> adjacency, OUT_AIGraphNode a, OUT_AIGraphNode b)
    {
        if (a == null || b == null || a == b)
            return;

        adjacency[a].Add(b);
        adjacency[b].Add(a);
    }
}
