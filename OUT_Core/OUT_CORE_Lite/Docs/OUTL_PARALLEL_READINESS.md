# OUTL Parallel Readiness

This pass adds a data snapshot/result boundary for future Jobs/Burst work. It does not move gameplay to jobs yet and does not change runtime behavior.

## Main Thread

Unity object access stays on the main thread:

- `OUTL_Registry` reads
- `OUTL_AIActor` reads
- `OUTL_EntityRuntime` reads/writes
- `Transform` position/forward reads
- Physics, line of sight, overlap queries
- NavMesh movement and repath
- Animator and animation bridge calls
- Audio, VFX, pooling, spawn/despawn

`OUTL_ParallelReadiness.BuildSnapshotFromRegistry` and `BuildSnapshotFromEntities` copy these values into data-only rows.

## Data Rows

The job-ready rows are:

- `OUTL_ActorSnapshotRow`
- `OUTL_ActorTierResultRow`
- `OUTL_AIStimulusScoreRow`
- `OUTL_AIDecisionResultRow`
- `OUTL_AIDebugTableRow`

Rows use ids, enums, flags, floats, and `Vector3`. They do not store `GameObject`, `MonoBehaviour`, `Transform`, `Animator`, `AudioSource`, `NavMeshAgent`, `Collider`, or `ScriptableObject` references.

## First Job Candidates

Safe candidates after snapshotting:

- tier distance calculation
- stimulus score decay
- simple AI decision scoring
- debug table row preparation
- lightweight spatial bucketing once the data is copied out of Unity objects

`OUTL_ParallelReadiness.CalculateTierResults` is the first pure C# prototype. It reads snapshot rows, focus position, and processing thresholds, then writes tier result rows. It does not access Unity objects, transforms, registry, physics, or navmesh.

## Result Boundary

`OUTL_ParallelReadiness.ApplyResultsMainThread` is intentionally no-op in v0.1. Future jobs should write result rows only. The main thread will later consume those rows and apply tier, AI state, goal, or command changes through existing OUTL systems.

## Current Blockers

- perception still uses Physics raycasts
- movement still uses Transform/NavMesh
- animation and audio must stay on the main thread
- AI schedules still hold object and asset references
- target acquisition still reads registry/sector data directly

## Migration Order

1. Keep gameplay behavior on the main thread.
2. Snapshot actors into data rows.
3. Run pure tier/stimulus/decision scoring on rows.
4. Apply results on the main thread through existing OUTL systems.
5. Replace the pure C# loops with Jobs only after the row contracts are stable.
