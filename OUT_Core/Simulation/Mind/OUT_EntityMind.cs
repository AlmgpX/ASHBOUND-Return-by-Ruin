using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(OUT_AIEntityMemory))]
public class OUT_EntityMind : MonoBehaviour, IOutSignalReceiver
{
    [Header("ПРОФИЛЬ")]
    [Tooltip("Профиль психики/личности. Он решает, что важнее: еда, страх, приказ, добыча, Attraction, сакральное и прочая человеческая катастрофа.")]
    [SerializeField] private OUT_EntityMindProfile profile;

    [Header("ССЫЛКИ")]
    [SerializeField] private OUT_AIEntityMemory entityMemory;
    [SerializeField] private OUT_AIActorBrain brain;
    [SerializeField] private OUT_AIMemoryBuffer aiMemoryBuffer;

    [Header("ПРИЁМ СИГНАЛОВ")]
    [Tooltip("Какие каналы сущность вообще слышит/чувствует. Для гоблина можно включить Food, Attraction, Treasure, Danger. Для солдата ещё Command/Social.")]
    [SerializeField] private OUT_SignalChannelFlags acceptedChannels = OUT_SignalChannelFlags.All;
    [Tooltip("Минимальная сила сигнала после затухания по расстоянию. Ниже этого сущность игнорирует сигнал.")]
    [SerializeField][Range(0f, 1f)] private float minimumIntensity = 0.05f;
    [Tooltip("Игнорировать сигналы от своего root-объекта, чтобы сущность не возбуждалась от собственного шума. Хотя люди именно так и делают.")]
    [SerializeField] private bool ignoreOwnRoot = true;

    [Header("ОБРАТНОЕ РАСПРОСТРАНЕНИЕ")]
    [SerializeField] private bool allowBackwardSignals = true;
    [SerializeField] private OUT_SignalChannelFlags backwardChannels = OUT_SignalChannelFlags.Fear | OUT_SignalChannelFlags.Suspicion;

    [Header("ОТЛАДКА")]
    [SerializeField] private bool logReceivedSignals = false;
    [SerializeField] private bool logNarrativeOnStrongSignal = false;
    [SerializeField][Range(0f, 1f)] private float narrativeLogThreshold = 0.45f;

    private float lastBackwardSignalTime = -999f;

    public GameObject SignalOwner { get { return gameObject; } }
    public Vector3 SignalPosition { get { return transform.position; } }
    public OUT_AIEntityMemory Memory { get { return entityMemory; } }
    public OUT_EntityMindProfile Profile { get { return profile; } }

    private void Reset()
    {
        entityMemory = GetComponent<OUT_AIEntityMemory>();
        brain = GetComponent<OUT_AIActorBrain>();
        aiMemoryBuffer = GetComponent<OUT_AIMemoryBuffer>();
    }

    private void Awake()
    {
        if (entityMemory == null)
            entityMemory = GetComponent<OUT_AIEntityMemory>();
        if (brain == null)
            brain = GetComponent<OUT_AIActorBrain>();
        if (aiMemoryBuffer == null)
            aiMemoryBuffer = GetComponent<OUT_AIMemoryBuffer>();

        if (entityMemory != null && profile != null)
            entityMemory.SetProfile(profile);
    }

    private void OnEnable()
    {
        OUT_SignalBus.Register(this);
    }

    private void OnDisable()
    {
        OUT_SignalBus.Unregister(this);
    }

    public bool CanReceiveSignal(in OUT_Signal signal, float distance, float attenuatedIntensity)
    {
        if (!isActiveAndEnabled)
            return false;
        if ((signal.Channels & acceptedChannels) == 0)
            return false;
        if (attenuatedIntensity < minimumIntensity)
            return false;
        if (ignoreOwnRoot && signal.Source != null && signal.Source.transform.root == transform.root)
            return false;
        return true;
    }

    public void ReceiveSignal(in OUT_Signal signal, float distance, float attenuatedIntensity)
    {
        if (entityMemory != null)
            entityMemory.RememberSignal(signal, attenuatedIntensity);

        if (aiMemoryBuffer != null)
            ApplySignalToAIMemory(signal, attenuatedIntensity);

        if (logReceivedSignals)
            OUT_AIDebugLogService.Log(this, OUT_AIDebugLogService.AIEventKind.Memory, BuildSignalLog(signal, attenuatedIntensity));

        if (logNarrativeOnStrongSignal && attenuatedIntensity >= narrativeLogThreshold)
            OUT_AIDebugLogService.Log(this, OUT_AIDebugLogService.AIEventKind.Brain, OUT_ThoughtNarrator.GenerateNarrative(gameObject, 2, "default"));

        TryEmitBackwardSignal(signal, attenuatedIntensity);
    }

    public void EmitSignal(OUT_SignalChannelFlags channels, float intensity, float radius, GameObject subject = null, OUT_SignalDirection direction = OUT_SignalDirection.Forward, int payload = 0, string label = null)
    {
        OUT_SignalBus.Emit(gameObject, subject, transform.position, channels, intensity, radius, direction, payload, label);

        if (entityMemory != null)
        {
            entityMemory.RememberEvent(new OUT_MemoryEvent(OUT_MemoryEventKind.SentSignal, channels, gameObject, subject, transform.position, Time.time, intensity, payload, label));
        }
    }

    private void ApplySignalToAIMemory(in OUT_Signal signal, float intensity)
    {
        OUT_SignalChannelFlags channels = signal.Channels;
        float priority = profile != null ? profile.EvaluateSignalPriority(channels, intensity) : intensity;

        if ((channels & (OUT_SignalChannelFlags.Danger | OUT_SignalChannelFlags.Fear | OUT_SignalChannelFlags.Death | OUT_SignalChannelFlags.Fire | OUT_SignalChannelFlags.Aversion)) != 0)
        {
            float threat = profile != null ? profile.GetThreatAffinity(channels) : 1f;
            aiMemoryBuffer.ObserveDanger(signal.Origin, Mathf.Clamp01(priority * Mathf.Max(0.35f, threat)), (int)channels);
        }

        if ((channels & (OUT_SignalChannelFlags.Noise | OUT_SignalChannelFlags.Curiosity | OUT_SignalChannelFlags.Suspicion | OUT_SignalChannelFlags.Food | OUT_SignalChannelFlags.Reward | OUT_SignalChannelFlags.Attraction | OUT_SignalChannelFlags.Treasure | OUT_SignalChannelFlags.Shelter | OUT_SignalChannelFlags.Sacred | OUT_SignalChannelFlags.Social)) != 0)
        {
            aiMemoryBuffer.ObserveInterest(signal.Origin, Mathf.Clamp01(priority), (int)channels);
        }
    }

    private void TryEmitBackwardSignal(in OUT_Signal signal, float attenuatedIntensity)
    {
        if (!allowBackwardSignals || profile == null || !profile.EmitBackwardSignals)
            return;
        if (signal.Direction == OUT_SignalDirection.Backward)
            return;
        if (Time.time - lastBackwardSignalTime < profile.MinBackwardInterval)
            return;
        if (attenuatedIntensity < profile.BackwardSignalThreshold)
            return;

        OUT_SignalChannelFlags responseChannels = backwardChannels;
        if ((signal.Channels & OUT_SignalChannelFlags.Aggression) != 0 && profile.Aggression > profile.Cowardice)
            responseChannels = OUT_SignalChannelFlags.Aggression | OUT_SignalChannelFlags.Command;
        else if ((signal.Channels & OUT_SignalChannelFlags.Death) != 0)
            responseChannels = OUT_SignalChannelFlags.Fear | OUT_SignalChannelFlags.Danger;
        else if ((signal.Channels & (OUT_SignalChannelFlags.Food | OUT_SignalChannelFlags.Reward | OUT_SignalChannelFlags.Treasure | OUT_SignalChannelFlags.Attraction)) != 0)
            responseChannels = OUT_SignalChannelFlags.Curiosity | OUT_SignalChannelFlags.Social;
        else if ((signal.Channels & OUT_SignalChannelFlags.Sacred) != 0)
            responseChannels = OUT_SignalChannelFlags.Sacred | OUT_SignalChannelFlags.Curiosity;

        lastBackwardSignalTime = Time.time;
        EmitSignal(responseChannels, attenuatedIntensity * profile.BackwardIntensityScale, signal.Radius * profile.BackwardRadiusScale, signal.Source, OUT_SignalDirection.Backward, signal.Payload, "response");
    }

    private string BuildSignalLog(in OUT_Signal signal, float intensity)
    {
        string source = signal.Source != null ? signal.Source.name : "none";
        string priority = profile != null ? " priority:" + profile.EvaluateSignalPriority(signal.Channels, intensity).ToString("0.00") : string.Empty;
        return "mind received " + signal.Direction + " " + signal.Channels + " int:" + intensity.ToString("0.00") + priority + " from:" + source + " label:" + signal.Label;
    }
}
