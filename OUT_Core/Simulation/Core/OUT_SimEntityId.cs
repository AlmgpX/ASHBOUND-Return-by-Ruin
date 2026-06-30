using System;

[Serializable]
public struct OUT_SimEntityId : IEquatable<OUT_SimEntityId>
{
    public int Value;

    public OUT_SimEntityId(int value)
    {
        Value = value;
    }

    public bool IsValid => Value > 0;

    public bool Equals(OUT_SimEntityId other) => Value == other.Value;
    public override bool Equals(object obj) => obj is OUT_SimEntityId other && Equals(other);
    public override int GetHashCode() => Value;
    public override string ToString() => IsValid ? Value.ToString() : "none";

    public static bool operator ==(OUT_SimEntityId a, OUT_SimEntityId b) => a.Value == b.Value;
    public static bool operator !=(OUT_SimEntityId a, OUT_SimEntityId b) => a.Value != b.Value;

    public static readonly OUT_SimEntityId None = new OUT_SimEntityId(0);
}
