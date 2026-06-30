# OUTL Tactical AI

Minimal stack:

- `OUTL_EntityAdapter`, `OUTL_Vitals`, `OUTL_DamageReceiver`, `OUTL_DeathHandler`
- collider + `OUTL_Hitbox`
- optional `OUTL_AIActor` for perception, memory, visible state and schedules
- `OUTL_TacticalPlanner`
- `OUTL_BotInputDriver`
- `OUTL_ActorControlBridge`
- `OUTL_NavMoverInputSink`, `OUTL_AimInputSink`, `OUTL_AttackDriverInputSink`
- optional `OUTL_AbilityInputSink`

Planner flow:

```text
Schedule -> Stimulus -> TacticalIntent -> BotInputDriver -> ActorInputFrame -> Movement/Aim/Weapon sinks
```

Aim/fire is delayed by `OUTL_AimProfile` reaction delay, fire delay and aim-hold. Friendly-fire checks can hold fire before the input frame asks the weapon sink to shoot.

Ranged actors use `OUTL_AttackProfile` through `OUTL_AttackDriver`. Leap or other creature-style attacks use `OUTL_AbilityProfile` through `OUTL_AbilityInputSink`; this is still the same actor input contract, not a second AI pipeline.

Workbench paths:

- `OUT CORE Lite/AI/Create Tactical AI Sample`
- `OUT CORE Lite/AI/Create Humanoid Soldier Sample`
- `OUT CORE Lite/AI/Create Quake Demon Leap Sample`
- `OUT CORE Lite/AI/Create Spider Pounce Sample`
- `OUT CORE Lite/AI/Validate Tactical AI Setup`

To test: create a sample, enter Play Mode, assign/observe target visibility, then run `OUTL_AITacticalSmokeRunner`.
