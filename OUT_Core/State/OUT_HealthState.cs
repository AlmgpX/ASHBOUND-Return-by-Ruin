using UnityEngine;

[System.Serializable]
public struct OUT_HealthState
{
    public int Current;
    public int Max;
    public bool IsDead;

    public float Normalized => Max <= 0 ? 0f : Mathf.Clamp01((float)Current / Max);
}
