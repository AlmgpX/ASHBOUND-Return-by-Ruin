# Chapter Contracts — PHASE 9

> **Статус:** `APPROVED` — 2026-06-15  
> **Движок:** `OUT_Prometheus_Narrative_Engine_v3_1.md` §12  
> **Векторы:** `ChapterMap.md` | **Сцены:** `SceneContracts.md`

---

## PR_000 — Проваленный эксперимент

```yaml
CHAPTER_CONTRACT_V3:
  chapter_id: PR_000
  title: Проваленный эксперимент
  chapter_index: 0
  total_chapters: 14
  arc_ratio: 0.00
  milestone: mask_established

  pov: CHAR_MARK
  narrative_mode: Universal + Masculine (страх / компетенция-маска)
  primary_function: маска энтузиаста; страх высоты; узор порога
  secondary_function: контраст земной нормальности (Пол)

  character_core_pressure:
    affected_key_characters: [CHAR_MARK, CHAR_POL]
    wound_pressure: Марк — «сотрут если слабый»
    mask_pressure: бравада реконструкции
    thesis_challenge: «настоящее» vs тело

  opening_state:
    plot: день переезда; друзья в промзоне
    character: маска цела; страх высоты активен
    world: Земля; бункер как табу
    symbolic: SYM_BUNKER_PATTERN ещё спит
    reader: знает страх; ждёт перенос

  required_external_event: прикосновение к узору → перенос
  required_internal_event: страх не побеждён — маска трещит
  required_choice: лезть в бункер ради «настоящего» vs отступить
  required_cost: высота; потеря земного контроля
  required_power_shift: Марк из субъекта дня → объект переноса
  required_symbol_mutation: SYM_BUNKER_PATTERN активирован

  reader_hook_in: обычный день, необычное здание
  reader_question_to_answer: почему Марк всё равно идёт внутрь?
  reader_question_to_open: что за узор; куда перенесёт
  micro_payoff: дошли до стены с узором
  final_hook: касание — провал/свет/тишина → cut

  scene_count: 4
  scene_functions: [переезд и бравада, вышка/высота, вход в бункер, узор]

  forbidden_drift: Серрат; магия Эрвена; гоблины; слово «фрактал» в прозе
  forbidden_repetition: повтор объяснения страха без действия
  continuity_requirements: зафиксировать Пол, промзону, непреодолённую высоту

  closing_state:
    plot: перенос инициирован
    character: маска сломана телом; wound_pressure ↑
    archetypal: threshold crossed (off-page end)
    symbolic: узор сожжён в память
    reader: open — где он; closed — земля кончилась
```

---

## CH_001 — Настоящие мечи

```yaml
CHAPTER_CONTRACT_V3:
  chapter_id: CH_001
  title: Настоящие мечи
  chapter_index: 1
  arc_ratio: 0.07
  milestone: irritant_or_call + threshold_crossed

  pov: CHAR_MARK
  narrative_mode: Masculine + Universal
  primary_function: порог Эрвена; первая смерть; иллюзия рушится
  secondary_function: казнь; Ренна; Лиэрра на помосте

  character_core_pressure:
    affected_key_characters: [CHAR_MARK, CHAR_LIERRA, CHAR_RENNA_KALD]
    wound_pressure: бессилие на помосте
    lie_pressure: «я знаю сталь» разбита
    desire_vs_need_pressure: спасти vs тело не тянет

  opening_state:
    plot: loop_iteration 1; пробуждение на площади
    character: дезориентация; инстинкт спасителя
    world: LOC_SERRAT_SQUARE_FORM; казнь по расписанию
    relationship: Марк–Лиэрра 0
    reader: после пролога — «это фэнтези?»

  required_external_event: попытка спасения; удар; смерть Марка
  required_internal_event: «настоящее» оказалось чужим и больным
  required_choice: броситься на помост vs наблюдать
  required_cost: первая смерть; сломанная маска компетенции
  required_power_shift: система (стража/Орден) доминирует
  required_symbol_mutation: SYM_CHOICE_POINT + SYM_NULL_BRIDE

  reader_question_to_answer: красота города — правда или приманка?
  reader_question_to_open: вернётся ли он; кто Нулевая невеста
  final_hook: смерть или мгновение до возврата

  scene_count: 4
  scene_functions: [пробуждение, иллюзия площади, казнь/Ренна, смерть]

  forbidden_drift: гоблины; Марк внезапно силён; эллипсис удара; Пол; слово «Земля»; NPC знают откуда чужак
  continuity_requirements: Ренна = процедура; Лиэрра на помосте; loop=1

  closing_state:
    plot: первая смерть; петля намечена
    character: mask_integrity ↓↓; thesis challenged
    relationship: фантазия спасителя зародилась
    world: закон показан телом
    reader: open петля; fear смерти
```

---

## CH_002 — Та же картина

```yaml
CHAPTER_CONTRACT_V3:
  chapter_id: CH_002
  title: Та же картина
  chapter_index: 2
  milestone: descent_deepens

  pov: CHAR_MARK
  primary_function: петля подтверждена; бегство не спасает
  secondary_function: стража маркирует чужака

  required_external_event: возврат перед казнью; погоня; снова площадь
  required_internal_event: паника; ложь «убежать = выжить» ломается
  required_choice: прятаться в толпе vs бежать из города
  required_cost: новые мучения; внимание системы
  required_power_shift: город «знает» чужака

  reader_question_to_open: правила петли; кто ещё видит повтор
  scene_count: 3

  forbidden_drift: гоблины; выезд за том без state change; Пол; «Земля»
  closing_state:
    plot: loop_iteration 2+; бегство провалено
    character: fear ↑; agency низкий
    world: стража активна
```

---

## CH_003 — Улица и высота

```yaml
CHAPTER_CONTRACT_V3:
  chapter_id: CH_003
  title: Улица и высота
  chapter_index: 3
  arc_ratio: 0.21
  milestone: threshold_crossed

  pov: CHAR_MARK
  primary_function: контакт до казни; Лиэрра не святая; ярлык собаки
  secondary_function: Лиэрра чует петлю

  character_core_pressure:
    affected_key_characters: [CHAR_MARK, CHAR_LIERRA]
    mask_pressure: её холод; его навязчивость
    thesis_challenge: спаситель vs объект

  required_external_event: разговор/подход до помоста; ярлык толпы
  required_internal_event: видит след у её глаз; она проверяет его
  required_symbol_mutation: травма у глаз (видимая)
  required_power_shift: толпа именует «уличная собака Нулевой невесты»

  scene_count: 3
  forbidden_drift: романтизация; гоблины; Пол; «Земля»
  closing_state:
    relationship: trust низкий; проверка начата
    world: rumor_level ↑
    character: need_visibility ↑ у обоих
```

---

## CH_004 — Кузница упадка

```yaml
CHAPTER_CONTRACT_V3:
  chapter_id: CH_004
  title: Кузница упадка
  chapter_index: 4
  arc_ratio: 0.29
  milestone: first_cost_paid

  pov: CHAR_MARK
  primary_function: казнь как бюрократия и производство
  secondary_function: «настоящее железо» унижает реконструкцию

  required_external_event: дом кузнеца-катариста; орудия казни
  required_internal_event: понимание процесса расправы
  required_cost: унижение; тошнота от системности
  required_new_information: каждый элемент казни — регламент

  scene_count: 4
  location: LOC_BLADE_QUARTER
  forbidden_drift: гоблины; бой героя; Пол; «Земля»; NPC называют мир героя
  closing_state:
    world: HOUSE_KELRAY контуры видны
    character: lie «сталь» cracked
    plot: знает механику казни
```

---

## CH_005 — Первая попытка мести

```yaml
CHAPTER_CONTRACT_V3:
  chapter_id: CH_005
  title: Первая попытка мести
  chapter_index: 5
  milestone: first_cost_paid + shadow_contact

  pov: CHAR_MARK
  primary_function: первая инженерия исхода; закон как оружие
  secondary_function: толпа переключает жертву

  required_external_event: страж под святотатство; травля стража
  required_internal_event: первая сладость чужого закона (яд)
  required_dirty_reward: страж страдает вместо Лиэрры
  required_cost: Марк использует систему; shadow ↑
  required_power_shift: толпа → новая мишень

  scene_count: 4
  forbidden_drift: прямое насилие Марка; гоблины
  closing_state:
    character: outcome_engineering_level ↑; shadow_pressure ↑
    archetypal: shadow_contact начат
    plot: паттерн стража в mark_loop_memory
```

---

## CH_006 — Изгой среди богов

```yaml
CHAPTER_CONTRACT_V3:
  chapter_id: CH_006
  title: Изгой среди богов
  chapter_index: 6

  pov: CHAR_MARK
  primary_function: Освобождённые; Лиэрра — ресурс, не спасение
  secondary_function: обида на стража (Мерзкий)

  required_external_event: встреча с подпольем FAC_SECT_LIBERATED
  required_internal_event: «добровольцев спасти мало»
  required_cost: доверие к союзникам подорвано
  npc_required: NPC_GUARD_MERZKY

  scene_count: 4
  location: LOC_SEVEN_BRIDGES
  forbidden_drift: гоблины
  closing_state:
    world: фракция подполья введена
    relationship: Лиэрра как политический объект
    character: need — не быть пешкой — pressured
```

---

## CH_007 — Невеста без неба

```yaml
CHAPTER_CONTRACT_V3:
  chapter_id: CH_007
  title: Невеста без неба
  chapter_index: 7
  arc_ratio: 0.50
  milestone: midpoint_reversal

  pov: CHAR_MARK
  primary_function: Лиэрра защищает / исчезает — не жертва
  secondary_function: THREAT_GOBLIN допустим по сцене

  required_external_event: попытка похищения; бой; исчезновение Лиэрры
  required_internal_event: midpoint — она центр, не кукла
  required_power_shift: конфликт вращается вокруг неё

  scene_count: 4
  allowed_threat: THREAT_GOBLIN
  forbidden_drift: гоблины без state change; Лиэрра только плачет
  closing_state:
    character: lierra agency ↑↑
    plot: аномалия продемонстрирована
    reader: midpoint — переоценка Лиэрры
```

---

## CH_008 — Цена исцеления

```yaml
CHAPTER_CONTRACT_V3:
  chapter_id: CH_008
  title: Цена исцеления
  chapter_index: 8

  pov: CHAR_MARK
  primary_function: WR_ETHER_COMPETITION в действии
  secondary_function: брат по крови «вываривает» ауру

  required_external_event: ритуал в LOC_WHITE_LEDGE
  required_new_information: лечение vs проклятье забирают фон
  required_symbol_mutation: SYM_ETHER_BACKGROUND
  npc_required: NPC_LIERRA_BLOOD_KIN

  scene_count: 4
  closing_state:
    world: magic_environment — конкуренция
    character: наблюдательность ↑ (не сила)
    plot: mark_loop_memory + WR_ETHER
```

---

## CH_009 — Новая блэкаут-техника

```yaml
CHAPTER_CONTRACT_V3:
  chapter_id: CH_009
  title: Новая блэкаут-техника
  chapter_index: 9

  pov: CHAR_MARK
  primary_function: исцеление Лиэрры с ценой; проклятье обратно
  secondary_function: охота магов на практику

  required_external_event: смертельная рана Лиэрры; исцеление; петля
  required_cost: боль перенесена на Марка
  required_power_shift: маги ищут источник

  scene_count: 4
  closing_state:
    relationship: debt ↑↑
    character: shadow + moral_limit approached
    plot: техника запомнена в persist
```

---

## CH_010 — Власть носит цепи

```yaml
CHAPTER_CONTRACT_V3:
  chapter_id: CH_010
  title: Власть носит цепи
  chapter_index: 10
  arc_ratio: 0.71
  milestone: false_solution

  pov: CHAR_MARK
  primary_function: двойная мораль власти; sweet_revenge unlock
  secondary_function: правда ≠ закон

  required_external_event: судебный приём у высшего лорда
  required_dirty_reward: первая сладкая месть через систему
  required_internal_event: понимание улыбок власти
  npc_required: NPC_HIGH_LORD

  scene_count: 5
  loop_flag: sweet_revenge_unlocked = true
  closing_state:
    character: forbidden_desire_pressure ↑
    archetypal: false_solution active
    world: дворянский дом (дом TBD)
```

---

## CH_011 — Фантом памяти

```yaml
CHAPTER_CONTRACT_V3:
  chapter_id: CH_011
  title: Фантом памяти
  chapter_index: 11
  milestone: shadow_contact

  pov: CHAR_MARK
  primary_function: отголоски петли у Лиэрры; она скрывает
  secondary_function: напряжение долга смертей

  required_external_event: видения/сон Лиэрры; Марк замечает знание без слов
  required_internal_event: недоверие ↑; shadow «потратить его смерть»
  required_new_information: lierra_loop_memory > 0

  scene_count: 3
  closing_state:
    relationship: betrayal_risk ↑; intimacy без trust
    character: Lierra shadow 210 pressured
```

---

## CH_012 — Нулевая Невеста

```yaml
CHAPTER_CONTRACT_V3:
  chapter_id: CH_012
  title: Нулевая Невеста
  chapter_index: 12
  arc_ratio: 0.86
  milestone: symbolic_death + final_cost

  pov: CHAR_MARK
  primary_function: побег хитростью; финальная смерть
  secondary_function: кульминация тома 1

  required_external_event: организованный побег; погоня; жизнеобмен/смерть
  required_cost: жертва жизнеобменом
  required_symbol_mutation: SYM_NULL_BRIDE пик
  required_power_shift: старый спаситель мёртв

  scene_count: 5
  closing_state:
    archetypal: symbolic_death_progress = 5
    character: thesis broken — спаситель
    plot: триггер финальной петли
```

---

## CH_013 — То, что осталось

```yaml
CHAPTER_CONTRACT_V3:
  chapter_id: CH_013
  title: То, что осталось
  chapter_index: 13
  arc_ratio: 1.00
  milestone: integration_choice + new_state

  pov: CHAR_MARK
  primary_function: не спасать — вывести из игры
  secondary_function: выбор свобода vs круг

  required_external_event: последняя петля; стратегия вывода, не куклы
  required_internal_event: synthesis — страх + действие без мести-единственной
  required_choice: жертвовать свободой Лиэрры vs бежать по кругу vs выход из игры
  required_power_shift: герои — угроза системе

  reader_question_to_answer: что значит «не спасай её»?
  final_hook: том 2 — дорога; правда происхождения

  scene_count: 4
  closing_state:
    plot: vol1 arc closed; external goal mutated
    character: new_state vol1 Mark + Lierra synthesis начат
    symbolic: CONCEPT_DONT_SAVE_HER активен
    reader: emotional landing + hook том 2
```

---

## NPC Contracts (до появления в прозе)

```yaml
NPC_GUARD_MERZKY:
  chapter: CH_006
  role: объект первой точной обиды
  core_pressure: виновник паттерна CH_005
  narrative_function: имя для мести читателя

NPC_LIERRA_BLOOD_KIN:
  chapter: CH_008
  role: маг-проклинатель; «выварить» ауру
  core_pressure: семья как контроль
  narrative_function: показать цену её тела для чужих

NPC_HIGH_LORD:
  chapter: CH_010
  role: двойная мораль власти
  house: TBD  # один из семи домов
  narrative_function: false_solution через аристократию
```

---

## Утверждение PHASE 9

```yaml
approval:
  status: APPROVED
  approved_date: 2026-06-15
  units: 14
  next: SceneContracts.md (PHASE 10)
```