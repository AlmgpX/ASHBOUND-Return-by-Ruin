using UnityEngine;

public abstract class OUT_AIAttackEvaluator : MonoBehaviour
{
    public abstract void Evaluate(
        OUT_AIBlackboard blackboard,
        ref OUT_AIConditionFlags conditions,
        OUT_AIState currentState);
}