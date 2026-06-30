using UnityEngine;

public enum OUTL_EquipmentSlot
{
    Primary = 0,
    Secondary = 1,
    Melee = 2,
    Head = 3,
    Body = 4,
    Utility = 5
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Item Equipment Def", fileName = "OUTL_EquipmentItemDef")]
public partial class OUTL_EquipmentItemDef
{
    public OUTL_EquipmentSlot Slot = OUTL_EquipmentSlot.Primary;
    public OUTL_AttackProfile AttackProfile;
    public OUTL_EffectDef[] OnEquipEffects;
    public OUTL_EffectDef[] OnUnequipEffects;
}

public static class OUTL_Equipment
{
    public static string BuildSlotStateKey(string slot)
    {
        return "Equip." + (string.IsNullOrEmpty(slot) ? "Primary" : slot);
    }

    public static string BuildSlotStateKey(OUTL_EquipmentSlot slot)
    {
        return BuildSlotStateKey(slot.ToString());
    }

    public static bool Equip(OUTL_EntityAdapter entity, OUTL_EquipmentItemDef item)
    {
        if (entity == null || entity.Runtime == null || item == null) return false;
        string slotKey = BuildSlotStateKey(item.Slot);
        entity.Runtime.State.SetString(slotKey, item.name);
        entity.Runtime.State.SetString(slotKey + ".Class", item.ClassName);
        entity.Runtime.State.SetString(slotKey + ".Display", item.DisplayName);

        OUTL_AttackDriver driver = entity.GetComponent<OUTL_AttackDriver>();
        if (driver != null && item.AttackProfile != null)
        {
            switch (item.Slot)
            {
                case OUTL_EquipmentSlot.Primary: driver.Primary = item.AttackProfile; break;
                case OUTL_EquipmentSlot.Secondary: driver.Secondary = item.AttackProfile; break;
                case OUTL_EquipmentSlot.Melee: driver.Melee = item.AttackProfile; break;
            }
        }

        if (OUTL_World.Instance != null)
        {
            OUTL_World.Instance.Effects.ApplyAll(item.OnEquipEffects, entity.Id, entity.Id, entity.transform.position);
            OUTL_World.Instance.Events.Emit(new OUTL_Event(OUTL_EventType.Equipped, entity.Id, entity.Id) { Key = item.Slot.ToString(), Point = entity.transform.position });
        }
        return true;
    }

    public static bool TryEquipFromInventory(OUTL_EntityAdapter entity, OUTL_EquipmentItemDef item)
    {
        if (entity == null || entity.Runtime == null || item == null || OUTL_World.Instance == null) return false;
        OUTL_World world = OUTL_World.Instance;
        if (!world.Inventory.HasItem(entity.Id, item, 1)) return false;
        if (!world.Inventory.RemoveItem(entity.Id, item, 1)) return false;
        if (Equip(entity, item)) return true;
        world.Inventory.AddItem(entity.Id, item, 1);
        return false;
    }

    public static bool TryUnequipToInventory(OUTL_EntityAdapter entity, OUTL_EquipmentItemDef item)
    {
        if (entity == null || entity.Runtime == null || item == null) return false;
        if (!Unequip(entity, item.Slot)) return false;
        if (OUTL_World.Instance != null) OUTL_World.Instance.Inventory.AddItem(entity.Id, item, 1);
        return true;
    }

    public static bool Unequip(OUTL_EntityAdapter entity, OUTL_EquipmentSlot slot)
    {
        if (entity == null || entity.Runtime == null) return false;
        string slotKey = BuildSlotStateKey(slot);
        entity.Runtime.State.SetString(slotKey, string.Empty);
        entity.Runtime.State.SetString(slotKey + ".Class", string.Empty);
        entity.Runtime.State.SetString(slotKey + ".Display", string.Empty);

        OUTL_AttackDriver driver = entity.GetComponent<OUTL_AttackDriver>();
        if (driver != null)
        {
            switch (slot)
            {
                case OUTL_EquipmentSlot.Primary: driver.Primary = null; break;
                case OUTL_EquipmentSlot.Secondary: driver.Secondary = null; break;
                case OUTL_EquipmentSlot.Melee: driver.Melee = null; break;
            }
        }

        if (OUTL_World.Instance != null)
            OUTL_World.Instance.Events.Emit(new OUTL_Event(OUTL_EventType.Unequipped, entity.Id, entity.Id) { Key = slot.ToString(), Point = entity.transform.position });
        return true;
    }
}
