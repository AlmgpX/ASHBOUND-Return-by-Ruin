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

Files:

```text
OUT_RayMicro/src/Input/OutmInputFrame.cs
OUT_RayMicro/src/Input/OutmCommand.cs
OUT_RayMicro/src/Runtime/OutmFixedStep.cs
```

Current types:

```text
OutmButtons
OutmInputFrame
OutmInputSampler
OutmUserCommand
OutmCommand
OutmCommandQueue
OutmFixedStep
```

Current frame / command data:

```text
Sequence
Tick
FixedDeltaTime
Move
LookDelta
Down
Pressed
Released
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
OutmInputSampler.Sample(frameDt)
```

The runtime now converts that sampled frame into fixed user commands:

```text
OutmInputFrame -> OutmUserCommand -> OutmCommandQueue -> SimulateFixedTick
```

Hardware input is an edge boundary, not seasoning to sprinkle across gameplay code like a gremlin with a salt shaker.

---

## Current fixed tick loop

Current runtime pattern:

```text
render frame:
  sample raw input once
  editor overlay consumes raw edge input for UI/debug only
  add frame time to accumulator
  while accumulator >= 1/60 and tick count is under safety cap:
    build OutmUserCommand
    enqueue command
    SimulateFixedTick(command queue)
  process audio events
  update streamed music
  render latest state
```

Initial tick rate:

```text
60 Hz
```

Safety cap:

```text
5 simulation ticks per render frame
```

This is not final deterministic netcode yet, but it is the first real transition away from variable-delta soup.

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
def version/hash
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
input commands by sequence/tick
random seed
version/hash of defs
```

Then replay is:

```text
load snapshot
for each fixed user command:
  simulate fixed tick
  compare optional checksums
```

### Multiplayer

Multiplayer should send input/commands, not arbitrary state mutation.

Client:

```text
samples OutmInputFrame
builds OutmUserCommand
predicts local movement/rendering
sends compact command to server
keeps pending command buffer
```

Server:

```text
receives user command
validates timing/order
simulates authoritative movement/fire/use
spawns projectiles/damage/events
sends snapshots and event deltas
```

Client reconciliation later:

```text
receive authoritative snapshot
rewind to server tick
reapply pending input commands
smooth visual correction
```

---

## Command shape target

`OutmUserCommand` is now the compact gameplay intent container:

```text
OutmUserCommand
  sequence
  tick
  fixedDeltaTime
  move
  lookDelta
  buttonsDown
  buttonsPressed
  buttonsReleased
```

Next additions:

```text
selectedWeapon
checksum/debug optional
clientTime optional
```

Do not send raw keyboard names. Do not send strings. Do not send `Raylib` enums. The network does not care that a human pressed `C`; it cares that `Crouch` is held.

---

## OUT CORE compliance

Allowed:

```text
RuntimeInput -> OutmInputFrame -> OutmUserCommand -> OutmCommandQueue -> Systems -> Events
```

Forbidden:

```text
WeaponSystem calls Raylib.IsMouseButtonDown directly
CameraMotor calls Raylib.GetMouseDelta directly
Pickup object mutates PlayerVitals directly
Network packet contains raw key names
Save file stores transient hardware input state as truth
```

Current code already moved camera, weapon and fixed gameplay tick to command-fed simulation. Continue this pattern.

---

## Relationship to OUTL_FPS_Controller

The Unity OUTL controller already had the correct conceptual split:

```text
BuildUnityInputFrame()
OUTL_ApplyInput(frame, world)
```

RayMicro now follows that pattern in smaller standalone form:

```text
OutmInputSampler.Sample(frameDt)
OutmUserCommand.ToInputFrame()
SimulateFixedTick(...)
```

The target is not a camera controller with key checks. The target is an actor motor consuming command frames.

---

## Next implementation steps

```text
1. Add state checksum for replay/debug.
2. Add selected weapon to OutmUserCommand.
3. Move trigger door logic into TriggerSystem.
4. Move debug F2/F3 into command types, not overlay-only direct calls.
5. Add save snapshot structs.
6. Add pending command history ring for future client prediction/reconciliation.
7. Add OutmJoltCollisionWorld backend behind IOutmCollisionWorld.
```

The command stream exists now. Do not sneak hardware reads back into gameplay, unless the goal is to summon bugs from 1998 and pretend they are retro charm.
