using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class OUT_PickupBase : MonoBehaviour, IOutUsable, IOutTriggerReceiver
{
    [System.Serializable]
    public class OUT_PickupEvent : UnityEvent<GameObject> { }

    [Header("Use")]
    [SerializeField] private OUT_UseCapabilityFlags useCaps = OUT_UseCapabilityFlags.ImpulseUse;
    [SerializeField] private bool allowUse = true;
    [SerializeField] private bool allowTriggerPickup = true;
    [SerializeField] private bool requireUser = true;

    [Header("Lifecycle")]
    [SerializeField] private bool disableOnPickup = true;
    [SerializeField] private bool destroyOnPickup = false;
    [SerializeField][Min(0f)] private float destroyDelay = 0f;
    [SerializeField] private bool allowRespawn = false;
    [SerializeField][Min(0.1f)] private float respawnDelay = 10f;

    [Header("State")]
    [SerializeField] private bool pickedUp;

    [Header("Events")]
    [SerializeField] private UnityEvent onPickedUp;
    [SerializeField] private OUT_PickupEvent onPickedUpBy;
    [SerializeField] private UnityEvent onRespawned;

    private float _respawnTime;
    private Collider[] _colliders;
    private Renderer[] _renderers;

    public OUT_UseCapabilityFlags UseCaps => useCaps;
    public bool IsPickedUp => pickedUp;

    private void Awake()
    {
        CacheComponents();
    }

    private void OnEnable()
    {
        if (_colliders == null || _colliders.Length == 0)
            CacheComponents();

        if (!pickedUp)
            SetVisibleState(true);
    }

    private void Update()
    {
        if (!allowRespawn || !pickedUp)
            return;

        if (Time.time < _respawnTime)
            return;

        pickedUp = false;
        SetVisibleState(true);
        onRespawned?.Invoke();
    }

    public bool CanUse(in OUT_UseRequest request)
    {
        if (!allowUse)
            return false;

        return CanPickup(request.User);
    }

    public OUT_UseResult Use(in OUT_UseRequest request)
    {
        if (!CanUse(request))
            return OUT_UseResult.Failed("Pickup use rejected");

        return TryPickup(request.User)
            ? OUT_UseResult.Performed(true, "Pickup collected")
            : OUT_UseResult.Failed("Pickup failed");
    }

    public bool CanReceiveTrigger(in OUT_TriggerContext context)
    {
        if (!allowTriggerPickup)
            return false;

        return CanPickup(context.Instigator);
    }

    public void ReceiveTrigger(in OUT_TriggerContext context)
    {
        if (!CanReceiveTrigger(context))
            return;

        TryPickup(context.Instigator);
    }

    protected virtual bool CanPickup(GameObject picker)
    {
        if (pickedUp)
            return false;

        if (requireUser && picker == null)
            return false;

        return true;
    }

    protected virtual bool ApplyPickup(GameObject picker)
    {
        return true;
    }

    protected bool TryPickup(GameObject picker)
    {
        if (!CanPickup(picker))
            return false;

        if (!ApplyPickup(picker))
            return false;

        pickedUp = true;
        onPickedUp?.Invoke();
        onPickedUpBy?.Invoke(picker);

        if (allowRespawn)
        {
            _respawnTime = Time.time + Mathf.Max(0.1f, respawnDelay);
            SetVisibleState(false);
        }
        else
        {
            if (disableOnPickup)
                gameObject.SetActive(false);

            if (destroyOnPickup)
                Destroy(gameObject, destroyDelay);
        }

        return true;
    }

    private void CacheComponents()
    {
        _colliders = GetComponentsInChildren<Collider>(true);
        _renderers = GetComponentsInChildren<Renderer>(true);
    }

    private void SetVisibleState(bool visible)
    {
        if (_colliders != null)
        {
            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] != null)
                    _colliders[i].enabled = visible;
            }
        }

        if (_renderers != null)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                    _renderers[i].enabled = visible;
            }
        }
    }
}
