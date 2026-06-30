#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class OUTL_WorldNarrativeAdvancedEditor
{
    private const string MenuRoot = "OUT CORE Lite/Legacy Demo/Worldgen/Advanced/";
    private const string Folder = "Assets/OUT/OUT_Core/OUT_CORE_Lite/Templates/Worldgen";

    // [MenuItem(MenuRoot + "Add Default Jung Archetypes To Selected Config")]
    public static void AddDefaultArchetypesToSelected()
    {
        OUTL_WorldNarrativeConfig config = Selection.activeObject as OUTL_WorldNarrativeConfig;
        if (config == null)
        {
            Debug.LogWarning("РЎРЅР°С‡Р°Р»Р° РІС‹РґРµР»Рё OUTL_WorldNarrativeConfig.");
            return;
        }
        Undo.RecordObject(config, "Add OUTL Jung Archetypes");
        config.UseJungArchetypes = true;
        config.ArchetypeStrength = 1f;
        config.MythicPressure = Mathf.Max(config.MythicPressure, 1.15f);
        config.Archetypes = DefaultArchetypes();
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        Debug.Log("OUTL: РґРµС„РѕР»С‚РЅС‹Рµ Р°СЂС…РµС‚РёРїС‹ Р®РЅРіР° РґРѕР±Р°РІР»РµРЅС‹ РІ РєРѕРЅС„РёРі.");
    }

    // [MenuItem(MenuRoot + "Generate Selected Report And Textures")]
    public static void GenerateSelectedReportAndTextures()
    {
        OUTL_WorldNarrativeConfig config = Selection.activeObject as OUTL_WorldNarrativeConfig;
        if (config == null)
        {
            Debug.LogWarning("РЎРЅР°С‡Р°Р»Р° РІС‹РґРµР»Рё OUTL_WorldNarrativeConfig.");
            return;
        }
        OUTL_WorldNarrativeWriteResult write = OUTL_WorldNarrativeWriter.GenerateWriteAndExport(config);
        Debug.Log("OUTL worldgen report: " + (write != null ? write.ReportPath : "null") + " textures: " + (write != null ? write.TextureFolder : "null"));
    }

    // [MenuItem(MenuRoot + "Create Russian Jung Texture World Config")]
    public static void CreateRussianJungTextureWorldConfig()
    {
        EnsureFolder(Folder);
        OUTL_WorldNarrativeConfig config = ScriptableObject.CreateInstance<OUTL_WorldNarrativeConfig>();
        config.Language = "ru";
        config.WorldName = Text("world.deep_mirror", "Р“Р»СѓР±РёРЅРЅРѕРµ Р—РµСЂРєР°Р»Рѕ", "Deep Mirror");
        config.EraName = Text("era.inner_weather", "Р­РїРѕС…Р° Р’РЅСѓС‚СЂРµРЅРЅРµР№ РџРѕРіРѕРґС‹", "Age of Inner Weather");
        config.Width = 96;
        config.Height = 64;
        config.Years = 320;
        config.Seed = Random.Range(1, int.MaxValue);
        config.Temperament = OUTL_WorldSimulationTemperament.Mythic;
        config.EventDensity = 1.2f;
        config.ConflictPressure = 1.15f;
        config.GrowthPressure = 1f;
        config.MythicPressure = 1.45f;
        config.ArchetypeStrength = 1.15f;
        config.UseJungArchetypes = true;
        config.Archetypes = DefaultArchetypes();
        config.ComputeVisibility = true;
        config.TextureWidth = 512;
        config.TextureHeight = 512;
        config.TexturePrefix = "deep_mirror";
        config.ExportLayerTextures = true;
        config.RiverCount = 9;
        config.Years = 320;
        config.Resources = DefaultResources();
        config.Plants = DefaultPlants();
        config.Events = DefaultEvents();
        string path = AssetDatabase.GenerateUniqueAssetPath(Folder + "/OUTL_WorldNarrativeConfig_DeepMirror_Jung.asset");
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();
        Selection.activeObject = config;
    }

    private static OUTL_WorldArchetypeDef[] DefaultArchetypes()
    {
        return new[]
        {
            Archetype("self", OUTL_WorldArchetypeType.Self, "РЎР°РјРѕСЃС‚СЊ", "Self", new[]{ OUTL_WorldZoneType.Sacred, OUTL_WorldZoneType.Mountains }, 1.2f, 0f, 1f, 0.25f, 1f, 0f, 0.8f, 0f, 1f, 1f, 1f, 1.35f, 0, 0, 3, 0.18f, "Р¦РµРЅС‚СЂ, СЃРѕР±РёСЂР°СЋС‰РёР№ РєР°СЂС‚Сѓ РІРѕРєСЂСѓРі СЃРјС‹СЃР»Р°; РјРµСЃС‚Р° С†РµР»РѕСЃС‚РЅРѕСЃС‚Рё, РѕСЃРё РјРёСЂР° Рё С„РёРЅР°Р»СЊРЅРѕР№ СЃР±РѕСЂРєРё Р»РёС‡РЅРѕСЃС‚Рё."),
            Archetype("shadow", OUTL_WorldArchetypeType.Shadow, "РўРµРЅСЊ", "Shadow", new[]{ OUTL_WorldZoneType.Wasteland, OUTL_WorldZoneType.Ruins, OUTL_WorldZoneType.Swamp }, 1.4f, 0f, 1f, 0f, 1f, 0f, 1f, 0f, 1f, 1.05f, 0.85f, 1.4f, -1, 3, 0, -0.15f, "Р’С‹С‚РµСЃРЅРµРЅРЅРѕРµ, РѕРїР°СЃРЅРѕРµ, РЅРµСѓС‡С‚С‘РЅРЅРѕРµ; Р·РѕРЅС‹, РіРґРµ РјРёСЂ РїРѕРєР°Р·С‹РІР°РµС‚ С†РµРЅСѓ СЃРѕР±СЃС‚РІРµРЅРЅРѕР№ Р»Р¶Рё."),
            Archetype("persona", OUTL_WorldArchetypeType.Persona, "РџРµСЂСЃРѕРЅР°", "Persona", new[]{ OUTL_WorldZoneType.Coast, OUTL_WorldZoneType.Plains, OUTL_WorldZoneType.Steppe }, 0.9f, 0f, 0.7f, 0f, 0.8f, 0.2f, 0.85f, 0f, 1f, 1f, 1f, 0.9f, 2, 0, 0, 0.12f, "Р¤Р°СЃР°Рґ, РґРѕСЂРѕРіР°, РїРѕСЃРµР»РµРЅРёРµ, РґРѕРіРѕРІРѕСЂ; РјРёСЂ РєР°Рє СЃРѕС†РёР°Р»СЊРЅР°СЏ РјР°СЃРєР°."),
            Archetype("great_mother", OUTL_WorldArchetypeType.GreatMother, "Р’РµР»РёРєР°СЏ РјР°С‚СЊ", "Great Mother", new[]{ OUTL_WorldZoneType.Forest, OUTL_WorldZoneType.River, OUTL_WorldZoneType.Lake, OUTL_WorldZoneType.Swamp }, 1.15f, 0f, 0.75f, 0.5f, 1f, 0f, 0.8f, 0f, 0.65f, 1.2f, 1.35f, 1.05f, 2, 0, 1, 0.1f, "РџР»РѕРґРѕСЂРѕРґРёРµ, РІРѕРґР°, С‚РµР»Рѕ РјРёСЂР°; РјРµСЃС‚Рѕ, РєРѕС‚РѕСЂРѕРµ РєРѕСЂРјРёС‚, РґСѓС€РёС‚ Рё С‚СЂРµР±СѓРµС‚ РїР»Р°С‚С‹."),
            Archetype("wise_elder", OUTL_WorldArchetypeType.WiseElder, "РњСѓРґСЂС‹Р№ СЃС‚Р°СЂРµС†", "Wise Elder", new[]{ OUTL_WorldZoneType.Mountains, OUTL_WorldZoneType.Tundra }, 1f, 0.65f, 1f, 0f, 0.8f, 0f, 0.55f, 0.3f, 1f, 0.9f, 0.75f, 1.1f, 0, 1, 2, 0.2f, "Р’С‹СЃРѕС‚Р°, С…РѕР»РѕРґ, РїР°РјСЏС‚СЊ, РґРёСЃС‚Р°РЅС†РёСЏ; Р·РЅР°РЅРёРµ, РєРѕС‚РѕСЂРѕРµ РЅРµ РѕР±СЏР·Р°РЅРѕ Р±С‹С‚СЊ РґРѕР±СЂС‹Рј."),
            Archetype("trickster", OUTL_WorldArchetypeType.Trickster, "РўСЂРёРєСЃС‚РµСЂ", "Trickster", new[]{ OUTL_WorldZoneType.Coast, OUTL_WorldZoneType.Ruins, OUTL_WorldZoneType.Steppe }, 0.9f, 0f, 0.85f, 0.15f, 0.85f, 0.2f, 0.95f, 0f, 1f, 1.05f, 0.95f, 1.35f, 0, 2, 0, 0.05f, "РЎСЂС‹РІ РїСЂР°РІРёР», РїРµСЂРµС…РѕРґ, РѕР±РјР°РЅ, СЃР»СѓС‡Р°Р№РЅРѕСЃС‚СЊ; Р·РѕРЅР°, РіРґРµ РєР°СЂС‚Р° РїРµСЂРµСЃС‚Р°С‘С‚ РІРµСЃС‚Рё СЃРµР±СЏ РїСЂРёР»РёС‡РЅРѕ."),
            Archetype("death_rebirth", OUTL_WorldArchetypeType.DeathRebirth, "РЎРјРµСЂС‚СЊ/Р’РѕР·СЂРѕР¶РґРµРЅРёРµ", "Death/Rebirth", new[]{ OUTL_WorldZoneType.Ruins, OUTL_WorldZoneType.Wasteland, OUTL_WorldZoneType.Desert, OUTL_WorldZoneType.River }, 1.05f, 0f, 1f, 0f, 0.9f, 0f, 1f, 0f, 1f, 0.8f, 0.9f, 1.25f, -1, 2, 2, -0.05f, "Р Р°СЃРїР°Рґ Рё РЅРѕРІР°СЏ С„РѕСЂРјР°; РјРµСЃС‚Рѕ, РіРґРµ СЃС‚Р°СЂРѕРµ СѓРјРёСЂР°РµС‚ РЅРµ РґРѕ РєРѕРЅС†Р°, РїРѕС‚РѕРјСѓ С‡С‚Рѕ Р¶Р°РЅСЂ С‚СЂРµР±СѓРµС‚ РїСЂРѕРґРѕР»Р¶РµРЅРёСЏ.")
        };
    }

    private static OUTL_WorldResourceDef[] DefaultResources()
    {
        return new[]
        {
            Resource("iron", "Р–РµР»РµР·РЅС‹Рµ Р¶РёР»С‹", "Iron Veins", new[]{ OUTL_WorldZoneType.Hills, OUTL_WorldZoneType.Mountains }, 0.18f, 0.55f, 1f, 0f, 1f, 0f, 0.78f, 0.3f, 1f, "Р’ СЃС‚Р°СЂРѕРј РєР°РјРЅРµ РѕС‚РєСЂС‹Р»РёСЃСЊ Р¶РµР»РµР·РЅС‹Рµ Р¶РёР»С‹.", "Iron was found under the old stone."),
            Resource("salt", "РЎРѕР»СЏРЅС‹Рµ РїР»Р°СЃС‚С‹", "Salt Flats", new[]{ OUTL_WorldZoneType.Desert, OUTL_WorldZoneType.Coast, OUTL_WorldZoneType.Lake }, 0.12f, 0.22f, 0.8f, 0f, 0.55f, 0.45f, 1f, 0.35f, 1f, "РЎРѕР»СЊ РІС‹С€Р»Р° РЅР° РїРѕРІРµСЂС…РЅРѕСЃС‚СЊ С‚Р°Рј, РіРґРµ РІРѕРґР° РѕС‚СЃС‚СѓРїРёР»Р°.", "Salt surfaced where water abandoned the land."),
            Resource("old_bones", "РЎС‚Р°СЂС‹Рµ РєРѕСЃС‚Рё", "Old Bones", new[]{ OUTL_WorldZoneType.Ruins, OUTL_WorldZoneType.Wasteland }, 0.25f, 0f, 1f, 0f, 1f, 0f, 1f, 0f, 1f, "РљРѕСЃС‚Рё РїРѕРєР°Р·Р°Р»РёСЃСЊ С‚Р°Рј, РіРґРµ РёСЃС‚РѕСЂРёСЏ РЅРµ СЃРјРѕРіР»Р° РѕСЃС‚Р°С‚СЊСЃСЏ РїРѕРґ Р·РµРјР»С‘Р№.", "Bones were uncovered where history failed to stay buried."),
            Resource("clear_water", "Р§РёСЃС‚Р°СЏ РІРѕРґР°", "Clear Water", new[]{ OUTL_WorldZoneType.Forest, OUTL_WorldZoneType.Hills, OUTL_WorldZoneType.Sacred, OUTL_WorldZoneType.River }, 0.16f, 0.3f, 0.8f, 0.55f, 1f, 0f, 0.75f, 0f, 0.55f, "Р§РёСЃС‚С‹Р№ РёСЃС‚РѕС‡РЅРёРє РїСЂРѕСЂРµР·Р°Р» РїРѕС‡РІСѓ.", "A clear spring cut through the soil.")
        };
    }

    private static OUTL_WorldPlantDef[] DefaultPlants()
    {
        return new[]
        {
            Plant("red_grass", "РљСЂР°СЃРЅР°СЏ С‚СЂР°РІР°", "Red Grass", new[]{ OUTL_WorldZoneType.Plains, OUTL_WorldZoneType.Hills, OUTL_WorldZoneType.Steppe }, 0.2f, 0.3f, 0.75f, 0.25f, 0.7f, 0.25f, 0.8f, 0.25f, 0.75f, "РџРѕСЃР»Рµ С‚С‘РїР»РѕРіРѕ СЃРµР·РѕРЅР° СЂР°Р·РѕС€Р»Р°СЃСЊ РєСЂР°СЃРЅР°СЏ С‚СЂР°РІР°.", "Red grass spread after a warm season."),
            Plant("reed", "Р‘РѕР»РѕС‚РЅС‹Р№ С‚СЂРѕСЃС‚РЅРёРє", "Marsh Reed", new[]{ OUTL_WorldZoneType.Swamp, OUTL_WorldZoneType.Coast, OUTL_WorldZoneType.River, OUTL_WorldZoneType.Lake }, 0.32f, 0.2f, 0.55f, 0.65f, 1f, 0.2f, 0.8f, 0f, 0.45f, "РўСЂРѕСЃС‚РЅРёРє Р·Р°Р±РёР» РјРѕРєСЂС‹Рµ РєСЂР°СЏ РІРѕРґС‹.", "Reeds crowded the wet margins."),
            Plant("grave_moss", "РњРѕРіРёР»СЊРЅС‹Р№ РјРѕС…", "Grave Moss", new[]{ OUTL_WorldZoneType.Ruins, OUTL_WorldZoneType.Sacred }, 0.22f, 0.2f, 0.8f, 0.4f, 1f, 0f, 0.7f, 0f, 0.65f, "РњРѕРіРёР»СЊРЅС‹Р№ РјРѕС… РѕС‚РјРµС‚РёР» РєР°РјРЅРё, РєРѕС‚РѕСЂС‹Рµ РЅРёРєС‚Рѕ РЅРµ РїСЂРёР·РЅР°РІР°Р» СЂСѓРєРѕС‚РІРѕСЂРЅС‹РјРё.", "Grave moss marked stones nobody admitted placing.")
        };
    }

    private static OUTL_WorldEventDef[] DefaultEvents()
    {
        return new[]
        {
            Event("drought", "Р—Р°СЃСѓС…Р°", "Drought", new[]{ OUTL_WorldZoneType.Plains, OUTL_WorldZoneType.Desert, OUTL_WorldZoneType.Hills, OUTL_WorldZoneType.Steppe }, 0.075f, 0f, 0.8f, 0f, 0.45f, 0.55f, 1f, 0.35f, 1f, 0, 999, -2, 1, 0, "РЎСѓС…РѕР№ РіРѕРґ СЂР°СЃС‚СЂРµСЃРєР°Р» Р·РµРјР»СЋ Рё РІС‹СЃС‚Р°РІРёР» РєРѕР»РѕРґС†С‹ Р±РµСЃРїРѕР»РµР·РЅС‹РјРё РґС‹СЂРєР°РјРё.", "A dry year cracked the ground and made fools of wells."),
            Event("pilgrimage", "РџР°Р»РѕРјРЅРёС‡РµСЃС‚РІРѕ", "Pilgrimage", new[]{ OUTL_WorldZoneType.Sacred, OUTL_WorldZoneType.Mountains, OUTL_WorldZoneType.Forest }, 0.055f, 0.35f, 1f, 0.25f, 0.9f, 0f, 0.75f, 0.2f, 1f, 0, 999, 1, 0, 2, "РџР°Р»РѕРјРЅРёРєРё РїРѕС€Р»Рё Р·Р° Р·РЅР°РєРѕРј, РєРѕС‚РѕСЂС‹Р№, РІРµСЂРѕСЏС‚РЅРѕ, РЅРёС‡РµРіРѕ РЅРµ Р·РЅР°С‡РёР». РџРѕСЌС‚РѕРјСѓ РѕРЅ, РєРѕРЅРµС‡РЅРѕ, СЃС‚Р°Р» РІР°Р¶РЅС‹Рј.", "Pilgrims followed a sign that probably meant nothing, so naturally it mattered."),
            Event("bandits", "Р Р°Р·Р±РѕР№РЅР°СЏ Р·РёРјР°", "Bandit Winter", new[]{ OUTL_WorldZoneType.Forest, OUTL_WorldZoneType.Hills, OUTL_WorldZoneType.Ruins, OUTL_WorldZoneType.Steppe }, 0.065f, 0.3f, 0.9f, 0.2f, 0.85f, 0f, 0.85f, 0.2f, 1f, 10, 999, -1, 3, 0, "Р Р°Р·Р±РѕР№РЅРёРєРё РѕСЃРµР»Рё С‚Р°Рј, РіРґРµ РІР»Р°СЃС‚СЊ РёРјРµР»Р° Р±Р»Р°РіРѕСЂР°Р·СѓРјРёРµ РѕС‚СЃСѓС‚СЃС‚РІРѕРІР°С‚СЊ.", "Bandits settled where authority had the good sense to be absent."),
            Event("ruin_wakes", "РџСЂРѕР±СѓР¶РґРµРЅРёРµ СЂСѓРёРЅ", "Ruin Wakes", new[]{ OUTL_WorldZoneType.Ruins, OUTL_WorldZoneType.Wasteland }, 0.08f, 0f, 1f, 0f, 1f, 0f, 1f, 0f, 1f, 20, 999, -1, 4, 1, "РџРѕРґ СЂСѓРёРЅР°РјРё С€РµРІРµР»СЊРЅСѓР»РѕСЃСЊ С‡С‚Рѕ-С‚Рѕ СЃС‚Р°СЂРѕРµ. РђСЂС…РёРІ РїРѕС‚РѕРј РЅР°Р·РІР°Р» СЌС‚Рѕ РїСЂРѕСЃР°РґРєРѕР№ РіСЂСѓРЅС‚Р°, РїРѕС‚РѕРјСѓ С‡С‚Рѕ С‚СЂСѓСЃР°Рј С‚РѕР¶Рµ РЅСѓР¶РЅР° С‚РµСЂРјРёРЅРѕР»РѕРіРёСЏ.", "Something old moved under the ruins. The archive later called it subsidence, because cowards need vocabulary.")
        };
    }

    private static OUTL_WorldArchetypeDef Archetype(string id, OUTL_WorldArchetypeType type, string ru, string en, OUTL_WorldZoneType[] zones, float weight, float minH, float maxH, float minM, float maxM, float minHeat, float maxHeat, float minD, float maxD, float resourceMul, float plantMul, float eventMul, int prosperity, int danger, int sanctity, float visibilityBias, string note)
    {
        return new OUTL_WorldArchetypeDef { Id = id, Type = type, DisplayName = Text("archetype." + id, ru, en), AllowedZones = zones, Weight = weight, MinHeight = minH, MaxHeight = maxH, MinMoisture = minM, MaxMoisture = maxM, MinHeat = minHeat, MaxHeat = maxHeat, MinDrainage = minD, MaxDrainage = maxD, ResourceChanceMultiplier = resourceMul, PlantChanceMultiplier = plantMul, EventChanceMultiplier = eventMul, ProsperityBias = prosperity, DangerBias = danger, SanctityBias = sanctity, VisibilityBias = visibilityBias, DesignNoteRu = note };
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
