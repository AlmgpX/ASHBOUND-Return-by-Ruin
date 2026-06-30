using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class OUT_AILocomotion_CharacterController : MonoBehaviour, IOutAILocomotion, IOutAIStuckAwareLocomotion
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.75f;
    [SerializeField] private float acceleration = 18f;
    [SerializeField] private float rotationSpeed = 720f;

    [Header("Crowd / Separation")]
    [SerializeField] private bool useSeparation = true;
    [SerializeField] private LayerMask separationMask = ~0;
    [SerializeField] [Min(0.1f)] private float separationRadius = 0.9f;
    [SerializeField] [Min(0f)] private float separationStrength = 1.25f;
    [SerializeField] [Min(0f)] private float separationPushStrength = 0.12f;
    [SerializeField] [Range(0.02f, 1f)] private float separationThinkInterval = 0.15f;
    [SerializeField] [Range(0.02f, 1f)] private float immediateSeparationInterval = 0.10f;
    [SerializeField] private bool randomizeSeparationOffset = true;

    [Header("Stuck Detection")]
    [SerializeField] [Min(0.05f)] private float stuckCheckInterval = 0.35f;
    [SerializeField] [Min(0f)] private float stuckMinProgress = 0.08f;
    [SerializeField] [Min(1)] private int stuckThresholdTicks = 2;

    [Header("Vertical")]
    [SerializeField] private float gravity = -24f;
    [SerializeField] private float groundedStickForce = -2f;

    private CharacterController _controller;
    private Transform _rootTransform;
    private Vector3 _planarVelocity;
    private float _verticalVelocity;

    private bool _isMoving;
    private Vector3 _currentDestination;
    private float _acceptanceRadius = 0.5f;

    private bool _hasFacePoint;
    private Vector3 _facePoint;

    private Vector3 _lastStuckCheckPosition;
    private float _nextStuckCheckTime;
    private int _stuckTicks;
    private bool _isStuck;

    private Vector3 _cachedSeparation;
    private float _nextSeparationThinkTime;
    private float _nextImmediateSeparationTime;

    private readonly Collider[] _separationBuffer = new Collider[24];

    public bool IsMoving => _isMoving;
    public Vector3 CurrentDestination => _currentDestination;
    public Vector3 Velocity => new Vector3(_planarVelocity.x, _verticalVelocity, _planarVelocity.z);
    public bool IsStuck => _isStuck;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _rootTransform = transform.root;
    }

    private void OnEnable()
    {
        float now = Time.time;
        if (randomizeSeparationOffset)
        {
            _nextSeparationThinkTime = now + Random.Range(0f, Mathf.Max(0.02f, separationThinkInterval));
            _nextImmediateSeparationTime = now + Random.Range(0f, Mathf.Max(0.02f, immediateSeparationInterval));
        }
        else
        {
            _nextSeparationThinkTime = now;
            _nextImmediateSeparationTime = now;
        }
    }

    private void OnDisable()
    {
        ResetStuckState();
        _isMoving = false;
        _planarVelocity = Vector3.zero;
        _cachedSeparation = Vector3.zero;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        Vector3 desiredPlanarVelocity = Vector3.zero;

        if (_isMoving)
        {
            Vector3 toDestination = _currentDestination - transform.position;
            toDestination.y = 0f;

            float sqrDistance = toDestination.sqrMagnitude;
            if (sqrDistance <= _acceptanceRadius * _acceptanceRadius)
            {
                _isMoving = false;
                ResetStuckState();
            }
            else
            {
                Vector3 direction = toDestination.normalized;

                if (useSeparation)
                {
                    Vector3 separation = GetCachedSeparation();
                    if (separation.sqrMagnitude > 0.0001f)
                    {
                        direction += separation * separationStrength;
                        direction.y = 0f;
                        if (direction.sqrMagnitude > 0.0001f)
                            direction.Normalize();
                    }
                }

                desiredPlanarVelocity = direction * moveSpeed;

                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * dt);
            }
        }
        else if (_hasFacePoint)
        {
            Vector3 faceDirection = _facePoint - transform.position;
            faceDirection.y = 0f;

            if (faceDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(faceDirection.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * dt);
            }
        }

        _planarVelocity = Vector3.MoveTowards(_planarVelocity, desiredPlanarVelocity, acceleration * dt);

        if (_controller.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = groundedStickForce;
        else
            _verticalVelocity += gravity * dt;

        Vector3 motion = _planarVelocity;
        motion.y = _verticalVelocity;

        _controller.Move(motion * dt);
        ApplyImmediateSeparationThrottled();
        UpdateStuckState();
    }

    public bool TryMoveTo(Vector3 destination, float acceptanceRadius = 0.5f)
    {
        bool destinationChanged = (_currentDestination - destination).sqrMagnitude > 0.0001f;
        bool radiusChanged = Mathf.Abs(_acceptanceRadius - acceptanceRadius) > 0.001f;

        _currentDestination = destination;
        _acceptanceRadius = Mathf.Max(0.05f, acceptanceRadius);

        if (!_isMoving || destinationChanged || radiusChanged)
            ResetStuckTrackingTimer();

        _hasFacePoint = false;
        _isMoving = true;
        return true;
    }

    public void Stop()
    {
        _isMoving = false;
        _planarVelocity = Vector3.zero;
        ResetStuckState();
    }

    public void Face(Vector3 point)
    {
        _facePoint = point;
        _hasFacePoint = true;
    }

    public void ClearFace()
    {
        _hasFacePoint = false;
    }

    public bool HasReachedDestination(float acceptanceRadius = 0.5f)
    {
        Vector3 toDestination = _currentDestination - transform.position;
        toDestination.y = 0f;
        float radius = Mathf.Max(0.05f, acceptanceRadius);
        return toDestination.sqrMagnitude <= radius * radius;
    }

    private void UpdateStuckState()
    {
        if (!_isMoving)
        {
            ResetStuckState();
            return;
        }

        if (Time.time < _nextStuckCheckTime)
            return;

        Vector3 currentPlanar = GetPlanar(transform.position);
        float moved = Vector3.Distance(currentPlanar, GetPlanar(_lastStuckCheckPosition));

        if (moved < stuckMinProgress)
            _stuckTicks++;
        else
            _stuckTicks = 0;

        _isStuck = _stuckTicks >= Mathf.Max(1, stuckThresholdTicks);
        _lastStuckCheckPosition = transform.position;
        _nextStuckCheckTime = Time.time + Mathf.Max(0.05f, stuckCheckInterval);

        if (_isStuck)
        {
            _isMoving = false;
            _planarVelocity = Vector3.zero;
        }
    }

    private void ResetStuckTrackingTimer()
    {
        ResetStuckState();
        _lastStuckCheckPosition = transform.position;
        _nextStuckCheckTime = Time.time + Mathf.Max(0.05f, stuckCheckInterval);
    }

    private void ResetStuckState()
    {
        _isStuck = false;
        _stuckTicks = 0;
        _lastStuckCheckPosition = transform.position;
        _nextStuckCheckTime = Time.time + Mathf.Max(0.05f, stuckCheckInterval);
    }

    private Vector3 GetCachedSeparation()
    {
        if (Time.time < _nextSeparationThinkTime)
            return _cachedSeparation;

        _cachedSeparation = ComputeSeparation();
        _nextSeparationThinkTime = Time.time + Mathf.Max(0.02f, separationThinkInterval);
        return _cachedSeparation;
    }

    private Vector3 ComputeSeparation()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            separationRadius,
            _separationBuffer,
            separationMask,
            QueryTriggerInteraction.Ignore);

        if (count <= 0)
            return Vector3.zero;

        Vector3 result = Vector3.zero;
        Transform ownRoot = _rootTransform != null ? _rootTransform : transform.root;

        for (int i = 0; i < count; i++)
        {
            Collider col = _separationBuffer[i];
            if (col == null)
                continue;

            Transform otherRoot = col.transform.root;
            if (otherRoot == ownRoot)
                continue;

            if (otherRoot.GetComponentInChildren<OUT_AIActorBrain>() == null &&
                otherRoot.GetComponentInChildren<OUT_AICrowdAgent>() == null)
                continue;

            Vector3 away = transform.position - col.bounds.center;
            away.y = 0f;

            float sqr = away.sqrMagnitude;
            float weight;

            if (sqr < 0.0001f)
            {
                away = GetFallbackSeparationDirection(otherRoot);
                weight = 1f;
            }
            else
            {
                float distance = Mathf.Sqrt(sqr);
                weight = 1f - Mathf.Clamp01(distance / separationRadius);
                away /= distance;
            }

            result += away * weight;
        }

        if (result.sqrMagnitude <= 0.0001f)
            return Vector3.zero;

        return Vector3.ClampMagnitude(result, 1f);
    }

    private void ApplyImmediateSeparationThrottled()
    {
        if (Time.time < _nextImmediateSeparationTime)
            return;

        _nextImmediateSeparationTime = Time.time + Mathf.Max(0.02f, immediateSeparationInterval);
        ApplyImmediateSeparation();
    }

    private void ApplyImmediateSeparation()
    {
        if (!useSeparation || separationRadius <= 0.01f || separationPushStrength <= 0f)
            return;

        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            separationRadius,
            _separationBuffer,
            separationMask,
            QueryTriggerInteraction.Ignore);

        if (count <= 0)
            return;

        Vector3 push = Vector3.zero;
        Transform ownRoot = _rootTransform != null ? _rootTransform : transform.root;
        float desiredDistance = Mathf.Max(0.05f, separationRadius * 0.85f);

        for (int i = 0; i < count; i++)
        {
            Collider col = _separationBuffer[i];
            if (col == null)
                continue;

            Transform otherRoot = col.transform.root;
            if (otherRoot == ownRoot)
                continue;

            if (otherRoot.GetComponentInChildren<OUT_AIActorBrain>() == null &&
                otherRoot.GetComponentInChildren<OUT_AICrowdAgent>() == null)
                continue;

            Vector3 away = transform.position - col.bounds.center;
            away.y = 0f;
            float distance = away.magnitude;

            if (distance <= 0.0001f)
            {
                away = GetFallbackSeparationDirection(otherRoot);
                distance = 0f;
            }
            else
            {
                away /= distance;
            }

            float weight = 1f - Mathf.Clamp01(distance / desiredDistance);
            if (weight <= 0f)
                continue;

            push += away * weight;
        }

        if (push.sqrMagnitude <= 0.0001f)
            return;

        Vector3 planarPush = Vector3.ClampMagnitude(push, 1f) * separationPushStrength;
        _controller.Move(planarPush);
    }

    private Vector3 GetFallbackSeparationDirection(Transform otherRoot)
    {
        Transform ownRoot = _rootTransform != null ? _rootTransform : transform.root;
        int selfKey = ownRoot.GetInstanceID();
        int otherKey = otherRoot != null ? otherRoot.GetInstanceID() : 0;
        int minKey = Mathf.Min(selfKey, otherKey);
        int maxKey = Mathf.Max(selfKey, otherKey);
        int hash = minKey ^ (maxKey * 73856093);

        unchecked
        {
            hash = (hash ^ 61) ^ (hash >> 16);
            hash *= 9;
            hash = hash ^ (hash >> 4);
            hash *= 0x27d4eb2d;
            hash = hash ^ (hash >> 15);
        }

        float angle = ((uint)hash & 1023) / 1024f * Mathf.PI * 2f;
        Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        return selfKey >= otherKey ? direction : -direction;
    }

    private Vector3 GetPlanar(Vector3 value)
    {
        value.y = 0f;
        return value;
    }
}
