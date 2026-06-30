using UnityEngine;

[DefaultExecutionOrder(-7200)]
[DisallowMultipleComponent]
public class OUT_CameraShakeService : MonoBehaviour
{
    public static OUT_CameraShakeService Instance { get; private set; }

    [Header("Lifecycle")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Header("Discovery")]
    [SerializeField] private bool refreshReceiversAutomatically = true;
    [SerializeField][Min(0.05f)] private float receiverRefreshInterval = 0.75f;

    [Header("Safety")]
    [SerializeField][Min(1)] private int maxReceivers = 32;
    [SerializeField][Min(0f)] private float amplitudeScale = 1f;
    [SerializeField][Min(0f)] private float maxLocalAmplitude = 2.5f;

    private IOutCameraShakeReceiver[] receivers;
    private float nextRefreshTime;

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

        receivers = new IOutCameraShakeReceiver[Mathf.Max(1, maxReceivers)];
        RefreshReceivers();
    }

    private void Update()
    {
        if (!refreshReceiversAutomatically)
            return;

        if (Time.unscaledTime < nextRefreshTime)
            return;

        nextRefreshTime = Time.unscaledTime + receiverRefreshInterval;
        RefreshReceivers();
    }

    public static void EnsureExists()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject("OUT_CameraShakeService");
        go.AddComponent<OUT_CameraShakeService>();
    }

    public static void Shake(in OUT_CameraShakeRequest request)
    {
        if (Instance == null)
            EnsureExists();

        Instance.EmitShake(request);
    }

    public void EmitShake(in OUT_CameraShakeRequest request)
    {
        if (request.Duration <= 0f || request.Amplitude <= 0f)
            return;

        if (receivers == null || receivers.Length != Mathf.Max(1, maxReceivers))
            receivers = new IOutCameraShakeReceiver[Mathf.Max(1, maxReceivers)];

        for (int i = 0; i < receivers.Length; i++)
        {
            IOutCameraShakeReceiver receiver = receivers[i];
            if (receiver == null)
                continue;

            if (!receiver.CanReceiveShake(request))
                continue;

            float localAmplitude = CalculateLocalAmplitude(receiver, request);
            if (localAmplitude <= 0f)
                continue;

            receiver.ReceiveShake(request, localAmplitude);
        }
    }

    private float CalculateLocalAmplitude(IOutCameraShakeReceiver receiver, in OUT_CameraShakeRequest request)
    {
        Component component = receiver as Component;
        if (component == null)
            return Mathf.Min(request.Amplitude * amplitudeScale, maxLocalAmplitude);

        if (request.Radius <= 0f)
            return Mathf.Min(request.Amplitude * amplitudeScale, maxLocalAmplitude);

        float distance = Vector3.Distance(component.transform.position, request.Origin);
        if (distance > request.Radius)
            return 0f;

        // GoldSrc UTIL_ScreenShake essentially gates by radius and keeps amplitude flat.
        // Keep a small optional falloff would change behavior, so no hidden easing here.
        return Mathf.Min(request.Amplitude * amplitudeScale, maxLocalAmplitude);
    }

    public void RefreshReceivers()
    {
        if (receivers == null || receivers.Length != Mathf.Max(1, maxReceivers))
            receivers = new IOutCameraShakeReceiver[Mathf.Max(1, maxReceivers)];

        for (int i = 0; i < receivers.Length; i++)
            receivers[i] = null;

        OUT_CameraShakeReceiver[] found = FindObjectsOfType<OUT_CameraShakeReceiver>();
        int count = Mathf.Min(found.Length, receivers.Length);
        for (int i = 0; i < count; i++)
            receivers[i] = found[i];
    }
}
