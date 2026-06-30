using UnityEngine;

[CreateAssetMenu(menuName = "OUT CORE Lite/NPC/Navigation Profile", fileName = "OUTL_NPCNavigationProfile")]
public sealed class OUTL_NPCNavigationProfile : ScriptableObject
{
    public string ProfileId = "npc_nav";
    public float WalkSpeed = 2.2f;
    public float RunSpeed = 4.2f;
    [Range(0f, 1f)] public float RoadPreference = 0.5f;
    [Range(0f, 4f)] public float AvoidDangerWeight = 1f;
    [Range(0f, 4f)] public float FactionTerritoryWeight = 1f;
    public bool CanUseAbstractTravel = true;
    public bool CanTeleportWhenDormant = false;
    public float MaxPathRequestRate = 0.25f;
    public float RepathCooldown = 2f;
    public float StuckTimeout = 6f;
    public float NavAgentRadius = 0.35f;
    public float NavAgentHeight = 1.8f;
    public float AbstractTravelSpeedMultiplier = 1f;
    public bool MaterializeTransformOnNear = true;
    public bool UpdateTransformWhileAbstract = true;

    public void Sanitize()
    {
        WalkSpeed = Mathf.Max(0.01f, WalkSpeed);
        RunSpeed = Mathf.Max(WalkSpeed, RunSpeed);
        MaxPathRequestRate = Mathf.Max(0f, MaxPathRequestRate);
        RepathCooldown = Mathf.Max(0.05f, RepathCooldown);
        StuckTimeout = Mathf.Max(0.1f, StuckTimeout);
        NavAgentRadius = Mathf.Max(0.01f, NavAgentRadius);
        NavAgentHeight = Mathf.Max(0.1f, NavAgentHeight);
        AbstractTravelSpeedMultiplier = Mathf.Max(0.01f, AbstractTravelSpeedMultiplier);
    }
}
