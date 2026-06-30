using System.Numerics;
using OUT_RayMicro.Core;
using OUT_RayMicro.Gameplay;

namespace OUT_RayMicro.World;

public sealed class OutmMapRuntimeStores
{
    public readonly OutmDoorStore Doors = new();
    public readonly OutmTriggerStore Triggers = new();
    public readonly OutmPickupStore Pickups = new();
    public readonly OutmEntityChunkMembership Chunks = new();

    public int StaticWorldEntities;
    public int DoorEntities => Doors.Count;
    public int TriggerEntities => Triggers.Count;
    public int PickupEntities => Pickups.Count;
}

public static class OutmMapEntitySpawner
{
    public static OutmMapRuntimeStores Spawn(OutmWorld world, OutmMapDef def, OutmDemoMap map, OutmLogicTickScheduler chunks)
    {
        var runtime = new OutmMapRuntimeStores();

        for (int i = 0; i < map.Boxes.Count; i++)
        {
            OutmBox box = map.Boxes[i];
            EntityId entity = world.Entities.Create(OutmEntityKind.StaticWorld, box.Id);
            world.Transforms.Set(entity, box.Center, Vector3.Zero);
            runtime.Chunks.Set(entity, chunks.PositionToChunk(box.Center));
            runtime.StaticWorldEntities++;
        }

        for (int i = 0; i < map.Doors.Count; i++)
        {
            OutmDoorRuntime door = map.Doors[i];
            EntityId entity = world.Entities.Create(OutmEntityKind.Door, door.Id);
            world.Transforms.Set(entity, door.Center, Vector3.Zero);
            runtime.Chunks.Set(entity, chunks.PositionToChunk(door.Center));
            runtime.Doors.Add(entity, door.Id, door.Center, door.Size, door.Color, door.Open, door.SurfaceId);
        }

        for (int i = 0; i < map.Triggers.Count; i++)
        {
            OutmTriggerRuntime trigger = map.Triggers[i];
            EntityId entity = world.Entities.Create(OutmEntityKind.Trigger, trigger.Id);
            world.Transforms.Set(entity, trigger.Center, Vector3.Zero);
            runtime.Chunks.Set(entity, chunks.PositionToChunk(trigger.Center));
            runtime.Triggers.Add(entity, trigger.Id, trigger.Kind, trigger.Target, trigger.Center, trigger.Size);
        }

        for (int i = 0; i < def.Pickups.Length; i++)
        {
            OutmPickupDef pickup = def.Pickups[i];
            Vector3 position = OutmMapDef.ToVector3(pickup.Position, Vector3.Zero);
            EntityId entity = world.Entities.Create(OutmEntityKind.Pickup, pickup.Id);
            world.Transforms.Set(entity, position, Vector3.Zero);
            runtime.Chunks.Set(entity, chunks.PositionToChunk(position));
            runtime.Pickups.Add(entity, pickup.Id, pickup.Kind, position, pickup.Radius, pickup.Amount, pickup.ArmorTier, pickup.Surface);
        }

        return runtime;
    }
}
