# OUTL Abstract Foundation Prefabs

Canonical setup menu:

```text
OUT CORE Lite/Setup/Create Core Gameplay Skeleton
```

Output folder:

```text
Assets/OUT/OUT_Core/OUT_CORE_Lite/Templates/Foundation
```

This generator creates neutral, reusable prefab classes for building a real game without baking specific lore, enemy names or project content into OUT CORE Lite.

Generated prefab classes:

- `OUTL_Abstract_Actor_Damageable`
- `OUTL_Abstract_Actor_Controlled`
- `OUTL_Abstract_Actor_ArmedRanged`
- `OUTL_Abstract_Actor_ArmedMelee`
- `OUTL_Abstract_NPC_Ranged`
- `OUTL_Abstract_NPC_Melee`
- `OUTL_Abstract_Creature`
- `OUTL_Abstract_Turret_Projectile`
- `OUTL_Abstract_Object_Destructible`
- `OUTL_Abstract_Object_Interactable`
- `OUTL_Abstract_ItemPickup`
- `OUTL_Abstract_Projectile`

Each actor prefab is Def/Profile-first and uses the existing stack:

```text
OUTL_EntityAdapter
OUTL_DamageReceiver
OUTL_Vitals
OUTL_DeathHandler
Collider
OUTL_Hitbox
optional OUTL_AttackDriver
optional OUTL_EquipmentRuntime
optional OUTL_AIActor
optional OUTL_NavMeshMover
optional OUTL_NPCBehaviorController
```

The controlled actor uses `OUTL_PlayerInputSource -> OUTL_ActorControlBridge -> OUTL_CharacterControllerInputSink/OUTL_AttackDriverInputSink`, so player movement and attacks stay on the OUTL tick/input contract instead of a hidden `Update` controller.

Projectiles are authored with `OUTL_Projectile` already on the prefab. Runtime combat uses the existing pool path through `OUTL_AttackDriver` and `OUTL_PoolSystem`; there is no runtime `AddComponent` or direct prefab construction in gameplay code.

Usage:

1. Run the canonical setup menu.
2. Duplicate a Foundation prefab into game content.
3. Replace primitive visuals/audio/balance in the duplicate.
4. Keep the OUTL components and Def/Profile references intact.
5. Give placed persistent scene instances their own `StableId` if they must save/load.

The generated templates set `SavePersistent = false` by default so duplicated/spawned game prefabs do not accidentally share save ids.

Canonical path:

- Use `Templates/Foundation` for new reusable game prefabs.
- Old `Templates/Create`, `Templates/Demo`, `Templates/Profiles`, `Templates/Worldgen` and `Samples/*` assets are legacy/demo compatibility material.
- Old contentful setup/demo/workbench menu actions are hidden from the active Unity menu surface; the generator remains an editor-only internal helper called by the canonical skeleton setup.
