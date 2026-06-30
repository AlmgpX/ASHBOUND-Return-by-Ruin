using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[DefaultExecutionOrder(-7100)]
[DisallowMultipleComponent]
public class OUT_SignalBus : MonoBehaviour
{
    public static OUT_SignalBus Instance { get; private set; }

    [Header("Lifecycle")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool autoCleanupNullReceivers = true;

    [Header("Safety")]
    [SerializeField][Min(8)] private int initialReceiverCapacity = 256;
    [SerializeField][Min(1)] private int maxSignalsPerFrame = 128;
    [SerializeField] private bool logSignals = false;

    private readonly List<IOutSignalReceiver> receivers = new List<IOutSignalReceiver>(256);
    private readonly List<IOutSignalReceiver> pendingAdd = new List<IOutSignalReceiver>(32);
    private readonly List<IOutSignalReceiver> pendingRemove = new List<IOutSignalReceiver>(32);

    private bool dispatching;
    private int signalSerial;
    private int signalsThisFrame;
    private int lastFrame = -1;

    public int ReceiverCount => receivers.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        if (initialReceiverCapacity > receivers.Capacity)
            receivers.Capacity = initialReceiverCapacity;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static OUT_SignalBus EnsureExists()
    {
        if (Instance != null)
            return Instance;

        OUT_SignalBus existing = FindObjectOfType<OUT_SignalBus>();
        if (existing != null)
            return existing;

        GameObject go = new GameObject("OUT_SignalBus");
        return go.AddComponent<OUT_SignalBus>();
    }

    public static void Register(IOutSignalReceiver receiver)
    {
        EnsureExists().RegisterReceiver(receiver);
    }

    public static void Unregister(IOutSignalReceiver receiver)
    {
        if (Instance != null)
            Instance.UnregisterReceiver(receiver);
    }

    public static int Emit(
        GameObject source,
        GameObject subject,
        Vector3 origin,
        OUT_SignalChannelFlags channels,
        float intensity,
        float radius,
        OUT_SignalDirection direction = OUT_SignalDirection.Forward,
        int payload = 0,
        string label = null)
    {
        OUT_SignalBus bus = EnsureExists();
        OUT_Signal signal = bus.CreateSignal(source, subject, origin, channels, intensity, radius, direction, payload, label);
        bus.Emit(signal);
        return signal.Id;
    }

    public OUT_Signal CreateSignal(
        GameObject source,
        GameObject subject,
        Vector3 origin,
        OUT_SignalChannelFlags channels,
        float intensity,
        float radius,
        OUT_SignalDirection direction = OUT_SignalDirection.Forward,
        int payload = 0,
        string label = null)
    {
        signalSerial++;
        return new OUT_Signal(signalSerial, source, subject, origin, channels, direction, intensity, radius, Time.time, payload, label);
    }

    public void RegisterReceiver(IOutSignalReceiver receiver)
    {
        if (receiver == null)
            return;

        if (dispatching)
        {
            if (!pendingAdd.Contains(receiver))
                pendingAdd.Add(receiver);
            return;
        }

        if (!receivers.Contains(receiver))
            receivers.Add(receiver);
    }

    public void UnregisterReceiver(IOutSignalReceiver receiver)
    {
        if (receiver == null)
            return;

        if (dispatching)
        {
            if (!pendingRemove.Contains(receiver))
                pendingRemove.Add(receiver);
            return;
        }

        receivers.Remove(receiver);
    }

    public void Emit(in OUT_Signal signal)
    {
        if (signal.Channels == OUT_SignalChannelFlags.None || signal.Intensity <= 0f)
            return;

        ResetFrameCounterIfNeeded();
        if (signalsThisFrame >= maxSignalsPerFrame)
            return;

        signalsThisFrame++;

        if (autoCleanupNullReceivers)
            CleanupNullReceivers();

        dispatching = true;
        List<IOutSignalReceiver> snapshot = ListPool<IOutSignalReceiver>.Get();
        try
        {
            snapshot.AddRange(receivers);
            for (int i = 0; i < snapshot.Count; i++)
            {
                IOutSignalReceiver receiver = snapshot[i];
                if (receiver == null)
                    continue;

                GameObject owner = receiver.SignalOwner;
                if (owner == null || !owner.activeInHierarchy)
                    continue;

                if (signal.Source != null && owner.transform.root == signal.Source.transform.root)
                    continue;

                Vector3 receiverPosition = receiver.SignalPosition;
                float distance = Vector3.Distance(signal.Origin, receiverPosition);
                bool bypassRadius = receiver is IOutGlobalSignalReceiver globalReceiver && globalReceiver.ReceivesSignalsWithoutBusRadiusFilter;

                if (!bypassRadius && signal.Radius > 0f && distance > signal.Radius)
                    continue;

                float attenuated = bypassRadius ? signal.Intensity : CalculateAttenuatedIntensity(signal.Intensity, signal.Radius, distance);
                if (attenuated <= 0f)
                    continue;

                if (!receiver.CanReceiveSignal(signal, distance, attenuated))
                    continue;

                receiver.ReceiveSignal(signal, distance, attenuated);
            }
        }
        finally
        {
            ListPool<IOutSignalReceiver>.Release(snapshot);
            dispatching = false;
            FlushPendingReceiverChanges();
        }

        if (logSignals)
        {
            OUT_AIDebugLogService.Log(signal.Source, OUT_AIDebugLogService.AIEventKind.Memory,
                $"signal {signal.Direction} {signal.Channels} int:{signal.Intensity:0.00} radius:{signal.Radius:0.0} label:{signal.Label}");
        }
    }

    private float CalculateAttenuatedIntensity(float intensity, float radius, float distance)
    {
        if (radius <= 0f)
            return Mathf.Clamp01(intensity);

        float t = Mathf.Clamp01(distance / Mathf.Max(0.001f, radius));
        return Mathf.Clamp01(intensity * (1f - t));
    }

    private void ResetFrameCounterIfNeeded()
    {
        if (lastFrame == Time.frameCount)
            return;

        lastFrame = Time.frameCount;
        signalsThisFrame = 0;
    }

    private void CleanupNullReceivers()
    {
        for (int i = receivers.Count - 1; i >= 0; i--)
        {
            IOutSignalReceiver receiver = receivers[i];
            if (receiver == null || receiver.SignalOwner == null)
                receivers.RemoveAt(i);
        }
    }

    private void FlushPendingReceiverChanges()
    {
        if (pendingRemove.Count > 0)
        {
            for (int i = 0; i < pendingRemove.Count; i++)
                receivers.Remove(pendingRemove[i]);
            pendingRemove.Clear();
        }

        if (pendingAdd.Count > 0)
        {
            for (int i = 0; i < pendingAdd.Count; i++)
            {
                IOutSignalReceiver receiver = pendingAdd[i];
                if (receiver != null && !receivers.Contains(receiver))
                    receivers.Add(receiver);
            }
            pendingAdd.Clear();
        }
    }
}
