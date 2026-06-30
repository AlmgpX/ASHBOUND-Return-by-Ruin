using UnityEngine;

[DisallowMultipleComponent]
public class OUT_ZombieTarget : MonoBehaviour
{
    [Header("Target")]
    public bool IsActiveTarget = true;
    public bool IsPrimaryObjective = false;
    [Min(0f)] public float Priority = 1f;
    [Min(0f)] public float Radius = 1f;

    [Header("Damage")]
    public bool ReceiveDamage = true;

    private IDamageable damageable;

    public Vector3 Position { get { return transform.position; } }
    public bool CanBeTargeted { get { return isActiveAndEnabled && IsActiveTarget && gameObject.activeInHierarchy; } }

    private void Awake()
    {
        damageable = GetComponent<IDamageable>();
        if (damageable == null) damageable = GetComponentInParent<IDamageable>();
    }

    private void OnEnable()
    {
        OUT_ZombieTargetHub.Register(this);
    }

    private void OnDisable()
    {
        OUT_ZombieTargetHub.Unregister(this);
    }

    public void ApplyZombieDamage(int damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (!ReceiveDamage || damage <= 0) return;
        if (damageable == null)
        {
            damageable = GetComponent<IDamageable>();
            if (damageable == null) damageable = GetComponentInParent<IDamageable>();
        }

        if (damageable != null)
            damageable.TakeDamage(damage, hitPoint, hitNormal);
    }
}
