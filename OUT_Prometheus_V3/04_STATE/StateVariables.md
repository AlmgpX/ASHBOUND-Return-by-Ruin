# State Variables — PHASE 6

> **Статус:** `APPROVED` — 2026-06-15  
> **Движок:** `OUT_Prometheus_Narrative_Engine_v3_1.md` §6  
> **Ledger:** `StateLedger_DEBUG.md` (PHASE 6.5)  
> **Continuity:** `ContinuityLog.md`

---

## 0. Правила

```yaml
project: "Не спасай её"
vol: 1
units: [PR_000, CH_001, CH_002, CH_003, CH_004, CH_005, CH_006, CH_007, CH_008, CH_009, CH_010, CH_011, CH_012, CH_013]
vector_scale: 0-5  # v3_1 §6.2–6.3
thesis_states: [intact, challenged, cracked, broken, transformed]

snapshot_rule: |
  Глава не завершена без AFTER snapshot в StateLedger_DEBUG.
  Следующая глава читает предыдущий AFTER как BEFORE.

loop_persist_rule: |
  Между итерациями петли persist только LoopState (память Марка, инженерия исходов).
  Тело, инвентарь, раны мира — reset каждую итерацию, кроме записи в ContinuityLog как «знает из петли».
```

---

## 1. Блоки состояния (схема v3_1)

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
  fear: 0-5
  core_wound_pressure: 0-5
  mask_integrity: 0-5
  shadow_pressure: 0-5
  agency_level: 0-5
  current_lie:
  thesis_state: intact | challenged | cracked | broken | transformed
  antithesis_pressure: 0-5
  synthesis_progress: 0-5
  current_choice_capacity: 0-5
  forbidden_desire_pressure: 0-5

RelationshipState:
  trust: 0-5
  fear: 0-5
  attraction: 0-5
  rivalry: 0-5
  debt: 0-5
  loyalty: 0-5
  betrayal_risk: 0-5
  power_balance: 0-5
  intimacy_level: 0-5
  ownership_pressure: 0-5

WorldState:
  current_power_balance: 0-5
  law_response: 0-5
  rumor_level: 0-5
  faction_tensions: 0-5
  economic_pressure: 0-5
  taboo_pressure: 0-5
  supernatural_pressure: 0-5
  magic_environment:  # фон стабилен / просадка / срыв
  location_id:
  location_changes:

ArchetypalState:
  stage:
  active_mask:
  active_shadow:
  threshold_status:
  descent_level: 0-5
  symbolic_death_progress: 0-5
  integration_progress: 0-5

SymbolicState:
  active_symbols: []
  symbol_mutations: {}
  motif_returns: []
  corrupted_symbols: []
  purified_symbols: []
  burned_symbols: []
  final_symbol_target:

ReaderState:
  knows: []
  suspects: []
  wants: []
  fears: []
  expects: []
  misunderstands: []
  current_question:
  promised_payoff:
  delayed_payoff:
  emotional_debt: 0-5
  next_page_pressure: 0-5
```

---

## 2. LoopState (проектное расширение — persist)

```yaml
LoopState:
  loop_iteration: 0
  choice_point_active: false
  mark_loop_memory: []       # id: LOC_, NPC_, WR_, SECRET_, MECH_
  tell_penalty_level: 0-5
  outcome_engineering_level: 0-5
  sweet_revenge_unlocked: false
  lierra_loop_memory_level: 0-5  # 0 → фрагменты CH_11 → частичная CH_13
```

---

## 3. STATE_SNAPSHOT — PR_000 BEFORE

```yaml
STATE_SNAPSHOT:
  chapter_id: PR_000
  snapshot_type: before

  plot_state:
    current_phase: mask_established
    active_conflict: страх высоты vs желание «настоящего»
    unresolved_hooks: [SYM_BUNKER_PATTERN, переезд]
    irreversible_events: []

  world_state:
    power_balance: n/a
    law_response: 0
    faction_tensions: 0
    economic_pressure: 0
    magic_environment: земной фон
    rumor_level: 0
    location_id: earth_bunker

  reader_state:
    knows: [Марк боится высоты; друг Пол]
    suspects: [символ на стене не случаен]
    wants: [перенос; цена «настоящего»]
    fears: [герой сломается раньше изменений]
    current_question: Что сделает узор при касании?

  character_states:
    - character_id: CHAR_MARK
      wound_pressure: 3
      lie_strength: 4
      mask_integrity: 4
      conscious_desire_pressure: 4
      real_need_visibility: 1
      shadow_pressure: 1
      agency_level: 2
      thesis_state: intact
      current_strategy: бравада реконструкции
    - character_id: CHAR_POL
      wound_pressure: 0
      mask_integrity: 3
      agency_level: 3
      thesis_state: intact
```

---

## 4. STATE_SNAPSHOT — CH_001 BEFORE (loop 1)

```yaml
STATE_SNAPSHOT:
  chapter_id: CH_001
  snapshot_type: before
  loop_iteration: 1

  plot_state:
    current_phase: irritant_or_call
    active_conflict: казнь Лиэрры vs инстинкт спасти
    unresolved_hooks: [перенос, Нулевая невеста, петля?]

  world_state:
    power_balance: 5
    law_response: 4
    faction_tensions: 3
    magic_environment: стабильный фон Серрата
    rumor_level: 3
    location_id: LOC_SERRAT_SQUARE_FORM

  reader_state:
    knows: [Марк на чужой площади; казнь]
    suspects: [иллюзия красоты города]
    current_question: Что будет, если бросится на помост?

  character_states:
    - character_id: CHAR_MARK
      wound_pressure: 4
      lie_strength: 4
      mask_integrity: 3
      conscious_desire_pressure: 5
      real_need_visibility: 1
      shadow_pressure: 2
      agency_level: 2
      thesis_state: intact
      current_misbelief: «реконструкция подготовила»
    - character_id: CHAR_LIERRA
      wound_pressure: 5
      lie_strength: 4
      mask_integrity: 4
      conscious_desire_pressure: 4
      real_need_visibility: 2
      shadow_pressure: 3
      agency_level: 3
      thesis_state: intact
    - character_id: CHAR_RENNA_KALD
      wound_pressure: 0
      lie_strength: 5
      mask_integrity: 5
      conscious_desire_pressure: 4
      agency_level: 4
      thesis_state: intact

  relationship_deltas_seed:
    CHAR_MARK_CHAR_LIERRA:
      trust: 0
      fear: 3
      debt: 0
      ownership_pressure: 4
```

---

## 5. Bitmasks (v3_1 §6.4–6.5)

```yaml
STATE_LANE_BITS:
  PLOT: 1
  CHAR: 2
  REL: 4
  WORLD: 8
  ARCH: 16
  SYMB: 32
  READER: 64

CHARACTER_CORE_BITS:
  WOUND: 1
  LIE: 2
  MASK: 4
  DESIRE: 8
  NEED: 16
  THESIS: 32
  SHADOW: 64
  MORAL_LIMIT: 128
```

---

## 6. Continuity-поля (факты тела / инвентарь)

> Заполняются в **ContinuityLog.md** после прозы; в snapshot — краткая ссылка `continuity_ref`.

```yaml
ContinuityFields:
  body_mark: []      # раны, синяки, рвота tell, высота
  body_lierra: []
  inventory_mark: []
  inventory_lierra: []
  status_mark: []    # ярлыки, статус в городе
  status_lierra: []
  experience_persist: []  # только то, что Марк помнит из петель
  npc_alive: {}      # сброс по петле; persist — знание паттерна
```

---

## 7. Feminine axes (сцены CHAR_LIERRA)

```yaml
FeminineSceneAxes:
  lierra_agency: 0-5
  trust_vs_control: 0-5
  ownership_pressure: 0-5
  mark_savior_fantasy: 0-5
  mutual_debt: 0-5
```

---

## 8. Утверждение PHASE 6

```yaml
approval:
  status: APPROVED
  approved_date: 2026-06-15
  aligned_to: OUT_Prometheus_Narrative_Engine_v3_1.md
```