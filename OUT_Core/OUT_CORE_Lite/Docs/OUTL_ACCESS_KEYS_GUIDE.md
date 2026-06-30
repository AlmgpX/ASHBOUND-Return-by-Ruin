# OUTL Access, Keys and Locks

Canonical flow:

```text
Player Use
  -> OUTL_CommandSystem
    -> OUTL_AccessController guard
      -> requirements pass
        -> optional atomic item consume
        -> Door / Button / PathMover receives command
      -> requirements fail
        -> OnAccessDenied
```

The guard runs before command receivers. A door cannot receive `Use`, `Open` or `Activate` and bypass a lock on the same entity.

## Doom / Quake retained key

Create an `OUTL_ItemDef` for the key and collect it through `OUTL_ItemPickup`.

On the door:

```text
OUTL_EntityAdapter
OUTL_Door
OUTL_Interactable
OUTL_AccessController
```

Access requirement:

```text
Condition.Op      = HasItem
Condition.Subject = Source
Condition.ItemDef = RedKey
Condition.IntValue = 1
ConsumePolicy     = Never
UnlockPermanentlyOnGrant = true
```

`Source` is the actor using the door. `Target` is the door.

## Consumable key

Set the requirement's `ConsumeItem` and use:

```text
ConsumePolicy = OnFirstGrant
```

or:

```text
ConsumePolicy = EveryGrant
UnlockPermanentlyOnGrant = false
```

Consumption uses `OUTL_InventorySystem.TryConsume`. It checks the complete amount before modifying inventory.

## External control

Send commands through `OUTL_OutputLink`:

```text
Lock
Unlock
```

Useful outputs:

```text
OnAccessGranted
OnAccessDenied
OnLocked
OnUnlocked
```

Create a working sample from:

```text
OUT CORE Lite -> Advanced -> Samples -> Create Key Door Access Example
```

The scene validator reports missing key definitions, incorrect `Target` inventory checks, empty guarded command lists and inventories that cannot resolve dynamic items during restore.
