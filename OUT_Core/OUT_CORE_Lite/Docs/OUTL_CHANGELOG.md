# OUTL Change Log

## 2026-06-25 - Access, Keys and Persistent Puzzle Logic

- Added command guards and `OUTL_AccessController`; access requirements are evaluated before door/button/platform receivers can handle a command.
- Added source-aware conditions, inventory `HasItem`, quest-stage and state-flag conditions plus `Lock` / `Unlock` commands.
- Inventory removal is atomic, item events carry stack counts, and inventory save can resolve dynamic items by stable definition id through `OUTL_DefDatabase`.
- Doors, buttons, logic gates/relays, multi-source logic, triggers, kill counters, output-once flags and path movers now participate in component save.
- Added a key-door sample preset, access validation and smoke coverage for retained keys, consumable keys, atomic inventory and puzzle state roundtrip.

## 2026-06-04 - RC Completion Compliance Pass

- Runtime code scanner now scans only `OUT_CORE_Lite` and classifies construction/search hits as `Allowed`, `Violation` or `NeedsReview` against `OUT_CORE_LITE.md`.
- Scene validator now shares the same construction boundary rules, ignores canonical pool facade calls and editor-authoring partials, and is available from `OUT CORE Lite/Validate Open Scene`.
- Moved actor hurtbox prefab authoring out of `OUTL_ActorShapeProfiles` into an editor-only authoring partial; runtime actor shape code refuses play-mode construction and contains no gameplay lifetime policy.
- Debug map mode 2 now shows NPC action, route progress, movement authority, AI intent, pickup/container state for fast Play Mode inspection.
- Dev console now exposes all NPC behavior dispatcher limits, including `sv_npc_interrupt_budget`, alongside behavior, route and path budgets.
- Verified runtime/editor compile with `0 errors`, missing-script scan clean, script GUID scan clean, and runtime gameplay construction scan clean.

## 2026-06-01 - Final Core Skeleton Integration Pass

- Added `OUTL_MaterializationSystem` under `OUTL_World`: persistent actors can capture save/component state into abstract ledger records, release through `OUTL_PoolSystem`, and re-materialize through the existing save spawn resolver.
- `OUTL_SaveSystem` now captures materialized entities plus abstract entity records, restores component payloads, and keeps life/death, inventory, NPC behavior, route progress, stimulus and egregore state on the canonical save path.
- Added minimal `OUTL_ContainerDef`, `OUTL_ContainerRuntime` and `OUTL_ChestInteractable`; containers open through commands, roll `OUTL_LootTableDef` once with a deterministic seed, emit container/item events and save opened/looted state.
- Quest runtime now supports objective counters for kill, collect, interact/reach and open chest objectives; counters are saved and restored, and completion/failure still emits the existing OUTL quest events for egregore integration.
- Added `OUTL_QuestBootstrap` for registering skeleton quests through the existing `OUTL_World.Quests` system.
- Route cache now asks `OUTL_WorldRouteGraph` for a minimal grid A* path before falling back to straight cell paths; NPC route cache uses the shared world route cache.
- Added day phase events on `OUTL_World` and a small budgeted abstract encounter pass that emits ledger-context danger stimuli instead of free-floating random spawns.
- `Create Core Gameplay Skeleton` now wires save resolver, quest bootstrap, materialization settings, abstract encounter budget, chest/container and core quest assets from the single canonical setup path.

## 2026-06-01 - Archetypal Egregore Cycle Pass

- Extended Egregore Lite from flat mood values into a scheduler-driven archetypal cycle for local places.
- Added archetype/cycle/rule data: archetype defs, cycle phases, archetype pressure, transformation, shadow and integration rules.
- `OUTL_EgregoreRuntime` now stores current cycle phase, dominant/shadow archetypes, tension, integration/corruption/renewal progress, trauma/boon memory, sacrifice debt, threshold state and memory traces.
- Egregore events now react to damage/death, pickups/containers, quest completion/failure and tagged ritual/hunger/desire/raid stimuli.
- Egregore ticks write `OUTL_EgregoreField` into `OUTL_WorldLedger`; NPC behavior and loot context read that summary without scanning all NPCs.
- Quest definitions gained optional archetypal hooks; quest completion/failure emits existing OUTL events that can move the local cycle.
- Core Gameplay Skeleton now creates a local Egregore zone and validates death, quest, NPC behavior, loot context and save/load restoration.

## 2026-06-01 - Core Gameplay Skeleton + World Ledger Pass

- Added the single active setup menu `OUT CORE Lite/Setup/Create Core Gameplay Skeleton`.
- Hid old setup/demo/workbench menu actions so new users enter through one canonical path.
- Skeleton setup creates the runtime root, ground, controlled actor, enemy/friendly/creature NPCs, pickup, destructible object, projectile/muzzle/profile links, factions, tick profile and pool prewarm plan.
- Foundation NPC prefabs now include the actor input bridge, bot input source, nav/aim/attack sinks, tactical planner, aim planner, arsenal selector and creature ability sink where applicable.
- Added `OUTL_PoolPrewarmPlan` for bootstrap prewarm through the existing `OutCore.pool.OUT` facade.
- Added a compact world ledger data layer on `OUTL_World`: cell keys, addresses, entity/NPC abstract records, cell summaries, movement cost profile, route graph, route cache and debug texture helper.
- Ledger updates are fed from `OUTL_EntityAdapter`, `OUTL_NPCBehaviorController` and `OUTL_World.Despawn`; records store ids, strings, enums, flags, floats and `Vector3`, not Unity object references.
- Added `Docs/OUTL_CORE_SKELETON_QUICKSTART.md`.

## 2026-06-01 - Abstract Foundation Prefab Generator

- Added an editor-only generator for neutral reusable game-building prefabs under `Templates/Foundation`.
- Generated abstract actor classes for damageable, controlled, armed ranged, armed melee, ranged NPC, melee NPC, creature, projectile turret, destructible object, interactable object, item pickup and projectile.
- Added generic Def, Faction, AttackProfile, EquipmentItem, AI, Perception, StateTable, NPC behavior, navigation and loot assets wired to those prefabs.
- Foundation projectile attacks reference a prefab that already contains `OUTL_Projectile`; no runtime component repair is needed.
- Foundation prefabs default to `SavePersistent = false` so reusable templates do not duplicate stable save ids.
- Demoted old contentful sample/preset generators to `OUT CORE Lite/Legacy Demo/*`; `Templates/Foundation` is the canonical reusable prefab path.
- Made world narrative layer texture export editor-only so runtime writer no longer performs direct Unity object destruction.
- Added `OUTL_CharacterControllerInputSink` and switched generated controlled Foundation actors to the OUTL actor input bridge instead of the legacy `OUTL_BasicPlayerController`.

## 2026-06-01 - Canon Compliance Fix Pass

- Moved projectile and trigger turret simulation off Unity `Update` into OUTL scheduler ticks.
- Removed runtime tag/find focus fallbacks from processing drivers; fallback now resolves through OUTL registry identity.
- Removed `Camera.main`/Unity tag focus fallbacks from console pick and chunk/sector debug overlays.
- Converted `OUTL_ExtendedSaveSystem` into a compatibility bridge over the canonical world save.
- Replaced surface tag fallback with `OUTL_SurfaceMarker`/physic material/name resolution.
- Routed `OUTL_Interactable` through canonical `OUTL_OutputLink` and made legacy direct targets/UnityEvents migration-only.
- Removed hierarchy-name and Unity instance-id fallbacks from UI/bootstrap/debug labels, surface probing, NPC phase salts and entity diary file paths.
- Removed coroutine-based delayed audio release from the pool system.
- Tightened scene validator source checks for runtime find/tag/coroutine construction violations.

## 2026-06-01 - Tactical AI Hardening + Ability/Save Pass

- Added phased actor input ordering: Movement, Aim, Weapon, Interaction.
- Added `FireAuthorized`, aim confidence and max fire angle gates to `OUTL_ActorInputFrame`.
- Added `OUTL_AimInputSink` and hardened `OUTL_AttackDriverInputSink` so AI fire can be blocked by aim/fire authorization.
- Added generic `OUTL_AbilityProfile`, `OUTL_LeapAbilityProfile` and `OUTL_AbilityInputSink` for profile-authored actor abilities.
- Added sector-bucketed cover queries with query counters and non-alloc local bucket lookup.
- Added NPC dispatcher debug snapshot for registered/ticked/skipped/budget counters.
- Added save workbench menus and smoke checks for NPC runtime, death and inventory roundtrip.
- Added tactical workbench samples for humanoid ranged actors and generic leap/pounce actors.

## 2026-06-01 - Tactical AI Input Contract Pass

- Added actor input contracts and bridge sinks for player/bot parity.
- Added `OUTL_BotInputDriver`, `OUTL_TacticalPlanner`, aim/fire delay, friendly-fire evaluation and arsenal selection.
- Upgraded cover to a registered/queryable/reservable runtime system.
- Added squad blackboard/shared cover reservations without adding a new AI manager.
- Added tactical AI smoke runner, debug snapshot and workbench menus.
- New tactical actors use `BotInputDriver -> ActorControlBridge -> NavMover/AttackDriver`; the bot does not move, damage or spawn directly.

## 2026-05-31 - Death Authority + NPC World Behavior Pass

- Added unified life/death runtime state through `OUTL_LifeState` and `OUTL_DeathRuntime`.
- Added `OUTL_NetworkAuthority` so damage, kill, drop, pickup and NPC schedule advancement are server/offline authority only.
- Updated combat/vitals/death handling so death emits once, death stimulus is emitted, dead actors stop attack/AI/navigation, and player death does not immediately despawn the player object.
- Added NPC schedule, behavior runtime, navigation profile, stimulus interrupt policy, route cache and abstract navigator for far/dormant NPC travel.
- Added `OUTL_NPCBehaviorController` as a scheduler-driven layer over `OUTL_AIActor`, not a replacement brain.
- Extended `OUTL_TickProfile` and `OUTL_World` with NPC behavior/path/stimulus budgets and tier intervals.
- Added minimal `OUTL_LootTableDef`, `OUTL_LootDropper` and `OUTL_ItemPickup` using the existing inventory, pool and world despawn paths.
- Added compact `OUTL_NPCWorldSmokeRunner` and workbench menu presets for generic NPC behavior models.
- Added NPC behavior dispatcher budgets, shared route cache, route request budget checks, stronger Mirror damage/pickup request validation, fixed loot stack count rolling, and expanded smoke checks/cleanup.

## 2026-05-31 - Pool, Stimulus, Sector, Egregore Readiness Pass

- Expanded `OutCore.pool.OUT` facade with common instantiate/destroy/release/prewarm/stat overloads.
- Added pooled instance metadata, double-release guard, pool stats, parent containers and Rigidbody2D reset support.
- Added runtime code scanner editor window for construction/search/hot-path hits.
- Added sector integrity validation, safe rebuild and runtime overlay stats.
- Extended stimuli with sector-indexed storage, query/consume APIs, budgeted cleanup and `OUTL_StimulusSensor`.
- Added AI memory fields for suspicion, fear/aggression memory and faction/allegiance influence.
- Added optional egregore module and debug view.
- Added `OUTL_TickProfile` and stimulus cleanup budget support on `OUTL_World`.
- Added golden diagnostics for pool stats, stimulus budget and egregore reaction.
