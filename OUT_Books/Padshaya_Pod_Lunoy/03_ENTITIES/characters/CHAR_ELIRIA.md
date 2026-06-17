# CHAR_ELIRIA — Элирия Лунная

> **Tier:** KEY · **Герой** · **POV:** true (единственный)  
> **Статус:** APPROVED

```yaml
CHARACTER_CORE_CONTRACT:
  character_id: CHAR_ELIRIA
  name: Элирия Лунная
  tier: KEY
  role: protagonist_hero
  narrative_function: маска идеала → признание желания → владычица демонов

  core_wound: любили как функцию — идеал, молчание, украшение; не как женщину
  core_lie: «Если я идеальна и тиха — мне здесь место»
  mask: улыбка, правильные ответы, сжатое тело; «человек, которому здесь место»

  conscious_desire: выжить; не быть стёртой; найти место
  subconscious_real_need: хотеть без смерти от стыда; собственная власть и голос тела

  thesis_at_start: принадлежность через идеальность
  antithesis_pressure: голод, изгнание, Morvan Wolfbjorne, демоническая линия
  synthesis_target: «Я хочу — и это моя власть»
  deliberate_no_synthesis: false

  fear: сказать не то; быть осуждённой; **быть увиденной настоящей**
  shame_trigger: щёки горят; дрожь; чужие вопросы
  forbidden_desire: Morvan Wolfbjorne; грязь; злость; голод тела
  shadow: владычица демонов — то, что двор назвал бы «сучкой»
  moral_limit: стёрта на старте; лимит тает с голодом
  false_strategy: казаться своей в чужих правилах

  competence: выдержка маски; чтение опасности двора
  incompetence: назвать желание вслух; принять помощь без долга
  movement_role: изгнанница → пара → корона
  adventure_use: бегство, ритуал, коронация демона

  what_they_want_from_morvan: чтобы увидел — и не обелил
  what_they_misread_about_morvan: что он придёт спасти
  what_morvan_misreads_about_them: что маска = пустота

  world_force_relation: WF_PERFECT_SILENCE давит; WF_HUNGER_TRUTH ломает
  relationship_function: страх ↔ желание с Morvan; он соблазнитель, не герой
  world_function: носитель спящей демонической линии
  symbolic_function: луна падшая / корона чёрная

  start_state: silver_cage_princess
  first_turn_state: erased_failed_exile
  midpoint_state: touch_shame_awake
  crisis_state: night_no_return  # CH_007 LOCKED: первый раз, узел, магия семени; «не тот размерчик»
  end_state: demon_lady_crowned_vol1  # CH_008

  scene_activation_rules:
    activate_when:
      - mask_pressure
      - shame_or_desire
      - hunger_body
```