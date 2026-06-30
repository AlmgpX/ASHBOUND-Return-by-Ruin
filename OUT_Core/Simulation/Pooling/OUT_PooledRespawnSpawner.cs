using OUTPool = OutCore.pool.OUT;
using UnityEngine;

[DisallowMultipleComponent]
public class OUT_PooledRespawnSpawner : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private bool spawnOnEnable = true;
    [SerializeField] private bool randomYaw = true;
    [SerializeField][Min(0f)] private float spawnRadiusJitter = 0f;

    [Header("Respawn")]
    [SerializeField] private bool respawnWhenInactive = true;
    [SerializeField][Min(0.1f)] private float respawnDelayMin = 2f;
    [SerializeField][Min(0.1f)] private float respawnDelayMax = 5f;
    [SerializeField][Min(0.05f)] private float checkInterval = 0.25f;

    private GameObject[] _activeInstances;
    private float[] _respawnAt;
    private bool[] _respawnScheduled;
    private float _nextCheckTime;

    private void OnEnable()
    {
        EnsureArrays();

        if (spawnOnEnable)
        {
            for (int i = 0; i < spawnPoints.Length; i++)
                SpawnAtIndex(i, immediate: true);
        }
    }

    private void Update()
    {
        if (!respawnWhenInactive || prefab == null || spawnPoints == null || spawnPoints.Length == 0)
            return;

        if (Time.time < _nextCheckTime)
            return;

        _nextCheckTime = Time.time + Mathf.Max(0.05f, checkInterval);

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform point = spawnPoints[i];
            if (point == null)
                continue;

            GameObject instance = _activeInstances[i];
            bool active = instance != null && instance.activeInHierarchy;
            if (active)
            {
                _respawnScheduled[i] = false;
                continue;
            }

            if (!_respawnScheduled[i])
            {
                _respawnScheduled[i] = true;
                _respawnAt[i] = Time.time + Random.Range(Mathf.Max(0.1f, respawnDelayMin), Mathf.Max(respawnDelayMin, respawnDelayMax));
                continue;
            }

            if (Time.time >= _respawnAt[i])
                SpawnAtIndex(i, immediate: false);
        }
    }

    public void SpawnAllNow()
    {
        EnsureArrays();
        for (int i = 0; i < spawnPoints.Length; i++)
            SpawnAtIndex(i, immediate: true);
    }

    public void SpawnAtIndexNow(int index)
    {
        EnsureArrays();
        if (index < 0 || index >= spawnPoints.Length)
            return;

        SpawnAtIndex(index, immediate: true);
    }

    private void EnsureArrays()
    {
        int count = spawnPoints != null ? spawnPoints.Length : 0;
        if (_activeInstances != null && _activeInstances.Length == count)
            return;

        _activeInstances = new GameObject[count];
        _respawnAt = new float[count];
        _respawnScheduled = new bool[count];
    }

    private void SpawnAtIndex(int index, bool immediate)
    {
        if (prefab == null || spawnPoints == null || index < 0 || index >= spawnPoints.Length)
            return;

        Transform point = spawnPoints[index];
        if (point == null)
            return;

        Vector3 position = point.position;
        if (spawnRadiusJitter > 0f)
        {
            Vector2 jitter = Random.insideUnitCircle * spawnRadiusJitter;
            position += new Vector3(jitter.x, 0f, jitter.y);
        }

        Quaternion rotation = randomYaw
            ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
            : point.rotation;

        GameObject instance = OUTPool.Instantiate(prefab, position, rotation);

        _activeInstances[index] = instance;
        _respawnScheduled[index] = false;

        if (!immediate)
            _respawnAt[index] = 0f;
    }
}
