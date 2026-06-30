using OUT_RayMicro.Core;
using OUT_RayMicro.World;

namespace OUT_RayMicro.Gameplay;

public sealed class OutmMapLogicRuntime
{
    public readonly OutBuffer<OutmLogicEntity> Entities = new(512);
    public readonly OutBuffer<OutmLogicOutput> Outputs = new(1024);
    public readonly OutBuffer<OutmLogicCommand> Commands = new(512);
    public readonly Dictionary<string, int> ByName = new(StringComparer.OrdinalIgnoreCase);
}

public static class OutmLogicInputParser
{
    public static OutmLogicInputId Parse(string value)
    {
        if (Enum.TryParse(value, true, out OutmLogicInputId parsed))
            return parsed;

        return OutmLogicInputId.Trigger;
    }
}
