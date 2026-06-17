# Scene Contracts — PHASE 10

> **Статус:** `APPROVED` — 2026-06-15  
> **Движок:** `OUT_Prometheus_Narrative_Engine_v3_1.md` §13  
> **Главы:** `ChapterContracts.md`

---

## PR_000

### SC_PR_000_01 — Переезд

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_PR_000_01
  chapter_id: PR_000
  scene_index: 1
  location: квартира → промзона (вечер, проникновение у забора), день переезда
  pov: CHAR_MARK
  participants: [CHAR_MARK, CHAR_POL]
  scene_type: transition
  narrative_mode: Universal

  participant_core_pressure:
    active_character: CHAR_MARK
    wound_pressured: true
    mask_performed_or_cracked: performed
    conscious_desire_pursued: не выглядеть слабым
    active_character_2: CHAR_POL
    mask_performed: угар
    lie_protected: «если ржём — не ранит»
    need_pressured: сказать важное без шутки (не выполнено)

  surface_goal: провести последний вечер как обычный
  obstacle: переезд; зов «эксперимента»; тревога под шуткой
  pressure_source: страх высоты (ещё не назван); страх потерять Поля
  conflict_type: internal + relational

  turn: Пол зовёт в заброшку — Марк соглашается
  cost: согласие из бравады; вечер не сказан вслух
  object_anchor: коробки, «Газель», термос, переписка
  prose_note: тепло через угар, не сироп; см. NarrativeMode prose_voice
  exit_hook: видна вышка/бункер
```

### SC_PR_000_02 — Вышка

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_PR_000_02
  chapter_id: PR_000
  scene_index: 2
  location: вышка / лестница
  participants: [CHAR_MARK, CHAR_POL]
  scene_type: action
  participant_core_pressure:
    active_character: CHAR_MARK
    wound_pressured: true
    thesis_challenged: true

  surface_goal: подняться «как нормальные»
  obstacle: ЖБ-столб; вертикальная обледенелая лестница прутьев; старт 2,5 м; руки затекают
  turn: Марк на земле — прыжок, 2 прута, спрыгнул; Пол — на вершину
  cost: публичный стыд; Пол взял высоту за обоих
  bodily_anchor: затекшие руки, лёд на прутьях, звон в ушах
  exit_hook: Пол с вершины → бункер; Марк с земли идёт следом
```

### SC_PR_000_03 — Бункер

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_PR_000_03
  chapter_id: PR_000
  scene_index: 3
  location: бункер, темнота
  participants: [CHAR_MARK, CHAR_POL]
  scene_type: discovery
  participant_core_pressure:
    active_character: CHAR_MARK
    desire_pursued: true
    fear_pressure: 4

  surface_goal: «настоящий» опыт
  obstacle: темнота; чужой запах бетона
  concrete_world_detail: капающая вода, ржавая дверь
  turn: на стене — узор (не «фрактал»)
  symbol_action: SYM_BUNKER_PATTERN появляется
  exit_hook: узор «смотрит» или притягивает
```

### SC_PR_000_04 — Касание

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_PR_000_04
  chapter_id: PR_000
  scene_index: 4
  location: бункер, у стены
  participants: [CHAR_MARK]
  scene_type: ritual
  scene_type_note: порог переноса

  surface_goal: понять символ
  hidden_goal: доказать, что он не трус
  turn: прикосновение — реальность ломается
  cost: потеря Земли
  line_of_no_return: касание
  power_shift: Марк → объект переноса
  exit_hook: cut / падение / смена света
```

---

## CH_001

### SC_CH_001_01 — Пробуждение

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_001_01
  chapter_id: CH_001
  scene_index: 1
  location: LOC_SERRAT_SQUARE_FORM (край толпы)
  loop_iteration: 1
  scene_type: aftermath
  surface_goal: понять где он
  turn: красота города — камень, стража, колокол
  sensory_anchor: запах травы и железа
  exit_hook: помост в центре
```

### SC_CH_001_02 — Иллюзия

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_001_02
  chapter_id: CH_001
  scene_index: 2
  participants: [CHAR_MARK, толпа]
  scene_type: discovery
  surface_goal: насладиться «настоящим фэнтези»
  obstacle: диссонанс — слишком чисто
  turn: видит Лиэрру на помосте
  symbol_action: SYM_NULL_BRIDE
  new_information: казнь как событие города
```

### SC_CH_001_03 — Казнь

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_001_03
  chapter_id: CH_001
  scene_index: 3
  participants: [CHAR_MARK, CHAR_LIERRA, CHAR_RENNA_KALD, FAC_SERRAT_GUARD]
  scene_type: ritual
  participant_core_pressure:
    active_character: CHAR_RENNA_KALD
    lie_protected: процедура = добро
    active_character_2: CHAR_LIERRA
    mask_performed: обречённость

  surface_goal: Ренна завершает обряд
  obstacle: Марк движется к помосту
  concrete_world_detail: печати, приговор, колокольный фон
  legal_bureaucratic_anchor: формулировки приговора
  turn: Марк бросается спасать
  power_shift: система отвечает силой
```

### SC_CH_001_04 — Смерть

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_001_04
  chapter_id: CH_001
  scene_index: 4
  scene_type: action
  participant_core_pressure:
    active_character: CHAR_MARK
    lie_broken: true
    wound_pressured: true

  surface_goal: спасти её
  obstacle: удар; неготовое тело
  turn: смерть — больно, глупо, быстро
  cost: жизнь; маска реконструкции
  symbol_action: SYM_CHOICE_POINT — день не затвердел (намёк)
  exit_hook: возврат или blackout
```

---

## CH_002

### SC_CH_002_01 — Возврат

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_002_01
  chapter_id: CH_002
  scene_index: 1
  loop_iteration: 2
  scene_type: discovery
  new_information: тот же момент; память сохранилась
  turn: паника — это снова
  cost: sanity hit
```

### SC_CH_002_02 — Бегство

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_002_02
  chapter_id: CH_002
  scene_index: 2
  scene_type: action
  surface_goal: уйти с площади
  obstacle: толпа; стража
  turn: его замечают как чужака
  cost: статус «незнакомец» закреплён
```

### SC_CH_002_03 — Снова площадь

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_002_03
  chapter_id: CH_002
  scene_index: 3
  scene_type: aftermath
  turn: петля возвращает к казни
  cost: новые мучения
  exit_hook: бегство не работает
```

---

## CH_003

### SC_CH_003_01 — До помоста

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_003_01
  chapter_id: CH_003
  scene_index: 1
  participants: [CHAR_MARK, CHAR_LIERRA]
  scene_type: dialogue
  participant_core_pressure:
    active_character: CHAR_LIERRA
    mask_performed: true
    lie_protected: близость = цепь

  surface_goal: поздороваться / предупредить
  obstacle: её холод; охрана
  turn: она отвечает так, будто уже знает цену
```

### SC_CH_003_02 — След у глаз

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_003_02
  chapter_id: CH_003
  scene_index: 2
  scene_type: discovery
  new_information: рисунок/след травмы — она не святая
  turn: Марк видит её морщиться/прятать
  symbol_action: тело как текст
```

### SC_CH_003_03 — Уличная собака

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_003_03
  chapter_id: CH_003
  scene_index: 3
  scene_type: social_attack
  surface_goal: слиться с толпой
  turn: ярлык «уличная собака Нулевой невесты»
  power_shift: статус унижен
  exit_hook: Лиэрра смотрит иначе — догадка
```

---

## CH_004

### SC_CH_004_01 — Путь в квартал

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_004_01
  chapter_id: CH_004
  scene_index: 1
  location: путь → LOC_BLADE_QUARTER
  scene_type: transition
  surface_goal: узнать как готовят казнь
```

### SC_CH_004_02 — Цех

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_004_02
  chapter_id: CH_004
  scene_index: 2
  scene_type: discovery
  concrete_world_detail: горн, контуры HOUSE_KELRAY, заготовки
  turn: орудия не героичны — функциональны
```

### SC_CH_004_03 — Процесс

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_004_03
  chapter_id: CH_004
  scene_index: 3
  scene_type: ritual
  new_information: вещи «разоблачённой»; бумаги; подписи
  bodily_anchor: жар и тошнота у Марка
```

### SC_CH_004_04 — Настоящая сталь

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_004_04
  chapter_id: CH_004
  scene_index: 4
  scene_type: social_attack
  participant_core_pressure:
    active_character: CHAR_MARK
    lie_broken: реконструкция vs кузнец

  turn: держит/не держит молот — унижение
  cost: mask_integrity ↓
  exit_hook: знает механику казни
```

---

## CH_005

### SC_CH_005_01 — Паттерн стража

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_005_01
  chapter_id: CH_005
  scene_index: 1
  scene_type: discovery
  new_information: страж отвлекает Лиэрру каждую петлю
  surface_goal: запомнить деталь
```

### SC_CH_005_02 — Подстава

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_005_02
  chapter_id: CH_005
  scene_index: 2
  scene_type: choice
  participant_core_pressure:
    active_character: CHAR_MARK
    shadow_pressured: true

  surface_goal: сорвать его с позиции
  obstacle: нужен закон, не кулак
  turn: святотатство кровью Нулевой звезды
```

### SC_CH_005_03 — Толпа переключается

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_005_03
  chapter_id: CH_005
  scene_index: 3
  scene_type: social_attack
  dirty_reward: страж вместо Лиэрры
  power_shift: толпа жрёт нового виновного
```

### SC_CH_005_04 — Яд сладости

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_005_04
  chapter_id: CH_005
  scene_index: 4
  scene_type: aftermath
  participant_core_pressure:
    active_character: CHAR_MARK
    moral_limit: approached

  turn: ему нравится результат
  cost: shadow ↑; первая инженерия исхода
```

---

## CH_006

### SC_CH_006_01 — Слухи

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_006_01
  chapter_id: CH_006
  scene_index: 1
  location: LOC_SEVEN_BRIDGES
  scene_type: discovery
  new_information: Освобождённые; голос «невиновна»
```

### SC_CH_006_02 — Подполье

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_006_02
  chapter_id: CH_006
  scene_index: 2
  participants: [CHAR_MARK, FAC_SECT_LIBERATED]
  scene_type: dialogue
  turn: хотят использовать Лиэрру
  obstacle: не спасение — политика
```

### SC_CH_006_03 — Мерзкий

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_006_03
  chapter_id: CH_006
  scene_index: 3
  participants: [CHAR_MARK, NPC_GUARD_MERZKY]
  scene_type: confrontation
  new_information: лицо обиды
  cost: ярость точная
```

### SC_CH_006_04 — Мало добровольцев

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_006_04
  chapter_id: CH_006
  scene_index: 4
  scene_type: aftermath
  turn: Марк понимает — спасателей нет
  exit_hook: только свои петли
```

---

## CH_007

### SC_CH_007_01 — Похищение

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_007_01
  chapter_id: CH_007
  scene_index: 1
  scene_type: action
  required_external_event: попытка увести их
```

### SC_CH_007_02 — Первый бой

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_007_02
  chapter_id: CH_007
  scene_index: 2
  participants: [CHAR_MARK, CHAR_LIERRA, FAC_SERRAT_GUARD]
  scene_type: action
  turn: Лиэрра защищает Марка
  participant_core_pressure:
    active_character: CHAR_LIERRA
    desire_pursued: true
```

### SC_CH_007_03 — Исчезновение

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_007_03
  chapter_id: CH_007
  scene_index: 3
  scene_type: action
  turn: она почти в бой — исчезает в критический момент
  new_information: аномалия; не только магия
```

### SC_CH_007_04 — Край (опционально гоблины)

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_007_04
  chapter_id: CH_007
  scene_index: 4
  scene_type: action
  allowed_threat: THREAT_GOBLIN
  note: включать только если сцена меняет world+plot lane
  turn: край закона / дорога; угроза вне стражи
  exit_hook: midpoint — она не жертва
```

---

## CH_008

### SC_CH_008_01 — Белый уступ

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_008_01
  chapter_id: CH_008
  scene_index: 1
  location: LOC_WHITE_LEDGE
  scene_type: transition
  concrete_world_detail: колокола, фон, лечебницы
```

### SC_CH_008_02 — Брат по крови

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_008_02
  chapter_id: CH_008
  scene_index: 2
  participants: [CHAR_LIERRA, NPC_LIERRA_BLOOD_KIN]
  scene_type: dialogue
  participant_core_pressure:
    active_character: CHAR_LIERRA
    wound_pressured: true
    shadow_pressured: true
```

### SC_CH_008_03 — Вываривание

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_008_03
  chapter_id: CH_008
  scene_index: 3
  scene_type: ritual
  turn: проклятье тянет фон
  symbol_action: SYM_ETHER_BACKGROUND
  new_information: WR_ETHER_COMPETITION
```

### SC_CH_008_04 — Утечка

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_008_04
  chapter_id: CH_008
  scene_index: 4
  scene_type: discovery
  turn: Марк видит — мана уходит в никуда
  cost: бессилие помочь силой
```

---

## CH_009

### SC_CH_009_01 — Рана

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_009_01
  chapter_id: CH_009
  scene_index: 1
  scene_type: action
  turn: Лиэрра смертельно ранена
```

### SC_CH_009_02 — Исцеление

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_009_02
  chapter_id: CH_009
  scene_index: 2
  scene_type: ritual
  participant_core_pressure:
    active_character: CHAR_MARK
    shadow_pressured: true
  turn: нагрузить проклятье обратно
```

### SC_CH_009_03 — Цена

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_009_03
  chapter_id: CH_009
  scene_index: 3
  scene_type: aftermath
  bodily_anchor: боль Марка; её дыхание
  cost: часть боли перенесена
```

### SC_CH_009_04 — Охота

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_009_04
  chapter_id: CH_009
  scene_index: 4
  scene_type: transition
  turn: петля; маги ищут источник
  relationship: debt ↑
```

---

## CH_010

### SC_CH_010_01 — Приглашение

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_010_01
  chapter_id: CH_010
  scene_index: 1
  scene_type: transition
  status_pressure: 4
  new_information: путь к лорду
```

### SC_CH_010_02 — Приём

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_010_02
  chapter_id: CH_010
  scene_index: 2
  participants: [CHAR_MARK, NPC_HIGH_LORD, двор]
  scene_type: social_attack
  concrete_world_detail: еда, печати, слуги
```

### SC_CH_010_03 — Улыбки

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_010_03
  chapter_id: CH_010
  scene_index: 3
  scene_type: discovery
  turn: добродетели как инструмент
  new_information: правда ≠ закон
```

### SC_CH_010_04 — Сладкая месть

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_010_04
  chapter_id: CH_010
  scene_index: 4
  scene_type: choice
  participant_core_pressure:
    active_character: CHAR_MARK
    forbidden_desire: activated
  dirty_reward: точная расплата через систему
  loop_flag: sweet_revenge_unlocked
```

### SC_CH_010_05 — Цепи

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_010_05
  chapter_id: CH_010
  scene_index: 5
  scene_type: aftermath
  turn: власть носит цепи — он видит на себе
  archetypal: false_solution
```

---

## CH_011

### SC_CH_011_01 — Сон

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_011_01
  chapter_id: CH_011
  scene_index: 1
  participants: [CHAR_LIERRA]
  scene_type: discovery
  pov_note: Марк наблюдает последствия
  new_information: фрагменты петель у неё
```

### SC_CH_011_02 — Блеск глаз

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_011_02
  chapter_id: CH_011
  scene_index: 2
  participants: [CHAR_MARK, CHAR_LIERRA]
  scene_type: dialogue
  turn: она знает то, что он не говорил
  participant_core_pressure:
    active_character: CHAR_LIERRA
    lie_protected: скрывает видения
```

### SC_CH_011_03 — Долг

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_011_03
  chapter_id: CH_011
  scene_index: 3
  scene_type: intimacy
  scene_type_note: не романтика — напряжение
  turn: mutual_debt невыплатим
  exit_hook: скрытая война доверия
```

---

## CH_012

### SC_CH_012_01 — План

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_012_01
  chapter_id: CH_012
  scene_index: 1
  scene_type: choice
  surface_goal: побег из заключения хитростью, не силой
```

### SC_CH_012_02 — Исполнение

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_012_02
  chapter_id: CH_012
  scene_index: 2
  scene_type: action
  turn: план срабатывает частично
```

### SC_CH_012_03 — Погоня

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_012_03
  chapter_id: CH_012
  scene_index: 3
  scene_type: action
  location: улицы Серрата
  power_shift: система сжимает
```

### SC_CH_012_04 — Капкан

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_012_04
  chapter_id: CH_012
  scene_index: 4
  scene_type: action
  turn: второй капкан; Лиэрра почти поймана
```

### SC_CH_012_05 — Жизнеобмен

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_012_05
  chapter_id: CH_012
  scene_index: 5
  scene_type: action
  turn: Марк вклинивается; жертвует собой
  cost: финальная смерть тома
  symbol_action: SYM_NULL_BRIDE пик
  archetypal: symbolic_death
```

---

## CH_013

### SC_CH_013_01 — Как обычно

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_013_01
  chapter_id: CH_013
  scene_index: 1
  scene_type: aftermath
  loop_iteration: final
  turn: возврат — но Марк другой
```

### SC_CH_013_02 — Она уже знает

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_013_02
  chapter_id: CH_013
  scene_index: 2
  participants: [CHAR_MARK, CHAR_LIERRA]
  scene_type: dialogue
  new_information: кусок памяти у неё
  participant_core_pressure:
    both: synthesis approached
```

### SC_CH_013_03 — Выбор

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_013_03
  chapter_id: CH_013
  scene_index: 3
  scene_type: choice
  required_choice: свобода vs круг vs выход из игры
  line_of_no_return: стратегия «не спасай»
  concept: CONCEPT_DONT_SAVE_HER
```

### SC_CH_013_04 — То, что осталось

```yaml
SCENE_CONTRACT_V3:
  scene_id: SC_CH_013_04
  chapter_id: CH_013
  scene_index: 4
  scene_type: aftermath
  turn: new_state vol1; угроза системе
  final_hook: дорога; правда происхождения; том 2
  reader: emotional landing
```

---

## Сводка

```yaml
SceneContractSummary:
  total_scenes: 55
  by_chapter:
    PR_000: 4
    CH_001: 4
    CH_002: 3
    CH_003: 3
    CH_004: 4
    CH_005: 4
    CH_006: 4
    CH_007: 4
    CH_008: 4
    CH_009: 4
    CH_010: 5
    CH_011: 3
    CH_012: 5
    CH_013: 4
```

---

## Утверждение PHASE 10

```yaml
approval:
  status: APPROVED
  approved_date: 2026-06-15
  prose_gate: OPEN for PR_000 after author confirm
  reminder: после каждой главы — StateLedger_DEBUG + ContinuityLog
```