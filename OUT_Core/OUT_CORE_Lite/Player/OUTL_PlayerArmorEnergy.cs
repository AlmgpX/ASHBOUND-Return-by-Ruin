using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_PlayerArmorEnergy : MonoBehaviour, OUTL_ITickable, OUTL_IPoolReset
{
    public OUTL_EntityAdapter Entity;
    public string ArmorKey = "Armor";
    public string MaxArmorKey = "MaxArmor";
    public string EnergyKey = "Energy";
    public string MaxEnergyKey = "MaxEnergy";

    [Header("Quake / Hexen Style Armor")]
    public bool EnableArmorAbsorb = true;
    [Range(0f, 1f)] public float ArmorAbsorbFraction = 0.66f;
    public float DefaultArmor = 0f;
    public float DefaultMaxArmor = 100f;

    [Header("Energy")]
    public float DefaultEnergy = 100f;
    public float DefaultMaxEnergy = 100f;
    public bool RegenerateEnergy = true;
    public float EnergyRegenPerSecond = 12f;
    public float EnergyRegenDelayAfterUse = 0.7f;
    public bool AutoRegister = true;
    public OUTL_TickLane TickLane = OUTL_TickLane.Full;
    public float TickInterval = 0.05f;

    private bool initialized;
    private bool registered;
    private float lastEnergyUseTime = -999f;

    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled && Entity != null && Entity.Runtime != null; } }
    public OUTL_TickLane OUTL_TickLane { get { return TickLane; } }
    public float OUTL_TickInterval { get { return Mathf.Max(0.01f, TickInterval); } }

    private void Awake()
    {
        Resolve();
    }

    private void OnEnable()
    {
        Resolve();
        EnsureInitialized();
        if (AutoRegister) Register();
    }

    private void OnDisable()
    {
        Unregister();
    }

    public void Register()
    {
        if (registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Register(this);
        registered = true;
    }

    public void Unregister()
    {
        if (!registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Unregister(this);
        registered = false;
    }

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        EnsureInitialized();
        if (!RegenerateEnergy || Entity == null || Entity.Runtime == null) return;
        if (time < lastEnergyUseTime + Mathf.Max(0f, EnergyRegenDelayAfterUse)) return;

        float max = Entity.Runtime.Stats.Get(MaxEnergyKey, DefaultMaxEnergy);
        float energy = Entity.Runtime.Stats.Get(EnergyKey, DefaultEnergy);
        if (energy < max)
            Entity.Runtime.Stats.Set(EnergyKey, Mathf.Min(max, energy + Mathf.Max(0f, EnergyRegenPerSecond) * deltaTime));
    }

    public float ModifyIncomingDamage(float damage)
    {
        EnsureInitialized();
        if (Entity != null && Entity.Runtime != null)
            Entity.Runtime.State.SetFloat("Player.LastArmorAbsorbed", 0f);

        if (!EnableArmorAbsorb || Entity == null || Entity.Runtime == null || damage <= 0f) return damage;

        float armor = Entity.Runtime.Stats.Get(ArmorKey, 0f);
        if (armor <= 0f) return damage;

        float absorbWanted = damage * Mathf.Clamp01(ArmorAbsorbFraction);
        float absorbed = Mathf.Min(armor, absorbWanted);
        Entity.Runtime.Stats.Set(ArmorKey, Mathf.Max(0f, armor - absorbed));
        Entity.Runtime.State.SetFloat("Player.LastArmorAbsorbed", absorbed);
        return Mathf.Max(0f, damage - absorbed);
    }

    public bool TrySpendEnergy(float amount)
    {
        EnsureInitialized();
        if (Entity == null || Entity.Runtime == null) return false;
        amount = Mathf.Max(0f, amount);
        float energy = Entity.Runtime.Stats.Get(EnergyKey, 0f);
        if (energy < amount) return false;
        Entity.Runtime.Stats.Set(EnergyKey, energy - amount);
        lastEnergyUseTime = OUTL_World.Instance != null ? OUTL_World.Instance.WorldTime : Time.time;
        return true;
    }

    public void EnsureInitialized()
    {
        if (initialized) return;
        Resolve();
        if (Entity == null || Entity.Runtime == null) return;

        float maxArmor = Entity.Runtime.Stats.Get(MaxArmorKey, 0f);
        if (maxArmor <= 0f) Entity.Runtime.Stats.Set(MaxArmorKey, Mathf.Max(0f, DefaultMaxArmor));
        if (Entity.Runtime.Stats.Get(ArmorKey, -1f) < 0f) Entity.Runtime.Stats.Set(ArmorKey, Mathf.Clamp(DefaultArmor, 0f, Mathf.Max(1f, DefaultMaxArmor)));
        Entity.Runtime.State.SetFloat("Player.LastArmorAbsorbed", 0f);

        float maxEnergy = Entity.Runtime.Stats.Get(MaxEnergyKey, 0f);
        if (maxEnergy <= 0f) Entity.Runtime.Stats.Set(MaxEnergyKey, Mathf.Max(1f, DefaultMaxEnergy));
        if (Entity.Runtime.Stats.Get(EnergyKey, -1f) < 0f) Entity.Runtime.Stats.Set(EnergyKey, Mathf.Clamp(DefaultEnergy, 0f, Mathf.Max(1f, DefaultMaxEnergy)));

        initialized = true;
    }

    public void OUTL_OnPoolSpawn()
    {
        initialized = false;
        EnsureInitialized();
    }

    public void OUTL_OnPoolRelease()
    {
        initialized = false;
    }

    private void Resolve()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
    }
}
