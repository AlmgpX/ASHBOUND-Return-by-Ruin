# OUT RayMicro OUT CORE layering

This document pins the engine back to OUT CORE Lite philosophy. The project was starting to smell like a normal prototype, which is how good ideas get buried under convenience calls and small lies.

---

## Prime law

Runtime must follow this chain:

```text
InputFrame -> Command -> System -> State -> Event -> Presentation
```

Not this:

```text
Input -> Random Object -> Direct Mutation -> Hidden Side Effect -> Why Is Save Broken
```

---

## Layers

### 1. Host bridge

```text
Raylib window
raw input sampling
font/audio/model loading
platform file paths
```

Files:

```text
src/Runtime
```

Rule: host may know raylib. Kernel and gameplay defs must not.

### 2. Input layer

```text
OutmInputFrame
OutmButtons
OutmInputSampler
```

Input is sampled once. Gameplay systems consume input frames or commands, never raw key state.

### 3. Content layer

```text
OutmContentRegistry
OutmWeaponDef
OutmItemDef
OutmActorDef
```

Files:

```text
src/Content
data/defs
```

Weapons, actors, pickups and scene metadata must migrate into defs. Hardcoded revolver numbers are temporary sins now being removed.

### 4. Core layer

```text
EntityId
OutmEvent
OutmEventQueue
OutmWorld
PlayerVitals
ArmorRules
```

Rule: state is explicit, evented and serializable.

### 5. Physics / collision layer

```text
IOutmCollisionWorld
OutmDemoCollisionWorld
future OutmJoltCollisionWorld
```

Gameplay talks to the OUT interface, not directly to Jolt.

Allowed:

```text
CameraMotor -> IOutmCollisionWorld.MoveCharacter
WeaponSystem -> IOutmCollisionWorld.CollidesSphere / Raycast
TriggerSystem -> IOutmCollisionWorld.OverlapBox
```

Forbidden:

```text
CameraMotor -> JoltPhysicsSharp directly
WeaponSystem -> raylib collision helpers
Gameplay entity -> raw backend body pointer
```

### 6. Gameplay systems

```text
Movement system / camera motor
Weapon system
Damage system
Pickup system
Enemy system
Trigger system
```

Rule: systems mutate world state and emit events. They do not play sounds, load models or read keyboard state.

### 7. Presentation layer

```text
Renderer
HUD
AudioSystem
Debug overlay
```

Presentation reacts to state/events. It is not authoritative gameplay.

---

## Current fixes made

```text
controller now moves through IOutmCollisionWorld
projectiles now query IOutmCollisionWorld
revolver numbers now come from data/defs/weapons/revolver.json
content registry exists
player health can lock input at zero
movement air control reduced to stop absurd bunnyhop launch behavior
sound pan restored to raylib 0..1 range
```

---

## Next required systems

### Command queue

```text
OutmCommand
OutmCommandQueue
OutmCommandType
```

Input frame should become command intent:

```text
InputFrame -> UserCommand -> CommandQueue -> Systems
```

### Scene content

```text
OutmSceneDef
OutmSceneLoader
OutmModelCache
OutmSceneRenderer
```

Runtime scene source:

```text
data/maps/*.outmap.json
data/meshes/**/*.glb
```

Blender stays source asset, not runtime format.

### Entity runtime

```text
OutmEntityStore
OutmTransformStore
OutmHealthStore
OutmWeaponRuntimeStore
OutmFactionStore
```

No enemies or pickups as random classes with private mutable logic. They need defs + runtime stores.

### Surface audio

```text
OutmSurfaceId
SurfaceDef
SurfaceAudioDef
```

Needed for:

```text
footsteps
bullet impacts
ricochets
occlusion material later
```

---

## Milestone target

Before more features, the slice must prove this:

```text
one data-driven revolver
one data-driven player def
one data-driven health/armor pickup
one enemy placeholder from actor def
all collision through IOutmCollisionWorld
one map loaded from outmap metadata
HUD and audio are presentation, not gameplay authority
```

If that is not true, do not add lore, factions, RPG systems or cinematic nonsense. The machine needs bones before tattoos.
