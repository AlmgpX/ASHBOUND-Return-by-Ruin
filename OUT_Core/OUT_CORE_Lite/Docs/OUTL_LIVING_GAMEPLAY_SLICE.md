# OUTL Living Gameplay Slice

Short usage note for the abstract living gameplay playground.

## Create

Use:

```text
OUT CORE Lite/Actors/Nature/Create Living Gameplay Playground
```

This editor-only workbench creates:

- OUTL runtime root with World, Pool, QuickSave and DevConsole.
- Controlled player actor from the foundation prefab.
- Three Basic Sheep actors.
- Five grass/food resource sources.
- One predator/danger stimulus dummy.
- One local egregore source.

## Runtime Flow

Living actors still use the canonical OUT CORE Lite stack:

```text
StimulusBus + WorldLedger/Egregore
  -> OUTL_DriveRuntime action scoring
  -> OUTL_NPCBehaviorController current action/target
  -> OUTL_NavMeshMover / ActorControlBridge
```

No SheepController, AnimalManager, NeedsManager, second AI path or runtime object construction is used.

## Sheep Checks

Basic Sheep supports:

- Wander when calm.
- Flee from damage, danger, fear and combat stimuli.
- FindFood from Food/Grass resource stimuli.
- Eat nearby living resources and reduce hunger.
- Rest to reduce fatigue.
- WasteDrop through the canonical pool/pickup path.
- Abstract reproduction through pending offspring/events, not immediate runtime actor construction.
- Death/drop through Vitals, DamageReceiver, DeathHandler and LootDropper.

## Validate

Use:

```text
OUT CORE Lite/Actors/Nature/Validate Basic Sheep
OUT CORE Lite/Actors/Nature/Validate Living Gameplay Playground
```

In Play Mode, validation additionally checks runtime ids and current drive action state.
