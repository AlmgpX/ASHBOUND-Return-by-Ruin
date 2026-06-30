using UnityEngine;

[DisallowMultipleComponent]
public class OUT_AIMemoryProfile : MonoBehaviour
{
    [Header("Capacity")]
    [SerializeField] [Min(0)] private int maxMemories = 8;

    [Header("Durations")]
    [SerializeField] [Min(0.1f)] private float enemyMemorySeconds = 5f;
    [SerializeField] [Min(0.1f)] private float interestMemorySeconds = 3f;
    [SerializeField] [Min(0.1f)] private float dangerMemorySeconds = 6f;

    [Header("Strength")]
    [SerializeField] [Range(0f, 1f)] private float minimumRecallConfidence = 0.15f;
    [SerializeField] [Range(0f, 1f)] private float newEnemyConfidence = 1f;
    [SerializeField] [Range(0f, 1f)] private float newInterestConfidence = 0.65f;
    [SerializeField] [Range(0f, 1f)] private float newDangerConfidence = 0.85f;

    [Header("Behaviour Influence")]
    [SerializeField] private bool keepEnemyLastKnownPositionFromMemory = true;
    [SerializeField] private bool clearWeakInterest = true;
    [SerializeField] [Range(0f, 1f)] private float alertConfidenceThreshold = 0.2f;

    public int MaxMemories => maxMemories;
    public float EnemyMemorySeconds => enemyMemorySeconds;
    public float InterestMemorySeconds => interestMemorySeconds;
    public float DangerMemorySeconds => dangerMemorySeconds;
    public float MinimumRecallConfidence => minimumRecallConfidence;
    public float NewEnemyConfidence => newEnemyConfidence;
    public float NewInterestConfidence => newInterestConfidence;
    public float NewDangerConfidence => newDangerConfidence;
    public bool KeepEnemyLastKnownPositionFromMemory => keepEnemyLastKnownPositionFromMemory;
    public bool ClearWeakInterest => clearWeakInterest;
    public float AlertConfidenceThreshold => alertConfidenceThreshold;
}
