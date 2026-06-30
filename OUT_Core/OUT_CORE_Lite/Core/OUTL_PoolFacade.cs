using UnityEngine;

namespace OutCore.pool
{
    public static class OUT
    {
        public static GameObject Instantiate(GameObject prefab)
        {
            if (prefab == null) return null;
            return Instantiate(prefab, prefab.transform.position, prefab.transform.rotation, true);
        }

        public static GameObject Instantiate(GameObject prefab, Transform parent)
        {
            if (prefab == null) return null;

            GameObject instance = global::OUTL_PoolSystem.SpawnShared(prefab, prefab.transform.position, prefab.transform.rotation, parent, false);
            if (instance == null) return null;

            if (parent != null)
            {
                instance.transform.SetParent(parent, false);
                instance.transform.localPosition = prefab.transform.localPosition;
                instance.transform.localRotation = prefab.transform.localRotation;
                instance.transform.localScale = prefab.transform.localScale;
            }

            RegisterManagedLiteInstance(instance);
            if (!instance.activeSelf) instance.SetActive(true);
            return instance;
        }

        public static GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return Instantiate(prefab, position, rotation, true);
        }

        public static GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation, bool activate)
        {
            if (prefab == null) return null;

            GameObject instance = global::OUTL_PoolSystem.SpawnShared(prefab, position, rotation, false);
            if (instance == null) return null;

            RegisterManagedLiteInstance(instance);
            if (activate && !instance.activeSelf) instance.SetActive(true);
            return instance;
        }

        public static GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (prefab == null) return null;

            GameObject instance = global::OUTL_PoolSystem.SpawnShared(prefab, position, rotation, parent, false);
            if (instance == null) return null;

            RegisterManagedLiteInstance(instance);
            if (!instance.activeSelf) instance.SetActive(true);
            return instance;
        }

        public static T Instantiate<T>(T prefab) where T : Component
        {
            if (prefab == null) return null;
            GameObject instance = Instantiate(prefab.gameObject);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        public static T Instantiate<T>(T prefab, Transform parent) where T : Component
        {
            if (prefab == null) return null;
            GameObject instance = Instantiate(prefab.gameObject, parent);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        public static T Instantiate<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            if (prefab == null) return null;
            GameObject instance = Instantiate(prefab.gameObject, position, rotation);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        public static T Instantiate<T>(T prefab, Vector3 position, Quaternion rotation, bool activate) where T : Component
        {
            if (prefab == null) return null;
            GameObject instance = Instantiate(prefab.gameObject, position, rotation, activate);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        public static T Instantiate<T>(GameObject prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            GameObject instance = Instantiate(prefab, position, rotation);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        public static T Instantiate<T>(GameObject prefab, Vector3 position, Quaternion rotation, bool activate) where T : Component
        {
            GameObject instance = Instantiate(prefab, position, rotation, activate);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        public static T Instantiate<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent) where T : Component
        {
            if (prefab == null) return null;
            GameObject instance = Instantiate(prefab.gameObject, position, rotation, parent);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        public static void Destroy(GameObject instance)
        {
            if (instance == null) return;

            global::OUTL_EntityAdapter adapter = instance.GetComponent<global::OUTL_EntityAdapter>();
            global::OUTL_World world = global::OUTL_World.Instance;
            if (world != null && adapter != null && adapter.Id.IsValid)
            {
                global::OUTL_EntityRuntime runtime;
                if (world.Registry.TryGet(adapter.Id, out runtime))
                {
                    world.Despawn(adapter.Id);
                    return;
                }
            }

            if (adapter != null && adapter.Runtime != null) adapter.ClearRuntimeRegistration();
            global::OUTL_PoolSystem.ReleaseShared(instance);
        }

        public static void Destroy(GameObject instance, float delay)
        {
            if (instance == null) return;
            if (delay <= 0f)
            {
                Destroy(instance);
                return;
            }

            global::OUTL_EntityAdapter adapter = instance.GetComponent<global::OUTL_EntityAdapter>();
            if (adapter != null && adapter.Id.IsValid && global::OUTL_World.Instance != null)
            {
                global::OUTL_DebugLog.Log(global::OUTL_DebugChannel.Perf, "OUT.Destroy delay requested for registered entity; routing immediately through OUTL_World.Despawn to keep registry consistent: " + instance.name, true);
                Destroy(instance);
                return;
            }

            global::OUTL_PoolSystem.ReleaseShared(instance, delay);
        }

        public static void Destroy(Component component)
        {
            if (component == null) return;
            Destroy(component.gameObject);
        }

        public static void Destroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            GameObject go = obj as GameObject;
            if (go != null)
            {
                Destroy(go);
                return;
            }

            Component component = obj as Component;
            if (component != null)
            {
                Destroy(component);
                return;
            }

            UnityEngine.Object.Destroy(obj);
        }

        public static void Release(GameObject instance)
        {
            Destroy(instance);
        }

        public static void Release(Component component)
        {
            if (component == null) return;
            Release(component.gameObject);
        }

        public static void Prewarm(GameObject prefab, int count)
        {
            global::OUTL_PoolSystem.PrewarmShared(prefab, count);
        }

        public static void Prewarm<T>(T prefab, int count) where T : Component
        {
            if (prefab == null) return;
            Prewarm(prefab.gameObject, count);
        }

        public static bool TryGetPoolStats(out global::OUTL_PoolStats stats)
        {
            return global::OUTL_PoolSystem.TryGetPoolStatsShared(out stats);
        }

        public static bool TryGetPoolStats(GameObject prefab, out global::OUTL_PoolStats stats)
        {
            return global::OUTL_PoolSystem.TryGetPoolStatsShared(prefab, out stats);
        }

        public static bool IsManaged(GameObject instance)
        {
            if (instance == null) return false;
            if (global::OUTL_PoolSystem.IsManagedShared(instance)) return true;
            global::OUTL_EntityAdapter adapter = instance.GetComponent<global::OUTL_EntityAdapter>();
            return adapter != null && adapter.Runtime != null;
        }

        public static bool IsManaged(Component component)
        {
            return component != null && IsManaged(component.gameObject);
        }

        private static void RegisterManagedLiteInstance(GameObject instance)
        {
            global::OUTL_EntityAdapter adapter = instance != null ? instance.GetComponent<global::OUTL_EntityAdapter>() : null;
            if (adapter == null) return;

            global::OUTL_World world = global::OUTL_World.Instance;
            if (world == null) return;

            if (adapter.Runtime != null && adapter.Id.IsValid)
            {
                global::OUTL_EntityRuntime runtime;
                if (world.Registry.TryGet(adapter.Id, out runtime))
                {
                    adapter.RebindRuntime(world);
                    return;
                }

                adapter.ClearRuntimeRegistration();
            }

            adapter.RegisterNow(world);
        }
    }
}
