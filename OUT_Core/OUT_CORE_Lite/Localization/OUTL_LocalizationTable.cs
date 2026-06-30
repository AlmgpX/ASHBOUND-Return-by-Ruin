using System;
using UnityEngine;

[Serializable]
public struct OUTL_LocalizedEntry
{
    public string Key;
    [TextArea] public string Value;
}

[Serializable]
public sealed class OUTL_LocalizedLanguageBlock
{
    public string LanguageCode = "ru";
    public OUTL_LocalizedEntry[] Entries;
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Localization/Localization Table", fileName = "OUTL_LocalizationTable")]
public sealed class OUTL_LocalizationTable : ScriptableObject
{
    public OUTL_LocalizedLanguageBlock[] Languages;

    public bool TryGet(string languageCode, string key, out string value)
    {
        value = string.Empty;
        if (string.IsNullOrEmpty(languageCode) || string.IsNullOrEmpty(key) || Languages == null) return false;

        for (int i = 0; i < Languages.Length; i++)
        {
            OUTL_LocalizedLanguageBlock block = Languages[i];
            if (block == null || block.LanguageCode != languageCode || block.Entries == null) continue;
            for (int j = 0; j < block.Entries.Length; j++)
            {
                if (block.Entries[j].Key != key) continue;
                value = block.Entries[j].Value;
                return !string.IsNullOrEmpty(value);
            }
        }
        return false;
    }
}
