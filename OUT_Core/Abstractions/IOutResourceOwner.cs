using UnityEngine;

public interface IOutResourceOwner
{
    GameObject ResourceOwnerObject { get; }

    bool HasResourcePool(OUT_ResourceKind kind);
    bool TryGetResourcePool(OUT_ResourceKind kind, out OUT_ResourcePool pool);

    int AddResource(OUT_ResourceKind kind, int amount);
    int ConsumeResource(OUT_ResourceKind kind, int amount);
}