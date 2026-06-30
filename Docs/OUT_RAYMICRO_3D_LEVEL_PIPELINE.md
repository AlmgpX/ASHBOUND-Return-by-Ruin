# OUT RayMicro 3D level pipeline and editor schedule

Purpose: define what the 3D level workflow becomes, in what order, and why we are not starting with a giant editor UI before the runtime can digest a room without choking on four color bytes like a Victorian aristocrat.

---

## Short answer

The level pipeline has three layers:

```text
1. Authoring source
   Blender / future editor scene

2. Runtime interchange
   .glb visuals + .outmap.json metadata

3. Runtime world
   entities + stores + systems + physics + renderer
```

The editor also has three stages:

```text
1. Text/JSON + Blender export
2. In-game debug editor
3. Standalone/editor-mode level tool
```

Do not build stage 3 before stages 1 and 2. That is how people create impressive tools that export unusable garbage. A proud tradition, but not ours.

---

## Current status

Already working:

```text
OUTMAP JSON file
map loading with fallback
boxes / doors / triggers
Use/E door interaction
fixed tick command loop
basic entity store seed
transform store seed
manual debug draw
```

Current level file:

```text
OUT_RayMicro/data/maps/test_room.outmap.json
```

Current loader:

```text
OUT_RayMicro/src/World/OutmMapDef.cs
OUT_RayMicro/src/World/OutmWorldGeometry.cs
```

Current limitation:

```text
mesh refs are declared but not rendered
collision is still box/blockout based
there is no Blender exporter yet
there is no level manifest yet
there is no editor save UI yet
```

---

## The target pipeline

### Source layout

```text
OUT_RayMicro/source/levels/test_room/test_room.blend
OUT_RayMicro/source/levels/test_room/export_settings.json
```

### Exported runtime layout

```text
OUT_RayMicro/data/maps/test_room.outmap.json
OUT_RayMicro/data/meshes/levels/test_room/test_room.glb
OUT_RayMicro/data/textures/levels/test_room/*
OUT_RayMicro/data/materials/levels/test_room/*.json
```

### Runtime load chain

```text
maps.json
  -> choose map id
    -> load .outmap.json
      -> load mesh refs
        -> create entity ids
          -> fill stores
            -> build physics backend
              -> render/debug/audio systems consume stores/events
```

---

## OUTMAP responsibility

OUTMAP is metadata, not a full 3D model format.

It owns:

```text
map id
map display name
player start
mesh refs
collision refs
triggers
doors
pickups
actors
surface/material ids
audio zones
fog/lighting hints
chunk/sector ids
script/effect ids
```

It does not own:

```text
raw mesh vertex data
texture pixels
animation curves
Blender editor-only junk
```

Raw visual geometry goes into:

```text
.glb / .gltf
```

---

## Blender naming convention

Objects in Blender should export predictably by name/prefix.

```text
OUT_SPAWN_PlayerStart
OUT_VIS_*             visual mesh, exported to GLB
OUT_COLLIDER_*        collision-only primitive/mesh
OUT_TRIGGER_*         trigger volume
OUT_DOOR_*            door runtime entity
OUT_PICKUP_*          pickup spawn
OUT_ACTOR_*           actor/enemy spawn
OUT_LIGHT_*           light hint / future baked data
OUT_AUDIO_*           audio zone / ambience
OUT_FOG_*             fog zone
OUT_SECTOR_*          sector/chunk marker
```

Examples:

```text
OUT_DOOR_Main
OUT_TRIGGER_MainDoorUse
OUT_COLLIDER_StoneWall_A
OUT_VIS_Hallway_A
OUT_PICKUP_GreenArmor_01
OUT_ACTOR_Zombie_01
```

---

## Iteration schedule

### P0: JSON blockout pipeline

Status:

```text
mostly done
```

What it gives:

```text
edit level by changing test_room.outmap.json
boxes/doors/triggers load without C# changes
fallback room if JSON is broken
```

Remaining:

```text
map validator
level manifest maps.json
door/trigger stores instead of raw map lists
```

Expected work:

```text
1-2 focused commits
```

Definition of done:

```text
broken outmap cannot crash startup
runtime can list/load map ids from data/maps/maps.json
validator prints missing target ids and malformed vectors/colors
```

---

### P1: Runtime GLB viewer inside OUTMAP

What it gives:

```text
OUTMAP mesh refs draw actual .glb models
blockout collision can remain separate
visual room can be made in Blender
```

Build:

```text
OutmModelCache
OutmSceneRenderer
OutmMeshRuntimeRef
simple material fallback
missing mesh placeholder
```

Files:

```text
src/Render/OutmModelCache.cs
src/Render/OutmSceneRenderer.cs
src/World/OutmSceneRuntime.cs
```

Expected work:

```text
2-4 focused commits
```

Definition of done:

```text
data/meshes/levels/test_room/test_room.glb loads from test_room.outmap.json
mesh renders at transform from OUTMAP
debug boxes can still be drawn separately
missing GLB does not crash the game
```

---

### P2: Blender manual export convention

What it gives:

```text
artist can build room in Blender
export GLB manually
write OUTMAP refs manually or semi-manually
```

Build:

```text
source/levels/test_room/README.md
Blender object naming guide
manual GLB export settings
axis/scale convention
```

Coordinate convention:

```text
1 Blender unit = 1 OUT meter
Y up in OUT runtime via raylib/System.Numerics
export must be verified with a test cube and player scale
```

Expected work:

```text
1 doc commit + one test GLB asset when available
```

Definition of done:

```text
one visible GLB room appears in runtime
player scale and floor alignment are sane
collision blockout still handles gameplay
```

---

### P3: Blender exporter script

What it gives:

```text
Blender produces .glb + .outmap.json
no hand-writing every trigger/door/collider
```

Build:

```text
tools/blender/export_out_level.py
export selected/all OUT_* objects
write mesh refs
write triggers
write doors
write collision boxes
write player start
write pickups/actors later
```

Command target:

```powershell
blender --background source/levels/test_room/test_room.blend --python tools/blender/export_out_level.py
```

Expected work:

```text
4-8 focused commits
```

Definition of done:

```text
one Blender file exports playable OUTMAP + GLB
runtime loads exported files
Use/E door still works
collision comes from exported OUT_COLLIDER objects
```

---

### P4: In-game debug editor

What it gives:

```text
move selected box/trigger/door in running game
save edited OUTMAP
reload map without restart later
```

Build:

```text
OutmEditorSelection
OutmGizmoMode
OutmMapWriter
OutmEditorCommandSystem
```

Minimal UI:

```text
TAB select object under crosshair
G move mode
R rotate mode later
S scale mode
Delete remove selected
Ctrl+S save OUTMAP
F5 reload OUTMAP
```

Expected work:

```text
6-12 focused commits
```

Definition of done:

```text
select a trigger/box/door
move it in game
save JSON
restart and see the changed position
```

This is not the final editor. This is the runtime scalpel, not the cathedral.

---

### P5: Entity/store-backed map runtime

What it gives:

```text
doors/triggers/pickups/actors become entity ids
save/load can persist runtime state
editor can operate on common entity ids
```

Build:

```text
DoorStore
TriggerStore
PickupStore
ActorSpawnStore
MapEntitySpawner
```

Expected work:

```text
4-8 focused commits
```

Definition of done:

```text
OUTMAP load creates entity ids for player, doors, triggers, pickups
TriggerSystem operates on store entities
save snapshot can store door open/closed state by entity/map id
```

---

### P6: Jolt collision from level data

What it gives:

```text
real physics backend for level collision
raycasts / overlaps / shape casts from Jolt
future Steam Audio occlusion and proper traces
```

Build:

```text
OutmJoltCollisionWorld
collision shape baker from OUTMAP
box colliders first
mesh colliders later
raycast/overlap/character move adapter
```

Expected work:

```text
6-12 focused commits
```

Definition of done:

```text
same OUTMAP can run with Custom backend or Jolt backend
player movement uses Jolt static collision
projectiles use physics raycast/shape query
Use traces can query physics instead of only trigger boxes
```

---

### P7: Standalone editor mode

What it gives:

```text
real editor workflow
viewport + hierarchy + property panel + asset browser
```

Do not start this before P1-P5. Otherwise it will be a beautiful lie wearing a toolbar.

Build options:

```text
raylib immediate-mode editor
ImGui.NET if we accept dependency
separate OUT_RayEditor executable later
```

Expected work:

```text
large milestone, not a weekend patch
```

Definition of done:

```text
open map
inspect hierarchy
select entity
edit transform/properties
save OUTMAP
play from current camera/player start
```

---

## What happens when

Immediate next phase:

```text
P0 finish: maps.json + validator
P1 start: ModelCache + SceneRenderer + GLB refs
```

After that:

```text
P2: manual Blender -> GLB room visible
P3: exporter script
P4: in-game editor save/reload
P5: map entities/stores
P6: Jolt level collision
P7: standalone/editor mode
```

Practical order for commits:

```text
1. Add maps.json manifest.
2. Add OUTMAP validator.
3. Add Render/OutmModelCache.
4. Add Render/OutmSceneRenderer.
5. Draw mesh refs from OUTMAP.
6. Add missing-mesh placeholder.
7. Add source/levels/test_room authoring notes.
8. Add Blender exporter script seed.
9. Add MapWriter for saving edited OUTMAP.
10. Add in-game selection/gizmo seed.
```

---

## First playable 3D authoring milestone

The first real milestone is this:

```text
Blender room -> export GLB + OUTMAP -> run game -> walk around -> press E on door -> save/load later
```

Minimum assets:

```text
one room GLB
one collision blockout
one player start
one use door
one light/fog hint
one pickup spawn later
```

Minimum runtime:

```text
GLB draw
collision boxes
Use/E triggers
data-driven weapon
fixed tick
entity ids
```

That is the moment when it stops being a tech demo with boxes and becomes a level pipeline.

---

## Editor philosophy

The editor should edit data the runtime already understands.

Wrong:

```text
build editor-only objects
then invent export later
then discover runtime cannot load half of it
then cry into coffee
```

Right:

```text
runtime format first
validator second
manual authoring third
exporter fourth
editor fifth
```

The editor is a UI over OUTMAP/entities/stores. It is not the source of truth.

---

## Near-term promise

Next engineering slice should deliver this:

```text
maps.json
OUTMAP validation
GLB model cache
GLB scene renderer
first visible mesh ref path
```

After that, Blender becomes useful immediately instead of being a decorative icon on the desktop, where tools go to die.
