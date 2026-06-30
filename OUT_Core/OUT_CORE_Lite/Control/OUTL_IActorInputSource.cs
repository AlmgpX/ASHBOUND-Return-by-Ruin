public interface OUTL_IActorInputSource
{
    bool TryBuildInput(OUTL_World world, OUTL_EntityAdapter entity, float time, float deltaTime, ref OUTL_ActorInputFrame frame);
}
