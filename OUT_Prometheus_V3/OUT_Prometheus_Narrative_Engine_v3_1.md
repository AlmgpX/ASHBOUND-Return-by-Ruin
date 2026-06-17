# OUT Prometheus Narrative Engine v3
## Единый литературный движок структурного письма, удержания и прохождения пути при любом количестве глав

> **Название:** OUT Prometheus Narrative Engine v3  
> **Русское имя:** OUT Прометей v3  
> **Дата сборки:** 2026-06-15  
> **Назначение:** единый Markdown-движок для романов, повестей, сериалов, визуальных новелл, квестов, игр и AI-assisted writing.  
> **Главное обновление v3:** движок больше не зависит от фиксированного количества глав. Если число глав известно, путь героя, арка персонажа, читательские крючки, символические мутации и сюжетные развороты распределяются по N главам через структурную карту. Если число глав неизвестно, текст пишется через динамические milestone-блоки и не растекается в красивую литературную болотину.

---

## 0. Elevator Pitch

OUT Prometheus — это не шаблон сюжета, не beat sheet, не «формула романа для людей, которым запретили думать».  
Это **data-driven narrative engine**, где история описывается как система состояний, сущностей, событий, контрактов, ожиданий читателя и трансформаций.

```text
DATA → STATE → ARC MAP → CHAPTER MAP → CONTRACT → SCENE → OUTPUT → VALIDATION → UPDATED STATE
```

История — это не набор красивых абзацев.  
История — это **машина изменения состояния героя, мира, отношений и читателя**.

Если сцена не меняет состояние — это не сцена, а декоративный idle animation.  
Если глава не меняет минимум две линии состояния — это не глава, а литературная заставка перед загрузкой, которую никто не просил.

---

## 1. Core Law

```text
Plot       = внешнее изменение.
Archetype  = внутреннее изменение.
Symbol     = интерфейс между внешним и внутренним.
Mechanic   = повторяемый способ давления на героя / игрока.
Scene      = минимальная единица изменения.
Chapter    = пакет сцен с общей функцией и финальным сдвигом.
Book       = система трансформации, растянутая на N глав.
Reader     = управляемое состояние вопросов, ожиданий, тревоги и желания продолжать.
```

### Закон v3

```text
Количество глав не определяет путь.
Путь определяет, как главы должны быть распределены.
```

Если автор говорит: «у меня будет 8 глав», движок не должен отвечать: «пиши 8 глав».  
Он должен ответить: «вот как 8 контейнеров распределяют маску, зов, порог, падение, тень, ложную победу, смерть старого состояния, интеграцию и финальный крючок».

---

## 2. One-File Mode

v3 можно использовать как один файл, без папочной структуры.  
Это полезно для Grok Build, Codex, локальных моделей и любого ИИ-агента, который начинает путаться в папках как стажёр в архиве ада.

```text
OUT_Prometheus_Narrative_Engine_v3.md
  1. Core Rules
  2. Project Input
  3. Ontology
  4. Narrative Mode
  5. World Rules
  6. Entity Bible
  7. Character Core Contracts
  8. State Variables
  9. Data-Driven State Ledger / Debug Vectors
 10. Arc Geometry
 11. Chapter Count Compiler
 12. Chapter Contracts
 13. Scene Contracts
 14. Engagement Engine
 15. Predictor-Corrector Layer
 16. Style Renderer
 17. Validation Gates
 18. Runtime Prompts
```

Workspace mode допустим, но не обязателен.  
Если проект сложный, один файл можно развернуть в папки. Но master-файл должен оставаться источником истины.

---

## 3. Required Build Order For AI Agents

Любой агент — ChatGPT, Grok Build, Codex, локальная Llama/Qwen, человек с кофе и иллюзией контроля — обязан работать в таком порядке.

```text
PHASE 0: Read Project Input
PHASE 1: Define Core Promise
PHASE 2: Define Core Ontology
PHASE 3: Define Narrative Mode
PHASE 4: Define World / Social Physics
PHASE 5: Define Entities / Character Cores / Symbols / Threats
PHASE 6: Define State Variables
PHASE 6.5: Define Data-Driven State Ledger / Debug Vector Schema
PHASE 7: Define Global Arc Geometry
PHASE 8: Compile Chapter Count Map
PHASE 9: Define Chapter Contracts
PHASE 10: Define Scene Contracts
PHASE 11: Generate Output
PHASE 12: Validate Output
PHASE 13: Correct Structure Before Style
PHASE 14: Update Continuity Log + State Ledger Snapshot
```

### Hard Rule

```text
Do not generate final prose before:
  1. Core Promise exists.
  2. Core Ontology exists.
  3. Narrative Mode exists.
  4. World / Social Physics exists.
  5. Entity Bible exists.
  6. Character Core Contracts exist for every key character.
  7. State Variables exist.
  8. State Ledger / Debug Vector schema exists.
  9. Global Arc Geometry exists.
 10. Chapter Count Map or Milestone Map exists.
 11. Current Chapter Contract exists.
 12. Scene Contracts exist.
```

Если агент пишет прозу до карты состояний, он не «творит». Он делает красивый туман и ждёт, что кто-то назовёт это атмосферой.

---

## 4. Project Input Template

Перед генерацией проекта заполнить минимум:

```yaml
ProjectInput:
  title:
  working_title:
  format: novel / novella / serial / visual_novel / game / quest / screenplay
  target_length_chars:
  target_chapter_count:
  target_scenes_per_chapter:
  known_chapter_count: true / false
  genre:
  subgenre:
  target_audience:
  market_position:
  content_rating:
  commercial_goal:
  artistic_goal:

  protagonist:
  protagonist_start_state:
  protagonist_end_state:
  protagonist_core_wound:
  protagonist_core_lie:
  protagonist_mask:
  protagonist_conscious_desire:
  protagonist_subconscious_real_need:
  protagonist_thesis:
  protagonist_antithesis:
  protagonist_synthesis_target:

  data_driven_workspace:
    require_state_ledger: true
    require_chapter_vectors: true
    require_character_bitmasks: true
    separate_debug_file: true
    state_snapshot_after_each_chapter: true

  key_characters:
    - id:
      role:
      core_wound:
      core_lie:
      mask:
      conscious_desire:
      subconscious_real_need:
      thesis:
      antithesis:
      synthesis_target:
      fear:
      shadow:
      moral_limit:
      relationship_to_protagonist:
      narrative_function:

  world_core:
  main_social_pressure:
  main_supernatural_or_systemic_pressure:
  main_antagonistic_force:

  romance_level:
  violence_level:
  humor_level:
  darkness_level:
  explicitness_level:

  mandatory_symbols:
  mandatory_locations:
  mandatory_relationships:
  forbidden_content:
  style_references:
  anti_references:
```

### If target_chapter_count is unknown

```yaml
UnknownChapterCountMode:
  use_milestones: true
  minimum_arc_units: 7
  maximum_arc_units:
  expandable_middle: true
  do_not_lock_final_count_until_global_arc_validated: true
```

### If target_chapter_count is known

```yaml
KnownChapterCountMode:
  chapter_count: N
  compile_chapter_map_before_writing: true
  every_chapter_has_unique_primary_function: true
  every_chapter_changes_at_least_two_state_lanes: true
  climax_position_locked: true
  midpoint_position_locked: true
```

---

## 5. Ontology

### 5.1 Entity

Entity — любая значимая сущность.

```text
Character
Faction
Location
Object
Symbol
Secret
Threat
Promise
Scene
Chapter
Arc
Rule
Event
Concept
Claim
```

```yaml
Entity:
  id:
  name:
  type:
  role:
  tags:
  current_state:
  relationships:
  active_components:
  narrative_function:
  symbolic_function:
  gameplay_function:
  commercial_function:
```

---

### 5.2 Component

Component — часть сущности, которую читают системы.

```yaml
CharacterComponents:
  Persona:
  Shadow:
  ConsciousDesire:
  SubconsciousRealNeed:
  Fear:
  CoreLie:
  CoreWound:
  Mask:
  Thesis:
  Antithesis:
  SynthesisTarget:
  FalseStrategy:
  SocialRole:
  PowerResource:
  Weakness:
  Boundary:
  Temptation:
  SpeechProfile:
  AgencyProfile:
  AttractionVector:
  ShameTrigger:
  StatusHunger:
  MoralLimit:
```

---

### 5.3 Character Core Contract

Every key character must be defined as an inner engine, not as furniture with dialogue.
A key character is any character who changes plot state, relationship state, world state, reader state, or appears across multiple structural turns.

Minimum key characters:

```text
protagonist
deuteragonist / primary relationship figure
main antagonist
major rival
major ally / mentor
major faction representative
any character whose choice can change the arc
```

For every key character, create this contract before chapter mapping:

```yaml
CHARACTER_CORE_CONTRACT:
  character_id:
  role:
  narrative_function:

  core_wound:
  core_lie:
  mask:

  conscious_desire:
  subconscious_real_need:

  thesis_at_start:
  antithesis_pressure:
  synthesis_target:

  fear:
  shame_trigger:
  forbidden_desire:
  shadow:
  moral_limit:
  false_strategy:

  what_they_want_from_protagonist:
  what_they_misread_about_protagonist:
  what_protagonist_misreads_about_them:

  relationship_function:
  world_function:
  symbolic_function:

  start_state:
  midpoint_state:
  crisis_state:
  end_state:
```

### 5.3.1 Definitions

```text
Core Wound = the deepest painful conclusion that shaped the character's survival strategy.
Core Lie = the false rule the character believes because of the wound.
Mask = the social/persona armor used to survive and control perception.
Conscious Desire = what the character knows they want.
Subconscious Real Need = what the character actually needs to become whole, free, dangerous, honest, or transformed.
Thesis = the character's starting worldview / survival formula.
Antithesis = the pressure that proves the starting formula incomplete.
Synthesis = the new state or worldview the arc demands, if the character transforms.
False Strategy = the repeated behavior that once protected the character but now creates damage.
```

### 5.3.2 Key Character Rule

```text
No key character may enter the chapter map without:
  1. core_wound
  2. core_lie
  3. mask
  4. conscious_desire
  5. subconscious_real_need
  6. thesis_at_start
  7. antithesis_pressure
  8. synthesis_target or deliberate_no_synthesis
```

If a key character has no wound, lie, mask, desire, need and thesis, the engine must mark them as underbuilt and block final prose.
This is not emotional bureaucracy. It prevents characters from becoming plot-shaped mannequins, humanity's second most popular renewable resource after bad decisions.

### 5.3.3 Scene Use Rule

When a key character appears in a scene, at least one part of their core contract must be active:

```text
wound pressured
lie protected
mask performed
conscious desire pursued
real need resisted
thesis challenged
shadow leaked
moral limit approached
false strategy repeated
```

If none is active, the character is present as decoration. Remove, merge, or give them pressure.

```yaml
WorldComponents:
  Law:
  Hierarchy:
  Economy:
  Religion:
  MagicRules:
  TechRules:
  Taboo:
  StatusSystem:
  ViolenceRules:
  GenderRules:
  ClassPressure:
  FactionPressure:
  Bureaucracy:
  PunishmentSystem:
  ReputationSystem:
```

```yaml
ReaderComponents:
  CurrentQuestion:
  CurrentDesire:
  CurrentFear:
  CurrentSuspicion:
  CurrentMisunderstanding:
  PromisedPayoff:
  DelayedPayoff:
  EmotionalDebt:
  TrustInAuthor:
  NeedToReadNext:
```

---

### 5.4 System

System — логика, которая меняет состояние.

```text
PlotSystem
ConflictSystem
RelationshipSystem
RomanceSystem
StatusSystem
RumorSystem
MysterySystem
FactionSystem
SymbolSystem
ArchetypeSystem
CharacterCoreSystem
ShadowPressureSystem
ContinuitySystem
ReaderStateSystem
PacingSystem
EngagementSystem
ChapterCountCompiler
PredictorCorrectorSystem
StyleRenderer
```

---

### 5.5 Event

Event — действие, меняющее состояние.

```text
GiftGiven
PublicHumiliation
SecretRevealed
BoundaryCrossed
Rescue
Betrayal
Confession
Ritual
Duel
SocialAttack
JealousyTrigger
SymbolReturn
ForbiddenDesireActivated
StatusChanged
NameChanged
BodyMarked
DebtCreated
LieProtected
LieBroken
MaskCracked
NeedResisted
NeedAccepted
ThesisChallenged
ChoiceCommitted
```

```yaml
Event:
  event_id:
  event_type:
  actor:
  target:
  witnesses:
  visible_action:
  hidden_motive:
  immediate_effect:
  delayed_effect:
  plot_delta:
  relationship_delta:
  world_delta:
  archetypal_delta:
  symbolic_delta:
  reader_delta:
  next_trigger:
```

---

## 6. State Variables

Каждый проект обязан иметь state-блоки. Без них ИИ начинает «чувствовать стиль», а потом внезапно герой забывает, чего хотел, кто его унизил и почему символ горел три главы назад.

```yaml
PlotState:
  current_phase:
  active_conflict:
  unresolved_hooks:
  active_promises:
  irreversible_events:
  next_required_turn:

CharacterState:
  conscious_desire:
  subconscious_real_need:
  fear:
  core_wound_pressure:
  mask_integrity:
  shadow_pressure:
  agency_level:
  current_lie:
  thesis_state:
  antithesis_pressure:
  synthesis_progress:
  current_choice_capacity:
  forbidden_desire_pressure:

RelationshipState:
  trust:
  fear:
  attraction:
  rivalry:
  debt:
  loyalty:
  betrayal_risk:
  power_balance:
  intimacy_level:
  ownership_pressure:

WorldState:
  current_power_balance:
  law_response:
  rumor_level:
  faction_tensions:
  economic_pressure:
  taboo_pressure:
  supernatural_pressure:
  location_changes:

ArchetypalState:
  stage:
  active_mask:
  active_shadow:
  threshold_status:
  descent_level:
  symbolic_death_progress:
  integration_progress:

SymbolicState:
  active_symbols:
  symbol_mutations:
  motif_returns:
  corrupted_symbols:
  purified_symbols:
  burned_symbols:
  final_symbol_target:

ReaderState:
  knows:
  suspects:
  wants:
  fears:
  expects:
  misunderstands:
  current_question:
  promised_payoff:
  delayed_payoff:
  emotional_debt:
  next_page_pressure:
```

---

### 6.1 Data-Driven State Ledger / Debug Vectors

For any serious project, the engine must keep prose and state apart.
Prose is the rendered output. State is the save file. Mixing them is how characters lose motives, worlds forget laws, and the author starts calling bugs “mystery”.

A project should maintain separate project files:

```text
ProjectCore.md                = stable canon, ontology, character contracts, world physics.
ChapterMap.md                 = planned chapter functions and milestones.
ChapterContracts.md           = required chapter inputs/outputs.
SceneContracts.md             = scene-level contracts.
StateLedger_DEBUG.md          = chapter-by-chapter state snapshots and vector deltas.
ContinuityLog.md              = facts already rendered into prose.
Draft.md                      = final prose only.
```

In one-file mode, these may be sections in a single document.
In production mode, `StateLedger_DEBUG.md` should be separate, because it is not prose and should not pollute the reader-facing manuscript.

### 6.2 State Snapshot Schema

Create a snapshot before and after every chapter.

```yaml
STATE_SNAPSHOT:
  chapter_id:
  snapshot_type: before / after

  plot_state:
    current_phase:
    active_conflict:
    unresolved_hooks:
    irreversible_events:

  world_state:
    power_balance:
    law_response:
    faction_tensions:
    economic_pressure:
    magic_environment:
    rumor_level:
    location_changes:

  reader_state:
    knows:
    suspects:
    wants:
    fears:
    current_question:
    promised_payoff:
    emotional_debt:
    next_page_pressure:

  character_states:
    - character_id:
      wound_pressure: 0-5
      lie_strength: 0-5
      mask_integrity: 0-5
      conscious_desire_pressure: 0-5
      real_need_visibility: 0-5
      shadow_pressure: 0-5
      agency_level: 0-5
      thesis_state: intact / challenged / cracked / broken / transformed
      current_strategy:
      current_misbelief:
      current_choice_capacity: 0-5
      relationship_deltas:
        target_character_id:
        trust_delta:
        fear_delta:
        attraction_delta:
        debt_delta:
        power_delta:
```

Rule:

```text
A chapter is not complete until it produces an AFTER snapshot.
The next chapter must read the previous AFTER snapshot as its BEFORE state.
```

### 6.3 Chapter Vector Schema

Every chapter needs a vector: a compact machine-readable summary of what the chapter must do.

```yaml
CHAPTER_VECTOR:
  chapter_id:
  arc_ratio:
  milestone:
  primary_function:
  secondary_function:

  state_lane_mask:
    PLOT: 0 / 1
    CHAR: 0 / 1
    REL: 0 / 1
    WORLD: 0 / 1
    ARCH: 0 / 1
    SYMB: 0 / 1
    READER: 0 / 1

  pressure_vector:
    physical_danger: 0-5
    social_danger: 0-5
    moral_danger: 0-5
    intimacy_pressure: 0-5
    mystery_pressure: 0-5
    status_pressure: 0-5
    supernatural_pressure: 0-5

  character_core_pressure:
    - character_id:
      wound: 0 / 1
      lie: 0 / 1
      mask: 0 / 1
      desire: 0 / 1
      need: 0 / 1
      thesis: 0 / 1
      shadow: 0 / 1
      moral_limit: 0 / 1

  reader_loop_vector:
    close_loop:
    open_loop:
    deepen_loop:
    micro_payoff:
    final_hook:

  output_requirements:
    irreversible_event:
    power_shift:
    cost_paid:
    dirty_reward:
    new_information:
    symbol_mutation:
```

### 6.4 Bitmask Notation

Bitmasks are allowed as shorthand when a project needs compact debug output.
They are not mystical. They are checkboxes with a trench coat.

```text
STATE_LANE_BITS:
  PLOT   = 1
  CHAR   = 2
  REL    = 4
  WORLD  = 8
  ARCH   = 16
  SYMB   = 32
  READER = 64
```

Examples:

```text
PLOT + CHAR + READER = 1 + 2 + 64 = 67
CHAR + REL + WORLD + SYMB = 2 + 4 + 8 + 32 = 46
```

Human-readable output should still show names, not only numbers.
If the debug file says `67` and the author has to reverse-engineer their own novel, the machine has won and everyone should be embarrassed.

### 6.5 Character Core Bitmask

For every key character appearing in a chapter, track which part of the core contract is pressured.

```text
CHARACTER_CORE_BITS:
  WOUND       = 1
  LIE         = 2
  MASK        = 4
  DESIRE      = 8
  NEED        = 16
  THESIS      = 32
  SHADOW      = 64
  MORAL_LIMIT = 128
```

Examples:

```text
WOUND + MASK + DESIRE = 1 + 4 + 8 = 13
LIE + NEED + SHADOW + MORAL_LIMIT = 2 + 16 + 64 + 128 = 210
```

Rule:

```text
If a key character appears in a chapter and their character_core_bitmask = 0, they are not structurally active.
Remove them, demote them to background, or rewrite the chapter so their core is pressured.
```

### 6.6 Debug File Contract

`StateLedger_DEBUG.md` is a required planning artifact for complex novels, serials, visual novels and games.

It should contain:

```markdown
# StateLedger_DEBUG

## Chapter 01

### BEFORE
[paste STATE_SNAPSHOT]

### VECTOR
[paste CHAPTER_VECTOR]

### AFTER
[paste STATE_SNAPSHOT]

### DELTA REPORT
- State lanes changed:
- Character core contracts pressured:
- World facts changed:
- Reader loops closed/opened/deepened:
- Continuity facts to export:
- Risks for next chapter:
```

The debug file is not a replacement for the chapter contract.
The chapter contract says what must happen.
The debug file proves what changed.

---

## 7. Narrative Modes

Narrative modes are not biological rules.  
They are social, genre, market and reader expectation profiles.

### 7.1 Universal Mode

Use when the narrative emphasizes survival, transformation, competence, belonging, truth, moral choice and agency.

```yaml
UniversalMode:
  core_question: What must change for the protagonist to become whole or free?
  axes:
    - agency
    - transformation
    - survival
    - competence
    - belonging
    - truth
    - moral choice
```

---

### 7.2 Masculine Narrative Physics

Use when pressure emphasizes competence, status, responsibility, honor, mastery, power, violence cost, brotherhood, exile, return or king/warrior/shadow integration.

```yaml
MasculineNarrativePhysics:
  primary_question: Can the protagonist become powerful without becoming a slave to power?
  core_axes:
    - competence
    - control
    - strength
    - responsibility
    - honor
    - status
    - survival
    - mastery
    - sacrifice
    - violence_cost
  failure_modes:
    - becomes_tyrant
    - becomes_weapon
    - wins_externally_loses_self
    - cannot_accept_tenderness
    - cannot_stop_fighting
```

---

### 7.3 Feminine Narrative Physics

Use when pressure emphasizes agency, desire, self-ownership, safety, reputation, intimacy, social gaze, boundaries, recognition, voice, being chosen without being owned.

```yaml
FeminineNarrativePhysics:
  primary_question: Can the protagonist desire, choose, receive, refuse, and become powerful without becoming an object?
  core_axes:
    - agency
    - self_ownership
    - safety
    - reputation
    - desire
    - boundaries
    - relational_power
    - trust
    - recognition
    - social_consequence
    - intimacy
    - voice
  failure_modes:
    - becomes_object
    - trades_agency_for_safety
    - performs_identity_instead_of_living
    - confuses_protection_with_ownership
    - confuses_desire_with_loss_of_self
    - becomes_cruel_to_avoid_vulnerability
```

---

### 7.4 Hybrid Mode

Use when a project deliberately mixes pressure systems.

```yaml
HybridMode:
  rule: Track active pressure per scene, not per biological sex.
  examples:
    - public duel: masculine pressure
    - reputation under social gaze: feminine pressure
    - survival decision: universal pressure
    - romance with power imbalance: feminine + status + danger
    - revenge arc: masculine + shadow + moral corruption
```

---

## 8. Archetypal Core

OUT Prometheus uses archetypes as transformation logic, not as decorative mythology pasted over weak scenes like gold foil over rotten wood.

```text
Ordinary Mask
→ Call / Irritant
→ Threshold
→ Descent
→ Shadow Contact
→ False Solution
→ Symbolic Death
→ Integration Choice
→ Boon / Cost
→ Return or New Exile
```

### 8.1 Persona / Shadow / Self

```yaml
Persona:
  public_mask:
  survival_function:
  reward_for_keeping:
  cost_of_keeping:

Shadow:
  rejected_force:
  forbidden_desire:
  forbidden_anger:
  forbidden_fear:
  projected_onto:
  triggered_by:
  integrates_through:

Self:
  possible_wholeness:
  required_sacrifice:
  final_choice:
  final_symbol:
```

### 8.2 Scene Questions

Every major scene should answer at least one:

```text
Which mask is being defended?
Which shadow is pushing upward?
Which symbol is active?
Which threshold is crossed?
Which old survival strategy breaks?
Which conscious desire conflicts with real need?
Which thesis is challenged?
What new state becomes possible?
What does the protagonist gain and what poison is attached?
```

---

## 9. Symbolic System

Symbol is not decoration.  
Symbol is an interface between external action and unconscious meaning.

```yaml
Symbol:
  id:
  name:
  surface_meaning:
  hidden_meaning:
  linked_character:
  linked_fear:
  linked_desire:
  first_appearance:
  mutations:
  corrupted_form:
  purified_form:
  burned_form:
  final_form:
  genre_function:
```

### 9.1 Symbol Evolution

```text
Detail → Motif → Symbol → Mutation → Revelation → Price
```

A symbol must not only repeat. It must mutate.

Bad:

```text
flower appears
flower appears again
flower appears again
reader dies inside
```

Good:

```text
flower = innocence
flower = stain
flower = social label
flower = forbidden desire
flower = burned proof
flower = new agency
```

---

## 10. Global Arc Geometry

Before chapter planning, create the global geometry.

```yaml
GlobalArcGeometry:
  start_state:
  end_state:
  core_transformation:
  core_wound:
  core_lie:
  mask_to_break:
  conscious_desire:
  subconscious_real_need:
  thesis_at_start:
  antithesis_pressure:
  synthesis_target:
  final_truth:
  final_relation_to_desire:
  main_external_goal:
  external_goal_mutation:
  main_antagonistic_force:
  main_shadow_force:
  main_symbol:
  final_symbol_state:

  required_milestones:
    - mask_established
    - irritant_or_call
    - threshold_crossed
    - first_cost_paid
    - descent_deepens
    - midpoint_reversal
    - false_solution
    - shadow_contact
    - symbolic_death
    - integration_choice
    - final_cost
    - new_state
```

### 10.1 Required Arc Spine

```text
1. Mask: герой живёт в ложном, но работающем порядке.
2. Irritant: мир даёт трещину, которую нельзя честно объяснить старой маской.
3. Threshold: герой переходит в зону, где старые правила больше не защищают.
4. First Cost: герой получает первый выигрыш с ядом.
5. Descent: давление нарастает, ложь уже требует оплаты.
6. Midpoint: герой получает силу/правду/желание, но не умеет с этим жить.
7. False Solution: герой пытается победить старым способом.
8. Shadow Contact: запрещённая часть личности становится видимой.
9. Symbolic Death: старая идентичность больше не работает.
10. Integration Choice: герой выбирает не между хорошим и плохим, а между двумя ценами.
11. Final Cost: новая сила требует отказа от старой сделки с миром.
12. New State: герой не возвращается прежним.
```

---

## 11. Chapter Count Compiler

### 11.1 Core Rule

```text
If chapter count N is known:
  compile the global arc into N chapter functions before writing.

If chapter count N is unknown:
  compile the global arc into milestone blocks, then expand only after validation.
```

Количество глав — это не список коробок. Это разрешение на распределение давления.

---

### 11.2 Milestone Ratio Map

Use ratios, then map them to chapter numbers.

```yaml
MilestoneRatioMap:
  0.00:
    milestone: Mask Established
    function: show old identity, old lie, old reward

  0.08-0.12:
    milestone: Irritant / Call
    function: first crack, first impossible desire, first system pressure

  0.18-0.25:
    milestone: Threshold
    function: protagonist crosses into irreversible social/world/inner zone

  0.28-0.35:
    milestone: First Cost
    function: first dirty reward, first public consequence

  0.45-0.55:
    milestone: Midpoint Reversal
    function: truth or power changes the reading of everything before it

  0.62-0.72:
    milestone: False Solution / Bad Bargain
    function: protagonist tries old strategy with new stakes

  0.78-0.88:
    milestone: Crisis / Symbolic Death
    function: old identity cannot continue

  0.90-0.97:
    milestone: Integration Choice
    function: protagonist chooses new state and pays visible cost

  1.00:
    milestone: Landing / New Hook
    function: show changed state, price, next promise
```

### 11.3 Mapping Formula

For N chapters:

```text
chapter_index = clamp(round(ratio * N), 1, N)
```

If two milestones map to the same chapter, combine them by hierarchy:

```text
Mask + Irritant = opening pressure chapter
Threshold + First Cost = irreversible entry chapter
False Solution + Crisis = collapse chapter
Integration + Landing = finale chapter
```

---

### 11.4 Chapter Count Profiles

#### 1 chapter / short story

```text
Scene 1: Mask + Irritant
Scene 2: Threshold + First Cost
Scene 3: Midpoint + Shadow Contact
Scene 4: Crisis + Integration + Cost
```

#### 3 chapters / novella skeleton

```text
Chapter 1: Mask → Call → Threshold
Chapter 2: First Cost → Midpoint → False Solution
Chapter 3: Crisis → Integration → New State
```

#### 5 chapters

```text
1. Mask / Desire / Irritant
2. Threshold / First Cost
3. Midpoint / Dirty Reward
4. False Solution / Shadow Contact / Crisis
5. Integration / Final Cost / New State
```

#### 8 chapters

```text
1. Mask and genre promise
2. Call and destabilization
3. Threshold and new rules
4. First cost and relationship/world shift
5. Midpoint revelation or power gain
6. False solution / bad bargain
7. Crisis / symbolic death
8. Integration / final price / next hook
```

#### 12 chapters

```text
1. Mask
2. Irritant
3. Threshold
4. First Cost
5. Descent
6. Midpoint
7. Consequences of Midpoint
8. False Solution
9. Shadow Contact
10. Symbolic Death
11. Integration Choice
12. Final Cost / New State
```

#### 16 chapters

```text
1. Old world / mask reward
2. Wound pressure
3. Irritant
4. Refusal or wrong interpretation
5. Threshold
6. New rules
7. First dirty reward
8. First real cost
9. Midpoint reversal
10. Aftershock
11. False solution
12. Bad bargain
13. Shadow contact
14. Symbolic death
15. Integration choice
16. New state + next promise
```

#### 24 chapters

```text
1. Old identity sells itself to reader
2. Hidden wound leaks
3. Desire appears in forbidden form
4. Call / irritant
5. Refusal / rationalization
6. Threshold
7. New world rules
8. First ally / first social pressure
9. First dirty reward
10. First visible cost
11. Descent pressure
12. Midpoint reversal
13. Midpoint aftershock
14. Relationship/world consequence
15. False solution begins
16. False solution seems to work
17. Bad bargain price appears
18. Shadow contact
19. Symbol mutation / truth approaches
20. Collapse of old strategy
21. Symbolic death
22. Integration choice
23. Final cost / irreversible action
24. New state / emotional landing / next hook
```

#### 25+ chapters / long serial

```text
Use 12 core milestone chapters as pillars.
Between pillars, insert expansion chapters only if they change at least two state lanes.
No filler arcs without permanent state consequences.
```

Expansion chapter types:

```text
relationship_pressure
status_attack
mystery_reframing
symbol_mutation
world_rule_demonstration
faction_consequence
temptation_episode
failed_escape
dirty_reward
public_identity_shift
```

---

### 11.5 Chapter Function Uniqueness Rule

Every chapter must have a primary function.

```yaml
ChapterFunction:
  primary:
  secondary:
  forbidden_repeat_of_previous_chapter: true
```

Forbidden:

```text
Chapter 4: heroine feels shame in temple
Chapter 5: heroine feels shame in temple again
Chapter 6: heroine feels shame in temple, but with better adjectives
```

Allowed:

```text
Chapter 4: temple offers clean identity
Chapter 5: temple tests body and creates public shame
Chapter 6: temple tries to monetize / classify / weaponize shame
```

Same location is allowed. Same function is not.

---

### 11.6 State Lane Rule

Each chapter must change at least two lanes.

```yaml
StateLanes:
  PlotState:
  CharacterState:
  RelationshipState:
  WorldState:
  ArchetypalState:
  SymbolicState:
  ReaderState:
```

Minimum:

```text
2 lanes changed = acceptable
3 lanes changed = strong
4+ lanes changed = major chapter / turning point
1 lane changed = weak chapter
0 lanes changed = remove or merge
```

---

## 12. Chapter Contract v3

```yaml
CHAPTER_CONTRACT_V3:
  chapter_id:
  title:
  chapter_index:
  total_chapters:
  arc_ratio:
  milestone:

  pov:
  narrative_mode:
  primary_function:
  secondary_function:

  character_core_pressure:
    affected_key_characters:
    wound_pressure:
    lie_pressure:
    mask_pressure:
    desire_vs_need_pressure:
    thesis_challenge:

  opening_state:
    plot:
    character:
    relationship:
    world:
    archetypal:
    symbolic:
    reader:

  required_external_event:
  required_internal_event:
  required_choice:
  required_cost:
  required_dirty_reward:
  required_power_shift:
  required_symbol_mutation:

  reader_hook_in:
  reader_question_to_answer:
  reader_question_to_open:
  micro_payoff:
  final_hook:

  scene_count:
  scene_functions:

  forbidden_drift:
  forbidden_repetition:
  continuity_requirements:

  closing_state:
    plot:
    character:
    relationship:
    world:
    archetypal:
    symbolic:
    reader:
```

---

## 13. Scene Contract v3

```yaml
SCENE_CONTRACT_V3:
  scene_id:
  chapter_id:
  scene_index:
  location:
  time:
  pov:
  participants:

  scene_type: action / dialogue / ritual / social_attack / intimacy / discovery / choice / aftermath / transition
  narrative_mode:

  participant_core_pressure:
    active_character:
    wound_pressured:
    lie_protected_or_broken:
    mask_performed_or_cracked:
    conscious_desire_pursued:
    real_need_resisted_or_accepted:
    thesis_challenged:

  input_state:
    plot:
    character:
    relationship:
    world:
    archetypal:
    symbolic:
    reader:

  surface_goal:
  hidden_goal:
  obstacle:
  pressure_source:
  conflict_type:

  mask_defended:
  shadow_pressure:
  desire_pressure:
  fear_pressure:
  status_pressure:
  body_pressure:

  turn:
  cost:
  reward:
  dirty_reward:
  new_information:
  power_shift:
  symbol_action:

  sensory_anchor:
  concrete_world_detail:
  object_anchor:
  bodily_anchor:

  line_of_no_return:
  exit_hook:

  output_state:
    plot:
    character:
    relationship:
    world:
    archetypal:
    symbolic:
    reader:
```

### Scene Rule

A scene must contain:

```text
Want → Pressure → Action → Turn → Cost → New State
```

If there is no cost, it is not drama.  
If there is no new state, it is wallpaper with feelings.

---

## 14. Engagement Engine

This is the addiction layer. Not cheap cliffhangers every paragraph like a mobile game begging for your soul, but controlled reader hunger.

### 14.1 Engagement Unit

Every scene/chapter needs:

```yaml
EngagementUnit:
  hook:
  immediate_want:
  friction:
  danger:
  novelty:
  emotional_contrast:
  turn:
  partial_payoff:
  new_question:
```

### 14.2 Open / Close / Deepen Rule

Every chapter should:

```text
Close 1 loop.
Open 1 loop.
Deepen 1 loop.
```

Example:

```yaml
ClosedLoop: why the temple wants her
OpenedLoop: who bought the record of her status
DeepenedLoop: the stain is not a curse but a door
```

If chapters only open loops, reader feels scammed.  
If chapters only close loops, tension dies.  
If chapters only deepen loops, the book becomes fog with punctuation.

---

### 14.3 Page-Turn Pressure

Each chapter ending must create at least one:

```text
danger_next
desire_next
truth_next
shame_next
romance_next
revenge_next
status_next
mystery_next
choice_next
```

Weak ending:

```text
She went to sleep.
```

Strong ending:

```text
She went to sleep after learning that the dream will be used as evidence against her.
```

---

### 14.4 Micro-Payoff Rule

Every chapter must reward the reader with something:

```text
new fact
new status
new intimacy
new danger
new symbol meaning
new power
new humiliation
new moral complication
new impossible choice
```

No chapter may exist only to «set up later».  
Readers do not pay rent to your future genius.

---

## 15. Pacing System

### 15.1 Rhythm Bands

Each chapter should deliberately mix rhythm bands.

```yaml
PacingBands:
  pressure:
    function: active conflict, pursuit, public attack, threat
  compression:
    function: consequences, trapped choice, narrowing options
  breath:
    function: emotional contrast, intimacy, sensory grounding
  revelation:
    function: new information changes meaning
  rupture:
    function: irreversible event
```

Rule:

```text
Do not write more than 2 chapters in the same dominant pacing band.
```

### 15.2 Anti-Monotony Control

Track:

```yaml
RecentChapterPattern:
  dominant_location:
  dominant_emotion:
  dominant_conflict:
  dominant_symbol:
  dominant_sentence_rhythm:
  dominant_scene_type:
```

If the next chapter repeats 3+ fields without mutation, correct before prose.

---

## 16. World / Social Physics

Worldbuilding must produce pressure, not encyclopedia dandruff.

```yaml
WorldRule:
  rule_id:
  public_rule:
  hidden_rule:
  who_enforces:
  who_benefits:
  who_suffers:
  punishment_for_breaking:
  loophole:
  scene_demonstration:
```

### Required Social Physics Questions

```text
Who owns names?
Who owns bodies?
Who owns records?
Who owns violence?
Who owns purity?
Who owns money?
Who owns desire?
Who owns truth?
Who profits when the protagonist is classified?
```

A strong world has bureaucracy, prices, penalties, rituals, jobs, documents, gossip and enforcement.  
Otherwise it is just mist, castles and vibes, the holy trinity of lazy fantasy.

---

## 17. Relationship / Romance System

Even non-romance stories need relational pressure. Romance projects need it like oxygen.

```yaml
RelationshipArc:
  participants:
  start_dynamic:
  end_dynamic:
  attraction_vector:
  fear_vector:
  trust_vector:
  power_imbalance:
  forbidden_desire:
  conscious_desire:
  subconscious_real_need:
  boundary:
  debt:
  betrayal_risk:
  intimacy_thresholds:
  public_consequence:
```

### 17.1 Romantic Addiction Loop

For romance / romantasy / dark feminine fantasy:

```text
Seen → Misread → Protected / Threatened → Desired → Shamed → Chosen → Cost → Deeper Risk
```

Every romantic beat must answer:

```text
What does this person see that others do not?
What does the protagonist want from them but cannot admit?
What would accepting this desire cost?
Is protection becoming ownership?
Is desire becoming loss of self?
Is intimacy giving power or taking it?
```

If romance is required by market, place relational escalation on the chapter map.  
Do not leave it as random garnish after the plot, like parsley on institutional trauma.

---

## 18. Advanced Narrative Systems

Enable per project.

```yaml
AdvancedNarrativeSystems:
  FractalStructureSystem:
    enabled:
    rule: every arc/chapter/scene repeats the core transformation at different scale

  ShadowPressureSystem:
    enabled:
    rule: every major scene pressures persona vs shadow

  OpenLoopSystem:
    enabled:
    rule: close one loop, open one loop, deepen one loop

  SymbolMutationSystem:
    enabled:
    rule: detail becomes motif, motif becomes symbol, symbol becomes revelation

  ForbiddenDesireSystem:
    enabled:
    rule: character wants something that violates self-image

  DoubleBindSystem:
    enabled:
    rule: every important choice wounds something

  RitualRepetitionSystem:
    enabled:
    rule: repeat key scene pattern 3 times with changed meaning

  ReaderStateSystem:
    enabled:
    rule: track what reader knows, suspects, wants, misunderstands

  EmotionalContrastSystem:
    enabled:
    rule: mix emotions instead of pure single-tone scenes

  ArchetypalMisdirectionSystem:
    enabled:
    rule: apparent archetype later reveals deeper function

  PowerShiftSystem:
    enabled:
    rule: every scene changes who has leverage

  DirtyRewardSystem:
    enabled:
    rule: every gain has poison attached

  KnowledgeAsymmetrySystem:
    enabled:
    rule: character A says X, B hears Y, reader understands Z, hidden observer plans W

  ChapterCountCompiler:
    enabled:
    rule: known N chapters must receive milestone mapping before prose

  PredictorCorrectorSystem:
    enabled:
    rule: generate predicted arc, validate, correct structural drift before style
```

---

## 19. Double Bind System

A strong dilemma hurts either way.

```text
accept help = admit weakness
reject help = lose protection

tell truth = destroy trust
hide truth = betray self

use power = become visible
hide power = remain owned

accept love = risk ownership
reject love = lose chosen safety

obey system = survive as object
break system = become hunted subject
```

Every major chapter should contain at least one double bind.

---

## 20. Dirty Reward System

The protagonist gets what they wanted, but with poison attached.

```text
gets power → becomes target
gets protection → owes debt
gets truth → loses innocence
wins status → becomes watched
gets love → loses old identity
gets freedom → loses safety
gets purity → loses desire
gets desire → loses purity
```

Dirty rewards create forward momentum because the reader gets payoff and complication at the same time. Efficient little misery engine. Good.

---

## 21. Predictor-Corrector Layer v3

### 21.1 Purpose

The Predictor-Corrector layer prevents AI-generated books from becoming:

```text
beautiful repetition
arc drift
middle sag
symbol spam
chapter filler
scene tourism
character amnesia
```

### 21.2 Predictor Pass

Before prose:

```yaml
PredictorPass:
  predicted_global_arc:
  predicted_chapter_map:
  predicted_midpoint:
  predicted_crisis:
  predicted_final_choice:
  predicted_symbol_mutations:
  predicted_key_character_core_turns:
  predicted_reader_questions:
  predicted_risk_of_sag:
  predicted_repetition_zones:
```

### 21.3 Corrector Pass

Then correct:

```yaml
CorrectorPass:
  missing_milestones:
  overloaded_chapters:
  repeated_functions:
  weak_state_lanes:
  decorative_scenes:
  delayed_payoffs_without_reward:
  symbol_overuse:
  flat_relationships:
  underbuilt_character_cores:
  absent_world_rules:
  climax_too_early:
  climax_too_late:
  midpoint_missing:
  chapter_count_mismatch:

  required_corrections:
    -
```

### 21.4 Correction Rules

```text
If midpoint is absent: insert revelation/power gain that changes previous meaning.
If 2+ adjacent chapters share same function: merge, mutate or replace one.
If chapter changes fewer than 2 lanes: add event, cost, revelation or relationship turn.
If symbol repeats without mutation: change function or remove mention.
If reader gets no micro-payoff: add answer, gain, danger or emotional turn.
If protagonist only reacts for 3+ scenes: force a choice with cost.
If world pressure is abstract: add institution, price, document, witness, punishment or loophole.
```

---

## 22. Style Renderer

Style is renderer, not core logic.

```yaml
StyleRenderer:
  format: prose / screenplay / quest / dialogue / design_doc / cutscene
  language:
  tense:
  pov:
  sentence_rhythm:
  sensory_density:
  metaphor_type:
  irony_level:
  emotional_palette:
  violence_level:
  romance_level:
  erotic_charge_level:
  humor_level:
  exposition_density:
  dialogue_style:
  inner_monologue:
  pacing:
  taboo_words:
  genre_markers:
  chapter_ending_style:
```

Core rule:

```text
Structure first.
State second.
Emotion third.
Style fourth.
Prose last.
```

### 22.1 Anti-LLM Prose Filter

Before accepting prose, check:

```yaml
AntiLLMFilter:
  repeated_sentence_starts:
  overused_short_fragments:
  repeated_symbol_words:
  abstract_emotion_without_body:
  inner_monologue_explaining_obvious:
  same_scene_shape_repeated:
  too_many_rhetorical_negations:
  too_many_paragraphs_that_only_state_theme:
  lack_of_concrete_world_objects:
  lack_of economic_or_social_specifics:
```

### 22.2 Concrete Anchor Rule

Every scene should contain at least 2:

```text
object anchor
body anchor
money/status anchor
legal/bureaucratic anchor
sensory anchor
spatial anchor
social witness
```

This keeps prose from floating away into a scented existential cloud. Humanity already has enough clouds.

---

## 23. Validation Gates

### 23.1 Project Gate

Before writing:

```text
1. Core Promise defined?
2. Protagonist start/end state defined?
3. Protagonist wound, lie, mask, conscious desire, real need and thesis defined?
4. Every key character has Character Core Contract?
5. Main lie and forbidden desire defined?
6. World pressure defined?
7. Main symbols defined?
8. State Ledger / Debug Vector schema defined?
9. Target chapter count known or unknown mode selected?
10. Global Arc Geometry complete?
11. Chapter Count Map compiled?
```

If any fail: do not write prose.

---

### 23.2 Chapter Gate

Every chapter must pass:

```text
1. Has primary function?
2. Has unique function compared to previous chapter?
3. Maps to milestone or expansion function?
4. Changes at least 2 state lanes?
5. Has hook in and hook out?
6. Closes 1 loop?
7. Opens 1 loop?
8. Deepens 1 loop?
9. Has micro-payoff?
10. Has cost?
11. Has power shift?
12. Has symbol mutation or deliberate symbol absence?
13. Pressures at least one key character core contract?
14. Has concrete world/social pressure?
15. Has CHAPTER_VECTOR?
16. Produces AFTER State Snapshot?
17. Updates Continuity Log?
```

If 3 or more fail, rewrite chapter contract before style.

---

### 23.3 Scene Gate

Every scene must pass:

```text
1. Who wants what?
2. What blocks them?
3. What pressure acts now?
4. What changes by the end?
5. What is the cost?
6. What does reader learn / fear / want next?
7. Which state lanes change?
8. Is at least one participant acting from wound, lie, mask, desire, need or thesis pressure?
9. Is there a power shift?
10. Is there body/world grounding?
11. Is it non-redundant?
```

If no one wants anything, delete scene.  
If nothing changes, merge scene.  
If only style is good, bury it with appropriate sarcasm.

---

## 24. Chapter Map Template

Use this table before writing.

```markdown
| Chapter | Arc Ratio | Milestone | Primary Function | Secondary Function | State Lanes Changed | Reader Payoff | Final Hook |
|---:|---:|---|---|---|---|---|---|
| 1 | 0.00 | Mask |  |  |  |  |  |
| 2 | 0.08 | Irritant |  |  |  |  |  |
| ... | ... | ... | ... | ... | ... | ... | ... |
```

### Required State Lane Notation

```text
PLOT
CHAR
REL
WORLD
ARCH
SYMB
READER
```

Example:

```text
State Lanes Changed: CHAR + WORLD + READER
```

---

## 25. Runtime Prompt: Build Full Project v3

```markdown
# TASK: Build OUT Prometheus Narrative Project v3

You are working inside OUT Prometheus Narrative Engine v3.

Do not write final prose yet.

## Input

ProjectInput:
[paste filled ProjectInput]

## Required Work

1. Define Core Promise.
2. Define Narrative Mode.
3. Define protagonist start state, end state, wound, lie, mask, conscious desire, subconscious real need, thesis, antithesis and synthesis target.
4. Define Character Core Contracts for every key character.
5. Define World / Social Physics.
6. Define key entities, symbols, threats, factions, relationships.
7. Define State Variables.
8. Define State Ledger / Debug Vector schema.
9. Build Global Arc Geometry.
10. If chapter count is known, compile Chapter Count Map for N chapters.
11. If chapter count is unknown, compile Milestone Map first.
12. Build Chapter Contracts.
13. Build initial Chapter Vectors.
14. Validate chapter map.
15. Stop.

## Hard Rules

- Do not generate final prose.
- Every chapter must have unique primary function.
- Every chapter must change at least two state lanes.
- Every chapter must close one loop, open one loop, deepen one loop.
- Milestones must be distributed across chapter count.
- Midpoint and crisis must be placed deliberately.
- If chapter count is too small, compress milestones.
- If chapter count is large, expansion chapters must still change state.

## Output Format

1. Core Promise
2. Narrative Mode
3. Character Core Contracts
4. State Variables
5. State Ledger / Debug Vector Schema
6. Global Arc Geometry
7. Chapter Count Map
8. Chapter Vectors
9. Chapter Contracts
10. Validation Report
11. Questions / Missing Data
```

---

## 26. Runtime Prompt: Write Chapter v3

```markdown
# TASK: Write Chapter Using OUT Prometheus v3

You are writing inside OUT Prometheus Narrative Engine v3.

## Input

ProjectCore:
[paste]

GlobalArcGeometry:
[paste]

ChapterCountMap:
[paste]

CurrentContinuityLog:
[paste]

PreviousStateSnapshot:
[paste]

ChapterVector:
[paste]

ChapterContract:
[paste]

StyleRenderer:
[paste]

## Required Before Prose

Briefly state:
1. Chapter function.
2. Key character core pressure: wound / lie / mask / desire / need / thesis.
3. State lanes that must change.
4. ChapterVector and active bitmasks.
5. Reader loop plan: close/open/deepen.
6. Symbol mutation.
7. Power shift.
8. Final hook.

Then write the chapter.

## Hard Rules

- Do not contradict ContinuityLog.
- Do not repeat previous chapter function.
- Do not make decorative scenes.
- Every scene must have want, pressure, turn, cost, output state.
- Do not overexplain symbols.
- Keep concrete world/body/status anchors.

## Output

1. Structural Preflight
2. Chapter Text
3. AFTER State Snapshot
4. Continuity Update
5. Validation Checklist
```

---

## 27. Runtime Prompt: Validate Existing Draft v3

```markdown
# TASK: Validate Draft With OUT Prometheus v3

Analyze the provided draft as a structural system.

## Input

Draft:
[paste chapter / whole book]

KnownChapterCount:
[N or unknown]

ProjectCore:
[paste if available]

## Check

1. Does the draft have a clear Global Arc Geometry?
2. Are milestones distributed correctly for the chapter count?
3. Does each chapter have unique primary function?
4. Does each chapter change at least two state lanes?
5. Do all key characters have wound, lie, mask, conscious desire, real need and thesis?
6. Does each key character scene pressure at least one of those core elements?
7. Can each chapter be represented as a CHAPTER_VECTOR?
8. Does each chapter produce a clear AFTER State Snapshot?
9. Is there a midpoint reversal?
10. Is there a crisis / symbolic death?
11. Is there an integration choice?
12. Are symbols mutating or merely repeating?
13. Is ReaderState managed through open/close/deepen loops?
14. Are scenes grounded in concrete world/body/status anchors?
15. Are there repeated LLM patterns in prose rhythm?
16. Which chapters should be merged, expanded, moved, or rewritten?

## Output

1. Structural Verdict
2. Chapter-by-Chapter Table
3. Missing Milestones
4. Weak Chapters
5. Repetition / Drift Report
6. Required Fixes
7. Optional Style Fixes
```

---

## 28. Generation Algorithm v3

```text
1. Read ProjectInput.
2. Determine known_chapter_count.
3. Build Core Ontology.
4. Build Character Core Contracts for every key character.
5. Validate that every key character has wound, lie, mask, conscious desire, subconscious real need and thesis.
6. Build World / Social Physics.
7. Build State Variables.
8. Build StateLedger / Debug Vector schema.
9. Build GlobalArcGeometry.
10. Use ChapterCountCompiler.
11. Assign milestone to chapter index.
12. Assign unique primary function to every chapter.
13. Assign minimum state lane changes.
14. Assign key character core pressure per chapter.
15. Assign reader loop plan.
16. Assign symbol mutation plan.
17. Assign relationship/world pressure.
18. Generate ChapterVectors and bitmasks.
19. Validate map.
20. Generate chapter contracts.
21. Generate scene contracts.
22. Write prose.
23. Run validation.
24. Correct structure.
25. Correct style.
26. Export AFTER StateSnapshot.
27. Update ContinuityLog and StateLedger_DEBUG.
```

---

## 29. Final Formula

```text
OUT Prometheus v3 = Narrative ECS + Character Core Contracts + Data-Driven State Ledger + Chapter Vectors + Archetypal State Machine + Chapter Count Compiler + Reader Engagement Loop + Symbol Mutation + Predictor-Corrector + Style Renderer
```

Simpler:

```text
Book = transformation path compiled into N state-changing chapters.
```

Even simpler:

```text
Не главы создают путь.
Путь распределяется по главам.
```

