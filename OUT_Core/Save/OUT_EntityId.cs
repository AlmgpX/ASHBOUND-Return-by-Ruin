using System;
using UnityEngine;

[Serializable]
public struct OUT_EntityId
{
    [SerializeField] private string value;

    public string Value => value;
    public bool IsValid => !string.IsNullOrWhiteSpace(value);

    public OUT_EntityId(string value)
    {
        this.value = value;
    }

    public override string ToString()
    {
        return value;
    }

    public static OUT_EntityId NewId()
    {
        return new OUT_EntityId(Guid.NewGuid().ToString("N"));
    }
}