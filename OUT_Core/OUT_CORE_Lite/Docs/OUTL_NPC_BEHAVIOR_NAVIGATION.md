# OUTL NPC Behavior + Navigation

The NPC world layer is a scheduler-driven controller over existing OUTL systems:

```text
OUTL_NPCBehaviorController
  -> OUTL_NPCBehaviorDispatcher for frame budgets
  -> OUTL_AIActor for near/full tactics
  -> OUTL_NPCScheduleDef for daily behavior
  -> OUTL_NPCAbstractNavigator for far/dormant travel
  -> OUTL_StimulusStore for interrupts
```

It is not a second AI brain. `OUTL_AIActor` still owns combat/perception visible state; the NPC behavior controller owns schedule, abstract route progress, saved behavior state and tier cadence.

`OUTL_NPCBehaviorDispatcher` is the shared budget boundary. It round-robins registered controllers and consumes `MaxNpcBehaviorTicksPerFrame`, `MaxNpcRouteUpdatesPerFrame`, `MaxNpcPathRequestsPerFrame` and `MaxNpcStimulusInterruptsPerFrame` from `OUTL_World` so large NPC crowds do not all tick, request paths or process interrupts in the same frame.

Tier cadence comes from `OUTL_TickProfile`:

```text
npcFullInterval     default 0.05
npcNearInterval     default 0.25
npcMidInterval      default 2.00
npcFarInterval      default 10.00
npcDormantInterval  default 60.00
```

Far and dormant NPCs advance `OUTL_NPCBehaviorRuntime.RouteProgress` without forcing a NavMesh path every tick. When the actor becomes near/full, the controller can materialize the transform and let `OUTL_NavMeshMover` resume exact movement.

Routes use a shared world route cache by default. The cache stores start/end sectors plus a compact sector path and route points; controllers can opt into a local cache only for isolated tests.

Stimuli interrupt schedules through `OUTL_NPCStimulusInterruptPolicy`. Typical policies are death/combat/fire -> flee, noise -> investigate, social/resource -> trade/loot/investigate. Completed interrupts resume the previous schedule entry when the policy allows it.

Create generic presets from:

```text
OUT CORE Lite/Legacy Demo/Workbench/Create Merchant Traveler Preset
OUT CORE Lite/Legacy Demo/Workbench/Create Bandit Patrol Preset
OUT CORE Lite/Legacy Demo/Workbench/Create Guard Preset
OUT CORE Lite/Legacy Demo/Workbench/Create NPC Schedule Demo
```
