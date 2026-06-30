using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class OUTL_DeathHandler : MonoBehaviour, OUTL_IEventListener, OUTL_ITickable, OUTL_IPoolReset
{
    public OUTL_EntityAdapter Entity;
    public OUTL_CharacterAnimationBridge AnimationBridge;
    public GameObject DeathVFX;
    public GameObject GibsPrefab;
    public AudioClip DeathSound;
    public bool DisableAI = true;
    public bool DisableColliders = false;
    public bool DisableRenderers = false;
    public bool QueueDespawn = true;
    public float DespawnDelay = 4f;
    public bool DespawnPlayerObjects = false;

    private bool dead;
    private bool despawnQueued;
    private float despawnTime;

    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled && dead && QueueDespawn && OUTL_World.Instance != null; } }
    public OUTL_TickLane OUTL_TickLane { get { return OUTL_TickLane.Logic; } }
    public float OUTL_TickInterval { get { return 0.25f; } }

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (AnimationBridge == null) AnimationBridge = GetComponentInChildren<OUTL_CharacterAnimationBridge>(true);
    }

    private void OnEnable()
    {
        dead = false;
        despawnQueued = false;
        if (OUTL_World.Instance != null)
        {
            OUTL_World.Instance.Events.Register(this, OUTL_EventType.Damaged);
            OUTL_World.Instance.Events.Register(this, OUTL_EventType.Killed);
        }
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
        if (!dead || !QueueDespawn || world == null) return;
        if (despawnQueued) return;
        if (time < despawnTime) return;
        if (IsPlayerEntity() && !DespawnPlayerObjects) return;
        despawnQueued = true;
        world.QueueDespawn(Entity != null ? Entity.Id : OUTL_EntityId.None);
        world.Scheduler.Unregister(this);
    }

    public void OUTL_OnEvent(in OUTL_Event evt, OUTL_World world)
    {
        if (dead || Entity == null || evt.Target != Entity.Id) return;
        if (evt.Type == OUTL_EventType.Killed) Die(evt.Point);
        else if (evt.Type == OUTL_EventType.Damaged && AnimationBridge != null) AnimationBridge.NotifyHurt();
    }

    public void Die(Vector3 point)
    {
        if (dead) return;
        dead = true;
        despawnQueued = false;
        despawnTime = ReadWorldTime() + Mathf.Max(0f, DespawnDelay);
        if (QueueDespawn && OUTL_World.Instance != null) OUTL_World.Instance.Scheduler.Register(this);

        if (AnimationBridge != null) AnimationBridge.NotifyDeath();
        if (DeathVFX != null) OUTL_PoolSystem.SpawnShared(DeathVFX, point != Vector3.zero ? point : transform.position, Quaternion.identity);
        if (GibsPrefab != null) OUTL_PoolSystem.SpawnShared(GibsPrefab, point != Vector3.zero ? point : transform.position, transform.rotation);
        if (DeathSound != null) OUTL_PoolSystem.PlayClipShared(DeathSound, transform.position);

        if (DisableAI)
        {
            OUTL_AIActor ai = GetComponent<OUTL_AIActor>();
            if (ai != null) ai.enabled = false;
            OUTL_NPCBehaviorController npc = GetComponent<OUTL_NPCBehaviorController>();
            if (npc != null) npc.ApplyDeadState();
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;
            OUTL_NavMeshMover mover = GetComponent<OUTL_NavMeshMover>();
            if (mover != null) mover.Stop("death_handler");
        }

        OUTL_AttackDriver attack = GetComponent<OUTL_AttackDriver>();
        if (attack != null) attack.BlockedByVitals = true;

        if (DisableColliders)
        {
            Collider[] cols = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;
        }

        if (DisableRenderers)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = false;
        }
    }

    private bool IsPlayerEntity()
    {
        if (GetComponent<OUTL_PlayerDeathBridge>() != null) return true;
        if (GetComponent<OUTL_BasicPlayerController>() != null) return true;
        return Entity != null && Entity.Runtime != null && (Entity.Runtime.HasTag("Player") || Entity.Runtime.ClassName == "player");
    }

    private static float ReadWorldTime()
    {
        OUTL_World world = OUTL_World.Instance;
        return world != null ? world.WorldTime : Time.time;
    }

    public void OUTL_OnPoolSpawn()
    {
        dead = false;
        despawnQueued = false;
        despawnTime = 0f;
    }

    public void OUTL_OnPoolRelease()
    {
        dead = false;
        despawnQueued = false;
        despawnTime = 0f;
        if (OUTL_World.Instance != null) OUTL_World.Instance.Scheduler.Unregister(this);
    }
}
