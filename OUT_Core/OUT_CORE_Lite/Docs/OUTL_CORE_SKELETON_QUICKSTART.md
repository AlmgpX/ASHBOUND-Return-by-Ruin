# OUTL Core Skeleton Quickstart

Canonical setup menu:

```text
OUT CORE Lite/Setup/Create Core Gameplay Skeleton
```

This is the only active setup entry for new OUT CORE Lite work. It creates/repairs the abstract Foundation prefabs, then builds a small scene skeleton that can be used as a practical starting point for a full game.

## What It Creates

- `OUTL_World` runtime root with scheduler, pool, quick save input and pool prewarm plan.
- Save spawn resolver, quest bootstrap, day phase runtime, materialization service and abstract encounter budget on the same `OUTL_World`.
- Solid ground cube with a non-trigger collider.
- `PlayerActor` using the actor input bridge and character/attack sinks.
- `EnemyNPC`, `FriendlyNPC` and `CreatureNPC` using the generic AI/NPC behavior stack.
- Generic pickup, destructible object, projectile prefab/muzzle and attack profiles.
- `ChestContainer` using `OUTL_ContainerRuntime`, `OUTL_ChestInteractable` and deterministic `OUTL_LootTableDef` rolling.
- `OUTL_Quest_CoreSkeleton` with kill, collect and open-container objectives listening to OUTL events.
- Local Egregore zone with archetypal cycle state, WorldLedger output and F6 debug view.
- Basic faction assets, tick profile, NPC behavior profiles/schedules and pool prewarm entries.
- Console validation report for the created skeleton.

## Player Stack

```text
OUTL_EntityAdapter
OUTL_Vitals
OUTL_DamageReceiver
OUTL_DeathRuntime / OUTL_DeathHandler
OUTL_PlayerInputSource
OUTL_ActorControlBridge
OUTL_CharacterControllerInputSink
OUTL_AttackDriverInputSink
OUTL_InventoryRuntime
OUTL_EquipmentRuntime
```

## NPC Stack

```text
OUTL_EntityAdapter
OUTL_AIActor
OUTL_NPCBehaviorController
OUTL_BotInputDriver
OUTL_ActorControlBridge
OUTL_NavMoverInputSink
OUTL_AimInputSink
OUTL_AttackDriverInputSink
OUTL_TacticalPlanner
OUTL_AimPlanner
OUTL_AIArsenalSelector
OUTL_Vitals
OUTL_DamageReceiver
OUTL_DeathRuntime / OUTL_DeathHandler
```

Creature actors add:

```text
OUTL_AbilityInputSink
OUTL_LeapAbilityProfile
```

## Local Egregore Stack

```text
OUTL_EntityAdapter
OUTL_EgregoreComponent
OUTL_LocalEgregoreDef
OUTL_EgregoreRuntime
OUTL_EgregoreDebugView
```

The local egregore listens to OUTL events/stimuli and writes its current cycle phase into `OUTL_World.WorldLedger`. NPC behavior can read the local phase as a behavior mode, quests can push the cycle through archetypal hooks, and loot rolls can read the same local context.

## Runtime Loop

```text
Actor / NPC / Container / Pickup
  -> OUTL_Command / OUTL_Event
  -> Inventory / Quest / Loot / Egregore
  -> WorldLedger summary
  -> NPC behavior / loot context / abstract encounter stimulus
  -> SaveSystem component payloads
```

Materialization is owned by `OUTL_World.Materialization`. Full/Near actors stay as pooled GameObjects; Far/Dormant persistent actors can be captured into save records, released through the pool and re-materialized through `OUTL_SaveSpawnResolverRegistry`. The save file now stores both materialized entities and abstract entity records.

## How To Test

1. Open an empty scene.
2. Run `OUT CORE Lite/Setup/Create Core Gameplay Skeleton`.
3. Press Play.
4. Confirm the Console report has no missing stack lines.
5. Test player movement/attack, NPC behavior budget counters, projectile pooling, pickup inventory, destructible death/drop, chest open/loot-once, quest completion events, creature ability input and F6 egregore debug state.
6. Use quick save/load on the runtime root to verify inventory, chest looted state, quest objective counters, egregore phase and abstract materialization records survive roundtrip.

Runtime gameplay construction still goes through the pool/world path. Editor setup and repair are allowed to create scene objects and attach components.

## RC Inspection Commands

```text
SV_Debug_Health 2
SV_Debug_Health_Offset 0.5
outl.debug.hud 2
outl.debug.map 2
sv_tick
sv_npc_budget 64
sv_route_budget 32
sv_path_budget 8
sv_npc_interrupt_budget 32
```

Use `OUT CORE Lite/Workbench/Runtime Code Scanner` and `OUT CORE Lite/Validate Open Scene` before treating a scene as RC-ready.
