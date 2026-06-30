# OUT CORE: how to make a level right now

This is the current practical level-design workflow before the Blender exporter and standalone editor exist.

It is not glamorous. It works. Glamour can wait in the corridor with the other expensive lies.

---

## Current level pipeline

Right now a level is:

```text
OUTMAP JSON gameplay blockout
+ optional GLB visual mesh
+ surface tags
+ doors/triggers/pickups
+ runtime entity stores
```

Current test level:

```text
OUT_RayMicro/data/maps/test_room.outmap.json
```

Current map manifest:

```text
OUT_RayMicro/data/maps/maps.json
```

Current optional visual room mesh:

```text
OUT_RayMicro/data/meshes/rooms/test_room.glb
```

If the GLB does not exist, OUT CORE draws a magenta placeholder. This is not a bug. This is the engine pointing at your missing asset with a tiny purple accusation.

---

## Step 1: create a new map file

Copy:

```text
OUT_RayMicro/data/maps/test_room.outmap.json
```

to for example:

```text
OUT_RayMicro/data/maps/arcade_entry.outmap.json
```

Change the id and display name:

```json
{
  "id": "map.arcade_entry",
  "displayName": "Arcade Entry Blockout"
}
```

---

## Step 2: add it to maps.json

Edit:

```text
OUT_RayMicro/data/maps/maps.json
```

Example:

```json
{
  "defaultMap": "map.arcade_entry",
  "maps": [
    {
      "id": "map.arcade_entry",
      "displayName": "Arcade Entry Blockout",
      "path": "maps/arcade_entry.outmap.json"
    },
    {
      "id": "map.test_room",
      "displayName": "Test Room OUTMAP",
      "path": "maps/test_room.outmap.json"
    }
  ]
}
```

Set `defaultMap` to whichever map you want to run.

---

## Step 3: block out the space with boxes

Boxes are gameplay collision and debug visuals.

Example floor:

```json
{
  "id": "floor.main",
  "center": [0.0, -0.1, 0.0],
  "size": [24.0, 0.2, 24.0],
  "color": [42, 43, 45, 255],
  "solid": true,
  "surface": "surface.stone"
}
```

Example wall:

```json
{
  "id": "wall.left",
  "center": [-12.0, 2.0, 0.0],
  "size": [0.4, 4.0, 24.0],
  "color": [74, 84, 96, 255],
  "solid": true,
  "surface": "surface.stone"
}
```

Rule:

```text
center = where the box is
size = full dimensions, not half-extents
```

Do not build every tiny detail as collision boxes. Blockout is for playable shape, not for recreating a cathedral from shoeboxes like a very determined raccoon.

---

## Step 4: set player start

```json
"playerStart": [0.0, 1.2, 7.0]
```

Use `Y = 1.2` for current player height assumptions.

---

## Step 5: add doors

Door example:

```json
{
  "id": "door.main",
  "center": [0.0, 2.0, -8.85],
  "size": [2.1, 4.0, 0.35],
  "color": [120, 62, 48, 255],
  "startsOpen": false,
  "surface": "surface.wood"
}
```

Doors now spawn into:

```text
DoorStore
EntityChunkMembership
OutmEntityStore
```

They are no longer just decorative entries in a list. Civilization advances by inches.

---

## Step 6: add triggers

Use trigger for a door:

```json
{
  "id": "trigger.door.main",
  "kind": "door_toggle",
  "target": "door.main",
  "center": [0.0, 1.0, -7.2],
  "size": [2.2, 2.0, 0.8]
}
```

Press `E` inside the trigger volume.

Triggers now spawn into:

```text
TriggerStore
EntityChunkMembership
OutmEntityStore
```

The trigger calls `UseSystem`, and `UseSystem` toggles `DoorStore`, then syncs to the current debug map collision.

---

## Step 7: add pickups

Health:

```json
{
  "id": "pickup.health.small.01",
  "kind": "Health",
  "position": [2.5, 0.45, 2.0],
  "radius": 0.75,
  "amount": 25,
  "surface": "surface.stone"
}
```

Armor:

```json
{
  "id": "pickup.armor.yellow.01",
  "kind": "Armor",
  "position": [-2.5, 0.45, 2.0],
  "radius": 0.75,
  "amount": 100,
  "armorTier": "Yellow",
  "surface": "surface.metal"
}
```

Mana:

```json
{
  "id": "pickup.mana.small.01",
  "kind": "Mana",
  "position": [0.0, 0.45, 4.5],
  "radius": 0.75,
  "amount": 25,
  "surface": "surface.stone"
}
```

Pickups now spawn into:

```text
PickupStore
EntityChunkMembership
OutmEntityStore
```

They save/load their collected state.

---

## Step 8: add a GLB visual mesh

In OUTMAP:

```json
{
  "id": "visual.arcade_entry",
  "path": "meshes/rooms/arcade_entry.glb",
  "position": [0.0, 0.0, 0.0],
  "rotation": [0.0, 0.0, 0.0],
  "scale": [1.0, 1.0, 1.0],
  "collision": "none",
  "surface": "surface.stone"
}
```

Put the actual file here:

```text
OUT_RayMicro/data/meshes/rooms/arcade_entry.glb
```

Current rule:

```text
GLB = visuals only
OUTMAP boxes = collision/gameplay
```

Do not rely on the GLB mesh for gameplay collision yet. Jolt collision baking comes later.

---

## Step 9: run and test

```powershell
git pull
cd OUT_RayMicro
dotnet clean
dotnet run -c Release
```

Controls:

```text
WASD move
Space jump
Ctrl/C crouch
E use
LMB fire
F1 overlay
F2 debug damage
F3 debug armor
F5 quicksave
F9 quickload
```

Test checklist:

```text
can walk around
cannot pass through walls
E toggles door
pickups disappear when collected
F5/F9 restores doors/pickups/projectiles/player state
surface footsteps appear in log
projectile impacts show surface tag
chunk diagnostics update around player
```

---

## What not to do yet

Do not start by making a huge Blender-only level and expecting the engine to solve reality.

Wrong:

```text
make giant pretty GLB
throw into engine
ask why collision, pickups, doors and triggers do not work
```

Right:

```text
blockout first
player movement second
doors/triggers third
pickups fourth
visual GLB fifth
```

---

## Current level-design target for Final Rider slice

For the first real slice, make:

```text
upper arcade service room / spawn point
small descent path
one locked/use door or breaker gate
one health pickup
one armor pickup
one visible GLB room shell
collision boxes matching the walkable space
```

Keep it small:

```text
one room
one corridor
one interaction
one reward
one exit frame
```

A tiny playable room is better than a giant unplayable temple to optimism.

---

## Next tooling step

Next engine/tooling step should be:

```text
Blender exporter seed
```

Exporter target:

```text
OUT_SPAWN_PlayerStart
OUT_VIS_*
OUT_COLLIDER_*
OUT_DOOR_*
OUT_TRIGGER_*
OUT_PICKUP_*
```

The exporter should write:

```text
.glb visual mesh
.outmap.json gameplay metadata
```

Until then, OUTMAP is hand-edited JSON. Primitive, yes. Effective, annoyingly also yes.
