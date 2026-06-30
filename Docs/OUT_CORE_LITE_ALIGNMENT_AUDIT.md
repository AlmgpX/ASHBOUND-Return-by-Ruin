# OUT CORE Lite alignment audit

Purpose: keep OUT RayMicro aligned with the original OUT CORE / OUT CORE Lite canon while moving from Unity-side prototypes into a standalone runtime.

This document exists because engines do not die from one bad class. They die from ten convenient shortcuts that all say: just this once.

---

## Prime rule

The runtime truth must not be named after authoring buckets.

Wrong as hot runtime truth:

```text
boxes[]
doors[]
triggers[]
meshes[]
```

Valid as authoring input or gameplay components:

```text
BoxColliderDef[]
DoorRuntime[]
TriggerRuntime[]
MeshColliderRuntime[]
```

Hot physics truth:

```text
Body
Shape
BroadphaseProxy
ActivePair
ContactManifold
TriggerOverlap
DirtyBodies
DynamicBodies
KinematicBodies
```

Meaning lives in components. Collision lives in physics buffers.

```text
DoorRuntime -> BodyHandle -> Shape solid
TriggerRuntime -> BodyHandle -> Shape sensor
MeshColliderRuntime -> BodyHandle -> Shape triangleMesh/proxy
```

---

## Current OUT RayMicro status

Implemented in this pass:

```text
OutBuffer<T>
OutmPhysicsRuntime
OutmBody
OutmShape
OutmBroadphaseProxy
OutmActivePair
OutmContactManifold
OutmTriggerOverlap
OutmBodyHandle
OutmShapeHandle
OutmPhysicsSourceKind
```

The Jolt facade now builds a dense runtime from the authoring physics scene:

```text
OUTMAP authoring data
  -> OutmPhysicsScene build stage
    -> OutmPhysicsRuntime dense buffers
      -> IOutmCollisionWorld queries
```

The important point:

```text
IOutmCollisionWorld no longer needs to treat boxes/doors/triggers as the runtime model.
```

---

## OUT CORE / Lite comparison

Original OUT_CORE has simple table and scheduler ideas:

```text
OUT_State
OUT_Table
OUT_Scheduler
OUT_ITickSystem
```

Those are useful for semantic direction:

```text
state owns data
systems tick state
runtime objects have stable ids
mode/scope separates world/local simulation
```

But they are not directly suitable as hot physics storage, because the old Unity-side/lite code uses convenience structures like:

```text
Dictionary
List
LINQ
Unity GameObject adapters
```

That is fine for the old adapter layer. It is not fine as the core physics loop for big standalone scenes.

OUT RayMicro must preserve the idea, not the exact containers:

```text
OUT_Table idea        -> EntityStore / component stores / stable ids
OUT_Scheduler idea    -> fixed tick systems / tier scheduler
OUT_SectorRuntime idea -> chunk focus and logic tiers
OUT Save idea         -> explicit snapshots
Unity Adapter idea    -> presentation adapter only, never core truth
```

---

## Accepted architecture

Current acceptable layering:

```text
Authoring:
  OUTMAP JSON
  Blender exporter objects
  BoxColliderDef / DoorDef / TriggerDef / MeshRefDef / PickupDef

Build stage:
  OutmMapDef
  OutmDemoMap transitional geometry
  OutmPhysicsScene
  OutmMapEntitySpawner

Runtime truth:
  OutmWorld
  OutmEntityStore
  OutmTransformStore
  OutmPhysicsRuntime
  OutmDoorStore / OutmTriggerStore / OutmPickupStore as gameplay component stores
  OutmEntityChunkMembership

Systems:
  Input -> Command -> System -> State -> Event -> Presentation
```

---

## Current compromise to remove

`OutmDemoMap` still exists and still exposes:

```text
Boxes
Doors
Triggers
```

This is now transitional.

Allowed temporary uses:

```text
debug drawing
OUTMAP blockout visualization
surface lookup until physics hit material is fully wired
save/load bridge for old door data
```

Not allowed long-term:

```text
main collision truth
main trigger query truth
main physics state
large world scanning
```

---

## Physics buffer law

The physics runtime must use explicit buffers:

```text
OutBuffer<OutmBody> Bodies
OutBuffer<OutmShape> Shapes
OutBuffer<OutmBroadphaseProxy> Proxies
OutBuffer<OutmActivePair> ActivePairs
OutBuffer<OutmContactManifold> Contacts
OutBuffer<OutmTriggerOverlap> TriggerOverlaps
OutBuffer<int> DirtyBodies
OutBuffer<int> DynamicBodies
OutBuffer<int> KinematicBodies
```

No LINQ in physics hot paths.
No foreach in physics hot paths.
No scanning authoring buckets per gameplay query.
No object identity as physics identity.
No GameObject-style ownership in the standalone core.

---

## Broadphase path

Current broadphase is simple N^2 proxy testing over active proxies.

This is acceptable only as a first dense-buffer backend.

Upgrade path:

```text
1. current: dense proxy array, N^2 active proxy overlap
2. next: chunk-filtered proxy ranges
3. later: AabbStore SoA
4. later: sweep-and-prune or grid broadphase
5. final/native: Jolt broadphase with OUT handles preserved
```

Future SoA target:

```csharp
public sealed class AabbStore
{
    public float[] MinX, MinY, MinZ;
    public float[] MaxX, MaxY, MaxZ;
    public int[] BodyId;
    public int[] ShapeId;
    public int Count;
}
```

Do not jump to SoA before profiling. But do not design against it either.

---

## Door and trigger contract

Door is gameplay meaning:

```text
DoorRuntime
  EntityId
  BodyHandle
  Open
  SurfaceId
```

Physics sees:

```text
Body flags: Active | Static | Kinematic | Door
Shape: Box / future convex / future mesh
```

Trigger is gameplay meaning:

```text
TriggerRuntime
  EntityId
  BodyHandle
  Kind
  Target
```

Physics sees:

```text
Body flags: Active | Static | Sensor
Shape: Box / future convex sensor
```

The gameplay system resolves `Kind` and `Target`. Physics only answers overlap.

---

## Next required cleanup

High priority:

```text
1. DoorStore pending changed-door buffer
2. Physics sync only changed door body handles
3. remove per-step map.Doors scan from Jolt facade
4. TriggerStore direct BodyHandle lookup path
5. physics material/surface returned from body/shape hit
6. replace DemoMap collision surface lookup with physics hit material
```

Then:

```text
7. move PickupStore to OutBuffer or packed store
8. move DoorStore/TriggerStore from List to packed component buffers
9. add chunk-proxy ranges for broadphase
10. native Jolt shape creation behind OutmJoltCollisionWorld
```

---

## Red flags

If future code says:

```text
foreach box in map.Boxes every tick
foreach trigger in map.Triggers every tick
foreach door in map.Doors every tick
LINQ in fixed tick
new List in fixed tick
ToArray in fixed tick
```

then the code is probably wrong.

Not always. But probably. Suspicion is healthy. Blind trust is how software becomes an archaeology site.

---

## Current verdict

OUT RayMicro is now moving back toward OUT CORE Lite canon:

```text
stable ids
explicit stores
fixed tick
state mutation through systems
events to presentation
component meaning separated from physics truth
runtime buffers instead of authoring buckets
```

Still not finished:

```text
Door/Trigger/Pickup stores are not fully packed yet
physics broadphase is dense-buffer N^2, not chunk-filtered yet
native Jolt shape creation is not fully wired
OutmDemoMap still exists as transition/debug bridge
```

This is acceptable only if the next passes continue removing the transition layer instead of worshipping it.
