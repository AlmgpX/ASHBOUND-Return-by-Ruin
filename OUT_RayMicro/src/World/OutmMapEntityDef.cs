namespace OUT_RayMicro.World;

public enum OutmEntityClassId : ushort
{
    Unknown,
    Worldspawn,
    InfoPlayerStart,
    FuncDoor,
    FuncButton,
    TriggerOnce,
    TriggerMultiple,
    TriggerHurt,
    TriggerChangeLevel,
    LogicRelay,
    LogicTimer,
    LogicCounter,
    ItemPickup,
    LightPoint,
    AmbientSound,
    PropStatic,
    PropDynamic
}

public sealed class OutmMapEntityDef
{
    public string Id { get; set; } = "entity";
    public OutmEntityClassId Class { get; set; } = OutmEntityClassId.Unknown;
    public string TargetName { get; set; } = "";
    public string Target { get; set; } = "";
    public float[] Position { get; set; } = { 0, 0, 0 };
    public float[] Rotation { get; set; } = { 0, 0, 0 };
    public float[] Size { get; set; } = { 1, 1, 1 };
    public string Surface { get; set; } = "surface.stone";
    public Dictionary<string, string> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public OutmMapIoDef[] Outputs { get; set; } = Array.Empty<OutmMapIoDef>();
}

public sealed class OutmMapIoDef
{
    public string Event { get; set; } = "OnTrigger";
    public string Target { get; set; } = "";
    public string Input { get; set; } = "Trigger";
    public string Parameter { get; set; } = "";
    public float Delay { get; set; }
    public bool Once { get; set; }
}
