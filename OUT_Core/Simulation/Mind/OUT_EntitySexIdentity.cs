using UnityEngine;

public enum OUT_EntityBiologicalSex
{
    Unspecified = 0,
    Male = 1,
    Female = 2
}

public enum OUT_EntitySexPreference
{
    Unspecified = 0,
    Any = 1,
    Male = 2,
    Female = 3,
    SameSex = 4,
    OppositeSex = 5,
    None = 6
}

[DisallowMultipleComponent]
public class OUT_EntitySexIdentity : MonoBehaviour
{
    [Header("Пол / базовая биологическая маркировка сущности")]
    [Tooltip("Пол сущности для игровых реакций. Unspecified = система не знает и почти не усиливает Attraction по полу.")]
    [SerializeField] private OUT_EntityBiologicalSex biologicalSex = OUT_EntityBiologicalSex.Unspecified;

    [Header("Предпочтение Attraction-сигналов")]
    [Tooltip("Какие цели сильнее цепляют эту сущность, если пришел Attraction-сигнал.")]
    [SerializeField] private OUT_EntitySexPreference preference = OUT_EntitySexPreference.Unspecified;

    [Tooltip("Сила реакции на мужскую цель.")]
    [SerializeField][Range(0f, 3f)] private float maleWeight = 1f;
    [Tooltip("Сила реакции на женскую цель.")]
    [SerializeField][Range(0f, 3f)] private float femaleWeight = 1f;
    [Tooltip("Сила реакции, если пол цели неизвестен.")]
    [SerializeField][Range(0f, 3f)] private float unknownWeight = 0.5f;

    [Tooltip("Множитель, если цель того же пола.")]
    [SerializeField][Range(0f, 3f)] private float sameSexMultiplier = 1f;
    [Tooltip("Множитель, если цель другого пола.")]
    [SerializeField][Range(0f, 3f)] private float oppositeSexMultiplier = 1f;
    [Tooltip("Во сколько раз ослаблять Attraction, если цель не подходит предпочтению.")]
    [SerializeField][Range(0f, 1f)] private float mismatchDamping = 0.2f;

    public OUT_EntityBiologicalSex BiologicalSex => biologicalSex;
    public OUT_EntitySexPreference Preference => preference;

    public float EvaluateAttractionTo(OUT_EntityBiologicalSex targetSex)
    {
        if (preference == OUT_EntitySexPreference.None)
            return 0f;

        float result = GetDirectSexWeight(targetSex);

        if (biologicalSex != OUT_EntityBiologicalSex.Unspecified && targetSex != OUT_EntityBiologicalSex.Unspecified)
            result *= biologicalSex == targetSex ? sameSexMultiplier : oppositeSexMultiplier;

        if (!MatchesPreference(targetSex))
            result *= mismatchDamping;

        return Mathf.Clamp(result, 0f, 3f);
    }

    private float GetDirectSexWeight(OUT_EntityBiologicalSex targetSex)
    {
        switch (targetSex)
        {
            case OUT_EntityBiologicalSex.Male:
                return maleWeight;
            case OUT_EntityBiologicalSex.Female:
                return femaleWeight;
            default:
                return unknownWeight;
        }
    }

    private bool MatchesPreference(OUT_EntityBiologicalSex targetSex)
    {
        if (preference == OUT_EntitySexPreference.Unspecified || preference == OUT_EntitySexPreference.Any)
            return true;

        if (targetSex == OUT_EntityBiologicalSex.Unspecified)
            return preference == OUT_EntitySexPreference.Unspecified;

        switch (preference)
        {
            case OUT_EntitySexPreference.Male:
                return targetSex == OUT_EntityBiologicalSex.Male;
            case OUT_EntitySexPreference.Female:
                return targetSex == OUT_EntityBiologicalSex.Female;
            case OUT_EntitySexPreference.SameSex:
                return biologicalSex != OUT_EntityBiologicalSex.Unspecified && targetSex == biologicalSex;
            case OUT_EntitySexPreference.OppositeSex:
                return biologicalSex != OUT_EntityBiologicalSex.Unspecified && targetSex != biologicalSex;
            default:
                return true;
        }
    }
}
