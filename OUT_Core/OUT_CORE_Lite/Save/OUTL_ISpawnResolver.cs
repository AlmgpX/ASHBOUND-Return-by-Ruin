public interface OUTL_ISpawnResolver
{
    bool CanResolve(OUTL_EntitySaveRecord record);
    OUTL_EntityAdapter ResolveOrSpawn(OUTL_World world, OUTL_EntitySaveRecord record);
}
