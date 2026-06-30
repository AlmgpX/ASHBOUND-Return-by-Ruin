# OUTL Performance Checklist

## Lifetime

- Spawn gameplay objects through `OutCore.pool.OUT` or `OUTL_World.Spawn`.
- Despawn entities through `OUTL_World.Despawn` or `OUT.Destroy`.
- Prewarm repeatable prefabs.
- Check `OUTL_PoolStats` for double release and fallback instantiate counters.

## AI

- Use runtime tiers.
- Use `OUTL_StimulusSensor` or `OUTL_HearingSensor`.
- Keep target acquisition sector-based.
- Avoid `FindObjectOfType`, `GameObject.Find`, LINQ and repeated `GetComponent` in tick methods.

## Sectors

- Run Sector Integrity Validator after pooled respawn or heavy scene repair.
- Watch worst sector density in `OUTL_SectorGridDebugView`.

## Stimuli

- Keep `MaxStoredStimuli`, `MaxStimuliPerSector`, and `MaxStimuliProcessedPerFrame` bounded.
- Prefer local radius queries through sensors.

## Egregores

- Use scheduler intervals:
  - local: 0.5 to 2s
  - regional: 2 to 5s
  - world: 5 to 15s
- Bind to sectors instead of scanning all entities.

## Scanner

Use:

```text
OUT CORE Lite -> Workbench -> Runtime Code Scanner
```

It reports construction/search/hot-path hits for manual migration.
