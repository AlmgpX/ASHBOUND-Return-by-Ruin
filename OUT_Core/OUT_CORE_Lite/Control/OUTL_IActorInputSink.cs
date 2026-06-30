public enum OUTL_ActorInputPhase : byte
{
    Movement = 0,
    Aim = 10,
    Weapon = 20,
    Interaction = 30
}

public interface OUTL_IActorInputSink
{
    void OUTL_ApplyInput(in OUTL_ActorInputFrame frame, OUTL_World world);
}

public interface OUTL_IActorInputClearableSink : OUTL_IActorInputSink
{
    void OUTL_ClearInput(OUTL_World world, float time);
}

public interface OUTL_IActorInputPhasedSink : OUTL_IActorInputSink
{
    OUTL_ActorInputPhase Phase { get; }
}
