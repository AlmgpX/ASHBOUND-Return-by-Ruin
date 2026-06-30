using System;
using UnityEngine;

public enum OUTL_DiaryEventType : byte
{
    Spawn = 0,
    Idle = 1,
    Patrol = 2,
    HeardSound = 3,
    SawEnemy = 4,
    LostEnemy = 5,
    TookDamage = 6,
    LowHealth = 7,
    ReceivedOrder = 8,
    TookCover = 9,
    Attacked = 10,
    Killed = 11,
    Died = 12,
    DroppedLoot = 13
}

[Serializable]
public class OUTL_DiaryLineBucket
{
    public OUTL_DiaryEventType EventType;
    [TextArea] public string[] Lines;
}

[CreateAssetMenu(menuName = "OUT CORE Lite/AI/Diary Line Set", fileName = "OUTL_DiaryLineSet")]
public class OUTL_DiaryLineSet : ScriptableObject
{
    public string SetId = "diary.default";
    public OUTL_DiaryLineBucket[] Buckets;

    public string Pick(OUTL_DiaryEventType eventType)
    {
        if (Buckets == null) return string.Empty;
        for (int i = 0; i < Buckets.Length; i++)
        {
            OUTL_DiaryLineBucket bucket = Buckets[i];
            if (bucket == null || bucket.EventType != eventType || bucket.Lines == null || bucket.Lines.Length == 0) continue;
            uint seed = OUTL_HumanRandom.Hash(0xD1A7Eu, StableHash(SetId), (int)eventType);
            int index = (int)(OUTL_HumanRandom.Hash(seed, Time.frameCount) % (uint)bucket.Lines.Length);
            return bucket.Lines[index];
        }
        return string.Empty;
    }

    private static int StableHash(string value)
    {
        if (string.IsNullOrEmpty(value)) return 0;
        unchecked
        {
            int hash = 23;
            for (int i = 0; i < value.Length; i++)
                hash = hash * 31 + value[i];
            return hash;
        }
    }
}
