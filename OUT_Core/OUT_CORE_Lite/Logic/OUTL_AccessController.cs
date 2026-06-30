using System;
using UnityEngine;

public enum OUTL_AccessConsumePolicy : byte
{
    Never = 0,
    OnFirstGrant = 1,
    EveryGrant = 2
}

[Serializable]
public sealed class OUTL_AccessRequirement
{
    public string RequirementId = "access.requirement";
    public OUTL_ConditionDef Condition = new OUTL_ConditionDef();
    [Tooltip("Only HasItem requirements can be consumed. Consumption is still controlled by ConsumePolicy.")]
    public bool ConsumeItem;
}

[DisallowMultipleComponent]
public sealed class OUTL_AccessController : MonoBehaviour, OUTL_ICommandGuard, OUTL_ICommandReceiver, OUTL_IComponentSaveParticipant
{
    [Header("OUTL")]
    public OUTL_EntityAdapter Entity;

    [Header("Lock State")]
    public bool StartsLocked = true;
    public bool IsLocked = true;
    public bool UnlockPermanentlyOnGrant = true;
    public bool AllowLockedWithoutRequirements;

    [Header("Guarded Commands")]
    public OUTL_CommandType[] GuardedCommands =
    {
        OUTL_CommandType.Use,
        OUTL_CommandType.Open,
        OUTL_CommandType.Activate
    };

    [Header("Requirements")]
    public OUTL_AccessRequirement[] Requirements;
    public OUTL_AccessConsumePolicy ConsumePolicy = OUTL_AccessConsumePolicy.Never;

    [Header("Outputs")]
    public OUTL_OutputLink[] Outputs;
    public string GrantedEventName = "OnAccessGranted";
    public string DeniedEventName = "OnAccessDenied";
    public string LockedEventName = "OnLocked";
    public string UnlockedEventName = "OnUnlocked";

    [Header("Feedback")]
    public AudioClip GrantedSound;
    public AudioClip DeniedSound;
    [Range(0f, 1f)] public float Volume = 0.85f;

    [Header("Runtime")]
    public bool HasGranted;
    public int GrantCount;
    public string LastDeniedRequirement;

    public string OUTL_SaveKey { get { return "OUTL_AccessController"; } }

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        IsLocked = StartsLocked;
        PushState();
    }

    private void OnEnable()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (Entity != null) Entity.RebuildCommandGuardCache();
        PushState();
    }

    private void Start()
    {
        PushState();
    }

    public bool OUTL_Allows(in OUTL_Command command, OUTL_World world)
    {
        if (!IsGuarded(command.Type) || !IsLocked) return true;
        if (Requirements == null || Requirements.Length == 0) return AllowLockedWithoutRequirements;

        bool hasRequirement = false;
        for (int i = 0; i < Requirements.Length; i++)
        {
            OUTL_AccessRequirement requirement = Requirements[i];
            if (requirement == null || requirement.Condition == null) continue;
            hasRequirement = true;
            if (!OUTL_Rules.Check(requirement.Condition, command.Source, command.Target, world))
                return false;
        }

        return hasRequirement || AllowLockedWithoutRequirements;
    }

    public void OUTL_OnCommandAccepted(in OUTL_Command command, OUTL_World world)
    {
        if (!IsGuarded(command.Type) || !IsLocked) return;

        bool consume = ConsumePolicy == OUTL_AccessConsumePolicy.EveryGrant
            || (ConsumePolicy == OUTL_AccessConsumePolicy.OnFirstGrant && !HasGranted);

        if (consume) ConsumeRequirements(command.Source, world);

        bool wasLocked = IsLocked;
        HasGranted = true;
        GrantCount++;
        LastDeniedRequirement = string.Empty;
        if (UnlockPermanentlyOnGrant) IsLocked = false;
        PushState();

        Fire(world, command.Source, GrantedEventName, command.Point, GrantCount);
        if (wasLocked && !IsLocked) Fire(world, command.Source, UnlockedEventName, command.Point, GrantCount);
        if (GrantedSound != null) OUTL_PoolSystem.PlayClipShared(GrantedSound, transform.position, Volume);
    }

    public void OUTL_OnCommandDenied(in OUTL_Command command, OUTL_World world)
    {
        LastDeniedRequirement = FindFailedRequirement(command, world);
        PushState();
        Fire(world, command.Source, DeniedEventName, command.Point, 0);
        if (DeniedSound != null) OUTL_PoolSystem.PlayClipShared(DeniedSound, transform.position, Volume);
    }

    public bool OUTL_CanReceive(in OUTL_Command command, OUTL_World world)
    {
        return command.Type == OUTL_CommandType.Lock || command.Type == OUTL_CommandType.Unlock;
    }

    public void OUTL_Receive(in OUTL_Command command, OUTL_World world)
    {
        if (command.Type == OUTL_CommandType.Lock)
            SetLocked(true, world, command.Source, command.Point);
        else if (command.Type == OUTL_CommandType.Unlock)
            SetLocked(false, world, command.Source, command.Point);
    }

    [ContextMenu("OUT Lock")]
    public void LockNow()
    {
        SetLocked(true, OUTL_World.Instance, Entity != null ? Entity.Id : OUTL_EntityId.None, transform.position);
    }

    [ContextMenu("OUT Unlock")]
    public void UnlockNow()
    {
        SetLocked(false, OUTL_World.Instance, Entity != null ? Entity.Id : OUTL_EntityId.None, transform.position);
    }

    public void SetLocked(bool value, OUTL_World world, OUTL_EntityId source, Vector3 point)
    {
        if (IsLocked == value) return;
        IsLocked = value;
        PushState();
        Fire(world, source, value ? LockedEventName : UnlockedEventName, point, value ? 1 : 0);
    }

    public void OUTL_Capture(OUTL_ComponentSaveWriter writer)
    {
        if (writer == null) return;
        writer.SetInt("locked", IsLocked ? 1 : 0);
        writer.SetInt("granted", HasGranted ? 1 : 0);
        writer.SetInt("grantCount", GrantCount);
        writer.SetString("lastDenied", LastDeniedRequirement);
        OUTL_OutputSaveUtility.Capture(writer, "outputs", Outputs);
    }

    public void OUTL_Restore(OUTL_ComponentSaveReader reader)
    {
        if (reader == null) return;
        IsLocked = reader.GetInt("locked", StartsLocked ? 1 : 0) != 0;
        HasGranted = reader.GetInt("granted", 0) != 0;
        GrantCount = Mathf.Max(0, reader.GetInt("grantCount", 0));
        LastDeniedRequirement = reader.GetString("lastDenied", string.Empty);
        OUTL_OutputSaveUtility.Restore(reader, "outputs", Outputs);
        PushState();
    }

    private bool IsGuarded(OUTL_CommandType type)
    {
        if (type == OUTL_CommandType.Lock || type == OUTL_CommandType.Unlock) return false;
        if (GuardedCommands == null) return false;
        for (int i = 0; i < GuardedCommands.Length; i++)
            if (GuardedCommands[i] == type)
                return true;
        return false;
    }

    private void ConsumeRequirements(OUTL_EntityId source, OUTL_World world)
    {
        if (world == null || Requirements == null) return;
        for (int i = 0; i < Requirements.Length; i++)
        {
            OUTL_AccessRequirement requirement = Requirements[i];
            OUTL_ConditionDef condition = requirement != null ? requirement.Condition : null;
            if (requirement == null || !requirement.ConsumeItem || condition == null || condition.Op != OUTL_ConditionOp.HasItem || condition.ItemDef == null) continue;
            OUTL_EntityId owner = condition.Subject == OUTL_ConditionSubject.Source ? source : (Entity != null ? Entity.Id : OUTL_EntityId.None);
            world.Inventory.TryConsume(owner, condition.ItemDef, Mathf.Max(1, condition.IntValue));
        }
    }

    private string FindFailedRequirement(in OUTL_Command command, OUTL_World world)
    {
        if (Requirements == null || Requirements.Length == 0) return "locked";
        for (int i = 0; i < Requirements.Length; i++)
        {
            OUTL_AccessRequirement requirement = Requirements[i];
            if (requirement == null || requirement.Condition == null) continue;
            if (!OUTL_Rules.Check(requirement.Condition, command.Source, command.Target, world))
                return !string.IsNullOrEmpty(requirement.RequirementId) ? requirement.RequirementId : "requirement." + i;
        }
        return "locked";
    }

    private void Fire(OUTL_World world, OUTL_EntityId source, string eventName, Vector3 point, int value)
    {
        if (world == null || string.IsNullOrEmpty(eventName)) return;
        Vector3 eventPoint = point != Vector3.zero ? point : transform.position;
        OUTL_OutputDispatcher.Fire(world, source, this, eventPoint, Outputs, eventName, eventName, value, true);
        world.Events.Emit(new OUTL_Event(OUTL_EventType.Signal, source, Entity != null ? Entity.Id : OUTL_EntityId.None)
        {
            Key = eventName,
            IntValue = value,
            Point = eventPoint
        });
    }

    private void PushState()
    {
        if (Entity == null || Entity.Runtime == null) return;
        Entity.Runtime.State.SetFlag(OUTL_StateId.Locked, IsLocked);
        Entity.Runtime.State.SetFlag("AccessGranted", HasGranted);
        Entity.Runtime.State.SetInt("AccessGrantCount", GrantCount);
        Entity.Runtime.State.SetString("AccessDeniedRequirement", LastDeniedRequirement ?? string.Empty);
    }
}
