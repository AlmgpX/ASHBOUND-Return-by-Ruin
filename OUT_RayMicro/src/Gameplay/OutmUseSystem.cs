using System.Numerics;
using OUT_RayMicro.Core;
using OUT_RayMicro.World;

namespace OUT_RayMicro.Gameplay;

[Flags]
public enum OutmUseCapabilityFlags : ushort
{
    None = 0,
    Instant = 1 << 0,
    Hold = 1 << 1,
    Toggle = 1 << 2,
    Door = 1 << 3,
    Trigger = 1 << 4,
    Pickup = 1 << 5,
    Editor = 1 << 6
}

public readonly struct OutmUseRequest
{
    public readonly EntityId User;
    public readonly Vector3 Origin;
    public readonly Vector3 Direction;
    public readonly float HoldTime;
    public readonly int Tick;

    public OutmUseRequest(EntityId user, Vector3 origin, Vector3 direction, float holdTime, int tick)
    {
        User = user;
        Origin = origin;
        Direction = direction.LengthSquared() > 0.0001f ? Vector3.Normalize(direction) : Vector3.UnitZ;
        HoldTime = MathF.Max(0.0f, holdTime);
        Tick = tick;
    }
}

public readonly struct OutmUseResult
{
    public readonly bool Accepted;
    public readonly EntityId Target;
    public readonly OutmUseCapabilityFlags Caps;
    public readonly float Value;
    public readonly string Message;

    public OutmUseResult(bool accepted, EntityId target, OutmUseCapabilityFlags caps, float value, string message)
    {
        Accepted = accepted;
        Target = target;
        Caps = caps;
        Value = value;
        Message = message;
    }

    public static OutmUseResult Rejected(string message)
    {
        return new OutmUseResult(false, EntityId.None, OutmUseCapabilityFlags.None, 0.0f, message);
    }

    public static OutmUseResult AcceptedResult(EntityId target, OutmUseCapabilityFlags caps, float value, string message)
    {
        return new OutmUseResult(true, target, caps, value, message);
    }
}

public readonly struct OutmInteractionHit
{
    public readonly bool HasHit;
    public readonly EntityId Target;
    public readonly string TargetId;
    public readonly string Kind;
    public readonly Vector3 Point;
    public readonly float Distance;
    public readonly OutmUseCapabilityFlags Caps;

    public OutmInteractionHit(bool hasHit, EntityId target, string targetId, string kind, Vector3 point, float distance, OutmUseCapabilityFlags caps)
    {
        HasHit = hasHit;
        Target = target;
        TargetId = targetId;
        Kind = kind;
        Point = point;
        Distance = distance;
        Caps = caps;
    }

    public static readonly OutmInteractionHit None = new(false, EntityId.None, "", "", Vector3.Zero, 0.0f, OutmUseCapabilityFlags.None);
}

public sealed class OutmUseSystem
{
    public OutmInteractionHit FocusTrigger(OutmTriggerStore triggers, Vector3 actorPosition)
    {
        if (!triggers.TryGetEntered(actorPosition, out OutmTriggerRecord trigger))
            return OutmInteractionHit.None;

        return new OutmInteractionHit(
            true,
            trigger.Entity,
            trigger.Id,
            trigger.Kind,
            actorPosition,
            0.0f,
            ResolveCaps(trigger.Kind));
    }

    public OutmUseResult UseTrigger(OutmWorld world, OutmDemoMap map, OutmDoorStore doors, OutmTriggerRecord trigger, in OutmUseRequest request)
    {
        OutmUseCapabilityFlags caps = ResolveCaps(trigger.Kind);
        if (caps == OutmUseCapabilityFlags.None)
            return Reject(world, request, $"use rejected: unknown trigger kind {trigger.Kind}");

        if ((caps & OutmUseCapabilityFlags.Door) != 0)
            return UseDoor(world, map, doors, trigger, request, caps);

        return Reject(world, request, $"use rejected: unsupported caps {caps}");
    }

    private static OutmUseResult UseDoor(OutmWorld world, OutmDemoMap map, OutmDoorStore doors, OutmTriggerRecord trigger, in OutmUseRequest request, OutmUseCapabilityFlags caps)
    {
        if (!doors.TryToggle(trigger.Target, out bool open))
            return Reject(world, request, $"use rejected: missing door {trigger.Target}");

        doors.SyncToDemoMap(map);
        doors.TryGet(trigger.Target, out OutmDoorRecord door);
        string message = open ? "use door opened" : "use door closed";

        world.Emit(new OutmEvent(
            OutmEventType.TriggerEntered,
            request.User,
            trigger.Entity,
            request.Origin,
            open ? 1.0f : 0.0f,
            message));

        world.Emit(new OutmEvent(
            OutmEventType.DoorToggled,
            request.User,
            door.Entity,
            request.Origin,
            open ? 1.0f : 0.0f,
            open ? "door opened" : "door closed"));

        return OutmUseResult.AcceptedResult(door.Entity, caps, open ? 1.0f : 0.0f, message);
    }

    private static OutmUseResult Reject(OutmWorld world, in OutmUseRequest request, string message)
    {
        world.Emit(new OutmEvent(
            OutmEventType.Debug,
            request.User,
            EntityId.None,
            request.Origin,
            0.0f,
            message));

        return OutmUseResult.Rejected(message);
    }

    private static OutmUseCapabilityFlags ResolveCaps(string kind)
    {
        return kind switch
        {
            "door_toggle" => OutmUseCapabilityFlags.Instant | OutmUseCapabilityFlags.Toggle | OutmUseCapabilityFlags.Door | OutmUseCapabilityFlags.Trigger,
            "use_door" => OutmUseCapabilityFlags.Instant | OutmUseCapabilityFlags.Toggle | OutmUseCapabilityFlags.Door | OutmUseCapabilityFlags.Trigger,
            _ => OutmUseCapabilityFlags.None
        };
    }
}
