using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("OUT CORE Lite/Actor/Character Identity")]
public sealed class OUTL_CharacterIdentity : MonoBehaviour, OUTL_IComponentSaveParticipant, OUTL_IPoolReset
{
    public OUTL_EntityAdapter Entity;
    public OUTL_CharacterIdentityProfile Profile;
    public bool GenerateFromStableId = true;

    [Header("Identity")]
    public string GivenName;
    public string FamilyName;
    public string Nickname;
    public string DisplayName;
    public string Role = "npc";
    public string Background;

    [Header("Attributes (0..1)")]
    [Range(0f, 1f)] public float Courage = 0.5f;
    [Range(0f, 1f)] public float Aggression = 0.5f;
    [Range(0f, 1f)] public float Discipline = 0.5f;
    [Range(0f, 1f)] public float Awareness = 0.5f;
    [Range(0f, 1f)] public float Loyalty = 0.5f;
    [Range(0f, 1f)] public float Greed = 0.5f;

    [Header("Runtime")]
    public int GenerationSeed;
    public bool Generated;
    [SerializeField] private string generatedForStableId;

    public string OUTL_SaveKey { get { return "OUTL_CharacterIdentity"; } }
    public string StableIdentityKey { get { return ResolveStableId(); } }

    private void Awake()
    {
        ResolveEntity();
    }

    public void EnsureGenerated()
    {
        ResolveEntity();
        string stableId = ResolveStableId();
        if (Generated && generatedForStableId == stableId) return;
        if (!GenerateFromStableId)
        {
            Generated = true;
            generatedForStableId = stableId;
            RebuildDisplayName();
            return;
        }

        ApplyGeneratedData(Generate(Profile, stableId, Role, Background));
    }

    public void RebuildDisplayName()
    {
        DisplayName = BuildDisplayName(GivenName, FamilyName, Nickname);
    }

    public void OUTL_Capture(OUTL_ComponentSaveWriter writer)
    {
        if (writer == null) return;
        EnsureGenerated();
        Write(writer, CaptureData());
    }

    public void OUTL_Restore(OUTL_ComponentSaveReader reader)
    {
        if (reader == null) return;
        ApplyGeneratedData(new IdentityData
        {
            StableId = reader.GetString("stableId", ResolveStableId()),
            GivenName = reader.GetString("given", GivenName),
            FamilyName = reader.GetString("family", FamilyName),
            Nickname = reader.GetString("nickname", Nickname),
            DisplayName = reader.GetString("display", DisplayName),
            Role = reader.GetString("role", Role),
            Background = reader.GetString("background", Background),
            Courage = reader.GetFloat("courage", Courage),
            Aggression = reader.GetFloat("aggression", Aggression),
            Discipline = reader.GetFloat("discipline", Discipline),
            Awareness = reader.GetFloat("awareness", Awareness),
            Loyalty = reader.GetFloat("loyalty", Loyalty),
            Greed = reader.GetFloat("greed", Greed),
            Seed = reader.GetInt("seed", GenerationSeed)
        });
    }

    public void OUTL_OnPoolSpawn()
    {
        ResolveEntity();
        ClearGeneratedRuntime();
    }

    public void OUTL_OnPoolRelease()
    {
        ClearGeneratedRuntime();
    }

    public static OUTL_ComponentSavePayload BuildInitialPayload(OUTL_CharacterIdentity template, string stableId)
    {
        if (template == null || string.IsNullOrEmpty(stableId)) return null;
        IdentityData data = Generate(template.Profile, stableId, template.Role, template.Background);
        OUTL_ComponentSavePayload payload = new OUTL_ComponentSavePayload { Key = "OUTL_CharacterIdentity" };
        OUTL_ComponentSaveWriter writer = new OUTL_ComponentSaveWriter(payload);
        Write(writer, data);
        return writer.HasData ? payload : null;
    }

    private IdentityData CaptureData()
    {
        return new IdentityData
        {
            StableId = generatedForStableId,
            GivenName = GivenName,
            FamilyName = FamilyName,
            Nickname = Nickname,
            DisplayName = DisplayName,
            Role = Role,
            Background = Background,
            Courage = Courage,
            Aggression = Aggression,
            Discipline = Discipline,
            Awareness = Awareness,
            Loyalty = Loyalty,
            Greed = Greed,
            Seed = GenerationSeed
        };
    }

    private void ApplyGeneratedData(IdentityData data)
    {
        GivenName = data.GivenName ?? string.Empty;
        FamilyName = data.FamilyName ?? string.Empty;
        Nickname = data.Nickname ?? string.Empty;
        DisplayName = data.DisplayName ?? string.Empty;
        Role = data.Role ?? string.Empty;
        Background = data.Background ?? string.Empty;
        Courage = Mathf.Clamp01(data.Courage);
        Aggression = Mathf.Clamp01(data.Aggression);
        Discipline = Mathf.Clamp01(data.Discipline);
        Awareness = Mathf.Clamp01(data.Awareness);
        Loyalty = Mathf.Clamp01(data.Loyalty);
        Greed = Mathf.Clamp01(data.Greed);
        GenerationSeed = data.Seed;
        generatedForStableId = data.StableId ?? ResolveStableId();
        Generated = true;
        if (string.IsNullOrEmpty(DisplayName)) RebuildDisplayName();
    }

    private void ClearGeneratedRuntime()
    {
        if (!GenerateFromStableId) return;
        GivenName = string.Empty;
        FamilyName = string.Empty;
        Nickname = string.Empty;
        DisplayName = string.Empty;
        GenerationSeed = 0;
        generatedForStableId = string.Empty;
        Generated = false;
    }

    private string ResolveStableId()
    {
        if (Entity != null)
        {
            if (Entity.Runtime != null && !string.IsNullOrEmpty(Entity.Runtime.StableId)) return Entity.Runtime.StableId;
            if (!string.IsNullOrEmpty(Entity.StableId)) return Entity.StableId;
        }
        return string.Empty;
    }

    private void ResolveEntity()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
    }

    private static IdentityData Generate(OUTL_CharacterIdentityProfile profile, string stableId, string role, string background)
    {
        string key = string.IsNullOrEmpty(stableId) ? "unbound" : stableId;
        int stableHash = OUTL_WorldCellUtility.StableStringHash(key);
        int profileSeed = profile != null ? profile.Seed : 1976;
        int seed = stableHash ^ profileSeed;
        string given = Pick(profile != null ? profile.GivenNames : null, seed, 0x47495645u);
        string family = Pick(profile != null ? profile.FamilyNames : null, seed, 0x46414D49u);
        float nicknameChance = profile != null ? Mathf.Clamp01(profile.NicknameChance) : 0f;
        string nickname = OUTL_HumanRandom.Value01(0x4E49434Bu, seed, profileSeed) <= nicknameChance
            ? Pick(profile != null ? profile.Nicknames : null, seed, 0x4E414D45u)
            : string.Empty;

        return new IdentityData
        {
            StableId = stableId,
            GivenName = given,
            FamilyName = family,
            Nickname = nickname,
            DisplayName = BuildDisplayName(given, family, nickname),
            Role = role,
            Background = background,
            Courage = GenerateRange(profile != null ? profile.Courage : new Vector2(0.25f, 0.85f), seed, 0x434F5552u),
            Aggression = GenerateRange(profile != null ? profile.Aggression : new Vector2(0.20f, 0.90f), seed, 0x41474752u),
            Discipline = GenerateRange(profile != null ? profile.Discipline : new Vector2(0.20f, 0.80f), seed, 0x44495343u),
            Awareness = GenerateRange(profile != null ? profile.Awareness : new Vector2(0.30f, 0.90f), seed, 0x41574152u),
            Loyalty = GenerateRange(profile != null ? profile.Loyalty : new Vector2(0.15f, 0.85f), seed, 0x4C4F5941u),
            Greed = GenerateRange(profile != null ? profile.Greed : new Vector2(0.10f, 0.90f), seed, 0x47524545u),
            Seed = seed
        };
    }

    private static string Pick(string[] values, int seed, uint salt)
    {
        if (values == null || values.Length == 0) return string.Empty;
        int index = Mathf.Clamp(Mathf.FloorToInt(OUTL_HumanRandom.Value01(salt, seed, values.Length) * values.Length), 0, values.Length - 1);
        return values[index] ?? string.Empty;
    }

    private static float GenerateRange(Vector2 range, int seed, uint salt)
    {
        float min = Mathf.Clamp01(Mathf.Min(range.x, range.y));
        float max = Mathf.Clamp01(Mathf.Max(range.x, range.y));
        return Mathf.Lerp(min, max, OUTL_HumanRandom.Value01(salt, seed, 1));
    }

    private static string BuildDisplayName(string given, string family, string nickname)
    {
        string baseName = ((given ?? string.Empty) + " " + (family ?? string.Empty)).Trim();
        if (string.IsNullOrEmpty(baseName)) baseName = "Unnamed";
        return string.IsNullOrEmpty(nickname) ? baseName : baseName + " \"" + nickname + "\"";
    }

    private static void Write(OUTL_ComponentSaveWriter writer, IdentityData data)
    {
        writer.SetString("stableId", data.StableId);
        writer.SetString("given", data.GivenName);
        writer.SetString("family", data.FamilyName);
        writer.SetString("nickname", data.Nickname);
        writer.SetString("display", data.DisplayName);
        writer.SetString("role", data.Role);
        writer.SetString("background", data.Background);
        writer.SetFloat("courage", data.Courage);
        writer.SetFloat("aggression", data.Aggression);
        writer.SetFloat("discipline", data.Discipline);
        writer.SetFloat("awareness", data.Awareness);
        writer.SetFloat("loyalty", data.Loyalty);
        writer.SetFloat("greed", data.Greed);
        writer.SetInt("seed", data.Seed);
    }

    private struct IdentityData
    {
        public string StableId;
        public string GivenName;
        public string FamilyName;
        public string Nickname;
        public string DisplayName;
        public string Role;
        public string Background;
        public float Courage;
        public float Aggression;
        public float Discipline;
        public float Awareness;
        public float Loyalty;
        public float Greed;
        public int Seed;
    }
}
