# OUT CORE Lite Canon

Canonical file path:

```text
Assets/OUT/OUT_Core/OUT_CORE_Lite/Docs/OUT_CORE_LITE.md
```

This is the single source of truth for OUT CORE Lite architecture. Other OUT CORE Lite docs are subordinate notes.

OUT CORE Lite is a compact data-driven gameplay kernel for Unity. Unity is the host. OUT CORE Lite is the gameplay language.

---
## CODEX PROMT 
Read these first and treat them as source of truth in this order:
1. `Assets/OUT/OUT_Core/OUT_CORE_Lite/Docs/OUT_CORE_LITE.md`
2. `Docs/OUT_CORE_LITE_GUIDE.md`
3. `Assets/OUT/OUT_Core/OUT_CORE_Lite/Docs/OUTL_COMBAT_AI_ACTOR_STACK.md`

Then inspect:
- Core: `OUTL_World`, `OUTL_Scheduler`, `OUTL_SectorGrid`, `OUTL_PoolSystem`, `OUTL_IPoolReset`, `OUTL_EntityAdapter`, `OUTL_DefDatabase`, `OUTL_Combat`, `OUTL_Projectile`
- AI: `OUTL_AIActor`, `OUTL_AIPerceptionUtility`, `OUTL_StimulusBus`, `OUTL_HearingSensor`, state/debug/validator related files
- Processing/debug: `OUTL_ChunkProcessingDriver`, `OUTL_ProcessingDistanceDriver`, `OUTL_SceneValidator`, `OUTL_GoldenTestRunner`, `OUTL_GameLoopGoldenTester`
- Heavy OUT CORE reference only if needed for optional living-world module: files under `Assets/OUT/OUT_Core/Simulation/Egregore/*`

Tasks:
1. Build `namespace OutCore.pool` with public static class `OUT`.
2. Add safe overloads for instantiate/destroy on `GameObject` and `Component`.
3. Route managed gameplay lifetime through `OUTL_PoolSystem` and `OUTL_World` release/despawn path.
4. Add migration-safe classification for managed Lite prefabs, foreign pooled prefabs and unmanaged fallback prefabs.
5. Replace repeated hot-path reset discovery with cached reset manifests across root + children.
6. Add physics-safe pooling support for bullets and rigidbody-driven spawned objects without creating a second combat ontology.
7. Extend validators to catch direct runtime Instantiate/Destroy violations in gameplay paths and broken pool/projectile configs.
8. Extend golden tests to cover pooled spawn/release reuse, AI sector acquisition + LOS, hearing stimulus interrupts, chunk tier updates and projectile stale-state reuse bugs.
9. Verify AI tick scaling by runtime tier and keep it externally tunable.
10. Add a Lite-friendly optional collective simulation module inspired by heavy OUT CORE egregore concepts: zone, registry, weighted state, dominant mood, ambient rebroadcast to AI memory/stimulus.
11. Keep the collective layer optional and ontology-safe: forest/city/ruin/faction spirits are content instances, not kernel special cases.
12. Document/update recommended cadence defaults:
   - full lane 0.05
   - logic 0.10
   - AI near 0.10–0.15
   - AI mid 0.35–0.60
   - AI far 1.0–2.0
   - random 0.25 with budget
   - chunk processing 0.25–0.30
   - collective updates 2–4s
   - collective ambient broadcast 4–8s

Hard constraints:
- No duplicate managers, buses or ontologies.
- No hidden Update-brains for authoritative gameplay.
- No content-specific infection of core.
- No runtime Instantiate/Destroy outside low-level pool internals.

Output:
- changed files
- reason for each change
- canon compliance
- Unity test path
- performance notes
- unresolved risks
- next recommended prompts
## CODEX PROMT END 

## 1. Core ontology

```text
Entity       = addressable runtime object
Def          = authoring data
Runtime      = mutable runtime state
Command      = requested action
Event        = observed fact
Effect       = reusable state mutation
Condition    = reusable rule predicate
OutputLink   = event-to-command edge
Registry     = address space
Scheduler    = simulation timing
Save         = persistent memory
Console      = runtime inspection shell
Validator    = architecture guardrail
Pool         = runtime lifetime boundary
```

---

## 2. Layer law

```text
OUTL.Kernel
  Pure gameplay grammar and runtime state.

OUTL.UnityBridge
  MonoBehaviour adapters, scene bootstrap, pooled object bridge, Transform/Physics/Audio/UI bridges.

OUTL.Modules
  Optional reusable gameplay modules. Modules must speak the kernel grammar.

OUTL.Content
  Game-specific definitions, prefabs, scenes and experiments. Content must never redefine the kernel.
```

---

## 3. Canonical runtime flow

```text
Source Event
  -> OUTL_OutputLink
    -> TargetName
      -> OUTL_CommandSystem
        -> OUTL_ICommandReceiver / Def Action / Module
          -> OUTL_EffectSystem
            -> OUTL_EventBus
```

No parallel gameplay routing.

---

## 4. Pooling law

Runtime object lifetime goes through the OUTL pool/spawn/despawn path.

Required path:

```text
spawn request
  -> OUTL_World or spawn service
    -> OUTL_PoolSystem or canonical pool facade
      -> OUTL_EntityAdapter registration
        -> OUTL_Registry visibility
```

Forbidden in runtime gameplay code:

```text
Object.Instantiate(...)
UnityEngine.Object.Instantiate(...)
new GameObject(...) for gameplay entities
Destroy(...) for ordinary gameplay despawn
AddComponent(...) as gameplay construction policy
Resources.Load(...) during gameplay hot path
direct prefab instantiation from gameplay module code
```

Allowed exceptions:

```text
Editor-only generation tools
scene bootstrap creating the one runtime root
migration/repair tools under Editor
tests that explicitly check construction boundaries
the low-level pool implementation itself
```

Despawning must route through:

```text
OUTL_World.QueueDespawn / Despawn
  -> Registry unregister
    -> Scheduler unregister
      -> Pool release
```

If an object is temporary, repeatable, visual-only, actor-like, pickup-like, row-like or effect-like, it is pooled by default.

---

## 5. Hard bans

These are architecture violations unless the task explicitly says migration glue, legacy repair or editor-only tool:

```text
new Manager class when an OUTL system already exists
second event bus
second command bus
second save system
second entity registry
second UI data path
second AI ontology
second canonical doc
content-specific names in core docs/classes
Unity tags/layers as primary entity identity
scene hierarchy names as primary entity identity
runtime instance ids as save ids
static mutable gameplay state as source of truth
FindObjectOfType / FindObjectsOfType in hot gameplay
GameObject.Find in runtime gameplay
repeated GetComponent in hot loops without caching
LINQ in hot simulation paths
per-tick allocations without a pool/buffer
coroutines as hidden core state machines
Invoke / InvokeRepeating as hidden scheduler
animation events as authoritative gameplay logic
UI state as authoritative gameplay state
synchronous asset loading in gameplay hot path
```

---

## 6. Anti-pattern catalog

### God World

`OUTL_World` owns every domain rule.

Fix: `OUTL_World` hosts kernel services; modules own module policy; content owns content policy.

### Manager Bloom

Every problem creates another manager.

Fix: search existing systems, extend the canonical path, add a module service only with a clear boundary.

### Direct Reference Graph

Inspector references become the gameplay graph.

Fix: use `TargetName + CommandSystem + OutputLink`. Direct references are bridge/editor glue only.

### Runtime Instantiate Soup

Runtime objects are created directly during gameplay.

Fix: use pool first, spawn through canonical facade, release through despawn path.

### Hidden Update Brain

A MonoBehaviour `Update()` owns gameplay decisions.

Fix: use Scheduler lanes, runtime state and commands/events/effects.

### Save by Accident

Mutable gameplay state has no save policy.

Fix: add save participant or mark state explicitly transient.

### UI as Truth

HUD or canvas state decides gameplay.

Fix: UI reads runtime state and sends requests; runtime remains source of truth.

### Content Infection

Specific content enters core docs or classes.

Fix: move it to content docs/assets and replace core wording with abstract roles.

---

## 7. Version gates

```text
v0.1 Kernel: ids, runtime, registry, state, stats, events, commands, effects, output links, scheduler interfaces, console, validator.
v0.2 UnityBridge: thin world host, thin entity adapter, pool/spawn bridge, transform/physics/audio/UI boundaries.
v0.3 Logic Module: button, trigger, relay, multisource, mover abstractions.
v0.4 Save Module: stable ids, component save participants, spawn resolver, save validator.
v0.5 Gameplay Modules: combat, inventory, AI, UI, game loops, chunks, worldgen, network seams.
```

Anything outside the active gate is experimental module work and must not redefine the kernel.

---

## 8. Assistant and Codex rules

Before changing OUT CORE Lite:

```text
read this file
search existing OUTL systems
identify the canonical path
do not create duplicate siblings
keep work inside the active gate unless told otherwise
put content-specific notes outside core docs
do not use runtime Instantiate/Destroy; use pool/spawn/despawn
```

Required report:

```text
changed files
reason for each change
canon compliance
Unity test path
known risks
unfinished work
```

---

## 9. Final rule

No runtime instantiation. No duplicate managers. No hidden state. No content in core. No second canon.

---

## 10. Current Lite compliance tools

Pool facade:

```text
using OutCore.pool;
OUT.Instantiate(prefab, position, rotation);
OUT.Instantiate(prefab, parent);
OUT.Destroy(instance);
OUT.Release(instance);
OUT.Prewarm(prefab, count);
OUT.TryGetPoolStats(out stats);
```

The facade lives in `Core/OUTL_PoolFacade.cs` as `OutCore.pool.OUT` and routes through `OUTL_PoolSystem` or `OUTL_World.Despawn` for registered entities.

Pool diagnostics:

```text
OUTL_PooledInstanceInfo.SourcePrefab
OUTL_PooledInstanceInfo.PoolId
OUTL_PooledInstanceInfo.IsActiveInPool
OUTL_PooledInstanceInfo.IsReleased
OUTL_PooledInstanceInfo.LastSpawnFrame
OUTL_PooledInstanceInfo.LastReleaseFrame
OUTL_PooledInstanceInfo.EntityIdAtSpawn
OUTL_PooledInstanceInfo.DebugSource
```

Double-release is guarded by `IsReleased`, not by Unity collection checks. Unknown/unmanaged releases use `OUTL_PoolFallbackPolicy` and must not run pool reset callbacks automatically.

Known pool limitations:

```text
fallback Instantiate is only a low-level emergency path and increments fallbackInstantiates
unmanaged releases are diagnostic events, not normal gameplay despawn
registered actors should despawn through OUTL_World so registry/scheduler/sector state stays consistent
```

Diagnostics:

```text
OUT CORE Lite/Workbench/Runtime Code Scanner
OUT CORE Lite/Workbench/Sector Integrity Validator
OUT CORE Lite/Workbench/Sector Integrity Window
OUTL_SectorGridDebugView
OUTL_AIStateTableDebugView
OUTL_EgregoreDebugView
```

Optional modules and notes:

```text
OUTL_StimulusStore sector-indexes emitted stimuli while OUTL_StimulusBus preserves the event API
OUTL_TickProfile is a ScriptableObject for logic/AI/quest/stimulus/chunk/egregore cadence defaults
Egregore Lite is an optional aggregate module over events/stimuli; it is not a second AI, quest, faction, spawn or world system
OUTL_DeathRuntime is the one death/lifecycle contract for player, NPC and generic entities
OUTL_NetworkAuthority gates damage, kill, drops, pickup and NPC schedule advancement
OUTL_NPCBehaviorController adds scheduler-driven schedules and abstract travel over OUTL_AIActor
Docs/OUTL_POOL_API_MIGRATION.md
Docs/OUTL_AI_STIMULUS_GUIDE.md
Docs/OUTL_SECTOR_VALIDATION_GUIDE.md
Docs/OUTL_EGREGORE_ARCHITECTURE.md
Docs/OUTL_DEATH_NETWORK_CONTRACT.md
Docs/OUTL_NPC_BEHAVIOR_NAVIGATION.md
Docs/OUTL_ITEMS_PICKUPS_LOOT_GUIDE.md
Docs/OUTL_PERFORMANCE_CHECKLIST.md
Docs/OUTL_CHANGELOG.md
```

Golden checklist additions:

```text
Pool_Facade_SpawnRelease_ReusesInstance
Pool_DoubleRelease_DoesNotDuplicate
Pool_RigidbodyReset_Works
Pool_EntityAdapter_RegistersAndUnregisters
Stimulus_Store_EmitQueryDecay
AI_ReceivesStimulus_FromStore
SectorGrid_RegisterMoveUnregister
Egregore_AggregatesStimuli
Egregore_SendsSignal
TickProfile_AppliesIntervals
```

`OUTL_TickProfile` is the data profile for Lite cadence and budgets. `OUTL_World` can apply it while still keeping runtime services in the existing world/scheduler path. If no profile is assigned, existing serialized world intervals remain authoritative.
