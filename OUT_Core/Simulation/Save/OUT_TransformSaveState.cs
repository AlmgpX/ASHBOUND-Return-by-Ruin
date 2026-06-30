using System;
using UnityEngine;

[DisallowMultipleComponent]
public class OUT_TransformSaveState : MonoBehaviour, IOutSaveState
{
    [SerializeField] private bool saveLocalTransform = false;

    public string SaveKey => "transform";

    [Serializable]
    private struct State
    {
        public bool Local;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
    }

    public string CaptureStateJson()
    {
        State state = new State
        {
            Local = saveLocalTransform,
            Position = saveLocalTransform ? transform.localPosition : transform.position,
            Rotation = saveLocalTransform ? transform.localRotation : transform.rotation,
            Scale = transform.localScale
        };

        return JsonUtility.ToJson(state);
    }

    public void RestoreStateJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;

        State state = JsonUtility.FromJson<State>(json);
        if (state.Local)
        {
            transform.localPosition = state.Position;
            transform.localRotation = state.Rotation;
        }
        else
        {
            transform.position = state.Position;
            transform.rotation = state.Rotation;
        }

        transform.localScale = state.Scale;
    }
}
