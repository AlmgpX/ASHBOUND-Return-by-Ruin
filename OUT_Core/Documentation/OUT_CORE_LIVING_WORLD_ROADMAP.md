# OUT CORE Living World Roadmap

This file is a subordinate module roadmap.

Canonical architecture lives in:

```text
Docs/OUT_CORE_LITE.md
```

If this file conflicts with the canon, the canon wins.

---

## Scope

This document covers optional large-world and long-running simulation modules.

It is not kernel law. It must not introduce content-specific entities, named species, named factions, named scenes or project-specific debug incidents into core architecture.

The large-world module may define abstract systems only:

```text
Sector
Chunk
Region
Zone
WorldCell
EntitySnapshot
EventJournal
PersistenceLayer
RelevancePolicy
ProcessingTier
StreamingBoundary
SimulationClock
```

---

## Strategic direction

OUT CORE large-world architecture should remain a layered system:

```text
Unity authoring / presentation / editor tooling
  -> Unity bridge adapters
    -> OUTL kernel grammar
      -> simulation modules
        -> persistence / journal / streaming
          -> optional replication boundary
```

Unity remains the host. The simulation module must not make Unity scenes the source of gameplay truth.

---

## Module responsibilities

The living-world module may own:

```text
world partitioning
chunk/sector activation
simulation distance tiers
persistent entity snapshots
event journals
spawn/despawn policies
long-running state updates
module-level debug overlays
large-world validation
```

It may not own:

```text
kernel entity identity
kernel command routing
kernel event routing
core save identity law
content-specific behavior
content-specific lore
```

---

## Processing model

Processing must be tiered and budgeted:

```text
Full    active local simulation
Near    frequent simplified simulation
Mid     reduced scheduled simulation
Far     rare abstract simulation
Dormant stored snapshot only
```

Tier changes must flow through OUTL runtime state and scheduler policy. They must not be hidden inside isolated MonoBehaviour updates.

---

## Persistence model

Large-world persistence must use stable identity:

```text
StableId
ClassName
TargetName where addressable
Def id/name where available
chunk/sector coordinate
serialized module payloads
last simulation timestamp
```

Runtime ids are session-local only.

---

## Event journal

Long-running modules should store meaningful facts as events, not as a pile of invisible side effects.

Abstract event examples:

```text
EntitySpawned
EntityDespawned
EntityDamaged
EntityStateChanged
ResourceChanged
ZoneStateChanged
ObjectiveChanged
SignalObserved
```

Content-specific event names belong in content modules, not the core roadmap.

---

## Development order

```text
1. Keep the kernel stable.
2. Add sector/chunk identity.
3. Add processing tiers.
4. Add snapshot persistence.
5. Add event journal.
6. Add debug view and validator.
7. Only then add specialized simulation modules.
```

---

## Hard ban

Do not use this roadmap as permission to add content-specific AI, creature behavior, faction lore or scene repair notes to core docs.

The living-world module is an abstraction layer. Content is content. Miraculous, apparently.
