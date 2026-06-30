# OUTL AI Input Contract

`NPC` is an actor/entity composition. `Bot` is only an input source.

Runtime path:

```text
PlayerInputSource -> OUTL_ActorInputFrame -> OUTL_ActorControlBridge -> Movement/Aim/Weapon/Interaction sinks
BotInputDriver    -> OUTL_ActorInputFrame -> OUTL_ActorControlBridge -> Movement/Aim/Weapon/Interaction sinks
```

The bot does not move transforms, apply damage or spawn projectiles directly. It fills `OUTL_ActorInputFrame`. `OUTL_NavMoverInputSink` sends movement to `OUTL_NavMeshMover`, and `OUTL_AttackDriverInputSink` sends fire requests to `OUTL_AttackDriver`.

Use `OUTL_AIActor.UseActorInputContract` for new tactical actors so `OUTL_AIActor` remains perception/schedule/debug state and does not fire through the legacy fallback path.

## Phase Order

`OUTL_ActorControlBridge` sorts `OUTL_IActorInputPhasedSink` by `OUTL_ActorInputPhase`:

- `Movement = 0`
- `Aim = 10`
- `Weapon = 20`
- `Interaction = 30`

This keeps movement and view alignment before weapon fire. `OUTL_NavMoverInputSink` is Movement, `OUTL_AimInputSink` is Aim, and weapon/ability sinks are Weapon.

## Fire Authorization

AI fire is explicit. `OUTL_AimPlanner` sets `FireAuthorized` only after reaction delay, line/friendly-fire checks, aim angle and settle time pass. `OUTL_AttackDriverInputSink` blocks weapon fire when `FireAuthorized == false` and exposes `LastBlockedReason`.

Player input can set `FireAuthorized = true` because the player is the source of intent; authority is still checked by runtime/network gates.

## Ability Input

`OUTL_ActorInputFrame` also carries generic ability requests:

- primary/secondary pressed or held
- ability slot
- target point

`OUTL_AbilityInputSink` validates cooldown, death and network authority before executing an authored `OUTL_AbilityProfile`.
