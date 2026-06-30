using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-7800)]
[DisallowMultipleComponent]
public class OUT_SectorRuntime : MonoBehaviour
{
    [Header("Center")]
    [SerializeField] private Transform runtimeCenter;
    [SerializeField] private bool useMainCameraIfMissing = true;

    [Header("Distances")]
    [SerializeField] private float nearDistance = 60f;
    [SerializeField] private float midDistance = 180f;
    [SerializeField] private float farDistance = 600f;
    [SerializeField] private float hysteresis = 15f;

    [Header("Tick")]
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private int maxEntitiesPerTick = 256;

    [Header("Debug")]
    [SerializeField] private bool debugStats = false;

    private readonly List<OUT_EntityAdapter> buffer = new List<OUT_EntityAdapter>(1024);
    private int cursor;
    private float nextUpdate;

    private void Update()
    {
        if (UnityEngine.Time.time < nextUpdate)
            return;

        nextUpdate = UnityEngine.Time.time + Mathf.Max(0.02f, updateInterval);
        TickSectors();
    }

    private void TickSectors()
    {
        OUT_EntityRegistry registry = OUT_EntityRegistry.EnsureExists();
        registry.CopyEntities(buffer, includeInactive: false);

        if (buffer.Count == 0)
            return;

        Vector3 center = GetCenterPosition();
        int limit = Mathf.Min(maxEntitiesPerTick <= 0 ? buffer.Count : maxEntitiesPerTick, buffer.Count);

        for (int i = 0; i < limit; i++)
        {
            if (cursor >= buffer.Count)
                cursor = 0;

            OUT_EntityAdapter entity = buffer[cursor++];
            if (entity == null)
                continue;

            float sqr = (entity.transform.position - center).sqrMagnitude;
            OUT_RuntimeTier tier = ResolveTier(entity.RuntimeTier, sqr);
            entity.SetRuntimeTier(tier);
        }
    }

    private OUT_RuntimeTier ResolveTier(OUT_RuntimeTier current, float sqrDistance)
    {
        float near = nearDistance;
        float mid = Mathf.Max(near, midDistance);
        float far = Mathf.Max(mid, farDistance);
        float h = Mathf.Max(0f, hysteresis);

        if (current == OUT_RuntimeTier.Near || current == OUT_RuntimeTier.Full)
        {
            near += h;
            mid += h;
            far += h;
        }
        else if (current == OUT_RuntimeTier.Dormant || current == OUT_RuntimeTier.Far)
        {
            near -= h;
            mid -= h;
            far -= h;
        }

        if (sqrDistance <= near * near) return OUT_RuntimeTier.Near;
        if (sqrDistance <= mid * mid) return OUT_RuntimeTier.Mid;
        if (sqrDistance <= far * far) return OUT_RuntimeTier.Far;
        return OUT_RuntimeTier.Dormant;
    }

    private Vector3 GetCenterPosition()
    {
        if (runtimeCenter != null)
            return runtimeCenter.position;

        if (useMainCameraIfMissing && Camera.main != null)
            return Camera.main.transform.position;

        return Vector3.zero;
    }

    private void OnGUI()
    {
        if (!debugStats)
            return;

        GUI.Label(new Rect(12, 36, 700, 22), "OUT SectorRuntime entities=" + buffer.Count + " cursor=" + cursor);
    }
}
