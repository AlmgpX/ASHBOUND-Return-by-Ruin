using UnityEngine;

[DisallowMultipleComponent]
public class OUTL_PatrolRoute : MonoBehaviour
{
    public Transform[] Points;
    public bool Loop = true;
    public bool PingPong;
    public float PointReachDistance = 1.2f;

    public int Count { get { return Points != null ? Points.Length : 0; } }

    public Transform GetPoint(int index)
    {
        if (Points == null || Points.Length == 0) return null;
        index = Mathf.Clamp(index, 0, Points.Length - 1);
        return Points[index];
    }

    public int NextIndex(int current, ref int direction)
    {
        if (Points == null || Points.Length == 0) return 0;
        if (Points.Length == 1) return 0;
        if (direction == 0) direction = 1;

        int next = current + direction;
        if (PingPong)
        {
            if (next >= Points.Length)
            {
                direction = -1;
                next = Points.Length - 2;
            }
            else if (next < 0)
            {
                direction = 1;
                next = 1;
            }
            return Mathf.Clamp(next, 0, Points.Length - 1);
        }

        if (next >= Points.Length) return Loop ? 0 : Points.Length - 1;
        if (next < 0) return Loop ? Points.Length - 1 : 0;
        return next;
    }
}
