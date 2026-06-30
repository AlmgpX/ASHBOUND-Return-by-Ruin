using UnityEngine;

[System.Serializable]
public struct OUT_AITask
{
    public OUT_AITaskType Type;
    public float FloatValue;
    public int IntValue;
    public Vector3 VectorValue;
    public string StringValue;

    public OUT_AITask(OUT_AITaskType type)
    {
        Type = type;
        FloatValue = 0f;
        IntValue = 0;
        VectorValue = Vector3.zero;
        StringValue = string.Empty;
    }

    public OUT_AITask(OUT_AITaskType type, float floatValue, int intValue = 0)
    {
        Type = type;
        FloatValue = floatValue;
        IntValue = intValue;
        VectorValue = Vector3.zero;
        StringValue = string.Empty;
    }

    public OUT_AITask(OUT_AITaskType type, Vector3 vectorValue)
    {
        Type = type;
        FloatValue = 0f;
        IntValue = 0;
        VectorValue = vectorValue;
        StringValue = string.Empty;
    }
}
