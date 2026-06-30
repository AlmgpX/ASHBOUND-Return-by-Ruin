# OUTL Creature Profiles And Abilities

Creatures are generic actors with different profiles. Do not add creature-specific AI classes for core behavior.

## Actor Shape

Use the same actor stack:

- `OUTL_EntityAdapter`
- `OUTL_Vitals`, `OUTL_DamageReceiver`, `OUTL_DeathHandler`
- collider + `OUTL_Hitbox`
- optional `OUTL_AIActor`
- `OUTL_TacticalPlanner`
- `OUTL_BotInputDriver`
- `OUTL_ActorControlBridge`
- movement, aim, weapon and optional ability sinks

## Ability Contract

`OUTL_AbilityProfile` is data for a generic action: slot, cooldown, range, windup, recovery, LOS/ground requirements and tags. `OUTL_AbilityInputSink` is the runtime sink that checks authority, death and cooldown.

`OUTL_LeapAbilityProfile` adds leap speed, arc, duration, impact radius/damage and preferred distance. It can use `CharacterController`, `Rigidbody` impulse or an explicit transform fallback for authored samples.

## Tactical Use

`OUTL_TacticalProfile.PrimaryAbility` and `LeapAbility` let `OUTL_TacticalPlanner` choose `AbilityAttack` or `LeapAttack` when target visibility and range are valid. `OUTL_BotInputDriver` writes the ability request into `OUTL_ActorInputFrame`.

## Testing

Use:

- `OUT CORE Lite/AI/Create Quake Demon Leap Sample`
- `OUT CORE Lite/AI/Create Spider Pounce Sample`
- `OUTL_AITacticalSmokeRunner`

Expected checks: valid leap range produces ability input; cooldown, death and client replica authority block the ability.
