using UnityEngine;

public enum OUTL_SquadRole : byte
{
    Rifle = 0,
    Leader = 1,
    Support = 2,
    Scout = 3,
    Sniper = 4,
    Melee = 5,
    Creature = 6,
    Guard = 7,
    Flanker = 8,
    Suppressor = 9
}

[DisallowMultipleComponent]
public class OUTL_SquadMember : MonoBehaviour
{
    public OUTL_AIActor Actor;
    public OUTL_EntityAdapter Entity;
    public OUTL_SquadCommander Commander;
    public OUTL_SquadBlackboard Blackboard;
    public OUTL_SquadDef SquadDef;
    public OUTL_SquadRole RoleKind = OUTL_SquadRole.Rifle;
    public string Role = "rifle";
    public OUTL_SquadOrder CurrentOrder;
    public OUTL_CoverPoint ReservedCover;

    private void Awake()
    {
        Resolve();
    }

    private void OnEnable()
    {
        Resolve();
        if (Commander != null) Commander.Register(this);
        if (Blackboard != null) Blackboard.Register(this);
    }

    private void OnDisable()
    {
        if (Commander != null) Commander.Unregister(this);
        if (Blackboard != null) Blackboard.Unregister(this);
        ReleaseCover();
    }

    public void ReceiveOrder(OUTL_SquadOrder order)
    {
        CurrentOrder = order;
        if (Actor != null) Actor.ReceiveSquadOrder(order);
        if (Entity != null) OUTL_DebugLog.TraceAI(Entity.Id, "order=" + order.Type + " key=" + order.Key + " pos=" + order.Position + " target=" + order.Target);
    }

    public bool TryReserveCover(OUTL_CoverPoint cover, float seconds, string reason)
    {
        if (cover == null || Entity == null) return false;
        if (Blackboard != null && !Blackboard.TryReserveCover(this, cover, seconds, reason)) return false;
        if (!cover.Reserve(Entity, seconds, reason)) return false;
        ReservedCover = cover;
        return true;
    }

    public void ReleaseCover()
    {
        if (ReservedCover != null && Entity != null) ReservedCover.Release(Entity);
        if (Blackboard != null) Blackboard.ReleaseCover(this);
        ReservedCover = null;
    }

    private void Resolve()
    {
        if (Actor == null) Actor = GetComponent<OUTL_AIActor>();
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (Blackboard == null) Blackboard = GetComponentInParent<OUTL_SquadBlackboard>();
    }
}
