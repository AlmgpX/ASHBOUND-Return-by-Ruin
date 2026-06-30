# OUT CORE level design and streaming contract

Purpose: define how 3D levels are authored, loaded, chunked and eventually streamed in OUT CORE.

This is written now because if level design starts before the runtime contract exists, the result is usually a heroic folder of beautiful meshes and an engine that can only stare at them like a goat at a server rack.

---

## Current runtime state

Already implemented:

```text
maps.json manifest
OUTMAP JSON level metadata
runtime map validation
visual mesh refs in OUTMAP
GLB model cache
scene renderer
missing GLB placeholder
box collision blockout
doors
triggers
Use/E interaction
quicksave snapshot
projectile snapshot
chunk focus diagnostics
surface ids on boxes/doors/mesh refs
surface registry
surface routed footsteps and projectile impact tags
```

Current runtime chain:

```text
data/maps/maps.json
  -> selected OUTMAP
    -> boxes / doors / triggers / mesh refs / surface ids
      -> OutmDemoMap runtime geometry
      -> OutmSceneRenderer draws GLB refs
      -> collision blockout handles gameplay
```

Current test map:

```text
OUT_RayMicro/data/maps/test_room.outmap.json
```

Current expected visual mesh path:

```text
OUT_RayMicro/data/meshes/rooms/test_room.glb
```

If the GLB is missing, the renderer draws a placeholder. This is intentional. Missing assets should look stupid, not crash the engine. Crashing is the laziest error message.

---

## Level design model

OUT CORE level design is split into two layers:

```text
Visual layer
  GLB / future streamed render chunks

Gameplay layer
  OUTMAP boxes / doors / triggers / pickups / actors / collision / surfaces
```

Do not bind gameplay truth to raw Blender object hierarchy. Blender is an authoring tool, not the constitution.

---

## Blockout-first workflow

Phase 1 level design:

```text
1. Build playable blockout with OUTMAP boxes.
2. Add playerStart.
3. Add doors and triggers.
4. Add surface ids.
5. Add placeholder GLB mesh refs.
6. Iterate gameplay scale and movement.
```

This phase is ugly and correct. A beautiful unplayable room is just a render portfolio with collision problems.

---

## Visual pass workflow

Phase 2 level design:

```text
1. Build visual room in Blender.
2. Export GLB.
3. Put GLB under data/meshes/...
4. Reference it from OUTMAP meshes[].
5. Keep gameplay collision separate until Jolt mesh collision is ready.
```

Example OUTMAP mesh ref:

```json
{
  "id": "visual.room.future",
  "path": "meshes/rooms/test_room.glb",
  "position": [0.0, 0.0, 0.0],
  "rotation": [0.0, 0.0, 0.0],
  "scale": [1.0, 1.0, 1.0],
  "collision": "none",
  "surface": "surface.stone"
}
```

---

## Surface contract

Surfaces are gameplay metadata.

Current default ids:

```text
surface.stone
surface.wood
surface.metal
surface.dirt
```

They control:

```text
footstep tag
projectile impact tag
future friction multiplier
future hazard damage
future decals / particles
```

Example box:

```json
{
  "id": "crate.left",
  "center": [-5.0, 0.5, 2.0],
  "size": [1.6, 1.0, 1.6],
  "solid": true,
  "surface": "surface.wood"
}
```

Example door:

```json
{
  "id": "door.main",
  "surface": "surface.wood"
}
```

---

## Chunking model

For FPS levels, default chunking is XZ-based:

```text
central chunk             active   full tick
3x3 ring around player    resident cheaper logic
outer tracked ring        sleeping / cheap background logic
outside tracked ring      eventually unload/snapshot
```

Current default:

```text
ActiveRadiusChunks = 0
ResidentRadiusChunks = 1
TrackedRadiusChunks = 4
UseVerticalChunking = false
```

Expected starting counts:

```text
active:   1
resident: 8
sleeping: 72
known:    81
```

This mirrors the old FPS idea better than a giant 3D cube of chunks. Vertical chunking is off because a Quake-like map usually does not need to tick empty air above the player just to satisfy a spreadsheet demon.

---

## Streaming target

Streaming is not fully implemented yet. Current chunks are runtime diagnostics and scheduling seeds.

Target load chain:

```text
region manifest
  -> chunk manifest
    -> visual mesh refs
    -> collision refs
    -> entity refs
    -> surface refs
    -> runtime spawn into stores
```

Future files:

```text
data/worlds/<world>/regions/r00.region.json
data/worlds/<world>/chunks/c_x_z.outchunk.json
data/meshes/worlds/<world>/chunks/c_x_z.glb
```

But do not split into chunk files too early. First we need:

```text
MapEntitySpawner
EntityChunkMembership
DoorStore
TriggerStore
PickupStore
ActorStore
Chunk dirty snapshots
```

---

## Blender authoring names

Future Blender exporter should read names like:

```text
OUT_SPAWN_PlayerStart
OUT_VIS_*             visual mesh
OUT_COLLIDER_*        collision primitive
OUT_TRIGGER_*         trigger volume
OUT_DOOR_*            door entity
OUT_PICKUP_*          pickup spawn
OUT_ACTOR_*           enemy/NPC spawn
OUT_SURFACE_*         optional surface marker/material hint
OUT_AUDIO_*           audio zone
OUT_FOG_*             fog zone
OUT_SECTOR_*          chunk/sector hint
```

Material or custom property should map to:

```text
surface.stone
surface.wood
surface.metal
surface.dirt
```

---

## What level designers can do right now

Right now:

```text
edit OUTMAP boxes/doors/triggers/surfaces by hand
add GLB path under meshes[]
use placeholder when GLB is missing
use F5/F9 to test persistent door/projectile/player state
use overlay to inspect chunk bubble
```

Not ready yet:

```text
Blender exporter
chunk file streaming
Jolt mesh collision baking
entity spawn stores
in-game transform editor save
standalone level editor
```

---

## Next code steps

```text
1. MapEntitySpawner
2. EntityChunkMembership
3. DoorStore / TriggerStore
4. PickupSystem with surface-aware pickup placement
5. Blender exporter seed
6. Jolt collision shape baking from OUTMAP/Blender colliders
7. chunk dirty snapshot save
```

The first real level-design milestone remains:

```text
Blender room GLB
+ OUTMAP collision blockout
+ surface ids
+ use door
+ pickup
+ enemy placeholder
+ quicksave/load
```

That is the first slice that deserves to be called a level instead of a cube arrangement with dreams.
