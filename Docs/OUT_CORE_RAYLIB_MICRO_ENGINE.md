# OUT CORE Raylib Micro Engine

Canonical intent document for the experimental raylib-based OUT CORE micro engine inside `ASHBOUND-Return-by-Ruin`.

This is not Unity OUT CORE Lite. This is a standalone high-performance, radically simple gameplay engine/tooling direction that borrows the OUT CORE Lite grammar and applies it to a tiny raylib runtime.

The goal is not to recreate Unity. The goal is to build a compact action-shooter RPG construction kit that can produce large, simple, fast 3D games with Quake 1-level immediacy, simple open-world streaming, dynamic lore, triggers, 3D meshes, lights, physics, weapons and optionally Duke Nukem 3D-style billboards/sprites.

---

## 1. Product goal

Build a small engine/editor that can create:

```text
large simple action-shooter RPG
first-person combat
3D mesh-based world
Quake 1-like level feel at first milestone
later open-world chunk streaming
dynamic lore / factions / world memory
simple but strong editor workflow
standalone builds
```

Hard visual preference:

```text
true 3D mesh rendering
no Build-engine perspective distortion
no mandatory sector warping tricks
support sprites/billboards, but do not depend on them
3D models are first-class because they are easier to author for this project
```

Target feeling:

```text
Quake 1 readability
Half-Life-style triggers and authored logic
Duke Nukem 3D-like interactive attitude
OUT CORE data-driven logic grammar
old-school speed, modern sanity
```

---

## 2. Non-goals

Do not build these first:

```text
full Unreal-style editor
visual scripting graph
full Quake BSP compiler
CSG brush boolean system
PBR material authoring suite
skeletal animation authoring package
network multiplayer
terrain mega-system
shader graph
Unity clone
```

The first useful target is a compact engine where a designer can build a small 3D shooter level, place entities, set triggers, press Play, and export a standalone game folder.

---

## 3. Architecture principle

The engine has three layers:

```text
OUTM.Kernel      pure gameplay grammar and deterministic runtime
OUTM.Runtime     raylib host, renderer, physics, audio, input, assets
OUTM.Editor      level editor, inspector, validators, build/export tools
```

Kernel must not know raylib. Runtime bridges raylib to kernel. Editor produces data, not hidden MonoBehaviour-style state.

The smallest grammar:

```text
Entity      addressable runtime object
Def         immutable authoring data
Runtime     mutable state
Command     requested action
Event       observed fact
Effect      reusable state mutation
Condition   reusable predicate
Rule        condition + effect package
Scheduler   deterministic timing
Registry    address space
Pool        lifetime boundary
Chunk       streaming unit
Sector      visibility/collision/logic grouping
```

The core rule:

```text
Command -> Validate -> Effect -> Event -> Scheduler/Rule reaction
```

No hidden object `Update()` brains. No direct cross-object spaghetti. The world changes because a command or scheduled event produced a reasoned effect. Apparently this is too much to ask from most game code, so write it down like a warning label on industrial poison.

---

## 4. Performance principles

Performance target is extremely high for simple scenes. Prefer simple data and predictable memory.

Rules:

```text
fixed-step simulation
array-backed stores
stable integer ids
no per-frame allocations
no LINQ in hot paths
no reflection in runtime hot paths
no string lookup in hot paths
no virtual call forests in hot paths
object pools for gameplay entities, projectiles, particles and transient VFX
bitsets for tags/component membership
chunk/sector visibility before entity update
spatial hash or loose grid for broadphase
compiled rule tables, not interpreted ScriptableObject soup
```

Primary data shapes:

```text
EntityId = int index + generation
DefId    = ushort/int stable id
TagMask  = bitset
ChunkId  = int
SectorId = int
```

Hot loop update order:

```text
Input sample
Command enqueue
Fixed scheduler tick
Physics broadphase/narrowphase
Gameplay systems
Projectile systems
AI tier tick
Event flush
Audio/VFX requests
Render extraction
Render
Editor overlay
```

---

## 5. Renderer direction

Use raylib for window, input, audio, low-level drawing and mesh rendering.

Rendering model:

```text
true 3D camera
mesh world geometry
static mesh batching per material/chunk
optional billboard sprites
simple dynamic lights first
baked lightmaps later
fog volumes / sector fog
debug draw always available
```

First renderer milestone:

```text
OBJ/glTF static mesh import
texture/material assignment
first-person camera
static world mesh draw
billboard sprite draw
weapon viewmodel draw
basic dynamic point lights or fake vertex lighting
skybox/color fog
```

Avoid Build-style projection distortion. If sector tools are used, they are authoring/streaming helpers, not the renderer's prison.

Recommended geometry pipeline:

```text
Editor map data
  -> chunks
    -> render batches
      -> raylib Mesh/Model
```

World geometry may be authored as:

```text
imported mesh chunks
simple brush-like boxes/walls compiled to mesh
handmade OBJ/glTF rooms
entity props
```

Do not require all world geometry to be sector walls. Quake 1-level target means real 3D spaces with verticality.

---

## 6. World representation

The world is chunked.

```text
World
  Chunk[]
    StaticMeshBatch[]
    ColliderBatch[]
    Light[]
    EntitySpawn[]
    Trigger[]
    LoreRegion[]
```

Chunks are the open-world boundary. Sectors are optional local grouping inside chunks.

Chunk responsibilities:

```text
load/unload assets
visibility/culling boundary
physics broadphase boundary
AI activation tier
lore/ambient state scope
save/load scope
```

Sector responsibilities:

```text
local trigger grouping
room/area name
ambient audio/fog/light modifiers
simple portal/occlusion hint
Quake-style indoor readability
```

This allows:

```text
small Quake-like levels now
larger connected worlds later
open-world streaming without rewriting the core
```

---

## 7. Physics direction

Start with custom simple physics. Do not pull a giant physics dependency until the engine proves it needs one.

MVP physics:

```text
capsule or cylinder FPS controller
AABB broadphase
triangle mesh collision for static world
raycasts for weapons/use/visibility
sphere/capsule sweeps for player and projectiles
simple rigid projectiles
gravity
sliding along surfaces
steps / slopes
triggers
```

Entity collision types:

```text
StaticWorld
Trigger
ActorCapsule
Projectile
Pickup
DoorMover
LoreZone
```

Quake 1-level requirement:

```text
fast FPS movement
solid stairs/slopes
doors/lifts/platforms
projectiles
reliable raycasts
simple enemy collision
```

Do not implement full rigidbody chaos first. The player and projectiles need to feel good before crates learn philosophy.

---

## 8. Gameplay grammar

Gameplay is data-driven.

Common command types:

```text
Use
Fire
AltFire
Reload
Equip
Damage
Heal
Open
Close
Toggle
MoveTo
Teleport
Spawn
Despawn
PlaySound
SetFact
AddFact
EmitLore
```

Common events:

```text
Spawned
Despawned
Used
Fired
Hit
Damaged
Killed
EnteredTrigger
ExitedTrigger
DoorOpened
DoorClosed
ItemPicked
QuestFactChanged
LoreHeard
FactionStateChanged
ChunkLoaded
ChunkUnloaded
```

Common systems:

```text
WeaponSystem
ProjectileSystem
DamageSystem
DoorSystem
TriggerSystem
PickupSystem
LoreSystem
FactionSystem
AISystem
SaveSystem
```

Every game-specific feature must try to use this grammar before inventing a new one. Humans keep inventing new buses because pain apparently builds character. Do not.

---

## 9. Weapons and combat target

First combat milestone:

```text
revolver projectile or hitscan mode
shotgun or simple rifle
melee attack
grenade projectile
enemy projectile
basic damage / armor / health
hit reactions
pickup ammo / health
```

Weapon runtime shape:

```text
WeaponDef
  PrimaryAttackDef
  SecondaryAttackDef
  AmmoDef
  Cooldown
  Reload
  ViewModel
  WorldModel
  Audio
  MuzzleFx
```

Attack modes:

```text
Hitscan
Projectile
MeleeArc
Explosion
UseRay
```

Projectile fields:

```text
speed
gravity
lifetime
damage
radius
maxBounces
bounceDamping
owner
faction
trailFx
impactFx
```

---

## 10. Sprites and billboards

Sprites are supported, but they are not the foundation.

Sprite use cases:

```text
Duke Nukem 3D-style pickups
decorations
simple enemies
particles
muzzle flashes
explosions
signs
cheap distant impostors
```

3D models remain first-class:

```text
weapons
characters
props
world modules
doors
machines
monsters if easier to author
```

Billboard renderer requirements:

```text
camera-facing billboard
axis-locked billboard
animated sprite sheet
depth sorting bucket
optional fullbright
optional soft collision capsule
```

---

## 11. Lighting

First milestone:

```text
ambient light
per-object tint
simple point/spot fake lights
sector/chunk light color
fullbright material flag
```

Second milestone:

```text
baked lightmaps for static geometry
lightmap UV import/generation tool
vertex color lighting fallback
simple dynamic muzzle/impact flashes
```

For Quake 1-like look, lightmaps or vertex-lit mesh chunks are enough. Do not start with physically-based lighting. The point is style, readability and speed, not simulating showroom tiles.

---

## 12. Dynamic lore / open-world memory

Dynamic lore is a gameplay system, not a dialogue gimmick.

Lore model:

```text
LoreFact
  id
  subject entity/faction/region
  predicate
  value
  confidence
  visibility
  timestamp
```

Examples:

```text
FactionA hates Player
Village heard about RuinGate
ElfCompanion trusts Player 0.64
Region AshField corruption 0.82
BossDead true
GateOpened true
```

Lore affects:

```text
NPC dialogue selection
ambient bark pools
faction hostility
quest availability
companion behavior
world events
spawn tables
rumor propagation
```

Runtime flow:

```text
Event -> LoreSystem -> LoreFactDelta -> Rule reactions -> visible world change
```

Do not make dynamic lore an LLM dependency. It must work offline and deterministically first. Later an LLM can author text around facts, not become the source of truth, because outsourcing reality to a text generator is how you get a haunted spreadsheet.

---

## 13. Editor goal

Editor must let the user build a small 3D action level without hand-editing JSON.

MVP editor modes:

```text
Play/Edit toggle
Scene viewport
Transform gizmo lite
Asset browser
Entity palette
Inspector
Trigger/link editor
Chunk list
Console/log
Build/export button
```

Level authoring:

```text
import mesh chunk
place prop
place player start
place enemy
place pickup
place trigger
link trigger to command
assign material/light/fog
press Play
save map
export standalone
```

Do not require sprites. Do not require sector-only authoring. Let imported 3D mesh chunks be the fastest path.

---

## 14. File formats

Prefer simple, inspectable data.

```text
.outmap.json      map structure
.outentity.json   entity definitions
.outweapon.json   weapons
.outlore.json     lore/faction definitions
.obj/.gltf        meshes
.png/.jpg         textures
.wav/.ogg         audio
```

Compiled cache:

```text
.outchunk.bin     optional packed runtime chunk
.outbatch.bin     optional render batch cache
```

JSON is for authoring. Binary cache is for performance later. Do not optimize file formats before the editor can save a door without crying.

---

## 15. Build/export

Export structure:

```text
GameName/
  Game.exe
  raylib.dll
  data/
    maps/
    meshes/
    textures/
    audio/
    defs/
    saves/
```

Export steps:

```text
validate map
copy assets
compile chunks
write manifest
write config
copy runtime executable
run smoke test
```

---

## 16. Line-count target

Expected code size:

```text
vertical slice:        4k-6k LOC
usable Quake-like MVP: 10k-15k LOC
comfortable editor:    18k-25k LOC
open-world/lore layer: 25k-40k LOC
```

The target is not the fewest lines at any cost. The target is few concepts, hot data, and short boring systems.

Boring systems are good. Exciting systems become archaeology.

---

## 17. Milestones

### M0: Raylib host

```text
window
camera
input
delta/fixed time
debug console
asset folder lookup
```

### M1: Quake-room renderer

```text
load one mesh room
draw textured mesh
FPS controller
static collision
simple light/fog
```

### M2: Gameplay kernel

```text
EntityId
Registry
CommandQueue
EventQueue
Scheduler
Pool
DefDatabase
```

### M3: Shooter slice

```text
player start
revolver
projectile/hitscan
damage
enemy placeholder
pickup
trigger door
```

### M4: Editor MVP

```text
viewport
asset browser
entity placement
inspector
save/load map
Play/Edit toggle
```

### M5: Quake-like level

```text
multiple rooms
verticality
lifts/doors
lights
props
enemies
weapons
basic save
standalone export
```

### M6: Open-world preparation

```text
chunk loading
chunk culling
streaming asset manifest
AI tiering
lore regions
save chunks
```

### M7: Dynamic lore

```text
LoreFact store
rumor events
faction memory
world reactions
companion/quest hooks
```

---

## 18. Required coding style

```text
small files
small structs
flat arrays
explicit ownership
no global mutable soup
no manager bloom
no hidden update brains
no allocation-heavy hot paths
no string ids in hot paths
no direct entity-to-entity gameplay calls
```

Acceptable runtime pattern:

```text
CommandQueue.Enqueue(FireCommand)
WeaponSystem consumes command
ProjectileSystem spawns pooled projectile
EventBus emits Fired
AudioSystem plays queued sound
Renderer draws extracted state
```

Unacceptable runtime pattern:

```text
Enemy object calls Player.TakeDamage directly
Door object polls every frame for player name
Projectile allocates new explosion object on impact
Trigger runs arbitrary string script in hot path
```

---

## 19. First implementation prompt for future assistant/Codex

Before writing code, read this file and produce a one-page implementation plan.

Then create:

```text
OUT_RayMicro/
  OUT_RayMicro.csproj
  Program.cs
  src/Core/EntityId.cs
  src/Core/OutmWorld.cs
  src/Core/OutmScheduler.cs
  src/Core/OutmEvents.cs
  src/Runtime/OutmRayHost.cs
  src/Runtime/OutmCamera.cs
  src/Runtime/OutmRenderer.cs
  src/Runtime/OutmFpsMotor.cs
  src/World/OutmMapDef.cs
  src/World/OutmMapLoader.cs
  src/World/OutmStaticCollision.cs
  src/Gameplay/OutmWeaponSystem.cs
  src/Editor/OutmEditorShell.cs
```

First slice must show:

```text
one textured 3D room
free FPS movement with collision
one light/fog style
one projectile or hitscan weapon
one trigger opening one door
one debug console line per command/event
```

No open-world system before M5 works. No dynamic lore before basic command/event gameplay works. The cart goes after the horse, not strapped to its skull like usual software planning.
