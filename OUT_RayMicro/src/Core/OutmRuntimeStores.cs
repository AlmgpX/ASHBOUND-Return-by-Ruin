using System.Numerics;

namespace OUT_RayMicro.Core;

public sealed class OutmTransformStore
{
    private Vector3[] positions;
    private Vector3[] rotations;
    private bool[] hasTransform;

    public OutmTransformStore(int initialCapacity = 512)
    {
        int capacity = Math.Max(16, initialCapacity);
        positions = new Vector3[capacity];
        rotations = new Vector3[capacity];
        hasTransform = new bool[capacity];
    }

    public void Set(EntityId entity, Vector3 position, Vector3 rotationEuler)
    {
        if (!entity.IsValid)
            return;

        EnsureCapacity(entity.Index + 1);
        positions[entity.Index] = position;
        rotations[entity.Index] = rotationEuler;
        hasTransform[entity.Index] = true;
    }

    public bool TryGet(EntityId entity, out Vector3 position, out Vector3 rotationEuler)
    {
        if (entity.Index < 0 || entity.Index >= hasTransform.Length || !hasTransform[entity.Index])
        {
            position = Vector3.Zero;
            rotationEuler = Vector3.Zero;
            return false;
        }

        position = positions[entity.Index];
        rotationEuler = rotations[entity.Index];
        return true;
    }

    public void Remove(EntityId entity)
    {
        if (entity.Index < 0 || entity.Index >= hasTransform.Length)
            return;

        hasTransform[entity.Index] = false;
        positions[entity.Index] = Vector3.Zero;
        rotations[entity.Index] = Vector3.Zero;
    }

    private void EnsureCapacity(int required)
    {
        if (required <= positions.Length)
            return;

        int next = positions.Length;
        while (next < required)
            next *= 2;

        Array.Resize(ref positions, next);
        Array.Resize(ref rotations, next);
        Array.Resize(ref hasTransform, next);
    }
}
