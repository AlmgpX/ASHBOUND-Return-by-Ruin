using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-7000)]
[DisallowMultipleComponent]
public class OUT_EgregoreRegistry : MonoBehaviour
{
    public static OUT_EgregoreRegistry Instance { get; private set; }

    [SerializeField] private bool dontDestroyOnLoad = true;

    private readonly List<OUT_EgregoreZone> zones = new List<OUT_EgregoreZone>(32);

    public int ZoneCount => zones.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static OUT_EgregoreRegistry EnsureExists()
    {
        if (Instance != null)
            return Instance;

        OUT_EgregoreRegistry existing = FindObjectOfType<OUT_EgregoreRegistry>();
        if (existing != null)
            return existing;

        GameObject go = new GameObject("OUT_EgregoreRegistry");
        return go.AddComponent<OUT_EgregoreRegistry>();
    }

    public static void Register(OUT_EgregoreZone zone)
    {
        EnsureExists().RegisterZone(zone);
    }

    public static void Unregister(OUT_EgregoreZone zone)
    {
        if (Instance != null)
            Instance.UnregisterZone(zone);
    }

    public void RegisterZone(OUT_EgregoreZone zone)
    {
        if (zone == null || zones.Contains(zone))
            return;

        zones.Add(zone);
    }

    public void UnregisterZone(OUT_EgregoreZone zone)
    {
        zones.Remove(zone);
    }

    public OUT_EgregoreZone FindStrongestZone(Vector3 point, out float influence)
    {
        influence = 0f;
        OUT_EgregoreZone best = null;

        for (int i = zones.Count - 1; i >= 0; i--)
        {
            OUT_EgregoreZone zone = zones[i];
            if (zone == null)
            {
                zones.RemoveAt(i);
                continue;
            }

            float candidateInfluence = zone.GetInfluenceAt(point);
            if (candidateInfluence <= influence)
                continue;

            influence = candidateInfluence;
            best = zone;
        }

        return best;
    }

    public void CollectZones(Vector3 point, List<OUT_EgregoreZone> results)
    {
        if (results == null)
            return;

        results.Clear();
        for (int i = zones.Count - 1; i >= 0; i--)
        {
            OUT_EgregoreZone zone = zones[i];
            if (zone == null)
            {
                zones.RemoveAt(i);
                continue;
            }

            if (zone.GetInfluenceAt(point) > 0f)
                results.Add(zone);
        }
    }
}
