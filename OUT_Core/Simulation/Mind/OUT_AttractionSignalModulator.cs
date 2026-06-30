using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(OUT_EntityMind))]
public class OUT_AttractionSignalModulator : MonoBehaviour, IOutSignalReceiver
{
    [Header("Ссылки")]
    [SerializeField] private OUT_EntityMind mind;
    [SerializeField] private OUT_AIEntityMemory entityMemory;
    [SerializeField] private OUT_AIMemoryBuffer aiMemoryBuffer;
    [SerializeField] private OUT_EntitySexIdentity sexIdentity;

    [Header("Настройки")]
    [Tooltip("Если true, компонент дополнительно взвешивает Attraction-сигналы по полу источника/subject.")]
    [SerializeField] private bool modulateAttractionBySex = true;
    [Tooltip("Минимальная сила после модуляции, чтобы записать усиленный интерес.")]
    [SerializeField][Range(0f, 1f)] private float minimumModulatedIntensity = 0.05f;
    [Tooltip("Писать в OUT_AIDebugLogService, что Attraction был пересчитан.")]
    [SerializeField] private bool logModulation;

    public GameObject SignalOwner => gameObject;
    public Vector3 SignalPosition => transform.position;

    private void Reset()
    {
        mind = GetComponent<OUT_EntityMind>();
        entityMemory = GetComponent<OUT_AIEntityMemory>();
        aiMemoryBuffer = GetComponent<OUT_AIMemoryBuffer>();
        sexIdentity = GetComponent<OUT_EntitySexIdentity>();
    }

    private void Awake()
    {
        if (mind == null) mind = GetComponent<OUT_EntityMind>();
        if (entityMemory == null) entityMemory = GetComponent<OUT_AIEntityMemory>();
        if (aiMemoryBuffer == null) aiMemoryBuffer = GetComponent<OUT_AIMemoryBuffer>();
        if (sexIdentity == null) sexIdentity = GetComponent<OUT_EntitySexIdentity>();
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
        if (!isActiveAndEnabled || !modulateAttractionBySex)
            return false;

        if ((signal.Channels & OUT_SignalChannelFlags.Attraction) == 0)
            return false;

        if (signal.Source != null && signal.Source.transform.root == transform.root)
            return false;

        return attenuatedIntensity > 0f;
    }

    public void ReceiveSignal(in OUT_Signal signal, float distance, float attenuatedIntensity)
    {
        if (sexIdentity == null)
            return;

        OUT_EntityBiologicalSex targetSex = ResolveTargetSex(signal);
        float multiplier = sexIdentity.EvaluateAttractionTo(targetSex);
        float modulated = Mathf.Clamp01(attenuatedIntensity * multiplier);

        if (modulated < minimumModulatedIntensity)
            return;

        if (aiMemoryBuffer != null)
            aiMemoryBuffer.ObserveInterest(signal.Origin, modulated, (int)signal.Channels);

        if (entityMemory != null)
        {
            entityMemory.RememberEvent(new OUT_MemoryEvent(
                OUT_MemoryEventKind.FeltAttraction,
                signal.Channels,
                signal.Source,
                signal.Subject,
                signal.Origin,
                Time.time,
                modulated,
                signal.Payload,
                signal.Label));
        }

        if (logModulation)
        {
            OUT_AIDebugLogService.Log(this, OUT_AIDebugLogService.AIEventKind.Memory,
                "attraction sex modulation target:" + targetSex + " x" + multiplier.ToString("0.00") + " final:" + modulated.ToString("0.00"));
        }
    }

    private OUT_EntityBiologicalSex ResolveTargetSex(in OUT_Signal signal)
    {
        OUT_EntitySexIdentity target = null;

        if (signal.Subject != null)
            target = signal.Subject.GetComponentInParent<OUT_EntitySexIdentity>();

        if (target == null && signal.Source != null)
            target = signal.Source.GetComponentInParent<OUT_EntitySexIdentity>();

        return target != null ? target.BiologicalSex : OUT_EntityBiologicalSex.Unspecified;
    }
}
