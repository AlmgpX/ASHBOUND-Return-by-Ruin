# OUT RayMicro level format

OUTMAP is the current editable level format for OUT RayMicro.

It is deliberately boring JSON because the engine needs a spine before it needs a cathedral. Blender/GLB support comes after the runtime can load, save and reason about level metadata without recompiling C# like a cave ritual.

---

## Runtime path

```text
OUT_RayMicro/data/maps/test_room.outmap.json
```

`data/**` is copied into output by the project file.

---

## Current runtime chain

```text
.outmap.json
  -> OutmMapDef
    -> OutmMapLoader.BuildDemoMap
      -> OutmDemoMap runtime
        -> OutmDemoCollisionWorld
        -> renderer/debug draw
        -> triggers/doors
```

The old hardcoded `CreateQuakeRoom()` path now delegates to the map loader.

---

## Current sections

```json
{
  "id": "map.test_room",
  "displayName": "Test Room OUTMAP",
  "playerStart": [0.0, 1.2, 7.0],
  "boxes": [],
  "doors": [],
  "triggers": [],
  "meshes": []
}
```

### Boxes

Boxes are temporary brush-like primitives.

```json
{
  "id": "wall.left",
  "center": [-9.0, 2.0, 0.0],
  "size": [0.4, 4.0, 18.0],
  "color": [74, 84, 96, 255],
  "solid": true
}
```

Current use:

```text
visual debug geometry
simple collision
prototype blocking
```

### Doors

```json
{
  "id": "door.main",
  "center": [0.0, 2.0, -8.85],
  "size": [2.1, 4.0, 0.35],
  "color": [120, 62, 48, 255],
  "startsOpen": false
}
```

Doors are runtime state. Their open/closed state is not hardcoded anymore.

### Triggers

```json
{
  "id": "trigger.door.main",
  "kind": "door_toggle",
  "target": "door.main",
  "center": [0.0, 1.0, -7.2],
  "size": [2.2, 2.0, 0.8]
}
```

Current trigger kinds:

```text
door_toggle
```

### Mesh refs

```json
{
  "id": "visual.room.future",
  "path": "meshes/rooms/test_room.glb",
  "position": [0.0, 0.0, 0.0],
  "rotation": [0.0, 0.0, 0.0],
  "scale": [1.0, 1.0, 1.0],
  "collision": "none"
}
```

Mesh refs are declared now, but rendering GLB comes next.

---

## Editing right now

To move a wall, edit:

```text
data/maps/test_room.outmap.json
```

Then run:

```powershell
cd OUT_RayMicro
dotnet run -c Release
```

No C# rebuild logic changes required unless the schema changes. Tiny mercy, but we will accept it.

---

## Next steps

```text
1. Move trigger handling out of OutmApp into OutmTriggerSystem.
2. Add OutmModelCache.
3. Draw GLB meshes from map.meshes.
4. Split visual geometry and collision geometry.
5. Add Blender exporter that writes .glb + .outmap.json.
6. Add level manifest: data/maps/maps.json.
7. Add save snapshot storing current map id + runtime door/entity state.
```
