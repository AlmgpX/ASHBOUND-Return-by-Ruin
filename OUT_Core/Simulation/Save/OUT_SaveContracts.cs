using System;
using UnityEngine;

public interface IOutSaveState
{
    string SaveKey { get; }
    string CaptureStateJson();
    void RestoreStateJson(string json);
}

[Serializable]
public sealed class OUT_WorldSaveFile
{
    public int Version = 1;
    public string SceneName;
    public string CreatedUtc;
    public float UnityTime;
    public OUT_SavedObject[] Objects;
}

[Serializable]
public sealed class OUT_SavedObject
{
    public string Id;
    public bool ActiveSelf;
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
    public OUT_SavedComponent[] Components;
}

[Serializable]
public sealed class OUT_SavedComponent
{
    public string Key;
    public string Json;
}
