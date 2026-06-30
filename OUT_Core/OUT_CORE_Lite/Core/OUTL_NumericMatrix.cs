using UnityEngine;

public enum OUTL_NumberSystemMode : byte
{
    Binary = 2,
    Decimal = 10,
    Duodecimal = 12
}

public enum OUTL_NumericMatrixMode : byte
{
    DigitalRoot9 = 0,
    Modulo12 = 1,
    Modulo7 = 2,
    AhmesRoot9 = 3,
    AhmesModulo12 = 4,
    AhmesModulo7 = 5,
    EulerIdentityField = 6
}

public static class OUTL_NumericMatrix
{
    public const int AhmesSize = 9;
    public const int DecimalCycle = 10;
    public const int EnneadicCycle = 9;
    public const int HeptadicCycle = 7;
    public const int DuodecimalCycle = 12;
    public const int BinaryCycle = 2;

    public static readonly byte[,] AhmesDigitalRoot9 = BuildAhmesDigitalRoot9();
    public static readonly byte[,] AhmesModulo12 = BuildAhmesModulo(12);
    public static readonly byte[,] AhmesModulo7 = BuildAhmesModulo(7);

    public static int DigitalRoot9(int value)
    {
        int v = AbsSafe(value);
        if (v == 0) return 0;
        return 1 + ((v - 1) % 9);
    }

    public static int DigitalRoot(int value, int radixMinusOne)
    {
        int modulus = Mathf.Max(1, radixMinusOne);
        int v = AbsSafe(value);
        if (v == 0) return 0;
        return 1 + ((v - 1) % modulus);
    }

    public static int TheosophicSum(int value, int radix = 10)
    {
        int baseMinusOne = Mathf.Max(1, radix - 1);
        return DigitalRoot(value, baseMinusOne);
    }

    public static int ModularResidue(int value, int modulus)
    {
        modulus = Mathf.Max(1, modulus);
        int r = value % modulus;
        return r < 0 ? r + modulus : r;
    }

    public static int OneBasedResidue(int value, int modulus)
    {
        modulus = Mathf.Max(1, modulus);
        int r = ModularResidue(value, modulus);
        return r == 0 ? modulus : r;
    }

    public static int AhmesPower(int number1To9, int power1To9)
    {
        int n = Mathf.Clamp(number1To9, 1, 9);
        int p = Mathf.Clamp(power1To9, 1, 9);
        int result = 1;
        for (int i = 0; i < p; i++) result *= n;
        return result;
    }

    public static int AhmesDigitalRoot(int number1To9, int power1To9)
    {
        return AhmesDigitalRoot9[Mathf.Clamp(power1To9, 1, 9) - 1, Mathf.Clamp(number1To9, 1, 9) - 1];
    }

    public static int AhmesResidue(int number1To9, int power1To9, int modulus)
    {
        return OneBasedResidue(AhmesPower(number1To9, power1To9), modulus);
    }

    public static int MatrixValue(OUTL_NumericMatrixMode mode, int x, int z)
    {
        int a = OneBasedResidue(x, 9);
        int b = OneBasedResidue(z, 9);
        switch (mode)
        {
            case OUTL_NumericMatrixMode.Modulo12: return OneBasedResidue(x + z, 12);
            case OUTL_NumericMatrixMode.Modulo7: return OneBasedResidue(x + z, 7);
            case OUTL_NumericMatrixMode.AhmesRoot9: return AhmesDigitalRoot(a, b);
            case OUTL_NumericMatrixMode.AhmesModulo12: return AhmesResidue(a, b, 12);
            case OUTL_NumericMatrixMode.AhmesModulo7: return AhmesResidue(a, b, 7);
            case OUTL_NumericMatrixMode.EulerIdentityField: return EulerIdentityGate(x, z);
            default: return DigitalRoot9(x + z);
        }
    }

    public static float Matrix01(OUTL_NumericMatrixMode mode, int x, int z)
    {
        int v = MatrixValue(mode, x, z);
        int max = MaxForMode(mode);
        return max > 1 ? (v - 1f) / (max - 1f) : 0f;
    }

    public static float MatrixSigned(OUTL_NumericMatrixMode mode, int x, int z)
    {
        return Matrix01(mode, x, z) * 2f - 1f;
    }

    public static float StructuredHeight(uint seed, int x, int z, OUTL_NumericMatrixMode mode = OUTL_NumericMatrixMode.AhmesRoot9, int octaves = 4, float persistence = 0.5f, float matrixBias = 0.45f)
    {
        octaves = Mathf.Clamp(octaves, 1, 8);
        persistence = Mathf.Clamp01(persistence);
        matrixBias = Mathf.Clamp01(matrixBias);

        float amp = 1f;
        float total = 0f;
        float norm = 0f;

        for (int i = 0; i < octaves; i++)
        {
            int scale = 1 << i;
            int sx = FloorDiv(x, scale);
            int sz = FloorDiv(z, scale);
            float h = OUTL_HumanRandom.Value01(seed + (uint)(i * 4099), sx, sz);
            float m = Matrix01(mode, sx + (int)(seed & 31u), sz + i * 17);
            total += Mathf.Lerp(h, m, matrixBias) * amp;
            norm += amp;
            amp *= persistence;
        }

        return norm > 0f ? Mathf.Clamp01(total / norm) : 0f;
    }

    public static bool EventGate(uint seed, int entityId, int eventSalt, OUTL_NumericMatrixMode mode, float probability, float matrixBias = 0.5f)
    {
        probability = Mathf.Clamp01(probability);
        matrixBias = Mathf.Clamp01(matrixBias);
        float random = OUTL_HumanRandom.Value01(seed, entityId, eventSalt);
        float matrix = Matrix01(mode, entityId, eventSalt);
        float value = Mathf.Lerp(random, matrix, matrixBias);
        return value <= probability;
    }

    public static int RiskCost(int jumps, int compensationPeriod = 10, int compensatedJumps = 9)
    {
        compensationPeriod = Mathf.Max(1, compensationPeriod);
        compensatedJumps = Mathf.Clamp(compensatedJumps, 0, compensationPeriod);
        int full = Mathf.Max(0, jumps) / compensationPeriod;
        int paidBack = full * compensatedJumps;
        return Mathf.Max(0, jumps - paidBack);
    }

    public static int RiskRoot(int jumps, int compensationPeriod = 10, int compensatedJumps = 9)
    {
        return DigitalRoot9(RiskCost(jumps, compensationPeriod, compensatedJumps));
    }

    public static int EulerIdentityGate(int x, int z)
    {
        int a = DigitalRoot9(x);
        int b = DigitalRoot9(z);
        int e = DigitalRoot9((a * a) + (b * b) + 1);
        int pi = DigitalRoot9(314159 + x - z);
        return DigitalRoot9(e + pi + 1);
    }

    public static int MaxForMode(OUTL_NumericMatrixMode mode)
    {
        switch (mode)
        {
            case OUTL_NumericMatrixMode.Modulo12:
            case OUTL_NumericMatrixMode.AhmesModulo12: return 12;
            case OUTL_NumericMatrixMode.Modulo7:
            case OUTL_NumericMatrixMode.AhmesModulo7: return 7;
            default: return 9;
        }
    }

    private static byte[,] BuildAhmesDigitalRoot9()
    {
        byte[,] m = new byte[9, 9];
        for (int p = 1; p <= 9; p++)
            for (int n = 1; n <= 9; n++)
                m[p - 1, n - 1] = (byte)DigitalRoot9(PowInt(n, p));
        return m;
    }

    private static byte[,] BuildAhmesModulo(int modulus)
    {
        byte[,] m = new byte[9, 9];
        for (int p = 1; p <= 9; p++)
            for (int n = 1; n <= 9; n++)
                m[p - 1, n - 1] = (byte)OneBasedResidue(PowInt(n, p), modulus);
        return m;
    }

    private static int PowInt(int value, int power)
    {
        int result = 1;
        for (int i = 0; i < power; i++) result *= value;
        return result;
    }

    private static int FloorDiv(int value, int divisor)
    {
        if (divisor <= 1) return value;
        if (value >= 0) return value / divisor;
        return -((-value + divisor - 1) / divisor);
    }

    private static int AbsSafe(int value)
    {
        return value == int.MinValue ? int.MaxValue : Mathf.Abs(value);
    }
}
