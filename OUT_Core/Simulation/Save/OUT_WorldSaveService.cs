using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-6400)]
[DisallowMultipleComponent]
public class OUT_WorldSaveService : MonoBehaviour
{
    public static OUT_WorldSaveService Instance { get; private set; }

    [Header("Storage")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private string folderName = "OUT_Saves";
    [SerializeField] private string defaultSlot = "quick";

    [Header("Debug")]
    [SerializeField] private bool logOperations = true;

    public string DefaultSlot => string.IsNullOrWhiteSpace(defaultSlot) ? "quick" : defaultSlot.Trim();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static OUT_WorldSaveService EnsureExists()
    {
        if (Instance != null)
            return Instance;

        OUT_WorldSaveService existing = FindObjectOfType<OUT_WorldSaveService>();
        if (existing != null)
            return existing;

        GameObject go = new GameObject("OUT_WorldSaveService");
        return go.AddComponent<OUT_WorldSaveService>();
    }

    public string Save(string slot = null)
    {
        string safeSlot = SanitizeFileName(string.IsNullOrWhiteSpace(slot) ? DefaultSlot : slot);
        string path = GetSlotPath(safeSlot);
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        OUT_SaveableEntity[] entities = FindObjectsOfType<OUT_SaveableEntity>(true);
        List<OUT_SavedObject> savedObjects = new List<OUT_SavedObject>(entities.Length);

        for (int i = 0; i < entities.Length; i++)
        {
            OUT_SaveableEntity entity = entities[i];
            if (entity == null)
                continue;

            IOutSaveState[] states = CollectSaveStates(entity);
            List<OUT_SavedComponent> components = new List<OUT_SavedComponent>(states.Length);

            for (int s = 0; s < states.Length; s++)
            {
                IOutSaveState state = states[s];
                if (state == null || string.IsNullOrWhiteSpace(state.SaveKey))
                    continue;

                string json = state.CaptureStateJson();
                components.Add(new OUT_SavedComponent
                {
                    Key = state.SaveKey,
                    Json = json ?? string.Empty
                });
            }

            OUT_SavedObject saved = new OUT_SavedObject
            {
                Id = entity.SaveId,
                ActiveSelf = entity.gameObject.activeSelf,
                Position = entity.transform.position,
                Rotation = entity.transform.rotation,
                Scale = entity.transform.localScale,
                Components = components.ToArray()
            };

            savedObjects.Add(saved);
        }

        OUT_WorldSaveFile file = new OUT_WorldSaveFile
        {
            Version = 1,
            SceneName = SceneManager.GetActiveScene().name,
            CreatedUtc = DateTime.UtcNow.ToString("o"),
            UnityTime = Time.time,
            Objects = savedObjects.ToArray()
        };

        File.WriteAllText(path, JsonUtility.ToJson(file, true));

        if (logOperations)
            Debug.Log("OUT_WorldSaveService saved " + savedObjects.Count + " objects to " + path);

        return path;
    }

    public bool Load(string slot = null)
    {
        string safeSlot = SanitizeFileName(string.IsNullOrWhiteSpace(slot) ? DefaultSlot : slot);
        string path = GetSlotPath(safeSlot);
        if (!File.Exists(path))
        {
            Debug.LogWarning("OUT_WorldSaveService load failed. File not found: " + path, this);
            return false;
        }

        OUT_WorldSaveFile file = JsonUtility.FromJson<OUT_WorldSaveFile>(File.ReadAllText(path));
        if (file == null || file.Objects == null)
            return false;

        OUT_SaveableEntity[] entities = FindObjectsOfType<OUT_SaveableEntity>(true);
        Dictionary<string, OUT_SaveableEntity> map = new Dictionary<string, OUT_SaveableEntity>(entities.Length);

        for (int i = 0; i < entities.Length; i++)
        {
            OUT_SaveableEntity entity = entities[i];
            if (entity == null)
                continue;

            string id = entity.SaveId;
            if (!map.ContainsKey(id))
                map.Add(id, entity);
        }

        int restored = 0;
        for (int i = 0; i < file.Objects.Length; i++)
        {
            OUT_SavedObject saved = file.Objects[i];
            if (saved == null || string.IsNullOrWhiteSpace(saved.Id))
                continue;

            OUT_SaveableEntity entity;
            if (!map.TryGetValue(saved.Id, out entity) || entity == null)
                continue;

            if (entity.SaveActiveState)
                entity.gameObject.SetActive(saved.ActiveSelf);

            if (entity.SaveTransform)
            {
                entity.transform.position = saved.Position;
                entity.transform.rotation = saved.Rotation;
                entity.transform.localScale = saved.Scale;
            }

            RestoreComponents(entity, saved.Components);
            restored++;
        }

        if (logOperations)
            Debug.Log("OUT_WorldSaveService loaded " + restored + " objects from " + path);

        return true;
    }

    public string GetSlotPath(string slot)
    {
        string safeSlot = SanitizeFileName(string.IsNullOrWhiteSpace(slot) ? DefaultSlot : slot);
        return Path.Combine(Application.persistentDataPath, folderName, safeSlot + ".json");
    }

    public string GetSaveDirectory()
    {
        return Path.Combine(Application.persistentDataPath, folderName);
    }

    private static IOutSaveState[] CollectSaveStates(OUT_SaveableEntity entity)
    {
        MonoBehaviour[] behaviours = entity.GetComponents<MonoBehaviour>();
        List<IOutSaveState> result = new List<IOutSaveState>(behaviours.Length);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IOutSaveState saveState)
                result.Add(saveState);
        }

        return result.ToArray();
    }

    private static void RestoreComponents(OUT_SaveableEntity entity, OUT_SavedComponent[] savedComponents)
    {
        if (savedComponents == null || savedComponents.Length == 0)
            return;

        IOutSaveState[] states = CollectSaveStates(entity);
        for (int c = 0; c < savedComponents.Length; c++)
        {
            OUT_SavedComponent savedComponent = savedComponents[c];
            if (savedComponent == null || string.IsNullOrWhiteSpace(savedComponent.Key))
                continue;

            for (int s = 0; s < states.Length; s++)
            {
                IOutSaveState state = states[s];
                if (state != null && state.SaveKey == savedComponent.Key)
                {
                    state.RestoreStateJson(savedComponent.Json);
                    break;
                }
            }
        }
    }

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "save";

        char[] invalid = Path.GetInvalidFileNameChars();
        string result = value.Trim();
        for (int i = 0; i < invalid.Length; i++)
            result = result.Replace(invalid[i], '_');
        return result;
    }
}
