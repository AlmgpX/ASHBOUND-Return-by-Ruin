using UnityEngine;

[DisallowMultipleComponent]
public class OUT_SoldierAttackExecutor : OUT_AIAttackExecutor
{
    [SerializeField] private OUT_WeaponController weapon;
    [SerializeField] private OUT_SoldierVoiceBarks voiceBarks;
    [SerializeField] private OUT_SoldierAttackEvaluator attackEvaluator;
    [SerializeField] private bool meleeUsesSecondary = true;

    private void Awake()
    {
        if (weapon == null)
            weapon = GetComponent<OUT_WeaponController>();

        if (voiceBarks == null)
            voiceBarks = GetComponent<OUT_SoldierVoiceBarks>();

        if (attackEvaluator == null)
            attackEvaluator = GetComponent<OUT_SoldierAttackEvaluator>();
    }

    public override bool CanExecute(OUT_AITaskType taskType)
    {
        if (weapon == null)
            return false;

        switch (taskType)
        {
            case OUT_AITaskType.Reload:
                return weapon.Primary.CanReload || weapon.Secondary.CanReload;
            case OUT_AITaskType.RangeAttack1:
                return true;
            case OUT_AITaskType.RangeAttack2:
                return true;
            case OUT_AITaskType.MeleeAttack1:
            case OUT_AITaskType.MeleeAttack2:
                return true;
            default:
                return false;
        }
    }

    public override void Execute(OUT_AITaskType taskType, OUT_AIBlackboard blackboard)
    {
        if (weapon == null)
            return;

        if (blackboard != null)
        {
            if (blackboard.Enemy != null)
                weapon.SetExplicitTarget(blackboard.Enemy.transform);
            else if (blackboard.EnemyLastKnownPosition != Vector3.zero)
                weapon.SetExplicitAimPoint(blackboard.EnemyLastKnownPosition);
        }

        switch (taskType)
        {
            case OUT_AITaskType.Reload:
                if (weapon.TryReloadPrimary() || weapon.TryReloadSecondary())
                    voiceBarks?.PlayReload(0.8f);
                break;

            case OUT_AITaskType.RangeAttack1:
                if (weapon.TryFirePrimary())
                    voiceBarks?.PlaySuppressFire(0.28f);
                break;

            case OUT_AITaskType.RangeAttack2:
                if (weapon.TryFireSecondary())
                {
                    voiceBarks?.PlayExplosiveAttack(0.9f);
                    attackEvaluator?.NotifySecondaryFired();
                }
                break;

            case OUT_AITaskType.MeleeAttack1:
            case OUT_AITaskType.MeleeAttack2:
                bool fired = meleeUsesSecondary ? weapon.TryFireSecondary() : weapon.TryFirePrimary();
                if (fired)
                    voiceBarks?.PlayMeleeAttack(0.55f);
                break;
        }
    }
}
