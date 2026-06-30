using UnityEngine;

public enum OUT_SoldierRole
{
    Rifleman = 0,
    Shotgunner = 1,
    Demolitions = 2,
    Commander = 3
}

[DisallowMultipleComponent]
public class OUT_SoldierSquadAgent : MonoBehaviour
{
    [Header("Squad")]
    [SerializeField] private OUT_SoldierSquadCommander commander;
    [SerializeField] private OUT_SoldierRole role = OUT_SoldierRole.Rifleman;
    [SerializeField] private bool autoBecomeCommanderIfMissing = false;

    [Header("Combat Ranges")]
    [SerializeField][Min(0f)] private float preferredMinRange = 8f;
    [SerializeField][Min(0f)] private float preferredMaxRange = 28f;
    [SerializeField][Min(0f)] private float explosiveMinRange = 12f;
    [SerializeField][Min(0f)] private float explosiveMaxRange = 36f;
    [SerializeField][Min(0f)] private float crushAvoidDistance = 8f;

    [Header("Health")]
    [SerializeField][Range(0.05f, 1f)] private float lowHealthThreshold01 = 0.35f;
    [SerializeField] private OUT_HealthSimple health;

    private int _slotIndex;

    public OUT_SoldierSquadCommander Commander => commander;
    public OUT_SoldierRole Role => role;
    public int SlotIndex => _slotIndex;
    public float PreferredMinRange => preferredMinRange;
    public float PreferredMaxRange => preferredMaxRange;
    public float ExplosiveMinRange => explosiveMinRange;
    public float ExplosiveMaxRange => explosiveMaxRange;
    public float CrushAvoidDistance => crushAvoidDistance;
    public bool IsCommanderRole => role == OUT_SoldierRole.Commander;

    private void Awake()
    {
        if (health == null)
            health = GetComponent<OUT_HealthSimple>();

        if (commander == null)
            commander = GetComponentInParent<OUT_SoldierSquadCommander>();
    }

    private void OnEnable()
    {
        if (commander != null)
            commander.RegisterAgent(this);
        else if (autoBecomeCommanderIfMissing)
            EnsureCommanderOnSelf();
    }

    private void OnDisable()
    {
        if (commander != null)
            commander.UnregisterAgent(this);
    }

    public void AssignCommander(OUT_SoldierSquadCommander newCommander)
    {
        if (commander == newCommander)
            return;

        if (commander != null)
            commander.UnregisterAgent(this);

        commander = newCommander;

        if (commander != null && isActiveAndEnabled)
            commander.RegisterAgent(this);
    }

    public void SetSlotIndex(int index)
    {
        _slotIndex = Mathf.Max(0, index);
    }

    public bool IsLowHealth()
    {
        return GetHealth01() <= lowHealthThreshold01;
    }

    public float GetHealth01()
    {
        if (health == null || health.MaxHealth <= 0)
            return 1f;

        return Mathf.Clamp01((float)health.CurrentHealth / health.MaxHealth);
    }

    public Vector3 GetDesiredCombatPoint(Vector3 fallbackOrigin, Vector3 enemyPosition)
    {
        if (commander != null)
            return commander.GetSlotWorldPoint(this, fallbackOrigin, enemyPosition);

        return fallbackOrigin;
    }

    public bool CanUseExplosives(float distanceToEnemy)
    {
        if (role != OUT_SoldierRole.Demolitions)
            return false;

        return distanceToEnemy >= explosiveMinRange && distanceToEnemy <= explosiveMaxRange;
    }

    public bool IsCrushThreat(Vector3 threatPosition)
    {
        return Vector3.Distance(transform.position, threatPosition) <= crushAvoidDistance;
    }

    private void EnsureCommanderOnSelf()
    {
        commander = GetComponent<OUT_SoldierSquadCommander>();
        if (commander == null)
            commander = gameObject.AddComponent<OUT_SoldierSquadCommander>();

        commander.RegisterAgent(this);
    }
}
