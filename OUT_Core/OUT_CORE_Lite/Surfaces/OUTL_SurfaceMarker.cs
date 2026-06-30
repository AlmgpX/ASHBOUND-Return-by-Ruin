using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_SurfaceMarker : MonoBehaviour
{
    public OUTL_SurfaceProfile Profile;
    public string SurfaceName;
    public string DefaultSurfaceName = "default";
    public bool OverrideFriction;
    public float Friction = 1f;

    public string ResolvedName
    {
        get
        {
            if (Profile != null && !string.IsNullOrEmpty(Profile.SurfaceId)) return Profile.SurfaceId;
            if (!string.IsNullOrEmpty(SurfaceName)) return SurfaceName;
            return !string.IsNullOrEmpty(DefaultSurfaceName) ? DefaultSurfaceName : "default";
        }
    }
}
