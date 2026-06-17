# PROJECT_STATUS — «Не спасай её»

> **Движок (канон):** `OUT_Prometheus_Narrative_Engine_v3_1.md`  
> **Обновлено:** 2026-06-15

## Фазы

| Phase | Артефакт | Статус |
|-------|----------|--------|
| 0 | `01_PROJECT_CORE/ProjectInput.md` | APPROVED |
| 1 | `01_PROJECT_CORE/ProjectCore.md` | APPROVED |
| 2 | `03_ENTITIES/CoreOntology.md` | APPROVED |
| 3 | `01_PROJECT_CORE/NarrativeMode.md` | APPROVED |
| 4 | `02_WORLD/StoryBible.md` | APPROVED |
| 5 | `03_ENTITIES/EntityBible.md` + `CharacterCoreContracts.md` | APPROVED |
| 6 | `04_STATE/StateVariables.md` | APPROVED |
| 6.5 | `04_STATE/StateLedger_DEBUG.md` + `ContinuityLog.md` | APPROVED |
| 7 | `05_ARCS/GlobalArcGeometry.md` | APPROVED |
| 8 | `06_CONTRACTS/ChapterMap.md` | APPROVED |
| 9 | `06_CONTRACTS/ChapterContracts.md` | APPROVED |
| 10 | `06_CONTRACTS/SceneContracts.md` (55 сцен) | APPROVED |
| 11 | `08_OUTPUT/PR_000–CH_004.md` | **IN PROGRESS** — CH_004 done 2026-06-15 |

## Структура state (v3_1)

```text
04_STATE/
  StateVariables.md      — схема + базовые BEFORE snapshots
  StateLedger_DEBUG.md   — BEFORE / VECTOR / AFTER / DELTA по главам
  ContinuityLog.md       — факты прозы (тело, инвентарь, опыт)
```

## Project Gate (v3_1 §23.1)

- [x] Core Promise
- [x] Protagonist wound/lie/mask/desire/need/thesis
- [x] Key character core contracts
- [x] World pressure + symbols
- [x] State Ledger schema
- [x] Chapter count + map
- [x] Global Arc Geometry
- [x] Chapter Contracts (PHASE 9)
- [x] Scene Contracts (PHASE 10)

**Проза:** разрешена с PR_000 / CH_001 по контрактам. После каждой главы — ledger + continuity.