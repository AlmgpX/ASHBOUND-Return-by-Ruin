using System;

[Serializable]
public struct OUT_ChunkId : IEquatable<OUT_ChunkId>
{
    public int X;
    public int Y;
    public int Layer;

    public OUT_ChunkId(int x, int y, int layer = 0)
    {
        X = x;
        Y = y;
        Layer = layer;
    }

    public bool Equals(OUT_ChunkId other) => X == other.X && Y == other.Y && Layer == other.Layer;
    public override bool Equals(object obj) => obj is OUT_ChunkId other && Equals(other);
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = X;
            hash = (hash * 397) ^ Y;
            hash = (hash * 397) ^ Layer;
            return hash;
        }
    }

    public override string ToString() => X + ":" + Y + ":" + Layer;

    public static bool operator ==(OUT_ChunkId a, OUT_ChunkId b) => a.Equals(b);
    public static bool operator !=(OUT_ChunkId a, OUT_ChunkId b) => !a.Equals(b);

    public static readonly OUT_ChunkId Zero = new OUT_ChunkId(0, 0, 0);
}
