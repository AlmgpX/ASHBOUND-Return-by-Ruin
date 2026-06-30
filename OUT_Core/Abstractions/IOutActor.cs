using UnityEngine;

public interface IOutActor
{
    GameObject ActorObject { get; }
    Transform ActorTransform { get; }

    bool IsAlive { get; }
    OUT_ActorState ActorState { get; }

    void SpawnActor();
    void DespawnActor();
}
