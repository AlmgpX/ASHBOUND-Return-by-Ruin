# State Variables — Падшая под Луной

```yaml
state_lanes_bitmask_max: 127

plot_state:
  court_erasure: scheduled / failed / hunting
  exile_years: int
  church_hunt_level: 0-5

character_states:
  CHAR_ELIRIA:
    mask_integrity: 0-5
    shame_heat: 0-5
    virginity: intact → lost CH_007
    desire_admitted: false → true CH_008
    knot_memory: false → true CH_007
    human_form_rejected: false → true CH_007
    demon_line_stage: 0-3
  CHAR_MORVAN_WOLFBJORNE:
    outcast_mark: active
    moon_control: 0-5
    pursuit_by_church: 0-5

relationship_states:
  pair: [CHAR_ELIRIA, CHAR_MORVAN_WOLFBJORNE]
  escalation: words → touch → night → crown
  power_balance: shifts each unit

symbol_states:
  SYM_SILVER_CAGE
  SYM_ASH_BRAND
  SYM_FALLEN_MOON
  SYM_DEMON_CROWN
```