using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class OUTL_SquadCommander : MonoBehaviour, OUTL_ITickable
{
    public OUTL_EntityAdapter Entity;
    public OUTL_AIActor Actor;
    public OUTL_SquadBlackboard Blackboard;
    public List<OUTL_SquadMember> Members = new List<OUTL_SquadMember>(8);
    public float OrderInterval = 1f;
    public float OrderLifetime = 4f;
    public float MemberSpacing = 3f;
    public bool AutoRegisterTick = true;

    private float nextOrderTime;

    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled; } }
    public OUTL_TickLane OUTL_TickLane { get { return OUTL_TickLane.AI; } }
    public float OUTL_TickInterval { get { return Mathf.Max(0.1f, OrderInterval); } }

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (Actor == null) Actor = GetComponent<OUTL_AIActor>();
        if (Blackboard == null) Blackboard = GetComponent<OUTL_SquadBlackboard>();
    }

    private void OnEnable()
    {
        if (AutoRegisterTick && OUTL_World.Instance != null) OUTL_World.Instance.Scheduler.Register(this);
    }

    private void OnDisable()
    {
        if (OUTL_World.Instance != null) OUTL_World.Instance.Scheduler.Unregister(this);
    }

    public void Register(OUTL_SquadMember member)
    {
        if (member != null && !Members.Contains(member)) Members.Add(member);
        if (Blackboard != null && member != null) Blackboard.Register(member);
    }

    public void Unregister(OUTL_SquadMember member)
    {
        Members.Remove(member);
        if (Blackboard != null) Blackboard.Unregister(member);
    }

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        if (time < nextOrderTime) return;
        nextOrderTime = time + OrderInterval;
        IssueOrders();
    }

    public void IssueOrders()
    {
        OUTL_EntityId target = Actor != null ? Actor.CurrentTarget : OUTL_EntityId.None;
        Vector3 orderPos = Actor != null ? Actor.LastKnownTargetPosition : transform.position;
        bool hasTarget = target.IsValid;
        if (Blackboard != null && hasTarget) Blackboard.PublishTarget(target, orderPos, 1f, OrderLifetime);

        for (int i = Members.Count - 1; i >= 0; i--)
        {
            OUTL_SquadMember member = Members[i];
            if (member == null)
            {
                Members.RemoveAt(i);
                continue;
            }

            OUTL_SquadOrderType type = OUTL_SquadOrderType.Hold;
            Vector3 pos = transform.position + transform.right * ((i - Members.Count * 0.5f) * MemberSpacing);
            string key = "hold";

            if (hasTarget)
            {
                if (i == 0) { type = OUTL_SquadOrderType.TakeCover; key = "cover"; pos = orderPos; }
                else if (i % 2 == 0) { type = OUTL_SquadOrderType.FlankLeft; key = "flank_left"; pos = orderPos - transform.right * MemberSpacing * 2f; }
                else { type = OUTL_SquadOrderType.FlankRight; key = "flank_right"; pos = orderPos + transform.right * MemberSpacing * 2f; }
            }

            member.ReceiveOrder(new OUTL_SquadOrder(type, target, pos, hasTarget ? 2f : 0.5f, OrderLifetime, key));
            if (Blackboard != null) Blackboard.PublishOrder(member, member.CurrentOrder);
        }
    }
}
