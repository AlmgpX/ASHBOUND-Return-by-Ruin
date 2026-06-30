using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_ProgressSystemLanguageBridge : MonoBehaviour, OUTL_ITickable
{
    public OUTL_LanguageService LanguageService;
    public bool ReadEveryFrame = true;
    public int EnglishIndex = 0;
    public int RussianIndex = 1;
    public bool AutoRegister = true;
    public OUTL_TickLane TickLane = OUTL_TickLane.Logic;
    [Min(0.05f)] public float TickInterval = 0.25f;

    private int lastIndex = int.MinValue;
    private bool registered;

    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled && ReadEveryFrame; } }
    public OUTL_TickLane OUTL_TickLane { get { return TickLane; } }
    public float OUTL_TickInterval { get { return Mathf.Max(0.05f, TickInterval); } }

    private void Awake()
    {
        if (LanguageService == null) LanguageService = OUTL_LanguageService.Instance;
        Sync(true);
    }

    private void Start()
    {
        if (AutoRegister) Register();
    }

    private void OnEnable()
    {
        if (AutoRegister) Register();
    }

    private void OnDisable()
    {
        Unregister();
    }

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        if (ReadEveryFrame) Sync(false);
    }

    [ContextMenu("OUT Register")]
    public void Register()
    {
        if (registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Register(this);
        registered = true;
    }

    [ContextMenu("OUT Unregister")]
    public void Unregister()
    {
        if (!registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Unregister(this);
        registered = false;
    }

    public void Sync(bool force)
    {
        if (LanguageService == null) LanguageService = OUTL_LanguageService.Instance;
        if (LanguageService == null) return;
        int index;
        if (!TryReadProgressSystemLanguageIndex(out index)) return;
        if (!force && index == lastIndex) return;
        lastIndex = index;
        LanguageService.SetLanguage(index == EnglishIndex ? "en" : "ru");
    }

    private static bool TryReadProgressSystemLanguageIndex(out int index)
    {
        index = 1;
        System.Type type = System.Type.GetType("ProgressSystem");
        if (type == null)
        {
            System.Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i].Name == "ProgressSystem")
                {
                    type = types[i];
                    break;
                }
            }
        }
        if (type == null) return false;

        FieldInfo field = type.GetField("LanguageIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (field != null && field.FieldType == typeof(int))
        {
            index = (int)field.GetValue(null);
            return true;
        }

        PropertyInfo property = type.GetProperty("LanguageIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (property != null && property.PropertyType == typeof(int))
        {
            index = (int)property.GetValue(null, null);
            return true;
        }

        return false;
    }
}
