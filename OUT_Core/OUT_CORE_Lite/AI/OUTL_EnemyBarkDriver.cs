using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_EnemyBarkDriver : MonoBehaviour, OUTL_IEventListener, OUTL_IPoolReset
{
    public OUTL_EntityAdapter Entity;
    public OUTL_AudioProfile Alert;
    public OUTL_AudioProfile Combat;
    public OUTL_AudioProfile Pain;
    public OUTL_AudioProfile Death;
    public float AlertCooldown = 4f;
    public float CombatCooldown = 7f;
    public float PainCooldown = 1.5f;

    private float nextAlert;
    private float nextCombat;
    private float nextPain;

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
    }

    private void OnEnable()
    {
        if (OUTL_World.Instance != null)
        {
            OUTL_World.Instance.Events.Register(this, OUTL_EventType.Damaged);
            OUTL_World.Instance.Events.Register(this, OUTL_EventType.Killed);
        }
    }

    private void OnDisable()
    {
        if (OUTL_World.Instance != null) OUTL_World.Instance.Events.Unregister(this);
    }

    public void NotifyAlert()
    {
        float time = ReadTime();
        if (time < nextAlert || Alert == null) return;
        nextAlert = time + Mathf.Max(0f, AlertCooldown);
        Alert.Play(transform.position);
    }

    public void NotifyCombat()
    {
        float time = ReadTime();
        if (time < nextCombat || Combat == null) return;
        nextCombat = time + Mathf.Max(0f, CombatCooldown);
        Combat.Play(transform.position);
    }

    public void OUTL_OnEvent(in OUTL_Event evt, OUTL_World world)
    {
        if (Entity == null || evt.Target != Entity.Id) return;
        float time = world != null ? world.WorldTime : Time.time;
        if (evt.Type == OUTL_EventType.Damaged)
        {
            if (time < nextPain || Pain == null) return;
            nextPain = time + Mathf.Max(0f, PainCooldown);
            Pain.Play(transform.position);
        }
        else if (evt.Type == OUTL_EventType.Killed && Death != null)
        {
            Death.Play(transform.position);
        }
    }

    public void OUTL_OnPoolSpawn()
    {
        nextAlert = 0f;
        nextCombat = 0f;
        nextPain = 0f;
    }

    public void OUTL_OnPoolRelease()
    {
        nextAlert = 0f;
        nextCombat = 0f;
        nextPain = 0f;
    }

    private static float ReadTime()
    {
        OUTL_World world = OUTL_World.Instance;
        return world != null ? world.WorldTime : Time.time;
    }
}
