using UnityEngine;

public interface IOutPerceptionTarget
{
    bool CanBePerceived { get; }
    Vector3 PerceptionOrigin { get; }
    float VisibilityRadius { get; }
    float NoiseRadius { get; }
}
