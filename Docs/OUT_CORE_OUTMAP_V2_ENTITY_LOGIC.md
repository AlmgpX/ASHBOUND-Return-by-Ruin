# OUT CORE: OUTMAP v2 entity logic layer

OUTMAP v2 adds an optional entity lump. The old map fields still work. The new layer lets the map describe gameplay wiring.

---

## Entity lump

Example:

```json
"entities": [
  {
    "id": "relay.demo",
    "class": "LogicRelay",
    "targetName": "relay.demo",
    "outputs": [
      { "event": "OnTrigger", "target": "door.main", "input": "Toggle", "delay": 0.0 }
    ]
  }
]
```

Loaded by:

```text
OutmMapEntitySidecar
```

Compiled by:

```text
OutmMapEntityCompiler
```

Executed by:

```text
OutmMapLogicSystem
```

---

## New files

```text
OUT_RayMicro/src/World/OutmMapEntityDef.cs
OUT_RayMicro/src/World/OutmMapEntitySidecar.cs
OUT_RayMicro/src/Gameplay/OutmLogicTypes.cs
OUT_RayMicro/src/Gameplay/OutmMapLogicRuntime.cs
OUT_RayMicro/src/Gameplay/OutmMapEntityCompiler.cs
OUT_RayMicro/src/Gameplay/OutmMapLogicSystem.cs
```

---

## Classes

Current schema includes:

```text
Worldspawn
InfoPlayerStart
FuncDoor
FuncButton
TriggerOnce
TriggerMultiple
TriggerHurt
TriggerChangeLevel
LogicRelay
LogicTimer
LogicCounter
ItemPickup
LightPoint
AmbientSound
PropStatic
PropDynamic
```

Only part of this list has behavior now. The rest is schema for the editor and future runtime passes.

---

## Inputs

Current inputs:

```text
Trigger
Enable
Disable
Toggle
Open
Close
Remove
Damage
ChangeLevel
Increment
Reset
```

Implemented now:

```text
FuncDoor.Open
FuncDoor.Close
FuncDoor.Toggle
FuncDoor.Trigger
LogicRelay.Trigger
Enable
Disable
ChangeLevel request log
```

---

## Output flow

Output format:

```json
{
  "event": "OnUse",
  "target": "relay.demo",
  "input": "Trigger",
  "parameter": "",
  "delay": 0.0,
  "once": false
}
```

Runtime flow:

```text
source event
  -> output
    -> delayed command
      -> target input
        -> runtime store mutation
```

Example:

```text
trigger.door.main OnUse
  -> door.main Toggle
```

Relay chain:

```text
button.demo OnUse
  -> relay.demo Trigger
    -> door.main Toggle
```

---

## Legacy bridge

Existing OUTMAP fields are compiled into logic runtime:

```text
doors[]    -> FuncDoor logic entities
triggers[] -> TriggerMultiple logic entities with OnUse output
```

Legacy door triggers stay use-only. They do not run on enter.

---

## Next editor layer

Next useful editor step:

```text
Entity list
Property inspector
Outputs editor
Create entity
Delete entity
Move selected entity
Save OUTMAP
```

Minimum target:

```text
select trigger
edit target
edit output event/input
save OUTMAP
run map
press E
logic fires
```
