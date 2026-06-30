using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_AbilityInputSink : MonoBehaviour, OUTL_IActorAbilitySink, OUTL_ITickable, OUTL_IPoolReset
{
    public OUTL_EntityAdapter Entity;
    public CharacterController CharacterController;
    public Rigidbody Rigidbody;
    public OUTL_NavMeshMover NavMover;
    public OUTL_AbilityProfile PrimaryAbility;
    public OUTL_AbilityProfile SecondaryAbility;
    public bool AutoRegister = true;
    public bool AllowTransformFallback = false;
    [Tooltip("If true, active Full/Near ability motion is advanced in Unity Update so close leaps/jumps do not inherit low scheduler cadence.")]
    public bool UseUnityUpdateForFullNear = true;
    public string LastBlockedReason = "";

    private OUTL_AbilityProfile activeAbility;
    private OUTL_LeapAbilityProfile activeLeap;
    private Vector3 leapStart;
    private Vector3 leapTarget;
    private float abilityStartTime;
    private float abilityEndTime;
    private float nextPrimaryTime;
    private float nextSecondaryTime;
    private bool impactApplied;
    private bool registered;
    private readonly Collider[] impactBuffer = new Collider[32];

    public OUTL_ActorInputPhase Phase { get { return OUTL_ActorInputPhase.Weapon; } }
    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled && activeAbility != null && !ShouldUseUnityUpdateAbility(); } }
    public OUTL_TickLane OUTL_TickLane { get { return OUTL_TickLane.Logic; } }
    public float OUTL_TickInterval { get { return 0.02f; } }

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
        ClearActive();
    }

    private void Update()
    {
        if (!ShouldUseUnityUpdateAbility() || activeAbility == null) return;
        OUTL_World world = OUTL_World.Instance;
        TickActive(world, ReadClock(world), Time.deltaTime);
    }

    public void OUTL_ApplyInput(in OUTL_ActorInputFrame frame, OUTL_World world)
    {
        Resolve();
        if (activeAbility != null) return;
        OUTL_AbilityProfile profile = ResolveRequestedAbility(frame);
        if (profile == null) return;
        if (!OUTL_CanUseAbility(profile, frame, world)) return;
        StartAbility(profile, frame, world);
    }

    public bool OUTL_CanUseAbility(OUTL_AbilityProfile profile, in OUTL_ActorInputFrame frame, OUTL_World world)
    {
        LastBlockedReason = "";
        if (profile == null) { LastBlockedReason = "no_profile"; return false; }
        if (Entity == null || Entity.Runtime == null) { LastBlockedReason = "no_entity"; return false; }
        if (!OUTL_NetworkAuthority.CanAuthoritativeSimulate(Entity)) { LastBlockedReason = "authority"; return false; }
        if (IsDead(Entity.Runtime)) { LastBlockedReason = "dead"; return false; }

        float time = ReadClock(world);
        if (profile == PrimaryAbility && time < nextPrimaryTime) { LastBlockedReason = "cooldown"; return false; }
        if (profile == SecondaryAbility && time < nextSecondaryTime) { LastBlockedReason = "cooldown"; return false; }

        Vector3 target = frame.HasAbilityTargetPoint ? frame.AbilityTargetPoint : (frame.HasAimWorldPoint ? frame.AimWorldPoint : transform.position + transform.forward * Mathf.Max(1f, profile.MaxRange));
        float dist = Vector3.Distance(transform.position, target);
        if (dist < Mathf.Max(0f, profile.MinRange) || dist > Mathf.Max(profile.MinRange, profile.MaxRange)) { LastBlockedReason = "range"; return false; }
        if (profile.RequiresLineOfSight && !HasLineOfSight(target)) { LastBlockedReason = "line_of_sight"; return false; }
        return true;
    }

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        if (ShouldUseUnityUpdateAbility()) return;
        TickActive(world, time, deltaTime);
    }

    private void TickActive(OUTL_World world, float time, float deltaTime)
    {
        if (activeAbility == null) return;
        if (time < abilityStartTime + Mathf.Max(0f, activeAbility.WindupTime)) return;

        if (activeLeap != null)
            TickLeap(world, time, deltaTime);

        if (time >= abilityEndTime)
            FinishAbility(world, time);
    }

    public bool CanUseProfile(OUTL_AbilityProfile profile, Vector3 targetPoint, float time)
    {
        OUTL_ActorInputFrame frame = OUTL_ActorInputFrame.Empty(time);
        frame.AbilityTargetPoint = targetPoint;
        frame.HasAbilityTargetPoint = true;
        return OUTL_CanUseAbility(profile, frame, OUTL_World.Instance);
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

    public void OUTL_OnPoolSpawn()
    {
        ClearActive();
        Register();
    }

    public void OUTL_OnPoolRelease()
    {
        ClearActive();
        Unregister();
    }

    private void StartAbility(OUTL_AbilityProfile profile, in OUTL_ActorInputFrame frame, OUTL_World world)
    {
        float time = ReadClock(world);
        activeAbility = profile;
        activeLeap = profile as OUTL_LeapAbilityProfile;
        leapStart = transform.position;
        leapTarget = frame.HasAbilityTargetPoint ? frame.AbilityTargetPoint : (frame.HasAimWorldPoint ? frame.AimWorldPoint : transform.position + transform.forward * Mathf.Max(1f, profile.MaxRange));
        abilityStartTime = time;
        abilityEndTime = time + Mathf.Max(0f, profile.WindupTime) + ReadDuration(profile) + Mathf.Max(0f, profile.RecoveryTime);
        impactApplied = false;
        float next = time + Mathf.Max(0f, profile.Cooldown);
        if (profile == PrimaryAbility) nextPrimaryTime = next;
        if (profile == SecondaryAbility) nextSecondaryTime = next;
        if (world != null) OUTL_StimulusBus.EmitCombat(Entity != null ? Entity.Id : OUTL_EntityId.None, transform.position, 8f, 0.35f, 0.35f, profile.AbilityId);
        if (NavMover != null) NavMover.Stop();
        if (Rigidbody != null && activeLeap != null && activeLeap.UsePhysicsImpulse)
        {
            Vector3 velocity = BuildLeapVelocity(activeLeap);
            Rigidbody.AddForce(velocity, ForceMode.VelocityChange);
        }
    }

    private void TickLeap(OUTL_World world, float time, float deltaTime)
    {
        if (activeLeap.UsePhysicsImpulse && Rigidbody != null) return;
        float start = abilityStartTime + Mathf.Max(0f, activeAbility.WindupTime);
        float duration = Mathf.Max(0.05f, activeLeap.LeapDuration);
        float t = Mathf.Clamp01((time - start) / duration);
        Vector3 flat = Vector3.Lerp(leapStart, leapTarget, t);
        float arc = Mathf.Sin(t * Mathf.PI) * Mathf.Max(0f, activeLeap.LeapArcHeight);
        Vector3 next = flat + Vector3.up * arc;
        Vector3 delta = next - transform.position;

        if (CharacterController != null && CharacterController.enabled && activeLeap.UseCharacterMotor)
            CharacterController.Move(delta);
        else if (AllowTransformFallback)
            transform.position = next;

        if (!impactApplied && t >= 0.92f)
            ApplyLeapImpact(world);
    }

    private void ApplyLeapImpact(OUTL_World world)
    {
        impactApplied = true;
        if (activeLeap == null || activeLeap.ImpactDamage <= 0f || activeLeap.ImpactRadius <= 0f) return;
        OUTL_Profile.Frame.Overlaps++;
        int count = Physics.OverlapSphereNonAlloc(transform.position, activeLeap.ImpactRadius, impactBuffer, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            OUTL_EntityAdapter target;
            if (!OUTL_Combat.TryGetEntityFromCollider(impactBuffer[i], out target)) continue;
            impactBuffer[i] = null;
            if (Entity != null && target.Id == Entity.Id) continue;
            OUTL_Combat.ApplyDamage(Entity != null ? Entity.Id : OUTL_EntityId.None, target.Id, activeLeap.ImpactDamage, target.transform.position, activeLeap.AbilityId);
        }
    }

    private void FinishAbility(OUTL_World world, float time)
    {
        if (!impactApplied && activeLeap != null) ApplyLeapImpact(world);
        ClearActive();
    }

    private OUTL_AbilityProfile ResolveRequestedAbility(in OUTL_ActorInputFrame frame)
    {
        if (frame.AbilitySlot >= 0)
        {
            if (PrimaryAbility != null && PrimaryAbility.AbilitySlot == frame.AbilitySlot) return PrimaryAbility;
            if (SecondaryAbility != null && SecondaryAbility.AbilitySlot == frame.AbilitySlot) return SecondaryAbility;
        }
        if (frame.AbilityPrimaryPressed || frame.AbilityPrimaryHeld) return PrimaryAbility;
        if (frame.AbilitySecondaryPressed) return SecondaryAbility;
        return null;
    }

    private float ReadDuration(OUTL_AbilityProfile profile)
    {
        OUTL_LeapAbilityProfile leap = profile as OUTL_LeapAbilityProfile;
        return leap != null ? Mathf.Max(0.05f, leap.LeapDuration + leap.LandingRecovery) : 0f;
    }

    private Vector3 BuildLeapVelocity(OUTL_LeapAbilityProfile leap)
    {
        Vector3 to = leapTarget - transform.position;
        to.y = 0f;
        Vector3 dir = to.sqrMagnitude > 0.001f ? to.normalized : transform.forward;
        return dir * Mathf.Max(0.1f, leap.LeapSpeed) + Vector3.up * Mathf.Max(0f, leap.LeapArcHeight);
    }

    private bool HasLineOfSight(Vector3 target)
    {
        Vector3 origin = transform.position + Vector3.up;
        Vector3 delta = target - origin;
        float distance = delta.magnitude;
        if (distance <= 0.05f) return true;
        OUTL_Profile.Frame.Raycasts++;
        RaycastHit hit;
        if (!Physics.Raycast(origin, delta / distance, out hit, distance, ~0, QueryTriggerInteraction.Ignore)) return true;
        OUTL_EntityAdapter hitEntity;
        if (OUTL_Combat.TryGetEntityFromCollider(hit.collider, out hitEntity))
        {
            if (Entity != null && hitEntity != null && hitEntity.Id == Entity.Id) return true;
            if ((hit.point - target).sqrMagnitude <= 2.25f) return true;
        }
        return false;
    }

    private bool IsDead(OUTL_EntityRuntime runtime)
    {
        return runtime == null || runtime.Dead || runtime.LifeState == OUTL_LifeState.Dead || runtime.State.GetFlag(OUTL_StateId.Dead) || runtime.Stats.Get(OUTL_StatId.Health, 1f) <= 0f;
    }

    private bool ShouldUseUnityUpdateAbility()
    {
        if (!UseUnityUpdateForFullNear || !isActiveAndEnabled) return false;
        if (OUTL_World.Instance != null && OUTL_World.Instance.IsPaused) return false;
        Resolve();
        if (Entity == null || Entity.Runtime == null) return false;
        OUTL_RuntimeTier tier = Entity.Runtime.Tier;
        return tier == OUTL_RuntimeTier.Full || tier == OUTL_RuntimeTier.Near;
    }

    private float ReadClock(OUTL_World world)
    {
        if (ShouldUseUnityUpdateAbility()) return Time.time;
        return world != null ? world.WorldTime : Time.time;
    }

    private void ClearActive()
    {
        activeAbility = null;
        activeLeap = null;
        impactApplied = false;
    }

    private void Resolve()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (CharacterController == null) CharacterController = GetComponent<CharacterController>();
        if (Rigidbody == null) Rigidbody = GetComponent<Rigidbody>();
        if (NavMover == null) NavMover = GetComponent<OUTL_NavMeshMover>();
    }
}
