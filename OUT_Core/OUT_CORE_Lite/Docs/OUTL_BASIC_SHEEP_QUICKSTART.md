# OUTL Basic Sheep Quickstart

Status: first data-driven living actor proof-of-concept.

## Menu

- `OUT CORE Lite/Actors/Nature/Create Basic Sheep`
- `OUT CORE Lite/Actors/Nature/Add Basic Sheep To Scene`
- `OUT CORE Lite/Actors/Nature/Validate Basic Sheep`

## Created Assets

The generator writes under:

`Assets/OUT/OUT_Core/OUT_CORE_Lite/Actors/Nature/BasicSheep`

It creates an entity def, passive faction, shape profile, hurtbox profile, navigation profile, schedule, NPC behavior model, AI profile, drive profile, action set, sheep action assets, loot table, item defs, pickup prefab, and `OUTL_Nature_BasicSheep.prefab`.

## Runtime Stack

The sheep prefab uses the existing OUT CORE Lite stack:

`EntityAdapter + Vitals + DamageReceiver + DeathHandler/DeathRuntime + AIActor + NPCBehaviorController + DriveRuntime + BotInputDriver + ActorControlBridge + NavMoverInputSink + NavMeshMover + NavMeshAgent + LootDropper + ActorShapeRuntime + authored hitbox children`

There is no `SheepController`, animal manager, custom movement path, or runtime hurtbox construction.

## Test

1. Run `Create Basic Sheep`.
2. Run `Validate Basic Sheep`; the console should report zero validation errors.
3. Run `Add Basic Sheep To Scene`.
4. In Play Mode, damage the authored Body/Head/Leg hitbox colliders through the normal OUTL combat path.
5. Verify health decreases, death fires once, and loot drops once.

## Known Limits

- Reproduction is abstract: it increments saved pending offspring state and does not spawn runtime objects.
- Visuals are primitive editor-authored placeholder meshes.
- Movement depends on NavMesh when available, with existing NavMeshMover fallback/grounding for simple test scenes.
