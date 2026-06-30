using System;
using UnityEngine;

[DisallowMultipleComponent]
public class OUT_FactionAgent : MonoBehaviour
{
    public enum Relation
    {
        Self = 0,
        Friendly = 1,
        Neutral = 2,
        Hostile = 3
    }

    [Header("Identity")]
    [SerializeField] private string factionId = "neutral";
    [SerializeField] private string displayName;

    [Header("Relations")]
    [SerializeField] private string[] friendlyFactionIds;
    [SerializeField] private string[] hostileFactionIds;
    [SerializeField] private bool treatUnlistedAsNeutral = true;
    [SerializeField] private bool reciprocalHostility = true;

    public string FactionId => string.IsNullOrWhiteSpace(factionId) ? "neutral" : factionId.Trim();
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;

    public Relation GetRelationTo(GameObject target)
    {
        if (target == null)
            return Relation.Neutral;

        OUT_FactionAgent other = target.GetComponentInParent<OUT_FactionAgent>();
        if (other == null)
            other = target.GetComponentInChildren<OUT_FactionAgent>();

        return GetRelationTo(other);
    }

    public Relation GetRelationTo(Transform target)
    {
        if (target == null)
            return Relation.Neutral;

        OUT_FactionAgent other = target.GetComponentInParent<OUT_FactionAgent>();
        if (other == null)
            other = target.GetComponentInChildren<OUT_FactionAgent>();

        return GetRelationTo(other);
    }

    public Relation GetRelationTo(OUT_FactionAgent other)
    {
        if (other == null)
            return treatUnlistedAsNeutral ? Relation.Neutral : Relation.Hostile;

        string self = Normalize(FactionId);
        string target = Normalize(other.FactionId);

        if (self == target)
            return Relation.Self;

        if (ContainsId(hostileFactionIds, target))
            return Relation.Hostile;

        if (ContainsId(friendlyFactionIds, target))
            return Relation.Friendly;

        if (reciprocalHostility && ContainsId(other.hostileFactionIds, self))
            return Relation.Hostile;

        return treatUnlistedAsNeutral ? Relation.Neutral : Relation.Hostile;
    }

    public bool IsHostileTo(GameObject target)
    {
        return GetRelationTo(target) == Relation.Hostile;
    }

    public bool IsFriendlyTo(GameObject target)
    {
        Relation relation = GetRelationTo(target);
        return relation == Relation.Self || relation == Relation.Friendly;
    }

    public static OUT_FactionAgent FindOn(GameObject go)
    {
        if (go == null)
            return null;

        OUT_FactionAgent agent = go.GetComponentInParent<OUT_FactionAgent>();
        if (agent != null)
            return agent;

        return go.GetComponentInChildren<OUT_FactionAgent>();
    }

    public static OUT_FactionAgent FindOn(Transform t)
    {
        if (t == null)
            return null;

        OUT_FactionAgent agent = t.GetComponentInParent<OUT_FactionAgent>();
        if (agent != null)
            return agent;

        return t.GetComponentInChildren<OUT_FactionAgent>();
    }

    private static bool ContainsId(string[] values, string id)
    {
        if (values == null || values.Length == 0 || string.IsNullOrWhiteSpace(id))
            return false;

        for (int i = 0; i < values.Length; i++)
        {
            if (Normalize(values[i]) == id)
                return true;
        }

        return false;
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(factionId))
            factionId = "neutral";
    }
#endif
}
