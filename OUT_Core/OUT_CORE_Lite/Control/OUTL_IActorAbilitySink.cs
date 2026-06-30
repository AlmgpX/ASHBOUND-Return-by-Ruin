public interface OUTL_IActorAbilitySink : OUTL_IActorInputPhasedSink
{
    bool OUTL_CanUseAbility(OUTL_AbilityProfile profile, in OUTL_ActorInputFrame frame, OUTL_World world);
}
