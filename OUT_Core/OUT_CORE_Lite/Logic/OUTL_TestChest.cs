using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_TestChest : MonoBehaviour, OUTL_ICommandReceiver, OUTL_ISaveState
{
    public OUTL_EntityAdapter Entity;
    public OUTL_DropTable DropTable;
    public bool IsOpen;
    public bool DropOnOpen = true;
    public bool DropOnlyOnce = true;
    public string OpenFlag = "Open";
    public string OpenedEventName = "OnOpened";
    public string ClosedEventName = "OnClosed";
    public OUTL_OutputLink[] Outputs;

    private bool dropped;

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        WriteState();
    }

    private void OnEnable()
    {
        WriteState();
    }

    public bool OUTL_CanReceive(in OUTL_Command command, OUTL_World world)
    {
        return command.Type == OUTL_CommandType.Use || command.Type == OUTL_CommandType.Open || command.Type == OUTL_CommandType.Close || command.Type == OUTL_CommandType.Activate || command.Type == OUTL_CommandType.Deactivate || command.Type == OUTL_CommandType.SendSignal || command.Type == OUTL_CommandType.Custom;
    }

    public void OUTL_Receive(in OUTL_Command command, OUTL_World world)
    {
        if (world == null) world = OUTL_World.Instance;
        switch (command.Type)
        {
            case OUTL_CommandType.Close:
            case OUTL_CommandType.Deactivate:
                Close(world, command.Source, command.Point);
                break;
            case OUTL_CommandType.Use:
                if (IsOpen) Close(world, command.Source, command.Point);
                else Open(world, command.Source, command.Point);
                break;
            default:
                Open(world, command.Source, command.Point);
                break;
        }
    }

    [ContextMenu("OUT Open Chest")]
    public void OpenNow()
    {
        Open(OUTL_World.Instance, Entity != null ? Entity.Id : OUTL_EntityId.None, transform.position);
    }

    [ContextMenu("OUT Close Chest")]
    public void CloseNow()
    {
        Close(OUTL_World.Instance, Entity != null ? Entity.Id : OUTL_EntityId.None, transform.position);
    }

    [ContextMenu("OUT Drop Chest Loot")]
    public void DropNow()
    {
        DropLoot(transform.position + Vector3.up * 0.7f);
    }

    public void Open(OUTL_World world, OUTL_EntityId source, Vector3 point)
    {
        if (IsOpen) return;
        IsOpen = true;
        WriteState();
        if (DropOnOpen) DropLoot(point != Vector3.zero ? point : transform.position + Vector3.up * 0.7f);
        Fire(world, source, OpenedEventName, point);
    }

    public void Close(OUTL_World world, OUTL_EntityId source, Vector3 point)
    {
        if (!IsOpen) return;
        IsOpen = false;
        WriteState();
        Fire(world, source, ClosedEventName, point);
    }

    public void OUTL_Capture(OUTL_SaveData data)
    {
        if (data == null) return;
        data.Set("testChest.open", IsOpen ? "1" : "0");
        data.Set("testChest.dropped", dropped ? "1" : "0");
    }

    public void OUTL_Restore(OUTL_SaveData data)
    {
        if (data == null) return;
        IsOpen = data.Get("testChest.open") == "1";
        dropped = data.Get("testChest.dropped") == "1";
        WriteState();
    }

    private void DropLoot(Vector3 point)
    {
        if (DropTable == null) return;
        if (DropOnlyOnce && dropped) return;
        dropped = true;
        DropTable.SpawnDrops(point, transform.rotation);
        WriteState();
    }

    private void Fire(OUTL_World world, OUTL_EntityId source, string eventName, Vector3 point)
    {
        if (world == null) return;
        OUTL_EntityId self = Entity != null ? Entity.Id : OUTL_EntityId.None;
        world.Events.Emit(new OUTL_Event(OUTL_EventType.Used, source, self) { Key = eventName, Point = point });
        OUTL_OutputDispatcher.Fire(world, self, this, point, Outputs, eventName, eventName, IsOpen ? 1 : 0, true);
    }

    private void WriteState()
    {
        if (Entity == null || Entity.Runtime == null) return;
        Entity.Runtime.State.SetFlag(OpenFlag, IsOpen);
        Entity.Runtime.State.SetFlag("Dropped", dropped);
    }
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Container Def", fileName = "OUTL_ContainerDef")]
public sealed partial class OUTL_ContainerDef
{
    public string ContainerId = "container";
    public OUTL_LootTableDef LootTable;
    public bool StartsLocked;
    public int Seed;
    public string OpenKey = "container.open";
    public string LootKey = "container.loot";
}

[DisallowMultipleComponent]
public sealed class OUTL_ContainerRuntime : MonoBehaviour, OUTL_ICommandReceiver, OUTL_IComponentSaveParticipant
{
    public OUTL_EntityAdapter Entity;
    public OUTL_AccessController Access;
    public OUTL_ContainerDef Def;
    public bool IsOpen;
    public bool IsLocked;
    public bool Looted;
    public int RolledSeed;
    public int LastSpawnedCount;

    public string OUTL_SaveKey { get { return "OUTL_ContainerRuntime"; } }

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (Access == null) Access = GetComponent<OUTL_AccessController>();
        if (Def != null && RolledSeed == 0) RolledSeed = Def.Seed != 0 ? Def.Seed : BuildSeed();
        if (Def != null) IsLocked = Def.StartsLocked;
        WriteState();
    }

    private void OnEnable()
    {
        WriteState();
    }

    public bool Open(OUTL_World world, OUTL_EntityId source, Vector3 point)
    {
        if (world == null) world = OUTL_World.Instance;
        if (Access == null) Access = GetComponent<OUTL_AccessController>();
        if (Access == null && IsLocked) return false;
        if (!IsOpen)
        {
            IsOpen = true;
            Emit(world, OUTL_EventType.ContainerOpened, source, Def != null ? Def.OpenKey : "container.open", point);
        }
        if (!Looted) RollLoot(world, source, point);
        WriteState();
        return true;
    }

    public bool OUTL_CanReceive(in OUTL_Command command, OUTL_World world)
    {
        return command.Type == OUTL_CommandType.Use
            || command.Type == OUTL_CommandType.Open
            || command.Type == OUTL_CommandType.Close
            || command.Type == OUTL_CommandType.Activate
            || command.Type == OUTL_CommandType.Deactivate
            || command.Type == OUTL_CommandType.Lock
            || command.Type == OUTL_CommandType.Unlock
            || command.Type == OUTL_CommandType.Custom;
    }

    public void OUTL_Receive(in OUTL_Command command, OUTL_World world)
    {
        if (command.Type == OUTL_CommandType.Lock)
        {
            Lock();
            return;
        }

        if (command.Type == OUTL_CommandType.Unlock)
        {
            Unlock();
            return;
        }

        if (command.Type == OUTL_CommandType.Close || command.Type == OUTL_CommandType.Deactivate)
        {
            Close();
            Emit(world != null ? world : OUTL_World.Instance, OUTL_EventType.Used, command.Source, "container.close", command.Point);
            return;
        }

        Open(world, command.Source, command.Point);
    }

    public void Close()
    {
        IsOpen = false;
        WriteState();
    }

    public void Unlock()
    {
        IsLocked = false;
        WriteState();
    }

    public void Lock()
    {
        IsLocked = true;
        IsOpen = false;
        WriteState();
    }

    public void OUTL_Capture(OUTL_ComponentSaveWriter writer)
    {
        if (writer == null) return;
        writer.SetFlag("open", IsOpen);
        writer.SetFlag("locked", IsLocked);
        writer.SetFlag("looted", Looted);
        writer.SetInt("seed", RolledSeed);
        writer.SetInt("spawned", LastSpawnedCount);
    }

    public void OUTL_Restore(OUTL_ComponentSaveReader reader)
    {
        if (reader == null) return;
        IsOpen = reader.GetFlag("open", IsOpen);
        IsLocked = reader.GetFlag("locked", IsLocked);
        Looted = reader.GetFlag("looted", Looted);
        RolledSeed = reader.GetInt("seed", RolledSeed != 0 ? RolledSeed : BuildSeed());
        LastSpawnedCount = reader.GetInt("spawned", LastSpawnedCount);
        WriteState();
    }

    private void RollLoot(OUTL_World world, OUTL_EntityId source, Vector3 point)
    {
        Looted = true;
        if (RolledSeed == 0) RolledSeed = BuildSeed();
        Vector3 origin = point != Vector3.zero ? point : transform.position + Vector3.up * 0.7f;
        if (Def != null && Def.LootTable != null)
            LastSpawnedCount = Def.LootTable.RollDeterministic(RolledSeed, origin, transform.rotation, Entity);
        Emit(world, OUTL_EventType.ContainerLooted, source, Def != null ? Def.LootKey : "container.loot", origin, LastSpawnedCount);
        Emit(world, OUTL_EventType.ItemTaken, source, Def != null ? Def.ContainerId : "container", origin, LastSpawnedCount);
    }

    private void Emit(OUTL_World world, OUTL_EventType type, OUTL_EntityId source, string key, Vector3 point, int count = 0)
    {
        if (world == null) return;
        OUTL_EntityId self = Entity != null ? Entity.Id : OUTL_EntityId.None;
        world.Events.Emit(new OUTL_Event(type, source, self) { Key = key, IntValue = count, Point = point != Vector3.zero ? point : transform.position });
    }

    private int BuildSeed()
    {
        string stable = Entity != null && !string.IsNullOrEmpty(Entity.StableId) ? Entity.StableId : name;
        unchecked
        {
            int hash = OUTL_WorldCellUtility.StableStringHash(stable);
            hash = hash * 31 + Mathf.RoundToInt(transform.position.x * 10f);
            hash = hash * 31 + Mathf.RoundToInt(transform.position.z * 10f);
            return hash != 0 ? hash : 1;
        }
    }

    private void WriteState()
    {
        if (Entity == null || Entity.Runtime == null) return;
        if (Access == null) Access = GetComponent<OUTL_AccessController>();
        bool locked = Access != null ? Access.IsLocked : IsLocked;
        Entity.Runtime.State.SetFlag("ContainerOpen", IsOpen);
        Entity.Runtime.State.SetFlag("ContainerLocked", locked);
        Entity.Runtime.State.SetFlag("ContainerLooted", Looted);
        Entity.Runtime.State.SetInt("ContainerSeed", RolledSeed);
        Entity.Runtime.State.SetInt("ContainerSpawned", LastSpawnedCount);
    }
}

[DisallowMultipleComponent]
public sealed class OUTL_ChestInteractable : MonoBehaviour, OUTL_ICommandReceiver
{
    public OUTL_ContainerRuntime Container;

    private void Awake()
    {
        if (Container == null) Container = GetComponent<OUTL_ContainerRuntime>();
    }

    public bool OUTL_CanReceive(in OUTL_Command command, OUTL_World world)
    {
        return command.Type == OUTL_CommandType.Use || command.Type == OUTL_CommandType.Open || command.Type == OUTL_CommandType.Close || command.Type == OUTL_CommandType.Activate || command.Type == OUTL_CommandType.Custom;
    }

    public void OUTL_Receive(in OUTL_Command command, OUTL_World world)
    {
        if (Container == null) return;
        if (command.Type == OUTL_CommandType.Close)
        {
            Container.Close();
            return;
        }
        Container.Open(world, command.Source, command.Point);
    }
}
