using UnityEngine;

public enum OUTL_CycleElement5 : byte
{
    Wood = 0,
    Fire = 1,
    Earth = 2,
    Metal = 3,
    Water = 4
}

public enum OUTL_CycleBranch12 : byte
{
    Rat = 0,
    Ox = 1,
    Tiger = 2,
    Rabbit = 3,
    Dragon = 4,
    Snake = 5,
    Horse = 6,
    Goat = 7,
    Monkey = 8,
    Rooster = 9,
    Dog = 10,
    Pig = 11
}

public enum OUTL_CycleKind : byte
{
    Binary2 = 2,
    Heptadic7 = 7,
    Enneadic9 = 9,
    Stem10 = 10,
    Solar11 = 11,
    Branch12 = 12,
    Metonic19 = 19,
    Magnetic22 = 22,
    Lunar28 = 28,
    Sexagenary60 = 60
}

public struct OUTL_YearCycleState
{
    public int Year;
    public int DigitalRoot9;
    public int Heptad7;
    public int Solar11;
    public int Magnetic22;
    public int Metonic19;
    public int Lunar28;
    public int Stem10;
    public OUTL_CycleElement5 Element5;
    public OUTL_CycleBranch12 Branch12;
    public int Sexagenary60;
    public int AhmesRoot9;
    public int CompositeKey;

    public bool IsBranch(OUTL_CycleBranch12 branch)
    {
        return Branch12 == branch;
    }

    public bool IsElement(OUTL_CycleElement5 element)
    {
        return Element5 == element;
    }
}

public static class OUTL_CycleMatrix
{
    public const int DefaultSexagenaryEpoch = 4;       // year 4 CE aligns to Jia-Zi style index 0 in the common modular formula.
    public const int DefaultSolar11Epoch = 2008;       // symbolic solar minimum anchor, not an ephemeris.
    public const int DefaultMetonicEpoch = 2008;       // symbolic lunar-solar anchor.
    public const int DefaultLunar28Epoch = 2001;       // symbolic 28-phase anchor.

    public const int SnakeBranchIndex = 5;

    public static readonly string[] Branch12Names =
    {
        "Rat", "Ox", "Tiger", "Rabbit", "Dragon", "Snake", "Horse", "Goat", "Monkey", "Rooster", "Dog", "Pig"
    };

    public static readonly string[] Element5Names =
    {
        "Wood", "Fire", "Earth", "Metal", "Water"
    };

    public static int PositiveModulo(int value, int modulus)
    {
        modulus = Mathf.Max(1, modulus);
        int r = value % modulus;
        return r < 0 ? r + modulus : r;
    }

    public static int OneBasedPhase(int value, int period, int epoch = 0)
    {
        return PositiveModulo(value - epoch, period) + 1;
    }

    public static int ZeroBasedPhase(int value, int period, int epoch = 0)
    {
        return PositiveModulo(value - epoch, period);
    }

    public static OUTL_CycleBranch12 Branch12(int year)
    {
        return (OUTL_CycleBranch12)PositiveModulo(year - DefaultSexagenaryEpoch, 12);
    }

    public static int Stem10(int year)
    {
        return PositiveModulo(year - DefaultSexagenaryEpoch, 10);
    }

    public static OUTL_CycleElement5 Element5(int year)
    {
        return (OUTL_CycleElement5)(Stem10(year) >> 1);
    }

    public static int Sexagenary60(int year)
    {
        return PositiveModulo(year - DefaultSexagenaryEpoch, 60);
    }

    public static bool IsSnakeYear(int year)
    {
        return Branch12(year) == OUTL_CycleBranch12.Snake;
    }

    public static int NextBranchYear(int year, OUTL_CycleBranch12 branch, bool includeCurrent = true)
    {
        int current = (int)Branch12(year);
        int target = (int)branch;
        int delta = PositiveModulo(target - current, 12);
        if (delta == 0 && !includeCurrent) delta = 12;
        return year + delta;
    }

    public static int PreviousBranchYear(int year, OUTL_CycleBranch12 branch, bool includeCurrent = true)
    {
        int current = (int)Branch12(year);
        int target = (int)branch;
        int delta = PositiveModulo(current - target, 12);
        if (delta == 0 && !includeCurrent) delta = 12;
        return year - delta;
    }

    public static OUTL_YearCycleState BuildYearState(int year, int salt = 0)
    {
        OUTL_YearCycleState s = new OUTL_YearCycleState();
        s.Year = year;
        s.DigitalRoot9 = OUTL_NumericMatrix.DigitalRoot9(year);
        s.Heptad7 = OneBasedPhase(year, 7, salt);
        s.Solar11 = OneBasedPhase(year, 11, DefaultSolar11Epoch + salt);
        s.Magnetic22 = OneBasedPhase(year, 22, DefaultSolar11Epoch + salt);
        s.Metonic19 = OneBasedPhase(year, 19, DefaultMetonicEpoch + salt);
        s.Lunar28 = OneBasedPhase(year, 28, DefaultLunar28Epoch + salt);
        s.Stem10 = Stem10(year);
        s.Element5 = Element5(year);
        s.Branch12 = Branch12(year);
        s.Sexagenary60 = Sexagenary60(year) + 1;
        s.AhmesRoot9 = OUTL_NumericMatrix.AhmesDigitalRoot(s.DigitalRoot9 == 0 ? 9 : s.DigitalRoot9, OneBasedPhase(year, 9, salt));
        s.CompositeKey = CompositeYearKey(year, salt);
        return s;
    }

    public static int CompositeYearKey(int year, int salt = 0)
    {
        OUTL_CycleBranch12 branch = Branch12(year);
        OUTL_CycleElement5 element = Element5(year);
        int root = OUTL_NumericMatrix.DigitalRoot9(year + salt);
        int h7 = OneBasedPhase(year, 7, salt);
        int s11 = OneBasedPhase(year, 11, DefaultSolar11Epoch + salt);
        int m19 = OneBasedPhase(year, 19, DefaultMetonicEpoch + salt);
        int key = root;
        key = key * 13 + (int)branch + 1;
        key = key * 7 + (int)element + 1;
        key = key * 11 + h7;
        key = key * 17 + s11;
        key = key * 19 + m19;
        return key < 0 ? -key : key;
    }

    public static float Cycle01(int value, int period, int epoch = 0)
    {
        period = Mathf.Max(1, period);
        return period <= 1 ? 0f : PositiveModulo(value - epoch, period) / (float)(period - 1);
    }

    public static float CycleWaveSigned(int value, int period, int epoch = 0)
    {
        float phase = Cycle01(value, period, epoch) * OUTL_GeometryConstants.Tau;
        return Mathf.Sin(phase);
    }

    public static float YearResonance01(int year, int salt = 0)
    {
        float r9 = (OUTL_NumericMatrix.DigitalRoot9(year + salt) - 1f) / 8f;
        float h7 = Cycle01(year, 7, salt);
        float b12 = Cycle01(year, 12, DefaultSexagenaryEpoch);
        float s11 = Cycle01(year, 11, DefaultSolar11Epoch + salt);
        float m19 = Cycle01(year, 19, DefaultMetonicEpoch + salt);
        return Mathf.Clamp01((r9 * 0.30f) + (h7 * 0.18f) + (b12 * 0.22f) + (s11 * 0.15f) + (m19 * 0.15f));
    }

    public static bool EventGate(uint seed, int year, int entityId, int eventSalt, float probability, float cycleBias = 0.55f)
    {
        probability = Mathf.Clamp01(probability);
        cycleBias = Mathf.Clamp01(cycleBias);
        float random = OUTL_HumanRandom.Value01(seed, entityId, eventSalt);
        float cycle = YearResonance01(year, eventSalt + entityId);
        return Mathf.Lerp(random, cycle, cycleBias) <= probability;
    }

    public static float StructuredCycleHeight(uint seed, int x, int z, int year, OUTL_NumericMatrixMode matrixMode = OUTL_NumericMatrixMode.AhmesRoot9, float cycleBias = 0.35f, float matrixBias = 0.45f)
    {
        float baseHeight = OUTL_NumericMatrix.StructuredHeight(seed, x, z, matrixMode, 4, 0.5f, matrixBias);
        int key = CompositeYearKey(year, x + z);
        float cycle = OUTL_HumanRandom.Value01(seed ^ (uint)key, x + key, z - key);
        float resonance = YearResonance01(year, key);
        float structured = Mathf.Lerp(cycle, resonance, 0.5f);
        return Mathf.Clamp01(Mathf.Lerp(baseHeight, structured, Mathf.Clamp01(cycleBias)));
    }

    public static int[] FillBranchYears(int startYearInclusive, int endYearInclusive, OUTL_CycleBranch12 branch, int[] buffer)
    {
        if (buffer == null) return null;
        int count = FillBranchYearsNonAlloc(startYearInclusive, endYearInclusive, branch, buffer);
        for (int i = count; i < buffer.Length; i++) buffer[i] = 0;
        return buffer;
    }

    public static int FillBranchYearsNonAlloc(int startYearInclusive, int endYearInclusive, OUTL_CycleBranch12 branch, int[] buffer)
    {
        if (buffer == null || buffer.Length == 0) return 0;
        if (endYearInclusive < startYearInclusive)
        {
            int t = startYearInclusive;
            startYearInclusive = endYearInclusive;
            endYearInclusive = t;
        }

        int y = NextBranchYear(startYearInclusive, branch, true);
        int count = 0;
        while (y <= endYearInclusive && count < buffer.Length)
        {
            buffer[count++] = y;
            y += 12;
        }
        return count;
    }

    public static string DescribeYear(int year)
    {
        OUTL_YearCycleState s = BuildYearState(year);
        return year + " root9=" + s.DigitalRoot9 + " branch12=" + Branch12Names[(int)s.Branch12] + " element5=" + Element5Names[(int)s.Element5] + " sexagenary=" + s.Sexagenary60 + " solar11=" + s.Solar11 + " magnetic22=" + s.Magnetic22 + " metonic19=" + s.Metonic19;
    }
}
