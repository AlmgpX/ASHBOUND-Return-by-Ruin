using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_AimPlanner : MonoBehaviour
{
    public OUTL_EntityAdapter Entity;
    public OUTL_AttackDriver AttackDriver;
    public OUTL_AimProfile Profile;
    public OUTL_AimState CurrentState;

    private OUTL_EntityId lastTarget = OUTL_EntityId.None;
    private OUTL_TacticalIntentId lastIntent = OUTL_TacticalIntentId.None;
    private float targetSeenTime;
    private float fireAllowedTime;
    private float stableStartTime;

    private void Awake()
    {
        Resolve();
    }

    public OUTL_AimState Plan(OUTL_World world, OUTL_TacticalDecision decision, OUTL_EntityRuntime target, OUTL_AttackProfile profile, float time, float deltaTime, OUTL_FactionDisciplineProfile disciplineOverride)
    {
        Resolve();
        OUTL_AimState state = default(OUTL_AimState);
        state.Command = OUTL_AimCommand.HoldFire;
        state.ReasonCode = OUTL_AimReasonCode.NoTarget;
        state.Reason = "no_target";

        if (decision.WantsReload)
        {
            state.Command = OUTL_AimCommand.Reload;
            state.ReasonCode = OUTL_AimReasonCode.Reload;
            state.Reason = "reload";
            CurrentState = state;
            return state;
        }

        if (decision.WantsSwitchWeapon)
        {
            state.Command = OUTL_AimCommand.SwitchWeapon;
            state.ReasonCode = OUTL_AimReasonCode.SwitchWeapon;
            state.Reason = "switch_weapon";
            CurrentState = state;
            return state;
        }

        if (target == null || target.Adapter == null || Entity == null || Entity.Runtime == null || profile == null)
        {
            CurrentState = state;
            return state;
        }

        OUTL_AimProfile aim = Profile;
        Vector3 origin = AttackDriver != null && AttackDriver.Muzzle != null ? AttackDriver.Muzzle.position : transform.position + Vector3.up;
        Vector3 aimPoint = decision.HasAimPoint ? decision.AimPoint : target.Adapter.transform.position + Vector3.up * ReadAimHeight(aim);
        state.AimPoint = aimPoint;
        state.HasAimPoint = true;
        state.TargetAcquiredTime = targetSeenTime;

        if (lastTarget != target.Id || lastIntent != decision.Intent)
        {
            lastTarget = target.Id;
            lastIntent = decision.Intent;
            targetSeenTime = time;
            stableStartTime = time;
            fireAllowedTime = time + RandomDelay(Entity.Id.Value, target.Id.Value, time, aim);
        }
        state.TargetAcquiredTime = targetSeenTime;

        Vector3 toAim = aimPoint - origin;
        Vector3 forward = AttackDriver != null && AttackDriver.Muzzle != null ? AttackDriver.Muzzle.forward : transform.forward;
        float maxError = ReadMaxFireError(aim);
        float dist = toAim.magnitude;
        float allowedByDistance = Mathf.Lerp(ReadAimErrorNear(aim), ReadAimErrorFar(aim), Mathf.Clamp01(dist / 40f));
        maxError = Mathf.Min(maxError, Mathf.Max(0.1f, allowedByDistance));
        state.MaxAllowedFireAngle = maxError;
        state.DesiredYaw = BuildYaw(transform, aimPoint);
        state.DesiredPitch = BuildPitch(origin, aimPoint);
        state.ErrorDegrees = toAim.sqrMagnitude > 0.001f ? Vector3.Angle(forward, toAim.normalized) : 0f;
        if (state.ErrorDegrees <= maxError) state.StableTime = time - stableStartTime;
        else
        {
            stableStartTime = time;
            state.StableTime = 0f;
        }

        state.Confidence = Mathf.Clamp01(1f - state.ErrorDegrees / Mathf.Max(0.1f, maxError * 2f));
        state.FireAllowedTime = fireAllowedTime;
        state.AimLocked = state.ErrorDegrees <= maxError && state.StableTime >= ReadSettleTime(aim, Entity != null ? Entity.Id.Value : 0, target.Id.Value);

        LayerMask lineMask = ReadLineMask(aim);
        if (ReadRequireLineOfSight(aim) && !HasLineOfFire(origin, aimPoint, target, lineMask))
        {
            state.Command = OUTL_AimCommand.AimOnly;
            state.ReasonCode = OUTL_AimReasonCode.NoLineOfFire;
            state.Reason = "no_line_of_fire";
            CurrentState = state;
            return state;
        }

        OUTL_FactionDisciplineProfile discipline = disciplineOverride != null ? disciplineOverride : (aim != null ? aim.DisciplineProfile : null);
        if (ReadUseFriendlyFire(aim))
        {
            state.FriendlyFire = OUTL_FriendlyFireEvaluator.Evaluate(world, Entity, target, origin, aimPoint, Mathf.Max(ReadAimRadius(aim), profile.Radius), lineMask, discipline);
            state.FriendlyFireBlocked = state.FriendlyFire.BlocksFire;
            if (state.FriendlyFireBlocked)
            {
                state.Command = OUTL_AimCommand.HoldFire;
                state.ReasonCode = OUTL_AimReasonCode.FriendlyFire;
                state.Reason = state.FriendlyFire.Reason;
                CurrentState = state;
                return state;
            }
        }

        if (!decision.WantsFire && !decision.WantsSuppress)
        {
            state.Command = OUTL_AimCommand.AimOnly;
            state.ReasonCode = OUTL_AimReasonCode.AimHold;
            state.Reason = "aim_hold";
            CurrentState = state;
            return state;
        }

        if (time < fireAllowedTime || state.StableTime < ReadAimHold(aim) || !state.AimLocked || ShouldFakeOrHoldAim(aim, Entity != null ? Entity.Id.Value : 0, target.Id.Value, time))
        {
            state.Command = OUTL_AimCommand.AimOnly;
            state.ReasonCode = OUTL_AimReasonCode.ReactionOrHold;
            state.Reason = "reaction_or_hold";
            CurrentState = state;
            return state;
        }

        state.Command = decision.WantsSuppress ? OUTL_AimCommand.Suppress : OUTL_AimCommand.FireSingle;
        state.ReasonCode = decision.WantsSuppress ? OUTL_AimReasonCode.Suppress : OUTL_AimReasonCode.Fire;
        state.FireAuthorized = true;
        state.Reason = decision.WantsSuppress ? "suppress" : "fire";
        CurrentState = state;
        return state;
    }

    private static float RandomDelay(int sourceId, int targetId, float time, OUTL_AimProfile aim)
    {
        float reactionMin = aim != null ? Mathf.Max(0f, aim.ReactionDelayMin) : 0.15f;
        float reactionMax = aim != null ? Mathf.Max(reactionMin, aim.ReactionDelayMax) : 0.45f;
        float fireMin = aim != null ? Mathf.Max(0f, aim.FireDelayMin) : 0.05f;
        float fireMax = aim != null ? Mathf.Max(fireMin, aim.FireDelayMax) : 0.25f;
        float reaction = Mathf.Lerp(reactionMin, reactionMax, OUTL_HumanRandom.Value01(0xA11CEu, sourceId, targetId));
        int window = Mathf.FloorToInt(time * 4f);
        float fire = Mathf.Lerp(fireMin, fireMax, OUTL_HumanRandom.Value01(0xF17Eu, sourceId, window));
        return reaction + fire;
    }

    private bool HasLineOfFire(Vector3 origin, Vector3 aimPoint, OUTL_EntityRuntime target, LayerMask mask)
    {
        Vector3 delta = aimPoint - origin;
        float distance = delta.magnitude;
        if (distance <= 0.05f) return true;
        RaycastHit hit;
        if (!Physics.Raycast(origin, delta / distance, out hit, distance, mask, QueryTriggerInteraction.Ignore)) return true;
        OUTL_EntityAdapter hitEntity;
        return OUTL_Combat.TryGetEntityFromCollider(hit.collider, out hitEntity) && target != null && hitEntity != null && hitEntity.Id == target.Id;
    }

    private static float ReadAimHeight(OUTL_AimProfile aim)
    {
        return aim != null ? Mathf.Max(0.1f, aim.AimHeight) : 1.1f;
    }

    private static float ReadAimRadius(OUTL_AimProfile aim)
    {
        return aim != null ? Mathf.Max(0.01f, aim.AimRadius) : 0.25f;
    }

    private static float ReadAimErrorNear(OUTL_AimProfile aim)
    {
        return aim != null ? Mathf.Max(0.1f, aim.AimErrorNear) : 1.5f;
    }

    private static float ReadAimErrorFar(OUTL_AimProfile aim)
    {
        return aim != null ? Mathf.Max(ReadAimErrorNear(aim), aim.AimErrorFar) : 6f;
    }

    private static float ReadAimHold(OUTL_AimProfile aim)
    {
        return aim != null ? Mathf.Max(0f, aim.AimHoldSeconds) : 0.12f;
    }

    private static float ReadSettleTime(OUTL_AimProfile aim, int sourceId, int targetId)
    {
        float min = aim != null ? Mathf.Max(0f, aim.AimSettleTimeMin) : 0.08f;
        float max = aim != null ? Mathf.Max(min, aim.AimSettleTimeMax) : 0.30f;
        return Mathf.Lerp(min, max, OUTL_HumanRandom.Value01(0x5E771Eu, sourceId, targetId));
    }

    private static bool ShouldFakeOrHoldAim(OUTL_AimProfile aim, int sourceId, int targetId, float time)
    {
        if (aim == null) return false;
        int phase = Mathf.FloorToInt(time * 2f);
        float chance = Mathf.Clamp01(aim.HoldAimChance + aim.FakeAimChance);
        return chance > 0f && OUTL_HumanRandom.Value01(0xFA1E5u, sourceId, targetId + phase) < chance;
    }

    private static float ReadMaxFireError(OUTL_AimProfile aim)
    {
        return aim != null ? Mathf.Max(0.1f, aim.MaxFireAngleError) : 8f;
    }

    private static bool ReadRequireLineOfSight(OUTL_AimProfile aim)
    {
        return aim == null || aim.RequireLineOfSight;
    }

    private static bool ReadUseFriendlyFire(OUTL_AimProfile aim)
    {
        return aim == null || aim.UseFriendlyFire;
    }

    private static LayerMask ReadLineMask(OUTL_AimProfile aim)
    {
        return aim != null ? aim.LineOfFireMask : ~0;
    }

    private static float BuildYaw(Transform root, Vector3 aimPoint)
    {
        Vector3 toAim = aimPoint - root.position;
        toAim.y = 0f;
        if (toAim.sqrMagnitude <= 0.0001f) return root.eulerAngles.y;
        return Quaternion.LookRotation(toAim.normalized).eulerAngles.y;
    }

    private static float BuildPitch(Vector3 origin, Vector3 aimPoint)
    {
        Vector3 toAim = aimPoint - origin;
        if (toAim.sqrMagnitude <= 0.0001f) return 0f;
        Vector3 flat = toAim;
        flat.y = 0f;
        return -Mathf.Atan2(toAim.y, flat.magnitude) * Mathf.Rad2Deg;
    }

    private void Resolve()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (AttackDriver == null) AttackDriver = GetComponent<OUTL_AttackDriver>();
    }
}
