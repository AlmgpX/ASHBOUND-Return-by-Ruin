# OUTL Items, Pickups and Loot

Minimal item flow:

```text
Killed event
  -> OUTL_LootDropper
    -> OUTL_LootTableDef / inventory copy
      -> OUTL_World.Spawn or OUTL_PoolSystem
        -> OUTL_ItemPickup
          -> OUTL_World.Inventory.AddItem
          -> OUTL_World.QueueDespawn or OUT pool release
```

Authority rules:

```text
Drop   -> OUTL_NetworkAuthority.CanSpawnDrop()
Pickup -> OUTL_NetworkAuthority.CanPickup()
```

Client replicas do not apply pickup or drop locally. They should request the server through the network bridge.

`OUTL_LootDropper` drops once per death contract and can also convert existing `OUTL_InventorySystem` contents into pickup prefabs. `OUTL_ItemPickup` receives `Use` or `Pickup` commands and adds the item to the receiver through the existing inventory system.

Pickup prefabs should already contain `OUTL_ItemPickup` and, for registered world objects, `OUTL_EntityAdapter`. Runtime code does not add missing components.

`OUTL_LootTableDef` rolls stack count once per selected entry. By default `MinCount`/`MaxCount` become the `OUTL_ItemPickup.Count` stack value on one spawned pickup. Enable `SpawnOneObjectPerCount` only when multiple physical pickup objects are intended. `RollEachEntry`, `Weight` and `MaxDrops` keep table behavior predictable for small gameplay loot tables.
