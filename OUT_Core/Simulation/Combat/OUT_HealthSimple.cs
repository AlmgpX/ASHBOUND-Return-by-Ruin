using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class OUT_HealthSimple : MonoBehaviour, IOutDamageable
{
    [Serializable]
    public class OUT_HealthUnityEvent : UnityEvent { }

    [Header("Health")]
    [SerializeField][Min(1)] private int maxHealth = 100;
    [SerializeField] private int startingHealth = -1;
    [SerializeField] private bool resetHealthOnEnable = true;
    [SerializeField] private bool invulnerable = false;

    [Header("Death")]
    [SerializeField] private bool disableGameObjectOnDeath = false;
    [SerializeField] private bool destroyGameObjectOnDeath = false;
    [SerializeField][Min(0f)] private float destroyDelay = 0f;
    [SerializeField] private bool disableCollidersOnDeath = false;
    [SerializeField] private bool disableRigidbodiesOnDeath = false;
    [SerializeField] private bool disableBehavioursOnDeath = false;
    [SerializeField] private Collider[] collidersToDisable;
    [SerializeField] private Rigidbody[] rigidbodiesToDisable;
    [SerializeField] private Behaviour[] behavioursToDisable;

    [Header("Runtime")]
    [SerializeField] private int currentHealth;
    [SerializeField] private bool isDead;

    [Header("Events")]
    [SerializeField] private OUT_HealthUnityEvent onDamaged;
    [SerializeField] private OUT_HealthUnityEvent onDied;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public float Health01 => maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 0f;
    public bool IsAlive => !isDead && currentHealth > 0;
    public bool IsDead => isDead;
    public OUT_DamageContext LastDamageContext { get; private set; }

    public event Action<OUT_DamageContext> Damaged;
    public event Action<OUT_DamageContext> Died;

    private void Awake()
    {
        InitializeHealth();
    }

    private void OnEnable()
    {
        if (resetHealthOnEnable)
            InitializeHealth();
    }

    public bool CanTakeDamage(in OUT_DamageContext context)
    {
        if (invulnerable)
            return false;

        if (isDead)
            return false;

        return context.DamageAmount > 0;
    }

    public void ApplyDamage(in OUT_DamageContext context)
    {
        if (!CanTakeDamage(context))
            return;

        LastDamageContext = context;
        currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(0, context.DamageAmount));

        onDamaged?.Invoke();
        Damaged?.Invoke(context);

        if (currentHealth <= 0)
            Die(context);
    }

    public void Heal(int amount)
    {
        if (isDead)
            return;

        if (amount <= 0)
            return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
    }

    public void ResetHealth()
    {
        InitializeHealth();
    }

    public void Kill()
    {
        OUT_DamageContext context = new OUT_DamageContext(
            instigator: null,
            inflictor: gameObject,
            hitPoint: transform.position,
            hitNormal: Vector3.up,
            damageAmount: Mathf.Max(1, currentHealth),
            damageKind: OUT_DamageKind.Generic);

        ApplyDamage(context);
    }

    private void InitializeHealth()
    {
        isDead = false;
        currentHealth = startingHealth > 0 ? Mathf.Min(startingHealth, maxHealth) : maxHealth;
    }

    private void Die(in OUT_DamageContext context)
    {
        if (isDead)
            return;

        isDead = true;
        LastDamageContext = context;

        onDied?.Invoke();
        Died?.Invoke(context);

        if (disableCollidersOnDeath)
            DisableConfiguredColliders();

        if (disableRigidbodiesOnDeath)
            DisableConfiguredRigidbodies();

        if (disableBehavioursOnDeath)
            DisableConfiguredBehaviours();

        if (disableGameObjectOnDeath)
            gameObject.SetActive(false);

        if (destroyGameObjectOnDeath)
            Destroy(gameObject, destroyDelay);
    }

    private void DisableConfiguredColliders()
    {
        if (collidersToDisable != null && collidersToDisable.Length > 0)
        {
            for (int i = 0; i < collidersToDisable.Length; i++)
            {
                if (collidersToDisable[i] != null)
                    collidersToDisable[i].enabled = false;
            }

            return;
        }

        Collider[] ownColliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < ownColliders.Length; i++)
            ownColliders[i].enabled = false;
    }

    private void DisableConfiguredRigidbodies()
    {
        if (rigidbodiesToDisable != null && rigidbodiesToDisable.Length > 0)
        {
            for (int i = 0; i < rigidbodiesToDisable.Length; i++)
            {
                if (rigidbodiesToDisable[i] == null)
                    continue;

                rigidbodiesToDisable[i].velocity = Vector3.zero;
                rigidbodiesToDisable[i].angularVelocity = Vector3.zero;
                rigidbodiesToDisable[i].isKinematic = true;
            }

            return;
        }

        Rigidbody[] ownBodies = GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < ownBodies.Length; i++)
        {
            ownBodies[i].velocity = Vector3.zero;
            ownBodies[i].angularVelocity = Vector3.zero;
            ownBodies[i].isKinematic = true;
        }
    }

    private void DisableConfiguredBehaviours()
    {
        if (behavioursToDisable != null && behavioursToDisable.Length > 0)
        {
            for (int i = 0; i < behavioursToDisable.Length; i++)
            {
                if (behavioursToDisable[i] != null && behavioursToDisable[i] != this)
                    behavioursToDisable[i].enabled = false;
            }

            return;
        }

        Behaviour[] ownBehaviours = GetComponentsInChildren<Behaviour>(true);
        for (int i = 0; i < ownBehaviours.Length; i++)
        {
            if (ownBehaviours[i] != this)
                ownBehaviours[i].enabled = false;
        }
    }
}
