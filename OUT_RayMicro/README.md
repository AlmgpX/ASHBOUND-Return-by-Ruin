# OUT RayMicro

Standalone raylib-based OUT CORE micro engine experiment.

This first slice uses `Raylib-cs.dll` plus native `raylib.dll` from the local `tools/` folder. OUTM core stays separate from raylib; raylib is only the host bridge.

## Required local files

Put these files here before running on Windows:

```text
OUT_RayMicro/tools/Raylib-cs.dll
OUT_RayMicro/tools/raylib.dll
```

The project copies everything under `tools/` into build and publish output, and `raylib.dll` is copied beside the executable because native DLL loading is apparently a small ritual.

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
simple Quake-room demo geometry
Quake-style movement seed
Quake 1-style armor absorption
Unicode vitals HUD
FPS movement with static collision
projectile revolver stub
bounce/impact events
trigger stub
debug/event overlay
```

This is not the final editor. This is the seed runtime.

## Controls

```text
WASD          move
Mouse         look
Space         jump
Left Mouse    fire projectile
F1            toggle editor/debug overlay
F2            debug damage 25
F3            debug armor pickup cycle: green -> yellow -> red
Esc           quit
```
