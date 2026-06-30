using UnityEngine;

[CreateAssetMenu(menuName = "OUT CORE Lite/Player/Motor Profile", fileName = "OUTL_PlayerMotorProfile")]
public sealed class OUTL_PlayerMotorProfile : ScriptableObject
{
    [Header("GoldSrc Scale")]
    public bool UseGoldSrcUnits = true;
    public float GoldSrcUnitsPerUnityUnit = 32f;

    [Header("Movement")]
    public float ForwardSpeed = 320f;
    public float SideSpeed = 320f;
    public float BackSpeed = 320f;
    public float WalkSpeed = 150f;
    public float RunSpeed = 320f;
    public bool HoldShiftToWalk = true;
    public float CrouchSpeedMultiplier = 0.333f;
    public float GroundAcceleration = 10f;
    public float AirAcceleration = 10f;
    public float AirWishSpeedCap = 30f;
    public float Friction = 4f;
    public float StopSpeed = 100f;
    public float JumpSpeed = 270f;

    [Header("Gravity")]
    public float Gravity = 981f;
    public float GravityMultiplier = 1.65f;
    public float RisingGravityMultiplier = 1.0f;
    public float FallingGravityMultiplier = 1.75f;
    public float LowJumpGravityMultiplier = 2.25f;
    public bool ApplyLowJumpGravityWhenJumpReleased = true;
    public float MaxFallSpeed = 54f;

    [Header("Ground")]
    public float GroundProbeExtraDistance = 0.20f;
    public float GroundSnapDistance = 0.24f;
    public float StepOffset = 0.35f;
    public float SlopeLimit = 45f;
    public float GroundStickSpeed = 8f;
    public float StableGroundUpSpeed = 2.5f;
    public bool ProjectMoveOnGroundPlane = true;
    public bool CancelDownhillSlideOnWalkableGround = true;
    public float SlopeSlideStartAngle = 50f;
    public bool SkipFrictionOnJumpFrame = true;
    public float JumpGroundLockout = 0.12f;

    [Header("Fall Damage")]
    public bool EnableFallDamage = true;
    public float FallDamageMinSpeed = 18f;
    public float FallDamageFatalSpeed = 32f;
    public float FallDamageScale = 7f;
    public float FallDamageMaxDamage = 100f;
    public string FallDamageKey = "fall";
    public float LandingSoundMinFallSpeed = 5f;

    [Header("Weapon Input")]
    public bool EnableWeaponSlotKeys = true;
    public bool EnableMouseWheelWeaponCycle = true;
    public OUTL_EquipmentSlot ActiveWeaponDefaultSlot = OUTL_EquipmentSlot.Primary;
    public KeyCode PrimarySlotKey = KeyCode.Alpha1;
    public KeyCode SecondarySlotKey = KeyCode.Alpha2;
    public KeyCode MeleeSlotKey = KeyCode.Alpha3;
    public KeyCode UtilitySlotKey = KeyCode.Alpha4;

    public void ApplyTo(OUTL_BasicPlayerController controller)
    {
        if (controller == null) return;

        controller.UseGoldSrcUnits = UseGoldSrcUnits;
        controller.GoldSrcUnitsPerUnityUnit = Mathf.Max(1f, GoldSrcUnitsPerUnityUnit);

        controller.ForwardSpeed = ForwardSpeed;
        controller.SideSpeed = SideSpeed;
        controller.BackSpeed = BackSpeed;
        controller.WalkSpeed = WalkSpeed;
        controller.RunSpeed = RunSpeed;
        controller.HoldShiftToWalk = HoldShiftToWalk;
        controller.CrouchSpeedMultiplier = CrouchSpeedMultiplier;
        controller.GroundAcceleration = GroundAcceleration;
        controller.AirAcceleration = AirAcceleration;
        controller.AirWishSpeedCap = AirWishSpeedCap;
        controller.Friction = Friction;
        controller.StopSpeed = StopSpeed;
        controller.JumpSpeed = JumpSpeed;

        controller.Gravity = Gravity;
        controller.GravityMultiplier = GravityMultiplier;
        controller.RisingGravityMultiplier = RisingGravityMultiplier;
        controller.FallingGravityMultiplier = FallingGravityMultiplier;
        controller.LowJumpGravityMultiplier = LowJumpGravityMultiplier;
        controller.ApplyLowJumpGravityWhenJumpReleased = ApplyLowJumpGravityWhenJumpReleased;
        controller.MaxFallSpeed = MaxFallSpeed;

        controller.GroundProbeExtraDistance = GroundProbeExtraDistance;
        controller.GroundSnapDistance = GroundSnapDistance;
        controller.StepOffset = StepOffset;
        controller.SlopeLimit = SlopeLimit;
        controller.GroundStickSpeed = GroundStickSpeed;
        controller.StableGroundUpSpeed = StableGroundUpSpeed;
        controller.ProjectMoveOnGroundPlane = ProjectMoveOnGroundPlane;
        controller.CancelDownhillSlideOnWalkableGround = CancelDownhillSlideOnWalkableGround;
        controller.SlopeSlideStartAngle = SlopeSlideStartAngle;
        controller.SkipFrictionOnJumpFrame = SkipFrictionOnJumpFrame;
        controller.JumpGroundLockout = JumpGroundLockout;

        controller.EnableFallDamage = EnableFallDamage;
        controller.FallDamageMinSpeed = FallDamageMinSpeed;
        controller.FallDamageFatalSpeed = FallDamageFatalSpeed;
        controller.FallDamageScale = FallDamageScale;
        controller.FallDamageMaxDamage = FallDamageMaxDamage;
        controller.FallDamageKey = FallDamageKey;
        controller.LandingSoundMinFallSpeed = LandingSoundMinFallSpeed;

        controller.EnableWeaponSlotKeys = EnableWeaponSlotKeys;
        controller.EnableMouseWheelWeaponCycle = EnableMouseWheelWeaponCycle;
        controller.ActiveWeaponDefaultSlot = ActiveWeaponDefaultSlot;
        controller.PrimarySlotKey = PrimarySlotKey;
        controller.SecondarySlotKey = SecondarySlotKey;
        controller.MeleeSlotKey = MeleeSlotKey;
        controller.UtilitySlotKey = UtilitySlotKey;
    }

    public void ApplyTo(OUTL_CharacterControllerInputSink controller)
    {
        if (controller == null) return;

        controller.UseGoldSrcUnits = UseGoldSrcUnits;
        controller.GoldSrcUnitsPerUnityUnit = Mathf.Max(1f, GoldSrcUnitsPerUnityUnit);

        controller.ForwardSpeed = ForwardSpeed;
        controller.SideSpeed = SideSpeed;
        controller.BackSpeed = BackSpeed;
        controller.WalkSpeed = WalkSpeed;
        controller.RunSpeed = RunSpeed;
        controller.HoldShiftToWalk = HoldShiftToWalk;
        controller.CrouchSpeedMultiplier = CrouchSpeedMultiplier;
        controller.GroundAcceleration = GroundAcceleration;
        controller.AirAcceleration = AirAcceleration;
        controller.AirWishSpeedCap = AirWishSpeedCap;
        controller.Friction = Friction;
        controller.StopSpeed = StopSpeed;
        controller.JumpSpeed = JumpSpeed;

        controller.Gravity = Gravity;
        controller.GravityMultiplier = GravityMultiplier;
        controller.RisingGravityMultiplier = RisingGravityMultiplier;
        controller.FallingGravityMultiplier = FallingGravityMultiplier;
        controller.LowJumpGravityMultiplier = LowJumpGravityMultiplier;
        controller.ApplyLowJumpGravityWhenJumpReleased = ApplyLowJumpGravityWhenJumpReleased;
        controller.MaxFallSpeed = MaxFallSpeed;

        controller.GroundProbeExtraDistance = GroundProbeExtraDistance;
        controller.GroundSnapDistance = GroundSnapDistance;
        controller.StepOffset = StepOffset;
        controller.SlopeLimit = SlopeLimit;
        controller.GroundStickSpeed = GroundStickSpeed;
        controller.StableGroundUpSpeed = StableGroundUpSpeed;
        controller.ProjectMoveOnGroundPlane = ProjectMoveOnGroundPlane;
        controller.CancelDownhillSlideOnWalkableGround = CancelDownhillSlideOnWalkableGround;
        controller.SlopeSlideStartAngle = SlopeSlideStartAngle;
        controller.SkipFrictionOnJumpFrame = SkipFrictionOnJumpFrame;
        controller.JumpGroundLockout = JumpGroundLockout;
    }
}
