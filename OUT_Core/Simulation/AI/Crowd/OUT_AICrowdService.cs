using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class OUT_AICrowdService : MonoBehaviour
{
    public static OUT_AICrowdService Instance { get; private set; }

    private readonly List<OUT_AICrowdAgent> _agents = new List<OUT_AICrowdAgent>(128);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple OUT_AICrowdService instances found. Keeping the first one.");
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Register(OUT_AICrowdAgent agent)
    {
        if (agent == null || _agents.Contains(agent))
            return;

        _agents.Add(agent);
    }

    public void Unregister(OUT_AICrowdAgent agent)
    {
        if (agent == null)
            return;

        _agents.Remove(agent);
    }

    public Vector3 GetSpreadGoal(
        OUT_AICrowdAgent agent,
        Vector3 rawGoal,
        Vector3 fallbackForward)
    {
        if (agent == null || agent.GoalSpreadRadius <= 0f)
            return rawGoal;

        int seed = Mathf.Abs(agent.StableSeed);
        int ringCapacity = Mathf.Max(1, agent.SpreadRingCapacity);

        int ring = 1 + (seed / ringCapacity) % 3;
        int slot = seed % ringCapacity;

        float angle = (slot / (float)ringCapacity) * Mathf.PI * 2f;
        float radius = agent.GoalSpreadRadius * ring;

        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

        if (fallbackForward.sqrMagnitude > 0.001f)
        {
            Quaternion align = Quaternion.LookRotation(fallbackForward.normalized, Vector3.up);
            offset = align * offset;
        }

        return rawGoal + offset;
    }

    public Vector3 GetLocalSeparationOffset(OUT_AICrowdAgent agent)
    {
        if (agent == null || agent.SeparationRadius <= 0f || agent.SeparationStrength <= 0f)
            return Vector3.zero;

        Vector3 origin = agent.transform.position;
        Vector3 sum = Vector3.zero;

        for (int i = 0; i < _agents.Count; i++)
        {
            OUT_AICrowdAgent other = _agents[i];
            if (other == null || other == agent || !other.isActiveAndEnabled)
                continue;

            Vector3 delta = origin - other.transform.position;
            delta.y = 0f;

            float dist = delta.magnitude;
            if (dist > agent.SeparationRadius)
                continue;

            Vector3 awayDir;
            float weight;

            if (dist <= 0.0001f)
            {
                awayDir = GetPairFallbackDirection(agent, other);
                weight = 1f;
            }
            else
            {
                awayDir = delta / dist;
                weight = 1f - (dist / agent.SeparationRadius);
            }

            sum += awayDir * weight;
        }

        if (sum.sqrMagnitude <= 0.0001f)
            return Vector3.zero;

        return Vector3.ClampMagnitude(sum, 1f) * agent.SeparationStrength;
    }

    private Vector3 GetPairFallbackDirection(OUT_AICrowdAgent agent, OUT_AICrowdAgent other)
    {
        int selfKey = agent.StableSeed != other.StableSeed ? agent.StableSeed : agent.transform.GetInstanceID();
        int otherKey = agent.StableSeed != other.StableSeed ? other.StableSeed : other.transform.GetInstanceID();

        int minKey = Mathf.Min(selfKey, otherKey);
        int maxKey = Mathf.Max(selfKey, otherKey);
        int hash = minKey ^ (maxKey * 73856093);

        unchecked
        {
            hash = (hash ^ 61) ^ (hash >> 16);
            hash *= 9;
            hash = hash ^ (hash >> 4);
            hash *= 0x27d4eb2d;
            hash = hash ^ (hash >> 15);
        }

        float angle = ((uint)hash & 1023) / 1024f * Mathf.PI * 2f;
        Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        return selfKey >= otherKey ? direction : -direction;
    }
}