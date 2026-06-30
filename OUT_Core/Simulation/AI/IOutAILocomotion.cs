using UnityEngine;

public interface IOutAILocomotion
{
    bool TryMoveTo(Vector3 destination, float acceptanceRadius = 0.5f);
    void Stop();
    void Face(Vector3 point);
    bool HasReachedDestination(float acceptanceRadius = 0.5f);

    bool IsMoving { get; }
    Vector3 CurrentDestination { get; }
    Vector3 Velocity { get; }
}
