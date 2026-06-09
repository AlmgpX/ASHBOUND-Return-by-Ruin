# OUT_SCALE_DEBUG_TABLES

## Purpose

OUT_ASHBOUND is not allowed to remain a cute toy loop over a few objects. The standalone OUT_CORE mini must be designed for thousands of runtime objects and thousands of events while staying lighter than the Unity OUT CORE Lite host.

This document defines the next kernel target.

## Canon constraints

Follow the same OUT grammar:

```text
Entity / Def / Runtime / Command / Event / Effect / Condition / OutputLink / Table / Scheduler / Save / Console / Validator
```

No second event bus. No second command bus. No hidden update brains. No renderer-owned gameplay state. No UI as truth.

## Performance targets

```text
1k entities       must remain trivial
10k entities      must remain debuggable
100k events run   must not allocate per event
1k events/tick    must be budgeted and observable
```

## OUT_Table v2

Replace naive list scanning with slot storage.

```text
OUT_Id = index + generation
slots[]
freeList
activeCount
idToSlot
scope index
position grid index
className index
targetName index
tag index or tag bitset
```

Required operations:

```text
Create(def, pos, scope)
Drop(id)
Get(id)
Move(id, oldPos, newPos)
QueryCell(pos, scope, buffer)
QueryRadius(pos, radius, scope, buffer)
QueryClass(className, buffer)
QueryTargetName(targetName, buffer)
CopyAll(buffer)
```

No LINQ in hot simulation paths.

## OUT_EventQueue v2

Use a ring buffer with a fixed capacity and diagnostics.

```text
capacity
head
tail
count
droppedCount
maxPerFlush
lastEventsDebugRing
```

Event structure should be compact:

```text
Type
SourceId
TargetId
KeyId or string key for debug build
IntValue
FloatValue
Pos
Tick
```

Flush model:

```text
while count > 0 and processed < budget:
    dispatch event
```

Overflow policy:

```text
DropOldest
DropNewest
ThrowInDebug
```

Default for dev: `ThrowInDebug`.
Default for release: `DropOldest + counter`.

## Scheduler lanes

Standalone OUT mini should inherit Lite cadence thinking, but remain cheaper.

```text
full        0.05
logic       0.10
ai_near     0.10-0.15
ai_mid      0.35-0.60
ai_far      1.0-2.0
random      0.25 with budget
world       0.25-0.30
collective  2.0-4.0 optional
ambient     4.0-8.0 optional
```

The scheduler must expose debug data:

```text
lane name
interval
budget
processed
skipped
last duration ticks
queue size
```

## Debug tables

Debug is a first-class kernel projection.

### OUT_DebugWorldTable

```text
turn
mode
entityCount
eventQueued
eventDropped
loopCount
worldMemory
residue
activeSectorCount
activeLocalObjects
```

### OUT_DebugEntityTable

```text
id
generation
defKey
className
targetName
scope
sector
pos
hp
state
flags
inventoryCount
tickLane
tickInterval
```

### OUT_DebugPlayerTable

```text
hp
stamina
inventory
worldPos
localPos
currentTile
shards
loops
memory
residue
lastCommand
lastDamageSource
```

### OUT_DebugEventTable

```text
tick
type
source
target
key
payload
```

Renderer may display debug tables, but renderer must not own them.

## Console commands

```text
out.stats
out.debug world
out.debug entities
out.debug entity <id>
out.debug player
out.debug events
out.tick
out.tick <lane> <interval>
out.budget <lane> <count>
out.spawn <def> <x> <y>
out.kill <id>
out.damage <id> <value>
out.save
out.load
```

## Save direction

Phase 1: JSON for readability.

Phase 2: optional binary snapshot for large saves.

Stable save identity must use:

```text
stableId if present
defKey
className
targetName
scope
pos
state payload
inventory payload
```

Runtime id is session-local and must not be the only stable identity.

## Next implementation prompt

```text
Refactor OUT_ASHBOUND toward scalable OUT CORE mini.
Implement OUT_Table v2 with slot storage, generation ids, free list, position index and class/target indexes.
Implement OUT_EventQueue ring buffer with budgeted flush and debug counters.
Add OUT_DebugTables projection for world, entities, player and events.
Add OUT_Console commands for debug tables and tick/budget tuning.
Remove LINQ from hot simulation paths.
Keep all file/class prefixes OUT_.
Do not introduce a second event bus, command bus, save system or gameplay ontology.
```
