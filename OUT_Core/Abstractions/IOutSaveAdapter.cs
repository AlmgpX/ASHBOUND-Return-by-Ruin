public interface IOutSaveAdapter
{
    string SaveKey { get; }
    object CaptureSaveState();
    void RestoreSaveState(object state);
}
