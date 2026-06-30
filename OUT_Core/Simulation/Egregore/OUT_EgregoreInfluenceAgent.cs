using UnityEngine;

[DisallowMultipleComponent]
public class OUT_EgregoreInfluenceAgent : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private OUT_AIEntityMemory entityMemory;
    [SerializeField] private OUT_AIMemoryBuffer aiMemoryBuffer;

    [Header("Influence")]
    [SerializeField] private bool receiveEgregoreInfluence = true;
    [SerializeField][Min(0.1f)] private float updateInterval = 4f;
    [SerializeField][Range(0f, 1f)] private float influenceIntensityScale = 0.35f;
    [SerializeField][Range(0f, 1f)] private float minimumInfluence = 0.05f;

    [Header("Debug")]
    [SerializeField] private bool logInfluence;
    [SerializeField] private string currentEgregore;
    [SerializeField] private OUT_EgregoreDominantForce currentForce;
    [SerializeField][Range(0f, 1f)] private float currentInfluence;

    private float nextUpdateTime;

    private void Reset()
    {
        entityMemory = GetComponent<OUT_AIEntityMemory>();
        aiMemoryBuffer = GetComponent<OUT_AIMemoryBuffer>();
    }

    private void Awake()
    {
        if (entityMemory == null)
            entityMemory = GetComponent<OUT_AIEntityMemory>();
        if (aiMemoryBuffer == null)
            aiMemoryBuffer = GetComponent<OUT_AIMemoryBuffer>();
    }

    private void OnEnable()
    {
        nextUpdateTime = Time.time + Random.Range(0f, updateInterval);
    }

    private void Update()
    {
        if (!receiveEgregoreInfluence)
            return;

        if (Time.time < nextUpdateTime)
            return;

        nextUpdateTime = Time.time + updateInterval;
        ApplyInfluence();
    }

    private void ApplyInfluence()
    {
        OUT_EgregoreRegistry registry = OUT_EgregoreRegistry.EnsureExists();
        OUT_EgregoreZone zone = registry.FindStrongestZone(transform.position, out float influence);
        currentInfluence = influence;
        currentEgregore = zone != null ? zone.EgregoreId : string.Empty;
        currentForce = zone != null ? zone.DominantForce : OUT_EgregoreDominantForce.Neutral;

        if (zone == null || influence < minimumInfluence)
            return;

        OUT_SignalChannelFlags channels = ConvertForceToSignal(currentForce);
        if (channels == OUT_SignalChannelFlags.None)
            return;

        float finalIntensity = Mathf.Clamp01(GetForceStrength(zone.State, currentForce) * influence * influenceIntensityScale);
        if (finalIntensity < minimumInfluence)
            return;

        if (aiMemoryBuffer != null)
        {
            if ((channels & (OUT_SignalChannelFlags.Danger | OUT_SignalChannelFlags.Fear | OUT_SignalChannelFlags.Aversion | OUT_SignalChannelFlags.Aggression)) != 0)
                aiMemoryBuffer.ObserveDanger(transform.position, finalIntensity, (int)channels);
            else
                aiMemoryBuffer.ObserveInterest(transform.position, finalIntensity, (int)channels);
        }

        if (entityMemory != null)
        {
            entityMemory.RememberEvent(new OUT_MemoryEvent(
                OUT_MemoryEventKind.ReceivedSignal,
                channels,
                zone.gameObject,
                zone.gameObject,
                transform.position,
                Time.time,
                finalIntensity,
                0,
                "egregore influence: " + zone.EgregoreId + " / " + currentForce));
        }

        if (logInfluence)
        {
            OUT_AIDebugLogService.Log(this, OUT_AIDebugLogService.AIEventKind.Memory,
                "egregore influence " + zone.EgregoreId + " force:" + currentForce + " int:" + finalIntensity.ToString("0.00"));
        }
    }

    private static OUT_SignalChannelFlags ConvertForceToSignal(OUT_EgregoreDominantForce force)
    {
        switch (force)
        {
            case OUT_EgregoreDominantForce.Threat:
                return OUT_SignalChannelFlags.Danger | OUT_SignalChannelFlags.Suspicion;
            case OUT_EgregoreDominantForce.Fear:
                return OUT_SignalChannelFlags.Fear | OUT_SignalChannelFlags.Danger;
            case OUT_EgregoreDominantForce.Violence:
                return OUT_SignalChannelFlags.Aggression | OUT_SignalChannelFlags.Danger;
            case OUT_EgregoreDominantForce.Hunger:
                return OUT_SignalChannelFlags.Food | OUT_SignalChannelFlags.Curiosity;
            case OUT_EgregoreDominantForce.Greed:
                return OUT_SignalChannelFlags.Treasure | OUT_SignalChannelFlags.Reward;
            case OUT_EgregoreDominantForce.Desire:
                return OUT_SignalChannelFlags.Reward | OUT_SignalChannelFlags.Social;
            case OUT_EgregoreDominantForce.Sacred:
                return OUT_SignalChannelFlags.Sacred | OUT_SignalChannelFlags.Curiosity;
            case OUT_EgregoreDominantForce.Shelter:
                return OUT_SignalChannelFlags.Shelter;
            case OUT_EgregoreDominantForce.Corruption:
                return OUT_SignalChannelFlags.Aversion | OUT_SignalChannelFlags.Danger;
            case OUT_EgregoreDominantForce.Social:
                return OUT_SignalChannelFlags.Social | OUT_SignalChannelFlags.Command;
            default:
                return OUT_SignalChannelFlags.None;
        }
    }

    private static float GetForceStrength(OUT_EgregoreState state, OUT_EgregoreDominantForce force)
    {
        switch (force)
        {
            case OUT_EgregoreDominantForce.Threat: return state.Threat;
            case OUT_EgregoreDominantForce.Fear: return state.Fear;
            case OUT_EgregoreDominantForce.Violence: return state.Violence;
            case OUT_EgregoreDominantForce.Hunger: return state.Hunger;
            case OUT_EgregoreDominantForce.Greed: return state.Greed;
            case OUT_EgregoreDominantForce.Desire: return state.Desire;
            case OUT_EgregoreDominantForce.Sacred: return state.Sacred;
            case OUT_EgregoreDominantForce.Shelter: return state.Shelter;
            case OUT_EgregoreDominantForce.Corruption: return state.Corruption;
            case OUT_EgregoreDominantForce.Social: return state.Social;
            default: return 0f;
        }
    }
}
