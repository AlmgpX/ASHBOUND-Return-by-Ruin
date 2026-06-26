namespace MediaRelic.Domain;

public readonly record struct SilenceHit(double Start, double End)
{
    public double Duration => Math.Max(0.0, End - Start);
}

public readonly record struct TimeRange(double Start, double End)
{
    public double Duration => Math.Max(0.0, End - Start);
}

public static class SegmentBuilder
{
    public static List<TimeRange> BuildSoundRanges(
        IReadOnlyList<SilenceHit> silences,
        double totalDuration,
        double minSegmentDuration)
    {
        var result = new List<TimeRange>();
        var cursor = 0.0;

        foreach (var silence in silences.OrderBy(s => s.Start))
        {
            var end = Clamp(silence.Start, 0.0, totalDuration);

            if (end - cursor >= minSegmentDuration)
                result.Add(new TimeRange(cursor, end));

            cursor = Math.Max(cursor, Clamp(silence.End, 0.0, totalDuration));
        }

        if (totalDuration - cursor >= minSegmentDuration)
            result.Add(new TimeRange(cursor, totalDuration));

        return result;
    }

    private static double Clamp(double value, double min, double max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
