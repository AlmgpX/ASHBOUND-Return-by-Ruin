using System;
using UnityEngine;

[Serializable]
public class OUTL_SurfaceMapEntry
{
    public int RvpSurfaceType = -1;
    public string SurfaceName;
    public OUTL_SurfaceProfile Profile;
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Surfaces/Surface Library", fileName = "OUTL_SurfaceLibrary")]
public class OUTL_SurfaceLibrary : ScriptableObject
{
    public OUTL_SurfaceProfile DefaultProfile;
    public OUTL_SurfaceMapEntry[] Entries;

    public OUTL_SurfaceProfile GetByRvpSurfaceType(int surfaceType)
    {
        if (Entries != null)
        {
            for (int i = 0; i < Entries.Length; i++)
            {
                OUTL_SurfaceMapEntry e = Entries[i];
                if (e != null && e.Profile != null && e.RvpSurfaceType == surfaceType)
                    return e.Profile;
            }
        }
        return DefaultProfile;
    }

    public OUTL_SurfaceProfile GetByName(string surfaceName)
    {
        if (!string.IsNullOrEmpty(surfaceName) && Entries != null)
        {
            for (int i = 0; i < Entries.Length; i++)
            {
                OUTL_SurfaceMapEntry e = Entries[i];
                if (e != null && e.Profile != null && e.SurfaceName == surfaceName)
                    return e.Profile;
            }
        }
        return DefaultProfile;
    }
}
