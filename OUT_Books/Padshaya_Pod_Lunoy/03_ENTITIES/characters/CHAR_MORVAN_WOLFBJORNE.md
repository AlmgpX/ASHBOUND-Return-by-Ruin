# CHAR_MORVAN_WOLFBJORNE — Morvan Wolfbjorne

> **Tier:** KEY  
> **Статус:** APPROVED  
> **Имя в тексте:** Морван Вольфбьорн (без русификации фамилии)  
> **Не герой** — соблазнитель героини

```yaml
CHARACTER_CORE_CONTRACT:
  character_id: CHAR_MORVAN_WOLFBJORNE
  name: Morvan Wolfbjorne
  name_in_prose_ru: Морван Вольфбьорн
  tier: KEY
  role: seducer_love_interest
  narrative_function: соблазнитель; не спаситель; будит желание; оборотень; цена крови

  core_wound: церковь отвергла после того, как он увидел слишком много и не сгорел красиво
  core_lie: «Спасение — это слабость; только желание честно»
  mask: холод; тёмная улыбка; паладин без креста

  conscious_desire: чтобы Элирия сама назвала, кем является
  subconscious_real_need: не быть одиноким в пепле; пара без иллюзии спасения

  thesis_at_start: контроль через правду без милости
  antithesis_pressure: церковь; луна; её стыд
  synthesis_target: рядом с её короной — не над ней
  deliberate_no_synthesis: false

  fear: снова стать инструментом церкви
  shame_trigger: клеймо Оступника
  forbidden_desire: её слабость и сила одновременно
  shadow: зверь без спроса
  moral_limit: не убивает её маску одним ударом — снимает слоями
  false_strategy: давить желанием вместо признания своей цены

  competence: бой; охота; чтение стыда
  incompetence: мягкое спасение; оправдание перед церковью
  movement_role: преследователь церкви; катализатор ночи без возврата
  adventure_use: оборотень; засада; убежище

  what_they_want_from_eliria: признание вслух
  what_they_misread_about_eliria: что идеальность = пустота
  what_eliria_misreads_about_them: что пришёл спасти

  world_force_relation: WF_CHURCH_ASH охотится; WF_HUNGER_TRUTH уважает
  relationship_function: слова → тело → CH_007 зверь (узел, семя-магия) → человек отвергнут телом героини
  locked_scene_CH_007: giant_werewolf; knotting; seed_magic; human_form_inadequate_after
  world_function: Оступник; шлейф крови
  symbolic_function: пепел / клык / луна полная

  start_state: ash_outcast_hunter
  first_turn_state: sees_eliria_mask
  midpoint_state: moon_revealed
  crisis_state: church_closes_net
  end_state: stands_beside_demon_crown

  scene_activation_rules:
    activate_when:
      - desire_pressure_on_eliria
      - church_hunt
      - full_moon_risk
```