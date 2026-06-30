using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_LanguageService : MonoBehaviour
{
    public static OUTL_LanguageService Instance { get; private set; }
    public static event Action<string> OnLanguageChangedGlobal;

    [Header("Language")]
    public string CurrentLanguage = "ru";
    public string FallbackLanguage = "en";
    public string PlayerPrefsKey = "OUTL.Language";
    public bool LoadFromPlayerPrefs = true;
    public bool SaveToPlayerPrefs = true;

    [Header("Tables")]
    public OUTL_LocalizationTable[] Tables;
    public bool UseBuiltInCoreFallback = true;

    [Header("Debug")]
    public bool ReturnKeyWhenMissing = true;

    private string cachedLanguage;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            return;
        }
        Instance = this;
        if (LoadFromPlayerPrefs && !string.IsNullOrEmpty(PlayerPrefsKey) && PlayerPrefs.HasKey(PlayerPrefsKey))
            CurrentLanguage = Normalize(PlayerPrefs.GetString(PlayerPrefsKey, CurrentLanguage));
        CurrentLanguage = Normalize(CurrentLanguage);
        FallbackLanguage = Normalize(FallbackLanguage);
        cachedLanguage = CurrentLanguage;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetLanguage(string languageCode)
    {
        string normalized = Normalize(languageCode);
        if (string.IsNullOrEmpty(normalized)) normalized = "ru";
        if (CurrentLanguage == normalized && cachedLanguage == normalized) return;
        CurrentLanguage = normalized;
        cachedLanguage = normalized;
        if (SaveToPlayerPrefs && !string.IsNullOrEmpty(PlayerPrefsKey))
        {
            PlayerPrefs.SetString(PlayerPrefsKey, CurrentLanguage);
            PlayerPrefs.Save();
        }
        if (OnLanguageChangedGlobal != null) OnLanguageChangedGlobal(CurrentLanguage);
    }

    [ContextMenu("OUTL Refresh Current Language")]
    public void RefreshCurrentLanguage()
    {
        string normalized = Normalize(CurrentLanguage);
        if (normalized != cachedLanguage) SetLanguage(normalized);
    }

    public string Get(string key, string fallback = "")
    {
        string value;
        if (TryGet(key, out value)) return value;
        if (!string.IsNullOrEmpty(fallback)) return fallback;
        return ReturnKeyWhenMissing ? key : string.Empty;
    }

    public bool TryGet(string key, out string value)
    {
        value = string.Empty;
        if (string.IsNullOrEmpty(key)) return false;
        if (TryGetFromTables(CurrentLanguage, key, out value)) return true;
        if (UseBuiltInCoreFallback && TryGetBuiltInCore(CurrentLanguage, key, out value)) return true;
        if (!string.IsNullOrEmpty(FallbackLanguage) && FallbackLanguage != CurrentLanguage)
        {
            if (TryGetFromTables(FallbackLanguage, key, out value)) return true;
            if (UseBuiltInCoreFallback && TryGetBuiltInCore(FallbackLanguage, key, out value)) return true;
        }
        return false;
    }

    private bool TryGetFromTables(string languageCode, string key, out string value)
    {
        value = string.Empty;
        if (Tables == null) return false;
        for (int i = 0; i < Tables.Length; i++)
        {
            OUTL_LocalizationTable table = Tables[i];
            if (table != null && table.TryGet(languageCode, key, out value)) return true;
        }
        return false;
    }

    public static string GetText(string key, string fallback = "")
    {
        return Instance != null ? Instance.Get(key, fallback) : (!string.IsNullOrEmpty(fallback) ? fallback : key);
    }

    public static string Normalize(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode)) return "ru";
        string s = languageCode.Trim().ToLowerInvariant();
        if (s == "0" || s == "eng" || s == "english") return "en";
        if (s == "1" || s == "rus" || s == "russian") return "ru";
        return s;
    }

    private static bool TryGetBuiltInCore(string languageCode, string key, out string value)
    {
        value = string.Empty;
        bool en = Normalize(languageCode) == "en";
        switch (key)
        {
            case "interact.verb.use": value = en ? "Use" : "Использовать"; return true;
            case "interact.verb.open": value = en ? "Open" : "Открыть"; return true;
            case "interact.verb.close": value = en ? "Close" : "Закрыть"; return true;
            case "interact.verb.activate": value = en ? "Activate" : "Активировать"; return true;
            case "interact.verb.talk": value = en ? "Talk" : "Говорить"; return true;
            case "interact.verb.pickup": value = en ? "Pick up" : "Взять"; return true;
            case "interact.verb.push": value = en ? "Push" : "Толкнуть"; return true;
            case "interact.object.name": value = en ? "Object" : "Объект"; return true;
            case "interact.object.description": value = en ? "Interact" : "Взаимодействовать"; return true;
            case "object.door.name": value = en ? "Door" : "Дверь"; return true;
            case "object.door.desc": value = en ? "Open/close door" : "Открыть/закрыть дверь"; return true;
            case "object.button.name": value = en ? "Button" : "Кнопка"; return true;
            case "object.button.desc": value = en ? "Press button" : "Нажать кнопку"; return true;
            case "object.chest.name": value = en ? "Chest" : "Сундук"; return true;
            case "object.chest.desc": value = en ? "Open chest" : "Открыть сундук"; return true;
        }
        return false;
    }
}
