using UnityEngine;

public static class OUTL_NetworkAuthority
{
    public static bool IsOffline()
    {
        OUTL_NetworkSession session = OUTL_NetworkSession.ActiveSession;
        return session == null || session.IsOffline;
    }

    public static bool IsServerOrHost()
    {
        OUTL_NetworkSession session = OUTL_NetworkSession.ActiveSession;
        return session == null || session.IsHostOrServer || session.IsOffline;
    }

    public static bool IsClientReplica()
    {
        OUTL_NetworkSession session = OUTL_NetworkSession.ActiveSession;
        return session != null && session.IsClientOnly;
    }

    public static bool CanAuthoritativeSimulate(OUTL_EntityAdapter entity)
    {
        if (IsOffline()) return true;
        if (IsServerOrHost()) return true;
        if (!IsClientReplica()) return true;
        OUTL_NetworkIdentityLite identity = entity != null ? entity.GetComponent<OUTL_NetworkIdentityLite>() : null;
        return identity != null && !identity.ServerOwned;
    }

    public static bool CanApplyDamage(OUTL_EntityAdapter entity)
    {
        return CanAuthoritativeSimulate(entity);
    }

    public static bool CanKill(OUTL_EntityAdapter entity)
    {
        return CanAuthoritativeSimulate(entity);
    }

    public static bool CanSpawnDrop()
    {
        return IsOffline() || IsServerOrHost();
    }

    public static bool CanPickup()
    {
        return IsOffline() || IsServerOrHost();
    }

    public static bool CanAdvanceNpcSchedule(OUTL_EntityAdapter entity)
    {
        return CanAuthoritativeSimulate(entity);
    }

    public static void TraceBlocked(string action, OUTL_EntityAdapter entity)
    {
        string id = entity != null && entity.Id.IsValid ? entity.Id.Value.ToString() : "none";
        OUTL_DebugLog.Log(OUTL_DebugChannel.General, "authority blocked " + action + " entity=" + id + " mode=" + ReadMode(), true);
    }

    private static string ReadMode()
    {
        OUTL_NetworkSession session = OUTL_NetworkSession.ActiveSession;
        return session != null ? session.Mode.ToString() : OUTL_NetworkMode.Offline.ToString();
    }
}
