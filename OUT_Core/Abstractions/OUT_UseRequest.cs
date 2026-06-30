using UnityEngine;

public readonly struct OUT_UseRequest
{
    public readonly GameObject User;
    public readonly Vector3 Origin;
    public readonly Vector3 Direction;
    public readonly float HoldTime;

    public OUT_UseRequest(GameObject user, Vector3 origin, Vector3 direction, float holdTime = 0f)
    {
        User = user;
        Origin = origin;
        Direction = direction;
        HoldTime = holdTime;
    }
}
