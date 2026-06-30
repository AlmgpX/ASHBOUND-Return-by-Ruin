using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_NetworkIdentityLite : MonoBehaviour
{
    public OUTL_EntityAdapter Entity;
    public int NetId;
    public bool ServerOwned = true;
    public bool ReplicateTransform = true;
    public bool ReplicateStats = true;
    public bool ReplicateState = true;
    public float SendInterval = 0.05f;
    public float PositionSnapDistance = 4f;

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
    }

    private void OnValidate()
    {
        SendInterval = Mathf.Max(0.01f, SendInterval);
        PositionSnapDistance = Mathf.Max(0f, PositionSnapDistance);
    }

    public string StableNetworkKey
    {
        get
        {
            if (Entity != null)
            {
                if (!string.IsNullOrEmpty(Entity.StableId)) return Entity.StableId;
                if (!string.IsNullOrEmpty(Entity.TargetName)) return Entity.TargetName;
            }
            return name;
        }
    }
}
