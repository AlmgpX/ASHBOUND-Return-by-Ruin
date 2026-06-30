using UnityEngine;

public static class OUT_ConditionAssembler
{
    public static void ResetCombatFlags(ref OUT_ConditionState state)
    {
        state.Clear(
            OUT_ConditionFlags.SeeEnemy |
            OUT_ConditionFlags.HearDanger |
            OUT_ConditionFlags.HearWorld |
            OUT_ConditionFlags.HearPlayer |
            OUT_ConditionFlags.HearCombat |
            OUT_ConditionFlags.EnemyOccluded |
            OUT_ConditionFlags.EnemyTooFar |
            OUT_ConditionFlags.EnemyDead |
            OUT_ConditionFlags.NewEnemy |
            OUT_ConditionFlags.LightDamage |
            OUT_ConditionFlags.HeavyDamage |
            OUT_ConditionFlags.CanRangeAttack1 |
            OUT_ConditionFlags.CanRangeAttack2 |
            OUT_ConditionFlags.CanMeleeAttack1 |
            OUT_ConditionFlags.CanMeleeAttack2 |
            OUT_ConditionFlags.NoAmmoLoaded |
            OUT_ConditionFlags.NoFire
        );
    }

    public static void ApplyPerception(
        ref OUT_ConditionState state,
        in OUT_PerceptionSnapshot snapshot,
        float tooFarDistance)
    {
        if (snapshot.TargetObject == null)
            return;

        state.Assign(snapshot.IsVisible, OUT_ConditionFlags.SeeEnemy);
        state.Assign(snapshot.IsAudible, OUT_ConditionFlags.HearCombat);

        if (!snapshot.IsVisible && (snapshot.IsAudible || snapshot.TargetObject != null))
            state.Set(OUT_ConditionFlags.EnemyOccluded);

        if (snapshot.Distance > tooFarDistance)
            state.Set(OUT_ConditionFlags.EnemyTooFar);
    }

    public static void ApplyAmmoState(ref OUT_ConditionState state, bool hasAmmoLoaded)
    {
        state.Assign(!hasAmmoLoaded, OUT_ConditionFlags.NoAmmoLoaded);
    }

    public static void ApplyAttackCapabilities(
        ref OUT_ConditionState state,
        bool canRangeAttack1,
        bool canRangeAttack2,
        bool canMeleeAttack1,
        bool canMeleeAttack2,
        bool noFriendlyFire)
    {
        state.Assign(canRangeAttack1, OUT_ConditionFlags.CanRangeAttack1);
        state.Assign(canRangeAttack2, OUT_ConditionFlags.CanRangeAttack2);
        state.Assign(canMeleeAttack1, OUT_ConditionFlags.CanMeleeAttack1);
        state.Assign(canMeleeAttack2, OUT_ConditionFlags.CanMeleeAttack2);
        state.Assign(!noFriendlyFire, OUT_ConditionFlags.NoFire);
    }

    public static void ApplyDamageFeedback(
        ref OUT_ConditionState state,
        int damageAmount,
        int lightDamageThreshold,
        int heavyDamageThreshold)
    {
        if (damageAmount >= heavyDamageThreshold)
        {
            state.Set(OUT_ConditionFlags.HeavyDamage);
            state.Clear(OUT_ConditionFlags.LightDamage);
            return;
        }

        if (damageAmount >= lightDamageThreshold)
            state.Set(OUT_ConditionFlags.LightDamage);
    }

    public static void ApplyEnemyDeath(ref OUT_ConditionState state, bool enemyDead)
    {
        state.Assign(enemyDead, OUT_ConditionFlags.EnemyDead);
    }

    public static void ApplyNewEnemy(ref OUT_ConditionState state, bool isNewEnemy)
    {
        state.Assign(isNewEnemy, OUT_ConditionFlags.NewEnemy);
    }
}