namespace OUT_RayMicro.Core;

public sealed class OutBuffer<T>
{
    public T[] Items;
    public int Count;

    public OutBuffer(int capacity)
    {
        Items = new T[Math.Max(1, capacity)];
    }

    public int Capacity => Items.Length;

    public ref T this[int index] => ref Items[index];

    public int Add(in T item)
    {
        EnsureCapacity(Count + 1);
        int index = Count++;
        Items[index] = item;
        return index;
    }

    public void Clear()
    {
        Count = 0;
    }

    public void ClearWithZero()
    {
        Array.Clear(Items, 0, Count);
        Count = 0;
    }

    public void RemoveSwapBack(int index)
    {
        if ((uint)index >= (uint)Count)
            return;

        int last = Count - 1;
        Items[index] = Items[last];
        Items[last] = default!;
        Count = last;
    }

    public void EnsureCapacity(int required)
    {
        if (required <= Items.Length)
            return;

        int next = Items.Length;
        while (next < required)
            next *= 2;

        Array.Resize(ref Items, next);
    }
}
