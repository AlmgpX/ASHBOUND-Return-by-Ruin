using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_PlayerDeathBridge : MonoBehaviour, OUTL_IEventListener, OUTL_IPoolReset
{
    public OUTL_EntityAdapter Entity;
    public OUTL_BasicPlayerController Controller;
    public Behaviour[] LocalOnlyBehaviours;
    public bool DisableLocalControlOnDeath = true;
    public bool KeepPlayerObjectAlive = true;
    public bool WaitingForRespawn;

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (Controller == null) Controller = GetComponent<OUTL_BasicPlayerController>();
    }

    private void OnEnable()
    {
        WaitingForRespawn = false;
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
        if (evt.Type == OUTL_EventType.Killed) ApplyDead(true);
        else if (evt.Type == OUTL_EventType.Healed) ApplyDead(false);
    }

    public void ResetForRespawn(Vector3 position, Quaternion rotation, float health = 100f)
    {
        if (Entity == null || Entity.Runtime == null) return;
        transform.SetPositionAndRotation(position, rotation);
        Entity.Runtime.Stats.Set(OUTL_StatId.Health, Mathf.Max(1f, health));
        OUTL_DeathRuntime.MarkAlive(Entity.Runtime);
        ApplyDead(false);
        if (OUTL_World.Instance != null)
            OUTL_World.Instance.Events.Emit(new OUTL_Event(OUTL_EventType.Healed, Entity.Id, Entity.Id) { Key = "Respawn", FloatValue = health, Point = position });
    }

    public void OUTL_OnPoolSpawn()
    {
        ApplyDead(false);
    }

    public void OUTL_OnPoolRelease()
    {
        WaitingForRespawn = false;
    }

    private void ApplyDead(bool dead)
    {
        WaitingForRespawn = dead;
        if (Controller != null && DisableLocalControlOnDeath) Controller.SetOUTLDead(dead);
        if (LocalOnlyBehaviours == null) return;
        for (int i = 0; i < LocalOnlyBehaviours.Length; i++)
            if (LocalOnlyBehaviours[i] != null)
                LocalOnlyBehaviours[i].enabled = !dead;
    }
}
