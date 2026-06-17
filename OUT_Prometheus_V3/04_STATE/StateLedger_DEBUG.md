# StateLedger_DEBUG — PHASE 6.5

> **Статус:** `APPROVED` — схема 2026-06-15  
> **Движок:** `OUT_Prometheus_Narrative_Engine_v3_1.md` §6.6  
> **Правило:** AFTER snapshot обязателен после каждой главы. Проза без ledger — нарушение Chapter Gate.

---

## Как пользоваться

1. Перед главой — скопировать предыдущий **AFTER** в **BEFORE**.
2. Заполнить **VECTOR** из `06_CONTRACTS/ChapterMap.md` (или контракта главы).
3. После прозы — заполнить **AFTER** + **DELTA REPORT**.
4. Факты тела/предметов — экспорт в `ContinuityLog.md`.

Шкала векторов: **0–5** (v3_1). Bitmasks — см. `StateVariables.md` §5.

---

## PR_000 — Проваленный эксперимент

### BEFORE

> См. `StateVariables.md` §3 — `STATE_SNAPSHOT PR_000 before`

### VECTOR

> См. `06_CONTRACTS/ChapterMap.md` → `PR_000`

### AFTER

```yaml
STATE_SNAPSHOT:
  chapter_id: PR_000
  snapshot_type: after

  plot_state:
    current_phase: threshold_crossed
    active_conflict: перенос инициирован
    unresolved_hooks: [куда перенесён; Пол остался на Земле]
    irreversible_events: [касание SYM_BUNKER_PATTERN; потеря Земли]

  world_state:
    location_id: transition  # → CH_001 LOC_SERRAT_SQUARE_FORM
    magic_environment: порог активен

  reader_state:
    knows: [Марк на земле у столба; 2 прута и спрыгнул; Пол на вершине; узор тронут]
    suspects: [перенос связан с узором]
    wants: [где он теперь]
    fears: [тело снова не справится]
    current_question: что за мир на другой стороне?

  character_states:
    - character_id: CHAR_MARK
      wound_pressure: 4
      lie_strength: 3
      mask_integrity: 2
      conscious_desire_pressure: 4
      thesis_state: challenged
      current_strategy: бравада провалилась на вышке
    - character_id: CHAR_POL
      note: взял вершину вышки; остался в бункере на Земле; «Марк…» оборвано
```

### DELTA REPORT

- State lanes changed: PLOT, CHAR, ARCH, SYMB, READER (51)
- Character core contracts pressured: MARK wound+lie+mask+desire+thesis; POL mask+desire
- World facts changed: земной день закрыт; порог открыт
- Reader loops closed/opened/deepened: closed обычный день; opened перенос; deepened страх высоты
- Continuity facts to export: ContinuityLog PR_000
- Risks for next chapter: CH_001 читает AFTER как пробуждение; не повторять бункер

---

## CH_001 — Настоящие мечи

### BEFORE

> См. `StateVariables.md` §4 — `STATE_SNAPSHOT CH_001 before`

### VECTOR

> См. `06_CONTRACTS/ChapterMap.md` → `CH_001`

### AFTER

```yaml
STATE_SNAPSHOT:
  chapter_id: CH_001
  snapshot_type: after
  loop_iteration: 1

  plot_state:
    current_phase: irritant_or_call
    active_conflict: казнь vs мёртвый спаситель
    unresolved_hooks: [петля намечена, Нулевая невеста, кто такой чужак]
    irreversible_events: [первая смерть Марка на площади]

  world_state:
    power_balance: 5
    law_response: 4
    faction_tensions: 3
    magic_environment: стабильный фон; колокольное разрешение
    rumor_level: 3
    location_id: LOC_SERRAT_SQUARE_FORM

  reader_state:
    knows: [казнь процедура; Ренна; Лиэрра Вейл; Марк умер; красота обман]
    suspects: [возврат возможен; день «не затвердел»]
    wants: [второй шанс; понять правила]
    fears: [боль повторится]
    current_question: вернётся ли он до удара меча?

  character_states:
    - character_id: CHAR_MARK
      wound_pressure: 5
      lie_strength: 2
      mask_integrity: 1
      conscious_desire_pressure: 4
      real_need_visibility: 1
      shadow_pressure: 2
      agency_level: 1
      thesis_state: challenged
      current_strategy: инстинкт спасителя провалился
    - character_id: CHAR_LIERRA
      wound_pressure: 5
      mask_integrity: 4
      thesis_state: intact
      note: «ещё один» — взгляд; казнь продолжена/завершена в итерации
    - character_id: CHAR_RENNA_KALD
      lie_strength: 5
      mask_integrity: 5
      thesis_state: intact

  relationship_deltas:
    CHAR_MARK_CHAR_LIERRA:
      trust: 0
      fear: 3
      attraction: 2
      debt: 0
      ownership_pressure: 3
```

### DELTA REPORT

- State lanes changed: PLOT, CHAR, REL, WORLD, ARCH, SYMB, READER (127)
- Character core contracts pressured: MARK wound+lie+mask+desire+thesis+shadow; LIERRA wound+mask; RENNA lie+mask+thesis
- World facts changed: Орден Люциуса процедура казни; Серрат; стража фиксирует чужаков
- Reader loops closed/opened/deepened: closed «красивый мир»; opened петля; deepened цена настоящего
- Continuity facts to export: см. ContinuityLog CH_001
- Risks for next chapter: не усилить Марка; петля CH_002 — бегство; стража помнит чужака

---

## CH_002 — Та же картина

### BEFORE

> CH_001 AFTER; тело reset; loop_iteration → 2

### VECTOR

> ChapterMap.md → CH_002

### AFTER

```yaml
STATE_SNAPSHOT:
  chapter_id: CH_002
  snapshot_type: after
  loop_iteration: 3  # начало 2-й попытки → сбой в конце → 3-е пробуждение

  plot_state:
    current_phase: descent_deepens
    active_conflict: петля + стража vs бегство
    unresolved_hooks: [правила петли; «утро»; допрос]
    irreversible_events: [второе вмешательство зафиксировано]

  world_state:
    law_response: 5
    rumor_level: 4
    location_id: LOC_SERRAT_SQUARE_FORM → подворотня (итерация)

  reader_state:
    knows: [бегство не работает; Ренна фиксирует повтор; казнь повторяется]
    suspects: [петля = расписание утра; стража помнит факт чужака]
    current_question: что будет, если не бежать и не лезть на помост?

  character_states:
    - character_id: CHAR_MARK
      wound_pressure: 5
      lie_strength: 2
      mask_integrity: 1
      agency_level: 1
      thesis_state: challenged
      fear: 5
    - character_id: CHAR_LIERRA
      note: взгляд «опять»; казнь повторена в итерации

  LoopState:
    mark_loop_memory: [LOC_SERRAT_SQUARE_FORM, CHAR_RENNA_KALD, повторное вмешательство, подворотня]
    choice_point_active: true
```

### DELTA REPORT

- State lanes changed: PLOT, CHAR, WORLD, ARCH, READER
- Character core pressured: MARK wound+lie+thesis; LIERRA mask+wound (взгляд)
- World: чужак в протоколе; стража активнее
- Reader: closed «убежать»; opened правила петли
- Continuity: ContinuityLog CH_002
- Risks CH_003: контакт с Лиэррой; не повторять бегство

---

## CH_003 — Улица и высота

### BEFORE

> CH_002 AFTER; loop_iteration 3; колокол не звонил

### VECTOR

> ChapterMap.md → CH_003; bitmask 110

### AFTER

```yaml
STATE_SNAPSHOT:
  chapter_id: CH_003
  snapshot_type: after
  loop_iteration: 3  # итерация внутри главы; казнь не завершена в тексте

  plot_state:
    current_phase: threshold_crossed
    active_conflict: контакт vs ярлык vs процедура
    unresolved_hooks: [сколько раз умирал; след у глаз; казнь в этой итерации]
    irreversible_events: [ярлык «уличная собака Нулевой невесты»; слух зафиксирован Ренной]

  world_state:
    rumor_level: 5
    law_response: 5
    location_id: LOC_SERRAT_SQUARE_FORM
    labels_public: [уличная собака Нулевой невесты]

  reader_state:
    knows: [Лиэрра не святая — метки у глаз; она проверяет его; ярлык собаки]
    suspects: [Лиэрра чует повтор; «опоздал» / «снова»]
    wants: [понять её; не усугубить]
    fears: [он — её новая цепь]
    current_question: услышит ли он «не спасай» до следующей смерти?

  character_states:
    - character_id: CHAR_MARK
      wound_pressure: 5
      lie_strength: 2
      conscious_desire_pressure: 5
      real_need_visibility: 2
      thesis_state: challenged
      current_strategy: слово вместо бегства/помоста — провалилось
    - character_id: CHAR_LIERRA
      wound_pressure: 5
      mask_integrity: 3
      shadow_pressure: 2
      thesis_state: challenged
      note: «сколько раз умирал»; «не спасай»; метки у глаз скрыты

  relationship_deltas:
    CHAR_MARK_CHAR_LIERRA:
      trust: 0
      fear: 4
      attraction: 2
      debt: 1
      ownership_pressure: 4
      verification_active: true

  LoopState:
    mark_loop_memory: [ярлык_собаки, след_у_глаз, лиэрра_проверяет, не_спасай]
    lierra_loop_suspicion: 2
    choice_point_active: true
```

### DELTA REPORT

- State lanes changed: CHAR, REL, WORLD, SYMB, READER (110)
- Character core pressured: MARK desire+thesis+need; LIERRA wound+mask+shadow+thesis
- World: rumor_level ↑; публичный ярлык; Ренна фиксирует слухи
- Reader: closed сила/бегство как стратегия; opened кто Лиэрра; deepened навязчивость vs холод
- Continuity: ContinuityLog CH_003
- Risks CH_004: кузница; не романтизировать «не спасай»; механика казни

---

## CH_004 — Кузница упадка

### BEFORE

> CH_003 AFTER; loop_iteration 3; удержание после казни

### VECTOR

> ChapterMap.md → CH_004; bitmask 127; LOC_BLADE_QUARTER

### AFTER

```yaml
STATE_SNAPSHOT:
  chapter_id: CH_004
  snapshot_type: after
  loop_iteration: 3

  plot_state:
    current_phase: first_cost_paid
    active_conflict: система казни vs инженерия исходов (зародыш)
    unresolved_hooks: [допрос Ренны; Лиэрра обнулена в итерации; сломать график]
    irreversible_events: [видел регламент казни; вещи разоблачённой; унижение у молота]

  world_state:
    location_id: LOC_BLADE_QUARTER → улица Серрата
    rumor_level: 5
    law_response: 5
    factions_visible: [HOUSE_KELRAY, HOUSE_HOLMGAR, HOUSE_VARDESS, HOUSE_ESCARN]

  reader_state:
    knows: [казнь = производство; серии клинков; каждый элемент с подписью]
    suspects: [щель между плавкой и колоколом]
    wants: [найти точку ломания процесса]
    fears: [он только зритель цеха]
    current_question: кого/что подставить под регламент (CH_005)

  character_states:
    - character_id: CHAR_MARK
      lie_strength: 1
      mask_integrity: 0
      thesis_state: challenged
      conscious_desire_pressure: 4
      real_need_visibility: 2
      current_strategy: запомнить механику; не лезть на помост
    - character_id: CHAR_LIERRA
      note: обнуление в итерации (off-page); вещи на столе учёта

  relationship_deltas:
    CHAR_MARK_CHAR_LIERRA:
      debt: 2
      note: «не спасай» прочитано как подсказка к процессу

  LoopState:
    mark_loop_memory: [механика_казни, квартал_клинков, кельрай, серия_В, график_поставки]
```

### DELTA REPORT

- State lanes changed: PLOT, CHAR, REL, WORLD, ARCH, SYMB, READER (127)
- Character core pressured: MARK lie+mask+thesis cracked
- World: HOUSE_KELRAY контуры; цепочка домов в казни
- Reader: closed драма казни; opened ломать процесс; deepened тошнота регламента
- Continuity: ContinuityLog CH_004
- Risks CH_005: месть через закон; не тиран; страж-паттерн

---

## CH_005 — Первая попытка мести

> CH_004 AFTER; loop_iteration 4; первая инженерия исхода

### VECTOR

> ChapterMap.md → CH_005; bitmask 127; LOC_SERRAT_SQUARE_FORM + подплощадь

### AFTER

```yaml
STATE_SNAPSHOT:
  chapter_id: CH_005
  snapshot_type: after
  loop_iteration: 4

  plot_state:
    current_phase: first_cost_paid
    active_conflict: инженерия исходов vs замена смены системой
    unresolved_hooks: [ревизия стража; завтра новый контур; допрос Ренны отложен]
    irreversible_events: [подпись свидетеля; фиксация святотатства контура; страж отстранён]

  world_state:
    location_id: LOC_SERRAT_SQUARE_FORM → коридор под площадью
    rumor_level: 5
    law_response: 6
    factions_visible: [HOUSE_KREINMAR, HOUSE_HOLMGAR, Орден Люциуса]

  reader_state:
    knows: [паттерн стража у цепи; святотатство = контаминация контура; толпа переключает жертву]
    suspects: [Лиэрра одобрила «переключай»; система закроет щель новой сменой]
    wants: [следующий рычаг — цепь, колокол, график]
    fears: [сладость мести через закон]
    current_question: кого съест толпа, когда переключатель исчерпан (CH_006)

  character_states:
    - character_id: CHAR_MARK
      lie_strength: 1
      mask_integrity: 0
      thesis_state: challenged → engineering
      conscious_desire_pressure: 4
      real_need_visibility: 2
      shadow_pressure: 3
      current_strategy: переключать строки регламента, не лезть на помост
    - character_id: CHAR_LIERRA
      agency_signal: 4
      note: жива в итерации; «переключай»; предупреждение о голоде толпы
    - character_id: NPC_GUARD_MERZKY
      note: страж с ожогом на костяшках; отстранён; объект травли; имя не названо

  relationship_deltas:
    CHAR_MARK_CHAR_LIERRA:
      debt: 2
      trust_shift: pragmatic_acknowledgment
      note: не спасение — перенос груза; вечер куплен ценой подписи
    CHAR_MARK_NPC_GUARD_MERZKY:
      hostility: 2
      note: подставлен по закону; видит «после вкусил»

  LoopState:
    mark_loop_memory: [паттерн_стража_цепи, святотатство_контура, крейнмар_фиксация, толпа_переключатель, сладость_закона]
```

### DELTA REPORT

- State lanes changed: PLOT, CHAR, REL, WORLD, ARCH, SYMB, READER (127)
- Character core pressured: MARK desire+shadow+moral_limit; LIERRA shadow+desire
- World: Kreinmar печать; Holmgar контур снят; rumor_level 5→5 (фиксирован), law_response ↑
- Reader: closed спасение силой; opened закон как оружие; deepened яд сладости
- Continuity: ContinuityLog CH_005
- Risks CH_006: обида на стража (Мерзкий); не тиранизировать Марка; подполье

---

## CH_006 — CH_013

> Слоты создаются при написании. Шаблон для каждой главы:

```markdown
## CH_XXX — [title]

### BEFORE
[previous AFTER]

### VECTOR
[from ChapterMap]

### AFTER
[STATE_SNAPSHOT after]

### DELTA REPORT
- State lanes changed:
- Character core contracts pressured:
- World facts changed:
- Reader loops closed/opened/deepened:
- Continuity facts to export:
- Risks for next chapter:
```

---

## Утверждение PHASE 6.5

```yaml
approval:
  status: APPROVED
  approved_date: 2026-06-15
  note: слоты PR_000–CH_001 готовы; CH_002+ по мере прозы
```