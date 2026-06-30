# OUTL Egregore Architecture

An egregore is an optional collective runtime entity. It is not a global brain, not a new manager of gameplay, and not content-specific lore.

Core pieces:

```text
OUTL_EgregoreDef
OUTL_EgregoreArchetypeDef
OUTL_EgregoreArchetypalCycle
OUTL_EgregoreCyclePhase
OUTL_EgregoreArchetypePressure
OUTL_EgregoreTransformationRule
OUTL_EgregoreShadowRule
OUTL_EgregoreIntegrationRule
OUTL_EgregoreRuntime
OUTL_EgregoreSystem
OUTL_EgregoreComponent
OUTL_EgregoreInfluenceZone
OUTL_EgregoreSignal
OUTL_EgregoreDebugView
```

Runtime state:

```text
violence
fear
health
prosperity
corruption
alertness
hostility
entropy
playerReputation
currentCyclePhase
dominantArchetype
shadowArchetype
unresolvedTension
integrationProgress
corruptionProgress
renewalProgress
traumaMemory
boonMemory
sacrificeDebt
thresholdOpen
```

Inputs:

```text
OUTL_EventBus
OUTL_StimulusBus
WorldLedger summaries
memory traces
```

Outputs are lightweight signals:

```text
RaiseAlert
SpawnPatrol
CalmWildlife
IncreaseHostility
ChangeAmbientProfile
ModifyFactionRelations
CyclePhaseChanged
OpenThreshold
CollapseWarning
RenewalPulse
```

The archetypal cycle is a systemic state machine for a place, not a story template:

```text
StableWorld -> Disturbance -> Call -> Threshold -> Descent/Trials
-> ShadowConfrontation -> Crisis/Sacrifice
-> RevelationOrBoon -> Return/Integration -> Renewal
or CorruptionLoop -> Collapse
```

Events and stimuli push the cycle:

```text
damage/death/combat -> Shadow, Warrior, VoidDeathRebirth, trauma
theft/container looting -> Trickster, Shadow, alertness
quest completed -> Integration/Renewal/Boon
quest failed -> CorruptionLoop/Collapse pressure
ritual/hunger/desire/raid tags -> Threshold, Devourer, Lover, WoundedKing pressure
```

The runtime writes an `OUTL_EgregoreField` into `OUTL_World.WorldLedger`. NPC behavior reads that cell summary and maps phases to behavior modes. Loot rolls read the same cell summary through `OUTL_LootContext`. Quest stage changes emit existing OUTL events with archetypal hook ids. Save/load captures phase, pressures and memory traces through `OUTL_IComponentSaveParticipant`.

Example authoring ideas:

```text
forest-like zone reacts to resource destruction and combat noise
city-like zone reacts to deaths, combat and faction alerts
temple-like zone reacts to ritual tags and threshold/revelation pressure
```

Keep those as content assets, not core classes.
