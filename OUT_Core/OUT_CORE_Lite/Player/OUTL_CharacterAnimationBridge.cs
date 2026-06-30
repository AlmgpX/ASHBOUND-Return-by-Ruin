using UnityEngine;

[DisallowMultipleComponent]
public class OUTL_CharacterAnimationBridge : MonoBehaviour
{
    public Animator Animator;
    public Transform VisualRoot;
    public bool RotateVisualToMoveDirection = false;
    public float VisualRotationSpeed = 720f;

    [Header("Animator Float Params")]
    public string SpeedParam = "Speed";
    public string ForwardParam = "Forward";
    public string StrafeParam = "Strafe";
    public string VerticalSpeedParam = "VerticalSpeed";
    public string AimPitchParam = "AimPitch";
    public string AimYawParam = "AimYaw";
    public string GroundSlopeParam = "GroundSlope";
    public string FallSpeedParam = "FallSpeed";
    public string FallDamageParam = "FallDamage";

    [Header("Animator Bool Params")]
    public string GroundedParam = "Grounded";
    public string CrouchParam = "Crouch";
    public string DeadParam = "Dead";
    public string FallingParam = "Falling";

    [Header("Animator Int Params")]
    public string WeaponSlotParam = "WeaponSlot";

    [Header("Animator Trigger Params")]
    public string JumpTrigger = "Jump";
    public string AttackTrigger = "Attack";
    public string LandTrigger = "Land";
    public string WeaponChangedTrigger = "WeaponChanged";
    public string HurtTrigger = "Hurt";
    public string DeathTrigger = "Death";

    private int speedHash;
    private int forwardHash;
    private int strafeHash;
    private int verticalSpeedHash;
    private int aimPitchHash;
    private int aimYawHash;
    private int groundSlopeHash;
    private int fallSpeedHash;
    private int fallDamageHash;
    private int groundedHash;
    private int crouchHash;
    private int deadHash;
    private int fallingHash;
    private int weaponSlotHash;
    private int jumpHash;
    private int attackHash;
    private int landHash;
    private int weaponChangedHash;
    private int hurtHash;
    private int deathHash;

    private void Awake()
    {
        if (Animator == null) Animator = GetComponentInChildren<Animator>(true);
        if (VisualRoot == null && Animator != null) VisualRoot = Animator.transform;
        CacheHashes();
    }

    private void OnValidate()
    {
        CacheHashes();
    }

    private void CacheHashes()
    {
        speedHash = Hash(SpeedParam);
        forwardHash = Hash(ForwardParam);
        strafeHash = Hash(StrafeParam);
        verticalSpeedHash = Hash(VerticalSpeedParam);
        aimPitchHash = Hash(AimPitchParam);
        aimYawHash = Hash(AimYawParam);
        groundSlopeHash = Hash(GroundSlopeParam);
        fallSpeedHash = Hash(FallSpeedParam);
        fallDamageHash = Hash(FallDamageParam);
        groundedHash = Hash(GroundedParam);
        crouchHash = Hash(CrouchParam);
        deadHash = Hash(DeadParam);
        fallingHash = Hash(FallingParam);
        weaponSlotHash = Hash(WeaponSlotParam);
        jumpHash = Hash(JumpTrigger);
        attackHash = Hash(AttackTrigger);
        landHash = Hash(LandTrigger);
        weaponChangedHash = Hash(WeaponChangedTrigger);
        hurtHash = Hash(HurtTrigger);
        deathHash = Hash(DeathTrigger);
    }

    public void PushLocomotion(OUTL_BasicPlayerController controller)
    {
        if (Animator == null || controller == null) return;
        PushLocomotionValues(
            controller.transform,
            controller.Velocity,
            controller.ViewPitch,
            controller.ViewYaw,
            controller.GroundSlopeAngle,
            controller.LastFallSpeed,
            controller.LastFallDamage,
            controller.IsGrounded,
            controller.IsCrouching,
            controller.ActiveWeaponSlot);
    }

    public void PushLocomotionValues(Transform actorRoot, Vector3 velocity, float viewPitch, float viewYaw, float groundSlopeAngle, float fallSpeed, float fallDamage, bool grounded, bool crouching, OUTL_EquipmentSlot activeWeaponSlot)
    {
        if (Animator == null) return;
        Vector3 local = actorRoot != null ? actorRoot.InverseTransformDirection(velocity) : velocity;
        Vector3 flat = velocity;
        flat.y = 0f;

        SetFloat(speedHash, flat.magnitude);
        SetFloat(forwardHash, local.z);
        SetFloat(strafeHash, local.x);
        SetFloat(verticalSpeedHash, velocity.y);
        SetFloat(aimPitchHash, viewPitch);
        SetFloat(aimYawHash, viewYaw);
        SetFloat(groundSlopeHash, groundSlopeAngle);
        SetFloat(fallSpeedHash, fallSpeed);
        SetFloat(fallDamageHash, fallDamage);
        SetBool(groundedHash, grounded);
        SetBool(crouchHash, crouching);
        SetBool(fallingHash, !grounded && velocity.y < 0f);
        SetInteger(weaponSlotHash, (int)activeWeaponSlot);

        if (RotateVisualToMoveDirection && VisualRoot != null && flat.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(flat.normalized, Vector3.up);
            VisualRoot.rotation = Quaternion.RotateTowards(VisualRoot.rotation, target, VisualRotationSpeed * Time.deltaTime);
        }
    }

    public void PushAI(OUTL_AIActor actor)
    {
        if (Animator == null || actor == null) return;
        Vector3 velocity = Vector3.zero;
        OUTL_NavMeshMover mover = actor.NavMover;
        if (mover != null && mover.Agent != null && mover.Agent.enabled)
            velocity = mover.Agent.velocity;

        Vector3 local = actor.transform.InverseTransformDirection(velocity);
        Vector3 flat = velocity;
        flat.y = 0f;

        SetFloat(speedHash, flat.magnitude);
        SetFloat(forwardHash, local.z);
        SetFloat(strafeHash, local.x);
        SetFloat(verticalSpeedHash, velocity.y);
        SetBool(groundedHash, true);
    }

    public void NotifyJump() { SetTrigger(jumpHash); }
    public void NotifyAttack() { SetTrigger(attackHash); }
    public void NotifyAttack(OUTL_EquipmentSlot slot) { SetInteger(weaponSlotHash, (int)slot); SetTrigger(attackHash); }
    public void NotifyWeaponChanged(OUTL_EquipmentSlot slot) { SetInteger(weaponSlotHash, (int)slot); SetTrigger(weaponChangedHash); }
    public void NotifyLand(float fallSpeed, float damage)
    {
        SetFloat(fallSpeedHash, fallSpeed);
        SetFloat(fallDamageHash, damage);
        SetTrigger(landHash);
    }
    public void NotifyHurt() { SetTrigger(hurtHash); }

    public void NotifyDeath()
    {
        SetBool(deadHash, true);
        SetTrigger(deathHash);
    }

    private static int Hash(string name)
    {
        return string.IsNullOrEmpty(name) ? 0 : Animator.StringToHash(name);
    }

    private void SetFloat(int hash, float value)
    {
        if (Animator != null && hash != 0) Animator.SetFloat(hash, value);
    }

    private void SetBool(int hash, bool value)
    {
        if (Animator != null && hash != 0) Animator.SetBool(hash, value);
    }

    private void SetInteger(int hash, int value)
    {
        if (Animator != null && hash != 0) Animator.SetInteger(hash, value);
    }

    private void SetTrigger(int hash)
    {
        if (Animator != null && hash != 0) Animator.SetTrigger(hash);
    }
}
