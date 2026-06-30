# OUTL Death + Network Contract

Death, damage, drops, pickup and NPC schedule authority use one rule:

```text
Offline -> local OUTL_World is authority
Host/Server -> server applies damage, kill, drop, pickup and NPC schedules
Client replica -> request only; no local damage/death/drop/pickup authority
```

Core files:

```text
Combat/OUTL_LifeState.cs
Combat/OUTL_DeathRuntime.cs
Network/OUTL_NetworkAuthority.cs
Player/OUTL_PlayerDeathBridge.cs
```

`OUTL_Combat.ApplyDamage` checks `OUTL_NetworkAuthority.CanApplyDamage`. Health reaching zero calls `OUTL_DeathRuntime.TryKill`, which marks runtime life state once, emits `OUTL_EventType.Killed`, emits `OUTL_StimulusType.Death`, and prevents duplicate death/drop paths.

Player objects should use `OUTL_PlayerDeathBridge`; player death blocks local control but does not immediately despawn the player object. NPC/entity death can despawn later through `OUTL_DeathHandler`, which now uses `OUTL_Scheduler` instead of an authoritative `Update` loop.

Mirror remains optional. Without `OUTL_MIRROR`, authority resolves to Offline. With Mirror enabled, `OUTL_MirrorEntityBridge` exposes request commands for damage and pickup without creating a second command bus.

Server-side Mirror requests validate the sender before applying gameplay state. Damage requests require a live sender, live target, request cooldown, max request distance, max requested damage, a permitted attack profile and optional line of sight. Pickup requests require a live sender, an available pickup, request cooldown and max pickup distance. Clients should never be treated as trusted sources of arbitrary damage.
