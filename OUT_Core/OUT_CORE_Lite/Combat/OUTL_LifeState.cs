public enum OUTL_LifeState
{
    Alive = 0,
    Dying = 1,
    Dead = 2,
    Respawning = 3,
    DormantDead = 4
}

public static class OUTL_LifecycleKeys
{
    public const string LifeState = "LifeState";
    public const string Dead = "Dead";
    public const string DeathTime = "DeathTime";
    public const string KillerId = "KillerId";
    public const string DeathKey = "DeathKey";
}
