using UnityEngine;

public enum OUTL_SquadOrderType : byte
{
    None = 0,
    Hold = 1,
    Attack = 2,
    TakeCover = 3,
    FlankLeft = 4,
    FlankRight = 5,
    Investigate = 6,
    Regroup = 7,
    Retreat = 8,
    Advance = 9,
    Suppress = 10,
    Search = 11
}

public struct OUTL_SquadOrder
{
    public OUTL_SquadOrderType Type;
    public OUTL_EntityId Target;
    public Vector3 Position;
    public float Priority;
    public float ExpireTime;
    public string Key;

    public bool IsValid { get { return Type != OUTL_SquadOrderType.None && Time.time <= ExpireTime; } }

    public OUTL_SquadOrder(OUTL_SquadOrderType type, OUTL_EntityId target, Vector3 position, float priority, float lifetime, string key)
    {
        Type = type;
        Target = target;
        Position = position;
        Priority = priority;
        ExpireTime = Time.time + Mathf.Max(0.1f, lifetime);
        Key = key;
    }
}
