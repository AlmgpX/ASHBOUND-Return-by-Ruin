using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-7700)]
[DisallowMultipleComponent]
public class OUT_ZombieTargetHub : MonoBehaviour
{
    public static OUT_ZombieTargetHub Instance { get; private set; }

    private readonly List<OUT_ZombieTarget> targets = new List<OUT_ZombieTarget>(32);

    public int Count { get { return targets.Count; } }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static OUT_ZombieTargetHub EnsureExists()
    {
        if (Instance != null) return Instance;
        OUT_ZombieTargetHub existing = FindObjectOfType<OUT_ZombieTargetHub>();
        if (existing != null) return existing;
        GameObject go = new GameObject("OUT_ZombieTargetHub");
        return go.AddComponent<OUT_ZombieTargetHub>();
    }

    public static void Register(OUT_ZombieTarget target)
    {
        if (target == null) return;
        OUT_ZombieTargetHub hub = EnsureExists();
        if (!hub.targets.Contains(target)) hub.targets.Add(target);
    }

    public static void Unregister(OUT_ZombieTarget target)
    {
        if (Instance == null || target == null) return;
        Instance.targets.Remove(target);
    }

    public OUT_ZombieTarget FindBestTarget(Vector3 from, OUT_ZombieHordeProfile profile, OUT_ZombieTarget previous)
    {
        OUT_ZombieTarget best = null;
        float bestScore = float.MaxValue;
        float previousBias = profile != null ? profile.RetargetDistanceBias : 0f;
        float objectiveBias = profile != null ? profile.ObjectiveBias : 0f;

        for (int i = targets.Count - 1; i >= 0; i--)
        {
            OUT_ZombieTarget t = targets[i];
            if (t == null)
            {
                targets.RemoveAt(i);
                continue;
            }

            if (!t.CanBeTargeted) continue;

            float sqr = (t.Position - from).sqrMagnitude;
            float priority = Mathf.Max(0.01f, t.Priority);
            float score = sqr / priority;

            if (t == previous) score -= previousBias * previousBias;
            if (t.IsPrimaryObjective) score -= objectiveBias * objectiveBias;

            if (score < bestScore)
            {
                bestScore = score;
                best = t;
            }
        }

        return best;
    }
}
