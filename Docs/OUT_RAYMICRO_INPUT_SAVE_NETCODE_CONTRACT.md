# OUT RayMicro input / save / multiplayer contract

Purpose: lock the technical meaning of input before the engine grows more systems and starts lying to itself, as software does when unsupervised.

This contract is inspired by Quake / GoldSrc style thinking:

```text
sample input once
turn it into a compact user command / input frame
simulate from that command
record commands or state deltas for replay/save/debug
send commands to server for multiplayer
server validates and simulates authoritative gameplay
```

OUT RayMicro must not allow every system to call raylib input directly.

---

## Current implementation

File:

```text
OUT_RayMicro/src/Input/OutmInputFrame.cs
```

Current types:

```text
OutmButtons
OutmInputFrame
OutmInputSampler
```

Current frame data:

```text
Sequence
DeltaTime
Move        // Vector2, normalized WASD intent
LookDelta   // mouse delta for this frame
Down        // current button bitset
Pressed     // edge bitset this frame
Released    // release edge bitset this frame
```

Current buttons:

```text
Jump
Crouch
Sprint
Use
FirePrimary
FireSecondary
Melee
Overlay
DebugDamage
DebugArmor
```

Raylib input is sampled in one place only:

```text
OutmInputSampler.Sample(dt)
```

The runtime then passes the frame into systems:

```text
editor.Update(world, input)
camera.Update(input, map)
weapons.Update(input, muzzle, forward, map, world)
```

This is the correct direction. Hardware input is an edge boundary, not seasoning to sprinkle across gameplay code like a gremlin with a salt shaker.

---

## Why this matters

### Saving

A save should not depend on what hardware keys are currently held.

The save format should store:

```text
world tick/time
player transform
player velocity
player vitals
inventory / weapon runtime
chunk states
entity states
lore facts
scheduler queue
random seeds
```

Optional debug/replay save may also store:

```text
input command log
state delta log
```

### Replay

Replay can be done by storing:

```text
initial state snapshot
input frames by sequence
random seed
version/hash of defs
```

Then replay is:

```text
load snapshot
for each input frame:
  simulate fixed tick
  compare optional checksums
```

### Multiplayer

Multiplayer should send input/commands, not arbitrary state mutation.

Client:

```text
samples OutmInputFrame
predicts local movement/rendering
sends compact command to server
keeps pending command buffer
```

Server:

```text
receives input command
validates timing/order
simulates authoritative movement/fire/use
spawns projectiles/damage/events
sends snapshots and event deltas
```

Client reconciliation later:

```text
receive authoritative snapshot
rewind to server tick
reapply pending input frames
smooth visual correction
```

---

## Command shape target

`OutmInputFrame` is currently the runtime input container. It should eventually produce a compact network command:

```text
OutmUserCommand
  sequence
  tick
  msec or fixedTickDelta
  moveX
  moveY
  lookYawDelta or viewYaw
  lookPitchDelta or viewPitch
  buttonsDown
  buttonsPressed
  selectedWeapon
  checksum/debug optional
```

Do not send raw keyboard names. Do not send strings. Do not send `Raylib` enums. The network does not care that a human pressed `C`; it cares that `Crouch` is held.

---

## Fixed-tick target

The current slice still uses clamped frame delta. That is acceptable for seed code only.

Target:

```text
render frame:
  sample hardware input
  accumulate time
  while accumulator >= fixedDt:
    build fixed OutmInputFrame
    simulate fixed tick
    accumulator -= fixedDt
  render interpolated state
```

Suggested fixed tick:

```text
60 Hz first
later 72/100/125 Hz if movement feel demands it
```

Quake-like movement and netcode become cleaner when simulation uses stable ticks. Variable dt is where determinism goes to be quietly murdered behind a shed.

---

## OUT CORE compliance

Allowed:

```text
RuntimeInput -> OutmInputFrame -> CommandQueue -> Systems -> Events
```

Forbidden:

```text
WeaponSystem calls Raylib.IsMouseButtonDown directly
CameraMotor calls Raylib.GetMouseDelta directly
Pickup object mutates PlayerVitals directly
Network packet contains raw key names
Save file stores transient hardware input state as truth
```

Current code already moved camera, weapon and overlay debug to `OutmInputFrame`. Continue this pattern.

---

## Relationship to OUTL_FPS_Controller

The Unity OUTL controller already had the correct conceptual split:

```text
BuildUnityInputFrame()
OUTL_ApplyInput(frame, world)
```

RayMicro must follow that pattern, but smaller:

```text
OutmInputSampler.Sample(dt)
OutmSystems.Apply(input, world)
```

The eventual target is not a camera controller with key checks. The target is an actor motor consuming a command frame.

---

## Next implementation steps

```text
1. Rename/refine OutmInputFrame into a stable user command contract if needed.
2. Add OutmCommandQueue for gameplay commands generated from input.
3. Add fixed tick accumulator in OutmApp.
4. Move trigger door interaction into TriggerSystem, not raw app code.
5. Move weapon firing request into CommandQueue.
6. Add save snapshot structs.
7. Add simple state checksum for debugging replays.
```

Do this before enemies, pickups and lore. Otherwise all later systems will inherit the wrong disease.
