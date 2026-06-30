using System;
using System.IO;
using UnityEngine;

[Serializable]
public sealed class OUTL_InputBinding
{
    public string Action = "QuickSave";
    public KeyCode Primary = KeyCode.F5;
    public KeyCode Secondary = KeyCode.None;
    public bool Enabled = true;
}

[Serializable]
public sealed class OUTL_InputConfigFile
{
    public int Version = 1;
    public OUTL_InputBinding QuickSave = new OUTL_InputBinding { Action = "QuickSave", Primary = KeyCode.F5, Secondary = KeyCode.None, Enabled = true };
    public OUTL_InputBinding QuickLoad = new OUTL_InputBinding { Action = "QuickLoad", Primary = KeyCode.F9, Secondary = KeyCode.None, Enabled = true };
    public OUTL_InputBinding ReloadConfig = new OUTL_InputBinding { Action = "ReloadInputConfig", Primary = KeyCode.F10, Secondary = KeyCode.None, Enabled = true };
}

[DefaultExecutionOrder(-9300)]
[DisallowMultipleComponent]
public sealed class OUTL_QuickSaveInput : MonoBehaviour
{
    [Header("Files")]
    public string ConfigFileName = "outl_input_config.json";
    public string QuickSaveFileName = "outl_quicksave.json";
    public bool AutoCreateConfig = true;
    public bool LoadConfigOnEnable = true;
    public bool LogActions = true;

    [Header("Runtime")]
    public OUTL_InputConfigFile Config = new OUTL_InputConfigFile();

    public string ConfigPath
    {
        get { return Path.Combine(Application.persistentDataPath, ConfigFileName); }
    }

    public string QuickSavePath
    {
        get { return Path.Combine(Application.persistentDataPath, QuickSaveFileName); }
    }

    private void OnEnable()
    {
        if (LoadConfigOnEnable) LoadOrCreateConfig();
    }

    private void Update()
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return;

        if (WasPressed(Config.ReloadConfig))
        {
            LoadOrCreateConfig();
            Log("input config reloaded: " + ConfigPath);
            return;
        }

        if (WasPressed(Config.QuickSave))
        {
            world.Save.SaveToFile(QuickSavePath);
            Log("quick saved: " + QuickSavePath);
        }

        if (WasPressed(Config.QuickLoad))
        {
            bool ok = world.Save.LoadFromFile(QuickSavePath);
            Log(ok ? "quick loaded: " + QuickSavePath : "quick load failed: " + QuickSavePath);
        }
    }

    [ContextMenu("OUT Load Or Create Input Config")]
    public void LoadOrCreateConfig()
    {
        string path = ConfigPath;
        try
        {
            if (!File.Exists(path))
            {
                if (AutoCreateConfig) SaveConfig();
                return;
            }

            string json = File.ReadAllText(path);
            OUTL_InputConfigFile loaded = JsonUtility.FromJson<OUTL_InputConfigFile>(json);
            if (loaded != null) Config = loaded;
            EnsureDefaults();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[OUTL QuickSaveInput] failed to load config: " + ex.Message, this);
            EnsureDefaults();
        }
    }

    [ContextMenu("OUT Save Input Config")]
    public void SaveConfig()
    {
        EnsureDefaults();
        string path = ConfigPath;
        try
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, JsonUtility.ToJson(Config, true));
            Log("input config saved: " + path);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[OUTL QuickSaveInput] failed to save config: " + ex.Message, this);
        }
    }

    [ContextMenu("OUT Quick Save Now")]
    public void QuickSaveNow()
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return;
        world.Save.SaveToFile(QuickSavePath);
        Log("quick saved: " + QuickSavePath);
    }

    [ContextMenu("OUT Quick Load Now")]
    public void QuickLoadNow()
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return;
        bool ok = world.Save.LoadFromFile(QuickSavePath);
        Log(ok ? "quick loaded: " + QuickSavePath : "quick load failed: " + QuickSavePath);
    }

    public void SetBinding(string action, KeyCode primary, KeyCode secondary = KeyCode.None)
    {
        OUTL_InputBinding binding = FindBinding(action);
        if (binding == null) return;
        binding.Primary = primary;
        binding.Secondary = secondary;
        binding.Enabled = true;
        SaveConfig();
    }

    private OUTL_InputBinding FindBinding(string action)
    {
        if (string.Equals(action, "QuickSave", StringComparison.OrdinalIgnoreCase)) return Config.QuickSave;
        if (string.Equals(action, "QuickLoad", StringComparison.OrdinalIgnoreCase)) return Config.QuickLoad;
        if (string.Equals(action, "ReloadInputConfig", StringComparison.OrdinalIgnoreCase)) return Config.ReloadConfig;
        return null;
    }

    private static bool WasPressed(OUTL_InputBinding binding)
    {
        if (binding == null || !binding.Enabled) return false;
        return (binding.Primary != KeyCode.None && Input.GetKeyDown(binding.Primary)) || (binding.Secondary != KeyCode.None && Input.GetKeyDown(binding.Secondary));
    }

    private void EnsureDefaults()
    {
        if (Config == null) Config = new OUTL_InputConfigFile();
        if (Config.QuickSave == null) Config.QuickSave = new OUTL_InputBinding { Action = "QuickSave", Primary = KeyCode.F5, Secondary = KeyCode.None, Enabled = true };
        if (Config.QuickLoad == null) Config.QuickLoad = new OUTL_InputBinding { Action = "QuickLoad", Primary = KeyCode.F9, Secondary = KeyCode.None, Enabled = true };
        if (Config.ReloadConfig == null) Config.ReloadConfig = new OUTL_InputBinding { Action = "ReloadInputConfig", Primary = KeyCode.F10, Secondary = KeyCode.None, Enabled = true };
        Config.QuickSave.Action = "QuickSave";
        Config.QuickLoad.Action = "QuickLoad";
        Config.ReloadConfig.Action = "ReloadInputConfig";
        if (Config.Version <= 0) Config.Version = 1;
    }

    private void Log(string message)
    {
        if (LogActions) Debug.Log("[OUTL QuickSaveInput] " + message, this);
    }
}
