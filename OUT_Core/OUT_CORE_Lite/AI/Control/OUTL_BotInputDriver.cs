using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_BotInputDriver : MonoBehaviour, OUTL_IActorInputSource
{
    public OUTL_EntityAdapter Entity;
    public OUTL_AIActor AIActor;
    public OUTL_TacticalPlanner TacticalPlanner;
    public OUTL_AimPlanner AimPlanner;
    public OUTL_AIArsenalSelector Arsenal;
    public OUTL_NPCBehaviorController NPCBehavior;
    public Transform MoveRoot;
    public float StopDistance = 1.25f;
    public float RetreatStepDistance = 6f;
    public bool ProduceInputOnlyNearOrMid = true;
    public bool AllowFire = true;
    public bool UseNPCScheduleMoveIntent = true;
    public OUTL_TacticalDecision LastDecision;
    public OUTL_AimState LastAimState;

    private void Awake()
    {
        Resolve();
    }

    public bool TryBuildInput(OUTL_World world, OUTL_EntityAdapter entity, float time, float deltaTime, ref OUTL_ActorInputFrame frame)
    {
        Resolve();
        if (world == null) world = OUTL_World.Instance;
        if (entity == null) entity = Entity;
        if (entity == null || entity.Runtime == null) return false;
        if (IsDead(entity.Runtime)) return false;
        if (!OUTL_NetworkAuthority.CanAuthoritativeSimulate(entity)) return false;

        OUTL_RuntimeTier tier = entity.Runtime.Tier;
        if (ProduceInputOnlyNearOrMid && (tier == OUTL_RuntimeTier.Far || tier == OUTL_RuntimeTier.Dormant))
            return false;

        OUTL_TacticalDecision decision;
        if (TacticalPlanner != null && TacticalPlanner.TryGetDecision(out decision) && decision.IsValid)
        {
            LastDecision = decision;
        }
        else if (TacticalPlanner != null)
        {
            LastDecision = TacticalPlanner.BuildDecision(world, time, deltaTime);
        }
        else
        {
            LastDecision = BuildFallbackDecision(world, entity, time);
        }

        if (UseNPCScheduleMoveIntent && IsIdleWithoutMove(LastDecision))
        {
            OUTL_TacticalDecision scheduleDecision;
            if (TryBuildScheduleMoveDecision(time, out scheduleDecision))
                LastDecision = scheduleDecision;
        }

        if (!LastDecision.IsValid || LastDecision.Intent == OUTL_TacticalIntentId.Dead) return false;

        Transform root = MoveRoot != null ? MoveRoot : transform;
        frame = OUTL_ActorInputFrame.Empty(time);
        frame.WeaponSlot = (int)LastDecision.WeaponSlot;

        if (LastDecision.HasMoveTarget && AIActor != null && !AIActor.Stationary)
            frame.Move = BuildMoveInput(root, LastDecision);

        if (LastDecision.HasAimPoint)
        {
            frame.AimWorldPoint = LastDecision.AimPoint;
            frame.HasAimWorldPoint = true;
            Vector3 flatAim = LastDecision.AimPoint - root.position;
            flatAim.y = 0f;
            frame.DesiredYaw = flatAim.sqrMagnitude > 0.001f ? Quaternion.LookRotation(flatAim.normalized).eulerAngles.y : root.eulerAngles.y;
            frame.DesiredPitch = 0f;
        }

        if (LastDecision.WantsAbility)
        {
            frame.AbilityPrimaryPressed = true;
            frame.AbilityPrimaryHeld = LastDecision.Intent == OUTL_TacticalIntentId.LeapAttack;
            frame.AbilitySlot = LastDecision.AbilitySlot;
            frame.AbilityTargetPoint = LastDecision.HasAbilityTargetPoint ? LastDecision.AbilityTargetPoint : LastDecision.AimPoint;
            frame.HasAbilityTargetPoint = LastDecision.HasAbilityTargetPoint || LastDecision.HasAimPoint;
        }

        OUTL_EntityRuntime target = ResolveTargetRuntime(world, LastDecision.Target);
        OUTL_AttackProfile attackProfile = LastDecision.AttackProfile;
        if (attackProfile == null && Arsenal != null && Arsenal.CurrentSelection.IsValid) attackProfile = Arsenal.CurrentSelection.AttackProfile;

        OUTL_FactionDisciplineProfile discipline = TacticalPlanner != null && TacticalPlanner.Profile != null ? TacticalPlanner.Profile.DisciplineProfile : null;
        if (AimPlanner != null)
            LastAimState = AimPlanner.Plan(world, LastDecision, target, attackProfile, time, deltaTime, discipline);
        else
            LastAimState = BuildFallbackAimState(LastDecision);

        ApplyAimToFrame(ref frame, LastAimState);
        return frame.HasAnyAction;
    }

    private void ApplyAimToFrame(ref OUTL_ActorInputFrame frame, OUTL_AimState aim)
    {
        if (aim.HasAimPoint)
        {
            frame.AimWorldPoint = aim.AimPoint;
            frame.HasAimWorldPoint = true;
        }

        frame.AimConfidence = aim.Confidence;
        frame.MaxAllowedFireAngle = aim.MaxAllowedFireAngle > 0f ? aim.MaxAllowedFireAngle : frame.MaxAllowedFireAngle;
        frame.DesiredYaw = aim.DesiredYaw;
        frame.DesiredPitch = aim.DesiredPitch;
        frame.HasDesiredView = aim.HasAimPoint;
        frame.FireAuthorized = aim.FireAuthorized;

        if (!AllowFire) return;
        if (aim.Command == OUTL_AimCommand.FireSingle || aim.Command == OUTL_AimCommand.FireBurst)
        {
            if (aim.FireAuthorized)
            {
                frame.FirePrimaryPressed = true;
                frame.FirePrimaryHeld = true;
            }
        }
        else if (aim.Command == OUTL_AimCommand.Suppress)
        {
            if (aim.FireAuthorized) frame.FirePrimaryHeld = true;
        }
        else if (aim.Command == OUTL_AimCommand.Reload)
        {
            frame.ReloadPressed = true;
        }
    }

    private Vector2 BuildMoveInput(Transform root, OUTL_TacticalDecision decision)
    {
        Vector3 desired = Vector3.zero;
        Vector3 to = decision.MoveTarget - root.position;
        to.y = 0f;

        if (decision.MoveMode == OUTL_TacticalMoveMode.RetreatFromTarget)
        {
            if (to.sqrMagnitude <= 0.001f) desired = -root.forward;
            else desired = -to.normalized;
        }
        else if (to.sqrMagnitude > Mathf.Max(0.05f, StopDistance) * Mathf.Max(0.05f, StopDistance))
        {
            desired = to.normalized;
        }

        if (desired.sqrMagnitude <= 0.0001f) return Vector2.zero;
        Vector3 local = root.InverseTransformDirection(desired);
        Vector2 move = new Vector2(local.x, local.z);
        if (move.sqrMagnitude > 1f) move.Normalize();
        return move;
    }

    private OUTL_TacticalDecision BuildFallbackDecision(OUTL_World world, OUTL_EntityAdapter entity, float time)
    {
        if (AIActor == null) return OUTL_TacticalDecision.Idle(time, "no_tactical_planner");
        OUTL_EntityRuntime target = ResolveTargetRuntime(world, AIActor.CurrentTarget);
        if (target != null && target.Adapter != null)
        {
            Vector3 point = target.Adapter.transform.position + Vector3.up;
            return new OUTL_TacticalDecision
            {
                Intent = AIActor.CurrentTargetVisible ? OUTL_TacticalIntentId.AttackRanged : OUTL_TacticalIntentId.Search,
                MoveMode = AIActor.CurrentTargetVisible ? OUTL_TacticalMoveMode.Hold : OUTL_TacticalMoveMode.MoveTo,
                Target = target.Id,
                MoveTarget = point,
                HasMoveTarget = !AIActor.CurrentTargetVisible,
                AimPoint = point,
                HasAimPoint = true,
                WantsFire = AIActor.CurrentTargetVisible,
                WeaponSlot = OUTL_EquipmentSlot.Primary,
                AttackProfile = AIActor.AttackDriver != null ? AIActor.AttackDriver.Primary : null,
                PreferredRange = AIActor.PreferredRange,
                MinSafeRange = AIActor.MinSafeRange,
                DecisionTime = time,
                Reason = "fallback_ai_actor"
            };
        }

        OUTL_TacticalDecision scheduleDecision;
        if (TryBuildScheduleMoveDecision(time, out scheduleDecision)) return scheduleDecision;
        return OUTL_TacticalDecision.Idle(time, "fallback_idle");
    }

    private bool TryBuildScheduleMoveDecision(float time, out OUTL_TacticalDecision decision)
    {
        decision = default(OUTL_TacticalDecision);
        if (!UseNPCScheduleMoveIntent || NPCBehavior == null) return false;
        return NPCBehavior.TryGetActorInputMoveIntent(time, out decision);
    }

    private static bool IsIdleWithoutMove(OUTL_TacticalDecision decision)
    {
        return !decision.IsValid
            || ((decision.Intent == OUTL_TacticalIntentId.Idle
                || decision.Intent == OUTL_TacticalIntentId.Patrol
                || decision.Intent == OUTL_TacticalIntentId.Work)
                && !decision.HasMoveTarget);
    }

    private static OUTL_AimState BuildFallbackAimState(OUTL_TacticalDecision decision)
    {
        return new OUTL_AimState
        {
            Command = decision.WantsSuppress ? OUTL_AimCommand.Suppress : (decision.WantsFire ? OUTL_AimCommand.FireSingle : OUTL_AimCommand.AimOnly),
            AimPoint = decision.AimPoint,
            HasAimPoint = decision.HasAimPoint,
            Confidence = decision.WantsFire ? 1f : 0.5f,
            Reason = "fallback"
        };
    }

    private OUTL_EntityRuntime ResolveTargetRuntime(OUTL_World world, OUTL_EntityId id)
    {
        if (world == null || !id.IsValid) return null;
        OUTL_EntityRuntime runtime;
        return world.Registry.TryGet(id, out runtime) ? runtime : null;
    }

    private static bool IsDead(OUTL_EntityRuntime runtime)
    {
        return runtime == null || runtime.Dead || runtime.LifeState == OUTL_LifeState.Dead || runtime.State.GetFlag(OUTL_StateId.Dead) || runtime.Stats.Get(OUTL_StatId.Health, 1f) <= 0f;
    }

    private void Resolve()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (AIActor == null) AIActor = GetComponent<OUTL_AIActor>();
        if (TacticalPlanner == null) TacticalPlanner = GetComponent<OUTL_TacticalPlanner>();
        if (AimPlanner == null) AimPlanner = GetComponent<OUTL_AimPlanner>();
        if (Arsenal == null) Arsenal = GetComponent<OUTL_AIArsenalSelector>();
        if (NPCBehavior == null) NPCBehavior = GetComponent<OUTL_NPCBehaviorController>();
        if (MoveRoot == null) MoveRoot = transform;
    }
}
