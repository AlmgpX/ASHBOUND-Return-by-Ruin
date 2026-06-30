using UnityEngine;

public sealed class OUTL_EgregoreInfluenceZone : MonoBehaviour
{
    public int[] SectorIds;
    public Vector3 BoundsSize = new Vector3(64f, 32f, 64f);
    public float Radius = 64f;
    public float Weight = 1f;
    public bool UseBounds;
    public bool DrawGizmos = true;

    public bool ContainsSector(int sectorId)
    {
        if (SectorIds == null || SectorIds.Length == 0) return false;
        for (int i = 0; i < SectorIds.Length; i++)
            if (SectorIds[i] == sectorId)
                return true;
        return false;
    }

    public bool ContainsPosition(Vector3 position)
    {
        if (UseBounds)
        {
            Bounds bounds = new Bounds(transform.position, new Vector3(Mathf.Max(0.1f, BoundsSize.x), Mathf.Max(0.1f, BoundsSize.y), Mathf.Max(0.1f, BoundsSize.z)));
            return bounds.Contains(position);
        }

        float radius = Mathf.Max(0.01f, Radius);
        return (position - transform.position).sqrMagnitude <= radius * radius;
    }

    private void OnDrawGizmosSelected()
    {
        if (!DrawGizmos) return;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.18f);
        if (UseBounds) Gizmos.DrawCube(transform.position, BoundsSize);
        else Gizmos.DrawSphere(transform.position, Mathf.Max(0.01f, Radius));
    }
}
