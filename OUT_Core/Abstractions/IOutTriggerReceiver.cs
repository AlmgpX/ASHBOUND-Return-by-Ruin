public interface IOutTriggerReceiver
{
    bool CanReceiveTrigger(in OUT_TriggerContext context);
    void ReceiveTrigger(in OUT_TriggerContext context);
}
