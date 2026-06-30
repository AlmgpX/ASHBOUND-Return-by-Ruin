using System;
using UnityEngine;

[Serializable]
public class OUTL_DropEntry
{
    public string Label = "drop";
    public GameObject Prefab;
    public OUTL_EntityDef EntityDef;
    [Range(0f, 1f)] public float Chance = 1f;
    public int MinCount = 1;
    public int MaxCount = 1;
    public float ScatterRadius = 0.5f;
    public Vector3 SpawnOffset = Vector3.up * 0.35f;
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Loot/Drop Table", fileName = "OUTL_DropTable")]
public class OUTL_DropTable : ScriptableObject
{
    public string TableId = "drop.table";
    public OUTL_DropEntry[] Drops;

    public void SpawnDrops(Vector3 position, Quaternion rotation)
    {
        if (Drops == null) return;
        for (int i = 0; i < Drops.Length; i++)
        {
            OUTL_DropEntry entry = Drops[i];
            if (entry == null) continue;
            if (UnityEngine.Random.value > entry.Chance) continue;

            int min = Mathf.Max(0, entry.MinCount);
            int max = Mathf.Max(min, entry.MaxCount);
            int count = UnityEngine.Random.Range(min, max + 1);
            for (int c = 0; c < count; c++)
            {
                Vector2 circle = UnityEngine.Random.insideUnitCircle * Mathf.Max(0f, entry.ScatterRadius);
                Vector3 p = position + entry.SpawnOffset + new Vector3(circle.x, 0f, circle.y);
                if (entry.EntityDef != null && OUTL_World.Instance != null)
                    OUTL_World.Instance.Spawn(entry.EntityDef, p, rotation);
                else if (entry.Prefab != null)
                    OUTL_PoolSystem.SpawnShared(entry.Prefab, p, rotation);
            }
        }
    }
}
