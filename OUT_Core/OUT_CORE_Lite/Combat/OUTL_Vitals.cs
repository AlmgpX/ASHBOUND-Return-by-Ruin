using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_Vitals : MonoBehaviour, OUTL_ITickable, OUTL_IEventListener, OUTL_ISaveState, OUTL_IPoolReset
{
    public OUTL_EntityAdapter Entity;
    public string HealthKey = "Health";
    public string MaxHealthKey = "MaxHealth";
    public bool InitializeMissingStats = true;
    public float DefaultHealth = 100f;
    public float DefaultMaxHealth = 100f;
    public bool ClampHealthToMax = true;
    public bool KillWhenHealthZero = true;
    public bool BlockMovementWhenDead = true;
    public bool BlockAttacksWhenDead = true;
    public bool DisableCharacterControllerWhenDead = false;
    public bool RegisterTick = true;
    public float TickInterval = 0.05f;

    private bool dead;
    private bool initializedStats;

    public bool IsDead { get { return dead; } }
    public bool OUTL_IsTickEnabled { get { return RegisterTick && isActiveAndEnabled && Entity != null && Entity.Runtime != null; } }
    public OUTL_TickLane OUTL_TickLane { get { return OUTL_TickLane.Full; } }
    public float OUTL_TickInterval { get { return Mathf.Max(0.01f, TickInterval); } }

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
    }

    private void OnEnable()
    {
        if (OUTL_World.Instance != null)
        {
            OUTL_World.Instance.Events.Register(this, OUTL_EventType.Damaged);
            OUTL_World.Instance.Events.Register(this, OUTL_EventType.Healed);
            OUTL_World.Instance.Events.Register(this, OUTL_EventType.Killed);
            if (RegisterTick) OUTL_World.Instance.Scheduler.Register(this);
        }
        TryInitializeStats();
    }

    private void OnDisable()
    {
        if (OUTL_World.Instance != null)
        {
            OUTL_World.Instance.Events.Unregister(this);
            OUTL_World.Instance.Scheduler.Unregister(this);
        }
    }

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        TryInitializeStats();
        Evaluate(world, Vector3.zero, false, OUTL_EntityId.None, false);
    }

    public void OUTL_OnEvent(in OUTL_Event evt, OUTL_World world)
    {
        if (Entity == null || evt.Target != Entity.Id) return;
        TryInitializeStats();
        if (evt.Type == OUTL_EventType.Killed)
        {
            dead = true;
            ApplyDeadState(true);
            return;
        }

        Evaluate(world, evt.Point, true, evt.Source, evt.Type == OUTL_EventType.Damaged);
    }

    [ContextMenu("OUT Evaluate Vitals")]
    public void EvaluateNow()
    {
        TryInitializeStats();
        Evaluate(OUTL_World.Instance, transform.position, true, OUTL_EntityId.None, false);
    }

    public void EnsureInitialized()
    {
        TryInitializeStats();
    }

    public void OUTL_OnPoolSpawn()
    {
        initializedStats = false;
        dead = false;
        TryInitializeStats();
        ApplyDeadState(false);
    }

    public void OUTL_OnPoolRelease()
    {
        dead = false;
        initializedStats = false;
    }

    public void OUTL_Capture(OUTL_SaveData data)
    {
        if (data == null) return;
        data.Set("vitals.dead", dead ? "1" : "0");
    }

    public void OUTL_Restore(OUTL_SaveData data)
    {
        dead = data != null && data.Get("vitals.dead") == "1";
        ApplyDeadState(dead);
    }

    private void Evaluate(OUTL_World world, Vector3 point, bool fromEvent, OUTL_EntityId eventSource, bool suppressKilledEvent)
    {
        if (Entity == null || Entity.Runtime == null) return;
        OUTL_EntityRuntime rt = Entity.Runtime;
        float hp = rt.Stats.Get(HealthKey, 0f);
        float max = rt.Stats.Get(MaxHealthKey, 0f);
        if (ClampHealthToMax && max > 0f && hp > max) rt.Stats.Set(HealthKey, max);
        bool shouldDead = KillWhenHealthZero && rt.Stats.Get(HealthKey, 0f) <= 0f;
        if ((rt.Dead || rt.LifeState == OUTL_LifeState.Dead) && shouldDead)
        {
            dead = true;
            ApplyDeadState(true);
            return;
        }

        if (shouldDead == dead) return;
        dead = shouldDead;
        ApplyDeadState(dead);
        if (world != null)
        {
            if (dead)
            {
                OUTL_DeathRuntime.TryKill(rt, eventSource, string.IsNullOrEmpty(rt.DeathKey) ? "HealthZero" : rt.DeathKey, point != Vector3.zero ? point : transform.position, world);
            }
            else
            {
                OUTL_DeathRuntime.MarkAlive(rt);
                world.Events.Emit(new OUTL_Event(OUTL_EventType.Healed, OUTL_EntityId.None, Entity.Id) { Key = "Revived", Point = transform.position });
            }
        }
    }

    private void TryInitializeStats()
    {
        if (initializedStats || !InitializeMissingStats) return;
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (Entity == null || Entity.Runtime == null) return;

        OUTL_EntityRuntime rt = Entity.Runtime;
        float max = rt.Stats.Get(MaxHealthKey, 0f);
        if (max <= 0f)
        {
            max = Mathf.Max(1f, DefaultMaxHealth > 0f ? DefaultMaxHealth : DefaultHealth);
            rt.Stats.Set(MaxHealthKey, max);
        }

        float hp = rt.Stats.Get(HealthKey, 0f);
        if (hp <= 0f)
            rt.Stats.Set(HealthKey, Mathf.Clamp(DefaultHealth > 0f ? DefaultHealth : max, 1f, max));

        initializedStats = true;
    }

    private void ApplyDeadState(bool value)
    {
        if (Entity != null && Entity.Runtime != null) Entity.Runtime.State.SetFlag(OUTL_StateId.Dead, value);
        OUTL_BasicPlayerController controller = GetComponent<OUTL_BasicPlayerController>();
        if (controller != null) controller.SetOUTLDead(value);
        OUTL_AttackDriver attack = GetComponent<OUTL_AttackDriver>();
        if (attack != null) attack.BlockedByVitals = value && BlockAttacksWhenDead;
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null && DisableCharacterControllerWhenDead) cc.enabled = !value;
    }
}
