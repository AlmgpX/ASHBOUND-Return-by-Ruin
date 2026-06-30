using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_TacticalPlanner : MonoBehaviour, OUTL_ITickable, OUTL_IPoolReset
{
    public OUTL_EntityAdapter Entity;
    public OUTL_AIActor AIActor;
    public OUTL_TacticalProfile Profile;
    public OUTL_AIArsenalSelector Arsenal;
    public OUTL_AimPlanner AimPlanner;
    public OUTL_AbilityInputSink AbilitySink;
    public OUTL_SquadMember SquadMember;
    public OUTL_SquadBlackboard SquadBlackboard;
    public bool AutoRegister = false;
    public bool AutoUseActorInputContract = true;
    public OUTL_TacticalDecision CurrentDecision;
    public OUTL_AIWeaponSelection CurrentWeaponSelection;
    public OUTL_AimState CurrentAimState;

    private readonly OUTL_CoverQueryResult[] coverResults = new OUTL_CoverQueryResult[8];
    private bool registered;

    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled && Entity != null && Entity.Runtime != null; } }
    public OUTL_TickLane OUTL_TickLane { get { return OUTL_TickLane.AI; } }
    public float OUTL_TickInterval
    {
        get
        {
            OUTL_RuntimeTier tier = Entity != null && Entity.Runtime != null ? Entity.Runtime.Tier : OUTL_RuntimeTier.Full;
            if (Profile == null)
            {
                if (tier == OUTL_RuntimeTier.Far || tier == OUTL_RuntimeTier.Dormant) return 1.25f;
                if (tier == OUTL_RuntimeTier.Mid) return 0.35f;
                return 0.12f;
            }
            if (tier == OUTL_RuntimeTier.Far || tier == OUTL_RuntimeTier.Dormant) return Mathf.Max(0.1f, Profile.FarThinkInterval);
            if (tier == OUTL_RuntimeTier.Mid) return Mathf.Max(0.05f, Profile.MidThinkInterval);
            return Mathf.Max(0.02f, Profile.NearThinkInterval);
        }
    }

    private void Awake()
    {
        Resolve();
    }

    private void OnEnable()
    {
        Resolve();
        if (AutoRegister) Register();
    }

    private void OnDisable()
    {
        Unregister();
        ReleaseCover();
    }

    public void Register()
    {
        if (registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Register(this);
        registered = true;
    }

    public void Unregister()
    {
        if (!registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Unregister(this);
        registered = false;
    }

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        CurrentDecision = BuildDecision(world, time, deltaTime);
        ApplyDebugState(CurrentDecision);
    }

    public bool TryGetDecision(out OUTL_TacticalDecision decision)
    {
        decision = CurrentDecision;
        return decision.IsValid;
    }

    public OUTL_TacticalDecision BuildDecision(OUTL_World world, float time, float deltaTime)
    {
        Resolve();
        if (world == null) world = OUTL_World.Instance;
        if (Entity == null || Entity.Runtime == null) return OUTL_TacticalDecision.Idle(time, "missing_entity");
        OUTL_EntityRuntime self = Entity.Runtime;
        if (IsDead(self)) return OUTL_TacticalDecision.Dead(time);

        OUTL_RuntimeTier tier = self.Tier;
        OUTL_EntityRuntime target = ResolveTarget(world, out bool targetVisible);
        Vector3 targetPoint = ResolveTargetPoint(target);
        float distance = target != null && target.Adapter != null ? Vector3.Distance(transform.position, target.Adapter.transform.position) : 0f;

        if ((tier == OUTL_RuntimeTier.Far || tier == OUTL_RuntimeTier.Dormant) && ReadFullTacticalOnlyNear())
            return BuildAbstractDecision(time, target, targetPoint);

        if (ShouldRetreat(self))
            return BuildRetreatOrCoverDecision(world, time, targetPoint, "low_health_or_fear");

        if (ShouldSeekCover())
            return BuildCoverDecision(world, time, targetPoint, "danger_cover");

        if (AIActor != null && AIActor.CurrentOrder.IsValid)
        {
            OUTL_SquadOrder order = AIActor.CurrentOrder;
            if (order.Type == OUTL_SquadOrderType.TakeCover) return BuildCoverDecision(world, time, order.Position, "squad_cover");
            if (order.Type == OUTL_SquadOrderType.Retreat) return BuildMoveDecision(OUTL_TacticalIntentId.Retreat, OUTL_TacticalMoveMode.RetreatFromTarget, order.Position, OUTL_EntityId.None, time, "squad_retreat");
            if (order.Type == OUTL_SquadOrderType.Investigate || order.Type == OUTL_SquadOrderType.Search) return BuildMoveDecision(OUTL_TacticalIntentId.Investigate, OUTL_TacticalMoveMode.MoveTo, order.Position, order.Target, time, "squad_investigate");
        }

        if (target != null && target.Adapter != null)
            return BuildCombatDecision(world, target, targetVisible, distance, targetPoint, time);

        if (AIActor != null)
        {
            if (AIActor.CreatureUsesFoodStimulus && AIActor.LastStimulusType == OUTL_StimulusType.SightFood)
                return BuildMoveDecision(OUTL_TacticalIntentId.EatOrUseResource, OUTL_TacticalMoveMode.MoveTo, AIActor.LastStimulusPosition, OUTL_EntityId.None, time, "food");
            if (IsInvestigateStimulus(AIActor.LastStimulusType))
                return BuildMoveDecision(OUTL_TacticalIntentId.Investigate, OUTL_TacticalMoveMode.MoveTo, AIActor.LastStimulusPosition, OUTL_EntityId.None, time, "stimulus");
            if (AIActor.CurrentIntent == "Patrol") return OUTL_TacticalDecision.Idle(time, "patrol_schedule");
            if (AIActor.CurrentIntent == "Work") return OUTL_TacticalDecision.Idle(time, "work_schedule");
            if (AIActor.CurrentIntent == "Flee") return BuildRetreatOrCoverDecision(world, time, AIActor.LastStimulusPosition, "flee");
        }

        return OUTL_TacticalDecision.Idle(time, "idle");
    }

    public void OUTL_OnPoolSpawn()
    {
        Resolve();
        Register();
    }

    public void OUTL_OnPoolRelease()
    {
        Unregister();
        ReleaseCover();
        CurrentDecision = default(OUTL_TacticalDecision);
    }

    private OUTL_TacticalDecision BuildCombatDecision(OUTL_World world, OUTL_EntityRuntime target, bool targetVisible, float distance, Vector3 targetPoint, float time)
    {
        OUTL_TacticalDecision abilityDecision;
        if (TryBuildAbilityDecision(target, targetVisible, distance, targetPoint, time, out abilityDecision))
            return abilityDecision;

        OUTL_AIWeaponSelection selection;
        if (Arsenal != null && Arsenal.TrySelect(Profile, target, distance, time, targetVisible, OUTL_TacticalIntentId.AttackRanged, out selection))
            CurrentWeaponSelection = selection;
        else
            selection = CurrentWeaponSelection;

        float preferred = selection.IsValid ? selection.PreferredRange : ReadPreferredRange();
        float minSafe = selection.IsValid ? selection.MinSafeRange : ReadMinSafeRange();
        OUTL_TacticalIntentId intent = selection.IsValid && selection.Slot == OUTL_EquipmentSlot.Melee ? OUTL_TacticalIntentId.AttackMelee : OUTL_TacticalIntentId.AttackRanged;

        if (distance < minSafe && selection.IsValid && selection.Slot != OUTL_EquipmentSlot.Melee)
        {
            OUTL_TacticalDecision cover = BuildCoverDecision(world, time, targetPoint, "target_too_close");
            if (cover.Intent == OUTL_TacticalIntentId.TakeCover || cover.Intent == OUTL_TacticalIntentId.FindCover) return cover;
            return BuildMoveDecision(OUTL_TacticalIntentId.Retreat, OUTL_TacticalMoveMode.RetreatFromTarget, targetPoint, target.Id, time, "too_close");
        }

        OUTL_TacticalDecision decision = new OUTL_TacticalDecision
        {
            Intent = targetVisible || !ReadFireOnlyWhenVisible() ? intent : OUTL_TacticalIntentId.Search,
            MoveMode = distance > preferred ? OUTL_TacticalMoveMode.KeepRange : OUTL_TacticalMoveMode.Hold,
            Target = target.Id,
            MoveTarget = targetPoint,
            HasMoveTarget = distance > preferred,
            AimPoint = targetPoint,
            HasAimPoint = true,
            WantsFire = targetVisible || !ReadFireOnlyWhenVisible(),
            WeaponSlot = selection.IsValid ? selection.Slot : OUTL_EquipmentSlot.Primary,
            AttackProfile = selection.AttackProfile,
            PreferredRange = preferred,
            MinSafeRange = minSafe,
            Reason = targetVisible ? "visible_target" : "lost_target",
            Score = targetVisible ? 1f : 0.35f,
            DecisionTime = time
        };

        if (ShouldSuppress(selection))
        {
            decision.Intent = OUTL_TacticalIntentId.Suppress;
            decision.WantsSuppress = true;
            decision.WantsFire = true;
            decision.Reason = "suppress";
        }

        ApplySquadRolePreference(ref decision, distance);
        ApplyFireLane(ref decision, targetPoint);

        if (!targetVisible && ReadFireOnlyWhenVisible())
        {
            decision.WantsFire = false;
            decision.WantsSuppress = false;
            decision.MoveMode = OUTL_TacticalMoveMode.MoveTo;
            decision.HasMoveTarget = true;
            decision.Reason = "search_last_known";
        }

        return decision;
    }

    private bool TryBuildAbilityDecision(OUTL_EntityRuntime target, bool targetVisible, float distance, Vector3 targetPoint, float time, out OUTL_TacticalDecision decision)
    {
        decision = default(OUTL_TacticalDecision);
        OUTL_LeapAbilityProfile leap = Profile != null ? Profile.LeapAbility : null;
        OUTL_AbilityProfile ability = leap != null ? leap : (Profile != null ? Profile.PrimaryAbility : null);
        if (ability == null || !targetVisible) return false;

        bool distanceOk = distance >= Mathf.Max(0f, ability.MinRange) && distance <= Mathf.Max(ability.MinRange, ability.MaxRange);
        if (leap != null)
            distanceOk = distance >= Mathf.Max(0f, leap.PreferWhenTargetDistanceMin) && distance <= Mathf.Max(leap.PreferWhenTargetDistanceMin, leap.PreferWhenTargetDistanceMax);
        if (!distanceOk) return false;
        if (AbilitySink != null && !AbilitySink.CanUseProfile(ability, targetPoint, time)) return false;

        decision = new OUTL_TacticalDecision
        {
            Intent = leap != null ? OUTL_TacticalIntentId.LeapAttack : OUTL_TacticalIntentId.AbilityAttack,
            MoveMode = OUTL_TacticalMoveMode.Hold,
            Target = target.Id,
            AimPoint = targetPoint,
            HasAimPoint = true,
            AbilityProfile = ability,
            WantsAbility = true,
            AbilitySlot = ability.AbilitySlot,
            AbilityTargetPoint = targetPoint,
            HasAbilityTargetPoint = true,
            WeaponSlot = OUTL_EquipmentSlot.Primary,
            Reason = leap != null ? "leap_ability" : "ability",
            Score = 1.1f,
            DecisionTime = time
        };
        return true;
    }

    private OUTL_TacticalDecision BuildCoverDecision(OUTL_World world, float time, Vector3 threat, string reason)
    {
        OUTL_CoverPoint current = AIActor != null ? AIActor.CurrentCover : null;
        if (current != null && current.IsFreeFor(Entity))
            return CoverDecision(current, time, threat, reason);

        OUTL_CoverQuery query = new OUTL_CoverQuery
        {
            Seeker = Entity,
            SeekerPosition = transform.position,
            ThreatPosition = threat != Vector3.zero ? threat : transform.position + transform.forward * 8f,
            SearchRadius = ReadCoverSearchRadius(),
            WeaponRole = CurrentWeaponSelection.IsValid && CurrentWeaponSelection.UseProfile != null ? CurrentWeaponSelection.UseProfile.Role : OUTL_WeaponRole.Any,
            VisibilityMask = ReadCoverMask(),
            RequireBlocksThreat = true,
            Time = time
        };

        int count = OUTL_CoverRegistry.QueryNonAlloc(query, coverResults);
        for (int i = 0; i < count; i++)
        {
            OUTL_CoverPoint point = coverResults[i].Point;
            if (point == null) continue;
            bool reserved = SquadMember != null ? SquadMember.TryReserveCover(point, ReadCoverReservationSeconds(), reason) : point.Reserve(Entity, ReadCoverReservationSeconds(), reason);
            if (!reserved) continue;
            if (AIActor != null) AIActor.CurrentCover = point;
            ClearCoverResults(count);
            return CoverDecision(point, time, threat, reason);
        }

        ClearCoverResults(count);
        return BuildMoveDecision(OUTL_TacticalIntentId.FindCover, OUTL_TacticalMoveMode.RetreatFromTarget, threat, OUTL_EntityId.None, time, reason + "_none_found");
    }

    private OUTL_TacticalDecision CoverDecision(OUTL_CoverPoint cover, float time, Vector3 threat, string reason)
    {
        return new OUTL_TacticalDecision
        {
            Intent = OUTL_TacticalIntentId.TakeCover,
            MoveMode = OUTL_TacticalMoveMode.TakeCover,
            MoveTarget = cover.StandPoint,
            HasMoveTarget = true,
            AimPoint = threat != Vector3.zero ? threat : cover.PeekPoint,
            HasAimPoint = threat != Vector3.zero,
            WantsCover = true,
            Cover = cover,
            WeaponSlot = CurrentWeaponSelection.IsValid ? CurrentWeaponSelection.Slot : OUTL_EquipmentSlot.Primary,
            AttackProfile = CurrentWeaponSelection.AttackProfile,
            PreferredRange = ReadPreferredRange(),
            MinSafeRange = ReadMinSafeRange(),
            Reason = reason,
            Score = 0.9f,
            DecisionTime = time
        };
    }

    private OUTL_TacticalDecision BuildRetreatOrCoverDecision(OUTL_World world, float time, Vector3 threat, string reason)
    {
        if (ReadUseCover()) return BuildCoverDecision(world, time, threat, reason);
        return BuildMoveDecision(OUTL_TacticalIntentId.Retreat, OUTL_TacticalMoveMode.RetreatFromTarget, threat, OUTL_EntityId.None, time, reason);
    }

    private OUTL_TacticalDecision BuildMoveDecision(OUTL_TacticalIntentId intent, OUTL_TacticalMoveMode mode, Vector3 point, OUTL_EntityId target, float time, string reason)
    {
        return new OUTL_TacticalDecision
        {
            Intent = intent,
            MoveMode = mode,
            Target = target,
            MoveTarget = point,
            HasMoveTarget = true,
            AimPoint = point,
            HasAimPoint = point != Vector3.zero,
            WeaponSlot = CurrentWeaponSelection.IsValid ? CurrentWeaponSelection.Slot : OUTL_EquipmentSlot.Primary,
            AttackProfile = CurrentWeaponSelection.AttackProfile,
            PreferredRange = ReadPreferredRange(),
            MinSafeRange = ReadMinSafeRange(),
            Reason = reason,
            Score = 0.5f,
            DecisionTime = time
        };
    }

    private OUTL_TacticalDecision BuildAbstractDecision(float time, OUTL_EntityRuntime target, Vector3 targetPoint)
    {
        if (target != null && target.Adapter != null)
            return BuildMoveDecision(OUTL_TacticalIntentId.Search, OUTL_TacticalMoveMode.None, targetPoint, target.Id, time, "far_abstract_target");
        if (AIActor != null && IsInvestigateStimulus(AIActor.LastStimulusType))
            return BuildMoveDecision(OUTL_TacticalIntentId.Investigate, OUTL_TacticalMoveMode.None, AIActor.LastStimulusPosition, OUTL_EntityId.None, time, "far_abstract_stimulus");
        return OUTL_TacticalDecision.Idle(time, "far_abstract_idle");
    }

    private OUTL_EntityRuntime ResolveTarget(OUTL_World world, out bool visible)
    {
        visible = false;
        if (world == null) return null;
        OUTL_EntityRuntime target = null;
        if (AIActor != null && AIActor.CurrentTarget.IsValid)
            world.Registry.TryGet(AIActor.CurrentTarget, out target);
        if ((target == null || target.Adapter == null) && SquadBlackboard != null && SquadBlackboard.HasSharedTarget)
            world.Registry.TryGet(SquadBlackboard.SharedTarget, out target);
        if (target == null || target.Adapter == null || IsDead(target)) return null;
        visible = AIActor != null && AIActor.CurrentTarget == target.Id && AIActor.CurrentTargetVisible;
        if (!visible && AIActor != null)
            visible = OUTL_AIPerceptionUtility.CanSeeRuntime(transform, Entity, AIActor.Profile, AIActor.PerceptionProfile, target, AIActor.EyeHeight, AIActor.TargetEyeHeight, AIActor.SightBlockMask);
        return target;
    }

    private Vector3 ResolveTargetPoint(OUTL_EntityRuntime target)
    {
        if (target != null && target.Adapter != null) return target.Adapter.transform.position + Vector3.up;
        if (AIActor != null)
        {
            if (AIActor.LastKnownTargetPosition != Vector3.zero) return AIActor.LastKnownTargetPosition + Vector3.up;
            if (AIActor.LastStimulusPosition != Vector3.zero) return AIActor.LastStimulusPosition + Vector3.up;
        }
        if (SquadBlackboard != null && SquadBlackboard.HasSharedTarget) return SquadBlackboard.SharedTargetPosition + Vector3.up;
        return transform.position + transform.forward * 8f + Vector3.up;
    }

    private bool ShouldRetreat(OUTL_EntityRuntime self)
    {
        float health = self != null ? self.Stats.Get(OUTL_StatId.Health, 100f) : 100f;
        float threshold = Profile != null && Profile.LowHealthRetreatThreshold > 0f ? Profile.LowHealthRetreatThreshold : (AIActor != null && AIActor.Profile != null ? AIActor.Profile.LowHealthThreshold : 15f);
        float fear = AIActor != null ? Mathf.Max(AIActor.CurrentFear, AIActor.MemoryFear) : 0f;
        return health > 0f && (health <= threshold || fear >= ReadFearToRetreat());
    }

    private bool ShouldSeekCover()
    {
        if (!ReadUseCover() || AIActor == null) return false;
        float danger = Mathf.Max(AIActor.CurrentDanger, AIActor.MemoryFear);
        return danger >= ReadDangerToCover() || AIActor.LastStimulusType == OUTL_StimulusType.TookDamage || AIActor.LastStimulusType == OUTL_StimulusType.SightDanger;
    }

    private bool ShouldSuppress(OUTL_AIWeaponSelection selection)
    {
        if (AIActor == null || !ReadAllowSuppress()) return false;
        if (SquadMember != null && SquadMember.RoleKind == OUTL_SquadRole.Suppressor) return true;
        if (SquadMember != null && SquadMember.RoleKind == OUTL_SquadRole.Support) return true;
        if (AIActor.CurrentOrder.IsValid && AIActor.CurrentOrder.Type == OUTL_SquadOrderType.Suppress) return true;
        if (selection.IsValid && selection.UseProfile != null && !selection.UseProfile.AllowSuppression) return false;
        return AIActor.CurrentMorale <= ReadMoraleToSuppress() && AIActor.CurrentAggression >= ReadAggressionToAttack();
    }

    private void ApplySquadRolePreference(ref OUTL_TacticalDecision decision, float distance)
    {
        if (SquadMember == null) return;
        if (SquadMember.RoleKind == OUTL_SquadRole.Sniper)
        {
            decision.PreferredRange = Mathf.Max(decision.PreferredRange, 28f);
            if (decision.MoveMode == OUTL_TacticalMoveMode.KeepRange && distance < decision.PreferredRange * 0.6f)
                decision.MoveMode = OUTL_TacticalMoveMode.RetreatFromTarget;
        }
        else if (SquadMember.RoleKind == OUTL_SquadRole.Flanker || SquadMember.RoleKind == OUTL_SquadRole.Scout)
        {
            if (decision.MoveMode == OUTL_TacticalMoveMode.Hold && decision.HasMoveTarget)
                decision.Reason = "role_flank";
        }
        else if (SquadMember.RoleKind == OUTL_SquadRole.Melee || SquadMember.RoleKind == OUTL_SquadRole.Creature)
        {
            decision.PreferredRange = Mathf.Min(decision.PreferredRange > 0f ? decision.PreferredRange : 4f, 4f);
        }
    }

    private void ApplyFireLane(ref OUTL_TacticalDecision decision, Vector3 targetPoint)
    {
        if (SquadBlackboard == null || Entity == null || !decision.WantsFire) return;
        Vector3 origin = transform.position + Vector3.up;
        if (!SquadBlackboard.TryReserveFireLane(Entity, origin, targetPoint, 1.25f, 0.5f))
        {
            decision.WantsFire = false;
            decision.WantsSuppress = false;
            decision.Intent = OUTL_TacticalIntentId.HoldFire;
            decision.Reason = "fire_lane_reserved";
        }
    }

    private void ApplyDebugState(OUTL_TacticalDecision decision)
    {
        if (AIActor == null || !decision.IsValid) return;
        AIActor.NextAction = decision.Intent.ToString();
        if (!string.IsNullOrEmpty(decision.Reason)) AIActor.LastEvent = "Tactical:" + decision.Reason;
        if (decision.AttackProfile != null)
        {
            AIActor.CurrentAttackProfile = decision.AttackProfile;
            AIActor.CurrentWeapon = decision.WeaponSlot.ToString();
        }
        if (decision.Target.IsValid) AIActor.CurrentTarget = decision.Target;
    }

    private void ReleaseCover()
    {
        if (SquadMember != null) SquadMember.ReleaseCover();
        else if (AIActor != null && AIActor.CurrentCover != null) AIActor.CurrentCover.Release(Entity);
    }

    private void ClearCoverResults(int count)
    {
        for (int i = 0; i < count && i < coverResults.Length; i++)
            coverResults[i] = default(OUTL_CoverQueryResult);
    }

    private static bool IsDead(OUTL_EntityRuntime runtime)
    {
        return runtime == null || runtime.Dead || runtime.LifeState == OUTL_LifeState.Dead || runtime.State.GetFlag(OUTL_StateId.Dead) || runtime.Stats.Get(OUTL_StatId.Health, 1f) <= 0f;
    }

    private static bool IsInvestigateStimulus(OUTL_StimulusType type)
    {
        return type == OUTL_StimulusType.HeardNoise || type == OUTL_StimulusType.HeardCombat || type == OUTL_StimulusType.LostTarget || type == OUTL_StimulusType.Suspicion || type == OUTL_StimulusType.Resource || type == OUTL_StimulusType.Alert;
    }

    private bool ReadFullTacticalOnlyNear() { return Profile == null || Profile.FullTacticalOnlyNear; }
    private bool ReadUseCover() { return Profile == null || Profile.UseCover; }
    private bool ReadAllowSuppress() { return Profile == null || Profile.AllowSuppress; }
    private bool ReadFireOnlyWhenVisible() { return Profile == null || Profile.FireOnlyWhenVisible; }
    private float ReadPreferredRange() { return Profile != null ? Mathf.Max(0.5f, Profile.PreferredRange) : (AIActor != null && AIActor.PreferredRange > 0f ? AIActor.PreferredRange : 18f); }
    private float ReadMinSafeRange() { return Profile != null ? Mathf.Max(0f, Profile.MinSafeRange) : (AIActor != null ? Mathf.Max(0f, AIActor.MinSafeRange) : 4f); }
    private float ReadCoverSearchRadius() { return Profile != null ? Mathf.Max(1f, Profile.CoverSearchRadius) : (AIActor != null ? Mathf.Max(1f, AIActor.CoverSearchRadius) : 18f); }
    private float ReadDangerToCover() { return Profile != null ? Mathf.Clamp01(Profile.DangerToSeekCover) : 0.45f; }
    private float ReadFearToRetreat() { return Profile != null ? Mathf.Clamp01(Profile.FearToRetreat) : 0.75f; }
    private float ReadMoraleToSuppress() { return Profile != null ? Mathf.Clamp01(Profile.MoraleToSuppress) : 0.35f; }
    private float ReadAggressionToAttack() { return Profile != null ? Mathf.Clamp01(Profile.AggressionToAttack) : 0.25f; }
    private float ReadCoverReservationSeconds() { return Profile != null ? Mathf.Max(0.1f, Profile.CoverReservationSeconds) : 4f; }
    private LayerMask ReadCoverMask() { return Profile != null ? Profile.CoverVisibilityMask : (AIActor != null ? AIActor.CoverVisibilityMask : ~0); }

    private void Resolve()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (AIActor == null) AIActor = GetComponent<OUTL_AIActor>();
        if (Arsenal == null) Arsenal = GetComponent<OUTL_AIArsenalSelector>();
        if (AimPlanner == null) AimPlanner = GetComponent<OUTL_AimPlanner>();
        if (AbilitySink == null) AbilitySink = GetComponent<OUTL_AbilityInputSink>();
        if (SquadMember == null) SquadMember = GetComponent<OUTL_SquadMember>();
        if (SquadBlackboard == null && SquadMember != null) SquadBlackboard = SquadMember.Blackboard;
        if (SquadBlackboard == null) SquadBlackboard = GetComponentInParent<OUTL_SquadBlackboard>();
        if (AutoUseActorInputContract && AIActor != null) AIActor.UseActorInputContract = true;
        if (AimPlanner != null && AimPlanner.Profile == null && Profile != null) AimPlanner.Profile = Profile.AimProfile;
    }
}
