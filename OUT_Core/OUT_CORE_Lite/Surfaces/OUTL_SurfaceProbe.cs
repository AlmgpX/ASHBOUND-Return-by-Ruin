using UnityEngine;

[DisallowMultipleComponent]
public class OUTL_SurfaceProbe : MonoBehaviour
{
    public OUTL_SurfaceLibrary Library;
    public LayerMask GroundMask = ~0;
    public float ProbeDistance = 1.25f;
    public float ProbeRadius = 0.22f;
    public QueryTriggerInteraction TriggerInteraction = QueryTriggerInteraction.Ignore;
    public OUTL_SurfaceProfile CurrentProfile;
    public float CurrentFriction = 1f;
    public int CurrentRvpSurfaceType = -1;
    public string CurrentSurfaceName = "";
    public string DefaultSurfaceName = "default";

    private RaycastHit lastHit;
    public RaycastHit LastHit { get { return lastHit; } }

    public bool Probe(Vector3 origin)
    {
        CurrentProfile = Library != null ? Library.DefaultProfile : null;
        CurrentFriction = 1f;
        CurrentRvpSurfaceType = -1;
        CurrentSurfaceName = string.Empty;

        if (!Physics.SphereCast(origin, ProbeRadius, Vector3.down, out lastHit, ProbeDistance, GroundMask, TriggerInteraction))
            return false;

        ResolveFromHit(lastHit);
        return true;
    }

    public bool ProbeBelow()
    {
        return Probe(transform.position + Vector3.up * 0.2f);
    }

    private void ResolveFromHit(RaycastHit hit)
    {
        Collider col = hit.collider;
        if (col == null) return;

        RVP.GroundSurfaceInstance instance = col.GetComponent<RVP.GroundSurfaceInstance>();
        if (instance != null)
        {
            CurrentRvpSurfaceType = instance.surfaceType;
            CurrentFriction = instance.friction > 0f ? instance.friction : CurrentFriction;
            CurrentProfile = Library != null ? Library.GetByRvpSurfaceType(CurrentRvpSurfaceType) : CurrentProfile;
            CurrentSurfaceName = CurrentProfile != null ? CurrentProfile.SurfaceId : "rvp." + CurrentRvpSurfaceType;
            return;
        }

        RVP.TerrainSurface terrain = col.GetComponent<RVP.TerrainSurface>();
        if (terrain != null)
        {
            CurrentRvpSurfaceType = terrain.GetDominantSurfaceTypeAtPoint(hit.point);
            CurrentFriction = terrain.GetFriction(CurrentRvpSurfaceType);
            CurrentProfile = Library != null ? Library.GetByRvpSurfaceType(CurrentRvpSurfaceType) : CurrentProfile;
            CurrentSurfaceName = CurrentProfile != null ? CurrentProfile.SurfaceId : "terrain." + CurrentRvpSurfaceType;
            return;
        }

        OUTL_SurfaceMarker marker = col.GetComponent<OUTL_SurfaceMarker>();
        if (marker == null) marker = col.GetComponentInParent<OUTL_SurfaceMarker>();
        if (marker != null)
        {
            CurrentProfile = marker.Profile != null ? marker.Profile : (Library != null ? Library.GetByName(marker.SurfaceName) : CurrentProfile);
            if (marker.OverrideFriction) CurrentFriction = Mathf.Max(0.01f, marker.Friction);
            CurrentSurfaceName = CurrentProfile != null ? CurrentProfile.SurfaceId : marker.ResolvedName;
            return;
        }

        string materialName = string.Empty;
        if (col.sharedMaterial != null)
        {
            CurrentFriction = Mathf.Max(0.01f, col.sharedMaterial.dynamicFriction * 2f);
            materialName = col.sharedMaterial.name;
        }

        string surfaceName = !string.IsNullOrEmpty(materialName) ? materialName : DefaultSurfaceName;
        CurrentProfile = Library != null ? Library.GetByName(surfaceName) : CurrentProfile;
        CurrentSurfaceName = CurrentProfile != null ? CurrentProfile.SurfaceId : (!string.IsNullOrEmpty(surfaceName) ? surfaceName : "default");
    }
}
