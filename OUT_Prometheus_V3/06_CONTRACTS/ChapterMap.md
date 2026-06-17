# Chapter Count Map — PHASE 8

> **Статус:** `APPROVED` — 2026-06-15  
> **Движок:** `OUT_Prometheus_Narrative_Engine_v3_1.md` §11  
> **Units:** 14 (PR_000 + CH_001–CH_013)  
> **Источник событий:** `don't save her.md` § Карта глав

```yaml
ChapterCountMap:
  known_chapter_count: true
  total_units: 14
  prose_units: 14
  compiler_N: 14
  arc_geometry: 05_ARCS/GlobalArcGeometry.md
```

---

## PR_000 — Проваленный эксперимент

```yaml
CHAPTER_VECTOR:
  chapter_id: PR_000
  chapter_index: 0
  total_chapters: 14
  arc_ratio: 0.00
  milestone: mask_established
  title: Проваленный эксперимент

  primary_function: установить маску, страх, узор порога
  secondary_function: земной контраст перед Серратом

  state_lane_mask:
    PLOT: 1
    CHAR: 1
    WORLD: 0
    ARCH: 1
    SYMB: 1
    READER: 1
  state_lane_bitmask: 51  # PLOT+CHAR+ARCH+SYMB+READER

  pressure_vector:
    physical_danger: 2
    social_danger: 1
    moral_danger: 0
    intimacy_pressure: 1
    mystery_pressure: 4
    status_pressure: 1
    supernatural_pressure: 3

  character_core_pressure:
    - character_id: CHAR_MARK
      wound: 1
      lie: 1
      mask: 1
      desire: 1
      thesis: 1
      bitmask: 43  # WOUND+LIE+MASK+DESIRE+THESIS
    - character_id: CHAR_POL
      mask: 1
      desire: 1
      bitmask: 12

  reader_loop_vector:
    close_loop: []
    open_loop: [что за узор бункера]
    deepen_loop: [страх высоты Марка]
    micro_payoff: дошли до символа
    final_hook: касание → перенос

  output_requirements:
    irreversible_event: перенос (off-screen end)
    power_shift: Марк теряет земной контроль
    cost_paid: страх высоты не преодолён
    symbol_mutation: SYM_BUNKER_PATTERN активирован

  forbidden: гоблины; Серрат
```

---

## CH_001 — Настоящие мечи

```yaml
CHAPTER_VECTOR:
  chapter_id: CH_001
  chapter_index: 1
  arc_ratio: 0.07
  milestone: irritant_or_call + threshold_crossed
  title: Настоящие мечи

  primary_function: порог мира; первая смерть; иллюзия красоты рушится
  secondary_function: представить Лиэрру, Ренну, систему казни

  state_lane_mask: {PLOT: 1, CHAR: 1, REL: 1, WORLD: 1, ARCH: 1, SYMB: 1, READER: 1}
  state_lane_bitmask: 127

  pressure_vector:
    physical_danger: 4
    social_danger: 3
    moral_danger: 2
    mystery_pressure: 4
    status_pressure: 2
    supernatural_pressure: 3

  character_core_pressure:
    - character_id: CHAR_MARK
      wound: 1
      lie: 1
      mask: 1
      desire: 1
      thesis: 1
      shadow: 1
      bitmask: 107
    - character_id: CHAR_LIERRA
      wound: 1
      mask: 1
      desire: 1
      bitmask: 13
    - character_id: CHAR_RENNA_KALD
      lie: 1
      mask: 1
      desire: 1
      thesis: 1
      bitmask: 46

  reader_loop_vector:
    open_loop: [петля? кто такая Нулевая невеста]
    deepen_loop: [настоящее ≠ красиво]
    final_hook: смерть / возврат?

  output_requirements:
    irreversible_event: первая смерть Марка
    power_shift: система доминирует на помосте
    cost_paid: маска реконструкции разбита
    symbol_mutation: SYM_CHOICE_POINT + SYM_NULL_BRIDE

  forbidden: гоблины
```

---

## CH_002 — Та же картина

```yaml
CHAPTER_VECTOR:
  chapter_id: CH_002
  milestone: descent_deepens
  title: Та же картина
  primary_function: петля подтверждена; бегство не работает
  secondary_function: стража фиксирует чужака

  state_lane_bitmask: 99  # PLOT+CHAR+WORLD+ARCH+READER
  pressure_vector: {physical_danger: 3, social_danger: 4, status_pressure: 3}
  character_core_pressure:
    - character_id: CHAR_MARK
      wound: 1
      lie: 1
      fear_via_thesis: 1
      bitmask: 35
  output_requirements:
    cost_paid: новые мучения на площади
    new_information: петля возвращает на казнь
  forbidden: гоблины
```

---

## CH_003 — Улица и высота

```yaml
CHAPTER_VECTOR:
  chapter_id: CH_003
  arc_ratio: 0.21
  milestone: threshold_crossed
  title: Улица и высота
  primary_function: контакт до казни; Лиэрра не безгрешна; ярлык «уличная собака»
  secondary_function: Лиэрра догадывается о роли Марка

  state_lane_bitmask: 110  # CHAR+REL+WORLD+READER
  character_core_pressure:
    - character_id: CHAR_MARK
      desire: 1
      thesis: 1
      need: 1
      bitmask: 56
    - character_id: CHAR_LIERRA
      wound: 1
      mask: 1
      shadow: 1
      thesis: 1
      bitmask: 101
  output_requirements:
    symbol_mutation: травма у глаз Лиэрры (видимая)
    power_shift: толпа именует Марка
  forbidden: гоблины
```

---

## CH_004 — Кузница упадка

```yaml
CHAPTER_VECTOR:
  chapter_id: CH_004
  arc_ratio: 0.29
  milestone: first_cost_paid
  title: Кузница упадка
  primary_function: бюрократия казни; кузница страха
  secondary_function: ложная компетенция умирает у «настоящего железа»

  state_lane_bitmask: 127
  location: LOC_BLADE_QUARTER
  character_core_pressure:
    - character_id: CHAR_MARK
      lie: 1
      mask: 1
      thesis: 1
      bitmask: 38
  output_requirements:
    new_information: расправа = процесс
    cost_paid: унижение у кузнецов
  forbidden: гоблины
```

---

## CH_005 — Первая попытка мести

```yaml
CHAPTER_VECTOR:
  chapter_id: CH_005
  milestone: first_cost_paid + shadow_contact
  title: Первая попытка мести
  primary_function: первая инженерия исхода; страж под святотатство
  secondary_function: толпа переключает злость

  state_lane_bitmask: 127
  character_core_pressure:
    - character_id: CHAR_MARK
      shadow: 1
      desire: 1
      moral_limit: 1
      bitmask: 200  # DESIRE+SHADOW+MORAL approx — use 8+64+128=200
    - character_id: CHAR_LIERRA
      shadow: 1
      desire: 1
      bitmask: 72
  output_requirements:
    dirty_reward: страж страдает вместо Лиэрры
    cost_paid: Марк использует закон как оружие
    power_shift: толпа → новая жертва
  forbidden: гоблины
```

---

## CH_006 — Изгой среди богов

```yaml
CHAPTER_VECTOR:
  chapter_id: CH_006
  title: Изгой среди богов
  primary_function: Освобождённые; Лиэрра как политический ресурс
  secondary_function: первая точная обида (страж Мерзкий — контракт до главы)

  state_lane_bitmask: 127
  location: LOC_SEVEN_BRIDGES / подполье
  character_core_pressure:
    - character_id: CHAR_MARK
      wound: 1
      need: 1
      bitmask: 17
    - character_id: CHAR_LIERRA
      wound: 1
      lie: 1
      desire: 1
      bitmask: 11
  output_requirements:
    new_information: спасителей почти нет
    cost_paid: доверие к «союзникам» под вопросом
  forbidden: гоблины
  npc_contract_required: NPC_GUARD_MERZKY
```

---

## CH_007 — Невеста без неба

```yaml
CHAPTER_VECTOR:
  chapter_id: CH_007
  arc_ratio: 0.50
  milestone: midpoint_reversal
  title: Невеста без неба
  primary_function: Лиэрра защищает / исчезает; не жертва
  secondary_function: первая допустимая угроза гоблинов (если контракт сцены)

  state_lane_bitmask: 127
  character_core_pressure:
    - character_id: CHAR_LIERRA
      desire: 1
      shadow: 1
      thesis: 1
      need: 1
      bitmask: 88
    - character_id: CHAR_MARK
      thesis: 1
      need: 1
      bitmask: 48
  output_requirements:
    power_shift: центр конфликта — Лиэрра
    irreversible_event: демонстрация силы аномалии
  allowed_threat: THREAT_GOBLIN  # не раньше этой главы
```

---

## CH_008 — Цена исцеления

```yaml
CHAPTER_VECTOR:
  chapter_id: CH_008
  title: Цена исцеления
  primary_function: конкуренция за эфир; брат по крови
  secondary_function: Марк наблюдает, не силой

  state_lane_bitmask: 127
  location: LOC_WHITE_LEDGE
  character_core_pressure:
    - character_id: CHAR_LIERRA
      wound: 1
      fear: 1
      shadow: 1
      bitmask: 65
    - character_id: CHAR_MARK
      lie: 1
      need: 1
      bitmask: 18
  output_requirements:
    new_information: WR_ETHER_COMPETITION
    symbol_mutation: SYM_ETHER_BACKGROUND
  npc_contract_required: NPC_LIERRA_BLOOD_KIN
  forbidden: гоблины unless contract
```

---

## CH_009 — Новая блэкаут-техника

```yaml
CHAPTER_VECTOR:
  chapter_id: CH_009
  title: Новая блэкаут-техника
  primary_function: исцеление с ценой; проклятье обратно
  secondary_function: охота магов на «практику»

  state_lane_bitmask: 127
  character_core_pressure:
    - character_id: CHAR_MARK
      shadow: 1
      desire: 1
      moral_limit: 1
      bitmask: 200
    - character_id: CHAR_LIERRA
      wound: 1
      need: 1
      debt_via_rel: 1
  output_requirements:
    cost_paid: боль перенесена на Марка
    power_shift: маги ищут источник
```

---

## CH_010 — Власть носит цепи

```yaml
CHAPTER_VECTOR:
  chapter_id: CH_010
  arc_ratio: 0.71
  milestone: false_solution
  title: Власть носит цепи
  primary_function: двойная мораль лорда; сладкая месть unlock
  secondary_function: правда ≠ закон

  state_lane_bitmask: 127
  character_core_pressure:
    - character_id: CHAR_MARK
      shadow: 1
      forbidden_desire: 1
      moral_limit: 1
      bitmask: 192
    - character_id: CHAR_LIERRA
      lie: 1
      shadow: 1
      bitmask: 66
  output_requirements:
    dirty_reward: первая сладкая месть
    new_information: власть с улыбкой
  npc_contract_required: NPC_HIGH_LORD
  loop_flag: sweet_revenge_unlocked = true
```

---

## CH_011 — Фантом памяти

```yaml
CHAPTER_VECTOR:
  chapter_id: CH_011
  milestone: shadow_contact
  title: Фантом памяти
  primary_function: отголоски петли у Лиэрры; скрывает
  secondary_function: напряжение Марк–Лиэрра

  state_lane_bitmask: 110
  character_core_pressure:
    - character_id: CHAR_LIERRA
      shadow: 1
      lie: 1
      need: 1
      moral_limit: 1
      bitmask: 210
    - character_id: CHAR_MARK
      need: 1
      thesis: 1
      bitmask: 48
  output_requirements:
    new_information: lierra_loop_memory > 0
    deepen_loop: долг смертей
```

---

## CH_012 — Нулевая Невеста

```yaml
CHAPTER_VECTOR:
  chapter_id: CH_012
  arc_ratio: 0.86
  milestone: symbolic_death + final_cost
  title: Нулевая Невеста
  primary_function: побег хитростью; финальная смерть Марка
  secondary_function: кульминация тома 1

  state_lane_bitmask: 127
  character_core_pressure:
    - character_id: CHAR_MARK
      thesis: 1
      shadow: 1
      need: 1
      bitmask: 112
    - character_id: CHAR_LIERRA
      desire: 1
      shadow: 1
      moral_limit: 1
      bitmask: 200
  output_requirements:
    irreversible_event: жизнеобмен / смерть
    symbol_mutation: SYM_NULL_BRIDE пик
    symbolic_death_progress: 5
```

---

## CH_013 — То, что осталось

```yaml
CHAPTER_VECTOR:
  chapter_id: CH_013
  arc_ratio: 1.00
  milestone: integration_choice + new_state
  title: То, что осталось
  primary_function: не спасать — вывести из игры
  secondary_function: выбор свобода vs круг

  state_lane_bitmask: 127
  character_core_pressure:
    - character_id: CHAR_MARK
      need: 1
      synthesis: 1
      thesis: 1
      bitmask: 48
    - character_id: CHAR_LIERRA
      need: 1
      synthesis: 1
      thesis: 1
      bitmask: 48
  output_requirements:
    power_shift: герои угроза системе, не жертвы
    final_hook: том 2 — дорога / правда происхождения
    concept_active: CONCEPT_DONT_SAVE_HER
```

---

## Утверждение PHASE 8

```yaml
approval:
  status: APPROVED
  approved_date: 2026-06-15
  validated:
    - unique primary_function per unit
    - goblins only CH_007+
    - milestones mapped to GlobalArcGeometry
  next_phase: ChapterContracts.md + SceneContracts.md — DONE
```