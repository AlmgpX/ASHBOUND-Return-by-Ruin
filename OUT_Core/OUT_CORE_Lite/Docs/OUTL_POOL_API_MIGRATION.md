# OUTL Pool API Migration

Use the canonical facade:

```csharp
using OutCore.pool;

GameObject instance = OUT.Instantiate(prefab, position, rotation);
OUT.Destroy(instance);
```

Do not use runtime gameplay `Instantiate`, `Destroy`, `AddComponent`, `Resources.Load`, or scene searches for ordinary spawned objects.

## Supported Calls

```csharp
OUT.Instantiate(prefab);
OUT.Instantiate(prefab, position, rotation);
OUT.Instantiate(prefab, position, rotation, parent);
OUT.Instantiate(prefab, parent);
OUT.Instantiate(componentPrefab);
OUT.Destroy(instance);
OUT.Destroy(component);
OUT.Destroy(obj);
OUT.Destroy(instance, delay);
OUT.Release(instance);
OUT.Prewarm(prefab, count);
OUT.TryGetPoolStats(out stats);
```

`OUTL_EntityAdapter` instances are despawned through `OUTL_World.Despawn`. Plain pooled objects return to `OUTL_PoolSystem`.

## Authoring Rules

- Projectile prefabs must already contain `OUTL_Projectile`.
- Pooled prefabs may implement `OUTL_IPoolReset`.
- Spawn-context aware components may implement `OUTL_IPoolSpawnContextReceiver`.
- Runtime repair by `AddComponent` is not allowed outside editor/tools/tests and low-level pool internals.

## Debug

`OUTL_PoolStats` exposes created, active, inactive, peak, fallback, failed spawn, and double-release counters.
