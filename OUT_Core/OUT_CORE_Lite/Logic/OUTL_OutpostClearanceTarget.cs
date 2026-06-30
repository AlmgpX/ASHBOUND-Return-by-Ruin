using System;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("OUT CORE Lite/Logic/Outpost Clearance Target")]
public sealed class OUTL_OutpostClearanceTarget : MonoBehaviour
{
    public OUTL_EntityAdapter Entity;
    public string StableId;
    public string OutpostId;
    public bool CountsForClearance = true;

    public bool IsDestroyed { get; private set; }

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        EnsureStableId();
        ApplyStableIdToEntity();
    }

    private void OnValidate()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        EnsureStableId();
        ApplyStableIdToEntity();
    }

    [ContextMenu("OUTL Mark Target Destroyed")]
    public void MarkDestroyed()
    {
        if (IsDestroyed) return;
        IsDestroyed = true;
        OUTL_World world = OUTL_World.Instance;
        if (world != null)
            world.Events.Emit(new OUTL_Event(OUTL_EventType.Custom, OUTL_EntityId.None, Entity != null ? Entity.Id : OUTL_EntityId.None)
            {
                Key = StableId,
                Point = transform.position
            });
    }

    private void EnsureStableId()
    {
        if (string.IsNullOrEmpty(StableId))
            StableId = "outpost.target." + Guid.NewGuid().ToString("N");
    }

    private void ApplyStableIdToEntity()
    {
        if (Entity == null || string.IsNullOrEmpty(StableId)) return;
        Entity.StableId = StableId;
        Entity.SavePersistent = true;
        Entity.MarkAddressDirty();
    }
}
