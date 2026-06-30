# OUT RayMicro logic tick scheduler

Purpose: separate gameplay logic frequency from render frames and from the global fixed simulation tick.

A large level cannot tick every actor, pickup, ambience brain, plant, door, trap, particle controller and decorative rat at 60 Hz just because the player exists somewhere on the same planet. That is how engines become space heaters with UI.

---

## The three clocks

OUT RayMicro now treats time as three different concerns:

```text
Render frame
  variable, visual only

Fixed tick
  stable 60 Hz simulation base

Logic tick policy
  per-entity / per-chunk frequency selection
```

The fixed tick remains the base clock. The logic scheduler decides which systems/entities/chunks actually need work on a given fixed tick.

---

## Core idea

```text
near player     -> tick often
mid distance    -> tick sometimes
far distance    -> tick rarely
dormant         -> almost never / event-only
```

Current seed:

```text
OUT_RayMicro/src/Core/OutmLogicTickScheduler.cs
```

Main types:

```text
OutmLogicTickTier
OutmLogicTickPolicy
OutmLogicTickDecision
OutmChunkKey
OutmLogicTickScheduler
```

Current default policy:

```text
NearDistance    24m     tick every 1 fixed tick
MidDistance     64m     tick every 4 fixed ticks
FarDistance     128m    tick every 16 fixed ticks
Dormant         beyond  tick every 60 fixed ticks
```

---

## RandomTickSpeed meaning

Do not use global nondeterministic RNG for this. That would poison replay, save debugging and future multiplayer because apparently chaos is bad when it is not in the plot.

OUT uses deterministic phase spread:

```text
phase = stable hash(entity/chunk id) % spread
shouldTick = (worldTick + phase) % interval == 0
```

This gives the important effect of random tick speed:

```text
objects wake on different ticks
large groups do not spike on the same frame
simulation stays replay-friendly
```

In the future, if we want Minecraft-style random block updates, that becomes a separate seeded random system:

```text
ChunkRandomTickSystem
seeded by map id + chunk key + world tick
budgeted by RandomTicksPerChunk
```

Do not mix that with base logic scheduling.

---

## Chunk handler target

Chunks are not just streaming containers. They are logic scheduling units.

Future chunk state:

```text
ChunkKey
Bounds
Loaded / Resident / Active / Sleeping
TickTier
Entity range/list
Static collision refs
Mesh refs
Audio zone refs
Nav refs
```

Chunk update pattern:

```text
for each loaded chunk:
  decision = LogicTickScheduler.DecideChunk(chunk, playerPosition, worldTick)
  if decision.ShouldTick:
    tick chunk systems at tier frequency
```

Entity update pattern:

```text
for each active entity in chunk:
  decision = LogicTickScheduler.DecideEntity(entity, transform.position, playerPosition, worldTick)
  if decision.ShouldTick:
    tick actor/item/trap/ambience logic
```

---

## What must remain always-tick

Some systems do not get distance throttled:

```text
player motor
player weapon input
camera/view state
current collision contacts around player
audio stream update
critical projectiles near player
currently interacting objects
scripted set pieces explicitly marked Always
```

If an explosive barrel is far away and unobserved, it can sleep. If the player is staring at it, shooting it, or it is in a chain reaction, it wakes. This is called sanity. Rare, but useful.

---

## Def-level controls later

Future JSON fields:

```json
{
  "logic": {
    "tier": "auto",
    "nearEveryTicks": 1,
    "midEveryTicks": 4,
    "farEveryTicks": 16,
    "dormantEveryTicks": 60,
    "randomPhaseSpreadTicks": 8,
    "forceAlwaysWhenVisible": true,
    "forceAlwaysWhenAggro": true
  }
}
```

For actors:

```json
{
  "id": "actor.zombie.basic",
  "logicProfile": "enemy.default"
}
```

For decorative logic:

```json
{
  "id": "ambience.flicker_light",
  "logicProfile": "ambient.low_frequency"
}
```

---

## Integration order

```text
1. Scheduler seed exists. Done.
2. Add ChunkStore.
3. Assign OUTMAP boxes/meshes/triggers to chunk keys.
4. Add ActiveChunkSet around player.
5. Move pickups/actors/traps into entity stores.
6. Apply logic scheduler in PickupSystem / ActorSystem / AmbientSystem.
7. Add seeded ChunkRandomTickSystem for low-priority world simulation.
```

---

## Rule

Fixed tick is the heartbeat.
Logic tick policy is the nervous system.
Render frame is just the face pretending everything is fine.

Do not tie gameplay truth to FPS. Do not tick the entire world at full rate because it is easy. Easy is how codebases become landfill with a version number.
