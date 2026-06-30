# OUT CORE Blender exporter workflow

This is the Blender -> OUT CORE bridge.

It exports:

```text
visual GLB
OUTMAP gameplay metadata
OUT material manifest
```

Exporter script:

```text
OUT_RayMicro/tools/blender/out_core_exporter.py
```

It exists so you can build geometry in Blender and stop hand-writing every wall, door, trigger and pickup in JSON like a haunted accountant.

---

## Install / run

In Blender:

```text
Edit > Preferences > Add-ons > Install...
```

Pick:

```text
OUT_RayMicro/tools/blender/out_core_exporter.py
```

Enable:

```text
OUT CORE Exporter
```

Then use either:

```text
File > Export > OUT CORE Level (.outmap + .glb + materials)
```

or the sidebar panel:

```text
3D Viewport > N panel > OUT CORE
```

---

## Scene settings

The exporter adds scene properties:

```text
Map Id
Display Name
Visual GLB Name
```

Example:

```text
Map Id: map.arcade_entry
Display Name: Arcade Entry
Visual GLB Name: arcade_entry
```

Output folder should be the OUT CORE data folder:

```text
OUT_RayMicro/data/
```

Exporter writes:

```text
OUT_RayMicro/data/maps/arcade_entry.outmap.json
OUT_RayMicro/data/meshes/rooms/arcade_entry.glb
OUT_RayMicro/data/materials/rooms/arcade_entry.materials.json
```

---

## Naming convention

Use object names:

```text
OUT_SPAWN_PlayerStart
OUT_VIS_*
OUT_COLLIDER_Box_*
OUT_COLLIDER_Mesh_*
OUT_DOOR_*
OUT_TRIGGER_*
OUT_PICKUP_*
```

Object names are metadata right now. Primitive, effective, and somehow still less annoying than inventing a bad editor too early.

---

## Player start

Create an Empty or any object:

```text
OUT_SPAWN_PlayerStart
```

Its location becomes:

```json
"playerStart": [x, y, z]
```

Current player start usually wants Y around:

```text
1.2
```

---

## Visual mesh

Objects named:

```text
OUT_VIS_*
```

are selected and exported into one GLB:

```text
meshes/rooms/<visual_name>.glb
```

The OUTMAP gets a visual mesh ref:

```json
{
  "id": "visual.arcade_entry",
  "path": "meshes/rooms/arcade_entry.glb",
  "materialManifest": "materials/rooms/arcade_entry.materials.json",
  "position": [0.0, 0.0, 0.0],
  "rotation": [0.0, 0.0, 0.0],
  "scale": [1.0, 1.0, 1.0],
  "collision": "none",
  "surface": "surface.stone"
}
```

Visual mesh is not collision unless you also define colliders.

---

## Blender materials

Blender materials on `OUT_VIS_*` objects are exported into the GLB by Blender.

OUT CORE also writes a sidecar manifest:

```text
materials/rooms/<visual_name>.materials.json
```

Each material entry contains:

```text
id
blenderName
surface
baseColor
metallic
roughness
baseColorTexture
normalTexture
emissiveTexture
```

To define gameplay/physics surface, add a custom property on the Blender material:

```text
surface = surface.stone
```

Useful values now:

```text
surface.stone
surface.wood
surface.metal
surface.dirt
surface.water
```

Example Blender material names:

```text
stone_floor
wood_trim
metal_panel
```

Example material custom props:

```text
stone_floor.surface = surface.stone
wood_trim.surface = surface.wood
metal_panel.surface = surface.metal
```

The GLB handles visual material data. The OUT material manifest gives the engine/editor stable surface ids and future material-routing data. Yes, two files. No, this is not decadence; it is separation of duties.

---

## Box colliders

Object name:

```text
OUT_COLLIDER_Box_floor_main
```

exports into OUTMAP `boxes[]`.

Uses:

```text
object location   -> center
object dimensions -> size
custom property surface -> surface id
custom property solid -> true/false
```

Example custom properties:

```text
surface = surface.stone
solid = true
```

Use these for actual playtesting now.

---

## Mesh colliders

Object name:

```text
OUT_COLLIDER_Mesh_wall_chunk
```

exports into OUTMAP `meshes[]` with:

```json
"collision": "mesh"
```

Current runtime treats this as a mesh-proxy collider seed. Real native Jolt triangle/convex baking is the next physics step.

Use box colliders for actual playtesting now. Mesh colliders are for the coming cooked collision path, not for building the whole cathedral out of hope and unpaid sleep.

---

## Doors

Object name:

```text
OUT_DOOR_main
```

exports into `doors[]`.

Custom properties:

```text
id = door.main
surface = surface.wood
startsOpen = false
```

If `id` is missing, exporter uses the object name:

```text
door.main
```

---

## Triggers

Object name:

```text
OUT_TRIGGER_door_main
```

exports into `triggers[]`.

Custom properties:

```text
id = trigger.door.main
kind = door_toggle
target = door.main
```

The player presses E while inside this trigger volume.

---

## Pickups

Object name:

```text
OUT_PICKUP_health_01
```

Custom properties:

```text
kind = Health
amount = 25
radius = 0.75
surface = surface.stone
```

Armor pickup:

```text
kind = Armor
amount = 100
armorTier = Yellow
radius = 0.75
surface = surface.metal
```

Mana pickup:

```text
kind = Mana
amount = 25
radius = 0.75
```

---

## Minimal first level recipe

For `Final Rider` first playable slice, create this in Blender:

```text
OUT_SPAWN_PlayerStart
OUT_VIS_RoomShell
OUT_COLLIDER_Box_floor
OUT_COLLIDER_Box_wall_left
OUT_COLLIDER_Box_wall_right
OUT_COLLIDER_Box_wall_back
OUT_DOOR_exit
OUT_TRIGGER_exit_use
OUT_PICKUP_health_01
OUT_PICKUP_armor_01
```

Set scene props:

```text
Map Id: map.arcade_entry
Display Name: Arcade Entry
Visual GLB Name: arcade_entry
```

Export to:

```text
OUT_RayMicro/data/
```

Then edit:

```text
OUT_RayMicro/data/maps/maps.json
```

and set:

```json
"defaultMap": "map.arcade_entry"
```

---

## Current runtime support

Works now:

```text
visual GLB rendering
Blender material export inside GLB
OUT material manifest sidecar
surface ids on boxes/doors/pickups/materials
box collision volumes
use triggers
doors
pickups
```

Next:

```text
native Jolt mesh collider cooking
material inspector in level panel
entity-lump based prop_static / func_button spawning
```
