using System.Collections.Generic;
using UnityEngine;

public static class OUT_FactionIncidentMemory
{
    private struct Pair
    {
        public int A;
        public int B;

        public Pair(int a, int b)
        {
            A = a;
            B = b;
        }
    }

    private static readonly HashSet<Pair> pairs = new HashSet<Pair>();

    public static void RememberIncident(OUT_FactionAgent observer, OUT_FactionAgent source)
    {
        if (observer == null || source == null || observer == source)
            return;

        pairs.Add(new Pair(observer.GetInstanceID(), source.GetInstanceID()));
    }

    public static bool HasIncident(OUT_FactionAgent observer, OUT_FactionAgent candidate)
    {
        if (observer == null || candidate == null)
            return false;

        return pairs.Contains(new Pair(observer.GetInstanceID(), candidate.GetInstanceID()));
    }

    public static void Clear()
    {
        pairs.Clear();
    }
}
