# OUTL Character Identity

`OUTL_CharacterIdentity` is the persistent actor identity module. It does not create a second entity identity: `OUTL_EntityAdapter.StableId` remains the canonical address.

The component stores:

- given name, family name, nickname and display name;
- role and background;
- courage, aggression, discipline, awareness, loyalty and greed;
- deterministic generation seed.

Generation is deterministic from `StableId + OUTL_CharacterIdentityProfile`. An abstract spawn receives the identity payload before any GameObject is materialized. Pool reuse clears transient prefab data; save restore returns the same identity and attributes.

The component has no `Update`, performs no scene search and persists through `OUTL_IComponentSaveParticipant`.

For Occultists, open a generated enemy prefab and inspect `OUTL_CharacterIdentity`. The shared name/attribute ranges live in:

```text
Generated/OccultistEnemyPack/Profiles/OUTL_Identity_Occultist.asset
```

Regenerate/repair content through:

```text
OUT CORE Lite/Content/Generate Occultist Enemy Pack
```
