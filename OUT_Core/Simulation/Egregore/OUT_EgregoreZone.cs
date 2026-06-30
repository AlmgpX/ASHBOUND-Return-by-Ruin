using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

[DefaultExecutionOrder(-6900)]
[DisallowMultipleComponent]
public class OUT_EgregoreZone : MonoBehaviour, IOutGlobalSignalReceiver
{
    [Header("Идентичность эгрегора")]
    [SerializeField] private string egregoreId = "Forest";
    [SerializeField] private string displayName = "Forest Spirit";

    [Header("Зона влияния")]
    [SerializeField] private OUT_EgregoreContainmentShape shape = OUT_EgregoreContainmentShape.Sphere;
    [SerializeField][Min(0.1f)] private float radius = 80f;
    [SerializeField] private Vector3 boxSize = new Vector3(80f, 30f, 80f);
    [SerializeField] private bool receiveSignalsWithoutBusRadiusFilter = true;
    [SerializeField] private OUT_SignalChannelFlags listenedChannels = OUT_SignalChannelFlags.All;

    [Header("Накопление")]
    [SerializeField] private OUT_EgregoreWeights weights = OUT_EgregoreWeights.Default;
    [SerializeField][Range(0f, 1f)] private float signalAccumulationScale = 0.08f;
    [SerializeField][Min(0f)] private float passiveDecayPerUpdate = 0.006f;
    [SerializeField][Min(0.05f)] private float updateInterval = 3f;

    [Header("Влияние на сущностей")]
    [SerializeField] private bool emitAmbientSignals = true;
    [SerializeField][Min(0.1f)] private float ambientEmitInterval = 5f;
    [SerializeField][Range(0f, 1f)] private float ambientIntensityScale = 0.45f;
    [SerializeField][Min(0f)] private float ambientRadius = 0f;

    [Header("Сохранение в читаемый файл")]
    [SerializeField] private bool saveToTextFile = true;
    [SerializeField][Min(1f)] private float saveInterval = 20f;
    [SerializeField] private string folderName = "OUT_Egregores";
    [SerializeField] private bool logSaves;

    [Header("Runtime")]
    [SerializeField] private OUT_EgregoreState state;
    [SerializeField] private OUT_EgregoreDominantForce dominantForce;
    [SerializeField] private int absorbedSignals;
    [SerializeField] private int ambientSignalsEmitted;

    private float nextUpdateTime;
    private float nextAmbientTime;
    private float nextSaveTime;
    private readonly StringBuilder builder = new StringBuilder(512);

    public string EgregoreId => string.IsNullOrWhiteSpace(egregoreId) ? name : egregoreId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? EgregoreId : displayName;
    public OUT_EgregoreState State => state;
    public OUT_EgregoreDominantForce DominantForce => dominantForce;
    public int AbsorbedSignals => absorbedSignals;

    public GameObject SignalOwner => gameObject;
    public Vector3 SignalPosition => transform.position;
    public bool ReceivesSignalsWithoutBusRadiusFilter => receiveSignalsWithoutBusRadiusFilter;

    private void Reset()
    {
        egregoreId = gameObject.name;
        displayName = gameObject.name;
        weights = OUT_EgregoreWeights.Default;
    }

    private void OnEnable()
    {
        OUT_SignalBus.Register(this);
        OUT_EgregoreRegistry.Register(this);
        nextUpdateTime = Time.time + Random.Range(0f, updateInterval);
        nextAmbientTime = Time.time + Random.Range(0f, ambientEmitInterval);
        nextSaveTime = Time.time + Random.Range(0f, saveInterval);
    }

    private void OnDisable()
    {
        OUT_SignalBus.Unregister(this);
        OUT_EgregoreRegistry.Unregister(this);
    }

    private void Update()
    {
        float now = Time.time;

        if (now >= nextUpdateTime)
        {
            nextUpdateTime = now + updateInterval;
            TickEgregore();
        }

        if (emitAmbientSignals && now >= nextAmbientTime)
        {
            nextAmbientTime = now + ambientEmitInterval;
            EmitAmbientSignal();
        }

        if (saveToTextFile && now >= nextSaveTime)
        {
            nextSaveTime = now + saveInterval;
            SaveTextSnapshot();
        }
    }

    public bool CanReceiveSignal(in OUT_Signal signal, float distance, float attenuatedIntensity)
    {
        if (!isActiveAndEnabled)
            return false;

        if ((signal.Channels & listenedChannels) == 0)
            return false;

        if (signal.Intensity <= 0f)
            return false;

        return ContainsPoint(signal.Origin);
    }

    public void ReceiveSignal(in OUT_Signal signal, float distance, float attenuatedIntensity)
    {
        float localIntensity = signal.Intensity * signalAccumulationScale;
        state.Add(signal.Channels, localIntensity, weights);
        absorbedSignals++;
    }

    public void SetRuntimeState(OUT_EgregoreState nextState, int nextAbsorbedSignals)
    {
        state = nextState;
        state.Clamp();
        absorbedSignals = Mathf.Max(0, nextAbsorbedSignals);
        dominantForce = state.GetDominantForce();
    }

    public bool ContainsPoint(Vector3 worldPoint)
    {
        Vector3 local = transform.InverseTransformPoint(worldPoint);
        if (shape == OUT_EgregoreContainmentShape.Sphere)
            return local.sqrMagnitude <= radius * radius;

        Vector3 half = boxSize * 0.5f;
        return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
    }

    public float GetInfluenceAt(Vector3 worldPoint)
    {
        Vector3 local = transform.InverseTransformPoint(worldPoint);
        if (shape == OUT_EgregoreContainmentShape.Sphere)
        {
            float distance = local.magnitude;
            if (distance > radius)
                return 0f;
            return 1f - Mathf.Clamp01(distance / Mathf.Max(0.001f, radius));
        }

        Vector3 half = boxSize * 0.5f;
        if (Mathf.Abs(local.x) > half.x || Mathf.Abs(local.y) > half.y || Mathf.Abs(local.z) > half.z)
            return 0f;

        float nx = half.x > 0f ? Mathf.Abs(local.x) / half.x : 0f;
        float ny = half.y > 0f ? Mathf.Abs(local.y) / half.y : 0f;
        float nz = half.z > 0f ? Mathf.Abs(local.z) / half.z : 0f;
        return 1f - Mathf.Clamp01(Mathf.Max(nx, ny, nz));
    }

    private void TickEgregore()
    {
        state.Decay(passiveDecayPerUpdate);
        dominantForce = state.GetDominantForce();
    }

    private void EmitAmbientSignal()
    {
        dominantForce = state.GetDominantForce();
        OUT_SignalChannelFlags channels = GetDominantSignalChannels(dominantForce);
        if (channels == OUT_SignalChannelFlags.None)
            return;

        float strength = GetDominantStrength(dominantForce);
        if (strength <= 0.01f)
            return;

        float radiusToUse = ambientRadius > 0f ? ambientRadius : (shape == OUT_EgregoreContainmentShape.Sphere ? radius : Mathf.Max(boxSize.x, boxSize.z) * 0.5f);
        OUT_SignalBus.Emit(gameObject, gameObject, transform.position, channels, Mathf.Clamp01(strength * ambientIntensityScale), radiusToUse, OUT_SignalDirection.Echo, 0, "egregore:" + EgregoreId + ":" + dominantForce);
        ambientSignalsEmitted++;
    }

    private OUT_SignalChannelFlags GetDominantSignalChannels(OUT_EgregoreDominantForce force)
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
                return OUT_SignalChannelFlags.Attraction | OUT_SignalChannelFlags.Social;
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

    private float GetDominantStrength(OUT_EgregoreDominantForce force)
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

    public void SaveTextSnapshot()
    {
        try
        {
            string directory = Path.Combine(Application.persistentDataPath, folderName);
            Directory.CreateDirectory(directory);
            string fileName = SanitizeFileName(EgregoreId) + ".txt";
            string path = Path.Combine(directory, fileName);

            builder.Length = 0;
            builder.AppendLine("OUT_EGREGORE_STATE");
            builder.AppendLine("id=" + EgregoreId);
            builder.AppendLine("name=" + DisplayName);
            builder.AppendLine("time=" + Time.time.ToString("0.000", CultureInfo.InvariantCulture));
            builder.AppendLine("dominant=" + dominantForce);
            builder.AppendLine("absorbedSignals=" + absorbedSignals);
            builder.AppendLine("ambientSignalsEmitted=" + ambientSignalsEmitted);
            AppendStateLine("threat", state.Threat);
            AppendStateLine("fear", state.Fear);
            AppendStateLine("violence", state.Violence);
            AppendStateLine("hunger", state.Hunger);
            AppendStateLine("greed", state.Greed);
            AppendStateLine("desire", state.Desire);
            AppendStateLine("sacred", state.Sacred);
            AppendStateLine("shelter", state.Shelter);
            AppendStateLine("corruption", state.Corruption);
            AppendStateLine("social", state.Social);

            File.WriteAllText(path, builder.ToString(), Encoding.UTF8);

            if (logSaves)
                Debug.Log("OUT_EgregoreZone saved: " + path);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("OUT_EgregoreZone save failed: " + ex.Message, this);
        }
    }

    private void AppendStateLine(string key, float value)
    {
        builder.Append(key).Append('=').AppendLine(value.ToString("0.0000", CultureInfo.InvariantCulture));
    }

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "egregore";

        char[] invalid = Path.GetInvalidFileNameChars();
        string result = value;
        for (int i = 0; i < invalid.Length; i++)
            result = result.Replace(invalid[i], '_');
        return result;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.35f, 0.85f, 1f, 0.25f);
        if (shape == OUT_EgregoreContainmentShape.Sphere)
        {
            Gizmos.DrawWireSphere(transform.position, radius);
        }
        else
        {
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, boxSize);
            Gizmos.matrix = previous;
        }
    }
}
