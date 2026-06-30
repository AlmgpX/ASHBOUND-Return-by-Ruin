using UnityEngine;

public interface IOutEntityRuntime
{
    OUT_SimEntityId SimEntityId { get; }
    int EntityId { get; }
    string EntityName { get; }
    GameObject EntityObject { get; }
    Transform EntityTransform { get; }
    OUT_RuntimeTier RuntimeTier { get; }
    int SectorId { get; }
}

public interface IOutRuntimeTierReceiver
{
    void OnRuntimeTierChanged(OUT_RuntimeTier oldTier, OUT_RuntimeTier newTier);
}

public interface IOutThinkable
{
    bool IsThinkEnabled { get; }
    OUT_ThinkGroup ThinkGroup { get; }
    float ThinkInterval { get; }
    void OutThink(float deltaTime);
}
