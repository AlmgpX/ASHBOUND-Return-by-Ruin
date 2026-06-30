using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_LootDropper : MonoBehaviour, OUTL_IEventListener, OUTL_IComponentSaveParticipant, OUTL_IPoolReset
{
    public OUTL_EntityAdapter Entity;
    public OUTL_LootTableDef LootTable;
    public GameObject InventoryPickupPrefab;
    public bool DropOnKilled = true;
    public bool DropOnlyOnce = true;
    public bool DropInventoryItems = true;
    public bool LogDrops = true;

    private readonly List<OUTL_InventoryItemSnapshot> inventoryBuffer = new List<OUTL_InventoryItemSnapshot>(16);
    private bool dropped;

    public string OUTL_SaveKey { get { return "OUTL_LootDropper"; } }

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
    }

    private void OnEnable()
    {
        dropped = false;
        if (OUTL_World.Instance != null) OUTL_World.Instance.Events.Register(this, OUTL_EventType.Killed);
    }

    private void OnDisable()
    {
        if (OUTL_World.Instance != null) OUTL_World.Instance.Events.Unregister(this);
    }

    public void OUTL_OnEvent(in OUTL_Event evt, OUTL_World world)
    {
        if (!DropOnKilled || Entity == null || evt.Target != Entity.Id) return;
        Drop(evt.Point != Vector3.zero ? evt.Point : transform.position);
    }

    public bool Drop(Vector3 position)
    {
        if (DropOnlyOnce && dropped) return false;
        if (!OUTL_NetworkAuthority.CanSpawnDrop())
        {
            OUTL_NetworkAuthority.TraceBlocked("drop", Entity);
            return false;
        }

        dropped = true;
        int count = 0;
        if (LootTable != null) count += LootTable.Roll(position, transform.rotation, Entity);
        if (DropInventoryItems) count += DropInventory(position);
        if (LogDrops) OUTL_DebugLog.Log(OUTL_DebugChannel.Loot, "[LOOT DROP] entity=" + (Entity != null ? Entity.Id.Value.ToString() : "?") + " count=" + count + " pos=" + position, true);
        return count > 0;
    }

    public void OUTL_Capture(OUTL_ComponentSaveWriter writer)
    {
        if (writer == null) return;
        writer.SetFlag("dropped", dropped);
    }

    public void OUTL_Restore(OUTL_ComponentSaveReader reader)
    {
        dropped = reader != null && reader.GetFlag("dropped", dropped);
    }

    public void OUTL_OnPoolSpawn()
    {
        dropped = false;
    }

    public void OUTL_OnPoolRelease()
    {
        inventoryBuffer.Clear();
    }

    private int DropInventory(Vector3 position)
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null || Entity == null || InventoryPickupPrefab == null) return 0;
        world.Inventory.CopyItems(Entity.Id, inventoryBuffer);
        int droppedCount = 0;
        for (int i = 0; i < inventoryBuffer.Count; i++)
        {
            OUTL_InventoryItemSnapshot item = inventoryBuffer[i];
            if (item == null || item.Item == null || item.Count <= 0) continue;
            Vector3 offset = new Vector3((i % 3 - 1) * 0.35f, 0.35f, (i / 3) * 0.35f);
            GameObject go = OUTL_PoolSystem.SpawnShared(InventoryPickupPrefab, position + offset, transform.rotation);
            OUTL_ItemPickup pickup = go != null ? go.GetComponent<OUTL_ItemPickup>() : null;
            if (pickup != null)
            {
                pickup.Item = item.Item;
                pickup.Count = item.Count;
                pickup.Source = Entity;
            }
            world.Inventory.RemoveItem(Entity.Id, item.Item, item.Count);
            OUTL_StimulusBus.EmitResource(Entity.Id, position + offset, 10f, 1f, 0.5f, item.Item.name);
            droppedCount++;
        }
        return droppedCount;
    }
}
