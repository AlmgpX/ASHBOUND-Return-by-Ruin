using UnityEngine;

[CreateAssetMenu(menuName = "OUT CORE Lite/AI/Character Identity Profile", fileName = "OUTL_CharacterIdentityProfile")]
public sealed class OUTL_CharacterIdentityProfile : ScriptableObject
{
    public string ProfileId = "character.generic";
    public int Seed = 1976;
    public string[] GivenNames;
    public string[] FamilyNames;
    public string[] Nicknames;
    [Range(0f, 1f)] public float NicknameChance = 0.18f;

    [Header("Generated attribute ranges")]
    public Vector2 Courage = new Vector2(0.25f, 0.85f);
    public Vector2 Aggression = new Vector2(0.20f, 0.90f);
    public Vector2 Discipline = new Vector2(0.20f, 0.80f);
    public Vector2 Awareness = new Vector2(0.30f, 0.90f);
    public Vector2 Loyalty = new Vector2(0.15f, 0.85f);
    public Vector2 Greed = new Vector2(0.10f, 0.90f);
}
