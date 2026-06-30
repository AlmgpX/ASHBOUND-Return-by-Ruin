using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_InventoryRuntime : MonoBehaviour, OUTL_IComponentSaveParticipant
{
    public OUTL_EntityAdapter Entity;
    [Tooltip("Optional stable item resolver. When omitted, restore tries the runtime save spawn resolver and then KnownItems.")]
    public OUTL_DefDatabase DefDatabase;
    public OUTL_ItemDef[] KnownItems;
    public bool ClearBeforeRestore = true;

    private readonly List<OUTL_InventoryItemSnapshot> buffer = new List<OUTL_InventoryItemSnapshot>(32);

    public string OUTL_SaveKey { get { return "OUTL_InventoryRuntime"; } }

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
    }

    public void OUTL_Capture(OUTL_ComponentSaveWriter writer)
    {
        if (writer == null || Entity == null || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Inventory.CopyItems(Entity.Id, buffer);
        writer.SetInt("itemCount", buffer.Count);
        for (int i = 0; i < buffer.Count; i++)
        {
            OUTL_InventoryItemSnapshot snapshot = buffer[i];
            if (snapshot == null || snapshot.Item == null || snapshot.Count <= 0) continue;
            writer.SetString("item." + i + ".id", GetItemId(snapshot.Item));
            writer.SetInt("item." + i + ".count", snapshot.Count);
        }

        if (KnownItems != null)
        {
            OUTL_InventorySystem inventory = OUTL_World.Instance.Inventory;
            for (int i = 0; i < KnownItems.Length; i++)
            {
                OUTL_ItemDef item = KnownItems[i];
                if (item == null) continue;
                int count = inventory.CountItem(Entity.Id, item);
                if (count > 0) writer.SetInt(BuildLegacyItemKey(item), count);
            }
        }
    }

    public void OUTL_Restore(OUTL_ComponentSaveReader reader)
    {
        if (reader == null || Entity == null || OUTL_World.Instance == null) return;
        OUTL_InventorySystem inventory = OUTL_World.Instance.Inventory;
        if (ClearBeforeRestore) inventory.Clear(Entity.Id);

        int itemCount = Mathf.Max(0, reader.GetInt("itemCount", 0));
        for (int i = 0; i < itemCount; i++)
        {
            string id = reader.GetString("item." + i + ".id", string.Empty);
            int count = reader.GetInt("item." + i + ".count", 0);
            OUTL_ItemDef item = ResolveItem(id);
            if (item != null && count > 0) inventory.AddItem(Entity.Id, item, count);
        }

        if (KnownItems == null) return;
        for (int i = 0; i < KnownItems.Length; i++)
        {
            OUTL_ItemDef item = KnownItems[i];
            if (item == null) continue;
            if (inventory.CountItem(Entity.Id, item) > 0) continue;
            int legacyCount = reader.GetInt(BuildLegacyItemKey(item), 0);
            if (legacyCount > 0) inventory.AddItem(Entity.Id, item, legacyCount);
        }
    }

    private OUTL_ItemDef ResolveItem(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (KnownItems != null)
        {
            for (int i = 0; i < KnownItems.Length; i++)
            {
                OUTL_ItemDef item = KnownItems[i];
                if (item != null && (GetItemId(item) == id || item.name == id)) return item;
            }
        }

        OUTL_DefDatabase database = ResolveDatabase();
        return database != null ? database.FindItemDef(id) : null;
    }

    private OUTL_DefDatabase ResolveDatabase()
    {
        if (DefDatabase != null) return DefDatabase;
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return null;
        OUTL_SaveSpawnResolverRegistry resolver = world.GetComponentInChildren<OUTL_SaveSpawnResolverRegistry>(true);
        return resolver != null ? resolver.DefDatabase : null;
    }

    private static string BuildLegacyItemKey(OUTL_ItemDef item)
    {
        if (item == null) return string.Empty;
        if (!string.IsNullOrEmpty(item.ClassName)) return "item." + item.ClassName;
        return "item." + item.name;
    }

    private static string GetItemId(OUTL_ItemDef item)
    {
        return item != null ? item.GetDefId() : string.Empty;
    }
}
