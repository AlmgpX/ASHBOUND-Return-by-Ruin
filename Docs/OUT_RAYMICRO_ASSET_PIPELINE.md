# OUT RayMicro asset pipeline

This document defines how OUT RayMicro should ingest 3D scenes, sounds, fonts and later physics data.

The rule is simple:

```text
authoring files are sources
runtime files are compiled/exported assets
```

A `.blend` file is a source file. Raylib runtime should not load `.blend` directly. Runtime loads `.glb` / `.gltf` / `.obj` / textures / audio / JSON metadata. The build/editor tool may invoke Blender to convert `.blend` sources into runtime files.

---

## 1. Directory layout

```text
OUT_RayMicro/
  data/
    sources/
      blender/
        test_room.blend
    maps/
      test_room.outmap.json
    meshes/
      rooms/
        test_room.glb
    textures/
    audio/
      weapons/
      world/
      steps/
    fonts/
      hud_unicode.ttf
```

`data/**` is copied into build output by the project file.

---

## 2. 3D scene pipeline

### Preferred authoring input

```text
.blend
```

### Preferred runtime output

```text
.glb
```

Why `.glb`:

```text
single binary file
supports meshes
supports materials
supports hierarchy
supports animation later
more scene-oriented than OBJ
better than raw OBJ for lights/cameras/source-scene meaning
```

### Runtime rule

Raylib loads runtime formats. It should not parse Blender internals.

```text
Blender source -> export tool -> GLB + OUT metadata -> RayMicro runtime
```

---

## 3. Blender source support

OUT RayMicro can support `.blend` as source through a Blender command-line export step:

```text
blender --background data/sources/blender/test_room.blend --python tools/blender/export_out_scene.py
```

The exporter should write:

```text
data/meshes/rooms/test_room.glb
data/maps/test_room.outmap.json
```

The `.glb` contains visual scene data. The `.outmap.json` contains game-specific metadata that should not be guessed by raylib:

```text
player spawn
trigger volumes
door ids
collision proxy names
lore region names
entity spawn ids
light gameplay hints
ambient/fog settings
```

Recommended Blender object naming convention:

```text
OUT_SPAWN_PlayerStart
OUT_TRIGGER_Door01
OUT_DOOR_Door01
OUT_COLLIDER_Room_Main
OUT_LIGHT_Torch_01
OUT_LORE_Region_AshGate
```

The exporter reads those names and writes `.outmap.json`.

---

## 4. Lights from Blender

Blender lights can be exported as authoring data, but do not trust them blindly as runtime lighting.

Recommended split:

```text
Blender light objects -> exported to OUT light definitions
RayMicro runtime -> simple point/ambient/fog/fullbright first
```

Example light metadata:

```json
{
  "lights": [
    {
      "id": "torch_01",
      "type": "point",
      "position": [2.5, 2.1, -4.0],
      "color": [255, 170, 90],
      "radius": 7.0,
      "intensity": 1.0
    }
  ]
}
```

Do not attempt physically correct Blender-to-raylib lighting in the first milestone. Quake-like readability beats a haunted approximation of a renderer that never asked to be Blender.

---

## 5. Collision data

Visual mesh and collision mesh are separate.

Use Blender object prefixes:

```text
OUT_COLLIDER_*
OUT_TRIGGER_*
OUT_LADDER_*
OUT_WATER_*
```

Export collision metadata separately:

```json
{
  "colliders": [
    {
      "id": "room_main",
      "type": "triangleMesh",
      "sourceObject": "OUT_COLLIDER_Room_Main"
    },
    {
      "id": "door_trigger",
      "type": "boxTrigger",
      "center": [0, 1, -7.2],
      "size": [2.2, 2.0, 0.8]
    }
  ]
}
```

First custom physics reads simple boxes and later triangle meshes. Jolt can become a backend after the OUT physics interface exists.

---

## 6. Audio pipeline

Runtime folder:

```text
data/audio/weapons/revolver_fire.wav
data/audio/weapons/ricochet_01.wav
data/audio/world/door_open.wav
data/audio/world/door_close.wav
data/audio/steps/stone_01.wav
```

Short sounds:

```text
wav or ogg loaded as Sound
```

Long music/ambience:

```text
ogg loaded as Music stream
```

Gameplay systems must emit events or audio requests. They should not call raylib audio directly.

Allowed:

```text
WeaponSystem emits Fired
AudioSystem receives Fired/AudioRequest
AudioSystem plays revolver_fire.wav
```

Forbidden:

```text
WeaponSystem calls Raylib.PlaySound directly
```

---

## 7. Font pipeline

Runtime font path:

```text
data/fonts/hud_unicode.ttf
```

Current recommended font:

```text
GNU Unifont TrueType
```

Reason:

```text
Latin
Cyrillic
HUD symbols: ♥ ❤ ▰ ■ □ ◆ ◇ ✦
wide Unicode BMP coverage
retro pixel-ish style
```

OUT RayMicro loads a selected glyph set:

```text
Basic Latin
Cyrillic
HUD symbols
arrows
```

If the font is missing, the engine falls back to raylib default font and the HUD may show boxes instead of hearts. This is not a mystery. It is font coverage doing what font coverage does: disappointing people with geometry.

---

## 8. First implementation target

```text
1. Put data/fonts/hud_unicode.ttf into place.
2. Add a Blender export script skeleton.
3. Create test_room.blend.
4. Export test_room.glb and test_room.outmap.json.
5. Add OutmModelCache.
6. Add OutmMapLoader.
7. Replace hardcoded box room rendering with loaded GLB.
8. Keep box collision until triangle collision exists.
```
