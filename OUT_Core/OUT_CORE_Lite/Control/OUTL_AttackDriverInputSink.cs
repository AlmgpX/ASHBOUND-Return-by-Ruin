using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_AttackDriverInputSink : MonoBehaviour, OUTL_IActorInputPhasedSink
{
    public OUTL_EntityAdapter Entity;
    public OUTL_AttackDriver AttackDriver;
    public OUTL_EquipmentSlot ActiveSlot = OUTL_EquipmentSlot.Primary;
    public bool FireHeldPrimary = true;
    public bool RequireFireAuthorization = true;
    public bool NotifyAnimation = true;
    public OUTL_CharacterAnimationBridge AnimationBridge;
    public string LastBlockedReason = "";

    public OUTL_ActorInputPhase Phase { get { return OUTL_ActorInputPhase.Weapon; } }

    private void Awake()
    {
        Resolve();
    }

    public void OUTL_ApplyInput(in OUTL_ActorInputFrame frame, OUTL_World world)
    {
        Resolve();
        if (AttackDriver == null || IsDead()) return;
        if (frame.WeaponSlot >= 0) ActiveSlot = ClampSlot(frame.WeaponSlot);

        bool fired = false;
        if (frame.FireSecondaryPressed)
            fired = FireSlot(OUTL_EquipmentSlot.Secondary, frame);
        else if (frame.FirePrimaryPressed || (FireHeldPrimary && frame.FirePrimaryHeld))
            fired = FireSlot(ActiveSlot, frame);

        if (fired && NotifyAnimation && AnimationBridge != null)
            AnimationBridge.NotifyAttack(ActiveSlot);
    }

    private bool FireSlot(OUTL_EquipmentSlot slot, in OUTL_ActorInputFrame frame)
    {
        LastBlockedReason = "";
        if (RequireFireAuthorization && !frame.FireAuthorized)
        {
            LastBlockedReason = "fire_not_authorized";
            return false;
        }
        OUTL_AttackProfile profile = ResolveProfile(slot);
        if (profile == null) return false;
        ActiveSlot = slot;
        if (frame.HasAimWorldPoint)
        {
            if (!CanFireAtAngle(frame))
            {
                LastBlockedReason = "aim_angle";
                return false;
            }
            return AttackDriver.FireAt(profile, frame.AimWorldPoint);
        }
        switch (slot)
        {
            case OUTL_EquipmentSlot.Secondary: return AttackDriver.FireSecondary();
            case OUTL_EquipmentSlot.Melee: return AttackDriver.FireMelee();
            case OUTL_EquipmentSlot.Primary:
            default: return AttackDriver.FirePrimary();
        }
    }

    private bool CanFireAtAngle(in OUTL_ActorInputFrame frame)
    {
        float maxAngle = frame.MaxAllowedFireAngle > 0f ? frame.MaxAllowedFireAngle : 180f;
        if (maxAngle >= 179.9f) return true;
        Transform aimRoot = AttackDriver != null && AttackDriver.Muzzle != null ? AttackDriver.Muzzle : transform;
        Vector3 toAim = frame.AimWorldPoint - aimRoot.position;
        if (toAim.sqrMagnitude <= 0.0001f) return false;
        float angle = Vector3.Angle(aimRoot.forward, toAim.normalized);
        return angle <= maxAngle;
    }

    private OUTL_AttackProfile ResolveProfile(OUTL_EquipmentSlot slot)
    {
        if (AttackDriver == null) return null;
        switch (slot)
        {
            case OUTL_EquipmentSlot.Secondary: return AttackDriver.Secondary != null ? AttackDriver.Secondary : AttackDriver.Primary;
            case OUTL_EquipmentSlot.Melee: return AttackDriver.Melee != null ? AttackDriver.Melee : AttackDriver.Primary;
            case OUTL_EquipmentSlot.Primary:
            default: return AttackDriver.Primary != null ? AttackDriver.Primary : (AttackDriver.Melee != null ? AttackDriver.Melee : AttackDriver.Secondary);
        }
    }

    private OUTL_EquipmentSlot ClampSlot(int slot)
    {
        if (slot == (int)OUTL_EquipmentSlot.Secondary) return OUTL_EquipmentSlot.Secondary;
        if (slot == (int)OUTL_EquipmentSlot.Melee) return OUTL_EquipmentSlot.Melee;
        return OUTL_EquipmentSlot.Primary;
    }

    private bool IsDead()
    {
        OUTL_EntityRuntime runtime = Entity != null ? Entity.Runtime : null;
        return runtime != null && (runtime.Dead || runtime.LifeState == OUTL_LifeState.Dead || runtime.State.GetFlag(OUTL_StateId.Dead) || runtime.Stats.Get(OUTL_StatId.Health, 1f) <= 0f);
    }

    private void Resolve()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (AttackDriver == null) AttackDriver = GetComponent<OUTL_AttackDriver>();
        if (AnimationBridge == null) AnimationBridge = GetComponentInChildren<OUTL_CharacterAnimationBridge>(true);
    }
}
