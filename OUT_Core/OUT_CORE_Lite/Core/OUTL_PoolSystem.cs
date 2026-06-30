using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

public enum OUTL_PoolMaxSizePolicy
{
    ReuseOldest = 0,
    Expand = 1,
    DropSpawn = 2,
    FallbackInstantiate = 3,
    LogOnly = 4
}

public enum OUTL_PoolFallbackPolicy
{
    DisableOnly = 0,
    DestroyUnsafe = 1,
    WarnOnly = 2,
    ThrowInEditor = 3
}

[Serializable]
public struct OUTL_PoolStats
{
    public string name;
    public int poolCount;
    public int totalCreated;
    public int totalSpawned;
    public int totalReleased;
    public int activeCount;
    public int inactiveCount;
    public int peakActive;
    public int failedSpawns;
    public int fallbackInstantiates;
    public int doubleReleaseWarnings;
    public int unmanagedReleases;
    public int resetManifestBuilds;
    public int rigidbodyResets;
    public int trailResets;
    public int particleResets;
    public int managedInstances;
    public int delayedReleaseCount;

    public string Name { get { return name; } set { name = value; } }
    public int PoolCount { get { return poolCount; } set { poolCount = value; } }
    public int TotalCreated { get { return totalCreated; } set { totalCreated = value; } }
    public int TotalSpawned { get { return totalSpawned; } set { totalSpawned = value; } }
    public int TotalReleased { get { return totalReleased; } set { totalReleased = value; } }
    public int ActiveCount { get { return activeCount; } set { activeCount = value; } }
    public int InactiveCount { get { return inactiveCount; } set { inactiveCount = value; } }
    public int PeakActive { get { return peakActive; } set { peakActive = value; } }
    public int FailedSpawns { get { return failedSpawns; } set { failedSpawns = value; } }
    public int FallbackInstantiates { get { return fallbackInstantiates; } set { fallbackInstantiates = value; } }
    public int DoubleReleaseWarnings { get { return doubleReleaseWarnings; } set { doubleReleaseWarnings = value; } }
    public int UnmanagedReleases { get { return unmanagedReleases; } set { unmanagedReleases = value; } }
    public int ResetManifestBuilds { get { return resetManifestBuilds; } set { resetManifestBuilds = value; } }
    public int RigidbodyResets { get { return rigidbodyResets; } set { rigidbodyResets = value; } }
    public int TrailResets { get { return trailResets; } set { trailResets = value; } }
    public int ParticleResets { get { return particleResets; } set { particleResets = value; } }
    public int ManagedInstances { get { return managedInstances; } set { managedInstances = value; } }
    public int DelayedReleaseCount { get { return delayedReleaseCount; } set { delayedReleaseCount = value; } }

    public override string ToString()
    {
        return "pool=" + Name
            + " pools=" + PoolCount
            + " created=" + TotalCreated
            + " spawned=" + TotalSpawned
            + " released=" + TotalReleased
            + " active=" + ActiveCount
            + " inactive=" + InactiveCount
            + " peak=" + PeakActive
            + " failed=" + FailedSpawns
            + " fallback=" + FallbackInstantiates
            + " unmanagedRelease=" + UnmanagedReleases
            + " doubleRelease=" + DoubleReleaseWarnings
            + " manifests=" + ResetManifestBuilds
            + " rbReset=" + RigidbodyResets
            + " trailReset=" + TrailResets
            + " particleReset=" + ParticleResets
            + " managed=" + ManagedInstances
            + " delayed=" + DelayedReleaseCount;
    }
}

public struct OUTL_PoolSpawnContext
{
    public GameObject Prefab;
    public GameObject Instance;
    public Transform Parent;
    public Vector3 Position;
    public Quaternion Rotation;
    public int PoolId;
    public int Frame;
    public bool FallbackInstance;
}

public interface OUTL_IPoolSpawnContextReceiver
{
    void OUTL_OnPoolSpawnContext(in OUTL_PoolSpawnContext context);
}

public interface OUTL_IPrefabProvider
{
    bool TryGetPrefab(string key, out GameObject prefab);
}

[DisallowMultipleComponent]
public sealed class OUTL_PooledInstanceInfo : MonoBehaviour
{
    public GameObject SourcePrefab;
    public int PoolId;
    public bool IsActiveInPool;
    public bool IsReleased = true;
    public int LastSpawnFrame;
    public int LastReleaseFrame;
    public OUTL_EntityId EntityIdAtSpawn;
    public string DebugSource;
    public bool FallbackInstance;
    public int SpawnCount;
    public int ReleaseCount;

    public bool Active { get { return IsActiveInPool; } set { IsActiveInPool = value; } }
    public bool Released { get { return IsReleased; } set { IsReleased = value; } }
}

[DisallowMultipleComponent]
public class OUTL_PoolSystem : MonoBehaviour
{
    public static OUTL_PoolSystem Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InstallSceneBootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInitialPool()
    {
        EnsureRuntimePool();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureRuntimePool();
    }

    private static void EnsureRuntimePool()
    {
        if (Instance != null)
            return;

        GameObject runtime = new GameObject("OUTL_PoolSystem_Runtime");
        runtime.AddComponent<OUTL_PoolSystem>();
    }

    [Header("Pool Defaults")]
    public int DefaultCapacity = 16;
    public int MaxSize = 512;
    public bool CollectionChecks = false;
    public OUTL_PoolMaxSizePolicy MaxSizePolicy = OUTL_PoolMaxSizePolicy.Expand;
    public OUTL_PoolFallbackPolicy UnmanagedReleasePolicy = OUTL_PoolFallbackPolicy.DisableOnly;

    [Header("Pool Reset")]
    public bool ResetChildComponents = true;
    public bool ResetRigidbodyState = true;
    public bool ResetTrailState = true;
    public bool ResetParticleState = true;
    public bool RestoreColliderStateOnSpawn = true;

    private readonly Dictionary<GameObject, PoolBucket> prefabBuckets = new Dictionary<GameObject, PoolBucket>(64);
    private readonly Dictionary<GameObject, GameObject> instanceToPrefab = new Dictionary<GameObject, GameObject>(512);
    private readonly Dictionary<GameObject, ResetManifest> resetManifests = new Dictionary<GameObject, ResetManifest>(512);
    private readonly Dictionary<AudioClip, ObjectPool<AudioSource>> audioPools = new Dictionary<AudioClip, ObjectPool<AudioSource>>(32);
    private readonly Dictionary<AudioSource, AudioClip> audioToClip = new Dictionary<AudioSource, AudioClip>(128);
    private readonly List<GameObject> prewarmBuffer = new List<GameObject>(64);
    private readonly List<DelayedReleaseRequest> delayedReleases = new List<DelayedReleaseRequest>(64);
    private readonly List<DelayedAudioReleaseRequest> delayedAudioReleases = new List<DelayedAudioReleaseRequest>(64);
    private Transform poolRoot;
    private Transform inactiveFactoryRoot;
    private int nextPoolId = 1;

    private static readonly OUTL_IPoolReset[] EmptyPoolResets = new OUTL_IPoolReset[0];
    private static readonly OUTL_IPoolSpawnContextReceiver[] EmptySpawnContextReceivers = new OUTL_IPoolSpawnContextReceiver[0];
    private static readonly RigidbodyResetState[] EmptyRigidbodyStates = new RigidbodyResetState[0];
    private static readonly Rigidbody2DResetState[] EmptyRigidbody2DStates = new Rigidbody2DResetState[0];
    private static readonly TrailResetState[] EmptyTrailStates = new TrailResetState[0];
    private static readonly ParticleResetState[] EmptyParticleStates = new ParticleResetState[0];
    private static readonly ColliderResetState[] EmptyColliderStates = new ColliderResetState[0];
    private static readonly Collider2DResetState[] EmptyCollider2DStates = new Collider2DResetState[0];
    private static OUTL_PoolStats fallbackStats;
    private static bool warnedNoPoolSystem;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        EnsurePoolRoot();
    }

    private void Update()
    {
        ProcessDelayedReleases();
    }

    private void OnDestroy()
    {
        ClearAllPoolsForSceneUnload();
        if (Instance == this) Instance = null;
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, bool activate = true)
    {
        return Spawn(prefab, position, rotation, null, activate);
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, bool activate = true)
    {
        using (OUTL_Profile.PoolSpawn.Auto())
        {
            if (prefab == null) return null;
            PoolBucket bucket = GetGameObjectPool(prefab);
            if (!PrepareMaxSizePolicy(prefab, bucket)) return null;
            if (ShouldFallbackInstantiate(bucket))
                return SpawnFallback(prefab, position, rotation, parent, activate, bucket);

            GameObject go = bucket.Pool.Get();
            if (go == null)
            {
                bucket.Stats.FailedSpawns++;
                return null;
            }

            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(position, rotation);
            instanceToPrefab[go] = prefab;
            OUTL_PoolSpawnContext context = MarkSpawn(prefab, go, parent, position, rotation, bucket, false);
            CacheManifest(go);
            InvokePoolSpawn(go, context);
            if (activate) go.SetActive(true);
            OUTL_Profile.Frame.PoolSpawns++;
            return go;
        }
    }

    public void Release(GameObject instance)
    {
        Release(instance, 0f);
    }

    public void Release(GameObject instance, float delay)
    {
        if (instance == null) return;
        if (delay > 0f)
        {
            OUTL_PooledInstanceInfo info = instance.GetComponent<OUTL_PooledInstanceInfo>();
            delayedReleases.Add(new DelayedReleaseRequest
            {
                Instance = instance,
                ReleaseTime = Time.time + delay,
                SpawnCount = info != null ? info.SpawnCount : 0
            });
            return;
        }

        ReleaseNow(instance);
    }

    private void ReleaseNow(GameObject instance)
    {
        using (OUTL_Profile.PoolRelease.Auto())
        {
            if (instance == null) return;
            OUTL_PooledInstanceInfo info = instance.GetComponent<OUTL_PooledInstanceInfo>();
            if (info != null && info.IsReleased)
            {
                RegisterDoubleRelease(info.SourcePrefab);
                OUTL_DebugLog.Log(OUTL_DebugChannel.Perf, "OUTL_PoolSystem double release ignored: " + instance.name, true);
                return;
            }

            GameObject prefab;
            if (!instanceToPrefab.TryGetValue(instance, out prefab) && info != null)
                prefab = info.SourcePrefab;

            if (prefab == null)
            {
                HandleUnmanagedRelease(instance, info, "missing SourcePrefab");
                OUTL_Profile.Frame.PoolMisses++;
                return;
            }

            PoolBucket bucket;
            if (!prefabBuckets.TryGetValue(prefab, out bucket))
            {
                HandleUnmanagedRelease(instance, info, "missing pool bucket for SourcePrefab");
                instanceToPrefab.Remove(instance);
                OUTL_Profile.Frame.PoolMisses++;
                return;
            }

            InvokePoolRelease(instance);
            MarkReleased(info, bucket);
            RemoveActive(bucket, instance);
            OUTL_Profile.Frame.PoolReleases++;
            if (info != null && info.FallbackInstance)
            {
                instanceToPrefab.Remove(instance);
                resetManifests.Remove(instance);
                Destroy(instance);
                return;
            }

            bucket.Pool.Release(instance);
        }
    }

    public static GameObject SpawnShared(GameObject prefab, Vector3 position, Quaternion rotation, bool activate = true)
    {
        if (prefab == null) return null;
        if (Instance != null) return Instance.Spawn(prefab, position, rotation, activate);
        WarnNoPoolSystemFallback();
        GameObject go = Instantiate(prefab, position, rotation);
        MarkFallbackInstance(prefab, go);
        if (!activate) go.SetActive(false);
        return go;
    }

    public static GameObject SpawnShared(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, bool activate = true)
    {
        if (prefab == null) return null;
        if (Instance != null) return Instance.Spawn(prefab, position, rotation, parent, activate);
        WarnNoPoolSystemFallback();
        GameObject go = Instantiate(prefab, position, rotation, parent);
        MarkFallbackInstance(prefab, go);
        if (!activate) go.SetActive(false);
        return go;
    }

    public static void ReleaseShared(GameObject instance)
    {
        if (instance == null) return;
        if (Instance != null) Instance.Release(instance);
        else
        {
            OUTL_PooledInstanceInfo info = instance.GetComponent<OUTL_PooledInstanceInfo>();
            if (info != null && info.IsReleased)
            {
                fallbackStats.DoubleReleaseWarnings++;
                OUTL_DebugLog.Log(OUTL_DebugChannel.Perf, "OUTL_PoolSystem fallback double release ignored: " + instance.name, true);
                return;
            }

            if (info != null)
            {
                info.IsActiveInPool = false;
                info.IsReleased = true;
                info.LastReleaseFrame = Time.frameCount;
                info.ReleaseCount++;
                fallbackStats.TotalReleased++;
                if (info.FallbackInstance) fallbackStats.ActiveCount = Mathf.Max(0, fallbackStats.ActiveCount - 1);
            }
            else
            {
                fallbackStats.UnmanagedReleases++;
                fallbackStats.TotalReleased++;
            }
            if (info != null && info.FallbackInstance) Destroy(instance);
            else instance.SetActive(false);
        }
    }

    public static void ReleaseShared(GameObject instance, float delay)
    {
        if (instance == null) return;
        if (Instance != null) Instance.Release(instance, delay);
        else if (delay > 0f) Destroy(instance, delay);
        else ReleaseShared(instance);
    }

    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;
        PoolBucket bucket = GetGameObjectPool(prefab);
        prewarmBuffer.Clear();

        int target = Mathf.Min(Mathf.Max(1, count), Mathf.Max(1, MaxSize));
        for (int i = 0; i < target; i++)
        {
            GameObject go = bucket.Pool.Get();
            if (go == null) break;
            instanceToPrefab[go] = prefab;
            OUTL_PooledInstanceInfo info = EnsureInfo(go);
            info.SourcePrefab = prefab;
            info.PoolId = bucket.PoolId;
            info.IsActiveInPool = false;
            info.IsReleased = true;
            info.FallbackInstance = false;
            info.DebugSource = "prewarm";
            CacheManifest(go);
            prewarmBuffer.Add(go);
        }

        for (int i = 0; i < prewarmBuffer.Count; i++)
            if (prewarmBuffer[i] != null)
                bucket.Pool.Release(prewarmBuffer[i]);

        prewarmBuffer.Clear();
    }

    public static void PrewarmShared(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;
        if (Instance != null) Instance.Prewarm(prefab, count);
    }

    public bool IsPoolManaged(GameObject instance)
    {
        if (instance == null) return false;
        if (instanceToPrefab.ContainsKey(instance)) return true;
        OUTL_PooledInstanceInfo info = instance.GetComponent<OUTL_PooledInstanceInfo>();
        return info != null && info.SourcePrefab != null;
    }

    public static bool IsManagedShared(GameObject instance)
    {
        return Instance != null && Instance.IsPoolManaged(instance);
    }

    public void PlayClip(AudioClip clip, Vector3 position, float volume = 1f)
    {
        PlayClip(clip, position, volume, 1f, 1f, 1f, 32f, false);
    }

    public void PlayClip(AudioClip clip, Vector3 position, float volume, float pitch, float spatialBlend, float minDistance, float maxDistance, bool unscaledTime)
    {
        if (clip == null) return;
        ObjectPool<AudioSource> pool = GetAudioPool(clip);
        AudioSource source = pool.Get();
        source.transform.position = position;
        source.clip = clip;
        source.volume = Mathf.Clamp01(volume);
        source.pitch = Mathf.Clamp(pitch, -3f, 3f);
        source.spatialBlend = Mathf.Clamp01(spatialBlend);
        source.minDistance = Mathf.Max(0.01f, minDistance);
        source.maxDistance = Mathf.Max(source.minDistance, maxDistance);
        source.gameObject.SetActive(true);
        source.Play();
        float duration = Mathf.Abs(source.pitch) > 0.01f ? clip.length / Mathf.Abs(source.pitch) : clip.length;
        delayedAudioReleases.Add(new DelayedAudioReleaseRequest
        {
            Source = source,
            ReleaseTime = (unscaledTime ? Time.unscaledTime : Time.time) + Mathf.Max(0.01f, duration),
            UnscaledTime = unscaledTime
        });
    }

    public static void PlayClipShared(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        if (Instance != null) Instance.PlayClip(clip, position, volume);
        else AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    public static void PlayClipShared(AudioClip clip, Vector3 position, float volume, float pitch, float spatialBlend, float minDistance, float maxDistance, bool unscaledTime)
    {
        if (clip == null) return;
        if (Instance != null) Instance.PlayClip(clip, position, volume, pitch, spatialBlend, minDistance, maxDistance, unscaledTime);
        else AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    public bool TryGetPoolStats(out OUTL_PoolStats stats)
    {
        stats = BuildAggregateStats();
        return true;
    }

    public OUTL_PoolStats GetStatsSnapshot()
    {
        return BuildAggregateStats();
    }

    public bool TryGetPoolStats(GameObject prefab, out OUTL_PoolStats stats)
    {
        stats = default(OUTL_PoolStats);
        if (prefab == null) return false;
        PoolBucket bucket;
        if (!prefabBuckets.TryGetValue(prefab, out bucket)) return false;
        stats = BuildBucketStats(bucket);
        return true;
    }

    public static bool TryGetPoolStatsShared(out OUTL_PoolStats stats)
    {
        if (Instance != null) return Instance.TryGetPoolStats(out stats);
        stats = fallbackStats;
        stats.Name = "OUTL_FALLBACK_NO_POOL";
        stats.InactiveCount = 0;
        return true;
    }

    public static bool TryGetPoolStatsShared(GameObject prefab, out OUTL_PoolStats stats)
    {
        if (Instance != null) return Instance.TryGetPoolStats(prefab, out stats);
        stats = fallbackStats;
        stats.Name = prefab != null ? prefab.name : "OUTL_FALLBACK_NO_POOL";
        return prefab != null;
    }

    public string DumpStats()
    {
        StringBuilder sb = new StringBuilder(256 + prefabBuckets.Count * 96);
        OUTL_PoolStats aggregate = BuildAggregateStats();
        sb.AppendLine(aggregate.ToString());
        foreach (KeyValuePair<GameObject, PoolBucket> pair in prefabBuckets)
            sb.AppendLine(BuildBucketStats(pair.Value).ToString());
        return sb.ToString();
    }

    public void ClearAllPoolsForSceneUnload()
    {
        foreach (KeyValuePair<GameObject, PoolBucket> pair in prefabBuckets)
            if (pair.Value != null && pair.Value.Pool != null)
                pair.Value.Pool.Clear();

        prefabBuckets.Clear();
        instanceToPrefab.Clear();
        resetManifests.Clear();
        delayedReleases.Clear();
        prewarmBuffer.Clear();
    }

    private PoolBucket GetGameObjectPool(GameObject prefab)
    {
        PoolBucket bucket;
        if (prefabBuckets.TryGetValue(prefab, out bucket)) return bucket;

        bucket = new PoolBucket
        {
            PoolId = nextPoolId++,
            Name = prefab != null ? prefab.name : "null",
            Container = GetPoolContainer(prefab)
        };

        ObjectPool<GameObject> pool = null;
        pool = new ObjectPool<GameObject>(
            () =>
            {
                GameObject go = Instantiate(prefab, EnsureInactiveFactoryRoot());
                go.name = prefab.name + " (Pooled)";
                go.SetActive(false);
                go.transform.SetParent(bucket.Container != null ? bucket.Container : transform, false);
                PrepareCreatedInstance(prefab, go, bucket);
                CacheManifest(go);
                return go;
            },
            go =>
            {
            },
            go =>
            {
                if (go == null) return;
                Transform container = bucket.Container != null ? bucket.Container : transform;
                go.transform.SetParent(container, false);
                go.SetActive(false);
            },
            go =>
            {
                if (go != null)
                {
                    instanceToPrefab.Remove(go);
                    resetManifests.Remove(go);
                    Destroy(go);
                }
            },
            CollectionChecks,
            Mathf.Max(1, DefaultCapacity),
            Mathf.Max(1, MaxSize));

        bucket.Pool = pool;
        prefabBuckets[prefab] = bucket;
        return bucket;
    }

    private bool PrepareMaxSizePolicy(GameObject prefab, PoolBucket bucket)
    {
        if (bucket == null || MaxSize <= 0) return true;
        if (bucket.Stats.ActiveCount < MaxSize || bucket.Stats.TotalCreated < MaxSize) return true;

        switch (MaxSizePolicy)
        {
            case OUTL_PoolMaxSizePolicy.DropSpawn:
                bucket.Stats.FailedSpawns++;
                OUTL_DebugLog.Log(OUTL_DebugChannel.Perf, "OUTL_PoolSystem dropped spawn for full pool: " + bucket.Name, true);
                return false;
            case OUTL_PoolMaxSizePolicy.ReuseOldest:
                if (ReleaseOldestActive(bucket)) return true;
                bucket.Stats.FailedSpawns++;
                return false;
            case OUTL_PoolMaxSizePolicy.FallbackInstantiate:
                return true;
            case OUTL_PoolMaxSizePolicy.LogOnly:
                OUTL_DebugLog.Log(OUTL_DebugChannel.Perf, "OUTL_PoolSystem max-size log-only spawn for pool: " + bucket.Name, true);
                return true;
            case OUTL_PoolMaxSizePolicy.Expand:
            default:
                return true;
        }
    }

    private bool ReleaseOldestActive(PoolBucket bucket)
    {
        if (bucket == null) return false;
        for (int i = 0; i < bucket.ActiveOrder.Count; i++)
        {
            GameObject candidate = bucket.ActiveOrder[i];
            if (candidate == null) continue;
            OUTL_PooledInstanceInfo info = candidate.GetComponent<OUTL_PooledInstanceInfo>();
            if (info != null && info.IsActiveInPool && !info.IsReleased)
            {
                ReleaseNow(candidate);
                return true;
            }
        }
        return false;
    }

    private bool ShouldFallbackInstantiate(PoolBucket bucket)
    {
        return bucket != null
            && MaxSizePolicy == OUTL_PoolMaxSizePolicy.FallbackInstantiate
            && MaxSize > 0
            && bucket.Stats.ActiveCount >= MaxSize
            && bucket.Stats.TotalCreated >= MaxSize;
    }

    private GameObject SpawnFallback(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, bool activate, PoolBucket bucket)
    {
        OUTL_DebugLog.Log(OUTL_DebugChannel.Perf, "OUTL_PoolSystem fallback Instantiate for full pool: " + (prefab != null ? prefab.name : "null"), true);
        GameObject go = parent != null ? Instantiate(prefab, position, rotation, parent) : Instantiate(prefab, position, rotation);
        if (go == null)
        {
            if (bucket != null) bucket.Stats.FailedSpawns++;
            return null;
        }

        if (bucket != null)
        {
            bucket.Stats.FallbackInstantiates++;
            bucket.Stats.TotalCreated++;
        }
        instanceToPrefab[go] = prefab;
        OUTL_PoolSpawnContext context = MarkSpawn(prefab, go, parent, position, rotation, bucket, true);
        CacheManifest(go);
        InvokePoolSpawn(go, context);
        if (!activate) go.SetActive(false);
        OUTL_Profile.Frame.PoolSpawns++;
        return go;
    }

    private OUTL_PoolSpawnContext MarkSpawn(GameObject prefab, GameObject instance, Transform parent, Vector3 position, Quaternion rotation, PoolBucket bucket, bool fallback)
    {
        OUTL_PooledInstanceInfo info = EnsureInfo(instance);
        info.SourcePrefab = prefab;
        info.PoolId = bucket != null ? bucket.PoolId : 0;
        info.IsActiveInPool = true;
        info.IsReleased = false;
        info.FallbackInstance = fallback;
        info.LastSpawnFrame = Time.frameCount;
        info.LastReleaseFrame = 0;
        info.DebugSource = fallback ? "pool_max_fallback" : "pool";
        OUTL_EntityAdapter adapter = instance.GetComponent<OUTL_EntityAdapter>();
        info.EntityIdAtSpawn = adapter != null ? adapter.Id : OUTL_EntityId.None;
        info.SpawnCount++;

        if (bucket != null)
        {
            bucket.Stats.TotalSpawned++;
            bucket.Stats.ActiveCount++;
            if (bucket.Stats.ActiveCount > bucket.Stats.PeakActive) bucket.Stats.PeakActive = bucket.Stats.ActiveCount;
            bucket.ActiveOrder.Add(instance);
        }

        return new OUTL_PoolSpawnContext
        {
            Prefab = prefab,
            Instance = instance,
            Parent = parent,
            Position = position,
            Rotation = rotation,
            PoolId = info.PoolId,
            Frame = info.LastSpawnFrame,
            FallbackInstance = fallback
        };
    }

    private void MarkReleased(OUTL_PooledInstanceInfo info, PoolBucket bucket)
    {
        if (info == null) return;
        info.IsActiveInPool = false;
        info.IsReleased = true;
        info.LastReleaseFrame = Time.frameCount;
        info.ReleaseCount++;
        if (bucket != null)
        {
            bucket.Stats.TotalReleased++;
            bucket.Stats.ActiveCount = Mathf.Max(0, bucket.Stats.ActiveCount - 1);
        }
    }

    private void RemoveActive(PoolBucket bucket, GameObject instance)
    {
        if (bucket == null || instance == null) return;
        for (int i = 0; i < bucket.ActiveOrder.Count; i++)
        {
            if (bucket.ActiveOrder[i] != instance) continue;
            int last = bucket.ActiveOrder.Count - 1;
            bucket.ActiveOrder[i] = bucket.ActiveOrder[last];
            bucket.ActiveOrder.RemoveAt(last);
            return;
        }
    }

    private void RegisterDoubleRelease(GameObject prefab)
    {
        PoolBucket bucket;
        if (prefab != null && prefabBuckets.TryGetValue(prefab, out bucket) && bucket != null)
        {
            bucket.Stats.DoubleReleaseWarnings++;
            return;
        }
        fallbackStats.DoubleReleaseWarnings++;
    }

    private void HandleUnmanagedRelease(GameObject instance, OUTL_PooledInstanceInfo info, string reason)
    {
        if (instance == null) return;
        if (info != null)
        {
            info.IsActiveInPool = false;
            info.IsReleased = true;
            info.LastReleaseFrame = Time.frameCount;
            info.DebugSource = "unmanaged_release:" + reason;
            info.ReleaseCount++;
        }

        fallbackStats.UnmanagedReleases++;
        fallbackStats.TotalReleased++;
        OUTL_DebugLog.Log(OUTL_DebugChannel.Perf, "OUTL_PoolSystem unmanaged release (" + reason + "): " + instance.name + " policy=" + UnmanagedReleasePolicy, true);

        if (UnmanagedReleasePolicy == OUTL_PoolFallbackPolicy.WarnOnly)
            return;

        if (UnmanagedReleasePolicy == OUTL_PoolFallbackPolicy.DestroyUnsafe)
        {
            Destroy(instance);
            return;
        }

        if (UnmanagedReleasePolicy == OUTL_PoolFallbackPolicy.ThrowInEditor)
        {
#if UNITY_EDITOR
            throw new InvalidOperationException("OUTL_PoolSystem unmanaged release: " + instance.name + " reason=" + reason);
#else
            instance.SetActive(false);
            return;
#endif
        }

        instance.SetActive(false);
    }

    private void PrepareCreatedInstance(GameObject prefab, GameObject instance, PoolBucket bucket)
    {
        if (instance == null || bucket == null) return;
        instanceToPrefab[instance] = prefab;
        OUTL_PooledInstanceInfo info = EnsureInfo(instance);
        info.SourcePrefab = prefab;
        info.PoolId = bucket.PoolId;
        info.IsActiveInPool = false;
        info.IsReleased = true;
        info.FallbackInstance = false;
        info.DebugSource = "created";
        bucket.Stats.TotalCreated++;
    }

    private Transform EnsurePoolRoot()
    {
        if (poolRoot != null) return poolRoot;
        Transform existing = transform.Find("OUTL_POOL_ROOT");
        if (existing != null)
        {
            poolRoot = existing;
            return poolRoot;
        }

        GameObject root = new GameObject("OUTL_POOL_ROOT");
        root.transform.SetParent(transform, false);
        poolRoot = root.transform;
        return poolRoot;
    }

    private Transform GetPoolContainer(GameObject prefab)
    {
        Transform root = EnsurePoolRoot();
        string containerName = (prefab != null ? prefab.name : "null") + "_POOL";
        Transform existing = root.Find(containerName);
        if (existing != null) return existing;
        GameObject container = new GameObject(containerName);
        container.transform.SetParent(root, false);
        return container.transform;
    }

    private Transform EnsureInactiveFactoryRoot()
    {
        if (inactiveFactoryRoot != null) return inactiveFactoryRoot;
        Transform existing = transform.Find("OUTL_POOL_FACTORY_INACTIVE");
        if (existing != null)
        {
            inactiveFactoryRoot = existing;
            if (inactiveFactoryRoot.gameObject.activeSelf) inactiveFactoryRoot.gameObject.SetActive(false);
            return inactiveFactoryRoot;
        }

        GameObject root = new GameObject("OUTL_POOL_FACTORY_INACTIVE");
        root.transform.SetParent(transform, false);
        root.SetActive(false);
        inactiveFactoryRoot = root.transform;
        return inactiveFactoryRoot;
    }

    private OUTL_PoolStats BuildAggregateStats()
    {
        OUTL_PoolStats stats = new OUTL_PoolStats { Name = "OUTL_PoolSystem", PoolCount = prefabBuckets.Count, DelayedReleaseCount = delayedReleases.Count + delayedAudioReleases.Count };
        foreach (KeyValuePair<GameObject, PoolBucket> pair in prefabBuckets)
        {
            OUTL_PoolStats s = BuildBucketStats(pair.Value);
            stats.TotalCreated += s.TotalCreated;
            stats.TotalSpawned += s.TotalSpawned;
            stats.TotalReleased += s.TotalReleased;
            stats.ActiveCount += s.ActiveCount;
            stats.InactiveCount += s.InactiveCount;
            stats.FailedSpawns += s.FailedSpawns;
            stats.FallbackInstantiates += s.FallbackInstantiates;
            stats.DoubleReleaseWarnings += s.DoubleReleaseWarnings;
            stats.UnmanagedReleases += s.UnmanagedReleases;
            stats.ResetManifestBuilds += s.ResetManifestBuilds;
            stats.RigidbodyResets += s.RigidbodyResets;
            stats.TrailResets += s.TrailResets;
            stats.ParticleResets += s.ParticleResets;
            if (s.PeakActive > stats.PeakActive) stats.PeakActive = s.PeakActive;
        }
        stats.TotalCreated += fallbackStats.TotalCreated;
        stats.TotalSpawned += fallbackStats.TotalSpawned;
        stats.TotalReleased += fallbackStats.TotalReleased;
        stats.ActiveCount += fallbackStats.ActiveCount;
        stats.FallbackInstantiates += fallbackStats.FallbackInstantiates;
        stats.DoubleReleaseWarnings += fallbackStats.DoubleReleaseWarnings;
        stats.UnmanagedReleases += fallbackStats.UnmanagedReleases;
        stats.ResetManifestBuilds += fallbackStats.ResetManifestBuilds;
        stats.RigidbodyResets += fallbackStats.RigidbodyResets;
        stats.TrailResets += fallbackStats.TrailResets;
        stats.ParticleResets += fallbackStats.ParticleResets;
        stats.ManagedInstances = instanceToPrefab.Count;
        return stats;
    }

    private static OUTL_PoolStats BuildBucketStats(PoolBucket bucket)
    {
        if (bucket == null) return default(OUTL_PoolStats);
        OUTL_PoolStats stats = bucket.Stats;
        stats.Name = bucket.Name;
        stats.PoolCount = 1;
        stats.InactiveCount = bucket.Pool != null ? bucket.Pool.CountInactive : Mathf.Max(0, stats.TotalCreated - stats.ActiveCount);
        stats.ManagedInstances = stats.TotalCreated;
        return stats;
    }

    private void ProcessDelayedReleases()
    {
        ProcessDelayedObjectReleases();
        ProcessDelayedAudioReleases();
    }

    private void ProcessDelayedObjectReleases()
    {
        if (delayedReleases.Count == 0) return;
        float now = Time.time;
        for (int i = delayedReleases.Count - 1; i >= 0; i--)
        {
            DelayedReleaseRequest request = delayedReleases[i];
            if (request.Instance == null)
            {
                delayedReleases.RemoveAt(i);
                continue;
            }

            if (now < request.ReleaseTime)
                continue;

            delayedReleases.RemoveAt(i);

            OUTL_PooledInstanceInfo info = request.Instance.GetComponent<OUTL_PooledInstanceInfo>();
            if (request.SpawnCount > 0 &&
                (info == null || info.IsReleased || info.SpawnCount != request.SpawnCount))
                continue;

            ReleaseNow(request.Instance);
        }
    }

    private void ProcessDelayedAudioReleases()
    {
        if (delayedAudioReleases.Count == 0) return;
        float scaledNow = Time.time;
        float unscaledNow = Time.unscaledTime;
        for (int i = delayedAudioReleases.Count - 1; i >= 0; i--)
        {
            DelayedAudioReleaseRequest request = delayedAudioReleases[i];
            float now = request.UnscaledTime ? unscaledNow : scaledNow;
            if (request.Source == null || now >= request.ReleaseTime)
            {
                delayedAudioReleases.RemoveAt(i);
                if (request.Source != null) ReleaseAudioNow(request.Source);
            }
        }
    }

    private void ReleaseAudioNow(AudioSource source)
    {
        if (source == null) return;
        AudioClip clip;
        if (!audioToClip.TryGetValue(source, out clip))
        {
            source.gameObject.SetActive(false);
            return;
        }

        ObjectPool<AudioSource> pool;
        if (audioPools.TryGetValue(clip, out pool)) pool.Release(source);
        else source.gameObject.SetActive(false);
    }

    private ObjectPool<AudioSource> GetAudioPool(AudioClip clip)
    {
        ObjectPool<AudioSource> pool;
        if (audioPools.TryGetValue(clip, out pool)) return pool;

        pool = new ObjectPool<AudioSource>(
            () =>
            {
                GameObject go = new GameObject("OUTL_Audio_" + clip.name);
                go.transform.SetParent(transform, false);
                AudioSource source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                go.SetActive(false);
                audioToClip[source] = clip;
                return source;
            },
            source =>
            {
                if (source == null) return;
                source.gameObject.SetActive(true);
            },
            source =>
            {
                if (source == null) return;
                source.Stop();
                source.clip = null;
                source.pitch = 1f;
                source.volume = 1f;
                source.spatialBlend = 1f;
                source.gameObject.SetActive(false);
            },
            source =>
            {
                if (source != null) Destroy(source.gameObject);
            },
            CollectionChecks,
            Mathf.Max(1, DefaultCapacity),
            Mathf.Max(1, MaxSize));

        audioPools[clip] = pool;
        return pool;
    }

    private void InvokePoolSpawn(GameObject go, in OUTL_PoolSpawnContext context)
    {
        if (go == null) return;
        ResetManifest manifest = CacheManifest(go);
        if (manifest == null) return;
        if (RestoreColliderStateOnSpawn) RestoreColliders(manifest);
        if (ResetRigidbodyState)
        {
            int count = ResetRigidbodies(manifest, false) + ResetRigidbodies2D(manifest, false);
            RegisterRigidbodyReset(go, count);
        }
        if (ResetTrailState) RegisterTrailReset(go, ResetTrails(manifest, false));
        if (ResetParticleState) RegisterParticleReset(go, ResetParticles(manifest, false));
        for (int i = 0; i < manifest.PoolResets.Length; i++)
            if (manifest.PoolResets[i] != null)
                manifest.PoolResets[i].OUTL_OnPoolSpawn();
        for (int i = 0; i < manifest.ContextReceivers.Length; i++)
            if (manifest.ContextReceivers[i] != null)
                manifest.ContextReceivers[i].OUTL_OnPoolSpawnContext(context);
    }

    private void InvokePoolRelease(GameObject go)
    {
        if (go == null) return;
        ResetManifest manifest = CacheManifest(go);
        if (manifest == null) return;
        for (int i = 0; i < manifest.PoolResets.Length; i++)
            if (manifest.PoolResets[i] != null)
                manifest.PoolResets[i].OUTL_OnPoolRelease();
        if (ResetRigidbodyState)
        {
            int count = ResetRigidbodies(manifest, true) + ResetRigidbodies2D(manifest, true);
            RegisterRigidbodyReset(go, count);
        }
        if (ResetTrailState) RegisterTrailReset(go, ResetTrails(manifest, true));
        if (ResetParticleState) RegisterParticleReset(go, ResetParticles(manifest, true));
    }

    private ResetManifest CacheManifest(GameObject go)
    {
        if (go == null) return null;
        ResetManifest manifest;
        if (resetManifests.TryGetValue(go, out manifest) && manifest != null) return manifest;

        manifest = BuildManifest(go);
        resetManifests[go] = manifest;
        RegisterManifestBuild(go);
        return manifest;
    }

    private void RegisterManifestBuild(GameObject instance)
    {
        PoolBucket bucket = FindBucketForInstance(instance);
        if (bucket != null) bucket.Stats.ResetManifestBuilds++;
        else fallbackStats.ResetManifestBuilds++;
    }

    private void RegisterRigidbodyReset(GameObject instance, int count)
    {
        if (count <= 0) return;
        PoolBucket bucket = FindBucketForInstance(instance);
        if (bucket != null) bucket.Stats.RigidbodyResets += count;
        else fallbackStats.RigidbodyResets += count;
    }

    private void RegisterTrailReset(GameObject instance, int count)
    {
        if (count <= 0) return;
        PoolBucket bucket = FindBucketForInstance(instance);
        if (bucket != null) bucket.Stats.TrailResets += count;
        else fallbackStats.TrailResets += count;
    }

    private void RegisterParticleReset(GameObject instance, int count)
    {
        if (count <= 0) return;
        PoolBucket bucket = FindBucketForInstance(instance);
        if (bucket != null) bucket.Stats.ParticleResets += count;
        else fallbackStats.ParticleResets += count;
    }

    private PoolBucket FindBucketForInstance(GameObject instance)
    {
        if (instance == null) return null;
        GameObject prefab;
        if (!instanceToPrefab.TryGetValue(instance, out prefab))
        {
            OUTL_PooledInstanceInfo info = instance.GetComponent<OUTL_PooledInstanceInfo>();
            if (info != null) prefab = info.SourcePrefab;
        }

        PoolBucket bucket;
        if (prefab != null && prefabBuckets.TryGetValue(prefab, out bucket)) return bucket;
        return null;
    }

    private ResetManifest BuildManifest(GameObject go)
    {
        MonoBehaviour[] behaviours = ResetChildComponents ? go.GetComponentsInChildren<MonoBehaviour>(true) : go.GetComponents<MonoBehaviour>();
        int resetCount = 0;
        int contextCount = 0;
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is OUTL_IPoolReset)
                resetCount++;
            if (behaviours[i] is OUTL_IPoolSpawnContextReceiver)
                contextCount++;
        }

        OUTL_IPoolReset[] resets = resetCount > 0 ? new OUTL_IPoolReset[resetCount] : EmptyPoolResets;
        OUTL_IPoolSpawnContextReceiver[] contextReceivers = contextCount > 0 ? new OUTL_IPoolSpawnContextReceiver[contextCount] : EmptySpawnContextReceivers;
        int write = 0;
        for (int i = 0; i < behaviours.Length; i++)
        {
            OUTL_IPoolReset reset = behaviours[i] as OUTL_IPoolReset;
            if (reset != null) resets[write++] = reset;
        }

        write = 0;
        for (int i = 0; i < behaviours.Length; i++)
        {
            OUTL_IPoolSpawnContextReceiver receiver = behaviours[i] as OUTL_IPoolSpawnContextReceiver;
            if (receiver != null) contextReceivers[write++] = receiver;
        }

        return new ResetManifest
        {
            PoolResets = resets,
            ContextReceivers = contextReceivers,
            Rigidbodies = BuildRigidbodyStates(go),
            Rigidbodies2D = BuildRigidbody2DStates(go),
            Trails = BuildTrailStates(go),
            Particles = BuildParticleStates(go),
            Colliders = BuildColliderStates(go),
            Colliders2D = BuildCollider2DStates(go)
        };
    }

    private RigidbodyResetState[] BuildRigidbodyStates(GameObject go)
    {
        Rigidbody[] bodies = ResetChildComponents ? go.GetComponentsInChildren<Rigidbody>(true) : go.GetComponents<Rigidbody>();
        if (bodies == null || bodies.Length == 0) return EmptyRigidbodyStates;
        RigidbodyResetState[] states = new RigidbodyResetState[bodies.Length];
        for (int i = 0; i < bodies.Length; i++)
        {
            Rigidbody body = bodies[i];
            states[i] = new RigidbodyResetState
            {
                Body = body,
                IsKinematic = body != null && body.isKinematic,
                UseGravity = body != null && body.useGravity,
                DetectCollisions = body != null && body.detectCollisions
            };
        }
        return states;
    }

    private Rigidbody2DResetState[] BuildRigidbody2DStates(GameObject go)
    {
        Rigidbody2D[] bodies = ResetChildComponents ? go.GetComponentsInChildren<Rigidbody2D>(true) : go.GetComponents<Rigidbody2D>();
        if (bodies == null || bodies.Length == 0) return EmptyRigidbody2DStates;
        Rigidbody2DResetState[] states = new Rigidbody2DResetState[bodies.Length];
        for (int i = 0; i < bodies.Length; i++)
        {
            Rigidbody2D body = bodies[i];
            states[i] = new Rigidbody2DResetState
            {
                Body = body,
                BodyType = body != null ? body.bodyType : RigidbodyType2D.Dynamic,
                GravityScale = body != null ? body.gravityScale : 1f,
                Simulated = body != null && body.simulated
            };
        }
        return states;
    }

    private TrailResetState[] BuildTrailStates(GameObject go)
    {
        TrailRenderer[] trails = ResetChildComponents ? go.GetComponentsInChildren<TrailRenderer>(true) : go.GetComponents<TrailRenderer>();
        if (trails == null || trails.Length == 0) return EmptyTrailStates;
        TrailResetState[] states = new TrailResetState[trails.Length];
        for (int i = 0; i < trails.Length; i++)
        {
            TrailRenderer trail = trails[i];
            states[i] = new TrailResetState { Trail = trail, Emitting = trail != null && trail.emitting };
        }
        return states;
    }

    private ParticleResetState[] BuildParticleStates(GameObject go)
    {
        ParticleSystem[] particles = ResetChildComponents ? go.GetComponentsInChildren<ParticleSystem>(true) : go.GetComponents<ParticleSystem>();
        if (particles == null || particles.Length == 0) return EmptyParticleStates;
        ParticleResetState[] states = new ParticleResetState[particles.Length];
        for (int i = 0; i < particles.Length; i++)
            states[i] = new ParticleResetState { System = particles[i] };
        return states;
    }

    private ColliderResetState[] BuildColliderStates(GameObject go)
    {
        Collider[] colliders = ResetChildComponents ? go.GetComponentsInChildren<Collider>(true) : go.GetComponents<Collider>();
        if (colliders == null || colliders.Length == 0) return EmptyColliderStates;
        ColliderResetState[] states = new ColliderResetState[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            states[i] = new ColliderResetState
            {
                Collider = collider,
                Enabled = collider != null && collider.enabled,
                IsTrigger = collider != null && collider.isTrigger
            };
        }
        return states;
    }

    private Collider2DResetState[] BuildCollider2DStates(GameObject go)
    {
        Collider2D[] colliders = ResetChildComponents ? go.GetComponentsInChildren<Collider2D>(true) : go.GetComponents<Collider2D>();
        if (colliders == null || colliders.Length == 0) return EmptyCollider2DStates;
        Collider2DResetState[] states = new Collider2DResetState[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];
            states[i] = new Collider2DResetState
            {
                Collider = collider,
                Enabled = collider != null && collider.enabled,
                IsTrigger = collider != null && collider.isTrigger
            };
        }
        return states;
    }

    private static void RestoreColliders(ResetManifest manifest)
    {
        for (int i = 0; i < manifest.Colliders.Length; i++)
        {
            Collider collider = manifest.Colliders[i].Collider;
            if (collider == null) continue;
            collider.enabled = manifest.Colliders[i].Enabled;
            collider.isTrigger = manifest.Colliders[i].IsTrigger;
        }

        for (int i = 0; i < manifest.Colliders2D.Length; i++)
        {
            Collider2D collider = manifest.Colliders2D[i].Collider;
            if (collider == null) continue;
            collider.enabled = manifest.Colliders2D[i].Enabled;
            collider.isTrigger = manifest.Colliders2D[i].IsTrigger;
        }
    }

    private static int ResetRigidbodies(ResetManifest manifest, bool releasing)
    {
        int count = 0;
        for (int i = 0; i < manifest.Rigidbodies.Length; i++)
        {
            Rigidbody body = manifest.Rigidbodies[i].Body;
            if (body == null) continue;
            body.isKinematic = manifest.Rigidbodies[i].IsKinematic;
            body.useGravity = manifest.Rigidbodies[i].UseGravity;
            body.detectCollisions = manifest.Rigidbodies[i].DetectCollisions;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            if (releasing) body.Sleep();
            else if (!body.isKinematic) body.WakeUp();
            count++;
        }
        return count;
    }

    private static int ResetRigidbodies2D(ResetManifest manifest, bool releasing)
    {
        int count = 0;
        for (int i = 0; i < manifest.Rigidbodies2D.Length; i++)
        {
            Rigidbody2D body = manifest.Rigidbodies2D[i].Body;
            if (body == null) continue;
            body.bodyType = manifest.Rigidbodies2D[i].BodyType;
            body.gravityScale = manifest.Rigidbodies2D[i].GravityScale;
            body.simulated = manifest.Rigidbodies2D[i].Simulated;
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            if (releasing) body.Sleep();
            else if (body.simulated && body.bodyType == RigidbodyType2D.Dynamic) body.WakeUp();
            count++;
        }
        return count;
    }

    private static int ResetTrails(ResetManifest manifest, bool releasing)
    {
        int count = 0;
        for (int i = 0; i < manifest.Trails.Length; i++)
        {
            TrailRenderer trail = manifest.Trails[i].Trail;
            if (trail == null) continue;
            trail.Clear();
            if (releasing) trail.emitting = false;
            else trail.emitting = manifest.Trails[i].Emitting;
            count++;
        }
        return count;
    }

    private static int ResetParticles(ResetManifest manifest, bool releasing)
    {
        int count = 0;
        for (int i = 0; i < manifest.Particles.Length; i++)
        {
            ParticleSystem ps = manifest.Particles[i].System;
            if (ps == null) continue;
            if (releasing)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                count++;
                continue;
            }

            ps.Clear(true);
            count++;
        }
        return count;
    }

    private static OUTL_PooledInstanceInfo EnsureInfo(GameObject go)
    {
        if (go == null) return null;
        OUTL_PooledInstanceInfo info = go.GetComponent<OUTL_PooledInstanceInfo>();
        if (info == null) info = go.AddComponent<OUTL_PooledInstanceInfo>();
        return info;
    }

    private static void WarnNoPoolSystemFallback()
    {
        fallbackStats.FallbackInstantiates++;
        if (warnedNoPoolSystem) return;
        warnedNoPoolSystem = true;
        OUTL_DebugLog.Log(OUTL_DebugChannel.Perf, "OUTL_PoolSystem missing; OutCore.pool.OUT is using low-level fallback Instantiate. Add OUTL_World/OUTL_PoolSystem for pooled lifetime.", true);
    }

    private static void MarkFallbackInstance(GameObject prefab, GameObject instance)
    {
        if (instance == null) return;
        OUTL_PooledInstanceInfo info = EnsureInfo(instance);
        info.SourcePrefab = prefab;
        info.PoolId = 0;
        info.IsActiveInPool = true;
        info.IsReleased = false;
        info.FallbackInstance = true;
        info.LastSpawnFrame = Time.frameCount;
        info.LastReleaseFrame = 0;
        info.EntityIdAtSpawn = OUTL_EntityId.None;
        info.DebugSource = "no_pool_fallback";
        info.SpawnCount++;
        fallbackStats.TotalCreated++;
        fallbackStats.TotalSpawned++;
        fallbackStats.ActiveCount++;
        if (fallbackStats.ActiveCount > fallbackStats.PeakActive) fallbackStats.PeakActive = fallbackStats.ActiveCount;
    }

    private sealed class ResetManifest
    {
        public OUTL_IPoolReset[] PoolResets;
        public OUTL_IPoolSpawnContextReceiver[] ContextReceivers;
        public RigidbodyResetState[] Rigidbodies;
        public Rigidbody2DResetState[] Rigidbodies2D;
        public TrailResetState[] Trails;
        public ParticleResetState[] Particles;
        public ColliderResetState[] Colliders;
        public Collider2DResetState[] Colliders2D;
    }

    private struct RigidbodyResetState
    {
        public Rigidbody Body;
        public bool IsKinematic;
        public bool UseGravity;
        public bool DetectCollisions;
    }

    private struct Rigidbody2DResetState
    {
        public Rigidbody2D Body;
        public RigidbodyType2D BodyType;
        public float GravityScale;
        public bool Simulated;
    }

    private struct TrailResetState
    {
        public TrailRenderer Trail;
        public bool Emitting;
    }

    private struct ParticleResetState
    {
        public ParticleSystem System;
    }

    private struct ColliderResetState
    {
        public Collider Collider;
        public bool Enabled;
        public bool IsTrigger;
    }

    private struct Collider2DResetState
    {
        public Collider2D Collider;
        public bool Enabled;
        public bool IsTrigger;
    }

    private sealed class PoolBucket
    {
        public string Name;
        public int PoolId;
        public ObjectPool<GameObject> Pool;
        public Transform Container;
        public OUTL_PoolStats Stats;
        public readonly List<GameObject> ActiveOrder = new List<GameObject>(64);
    }

    private struct DelayedReleaseRequest
    {
        public GameObject Instance;
        public float ReleaseTime;
        public int SpawnCount;
    }

    private struct DelayedAudioReleaseRequest
    {
        public AudioSource Source;
        public float ReleaseTime;
        public bool UnscaledTime;
    }
}
