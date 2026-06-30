using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct OUTL_SquadCoverReservation
{
    public OUTL_SquadMember Member;
    public OUTL_CoverPoint Cover;
    public float UntilTime;
    public string Reason;
}

[Serializable]
public struct OUTL_FireLaneReservation
{
    public OUTL_EntityId Entity;
    public Vector3 Origin;
    public Vector3 Target;
    public float Width;
    public float UntilTime;
}

[DisallowMultipleComponent]
public sealed class OUTL_SquadBlackboard : MonoBehaviour
{
    public OUTL_SquadDef Def;
    public List<OUTL_SquadMember> Members = new List<OUTL_SquadMember>(8);
    public OUTL_EntityId SharedTarget;
    public Vector3 SharedTargetPosition;
    public float SharedTargetConfidence;
    public float SharedTargetExpireTime;
    public OUTL_SquadOrder LastOrder;

    private readonly List<OUTL_SquadCoverReservation> coverReservations = new List<OUTL_SquadCoverReservation>(16);
    private readonly List<OUTL_FireLaneReservation> fireLaneReservations = new List<OUTL_FireLaneReservation>(16);

    public bool HasSharedTarget
    {
        get
        {
            float time = OUTL_World.Instance != null ? OUTL_World.Instance.WorldTime : Time.time;
            return SharedTarget.IsValid && time <= SharedTargetExpireTime;
        }
    }

    public void Register(OUTL_SquadMember member)
    {
        if (member == null || Members.Contains(member)) return;
        Members.Add(member);
        if (member.Blackboard == null) member.Blackboard = this;
    }

    public void Unregister(OUTL_SquadMember member)
    {
        Members.Remove(member);
        ReleaseCover(member);
    }

    public void PublishTarget(OUTL_EntityId target, Vector3 position, float confidence, float lifetime)
    {
        SharedTarget = target;
        SharedTargetPosition = position;
        SharedTargetConfidence = Mathf.Clamp01(confidence);
        float time = OUTL_World.Instance != null ? OUTL_World.Instance.WorldTime : Time.time;
        float memory = Def != null && Def.SharedTargetMemory > 0f ? Def.SharedTargetMemory : lifetime;
        SharedTargetExpireTime = time + Mathf.Max(0.1f, memory);
    }

    public void PublishDanger(Vector3 position, float confidence, float lifetime)
    {
        SharedTargetPosition = position;
        SharedTargetConfidence = Mathf.Max(SharedTargetConfidence, Mathf.Clamp01(confidence));
        float time = OUTL_World.Instance != null ? OUTL_World.Instance.WorldTime : Time.time;
        SharedTargetExpireTime = Mathf.Max(SharedTargetExpireTime, time + Mathf.Max(0.1f, lifetime));
    }

    public void PublishOrder(OUTL_SquadMember member, OUTL_SquadOrder order)
    {
        LastOrder = order;
        if (member != null) member.CurrentOrder = order;
    }

    public bool TryReserveCover(OUTL_SquadMember member, OUTL_CoverPoint cover, float seconds, string reason)
    {
        if (member == null || cover == null) return false;
        float time = OUTL_World.Instance != null ? OUTL_World.Instance.WorldTime : Time.time;
        PruneCoverReservations(time);

        bool share = Def == null || Def.ShareCoverReservations;
        if (share && IsCoverReservedByOther(member, cover, time)) return false;

        for (int i = 0; i < coverReservations.Count; i++)
        {
            OUTL_SquadCoverReservation reservation = coverReservations[i];
            if (reservation.Member == member)
            {
                reservation.Cover = cover;
                reservation.UntilTime = time + Mathf.Max(0.1f, seconds);
                reservation.Reason = reason;
                coverReservations[i] = reservation;
                return true;
            }
        }

        coverReservations.Add(new OUTL_SquadCoverReservation
        {
            Member = member,
            Cover = cover,
            UntilTime = time + Mathf.Max(0.1f, seconds),
            Reason = reason
        });
        return true;
    }

    public void ReleaseCover(OUTL_SquadMember member)
    {
        for (int i = coverReservations.Count - 1; i >= 0; i--)
            if (coverReservations[i].Member == member)
                coverReservations.RemoveAt(i);
    }

    public bool TryReserveFireLane(OUTL_EntityAdapter entity, Vector3 origin, Vector3 target, float width, float duration)
    {
        if (entity == null || !entity.Id.IsValid) return false;
        float time = OUTL_World.Instance != null ? OUTL_World.Instance.WorldTime : Time.time;
        PruneFireLanes(time);
        for (int i = 0; i < fireLaneReservations.Count; i++)
        {
            OUTL_FireLaneReservation lane = fireLaneReservations[i];
            if (lane.Entity == entity.Id)
            {
                lane.Origin = origin;
                lane.Target = target;
                lane.Width = Mathf.Max(0.1f, width);
                lane.UntilTime = time + Mathf.Max(0.1f, duration);
                fireLaneReservations[i] = lane;
                return true;
            }
            if (FireLanesOverlap(lane, origin, target, width)) return false;
        }

        fireLaneReservations.Add(new OUTL_FireLaneReservation
        {
            Entity = entity.Id,
            Origin = origin,
            Target = target,
            Width = Mathf.Max(0.1f, width),
            UntilTime = time + Mathf.Max(0.1f, duration)
        });
        return true;
    }

    public void ReleaseFireLane(OUTL_EntityAdapter entity)
    {
        if (entity == null) return;
        for (int i = fireLaneReservations.Count - 1; i >= 0; i--)
            if (fireLaneReservations[i].Entity == entity.Id)
                fireLaneReservations.RemoveAt(i);
    }

    public bool IsCoverReservedByOther(OUTL_SquadMember member, OUTL_CoverPoint cover, float time)
    {
        for (int i = 0; i < coverReservations.Count; i++)
        {
            OUTL_SquadCoverReservation reservation = coverReservations[i];
            if (reservation.Cover == cover && reservation.Member != member && time <= reservation.UntilTime)
                return true;
        }
        return false;
    }

    private void PruneCoverReservations(float time)
    {
        for (int i = coverReservations.Count - 1; i >= 0; i--)
        {
            OUTL_SquadCoverReservation reservation = coverReservations[i];
            if (reservation.Member == null || reservation.Cover == null || time > reservation.UntilTime)
                coverReservations.RemoveAt(i);
        }
    }

    private void PruneFireLanes(float time)
    {
        for (int i = fireLaneReservations.Count - 1; i >= 0; i--)
            if (time > fireLaneReservations[i].UntilTime)
                fireLaneReservations.RemoveAt(i);
    }

    private static bool FireLanesOverlap(OUTL_FireLaneReservation lane, Vector3 origin, Vector3 target, float width)
    {
        Vector3 a = lane.Target - lane.Origin;
        Vector3 b = target - origin;
        a.y = 0f;
        b.y = 0f;
        if (a.sqrMagnitude <= 0.001f || b.sqrMagnitude <= 0.001f) return false;
        float angle = Vector3.Angle(a.normalized, b.normalized);
        float lateral = DistancePointToLine(origin, lane.Origin, lane.Target);
        return angle < 12f && lateral <= Mathf.Max(width, lane.Width);
    }

    private static float DistancePointToLine(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        ab.y = 0f;
        if (ab.sqrMagnitude <= 0.001f) return Vector3.Distance(point, a);
        Vector3 ap = point - a;
        ap.y = 0f;
        float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude);
        return Vector3.Distance(point, a + ab * t);
    }
}
