public interface IOutUsable
{
    OUT_UseCapabilityFlags UseCaps { get; }

    bool CanUse(in OUT_UseRequest request);
    OUT_UseResult Use(in OUT_UseRequest request);
}
