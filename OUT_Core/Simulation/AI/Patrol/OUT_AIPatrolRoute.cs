using UnityEngine;

[DisallowMultipleComponent]
public class OUT_AIPatrolRoute : MonoBehaviour, IOutAIPatrolProvider
{
    public enum PatrolMode
    {
        Loop = 0,
        PingPong = 1,
        Random = 2,
        OnceThenHold = 3
    }

    public enum StartMode
    {
        FirstPoint = 0,
        NearestPoint = 1,
        RandomPoint = 2
    }

    [Header("Route")]
    [SerializeField] private Transform[] points;
    [SerializeField] private PatrolMode patrolMode = PatrolMode.Loop;
    [SerializeField] private StartMode startMode = StartMode.NearestPoint;
    [SerializeField] private bool ignoreNullPoints = true;

    [Header("Timing")]
    [SerializeField][Min(0f)] private float waitTimeMin = 0.35f;
    [SerializeField][Min(0f)] private float waitTimeMax = 1.25f;
    [SerializeField] private bool randomizeWaitTime = true;

    [Header("Recovery")]
    [SerializeField] private bool resumeNearestAfterInterrupt = true;
    [SerializeField] private bool markInterruptedWhenDisabled = true;
    [SerializeField][Min(0f)] private float reachedDistance = 0.85f;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private float gizmoRadius = 0.22f;

    private int _currentIndex;
    private int _direction = 1;
    private bool _initialized;
    private bool _interrupted;
    private int _lastRandomIndex = -1;

    public bool HasPatrol => CountValidPoints() > 0;
    public bool IsReturningToPatrol => _interrupted;
    public int CurrentIndex => _currentIndex;

    private void OnEnable()
    {
        _initialized = false;
    }

    private void OnDisable()
    {
        if (markInterruptedWhenDisabled)
            _interrupted = true;
    }

    public bool TryGetCurrentPatrolPoint(Vector3 actorPosition, out Vector3 point, out float waitTime)
    {
        point = actorPosition;
        waitTime = GetWaitTime();

        int validCount = CountValidPoints();
        if (validCount <= 0)
            return false;

        EnsureInitialized(actorPosition);

        Transform target = GetPointAtSafe(_currentIndex);
        if (target == null)
        {
            int nearest = FindNearestValidPoint(actorPosition);
            if (nearest < 0)
                return false;

            _currentIndex = nearest;
            target = points[_currentIndex];
        }

        point = target.position;
        return true;
    }

    public void NotifyPatrolPointReached(Vector3 actorPosition)
    {
        if (!HasPatrol)
            return;

        if (!_initialized)
            EnsureInitialized(actorPosition);

        Advance(actorPosition);
        _interrupted = false;
    }

    public void NotifyPatrolInterrupted()
    {
        _interrupted = true;
    }

    public void NotifyPatrolResumed()
    {
        _interrupted = false;
    }

    public void ForceNearest(Vector3 actorPosition)
    {
        int nearest = FindNearestValidPoint(actorPosition);
        if (nearest >= 0)
        {
            _currentIndex = nearest;
            _initialized = true;
        }
    }

    private void EnsureInitialized(Vector3 actorPosition)
    {
        if (_initialized)
        {
            if (_interrupted && resumeNearestAfterInterrupt)
                ForceNearest(actorPosition);
            return;
        }

        switch (startMode)
        {
            case StartMode.RandomPoint:
                _currentIndex = GetRandomValidIndex(-1);
                break;

            case StartMode.FirstPoint:
                _currentIndex = FindFirstValidPoint();
                break;

            case StartMode.NearestPoint:
            default:
                _currentIndex = FindNearestValidPoint(actorPosition);
                break;
        }

        if (_currentIndex < 0)
            _currentIndex = 0;

        _direction = 1;
        _initialized = true;
    }

    private void Advance(Vector3 actorPosition)
    {
        int validCount = CountValidPoints();
        if (validCount <= 1)
            return;

        switch (patrolMode)
        {
            case PatrolMode.Random:
                _currentIndex = GetRandomValidIndex(_currentIndex);
                break;

            case PatrolMode.OnceThenHold:
                _currentIndex = FindNextValidIndex(_currentIndex, 1, false);
                break;

            case PatrolMode.PingPong:
                AdvancePingPong();
                break;

            case PatrolMode.Loop:
            default:
                _currentIndex = FindNextValidIndex(_currentIndex, 1, true);
                break;
        }

        if (_currentIndex < 0)
            ForceNearest(actorPosition);
    }

    private void AdvancePingPong()
    {
        int next = FindNextValidIndex(_currentIndex, _direction, false);
        if (next >= 0)
        {
            _currentIndex = next;
            return;
        }

        _direction *= -1;
        next = FindNextValidIndex(_currentIndex, _direction, false);
        if (next >= 0)
            _currentIndex = next;
    }

    private int FindNextValidIndex(int from, int dir, bool wrap)
    {
        if (points == null || points.Length == 0)
            return -1;

        int index = from;
        for (int i = 0; i < points.Length; i++)
        {
            index += dir;

            if (wrap)
            {
                if (index >= points.Length) index = 0;
                if (index < 0) index = points.Length - 1;
            }
            else
            {
                if (index < 0 || index >= points.Length)
                    return from;
            }

            if (IsPointValid(index))
                return index;
        }

        return from;
    }

    private int GetRandomValidIndex(int exclude)
    {
        int validCount = CountValidPoints();
        if (validCount <= 0)
            return -1;

        if (validCount == 1)
            return FindFirstValidPoint();

        for (int guard = 0; guard < 32; guard++)
        {
            int candidate = Random.Range(0, points.Length);
            if (!IsPointValid(candidate) || candidate == exclude || candidate == _lastRandomIndex)
                continue;

            _lastRandomIndex = candidate;
            return candidate;
        }

        for (int i = 0; i < points.Length; i++)
        {
            if (IsPointValid(i) && i != exclude)
            {
                _lastRandomIndex = i;
                return i;
            }
        }

        return FindFirstValidPoint();
    }

    private int FindFirstValidPoint()
    {
        if (points == null)
            return -1;

        for (int i = 0; i < points.Length; i++)
        {
            if (IsPointValid(i))
                return i;
        }

        return -1;
    }

    private int FindNearestValidPoint(Vector3 actorPosition)
    {
        if (points == null || points.Length == 0)
            return -1;

        int best = -1;
        float bestSqr = float.PositiveInfinity;

        for (int i = 0; i < points.Length; i++)
        {
            if (!IsPointValid(i))
                continue;

            float sqr = (points[i].position - actorPosition).sqrMagnitude;
            if (sqr < bestSqr)
            {
                best = i;
                bestSqr = sqr;
            }
        }

        return best;
    }

    private bool IsPointValid(int index)
    {
        if (points == null || index < 0 || index >= points.Length)
            return false;

        if (points[index] != null)
            return true;

        return !ignoreNullPoints;
    }

    private Transform GetPointAtSafe(int index)
    {
        if (points == null || index < 0 || index >= points.Length)
            return null;

        return points[index];
    }

    private int CountValidPoints()
    {
        if (points == null || points.Length == 0)
            return 0;

        if (!ignoreNullPoints)
            return points.Length;

        int count = 0;
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] != null)
                count++;
        }

        return count;
    }

    private float GetWaitTime()
    {
        if (!randomizeWaitTime)
            return waitTimeMin;

        float min = Mathf.Min(waitTimeMin, waitTimeMax);
        float max = Mathf.Max(waitTimeMin, waitTimeMax);
        return Random.Range(min, max);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || points == null)
            return;

        Gizmos.color = Color.green;
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null)
                continue;

            Gizmos.DrawWireSphere(points[i].position, gizmoRadius);

            int next = FindNextForGizmo(i);
            if (next >= 0 && next != i && next < points.Length && points[next] != null)
                Gizmos.DrawLine(points[i].position, points[next].position);
        }
    }

    private int FindNextForGizmo(int from)
    {
        if (points == null || points.Length <= 1)
            return -1;

        switch (patrolMode)
        {
            case PatrolMode.PingPong:
            case PatrolMode.OnceThenHold:
                return from + 1 < points.Length ? from + 1 : -1;

            case PatrolMode.Random:
                return -1;

            case PatrolMode.Loop:
            default:
                return FindNextValidIndex(from, 1, true);
        }
    }
}
