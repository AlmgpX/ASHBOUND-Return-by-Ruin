using UnityEngine;

public enum OUTL_NPCBehaviorArchetype
{
    Generic = 0,
    MerchantTraveler = 1,
    BanditPatrol = 2,
    Guard = 3,
    Wildlife = 4
}

[CreateAssetMenu(menuName = "OUT CORE Lite/NPC/Behavior Model", fileName = "OUTL_NPCBehaviorModel")]
public sealed class OUTL_NPCBehaviorModel : ScriptableObject
{
    public string ModelId = "npc_behavior";
    public OUTL_NPCBehaviorArchetype Archetype = OUTL_NPCBehaviorArchetype.Generic;
    public OUTL_NPCScheduleDef Schedule;
    public OUTL_NPCNavigationProfile NavigationProfile;
    public OUTL_NPCStimulusInterruptPolicy[] InterruptPolicies;
    public bool UseAIActorForNearTactics = true;
    public bool ResumeScheduleAfterInterrupt = true;
    public float DayLengthSeconds = 1440f;
    public float StimulusRadius = 24f;
    public float StimulusMinimumPriority = 0.15f;
    public int StimulusBudget = 8;
    public string Role = "generic";
}
