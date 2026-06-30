# OUT CORE: Jolt physics slice contract

Purpose: define how physics is now routed and what remains before native Jolt body creation is fully wired.

The short version: gameplay no longer talks directly to OUTMAP lists for collision and sensors. It talks to `IOutmCollisionWorld`. That is the important boundary. Without that boundary, adding Jolt is just gluing a rocket engine to a cupboard.

---

## Implemented in this slice

Added:

```text
OutmPhysicsScene
OutmPhysicsBody
OutmPhysicsTrigger
OutmPhysicsSceneBuilder
OutmJoltCollisionWorld
IOutmCollisionWorld.QuerySensor(...)
```

Runtime now creates:

```text
IOutmCollisionWorld collision = new OutmJoltCollisionWorld(mapDef, map)
```

Trigger flow now goes through physics:

```text
player position
  -> IOutmCollisionWorld.QuerySensor(...)
    -> TriggerStore lookup by sensor id
      -> UseSystem
        -> DoorStore
```

This means gameplay is no longer hardwired to `map.TryGetEnteredTrigger(...)`.

---

## Current collision source

The physics scene is built from OUTMAP:

```text
boxes[]   -> static collision bodies
doors[]   -> door collision bodies
triggers[] -> sensor volumes
meshes[] with collision != none -> mesh-proxy collision bodies
```

Doors sync each physics step:

```text
door open  -> physics door body inactive
door closed -> physics door body active
```

---

## Mesh collider support right now

Current supported mesh collision modes on `meshes[]`:

```text
"collision": "none"   no collision
"collision": "box"    box proxy from mesh position/scale
"collision": "mesh"   mesh-proxy body for the future native Jolt triangle/convex bake path
```

Important: `"mesh"` currently behaves as a proxy body, not as a real triangle soup yet. This is intentional and temporary. The contract exists so the native Jolt bake can be inserted without changing OUTMAP or gameplay systems.

Example:

```json
{
  "id": "visual.wall_block",
  "path": "meshes/rooms/wall_block.glb",
  "position": [6.0, 0.0, 0.0],
  "rotation": [0.0, 0.0, 0.0],
  "scale": [2.0, 4.0, 8.0],
  "collision": "box",
  "surface": "surface.stone"
}
```

Do not set the whole visual room GLB to `"collision": "mesh"` yet unless you want one giant proxy body. That would be a very efficient way to invent a wall named Everywhere.

---

## Native Jolt insertion point

Native Jolt belongs inside:

```text
OutmJoltCollisionWorld
```

The public gameplay contract must remain:

```text
IOutmCollisionWorld
```

The eventual native implementation should:

```text
1. initialize Jolt physics system
2. bake OUTMAP boxes into static box shapes
3. bake doors into kinematic/static toggled bodies
4. bake triggers into sensor bodies
5. bake GLB/Blender collision meshes into triangle mesh or convex shapes
6. step physics in fixed tick
7. expose raycast / overlap / character move through IOutmCollisionWorld
```

---

## Blender exporter target

Blender object names should map like this:

```text
OUT_COLLIDER_Box_*      -> OUTMAP box or mesh collision box
OUT_COLLIDER_Mesh_*     -> meshes[] collision mesh
OUT_TRIGGER_*           -> triggers[] sensor volume
OUT_DOOR_*              -> doors[] + trigger helper if requested
OUT_PICKUP_*            -> pickups[]
OUT_VIS_*               -> GLB visual mesh only
```

Custom properties should include:

```text
surface
collision
triggerKind
target
pickupKind
amount
armorTier
```

---

## What this slice does not pretend to do

It does not yet create true native Jolt triangle mesh bodies from GLB vertices.

That requires one of these next steps:

```text
A. Blender exporter writes collider vertices/indices into OUTMAP/outcollider file
B. runtime extracts mesh data from Raylib Model and bakes it into Jolt
C. separate offline collision bake step generates .outcollider.json or .outcollider.bin
```

Best path for OUT CORE:

```text
Blender exporter -> .glb visual + .outmap.json + .outcollider.bin
```

Runtime should load cooked data, not perform expensive authoring-time mesh surgery every launch. Amazing that computers prefer prepared data instead of improvisational panic.

---

## Next mandatory step

```text
Blender exporter seed
```

It should export:

```text
player start
visual GLB
box colliders
trigger volumes
doors
pickups
surface tags
```

Then native Jolt body creation can consume those collider definitions cleanly.
