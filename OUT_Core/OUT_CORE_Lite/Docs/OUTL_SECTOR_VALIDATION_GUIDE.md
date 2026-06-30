# OUTL Sector Validation Guide

`OUTL_SectorGrid` is the spatial index used by processing, perception and diagnostics.

Use:

```text
OUT CORE Lite -> Workbench -> Sector Integrity Validator
```

Checks:

```text
registry entity missing sector
sector entry missing registry runtime
duplicate sector entries
stale sector address
worst sector density
```

The validator can call `RebuildSectorIndexSafe()` for editor/runtime diagnostics. This rebuild is not a new gameplay path; it repairs the existing sector index from `OUTL_World.Registry`.

Runtime overlay:

```text
OUTL_SectorGridDebugView
```

It shows sector count, indexed entities, tier counts, average entities per sector and worst sector density.
