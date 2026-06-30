using OUT_RayMicro.Core;
using OUT_RayMicro.World;

namespace OUT_RayMicro.Gameplay;

public sealed class OutmMapLogicSystem
{
    private readonly OutmMapLogicRuntime runtime;

    public OutmMapLogicSystem(OutmMapLogicRuntime runtime)
    {
        this.runtime = runtime;
    }

    public int EntityCount => runtime.Entities.Count;
    public int OutputCount => runtime.Outputs.Count;
    public int PendingCommandCount => runtime.Commands.Count;

    public void Fire(OutmWorld world, string sourceName, string eventName)
    {
        if (!runtime.ByName.TryGetValue(sourceName, out int sourceIndex))
            return;

        for (int i = 0; i < runtime.Outputs.Count; i++)
        {
            OutmLogicOutput output = runtime.Outputs.Items[i];
            if (output.SourceIndex != sourceIndex)
                continue;
            if (!string.Equals(output.Event, eventName, StringComparison.OrdinalIgnoreCase))
                continue;
            if (output.Once && output.Fired)
                continue;

            runtime.Commands.Add(new OutmLogicCommand
            {
                Target = output.Target,
                Input = output.Input,
                Parameter = output.Parameter,
                FireTime = world.Time + output.Delay
            });

            output.Fired = true;
            runtime.Outputs.Items[i] = output;
        }
    }

    public void Update(OutmWorld world, OutmMapRuntimeStores stores)
    {
        int i = 0;
        while (i < runtime.Commands.Count)
        {
            OutmLogicCommand command = runtime.Commands.Items[i];
            if (command.FireTime > world.Time)
            {
                i++;
                continue;
            }

            Execute(world, stores, command);
            runtime.Commands.RemoveSwapBack(i);
        }
    }

    private void Execute(OutmWorld world, OutmMapRuntimeStores stores, in OutmLogicCommand command)
    {
        if (!runtime.ByName.TryGetValue(command.Target, out int targetIndex))
        {
            world.PushLog($"logic target missing: {command.Target}");
            return;
        }

        OutmLogicEntity target = runtime.Entities.Items[targetIndex];
        if (!target.Enabled && command.Input != OutmLogicInputId.Enable)
            return;

        switch (target.Class)
        {
            case OutmEntityClassId.FuncDoor:
                ApplyDoorInput(world, stores, target, command.Input);
                break;

            case OutmEntityClassId.LogicRelay:
                if (command.Input == OutmLogicInputId.Trigger)
                    Fire(world, target.TargetName, "OnTrigger");
                break;

            case OutmEntityClassId.TriggerChangeLevel:
                if (command.Input == OutmLogicInputId.ChangeLevel || command.Input == OutmLogicInputId.Trigger)
                    world.PushLog($"changelevel requested: {command.Parameter}");
                break;

            default:
                ApplyGenericInput(world, targetIndex, command.Input);
                break;
        }
    }

    private static void ApplyDoorInput(OutmWorld world, OutmMapRuntimeStores stores, OutmLogicEntity target, OutmLogicInputId input)
    {
        switch (input)
        {
            case OutmLogicInputId.Open:
                stores.Doors.TrySetOpen(target.Id, true);
                world.Emit(new OutmEvent(OutmEventType.DoorToggled, target.Entity, EntityId.None, default, 1, target.Id));
                break;

            case OutmLogicInputId.Close:
                stores.Doors.TrySetOpen(target.Id, false);
                world.Emit(new OutmEvent(OutmEventType.DoorToggled, target.Entity, EntityId.None, default, 0, target.Id));
                break;

            case OutmLogicInputId.Toggle:
            case OutmLogicInputId.Trigger:
                if (stores.Doors.TryToggle(target.Id, out bool open))
                    world.Emit(new OutmEvent(OutmEventType.DoorToggled, target.Entity, EntityId.None, default, open ? 1 : 0, target.Id));
                break;
        }
    }

    private void ApplyGenericInput(OutmWorld world, int targetIndex, OutmLogicInputId input)
    {
        OutmLogicEntity target = runtime.Entities.Items[targetIndex];
        switch (input)
        {
            case OutmLogicInputId.Enable:
                target.Enabled = true;
                runtime.Entities.Items[targetIndex] = target;
                break;

            case OutmLogicInputId.Disable:
                target.Enabled = false;
                runtime.Entities.Items[targetIndex] = target;
                break;

            case OutmLogicInputId.Trigger:
                Fire(world, target.TargetName, "OnTrigger");
                break;
        }
    }
}
