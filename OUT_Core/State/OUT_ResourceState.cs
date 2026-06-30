using UnityEngine;

[System.Serializable]
public struct OUT_ResourceState
{
    public int Current;
    public int Max;
    public bool IsDepleted;

    public float Normalized => Max <= 0 ? 0f : Mathf.Clamp01((float)Current / Max);
}
