using UnityEngine;

[System.Serializable]
public class OUT_AIBlackboard
{
    public GameObject Enemy;
    public Vector3 EnemyLastKnownPosition;
    public float LastEnemySeenTime;

    public Vector3 MoveTargetPoint;
    public Vector3 CoverPoint;

    public Vector3 InterestPoint;
    public float InterestStrength;

    public float WaitUntilTime;
    public int MemoryFlags;

    public void ClearEnemy()
    {
        Enemy = null;
        EnemyLastKnownPosition = Vector3.zero;
        LastEnemySeenTime = 0f;
    }

    public void ClearInterest()
    {
        InterestPoint = Vector3.zero;
        InterestStrength = 0f;
    }

    public void ResetState()
    {
        ClearEnemy();
        ClearInterest();
        MoveTargetPoint = Vector3.zero;
        CoverPoint = Vector3.zero;
        WaitUntilTime = 0f;
        MemoryFlags = 0;
    }
}
