using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class OUTL_SurfaceVolume : MonoBehaviour
{
    public OUTL_SurfaceProfile Profile;
    public bool RequireEntity = true;
    public string[] RequiredTags;
    public float TickInterval = 0.25f;
    public bool ApplyDamage = true;
    public bool ApplyEffects = true;

    private readonly Collider[] buffer = new Collider[128];
    private float nextTick;
    private Collider volumeCollider;

    private void Awake()
    {
        volumeCollider = GetComponent<Collider>();
        if (volumeCollider != null) volumeCollider.isTrigger = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (Time.time < nextTick) return;
        nextTick = Time.time + Mathf.Max(0.02f, TickInterval);

        OUTL_EntityAdapter entity = other != null ? other.GetComponentInParent<OUTL_EntityAdapter>() : null;
        if (RequireEntity && entity == null) return;
        if (!TagsMatch(entity)) return;
        if (entity == null || entity.Runtime == null || Profile == null) return;

        float damage = Mathf.Max(0f, Profile.DamagePerSecond) * TickInterval;
        if (ApplyDamage && damage > 0f)
            OUTL_Combat.ApplyDamage(OUTL_EntityId.None, entity.Id, damage, entity.transform.position, Profile.DamageKey);

        if (ApplyEffects && OUTL_World.Instance != null)
            OUTL_World.Instance.Effects.ApplyAll(Profile.TickEffects, OUTL_EntityId.None, entity.Id, entity.transform.position);
    }

    private bool TagsMatch(OUTL_EntityAdapter entity)
    {
        if (RequiredTags == null || RequiredTags.Length == 0) return true;
        if (entity == null || entity.Runtime == null) return false;
        for (int i = 0; i < RequiredTags.Length; i++)
            if (entity.Runtime.HasTag(RequiredTags[i]))
                return true;
        return false;
    }
}
