using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(OUT_HL1PlayerController))]
public class OUT_HL1PlayerMovementShake : MonoBehaviour
{
    [SerializeField] private OUT_HL1PlayerController playerController;
    [SerializeField] private bool autoCreateReceiverOnMainCamera = true;

    [Header("Footsteps")]
    [SerializeField] private bool enableFootstepShake = true;
    [SerializeField][Min(0f)] private float minSpeed = 0.8f;
    [SerializeField][Min(0.05f)] private float walkInterval = 0.42f;
    [SerializeField][Min(0.05f)] private float runInterval = 0.31f;
    [SerializeField][Min(0.05f)] private float crouchInterval = 0.58f;
    [SerializeField][Min(0f)] private float walkAmplitude = 0.025f;
    [SerializeField][Min(0f)] private float runAmplitude = 0.045f;
    [SerializeField][Min(0f)] private float crouchAmplitude = 0.014f;
    [SerializeField][Min(0.01f)] private float footstepFrequency = 16f;
    [SerializeField][Min(0.01f)] private float footstepDuration = 0.055f;

    [Header("Landing")]
    [SerializeField] private bool enableLandingShake = true;
    [SerializeField][Min(0f)] private float minLandingSpeed = 4.5f;
    [SerializeField][Min(0f)] private float landingAmplitudeScale = 0.018f;
    [SerializeField][Min(0f)] private float maxLandingAmplitude = 0.35f;
    [SerializeField][Min(0.01f)] private float landingFrequency = 13f;
    [SerializeField][Min(0.01f)] private float landingDuration = 0.18f;

    private float stepTimer;
    private bool wasGrounded;
    private float lastVerticalVelocity;

    private void Reset()
    {
        playerController = GetComponent<OUT_HL1PlayerController>();
    }

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<OUT_HL1PlayerController>();

        EnsureReceiver();
        OUT_CameraShakeService.EnsureExists();
    }

    private void OnEnable()
    {
        wasGrounded = playerController != null && playerController.IsGrounded;
        lastVerticalVelocity = 0f;
    }

    private void Update()
    {
        if (playerController == null)
            return;

        Vector3 v = playerController.Velocity;
        Vector3 horizontal = v;
        horizontal.y = 0f;
        float speed = horizontal.magnitude;
        bool grounded = playerController.IsGrounded;

        if (enableFootstepShake)
            UpdateFootstepShake(speed, grounded);

        if (enableLandingShake && !wasGrounded && grounded)
            EmitLandingShake(Mathf.Abs(lastVerticalVelocity));

        if (!grounded)
            lastVerticalVelocity = v.y;

        wasGrounded = grounded;
    }

    private void UpdateFootstepShake(float speed, bool grounded)
    {
        if (!grounded || playerController.IsOnLadder || speed < minSpeed)
        {
            stepTimer = 0f;
            return;
        }

        float interval = walkInterval;
        float amplitude = walkAmplitude;

        if (playerController.IsCrouching)
        {
            interval = crouchInterval;
            amplitude = crouchAmplitude;
        }
        else if (speed > 6.5f)
        {
            interval = runInterval;
            amplitude = runAmplitude;
        }

        stepTimer += Time.deltaTime;
        if (stepTimer < interval)
            return;

        stepTimer = 0f;
        OUT_CameraShakeService.Shake(new OUT_CameraShakeRequest(gameObject, transform.position, amplitude, footstepFrequency, footstepDuration, 0f, OUT_CameraShakeKind.Footstep, false));
    }

    private void EmitLandingShake(float landingSpeed)
    {
        if (landingSpeed < minLandingSpeed)
            return;

        float amplitude = Mathf.Min(maxLandingAmplitude, landingSpeed * landingAmplitudeScale);
        OUT_CameraShakeService.Shake(new OUT_CameraShakeRequest(gameObject, transform.position, amplitude, landingFrequency, landingDuration, 0f, OUT_CameraShakeKind.Landing, false));
    }

    private void EnsureReceiver()
    {
        if (!autoCreateReceiverOnMainCamera || Camera.main == null)
            return;

        if (Camera.main.GetComponent<OUT_CameraShakeReceiver>() == null)
            Camera.main.gameObject.AddComponent<OUT_CameraShakeReceiver>();
    }
}
