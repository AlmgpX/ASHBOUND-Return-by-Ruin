using OUTPool = OutCore.pool.OUT;
using UnityEngine;

public class OUT_ZombieHordeSpawner : MonoBehaviour
{
    [Header("Spawn")]
    public GameObject ZombiePrefab;
    public OUT_ZombieHordeProfile Profile;
    [Min(0)] public int SpawnCount = 128;
    [Min(0f)] public float Radius = 30f;
    public bool SpawnOnStart = false;
    public bool UsePoolService = true;
    public bool PrewarmPool = true;

    [Header("Placement")]
    public LayerMask GroundMask = ~0;
    [Min(0f)] public float GroundRayHeight = 25f;
    [Min(0f)] public float GroundRayDistance = 80f;
    public bool RandomYaw = true;

    [Header("Debug")]
    public bool LogSpawn = false;

    private void Start()
    {
        if (PrewarmPool && ZombiePrefab != null)
            OUTPool.Prewarm(ZombiePrefab, SpawnCount);

        if (SpawnOnStart)
            Spawn();
    }

    [ContextMenu("Spawn Horde")]
    public void Spawn()
    {
        if (ZombiePrefab == null || SpawnCount <= 0)
            return;

        OUT_ZombieHordeSystem.EnsureExists();
        OUT_ZombieTargetHub.EnsureExists();

        int spawned = 0;
        for (int i = 0; i < SpawnCount; i++)
        {
            Vector3 pos = PickPoint(i);
            Quaternion rot = RandomYaw ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) : transform.rotation;
            GameObject go;

            if (UsePoolService)
                go = OUTPool.Instantiate(ZombiePrefab, pos, rot);
            else
                go = Instantiate(ZombiePrefab, pos, rot);

            if (go == null)
                continue;

            OUT_ZombieHordeAgent agent = go.GetComponent<OUT_ZombieHordeAgent>();
            if (agent == null)
                agent = go.AddComponent<OUT_ZombieHordeAgent>();

            if (Profile != null)
                agent.Profile = Profile;

            OUT_ZombieHordeSystem.Register(agent);
            spawned++;
        }

        if (LogSpawn)
            Debug.Log("OUT_ZombieHordeSpawner spawned " + spawned + " zombies", this);
    }

    private Vector3 PickPoint(int salt)
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float r = Mathf.Sqrt(Random.value) * Radius;
        Vector3 pos = transform.position + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);

        Vector3 origin = pos + Vector3.up * GroundRayHeight;
        RaycastHit hit;
        if (Physics.Raycast(origin, Vector3.down, out hit, GroundRayDistance, GroundMask, QueryTriggerInteraction.Ignore))
            pos = hit.point;

        return pos;
    }
}
