using UnityEngine;

public enum OUTL_HumanRandomMatrix : byte
{
    HumanPick1To100 = 0,
    HumanLeastPicked1To100 = 1,
    HumanDigit0To9 = 2,
    AntiHumanDigit0To9 = 3
}

public static class OUTL_HumanRandom
{
    private const uint FnvOffset = 2166136261u;
    private const uint FnvPrime = 16777619u;

    private static readonly ushort[] humanPick1To100 =
    {
        125,118,92,86,96,74,175,74,70,55,
        52,72,78,54,42,50,88,46,48,42,
        62,78,110,70,58,52,104,60,44,34,
        55,63,92,86,43,64,190,70,58,42,
        48,178,70,62,72,58,124,60,60,132,
        45,56,74,72,112,132,140,60,58,35,
        43,52,76,80,55,70,128,58,255,36,
        60,126,172,68,48,80,168,82,72,42,
        52,62,86,70,58,50,96,82,72,45,
        54,58,62,52,47,42,82,70,132,76
    };

    private static readonly ushort[] humanLeastPicked1To100 =
    {
        105,96,170,86,90,78,185,72,82,58,
        62,80,92,72,58,65,102,58,62,54,
        68,86,112,78,62,60,108,70,54,42,
        52,62,92,82,54,70,176,72,60,48,
        42,82,92,72,65,58,118,60,146,72,
        48,70,80,76,132,145,150,60,62,44,
        48,78,92,88,60,70,160,60,255,38,
        70,88,168,82,60,78,176,84,78,50,
        58,72,94,80,64,56,100,86,78,48,
        55,60,68,58,52,44,88,72,118,70
    };

    private static readonly ushort[] humanDigit0To9 =
    {
        30,96,108,135,98,112,96,190,104,131
    };

    private static readonly ushort[] antiHumanDigit0To9 =
    {
        190,82,76,45,84,70,86,30,78,50
    };

    private static readonly ushort[] prefixHumanPick1To100 = BuildPrefix(humanPick1To100);
    private static readonly ushort[] prefixHumanLeastPicked1To100 = BuildPrefix(humanLeastPicked1To100);
    private static readonly ushort[] prefixHumanDigit0To9 = BuildPrefix(humanDigit0To9);
    private static readonly ushort[] prefixAntiHumanDigit0To9 = BuildPrefix(antiHumanDigit0To9);

    public static uint Hash(uint seed, int a)
    {
        uint h = seed ^ FnvOffset;
        h = (h ^ (uint)a) * FnvPrime;
        h ^= h >> 16;
        h *= 2246822519u;
        h ^= h >> 13;
        h *= 3266489917u;
        h ^= h >> 16;
        return h;
    }

    public static uint Hash(uint seed, int a, int b)
    {
        uint h = seed ^ FnvOffset;
        h = (h ^ (uint)a) * FnvPrime;
        h = (h ^ (uint)b) * FnvPrime;
        h ^= h >> 16;
        h *= 2246822519u;
        h ^= h >> 13;
        h *= 3266489917u;
        h ^= h >> 16;
        return h;
    }

    public static uint Hash(uint seed, int a, int b, int c)
    {
        uint h = seed ^ FnvOffset;
        h = (h ^ (uint)a) * FnvPrime;
        h = (h ^ (uint)b) * FnvPrime;
        h = (h ^ (uint)c) * FnvPrime;
        h ^= h >> 16;
        h *= 2246822519u;
        h ^= h >> 13;
        h *= 3266489917u;
        h ^= h >> 16;
        return h;
    }

    public static float Value01(uint seed, int x)
    {
        return (Hash(seed, x) & 0x00FFFFFFu) / 16777215f;
    }

    public static float Value01(uint seed, int x, int y)
    {
        return (Hash(seed, x, y) & 0x00FFFFFFu) / 16777215f;
    }

    public static float ValueSigned(uint seed, int x, int y)
    {
        return Value01(seed, x, y) * 2f - 1f;
    }

    public static int Pick1To100(uint seed, int salt = 0)
    {
        return PickWeightedOneBased(prefixHumanPick1To100, Hash(seed, salt));
    }

    public static int PickLeastPicked1To100(uint seed, int salt = 0)
    {
        return PickWeightedOneBased(prefixHumanLeastPicked1To100, Hash(seed, salt));
    }

    public static int PickDigit(uint seed, int salt = 0)
    {
        return PickWeightedZeroBased(prefixHumanDigit0To9, Hash(seed, salt));
    }

    public static int PickAntiHumanDigit(uint seed, int salt = 0)
    {
        return PickWeightedZeroBased(prefixAntiHumanDigit0To9, Hash(seed, salt));
    }

    public static int Pick(OUTL_HumanRandomMatrix matrix, uint seed, int salt = 0)
    {
        switch (matrix)
        {
            case OUTL_HumanRandomMatrix.HumanLeastPicked1To100: return PickLeastPicked1To100(seed, salt);
            case OUTL_HumanRandomMatrix.HumanDigit0To9: return PickDigit(seed, salt);
            case OUTL_HumanRandomMatrix.AntiHumanDigit0To9: return PickAntiHumanDigit(seed, salt);
            default: return Pick1To100(seed, salt);
        }
    }

    public static float HumanNoise01(uint seed, int x, int z, int octave = 0, float humanBias = 0.35f)
    {
        humanBias = Mathf.Clamp01(humanBias);
        float uniform = Value01(seed + (uint)(octave * 1013), x, z);
        int human = Pick1To100(Hash(seed, x, z), octave);
        float human01 = (human - 1) / 99f;
        return Mathf.Lerp(uniform, human01, humanBias);
    }

    public static float HumanRidgedNoise01(uint seed, int x, int z, int octave = 0, float humanBias = 0.35f)
    {
        float v = HumanNoise01(seed, x, z, octave, humanBias);
        return 1f - Mathf.Abs(v * 2f - 1f);
    }

    public static float HumanHeightSample(uint seed, int x, int z, int octaves = 4, float persistence = 0.5f, float humanBias = 0.25f)
    {
        octaves = Mathf.Clamp(octaves, 1, 8);
        persistence = Mathf.Clamp01(persistence);
        float amplitude = 1f;
        float total = 0f;
        float norm = 0f;

        for (int i = 0; i < octaves; i++)
        {
            int scale = 1 << i;
            float n = HumanNoise01(seed + (uint)(i * 7919), x / scale, z / scale, i, humanBias);
            total += n * amplitude;
            norm += amplitude;
            amplitude *= persistence;
        }

        return norm > 0f ? Mathf.Clamp01(total / norm) : 0f;
    }

    public static bool Chance(uint seed, int salt, float probability)
    {
        return Value01(seed, salt) < Mathf.Clamp01(probability);
    }

    public static bool HumanChance(uint seed, int salt, float probability, float humanBias = 0.25f)
    {
        float u = Value01(seed, salt);
        float h = (Pick1To100(seed, salt) - 1) / 99f;
        return Mathf.Lerp(u, h, Mathf.Clamp01(humanBias)) < Mathf.Clamp01(probability);
    }

    private static int PickWeightedOneBased(ushort[] prefix, uint hash)
    {
        return PickWeightedZeroBased(prefix, hash) + 1;
    }

    private static int PickWeightedZeroBased(ushort[] prefix, uint hash)
    {
        if (prefix == null || prefix.Length == 0) return 0;
        int total = prefix[prefix.Length - 1];
        if (total <= 0) return 0;
        int roll = (int)(hash % (uint)total);
        int lo = 0;
        int hi = prefix.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi) >> 1;
            if (roll < prefix[mid]) hi = mid;
            else lo = mid + 1;
        }
        return lo;
    }

    private static ushort[] BuildPrefix(ushort[] weights)
    {
        ushort[] prefix = new ushort[weights.Length];
        int total = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            total += weights[i];
            if (total > ushort.MaxValue) total = ushort.MaxValue;
            prefix[i] = (ushort)total;
        }
        return prefix;
    }
}
