namespace OUT_RayMicro.Core;

public enum OutmEntityKind : ushort
{
    None,
    Player,
    Door,
    Trigger,
    Projectile,
    Pickup,
    Actor,
    StaticWorld
}

public sealed class OutmEntityStore
{
    private EntitySlot[] slots;
    private int freeHead = -1;

    public OutmEntityStore(int initialCapacity = 256)
    {
        slots = new EntitySlot[Math.Max(16, initialCapacity)];
        for (int i = 0; i < slots.Length; i++)
            slots[i].Generation = 1;
    }

    public int AliveCount { get; private set; }
    public int Capacity => slots.Length;

    public EntityId Create(OutmEntityKind kind, string debugName = "")
    {
        int index;
        if (freeHead >= 0)
        {
            index = freeHead;
            freeHead = slots[index].NextFree;
        }
        else
        {
            index = FindFreeSlot();
            if (index < 0)
            {
                Grow();
                index = FindFreeSlot();
            }
        }

        ref EntitySlot slot = ref slots[index];
        slot.Alive = true;
        slot.Kind = kind;
        slot.DebugName = debugName;
        slot.NextFree = -1;
        AliveCount++;

        return new EntityId(index, slot.Generation);
    }

    public bool Destroy(EntityId id)
    {
        if (!IsAlive(id))
            return false;

        ref EntitySlot slot = ref slots[id.Index];
        slot.Alive = false;
        slot.Kind = OutmEntityKind.None;
        slot.DebugName = "";
        slot.Generation++;
        if (slot.Generation <= 0)
            slot.Generation = 1;
        slot.NextFree = freeHead;
        freeHead = id.Index;
        AliveCount--;
        return true;
    }

    public bool IsAlive(EntityId id)
    {
        return id.Index >= 0 && id.Index < slots.Length && slots[id.Index].Alive && slots[id.Index].Generation == id.Generation;
    }

    public OutmEntityKind GetKind(EntityId id)
    {
        return IsAlive(id) ? slots[id.Index].Kind : OutmEntityKind.None;
    }

    public string GetDebugName(EntityId id)
    {
        return IsAlive(id) ? slots[id.Index].DebugName : "";
    }

    private int FindFreeSlot()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].Alive && slots[i].NextFree == 0 && i != freeHead)
                return i;
        }

        return -1;
    }

    private void Grow()
    {
        int oldLength = slots.Length;
        Array.Resize(ref slots, slots.Length * 2);
        for (int i = oldLength; i < slots.Length; i++)
            slots[i].Generation = 1;
    }

    private struct EntitySlot
    {
        public bool Alive;
        public int Generation;
        public int NextFree;
        public OutmEntityKind Kind;
        public string DebugName;
    }
}
