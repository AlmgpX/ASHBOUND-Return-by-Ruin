# OUTL Combat AI Actor Stack

An actor is a prefab composition, not a class hierarchy. The same stack should work for ranged NPCs, melee NPCs, guards, creature actors, and neutral actors that react to danger, food/resources, noise, sight, and damage.

## Damageable Actor

Required:

- `OUTL_EntityAdapter`
- `OUTL_DamageReceiver`
- `OUTL_Vitals`
- `OUTL_DeathHandler`
- at least one `Collider`
- `OUTL_Hitbox` or a clear root collider fallback

Use `OUT CORE Lite/Workbench/Repair Selected Damageable Actor`.

## Armed Actor

Add `OUTL_AttackDriver` and authored `OUTL_AttackProfile` assets. Projectile profiles require `ProjectilePrefab`, and the prefab must already contain `OUTL_Projectile`.

Use `Repair Selected Ranged Combat Actor` for primary ranged profiles, or `Repair Selected Melee Combat Actor` for melee fallback/close combat.

## AI Actor

Add `OUTL_AIActor`, `OUTL_AIPerceptionProfile`, `OUTL_AIStateTable`, and either `OUTL_NavMeshMover` or `Stationary`.

The visible runtime row is owned by `OUTL_AIActor`: state, goal, stimulus, target, weapon/profile, health, fear, aggression, morale, distance, visibility, danger, food, next action, and last event.

Use `Repair Selected AI Actor`.

## Ranged NPC

Ranged actors set `PreferRangedCombat`, use `Primary` for ranged attacks, and keep `Melee` as fallback if authored. If the target is visible and in range, state becomes `AttackRanged`. If close enough for the melee fallback, state becomes `AttackMelee` or `SwitchWeapon`.

## Melee NPC

Melee actors require a melee profile with positive range, radius, and arc. The same `AttackDriver` path applies damage through `OUTL_Hitbox` and `OUTL_Combat`.

## Creature Actor

Creature actors are normal AI actors with `CreatureUsesFoodStimulus` and `FleeFromDanger`. Food/resource tags come from `OUTL_AIPerceptionProfile.FoodTags`; danger tags come from `DangerTags`. A melee profile is optional; assign one only if the creature should attack.

Use `Repair Selected Creature Actor`.

## Stimulus Flow

Supported stimulus types include sight enemy/ally/food/danger, heard noise/combat, took damage, lost target, found cover, low health, goal completed, and schedule changed.

Damage interrupts schedules through `OUTL_AIActor.OUTL_OnEvent`. Combat also emits a heard-combat stimulus for nearby hearing sensors. Sight and ambient danger/food are sampled from the existing sector grid and perception profile.

## Perception Flow

`OUTL_AIPerceptionProfile` controls sight cone, sight distance, hearing radius, danger radius, food radius, memory duration, line of sight, faction filter, and tag filters. Target acquisition still uses existing `OUTL_AIPerceptionUtility`, `OUTL_FactionSystem`, and sector queries.

## State Table

`OUTL_AIStateTable` is a small data table with columns for state, entry/exit conditions, interrupts, main command, target rule, movement rule, attack profile, animation hint, debug color, and notes. Runtime state is exposed by `OUTL_AIStateTableDebugView` on the runtime root.

## Test

1. Repair or create a damageable target actor.
2. Repair a ranged, melee, AI, or creature actor.
3. Assign `OUTL_AIProfile`, faction or enemy tags, perception profile, state table, and attack profiles.
4. Run `OUT CORE Lite/Validate Open Scene`.
5. Press the debug table toggle on the runtime root (`OUTL_AIStateTableDebugView`) and verify state, stimulus, target, weapon/profile, health, fear, aggression, morale, visibility, danger, food, next action, and last event change during play.
