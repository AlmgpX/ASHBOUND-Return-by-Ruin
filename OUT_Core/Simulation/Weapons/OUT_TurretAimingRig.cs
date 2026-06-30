using UnityEngine;

[DisallowMultipleComponent]
public class OUT_TurretAimingRig : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform yawPivot;
    [SerializeField] private Transform pitchPivot;
    [SerializeField] private Transform fireOrigin;

    [Header("Yaw Limits")]
    [SerializeField] private float minYaw = -90f;
    [SerializeField] private float maxYaw = 90f;
    [SerializeField][Min(1f)] private float yawSpeed = 180f;

    [Header("Pitch Limits")]
    [SerializeField] private float minPitch = -10f;
    [SerializeField] private float maxPitch = 35f;
    [SerializeField][Min(1f)] private float pitchSpeed = 120f;

    [Header("Firing")]
    [SerializeField][Min(0.1f)] private float fireAlignmentTolerance = 4f;
    [SerializeField] private bool updateContinuously = true;

    private Vector3 _desiredAimPoint;
    private bool _hasDesiredAimPoint;

    private void Awake()
    {
        if (yawPivot == null)
            yawPivot = transform;

        if (pitchPivot == null)
            pitchPivot = transform;

        if (fireOrigin == null)
            fireOrigin = pitchPivot != null ? pitchPivot : transform;
    }

    private void Update()
    {
        if (!updateContinuously || !_hasDesiredAimPoint)
            return;

        UpdateAimTowards(_desiredAimPoint, Time.deltaTime);
    }

    public void SetDesiredAimPoint(Vector3 worldPoint)
    {
        _desiredAimPoint = worldPoint;
        _hasDesiredAimPoint = true;

        if (!updateContinuously)
            UpdateAimTowards(_desiredAimPoint, Time.deltaTime);
    }

    public void ClearDesiredAimPoint()
    {
        _hasDesiredAimPoint = false;
    }

    public Transform GetFireOrigin()
    {
        return fireOrigin != null ? fireOrigin : transform;
    }

    public Vector3 GetFireDirection()
    {
        Transform origin = GetFireOrigin();
        return origin != null ? origin.forward : transform.forward;
    }

    public bool IsAligned(Vector3 worldPoint)
    {
        Transform origin = GetFireOrigin();
        if (origin == null)
            return false;

        Vector3 desired = worldPoint - origin.position;
        desired.y = desired.y;
        if (desired.sqrMagnitude <= 0.0001f)
            return true;

        float angle = Vector3.Angle(origin.forward, desired.normalized);
        return angle <= fireAlignmentTolerance;
    }

    public void UpdateAimTowards(Vector3 worldPoint, float deltaTime)
    {
        if (yawPivot == null || pitchPivot == null)
            return;

        UpdateYaw(worldPoint, deltaTime);
        UpdatePitch(worldPoint, deltaTime);
    }

    private void UpdateYaw(Vector3 worldPoint, float deltaTime)
    {
        Transform reference = yawPivot.parent != null ? yawPivot.parent : yawPivot;
        Vector3 worldDir = worldPoint - yawPivot.position;
        if (worldDir.sqrMagnitude <= 0.0001f)
            return;

        Vector3 localDir = reference.InverseTransformDirection(worldDir.normalized);
        float desiredYaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
        desiredYaw = Mathf.Clamp(desiredYaw, minYaw, maxYaw);

        Vector3 localEuler = yawPivot.localEulerAngles;
        float currentYaw = NormalizeAngle(localEuler.y);
        float nextYaw = Mathf.MoveTowardsAngle(currentYaw, desiredYaw, yawSpeed * Mathf.Max(0f, deltaTime));

        yawPivot.localRotation = Quaternion.Euler(localEuler.x, nextYaw, localEuler.z);
    }

    private void UpdatePitch(Vector3 worldPoint, float deltaTime)
    {
        Transform reference = pitchPivot.parent != null ? pitchPivot.parent : pitchPivot;
        Vector3 worldDir = worldPoint - pitchPivot.position;
        if (worldDir.sqrMagnitude <= 0.0001f)
            return;

        Vector3 localDir = reference.InverseTransformDirection(worldDir.normalized);

        float planar = new Vector2(localDir.x, localDir.z).magnitude;
        float desiredPitch = -Mathf.Atan2(localDir.y, Mathf.Max(0.0001f, planar)) * Mathf.Rad2Deg;
        desiredPitch = Mathf.Clamp(desiredPitch, minPitch, maxPitch);

        Vector3 localEuler = pitchPivot.localEulerAngles;
        float currentPitch = NormalizeAngle(localEuler.x);
        float nextPitch = Mathf.MoveTowardsAngle(currentPitch, desiredPitch, pitchSpeed * Mathf.Max(0f, deltaTime));

        pitchPivot.localRotation = Quaternion.Euler(nextPitch, localEuler.y, localEuler.z);
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        if (angle < -180f) angle += 360f;
        return angle;
    }
}