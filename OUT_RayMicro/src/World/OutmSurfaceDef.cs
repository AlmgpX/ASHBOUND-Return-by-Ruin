namespace OUT_RayMicro.World;

public readonly struct OutmSurfaceId : IEquatable<OutmSurfaceId>
{
    public readonly string Value;

    public OutmSurfaceId(string value)
    {
        Value = string.IsNullOrWhiteSpace(value) ? "surface.stone" : value;
    }

    public bool Equals(OutmSurfaceId other) => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    public override bool Equals(object? obj) => obj is OutmSurfaceId other && Equals(other);
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
    public override string ToString() => Value;

    public static readonly OutmSurfaceId Stone = new("surface.stone");
    public static readonly OutmSurfaceId Wood = new("surface.wood");
    public static readonly OutmSurfaceId Metal = new("surface.metal");
    public static readonly OutmSurfaceId Dirt = new("surface.dirt");
}

public sealed class OutmSurfaceDef
{
    public string Id { get; set; } = "surface.stone";
    public string DisplayName { get; set; } = "Stone";
    public string FootstepTag { get; set; } = "footstep stone";
    public string ImpactTag { get; set; } = "impact stone";
    public float FrictionMultiplier { get; set; } = 1.0f;
    public int DamagePerSecond { get; set; }
}

public sealed class OutmSurfaceRegistry
{
    private readonly Dictionary<string, OutmSurfaceDef> surfaces = new(StringComparer.OrdinalIgnoreCase);

    public OutmSurfaceRegistry()
    {
        Register(new OutmSurfaceDef { Id = "surface.stone", DisplayName = "Stone", FootstepTag = "footstep stone", ImpactTag = "impact stone", FrictionMultiplier = 1.0f });
        Register(new OutmSurfaceDef { Id = "surface.wood", DisplayName = "Wood", FootstepTag = "footstep wood", ImpactTag = "impact wood", FrictionMultiplier = 1.0f });
        Register(new OutmSurfaceDef { Id = "surface.metal", DisplayName = "Metal", FootstepTag = "footstep metal", ImpactTag = "impact metal", FrictionMultiplier = 0.92f });
        Register(new OutmSurfaceDef { Id = "surface.dirt", DisplayName = "Dirt", FootstepTag = "footstep dirt", ImpactTag = "impact dirt", FrictionMultiplier = 0.86f });
    }

    public void Register(OutmSurfaceDef def)
    {
        if (string.IsNullOrWhiteSpace(def.Id))
            return;

        surfaces[def.Id] = def;
    }

    public OutmSurfaceDef Get(string id)
    {
        if (!string.IsNullOrWhiteSpace(id) && surfaces.TryGetValue(id, out OutmSurfaceDef? def))
            return def;

        return surfaces[OutmSurfaceId.Stone.Value];
    }
}
