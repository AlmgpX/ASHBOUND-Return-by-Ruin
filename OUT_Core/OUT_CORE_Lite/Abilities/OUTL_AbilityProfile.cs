using UnityEngine;

[CreateAssetMenu(menuName = "OUT CORE Lite/Abilities/Ability Profile", fileName = "OUTL_AbilityProfile")]
public class OUTL_AbilityProfile : ScriptableObject
{
    public string AbilityId = "ability";
    public int AbilitySlot = 0;
    public float Cooldown = 1f;
    public float MinRange = 0f;
    public float MaxRange = 8f;
    public bool RequiresGround = true;
    public bool RequiresLineOfSight = true;
    public float WindupTime = 0.1f;
    public float RecoveryTime = 0.25f;
    public bool Interruptible = true;
    public OUTL_StimulusType StimulusOnUse = OUTL_StimulusType.Combat;
    public string[] Tags;
}
