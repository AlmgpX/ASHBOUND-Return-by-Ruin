using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

[DefaultExecutionOrder(-6880)]
[DisallowMultipleComponent]
public class OUT_EgregoreEventLedger : MonoBehaviour, IOutGlobalSignalReceiver, IOutSaveState
{
    [Serializable]
    public class Rule
    {
        public string EventId = "event";
        public string DisplayName = "Event";
        public OUT_SignalChannelFlags RequiredChannels = OUT_SignalChannelFlags.None;
        public OUT_SignalChannelFlags AnyChannels = OUT_SignalChannelFlags.All;
        public string RequiredLabelContains = string.Empty;
        public string RequiredSourceTag = string.Empty;
        public int RequiredPayload = int.MinValue;
        [Range(0f, 1f)] public float MinimumIntensity = 0.05f;
        [Range(0f, 5f)] public float ScoreScale = 1f;
        public bool InjectIntoZoneState = true;
        [Range(0f, 1f)] public float ZoneInjectScale = 0.25f;
    }

    [Serializable]
    public struct Record
    {
        public string EventId;
        public string Label;
        public OUT_SignalChannelFlags Channels;
        public Vector3 Position;
        public float Time;
        public float Score;
        public string SourceName;
        public int Payload;
    }

    [Serializable]
    public struct Counter
    {
        public string EventId;
        public float Score;
        public int Count;
    }

    [Serializable]
    private struct SaveData
    {
        public Record[] Records;
        public Counter[] Counters;
        public int RecordCount;
        public int NextRecordIndex;
    }

    [Header("References")]
    [SerializeField] private OUT_EgregoreZone zone;

    [Header("Filtering")]
    [SerializeField] private bool receiveSignalsWithoutBusRadiusFilter = true;
    [SerializeField] private bool requireSignalInsideZone = true;
    [SerializeField] private OUT_SignalChannelFlags listenedChannels = OUT_SignalChannelFlags.All;

    [Header("Rules")]
    [SerializeField] private Rule[] rules;

    [Header("History")]
    [SerializeField][Min(8)] private int maxRecords = 128;
    [SerializeField] private Record[] records;
    [SerializeField] private int recordCount;
    [SerializeField] private int nextRecordIndex;
    [SerializeField] private Counter[] counters;

    [Header("Decay")]
    [SerializeField] private bool decayCounters = true;
    [SerializeField][Min(0.25f)] private float decayInterval = 15f;
    [SerializeField][Range(0f, 1f)] private float decayAmount = 0.01f;

    [Header("File")]
    [SerializeField] private bool saveTextFile = true;
    [SerializeField][Min(1f)] private float saveInterval = 20f;
    [SerializeField] private string folderName = "OUT_Egregores";
    [SerializeField] private bool logWrites;

    private float nextDecayTime;
    private float nextSaveTime;
    private readonly StringBuilder builder = new StringBuilder(4096);

    public string SaveKey => "egregore.ledger";
    public GameObject SignalOwner => gameObject;
    public Vector3 SignalPosition => transform.position;
    public bool ReceivesSignalsWithoutBusRadiusFilter => receiveSignalsWithoutBusRadiusFilter;
    public int RecordCount => recordCount;
    public int CounterCount => counters != null ? counters.Length : 0;

    private void Reset()
    {
        zone = GetComponent<OUT_EgregoreZone>();
        rules = new[]
        {
            new Rule { EventId = "death", DisplayName = "Death", AnyChannels = OUT_SignalChannelFlags.Death, ScoreScale = 1f },
            new Rule { EventId = "sacred", DisplayName = "Sacred", AnyChannels = OUT_SignalChannelFlags.Sacred, ScoreScale = 1f },
            new Rule { EventId = "fire", DisplayName = "Fire", AnyChannels = OUT_SignalChannelFlags.Fire, ScoreScale = 1f },
            new Rule { EventId = "loot", DisplayName = "Loot", AnyChannels = OUT_SignalChannelFlags.Reward | OUT_SignalChannelFlags.Treasure, ScoreScale = 1f }
        };
    }

    private void Awake()
    {
        if (zone == null)
            zone = GetComponent<OUT_EgregoreZone>();
        EnsureBuffers();
    }

    private void OnEnable()
    {
        OUT_SignalBus.Register(this);
        nextDecayTime = Time.time + UnityEngine.Random.Range(0f, decayInterval);
        nextSaveTime = Time.time + UnityEngine.Random.Range(0f, saveInterval);
    }

    private void OnDisable()
    {
        OUT_SignalBus.Unregister(this);
    }

    private void Update()
    {
        if (decayCounters && Time.time >= nextDecayTime)
        {
            nextDecayTime = Time.time + decayInterval;
            DecayCounters();
        }

        if (saveTextFile && Time.time >= nextSaveTime)
        {
            nextSaveTime = Time.time + saveInterval;
            SaveTextSnapshot();
        }
    }

    public bool CanReceiveSignal(in OUT_Signal signal, float distance, float attenuatedIntensity)
    {
        if (!isActiveAndEnabled)
            return false;
        if ((signal.Channels & listenedChannels) == 0)
            return false;
        if (requireSignalInsideZone && zone != null && !zone.ContainsPoint(signal.Origin))
            return false;
        return true;
    }

    public void ReceiveSignal(in OUT_Signal signal, float distance, float attenuatedIntensity)
    {
        if (rules == null || rules.Length == 0)
            return;

        for (int i = 0; i < rules.Length; i++)
        {
            Rule rule = rules[i];
            if (rule == null || !Matches(rule, signal, attenuatedIntensity))
                continue;

            float score = Mathf.Clamp01(attenuatedIntensity) * Mathf.Max(0f, rule.ScoreScale);
            AddRecord(rule, signal, score);
            AddCounter(rule.EventId, score);

            if (rule.InjectIntoZoneState && zone != null && rule.ZoneInjectScale > 0f)
                zone.ReceiveSignal(signal, 0f, Mathf.Clamp01(score * rule.ZoneInjectScale));
        }
    }

    public bool TryGetCounter(string eventId, out Counter counter)
    {
        counter = default;
        if (counters == null || string.IsNullOrWhiteSpace(eventId))
            return false;

        for (int i = 0; i < counters.Length; i++)
        {
            if (counters[i].EventId == eventId)
            {
                counter = counters[i];
                return true;
            }
        }
        return false;
    }

    public void ClearLedger()
    {
        if (records != null)
            Array.Clear(records, 0, records.Length);
        if (counters != null)
            Array.Clear(counters, 0, counters.Length);
        recordCount = 0;
        nextRecordIndex = 0;
    }

    public string CaptureStateJson()
    {
        SaveData data = new SaveData
        {
            Records = records,
            Counters = counters,
            RecordCount = recordCount,
            NextRecordIndex = nextRecordIndex
        };
        return JsonUtility.ToJson(data);
    }

    public void RestoreStateJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;

        SaveData data = JsonUtility.FromJson<SaveData>(json);
        records = data.Records ?? Array.Empty<Record>();
        counters = data.Counters ?? Array.Empty<Counter>();
        recordCount = Mathf.Clamp(data.RecordCount, 0, records.Length);
        nextRecordIndex = records.Length > 0 ? Mathf.Clamp(data.NextRecordIndex, 0, records.Length - 1) : 0;
    }

    public void SaveTextSnapshot()
    {
        try
        {
            string directory = Path.Combine(Application.persistentDataPath, folderName);
            Directory.CreateDirectory(directory);
            string id = zone != null ? zone.EgregoreId : gameObject.name;
            string path = Path.Combine(directory, SanitizeFileName(id) + "_ledger.txt");

            builder.Length = 0;
            builder.AppendLine("OUT_EGREGORE_EVENT_LEDGER");
            builder.AppendLine("id=" + id);
            builder.AppendLine("time=" + Time.time.ToString("0.000", CultureInfo.InvariantCulture));
            builder.AppendLine("records=" + recordCount);
            builder.AppendLine("counters:");
            if (counters != null)
            {
                for (int i = 0; i < counters.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(counters[i].EventId))
                        continue;
                    builder.Append(counters[i].EventId).Append(" count=").Append(counters[i].Count)
                        .Append(" score=").AppendLine(counters[i].Score.ToString("0.0000", CultureInfo.InvariantCulture));
                }
            }

            builder.AppendLine("recent:");
            int count = Mathf.Min(recordCount, records != null ? records.Length : 0);
            for (int i = 0; i < count; i++)
            {
                int index = nextRecordIndex - 1 - i;
                while (records.Length > 0 && index < 0)
                    index += records.Length;

                if (records.Length == 0)
                    break;

                Record r = records[index];
                builder.Append(r.Time.ToString("0.000", CultureInfo.InvariantCulture)).Append(' ')
                    .Append(r.EventId).Append(" score=").Append(r.Score.ToString("0.000", CultureInfo.InvariantCulture))
                    .Append(" channels=").Append(r.Channels)
                    .Append(" source=").Append(r.SourceName)
                    .Append(" label=").AppendLine(r.Label);
            }

            File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
            if (logWrites)
                Debug.Log("OUT_EgregoreEventLedger saved: " + path);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("OUT_EgregoreEventLedger save failed: " + ex.Message, this);
        }
    }

    private bool Matches(Rule rule, in OUT_Signal signal, float intensity)
    {
        if (intensity < rule.MinimumIntensity)
            return false;
        if (rule.RequiredChannels != OUT_SignalChannelFlags.None && (signal.Channels & rule.RequiredChannels) != rule.RequiredChannels)
            return false;
        if (rule.AnyChannels != OUT_SignalChannelFlags.None && (signal.Channels & rule.AnyChannels) == 0)
            return false;
        if (!string.IsNullOrWhiteSpace(rule.RequiredLabelContains) && (signal.Label == null || signal.Label.IndexOf(rule.RequiredLabelContains, StringComparison.OrdinalIgnoreCase) < 0))
            return false;
        if (!string.IsNullOrWhiteSpace(rule.RequiredSourceTag) && (signal.Source == null || !signal.Source.CompareTag(rule.RequiredSourceTag)))
            return false;
        if (rule.RequiredPayload != int.MinValue && signal.Payload != rule.RequiredPayload)
            return false;
        return true;
    }

    private void AddRecord(Rule rule, in OUT_Signal signal, float score)
    {
        EnsureBuffers();
        if (records == null || records.Length == 0)
            return;

        records[nextRecordIndex] = new Record
        {
            EventId = rule.EventId,
            Label = string.IsNullOrWhiteSpace(signal.Label) ? rule.DisplayName : signal.Label,
            Channels = signal.Channels,
            Position = signal.Origin,
            Time = Time.time,
            Score = score,
            SourceName = signal.Source != null ? signal.Source.name : "none",
            Payload = signal.Payload
        };

        nextRecordIndex = (nextRecordIndex + 1) % records.Length;
        recordCount = Mathf.Min(recordCount + 1, records.Length);
    }

    private void AddCounter(string eventId, float score)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            eventId = "event";

        EnsureCounterCapacity(eventId);
        for (int i = 0; i < counters.Length; i++)
        {
            if (counters[i].EventId == eventId)
            {
                counters[i].Score = Mathf.Max(0f, counters[i].Score + score);
                counters[i].Count++;
                return;
            }
        }
    }

    private void DecayCounters()
    {
        if (counters == null)
            return;

        for (int i = 0; i < counters.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(counters[i].EventId))
                continue;
            counters[i].Score = Mathf.Max(0f, counters[i].Score - decayAmount);
        }
    }

    private void EnsureBuffers()
    {
        int desired = Mathf.Max(8, maxRecords);
        if (records == null || records.Length != desired)
        {
            records = new Record[desired];
            recordCount = 0;
            nextRecordIndex = 0;
        }

        if (counters == null)
            counters = Array.Empty<Counter>();
    }

    private void EnsureCounterCapacity(string eventId)
    {
        if (counters == null)
        {
            counters = new[] { new Counter { EventId = eventId } };
            return;
        }

        for (int i = 0; i < counters.Length; i++)
            if (counters[i].EventId == eventId)
                return;

        int old = counters.Length;
        Array.Resize(ref counters, old + 1);
        counters[old].EventId = eventId;
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
}
