using UnityEngine;

[DisallowMultipleComponent]
public class OUT_AIHearingSensor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private OUT_AIMemoryBuffer memoryBuffer;

    [Header("Hearing")]
    [SerializeField] private bool enableHearing = true;
    [SerializeField] [Range(0f, 1f)] private float hearingThreshold = 0.18f;
    [SerializeField] [Range(0f, 1f)] private float dangerThreshold = 0.12f;
    [SerializeField] [Range(0f, 1f)] private float combatThreshold = 0.25f;
    [SerializeField] [Range(0f, 1f)] private float maskingMargin = 0.08f;

    [Header("Channels")]
    [SerializeField] private OUT_SensoryChannelFlags worldNoiseChannels = OUT_SensoryChannelFlags.Noise;
    [SerializeField] private OUT_SensoryChannelFlags dangerChannels = OUT_SensoryChannelFlags.Danger | OUT_SensoryChannelFlags.Fire;

    [Header("Behaviour")]
    [SerializeField] private bool writeInterestPoint = true;
    [SerializeField] private bool rememberHeardStimuli = true;
    [SerializeField] private bool logHeardStimuli = false;

    public void Evaluate(OUT_AIBlackboard blackboard, ref OUT_AIConditionFlags conditions)
    {
        if (!enableHearing || OUT_SceneStimulusService.Instance == null)
            return;

        Vector3 position = transform.position;
        OUT_SceneStimulusService service = OUT_SceneStimulusService.Instance;

        bool heardSomething = false;
        OUT_SceneStimulusService.OUT_StimulusHit bestHit = default;
        OUT_AIConditionFlags heardFlags = OUT_AIConditionFlags.None;

        if (service.TryGetStrongestStimulus(position, dangerChannels, out OUT_SceneStimulusService.OUT_StimulusHit dangerHit))
        {
            if (dangerHit.Strength >= dangerThreshold)
            {
                heardSomething = true;
                bestHit = dangerHit;
                heardFlags |= OUT_AIConditionFlags.HearDanger;

                if ((dangerHit.Channels & OUT_SensoryChannelFlags.Fire) != 0 || dangerHit.Strength >= combatThreshold)
                    heardFlags |= OUT_AIConditionFlags.HearCombat;
            }
        }

        if (service.TryGetStrongestStimulus(position, worldNoiseChannels, out OUT_SceneStimulusService.OUT_StimulusHit noiseHit))
        {
            float localNoiseFloor = service.Sample(position, OUT_SensoryChannelFlags.Noise).Noise;
            bool passesThreshold = noiseHit.Strength >= hearingThreshold;
            bool passesMasking = noiseHit.Strength >= localNoiseFloor + maskingMargin || noiseHit.Strength >= combatThreshold;

            if (passesThreshold && passesMasking)
            {
                if (!heardSomething || noiseHit.Strength > bestHit.Strength)
                    bestHit = noiseHit;

                heardSomething = true;
                heardFlags |= OUT_AIConditionFlags.HearWorld;

                if (noiseHit.Strength >= combatThreshold)
                    heardFlags |= OUT_AIConditionFlags.HearCombat;
            }
        }

        if (!heardSomething)
            return;

        conditions |= heardFlags;

        if (writeInterestPoint && blackboard != null)
        {
            blackboard.InterestPoint = bestHit.Position;
            blackboard.InterestStrength = bestHit.Strength;
        }

        if (rememberHeardStimuli && memoryBuffer != null)
        {
            if ((heardFlags & (OUT_AIConditionFlags.HearDanger | OUT_AIConditionFlags.HearCombat)) != 0)
                memoryBuffer.ObserveDanger(bestHit.Position, bestHit.Strength, (int)heardFlags);
            else
                memoryBuffer.ObserveInterest(bestHit.Position, bestHit.Strength, (int)heardFlags);
        }

        if (logHeardStimuli)
        {
            OUT_AIDebugLogService.Log(this, OUT_AIDebugLogService.AIEventKind.Hearing,
                $"heard {bestHit.Channels} at {bestHit.Position} strength:{bestHit.Strength:0.00} flags:{heardFlags}");
        }
    }

    private void Awake()
    {
        if (memoryBuffer == null)
            memoryBuffer = GetComponent<OUT_AIMemoryBuffer>();
    }
}
