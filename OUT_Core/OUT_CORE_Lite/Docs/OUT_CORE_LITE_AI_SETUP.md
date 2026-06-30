# OUT CORE Lite AI Module Note

This file is subordinate to:

```text
Docs/OUT_CORE_LITE.md
```

If this file conflicts with the canon, the canon wins.

---

## Scope

This document describes the optional abstract AI module.

It may describe reusable roles:

```text
Agent
Sensor
Memory
Schedule
Task
Intent
TargetSelector
LocomotionBridge
CombatBridge
SquadOrder
CoverPoint
```

It must not define content-specific creatures, story factions, named encounters or scene-specific bug lore. Content presets belong in content folders or content notes.

---

## AI module rule

AI must speak OUTL grammar:

```text
perception/signal/event
  -> runtime memory/state
    -> schedule/intent decision
      -> OUTL_Command
        -> OUTL_Event / OUTL_Effect
```

AI must not directly control unrelated gameplay scripts through hard references when an OUTL command/event can express the same action.

---

## Canonical setup pattern

```text
Actor entity
  OUTL_EntityAdapter
  AI profile/def
  perception module
  memory/state module
  schedule/intent resolver
  locomotion bridge
  action bridge
  optional squad/faction module
```

All mutable runtime state needs inspection and save policy if persistent.

---

## Testing checklist

```text
create abstract AI test actor
verify entity registration
verify perception/state change
verify command emission
verify receiver handles command
verify debug/console inspection
verify validator warnings are actionable
```

---

Detailed presets may exist, but they are examples only. The canon remains `Docs/OUT_CORE_LITE.md`.
