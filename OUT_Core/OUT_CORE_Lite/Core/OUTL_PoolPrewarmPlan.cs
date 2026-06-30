using System;
using UnityEngine;

[Serializable]
public struct OUTL_PoolPrewarmEntry
{
    public GameObject Prefab;
    public int Count;
}

[DisallowMultipleComponent]
public sealed class OUTL_PoolPrewarmPlan : MonoBehaviour
{
    public OUTL_PoolPrewarmEntry[] Entries;
    public bool PrewarmOnStart = true;

    private bool warmed;

    private void Start()
    {
        if (PrewarmOnStart) Run();
    }

    [ContextMenu("Run Prewarm")]
    public void Run()
    {
        if (warmed || Entries == null) return;
        warmed = true;
        for (int i = 0; i < Entries.Length; i++)
        {
            GameObject prefab = Entries[i].Prefab;
            int count = Mathf.Max(0, Entries[i].Count);
            if (prefab != null && count > 0) global::OutCore.pool.OUT.Prewarm(prefab, count);
        }
    }
}
