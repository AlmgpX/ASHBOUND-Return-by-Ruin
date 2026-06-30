using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class OUT_ResourceMachine : MonoBehaviour, IOutUsable, IOutContinuousUsable
{


    protected virtual void OUT_OnUseStarted(GameObject user) { }
    protected virtual void OUT_OnPulse(GameObject user, int transferredAmount) { }
    protected virtual void OUT_OnUseEnded(GameObject user) { }
    protected virtual void OUT_OnDepleted() { }
    protected virtual void OUT_OnRecharged() { }

    [Header("Identity")]
    [SerializeField] private OUT_EntityId entityId;
    [SerializeField] private string debugName;

    [Header("State")]
    [SerializeField] private OUT_WorldMachineState machineState = OUT_WorldMachineState.Idle;
    [SerializeField] private bool startLocked = false;

    [Header("Resource")]
    [SerializeField] private OUT_ResourcePool reserve = new OUT_ResourcePool(OUT_ResourceKind.Energy, 100, 100);
    [SerializeField, Min(1)] private int transferPerPulse = 1;
    [SerializeField, Min(0.01f)] private float transferInterval = 0.1f;

    [Header("Recharge")]
    [SerializeField] private bool autoRecharge = true;
    [SerializeField, Min(0f)] private float rechargeDelay = 20f;
    [SerializeField, Min(0.01f)] private float rechargeInterval = 0.25f;
    [SerializeField, Min(1)] private int rechargePerPulse = 5;

    [Header("Use")]
    [SerializeField]
    private OUT_UseCapabilityFlags useCaps =
        OUT_UseCapabilityFlags.ImpulseUse | OUT_UseCapabilityFlags.ContinuousUse;

    [SerializeField] private bool requireResourceOwner = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onUseStarted;
    [SerializeField] private UnityEvent onPulse;
    [SerializeField] private UnityEvent onUseEnded;
    [SerializeField] private UnityEvent onDepleted;
    [SerializeField] private UnityEvent onRecharged;

    private GameObject _activeUser;
    private float _nextTransferTime;
    private float _rechargeStartTime = -1f;
    private float _nextRechargeTime;

    public OUT_EntityId EntityId => entityId;
    public string DebugName => string.IsNullOrWhiteSpace(debugName) ? name : debugName;
    public OUT_WorldMachineState MachineState => machineState;
    public OUT_ResourcePool Reserve => reserve;

    public OUT_UseCapabilityFlags UseCaps => useCaps;

    private void Reset()
    {
        if (!entityId.IsValid)
            entityId = OUT_EntityId.NewId();

        if (startLocked)
            machineState = OUT_WorldMachineState.Locked;
        else if (machineState == OUT_WorldMachineState.None)
            machineState = OUT_WorldMachineState.Idle;
    }

    private void Awake()
    {
        if (!entityId.IsValid)
            entityId = OUT_EntityId.NewId();

        EnsureReserve();

        if (startLocked)
            machineState = OUT_WorldMachineState.Locked;
        else if (machineState == OUT_WorldMachineState.None)
            machineState = OUT_WorldMachineState.Idle;

        if (reserve.IsDepleted && machineState == OUT_WorldMachineState.Idle)
            machineState = OUT_WorldMachineState.Depleted;
    }



    public OUT_ResourceKind ResourceKind => reserve != null ? reserve.Kind : OUT_ResourceKind.None;

    public void OUT_ConfigureResource(OUT_ResourceKind kind, int max, int current = -1)
    {
        EnsureReserve();
        reserve.EnsureKind(kind);
        reserve.SetMax(max, true);

        if (current >= 0)
            reserve.SetCurrent(current);
        else
            reserve.Refill();

        if (reserve.IsDepleted)
            machineState = OUT_WorldMachineState.Depleted;
        else if (machineState == OUT_WorldMachineState.None || machineState == OUT_WorldMachineState.Depleted)
            machineState = OUT_WorldMachineState.Idle;
    }

    public void OUT_ConfigureTransfer(int amountPerPulse, float intervalSeconds)
    {
        transferPerPulse = Mathf.Max(1, amountPerPulse);
        transferInterval = Mathf.Max(0.01f, intervalSeconds);
    }

    public void OUT_ConfigureRecharge(bool enabled, float delaySeconds, float intervalSeconds, int amountPerPulse)
    {
        autoRecharge = enabled;
        rechargeDelay = Mathf.Max(0f, delaySeconds);
        rechargeInterval = Mathf.Max(0.01f, intervalSeconds);
        rechargePerPulse = Mathf.Max(1, amountPerPulse);
    }

    public void OUT_ConfigureUseCaps(OUT_UseCapabilityFlags caps, bool requireOwner)
    {
        useCaps = caps;
        requireResourceOwner = requireOwner;
    }

    private void Update()
    {
        TickRecharge();
    }

    public bool CanUse(in OUT_UseRequest request)
    {
        if (machineState == OUT_WorldMachineState.Disabled ||
            machineState == OUT_WorldMachineState.Broken ||
            machineState == OUT_WorldMachineState.Locked)
            return false;

        EnsureReserve();

        if (!reserve.IsValid)
            return false;

        if (reserve.IsDepleted)
            return false;

        if (!requireResourceOwner)
            return request.User != null;

        return TryResolveOwner(request.User, out IOutResourceOwner owner) &&
               CanTransferToOwner(owner);
    }

    public OUT_UseResult Use(in OUT_UseRequest request)
    {
        if (!CanUse(request))
            return OUT_UseResult.Failed("Machine rejected use");

        if ((useCaps & OUT_UseCapabilityFlags.ContinuousUse) != 0)
        {
            _activeUser = request.User;
            machineState = OUT_WorldMachineState.Starting;
            _nextTransferTime = 0f;
            OUT_OnUseStarted(_activeUser);
            onUseStarted?.Invoke();

            int transferred = TryTransferPulse(_activeUser);
            return transferred > 0
                ? OUT_UseResult.Performed(true, "Continuous use started")
                : OUT_UseResult.Failed("No resource transferred");
        }

        int impulseTransferred = TryTransferPulse(request.User);
        return impulseTransferred > 0
            ? OUT_UseResult.Performed(true, "Impulse use performed")
            : OUT_UseResult.Failed("No resource transferred");
    }

    public bool CanContinueUse(in OUT_UseRequest request)
    {
        if (_activeUser == null)
            return false;

        if (request.User != _activeUser)
            return false;

        if (machineState == OUT_WorldMachineState.Disabled ||
            machineState == OUT_WorldMachineState.Broken ||
            machineState == OUT_WorldMachineState.Locked ||
            machineState == OUT_WorldMachineState.Depleted)
            return false;

        EnsureReserve();

        if (!reserve.IsValid || reserve.IsDepleted)
            return false;

        if (!requireResourceOwner)
            return true;

        return TryResolveOwner(request.User, out IOutResourceOwner owner) &&
               CanTransferToOwner(owner);
    }

    public OUT_UseResult ContinueUse(in OUT_UseRequest request)
    {
        if (!CanContinueUse(request))
        {
            EndUse(request);
            return OUT_UseResult.Failed("Cannot continue use");
        }

        if (Time.time < _nextTransferTime)
            return OUT_UseResult.Performed(false, "Waiting for next transfer pulse");

        int transferred = TryTransferPulse(request.User);

        if (transferred <= 0)
        {
            EndUse(request);
            return OUT_UseResult.Failed("Transfer pulse failed");
        }

        return OUT_UseResult.Performed(true, "Transfer pulse performed");
    }

    public void EndUse(in OUT_UseRequest request)
    {
        if (_activeUser != null && request.User != null && request.User != _activeUser)
            return;

        GameObject endedUser = _activeUser;
        _activeUser = null;

        if (machineState == OUT_WorldMachineState.Starting ||
            machineState == OUT_WorldMachineState.Active)
        {
            machineState = reserve.IsDepleted
                ? OUT_WorldMachineState.Depleted
                : OUT_WorldMachineState.Idle;
        }

        OUT_OnUseEnded(endedUser);
        onUseEnded?.Invoke();
    }

    public void OUT_SetLocked(bool locked)
    {
        if (locked)
        {
            _activeUser = null;
            machineState = OUT_WorldMachineState.Locked;
            return;
        }

        if (machineState == OUT_WorldMachineState.Locked)
            machineState = reserve != null && !reserve.IsDepleted
                ? OUT_WorldMachineState.Idle
                : OUT_WorldMachineState.Depleted;
    }

    public void OUT_SetBroken(bool broken)
    {
        if (broken)
        {
            _activeUser = null;
            machineState = OUT_WorldMachineState.Broken;
            return;
        }

        if (machineState == OUT_WorldMachineState.Broken)
            machineState = reserve != null && !reserve.IsDepleted
                ? OUT_WorldMachineState.Idle
                : OUT_WorldMachineState.Depleted;
    }

    public void OUT_ForceRefill()
    {
        EnsureReserve();
        reserve.Refill();
        machineState = OUT_WorldMachineState.Idle;
        _rechargeStartTime = -1f;
        _nextRechargeTime = 0f;
        OUT_OnRecharged();
        onRecharged?.Invoke();
    }

    public void OUT_ForceDeplete()
    {
        EnsureReserve();
        reserve.Deplete();
        HandleDepleted();
    }

    private int TryTransferPulse(GameObject userObject)
    {
        EnsureReserve();

        if (!reserve.IsValid || reserve.IsDepleted)
        {
            HandleDepleted();
            return 0;
        }

        int transferred = 0;

        if (requireResourceOwner)
        {
            if (!TryResolveOwner(userObject, out IOutResourceOwner owner))
                return 0;

            if (!CanTransferToOwner(owner))
                return 0;

            int requestAmount = Mathf.Min(transferPerPulse, reserve.Current);
            transferred = owner.AddResource(reserve.Kind, requestAmount);
        }
        else
        {
            transferred = Mathf.Min(transferPerPulse, reserve.Current);
        }

        if (transferred <= 0)
            return 0;

        reserve.Consume(transferred);

        machineState = reserve.IsDepleted
            ? OUT_WorldMachineState.Depleted
            : OUT_WorldMachineState.Active;

        _nextTransferTime = Time.time + transferInterval;
        OUT_OnPulse(userObject, transferred);
        onPulse?.Invoke();

        if (reserve.IsDepleted)
            HandleDepleted();

        return transferred;
    }

    private bool CanTransferToOwner(IOutResourceOwner owner)
    {
        if (owner == null)
            return false;

        if (!owner.TryGetResourcePool(reserve.Kind, out OUT_ResourcePool pool))
            return false;

        if (pool == null)
            return false;

        return pool.CanAdd(1);
    }

    private bool TryResolveOwner(GameObject userObject, out IOutResourceOwner owner)
    {
        owner = null;

        if (userObject == null)
            return false;

        MonoBehaviour[] behaviours = userObject.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IOutResourceOwner resourceOwner)
            {
                owner = resourceOwner;
                return true;
            }
        }

        return false;
    }

    private void HandleDepleted()
    {
        _activeUser = null;
        machineState = OUT_WorldMachineState.Depleted;
        OUT_OnDepleted();
        onDepleted?.Invoke();

        if (autoRecharge)
        {
            _rechargeStartTime = Time.time + rechargeDelay;
            _nextRechargeTime = _rechargeStartTime;
        }
    }

    private void TickRecharge()
    {
        if (!autoRecharge)
            return;

        if (machineState != OUT_WorldMachineState.Depleted &&
            machineState != OUT_WorldMachineState.Recharging)
            return;

        EnsureReserve();

        if (!reserve.IsValid || reserve.Max <= 0)
            return;

        if (_rechargeStartTime < 0f)
            return;

        if (Time.time < _rechargeStartTime)
            return;

        if (Time.time < _nextRechargeTime)
            return;

        machineState = OUT_WorldMachineState.Recharging;

        reserve.Add(rechargePerPulse);
        _nextRechargeTime = Time.time + rechargeInterval;

        if (reserve.IsFull)
        {
            _rechargeStartTime = -1f;
            _nextRechargeTime = 0f;
            machineState = OUT_WorldMachineState.Idle;
            OUT_OnRecharged();
            onRecharged?.Invoke();
        }
    }

    private void EnsureReserve()
    {
        if (reserve == null)
            reserve = new OUT_ResourcePool();
    }
}