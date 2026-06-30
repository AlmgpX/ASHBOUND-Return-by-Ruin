using System;
using UnityEngine;

[DisallowMultipleComponent]
public class OUT_AIMemoryBuffer : MonoBehaviour
{
    public enum MemoryKind
    {
        Enemy = 0,
        Interest = 1,
        Danger = 2,
        World = 3
    }

    [Serializable]
    public struct MemoryRecord
    {
        public MemoryKind Kind;
        public Vector3 Position;
        public GameObject Subject;
        public float CreatedTime;
        public float LastRefreshTime;
        public float Duration;
        public float Confidence;
        public int Flags;

        public bool IsAlive(float now, float minConfidence)
        {
            if (Duration <= 0f)
                return false;

            if (now - LastRefreshTime > Duration)
                return false;

            return GetConfidence(now) >= minConfidence;
        }

        public float GetConfidence(float now)
        {
            if (Duration <= 0f)
                return 0f;

            float age01 = Mathf.Clamp01((now - LastRefreshTime) / Duration);
            return Mathf.Clamp01(Confidence * (1f - age01));
        }
    }

    [Header("References")]
    [SerializeField] private OUT_AIMemoryProfile profile;

    [Header("Runtime")]
    [SerializeField] private MemoryRecord[] memories;
    [SerializeField] private int memoryCount;

    public int MemoryCount => memoryCount;
    public int Capacity => memories != null ? memories.Length : 0;

    private void Awake()
    {
        if (profile == null)
            profile = GetComponent<OUT_AIMemoryProfile>();

        EnsureCapacity();
    }

    private void OnValidate()
    {
        if (profile == null)
            profile = GetComponent<OUT_AIMemoryProfile>();
    }

    public void ObserveEnemy(GameObject enemy, Vector3 position)
    {
        if (profile == null)
            return;

        AddOrRefresh(MemoryKind.Enemy, enemy, position, profile.EnemyMemorySeconds, profile.NewEnemyConfidence, 0);
    }

    public void ObserveInterest(Vector3 position, float confidence, int flags = 0)
    {
        if (profile == null)
            return;

        float clamped = Mathf.Clamp01(Mathf.Max(confidence, profile.NewInterestConfidence));
        AddOrRefresh(MemoryKind.Interest, null, position, profile.InterestMemorySeconds, clamped, flags);
    }

    public void ObserveDanger(Vector3 position, float confidence, int flags = 0)
    {
        if (profile == null)
            return;

        float clamped = Mathf.Clamp01(Mathf.Max(confidence, profile.NewDangerConfidence));
        AddOrRefresh(MemoryKind.Danger, null, position, profile.DangerMemorySeconds, clamped, flags);
    }

    public bool TryRecall(MemoryKind kind, out MemoryRecord record)
    {
        record = default;
        if (profile == null || memories == null)
            return false;

        float now = Time.time;
        float best = profile.MinimumRecallConfidence;
        bool found = false;

        CleanupExpired();

        for (int i = 0; i < memoryCount; i++)
        {
            MemoryRecord candidate = memories[i];
            if (candidate.Kind != kind)
                continue;

            float confidence = candidate.GetConfidence(now);
            if (confidence < best)
                continue;

            best = confidence;
            candidate.Confidence = confidence;
            record = candidate;
            found = true;
        }

        return found;
    }

    public void ApplyToBlackboard(OUT_AIBlackboard blackboard, ref OUT_AIConditionFlags conditions)
    {
        if (blackboard == null || profile == null)
            return;

        CleanupExpired();

        if (profile.KeepEnemyLastKnownPositionFromMemory && blackboard.Enemy == null)
        {
            if (TryRecall(MemoryKind.Enemy, out MemoryRecord enemyMemory))
            {
                blackboard.EnemyLastKnownPosition = enemyMemory.Position;
                blackboard.LastEnemySeenTime = Time.time;
                conditions |= OUT_AIConditionFlags.HasEnemyLKP;
            }
        }

        if (TryRecall(MemoryKind.Danger, out MemoryRecord dangerMemory))
        {
            blackboard.InterestPoint = dangerMemory.Position;
            blackboard.InterestStrength = dangerMemory.GetConfidence(Time.time);
            if (blackboard.InterestStrength >= profile.AlertConfidenceThreshold)
                conditions |= OUT_AIConditionFlags.HearDanger;
        }
        else if (TryRecall(MemoryKind.Interest, out MemoryRecord interestMemory))
        {
            blackboard.InterestPoint = interestMemory.Position;
            blackboard.InterestStrength = interestMemory.GetConfidence(Time.time);
        }
        else if (profile.ClearWeakInterest)
        {
            blackboard.ClearInterest();
        }
    }

    public void CleanupExpired()
    {
        if (profile == null || memories == null)
            return;

        float now = Time.time;
        float min = profile.MinimumRecallConfidence;

        for (int i = memoryCount - 1; i >= 0; i--)
        {
            if (!memories[i].IsAlive(now, min))
                RemoveAtSwapBack(i);
        }
    }

    public void Clear()
    {
        if (memories != null)
            Array.Clear(memories, 0, memories.Length);

        memoryCount = 0;
    }

    private void AddOrRefresh(MemoryKind kind, GameObject subject, Vector3 position, float duration, float confidence, int flags)
    {
        EnsureCapacity();

        if (memories == null || memories.Length == 0)
            return;

        float now = Time.time;
        int existing = FindExisting(kind, subject, position);

        if (existing >= 0)
        {
            MemoryRecord record = memories[existing];
            record.Position = position;
            record.Subject = subject;
            record.LastRefreshTime = now;
            record.Duration = Mathf.Max(0.1f, duration);
            record.Confidence = Mathf.Clamp01(Mathf.Max(record.Confidence, confidence));
            record.Flags = flags;
            memories[existing] = record;
            return;
        }

        int index = GetFreeIndexOrReplaceWeakest();
        memories[index] = new MemoryRecord
        {
            Kind = kind,
            Position = position,
            Subject = subject,
            CreatedTime = now,
            LastRefreshTime = now,
            Duration = Mathf.Max(0.1f, duration),
            Confidence = Mathf.Clamp01(confidence),
            Flags = flags
        };

        if (index == memoryCount)
            memoryCount++;

        OUT_AIDebugLogService.Log(this, OUT_AIDebugLogService.AIEventKind.Memory,
            $"remember {kind} pos:{position} conf:{confidence:0.00} cap:{memoryCount}/{Capacity}");
    }

    private int FindExisting(MemoryKind kind, GameObject subject, Vector3 position)
    {
        for (int i = 0; i < memoryCount; i++)
        {
            MemoryRecord record = memories[i];
            if (record.Kind != kind)
                continue;

            if (subject != null && record.Subject == subject)
                return i;

            if (subject == null && (record.Position - position).sqrMagnitude < 1f)
                return i;
        }

        return -1;
    }

    private int GetFreeIndexOrReplaceWeakest()
    {
        if (memoryCount < memories.Length)
            return memoryCount;

        float now = Time.time;
        int weakest = 0;
        float weakestConfidence = float.MaxValue;

        for (int i = 0; i < memories.Length; i++)
        {
            float confidence = memories[i].GetConfidence(now);
            if (confidence < weakestConfidence)
            {
                weakestConfidence = confidence;
                weakest = i;
            }
        }

        return weakest;
    }

    private void RemoveAtSwapBack(int index)
    {
        if (index < 0 || index >= memoryCount)
            return;

        int last = memoryCount - 1;
        memories[index] = memories[last];
        memories[last] = default;
        memoryCount--;
    }

    private void EnsureCapacity()
    {
        int desired = profile != null ? Mathf.Max(0, profile.MaxMemories) : 0;
        if (desired <= 0)
        {
            memories = Array.Empty<MemoryRecord>();
            memoryCount = 0;
            return;
        }

        if (memories != null && memories.Length == desired)
            return;

        MemoryRecord[] next = new MemoryRecord[desired];
        if (memories != null && memoryCount > 0)
        {
            int copy = Mathf.Min(memoryCount, desired);
            Array.Copy(memories, next, copy);
            memoryCount = copy;
        }
        else
        {
            memoryCount = 0;
        }

        memories = next;
    }
}
