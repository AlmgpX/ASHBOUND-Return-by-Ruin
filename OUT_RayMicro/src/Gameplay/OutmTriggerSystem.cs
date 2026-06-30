using System.Numerics;
using OUT_RayMicro.Core;
using OUT_RayMicro.Input;
using OUT_RayMicro.World;

namespace OUT_RayMicro.Gameplay;

public sealed class OutmTriggerSystem
{
    private string currentTriggerId = "";

    public void UpdateUseTriggers(OutmWorld world, OutmDemoMap map, Vector3 actorPosition, in OutmInputFrame input)
    {
        if (world.PlayerVitals.IsDead)
            return;

        if (!map.TryGetEnteredTrigger(actorPosition, out OutmTriggerRuntime trigger))
        {
            currentTriggerId = "";
            return;
        }

        if (!string.Equals(currentTriggerId, trigger.Id, StringComparison.OrdinalIgnoreCase))
        {
            currentTriggerId = trigger.Id;
            world.Emit(new OutmEvent(OutmEventType.TriggerEntered, EntityId.None, EntityId.None, actorPosition, 0, trigger.Id));
        }

        if (!input.IsPressed(OutmButtons.Use))
            return;

        ExecuteUse(world, map, trigger, actorPosition);
    }

    private static void ExecuteUse(OutmWorld world, OutmDemoMap map, OutmTriggerRuntime trigger, Vector3 actorPosition)
    {
        switch (trigger.Kind)
        {
            case "door_toggle":
            case "use_door":
            {
                bool open = map.TryToggleDoor(trigger.Target);
                world.Emit(new OutmEvent(
                    OutmEventType.DoorToggled,
                    EntityId.None,
                    EntityId.None,
                    actorPosition,
                    open ? 1 : 0,
                    open ? "use: door opened" : "use: door closed"));
                return;
            }

            default:
                world.PushLog($"use ignored: unknown trigger kind {trigger.Kind}");
                return;
        }
    }
}
