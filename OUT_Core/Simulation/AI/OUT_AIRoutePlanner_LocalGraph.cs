using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[DisallowMultipleComponent]
public class OUT_AIRoutePlanner_LocalGraph : MonoBehaviour, IOutAIRoutePlanner
{
    [System.Serializable]
    public class GraphNode
    {
        public string Name;
        public Transform Point;
        public int[] Links;
        public bool IsCoverHint = false;
        public bool Enabled = true;
    }

    private struct NodeDistance
    {
        public int Index;
        public float Distance;

        public NodeDistance(int index, float distance)
        {
            Index = index;
            Distance = distance;
        }
    }

    [Header("Shared Graph")]
    [SerializeField] private OUT_AIGraph sharedGraph;
    [SerializeField] private bool autoFindGraphInParents = true;

    [Header("Collision / Hull")]
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private float agentRadius = 0.35f;
    [SerializeField] private float agentHeight = 1.8f;
    [SerializeField] private float localMoveStep = 0.75f;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
    [SerializeField] [Min(0f)] private float floorIgnoreEpsilon = 0.08f;

    [Header("Triangulation")]
    [SerializeField] private float triangulationStartOffset = 1.5f;
    [SerializeField] private float triangulationStep = 1.25f;
    [SerializeField] private int triangulationIterations = 6;
    [SerializeField] private bool useDeterministicSideBias = true;

    [Header("Goal Spreading")]
    [SerializeField] private bool useGoalSpreading = true;
    [SerializeField] [Min(0f)] private float goalSpreadRadius = 0.75f;
    [SerializeField] private bool spreadCoverTargets = false;

    [Header("Crowd")]
    [SerializeField] private OUT_AICrowdAgent crowdAgent;
    [SerializeField] private bool useLocalSeparation = true;
    [SerializeField] private bool treatDynamicAgentsAsObstacles = false;

    [Header("Cover Search")]
    [SerializeField] private int radialCoverSamples = 16;
    [SerializeField] private float coverSampleStep = 1.25f;
    [SerializeField] private float coverEyeHeight = 1.4f;

    [Header("Route")]
    [SerializeField] private float waypointReachDistance = 0.6f;
    [SerializeField] private bool useSteeringLookAhead = true;
    [SerializeField] [Min(1)] private int steeringLookAheadPoints = 4;
    [SerializeField] [Min(0.1f)] private float steeringLookAheadDistance = 6f;

    [Header("Legacy Local Graph (Fallback)")]
    [SerializeField] private GraphNode[] graphNodes;

    private readonly List<Vector3> _routePoints = new List<Vector3>(16);
    private readonly Collider[] _overlapBuffer = new Collider[32];
    private readonly Dictionary<Transform, bool> _dynamicAgentRootCache = new Dictionary<Transform, bool>(128);

    private float[] _gScoreBuffer;
    private int[] _cameFromBuffer;
    private bool[] _openSetBuffer;
    private bool[] _closedSetBuffer;

    private OUT_AIRouteRequest _lastRequest;
    private bool _hasLastRequest;
    private int _routeIndex;

    public bool HasActiveRoute => _routePoints.Count > 0 && _routeIndex < _routePoints.Count;
    public Vector3 CurrentWaypoint => HasActiveRoute ? GetSteeringTarget() : transform.position;

    private void Awake()
    {
        if (crowdAgent == null)
            crowdAgent = GetComponent<OUT_AICrowdAgent>();

        ResolveSharedGraphReference();
    }

    private void OnEnable()
    {
        ResolveSharedGraphReference();
    }

    private void OnDisable()
    {
        _dynamicAgentRootCache.Clear();
    }

    private void Update()
    {
        if (!HasActiveRoute)
            return;

        float reachSqr = waypointReachDistance * waypointReachDistance;
        float sqr = (transform.position - _routePoints[_routeIndex]).sqrMagnitude;
        if (sqr <= reachSqr)
        {
            _routeIndex++;

            if (_routeIndex >= _routePoints.Count)
                ClearRoute();
        }
    }

    public bool TryBuildRoute(in OUT_AIRouteRequest request, out Vector3 firstWaypoint)
    {
        ResolveSharedGraphReference();

        _lastRequest = request;
        _hasLastRequest = true;

        ClearRoute();

        Vector3 currentPosition = transform.position;
        Vector3 destination = request.Destination;

        if (useGoalSpreading)
        {
            if (crowdAgent != null && OUT_AICrowdService.Instance != null)
            {
                Vector3 forwardHint = destination - currentPosition;
                forwardHint.y = 0f;
                destination = OUT_AICrowdService.Instance.GetSpreadGoal(crowdAgent, destination, forwardHint);
            }
            else
            {
                destination = ApplyGoalSpread(request, destination);
            }
        }

        if (request.RequireCover)
        {
            if (TryFindCover(request.ThreatPosition, request.MinDistance, request.MaxDistance, out Vector3 coverPoint))
            {
                if (CanMoveDirect(currentPosition, coverPoint))
                {
                    PushRoutePoint(coverPoint);
                    firstWaypoint = CurrentWaypoint;
                    return true;
                }

                if (TryBuildGraphRoute(currentPosition, coverPoint, requireGoalVisibility: false))
                {
                    firstWaypoint = CurrentWaypoint;
                    return true;
                }
            }
        }

        if (CanMoveDirect(currentPosition, destination))
        {
            PushRoutePoint(destination);
            firstWaypoint = CurrentWaypoint;
            return true;
        }

        if (request.AllowTriangulation && TryTriangulate(destination, out Vector3 apexPoint))
        {
            PushRoutePoint(apexPoint);

            if (CanMoveDirect(apexPoint, destination))
                PushRoutePoint(destination);

            firstWaypoint = CurrentWaypoint;
            return true;
        }

        if (TryBuildGraphRoute(currentPosition, destination, requireGoalVisibility: true))
        {
            firstWaypoint = CurrentWaypoint;
            return true;
        }

        firstWaypoint = default;
        return false;
    }

    public bool TryFindCover(Vector3 threatPosition, float minDistance, float maxDistance, out Vector3 coverPoint)
    {
        Vector3 currentPosition = transform.position;
        float bestScore = float.MaxValue;
        coverPoint = default;
        bool found = false;

        int nodeCount = GetNodeCount();
        for (int i = 0; i < nodeCount; i++)
        {
            if (!IsNodeUsable(i) || !IsNodeCoverHint(i))
                continue;

            Vector3 point = GetNodePosition(i);
            if (!IsValidCoverPoint(point, threatPosition, minDistance, maxDistance))
                continue;

            float score = (currentPosition - point).sqrMagnitude;
            if (score < bestScore)
            {
                bestScore = score;
                coverPoint = point;
                found = true;
            }
        }

        if (found)
            return true;

        float searchMin = Mathf.Max(0f, minDistance);
        float searchMax = Mathf.Max(searchMin + 0.1f, maxDistance);

        for (int ring = 1; ; ring++)
        {
            float radius = ring * coverSampleStep;
            if (radius > searchMax)
                break;

            if (radius < searchMin)
                continue;

            for (int i = 0; i < radialCoverSamples; i++)
            {
                float angle = (i / (float)radialCoverSamples) * Mathf.PI * 2f;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                Vector3 sample = currentPosition + offset;

                if (!IsValidCoverPoint(sample, threatPosition, minDistance, maxDistance))
                    continue;

                float score = (currentPosition - sample).sqrMagnitude;
                if (score < bestScore)
                {
                    bestScore = score;
                    coverPoint = sample;
                    found = true;
                }
            }
        }

        return found;
    }

    public bool TryTriangulate(Vector3 destination, out Vector3 apexPoint)
    {
        Vector3 origin = transform.position;
        Vector3 toDestination = destination - origin;
        toDestination.y = 0f;

        apexPoint = default;

        if (toDestination.sqrMagnitude < 0.001f)
            return false;

        Vector3 forward = toDestination.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        int bias = GetSideBiasSign();

        for (int i = 0; i < triangulationIterations; i++)
        {
            float lateralDistance = triangulationStartOffset + triangulationStep * i;
            float forwardDistance = Mathf.Min(toDestination.magnitude, triangulationStartOffset + triangulationStep * (i + 1));

            Vector3 leftCandidate = origin + forward * forwardDistance - right * lateralDistance;
            Vector3 rightCandidate = origin + forward * forwardDistance + right * lateralDistance;

            if (bias >= 0)
            {
                if (TryTriangulationCandidate(origin, leftCandidate, destination, out apexPoint))
                    return true;
                if (TryTriangulationCandidate(origin, rightCandidate, destination, out apexPoint))
                    return true;
            }
            else
            {
                if (TryTriangulationCandidate(origin, rightCandidate, destination, out apexPoint))
                    return true;
                if (TryTriangulationCandidate(origin, leftCandidate, destination, out apexPoint))
                    return true;
            }
        }

        return false;
    }

    public void RefreshRoute()
    {
        if (!_hasLastRequest)
            return;

        if (!_lastRequest.RefreshIfStale)
            return;

        TryBuildRoute(_lastRequest, out _);
    }

    public void ClearRoute()
    {
        _routePoints.Clear();
        _routeIndex = 0;
    }

    private Vector3 GetSteeringTarget()
    {
        if (!useSteeringLookAhead || !HasActiveRoute)
            return _routePoints[_routeIndex];

        Vector3 origin = transform.position;
        Vector3 best = _routePoints[_routeIndex];
        int maxIndex = Mathf.Min(_routePoints.Count - 1, _routeIndex + Mathf.Max(1, steeringLookAheadPoints));
        float maxDistanceSqr = steeringLookAheadDistance * steeringLookAheadDistance;

        for (int i = _routeIndex; i <= maxIndex; i++)
        {
            Vector3 candidate = _routePoints[i];
            if ((candidate - origin).sqrMagnitude > maxDistanceSqr)
                break;

            if (!CanMoveDirect(origin, candidate))
                break;

            best = candidate;
        }

        if (useLocalSeparation && crowdAgent != null && OUT_AICrowdService.Instance != null)
            best += OUT_AICrowdService.Instance.GetLocalSeparationOffset(crowdAgent);

        return best;
    }

    private Vector3 ApplyGoalSpread(in OUT_AIRouteRequest request, Vector3 destination)
    {
        if (!useGoalSpreading)
            return destination;

        if (goalSpreadRadius <= 0.001f)
            return destination;

        if (request.RequireCover && !spreadCoverTargets)
            return destination;

        float hash01 = GetDeterministicHash01();
        float angle = hash01 * Mathf.PI * 2f;
        float radius = goalSpreadRadius * (0.55f + 0.45f * GetDeterministicHash01(31));
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        return destination + offset;
    }

    private bool TryBuildGraphRoute(Vector3 start, Vector3 goal, bool requireGoalVisibility)
    {
        if (GetNodeCount() == 0)
            return false;

        List<int> startCandidates = ListPool<int>.Get();
        List<int> goalCandidates = ListPool<int>.Get();
        List<int> bestPath = null;

        try
        {
            CollectReachableNodes(start, startCandidates);
            if (startCandidates.Count == 0)
                return false;

            CollectGoalCandidates(goal, requireGoalVisibility, goalCandidates);
            if (goalCandidates.Count == 0)
                return false;

            float bestCost = float.MaxValue;
            int bestStartNode = -1;
            int bestGoalNode = -1;

            for (int s = 0; s < startCandidates.Count; s++)
            {
                int startNode = startCandidates[s];

                for (int g = 0; g < goalCandidates.Count; g++)
                {
                    int goalNode = goalCandidates[g];
                    List<int> path = ListPool<int>.Get();

                    try
                    {
                        if (!TryFindNodePath(startNode, goalNode, path, out float cost))
                            continue;

                        float totalCost = cost
                            + (start - GetNodePosition(startNode)).sqrMagnitude
                            + (goal - GetNodePosition(goalNode)).sqrMagnitude;

                        totalCost += path.Count * 0.01f;

                        if (totalCost < bestCost)
                        {
                            bestCost = totalCost;

                            if (bestPath == null)
                                bestPath = ListPool<int>.Get();

                            bestPath.Clear();
                            bestPath.AddRange(path);
                            bestStartNode = startNode;
                            bestGoalNode = goalNode;
                        }
                    }
                    finally
                    {
                        ListPool<int>.Release(path);
                    }
                }
            }

            if (bestPath == null || bestPath.Count == 0)
                return false;

            if (!CanMoveDirect(start, GetNodePosition(bestStartNode)))
                return false;

            for (int i = 0; i < bestPath.Count; i++)
                PushRoutePoint(GetNodePosition(bestPath[i]));

            if (!requireGoalVisibility || CanMoveDirect(GetNodePosition(bestGoalNode), goal))
                PushRoutePoint(goal);

            return HasActiveRoute;
        }
        finally
        {
            if (bestPath != null)
                ListPool<int>.Release(bestPath);

            ListPool<int>.Release(goalCandidates);
            ListPool<int>.Release(startCandidates);
        }
    }

    private void CollectReachableNodes(Vector3 from, List<int> result)
    {
        result.Clear();
        int nodeCount = GetNodeCount();

        for (int i = 0; i < nodeCount; i++)
        {
            if (!IsNodeUsable(i))
                continue;

            Vector3 point = GetNodePosition(i);
            if (CanMoveDirect(from, point))
                result.Add(i);
        }
    }

    private void CollectGoalCandidates(Vector3 goal, bool requireGoalVisibility, List<int> result)
    {
        result.Clear();
        List<NodeDistance> temp = ListPool<NodeDistance>.Get();

        try
        {
            int nodeCount = GetNodeCount();

            for (int i = 0; i < nodeCount; i++)
            {
                if (!IsNodeUsable(i))
                    continue;

                Vector3 point = GetNodePosition(i);
                if (requireGoalVisibility && !CanMoveDirect(point, goal))
                    continue;

                float bias = useDeterministicSideBias ? GetGoalSideBiasPenalty(goal, point) : 0f;
                temp.Add(new NodeDistance(i, (point - goal).sqrMagnitude + bias));
            }

            temp.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            int limit = Mathf.Min(6, temp.Count);
            for (int i = 0; i < limit; i++)
                result.Add(temp[i].Index);
        }
        finally
        {
            ListPool<NodeDistance>.Release(temp);
        }
    }

    private bool TryFindNodePath(int startNode, int goalNode, List<int> path, out float cost)
    {
        path.Clear();
        cost = 0f;

        if (startNode == goalNode)
        {
            path.Add(startNode);
            return true;
        }

        int nodeCount = GetNodeCount();
        EnsurePathBuffers(nodeCount);

        for (int i = 0; i < nodeCount; i++)
        {
            _gScoreBuffer[i] = float.MaxValue;
            _cameFromBuffer[i] = -1;
            _openSetBuffer[i] = false;
            _closedSetBuffer[i] = false;
        }

        _gScoreBuffer[startNode] = 0f;
        _openSetBuffer[startNode] = true;

        while (true)
        {
            int current = -1;
            float bestF = float.MaxValue;

            for (int i = 0; i < nodeCount; i++)
            {
                if (!_openSetBuffer[i] || _closedSetBuffer[i] || !IsNodeUsable(i))
                    continue;

                float h = (GetNodePosition(i) - GetNodePosition(goalNode)).sqrMagnitude;
                float f = _gScoreBuffer[i] + h;

                if (f < bestF)
                {
                    bestF = f;
                    current = i;
                }
            }

            if (current < 0)
                return false;

            if (current == goalNode)
            {
                ReconstructPath(_cameFromBuffer, current, path);
                cost = _gScoreBuffer[current];
                return true;
            }

            _openSetBuffer[current] = false;
            _closedSetBuffer[current] = true;

            int[] links = GetNodeLinks(current);
            if (links == null)
                continue;

            for (int i = 0; i < links.Length; i++)
            {
                int neighbour = links[i];
                if (neighbour < 0 || neighbour >= nodeCount)
                    continue;

                if (!IsNodeUsable(neighbour) || _closedSetBuffer[neighbour])
                    continue;

                Vector3 currentPos = GetNodePosition(current);
                Vector3 neighbourPos = GetNodePosition(neighbour);

                if (!CanMoveDirect(currentPos, neighbourPos))
                    continue;

                float tentative = _gScoreBuffer[current] + Vector3.Distance(currentPos, neighbourPos);
                if (tentative >= _gScoreBuffer[neighbour])
                    continue;

                _cameFromBuffer[neighbour] = current;
                _gScoreBuffer[neighbour] = tentative;
                _openSetBuffer[neighbour] = true;
            }
        }
    }

    private void EnsurePathBuffers(int nodeCount)
    {
        if (_gScoreBuffer != null && _gScoreBuffer.Length >= nodeCount)
            return;

        _gScoreBuffer = new float[nodeCount];
        _cameFromBuffer = new int[nodeCount];
        _openSetBuffer = new bool[nodeCount];
        _closedSetBuffer = new bool[nodeCount];
    }

    private bool TryTriangulationCandidate(Vector3 origin, Vector3 candidate, Vector3 destination, out Vector3 apexPoint)
    {
        apexPoint = default;

        if (!CanMoveDirect(origin, candidate))
            return false;

        if (!CanMoveDirect(candidate, destination))
            return false;

        apexPoint = candidate;
        return true;
    }

    private float GetGoalSideBiasPenalty(Vector3 goal, Vector3 candidate)
    {
        int sign = GetSideBiasSign();
        Vector3 toCandidate = candidate - transform.position;
        Vector3 toGoal = goal - transform.position;
        toCandidate.y = 0f;
        toGoal.y = 0f;

        if (toCandidate.sqrMagnitude < 0.001f || toGoal.sqrMagnitude < 0.001f)
            return 0f;

        float cross = Vector3.Cross(toGoal.normalized, toCandidate.normalized).y;
        bool samePreferredSide = sign >= 0 ? cross >= 0f : cross <= 0f;
        return samePreferredSide ? 0f : 0.05f;
    }

    private int GetSideBiasSign()
    {
        if (!useDeterministicSideBias)
            return 1;

        return GetDeterministicHash01() >= 0.5f ? 1 : -1;
    }

    private float GetDeterministicHash01(int salt = 0)
    {
        int baseSeed = crowdAgent != null ? crowdAgent.StableSeed : transform.GetInstanceID();
        int value = baseSeed ^ (salt * 73856093);

        unchecked
        {
            value = (value ^ 61) ^ (value >> 16);
            value *= 9;
            value = value ^ (value >> 4);
            value *= 0x27d4eb2d;
            value = value ^ (value >> 15);
        }

        uint unsignedValue = (uint)value;
        return (unsignedValue & 0x00FFFFFF) / 16777215f;
    }

    private void ReconstructPath(int[] cameFrom, int current, List<int> result)
    {
        result.Clear();
        result.Add(current);

        while (cameFrom[current] >= 0)
        {
            current = cameFrom[current];
            result.Add(current);
        }

        result.Reverse();
    }

    private void PushRoutePoint(Vector3 point)
    {
        if (_routePoints.Count > 0)
        {
            Vector3 prev = _routePoints[_routePoints.Count - 1];
            if ((prev - point).sqrMagnitude < 0.01f)
                return;
        }

        _routePoints.Add(point);
    }

    private bool IsNodeUsable(int index)
    {
        if (sharedGraph != null && sharedGraph.HasNodes)
            return sharedGraph.IsNodeUsable(index);

        if (graphNodes == null || index < 0 || index >= graphNodes.Length)
            return false;

        GraphNode node = graphNodes[index];
        return node != null && node.Enabled && node.Point != null;
    }

    private Vector3 GetNodePosition(int index)
    {
        if (sharedGraph != null && sharedGraph.HasNodes)
            return sharedGraph.GetNodePosition(index);

        return graphNodes[index].Point.position;
    }

    private int[] GetNodeLinks(int index)
    {
        if (sharedGraph != null && sharedGraph.HasNodes)
            return sharedGraph.GetNodeLinks(index);

        return graphNodes[index].Links;
    }

    private bool IsNodeCoverHint(int index)
    {
        if (sharedGraph != null && sharedGraph.HasNodes)
            return sharedGraph.IsNodeCoverHint(index);

        return graphNodes[index].IsCoverHint;
    }

    private int GetNodeCount()
    {
        if (sharedGraph != null && sharedGraph.HasNodes)
            return sharedGraph.NodeCount;

        return graphNodes != null ? graphNodes.Length : 0;
    }

    private void ResolveSharedGraphReference()
    {
        if (!autoFindGraphInParents)
            return;

        if (sharedGraph != null && sharedGraph.HasNodes)
            return;

        sharedGraph = GetComponentInParent<OUT_AIGraph>();
    }

    private bool IsValidCoverPoint(Vector3 point, Vector3 threatPosition, float minDistance, float maxDistance)
    {
        float distance = Vector3.Distance(threatPosition, point);
        if (distance < minDistance)
            return false;

        if (maxDistance > 0f && distance > maxDistance)
            return false;

        if (!IsPositionFree(point))
            return false;

        Vector3 pointEye = point + Vector3.up * coverEyeHeight;
        Vector3 threatEye = threatPosition + Vector3.up * coverEyeHeight;

        if (!Physics.Linecast(threatEye, pointEye, obstacleMask, triggerInteraction))
            return false;

        return true;
    }

    private bool CanMoveDirect(Vector3 start, Vector3 end)
    {
        Vector3 delta = end - start;
        delta.y = 0f;

        float distance = delta.magnitude;
        if (distance < 0.001f)
            return IsPositionFree(end);

        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / Mathf.Max(0.1f, localMoveStep)));

        for (int i = 1; i <= steps; i++)
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

            if (ShouldIgnoreObstacle(col, ownRoot, position))
                continue;

            return false;
        }

        return true;
    }

    private bool ShouldIgnoreObstacle(Collider col, Transform ownRoot, Vector3 samplePosition)
    {
        Transform otherRoot = col.transform.root;
        if (otherRoot == ownRoot)
            return true;

        if (!treatDynamicAgentsAsObstacles && IsDynamicAgentRoot(otherRoot))
            return true;

        if (col.bounds.max.y <= samplePosition.y + Mathf.Max(0f, floorIgnoreEpsilon))
            return true;

        return false;
    }

    private bool IsDynamicAgentRoot(Transform root)
    {
        if (root == null)
            return false;

        if (_dynamicAgentRootCache.TryGetValue(root, out bool isDynamicAgent))
            return isDynamicAgent;

        isDynamicAgent = root.GetComponentInChildren<OUT_AICrowdAgent>() != null ||
                         root.GetComponentInChildren<OUT_AIActorBrain>() != null;

        _dynamicAgentRootCache[root] = isDynamicAgent;
        return isDynamicAgent;
    }

    private void GetCapsulePoints(Vector3 center, out Vector3 p1, out Vector3 p2)
    {
        float halfHeight = Mathf.Max(agentRadius + 0.01f, agentHeight * 0.5f - agentRadius);
        Vector3 up = Vector3.up * halfHeight;

        p1 = center + up;
        p2 = center - up;
    }

    private void OnDrawGizmosSelected()
    {
        DrawGraphGizmos();
        DrawRouteGizmos();
    }

    private void DrawGraphGizmos()
    {
        int nodeCount = GetNodeCount();
        if (nodeCount == 0)
            return;

        for (int i = 0; i < nodeCount; i++)
        {
            if (!IsNodeUsable(i))
                continue;

            Gizmos.color = IsNodeCoverHint(i) ? Color.yellow : Color.cyan;
            Gizmos.DrawWireSphere(GetNodePosition(i), 0.2f);

            int[] links = GetNodeLinks(i);
            if (links == null)
                continue;

            Gizmos.color = Color.gray;
            for (int j = 0; j < links.Length; j++)
            {
                int link = links[j];
                if (link < 0 || link >= nodeCount || !IsNodeUsable(link))
                    continue;

                Gizmos.DrawLine(GetNodePosition(i), GetNodePosition(link));
            }
        }
    }

    private void DrawRouteGizmos()
    {
        if (!HasActiveRoute)
            return;

        Gizmos.color = Color.green;
        Vector3 prev = transform.position;

        for (int i = _routeIndex; i < _routePoints.Count; i++)
        {
            Gizmos.DrawLine(prev, _routePoints[i]);
            Gizmos.DrawSphere(_routePoints[i], 0.12f);
            prev = _routePoints[i];
        }
    }
}
