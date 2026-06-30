using UnityEngine;
using Time = OUT_SimTime;

[DisallowMultipleComponent]
public class OUT_SquadPatrolAnchor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private OUT_SoldierSquadCommander commander;
    [SerializeField] private Transform anchorTransform;

    [Header("Patrol Points")]
    [SerializeField] private Transform[] points;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool startAtNearest = true;

    [Header("Motion")]
    [SerializeField][Min(0.05f)] private float moveSpeed = 2.25f;
    [SerializeField][Min(0.05f)] private float reachDistance = 0.85f;
    [SerializeField][Min(0f)] private float waitAtPoint = 0.35f;
    [SerializeField] private bool rotateAlongPath = true;
    [SerializeField][Min(1f)] private float rotationSpeed = 360f;

    [Header("Runtime")]
    [SerializeField] private bool paused;

    private int currentIndex;
    private bool initialized;
    private float waitUntilTime;

    public Transform AnchorTransform => anchorTransform != null ? anchorTransform : transform;
    public bool HasPoints => points != null && points.Length > 0;
    public Vector3 AnchorPosition => AnchorTransform.position;

    private void Awake()
    {
        if (commander == null)
            commander = GetComponent<OUT_SoldierSquadCommander>();

        if (anchorTransform == null)
            anchorTransform = transform;

        if (commander != null)
            commander.SetSquadAnchor(AnchorTransform);
    }

    private void OnEnable()
    {
        initialized = false;
        if (commander != null)
            commander.SetSquadAnchor(AnchorTransform);
    }

    private void Update()
    {
        if (paused || !HasPoints)
            return;

        EnsureInitialized();

        if (Time.time < waitUntilTime)
            return;

        Transform target = GetCurrentPoint();
        if (target == null)
        {
            Advance();
            return;
        }

        Transform anchor = AnchorTransform;
        Vector3 toTarget = target.position - anchor.position;
        toTarget.y = 0f;

        if (toTarget.magnitude <= reachDistance)
        {
            Advance();
            waitUntilTime = Time.time + waitAtPoint;
            return;
        }

        Vector3 direction = toTarget.normalized;
        anchor.position += direction * moveSpeed * Time.deltaTime;

        if (rotateAlongPath && direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            anchor.rotation = Quaternion.RotateTowards(anchor.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;
        currentIndex = startAtNearest ? FindNearestPointIndex(AnchorTransform.position) : 0;
        if (currentIndex < 0)
            currentIndex = 0;
    }

    private Transform GetCurrentPoint()
    {
        if (points == null || points.Length == 0)
            return null;

        currentIndex = Mathf.Clamp(currentIndex, 0, points.Length - 1);
        return points[currentIndex];
    }

    private void Advance()
    {
        if (points == null || points.Length == 0)
            return;

        currentIndex++;
        if (currentIndex >= points.Length)
            currentIndex = loop ? 0 : points.Length - 1;
    }

    private int FindNearestPointIndex(Vector3 position)
    {
        if (points == null || points.Length == 0)
            return -1;

        int best = -1;
        float bestSqr = float.PositiveInfinity;
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null)
                continue;

            float sqr = (points[i].position - position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                best = i;
                bestSqr = sqr;
            }
        }

        return best;
    }

    private void OnDrawGizmosSelected()
    {
        if (points == null)
            return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null)
                continue;

            Gizmos.DrawWireSphere(points[i].position, 0.3f);
            int next = i + 1;
            if (next >= points.Length)
                next = loop ? 0 : -1;
            if (next >= 0 && next < points.Length && points[next] != null)
                Gizmos.DrawLine(points[i].position, points[next].position);
        }
    }
}
