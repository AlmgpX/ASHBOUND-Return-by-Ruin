using OUT_RayMicro.Core;
using OUT_RayMicro.World;

namespace OUT_RayMicro.Gameplay;

public static class OutmMapEntityCompiler
{
    public static OutmMapLogicRuntime Compile(OutmWorld world, OutmMapDef mapDef, OutmMapEntityDef[] entityDefs)
    {
        var runtime = new OutmMapLogicRuntime();

        for (int i = 0; i < entityDefs.Length; i++)
            AddEntity(world, runtime, entityDefs[i]);

        AddLegacyDoors(world, runtime, mapDef);
        AddLegacyTriggers(world, runtime, mapDef);

        world.PushLog($"logic entities {runtime.Entities.Count} outputs {runtime.Outputs.Count}");
        return runtime;
    }

    private static void AddLegacyDoors(OutmWorld world, OutmMapLogicRuntime runtime, OutmMapDef mapDef)
    {
        for (int i = 0; i < mapDef.Doors.Length; i++)
        {
            OutmDoorDef door = mapDef.Doors[i];
            AddEntity(world, runtime, new OutmMapEntityDef
            {
                Id = door.Id,
                TargetName = door.Id,
                Class = OutmEntityClassId.FuncDoor
            });
        }
    }

    private static void AddLegacyTriggers(OutmWorld world, OutmMapLogicRuntime runtime, OutmMapDef mapDef)
    {
        for (int i = 0; i < mapDef.Triggers.Length; i++)
        {
            OutmTriggerDef item = mapDef.Triggers[i];
            AddEntity(world, runtime, new OutmMapEntityDef
            {
                Id = item.Id,
                TargetName = item.Id,
                Class = OutmEntityClassId.TriggerMultiple
            });
        }
    }

    private static void AddEntity(OutmWorld world, OutmMapLogicRuntime runtime, OutmMapEntityDef def)
    {
        string id = string.IsNullOrWhiteSpace(def.Id) ? $"logic.{runtime.Entities.Count}" : def.Id;
        string targetName = string.IsNullOrWhiteSpace(def.TargetName) ? id : def.TargetName;
        EntityId entity = world.Entities.Create(OutmEntityKind.Logic, id);

        int entityIndex = runtime.Entities.Add(new OutmLogicEntity
        {
            Entity = entity,
            Id = id,
            TargetName = targetName,
            Class = def.Class,
            Enabled = true,
            FiredOnce = false
        });

        runtime.ByName[id] = entityIndex;
        runtime.ByName[targetName] = entityIndex;

        for (int i = 0; i < def.Outputs.Length; i++)
        {
            OutmMapIoDef output = def.Outputs[i];
            runtime.Outputs.Add(new OutmLogicOutput
            {
                SourceIndex = entityIndex,
                Event = string.IsNullOrWhiteSpace(output.Event) ? "OnEnter" : output.Event,
                Target = output.Target,
                Input = OutmLogicInputParser.Parse(output.Input),
                Parameter = output.Parameter,
                Delay = MathF.Max(0.0f, output.Delay),
                Once = output.Once,
                Fired = false
            });
        }
    }
}
