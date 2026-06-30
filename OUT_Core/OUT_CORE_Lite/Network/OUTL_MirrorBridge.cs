using UnityEngine;

#if OUTL_MIRROR
using Mirror;

[RequireComponent(typeof(OUTL_NetworkIdentityLite))]
public sealed partial class OUTL_MirrorEntityBridge : NetworkBehaviour
{
    public OUTL_NetworkIdentityLite Identity;
    public OUTL_EntityAdapter Entity;
    public bool DisableLocalControlForRemotePlayers = true;
    public Behaviour[] LocalOnlyBehaviours;

    [Header("Server Request Validation")]
    public float MaxDamageRequestDistance = 80f;
    public float MaxPickupRequestDistance = 3f;
    public float MaxRequestedDamage = 250f;
    public float DamageRequestCooldown = 0.08f;
    public float PickupRequestCooldown = 0.12f;
    public bool RequireAttackProfileForDamageRequest = true;
    public bool RequireDamageLineOfSight = true;
    public LayerMask DamageLineOfSightMask = ~0;
    public float DamageProfileTolerance = 1.5f;

    [SyncVar] private int syncNetId;
    [SyncVar] private string stableKey;
    [SyncVar] private Vector3 syncPosition;
    [SyncVar] private Quaternion syncRotation;
    [SyncVar] private float syncHealth;
    [SyncVar] private float syncArmor;
    [SyncVar] private OUTL_RuntimeTier syncTier;

    private float nextSend;
    private float nextDamageRequestTime;
    private float nextPickupRequestTime;
    private bool localModeApplied;

    private void Awake()
    {
        if (Identity == null) Identity = GetComponent<OUTL_NetworkIdentityLite>();
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (Identity == null) Identity = GetComponent<OUTL_NetworkIdentityLite>();
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        syncNetId = Identity != null ? Identity.NetId : 0;
        stableKey = Identity != null ? Identity.StableNetworkKey : name;
        CaptureToSyncVars();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        localModeApplied = false;
        ApplySyncVarsToEntity();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        localModeApplied = false;
        ApplyLocalAuthorityMode();
    }

    private void Update()
    {
        if (!localModeApplied) ApplyLocalAuthorityMode();

        if (isServer)
        {
            float now = Time.unscaledTime;
            float interval = Identity != null ? Mathf.Max(0.01f, Identity.SendInterval) : 0.05f;
            if (now >= nextSend)
            {
                nextSend = now + interval;
                CaptureToSyncVars();
            }
        }
        else
        {
            ApplyRemoteTransform();
            ApplySyncVarsToEntity();
        }
    }

    [Command]
    public void CmdSendOUTLCommand(int commandType, string targetName, string key, float floatValue, int intValue, Vector3 point)
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null || string.IsNullOrEmpty(targetName)) return;
        OUTL_Command command = new OUTL_Command((OUTL_CommandType)commandType, Entity != null ? Entity.Id : OUTL_EntityId.None, OUTL_EntityId.None)
        {
            Key = key,
            FloatValue = floatValue,
            IntValue = intValue,
            Point = point
        };
        world.Commands.SendToTargetName(targetName, command);
    }

    [Command]
    public void CmdRequestDamage(int targetEntityId, float damage, string damageKey, Vector3 point)
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return;
        OUTL_EntityRuntime target;
        string reason;
        if (!ValidateDamageRequest(world, targetEntityId, damage, damageKey, point, out target, out reason))
        {
            OUTL_DebugLog.Log(OUTL_DebugChannel.Combat, "blocked CmdRequestDamage: " + reason, true);
            return;
        }

        OUTL_Combat.ApplyDamage(Entity.Id, target.Id, damage, point != Vector3.zero ? point : GetEntityPoint(target), damageKey);
    }

    [Command]
    public void CmdRequestPickup(int pickupEntityId)
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return;
        OUTL_EntityRuntime runtime;
        if (!world.Registry.TryGet(new OUTL_EntityId(pickupEntityId), out runtime) || runtime == null || runtime.Adapter == null) return;
        OUTL_ItemPickup pickup = runtime.Adapter.GetComponent<OUTL_ItemPickup>();
        string reason;
        if (!ValidatePickupRequest(pickup, out reason))
        {
            OUTL_DebugLog.Log(OUTL_DebugChannel.Loot, "blocked CmdRequestPickup: " + reason, true);
            return;
        }
        pickup.TryPickup(Entity);
    }

    private bool ValidateDamageRequest(OUTL_World world, int targetEntityId, float damage, string damageKey, Vector3 point, out OUTL_EntityRuntime target, out string reason)
    {
        target = null;
        reason = "";
        float now = Time.unscaledTime;
        if (now < nextDamageRequestTime) { reason = "cooldown"; return false; }
        nextDamageRequestTime = now + Mathf.Max(0f, DamageRequestCooldown);

        if (Entity == null || Entity.Runtime == null || !Entity.Id.IsValid) { reason = "missing sender entity"; return false; }
        if (IsDead(Entity.Runtime)) { reason = "sender dead"; return false; }
        if (damage <= 0f || damage > Mathf.Max(0.01f, MaxRequestedDamage)) { reason = "damage out of range"; return false; }
        OUTL_EntityId targetId = new OUTL_EntityId(targetEntityId);
        if (!targetId.IsValid || !world.Registry.TryGet(targetId, out target) || target == null || target.Adapter == null) { reason = "target missing"; return false; }
        if (target.Id == Entity.Id) { reason = "self damage request"; return false; }
        if (IsDead(target)) { reason = "target dead"; return false; }

        Vector3 sourcePoint = GetEntityPoint(Entity.Runtime);
        Vector3 targetPoint = GetEntityPoint(target);
        float maxDistance = Mathf.Max(0.1f, MaxDamageRequestDistance);
        if ((targetPoint - sourcePoint).sqrMagnitude > maxDistance * maxDistance) { reason = "target too far"; return false; }
        if (point != Vector3.zero && (point - targetPoint).sqrMagnitude > 4f) { reason = "hit point not near target"; return false; }

        OUTL_AttackProfile profile = ResolvePermittedProfile(target, damage, damageKey);
        if (RequireAttackProfileForDamageRequest && profile == null) { reason = "no permitted attack profile"; return false; }
        if (profile != null)
        {
            float allowedRange = Mathf.Max(0.1f, profile.Range + profile.Radius + 0.5f);
            if ((targetPoint - sourcePoint).sqrMagnitude > allowedRange * allowedRange) { reason = "profile range"; return false; }
        }

        if (RequireDamageLineOfSight && !HasLineOfSight(sourcePoint, targetPoint, target)) { reason = "line of sight"; return false; }
        return true;
    }

    private bool ValidatePickupRequest(OUTL_ItemPickup pickup, out string reason)
    {
        reason = "";
        float now = Time.unscaledTime;
        if (now < nextPickupRequestTime) { reason = "cooldown"; return false; }
        nextPickupRequestTime = now + Mathf.Max(0f, PickupRequestCooldown);

        if (Entity == null || Entity.Runtime == null || !Entity.Id.IsValid) { reason = "missing sender entity"; return false; }
        if (IsDead(Entity.Runtime)) { reason = "sender dead"; return false; }
        if (pickup == null) { reason = "pickup missing"; return false; }
        return pickup.CanPickup(Entity, Mathf.Max(0.1f, MaxPickupRequestDistance), out reason);
    }

    private OUTL_AttackProfile ResolvePermittedProfile(OUTL_EntityRuntime target, float requestedDamage, string damageKey)
    {
        OUTL_AttackDriver driver = Entity != null ? Entity.GetComponent<OUTL_AttackDriver>() : null;
        if (driver == null) return RequireAttackProfileForDamageRequest ? null : null;
        OUTL_AttackProfile profile = MatchProfile(driver.Primary, requestedDamage, damageKey);
        if (profile != null) return profile;
        profile = MatchProfile(driver.Secondary, requestedDamage, damageKey);
        if (profile != null) return profile;
        return MatchProfile(driver.Melee, requestedDamage, damageKey);
    }

    private OUTL_AttackProfile MatchProfile(OUTL_AttackProfile profile, float requestedDamage, string damageKey)
    {
        if (profile == null) return null;
        if (requestedDamage > Mathf.Max(0.01f, profile.Damage * Mathf.Max(1f, DamageProfileTolerance))) return null;
        if (!string.IsNullOrEmpty(damageKey) && !string.IsNullOrEmpty(profile.HitDamageKey) && !damageKey.StartsWith(profile.HitDamageKey)) return null;
        return profile;
    }

    private bool HasLineOfSight(Vector3 sourcePoint, Vector3 targetPoint, OUTL_EntityRuntime target)
    {
        Vector3 delta = targetPoint - sourcePoint;
        float distance = delta.magnitude;
        if (distance <= 0.05f) return true;
        RaycastHit hit;
        if (!Physics.Raycast(sourcePoint, delta / distance, out hit, distance, DamageLineOfSightMask, QueryTriggerInteraction.Ignore)) return true;
        OUTL_EntityAdapter hitEntity;
        return OUTL_Combat.TryGetEntityFromCollider(hit.collider, out hitEntity) && target != null && hitEntity != null && hitEntity.Id == target.Id;
    }

    private static bool IsDead(OUTL_EntityRuntime runtime)
    {
        return runtime == null || runtime.Dead || runtime.LifeState == OUTL_LifeState.Dead || runtime.State.GetFlag(OUTL_StateId.Dead) || runtime.Stats.Get(OUTL_StatId.Health, 1f) <= 0f;
    }

    private static Vector3 GetEntityPoint(OUTL_EntityRuntime runtime)
    {
        if (runtime == null || runtime.Adapter == null) return Vector3.zero;
        Collider c = runtime.Adapter.GetComponentInChildren<Collider>();
        return c != null ? c.bounds.center : runtime.Adapter.transform.position + Vector3.up;
    }

    private void CaptureToSyncVars()
    {
        if (Identity != null)
        {
            syncNetId = Identity.NetId;
            stableKey = Identity.StableNetworkKey;
        }
        syncPosition = transform.position;
        syncRotation = transform.rotation;
        if (Entity != null && Entity.Runtime != null)
        {
            syncHealth = Entity.Runtime.Stats.Get(OUTL_StatId.Health, syncHealth);
            syncArmor = Entity.Runtime.Stats.Get(OUTL_StatId.Armor, syncArmor);
            syncTier = Entity.Runtime.Tier;
        }
    }

    private void ApplyRemoteTransform()
    {
        if (Identity == null || !Identity.ReplicateTransform) return;
        if (isLocalPlayer) return;
        float snap = Identity.PositionSnapDistance;
        if (snap > 0f && (transform.position - syncPosition).sqrMagnitude > snap * snap)
            transform.position = syncPosition;
        else
            transform.position = Vector3.Lerp(transform.position, syncPosition, Time.deltaTime * 12f);
        transform.rotation = Quaternion.Slerp(transform.rotation, syncRotation, Time.deltaTime * 12f);
    }

    private void ApplySyncVarsToEntity()
    {
        if (Entity == null || Entity.Runtime == null || Identity == null) return;
        Identity.NetId = syncNetId;
        if (Identity.ReplicateStats)
        {
            Entity.Runtime.Stats.Set(OUTL_StatId.Health, syncHealth);
            Entity.Runtime.Stats.Set(OUTL_StatId.Armor, syncArmor);
        }
        Entity.Runtime.Tier = syncTier;
    }

    private void ApplyLocalAuthorityMode()
    {
        if (!DisableLocalControlForRemotePlayers)
        {
            localModeApplied = true;
            return;
        }

        if (isLocalPlayer)
        {
            SetLocalOnlyBehaviours(true);
            localModeApplied = true;
            return;
        }

        if (!isServer && isClient)
        {
            SetLocalOnlyBehaviours(false);
            localModeApplied = true;
        }
    }

    private void SetLocalOnlyBehaviours(bool enabled)
    {
        if (LocalOnlyBehaviours == null) return;
        for (int i = 0; i < LocalOnlyBehaviours.Length; i++)
            if (LocalOnlyBehaviours[i] != null)
                LocalOnlyBehaviours[i].enabled = enabled;
    }
}
#else
[DisallowMultipleComponent]
public sealed partial class OUTL_MirrorEntityBridge : MonoBehaviour
{
    [TextArea(3, 8)] public string Info = "Install Mirror and add OUTL_MIRROR to Scripting Define Symbols to enable this bridge. Without Mirror this component is an inert placeholder so OUT CORE Lite still compiles.";
}
#endif
