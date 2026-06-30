using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_InteractionInputSink : MonoBehaviour, OUTL_IActorInputPhasedSink
{
    public OUTL_EntityAdapter Entity;
    public Camera ViewCamera;
    public Transform ViewRoot;
    public float UseDistance = 3f;
    public LayerMask UseMask = ~0;
    public QueryTriggerInteraction UseTriggers = QueryTriggerInteraction.Collide;
    public OUTL_Interactable CurrentInteractable;
    public OUTL_EntityAdapter CurrentCommandTarget;
    public string LastUseReason = "";

    public OUTL_ActorInputPhase Phase { get { return OUTL_ActorInputPhase.Interaction; } }

    private void Awake()
    {
        Resolve();
    }

    public void OUTL_ApplyInput(in OUTL_ActorInputFrame frame, OUTL_World world)
    {
        Resolve();
        ScanUseTarget();
        if (!frame.UsePressed) return;

        if (CurrentInteractable == null && CurrentCommandTarget == null)
        {
            LastUseReason = "no_target";
            return;
        }

        if (Entity == null || Entity.Runtime == null || IsDead())
        {
            LastUseReason = "source_not_ready";
            return;
        }

        LastUseReason = "used";
        if (CurrentInteractable != null)
        {
            CurrentInteractable.Use(Entity.Id);
            return;
        }

        if (world == null) world = OUTL_World.Instance;
        if (world == null || CurrentCommandTarget == null || !CurrentCommandTarget.Id.IsValid)
        {
            LastUseReason = "target_not_ready";
            return;
        }

        bool sent = world.Commands.Send(new OUTL_Command(OUTL_CommandType.Use, Entity.Id, CurrentCommandTarget.Id) { Point = CurrentCommandTarget.transform.position });
        LastUseReason = sent ? "used_entity" : "command_unhandled";
    }

    private void ScanUseTarget()
    {
        CurrentInteractable = null;
        CurrentCommandTarget = null;

        Transform origin = ViewCamera != null ? ViewCamera.transform : (ViewRoot != null ? ViewRoot : transform);
        Ray ray = new Ray(origin.position, origin.forward);
        RaycastHit hit;
        OUTL_Profile.Frame.Raycasts++;
        if (!Physics.Raycast(ray, out hit, Mathf.Max(0.05f, UseDistance), UseMask, UseTriggers)) return;
        if (hit.collider == null) return;

        CurrentInteractable = hit.collider.GetComponentInParent<OUTL_Interactable>();
        if (CurrentInteractable != null) return;

        OUTL_EntityAdapter adapter = hit.collider.GetComponentInParent<OUTL_EntityAdapter>();
        if (CanUseEntity(adapter)) CurrentCommandTarget = adapter;
    }

    private static bool CanUseEntity(OUTL_EntityAdapter adapter)
    {
        if (adapter == null || adapter.Runtime == null || !adapter.Id.IsValid) return false;
        OUTL_World world = OUTL_World.Instance;
        OUTL_Command command = new OUTL_Command(OUTL_CommandType.Use, OUTL_EntityId.None, adapter.Id);
        OUTL_ICommandReceiver[] receivers = adapter.CommandReceivers;
        for (int i = 0; i < receivers.Length; i++)
            if (receivers[i] != null && receivers[i].OUTL_CanReceive(command, world))
                return true;
        return false;
    }

    private bool IsDead()
    {
        OUTL_EntityRuntime runtime = Entity != null ? Entity.Runtime : null;
        return runtime != null && (runtime.Dead || runtime.LifeState == OUTL_LifeState.Dead || runtime.State.GetFlag(OUTL_StateId.Dead) || runtime.Stats.Get(OUTL_StatId.Health, 1f) <= 0f);
    }

    private void Resolve()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (ViewCamera == null) ViewCamera = GetComponentInChildren<Camera>();
        if (ViewRoot == null) ViewRoot = ViewCamera != null ? ViewCamera.transform : transform;
    }
}
