using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_EquipmentRuntime : MonoBehaviour, OUTL_ICommandReceiver, OUTL_IComponentSaveParticipant, OUTL_ITickable
{
    public OUTL_EntityAdapter Entity;
    public OUTL_EquipmentItemDef[] KnownItems;
    public bool RequireInventoryForEquip = true;
    public bool ReturnUnequippedToInventory = true;
    public bool AutoEquipKnownItemsOnStart = false;
    public bool AutoEquipOnlyEmptySlots = true;

    public string OUTL_SaveKey { get { return "OUTL_EquipmentRuntime"; } }
    private bool autoEquipDone;
    private bool registeredTick;

    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled && !autoEquipDone; } }
    public OUTL_TickLane OUTL_TickLane { get { return OUTL_TickLane.Logic; } }
    public float OUTL_TickInterval { get { return 0.10f; } }

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
    }

    private void OnEnable()
    {
        SyncAutoEquipTickRegistration();
    }

    private void OnDisable()
    {
        UnregisterAutoEquipTick();
    }

    private void Start()
    {
        TryAutoEquipKnownItemsWhenReady();
        SyncAutoEquipTickRegistration();
    }

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        if (!autoEquipDone) TryAutoEquipKnownItemsWhenReady();
        SyncAutoEquipTickRegistration();
    }

    public bool OUTL_CanReceive(in OUTL_Command command, OUTL_World world)
    {
        return Entity != null && Entity.Runtime != null && (command.Type == OUTL_CommandType.Equip || command.Type == OUTL_CommandType.Unequip);
    }

    public void OUTL_Receive(in OUTL_Command command, OUTL_World world)
    {
        if (command.Type == OUTL_CommandType.Equip) TryEquipByKey(command.Key);
        else if (command.Type == OUTL_CommandType.Unequip) TryUnequipByKey(command.Key);
    }

    public bool TryEquip(OUTL_EquipmentItemDef item)
    {
        if (Entity == null || Entity.Runtime == null || item == null) return false;
        if (RequireInventoryForEquip) return OUTL_Equipment.TryEquipFromInventory(Entity, item);
        return OUTL_Equipment.Equip(Entity, item);
    }

    public bool TryUnequip(OUTL_EquipmentSlot slot)
    {
        if (Entity == null || Entity.Runtime == null) return false;
        OUTL_EquipmentItemDef old = FindItemBySavedSlot(slot);
        if (ReturnUnequippedToInventory && old != null) return OUTL_Equipment.TryUnequipToInventory(Entity, old);
        return OUTL_Equipment.Unequip(Entity, slot);
    }

    public void OUTL_Capture(OUTL_ComponentSaveWriter writer)
    {
        for (int i = 0; i < 6; i++)
        {
            OUTL_EquipmentSlot slot = (OUTL_EquipmentSlot)i;
            string key = OUTL_Equipment.BuildSlotStateKey(slot);
            string item = Entity != null && Entity.Runtime != null ? Entity.Runtime.State.GetString(key, string.Empty) : string.Empty;
            if (!string.IsNullOrEmpty(item)) writer.SetString(slot.ToString(), item);
        }
    }

    public void OUTL_Restore(OUTL_ComponentSaveReader reader)
    {
        for (int i = 0; i < 6; i++)
        {
            OUTL_EquipmentSlot slot = (OUTL_EquipmentSlot)i;
            string itemName = reader.GetString(slot.ToString(), string.Empty);
            if (string.IsNullOrEmpty(itemName)) continue;
            OUTL_EquipmentItemDef item = FindItem(itemName);
            if (item != null) OUTL_Equipment.Equip(Entity, item);
        }
    }

    public bool TryEquipByKey(string key)
    {
        OUTL_EquipmentItemDef item = FindItem(key);
        return TryEquip(item);
    }

    public bool TryUnequipByKey(string key)
    {
        OUTL_EquipmentSlot slot;
        if (!System.Enum.TryParse(key, true, out slot)) slot = OUTL_EquipmentSlot.Primary;
        return TryUnequip(slot);
    }

    public int AutoEquipKnownItems()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (Entity != null && Entity.Runtime == null && OUTL_World.Instance != null)
            Entity.RegisterNow(OUTL_World.Instance);
        if (Entity == null || Entity.Runtime == null || KnownItems == null) return 0;

        int equipped = 0;
        for (int i = 0; i < KnownItems.Length; i++)
        {
            OUTL_EquipmentItemDef item = KnownItems[i];
            if (item == null) continue;
            if (AutoEquipOnlyEmptySlots)
            {
                string slotKey = OUTL_Equipment.BuildSlotStateKey(item.Slot);
                if (!string.IsNullOrEmpty(Entity.Runtime.State.GetString(slotKey, string.Empty))) continue;
            }
            if (OUTL_Equipment.Equip(Entity, item)) equipped++;
        }
        return equipped;
    }

    private void TryAutoEquipKnownItemsWhenReady()
    {
        if (!AutoEquipKnownItemsOnStart)
        {
            autoEquipDone = true;
            return;
        }
        int equipped = AutoEquipKnownItems();
        if (equipped > 0 || Entity == null || Entity.Runtime != null)
            autoEquipDone = true;
    }

    private void SyncAutoEquipTickRegistration()
    {
        if (autoEquipDone || OUTL_World.Instance == null)
        {
            UnregisterAutoEquipTick();
            return;
        }

        if (registeredTick) return;
        OUTL_World.Instance.Scheduler.Register(this);
        registeredTick = true;
    }

    private void UnregisterAutoEquipTick()
    {
        if (!registeredTick || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Unregister(this);
        registeredTick = false;
    }

    private OUTL_EquipmentItemDef FindItemBySavedSlot(OUTL_EquipmentSlot slot)
    {
        if (Entity == null || Entity.Runtime == null) return null;
        string itemName = Entity.Runtime.State.GetString(OUTL_Equipment.BuildSlotStateKey(slot), string.Empty);
        return FindItem(itemName);
    }

    private OUTL_EquipmentItemDef FindItem(string key)
    {
        if (KnownItems == null || string.IsNullOrEmpty(key)) return null;
        for (int i = 0; i < KnownItems.Length; i++)
        {
            OUTL_EquipmentItemDef item = KnownItems[i];
            if (item == null) continue;
            if (item.name == key || item.ClassName == key || item.DisplayName == key) return item;
        }
        return null;
    }
}
