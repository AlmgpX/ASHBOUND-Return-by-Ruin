using OutCore.pool;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_ItemPickup : MonoBehaviour, OUTL_ICommandReceiver, OUTL_IPoolReset
{
    public OUTL_EntityAdapter Entity;
    public OUTL_ItemDef Item;
    public int Count = 1;
    public OUTL_EntityAdapter Source;
    public bool UseCommand = true;
    public bool AutoDespawnOnPickup = true;
    public string PickupKey = "pickup";

    private bool pickedUp;
    public bool IsPickedUp { get { return pickedUp; } }

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
    }

    public bool OUTL_CanReceive(in OUTL_Command command, OUTL_World world)
    {
        return UseCommand && (command.Type == OUTL_CommandType.Use || command.Type == OUTL_CommandType.Pickup);
    }

    public void OUTL_Receive(in OUTL_Command command, OUTL_World world)
    {
        OUTL_EntityAdapter target = null;
        if (world != null)
        {
            OUTL_EntityRuntime runtime;
            if (world.Registry.TryGet(command.Source, out runtime) && runtime != null) target = runtime.Adapter;
        }
        TryPickup(target);
    }

    public bool TryPickup(OUTL_EntityAdapter receiver)
    {
        string reason;
        if (!CanPickup(receiver, -1f, out reason))
        {
            if (!string.IsNullOrEmpty(reason)) OUTL_DebugLog.Log(OUTL_DebugChannel.Loot, "pickup blocked: " + reason, true);
            return false;
        }
        if (!OUTL_NetworkAuthority.CanPickup())
        {
            OUTL_NetworkAuthority.TraceBlocked("pickup", Entity);
            return false;
        }

        OUTL_World world = OUTL_World.Instance;
        if (world == null) return false;
        pickedUp = true;
        world.Inventory.AddItem(receiver.Id, Item, Mathf.Max(1, Count));
        world.Events.Emit(new OUTL_Event(OUTL_EventType.PickedUp, receiver.Id, Entity != null ? Entity.Id : OUTL_EntityId.None) { Key = PickupKey, IntValue = Count, Point = transform.position });
        world.Events.Emit(new OUTL_Event(OUTL_EventType.ItemTaken, receiver.Id, Entity != null ? Entity.Id : OUTL_EntityId.None) { Key = Item != null ? Item.name : PickupKey, IntValue = Count, Point = transform.position });
        if (AutoDespawnOnPickup) DespawnPickup(world);
        return true;
    }

    public bool CanPickup(OUTL_EntityAdapter receiver, float maxDistance, out string reason)
    {
        reason = "";
        if (pickedUp) { reason = "already picked up"; return false; }
        if (Item == null) { reason = "missing item"; return false; }
        if (Count <= 0) { reason = "empty count"; return false; }
        if (receiver == null || receiver.Runtime == null) { reason = "missing receiver"; return false; }
        if (receiver.Runtime.Dead || receiver.Runtime.LifeState == OUTL_LifeState.Dead || receiver.Runtime.State.GetFlag(OUTL_StateId.Dead))
        {
            reason = "receiver dead";
            return false;
        }
        if (maxDistance > 0f && (receiver.transform.position - transform.position).sqrMagnitude > maxDistance * maxDistance)
        {
            reason = "too far";
            return false;
        }
        return true;
    }

    public void OUTL_OnPoolSpawn()
    {
        pickedUp = false;
    }

    public void OUTL_OnPoolRelease()
    {
        pickedUp = false;
        Source = null;
    }

    private void DespawnPickup(OUTL_World world)
    {
        if (Entity != null && Entity.Id.IsValid && world != null)
        {
            world.QueueDespawn(Entity.Id);
            return;
        }

        OutCore.pool.OUT.Release(gameObject);
    }
}
