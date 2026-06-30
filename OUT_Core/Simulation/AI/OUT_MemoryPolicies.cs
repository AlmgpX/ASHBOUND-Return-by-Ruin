public static class OUT_MemoryPolicies
{
    public static void OnNewEnemy(ref OUT_MemoryState memory)
    {
        memory.Remember(OUT_MemoryFlags.Provoked);
        memory.Forget(OUT_MemoryFlags.MoveFailed);
    }

    public static void OnEnterCover(ref OUT_MemoryState memory)
    {
        memory.Remember(OUT_MemoryFlags.InCover);
    }

    public static void OnLeaveCover(ref OUT_MemoryState memory)
    {
        memory.Forget(OUT_MemoryFlags.InCover);
    }

    public static void OnMoveFailed(ref OUT_MemoryState memory)
    {
        memory.Remember(OUT_MemoryFlags.MoveFailed);
    }

    public static void OnPathFinished(ref OUT_MemoryState memory)
    {
        memory.Remember(OUT_MemoryFlags.PathFinished);
        memory.Forget(OUT_MemoryFlags.OnPath);
    }

    public static void OnPathStarted(ref OUT_MemoryState memory)
    {
        memory.Remember(OUT_MemoryFlags.OnPath);
        memory.Forget(OUT_MemoryFlags.PathFinished);
    }

    public static void OnDamageTaken(ref OUT_MemoryState memory)
    {
        memory.Remember(OUT_MemoryFlags.Provoked);
        memory.Forget(OUT_MemoryFlags.InCover);
    }

    public static void OnDeath(ref OUT_MemoryState memory)
    {
        memory.Remember(OUT_MemoryFlags.Killed);
    }

    public static void OnSuspicious(ref OUT_MemoryState memory, bool value)
    {
        memory.Assign(value, OUT_MemoryFlags.Suspicious);
    }
}