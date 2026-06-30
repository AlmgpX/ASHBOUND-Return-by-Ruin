# OUT CORE Micro: level model, collision and material status

This document answers the practical level-design question: when can a real level model be loaded with collision and physical materials?

Short answer: visual GLB loading already exists; authoring collision volumes already export from Blender; physics surfaces now come from physics shapes. True native triangle mesh collision is the next backend step, not gameplay work.

Because apparently even engines need a confession booth.

---

## What works now

### Visual level model

OUTMAP supports visual GLB refs:

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

Runtime loads this through:

```text
OutmModelCache
OutmSceneRenderer
```

If the GLB is missing, renderer draws a placeholder. It is not subtle. It is not meant to be.

---

## What works for collision now

Blender exporter objects:

```text
OUT_COLLIDER_Box_*
OUT_DOOR_*
OUT_TRIGGER_*
OUT_PICKUP_*
```

Export into OUTMAP metadata:

```text
boxes[]
doors[]
triggers[]
pickups[]
```

Then the engine builds:

```text
OutmPhysicsScene
  -> OutmPhysicsRuntime
    -> Body
    -> Shape
    -> BroadphaseProxy
```

So gameplay collision is no longer supposed to think in `boxes[]`, `doors[]`, `triggers[]`.

Those are authoring/component data only.

---

## Physical materials now

Physics shapes carry:

```text
SurfaceId
```

Physics ray hits now carry:

```text
OutmRayHit.SurfaceId
```

Used by:

```text
projectile impacts
footsteps
```

So a wall marked:

```text
surface.metal
```

can produce metal impact/footstep routing, while a wood crate can route to wood.

This is now a physics/material contract, not a DemoMap guess.

---

## Broadphase now

Current broadphase is not full N² anymore.

Current path:

```text
Proxy AABB
  -> spatial hash cell
    -> neighboring 27 cells
      -> candidate pairs
        -> contact / sensor overlap
```

Still not final, but it is no longer scanning every proxy against every proxy like a caffeinated intern with no spatial awareness.

---

## Native Jolt status

Native Jolt is now hidden behind:

```text
IOutmPhysicsBackendBridge
OutmManagedPhysicsBridge
OutmNativeJoltBridge
OutmCorePhysicsWorld
```

Gameplay sees:

```text
IOutmCollisionWorld
```

Not Jolt directly.

That is the correct boundary.

Current default bridge:

```text
ManagedFallback
```

Next bridge:

```text
NativeJolt
```

Native Jolt body creation must happen inside:

```text
OutmNativeJoltBridge.BuildFromRuntime(...)
```

not inside gameplay, weapon code, door code, or level editor code.

---

## What does not work yet

True GLB triangle mesh collision is not active yet.

This means:

```text
OUT_VIS_* GLB visual model loads visually
OUT_COLLIDER_Box_* drives physical collision now
OUT_COLLIDER_Mesh_* is the future cooked/native mesh path
```

Do not expect the visual mesh itself to become collision just because it looks solid. Reality already makes that mistake often enough.

---

## Correct next step

The next physics/level-design layer is:

```text
Blender exporter writes cooked collider metadata
OutmNativeJoltBridge reads Body/Shape data
Jolt creates native box/sensor/mesh bodies
Physics hit returns native body -> OUT shape -> surface id
```

Minimum practical target:

```text
1 visual GLB room
1 set of OUT_COLLIDER_Box_* collision volumes
1 OUT_DOOR_* with sensor trigger
1 surface.stone floor
1 surface.wood crate
1 surface.metal block
projectiles and footsteps report different impact tags
```

Then:

```text
OUT_COLLIDER_Mesh_* -> cooked mesh collision
```

---

## Rename note

`OUT_RayMicro` can be renamed to:

```text
OUT_CORE_Micro
```

Recommended rename order:

```text
1. finish green build after physics/store changes
2. rename folder
3. rename csproj
4. rename assembly/root namespace
5. update docs/scripts/blender exporter paths
6. commit separately
```

Do not combine the rename with physics work. Combining rename refactors with deep engine changes is how humans manufacture haunted repositories.
