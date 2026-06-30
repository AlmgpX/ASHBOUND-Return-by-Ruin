using UnityEngine;

[System.Serializable]
public struct OUT_MoodState
{
    [Range(0f, 1f)] public float Stress;
    [Range(0f, 1f)] public float Fear;
    [Range(0f, 1f)] public float Aggression;
    [Range(0f, 1f)] public float Despair;
    [Range(0f, 1f)] public float Curiosity;

    public bool IsPanicking => Fear >= 0.85f || Stress >= 0.90f;
    public bool IsBroken => Despair >= 0.95f;

    public float Stability =>
        Mathf.Clamp01(
            1f
            - (Stress * 0.35f)
            - (Fear * 0.30f)
            - (Despair * 0.25f)
            + (Curiosity * 0.10f)
            - (Aggression * 0.05f));

    public void Clamp()
    {
        Stress = Mathf.Clamp01(Stress);
        Fear = Mathf.Clamp01(Fear);
        Aggression = Mathf.Clamp01(Aggression);
        Despair = Mathf.Clamp01(Despair);
        Curiosity = Mathf.Clamp01(Curiosity);
    }

    public void AddStress(float value)
    {
        Stress = Mathf.Clamp01(Stress + value);
    }

    public void AddFear(float value)
    {
        Fear = Mathf.Clamp01(Fear + value);
    }

    public void AddAggression(float value)
    {
        Aggression = Mathf.Clamp01(Aggression + value);
    }

    public void AddDespair(float value)
    {
        Despair = Mathf.Clamp01(Despair + value);
    }

    public void AddCuriosity(float value)
    {
        Curiosity = Mathf.Clamp01(Curiosity + value);
    }
}