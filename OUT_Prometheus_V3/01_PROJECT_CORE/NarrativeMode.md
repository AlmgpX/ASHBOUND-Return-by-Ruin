# Narrative Mode — PHASE 3

> **Статус:** `APPROVED` — 2026-06-15  
> **Движок:** `OUT_Prometheus_Narrative_Engine_v3_1.md`

```yaml
NarrativeMode: Hybrid

primary_pressure: Masculine Narrative Physics
  question: Can the protagonist become powerful without becoming a slave to power?
  active_axes:
    - competence (ложная → реальная наблюдательность)
    - survival
    - violence_cost
    - mastery (реконструкция vs настоящее железо)
    - sacrifice
    - revenge / control

secondary_pressure: Universal
  axes:
    - survival
    - truth (что реально работает в мире)
    - moral choice (месть с ценой)
    - agency (от зрителя к стратегу)

scene_rule: |
  Track active pressure per scene.
  Бой / месть / компетенция → Masculine.
  Петля / мораль / выбор исхода → Universal.
  Сцены с Лиэррой → добавить Feminine axes (agency, trust, power_imbalance),
  но Лиэрра — субъект, не объект спасения.

pov: first_person  # LOCKED автор
narrator: Марк
narrator_reliability: degrading — наивность → точная ненависть; не ненадёжный в фактах насилия

romance:
  present: yes
  lierra_dynamic: морозится; страх + использование; не «спаси меня»

failure_modes_to_watch:
  - becomes_tyrant (месть без цены)
  - becomes_weapon (только смерти без стратегии)
  - wins_externally_loses_self
  - cannot_stop_fighting (петля как ад без прогресса)

prose_voice:  # LOCKED 2026-06-15 — эталон PR_000, ревизия глав 2026-06-15
  pov: Марк, 1 лицо
  rhythm: |
    Короткие фразы и абзацы-удары. Одно предложение = одна мысль, где можно.
    Длинное — только если несёт деталь, не тему. Вывод после факта, не до.
  readability: |
    Легко читать вслух: меньше вложенности, меньше жирного, меньше «Не X. Y.» подряд.
    Каждый абзац двигает сцену или тело.
  warmth: через угар и точную деталь, не через сироп и не через пафос
  inner_monologue: курсив *…*; параллельные голоса (тело / стыд / правило) — без лекций
  dialogue: реплики с подтекстом; шутка часто прикрывает страх
  anchor_line: «Не будущую легенду. Просто Марка.» — тон пролога
  char_pol_rule: |
    LOCKED: Пол — только PR_000 (пролог) и клифхэнгер финала тома (перенос Пола).
    В CH_001–CH_012: ни имени, ни реплик, ни «узнал бы Пол», ни памяти о нём в прозе.
    Сравнение с реконструкцией / бункером / вышкой — без имён.
  other_world_rule: |
    Эрвен не знает «Землю» и происхождение Марка. NPC не называют мир героя.
    Марк дезориентирован: откуда, как, почему язык в голове — без ответа.
    Чужак / аномалия / дырка в учёте — да. «С Земли» — нет.
  forbidden_tone:
    - упоминание Поля вне пролога и финала
    - слово «Земля» в прозе Эрвена (CH_001+)
    - обесценивание хобби Марка со стороны рассказчика
    - героизация через красивые абзацы
```