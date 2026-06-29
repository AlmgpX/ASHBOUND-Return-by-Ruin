# OUT RayMicro physics decision: Jolt vs PhysX

Decision status: **choose Jolt later, but keep custom physics now**.

Short version:

```text
Now:   custom micro-physics for FPS/controller/projectiles/triggers
Later: Jolt if rigid bodies, mesh collision and scalable broadphase become necessary
Avoid: PhysX as first integration for this project
```

This is a design decision for OUT RayMicro, not a universal religious war about physics engines. Humanity has enough of those, usually with worse variable names.

---

## Current project needs

OUT RayMicro currently needs:

```text
Quake-like FPS movement
capsule/cylinder character collision
steps and slopes
raycasts
sphere/capsule casts
triggers
projectiles with bounce
static mesh collision
simple moving doors/lifts/platforms
high performance
simple build/distribution
small code surface
clean OUT CORE command/event model
```

It does **not** yet need:

```text
large rigidbody stacks
vehicles
ragdolls
cloth
fluid/fire simulation
destruction SDK
GPU physics
complex articulation
industrial simulation pipeline
```

So the first physics layer should be custom and boring:

```text
OutmPhysicsWorld
OutmCharacterMotor
OutmStaticMeshCollider
OutmRaycast
OutmShapeCast
OutmTriggerQuery
```

---

## Recommendation

### Phase 1: custom physics

Keep custom physics until these are stable:

```text
movement feel
static collision
projectile behavior
triggers
pickups
basic enemies
imported mesh room
```

Reason: Quake-style movement is gameplay code, not generic rigidbody code. If an external solver fights the movement feel, the project loses its spine immediately. Very elegant way to die, naturally.

### Phase 2: integrate Jolt if needed

Use Jolt when the project needs:

```text
many dynamic rigid bodies
stable broadphase at scale
triangle mesh collision
character controller support beyond our tiny motor
raycasts / shape casts in a real physics world
background loading / chunk insertion
possible deterministic server simulation later
```

Jolt fits OUT RayMicro better because it is explicitly designed as a multi-core friendly rigid body physics and collision library for games/VR, has MIT license, supports raycasts/shape casts, mesh/heightfield/convex/capsule/box shapes, sensors, character simulation, vehicles and deterministic simulation limits. It is also not tied to a giant vendor ecosystem.

### Avoid PhysX first

PhysX is powerful and battle-tested, but it is a heavier ecosystem choice. The current public NVIDIA-Omniverse PhysX repository includes the PhysX SDK, Omniverse extensions, ovphysx Python/USD simulation bindings, Blast and Flow. That is useful for Omniverse/Isaac/industrial simulation-style stacks, but it is oversized for a tiny raylib action-shooter RPG engine seed.

PhysX also brings more integration weight:

```text
heavier build surface
C++ SDK bridge work
vendor ecosystem gravity
more concepts than the current engine needs
larger distribution/debug burden
```

This is not because PhysX is bad. It is because OUT RayMicro needs a blade, not a forklift.

---

## Jolt strengths for OUT RayMicro

```text
MIT license
C++17
no RTTI
no exceptions
game/VR orientation
multi-core friendly architecture
parallel collision queries
background content loading design
rigid bodies
mesh collision
heightfields
capsules/boxes/spheres/convex hulls
sensors/triggers
ray casts
shape casts
character simulation options
vehicles later
soft bodies later if ever needed
optional double precision for large worlds
C# binding ecosystem exists
```

Project fit:

```text
small engine
performance focus
chunk streaming later
open-world preparation
server/replay ambition later
data-oriented runtime direction
```

---

## PhysX strengths

```text
mature NVIDIA SDK
BSD-3 license
rigid body simulation
strong ecosystem
Omniverse/Isaac integration
Blast destruction
Flow fluid/fire simulation
Python/USD-oriented ovphysx path
large industry footprint
```

Project fit:

```text
excellent if the project becomes Omniverse/robotics/industrial-simulation adjacent
less ideal as first physics dependency for a tiny standalone raylib shooter engine
```

---

## Final decision table

| Need | Custom now | Jolt later | PhysX later |
|---|---:|---:|---:|
| Quake-like movement feel | Best | Good with custom wrapper | Risky/heavy |
| Simple triggers/raycasts | Best now | Strong | Strong |
| Static mesh collision | Temporary | Strong | Strong |
| Many rigid bodies | Weak | Strong | Strong |
| Chunk streaming | Simple at first | Strong fit | Possible but heavier |
| Dynamic lore/open-world RPG | Neutral | Good fit | Overkill |
| Small code/distribution | Best | Acceptable | Heavy |
| C# + raylib project ergonomics | Best | Acceptable with binding | More bridge work |
| Vendor/ecosystem gravity | None | Low | High |

---

## Integration rule

Do not let Jolt or PhysX become the gameplay engine.

Physics must stay behind an OUT interface:

```text
IOutmPhysicsWorld
  Raycast
  ShapeCast
  Overlap
  MoveCharacter
  SpawnStaticMeshCollider
  SpawnDynamicBody
  Step
```

Gameplay must not call Jolt/PhysX directly.

Allowed:

```text
WeaponSystem -> OutmPhysics.Raycast
CharacterMotor -> OutmPhysics.MoveCharacter
TriggerSystem -> OutmPhysics.Overlap
```

Forbidden:

```text
WeaponSystem -> JPH::PhysicsSystem directly
Gameplay entity stores raw Jolt body id everywhere
LoreSystem knows physics SDK objects exist
Editor stores vendor-specific physics blobs as source of truth
```

Vendor details belong in runtime adapters:

```text
OutmPhysics.Custom
OutmPhysics.Jolt
OutmPhysics.PhysX   // only if ever needed
```

---

## Required next code before any external physics SDK

```text
1. OutmInputFrame and OutmCommandQueue
2. OutmPhysicsWorld interface
3. Custom static collision/raycast/spherecast
4. Capsule character motor with steps/slopes
5. Trigger overlap events
6. Projectile collision through physics interface
7. Map format with collider definitions
```

Only after those exist should Jolt be integrated. Otherwise the external SDK will define the engine instead of serving it. Software loves coups.

---

## External references

```text
Jolt Physics:
https://github.com/jrouwe/JoltPhysics
https://jrouwe.github.io/JoltPhysics/

NVIDIA PhysX:
https://github.com/NVIDIA-Omniverse/PhysX
https://nvidia-omniverse.github.io/PhysX/
```
