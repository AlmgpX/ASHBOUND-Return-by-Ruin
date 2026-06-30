# OUTL AI Stimulus Guide

Stimuli are lightweight facts stored by `OUTL_StimulusBus` and consumed by sensors.

Core fields:

```text
Type, Source, Position, Radius, Strength, Confidence, Priority, DecayTime, Key, Tags
```

Use `OUTL_StimulusBus.Emit(...)` for sound, combat, danger, resource, social, scripted and other local facts. The bus keeps sector-indexed storage, supports radius queries, and removes expired rows under `OUTL_World` stimulus budget.

## Sensors

`OUTL_StimulusSensor` is scheduler-driven and feeds `OUTL_AIActor.ReceiveStimulus`.

Modes:

```text
Hearing
Vision
Threat
Territory
```

`OUTL_HearingSensor` remains supported for direct hearing subscriptions.

## AI Memory

`OUTL_AIActor` tracks:

```text
LastKnownTargetPosition
LastStimulusTime
Suspicion
MemoryFear
MemoryAggression
AllegianceInfluence
FactionInfluence
```

Stimuli interrupt schedules through the existing AI actor path. No hidden Update brain is added.
