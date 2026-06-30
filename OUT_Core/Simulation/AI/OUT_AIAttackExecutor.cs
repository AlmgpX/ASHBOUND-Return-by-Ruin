using UnityEngine;

public abstract class OUT_AIAttackExecutor : MonoBehaviour
{
    public abstract bool CanExecute(OUT_AITaskType taskType);

    public abstract void Execute(
        OUT_AITaskType taskType,
        OUT_AIBlackboard blackboard);
}