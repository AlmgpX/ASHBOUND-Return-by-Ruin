using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(OUT_EgregoreZone))]
public class OUT_EgregoreZoneSaveState : MonoBehaviour, IOutSaveState
{
    [SerializeField] private OUT_EgregoreZone zone;

    public string SaveKey => "egregore.zone";

    [Serializable]
    private struct State
    {
        public float Threat;
        public float Fear;
        public float Violence;
        public float Hunger;
        public float Greed;
        public float Desire;
        public float Sacred;
        public float Shelter;
        public float Corruption;
        public float Social;
        public int AbsorbedSignals;
    }

    private void Reset()
    {
        zone = GetComponent<OUT_EgregoreZone>();
    }

    private void Awake()
    {
        if (zone == null)
            zone = GetComponent<OUT_EgregoreZone>();
    }

    public string CaptureStateJson()
    {
        if (zone == null)
            zone = GetComponent<OUT_EgregoreZone>();

        OUT_EgregoreState s = zone != null ? zone.State : default;
        State state = new State
        {
            Threat = s.Threat,
            Fear = s.Fear,
            Violence = s.Violence,
            Hunger = s.Hunger,
            Greed = s.Greed,
            Desire = s.Desire,
            Sacred = s.Sacred,
            Shelter = s.Shelter,
            Corruption = s.Corruption,
            Social = s.Social,
            AbsorbedSignals = zone != null ? zone.AbsorbedSignals : 0
        };

        return JsonUtility.ToJson(state);
    }

    public void RestoreStateJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;

        if (zone == null)
            zone = GetComponent<OUT_EgregoreZone>();

        if (zone == null)
            return;

        State state = JsonUtility.FromJson<State>(json);
        OUT_EgregoreState restored = new OUT_EgregoreState
        {
            Threat = state.Threat,
            Fear = state.Fear,
            Violence = state.Violence,
            Hunger = state.Hunger,
            Greed = state.Greed,
            Desire = state.Desire,
            Sacred = state.Sacred,
            Shelter = state.Shelter,
            Corruption = state.Corruption,
            Social = state.Social
        };
        restored.Clamp();
        zone.SetRuntimeState(restored, state.AbsorbedSignals);
    }
}
