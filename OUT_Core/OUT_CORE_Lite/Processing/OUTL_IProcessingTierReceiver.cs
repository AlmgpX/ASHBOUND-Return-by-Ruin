public interface OUTL_IProcessingTierReceiver
{
    void OUTL_OnProcessingTierChanged(OUTL_RuntimeTier oldTier, OUTL_RuntimeTier newTier, in OUTL_TierProcessingSettings settings);
}
