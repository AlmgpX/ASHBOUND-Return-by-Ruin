# OUTL Actor Combat Stack Test Note

Minimal workbench/golden case:

1. Actor A has `OUTL_EntityAdapter`, `OUTL_AttackDriver`, a muzzle, and a melee or hitscan `OUTL_AttackProfile`.
2. Actor B has `OUTL_EntityAdapter`, `OUTL_DamageReceiver`, `OUTL_Vitals`, `OUTL_DeathHandler`, a collider, and `OUTL_Hitbox` or root collider fallback.
3. Actor A attacks Actor B.
4. Actor B loses `Health`.
5. The world event bus emits `Damaged`.
6. When Actor B reaches zero health, the event bus emits `Killed`.
7. `OUTL_DeathHandler` reacts to `Killed`.
8. Gameplay code must not use runtime `Instantiate`, `Destroy`, or runtime component construction for this path.

`OUTL_GoldenTestRunner` includes this boundary check in its melee combat golden test. It constructs synthetic test actors inside the debug test boundary, then verifies damage, damaged/killed events, and death-handler reaction.
