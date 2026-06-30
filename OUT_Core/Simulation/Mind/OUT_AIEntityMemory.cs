using System;
using UnityEngine;

[DisallowMultipleComponent]
public class OUT_AIEntityMemory : MonoBehaviour
{
    [Header("Profile")]
    [SerializeField] private OUT_EntityMindProfile profile;

    [Header("Runtime")]
    [SerializeField] private OUT_MoodState mood;
    [SerializeField] private OUT_MemoryState memoryState;
    [SerializeField] private OUT_MemoryEvent[] events;
    [SerializeField] private int eventCount;
    [SerializeField] private int nextIndex;

    public OUT_EntityMindProfile Profile => profile;
    public OUT_MoodState Mood => mood;
    public OUT_MemoryState MemoryState => memoryState;
    public int EventCount => eventCount;
    public int Capacity => events != null ? events.Length : 0;

    private void Awake()
    {
        EnsureCapacity();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            EnsureCapacity();
    }

    private void Update()
    {
        DecayMood(Time.deltaTime);
    }

    public void SetProfile(OUT_EntityMindProfile nextProfile)
    {
        profile = nextProfile;
        EnsureCapacity();
    }

    public void RememberEvent(in OUT_MemoryEvent memoryEvent)
    {
        EnsureCapacity();
        if (events == null || events.Length == 0)
            return;

        events[nextIndex] = memoryEvent;
        nextIndex = (nextIndex + 1) % events.Length;
        eventCount = Mathf.Min(eventCount + 1, events.Length);

        ApplyEventToMood(memoryEvent.Channels, memoryEvent.Intensity);
        ApplyEventToFlags(memoryEvent);
    }

    public void RememberSignal(in OUT_Signal signal, float attenuatedIntensity)
    {
        OUT_MemoryEvent memoryEvent = new OUT_MemoryEvent(
            OUT_MemoryEventKind.ReceivedSignal,
            signal.Channels,
            signal.Source,
            signal.Subject,
            signal.Origin,
            Time.time,
            attenuatedIntensity,
            signal.Payload,
            signal.Label);

        RememberEvent(memoryEvent);
    }

    public bool TryGetRecentEvent(int newestOffset, out OUT_MemoryEvent memoryEvent)
    {
        memoryEvent = default;
        if (events == null || eventCount <= 0)
            return false;

        if (newestOffset < 0 || newestOffset >= eventCount)
            return false;

        int index = nextIndex - 1 - newestOffset;
        while (index < 0)
            index += events.Length;

        memoryEvent = events[index];
        return true;
    }

    public void Clear()
    {
        if (events != null)
            Array.Clear(events, 0, events.Length);

        eventCount = 0;
        nextIndex = 0;
        mood = default;
        memoryState.Reset();
    }

    public float EstimateTension()
    {
        return Mathf.Clamp01((mood.Stress * 0.35f) + (mood.Fear * 0.30f) + (mood.Aggression * 0.20f) + (mood.Curiosity * 0.15f));
    }

    public float EstimatePleasurePull()
    {
        if (eventCount <= 0)
            return 0f;

        float strongest = 0f;
        int count = Mathf.Min(eventCount, 4);
        for (int i = 0; i < count; i++)
        {
            if (!TryGetRecentEvent(i, out OUT_MemoryEvent e))
                continue;

            float pull = profile != null
                ? profile.GetPleasureAffinity(e.Channels) * e.Intensity
                : GetFallbackPleasureAffinity(e.Channels) * e.Intensity;

            if (pull > strongest)
                strongest = pull;
        }

        return Mathf.Clamp01(strongest);
    }

    private void ApplyEventToMood(OUT_SignalChannelFlags channels, float intensity)
    {
        float weighted = intensity * (profile != null ? profile.GetSignalWeight(channels) : 1f);

        if ((channels & OUT_SignalChannelFlags.Danger) != 0)
            mood.AddStress(weighted * 0.35f);
        if ((channels & OUT_SignalChannelFlags.Fear) != 0)
            mood.AddFear(weighted * 0.45f);
        if ((channels & OUT_SignalChannelFlags.Aggression) != 0)
            mood.AddAggression(weighted * 0.40f);
        if ((channels & OUT_SignalChannelFlags.Curiosity) != 0)
            mood.AddCuriosity(weighted * 0.35f);
        if ((channels & OUT_SignalChannelFlags.Death) != 0)
        {
            mood.AddFear(weighted * 0.35f);
            mood.AddDespair(weighted * 0.15f);
        }
        if ((channels & OUT_SignalChannelFlags.Noise) != 0)
            mood.AddStress(weighted * 0.12f);

        if ((channels & OUT_SignalChannelFlags.Food) != 0)
            mood.AddCuriosity(weighted * 0.20f);
        if ((channels & OUT_SignalChannelFlags.Reward) != 0)
        {
            mood.AddCuriosity(weighted * 0.25f);
            mood.AddAggression(weighted * 0.08f);
        }
        if ((channels & OUT_SignalChannelFlags.Attraction) != 0)
        {
            mood.AddCuriosity(weighted * 0.30f);
            if (profile != null && profile.Shadow > profile.EgoStrength)
                mood.AddAggression(weighted * 0.14f);
        }
        if ((channels & OUT_SignalChannelFlags.Treasure) != 0)
        {
            mood.AddCuriosity(weighted * 0.25f);
            if (profile != null && profile.Greed > profile.Discipline)
                mood.AddAggression(weighted * 0.10f);
        }
        if ((channels & OUT_SignalChannelFlags.Shelter) != 0)
            mood.AddFear(-weighted * 0.08f);
        if ((channels & OUT_SignalChannelFlags.Sacred) != 0)
            mood.AddCuriosity(weighted * 0.20f);
        if ((channels & OUT_SignalChannelFlags.Aversion) != 0)
        {
            mood.AddFear(weighted * 0.20f);
            mood.AddStress(weighted * 0.25f);
        }
        if ((channels & OUT_SignalChannelFlags.Social) != 0)
            mood.AddCuriosity(weighted * 0.10f);

        mood.Clamp();
    }

    private void ApplyEventToFlags(in OUT_MemoryEvent memoryEvent)
    {
        if ((memoryEvent.Channels & (OUT_SignalChannelFlags.Danger | OUT_SignalChannelFlags.Fear | OUT_SignalChannelFlags.Death | OUT_SignalChannelFlags.Aversion)) != 0)
            memoryState.Remember(OUT_MemoryFlags.Suspicious);

        if ((memoryEvent.Channels & OUT_SignalChannelFlags.Aggression) != 0)
            memoryState.Remember(OUT_MemoryFlags.Provoked);

        if ((memoryEvent.Channels & (OUT_SignalChannelFlags.Food | OUT_SignalChannelFlags.Reward | OUT_SignalChannelFlags.Attraction | OUT_SignalChannelFlags.Treasure | OUT_SignalChannelFlags.Shelter | OUT_SignalChannelFlags.Sacred)) != 0)
            memoryState.Remember(OUT_MemoryFlags.Interested);
    }

    private void DecayMood(float dt)
    {
        if (dt <= 0f || profile == null)
            return;

        float decay = profile.MemoryDecayPerSecond * dt;
        mood.Stress = Mathf.MoveTowards(mood.Stress, 0f, decay);
        mood.Fear = Mathf.MoveTowards(mood.Fear, 0f, decay);
        mood.Aggression = Mathf.MoveTowards(mood.Aggression, 0f, decay * 0.75f);
        mood.Despair = Mathf.MoveTowards(mood.Despair, 0f, decay * 0.35f);
        mood.Curiosity = Mathf.MoveTowards(mood.Curiosity, 0f, decay * 0.5f);
        mood.Clamp();
    }

    private void EnsureCapacity()
    {
        int desired = profile != null ? profile.MemoryCapacity : 16;
        desired = Mathf.Max(4, desired);

        if (events != null && events.Length == desired)
            return;

        OUT_MemoryEvent[] next = new OUT_MemoryEvent[desired];
        if (events != null && eventCount > 0)
        {
            int copy = Mathf.Min(eventCount, desired);
            for (int i = 0; i < copy; i++)
            {
                if (TryGetRecentEvent(copy - 1 - i, out OUT_MemoryEvent e))
                    next[i] = e;
            }
            eventCount = copy;
            nextIndex = copy % desired;
        }
        else
        {
            eventCount = 0;
            nextIndex = 0;
        }

        events = next;
    }

    private static float GetFallbackPleasureAffinity(OUT_SignalChannelFlags channels)
    {
        float value = 0f;
        if ((channels & OUT_SignalChannelFlags.Food) != 0) value += 0.5f;
        if ((channels & OUT_SignalChannelFlags.Attraction) != 0) value += 0.5f;
        if ((channels & OUT_SignalChannelFlags.Treasure) != 0) value += 0.5f;
        if ((channels & OUT_SignalChannelFlags.Reward) != 0) value += 0.5f;
        if ((channels & OUT_SignalChannelFlags.Shelter) != 0) value += 0.25f;
        return Mathf.Clamp01(value);
    }
}
