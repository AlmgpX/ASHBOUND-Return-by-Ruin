# OUTL Actor Combat Stack

An Actor is a prefab/def composition, not a class hierarchy. The same stack should cover NPCs, damageable props, neutral objects, melee users, projectile users, weak points, and future vehicle-style targets.

## Damageable Stack

Required on the authored prefab or repaired selection:

- `OUTL_EntityAdapter`
- `OUTL_DamageReceiver`
- `OUTL_Vitals`
- `OUTL_DeathHandler`
- at least one `Collider`
- at least one `OUTL_Hitbox`, or a root collider that can be resolved as the fallback damage target
- `Health` and `MaxHealth` through `OUTL_EntityDef.BaseStats`, or `OUTL_Vitals.InitializeMissingStats`

Use `OUT CORE Lite/Workbench/Repair Selected Actor Combat Stack` to add the generic stack in editor setup.

## Armed Stack

An armed actor adds `OUTL_AttackDriver` and an assigned `OUTL_AttackProfile`. The profile is data first: mode, damage, cooldown, range, damage key, VFX/SFX, and projectile data live there.

Projectile attacks require `ProjectilePrefab` to already contain `OUTL_Projectile`. Do not rely on runtime component construction to fix broken projectile prefabs.

Use `OUT CORE Lite/Workbench/Repair Selected Armed Actor` after assigning or selecting an existing attack profile.

## NPC Stack

An NPC actor adds `OUTL_AIActor` and normally `OUTL_NavMeshMover`. It still uses the same damageable and armed stack. AI target acquisition should come from existing faction hostility or profile enemy tags.

Use `OUT CORE Lite/Workbench/Repair Selected NPC Combat Stack`, then assign an `OUTL_AIProfile` and faction/profile data as needed.

## Damage Flow

`OUTL_AttackDriver` executes an `OUTL_AttackProfile`. Hitscan, melee, direct, projectile, and explosion paths resolve a target through `OUTL_Hitbox` or root collider fallback, then call `OUTL_Combat.ApplyDamage`.

`OUTL_Combat` reduces `Health`, emits `Damaged`, and emits `Killed` when health crosses zero. `OUTL_Vitals` applies dead state, while `OUTL_DeathHandler` reacts to killed events with authored VFX/SFX, AI disable, collider/renderer disable, or pooled despawn.

## Hitboxes

Hitboxes are generic zones:

- `Generic`
- `Head`
- `Torso`
- `Arm`
- `Leg`
- `WeakPoint`
- `Armor`

Damage keys compose the attack key and hitbox suffix, for example `bullet.head`, `slash.armor`, `fire.weakpoint`, or `explosion.torso`.

## Unity Test

1. Create or select Actor B, add a collider, run `Repair Selected Actor Combat Stack`, and confirm `Health` and `MaxHealth` exist through def stats or `OUTL_Vitals`.
2. Create or select Actor A, run `Repair Selected Armed Actor`, assign an existing `OUTL_AttackProfile`, and make sure the muzzle is set.
3. For projectiles, author the projectile prefab with `OUTL_Projectile` already present before assigning it to the profile.
4. Enter Play Mode and attack Actor B. B should lose health, emit `Damaged`, reach zero health, emit `Killed`, and let `OUTL_DeathHandler` react.
5. Run `OUT CORE Lite/Validate Open Scene` to catch missing stack pieces before shipping the prefab.
