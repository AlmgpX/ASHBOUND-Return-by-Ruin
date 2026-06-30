# OUTL NPC LOD Ticks

NPC behavior is scheduler/dispatcher driven. Do not put authoritative NPC brains in Unity `Update`.

## Tiers

- `Full`: close, exact behavior and frequent tactical input
- `Near`: regular behavior and local movement
- `Mid`: slower behavior, coarse travel
- `Far`: abstract schedule/travel, no full bot input
- `Dormant`: rare checks, no bot input

`OUTL_NPCBehaviorController` reads tier intervals from `OUTL_TickProfile` when present.

## Dispatcher Budget

`OUTL_NPCBehaviorDispatcher` owns the per-frame budget for NPC behavior ticks:

- `MaxNpcBehaviorTicksPerFrame`
- `MaxNpcRouteUpdatesPerFrame`
- `MaxNpcPathRequestsPerFrame`
- `MaxNpcStimulusInterruptsPerFrame`

The dispatcher rotates over registered controllers, skips actors that are not due by tier interval, and stops when the behavior budget is exhausted. Route/path/stimulus budgets are consumed from controller code.

## Debug

Use `OUT CORE Lite/Debug/Show NPC Tick Budget Snapshot`.

Snapshot fields:

- registered NPC controllers
- ticked this frame
- skipped by tier
- skipped by budget
- route/path/stimulus budget used

`OUTL_BotInputDriver` only produces full actor input for Full/Near/Mid by default. Far/Dormant actors stay abstract.
