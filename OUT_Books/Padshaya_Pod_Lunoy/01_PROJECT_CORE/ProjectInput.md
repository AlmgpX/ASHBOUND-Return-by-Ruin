# ProjectInput — PHASE 0

> **Статус:** `APPROVED` — 2026-06-16  
> **Источник:** `STORY_BIBLE.md` (author LOCKED)  
> **Engine:** `OUT_Prometheus_Narrative_Engine_v3_3`

```yaml
ProjectInput:
  project_id: OUT_PADSHAYA_POD_LUNOY
  engine: OUT_Prometheus_Narrative_Engine_v3_3
  engine_path: ../../OUT_Prometheus_V3_3/00_ENGINE/INDEX.md
  books_root: OUT_Books/Padshaya_Pod_Lunoy

  title: Падшая под Луной
  title_en: Fallen Under the Moon
  title_ru: Падшая под Луной
  title_ru_tagline: Она училась быть идеальной. Он научил её хотеть.
  title_lock: LOCKED
  source_bible: STORY_BIBLE.md

  FormatAdapter:
    primary_format: novel
    unit_name: chapter
    known_unit_count: true
    total_units: 8
    chapter_length_policy: flexible    # по драматургии сцены
    pov_policy: single                 # только Элирия; женский авторский голос

  genre: [dark_fantasy, dark_romance, court_intrigue, monster_romance, religious_horror]
  subgenre: [corruption_arc, werewolf, demon_ascension]
  target_audience: adult_18+
  content_rating: R+
  market_position: |
    Dark romance с героиней: маска идеальной эльфийки → изгнание →
    запретная страсть с падшим паладином-оборотнем → путь к владычеству демонов.
    Не исекай. Не петля. Не спаситель без цены.

  core_reader_desire: |
    Чтобы Элирию наконец увидели. Чтобы страсть была честной.
    Чтобы она выбрала себя — даже если это «грязно» и страшно.

  first_attachment_target: CH_001 — Элирия; удушающая идеальность двора
  first_forbidden_formula: «Если я идеальна и тиха — мне здесь место»
  when_forbidden_formula_appears: CH_008
  why_reader_cares_before_forbidden_formula: |
    Стыд, голод, инфантилизация, цитата про силы на «казаться своей» — до первой R+ сцены.

  protagonist_id: CHAR_ELIRIA
  hero: CHAR_ELIRIA                    # единственный герой; POV 1 лицо
  seducer_id: CHAR_MORVAN_WOLFBJORNE   # соблазнитель; не POV; не герой

  world_name: Лунелия / Вереск

  naming_policy:
    naming_mode: mythic-grounded
    banned_naming_patterns: [re_zero_clone_names, abstract_symbolic_titles, russified_character_surnames]
    name_rule: имена и фамилии без русификации (Wolfbjorne, не Крестов)
    term_budget_per_unit: 5

  prose_voice:
    author_gender_feel: feminine       # как будто женщина написала книгу
    narrator: CHAR_ELIRIA_only

  WorldForce:
    enabled: true
    forces: [WF_PERFECT_SILENCE, WF_CHURCH_ASH, WF_HUNGER_TRUTH]

  loop_profile:
    has_loop: false
    residue_types: [body, shame_memory, relationship, demon_seed]
    micro_win_every_n_units: 2

  motion_profile:
    travel_required: true
    maximum_static_scenes_in_a_row: 2

  NarrativeMode:
    primary: Feminine

  key_characters:
    - {id: CHAR_ELIRIA, tier: KEY, role: protagonist_hero}
    - {id: CHAR_MORVAN_WOLFBJORNE, tier: KEY, role: seducer_love_interest}

  support_characters:
    - CHAR_SILVER_COURT       # аллегория двора / регент-сила
    - CHAR_ASH_PRIOR          # церковь на хвосте
    - CHAR_VERESK_BROKER      # Вереск, сдаёт чужаков

  data_driven_workspace:
    require_state_ledger: true
    require_unit_vectors: true
    separate_debug_file: true
    snapshot_after_each_unit: true
```