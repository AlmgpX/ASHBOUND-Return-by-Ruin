# OUTL Save Load Quick Guide

OUT CORE Lite uses the existing `OUTL_World.Save` system. Do not add another save manager.

## Menus

- `OUT CORE Lite/Save/Quick Save OUTL Runtime`
- `OUT CORE Lite/Save/Quick Load OUTL Runtime`
- `OUT CORE Lite/Save/Validate Runtime Save Roundtrip`

Quick save/load uses `OUTL_SaveSystem.DefaultPath`.

## What Saves

Entity records save stable id, class/target names, tier, transform, stats, state flags and component payloads.

Component participants currently include:

- `OUTL_NPCBehaviorController` schedule, route, tier, interrupt and stimulus state
- `OUTL_DeathRuntime` death state
- `OUTL_InventoryRuntime` known item counts
- `OUTL_EquipmentRuntime` equipped slots
- existing gameplay loop/dropper participants

## Testing

Run `OUTL_NPCWorldSmokeRunner`.

Expected checks:

- pickup adds item to inventory
- dead NPC stops attack/nav/behavior
- save/load restores NPC travel runtime
- save/load restores dead runtime
- save/load restores inventory count when `OUTL_InventoryRuntime.KnownItems` is authored
