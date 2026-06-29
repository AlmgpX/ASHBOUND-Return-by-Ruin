# OUT RayMicro

Standalone raylib-based OUT CORE micro engine experiment.

This project intentionally uses native `raylib.dll` directly through a tiny P/Invoke layer instead of depending on a specific `Raylib-cs.dll` wrapper version. Wrapper APIs change. C ABI is boring. Boring wins.

## Required local file

Put native raylib here before running on Windows:

```text
OUT_RayMicro/tools/raylib.dll
```

The project copies everything under `tools/` into build and publish output.

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
FPS movement with static collision
projectile revolver stub
bounce/impact events
trigger stub
debug/event overlay
```

This is not the final editor. This is the seed runtime. If it grows managers like mold, delete the mold.

## Controls

```text
WASD          move
Mouse         look
Left Mouse    fire projectile
F1            toggle editor/debug overlay
Esc           quit
```
