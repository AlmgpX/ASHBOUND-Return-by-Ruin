# OUT CORE Lite: Generic Living Actor Protocol

**Date:** 2026-06-02  
**Context:** preserved from an architecture dialogue with ChatGPT about OUT CORE Lite after the GoldSrc/Quake-inspired gameplay-kernel discussion. The immediate trigger was the need to support sheep, cows, moose, spiders, fish, cowardly peasants, drunk bandits, demons, and other living entities through one systemic data-driven model instead of a controller zoo.

**Status:** architectural protocol / design contract.  
**Purpose:** keep future OUT CORE Lite work aligned with the existing Entity / ActorInput / WorldLedger / Egregore / Save contracts.

---

# OUT CORE LITE PERSONAL ARCHITECTURE PROTOCOL
## Generic Living Actor / Drive / Action / Reproduction / Shape / Hitbox Layer

Главный принцип:

OUT CORE Lite должен уметь создавать любое живое существо через один системный словарь:

```text
EntityDef + Runtime + ActorInputFrame + ActorControlBridge + MovementSink + ShapeProfile + HurtboxProfile + DriveProfile + ActionSet + StimulusProfile + Schedule + EgregoreInfluence + WorldLedger + Save
```

Это должно покрывать:

- игрока;
- NPC-солдата;
- трусливого крестьянина;
- пьяного разбойника;
- овцу;
- корову;
- лося;
- злого паука;
- рыбу в реке;
- демона;
- любую другую сущность без отдельного `SheepController`, `CowAI`, `SpiderBrain`.

---

## Canon

NPC / Creature / Animal / Humanoid = Entity/Actor.  
Bot = AI input source.  
Player = human input source.  
GameObject = временное тело сущности.  
WorldLedger record = дальняя/абстрактная форма сущности.

Один контроллер означает:

```text
PlayerInputSource или BotInputSource
-> OUTL_ActorInputFrame
-> OUTL_ActorControlBridge
-> набор sinks
```

Но sinks могут быть разными:

- `HumanoidGoldSrcMotorSink` для игрока/гуманоида;
- `NavMoverInputSink` для обычного NPC;
- `QuadrupedGroundMotorSink` для овцы/коровы/лося;
- `SpiderClimb/Leap/QuadrupedSink` для паука;
- `SwimMotorSink` для рыбы;
- `FlyingMotorSink` для летучей твари.

Нельзя:

- `SheepController`;
- `CowController`;
- `FishController`;
- `DemonAI`;
- `AnimalManager`;
- `NeedsManager Singleton`;
- отдельный combat path;
- отдельный movement path;
- прямое движение Transform мимо actor/mover contract;
- прямой `Instantiate/Destroy`;
- direct damage мимо `OUTL_Combat`.

---

## 1. Shape / Hull / Hitbox Contract

Нельзя использовать одну humanoid capsule как истину для всех существ.

Нужно разделить 4 слоя.

### A. Movement Hull

Используется для движения/навигации/grounding:

- `CharacterController` для игрока/гуманоида;
- `NavMeshAgent` radius/height для NPC;
- custom ground hull для четвероногих;
- swim volume для рыб;
- flying bounds для летающих.

### B. Hurtboxes / Damage Hit Volumes

Используются для попаданий projectile/hitscan/melee.

Они **не обязаны совпадать** с movement collider.

### C. Interaction Bounds

Используются для `Use`, `Talk`, `Open`, `Pickup`.

### D. Perception Bounds / Eye/Ear Points

Используются для AI perception/stimuli.

### Example: Sheep

Для овцы:

- movement hull может быть капсулой/агентом для навигации;
- hurtboxes должны быть ближе к телу:
  - body capsule/box вдоль корпуса;
  - head sphere/capsule;
  - optional leg hurtboxes, если нужно;
- если игрок стреляет между ног или чуть над спиной, projectile не должен попадать в невидимую humanoid capsule;
- если projectile пересёк body/head hurtbox, тогда `OUTL_Combat` получает entity через `HurtboxProxy`.

### Needed abstractions

- `OUTL_ActorShapeProfileDef`
- `OUTL_HurtboxProfileDef`
- `OUTL_Hurtbox`
- `OUTL_HurtboxProxy` / `DamageProxy`
- `OUTL_MovementHullProfile`
- `OUTL_PerceptionAnchorProfile`

### ShapeProfile fields

- `bodyLength`
- `bodyHeight`
- `bodyWidth`
- `eyeHeight`
- `centerOffset`
- `groundOffset`
- `movementRadius`
- `navAgentHeight`
- `navAgentRadius`
- `interactionRadius`
- crouch/pose variants optional
- medium: `Ground / Water / Air / Climb / Burrow`

### HurtboxProfile fields

- `hurtboxes[]`
  - `id`
  - type: `sphere / capsule / box`
  - local center
  - local size/radius/height
  - damage multiplier
  - tags: `Head`, `Body`, `Leg`, `WeakPoint`, `Armor`, `Shell`
  - enabled by pose/state
- `projectileHitPolicy`
- `meleeHitPolicy`
- `friendlyFirePolicy` optional

### Combat rule

Projectile/raycast/melee должен проверять hurtbox layer/proxy, а не слепо movement capsule.

`OUTL_Combat.TryGetEntityFromCollider` должен уметь найти `EntityAdapter` через `HurtboxProxy` parent entity.

---

## 2. Generic Drive Layer

Добавить не “потребности животных”, а общий Drive layer для всех живых actor’ов.

### Drives

- `Fear`
- `Hunger`
- `Thirst`
- `Fatigue`
- `Pain`
- `Aggression`
- `Curiosity`
- `Territory`
- `Social/Herd`
- `Comfort`
- `Greed`
- `Duty`
- `Alcohol/Intoxication`
- `Corruption`
- `Ritual`
- `ReproductionPressure`
- `PairBond`
- `Nesting`
- `BroodCare`
- `Rivalry`
- `SeasonalRut / Heat` optional
- `OffspringProtection`

### OUTL_DriveProfileDef

- drive ids;
- initial values;
- growth/decay;
- min/max;
- thresholds;
- tags;
- species/profile modifiers;
- egregore modifiers;
- save policy.

### OUTL_DriveRuntime

- current drive values;
- cooldowns;
- currentAction;
- actionStartTime;
- localSeed;
- lastThreat;
- lastFood/Resource;
- pairBond target stable id;
- nest cell;
- offspring pending state;
- last reproduction time;
- last waste drop time;
- save/load participant.

---

## 3. Generic Action Layer

### Actions

- `Idle`
- `Wander`
- `FleeFromThreat`
- `FindFood`
- `Eat`
- `FindWater`
- `Rest`
- `FollowHerd`
- `AvoidArea`
- `AttackTarget`
- `Ambush`
- `Guard`
- `Patrol`
- `Trade`
- `Hide`
- `InvestigateStimulus`
- `Poop/WasteDrop`
- `SwimWander`
- `StayInMedium`
- `CallForHelp`
- `SeekMate`
- `Courtship`
- `PairBond`
- `MoveToNest`
- `ReproduceAbstract`
- `ProtectOffspring`
- `RivalChallenge`
- `LeaveGroup / JoinHerd`

### OUTL_BehaviorActionDef

- action id;
- action type;
- tags;
- conditions;
- drive weights;
- stimulus weights;
- egregore weights;
- schedule weights;
- cooldown;
- min duration;
- max duration;
- target query;
- output intent/command/effect/stimulus;
- abstractMode support.

### OUTL_BehaviorActionSetDef

- list of possible actions;
- species/profile tags;
- fallback action;
- conflict rules;
- hysteresis rules.

### Scoring

```text
score =
baseWeight
+ drivePressure * driveWeight
+ stimulusWeight
+ scheduleWeight
+ egregoreBias
+ worldLedgerBias
+ randomJitter
- cooldownPenalty
- impossibleConditionPenalty
```

Важно:

Action layer не исполняет движение напрямую.

Он выбирает intent/current behavior.

Исполнение идёт через существующие:

- `NPCBehaviorController`
- `BotInputDriver`
- `ActorControlBridge`
- `MovementSink`
- `AttackDriver`
- `AbilitySink`
- `Command/Event/Effect`

---

## 4. Reproduction / Social-Bond Cycle

Репродукция = общий biological/social drive, не explicit animation system.

### Conditions

- adult/mature flag or stat;
- compatible species/tag/faction/profile;
- health above threshold;
- not dead;
- not fleeing;
- not in combat;
- safe enough cell/private/nest area;
- cooldown;
- season/day phase optional;
- egregore modifiers;
- population cap/density cap;
- player proximity rules optional.

### Actions

- `SeekMate`
- `Courtship`
- `PairBond`
- `MoveToNest`
- `ReproduceAbstract`
- `ProtectOffspring`
- `RivalChallenge`

### Outputs

- stimulus;
- event;
- schedule change;
- spawn request through WorldLedger / OUT pool;
- egregore memory trace;
- offspring abstract record or pooled prefab near player.

### Far/Dormant

- reproduction resolves abstractly;
- no GameObject materialization just for reproduction;
- create pending offspring abstract record/spawn request;
- update population/cell summary.

### Near/Full

- entities may approach, court, pair-bond, move to nest/private cell;
- result still goes through Event/Stimulus/WorldLedger;
- offspring spawn only through `OUTL_World` / `OUTL_PoolSystem` / `OutCore.pool.OUT`.

---

## 5. Egregore Influence

Эгрегор не управляет NPC напрямую.

Он не вызывает `sheep.RunAwayNow()`.

Он меняет давление и веса.

```text
EgregoreField -> DriveModifiers
EgregoreField -> ActionScoreBias
EgregoreField -> Stimulus injection
EgregoreField -> Schedule/BehaviorMode bias
```

### Examples

ForestSpirit angry:

- Fear +0.2 for herbivores;
- Aggression +0.3 for predators;
- FoodFinding -0.2;
- FleeFromThreat +0.4;
- Ambush +0.2.

Market prosperous:

- Trade +0.5;
- Wander +0.2;
- Flee -0.2;
- LootQuality +0.2;
- Reproduction/PairBond +0.1 if safety high.

Swamp corruption:

- Hunger +0.3;
- Curiosity +0.2;
- Fear +0.2;
- CursedLoot +0.4;
- Reproduction/Attraction/Rivalry distorted if Devourer/Lust/Corruption phase high.

Renewal/Safety/Prosperity:

- Reproduction + PairBond + BroodCare up.

Fear/Violence/Collapse:

- Reproduction down;
- Flee/Hide up.

Ritual/Temple/Forest cycle:

- can open/close mating/nesting seasons.

Hunger/resource pressure:

- suppress reproduction.

---

## 6. LOD / Materialization

### Full/Near

- normal actor stack;
- sensors;
- movement;
- combat;
- animation;
- hurtboxes;
- drops.

### Mid

- simplified decision tick;
- no expensive sensors;
- read WorldLedger summaries;
- cheaper route/schedule/drive tick.

### Far/Dormant

- drive decay/growth;
- route/schedule progress;
- reproduction abstract resolution;
- encounter abstract resolution;
- no GameObject;
- no NavMeshAgent;
- no hurtbox checks.

### Materialization

ledger record -> pooled GameObject actor

must restore:

- EntityId / stable id;
- class/faction/species;
- shape profile;
- hurtbox profile;
- movement hull profile;
- health/death;
- drives;
- current action;
- cooldowns;
- route progress;
- schedule state;
- inventory/equipment;
- pair bond/nest/reproduction state;
- egregore context.

### Dematerialization

GameObject actor -> ledger record

must save all above.

---

## 7. Examples

### Sheep

- EntityDef: `Sheep`
- Faction: `PassiveAnimal`
- Shape: `QuadrupedSmall`
- Hurtbox: body capsule horizontal + head sphere
- DriveProfile: `HerbivorePassive`
- ActionSet: `Graze/Flee/Wander/FollowHerd/WasteDrop/SeekMate/ReproduceAbstract`
- Schedule: `DayGrazeNightRest`
- Egregore: strong fear response, high herd response
- Loot: meat/wool

### Cow

Same as sheep but bigger Shape/Hurtbox, slower, larger waste drop, herd high.

### Moose

Large herbivore, Fear medium, Aggression/Territory medium, can charge if cornered.

### Spider

- Shape: low/wide body, multiple hurtboxes, optional leg zones
- DriveProfile: `PredatorAmbusher`
- ActionSet: `Ambush/LeapAttack/WebNest/SeekMate/GuardEggSac/FleeFire`
- Movement: ground/climb/leap sink
- Egregore: Shadow/Corruption boosts ambush/aggression

### Fish

- Shape: swim hull
- Hurtbox: small body capsule
- Movement: `SwimMotorSink`
- ActionSet: `SwimWander/FollowSchool/FleePredator/FindFood/MoveToSpawningArea/ReproduceAbstract`
- Medium requirement: water cell/volume

### CowardPeasant

- Shape: humanoid
- Motor: humanoid/nav
- DriveProfile: `CivilianFearDuty`
- ActionSet: `Work/Flee/Hide/CallGuard/Trade/PairBond/ProtectFamily`

### DrunkBandit

- Shape: humanoid
- DriveProfile: `GreedAlcoholAggression`
- ActionSet: `WanderDrunk/Ambush/Attack/FleeIfHurt/RivalChallenge`
- Egregore: Violence/Corruption increases aggression, Fear less effective when intoxicated

---

## 8. Validation Questions

Whenever designing a new living entity:

- Does it use EntityDef/EntityAdapter/Runtime?
- Does it use ActorInputFrame/ActorControlBridge?
- Is movement chosen by sink/profile, not custom controller?
- Are hurtboxes separate from movement hull?
- Does projectile hit use hurtbox/proxy?
- Does it have DriveProfile?
- Does it have ActionSet?
- Does egregore modify scores, not force states?
- Does it save drive/action/cooldown/reproduction state?
- Does it work as abstract record in Far/Dormant?
- Does spawn/despawn go through OUT pool/world?
- Can the same system make sheep, spider, fish, peasant and bandit?

If not, architecture is drifting into controller zoo.

---

## Compact Formula

```text
Movement Hull отвечает: “как существо ходит и не проваливается”.
Hurtbox отвечает: “куда можно попасть”.
ActorInput отвечает: “что существо пытается сделать”.
Drive/Action отвечает: “почему оно это делает”.
Egregore отвечает: “как место давит на его мотивы”.
```

