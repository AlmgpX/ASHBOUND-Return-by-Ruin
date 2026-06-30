using UnityEngine;

[DisallowMultipleComponent]
public class OUTL_HearingSensor : MonoBehaviour
{
    public OUTL_AIActor Actor;
    public OUTL_EntityAdapter Entity;
    public bool Enabled = true;
    public float HearingMultiplier = 1f;
    public float MinPriority = 0.05f;
    public bool UseOcclusionRaycast = true;
    public LayerMask OcclusionMask = ~0;
    public float OcclusionPenalty = 0.5f;
    public string[] IgnoreKeys;

    public OUTL_Stimulus LastStimulus;
    public Vector3 LastHeardPosition;
    public float LastHeardPriority;
    public float LastHeardTime;

    private void Awake()
    {
        if (Actor == null) Actor = GetComponent<OUTL_AIActor>();
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
    }

    private void OnEnable()
    {
        OUTL_StimulusBus.OnStimulus += OnStimulus;
    }

    private void OnDisable()
    {
        OUTL_StimulusBus.OnStimulus -= OnStimulus;
    }

    private void OnStimulus(OUTL_Stimulus stimulus)
    {
        if (!Enabled || Actor == null || Entity == null || Entity.Runtime == null) return;
        if (stimulus.Source == Entity.Id) return;
        if (ShouldIgnore(stimulus.Key)) return;
        if (!IsAudibleStimulus(stimulus.Type)) return;

        float radius = Mathf.Max(0.01f, stimulus.Radius * Mathf.Max(0.01f, HearingMultiplier));
        float dist = Vector3.Distance(transform.position, stimulus.Position);
        if (dist > radius) return;

        float priority = stimulus.Priority * Mathf.Clamp01(1f - dist / radius) * Mathf.Max(0.01f, stimulus.Strength > 0f ? stimulus.Strength : stimulus.Loudness);
        if (UseOcclusionRaycast)
        {
            Vector3 origin = transform.position + Vector3.up * 1.4f;
            Vector3 target = stimulus.Position + Vector3.up * 0.25f;
            Vector3 dir = target - origin;
            if (Physics.Raycast(origin, dir.normalized, dir.magnitude, OcclusionMask, QueryTriggerInteraction.Ignore))
                priority *= Mathf.Clamp01(OcclusionPenalty);
        }

        if (priority < MinPriority || priority < LastHeardPriority * 0.75f) return;

        LastStimulus = stimulus;
        LastHeardPosition = stimulus.Position;
        LastHeardPriority = priority;
        LastHeardTime = stimulus.Time;
        Actor.ReceiveStimulus(stimulus, priority);
    }

    private static bool IsAudibleStimulus(OUTL_StimulusType type)
    {
        return type == OUTL_StimulusType.Sound
            || type == OUTL_StimulusType.HeardNoise
            || type == OUTL_StimulusType.HeardCombat
            || type == OUTL_StimulusType.Command
            || type == OUTL_StimulusType.Damage
            || type == OUTL_StimulusType.TookDamage
            || type == OUTL_StimulusType.SightDanger
            || type == OUTL_StimulusType.Combat
            || type == OUTL_StimulusType.Death
            || type == OUTL_StimulusType.Fear
            || type == OUTL_StimulusType.Fire
            || type == OUTL_StimulusType.Territory
            || type == OUTL_StimulusType.Social
            || type == OUTL_StimulusType.Alert
            || type == OUTL_StimulusType.Egregore
            || type == OUTL_StimulusType.Scripted;
    }

    private bool ShouldIgnore(string key)
    {
        if (IgnoreKeys == null || IgnoreKeys.Length == 0 || string.IsNullOrEmpty(key)) return false;
        for (int i = 0; i < IgnoreKeys.Length; i++)
            if (IgnoreKeys[i] == key)
                return true;
        return false;
    }
}
