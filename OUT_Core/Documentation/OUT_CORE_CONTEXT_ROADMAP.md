# OUT CORE Context Roadmap

This file is now a subordinate index.

Canonical architecture lives in:

```text
Docs/OUT_CORE_LITE.md
```

If this file conflicts with the canon, the canon wins.

---

## Purpose

OUT CORE is the larger research and production space around reusable gameplay architecture.

OUT CORE Lite is the small canonical kernel inside that space.

This roadmap must not contain project-specific creatures, named factions, named scenes, named story beats, named test jokes or one-off debugging incidents. Those belong in content documentation, scene notes or issue tickets. Core documentation is for abstractions only. Astonishing that this has to be said, but here we are.

---

## Stable principle

Unity is the host, not the architecture.

OUT CORE owns reusable gameplay contracts:

```text
entity identity
commands
events
effects
conditions
signals
state
save/load
scheduling
inspection
validation
simulation modules
```

Unity provides:

```text
rendering
physics integration
audio
input bridge
editor tooling
scene/prefab authoring
build pipeline
```

---

## Documentation hierarchy

```text
Docs/OUT_CORE_LITE.md
  Canon. Architectural law.

Docs/CODEX_OUT_CORE_LITE_MASTER_PROMPT.md
  Work protocol. Must obey canon.

Docs/OUT_CORE_LITE_ITERATION_PROTOCOL.md
  Iteration process. Must obey canon.

Docs/OUT_CORE_LITE_GUIDE.md
  User-facing setup notes. Must obey canon.

Docs/OUTL_*.md
  Module notes. Must not redefine kernel ontology.

Assets/OUT/OUT_Core/Documentation/*.md
  Legacy/research notes. Must defer to canon.
```

---

## Abstract roadmap

### Phase 0: Canon and compile stability

```text
one canonical architecture document
no duplicate framework roots
no content-specific rules inside core docs
project compiles after every change
```

### Phase 1: Kernel stabilization

```text
EntityId
EntityRuntime
Registry
StateBag
StatBlock
CommandSystem
EventBus
EffectSystem
OutputLink
Scheduler interfaces
Console inspection
Validator basics
```

### Phase 2: Unity bridge cleanup

```text
thin world host
thin entity adapter
scene bootstrap
pool/spawn bridge
Transform/Physics/Audio/UI bridge boundaries
```

### Phase 3: Logic module

```text
interactable source
receiver
button
trigger
relay
multi-source gate
multi-command fanout
mover/door/platform abstraction
```

### Phase 4: Persistence module

```text
StableId workflow
component save participants
spawn resolver
save validation
save inspection
```

### Phase 5: Simulation modules

Optional modules may be built only through the kernel grammar:

```text
AI
combat
inventory/equipment
world state
chunk processing
world generation
UI binding
game loop/reward/challenge authoring
network seams
```

Modules are not allowed to bypass commands/events/effects or invent a second entity ontology.

---

## Rules for future work

Before writing code:

```text
1. Read Docs/OUT_CORE_LITE.md.
2. Search existing OUT_ and OUTL_ systems.
3. Identify whether the change belongs to Kernel, UnityBridge, Module or Content.
4. Do not create duplicate managers.
5. Do not put content names in core class names or core docs.
6. Keep the change inside the smallest possible layer.
7. Update docs only if the architecture actually changes.
```

---

## Content quarantine

Content-specific behavior belongs under content folders and content docs.

Core may define:

```text
Actor
Agent
Sensor
Memory
Need
Faction
Squad
Target
Danger
Resource
Interactable
Objective
```

Core may not define named creatures, story factions or level-specific incidents.

---

End of index. Read the canon before summoning more architecture demons.
