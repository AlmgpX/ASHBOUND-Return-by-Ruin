# WorldForce — Падшая под Луной

> **Статус:** APPROVED

```yaml
WorldForce:
  enabled: true
  forces:
    - WF_PERFECT_SILENCE
    - WF_CHURCH_ASH
    - WF_HUNGER_TRUTH
```

## WF_PERFECT_SILENCE

```yaml
id: WF_PERFECT_SILENCE
name: Идеальная тишина
function: |
  Мир платит за правильное лицо. Кто перестаёт казаться «своим» в чужих правилах —
  теряет место или получает власть иначе (через стыд и желание).
not: злодей-регент с монологом
symptoms: [сжатие в груди, правильные ответы, инфантилизация, улыбка как броня]
institutions_embodied: [Серебряный Двор]
```

## WF_CHURCH_ASH

```yaml
id: WF_CHURCH_ASH
name: Пепельная запись
function: |
  Церковь не спасает — фиксирует грех и сжигает тех, кто не вписался.
  Оступник = тот, кого выписали из «чистоты».
not: добрый орден
symptoms: [клеймо, крест пепла, охота, кровь как документ]
institutions_embodied: [Орден Пепельной Луны]
```

## WF_HUNGER_TRUTH

```yaml
id: WF_HUNGER_TRUTH
name: Голод правды
function: |
  Голод (еда, тело, признание) снимает маску быстрее насилия.
not: мораль «надо поститься»
symptoms: [пустой желудок, дрожь, щёки горят, ночь без возврата]
institutions_embodied: [рынок Вереска, изгнание]
```