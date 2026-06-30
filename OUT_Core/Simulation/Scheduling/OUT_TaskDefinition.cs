using System;
using UnityEngine;

[Serializable]
public sealed class OUT_TaskDefinition
{
    [SerializeField] private string id;
    [SerializeField] private float data;

    public string Id => id;
    public float Data => data;

    public OUT_TaskDefinition(string id, float data = 0f)
    {
        this.id = id;
        this.data = data;
    }

    public OUT_TaskHandle ToHandle()
    {
        return new OUT_TaskHandle(id);
    }
}