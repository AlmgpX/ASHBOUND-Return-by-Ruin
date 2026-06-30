using UnityEngine;
using Time = OUT_SimTime;

[DisallowMultipleComponent]
public class OUT_SceneStimulusEmitter : MonoBehaviour
{
    [Header("Stimulus")]
    [SerializeField]
    private OUT_SensoryChannelFlags channels =
        OUT_SensoryChannelFlags.Noise;

    [SerializeField][Min(0.1f)] private float radius = 5f;
    [SerializeField][Range(0f, 1f)] private float intensity = 1f;
    [SerializeField] private Vector3 localOffset = Vector3.zero;

    [Header("Lifetime")]
    [SerializeField] private bool registerOnEnable = true;
    [SerializeField] private float lifetime = -1f;
    [SerializeField] private bool useUnscaledTime = false;

    [Header("Debug")]
    [SerializeField] private bool drawGizmo = true;

    private float _spawnTime;
    private bool _registered;

    public OUT_SensoryChannelFlags Channels => channels;
    public float Radius => radius;
    public float Intensity => intensity;
    public Vector3 WorldPosition => transform.TransformPoint(localOffset);

    private void OnEnable()
    {
        _spawnTime = useUnscaledTime ? UnityEngine.Time.unscaledTime : Time.time;

        if (registerOnEnable)
            Register();
    }

    private void Start()
    {
        if (registerOnEnable)
            Register();
    }

    private void OnDisable()
    {
        Unregister();
    }

    private void Update()
    {
        if (registerOnEnable && !_registered && OUT_SceneStimulusService.Instance != null)
            Register();

        if (lifetime <= 0f)
            return;

        float now = useUnscaledTime ? UnityEngine.Time.unscaledTime : Time.time;
        if (now - _spawnTime >= lifetime)
        {
            Unregister();
            enabled = false;
        }
    }

    public void Register()
    {
        if (_registered)
            return;

        if (OUT_SceneStimulusService.Instance != null)
        {
            OUT_SceneStimulusService.Instance.Register(this);
            _registered = true;
        }
    }

    public void Unregister()
    {
        if (!_registered)
            return;

        if (OUT_SceneStimulusService.Instance != null)
            OUT_SceneStimulusService.Instance.Unregister(this);

        _registered = false;
    }

    public void SetRuntimeStimulus(OUT_SensoryChannelFlags nextChannels, float nextRadius, float nextIntensity)
    {
        channels = nextChannels;
        radius = Mathf.Max(0.1f, nextRadius);
        intensity = Mathf.Clamp01(nextIntensity);

        if (registerOnEnable && !_registered)
            Register();
    }

    public bool Matches(OUT_SensoryChannelFlags requiredChannels)
    {
        return (channels & requiredChannels) != 0;
    }

    public float EvaluateStrength(Vector3 worldPosition, OUT_SensoryChannelFlags requiredChannels)
    {
        if (!Matches(requiredChannels))
            return 0f;

        if (radius <= 0.001f || intensity <= 0f)
            return 0f;

        float distance = Vector3.Distance(worldPosition, WorldPosition);
        if (distance > radius)
            return 0f;

        float attenuation = 1f - (distance / radius);
        return Mathf.Clamp01(intensity * attenuation);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmo)
            return;

        Color color = GetDebugColor();
        color.a = 0.25f;

        Gizmos.color = color;
        Gizmos.DrawWireSphere(WorldPosition, radius);
    }

    private Color GetDebugColor()
    {
        if ((channels & OUT_SensoryChannelFlags.Fire) != 0)
            return Color.red;

        if ((channels & OUT_SensoryChannelFlags.Food) != 0)
            return Color.green;

        if ((channels & OUT_SensoryChannelFlags.Danger) != 0)
            return new Color(1f, 0.45f, 0f);

        if ((channels & OUT_SensoryChannelFlags.Noise) != 0)
            return Color.cyan;

        if ((channels & OUT_SensoryChannelFlags.Luminance) != 0)
            return Color.yellow;

        return Color.white;
    }
}
