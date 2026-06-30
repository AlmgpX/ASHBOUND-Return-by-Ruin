using UnityEngine;
using Time = OUT_SimTime;

[DisallowMultipleComponent]
[RequireComponent(typeof(OUT_SceneStimulusEmitter))]
public class OUT_PlayerMovementStimulusDriver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private OUT_HL1PlayerController controller;
    [SerializeField] private OUT_SceneStimulusEmitter emitter;

    [Header("Noise")]
    [SerializeField][Min(0f)] private float idleRadius = 0.5f;
    [SerializeField][Range(0f, 1f)] private float idleIntensity = 0.02f;
    [SerializeField][Min(0f)] private float walkRadius = 5f;
    [SerializeField][Range(0f, 1f)] private float walkIntensity = 0.35f;
    [SerializeField][Min(0f)] private float runRadius = 9f;
    [SerializeField][Range(0f, 1f)] private float runIntensity = 0.7f;
    [SerializeField][Min(0f)] private float crouchRadius = 2.5f;
    [SerializeField][Range(0f, 1f)] private float crouchIntensity = 0.15f;
    [SerializeField][Min(0f)] private float airRadius = 7f;
    [SerializeField][Range(0f, 1f)] private float airIntensity = 0.45f;
    [SerializeField][Min(0f)] private float jumpPulseRadius = 14f;
    [SerializeField][Range(0f, 1f)] private float jumpPulseIntensity = 1f;
    [SerializeField][Min(0f)] private float landingPulseRadius = 16f;
    [SerializeField][Range(0f, 1f)] private float landingPulseIntensity = 1f;
    [SerializeField][Min(0f)] private float pulseDuration = 0.28f;
    [SerializeField][Min(0f)] private float runSpeedThreshold = 4.5f;
    [SerializeField][Min(0f)] private float movingSpeedThreshold = 0.2f;
    [SerializeField] private bool useDangerOnLanding = true;

    [Header("Runtime")]
    [SerializeField] private float currentRadius;
    [SerializeField] private float currentIntensity;
    [SerializeField] private bool wasGrounded;
    [SerializeField] private bool wasCrouching;
    [SerializeField] private float pulseUntilTime;
    [SerializeField] private float pulseRadius;
    [SerializeField] private float pulseIntensity;

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<OUT_HL1PlayerController>();

        if (emitter == null)
            emitter = GetComponent<OUT_SceneStimulusEmitter>();
    }

    private void OnEnable()
    {
        wasGrounded = controller == null || controller.IsGrounded;
        wasCrouching = controller != null && controller.IsCrouching;
        ApplyToEmitter(idleRadius, idleIntensity, OUT_SensoryChannelFlags.Noise);
    }

    private void Update()
    {
        if (controller == null || emitter == null)
            return;

        float speed = new Vector3(controller.Velocity.x, 0f, controller.Velocity.z).magnitude;
        bool grounded = controller.IsGrounded;
        bool crouching = controller.IsCrouching;

        if (wasGrounded && !grounded)
            StartPulse(jumpPulseRadius, jumpPulseIntensity);

        if (!wasGrounded && grounded)
            StartPulse(landingPulseRadius, landingPulseIntensity);

        float radius;
        float intensity;

        if (Time.time < pulseUntilTime)
        {
            radius = pulseRadius;
            intensity = pulseIntensity;
        }
        else if (!grounded)
        {
            radius = airRadius;
            intensity = airIntensity;
        }
        else if (crouching && speed > movingSpeedThreshold)
        {
            radius = crouchRadius;
            intensity = crouchIntensity;
        }
        else if (speed >= runSpeedThreshold)
        {
            radius = runRadius;
            intensity = runIntensity;
        }
        else if (speed > movingSpeedThreshold)
        {
            radius = walkRadius;
            intensity = walkIntensity;
        }
        else
        {
            radius = idleRadius;
            intensity = idleIntensity;
        }

        OUT_SensoryChannelFlags channels = OUT_SensoryChannelFlags.Noise;
        if (useDangerOnLanding && Time.time < pulseUntilTime)
            channels |= OUT_SensoryChannelFlags.Danger;

        ApplyToEmitter(radius, intensity, channels);

        wasGrounded = grounded;
        wasCrouching = crouching;
    }

    private void StartPulse(float radius, float intensity)
    {
        pulseRadius = radius;
        pulseIntensity = intensity;
        pulseUntilTime = Time.time + pulseDuration;
    }

    private void ApplyToEmitter(float radius, float intensity, OUT_SensoryChannelFlags channels)
    {
        currentRadius = radius;
        currentIntensity = intensity;
        emitter.SetRuntimeStimulus(channels, radius, intensity);
    }
}
