using UnityEngine;

public static class OUTL_Combat
{
    private static readonly Collider[] explosionBuffer = new Collider[128];
    private static readonly OUTL_EntityAdapter[] entityBuffer = new OUTL_EntityAdapter[128];

    public static bool TryGetEntityFromCollider(Collider collider, out OUTL_EntityAdapter adapter)
    {
        adapter = null;
        if (collider == null) return false;
        adapter = collider.GetComponentInParent<OUTL_EntityAdapter>();
        return adapter != null && adapter.Runtime != null;
    }

    public static bool ApplyDamage(OUTL_EntityId source, OUTL_EntityId target, float damage, Vector3 point, OUTL_EffectDef[] extraEffects = null)
    {
        return ApplyDamage(source, target, damage, point, string.Empty, extraEffects);
    }

    public static bool ApplyDamage(OUTL_EntityId source, OUTL_EntityId target, float damage, Vector3 point, string damageKey, OUTL_EffectDef[] extraEffects = null)
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null || !target.IsValid || damage <= 0f) return false;

        OUTL_EntityRuntime runtime;
        if (!world.Registry.TryGet(target, out runtime)) return false;
        if (runtime.Adapter != null && !OUTL_NetworkAuthority.CanApplyDamage(runtime.Adapter))
        {
            TraceBlocked("no_damage_authority", source, target, damageKey, point, null, runtime);
            OUTL_NetworkAuthority.TraceBlocked("damage", runtime.Adapter);
            return false;
        }

        EnsureVitalsInitialized(runtime);
        if (runtime.Dead || runtime.LifeState == OUTL_LifeState.Dead || runtime.State.GetFlag(OUTL_StateId.Dead))
        {
            TraceBlocked("target_dead", source, target, damageKey, point, null, runtime);
            return false;
        }

        float oldHealth = runtime.Stats.Get(OUTL_StatId.Health, 0f);
        if (oldHealth <= 0f)
        {
            TraceBlocked("target_zero_health", source, target, damageKey, point, null, runtime);
            return false;
        }

        OUTL_EntityRuntime sourceRuntime = null;
        if (source.IsValid)
        {
            world.Registry.TryGet(source, out sourceRuntime);
            if (sourceRuntime != null && (sourceRuntime.Dead || sourceRuntime.LifeState == OUTL_LifeState.Dead || sourceRuntime.State.GetFlag(OUTL_StateId.Dead)))
            {
                TraceBlocked("source_dead", source, target, damageKey, point, sourceRuntime, runtime);
                return false;
            }
            if (sourceRuntime != null && sourceRuntime != runtime && world.Factions.AreFriendly(sourceRuntime, runtime))
            {
                TraceBlocked("friendly_fire", source, target, damageKey, point, sourceRuntime, runtime);
                return false;
            }
        }

        OUTL_DamageModifierSet mods = runtime.Adapter != null ? runtime.Adapter.GetComponent<OUTL_DamageModifierSet>() : null;
        if (mods != null)
            damage = mods.ModifyDamage(damageKey, damage, runtime);
        OUTL_PlayerArmorEnergy armorEnergy = runtime.Adapter != null ? runtime.Adapter.GetComponent<OUTL_PlayerArmorEnergy>() : null;
        if (armorEnergy != null)
            damage = armorEnergy.ModifyIncomingDamage(damage);
        float armorAbsorbed = runtime.State.GetFloat("Player.LastArmorAbsorbed", 0f);
        if (damage <= 0f)
        {
            if (armorAbsorbed > 0.01f)
            {
                world.Events.Emit(new OUTL_Event(OUTL_EventType.Damaged, source, target) { Key = damageKey, FloatValue = 0f, Point = point });
                return true;
            }
            return false;
        }

        float newHealth = oldHealth - damage;
        runtime.Stats.Set(OUTL_StatId.Health, newHealth);
        TraceDamage(sourceRuntime, runtime, source, damage, oldHealth, newHealth, damageKey, point);

        world.Events.Emit(new OUTL_Event(OUTL_EventType.Damaged, source, target) { Key = damageKey, FloatValue = damage, Point = point });
        OUTL_StimulusBus.EmitCombat(source, point, Mathf.Max(8f, damage * 0.5f), Mathf.Max(0.1f, damage / 25f), Mathf.Clamp01(damage / 25f), damageKey);
        if (extraEffects != null) world.Effects.ApplyAll(extraEffects, source, target, point);

        if (oldHealth > 0f && newHealth <= 0f)
        {
            TraceKill(sourceRuntime, runtime, source, damageKey, point);
            OUTL_DeathRuntime.TryKill(runtime, source, damageKey, point, world);
        }

        return true;
    }

    private static void EnsureVitalsInitialized(OUTL_EntityRuntime runtime)
    {
        if (runtime == null || runtime.Adapter == null) return;
        OUTL_Vitals vitals = runtime.Adapter.GetComponent<OUTL_Vitals>();
        if (vitals != null) vitals.EnsureInitialized();
    }

    public static int ApplyExplosion(OUTL_EntityId source, OUTL_AttackProfile profile, Vector3 origin)
    {
        if (profile == null || !profile.HasExplosion) return 0;

        float radius = Mathf.Max(0.01f, profile.ExplosionRadius);
        int colliderCount = Physics.OverlapSphereNonAlloc(origin, radius, explosionBuffer, profile.ExplosionHitMask, QueryTriggerInteraction.Ignore);
        int entityCount = 0;
        int damaged = 0;

        for (int i = 0; i < colliderCount; i++)
        {
            Collider c = explosionBuffer[i];
            explosionBuffer[i] = null;
            OUTL_EntityAdapter entity;
            if (!TryGetEntityFromCollider(c, out entity)) continue;
            if (!entity.Id.IsValid || entity.Id == source) continue;
            if (ContainsEntity(entityBuffer, entityCount, entity)) continue;
            if (entityCount < entityBuffer.Length) entityBuffer[entityCount++] = entity;
        }

        for (int i = 0; i < entityCount; i++)
        {
            OUTL_EntityAdapter entity = entityBuffer[i];
            entityBuffer[i] = null;
            if (entity == null || entity.Runtime == null) continue;

            Vector3 targetPoint = GetExplosionTargetPoint(entity);
            Vector3 toTarget = targetPoint - origin;
            float distance = toTarget.magnitude;
            if (distance > radius) continue;

            if (profile.ExplosionRequireLineOfSight && distance > 0.05f)
            {
                RaycastHit hit;
                Vector3 dir = toTarget / distance;
                if (Physics.Raycast(origin, dir, out hit, distance, profile.ExplosionObstacleMask, QueryTriggerInteraction.Ignore))
                {
                    OUTL_EntityAdapter hitEntity;
                    if (!TryGetEntityFromCollider(hit.collider, out hitEntity) || hitEntity.Id != entity.Id)
                        continue;
                }
            }

            float damage = profile.ExplosionDamage * BuildExplosionMultiplier(profile.ExplosionFalloff, distance, radius);
            if (damage <= 0f) continue;
            if (ApplyDamage(source, entity.Id, damage, targetPoint, profile.ExplosionDamageKey, profile.ExtraHitEffects)) damaged++;
        }

        return damaged;
    }

    private static bool ContainsEntity(OUTL_EntityAdapter[] buffer, int count, OUTL_EntityAdapter entity)
    {
        for (int i = 0; i < count; i++)
            if (buffer[i] == entity)
                return true;
        return false;
    }

    private static Vector3 GetExplosionTargetPoint(OUTL_EntityAdapter entity)
    {
        Collider c = entity != null ? entity.GetComponentInChildren<Collider>() : null;
        if (c != null) return c.bounds.center;
        return entity != null ? entity.transform.position : Vector3.zero;
    }

    private static float BuildExplosionMultiplier(OUTL_ExplosionFalloff falloff, float distance, float radius)
    {
        float t = Mathf.Clamp01(distance / Mathf.Max(0.01f, radius));
        switch (falloff)
        {
            case OUTL_ExplosionFalloff.None: return 1f;
            case OUTL_ExplosionFalloff.Linear: return 1f - t;
            case OUTL_ExplosionFalloff.Smooth: return Mathf.SmoothStep(1f, 0f, t);
        }
        return 1f;
    }

    private static void TraceDamage(OUTL_EntityRuntime sourceRuntime, OUTL_EntityRuntime targetRuntime, OUTL_EntityId sourceId, float damage, float oldHealth, float newHealth, string damageKey, Vector3 point)
    {
        if (!OUTL_DebugLog.ShouldTraceCombat()) return;
        OUTL_DebugLog.TraceCombat("HIT " + DescribeEntity(sourceRuntime, sourceId) + " -> " + DescribeEntity(targetRuntime, targetRuntime != null ? targetRuntime.Id : OUTL_EntityId.None) + " dmg=" + damage.ToString("0.##") + " key=" + SafeKey(damageKey) + " hp=" + oldHealth.ToString("0.##") + "->" + newHealth.ToString("0.##") + " at=" + FormatPoint(point));
    }

    private static void TraceKill(OUTL_EntityRuntime sourceRuntime, OUTL_EntityRuntime targetRuntime, OUTL_EntityId sourceId, string damageKey, Vector3 point)
    {
        if (!OUTL_DebugLog.ShouldTraceCombat()) return;
        OUTL_DebugLog.TraceCombat("KILL " + DescribeEntity(sourceRuntime, sourceId) + " -> " + DescribeEntity(targetRuntime, targetRuntime != null ? targetRuntime.Id : OUTL_EntityId.None) + " key=" + SafeKey(damageKey) + " at=" + FormatPoint(point));
    }

    private static void TraceBlocked(string reason, OUTL_EntityId sourceId, OUTL_EntityId targetId, string damageKey, Vector3 point, OUTL_EntityRuntime sourceRuntime, OUTL_EntityRuntime targetRuntime)
    {
        if (!OUTL_DebugLog.ShouldTraceCombat()) return;
        OUTL_DebugLog.TraceCombat("BLOCK " + reason + " " + DescribeEntity(sourceRuntime, sourceId) + " -> " + DescribeEntity(targetRuntime, targetId) + " key=" + SafeKey(damageKey) + " at=" + FormatPoint(point));
    }

    private static string DescribeEntity(OUTL_EntityRuntime runtime, OUTL_EntityId fallbackId)
    {
        if (runtime == null)
            return fallbackId.IsValid ? "#" + fallbackId.Value : "world";

        string name = runtime.Adapter != null ? runtime.Adapter.name : "entity";
        string cls = string.IsNullOrEmpty(runtime.ClassName) ? "-" : runtime.ClassName;
        string targetName = string.IsNullOrEmpty(runtime.TargetName) ? "-" : runtime.TargetName;
        string faction = runtime.Faction != null ? runtime.Faction.FactionId : "-";
        return name + "#" + runtime.Id.Value + "(" + cls + "/" + targetName + "/f=" + faction + ")";
    }

    private static string SafeKey(string key)
    {
        return string.IsNullOrEmpty(key) ? "-" : key;
    }

    private static string FormatPoint(Vector3 point)
    {
        return "(" + point.x.ToString("0.##") + "," + point.y.ToString("0.##") + "," + point.z.ToString("0.##") + ")";
    }
}
