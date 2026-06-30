# OUT RayMicro full engine iterations

Purpose: define what is missing before OUT RayMicro can honestly call itself a usable compact engine, not just a brave little window with delusions.

This roadmap aligns four sources of truth:

```text
1. Current OUT RayMicro codebase.
2. OUT CORE Lite philosophy from the Unity project.
3. id Tech / GoldSrc lessons: user commands, deterministic player movement, BSP/PVS/lightmaps, separated game logic, simple files.
4. Modern industry practice: data-oriented stores, stable ids, fixed ticks, hot/cold data split, no per-frame allocation, tools producing data.
```

---

## Current state snapshot

Already present:

```text
raylib host window
net9.0 runtime
JoltPhysicsSharp package reference
fixed tick accumulator
OutmInputFrame
OutmUserCommand
OutmCommandQueue
edge buffering across fixed ticks
IOutmCollisionWorld
OutmDemoCollisionWorld
manual spatial audio bridge
music stream playback
OUTMAP JSON level file
OutmMapDef / OutmMapLoader
data-driven weapon def for revolver
simple projectile pool
Quake-like armor absorption
player death lockout
Unicode HUD font loader
basic debug overlay
```

Current serious weaknesses:

```text
OutmApp still owns too much orchestration.
No real entity store.
No component/runtime stores.
No system scheduler.
No save snapshot.
No replay log.
No Jolt backend adapter yet.
No real capsule sweep / step solver / slope solver.
No GLB renderer/cache yet.
No material/surface system.
No enemy actor runtime.
No pickup runtime.
No inventory/ammo runtime.
No resource manifest.
No editor tool, only JSON editing.
No asset import pipeline actually generating OUTMAP from Blender.
No build/export pipeline.
```

---

## Lessons from OUT CORE Lite

The Unity OUTL controller is not just a controller. It shows the missing engine vocabulary.

Important design signals:

```text
input frame first, then apply input
movement parameters explicit and inspectable
GoldSrc unit scale exists
jump buffer and coyote time exist
crouch hull and uncrouch clearance exist
surface data exists
ladder logic exists
ledge grab exists
interaction/use has sticky grace
footstep distance and surface audio exist
fall damage exists
runtime motor state is written out
```

RayMicro must not copy Unity dependencies. It must copy the shape of the solution:

```text
input intent
explicit motor state
surface query
interaction query
movement system
feedback events
runtime stores
```

OUTL has many Unity-facing object references. RayMicro should replace those with stable ids and stores:

```text
Unity reference       -> OUT runtime equivalent
CharacterController  -> IOutmCollisionWorld.MoveCharacter
AudioSource          -> Audio event request
OUT_SurfaceData      -> SurfaceDef + SurfaceRuntimeId
EquipmentRuntime     -> InventoryStore + WeaponRuntimeStore
AttackDriver         -> WeaponSystem command/effect
AnimationBridge      -> Presentation event sink
```

---

## Lessons from id Tech / GoldSrc

### 1. Keep command input compact

Quake/GoldSrc style movement is built around compact user intent, not raw keyboard state sprayed across objects.

OUT RayMicro direction:

```text
OutmInputFrame -> OutmUserCommand -> fixed simulation tick
```

Next:

```text
selectedWeapon
impulse/use commands
command history ring
state checksum
client prediction buffer
```

### 2. Keep level data simple and compiled

id Tech did not rely on a live object soup. The runtime consumed compiled/structured level data.

OUT RayMicro direction:

```text
source .blend / editor objects
  -> .glb visual mesh chunks
  -> .outmap.json metadata
  -> runtime batches/stores
```

Do not make runtime parse Blender. Blender is a source tool, not a shipping level format.

### 3. Visibility and static lighting matter

Quake's key lesson is not nostalgia. It is that static world data can be preprocessed into cheap runtime data.

Future OUT version:

```text
chunk visibility sets
sector/portal hints
static mesh batches
baked lightmaps or vertex color lighting
fog/audio zones
```

We do not need a full BSP compiler first. We do need chunk/sector grouping and cheap culling before scenes grow.

### 4. Separate engine and game logic

QuakeC / later DLL game modules show the principle: engine provides services, game logic stays modular.

OUT equivalent:

```text
Runtime services:
  input
  physics
  rendering
  audio
  assets
  save files

Game systems:
  weapons
  pickups
  actors
  factions
  triggers
  quests/lore
```

### 5. Boring file formats beat magical editors early

JSON is not glamorous, but it lets the engine prove data flow before editor UI exists. Glamour is usually how codebases acquire disease.

---

## Industry principles to enforce

### Data-oriented hot paths

Rules:

```text
stable integer ids
array-backed stores
struct runtime data where practical
no per-frame allocations
no LINQ in hot loops
no string lookup in hot loops
hot/cold data split
compiled def ids, not string ids during simulation
```

Current problem:

```text
runtime still uses strings for map triggers and defs
```

Acceptable now, forbidden later. Add `DefId` and `StringIdRegistry` before content count grows.

### Systems, not object brains

Allowed:

```text
WeaponSystem.Update(store, commands, physics, events)
PickupSystem.Update(store, player, events)
ActorSystem.Update(actorStore, aiStore, physics, events)
```

Forbidden:

```text
Enemy.Update()
Door.Update()
Pickup.Update()
Random class directly mutates player
```

### Presentation reacts, it does not rule

Allowed:

```text
AudioSystem consumes events
HUD reads snapshot
Renderer draws extracted render state
```

Forbidden:

```text
AudioSystem changes gameplay
HUD mutates health
Renderer owns entity truth
```

### Determinism where it matters

The whole engine does not need to be bit-perfect deterministic today. The gameplay tick should still be structured as if replay and multiplayer matter.

Needed:

```text
fixed tick
command stream
stable random seeds
snapshot state
event journal
optional checksum
```

---

## Target architecture

```text
OUTM.Kernel
  EntityId / DefId / StringId
  WorldState
  EventQueue / EventJournal
  CommandQueue
  Scheduler
  SaveSnapshot

OUTM.Content
  ContentRegistry
  WeaponDef / ItemDef / ActorDef / SurfaceDef / MapDef
  Def compiler / validator

OUTM.Simulation
  PlayerMotorSystem
  WeaponSystem
  ProjectileSystem
  TriggerSystem
  PickupSystem
  DamageSystem
  ActorSystem
  AISystem
  FactionSystem

OUTM.Physics
  IOutmCollisionWorld
  OutmDemoCollisionWorld
  OutmJoltCollisionWorld
  collision shape baking

OUTM.Render
  ModelCache
  TextureCache
  MaterialDef
  SceneRenderer
  DebugDraw

OUTM.Audio
  AudioDef
  SurfaceAudioDef
  AudioEventSystem
  music/ambience stream
  future Steam Audio bridge

OUTM.Tools
  OUTMAP validator
  Blender exporter
  level manifest generator
  build/export pipeline
```

---

## Iteration plan

### I0: Green build and regression clamp

Goal:

```text
project builds cleanly
current test room runs
Use/E toggles door
jump is reliable
no old DoorOpen-style globals
```

Tasks:

```text
fix compile errors immediately
add tiny smoke checklist to README
avoid feature work while build is red
```

Definition of done:

```text
dotnet run -c Release works
F1 overlay works
E toggles door in use zone
LMB fires
Space jumps reliably
```

---

### I1: TriggerSystem and UseSystem extraction

Problem:

```text
OutmApp still contains trigger logic
```

Build:

```text
OutmTriggerSystem
OutmUseSystem or Use command path
OutmInteractionHit
```

Shape:

```text
UsePressed -> UseSystem.Query -> Command/Effect -> Event
```

Definition of done:

```text
OutmApp no longer knows door trigger rules
trigger kind resolves to system/effect
E is the only way to use door trigger unless trigger explicitly says auto_touch
```

---

### I2: Entity runtime stores

Problem:

```text
doors/triggers/projectiles/player are still special-case state
```

Build:

```text
OutmEntityStore
OutmTransformStore
OutmHealthStore
OutmDoorStore
OutmTriggerStore
OutmProjectileStore
OutmInventoryStore seed
```

Data shape:

```text
EntityId = index + generation
Store arrays indexed by entity index
TagMask / component bitsets later
```

Definition of done:

```text
player, door, trigger and projectile have entity ids
no runtime gameplay entity requires a C# object instance as truth
```

---

### I3: DefId and content compilation

Problem:

```text
strings are still used during runtime lookup
```

Build:

```text
OutmStringIdRegistry
DefId
compiled ContentRegistry
MapDef resolved to runtime ids
```

Definition of done:

```text
JSON uses strings for authoring
runtime uses integer ids
validator catches missing target ids before play
```

---

### I4: Real player motor milestone

Problem:

```text
current motor is camera-shaped, not actor-shaped
```

Build:

```text
OutmPlayerMotorState
OutmActorMotorSystem
standing/crouch hull
uncrouch overlap
step solver
slope solver
clip planes
fall damage
jump/land events
```

Borrow from OUTL:

```text
jump buffer
coyote time
ground probe
surface friction
fall damage thresholds
step distance footsteps
```

Definition of done:

```text
movement feels stable and Quake/HL-like
standing/crouching has real hull semantics
fall damage works
footsteps use distance, not timer-only
```

---

### I5: Jolt backend adapter

Problem:

```text
custom collision backend is only a demo adapter
```

Build:

```text
OutmJoltCollisionWorld
static box shapes from OUTMAP
capsule/character movement query
raycast
shape cast
overlap
sensors/triggers
```

Rule:

```text
gameplay never imports JoltPhysicsSharp directly
```

Definition of done:

```text
same level runs through Custom backend and Jolt backend by config
player collides through Jolt-backed static geometry
projectiles raycast/shapecast through physics interface
```

---

### I6: Scene renderer and GLB content

Problem:

```text
OUTMAP declares meshes but runtime does not draw GLB
```

Build:

```text
OutmModelCache
OutmTextureCache
OutmSceneRenderer
Map mesh refs
simple material defs
```

Definition of done:

```text
OUTMAP mesh refs load and draw GLB
debug boxes can be hidden while collision remains
renderer does not own gameplay truth
```

---

### I7: Blender exporter

Problem:

```text
manual JSON editing is acceptable for bootstrap, not production
```

Build:

```text
tools/blender/export_out_scene.py
source .blend naming rules
.glb export
.outmap.json metadata export
validator report
```

Naming convention:

```text
OUT_SPAWN_PlayerStart
OUT_COLLIDER_*
OUT_TRIGGER_*
OUT_DOOR_*
OUT_LIGHT_*
OUT_PROP_*
OUT_LORE_*
```

Definition of done:

```text
one Blender room exports GLB + OUTMAP
runtime loads it
no manual C# edits for level layout
```

---

### I8: Save / load snapshot

Problem:

```text
there is no persistent world truth
```

Build:

```text
OutmSaveSnapshot
player snapshot
door state snapshot
entity runtime snapshot
projectile snapshot optional
scheduler snapshot
content version/hash
```

Definition of done:

```text
press debug save
quit
load
player position, HP, door state and map id restore
```

---

### I9: Replay and debug checksum

Problem:

```text
we cannot prove simulation stability
```

Build:

```text
command history ring
event journal
state checksum
replay loader
```

Definition of done:

```text
record 30 seconds of movement/fire/use
replay from snapshot
checksums match or report first divergent tick
```

---

### I10: Items, pickups, inventory, ammo

Problem:

```text
armor pickup exists only as debug key
```

Build:

```text
ItemDef
PickupSystem
InventoryStore
AmmoStore
WeaponSlotStore
Pickup events
```

Definition of done:

```text
health kit entity
armor entity
ammo entity
weapon entity
all placed by OUTMAP and driven by defs
```

---

### I11: Actor/enemy placeholder

Problem:

```text
no gameplay target exists
```

Build:

```text
ActorDef
ActorStore
SimpleBrainState
Navigation seed
Enemy perception seed
Damageable target
```

Definition of done:

```text
one enemy spawns from actor def
can be shot
takes damage
dies/emits event
no per-enemy object Update brain
```

---

### I12: Surface/material/audio routing

Problem:

```text
all footsteps are stone and impacts are generic
```

Build:

```text
SurfaceDef
SurfaceId
SurfaceAudioDef
surface field on boxes/colliders
physics query returns surface id
```

Definition of done:

```text
stone/wood/metal/water footsteps
bullet impact sound by surface
basic occlusion raycast attenuates sound through walls
```

---

### I13: Chunk/sector structure

Problem:

```text
large levels will update/draw everything
```

Build:

```text
ChunkDef
SectorDef
sector id per trigger/box/mesh/entity
visibility groups
activation tiers
```

Definition of done:

```text
runtime can load one map as chunks
debug overlay shows current sector/chunk
only active chunk systems tick full rate
```

---

### I14: Build/export pipeline

Problem:

```text
no game packaging path exists
```

Build:

```text
OUT build manifest
asset copy validator
missing asset report
release folder exporter
```

Definition of done:

```text
one command creates standalone folder with exe, tools/native dlls, data and manifest
```

---

## Minimal vertical slice target

The next honest milestone is not “open world RPG”. It is this:

```text
one exported GLB room
one OUTMAP file
one player motor
one use door
one pickup
one enemy
one weapon def
one save/load
one replay proof
all through fixed tick and OUT stores
```

When that works, the engine has bones.

Before that, adding lore systems, factions, RPG UI or giant maps is just painting muscles on a skeleton that has not passed anatomy class.

---

## Priority order

Immediate next commits should be:

```text
1. I0 green build after OUTMAP refactor.
2. I1 TriggerSystem / UseSystem extraction.
3. I2 Entity runtime stores.
4. I4 real actor motor split from camera.
5. I5 Jolt backend.
6. I6 GLB renderer/cache.
7. I7 Blender exporter.
```

Do not start with Jolt if gameplay still has no stores. Do not start with editor UI if runtime cannot load/save stable data. Do not start with enemies before entities exist. Civilization may be doomed, but this repository does not need to imitate it.

---

## Source notes

Useful references and inspected material:

```text
OUT_RayMicro docs:
  Docs/OUT_CORE_RAYLIB_MICRO_ENGINE.md
  Docs/OUT_RAYMICRO_OUTCORE_LAYERING.md
  Docs/OUT_RAYMICRO_INPUT_SAVE_NETCODE_CONTRACT.md
  Docs/OUT_RAYMICRO_LEVEL_FORMAT.md

OUT CORE Lite inspected:
  NemesisRider_wip/Assets/OUT/OUT_Core/OUT_CORE_Lite/Player/OUTL_FPS_Controller.cs

Historical/industry references:
  id Software Quake source / Quake engine history
  GoldSrc/Source lineage
  QuakeC separation of game logic
  static lightmaps / BSP/PVS lessons
  ECS / data-oriented design performance and determinism literature
```
