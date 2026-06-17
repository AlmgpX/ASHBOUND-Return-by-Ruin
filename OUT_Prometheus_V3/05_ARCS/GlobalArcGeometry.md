# Global Arc Geometry — PHASE 7

> **Статус:** `APPROVED` — 2026-06-15  
> **Движок:** `OUT_Prometheus_Narrative_Engine_v3_1.md` §10  
> **Том 1:** 14 reading units (PR_000 + CH_001–CH_013)

```yaml
GlobalArcGeometry:
  protagonist_id: CHAR_MARK
  deuteragonist_id: CHAR_LIERRA

  start_state: |
    Наивный энтузиаст реконструкции; боится высоты; мечтает о «настоящем»;
    ноль статуса в Эрвене; бравада вместо тела.
  end_state: |
    Не спасатель — стратег и угроза системе; выводит Лиэрру из игры «Нулевой звезды»;
    действует со страхом; месть — инструмент, не смысл.

  core_transformation: |
    Слабый свидетель петли → инженер плохих исходов для закона;
    от «спасти куклу» к «не спасай её».

  core_wound: обычный / слабый / незначительный — сотрут, если не докажет обратное
  core_lie: стану полезным/смелым/жестоким — меня нельзя выбросить
  mask_to_break: реконструкция и «настоящая сталь»
  conscious_desire: спасти Лиэрру; доказать, что не пустое место
  subconscious_real_need: принять страх и слабость; не сделать месть единственным смыслом

  thesis_at_start: «Настоящая опасность докажет, что я не пустое место».
  antithesis_pressure: |
    Опасность калечит; система с печатью сильнее тела; спасение порождает долг;
    Лиэрра не объект спасения.
  synthesis_target: |
    Вывести из системы; связь без владения смыслом; инженерия исходов.

  final_truth: |
    Закон не злой — пустой и точный; правда ≠ приговор; петля — не избранность.
  final_relation_to_desire: |
    Внешняя цель мутирует: спасти → вывести; желание доказать ценность → стать условием плохого исхода.

  main_external_goal: спасти Лиэрру → вывести из системы Нулевой звезды
  external_goal_mutation: CH_013 — «не спасай её» как финальная стратегия

  main_antagonistic_force: THREAT_SYSTEM_LAW  # Орден, дома, стража, толпа
  main_shadow_force: сладкая месть; зависимость Лиэрры от его смертей

  main_symbol: SYM_CHOICE_POINT
  final_symbol_state: SYM_DONT_SAVE_HER / выход из игры, не кукла

  lierra_parallel_arc:
    thesis_at_start: «Никто не сделает меня своей»
    synthesis_target: «Связь по выбору, не по долгу»
    shadow: потратить смерти Марка правильно

  required_milestones:
    - id: mask_established
      unit: PR_000
    - id: irritant_or_call
      unit: CH_001
    - id: threshold_crossed
      unit: CH_001
    - id: first_cost_paid
      unit: CH_005
    - id: descent_deepens
      unit: CH_002
    - id: midpoint_reversal
      unit: CH_007
    - id: false_solution
      unit: CH_010
    - id: shadow_contact
      unit: CH_011
    - id: symbolic_death
      unit: CH_012
    - id: integration_choice
      unit: CH_013
    - id: final_cost
      unit: CH_012
    - id: new_state
      unit: CH_013

  arc_spine_notes: |
    1 Mask — PR_000 земная бравада
    2 Irritant — CH_001 казнь и первая смерть
    3 Threshold — перенос + помост (CH_001)
    4 First Cost — CH_005 первая месть с ядом
    5 Descent — CH_002–006 система не отпускает
    6 Midpoint — CH_007 Лиэрра не жертва; гоблины
    7 False Solution — CH_010 сладкая месть у лорда
    8 Shadow — CH_011 память петли у неё; долг смертей
    9 Symbolic Death — CH_012 спаситель умирает
    10 Integration — CH_013 вывести, не спасти
```

---

## Утверждение PHASE 7

```yaml
approval:
  status: APPROVED
  approved_date: 2026-06-15
```