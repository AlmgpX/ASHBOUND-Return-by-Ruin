# OUTL Occultist Enemy Pack

Canonical OUT CORE Lite enemy stack. The generated models are placeholders.

## Enemy prefabs

- `OUTL_Enemy_Occultist_Shotgun`
- `OUTL_Enemy_Occultist_Rifle`
- `OUTL_Enemy_Occultist_SMG`
- `OUTL_Enemy_Occultist_Grenadier`
- `OUTL_Enemy_Occultist_Breacher`

Replace `VisualRoot_REPLACE_ME/PlaceholderBody_DELETE_ME` with the final model. Assign its renderers and animators to `OUTL_ProcessingTierController`.

Fill the four audio profiles in `Audio` with alert/combat/pain/death clips. Audio playback uses the shared OUTL audio pool; do not add one permanent AudioSource per enemy.

## Required scene setup

1. Add one `OUTL_World`.
2. Add one `OUTL_ChunkProcessingDriver`, set Focus to the player and disable both Parallel Readiness diagnostic toggles.
3. Add `OUTL_Occultist_RuntimeRig` for pool prewarm and save/materialization prefab resolution.
4. Enable automatic materialization on `OUTL_World`.
5. Recommended materialization values: tick `0.25`, budget `12`, enter distance `72-88`, exit distance `128-144`.
6. Player must have `OUTL_EntityAdapter`, `OUTL_Vitals`, a collider and `OUTL_Faction_Player`.

Canonical chunk rings are `Full 0 / Near 1 / Mid 2 / Far 3`: the expensive actor area is at most the center `3x3`; ring 2 is simplified, ring 3 is far, and everything beyond is dormant.

Each enemy prefab contains `OUTL_CharacterIdentity`. Name, surname, nickname and attributes are deterministic from `StableId`, are written into the abstract spawn record before materialization, and survive pool reuse.
7. For the current legacy player health system add `OUTL_LegacyPlayerHealthBridge`.

The player's adapter `TargetName` must match `OUTL_World.MaterializationFocusTargetName`.

## 1000-enemy field

`OUTL_Occultist_1000_AbstractField` registers 1000 compact abstract records and creates no enemy GameObjects at startup. Only records entering materialization range are spawned from the OUTL pool.

Place the field over walkable/NavMesh terrain and resize its `Size`. Do not place 1000 prefab instances manually.

## Runtime design

- No enemy-owned `Update` or coroutine in targeting, combat, barks or wandering.
- Five states: Wander, Alert, Combat, ReturnHome, Dead.
- Hostile lookup uses the OUTL sector grid.
- Physical bullets and grenades use `OUTL_RigidbodyProjectile`.
- Projectiles, enemies, impacts and positional audio use OUTL pooling.
- Far and dormant behavior is scheduler-budgeted.
- Abstract materialization is spatially indexed instead of scanning all dormant records.
