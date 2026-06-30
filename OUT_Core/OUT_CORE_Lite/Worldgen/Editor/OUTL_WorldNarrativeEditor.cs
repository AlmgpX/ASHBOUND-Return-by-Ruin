#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class OUTL_WorldNarrativeEditor
{
    private const string MenuRoot = "OUT CORE Lite/Legacy Demo/Worldgen/";
    private const string Folder = "Assets/OUT/OUT_Core/OUT_CORE_Lite/Templates/Worldgen";

    // [MenuItem(MenuRoot + "Create Default Narrative World Config RU")]
    public static void CreateDefaultConfig()
    {
        EnsureFolder(Folder);
        OUTL_WorldNarrativeConfig config = ScriptableObject.CreateInstance<OUTL_WorldNarrativeConfig>();
        config.Language = "ru";
        config.WorldName = Text("world.ashen_vale", "РџРµРїРµР»СЊРЅР°СЏ Р”РѕР»РёРЅР°", "Ashen Vale");
        config.EraName = Text("era.first_smoke", "Р­РїРѕС…Р° РџРµСЂРІРѕРіРѕ Р”С‹РјР°", "Age of First Smoke");
        config.Width = 64;
        config.Height = 48;
        config.Years = 160;
        config.Seed = Random.Range(1, int.MaxValue);
        config.IncludeTileDump = false;
        config.RiverCount = 7;
        config.GenerateRivers = true;

        config.Resources = new[]
        {
            Resource("iron", "Р–РµР»РµР·РЅС‹Рµ Р¶РёР»С‹", "Iron Veins", new[]{ OUTL_WorldZoneType.Hills, OUTL_WorldZoneType.Mountains }, 0.18f, 0.55f, 1f, 0f, 1f, 0f, 0.78f, 0.3f, 1f, "Р’ СЃС‚Р°СЂРѕРј РєР°РјРЅРµ РѕС‚РєСЂС‹Р»РёСЃСЊ Р¶РµР»РµР·РЅС‹Рµ Р¶РёР»С‹.", "Iron was found under the old stone."),
            Resource("salt", "РЎРѕР»СЏРЅС‹Рµ РїР»Р°СЃС‚С‹", "Salt Flats", new[]{ OUTL_WorldZoneType.Desert, OUTL_WorldZoneType.Coast, OUTL_WorldZoneType.Lake }, 0.12f, 0.22f, 0.8f, 0f, 0.55f, 0.45f, 1f, 0.35f, 1f, "РЎРѕР»СЊ РІС‹С€Р»Р° РЅР° РїРѕРІРµСЂС…РЅРѕСЃС‚СЊ С‚Р°Рј, РіРґРµ РІРѕРґР° РѕС‚СЃС‚СѓРїРёР»Р°.", "Salt surfaced where water abandoned the land."),
            Resource("black_wood", "Р§С‘СЂРЅР°СЏ РґСЂРµРІРµСЃРёРЅР°", "Blackwood", new[]{ OUTL_WorldZoneType.Forest, OUTL_WorldZoneType.Swamp, OUTL_WorldZoneType.River }, 0.14f, 0.25f, 0.72f, 0.45f, 1f, 0f, 0.75f, 0f, 0.62f, "РўС‘РјРЅС‹Рµ РґРµСЂРµРІСЊСЏ СѓРєРѕСЂРµРЅРёР»РёСЃСЊ РІРѕ РІР»Р°Р¶РЅРѕР№ Р·РµРјР»Рµ.", "Dark trees took root in wet soil."),
            Resource("old_bones", "РЎС‚Р°СЂС‹Рµ РєРѕСЃС‚Рё", "Old Bones", new[]{ OUTL_WorldZoneType.Ruins, OUTL_WorldZoneType.Wasteland }, 0.25f, 0f, 1f, 0f, 1f, 0f, 1f, 0f, 1f, "РљРѕСЃС‚Рё РїРѕРєР°Р·Р°Р»РёСЃСЊ С‚Р°Рј, РіРґРµ РёСЃС‚РѕСЂРёСЏ РЅРµ СЃРјРѕРіР»Р° РѕСЃС‚Р°С‚СЊСЃСЏ РїРѕРґ Р·РµРјР»С‘Р№.", "Bones were uncovered where history failed to stay buried."),
            Resource("clear_water", "Р§РёСЃС‚Р°СЏ РІРѕРґР°", "Clear Water", new[]{ OUTL_WorldZoneType.Forest, OUTL_WorldZoneType.Hills, OUTL_WorldZoneType.Sacred, OUTL_WorldZoneType.River }, 0.16f, 0.3f, 0.8f, 0.55f, 1f, 0f, 0.75f, 0f, 0.55f, "Р§РёСЃС‚С‹Р№ РёСЃС‚РѕС‡РЅРёРє РїСЂРѕСЂРµР·Р°Р» РїРѕС‡РІСѓ.", "A clear spring cut through the soil."),
            Resource("clay", "Р“Р»РёРЅР°", "Clay", new[]{ OUTL_WorldZoneType.River, OUTL_WorldZoneType.Lake, OUTL_WorldZoneType.Swamp, OUTL_WorldZoneType.Coast }, 0.22f, 0.2f, 0.55f, 0.55f, 1f, 0.15f, 0.9f, 0f, 0.5f, "РќР° РІР»Р°Р¶РЅС‹С… Р±РµСЂРµРіР°С… Р»РµРіР»Рё С‚СЏР¶С‘Р»С‹Рµ РіР»РёРЅС‹.", "Heavy clay settled near wet banks.")
        };

        config.Plants = new[]
        {
            Plant("red_grass", "РљСЂР°СЃРЅР°СЏ С‚СЂР°РІР°", "Red Grass", new[]{ OUTL_WorldZoneType.Plains, OUTL_WorldZoneType.Hills, OUTL_WorldZoneType.Steppe }, 0.2f, 0.3f, 0.75f, 0.25f, 0.7f, 0.25f, 0.8f, 0.25f, 0.75f, "РџРѕСЃР»Рµ С‚С‘РїР»РѕРіРѕ СЃРµР·РѕРЅР° СЂР°Р·РѕС€Р»Р°СЃСЊ РєСЂР°СЃРЅР°СЏ С‚СЂР°РІР°.", "Red grass spread after a warm season."),
            Plant("reed", "Р‘РѕР»РѕС‚РЅС‹Р№ С‚СЂРѕСЃС‚РЅРёРє", "Marsh Reed", new[]{ OUTL_WorldZoneType.Swamp, OUTL_WorldZoneType.Coast, OUTL_WorldZoneType.River, OUTL_WorldZoneType.Lake }, 0.32f, 0.2f, 0.55f, 0.65f, 1f, 0.2f, 0.8f, 0f, 0.45f, "РўСЂРѕСЃС‚РЅРёРє Р·Р°Р±РёР» РјРѕРєСЂС‹Рµ РєСЂР°СЏ РІРѕРґС‹.", "Reeds crowded the wet margins."),
            Plant("ashen_pine", "РџРµРїРµР»СЊРЅР°СЏ СЃРѕСЃРЅР°", "Ashen Pine", new[]{ OUTL_WorldZoneType.Forest, OUTL_WorldZoneType.Mountains, OUTL_WorldZoneType.Tundra }, 0.16f, 0.45f, 1f, 0.35f, 0.85f, 0f, 0.55f, 0.35f, 1f, "РЎРѕСЃРЅС‹ РІС‹Р¶РёР»Рё С‚Р°Рј, РіРґРµ РјСЏРіРєРёРµ РґРµСЂРµРІСЊСЏ РЅРµ РІС‹РґРµСЂР¶Р°Р»Рё.", "Pines survived where gentler trees failed."),
            Plant("glass_cactus", "РЎС‚РµРєР»СЏРЅРЅС‹Р№ РєР°РєС‚СѓСЃ", "Glass Cactus", new[]{ OUTL_WorldZoneType.Desert, OUTL_WorldZoneType.Wasteland }, 0.18f, 0.3f, 0.75f, 0f, 0.35f, 0.62f, 1f, 0.45f, 1f, "РЎС‚РµРєР»СЏРЅРЅС‹Р№ РєР°РєС‚СѓСЃ РїРѕСЏРІРёР»СЃСЏ С‚Р°Рј, РіРґРµ Р¶Р°СЂР° Р·Р°РїРµРєР»Р° Р·РµРјР»СЋ.", "Glass cactus appeared after the heat hardened the ground."),
            Plant("grave_moss", "РњРѕРіРёР»СЊРЅС‹Р№ РјРѕС…", "Grave Moss", new[]{ OUTL_WorldZoneType.Ruins, OUTL_WorldZoneType.Sacred }, 0.22f, 0.2f, 0.8f, 0.4f, 1f, 0f, 0.7f, 0f, 0.65f, "РњРѕРіРёР»СЊРЅС‹Р№ РјРѕС… РѕС‚РјРµС‚РёР» РєР°РјРЅРё, РєРѕС‚РѕСЂС‹Рµ РЅРёРєС‚Рѕ РЅРµ РїСЂРёР·РЅР°РІР°Р» СЂСѓРєРѕС‚РІРѕСЂРЅС‹РјРё.", "Grave moss marked stones nobody admitted placing.")
        };

        config.Events = new[]
        {
            Event("drought", "Р—Р°СЃСѓС…Р°", "Drought", new[]{ OUTL_WorldZoneType.Plains, OUTL_WorldZoneType.Desert, OUTL_WorldZoneType.Hills, OUTL_WorldZoneType.Steppe }, 0.075f, 0f, 0.8f, 0f, 0.45f, 0.55f, 1f, 0.35f, 1f, 0, 999, -2, 1, 0, "РЎСѓС…РѕР№ РіРѕРґ СЂР°СЃС‚СЂРµСЃРєР°Р» Р·РµРјР»СЋ Рё РІС‹СЃС‚Р°РІРёР» РєРѕР»РѕРґС†С‹ Р±РµСЃРїРѕР»РµР·РЅС‹РјРё РґС‹СЂРєР°РјРё.", "A dry year cracked the ground and made fools of wells."),
            Event("plague", "РњРѕСЂ", "Plague", new[]{ OUTL_WorldZoneType.Coast, OUTL_WorldZoneType.Plains, OUTL_WorldZoneType.Swamp, OUTL_WorldZoneType.River }, 0.045f, 0.2f, 0.65f, 0.45f, 1f, 0.25f, 0.9f, 0f, 0.6f, 5, 999, -3, 3, 0, "Р‘РѕР»РµР·РЅСЊ РїСЂРѕС€Р»Р° РЅРёР·РєРёРјРё РґРѕСЂРѕРіР°РјРё Р±С‹СЃС‚СЂРµРµ СЃР»СѓС…РѕРІ.", "A sickness crossed the low roads faster than rumor."),
            Event("pilgrimage", "РџР°Р»РѕРјРЅРёС‡РµСЃС‚РІРѕ", "Pilgrimage", new[]{ OUTL_WorldZoneType.Sacred, OUTL_WorldZoneType.Mountains, OUTL_WorldZoneType.Forest }, 0.055f, 0.35f, 1f, 0.25f, 0.9f, 0f, 0.75f, 0.2f, 1f, 0, 999, 1, 0, 2, "РџР°Р»РѕРјРЅРёРєРё РїРѕС€Р»Рё Р·Р° Р·РЅР°РєРѕРј, РєРѕС‚РѕСЂС‹Р№, РІРµСЂРѕСЏС‚РЅРѕ, РЅРёС‡РµРіРѕ РЅРµ Р·РЅР°С‡РёР». РџРѕСЌС‚РѕРјСѓ РѕРЅ, РєРѕРЅРµС‡РЅРѕ, СЃС‚Р°Р» РІР°Р¶РЅС‹Рј.", "Pilgrims followed a sign that probably meant nothing, so naturally it mattered."),
            Event("bandits", "Р Р°Р·Р±РѕР№РЅР°СЏ Р·РёРјР°", "Bandit Winter", new[]{ OUTL_WorldZoneType.Forest, OUTL_WorldZoneType.Hills, OUTL_WorldZoneType.Ruins, OUTL_WorldZoneType.Steppe }, 0.065f, 0.3f, 0.9f, 0.2f, 0.85f, 0f, 0.85f, 0.2f, 1f, 10, 999, -1, 3, 0, "Р Р°Р·Р±РѕР№РЅРёРєРё РѕСЃРµР»Рё С‚Р°Рј, РіРґРµ РІР»Р°СЃС‚СЊ РёРјРµР»Р° Р±Р»Р°РіРѕСЂР°Р·СѓРјРёРµ РѕС‚СЃСѓС‚СЃС‚РІРѕРІР°С‚СЊ.", "Bandits settled where authority had the good sense to be absent."),
            Event("founding", "РћСЃРЅРѕРІР°РЅРёРµ РїРѕСЃРµР»РµРЅРёСЏ", "Settlement Founded", new[]{ OUTL_WorldZoneType.Coast, OUTL_WorldZoneType.Plains, OUTL_WorldZoneType.Forest, OUTL_WorldZoneType.River, OUTL_WorldZoneType.Lake }, 0.05f, 0.25f, 0.65f, 0.35f, 0.95f, 0.2f, 0.75f, 0f, 0.6f, 0, 999, 4, 1, 0, "Р‘С‹Р»Рѕ РѕСЃРЅРѕРІР°РЅРѕ РїРѕСЃРµР»РµРЅРёРµ Рё СЃСЂР°Р·Сѓ РЅР°С‡Р°Р»Рѕ РґРµР»Р°С‚СЊ РІРёРґ, С‡С‚Рѕ РІСЃРµРіРґР° РёРјРµР»Рѕ РїСЂР°РІРѕ Р·РґРµСЃСЊ СЃС‚РѕСЏС‚СЊ.", "A settlement was founded, and immediately began pretending it had always belonged there."),
            Event("ruin_wakes", "РџСЂРѕР±СѓР¶РґРµРЅРёРµ СЂСѓРёРЅ", "Ruin Wakes", new[]{ OUTL_WorldZoneType.Ruins, OUTL_WorldZoneType.Wasteland }, 0.08f, 0f, 1f, 0f, 1f, 0f, 1f, 0f, 1f, 20, 999, -1, 4, 1, "РџРѕРґ СЂСѓРёРЅР°РјРё С€РµРІРµР»СЊРЅСѓР»РѕСЃСЊ С‡С‚Рѕ-С‚Рѕ СЃС‚Р°СЂРѕРµ. РђСЂС…РёРІ РїРѕС‚РѕРј РЅР°Р·РІР°Р» СЌС‚Рѕ РїСЂРѕСЃР°РґРєРѕР№ РіСЂСѓРЅС‚Р°, РїРѕС‚РѕРјСѓ С‡С‚Рѕ С‚СЂСѓСЃР°Рј С‚РѕР¶Рµ РЅСѓР¶РЅР° С‚РµСЂРјРёРЅРѕР»РѕРіРёСЏ.", "Something old moved under the ruins. The archive later called it subsidence, because cowards need vocabulary.")
        };

        string path = AssetDatabase.GenerateUniqueAssetPath(Folder + "/OUTL_WorldNarrativeConfig_PepelnayaDolina.asset");
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();
        Selection.activeObject = config;
    }

    // [MenuItem(MenuRoot + "Create Author In Scene")]
    public static void CreateAuthorInScene()
    {
        GameObject go = new GameObject("OUTL_WorldNarrativeAuthor");
        Undo.RegisterCreatedObjectUndo(go, "Create OUTL World Narrative Author");
        go.AddComponent<OUTL_WorldNarrativeAuthor>();
        Selection.activeGameObject = go;
    }

    // [MenuItem(MenuRoot + "Generate Selected Narrative Report")]
    public static void GenerateSelectedReport()
    {
        OUTL_WorldNarrativeConfig config = Selection.activeObject as OUTL_WorldNarrativeConfig;
        if (config == null)
        {
            Debug.LogWarning("РЎРЅР°С‡Р°Р»Р° РІС‹РґРµР»Рё OUTL_WorldNarrativeConfig asset.");
            return;
        }
        string path = OUTL_WorldNarrativeGenerator.GenerateAndWrite(config);
        Debug.Log("OUTL narrative report written: " + path);
    }

    private static OUTL_WorldResourceDef Resource(string id, string ru, string en, OUTL_WorldZoneType[] zones, float chance, float minH, float maxH, float minM, float maxM, float minHeat, float maxHeat, float minD, float maxD, string lineRu, string lineEn)
    {
        return new OUTL_WorldResourceDef { Id = id, DisplayName = Text("resource." + id, ru, en), AllowedZones = zones, BaseChance = chance, MinHeight = minH, MaxHeight = maxH, MinMoisture = minM, MaxMoisture = maxM, MinHeat = minHeat, MaxHeat = maxHeat, MinDrainage = minD, MaxDrainage = maxD, DiscoveryLines = new[] { Text("resource." + id + ".discovery", lineRu, lineEn) } };
    }

    private static OUTL_WorldPlantDef Plant(string id, string ru, string en, OUTL_WorldZoneType[] zones, float chance, float minH, float maxH, float minM, float maxM, float minHeat, float maxHeat, float minD, float maxD, string lineRu, string lineEn)
    {
        return new OUTL_WorldPlantDef { Id = id, DisplayName = Text("plant." + id, ru, en), AllowedZones = zones, BaseChance = chance, MinHeight = minH, MaxHeight = maxH, MinMoisture = minM, MaxMoisture = maxM, MinHeat = minHeat, MaxHeat = maxHeat, MinDrainage = minD, MaxDrainage = maxD, SpreadLines = new[] { Text("plant." + id + ".spread", lineRu, lineEn) } };
    }

    private static OUTL_WorldEventDef Event(string id, string ru, string en, OUTL_WorldZoneType[] zones, float chance, float minH, float maxH, float minM, float maxM, float minHeat, float maxHeat, float minD, float maxD, int minYear, int maxYear, int prosperity, int danger, int sanctity, string lineRu, string lineEn)
    {
        return new OUTL_WorldEventDef { Id = id, DisplayName = Text("event." + id, ru, en), AllowedZones = zones, BaseChancePerYear = chance, MinHeight = minH, MaxHeight = maxH, MinMoisture = minM, MaxMoisture = maxM, MinHeat = minHeat, MaxHeat = maxHeat, MinDrainage = minD, MaxDrainage = maxD, MinYear = minYear, MaxYear = maxYear, ProsperityDelta = prosperity, DangerDelta = danger, SanctityDelta = sanctity, Lines = new[] { Text("event." + id + ".line", lineRu, lineEn) } };
    }

    private static OUTL_LocalizedText Text(string key, string ru, string en)
    {
        OUTL_LocalizedText t = new OUTL_LocalizedText();
        t.Key = key;
        t.Variants = new[] { new OUTL_LocalizedString("ru", ru), new OUTL_LocalizedString("en", en) };
        return t;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
