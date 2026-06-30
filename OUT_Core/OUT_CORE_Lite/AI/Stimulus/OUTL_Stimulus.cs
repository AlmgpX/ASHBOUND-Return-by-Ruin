using System;
using System.Collections.Generic;
using UnityEngine;

public enum OUTL_StimulusType : byte
{
    None = 0,
    Sound = 1,
    Damage = 2,
    Sight = 3,
    Command = 4,
    Touch = 5,
    SightEnemy = 6,
    SightAlly = 7,
    SightFood = 8,
    SightDanger = 9,
    HeardNoise = 10,
    HeardCombat = 11,
    TookDamage = 12,
    LostTarget = 13,
    FoundCover = 14,
    LowHealth = 15,
    GoalCompleted = 16,
    ScheduleChanged = 17,
    Combat = 18,
    Death = 19,
    Suspicion = 20,
    Fear = 21,
    Fire = 22,
    Light = 23,
    Smell = 24,
    Territory = 25,
    Resource = 26,
    Social = 27,
    Scripted = 28,
    Alert = 29,
    Egregore = 30
}

public struct OUTL_Stimulus
{
    public OUTL_StimulusType Type;
    public OUTL_EntityId Source;
    public Vector3 Position;
    public float Radius;
    public float Loudness;
    public float Strength;
    public float Confidence;
    public float Priority;
    public float DecayTime;
    public string Key;
    public string[] Tags;
    public float Time;

    public OUTL_Stimulus(OUTL_StimulusType type, OUTL_EntityId source, Vector3 position, float radius, float loudness, float priority, string key)
    {
        Type = type;
        Source = source;
        Position = position;
        Radius = radius;
        Loudness = loudness;
        Strength = loudness;
        Confidence = 1f;
        Priority = priority;
        DecayTime = 0f;
        Key = key;
        Tags = null;
        OUTL_World world = OUTL_World.Instance;
        Time = world != null ? world.WorldTime : UnityEngine.Time.time;
    }

    public OUTL_Stimulus(OUTL_StimulusType type, OUTL_EntityId source, Vector3 position, float radius, float strength, float confidence, float priority, float decayTime, string key, string[] tags)
    {
        Type = type;
        Source = source;
        Position = position;
        Radius = radius;
        Loudness = strength;
        Strength = strength;
        Confidence = confidence;
        Priority = priority;
        DecayTime = decayTime;
        Key = key;
        Tags = tags;
        OUTL_World world = OUTL_World.Instance;
        Time = world != null ? world.WorldTime : UnityEngine.Time.time;
    }

    public bool HasTag(string tag)
    {
        if (string.IsNullOrEmpty(tag) || Tags == null) return false;
        for (int i = 0; i < Tags.Length; i++)
            if (Tags[i] == tag)
                return true;
        return false;
    }
}

public struct OUTL_StimulusQuery
{
    public Vector3 Position;
    public float Radius;
    public OUTL_StimulusType Type;
    public float MinPriority;
    public int MaxCount;
    public OUTL_EntityId IgnoreSource;
    public string RequiredTag;
}

public sealed class OUTL_StimulusBucket
{
    public long Cell;
    public readonly List<int> Indices = new List<int>(32);
}

public sealed class OUTL_StimulusStore
{
    public float CellSize = 32f;
    public int MaxStoredStimuli = 2048;
    public int MaxStimuliPerSector = 128;
    public int MaxProcessedPerFrame = 256;
    public float DefaultDecayTime = 8f;

    private readonly List<StoredStimulus> stimuli = new List<StoredStimulus>(512);
    private readonly Dictionary<long, OUTL_StimulusBucket> sectorIndex = new Dictionary<long, OUTL_StimulusBucket>(128);
    private int nextSequence = 1;
    private int cleanupCursor;

    public int StoredCount { get { return stimuli.Count; } }

    private struct StoredStimulus
    {
        public OUTL_Stimulus Stimulus;
        public long Cell;
        public int Sequence;
        public bool Alive;
    }

    public void Store(OUTL_Stimulus stimulus)
    {
        if (stimulus.Type == OUTL_StimulusType.None) return;
        if (stimulus.DecayTime <= 0f) stimulus.DecayTime = DefaultDecayTime;
        if (stimulus.Time <= 0f) stimulus.Time = ReadTime();

        long cell = CellToKey(WorldToCell(stimulus.Position));
        OUTL_StimulusBucket bucket = GetBucket(cell);
        TrimBucket(bucket);
        if (stimuli.Count >= Mathf.Max(1, MaxStoredStimuli)) RemoveAt(0);

        StoredStimulus stored = new StoredStimulus
        {
            Stimulus = stimulus,
            Cell = cell,
            Sequence = nextSequence++,
            Alive = true
        };
        stimuli.Add(stored);
        bucket.Indices.Add(stimuli.Count - 1);
    }

    public int Query(in OUTL_StimulusQuery query, List<OUTL_Stimulus> output)
    {
        if (output == null) return 0;
        output.Clear();
        int maxCount = query.MaxCount > 0 ? query.MaxCount : MaxProcessedPerFrame;
        if (maxCount <= 0) return 0;

        float radius = Mathf.Max(0f, query.Radius);
        float sqrRadius = radius * radius;
        float now = ReadTime();
        int cellRadius = Mathf.Max(0, Mathf.CeilToInt(radius / Mathf.Max(1f, CellSize)));
        Vector2Int center = WorldToCell(query.Position);

        for (int z = -cellRadius; z <= cellRadius; z++)
        {
            for (int x = -cellRadius; x <= cellRadius; x++)
            {
                OUTL_StimulusBucket bucket;
                if (!sectorIndex.TryGetValue(CellToKey(new Vector2Int(center.x + x, center.y + z)), out bucket) || bucket == null) continue;
                List<int> indices = bucket.Indices;
                for (int i = 0; i < indices.Count; i++)
                {
                    if (output.Count >= maxCount) return output.Count;
                    int index = indices[i];
                    if (index < 0 || index >= stimuli.Count) continue;
                    StoredStimulus stored = stimuli[index];
                    if (!stored.Alive || IsExpired(stored.Stimulus, now)) continue;
                    if (query.Type != OUTL_StimulusType.None && stored.Stimulus.Type != query.Type) continue;
                    if (stored.Stimulus.Priority < query.MinPriority) continue;
                    if (query.IgnoreSource.IsValid && stored.Stimulus.Source == query.IgnoreSource) continue;
                    if (!string.IsNullOrEmpty(query.RequiredTag) && !stored.Stimulus.HasTag(query.RequiredTag)) continue;

                    float stimulusRadius = Mathf.Max(radius, stored.Stimulus.Radius);
                    float checkSqr = Mathf.Max(sqrRadius, stimulusRadius * stimulusRadius);
                    if ((stored.Stimulus.Position - query.Position).sqrMagnitude > checkSqr) continue;
                    output.Add(stored.Stimulus);
                }
            }
        }

        return output.Count;
    }

    public int QueryRadius(Vector3 position, float radius, List<OUTL_Stimulus> output, int maxCount)
    {
        OUTL_StimulusQuery query = new OUTL_StimulusQuery
        {
            Position = position,
            Radius = radius,
            Type = OUTL_StimulusType.None,
            MinPriority = 0f,
            MaxCount = maxCount
        };
        return Query(query, output);
    }

    public int ConsumeForEntity(OUTL_EntityId entity, Vector3 position, float radius, List<OUTL_Stimulus> output, int maxCount)
    {
        OUTL_StimulusQuery query = new OUTL_StimulusQuery
        {
            Position = position,
            Radius = radius,
            Type = OUTL_StimulusType.None,
            MinPriority = 0f,
            MaxCount = maxCount,
            IgnoreSource = entity
        };
        return Query(query, output);
    }

    public int Tick(float time, int budget)
    {
        if (stimuli.Count == 0) return 0;
        int processed = 0;
        int max = Mathf.Min(Mathf.Max(1, budget), stimuli.Count);
        while (processed < max && stimuli.Count > 0)
        {
            if (cleanupCursor >= stimuli.Count) cleanupCursor = 0;
            StoredStimulus stored = stimuli[cleanupCursor];
            if (!stored.Alive || IsExpired(stored.Stimulus, time))
            {
                RemoveAt(cleanupCursor);
                processed++;
                continue;
            }

            cleanupCursor++;
            processed++;
        }
        return processed;
    }

    public int CountInCell(long cellKey)
    {
        OUTL_StimulusBucket bucket;
        if (!sectorIndex.TryGetValue(cellKey, out bucket) || bucket == null) return 0;
        int count = 0;
        float now = ReadTime();
        List<int> indices = bucket.Indices;
        for (int i = 0; i < indices.Count; i++)
        {
            int index = indices[i];
            if (index < 0 || index >= stimuli.Count) continue;
            StoredStimulus stored = stimuli[index];
            if (stored.Alive && !IsExpired(stored.Stimulus, now)) count++;
        }
        return count;
    }

    public void Clear()
    {
        stimuli.Clear();
        sectorIndex.Clear();
        cleanupCursor = 0;
    }

    private OUTL_StimulusBucket GetBucket(long cell)
    {
        OUTL_StimulusBucket bucket;
        if (!sectorIndex.TryGetValue(cell, out bucket))
        {
            bucket = new OUTL_StimulusBucket { Cell = cell };
            sectorIndex[cell] = bucket;
        }
        return bucket;
    }

    private void TrimBucket(OUTL_StimulusBucket bucket)
    {
        if (bucket == null) return;
        int max = Mathf.Max(1, MaxStimuliPerSector);
        while (bucket.Indices.Count >= max)
            RemoveAt(bucket.Indices[0]);
    }

    private void RemoveAt(int index)
    {
        if (index < 0 || index >= stimuli.Count) return;
        StoredStimulus removed = stimuli[index];
        RemoveIndexFromCell(removed.Cell, index);

        int last = stimuli.Count - 1;
        if (index != last)
        {
            StoredStimulus moved = stimuli[last];
            stimuli[index] = moved;
            ReplaceIndexInCell(moved.Cell, last, index);
        }

        stimuli.RemoveAt(last);
        if (cleanupCursor > stimuli.Count) cleanupCursor = stimuli.Count;
    }

    private void RemoveIndexFromCell(long cell, int index)
    {
        OUTL_StimulusBucket bucket;
        if (!sectorIndex.TryGetValue(cell, out bucket) || bucket == null) return;
        List<int> list = bucket.Indices;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != index) continue;
            int last = list.Count - 1;
            list[i] = list[last];
            list.RemoveAt(last);
            break;
        }
        if (list.Count == 0) sectorIndex.Remove(cell);
    }

    private void ReplaceIndexInCell(long cell, int oldIndex, int newIndex)
    {
        OUTL_StimulusBucket bucket;
        if (!sectorIndex.TryGetValue(cell, out bucket) || bucket == null) return;
        List<int> list = bucket.Indices;
        for (int i = 0; i < list.Count; i++)
            if (list[i] == oldIndex)
            {
                list[i] = newIndex;
                return;
            }
    }

    private bool IsExpired(OUTL_Stimulus stimulus, float now)
    {
        float decay = stimulus.DecayTime > 0f ? stimulus.DecayTime : DefaultDecayTime;
        return decay > 0f && now - stimulus.Time > decay;
    }

    private Vector2Int WorldToCell(Vector3 position)
    {
        float size = Mathf.Max(1f, CellSize);
        return new Vector2Int(Mathf.FloorToInt(position.x / size), Mathf.FloorToInt(position.z / size));
    }

    public long CellToKey(Vector2Int cell)
    {
        unchecked { return ((long)cell.x << 32) ^ (uint)cell.y; }
    }

    private static float ReadTime()
    {
        OUTL_World world = OUTL_World.Instance;
        return world != null ? world.WorldTime : Time.time;
    }
}

public static class OUTL_StimulusBus
{
    public static event Action<OUTL_Stimulus> OnStimulus;

    private static readonly OUTL_StimulusStore store = new OUTL_StimulusStore();

    public static OUTL_StimulusStore Store { get { return store; } }
    public static float CellSize { get { return store.CellSize; } set { store.CellSize = value; } }
    public static int MaxStoredStimuli { get { return store.MaxStoredStimuli; } set { store.MaxStoredStimuli = value; } }
    public static int MaxStimuliPerSector { get { return store.MaxStimuliPerSector; } set { store.MaxStimuliPerSector = value; } }
    public static int MaxProcessedPerFrame { get { return store.MaxProcessedPerFrame; } set { store.MaxProcessedPerFrame = value; } }
    public static float DefaultDecayTime { get { return store.DefaultDecayTime; } set { store.DefaultDecayTime = value; } }
    public static int StoredCount { get { return store.StoredCount; } }

    public static void Emit(OUTL_Stimulus stimulus)
    {
        if (OUTL_World.Instance != null) store.Store(stimulus);
        Action<OUTL_Stimulus> handler = OnStimulus;
        if (handler != null) handler(stimulus);
        OUTL_DebugLog.TraceStimulus(stimulus.Type + " key=" + stimulus.Key + " pos=" + stimulus.Position + " r=" + stimulus.Radius.ToString("0.0") + " p=" + stimulus.Priority.ToString("0.0") + " source=" + stimulus.Source);
    }

    public static void EmitSound(OUTL_EntityId source, Vector3 position, float radius, float loudness = 1f, float priority = 1f, string key = "sound")
    {
        Emit(new OUTL_Stimulus(OUTL_StimulusType.HeardNoise, source, position, radius, loudness, priority, key));
    }

    public static void EmitCombat(OUTL_EntityId source, Vector3 position, float radius, float strength = 1f, float priority = 1f, string key = "combat")
    {
        Emit(new OUTL_Stimulus(OUTL_StimulusType.HeardCombat, source, position, radius, strength, priority, key));
    }

    public static void EmitDamage(OUTL_EntityId source, Vector3 position, float radius, float strength = 1f, float priority = 1f, string key = "damage")
    {
        Emit(new OUTL_Stimulus(OUTL_StimulusType.TookDamage, source, position, radius, strength, 1f, priority, DefaultDecayTime, key, null));
    }

    public static void EmitDeath(OUTL_EntityId source, Vector3 position, float radius, float strength = 1f, float priority = 1f, string key = "death")
    {
        Emit(new OUTL_Stimulus(OUTL_StimulusType.Death, source, position, radius, strength, 1f, priority, DefaultDecayTime, key, null));
    }

    public static void EmitSuspicion(OUTL_EntityId source, Vector3 position, float radius, float strength = 1f, float priority = 1f, string key = "suspicion")
    {
        Emit(new OUTL_Stimulus(OUTL_StimulusType.Suspicion, source, position, radius, strength, 1f, priority, DefaultDecayTime, key, null));
    }

    public static void EmitFear(OUTL_EntityId source, Vector3 position, float radius, float strength = 1f, float priority = 1f, string key = "fear")
    {
        Emit(new OUTL_Stimulus(OUTL_StimulusType.Fear, source, position, radius, strength, 1f, priority, DefaultDecayTime, key, null));
    }

    public static void EmitResource(OUTL_EntityId source, Vector3 position, float radius, float strength = 1f, float priority = 1f, string key = "resource")
    {
        Emit(new OUTL_Stimulus(OUTL_StimulusType.Resource, source, position, radius, strength, 1f, priority, DefaultDecayTime, key, null));
    }

    public static void EmitTerritory(OUTL_EntityId source, Vector3 position, float radius, float strength = 1f, float priority = 1f, string key = "territory")
    {
        Emit(new OUTL_Stimulus(OUTL_StimulusType.Territory, source, position, radius, strength, 1f, priority, DefaultDecayTime, key, null));
    }

    public static void EmitSocial(OUTL_EntityId source, Vector3 position, float radius, float strength = 1f, float priority = 1f, string key = "social")
    {
        Emit(new OUTL_Stimulus(OUTL_StimulusType.Social, source, position, radius, strength, 1f, priority, DefaultDecayTime, key, null));
    }

    public static int Query(in OUTL_StimulusQuery query, List<OUTL_Stimulus> output)
    {
        return store.Query(query, output);
    }

    public static int QueryRadius(Vector3 position, float radius, List<OUTL_Stimulus> output, int maxCount)
    {
        return store.QueryRadius(position, radius, output, maxCount);
    }

    public static int ConsumeForEntity(OUTL_EntityId entity, Vector3 position, float radius, List<OUTL_Stimulus> output, int maxCount)
    {
        return store.ConsumeForEntity(entity, position, radius, output, maxCount);
    }

    public static int Tick(float time, int budget)
    {
        return store.Tick(time, budget);
    }

    public static int CountInCell(long cellKey)
    {
        return store.CountInCell(cellKey);
    }

    public static void Clear()
    {
        store.Clear();
    }
}

public enum OUTL_StimulusSensorMode
{
    Hearing = 0,
    Vision = 1,
    Threat = 2,
    Territory = 3
}

[DisallowMultipleComponent]
public sealed class OUTL_StimulusSensor : MonoBehaviour, OUTL_ITickable
{
    public OUTL_AIActor Actor;
    public OUTL_EntityAdapter Entity;
    public OUTL_StimulusSensorMode Mode = OUTL_StimulusSensorMode.Hearing;
    public bool AutoRegister = true;
    public bool Enabled = true;
    public float Radius = 16f;
    public float TickInterval = 0.35f;
    public float MinPriority = 0.05f;
    public int MaxStimuliPerTick = 12;
    public bool IgnoreSelf = true;

    private readonly List<OUTL_Stimulus> buffer = new List<OUTL_Stimulus>(16);
    private bool registered;
    private bool referencesResolved;

    public bool OUTL_IsTickEnabled { get { return Enabled && isActiveAndEnabled && Actor != null; } }
    public OUTL_TickLane OUTL_TickLane { get { return OUTL_TickLane.AI; } }
    public float OUTL_TickInterval { get { return Mathf.Max(0.05f, TickInterval); } }

    private void Awake()
    {
        Resolve();
    }

    private void OnEnable()
    {
        Resolve();
        if (AutoRegister) Register();
    }

    private void OnDisable()
    {
        Unregister();
    }

    private void OnValidate()
    {
        referencesResolved = false;
    }

    public void Register()
    {
        if (registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Register(this);
        registered = true;
    }

    public void Unregister()
    {
        if (!registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Unregister(this);
        registered = false;
    }

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        if (!referencesResolved) Resolve();
        if (Actor == null || Entity == null) return;

        float radius = ResolveRadius();
        OUTL_StimulusBus.ConsumeForEntity(Entity.Id, transform.position, radius, buffer, Mathf.Max(1, MaxStimuliPerTick));
        for (int i = 0; i < buffer.Count; i++)
        {
            OUTL_Stimulus stimulus = buffer[i];
            if (IgnoreSelf && stimulus.Source == Entity.Id) continue;
            if (!MatchesMode(stimulus.Type)) continue;
            float priority = Score(stimulus, radius);
            if (priority < MinPriority) continue;
            Actor.ReceiveStimulus(stimulus, priority);
        }
    }

    private float ResolveRadius()
    {
        OUTL_AIPerceptionProfile profile = Actor != null ? Actor.PerceptionProfile : null;
        if (profile == null) return Mathf.Max(0.1f, Radius);
        switch (Mode)
        {
            case OUTL_StimulusSensorMode.Vision: return Mathf.Max(0.1f, profile.SightDistance);
            case OUTL_StimulusSensorMode.Threat: return Mathf.Max(0.1f, profile.DangerRadius);
            case OUTL_StimulusSensorMode.Territory: return Mathf.Max(0.1f, profile.DangerRadius);
            case OUTL_StimulusSensorMode.Hearing:
            default: return Mathf.Max(0.1f, profile.HearingRadius > 0f ? profile.HearingRadius : Radius);
        }
    }

    private float Score(OUTL_Stimulus stimulus, float radius)
    {
        float dist = Vector3.Distance(transform.position, stimulus.Position);
        float falloff = radius > 0.01f ? Mathf.Clamp01(1f - dist / radius) : 1f;
        float strength = stimulus.Strength > 0f ? stimulus.Strength : stimulus.Loudness;
        return Mathf.Max(0f, stimulus.Priority) * Mathf.Max(0.01f, strength) * Mathf.Max(0.05f, stimulus.Confidence) * Mathf.Max(0.05f, falloff);
    }

    private bool MatchesMode(OUTL_StimulusType type)
    {
        switch (Mode)
        {
            case OUTL_StimulusSensorMode.Vision:
                return type == OUTL_StimulusType.Sight
                    || type == OUTL_StimulusType.SightEnemy
                    || type == OUTL_StimulusType.SightAlly
                    || type == OUTL_StimulusType.SightFood
                    || type == OUTL_StimulusType.SightDanger
                    || type == OUTL_StimulusType.Light
                    || type == OUTL_StimulusType.Resource;
            case OUTL_StimulusSensorMode.Threat:
                return type == OUTL_StimulusType.Damage
                    || type == OUTL_StimulusType.TookDamage
                    || type == OUTL_StimulusType.HeardCombat
                    || type == OUTL_StimulusType.Combat
                    || type == OUTL_StimulusType.Death
                    || type == OUTL_StimulusType.Fear
                    || type == OUTL_StimulusType.Fire
                    || type == OUTL_StimulusType.Alert
                    || type == OUTL_StimulusType.Egregore
                    || type == OUTL_StimulusType.SightDanger;
            case OUTL_StimulusSensorMode.Territory:
                return type == OUTL_StimulusType.Territory
                    || type == OUTL_StimulusType.Resource
                    || type == OUTL_StimulusType.Social
                    || type == OUTL_StimulusType.Egregore
                    || type == OUTL_StimulusType.Scripted;
            case OUTL_StimulusSensorMode.Hearing:
            default:
                return type == OUTL_StimulusType.Sound
                    || type == OUTL_StimulusType.HeardNoise
                    || type == OUTL_StimulusType.HeardCombat
                    || type == OUTL_StimulusType.Combat
                    || type == OUTL_StimulusType.Command
                    || type == OUTL_StimulusType.Damage
                    || type == OUTL_StimulusType.TookDamage
                    || type == OUTL_StimulusType.Death
                    || type == OUTL_StimulusType.Fire
                    || type == OUTL_StimulusType.Fear
                    || type == OUTL_StimulusType.Alert
                    || type == OUTL_StimulusType.Egregore;
        }
    }

    private void Resolve()
    {
        if (referencesResolved) return;
        if (Actor == null) Actor = GetComponent<OUTL_AIActor>();
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        referencesResolved = true;
    }
}
