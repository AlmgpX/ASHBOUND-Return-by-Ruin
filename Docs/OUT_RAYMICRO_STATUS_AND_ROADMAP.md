# OUT RayMicro status and roadmap

Runtime branch: `mediarelic-carmack-architecture`

Project root:

```text
OUT_RayMicro/
```

This document tracks what has been implemented, what must stay aligned with OUT CORE philosophy, and what should be built next. It is intentionally boring. Boring documents prevent future archeology, allegedly.

---

## Current status

### Done: project shell

```text
OUT_RayMicro/OUT_RayMicro.csproj
OUT_RayMicro/Program.cs
OUT_RayMicro/README.md
OUT_RayMicro/TOOLS.txt
```

The project is a standalone .NET 8 executable using local raylib binaries:

```text
OUT_RayMicro/tools/Raylib-cs.dll
OUT_RayMicro/tools/raylib.dll
```

`raylib.dll` is copied beside the executable in build output because native DLL loading does not care that a human put the file into a nearby folder with good intentions. Tiny ceremony, giant irritation.

### Done: core grammar seed

```text
src/Core/OutmCore.cs
```

Implemented:

```text
EntityId
OutmEventType
OutmEvent
OutmEventQueue
OutmWorld
OutmPlayerVitals
OutmArmorTier
OutmArmorRules
```

Current core direction:

```text
small structs
explicit state
bounded event queue
no hidden runtime allocations for events
no MonoBehaviour-style object brains
```

The core is still too small, but it is pointing in the right direction: state lives in the world, systems mutate it, events explain what happened.

### Done: input frame seed

```text
src/Input/OutmInputFrame.cs
```

Implemented:

```text
OutmButtons bitset
OutmInputFrame
OutmInputSampler
sample once per frame
Pressed / Down / Released edge tracking
Move axis
LookDelta
```

This follows the OUTL_FPS_Controller pattern: build an input frame, then apply that frame to systems. Raylib input should not be read from random gameplay systems. That path leads to replay/netcode rot, and then everyone pretends it was unavoidable.

Current users of `OutmInputFrame`:

```text
OutmEditorShell
OutmCameraMotor
OutmWeaponSystem
```

### Done: raylib host and renderer seed

```text
src/Runtime/OutmApp.cs
```

Implemented:

```text
raylib window
3D camera mode
frame loop
input sampling once per frame
debug/editor overlay call
simple room draw
projectile draw
HUD draw
```

Current state is a runtime seed, not an editor and not a final renderer.

### Done: FPS movement seed

```text
src/Runtime/OutmCamera.cs
```

Implemented:

```text
WASD movement
mouse look via input frame
corrected A/D strafe direction
velocity-based motion
ground acceleration
air acceleration
ground friction
gravity
jump
coyote time
jump input buffer
CTRL/C crouch hold
smooth eye-height crouch
crouch speed multiplier
basic bunnyhop/air-control feel seed
```

Still missing:

```text
real standing/crouching hull height
uncrouch clearance check via capsule overlap
step solver
slope solver
clip planes
proper capsule sweep
ramp slide
surf/ramp behavior
water/lava/slime volumes
movement command struct / fixed tick command stream
```

### Done: demo world and collision seed

```text
src/World/OutmWorldGeometry.cs
```

Implemented:

```text
Quake-like box room
floor / ceiling / walls / pillars
closed/open trigger door
simple static collision
trigger volume
```

This is temporary geometry. It is acceptable only as a seed. The next real step is imported mesh chunks with generated collision data.

### Done: weapon/projectile seed

```text
src/Gameplay/OutmWeaponSystem.cs
```

Implemented:

```text
input-frame-driven projectile fire
fixed projectile pool
projectile motion
collision check
basic ricochet up to 2 bounces
impact/bounce event emission
```

Still missing:

```text
WeaponDef
AttackDef
ammo
reload
cooldown as data
muzzle/viewmodel
damage integration
projectile owner/faction
hitscan mode
melee mode
grenade mode
```

### Done: Quake 1-style armor seed

```text
src/Gameplay/OutmDamageSystem.cs
```

Implemented:

```text
Green Armor  = 100 armor, 30% damage absorption
Yellow Armor = 150 armor, 60% damage absorption
Red Armor    = 200 armor, 80% damage absorption
```

Current damage split:

```text
armorSave = ceil(incomingDamage * armorAbsorbFraction)
healthDamage = incomingDamage - armorSave
armor -= armorSave
```

Armor pickup replacement compares effective protection, not just raw armor points:

```text
effectiveScore = armorPoints * absorbFraction
```

This follows the OUT CORE rule: damage is not a direct object call. Damage is handled by a system, state is mutated in `OutmWorld`, and events/logs describe the reason.

### Done: Unicode HUD / debug overlay

```text
src/Editor/OutmEditorShell.cs
```

Implemented:

```text
Unicode vitals HUD
HP hearts
armor blocks with GA/YA/RA tier code
mana diamonds
movement speed display
GROUND/AIR/CROUCH display
event log overlay
F1 overlay toggle through input frame
F2 debug damage through input frame
F3 debug armor pickup through input frame
```

Current HUD style:

```text
HP  ♥ hearts
GA  ▰ blocks
YA  ▰ blocks
RA  ▰ blocks
MN  ◆ diamonds + ✦ accent
```

If any glyph renders as squares on a target machine, swap to safer symbols in one place inside `OutmEditorShell`.

---

## Current controls

```text
WASD          move
Mouse         look
Space         jump
Left Ctrl     crouch
C             crouch
Left Shift    sprint modifier
Left Mouse    fire projectile
F1            toggle overlay
F2            debug damage 25
F3            debug armor pickup cycle: Green -> Yellow -> Red
Esc           quit
```

---

## OUT CORE compliance checklist

Every new feature must answer these questions before code is written:

```text
1. What state does it own?
2. Which system mutates that state?
3. Which command or event caused the mutation?
4. Can it run without per-frame allocation?
5. Can it be logged, replayed or debugged?
6. Does it avoid direct entity-to-entity gameplay calls?
7. Does it avoid string lookup in hot paths?
8. Does it keep raylib out of the kernel layer?
```

Allowed pattern:

```text
Input -> Command -> System -> StateDelta -> Event -> Render/Audio/VFX request
```

Avoid:

```text
Object A calls Object B directly
random hidden Update brains
global mutable soup
allocating bullets/events every frame
magic strings in hot loops
feature-specific manager bloom
```

---

## Immediate next steps

### Step 1: command layer

Input frame exists. Next create explicit gameplay command structs:

```text
OutmCommand
OutmCommandQueue
OutmCommandType
```

Hardware input should become command intent:

```text
InputFrame -> UserCommand -> CommandQueue -> Systems
```

Reason: this is required for replays, networking later, AI reuse, and clean OUT CORE grammar. Also because letting every system sniff hardware input is how code becomes a damp basement.

### Step 2: fixed tick loop

Current runtime still simulates with clamped frame dt. Replace with:

```text
render frame:
  sample input
  accumulate time
  while accumulator >= fixedDt:
    build fixed input command
    simulate fixed tick
  render latest/interpolated state
```

This is mandatory for Quake/Half-Life-style save/replay/netcode discipline.

### Step 3: real collision foundation

Replace point/radius collision with a real static collision module:

```text
capsule sweep
raycast
sphere cast
AABB broadphase
triangle mesh static collision
trigger overlap query
uncrouch clearance check
```

Short-term file target:

```text
src/Physics/OutmPhysicsWorld.cs
src/Physics/OutmCollider.cs
src/Physics/OutmRaycast.cs
src/Physics/OutmCharacterMotor.cs
```

This should remain custom until it becomes painful enough to justify Jolt integration.

### Step 4: imported mesh room

Add model loading and map format seed:

```text
.outmap.json
mesh path
spawn point
static colliders
trigger definitions
light/fog settings
```

First goal: load one OBJ/glTF room instead of hardcoded boxes.

### Step 5: weapon data

Create:

```text
OutmWeaponDef
OutmAttackDef
OutmProjectileDef
OutmWeaponRuntime
```

Move revolver behavior out of hardcoded values.

### Step 6: pickups

Add pickups as data-driven entities:

```text
health pickup
green/yellow/red armor pickup
mana pickup
ammo pickup
```

They should emit `ItemPicked` / `ArmorPicked` / `HealthChanged` events. No direct player mutation from pickup objects.

### Step 7: first enemy placeholder

Start with a simple billboard or cube enemy:

```text
position
health
faction
detect player
shoot projectile
receive damage
emit killed
```

Do not build AI architecture yet. First make damage/weapon/entity grammar work.

### Step 8: light/fog style

Add simple visual style flags:

```text
ambient color
sector/chunk fog color
fullbright material flag
muzzle flash light stub
```

For Quake-like feeling, readability matters more than physically correct lighting. The photons can file a complaint later.

---

## Medium roadmap

```text
M0 host: mostly done
M1 quake-room renderer: started, needs imported mesh room
M2 gameplay kernel: started, needs command queue/scheduler/registry/pools
M3 shooter slice: started, needs data-driven weapons, damage targets and pickups
M4 editor MVP: not started
M5 Quake-like level: not started
M6 open-world preparation: not started
M7 dynamic lore: not started
```

---

## Definition of next playable milestone

The next milestone is not open world. The next milestone is one convincing Quake-like room slice:

```text
imported 3D room mesh
working FPS movement with steps/slopes/crouch hull
one revolver weapon def
one armor pickup
one health pickup
one enemy placeholder
one trigger door
one event log showing why things happened
one Unicode HUD showing HP/armor/mana/ammo
```

When this works, then build the editor around it.

---

## Do not do yet

```text
do not add networking gameplay before fixed tick + command stream
do not add dynamic lore
do not add full rigidbody physics
do not add complex editor gizmos
do not add skeletal animation
do not add terrain streaming
```

This project needs a hard spine before it grows limbs. Otherwise it becomes a wet octopus with a build button.
