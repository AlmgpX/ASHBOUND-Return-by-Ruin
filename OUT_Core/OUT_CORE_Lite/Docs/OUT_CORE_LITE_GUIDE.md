# OUT CORE Lite Guide

OUT CORE Lite is a compact data-driven gameplay layer for Unity. It is not meant to replace the whole engine. It gives you a small vocabulary for building game logic without turning every prefab into a pile of one-off MonoBehaviours.

Core grammar:

```text
Entity    = noun
Command   = verb
Effect    = action
Event     = fact
Condition = grammar
Def       = dictionary
Scheduler = time
Sector    = space
Save      = memory
Console   = speech of gods
```

## 1. Minimal scene setup

Every scene using Lite should have one runtime object:

```text
OUTL_Runtime
  OUTL_World
  OUTL_PoolSystem optional, auto-added by OUTL_World
  OUTL_DevConsole optional but strongly recommended
```

Fast creation:

```text
OUT CORE Lite -> Profiles -> Create Dev Console In Scene
```

`OUTL_World` owns the registry, event bus, command system, effect system, inventory, quests, scheduler, sector grid, factions and save system. It also applies `sv_gravity` to Unity physics when `ApplySvGravityToPhysics` is enabled. The world ticks logic, AI, quest and random lanes using separate intervals, so not everything needs to run every frame.

Recommended world values:

```text
LogicTickInterval = 0.1
AITickInterval = 0.2
QuestTickInterval = 1
RandomTickInterval = 0.25
RandomTickBudget = 64
DespawnBudgetPerFrame = 64
```

## 2. Entity setup

Any gameplay object that should be known by Lite needs:

```text
OUTL_EntityAdapter
```

The adapter registers into `OUTL_World.Registry`, receives an `OUTL_EntityId`, binds stats/tags/faction from `OUTL_EntityDef`, registers with sectors and scheduler, and forwards commands to local command receivers.

Important fields:

```text
Def = OUTL_EntityDef
Faction = OUTL_FactionDef
Tier = Full / Near / Mid / Far / Dormant
RegisterOnEnable = true
RegisterTick = true for active logic entities
RegisterRandomTick = true only if modules need random ticks
RegisterInSectors = true for AI/searchable objects
TickLane = Logic by default
TickInterval = 0.25
```

Use runtime tags through `OUTL_EntityDef.Tags`, not only Unity tags. Example tags:

```text
Player
Actor
Enemy
NPC
Item
Door
Interactable
```

## 3. Stats and state

Hot-path stats are compact byte ids:

```text
Health
Damage
Speed
Stamina
Mana
Armor
```

Known state ids:

```text
Open
On
Dead
Alert
Combat
Locked
```

These are backed by compact arrays/bit flags. Unknown/custom stat/state keys still work through dictionaries, but do not spam dictionary keys in hot combat loops unless you enjoy wasting CPU cache, because apparently silicon needed suffering too.

## 4. Definitions

Create definitions from Unity menus:

```text
Create -> OUT CORE Lite -> Entity Def
Create -> OUT CORE Lite -> Character Template
Create -> OUT CORE Lite -> Faction Def
Create -> OUT CORE Lite -> Action Def
Create -> OUT CORE Lite -> Item Def
Create -> OUT CORE Lite -> Quest Def
Create -> OUT CORE Lite -> Module Def
Create -> OUT CORE Lite -> AI Profile
```

`OUTL_EntityDef` contains:

```text
ClassName
DisplayName
Tags
BaseStats
Actions
Modules
Prefab
```

`OUTL_ActionDef` is command-driven logic:

```text
TriggerCommand = Use / Open / Attack / Custom...
Conditions
Effects
Cooldown
```

`OUTL_ModuleDef` is a reusable behavior block:

```text
HandledCommands
OnCommandEffects
OnRandomTickEffects
```

## 5. Commands, events and effects

Commands are direct intent:

```text
Use
Damage
Heal
Equip
Attack
Cast
Open
Close
Activate
Deactivate
Talk
AddItem
RemoveItem
SetQuestStage
SendSignal
```

Events are facts emitted after something happened:

```text
CommandExecuted
Used
Damaged
Healed
Killed
ItemAdded
ItemRemoved
Equipped
Unequipped
QuestStageChanged
Signal
RandomTick
Spawned
Despawned
```

Effects are data actions:

```text
Damage
Heal
ModifyStat
AddItem
RemoveItem
SetStateBool
SetStateFloat
SendCommand
SendEvent
SpawnPrefab
PlaySound
SetQuestStage
```

Flow example:

```text
Player presses use -> OUTL_Interactable sends Command Use -> target ActionDef runs Conditions -> Effects run -> Event emitted
```

## 6. Console

Add:

```text
OUTL_DevConsole
```

Useful commands:

```text
help
outl.stats
pick
pick_mouse
pick_self
outl.inspect <id>
outl.ai <id>
outl.aitrace 1
outl.aiwatch <id>
outl.tick
outl.tick ai 0.2
outl.entitytick <id> 0.4
outl.damage <id> 50
outl.kill <id>
outl.save
outl.load
sv_cheats 1
sv_gravity 981
god
noclip
impulse 101
restart
```

`pick` raycasts from screen center and skips the player by default. `pick_self` selects the player. The selected id is copied to clipboard, because manually copying ids from the inspector is how civilization loses wars against spreadsheets.

## 7. Player setup

Minimal player:

```text
PlayerRoot
  OUTL_EntityAdapter
  CharacterController
  OUTL_BasicPlayerController
  OUTL_AttackDriver
  OUTL_PlayerSurfaceMotorModifier optional
  OUTL_SurfaceProbe optional
  OUTL_BasicHUD optional
  OUTL_DamageHUDConnector optional
```

Entity def should include:

```text
Tags = Player, Actor
BaseStats = Health 100, Armor 0
```

Movement uses GoldSrc-like units by default:

```text
UseGoldSrcUnits = true
GoldSrcUnitsPerUnityUnit = 32
ForwardSpeed = 320
JumpSpeed = 270
```

If jump is too high with `sv_gravity 981`, add `OUTL_PlayerSurfaceMotorModifier` and tune:

```text
PlayerJumpMultiplier = 0.65-0.85
```

## 8. Combat setup

Create attack profiles:

```text
Create -> OUT CORE Lite -> Combat -> Attack Profile
```

Attack modes:

```text
Hitscan
Projectile
Melee
Direct
```

On attacker:

```text
OUTL_AttackDriver
  Source = local OUTL_EntityAdapter
  Muzzle = muzzle transform
  Primary = ranged attack
  Secondary = optional
  Melee = melee attack
  SmartMeleeWhenFireAtPrimary = true for AI
  RespectCooldownOnFireAt = true
```

Damage goes through `OUTL_Combat.ApplyDamage`, applies optional `OUTL_DamageModifierSet`, emits `Damaged`, and emits `Killed` when Health crosses zero.

For projectiles, use `OUTL_Projectile` authored on the projectile prefab. Runtime `AddComponent` repair is not allowed for gameplay projectiles.

## 9. Damage modifiers

Use:

```text
OUTL_DamageModifierSet
```

Put it on an entity to scale damage by key/type. Useful for armor, resistances, vulnerabilities, acid/radiation/fire, head/body zones later.

## 10. Death, drops and loot

Typical enemy death stack:

```text
OUTL_DeathHandler
OUTL_Dropper optional
```

Create drop table:

```text
Create -> OUT CORE Lite -> Loot -> Drop Table
```

Drop table entries can spawn either prefabs or `OUTL_EntityDef` through `OUTL_World.Spawn`.

## 11. AI quick setup

Use the ready preset:

```text
OUT CORE Lite -> Profiles -> Create Ready Enemy Ranged Melee Cover
```

Enemy root:

```text
OUTL_EntityAdapter
OUTL_AIActor
OUTL_NavMeshMover
OUTL_AttackDriver
OUTL_HearingSensor
OUTL_PatrolRoute optional
OUTL_EntityDiary optional
OUTL_Dropper optional
```

Assign:

```text
OUTL_AIActor.Profile = OUTL_AI_Profile_ReadyEnemy_RangedMeleeCover
OUTL_AttackDriver.Primary = OUTL_Attack_Enemy_RangedHitscan
OUTL_AttackDriver.Melee = OUTL_Attack_Enemy_Melee
```

AI uses schedules. A schedule is a list of tasks:

```text
Wait
Stop
FindTarget
MoveToTarget
AttackTarget
MoveToPoint
FleeFromTarget
FaceTarget
SendCommandToTarget
ApplyEffects
SetStateFlag
Patrol
InvestigateStimulus
FindCover
MoveToCover
FollowSquadOrder
```

Ready enemy behavior:

```text
Patrol -> Wait -> FindTarget
Hear/see interest -> InvestigateStimulus -> Wait -> FindTarget
Combat -> FindCover -> MoveToCover -> FaceTarget -> AttackTarget
Lost target -> last known position -> clear interest -> return to patrol
```

Important AI fields:

```text
RequireLineOfSightToAcquireTarget = true
RequireLineOfSightToKeepTarget = true
SightBlockMask = world geometry layers
CoverVisibilityMask = world geometry layers
LostTargetGraceTime = 1.25
ForgetTargetAfter = 3-6
StimulusForgetAfter = 6-12
ReturnToPatrolAfterInterestLost = true
```

## 12. NavMesh movement

Use:

```text
OUTL_NavMeshMover
```

If a `NavMeshAgent` exists and is on navmesh, it uses it. Otherwise it falls back to transform movement.

Fields:

```text
Agent
UseTransformFallback
FallbackSpeed
RepathInterval
StopDistance
AffectedByGravity
Flying
Swimming
```

For real NPCs, bake NavMesh and put `NavMeshAgent` on the NPC. Fallback is useful for primitive tests and dumb mobs.

## 13. Patrol

Add:

```text
OUTL_PatrolRoute
```

Assign scene transforms:

```text
Points[0]
Points[1]
Points[2]
Loop = true
PingPong = false
PointReachDistance = 1.2
```

The AI `Patrol` task consumes this route.

## 14. Stimulus and hearing

Emit sound from code:

```csharp
OUTL_StimulusBus.EmitSound(sourceId, transform.position, 25f, 1f, 2f, "gunshot");
```

NPC receives it through:

```text
OUTL_HearingSensor
```

Fields:

```text
HearingMultiplier
MinPriority
UseOcclusionRaycast
OcclusionMask
OcclusionPenalty
IgnoreKeys
```

The sensor passes important stimuli to `OUTL_AIActor.ReceiveStimulus`, which stores last interest position and can switch to investigation.

New scheduler-driven sensor:

```text
OUTL_StimulusSensor
  Mode = Hearing / Vision / Threat / Territory
```

Stimuli are stored in `OUTL_StimulusBus`, indexed by sector and cleaned under the `OUTL_World` stimulus budget.

## 15. Cover

Place cover points manually:

```text
CoverPointObject
  OUTL_CoverPoint
```

The cover system checks whether a ray from threat position to the cover point is blocked by geometry. It is cheap, simple and good enough for Lite.

If cover fails:

```text
Cover objects must have colliders.
CoverVisibilityMask must include cover/wall layers.
CoverPoint must be near usable cover, not inside decorative nonsense.
```

## 16. Squad commander

Commander:

```text
OUTL_SquadCommander
OUTL_AIActor optional but useful
```

Members:

```text
OUTL_SquadMember
  Commander = commander reference
```

Commander gives simple orders:

```text
Hold
Attack
TakeCover
FlankLeft
FlankRight
Investigate
Regroup
Retreat
```

This is not a huge squad blackboard. It is intentionally explicit and small.

## 17. Diary

Add:

```text
OUTL_EntityDiary
```

Optional phrase set:

```text
OUTL_DiaryLineSet
```

Logs memory in RAM and writes files to:

```text
Application.persistentDataPath/OUTL_Diaries/
```

Events include spawn, patrol, heard sound, lost enemy, took damage, received order, took cover, attacked, died, dropped loot.

## 18. Surfaces and environment damage

Surface bridge:

```text
OUTL_SurfaceProfile
OUTL_SurfaceLibrary
OUTL_SurfaceProbe
OUTL_PlayerSurfaceMotorModifier
```

`OUTL_SurfaceProbe` reads RVP `GroundSurfaceInstance`, `TerrainSurface`, physic material fallback, or tag fallback.

For damaging volumes:

```text
OUTL_SurfaceVolume
```

Use for acid, fire, radiation, water hazards, etc.

## 19. Doors, buttons and logic

Typical setup:

```text
Door
  OUTL_EntityAdapter
  OUTL_Door
  OUTL_Interactable optional
  OUTL_AccessController optional

Button
  OUTL_EntityAdapter
  OUTL_Button or OUTL_Interactable
```

Logic gates:

```text
OUTL_LogicGate
OUTL_TouchTrigger
```

Keys are ordinary `OUTL_ItemDef` assets collected through `OUTL_ItemPickup`. `OUTL_AccessController` guards commands before receivers and evaluates source-aware requirements such as `HasItem`. Use `ConsumePolicy = Never` for Doom/Quake keys. Use `Lock` and `Unlock` commands for external control.

Working sample:

```text
OUT CORE Lite -> Advanced -> Samples -> Create Key Door Access Example
```

See `OUTL_ACCESS_KEYS_GUIDE.md`.

Use commands/effects/events instead of direct references whenever possible. This keeps the system data-driven instead of turning the inspector into a conspiracy board with strings.

## 20. Save system

`OUTL_SaveSystem` is owned by `OUTL_World`. Component state uses `OUTL_IComponentSaveParticipant`. Inventory restores items by stable definition id through `OUTL_DefDatabase`, with `KnownItems` retained as a compatibility fallback. Doors, buttons, access controllers, gates, relays, triggers, counters, output-once flags and path movers preserve their runtime state.

Console:

```text
outl.save
outl.load
```

## 21. Performance rules

Do:

```text
Use tick intervals.
Use RuntimeTier.
Use sector queries.
Use object pools.
Use despawn queue.
Use compact hot stats.
Keep AI schedules short.
Use NavMeshAgent repath interval.
```

Do not:

```text
Do not FindObjects every tick.
Do not Instantiate/Destroy in combat.
Do not run cover search every frame.
Do not put huge behavior trees into Lite.
Do not use string stats in hot loops when a compact id exists.
```

Pool API:

```csharp
using OutCore.pool;
GameObject instance = OUT.Instantiate(prefab, position, rotation);
GameObject child = OUT.Instantiate(rowPrefab, parent);
OUT.Destroy(instance);
OUT.Release(instance);
OUT.Prewarm(prefab, 32);
```

Pool rules:

```text
Use OUT.Instantiate/OUT.Destroy for repeatable gameplay objects.
Double-release is safe and logged; it must not push the same object into ObjectPool twice.
Unknown releases follow OUTL_PoolFallbackPolicy: DisableOnly, DestroyUnsafe, WarnOnly or ThrowInEditor.
Get stats through OUTL_PoolSystem.GetStatsSnapshot(), DumpStats() or OUT.TryGetPoolStats().
```

Validation tools:

```text
OUT CORE Lite -> Workbench -> Runtime Code Scanner
OUT CORE Lite -> Workbench -> Sector Integrity Validator
OUT CORE Lite -> Workbench -> Sector Integrity Window
```

Stimulus store:

```text
OUTL_StimulusBus.Emit still notifies listeners.
When OUTL_World exists, emitted stimuli also go into OUTL_StimulusStore.
Store queries are sector-indexed, radius/type/priority filtered and use caller-provided buffers.
AI actors can receive ambient store stimuli without scanning the whole scene.
```

Tick profile:

```text
Create -> OUT CORE Lite -> Core -> Tick Profile
```

Assign it to `OUTL_World.TickProfile` to drive logic, AI near/mid/far authoring defaults, quest, stimulus, chunk processing and egregore cadence budgets. If it is not assigned, the world keeps its existing serialized values.

Optional collective simulation:

```text
OUTL_EgregoreDef
OUTL_EgregoreComponent
OUTL_EgregoreDebugView
```

Egregores listen to existing events/stimuli and emit lightweight signals plus `OUTL_StimulusType.Egregore` ambient stimuli. They do not replace AI, quests, factions, spawning or the scheduler.

Create sample defs:

```text
OUT CORE Lite -> Egregore -> Create Sample Egregore Defs
```

Golden checks to run through `OUTL_GoldenTestRunner` now include pool reuse/double-release/rigidbody reset, StimulusStore query/decay, AI store delivery, sector move/unregister, egregore aggregate/signal and TickProfile interval application.

Recommended AI rates:

```text
Near = 0.1-0.2
Mid = 0.35-0.75
Far = 1.0-2.5
```

Console tuning:

```text
outl.tick ai 0.2
outl.entitytick <id> 0.4
outl.tickbudget 64
```

## 22. Debug checklist

If entity is invisible to systems:

```text
Has OUTL_EntityAdapter?
Has OUTL_World in scene?
Adapter registered? Use outl.stats.
Entity has runtime tags in OUTL_EntityDef?
RegisterInSectors enabled?
Was object disabled and re-enabled? Adapter now rebinds automatically.
```

If AI cannot see player:

```text
Player has OUTL_EntityAdapter.
Player def has Player tag.
Faction relation is hostile, or EnemyTags contains Player.
SightBlockMask does not include player layer as blocker.
ViewDistance is large enough.
```

If AI sees through walls:

```text
SightBlockMask must include wall/level geometry layers.
Walls need colliders.
```

If attacks do not damage:

```text
Target collider must be under OUTL_EntityAdapter.
Attack HitMask must include target collider layer.
Target Health must be > 0 and present in BaseStats.
```

If console pick returns self:

```text
Use pick, not pick_self. pick skips player by default.
Aim center screen at target.
Target collider must be child of OUTL_EntityAdapter.
```

## 23. Known limitations

Lite is not yet a full Bethesda/GoldSrc-style production editor. Missing or still basic:

```text
visual schedule debugger
cover occupancy visualization
robust save/restore for every subsystem
inventory UI
quest UI
complex squad tactics
full sensory blackboard
proper prefab wizard for complete ready NPCs
full production egregore content action authoring
```

The point is to keep it small enough to understand and extend. When a script starts trying to become a parliament, split it.
