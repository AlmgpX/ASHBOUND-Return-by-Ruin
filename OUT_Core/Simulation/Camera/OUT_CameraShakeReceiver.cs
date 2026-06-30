using UnityEngine;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class OUT_CameraShakeReceiver : MonoBehaviour, IOutCameraShakeReceiver
{
    [Header("References")]
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private OUT_HL1PlayerController playerController;

    [Header("Filtering")]
    [SerializeField] private bool ignoreWhenDisabled = true;
    [SerializeField] private bool respectRequireGrounded = true;

    [Header("Response")]
    [SerializeField][Min(0f)] private float amplitudeMultiplier = 1f;
    [SerializeField][Min(0f)] private float rotationalAmplitude = 1.65f;
    [SerializeField][Min(0f)] private float positionalAmplitude = 0.045f;
    [SerializeField][Min(0f)] private float returnSpeed = 20f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Additive Motion")]
    [SerializeField] private bool preserveExternalMotion = true;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private Vector3 shakePosition;
    private Vector3 shakeEuler;
    private Vector3 lastAppliedShakePosition;
    private Vector3 lastAppliedShakeEuler;
    private float remaining;
    private float duration;
    private float amplitude;
    private float frequency;
    private float seed;

    private void Awake()
    {
        if (cameraRoot == null)
            cameraRoot = transform;

        if (playerController == null)
            playerController = GetComponentInParent<OUT_HL1PlayerController>();

        CaptureBasePose();
    }

    private void OnEnable()
    {
        CaptureBasePose();
        OUT_CameraShakeService.EnsureExists();
        if (OUT_CameraShakeService.Instance != null)
            OUT_CameraShakeService.Instance.RefreshReceivers();
    }

    private void OnDisable()
    {
        if (cameraRoot == null)
            return;

        RemoveLastAppliedShake(out Vector3 currentBasePosition, out Quaternion currentBaseRotation);
        cameraRoot.SetLocalPositionAndRotation(currentBasePosition, currentBaseRotation);
        lastAppliedShakePosition = Vector3.zero;
        lastAppliedShakeEuler = Vector3.zero;
    }

    private void LateUpdate()
    {
        if (cameraRoot == null)
            return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (dt <= 0f)
            return;

        RemoveLastAppliedShake(out Vector3 currentBasePosition, out Quaternion currentBaseRotation);

        if (preserveExternalMotion)
        {
            baseLocalPosition = currentBasePosition;
            baseLocalRotation = currentBaseRotation;
        }

        if (remaining > 0f)
        {
            remaining = Mathf.Max(0f, remaining - dt);
            float life = duration > 0f ? remaining / duration : 0f;
            float t = (useUnscaledTime ? Time.unscaledTime : Time.time) * Mathf.Max(0.01f, frequency) + seed;
            float fade = life * life;

            shakeEuler.x = (Mathf.PerlinNoise(seed + 0.11f, t) - 0.5f) * 2f * amplitude * rotationalAmplitude * fade;
            shakeEuler.y = (Mathf.PerlinNoise(seed + 7.31f, t) - 0.5f) * 2f * amplitude * rotationalAmplitude * 0.55f * fade;
            shakeEuler.z = (Mathf.PerlinNoise(seed + 13.73f, t) - 0.5f) * 2f * amplitude * rotationalAmplitude * 0.35f * fade;

            shakePosition.x = (Mathf.PerlinNoise(seed + 3.41f, t) - 0.5f) * 2f * amplitude * positionalAmplitude * 0.55f * fade;
            shakePosition.y = (Mathf.PerlinNoise(seed + 5.29f, t) - 0.5f) * 2f * amplitude * positionalAmplitude * fade;
            shakePosition.z = (Mathf.PerlinNoise(seed + 9.63f, t) - 0.5f) * 2f * amplitude * positionalAmplitude * 0.35f * fade;
        }
        else
        {
            shakeEuler = Vector3.Lerp(shakeEuler, Vector3.zero, 1f - Mathf.Exp(-returnSpeed * dt));
            shakePosition = Vector3.Lerp(shakePosition, Vector3.zero, 1f - Mathf.Exp(-returnSpeed * dt));
        }

        lastAppliedShakePosition = shakePosition;
        lastAppliedShakeEuler = shakeEuler;

        cameraRoot.localPosition = baseLocalPosition + lastAppliedShakePosition;
        cameraRoot.localRotation = baseLocalRotation * Quaternion.Euler(lastAppliedShakeEuler);
    }

    public bool CanReceiveShake(in OUT_CameraShakeRequest request)
    {
        if (ignoreWhenDisabled && (!isActiveAndEnabled || cameraRoot == null))
            return false;

        if (respectRequireGrounded && request.RequireGrounded && playerController != null && !playerController.IsGrounded)
            return false;

        return true;
    }

    public void ReceiveShake(in OUT_CameraShakeRequest request, float localAmplitude)
    {
        if (cameraRoot == null)
            return;

        float scaledAmplitude = Mathf.Max(0f, localAmplitude * amplitudeMultiplier);
        if (scaledAmplitude <= 0f)
            return;

        if (request.Duration >= remaining || scaledAmplitude >= amplitude)
        {
            amplitude = scaledAmplitude;
            frequency = Mathf.Max(0.01f, request.Frequency);
            duration = Mathf.Max(0.01f, request.Duration);
            remaining = duration;
            seed = Random.value * 1000f;
        }
        else
        {
            amplitude = Mathf.Max(amplitude, scaledAmplitude);
        }
    }

    public void CaptureBasePose()
    {
        if (cameraRoot == null)
            cameraRoot = transform;

        baseLocalPosition = cameraRoot.localPosition;
        baseLocalRotation = cameraRoot.localRotation;
        lastAppliedShakePosition = Vector3.zero;
        lastAppliedShakeEuler = Vector3.zero;
    }

    private void RemoveLastAppliedShake(out Vector3 currentBasePosition, out Quaternion currentBaseRotation)
    {
        currentBasePosition = cameraRoot.localPosition - lastAppliedShakePosition;
        Quaternion inverseShake = Quaternion.Inverse(Quaternion.Euler(lastAppliedShakeEuler));
        currentBaseRotation = cameraRoot.localRotation * inverseShake;
    }
}
