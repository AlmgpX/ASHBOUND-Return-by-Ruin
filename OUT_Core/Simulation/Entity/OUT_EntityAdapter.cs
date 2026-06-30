using UnityEngine;

[DisallowMultipleComponent]
public class OUT_EntityAdapter : MonoBehaviour, IOutEntityRuntime
{
    [Header("Identity")]
    [SerializeField] private string entityName;
    [SerializeField] private string archetypeId;
    [SerializeField] private OUT_RuntimeTier runtimeTier = OUT_RuntimeTier.Full;
    [SerializeField] private int sectorId = -1;

    [Header("Lifecycle")]
    [SerializeField] private bool registerOnEnable = true;
    [SerializeField] private bool unregisterOnDisable = true;
    [SerializeField] private bool autoRegisterThinkables = true;

    private OUT_SimEntityId simEntityId = OUT_SimEntityId.None;
    private IOutRuntimeTierReceiver[] tierReceivers;

    public OUT_SimEntityId SimEntityId { get { return simEntityId; } }
    public int EntityId { get { return simEntityId.Value; } }
    public string EntityName { get { return string.IsNullOrEmpty(entityName) ? name : entityName; } }
    public string ArchetypeId { get { return archetypeId; } }
    public GameObject EntityObject { get { return gameObject; } }
    public Transform EntityTransform { get { return transform; } }
    public OUT_RuntimeTier RuntimeTier { get { return runtimeTier; } }
    public int SectorId { get { return sectorId; } }

    private void OnEnable()
    {
        if (registerOnEnable)
            OUT_EntityRegistry.EnsureExists().Register(this);

        if (autoRegisterThinkables)
            OUT_ThinkScheduler.RegisterFromGameObject(gameObject);
    }

    private void OnDisable()
    {
        if (autoRegisterThinkables)
            OUT_ThinkScheduler.UnregisterFromGameObject(gameObject);

        if (unregisterOnDisable && OUT_EntityRegistry.Instance != null)
            OUT_EntityRegistry.Instance.Unregister(this);
    }

    public void ApplySimEntityId(OUT_SimEntityId id)
    {
        simEntityId = id;
    }

    public void ClearSimEntityId()
    {
        simEntityId = OUT_SimEntityId.None;
    }

    public void SetSectorId(int newSectorId)
    {
        sectorId = newSectorId;
    }

    public void SetRuntimeTier(OUT_RuntimeTier newTier)
    {
        if (runtimeTier == newTier)
            return;

        OUT_RuntimeTier oldTier = runtimeTier;
        runtimeTier = newTier;

        if (tierReceivers == null)
            tierReceivers = GetComponentsInChildren<IOutRuntimeTierReceiver>(true);

        for (int i = 0; i < tierReceivers.Length; i++)
            if (tierReceivers[i] != null)
                tierReceivers[i].OnRuntimeTierChanged(oldTier, newTier);
    }
}
