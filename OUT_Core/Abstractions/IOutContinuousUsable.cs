public interface IOutContinuousUsable : IOutUsable
{
    bool CanContinueUse(in OUT_UseRequest request);
    OUT_UseResult ContinueUse(in OUT_UseRequest request);
    void EndUse(in OUT_UseRequest request);
}
