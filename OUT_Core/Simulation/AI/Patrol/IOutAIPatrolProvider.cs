using UnityEngine;

public interface IOutAIPatrolProvider
{
    bool HasPatrol { get; }
    bool IsReturningToPatrol { get; }
    bool TryGetCurrentPatrolPoint(Vector3 actorPosition, out Vector3 point, out float waitTime);
    void NotifyPatrolPointReached(Vector3 actorPosition);
    void NotifyPatrolInterrupted();
    void NotifyPatrolResumed();
}
