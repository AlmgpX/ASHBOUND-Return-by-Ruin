using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_DeathRuntime : MonoBehaviour, OUTL_IEventListener, OUTL_IComponentSaveParticipant, OUTL_IPoolReset
{
    public OUTL_EntityAdapter Entity;
    public OUTL_LifeState LifeState = OUTL_LifeState.Alive;
    public bool Dead;
    public float DeathTime;
    public OUTL_EntityId KillerId = OUTL_EntityId.None;
    public string DeathKey = "";
    public bool EmitDeathStimulus = true;

    public string OUTL_SaveKey { get { return "OUTL_DeathRuntime"; } }

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
    }

    private void OnEnable()
    {
        SyncFromRuntime();
        if (OUTL_World.Instance != null)
        {
            OUTL_World.Instance.Events.Register(this, OUTL_EventType.Killed);
            OUTL_World.Instance.Events.Register(this, OUTL_EventType.Healed);
        }
    }

    private void OnDisable()
    {
        if (OUTL_World.Instance != null) OUTL_World.Instance.Events.Unregister(this);
    }

    public void OUTL_OnEvent(in OUTL_Event evt, OUTL_World world)
    {
        if (Entity == null || evt.Target != Entity.Id) return;
        if (evt.Type == OUTL_EventType.Killed)
        {
            SyncFromRuntime();
            return;
        }

        if (evt.Type == OUTL_EventType.Healed && LifeState == OUTL_LifeState.Dead)
            MarkAlive(Entity.Runtime);
    }

    public void OUTL_Capture(OUTL_ComponentSaveWriter writer)
    {
        if (writer == null) return;
        SyncFromRuntime();
        writer.SetInt("lifeState", (int)LifeState);
        writer.SetFlag("dead", Dead);
        writer.SetFloat("deathTime", DeathTime);
        writer.SetInt("killerId", KillerId.Value);
        writer.SetString("deathKey", DeathKey);
    }

    public void OUTL_Restore(OUTL_ComponentSaveReader reader)
    {
        if (reader == null) return;
        LifeState = (OUTL_LifeState)Mathf.Clamp(reader.GetInt("lifeState", (int)OUTL_LifeState.Alive), 0, (int)OUTL_LifeState.DormantDead);
        Dead = reader.GetFlag("dead", LifeState == OUTL_LifeState.Dead || LifeState == OUTL_LifeState.DormantDead);
        DeathTime = reader.GetFloat("deathTime", 0f);
        KillerId = new OUTL_EntityId(reader.GetInt("killerId", 0));
        DeathKey = reader.GetString("deathKey", "");
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        ApplyToRuntime(Entity != null ? Entity.Runtime : null, LifeState, Dead, DeathTime, KillerId, DeathKey);
    }

    public void OUTL_OnPoolSpawn()
    {
        MarkAlive(Entity != null ? Entity.Runtime : null);
    }

    public void OUTL_OnPoolRelease()
    {
        LifeState = OUTL_LifeState.Alive;
        Dead = false;
        DeathTime = 0f;
        KillerId = OUTL_EntityId.None;
        DeathKey = "";
    }

    public static bool TryKill(OUTL_EntityRuntime runtime, OUTL_EntityId killerId, string deathKey, Vector3 point, OUTL_World world)
    {
        if (runtime == null) return false;
        if (runtime.LifeState == OUTL_LifeState.Dead || runtime.LifeState == OUTL_LifeState.DormantDead || runtime.Dead)
            return false;

        if (runtime.Adapter != null && !OUTL_NetworkAuthority.CanKill(runtime.Adapter))
        {
            OUTL_NetworkAuthority.TraceBlocked("kill", runtime.Adapter);
            return false;
        }

        float time = world != null ? world.WorldTime : Time.time;
        ApplyToRuntime(runtime, OUTL_LifeState.Dead, true, time, killerId, deathKey);

        OUTL_DeathRuntime deathRuntime = runtime.Adapter != null ? runtime.Adapter.GetComponent<OUTL_DeathRuntime>() : null;
        if (deathRuntime != null) deathRuntime.SyncFromRuntime();

        if (world != null)
        {
            Vector3 deathPoint = point != Vector3.zero ? point : (runtime.Adapter != null ? runtime.Adapter.transform.position : Vector3.zero);
            world.Events.Emit(new OUTL_Event(OUTL_EventType.Killed, killerId, runtime.Id) { Key = deathKey, Point = deathPoint, FloatValue = time });
            OUTL_StimulusBus.EmitDeath(runtime.Id, deathPoint, 24f, 1f, 1f, deathKey);
        }

        return true;
    }

    public static void MarkAlive(OUTL_EntityRuntime runtime)
    {
        ApplyToRuntime(runtime, OUTL_LifeState.Alive, false, 0f, OUTL_EntityId.None, "");
    }

    private void SyncFromRuntime()
    {
        OUTL_EntityRuntime runtime = Entity != null ? Entity.Runtime : null;
        if (runtime == null) return;
        LifeState = runtime.LifeState;
        Dead = runtime.Dead;
        DeathTime = runtime.DeathTime;
        KillerId = runtime.KillerId;
        DeathKey = runtime.DeathKey;
    }

    private static void ApplyToRuntime(OUTL_EntityRuntime runtime, OUTL_LifeState lifeState, bool dead, float deathTime, OUTL_EntityId killerId, string deathKey)
    {
        if (runtime == null) return;
        runtime.LifeState = lifeState;
        runtime.Dead = dead;
        runtime.DeathTime = deathTime;
        runtime.KillerId = killerId;
        runtime.DeathKey = deathKey ?? "";
        runtime.State.SetString(OUTL_LifecycleKeys.LifeState, lifeState.ToString());
        runtime.State.SetFlag(OUTL_StateId.Dead, dead);
        runtime.State.SetFlag(OUTL_LifecycleKeys.Dead, dead);
        runtime.State.SetFloat(OUTL_LifecycleKeys.DeathTime, deathTime);
        runtime.State.SetInt(OUTL_LifecycleKeys.KillerId, killerId.Value);
        runtime.State.SetString(OUTL_LifecycleKeys.DeathKey, deathKey ?? "");
    }
}
