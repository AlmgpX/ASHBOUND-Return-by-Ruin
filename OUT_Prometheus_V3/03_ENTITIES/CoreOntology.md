# Core Ontology — PHASE 2

> **Статус:** `APPROVED` — 2026-06-15  
> **Движок:** `OUT_Prometheus_Narrative_Engine_v3_1.md`

```yaml
CoreOntology:
  status: APPROVED
  engine: OUT_Prometheus_Narrative_Engine_v3_1.md

  entity_types_active:
    - Character      # Марк, Лиэрра, друг, NPC петли
    - Faction        # церковь, дворяне, магические корпорации/мастерские
    - Location       # бункер, площадь казни, кварталы по фону
    - Object         # оружие, печати, контуры
    - Symbol         # узор бункера, фон/просадка, точка выбора
    - Secret         # природа петли, природа Лиэрры
    - Threat         # система, сопротивление исходов, церковные контуры охоты
    - Rule           # законы магии, социальные WorldRule
    - Event          # DeathLoop, PublicExecution, OutcomeEngineered, ContourBreak
    - Concept        # LOOP_CHOICE_POINT, OUTCOME_ENGINEERING, FALSE_COMPETENCE
    - Claim          # что мир говорит vs что Марк знает из петель

  systems_active:
    - PlotSystem
    - LoopSystem              # петля «точки выбора» — проектный, не в engine по умолчанию
    - ConflictSystem
    - RelationshipSystem      # Марк–Лиэрра опасная взаимозависимость
    - StatusSystem
    - FactionSystem
    - SymbolSystem
    - CharacterCoreSystem
    - ReaderStateSystem
    - EngagementSystem
    - MagicPhysicsSystem      # фон/напор/контур/просадка
    - PredictorCorrectorSystem
    - ContinuitySystem
    - PacingSystem

  event_types_expected:
    - PublicHumiliation
    - PublicExecution
    - DeathLoop
    - ContourBreak
    - StatusChanged
    - BodyMarked
    - Betrayal
    - ChoiceCommitted
    - OutcomeEngineered
    - SecretRevealed
    - BoundaryCrossed
    - GoblinRaid              # когда контракт требует
    - SexualViolence          # R+ по контракту, без эллипсиса
    - LoopVariant             # альтернативный исход петли
    - TravelStateChange       # смена региона / каста
    - AllyGained / AllyLost
    - EnemyIdentified

  loop_mechanic_entity:
    id: MECH_CHOICE_POINT
    name: "точка выбора"
    type: Mechanic
    prose_term: |
      «день ещё не затвердел», «событие ещё не стало настоящим»,
      «реальность ещё держала несколько вариантов»
    rules:
      - активируется рядом с Лиэррой или событием, связанным с ней
      - память исходов даётся; сила — нет
      - детали меняются между петлями
      - сопротивление мира растёт при давлении на один исход
      - рассказ слушателю → побочные эффекты (рвота, кровь, охота контуров церкви)
      - сохраняются страховые рефлексы, не навыки
      - месть через допустимый плохой исход системы

  key_characters:  # CharacterCoreContracts.md
    - CHAR_MARK
    - CHAR_LIERRA
    - CHAR_RENNA_KALD
    - CHAR_POL
```