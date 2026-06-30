using OUT_RayMicro.Core;
using OUT_RayMicro.World;

namespace OUT_RayMicro.Gameplay;

public enum OutmLogicInputId : ushort
{
    None,
    Trigger,
    Enable,
    Disable,
    Toggle,
    Open,
    Close,
    Remove,
    Damage,
    ChangeLevel,
    Increment,
    Reset
}

public struct OutmLogicEntity
{
    public EntityId Entity;
    public string Id;
    public string TargetName;
    public OutmEntityClassId Class;
    public bool Enabled;
    public bool FiredOnce;
}

public struct OutmLogicOutput
{
    public int SourceIndex;
    public string Event;
    public string Target;
    public OutmLogicInputId Input;
    public string Parameter;
    public float Delay;
    public bool Once;
    public bool Fired;
}

public struct OutmLogicCommand
{
    public string Target;
    public OutmLogicInputId Input;
    public string Parameter;
    public float FireTime;
}
