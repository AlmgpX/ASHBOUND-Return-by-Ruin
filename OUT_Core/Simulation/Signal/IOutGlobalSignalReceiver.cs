public interface IOutGlobalSignalReceiver : IOutSignalReceiver
{
    bool ReceivesSignalsWithoutBusRadiusFilter { get; }
}
