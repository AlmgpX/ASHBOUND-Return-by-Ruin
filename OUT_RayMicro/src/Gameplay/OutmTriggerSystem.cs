using System.Numerics;
using OUT_RayMicro.Core;
using OUT_RayMicro.Input;
using OUT_RayMicro.World;

namespace OUT_RayMicro.Gameplay;

public sealed class OutmTriggerSystem
{
    private readonly OutmUseSystem useSystem;
    private string currentTriggerId = "";

    public OutmTriggerSystem(OutmUseSystem useSystem)
    {
        this.useSystem = useSystem;
    }

    public void UpdateUseTriggers(OutmWorld world, OutmDemoMap map, OutmTriggerStore triggers, OutmDoorStore doors, Vector3 actorPosition, Vector3 actorForward, in OutmInputFrame input)
    {
        if (world.PlayerVitals.IsDead)
            return;

        if (!triggers.TryGetEntered(actorPosition, out OutmTriggerRecord trigger))
        {
            currentTriggerId = "";
            return;
        }

        if (!string.Equals(currentTriggerId, trigger.Id, StringComparison.OrdinalIgnoreCase))
        {
            currentTriggerId = trigger.Id;
            world.Emit(new OutmEvent(OutmEventType.TriggerEntered, world.PlayerEntity, trigger.Entity, actorPosition, 0, trigger.Id));
        }

        if (!input.IsPressed(OutmButtons.Use))
            return;

        var request = new OutmUseRequest(world.PlayerEntity, actorPosition, actorForward, 0.0f, world.Tick);
        useSystem.UseTrigger(world, map, doors, trigger, request);
    }
}
