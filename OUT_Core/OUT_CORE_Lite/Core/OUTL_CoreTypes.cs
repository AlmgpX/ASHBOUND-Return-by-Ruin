using System;
using UnityEngine;

[Serializable]
public struct OUTL_EntityId : IEquatable<OUTL_EntityId>
{
    [SerializeField] private int value;
    public int Value { get { return value; } }
    public bool IsValid { get { return value > 0; } }
    public static OUTL_EntityId None { get { return new OUTL_EntityId(0); } }
    public OUTL_EntityId(int value) { this.value = value; }
    public bool Equals(OUTL_EntityId other) { return value == other.value; }
    public override bool Equals(object obj) { return obj is OUTL_EntityId other && Equals(other); }
    public override int GetHashCode() { return value; }
    public override string ToString() { return IsValid ? value.ToString() : "None"; }
    public static bool operator ==(OUTL_EntityId a, OUTL_EntityId b) { return a.value == b.value; }
    public static bool operator !=(OUTL_EntityId a, OUTL_EntityId b) { return a.value != b.value; }
}

public enum OUTL_RuntimeTier
{
    Dormant = 0,
    Far = 1,
    Mid = 2,
    Near = 3,
    Full = 4
}

public enum OUTL_DayPhase
{
    Dawn = 0,
    Day = 1,
    Dusk = 2,
    Night = 3,
    Midnight = 4
}

public enum OUTL_TickLane
{
    Full = 0,
    Logic = 1,
    AI = 2,
    Quest = 3,
    Random = 4,
    Custom = 5
}

public enum OUTL_CommandType
{
    None = 0,
    Use = 1,
    Damage = 2,
    Heal = 3,
    Equip = 4,
    Unequip = 5,
    Attack = 6,
    Cast = 7,
    Drink = 8,
    Open = 9,
    Close = 10,
    Activate = 11,
    Deactivate = 12,
    Talk = 13,
    AddItem = 14,
    RemoveItem = 15,
    SetQuestStage = 16,
    SendSignal = 17,
    Pickup = 18,
    Lock = 19,
    Unlock = 20,
    Custom = 1000
}

public enum OUTL_EventType
{
    None = 0,
    CommandExecuted = 1,
    Used = 2,
    Damaged = 3,
    Healed = 4,
    Killed = 5,
    ItemAdded = 6,
    ItemRemoved = 7,
    Equipped = 8,
    Unequipped = 9,
    QuestStageChanged = 10,
    Signal = 11,
    RandomTick = 12,
    Spawned = 13,
    Despawned = 14,
    ItemDropped = 15,
    PickedUp = 16,
    QuestStarted = 17,
    QuestCompleted = 18,
    QuestFailed = 19,
    ContainerOpened = 20,
    ContainerLooted = 21,
    LootRolled = 22,
    ItemTaken = 23,
    DayPhaseChanged = 24,
    BehaviorModeChanged = 25,
    EgregoreMoodChanged = 26,
    Custom = 1000
}

public enum OUTL_EffectType
{
    None = 0,
    Damage = 1,
    Heal = 2,
    ModifyStat = 3,
    AddItem = 4,
    RemoveItem = 5,
    SetStateBool = 6,
    SetStateFloat = 7,
    AddStatus = 8,
    RemoveStatus = 9,
    SendCommand = 10,
    SendEvent = 11,
    SpawnPrefab = 12,
    PlaySound = 13,
    SetQuestStage = 14,
    Custom = 1000
}

[Serializable]
public struct OUTL_TagMask
{
    public string[] Tags;

    public bool Contains(string tag)
    {
        if (string.IsNullOrEmpty(tag) || Tags == null) return false;
        for (int i = 0; i < Tags.Length; i++)
            if (Tags[i] == tag) return true;
        return false;
    }

    public bool MatchesAny(string[] tags)
    {
        if (tags == null || Tags == null) return false;
        for (int i = 0; i < tags.Length; i++)
            if (Contains(tags[i])) return true;
        return false;
    }
}

[Serializable]
public struct OUTL_Command
{
    public OUTL_CommandType Type;
    public OUTL_EntityId Source;
    public OUTL_EntityId Target;
    public OUTL_EntityId Item;
    public string Key;
    public float FloatValue;
    public int IntValue;
    public Vector3 Point;
    public UnityEngine.Object Context;

    public OUTL_Command(OUTL_CommandType type, OUTL_EntityId source, OUTL_EntityId target)
    {
        Type = type;
        Source = source;
        Target = target;
        Item = OUTL_EntityId.None;
        Key = string.Empty;
        FloatValue = 0f;
        IntValue = 0;
        Point = Vector3.zero;
        Context = null;
    }
}

[Serializable]
public struct OUTL_Event
{
    public OUTL_EventType Type;
    public OUTL_EntityId Source;
    public OUTL_EntityId Target;
    public string Key;
    public float FloatValue;
    public int IntValue;
    public Vector3 Point;

    public OUTL_Event(OUTL_EventType type, OUTL_EntityId source, OUTL_EntityId target)
    {
        Type = type;
        Source = source;
        Target = target;
        Key = string.Empty;
        FloatValue = 0f;
        IntValue = 0;
        Point = Vector3.zero;
    }
}

public interface OUTL_ITickable
{
    bool OUTL_IsTickEnabled { get; }
    OUTL_TickLane OUTL_TickLane { get; }
    float OUTL_TickInterval { get; }
    void OUTL_Tick(OUTL_World world, float time, float deltaTime);
}

public interface OUTL_IRandomTickable
{
    bool OUTL_IsRandomTickEnabled { get; }
    void OUTL_RandomTick(OUTL_World world, float time);
}

public interface OUTL_IRandomTickIntervalProvider
{
    float OUTL_RandomTickInterval { get; }
}

public interface OUTL_ICommandReceiver
{
    bool OUTL_CanReceive(in OUTL_Command command, OUTL_World world);
    void OUTL_Receive(in OUTL_Command command, OUTL_World world);
}

public interface OUTL_ICommandGuard
{
    bool OUTL_Allows(in OUTL_Command command, OUTL_World world);
    void OUTL_OnCommandAccepted(in OUTL_Command command, OUTL_World world);
    void OUTL_OnCommandDenied(in OUTL_Command command, OUTL_World world);
}

public interface OUTL_IEventListener
{
    void OUTL_OnEvent(in OUTL_Event evt, OUTL_World world);
}

public interface OUTL_ISaveState
{
    void OUTL_Capture(OUTL_SaveData data);
    void OUTL_Restore(OUTL_SaveData data);
}
