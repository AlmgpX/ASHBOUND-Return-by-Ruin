# OUT CORE standalone porting rules

Purpose: define how the original Unity OUT CORE Lite code should be moved into the standalone OUT CORE runtime.

The engine name is:

```text
OUT CORE
```

The current C# project folder is still:

```text
OUT_RayMicro
```

That folder is a bootstrap host name, not the product name. Rename namespaces only when the core is stable enough to survive the surgery. Large namespace renames during active architecture work are how repositories discover religion and then die.

---

## Can Unity scripts be copied as-is?

Mostly no.

Unity scripts depend on Unity runtime concepts:

```text
MonoBehaviour
GameObject
Transform
CharacterController
LayerMask
RaycastHit
Collider
AudioSource
AudioClip
Camera
ScriptableObject
Prefab
Unity Editor menus
Unity serialization
```

OUT CORE standalone does not have those concepts as truth. It has its own runtime grammar:

```text
EntityId
stable authoring ids
stores
commands
events
defs
systems
physics adapter
render adapter
audio adapter
save snapshots
```

So the Unity code cannot be pasted in and expected to compile unless we drag Unity behind it like a corpse cart. That defeats the point.

---

## What can be ported almost directly?

The architecture contracts.

Original Unity concept:

```text
IOutUsable
OUT_UseRequest
OUT_UseResult
IOutDamageable
OUT_DamageContext
IOutSaveAdapter
IOutScheduleAgent
IOutTaskExecutor
IOutResourceOwner
IOutFactionMember
IOutPerceptionTarget
IOutRelationshipResolver
OUT_SurfaceData
OUTL_ActorInputFrame
OUTL_ApplyInput
OUTL motor state
```

Standalone OUT CORE equivalent:

```text
IOutmUsable or store-based OutmUseSystem
OutmUseRequest
OutmUseResult
OutmDamageContext
OutmSaveSnapshot
OutmScheduleHandle
OutmTaskHandle
OutmResourceStore
OutmFactionStore
OutmPerceptionStore
OutmSurfaceDef
OutmUserCommand
OutmActorMotorState
```

Port the shape, not the Unity plumbing.

---

## Translation table

```text
Unity / OUTL                         -> Standalone OUT CORE

GameObject                           -> EntityId + stable authoring id
Transform                            -> OutmTransformStore
MonoBehaviour.Update                 -> explicit system update
CharacterController                  -> IOutmCollisionWorld.MoveCharacter
Physics.Raycast                      -> IOutmCollisionWorld.Raycast
Collider trigger                     -> TriggerStore / Physics sensor query
LayerMask                            -> collision channel/filter ids
AudioSource.PlayOneShot              -> OutmEvent -> AudioSystem
AudioClip                            -> AudioDef / sound bank path
ScriptableObject def                 -> JSON def / compiled DefId
Prefab                               -> ActorDef + visual/physics refs
Unity scene                          -> OUTMAP + GLB + manifest
Unity Editor tool                    -> Blender exporter / OUT editor tool
OUTL_EntityAdapter                   -> entity stores + bridge metadata
OUTL_ActorInputFrame                 -> OutmInputFrame / OutmUserCommand
OUTL_ApplyInput                      -> system consuming fixed user command
```

---

## Porting layers

### Layer 1: Pure contracts

Port first:

```text
UseRequest / UseResult
DamageContext
SaveSnapshot / Save participant
Task/Schedule handles
Resource/Faction/Perception contracts
Surface ids/defs
```

These contain almost no host-specific behavior once Unity references are replaced with EntityId and System.Numerics.Vector3.

### Layer 2: Runtime stores

Then create stores:

```text
UseFocusStore
DamageableStore
ResourceStore
FactionStore
PerceptionStore
SurfaceStore
DoorStore
TriggerStore
PickupStore
ActorStore
```

Do not create object brains. Create data stores and systems. Objects with private destinies are how update loops become folklore.

### Layer 3: Systems

Then port behavior:

```text
UseSystem
DamageSystem
PickupSystem
ActorMotorSystem
SurfaceSystem
TaskSystem
ScheduleSystem
PerceptionSystem
```

### Layer 4: Host bridges

Only after that, bind to host APIs:

```text
raylib rendering/audio/input
Jolt physics
Blender/GLB import path
OUT editor tooling
```

---

## What must not be ported as-is

Do not directly port:

```text
GetComponent chains
MonoBehaviour life-cycle assumptions
Unity Inspector-only state as runtime truth
Unity LayerMask logic as kernel logic
AudioSource ownership
Camera ownership of motor truth
Unity scene object hierarchy as gameplay truth
```

Those are host conveniences, not core architecture.

---

## Naming law

Product / engine name:

```text
OUT CORE
```

Current bootstrap project folder:

```text
OUT_RayMicro
```

Current namespaces may temporarily remain:

```text
OUT_RayMicro.*
```

Future namespace migration target:

```text
OUT_CORE.*
```

Do not rename every namespace yet. First finish:

```text
UseSystem
DamageContext
SaveSnapshot
SurfaceDef
ActorMotorState
ChunkStore
```

Then perform a mechanical namespace rename in one controlled commit. Doing it earlier creates a huge diff that teaches nobody anything, except that text replacement exists. Humanity already knows and still chooses meetings.

---

## Immediate porting order

```text
1. OutmUseRequest / OutmUseResult / UseSystem
2. OutmDamageContext / OutmDamageKind / OutmHitZone
3. OutmSaveSnapshot
4. OutmSurfaceDef / OutmSurfaceId
5. OutmActorMotorState
6. DoorStore / TriggerStore
7. ChunkStore / ActiveChunkSet
8. Task/Schedule handles
9. Resource/Faction/Perception stores
```

This order matches the original OUT CORE Lite spirit without copying Unity's host organs into the standalone runtime.

---

## Hard rule

Every port must answer:

```text
Is this kernel grammar?
Is this host bridge?
Is this content?
Is this editor tooling?
```

If the answer is unclear, do not write code yet. Ambiguity is where architecture goes to become soup.
