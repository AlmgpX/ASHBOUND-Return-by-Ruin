# OUT RayMicro vs original OUT CORE Lite parity audit

Purpose: keep OUT RayMicro aligned with the original Unity OUT CORE Lite instead of letting it mutate into a generic raylib toy engine with a dramatic name. Humanity has enough of those.

This is not a request to copy Unity classes. Unity is host glue. OUT CORE Lite is the architecture grammar.

---

## Sources checked

Original Unity reference added in commit:

```text
4fca41a6d28c731f666390b53656c2f9ff0520e3
OUT CORE Lite - добавил оригинал ядра из Unity
```

Important inspected files:

```text
OUT_Core/Documentation/OUT_CORE_CONTEXT_ROADMAP.md
OUT_Core/Documentation/OUT_CORE_LIVING_WORLD_ROADMAP.md
OUT_Core/Abstractions/IOutUsable.cs
OUT_Core/Abstractions/OUT_UseRequest.cs
OUT_Core/Abstractions/IOutSaveAdapter.cs
OUT_Core/Abstractions/OUT_DamageContext.cs
OUT_Core/OUT_CORE_Lite/Player/OUTL_FPS_Controller.cs
```

Current RayMicro files compared:

```text
OUT_RayMicro/src/Core/OutmCore.cs
OUT_RayMicro/src/Core/OutmEntityStore.cs
OUT_RayMicro/src/Core/OutmRuntimeStores.cs
OUT_RayMicro/src/Core/OutmLogicTickScheduler.cs
OUT_RayMicro/src/Input/OutmInputFrame.cs
OUT_RayMicro/src/Input/OutmCommand.cs
OUT_RayMicro/src/Gameplay/OutmTriggerSystem.cs
OUT_RayMicro/src/Gameplay/OutmDamageSystem.cs
OUT_RayMicro/src/Physics/OutmCollisionWorld.cs
OUT_RayMicro/src/World/OutmMapDef.cs
OUT_RayMicro/src/Render/OutmSceneRenderer.cs
```

---

## High-level verdict

OUT RayMicro is directionally correct, but not parity-complete.

Current parity level:

```text
Kernel identity/events/input/fixed tick:          partial-good
Use/interact grammar:                            weak seed
Damage grammar:                                  weak seed
Save/load contracts:                             missing
Task/schedule contracts:                         missing
Actor/resource/faction/perception contracts:     missing
FPS motor parity:                                partial movement only
Surface/footstep/fall/hazard parity:             partial/weak
Large-world chunk processing:                    seed exists, not integrated
AI/living world:                                 missing
Editor/tools parity:                             different host path, acceptable
```

Translation: the skeleton is sane, but it still has the emotional depth of a traffic cone.

---

## What matches well

### 1. Unity host vs architecture separation

Original OUT CORE roadmap says Unity is the host, not the architecture. OUT CORE owns entity identity, commands, events, effects, conditions, signals, state, save/load, scheduling, inspection, validation and simulation modules.

RayMicro matches this direction by making raylib the host and moving gameplay toward:

```text
EntityId
OutmWorld
OutmCommandQueue
OutmEventQueue
IOutmCollisionWorld
OutmContentRegistry
OutmMapDef / OUTMAP
OutmLogicTickScheduler
```

Good.

Do not regress into gameplay systems calling raylib input/physics/audio directly.

---

### 2. Input frame / command stream

Original OUTL_FPS_Controller consumes an `OUTL_ActorInputFrame` through `OUTL_ApplyInput(frame, world)` after building or receiving an input frame. RayMicro mirrors the idea with:

```text
OutmInputSampler.Sample
OutmInputFrame
OutmUserCommand
OutmCommandQueue
SimulateFixedTick
```

Good.

Missing:

```text
phased input sinks
command target routing
selected weapon in user command
impulse command vocabulary
command history/checksum
```

---

### 3. Fixed tick and large-world scheduling direction

Original Living World roadmap explicitly names:

```text
Sector
Chunk
Region
Zone
WorldCell
EntitySnapshot
EventJournal
PersistenceLayer
RelevancePolicy
ProcessingTier
StreamingBoundary
SimulationClock
```

It also says processing must be tiered and budgeted:

```text
Full / Near / Mid / Far / Dormant
```

RayMicro now has:

```text
OutmLogicTickTier.Always/Near/Mid/Far/Dormant
OutmLogicTickPolicy
OutmLogicTickScheduler
OutmChunkKey
```

Good seed.

Missing:

```text
ChunkStore
ActiveChunkSet
Entity-to-chunk membership
chunk snapshot persistence
event journal
budgeted system scheduler
```

---

### 4. OUTMAP validator / manifest path

Original core roadmap demands validation and thin host/tool boundaries. RayMicro now has:

```text
maps.json
OutmMapManifest
OutmMapValidator
OutmMapDef
OutmSceneRenderer
OutmModelCache
```

Good direction.

Missing:

```text
validator severity report storage
editor-facing validation UI
asset existence validation for mesh refs
map entity spawning into stores
```

---

## Major parity gaps

### Gap A: Use/interact contract is too narrow

Original OUT CORE Lite has a real interface:

```csharp
public interface IOutUsable
{
    OUT_UseCapabilityFlags UseCaps { get; }
    bool CanUse(in OUT_UseRequest request);
    OUT_UseResult Use(in OUT_UseRequest request);
}
```

and the request contains:

```text
User
Origin
Direction
HoldTime
```

RayMicro currently has:

```text
TriggerSystem checks trigger box
Use button toggles door by trigger.Kind string
```

This is not enough.

Needed RayMicro equivalent:

```text
OutmUseRequest
OutmUseResult
OutmUseCapabilityFlags
IOutmUsable or store-based UseSystem
OutmInteractionHit
Use sticky grace
Use ray / shape probe through IOutmCollisionWorld
continuous use support
```

Implementation target:

```text
input.IsPressed(Use)
  -> UseSystem.BuildUseRequest(playerEntity, origin, direction, holdTime)
  -> query focus / trigger / ray hit
  -> CanUse
  -> UseResult
  -> events/effects
```

Door toggle becomes one implementation of Use, not the whole religion.

---

### Gap B: Damage context is too primitive

Original has:

```text
DamageKind
HitZone
Instigator
Inflictor
HitPoint
HitNormal
DamageAmount
HitDirection
Impulse
```

RayMicro currently has Quake armor and basic player damage, but no full damage context. Projectile hit events also do not carry a real typed damage payload.

Needed RayMicro equivalent:

```text
OutmDamageKind
OutmHitZone
OutmDamageContext
IOutmDamageable or DamageableStore
OutmDamageResult
DamageSystem.Apply(context)
```

Do not keep using generic float `OutmEvent.Value` for serious combat. That way lies soup.

---

### Gap C: Save/load contract is missing

Original has:

```csharp
public interface IOutSaveAdapter
{
    string SaveKey { get; }
    object CaptureSaveState();
    void RestoreSaveState(object state);
}
```

RayMicro has crash logging and runtime stores, but no save snapshot contract.

Needed:

```text
OutmSaveSnapshot
IOutmSaveParticipant or store-based SaveWriter
stable map id
stable entity id / authoring id
door state snapshot
player transform/vitals snapshot
content version/hash
```

Runtime `EntityId` is session-local. Save identity must use stable ids.

---

### Gap D: Task/schedule/contracts are missing

Original has schedule/task abstractions:

```text
IOutScheduleAgent
IOutTaskExecutor
OUT_ScheduleHandle
OUT_TaskHandle
OUT_TaskStatus
```

RayMicro now has a logic tick scheduler but not a task/schedule layer.

Needed later:

```text
OutmScheduleHandle
OutmTaskHandle
OutmTaskStatus
OutmTaskSystem
OutmScheduleSystem
```

This matters for AI, doors/movers, scripted sequences, ambient world logic and delayed events.

---

### Gap E: Actor, resource, faction, perception contracts are missing

Original abstractions include:

```text
IOutActor
IOutResourceOwner
IOutFactionMember
IOutRelationshipResolver
IOutPerceptionTarget
```

RayMicro has `OutmEntityStore` and `OutmActorDef`, but no runtime actor/resource/faction/perception stores.

Needed:

```text
OutmActorStore
OutmResourceStore
OutmFactionStore
OutmPerceptionStore
OutmRelationshipResolver
OutmStimulusEvent
```

Do not start with full AI behavior. Start with the contracts/stores.

---

### Gap F: FPS controller parity is only partial

OUTL_FPS_Controller contains far more than current RayMicro motor:

```text
GoldSrc unit scale
input fallback / actor bridge
view bob / view roll / FOV feel
GoldSrc movement values
jump buffer / coyote
long jump
crouch hull / uncrouch clearance
slope/step/ground probe
surface friction/hazards
ladder system
ledge grab
use ray/probe with sticky seconds
weapon slot handling
audio footsteps/jump/landing
fall damage
runtime motor state writeback
pool reset hooks
```

RayMicro currently has:

```text
camera motor
basic crouch
basic jump/coyote/buffer
basic collision adapter
basic footsteps timer
basic death lockout
world transform mirror
```

Missing high-priority motor parity:

```text
real actor motor state separated from camera
standing/crouch hull correctness
uncrouch clearance query
step solver
slope solver
surface friction
fall damage
distance-based footsteps
landing/jump events
use focus/sticky interaction
weapon slot command fields
```

Ladder, ledge grab and long jump can wait. Hull/step/slope/fall/surface cannot wait forever.

---

### Gap G: Surface system is missing

Original controller has `OUT_SurfaceData`, known surfaces, surface probe, friction, hazards and surface audio.

RayMicro has hardcoded `footstep stone`.

Needed:

```text
OutmSurfaceId
OutmSurfaceDef
OutmSurfaceStore
surface id on OUTMAP boxes/colliders
physics queries return surface id
footstep/impact audio by surface
surface friction/hazard hooks
```

This is high-value and should come before fancy enemy AI.

---

## What we should NOT copy directly

Do not copy Unity MonoBehaviour patterns:

```text
GetComponent chains
GameObject references
Unity LayerMask as kernel truth
MonoBehaviour Update as simulation authority
ScriptableObject assets as runtime-only truth
```

RayMicro equivalents:

```text
GameObject         -> EntityId / stable authoring id
Transform          -> OutmTransformStore
LayerMask          -> collision/filter ids
MonoBehaviour      -> system update over stores
ScriptableObject   -> JSON def / compiled def id
Unity Physics      -> IOutmCollisionWorld / Jolt backend
AudioSource        -> audio event requests
```

---

## Priority fixes after this audit

### P1: Use contract parity

```text
OutmUseRequest
OutmUseResult
OutmUseCapabilityFlags
OutmUseSystem
OutmInteractionHit
sticky use focus
```

Door use must move from `TriggerSystem` string switch toward `UseSystem` effect resolution.

### P2: Damage context parity

```text
OutmDamageKind
OutmHitZone
OutmDamageContext
DamageableStore seed
projectile hit builds typed damage context
```

### P3: Save snapshot contract

```text
OutmSaveSnapshot
stable map id
stable entity authoring id
player snapshot
door snapshot
store snapshot interfaces
```

### P4: Surface system

```text
OutmSurfaceId
SurfaceDef
surface on boxes/colliders
footstep + impact routing
surface friction later
```

### P5: Motor parity pass

```text
ActorMotorState
uncrouch clearance
step/slope solver
fall damage
distance-based footsteps
jump/land events
```

### P6: Chunk/store integration

```text
ChunkStore
ActiveChunkSet
entity membership
logic scheduler applies to actor/pickup/ambient systems
```

---

## Current conclusion

OUT RayMicro has not betrayed OUT CORE Lite. Yet.

It correctly follows:

```text
host is not architecture
input frame before gameplay
fixed tick before replay/netcode
entity identity before object soup
manifest/validator before editor UI
logic tier scheduler before large-world simulation
```

But it is missing several canonical OUT CORE contracts:

```text
UseRequest/UseResult
DamageContext
SaveAdapter/Snapshot
Task/Schedule
Resource/Faction/Perception
SurfaceData
proper actor motor state
```

Next code slice should be `OutmUseSystem`. That is the cleanest way to reconnect RayMicro to OUT CORE grammar while immediately improving doors, buttons, pickups, ladders later, terminals, NPC commands and editor interactions.

If we build enemies before Use/Damage/Save/Surface contracts, we are not building a core. We are decorating a hole.
