using UnityEngine;

[CreateAssetMenu(menuName = "OUT CORE Lite/Core/Tick Profile", fileName = "OUTL_TickProfile")]
public sealed class OUTL_TickProfile : ScriptableObject
{
    public float logicInterval = 0.10f;
    public float aiNearInterval = 0.10f;
    public float aiMidInterval = 0.50f;
    public float aiFarInterval = 1.50f;
    public float aiDormantInterval = 3.00f;
    public float questInterval = 0.50f;
    public float stimulusInterval = 0.25f;
    public float chunkProcessingInterval = 0.30f;
    public float egregoreLocalInterval = 2.00f;
    public float egregoreRegionalInterval = 5.00f;
    public float egregoreWorldInterval = 10.00f;
    public float npcFullInterval = 0.05f;
    public float npcNearInterval = 0.25f;
    public float npcMidInterval = 2.00f;
    public float npcFarInterval = 10.00f;
    public float npcDormantInterval = 60.00f;
    public int maxAITicksPerFrame = 64;
    public int maxStimuliProcessedPerFrame = 256;
    public int maxSectorUpdatesPerFrame = 128;
    public int maxEgregoreSignalsPerFrame = 64;
    public int maxNpcBehaviorTicksPerFrame = 128;
    public int maxNpcRouteUpdatesPerFrame = 64;
    public int maxNpcPathRequestsPerFrame = 16;
    public int maxNpcStimulusInterruptsPerFrame = 64;

    public void Sanitize()
    {
        logicInterval = Mathf.Max(0.001f, logicInterval);
        aiNearInterval = Mathf.Max(0.001f, aiNearInterval);
        aiMidInterval = Mathf.Max(aiNearInterval, aiMidInterval);
        aiFarInterval = Mathf.Max(aiMidInterval, aiFarInterval);
        aiDormantInterval = Mathf.Max(aiFarInterval, aiDormantInterval);
        questInterval = Mathf.Max(0.001f, questInterval);
        stimulusInterval = Mathf.Max(0.001f, stimulusInterval);
        chunkProcessingInterval = Mathf.Max(0.001f, chunkProcessingInterval);
        egregoreLocalInterval = Mathf.Max(0.001f, egregoreLocalInterval);
        egregoreRegionalInterval = Mathf.Max(egregoreLocalInterval, egregoreRegionalInterval);
        egregoreWorldInterval = Mathf.Max(egregoreRegionalInterval, egregoreWorldInterval);
        npcFullInterval = Mathf.Clamp(npcFullInterval, 0.01f, 0.5f);
        npcNearInterval = Mathf.Clamp(npcNearInterval, npcFullInterval, 1f);
        npcMidInterval = Mathf.Clamp(npcMidInterval, npcNearInterval, 10f);
        npcFarInterval = Mathf.Clamp(npcFarInterval, npcMidInterval, 60f);
        npcDormantInterval = Mathf.Clamp(npcDormantInterval, npcFarInterval, 240f);
        maxAITicksPerFrame = Mathf.Max(0, maxAITicksPerFrame);
        maxStimuliProcessedPerFrame = Mathf.Max(0, maxStimuliProcessedPerFrame);
        maxSectorUpdatesPerFrame = Mathf.Max(0, maxSectorUpdatesPerFrame);
        maxEgregoreSignalsPerFrame = Mathf.Max(0, maxEgregoreSignalsPerFrame);
        maxNpcBehaviorTicksPerFrame = Mathf.Max(0, maxNpcBehaviorTicksPerFrame);
        maxNpcRouteUpdatesPerFrame = Mathf.Max(0, maxNpcRouteUpdatesPerFrame);
        maxNpcPathRequestsPerFrame = Mathf.Max(0, maxNpcPathRequestsPerFrame);
        maxNpcStimulusInterruptsPerFrame = Mathf.Max(0, maxNpcStimulusInterruptsPerFrame);
    }
}
