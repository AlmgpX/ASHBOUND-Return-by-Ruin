# OUT RayMicro

Standalone raylib-based OUT CORE micro engine experiment.

This first slice uses `Raylib-cs.dll` plus native `raylib.dll` from the local `tools` folder. OUTM core stays separate from raylib; raylib is only the host bridge.

## Required local files

Put these files here before running on Windows:

```text
OUT_RayMicro/tools/Raylib-cs.dll
OUT_RayMicro/tools/raylib.dll
```

For Unicode HUD symbols, put a font here:

```text
OUT_RayMicro/data/fonts/hud_unicode.ttf
```

If the font has another `.ttf` or `.otf` name, the runtime now tries to auto-discover it inside:

```text
OUT_RayMicro/data/fonts/
```

Recommended font for the current retro HUD: GNU Unifont TrueType.

The project copies everything under `tools` and `data` into build output, and `raylib.dll` is copied beside the executable because native DLL loading is apparently a small ritual.

## Runtime requirement

The project now targets:

```text
net9.0
```

Install .NET 9 SDK before building.

## Jolt package

The project file now contains:

```xml
<PackageReference Include="JoltPhysicsSharp" Version="2.21.0" />
```

Physics code must stay behind the OUT interface:

```text
src/Physics/OutmCollisionWorld.cs
```

## Audio folders

Current discovered audio layout:

```text
OUT_RayMicro/data/audio/Weapon/OUT_BulletShot_*.wav
OUT_RayMicro/data/audio/Weapon/OUT_Impact*.wav
OUT_RayMicro/data/audio/Misc/BulletRicImpact.*
OUT_RayMicro/data/audio/Misc/DoorOpen.wav
OUT_RayMicro/data/audio/Footstep/*.wav
OUT_RayMicro/data/audio/Music/*.mp3
OUT_RayMicro/data/audio/Music/*.ogg
```

Audio is event-driven for the current slice:

```text
Fired            -> spatial shot sound + pitch variation
ProjectileBounce -> spatial ricochet sound + pitch variation
ProjectileHit    -> spatial impact sound + pitch variation
DoorToggled      -> spatial door sound
Footstep         -> spatial stone footstep sound + pitch variation
```

Raylib does not provide a full 3D audio scene graph here. OUT RayMicro currently does a small manual spatialization layer:

```text
distance attenuation
left/right pan from listener right vector
random pitch variation
sound-bank round robin
```

First music file from `data/audio/Music` is streamed automatically and ticked every frame.

## Run

```powershell
cd OUT_RayMicro
dotnet run -c Release
```

## Current slice

M0/M1/M3 seed:

```text
raylib window
true 3D camera
single sampled input frame
event-driven spatial audio bridge
streamed music playback
OUT collision interface contract
JoltPhysicsSharp package reference
simple Quake-room demo geometry
Quake-style movement seed
CTRL/C crouch seed without FOV mutation
Quake 1-style armor absorption
Unicode font loader for vitals HUD
FPS movement with static collision
projectile revolver stub
bounce/impact events
trigger door stub
debug/event overlay
```

This is not the final editor. This is the seed runtime.

## Controls

```text
WASD          move
Mouse         look
Space         jump
Left Ctrl     crouch
C             crouch
Left Shift    sprint modifier
Left Mouse    fire projectile
F1            toggle editor/debug overlay
F2            debug damage 25
F3            debug armor pickup cycle: green -> yellow -> red
Esc           quit
```

## Documentation

```text
Docs/OUT_CORE_RAYLIB_MICRO_ENGINE.md
Docs/OUT_RAYMICRO_STATUS_AND_ROADMAP.md
Docs/OUT_RAYMICRO_PHYSICS_DECISION_JOLT_VS_PHYSX.md
Docs/OUT_RAYMICRO_INPUT_SAVE_NETCODE_CONTRACT.md
Docs/OUT_RAYMICRO_ASSET_PIPELINE.md
Docs/OUT_RAYMICRO_JOLT_INTEGRATION.md
```

Current physics decision:

```text
Custom OUT collision interface first.
Jolt package/backend second.
Do not let gameplay code call the external physics package directly.
```

Current input decision:

```text
Sample raylib input once into OutmInputFrame.
Pass OutmInputFrame into systems.
Later convert it into fixed-tick user commands for save/replay/multiplayer.
No gameplay system should read raw hardware input directly.
```

Current asset decision:

```text
.blend files are source assets.
Runtime should load exported .glb/.gltf plus .outmap.json metadata.
Use Blender command-line export later to convert .blend -> .glb + OUT metadata.
```

Next engineering target:

```text
command queue
fixed tick accumulator
collision backend adapter
capsule sweep / raycast / trigger queries
imported GLB room
weapon definitions
pickups
enemy placeholder
```
