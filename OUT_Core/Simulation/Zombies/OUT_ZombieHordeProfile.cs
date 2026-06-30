using UnityEngine;

[CreateAssetMenu(menuName = "OUT/Core/Zombies/Horde Profile", fileName = "OUT_ZombieHordeProfile")]
public class OUT_ZombieHordeProfile : ScriptableObject
{
    [Header("Movement")]
    [Min(0f)] public float WalkSpeed = 1.8f;
    [Min(0f)] public float RunSpeed = 4.2f;
    [Min(0f)] public float RotationSpeed = 540f;
    [Min(0f)] public float StopDistance = 1.25f;
    [Min(0f)] public float GroundRayHeight = 1.4f;
    [Min(0f)] public float GroundRayDistance = 3f;
    public LayerMask GroundMask = ~0;

    [Header("Targeting")]
    [Min(0.02f)] public float TargetRefreshInterval = 0.35f;
    [Min(0f)] public float RetargetDistanceBias = 12f;
    [Min(0f)] public float ObjectiveBias = 30f;

    [Header("Attack")]
    [Min(0f)] public float AttackRange = 1.45f;
    [Min(0f)] public float AttackInterval = 0.85f;
    [Min(0)] public int AttackDamage = 8;

    [Header("Health")]
    [Min(1)] public int MaxHealth = 35;
    public bool HideInsteadOfDestroy = true;

    [Header("LOD")]
    [Min(0f)] public float NearDistance = 45f;
    [Min(0f)] public float MidDistance = 160f;
    [Min(0f)] public float FarDistance = 420f;
    [Min(0.02f)] public float NearThinkInterval = 0.05f;
    [Min(0.05f)] public float MidThinkInterval = 0.25f;
    [Min(0.1f)] public float FarThinkInterval = 1.0f;

    [Header("Animation Params")]
    public string AnimatorSpeedFloat = "Speed";
    public string AnimatorAttackTrigger = "Attack";
    public string AnimatorHitTrigger = "Hit";
    public string AnimatorDeathTrigger = "Death";
    public string AnimatorAliveBool = "Alive";

    [Header("Audio")]
    [Min(0f)] public float AudioMaxDistance = 45f;
    [Min(0f)] public float MoanMinInterval = 3f;
    [Min(0f)] public float MoanMaxInterval = 9f;
    [Range(0f, 1f)] public float MoanChance = 0.2f;

    [Header("VFX")]
    public GameObject GibVFX;
    public GameObject HitVFX;
}
