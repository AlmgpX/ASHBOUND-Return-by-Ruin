using System.Text;
using UnityEngine;

public static class OUT_ThoughtEngine
{
    private static readonly StringBuilder Builder = new StringBuilder(1024);

    public static string Generate(OUT_EntityMind mind, int depth = 2, OUT_ThoughtTemplateSet templateSet = null)
    {
        if (mind == null || mind.Memory == null)
            return string.Empty;

        OUT_AIEntityMemory memory = mind.Memory;
        OUT_EntityMindProfile profile = mind.Profile != null ? mind.Profile : memory.Profile;
        OUT_MoodState mood = memory.Mood;

        OUT_MemoryEvent recent;
        bool hasRecent = memory.TryGetRecentEvent(0, out recent);

        Builder.Length = 0;
        string name = mind.gameObject.name;
        string role = profile != null ? profile.RoleName : "Entity";
        string eventText = hasRecent ? DescribeEvent(recent) : "nothing important";

        if (templateSet != null)
        {
            Builder.Append(ApplyTokens(templateSet.HeaderTemplate, name, role, eventText, string.Empty));
            Builder.AppendLine();
        }
        else
        {
            Builder.Append(name).Append(" (").Append(role).Append(") processed ").Append(eventText).Append('.').AppendLine();
        }

        int safeDepth = Mathf.Clamp(depth, 1, 3);
        for (int i = 0; i < safeDepth; i++)
        {
            OUT_MemoryEvent e;
            bool ok = memory.TryGetRecentEvent(i, out e);
            OUT_SignalChannelFlags channels = ok ? e.Channels : OUT_SignalChannelFlags.None;
            int seed = BuildSeed(name, i, ok ? e.Payload : 0, channels);
            string line = BuildLine(profile, mood, channels, ok ? e.Intensity : 0f, seed);

            if (templateSet != null)
            {
                string template = PickTemplate(templateSet, mood, channels);
                Builder.AppendLine(ApplyTokens(template, name, role, ok ? DescribeEvent(e) : eventText, line));
            }
            else
            {
                Builder.Append(name).Append(" thinks: ").Append(line).Append('.').AppendLine();
            }
        }

        return Builder.ToString();
    }

    private static string BuildLine(OUT_EntityMindProfile profile, OUT_MoodState mood, OUT_SignalChannelFlags channels, float intensity, int seed)
    {
        string lexicon = profile != null ? profile.PickLexiconLine(channels, seed) : "оценивает ситуацию";
        float pleasure = profile != null ? profile.GetPleasureAffinity(channels) * intensity : 0f;
        float threat = profile != null ? profile.GetThreatAffinity(channels) * intensity : 0f;
        float order = profile != null ? profile.GetOrderAffinity(channels) * intensity : 0f;

        if (mood.IsPanicking && threat >= pleasure)
            return lexicon + ", но страх уже давит на решение";

        if (pleasure > threat && pleasure > order && pleasure > 0.25f)
        {
            if ((channels & OUT_SignalChannelFlags.Attraction) != 0)
                return lexicon + ", притяжение спорит с осторожностью";
            if ((channels & OUT_SignalChannelFlags.Food) != 0)
                return lexicon + ", голод пытается вырубить остальные мысли";
            if ((channels & OUT_SignalChannelFlags.Treasure) != 0)
                return lexicon + ", жадность уже считает маршрут";
            return lexicon + ", награда слишком близко";
        }

        if (order > pleasure && order > threat && order > 0.25f)
            return lexicon + ", символический порядок сильнее импульса";

        if (mood.Fear > mood.Aggression && mood.Fear > 0.45f)
            return lexicon + ", держаться ближе к укрытию";

        if (mood.Aggression > mood.Fear && mood.Aggression > 0.45f)
            return lexicon + ", цель надо подавить";

        if (mood.Curiosity > 0.45f)
            return lexicon + ", источник надо проверить";

        if (profile != null && profile.Discipline > 0.65f)
            return lexicon + ", порядок важнее паники";

        return lexicon;
    }

    private static string PickTemplate(OUT_ThoughtTemplateSet set, OUT_MoodState mood, OUT_SignalChannelFlags channels)
    {
        if (mood.IsPanicking)
            return set.PanicTemplate;
        if ((channels & OUT_SignalChannelFlags.Fear) != 0 || mood.Fear > 0.5f)
            return set.FearTemplate;
        if ((channels & OUT_SignalChannelFlags.Aggression) != 0 || mood.Aggression > 0.5f)
            return set.AggressionTemplate;
        if ((channels & OUT_SignalChannelFlags.Curiosity) != 0 || mood.Curiosity > 0.5f)
            return set.CuriosityTemplate;
        return set.LowTensionTemplate;
    }

    private static string ApplyTokens(string template, string name, string role, string eventText, string line)
    {
        if (string.IsNullOrEmpty(template))
            return line;

        return template
            .Replace("{name}", name)
            .Replace("{role}", role)
            .Replace("{event}", eventText)
            .Replace("{line}", line);
    }

    private static string DescribeEvent(in OUT_MemoryEvent e)
    {
        if (!string.IsNullOrEmpty(e.Label))
            return e.Label;

        switch (e.Kind)
        {
            case OUT_MemoryEventKind.SawEnemy:
                return "enemy contact";
            case OUT_MemoryEventKind.HeardNoise:
                return "noise";
            case OUT_MemoryEventKind.SawCorpse:
                return "corpse";
            case OUT_MemoryEventKind.TookDamage:
                return "pain";
            case OUT_MemoryEventKind.SawValuable:
                return "valuable object";
            case OUT_MemoryEventKind.FeltAttraction:
                return "attraction";
            case OUT_MemoryEventKind.FoundFood:
                return "food";
            case OUT_MemoryEventKind.FoundShelter:
                return "shelter";
            case OUT_MemoryEventKind.ReceivedSignal:
                return "signal " + e.Channels;
            case OUT_MemoryEventKind.SentSignal:
                return "sent signal " + e.Channels;
            default:
                return e.Kind.ToString();
        }
    }

    private static int BuildSeed(string name, int index, int payload, OUT_SignalChannelFlags channels)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (name != null ? name.GetHashCode() : 0);
            hash = hash * 31 + index;
            hash = hash * 31 + payload;
            hash = hash * 31 + (int)channels;
            return hash;
        }
    }
}
