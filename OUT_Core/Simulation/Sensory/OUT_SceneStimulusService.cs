using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class OUT_SceneStimulusService : MonoBehaviour
{
    public struct OUT_StimulusHit
    {
        public OUT_SceneStimulusEmitter Emitter;
        public Vector3 Position;
        public float Distance;
        public float Strength;
        public OUT_SensoryChannelFlags Channels;
    }

    public static OUT_SceneStimulusService Instance { get; private set; }

    private readonly List<OUT_SceneStimulusEmitter> _emitters = new List<OUT_SceneStimulusEmitter>(64);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple OUT_SceneStimulusService instances found. Keeping the first one.");
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Register(OUT_SceneStimulusEmitter emitter)
    {
        if (emitter == null)
            return;

        if (_emitters.Contains(emitter))
            return;

        _emitters.Add(emitter);
    }

    public void Unregister(OUT_SceneStimulusEmitter emitter)
    {
        if (emitter == null)
            return;

        _emitters.Remove(emitter);
    }

    public OUT_SceneSensorySample Sample(
        Vector3 worldPosition,
        OUT_SensoryChannelFlags channels = OUT_SensoryChannelFlags.AllDynamic)
    {
        OUT_SceneSensorySample sample = default;
        sample.Clear();

        CleanupNullEmitters();

        for (int i = 0; i < _emitters.Count; i++)
        {
            OUT_SceneStimulusEmitter emitter = _emitters[i];
            if (emitter == null || !emitter.isActiveAndEnabled)
                continue;

            float strength = emitter.EvaluateStrength(worldPosition, channels);
            if (strength <= 0f)
                continue;

            OUT_SensoryChannelFlags emitterChannels = emitter.Channels & channels;

            if ((emitterChannels & OUT_SensoryChannelFlags.Luminance) != 0)
                sample.SetMax(OUT_SensoryChannelFlags.Luminance, strength);

            if ((emitterChannels & OUT_SensoryChannelFlags.Noise) != 0)
                sample.SetMax(OUT_SensoryChannelFlags.Noise, strength);

            if ((emitterChannels & OUT_SensoryChannelFlags.Danger) != 0)
                sample.SetMax(OUT_SensoryChannelFlags.Danger, strength);

            if ((emitterChannels & OUT_SensoryChannelFlags.Food) != 0)
                sample.SetMax(OUT_SensoryChannelFlags.Food, strength);

            if ((emitterChannels & OUT_SensoryChannelFlags.Fire) != 0)
                sample.SetMax(OUT_SensoryChannelFlags.Fire, strength);
        }

        return sample;
    }

    public bool TryGetStrongestStimulus(
        Vector3 worldPosition,
        OUT_SensoryChannelFlags channels,
        out OUT_StimulusHit hit)
    {
        CleanupNullEmitters();

        hit = default;
        bool found = false;
        float bestStrength = 0f;

        for (int i = 0; i < _emitters.Count; i++)
        {
            OUT_SceneStimulusEmitter emitter = _emitters[i];
            if (emitter == null || !emitter.isActiveAndEnabled)
                continue;

            float strength = emitter.EvaluateStrength(worldPosition, channels);
            if (strength <= bestStrength)
                continue;

            Vector3 emitterPosition = emitter.WorldPosition;

            bestStrength = strength;
            found = true;

            hit = new OUT_StimulusHit
            {
                Emitter = emitter,
                Position = emitterPosition,
                Distance = Vector3.Distance(worldPosition, emitterPosition),
                Strength = strength,
                Channels = emitter.Channels & channels
            };
        }

        return found;
    }

    private void CleanupNullEmitters()
    {
        for (int i = _emitters.Count - 1; i >= 0; i--)
        {
            if (_emitters[i] == null)
                _emitters.RemoveAt(i);
        }
    }
}