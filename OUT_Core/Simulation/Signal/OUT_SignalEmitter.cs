using UnityEngine;

[DisallowMultipleComponent]
public class OUT_SignalEmitter : MonoBehaviour, IOutSignalEmitter
{
    [Header("Signal")]
    [SerializeField] private OUT_SignalChannelFlags channels = OUT_SignalChannelFlags.Noise;
    [SerializeField][Range(0f, 1f)] private float intensity = 1f;
    [SerializeField][Min(0f)] private float radius = 12f;
    [SerializeField] private OUT_SignalDirection defaultDirection = OUT_SignalDirection.Forward;
    [SerializeField] private string label;
    [SerializeField] private int payload;
    [SerializeField] private GameObject subject;

    [Header("Lifecycle")]
    [SerializeField] private bool emitOnEnable = false;
    [SerializeField] private bool emitRepeatedly = false;
    [SerializeField][Min(0.05f)] private float repeatInterval = 1f;

    private float nextEmitTime;

    private void OnEnable()
    {
        if (emitOnEnable)
            Emit();

        nextEmitTime = Time.time + repeatInterval;
    }

    private void Update()
    {
        if (!emitRepeatedly)
            return;

        if (Time.time < nextEmitTime)
            return;

        nextEmitTime = Time.time + repeatInterval;
        Emit();
    }

    public OUT_Signal BuildSignal(OUT_SignalDirection direction = OUT_SignalDirection.Forward)
    {
        OUT_SignalBus bus = OUT_SignalBus.EnsureExists();
        return bus.CreateSignal(gameObject, subject, transform.position, channels, intensity, radius, direction, payload, label);
    }

    public void Emit()
    {
        OUT_SignalBus.Emit(gameObject, subject, transform.position, channels, intensity, radius, defaultDirection, payload, label);
    }

    public void EmitForward()
    {
        OUT_SignalBus.Emit(gameObject, subject, transform.position, channels, intensity, radius, OUT_SignalDirection.Forward, payload, label);
    }

    public void EmitBackward()
    {
        OUT_SignalBus.Emit(gameObject, subject, transform.position, channels, intensity, radius, OUT_SignalDirection.Backward, payload, label);
    }

    public void EmitWithSubject(GameObject nextSubject)
    {
        OUT_SignalBus.Emit(gameObject, nextSubject, transform.position, channels, intensity, radius, defaultDirection, payload, label);
    }
}
