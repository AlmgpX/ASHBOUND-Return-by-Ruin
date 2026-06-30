using UnityEngine;

[System.Serializable]
public sealed class OUT_ResourcePool
{
    [SerializeField] private OUT_ResourceKind kind = OUT_ResourceKind.None;
    [SerializeField] private OUT_ResourceState state;

    public OUT_ResourceKind Kind => kind;
    public OUT_ResourceState State => state;

    public int Current => state.Current;
    public int Max => state.Max;

    public float Normalized => state.Normalized;

    public bool IsValid => kind != OUT_ResourceKind.None && state.Max > 0;
    public bool IsDepleted => state.Current <= 0 || state.IsDepleted;
    public bool IsFull => state.Max > 0 && state.Current >= state.Max;
    public int Missing => Mathf.Max(0, state.Max - state.Current);

    public OUT_ResourcePool()
    {
        kind = OUT_ResourceKind.None;
        state = new OUT_ResourceState
        {
            Current = 0,
            Max = 0,
            IsDepleted = true
        };
    }

    public OUT_ResourcePool(OUT_ResourceKind kind, int max, int current = -1)
    {
        this.kind = kind;
        state = new OUT_ResourceState
        {
            Max = Mathf.Max(0, max),
            Current = current < 0 ? Mathf.Max(0, max) : Mathf.Clamp(current, 0, Mathf.Max(0, max)),
            IsDepleted = false
        };

        SyncFlags();
    }

    public void EnsureKind(OUT_ResourceKind targetKind)
    {
        kind = targetKind;
        SyncFlags();
    }

    public void SetMax(int max, bool clampCurrent = true)
    {
        state.Max = Mathf.Max(0, max);

        if (clampCurrent)
            state.Current = Mathf.Clamp(state.Current, 0, state.Max);

        SyncFlags();
    }

    public void SetCurrent(int current)
    {
        state.Current = Mathf.Clamp(current, 0, Mathf.Max(0, state.Max));
        SyncFlags();
    }

    public bool CanAdd(int amount)
    {
        if (!IsValid)
            return false;

        if (amount <= 0)
            return false;

        return state.Current < state.Max;
    }

    public int Add(int amount)
    {
        if (!CanAdd(amount))
            return 0;

        int add = Mathf.Min(amount, state.Max - state.Current);
        state.Current += add;
        SyncFlags();

        return add;
    }

    public bool CanConsume(int amount)
    {
        if (!IsValid)
            return false;

        if (amount <= 0)
            return false;

        return state.Current > 0;
    }

    public int Consume(int amount)
    {
        if (!CanConsume(amount))
            return 0;

        int consume = Mathf.Min(amount, state.Current);
        state.Current -= consume;
        SyncFlags();

        return consume;
    }

    public void Refill()
    {
        if (state.Max <= 0)
            return;

        state.Current = state.Max;
        SyncFlags();
    }

    public void Deplete()
    {
        state.Current = 0;
        SyncFlags();
    }

    private void SyncFlags()
    {
        if (state.Max < 0)
            state.Max = 0;

        if (state.Current < 0)
            state.Current = 0;

        if (state.Current > state.Max)
            state.Current = state.Max;

        state.IsDepleted = state.Current <= 0 || state.Max <= 0;
    }
}