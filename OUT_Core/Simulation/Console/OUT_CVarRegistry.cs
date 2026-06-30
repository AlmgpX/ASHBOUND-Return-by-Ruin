using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[Flags]
public enum OUT_CVarFlags
{
    None = 0,
    Archive = 1 << 0,
    Protected = 1 << 1,
    ReadOnly = 1 << 2
}

public enum OUT_CVarType
{
    String = 0,
    Bool = 1,
    Int = 2,
    Float = 3
}

public sealed class OUT_CVar
{
    public readonly string Name;
    public readonly string DefaultValue;
    public readonly string Help;
    public readonly OUT_CVarType Type;
    public readonly OUT_CVarFlags Flags;

    private string value;
    private readonly Action<OUT_CVar> onChanged;

    public string Value => value;
    public bool BoolValue => ParseBool(value);
    public int IntValue => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;
    public float FloatValue => float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : 0f;

    public OUT_CVar(string name, string defaultValue, OUT_CVarType type, OUT_CVarFlags flags, string help, Action<OUT_CVar> onChanged)
    {
        Name = name;
        DefaultValue = defaultValue ?? string.Empty;
        value = DefaultValue;
        Type = type;
        Flags = flags;
        Help = help ?? string.Empty;
        this.onChanged = onChanged;
    }

    public bool TrySet(string rawValue, bool allowProtected, out string error)
    {
        error = null;

        if ((Flags & OUT_CVarFlags.ReadOnly) != 0)
        {
            error = $"cvar '{Name}' is read-only";
            return false;
        }

        if ((Flags & OUT_CVarFlags.Protected) != 0 && !allowProtected)
        {
            error = $"cvar '{Name}' is protected. Set sv_cheats 1 first.";
            return false;
        }

        if (!TryNormalize(rawValue, Type, out string normalized, out error))
            return false;

        if (value == normalized)
            return true;

        value = normalized;
        onChanged?.Invoke(this);
        return true;
    }

    public void ResetToDefault(bool allowProtected = true)
    {
        TrySet(DefaultValue, allowProtected, out _);
    }

    private static bool TryNormalize(string rawValue, OUT_CVarType type, out string normalized, out string error)
    {
        error = null;
        normalized = rawValue ?? string.Empty;

        switch (type)
        {
            case OUT_CVarType.Bool:
                if (TryParseBool(rawValue, out bool boolValue))
                {
                    normalized = boolValue ? "1" : "0";
                    return true;
                }
                error = $"'{rawValue}' is not a bool. Use 0/1, true/false, on/off.";
                return false;

            case OUT_CVarType.Int:
                if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
                {
                    normalized = intValue.ToString(CultureInfo.InvariantCulture);
                    return true;
                }
                error = $"'{rawValue}' is not an integer.";
                return false;

            case OUT_CVarType.Float:
                if (float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
                {
                    normalized = floatValue.ToString("0.########", CultureInfo.InvariantCulture);
                    return true;
                }
                error = $"'{rawValue}' is not a float.";
                return false;

            case OUT_CVarType.String:
            default:
                normalized = rawValue ?? string.Empty;
                return true;
        }
    }

    public static bool ParseBool(string value)
    {
        TryParseBool(value, out bool result);
        return result;
    }

    public static bool TryParseBool(string value, out bool result)
    {
        result = false;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string v = value.Trim().ToLowerInvariant();
        if (v == "1" || v == "true" || v == "on" || v == "yes")
        {
            result = true;
            return true;
        }

        if (v == "0" || v == "false" || v == "off" || v == "no")
        {
            result = false;
            return true;
        }

        return false;
    }
}

[DefaultExecutionOrder(-8800)]
public class OUT_CVarRegistry : MonoBehaviour
{
    private readonly Dictionary<string, OUT_CVar> cvars = new Dictionary<string, OUT_CVar>(StringComparer.OrdinalIgnoreCase);
    private readonly List<OUT_CVar> sortedCache = new List<OUT_CVar>(128);
    private bool cacheDirty = true;

    public IReadOnlyDictionary<string, OUT_CVar> CVars => cvars;

    public OUT_CVar RegisterString(string name, string defaultValue, OUT_CVarFlags flags = OUT_CVarFlags.None, string help = "", Action<OUT_CVar> onChanged = null)
    {
        return Register(name, defaultValue, OUT_CVarType.String, flags, help, onChanged);
    }

    public OUT_CVar RegisterBool(string name, bool defaultValue, OUT_CVarFlags flags = OUT_CVarFlags.None, string help = "", Action<OUT_CVar> onChanged = null)
    {
        return Register(name, defaultValue ? "1" : "0", OUT_CVarType.Bool, flags, help, onChanged);
    }

    public OUT_CVar RegisterInt(string name, int defaultValue, OUT_CVarFlags flags = OUT_CVarFlags.None, string help = "", Action<OUT_CVar> onChanged = null)
    {
        return Register(name, defaultValue.ToString(CultureInfo.InvariantCulture), OUT_CVarType.Int, flags, help, onChanged);
    }

    public OUT_CVar RegisterFloat(string name, float defaultValue, OUT_CVarFlags flags = OUT_CVarFlags.None, string help = "", Action<OUT_CVar> onChanged = null)
    {
        return Register(name, defaultValue.ToString("0.########", CultureInfo.InvariantCulture), OUT_CVarType.Float, flags, help, onChanged);
    }

    public OUT_CVar Register(string name, string defaultValue, OUT_CVarType type, OUT_CVarFlags flags, string help = "", Action<OUT_CVar> onChanged = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Debug.LogWarning("OUT_CVarRegistry: rejected empty cvar name.");
            return null;
        }

        name = name.Trim();
        if (cvars.TryGetValue(name, out OUT_CVar existing))
            return existing;

        OUT_CVar cvar = new OUT_CVar(name, defaultValue, type, flags, help, onChanged);
        cvars.Add(name, cvar);
        cacheDirty = true;
        return cvar;
    }

    public bool TryGet(string name, out OUT_CVar cvar)
    {
        if (name == null)
        {
            cvar = null;
            return false;
        }

        return cvars.TryGetValue(name.Trim(), out cvar);
    }

    public bool Exists(string name)
    {
        return TryGet(name, out _);
    }

    public bool TrySet(string name, string value, bool allowProtected, out string result)
    {
        if (!TryGet(name, out OUT_CVar cvar))
        {
            result = $"unknown cvar '{name}'";
            return false;
        }

        if (!cvar.TrySet(value, allowProtected, out string error))
        {
            result = error;
            return false;
        }

        result = $"{cvar.Name} = {cvar.Value}";
        return true;
    }

    public bool GetBool(string name, bool fallback = false)
    {
        return TryGet(name, out OUT_CVar cvar) ? cvar.BoolValue : fallback;
    }

    public int GetInt(string name, int fallback = 0)
    {
        return TryGet(name, out OUT_CVar cvar) ? cvar.IntValue : fallback;
    }

    public float GetFloat(string name, float fallback = 0f)
    {
        return TryGet(name, out OUT_CVar cvar) ? cvar.FloatValue : fallback;
    }

    public string GetString(string name, string fallback = "")
    {
        return TryGet(name, out OUT_CVar cvar) ? cvar.Value : fallback;
    }

    public List<OUT_CVar> GetSortedSnapshot()
    {
        if (!cacheDirty)
            return sortedCache;

        sortedCache.Clear();
        foreach (KeyValuePair<string, OUT_CVar> pair in cvars)
            sortedCache.Add(pair.Value);

        sortedCache.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        cacheDirty = false;
        return sortedCache;
    }
}
