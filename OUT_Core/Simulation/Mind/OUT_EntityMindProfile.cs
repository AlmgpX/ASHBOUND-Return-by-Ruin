using UnityEngine;

[CreateAssetMenu(menuName = "OUT/Core/Mind/Entity Mind Profile", fileName = "OUT_EntityMindProfile")]
public class OUT_EntityMindProfile : ScriptableObject
{
    [Header("ИДЕНТИЧНОСТЬ / кто это вообще такое")]
    [Tooltip("Архетип сущности: Soldier, Goblin, Roach, Cultist, Animal. Нужно для нарратива и общей настройки поведения.")]
    [SerializeField] private string archetypeName = "Entity";
    [Tooltip("Роль внутри архетипа: Commander, Rifleman, Thief, Worker, Queen, Victim. Не ломает AI, но помогает профилю и текстам.")]
    [SerializeField] private string roleName = "Unknown";

    [Header("ЮНГИАНСКАЯ ОСЬ / Persona, Shadow, Ego, Self")]
    [Tooltip("Persona: насколько сущность держит социальную маску, строй, приказ, роль. Высоко = дисциплина и поведение по роли.")]
    [SerializeField][Range(0f, 1f)] private float persona = 0.5f;
    [Tooltip("Shadow: вытесненная хищность, жестокость, импульс. Высоко = агрессия и тяга к запретному сильнее тормозов.")]
    [SerializeField][Range(0f, 1f)] private float shadow = 0.35f;
    [Tooltip("Ego: устойчивость Я. Высоко = меньше паники, лучше выбор между страхом, выгодой и приказом.")]
    [SerializeField][Range(0f, 1f)] private float egoStrength = 0.5f;
    [Tooltip("Self: стремление к целостности/смыслу. Высоко = сакральные/сюжетные сигналы важнее тупой еды и шума.")]
    [SerializeField][Range(0f, 1f)] private float selfPull = 0.35f;

    [Header("ЮНГИАНСКИЕ ФУНКЦИИ / как сущность решает")]
    [Tooltip("Thinking: холодная оценка. Высоко = меньше бросается на первый стимул.")]
    [SerializeField][Range(0f, 1f)] private float thinking = 0.5f;
    [Tooltip("Feeling: ценностная оценка. Высоко = сильнее реагирует на социальное, сакральное, привязанности.")]
    [SerializeField][Range(0f, 1f)] private float feeling = 0.45f;
    [Tooltip("Sensation: телесные стимулы. Высоко = еда, секс/attraction, боль, сокровища цепляют сильнее.")]
    [SerializeField][Range(0f, 1f)] private float sensation = 0.45f;
    [Tooltip("Intuition: догадка и паттерны. Высоко = любопытство, подозрение и смысловые сигналы цепляют сильнее.")]
    [SerializeField][Range(0f, 1f)] private float intuition = 0.45f;

    [Header("СТАРЫЕ ВЕСА ЛИЧНОСТИ / оставлены для обратной совместимости")]
    [Tooltip("Агрессия: желание атаковать, давить, подавлять цель.")]
    [SerializeField][Range(0f, 1f)] private float aggression = 0.5f;
    [Tooltip("Трусость: желание выжить, отступать, искать укрытие.")]
    [SerializeField][Range(0f, 1f)] private float cowardice = 0.35f;
    [Tooltip("Паранойя: насколько шум/странность превращаются в угрозу.")]
    [SerializeField][Range(0f, 1f)] private float paranoia = 0.45f;
    [Tooltip("Любопытство: желание проверить неизвестное.")]
    [SerializeField][Range(0f, 1f)] private float curiosity = 0.35f;
    [Tooltip("Лояльность: важность своих, командира, группы.")]
    [SerializeField][Range(0f, 1f)] private float loyalty = 0.5f;
    [Tooltip("Дисциплина: выполнение роли и приказа вместо импульса.")]
    [SerializeField][Range(0f, 1f)] private float discipline = 0.5f;

    [Header("ВЛЕЧЕНИЯ / что вызывает удовольствие, жадность и тупые решения")]
    [Tooltip("Голод: вес еды. Гоблин с высоким Hunger бросится на еду, если нет более сильного сигнала.")]
    [SerializeField][Range(0f, 1f)] private float hunger = 0.35f;
    [Tooltip("Libido / Attraction: вес сексуального/привлекающего объекта. Это игровой сигнал Attraction, не моральный трактат, спасибо.")]
    [SerializeField][Range(0f, 1f)] private float libido = 0.25f;
    [Tooltip("Greed: вес сокровищ, лута, ценных предметов.")]
    [SerializeField][Range(0f, 1f)] private float greed = 0.35f;
    [Tooltip("Comfort: вес убежища, тепла, безопасного места.")]
    [SerializeField][Range(0f, 1f)] private float comfort = 0.25f;
    [Tooltip("Awe: вес сакрального, странного, божественного, сюжетно-значимого.")]
    [SerializeField][Range(0f, 1f)] private float awe = 0.25f;
    [Tooltip("Disgust: вес отвращения/избегания. Полезно для яда, трупов, запретных зон.")]
    [SerializeField][Range(0f, 1f)] private float disgust = 0.25f;

    [Header("ЧУВСТВИТЕЛЬНОСТЬ К СИГНАЛАМ / множители 0..3")]
    [Tooltip("Danger: опасность, угроза, зона смерти.")]
    [SerializeField][Range(0f, 3f)] private float dangerWeight = 1f;
    [Tooltip("Fear: страх, паника, чужой испуг.")]
    [SerializeField][Range(0f, 3f)] private float fearWeight = 1f;
    [Tooltip("Aggression: чужая/своя агрессия, команда атаковать.")]
    [SerializeField][Range(0f, 3f)] private float aggressionWeight = 1f;
    [Tooltip("Curiosity: неизвестное, интересное, подозрительное.")]
    [SerializeField][Range(0f, 3f)] private float curiosityWeight = 1f;
    [Tooltip("Noise: шум. Чем выше, тем сильнее звук превращается в Interest/Danger.")]
    [SerializeField][Range(0f, 3f)] private float noiseWeight = 1f;
    [Tooltip("Death: труп/смерть. Обычно сильно влияет на страх и подозрение.")]
    [SerializeField][Range(0f, 3f)] private float deathWeight = 1.3f;
    [Tooltip("Food: еда, корм, ресурс насыщения.")]
    [SerializeField][Range(0f, 3f)] private float foodWeight = 1f;
    [Tooltip("Reward: награда/наслаждение без конкретного типа.")]
    [SerializeField][Range(0f, 3f)] private float rewardWeight = 1f;
    [Tooltip("Attraction: сексуальная/эстетическая/инстинктивная привлекательность объекта.")]
    [SerializeField][Range(0f, 3f)] private float attractionWeight = 1f;
    [Tooltip("Treasure: сокровища, деньги, ценный лут.")]
    [SerializeField][Range(0f, 3f)] private float treasureWeight = 1f;
    [Tooltip("Shelter: укрытие, дом, безопасная точка.")]
    [SerializeField][Range(0f, 3f)] private float shelterWeight = 1f;
    [Tooltip("Sacred: сакральное, ритуальное, архетипически значимое.")]
    [SerializeField][Range(0f, 3f)] private float sacredWeight = 1f;
    [Tooltip("Aversion: отвращение, яд, мерзость, то, от чего надо отойти.")]
    [SerializeField][Range(0f, 3f)] private float aversionWeight = 1f;
    [Tooltip("Social: свои, группа, иерархия, социальный объект.")]
    [SerializeField][Range(0f, 3f)] private float socialWeight = 1f;

    [Header("КОНФЛИКТ СТИМУЛОВ / кто победит: еда, добыча, страх или приказ")]
    [Tooltip("Насколько опасность подавляет наслаждение. 1 = опасность легко перебивает еду/секс/сокровища.")]
    [SerializeField][Range(0f, 2f)] private float threatOverridesPleasure = 0.9f;
    [Tooltip("Насколько удовольствие перебивает осторожность. 1 = гоблин может проигнорировать страх ради добычи.")]
    [SerializeField][Range(0f, 2f)] private float pleasureOverridesThreat = 0.45f;
    [Tooltip("Насколько Shadow усиливает запретные/хищные стимулы Attraction/Aggression/Reward.")]
    [SerializeField][Range(0f, 2f)] private float shadowDriveAmplifier = 0.75f;

    [Header("ПАМЯТЬ")]
    [Tooltip("Сколько событий хранит ring-buffer. Больше = умнее и дороже, но не делай таракану 128, он не философ.")]
    [SerializeField][Min(4)] private int memoryCapacity = 16;
    [Tooltip("Как быстро mood возвращается к нулю. Больше = быстрее забывает эмоциональный шум.")]
    [SerializeField][Min(0f)] private float memoryDecayPerSecond = 0.02f;

    [Header("ОБРАТНЫЕ СИГНАЛЫ")]
    [Tooltip("Может ли сущность отвечать сигналом на сигнал: испугался -> передал страх; разозлился -> передал агрессию.")]
    [SerializeField] private bool emitBackwardSignals = true;
    [Tooltip("Минимальная сила входящего сигнала, после которой сущность отвечает backward-сигналом.")]
    [SerializeField][Range(0f, 1f)] private float backwardSignalThreshold = 0.35f;
    [Tooltip("Радиус ответного сигнала относительно входящего.")]
    [SerializeField][Min(0f)] private float backwardRadiusScale = 0.65f;
    [Tooltip("Сила ответного сигнала относительно входящего.")]
    [SerializeField][Range(0f, 1f)] private float backwardIntensityScale = 0.55f;
    [Tooltip("Минимальная пауза между ответными сигналами, чтобы стадо не устроило радиоад.")]
    [SerializeField][Min(0f)] private float minBackwardInterval = 0.35f;

    [Header("НАРРАТИВ / русские строки для дебага мыслей")]
    [SerializeField] private string[] neutralOpeners = { "оценивает ситуацию", "перебирает варианты", "держит паузу" };
    [SerializeField] private string[] fearLines = { "лучше не геройствовать", "надо держаться ближе к укрытию", "это пахнет проблемой" };
    [SerializeField] private string[] aggressionLines = { "лучше подавить цель огнем", "надо ударить первым", "сомнения оставим мертвым" };
    [SerializeField] private string[] curiosityLines = { "стоит проверить источник", "надо подойти ближе", "там может быть след" };
    [SerializeField] private string[] disciplineLines = { "сначала доклад, потом действие", "держать сектор", "не ломать строй" };
    [SerializeField] private string[] foodLines = { "еда ближе, чем смысл", "сначала жрать, потом думать", "запах еды режет все остальные мысли" };
    [SerializeField] private string[] attractionLines = { "желание тянет сильнее осторожности", "объект слишком притягателен, чтобы игнорировать", "инстинкт уже пишет план вместо разума" };
    [SerializeField] private string[] treasureLines = { "ценность надо забрать", "блеск важнее шума", "добыча сама себя не украдет" };
    [SerializeField] private string[] sacredLines = { "это похоже на знак", "символ давит на внутренний порядок", "сакральное важнее бытового" };
    [SerializeField] private string[] aversionLines = { "от этого лучше держаться дальше", "мерзость ломает желание подходить", "тело голосует против" };

    public string ArchetypeName => archetypeName;
    public string RoleName => roleName;

    public float Persona => persona;
    public float Shadow => shadow;
    public float EgoStrength => egoStrength;
    public float SelfPull => selfPull;
    public float Thinking => thinking;
    public float Feeling => feeling;
    public float Sensation => sensation;
    public float Intuition => intuition;

    public float Aggression => aggression;
    public float Cowardice => cowardice;
    public float Paranoia => paranoia;
    public float Curiosity => curiosity;
    public float Loyalty => loyalty;
    public float Discipline => discipline;

    public float Hunger => hunger;
    public float Libido => libido;
    public float Greed => greed;
    public float Comfort => comfort;
    public float Awe => awe;
    public float Disgust => disgust;
    public float ThreatOverridesPleasure => threatOverridesPleasure;
    public float PleasureOverridesThreat => pleasureOverridesThreat;
    public float ShadowDriveAmplifier => shadowDriveAmplifier;

    public int MemoryCapacity => Mathf.Max(4, memoryCapacity);
    public float MemoryDecayPerSecond => Mathf.Max(0f, memoryDecayPerSecond);

    public bool EmitBackwardSignals => emitBackwardSignals;
    public float BackwardSignalThreshold => backwardSignalThreshold;
    public float BackwardRadiusScale => backwardRadiusScale;
    public float BackwardIntensityScale => backwardIntensityScale;
    public float MinBackwardInterval => minBackwardInterval;

    public float GetSignalWeight(OUT_SignalChannelFlags channels)
    {
        float weight = 0f;
        int count = 0;

        AddWeightIfPresent(channels, OUT_SignalChannelFlags.Danger, dangerWeight * (0.5f + paranoia + cowardice * 0.5f), ref weight, ref count);
        AddWeightIfPresent(channels, OUT_SignalChannelFlags.Fear, fearWeight * (0.5f + cowardice + paranoia * 0.35f), ref weight, ref count);
        AddWeightIfPresent(channels, OUT_SignalChannelFlags.Aggression, aggressionWeight * (0.5f + aggression + shadow * shadowDriveAmplifier), ref weight, ref count);
        AddWeightIfPresent(channels, OUT_SignalChannelFlags.Curiosity, curiosityWeight * (0.5f + curiosity + intuition * 0.5f), ref weight, ref count);
        AddWeightIfPresent(channels, OUT_SignalChannelFlags.Noise, noiseWeight * (0.5f + paranoia * 0.75f), ref weight, ref count);
        AddWeightIfPresent(channels, OUT_SignalChannelFlags.Death, deathWeight * (0.5f + cowardice + shadow * 0.25f), ref weight, ref count);
        AddWeightIfPresent(channels, OUT_SignalChannelFlags.Food, foodWeight * (0.5f + hunger + sensation * 0.5f), ref weight, ref count);
        AddWeightIfPresent(channels, OUT_SignalChannelFlags.Reward, rewardWeight * (0.5f + sensation + shadow * 0.35f), ref weight, ref count);
        AddWeightIfPresent(channels, OUT_SignalChannelFlags.Attraction, attractionWeight * (0.5f + libido + sensation * 0.35f + shadow * shadowDriveAmplifier), ref weight, ref count);
        AddWeightIfPresent(channels, OUT_SignalChannelFlags.Treasure, treasureWeight * (0.5f + greed + sensation * 0.35f), ref weight, ref count);
        AddWeightIfPresent(channels, OUT_SignalChannelFlags.Shelter, shelterWeight * (0.5f + comfort + cowardice * 0.35f), ref weight, ref count);
        AddWeightIfPresent(channels, OUT_SignalChannelFlags.Sacred, sacredWeight * (0.5f + awe + selfPull + feeling * 0.35f), ref weight, ref count);
        AddWeightIfPresent(channels, OUT_SignalChannelFlags.Aversion, aversionWeight * (0.5f + disgust + intuition * 0.25f), ref weight, ref count);
        AddWeightIfPresent(channels, OUT_SignalChannelFlags.Social, socialWeight * (0.5f + loyalty + feeling * 0.35f + persona * 0.25f), ref weight, ref count);

        return count > 0 ? weight / count : 1f;
    }

    public float EvaluateSignalPriority(OUT_SignalChannelFlags channels, float intensity)
    {
        float weighted = Mathf.Clamp01(intensity * GetSignalWeight(channels));
        float threat = GetThreatAffinity(channels) * (1f + threatOverridesPleasure * (1f - egoStrength));
        float pleasure = GetPleasureAffinity(channels) * (1f + pleasureOverridesThreat + shadow * shadowDriveAmplifier);
        float order = GetOrderAffinity(channels) * (1f + persona + discipline);
        return Mathf.Clamp01(weighted * (0.5f + threat + pleasure + order));
    }

    public float GetPleasureAffinity(OUT_SignalChannelFlags channels)
    {
        float value = 0f;
        if ((channels & OUT_SignalChannelFlags.Food) != 0) value += hunger;
        if ((channels & OUT_SignalChannelFlags.Attraction) != 0) value += libido + shadow * 0.5f;
        if ((channels & OUT_SignalChannelFlags.Treasure) != 0) value += greed;
        if ((channels & OUT_SignalChannelFlags.Reward) != 0) value += Mathf.Max(hunger, libido, greed);
        if ((channels & OUT_SignalChannelFlags.Shelter) != 0) value += comfort;
        return Mathf.Clamp01(value);
    }

    public float GetThreatAffinity(OUT_SignalChannelFlags channels)
    {
        float value = 0f;
        if ((channels & OUT_SignalChannelFlags.Danger) != 0) value += paranoia + cowardice * 0.5f;
        if ((channels & OUT_SignalChannelFlags.Fear) != 0) value += cowardice;
        if ((channels & OUT_SignalChannelFlags.Death) != 0) value += cowardice + paranoia * 0.5f;
        if ((channels & OUT_SignalChannelFlags.Pain) != 0) value += cowardice + sensation * 0.25f;
        if ((channels & OUT_SignalChannelFlags.Fire) != 0) value += cowardice + disgust * 0.5f;
        if ((channels & OUT_SignalChannelFlags.Aversion) != 0) value += disgust;
        return Mathf.Clamp01(value);
    }

    public float GetOrderAffinity(OUT_SignalChannelFlags channels)
    {
        float value = 0f;
        if ((channels & OUT_SignalChannelFlags.Command) != 0) value += discipline + persona;
        if ((channels & OUT_SignalChannelFlags.Help) != 0) value += loyalty + feeling;
        if ((channels & OUT_SignalChannelFlags.Social) != 0) value += loyalty + feeling;
        if ((channels & OUT_SignalChannelFlags.Sacred) != 0) value += awe + selfPull;
        return Mathf.Clamp01(value);
    }

    public string PickLexiconLine(OUT_SignalChannelFlags channels, int seed)
    {
        if ((channels & OUT_SignalChannelFlags.Fear) != 0)
            return Pick(fearLines, seed, "лучше отступить");
        if ((channels & OUT_SignalChannelFlags.Aggression) != 0)
            return Pick(aggressionLines, seed, "надо атаковать");
        if ((channels & OUT_SignalChannelFlags.Food) != 0)
            return Pick(foodLines, seed, "еда важнее философии");
        if ((channels & OUT_SignalChannelFlags.Attraction) != 0)
            return Pick(attractionLines, seed, "желание тянет вперед");
        if ((channels & OUT_SignalChannelFlags.Treasure) != 0)
            return Pick(treasureLines, seed, "ценность надо забрать");
        if ((channels & OUT_SignalChannelFlags.Sacred) != 0)
            return Pick(sacredLines, seed, "это похоже на знак");
        if ((channels & OUT_SignalChannelFlags.Aversion) != 0)
            return Pick(aversionLines, seed, "лучше держаться дальше");
        if ((channels & OUT_SignalChannelFlags.Curiosity) != 0)
            return Pick(curiosityLines, seed, "надо проверить");
        if (discipline >= 0.6f)
            return Pick(disciplineLines, seed, "держать порядок");
        return Pick(neutralOpeners, seed, "оценивает ситуацию");
    }

    private static void AddWeightIfPresent(OUT_SignalChannelFlags channels, OUT_SignalChannelFlags flag, float value, ref float weight, ref int count)
    {
        if ((channels & flag) == 0)
            return;

        weight += value;
        count++;
    }

    private static string Pick(string[] array, int seed, string fallback)
    {
        if (array == null || array.Length == 0)
            return fallback;

        int index = Mathf.Abs(seed) % array.Length;
        return string.IsNullOrWhiteSpace(array[index]) ? fallback : array[index];
    }
}
