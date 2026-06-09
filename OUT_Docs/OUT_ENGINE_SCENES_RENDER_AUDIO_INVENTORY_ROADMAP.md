# OUT_ENGINE_SCENES_RENDER_AUDIO_INVENTORY_ROADMAP

## Goal

OUT_ASHBOUND is a game made on top of OUT_CORE_MINI.
OUT_CORE_MINI is the engine layer.

The project must teach the author how to create scenes, generate scenes procedurally, attach rendering/audio backends, build inventory, and later move toward 3D without destroying the OUT grammar.

## Hard separation

```text
OUT_Core        engine kernel
OUT_Content     game data / defs / scenes / maps / items
OUT_Rendering   replaceable render backends
OUT_Audio       replaceable audio backend
OUT_Tools       scene authoring / generation tools
OUT_Game        Ashbound-specific gameplay module
```

Core must not contain named Ashbound lore.
Game content may contain Crownbound, Lirael, Noxara, Crown, Ruin, etc.

## Scene system

A scene is data, not code.

```text
OUT_SceneDef
  key
  displayName
  kind: world | local | interior | dungeon | battle | dialogue | cutscene
  seed
  width
  height
  tileSet
  generator
  spawnTable
  exits
  triggers
  ambient
  music
```

Scene runtime:

```text
OUT_SceneRuntime
  sceneKey
  map
  activeObjects
  localEventQueue
  discoveredFlags
  generationState
```

Required scene APIs:

```text
OUT_SceneSystem.Load(sceneKey)
OUT_SceneSystem.Unload(sceneKey)
OUT_SceneSystem.Enter(exitKey)
OUT_SceneSystem.Generate(sceneDef, seed)
OUT_SceneSystem.SaveSceneState()
OUT_SceneSystem.RestoreSceneState()
```

No scene-specific script gods.

## Procedural generation

Generation is a pipeline of data-driven passes.

```text
OUT_GenPipeline
  pass 0: allocate grid
  pass 1: base terrain
  pass 2: height/noise/biome
  pass 3: rivers/roads
  pass 4: rooms/caves/ruins
  pass 5: exits
  pass 6: spawn points
  pass 7: resources
  pass 8: validation
```

Generator defs:

```text
OUT_GeneratorDef
  key
  algorithm: cellular | drunkard | bsp | wfc | noise | handauthored_plus_noise
  seedPolicy
  passes[]
  constraints[]
```

Validation examples:

```text
player start exists
all required exits reachable
no blocking object on non-walkable tile
required resources can spawn
boss/key/gate constraints satisfied
```

## Learning path for scenes

```text
Lesson 1: hand-authored JSON scene
Lesson 2: local procedural forest
Lesson 3: dungeon rooms + corridors
Lesson 4: world map with travel costs
Lesson 5: scene exits and backtracking
Lesson 6: resource distribution
Lesson 7: trigger/event/command links
Lesson 8: save/restore scene state
Lesson 9: debug scene table
Lesson 10: procedural generation profiles
```

## Renderer backends

Renderer is a projection over OUT_State. It must never mutate gameplay.

```text
OUT_IRenderBackend
  Init(window/settings)
  Render(frame)
  Shutdown()
```

Render layers:

```text
WorldTiles
Objects
Particles
UI
DebugTables
Console
```

### Backend phase plan

```text
v0 console_ascii      current fallback
v1 raylib_ascii       windowed glyph renderer
v2 raylib_sprite2d    tiles, portraits, inventory UI, particles
v3 raylib_hybrid      ASCII + sprites + post effects
v4 silk3d_experiment  optional low-level 3D backend
```

Recommended first graphical library: Raylib-cs.

Reason:

```text
small
C# binding exists
window + input + 2D + 3D + audio in one stack
fits the OUT "host library, not full engine" idea
fast enough for learning and Steam prototype
```

Silk.NET is the later low-level path for serious custom 3D rendering, Vulkan/OpenGL/compute/windowing/input/audio control.
MonoGame is the safer classic C# framework option if the project becomes mostly 2D and content-pipeline heavy.

## Audio system

Audio listens to events. It does not own gameplay.

```text
OUT_AudioSystem
  LoadBank(audioDef)
  OnEvent(event)
  PlayMusic(key)
  StopMusic(key)
  PlaySfx(key, position)
  SetBusVolume(bus, value)
```

Audio defs:

```text
OUT_AudioBankDef
  music[]
  sfx[]
  ambience[]
  eventBindings[]
```

Event binding example:

```text
EventType: Damage      -> hit.wav
EventType: ShardTaken  -> shard_pickup.wav
EventType: GateOpened  -> gate_open.ogg
Scene: forest          -> forest_ambience.ogg
```

## Inventory

Inventory belongs to runtime state, not UI.

```text
OUT_ItemDef
  key
  name
  tags
  stackLimit
  weight
  icon
  equipSlot
  effectsOnUse[]
  effectsOnEquip[]
```

Runtime:

```text
OUT_Inventory
  ownerId
  slots[]
  currency
  capacity
```

Commands:

```text
OUT_CommandType.AddItem
OUT_CommandType.RemoveItem
OUT_CommandType.UseItem
OUT_CommandType.EquipItem
OUT_CommandType.DropItem
OUT_CommandType.TransferItem
```

Events:

```text
ItemAdded
ItemRemoved
ItemUsed
ItemEquipped
ItemDropped
InventoryChanged
```

UI only reads inventory and sends commands.

## 3D path

Do not start with 3D. Prepare for it.

3D-compatible assumptions now:

```text
use OUT_Pos2 for grid scenes
introduce OUT_Pos3 later for 3D scenes
scene/entity runtime must not depend on console coordinates
renderer receives abstract render frame
physics is a backend/module, not core truth
```

3D phase plan:

```text
phase 1: 2D glyph/sprite scenes in Raylib-cs
phase 2: 2.5D tile height / fake depth
phase 3: Raylib 3D models/camera experiment
phase 4: split render backend interface cleanly
phase 5: Silk.NET backend experiment if real custom renderer is needed
```

## Debug tables to support authoring

```text
out.debug scene
out.debug gen
out.debug objects
out.debug inventory
out.debug render
out.debug audio
```

Scene debug table:

```text
sceneKey
seed
size
generator
objectCount
exitCount
triggerCount
reachablePercent
lastValidationError
```

Inventory debug table:

```text
ownerId
slotsUsed
capacity
weight
items
lastCommand
```

## Next implementation order

```text
1. Finish compile stability.
2. Add OUT_Inventory and OUT_ItemDef.
3. Add OUT_SceneDef JSON and OUT_SceneSystem.
4. Move current world/local maps into scene defs.
5. Add procedural generator pipeline.
6. Add debug tables.
7. Add Raylib-cs backend as optional project/compile flag.
8. Add event-driven audio bank.
9. Add sprite inventory UI.
10. Add 3D experiment only after renderer boundary is clean.
```

## Non-negotiable law

```text
Content can be weird.
Core must stay abstract.
Renderer is projection.
Audio is listener.
UI sends commands.
Scene is data.
Generation is pipeline.
Save is memory.
Debug is a first-class engine surface.
```
