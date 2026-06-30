using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_OutpostEnemyBrain : MonoBehaviour, OUTL_ITickable, OUTL_IEventListener, OUTL_IPoolReset, OUTL_IComponentSaveParticipant
{
    public OUTL_EntityAdapter Entity;
    public OUTL_NavMeshMover NavMover;
    public OUTL_AttackDriver AttackDriver;
    public OUTL_EnemyBarkDriver BarkDriver;
    public OUTL_EnemyArchetypeProfile Profile;
    public Transform Eye;
    public Transform HomeAnchor;
    public bool AutoRegister = true;

    [Header("Runtime")]
    public OUTL_EnemyState State = OUTL_EnemyState.Wander;
    public OUTL_EntityId Target;
    public Vector3 HomePosition;
    public Vector3 LastKnownTargetPosition;

    private float nextAcquireTime;
    private float lastSeenTime;
    private float nextWanderTime;
    private int wanderSequence;
    private bool registered;
    private bool componentStateRestored;
    private string restoredTargetStableId;
    private string restoredTargetName;

    public string OUTL_SaveKey { get { return "OUTL_OutpostEnemyBrain"; } }

    public bool OUTL_IsTickEnabled
    {
        get { return isActiveAndEnabled && Entity != null && Entity.Runtime != null && Profile != null && State != OUTL_EnemyState.Dead; }
    }

    public OUTL_TickLane OUTL_TickLane { get { return OUTL_TickLane.AI; } }
    public float OUTL_TickInterval
    {
        get
        {
            OUTL_RuntimeTier tier = Entity != null && Entity.Runtime != null ? Entity.Runtime.Tier : OUTL_RuntimeTier.Dormant;
            return Profile != null ? Profile.GetInterval(tier) : 1f;
        }
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (AutoRegister) Register();
        if (OUTL_World.Instance != null)
        {
            OUTL_World.Instance.Events.Register(this, OUTL_EventType.Damaged);
            OUTL_World.Instance.Events.Register(this, OUTL_EventType.Killed);
        }
    }

    private void OnDisable()
    {
        Unregister();
        if (OUTL_World.Instance != null) OUTL_World.Instance.Events.Unregister(this);
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
        if (world == null || Profile == null || Entity == null || Entity.Runtime == null) return;
        EnsureHomePosition();
        OUTL_EntityRuntime self = Entity.Runtime;
        if (self.Dead || self.State.GetFlag(OUTL_StateId.Dead) || self.Stats.Get(OUTL_StatId.Health, 1f) <= 0f)
        {
            EnterState(OUTL_EnemyState.Dead);
            return;
        }

        OUTL_RuntimeTier tier = self.Tier;
        if (tier == OUTL_RuntimeTier.Dormant)
        {
            ClearTarget();
            if (NavMover != null && NavMover.HasDestination) NavMover.Stop("enemy_dormant");
            WriteRuntimeState();
            return;
        }

        OUTL_EntityRuntime target = ResolveTarget(world);
        if (tier == OUTL_RuntimeTier.Full || tier == OUTL_RuntimeTier.Near)
        {
            if (time >= nextAcquireTime)
            {
                nextAcquireTime = time + Mathf.Max(0.05f, Profile.AcquireInterval);
                OUTL_EntityRuntime candidate = world.Sectors.FindNearestHostile(self, transform.position, Profile.SightDistance);
                if (candidate != null && CanSee(candidate))
                {
                    target = candidate;
                    RememberTarget(candidate);
                    LastKnownTargetPosition = candidate.Adapter.transform.position;
                    lastSeenTime = time;
                }
            }

            if (target != null && target.Adapter != null)
            {
                float leashSqr = Profile.HomeLeashDistance * Profile.HomeLeashDistance;
                if ((target.Adapter.transform.position - HomePosition).sqrMagnitude > leashSqr)
                {
                    ClearTarget();
                    target = null;
                }
                else if (CanSee(target))
                {
                    LastKnownTargetPosition = target.Adapter.transform.position;
                    lastSeenTime = time;
                    TickCombat(target, time, deltaTime);
                    WriteRuntimeState();
                    return;
                }
            }
        }

        if (LastKnownTargetPosition != Vector3.zero && time - lastSeenTime <= Mathf.Max(0f, Profile.TargetMemorySeconds))
        {
            EnterState(OUTL_EnemyState.Alert);
            MoveTo(LastKnownTargetPosition, "enemy_alert");
            WriteRuntimeState();
            return;
        }

        ClearTarget();
        TickHomeBehavior(time);
        WriteRuntimeState();
    }

    public void OUTL_OnEvent(in OUTL_Event evt, OUTL_World world)
    {
        if (Entity == null || evt.Target != Entity.Id) return;
        if (evt.Type == OUTL_EventType.Killed)
        {
            EnterState(OUTL_EnemyState.Dead);
            if (NavMover != null) NavMover.Stop("enemy_dead");
            return;
        }

        if (evt.Type != OUTL_EventType.Damaged) return;
        OUTL_EntityRuntime attacker;
        if (world != null && world.Registry.TryGet(evt.Source, out attacker) && attacker != null && attacker.Adapter != null)
        {
            RememberTarget(attacker);
            LastKnownTargetPosition = attacker.Adapter.transform.position;
            lastSeenTime = world.WorldTime;
            EnterState(OUTL_EnemyState.Alert);
        }
    }

    private void TickCombat(OUTL_EntityRuntime target, float time, float deltaTime)
    {
        EnterState(OUTL_EnemyState.Combat);
        if (BarkDriver != null) BarkDriver.NotifyCombat();

        Vector3 targetPoint = target.Adapter.transform.position + Vector3.up;
        Vector3 flat = targetPoint - transform.position;
        flat.y = 0f;
        float distance = flat.magnitude;
        Face(flat, deltaTime);

        if (distance > Profile.PreferredRange)
            MoveTo(target.Adapter.transform.position, "enemy_combat_close");
        else if (distance < Profile.MinimumRange && flat.sqrMagnitude > 0.001f)
            MoveTo(transform.position - flat.normalized * Mathf.Max(2f, Profile.MinimumRange), "enemy_combat_backoff");
        else if (NavMover != null && NavMover.HasDestination)
            NavMover.Stop("enemy_combat_fire");

        OUTL_AttackProfile attack = SelectAttack(time, distance);
        if (AttackDriver != null && attack != null && AttackDriver.FireAt(attack, targetPoint))
            OUTL_StimulusBus.EmitCombat(Entity.Id, transform.position, Profile.CombatStimulusRadius, 1f, 1f, attack.AttackId);
    }

    private OUTL_AttackProfile SelectAttack(float time, float distance)
    {
        OUTL_AttackProfile secondary = Profile.SecondaryAttack;
        if (secondary != null && distance >= Profile.SecondaryMinimumRange && distance <= Profile.SecondaryMaximumRange)
        {
            int id = Entity != null && Entity.Id.IsValid ? Entity.Id.Value : GetInstanceID();
            int bucket = Mathf.FloorToInt(time * 2f);
            float roll = OUTL_HumanRandom.Value01(0x47524E44u, id, bucket);
            if (roll <= Profile.SecondaryChance) return secondary;
        }
        return Profile.PrimaryAttack;
    }

    private void TickHomeBehavior(float time)
    {
        Vector3 homeDelta = HomePosition - transform.position;
        homeDelta.y = 0f;
        if (homeDelta.sqrMagnitude > Profile.WanderRadius * Profile.WanderRadius)
        {
            EnterState(OUTL_EnemyState.ReturnHome);
            MoveTo(HomePosition, "enemy_return_home");
            return;
        }

        EnterState(OUTL_EnemyState.Wander);
        if (time < nextWanderTime) return;
        int id = Entity != null && Entity.Id.IsValid ? Entity.Id.Value : GetInstanceID();
        int sequence = ++wanderSequence;
        float angle = OUTL_HumanRandom.Value01(0x57414E44u, id, sequence) * Mathf.PI * 2f;
        float radius = Mathf.Sqrt(OUTL_HumanRandom.Value01(0x52414449u, id, sequence)) * Mathf.Max(0f, Profile.WanderRadius);
        Vector3 destination = HomePosition + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        MoveTo(destination, "enemy_wander");
        float min = Mathf.Min(Profile.WanderPointIntervalMin, Profile.WanderPointIntervalMax);
        float max = Mathf.Max(Profile.WanderPointIntervalMin, Profile.WanderPointIntervalMax);
        nextWanderTime = time + Mathf.Lerp(min, max, OUTL_HumanRandom.Value01(0x54494D45u, id, sequence));
    }

    private OUTL_EntityRuntime ResolveTarget(OUTL_World world)
    {
        OUTL_EntityRuntime target;
        if (world == null) return null;
        if (Target.IsValid && world.Registry.TryGet(Target, out target) && target != null && target.Adapter != null && !target.Dead)
            return target;

        Target = OUTL_EntityId.None;
        target = ResolveRememberedTarget(world);
        if (target != null)
        {
            Target = target.Id;
            return target;
        }
        return null;
    }

    private bool CanSee(OUTL_EntityRuntime target)
    {
        if (target == null || target.Adapter == null || Profile == null) return false;
        Vector3 origin = Eye != null ? Eye.position : transform.position + Vector3.up * Profile.EyeHeight;
        Vector3 targetPoint = target.Adapter.transform.position + Vector3.up;
        Vector3 delta = targetPoint - origin;
        float distance = delta.magnitude;
        if (distance > Profile.SightDistance || distance <= 0.001f) return false;

        if (Profile.SightAngle < 359f)
        {
            Vector3 flat = delta;
            flat.y = 0f;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (flat.sqrMagnitude > 0.001f && forward.sqrMagnitude > 0.001f)
            {
                float minDot = Mathf.Cos(Profile.SightAngle * 0.5f * Mathf.Deg2Rad);
                if (Vector3.Dot(forward.normalized, flat.normalized) < minDot) return false;
            }
        }

        RaycastHit hit;
        if (!Physics.Raycast(origin, delta / distance, out hit, distance, Profile.LineOfSightMask, QueryTriggerInteraction.Ignore)) return true;
        OUTL_EntityAdapter hitEntity;
        return OUTL_Combat.TryGetEntityFromCollider(hit.collider, out hitEntity) && hitEntity == target.Adapter;
    }

    private void Face(Vector3 direction, float deltaTime)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f) return;
        Quaternion wanted = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, wanted, Profile.TurnDegreesPerSecond * Mathf.Max(0f, deltaTime));
    }

    private void MoveTo(Vector3 destination, string authority)
    {
        if (NavMover != null) NavMover.SetDestination(destination, authority);
    }

    private void EnterState(OUTL_EnemyState next)
    {
        if (State == next) return;
        OUTL_EnemyState old = State;
        State = next;
        if ((next == OUTL_EnemyState.Alert || next == OUTL_EnemyState.Combat) && old != OUTL_EnemyState.Alert && old != OUTL_EnemyState.Combat && BarkDriver != null)
            BarkDriver.NotifyAlert();
    }

    private void ClearTarget()
    {
        Target = OUTL_EntityId.None;
        restoredTargetStableId = string.Empty;
        restoredTargetName = string.Empty;
    }

    private void EnsureHomePosition()
    {
        if (HomeAnchor != null)
        {
            HomePosition = HomeAnchor.position;
            StoreHome();
            return;
        }
        if (Entity == null || Entity.Runtime == null) return;
        if (Entity.Runtime.State.GetFlag("Enemy.HomeSet"))
        {
            HomePosition = new Vector3(
                Entity.Runtime.State.GetFloat("Enemy.HomeX", transform.position.x),
                Entity.Runtime.State.GetFloat("Enemy.HomeY", transform.position.y),
                Entity.Runtime.State.GetFloat("Enemy.HomeZ", transform.position.z));
            return;
        }
        HomePosition = transform.position;
        StoreHome();
    }

    private void StoreHome()
    {
        if (Entity == null || Entity.Runtime == null) return;
        Entity.Runtime.State.SetFlag("Enemy.HomeSet", true);
        Entity.Runtime.State.SetFloat("Enemy.HomeX", HomePosition.x);
        Entity.Runtime.State.SetFloat("Enemy.HomeY", HomePosition.y);
        Entity.Runtime.State.SetFloat("Enemy.HomeZ", HomePosition.z);
    }

    private void WriteRuntimeState()
    {
        if (Entity == null || Entity.Runtime == null) return;
        Entity.Runtime.State.SetString("Enemy.State", State.ToString());
        Entity.Runtime.State.SetInt("Enemy.Target", Target.Value);
        Entity.Runtime.State.SetString("Enemy.TargetStableId", restoredTargetStableId);
        Entity.Runtime.State.SetString("Enemy.TargetName", restoredTargetName);
        Entity.Runtime.State.SetFloat("Enemy.LastKnownX", LastKnownTargetPosition.x);
        Entity.Runtime.State.SetFloat("Enemy.LastKnownY", LastKnownTargetPosition.y);
        Entity.Runtime.State.SetFloat("Enemy.LastKnownZ", LastKnownTargetPosition.z);
    }

    private void ResolveReferences()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (NavMover == null) NavMover = GetComponent<OUTL_NavMeshMover>();
        if (AttackDriver == null) AttackDriver = GetComponent<OUTL_AttackDriver>();
        if (BarkDriver == null) BarkDriver = GetComponent<OUTL_EnemyBarkDriver>();
    }

    public void OUTL_OnPoolSpawn()
    {
        ResolveReferences();
        componentStateRestored = false;
        restoredTargetStableId = string.Empty;
        restoredTargetName = string.Empty;
        State = OUTL_EnemyState.Wander;
        Target = OUTL_EntityId.None;
        LastKnownTargetPosition = Vector3.zero;
        HomePosition = transform.position;
        nextAcquireTime = 0f;
        lastSeenTime = -999f;
        nextWanderTime = 0f;
        wanderSequence = 0;
        if (AutoRegister) Register();
    }

    public void OUTL_OnPoolRelease()
    {
        Unregister();
        Target = OUTL_EntityId.None;
        restoredTargetStableId = string.Empty;
        restoredTargetName = string.Empty;
        componentStateRestored = false;
        if (NavMover != null) NavMover.Stop("enemy_pool_release");
    }

    public void OUTL_Capture(OUTL_ComponentSaveWriter writer)
    {
        if (writer == null) return;
        EnsureHomePosition();
        CaptureTargetAddress();
        float time = OUTL_World.Instance != null ? OUTL_World.Instance.WorldTime : Time.time;
        writer.SetInt("state", (int)State);
        writer.SetString("targetStableId", restoredTargetStableId);
        writer.SetString("targetName", restoredTargetName);
        writer.SetFloat("home.x", HomePosition.x);
        writer.SetFloat("home.y", HomePosition.y);
        writer.SetFloat("home.z", HomePosition.z);
        writer.SetFloat("lastKnown.x", LastKnownTargetPosition.x);
        writer.SetFloat("lastKnown.y", LastKnownTargetPosition.y);
        writer.SetFloat("lastKnown.z", LastKnownTargetPosition.z);
        writer.SetFloat("lastSeenAge", Mathf.Max(0f, time - lastSeenTime));
        writer.SetFloat("acquireDelay", Mathf.Max(0f, nextAcquireTime - time));
        writer.SetFloat("wanderDelay", Mathf.Max(0f, nextWanderTime - time));
        writer.SetInt("wanderSequence", wanderSequence);
    }

    public void OUTL_Restore(OUTL_ComponentSaveReader reader)
    {
        if (reader == null) return;
        float time = OUTL_World.Instance != null ? OUTL_World.Instance.WorldTime : Time.time;
        State = (OUTL_EnemyState)Mathf.Clamp(reader.GetInt("state", (int)State), 0, (int)OUTL_EnemyState.Dead);
        Target = OUTL_EntityId.None;
        restoredTargetStableId = reader.GetString("targetStableId", string.Empty);
        restoredTargetName = reader.GetString("targetName", string.Empty);
        HomePosition = new Vector3(
            reader.GetFloat("home.x", transform.position.x),
            reader.GetFloat("home.y", transform.position.y),
            reader.GetFloat("home.z", transform.position.z));
        LastKnownTargetPosition = new Vector3(
            reader.GetFloat("lastKnown.x", transform.position.x),
            reader.GetFloat("lastKnown.y", transform.position.y),
            reader.GetFloat("lastKnown.z", transform.position.z));
        lastSeenTime = time - Mathf.Max(0f, reader.GetFloat("lastSeenAge", 999f));
        nextAcquireTime = time + Mathf.Max(0f, reader.GetFloat("acquireDelay", 0f));
        nextWanderTime = time + Mathf.Max(0f, reader.GetFloat("wanderDelay", 0f));
        wanderSequence = Mathf.Max(0, reader.GetInt("wanderSequence", wanderSequence));
        componentStateRestored = true;
    }

    public void ResumeAfterMaterialization()
    {
        ResolveReferences();
        if (!componentStateRestored) RestoreFromRuntimeState();
        if (AutoRegister) Register();
        EnsureHomePosition();

        if ((State == OUTL_EnemyState.Alert || State == OUTL_EnemyState.Combat) && LastKnownTargetPosition != Vector3.zero)
            MoveTo(LastKnownTargetPosition, "enemy_resume_target");
        else
            nextWanderTime = 0f;
    }

    private void RestoreFromRuntimeState()
    {
        if (Entity == null || Entity.Runtime == null) return;
        OUTL_StateBag state = Entity.Runtime.State;
        OUTL_EnemyState restoredState;
        if (System.Enum.TryParse(state.GetString("Enemy.State", State.ToString()), true, out restoredState))
            State = restoredState;
        restoredTargetStableId = state.GetString("Enemy.TargetStableId", restoredTargetStableId);
        restoredTargetName = state.GetString("Enemy.TargetName", restoredTargetName);
        LastKnownTargetPosition = new Vector3(
            state.GetFloat("Enemy.LastKnownX", LastKnownTargetPosition.x),
            state.GetFloat("Enemy.LastKnownY", LastKnownTargetPosition.y),
            state.GetFloat("Enemy.LastKnownZ", LastKnownTargetPosition.z));
    }

    private void RememberTarget(OUTL_EntityRuntime target)
    {
        if (target == null) return;
        Target = target.Id;
        restoredTargetStableId = target.StableId ?? string.Empty;
        restoredTargetName = target.TargetName ?? string.Empty;
    }

    private void CaptureTargetAddress()
    {
        OUTL_World world = OUTL_World.Instance;
        OUTL_EntityRuntime target;
        if (world != null && Target.IsValid && world.Registry.TryGet(Target, out target) && target != null)
            RememberTarget(target);
    }

    private OUTL_EntityRuntime ResolveRememberedTarget(OUTL_World world)
    {
        if (world == null) return null;
        OUTL_EntityRuntime target = !string.IsNullOrEmpty(restoredTargetStableId)
            ? world.Registry.FindByStableId(restoredTargetStableId)
            : null;
        if (target == null && !string.IsNullOrEmpty(restoredTargetName))
            target = world.Registry.FindFirstByTargetName(restoredTargetName);
        return target != null && target.Adapter != null && !target.Dead ? target : null;
    }
}
