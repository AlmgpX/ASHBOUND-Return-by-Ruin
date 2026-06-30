using OUTPool = OutCore.pool.OUT;
using UnityEngine;

/// <summary>
/// Compatibility facade for older OUT Core callers.
/// Object storage and lifetime are owned by OUT CORE Lite's OUTL_PoolSystem.
/// </summary>
public class OUT_RuntimePoolService : MonoBehaviour
{
    public static OUT_RuntimePoolService Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
            return null;

        GameObject instance = OUTPool.Instantiate(prefab, position, rotation);
        return instance;
    }

    public void Return(GameObject instance)
    {
        if (instance == null)
            return;

        OUTPool.Destroy(instance);
    }

    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0)
            return;

        OUTPool.Prewarm(prefab, count);
    }
}
