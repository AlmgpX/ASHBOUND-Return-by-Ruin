using UnityEngine;

[DisallowMultipleComponent]
public class OUT_SoldierAttackEvaluator : OUT_AIAttackEvaluator
{
    [Header("References")]
    [SerializeField] private OUT_SoldierSquadAgent squadAgent;
    [SerializeField] private OUT_WeaponController weapon;
    [SerializeField] private OUT_HealthSimple health;
    [SerializeField] private OUT_SoldierVoiceBarks voiceBarks;

    [Header("Perception")]
    [SerializeField] private LayerMask visibilityMask = ~0;
    [SerializeField] private QueryTriggerInteraction visibilityTriggers = QueryTriggerInteraction.Ignore;
    [SerializeField][Min(0.1f)] private float maxViewDistance = 80f;
    [SerializeField][Min(0f)] private float retreatDistance = 6f;
    [SerializeField][Min(0f)] private float friendlyFireRadius = 1.4f;

    [Header("Explosives")]
    [SerializeField] private bool allowSecondaryExplosives = true;
    [SerializeField][Min(0f)] private float secondaryMinCooldown = 1.25f;

    [Header("Health")]
    [SerializeField][Range(0.05f, 1f)] private float heavyDamageHealth01 = 0.35f;
    [SerializeField][Range(0.05f, 1f)] private float lightDamageHealth01 = 0.65f;

    private float _nextSecondaryTime;
    private bool _hadEnemy;
    private bool _wasHeavyDamage;
    private bool _wasLightDamage;

    private void Awake()
    {
        if (squadAgent == null)
            squadAgent = GetComponent<OUT_SoldierSquadAgent>();

        if (weapon == null)
            weapon = GetComponent<OUT_WeaponController>();

        if (health == null)
            health = GetComponent<OUT_HealthSimple>();

        if (voiceBarks == null)
            voiceBarks = GetComponent<OUT_SoldierVoiceBarks>();
    }

    public override void Evaluate(OUT_AIBlackboard blackboard, ref OUT_AIConditionFlags conditions, OUT_AIState currentState)
    {
        if (blackboard == null)
            return;

        GameObject enemy = blackboard.Enemy;
        if (enemy == null)
        {
            if (_hadEnemy)
                voiceBarks?.PlayLostEnemy(0.45f);

            _hadEnemy = false;

            if (blackboard.EnemyLastKnownPosition != Vector3.zero)
                conditions |= OUT_AIConditionFlags.HasEnemyLKP;

            ApplyAmmoConditions(ref conditions);
            ApplyHealthConditions(ref conditions);
            return;
        }

        if (!_hadEnemy)
            voiceBarks?.PlayEnemySpotted(0.8f);

        _hadEnemy = true;

        Vector3 targetPoint = GetTargetPoint(enemy.transform);
        float distance = Vector3.Distance(transform.position, targetPoint);
        blackboard.EnemyLastKnownPosition = targetPoint;
        blackboard.LastEnemySeenTime = Time.time;

        conditions |= OUT_AIConditionFlags.SeeEnemy | OUT_AIConditionFlags.HasEnemyLKP;

        if (distance > maxViewDistance)
            conditions |= OUT_AIConditionFlags.EnemyTooFar;

        if (distance <= retreatDistance)
            conditions |= OUT_AIConditionFlags.EnemyFacingMe;

        if (squadAgent != null && squadAgent.Commander != null)
            squadAgent.Commander.ReportEnemy(targetPoint);

        bool hasLineOfFire = HasLineOfFire(targetPoint, enemy.transform.root);
        bool safeShot = !HasFriendlyFireRisk(targetPoint);

        if (safeShot)
            conditions |= OUT_AIConditionFlags.NoFriendlyFire;

        ApplyAmmoConditions(ref conditions);
        ApplyHealthConditions(ref conditions);

        if (blackboard.CoverPoint != Vector3.zero && Vector3.Distance(transform.position, blackboard.CoverPoint) <= 1.25f)
            conditions |= OUT_AIConditionFlags.InCover;

        if (weapon == null)
            return;

        float preferredMax = squadAgent != null ? squadAgent.PreferredMaxRange : 24f;
        float preferredMin = squadAgent != null ? squadAgent.PreferredMinRange : 8f;

        bool canPrimary = hasLineOfFire && safeShot && distance <= preferredMax;
        if (canPrimary)
            conditions |= OUT_AIConditionFlags.CanRangeAttack1;

        if (allowSecondaryExplosives && Time.time >= _nextSecondaryTime && hasLineOfFire)
        {
            bool roleAllows = squadAgent == null || squadAgent.CanUseExplosives(distance);
            bool commanderAllows = squadAgent == null || squadAgent.Commander == null || squadAgent.Commander.CanUseExplosives(squadAgent, targetPoint);
            bool inRange = squadAgent == null || (distance >= squadAgent.ExplosiveMinRange && distance <= squadAgent.ExplosiveMaxRange);
            bool secondaryReady = weapon.Secondary.HasAmmoInClip || weapon.Secondary.InfiniteClip;

            if (roleAllows && commanderAllows && inRange && secondaryReady)
                conditions |= OUT_AIConditionFlags.CanRangeAttack2;
        }

        if (distance < preferredMin)
            conditions |= OUT_AIConditionFlags.EnemyFacingMe;
    }

    public void NotifySecondaryFired()
    {
        _nextSecondaryTime = Time.time + secondaryMinCooldown;
    }

    private void ApplyAmmoConditions(ref OUT_AIConditionFlags conditions)
    {
        if (weapon == null)
            return;

        if (!weapon.Primary.HasAmmoInClip)
            conditions |= OUT_AIConditionFlags.NoAmmoLoaded;
    }

    private void ApplyHealthConditions(ref OUT_AIConditionFlags conditions)
    {
        float health01 = GetHealth01();

        bool heavy = health01 <= heavyDamageHealth01;
        bool light = !heavy && health01 <= lightDamageHealth01;

        if (heavy)
        {
            conditions |= OUT_AIConditionFlags.HeavyDamage;
            if (!_wasHeavyDamage)
                voiceBarks?.PlayHeavyDamage(0.85f);
        }
        else if (light)
        {
            conditions |= OUT_AIConditionFlags.LightDamage;
            if (!_wasLightDamage)
                voiceBarks?.PlayLightDamage(0.35f);
        }

        _wasHeavyDamage = heavy;
        _wasLightDamage = light;
    }

    private float GetHealth01()
    {
        if (health == null || health.MaxHealth <= 0)
            return 1f;

        return Mathf.Clamp01((float)health.CurrentHealth / health.MaxHealth);
    }

    private bool HasLineOfFire(Vector3 targetPoint, Transform targetRoot)
    {
        Vector3 origin = transform.position + Vector3.up * 1.4f;
        Vector3 dir = targetPoint - origin;
        float distance = dir.magnitude;
        if (distance <= 0.001f)
            return true;

        dir /= distance;

        if (!Physics.Raycast(origin, dir, out RaycastHit hit, distance, visibilityMask, visibilityTriggers))
            return true;

        return hit.transform.root == targetRoot;
    }

    private bool HasFriendlyFireRisk(Vector3 targetPoint)
    {
        if (squadAgent == null || squadAgent.Commander == null)
            return false;

        Vector3 origin = transform.position + Vector3.up * 1.2f;
        Vector3 dir = targetPoint - origin;
        float length = dir.magnitude;
        if (length <= 0.001f)
            return false;

        dir /= length;
        float radius = Mathf.Max(0.1f, friendlyFireRadius);

        OUT_SoldierSquadCommander commander = squadAgent.Commander;
        for (int i = 0; i < 8; i++)
        {
            OUT_SoldierSquadAgent mate = commander.GetAgentAt(i);
            if (mate == null || mate == squadAgent || !mate.isActiveAndEnabled)
                continue;

            Vector3 point = mate.transform.position + Vector3.up * 1.1f;
            float distanceToLine = DistancePointToSegment(point, origin, targetPoint);
            if (distanceToLine <= radius)
                return true;
        }

        return false;
    }

    private float DistancePointToSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float denominator = Vector3.Dot(ab, ab);
        if (denominator <= 0.0001f)
            return Vector3.Distance(point, a);

        float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / denominator);
        Vector3 closest = a + ab * t;
        return Vector3.Distance(point, closest);
    }

    private Vector3 GetTargetPoint(Transform target)
    {
        Collider col = target.GetComponentInChildren<Collider>();
        if (col != null)
            return col.bounds.center;

        return target.position + Vector3.up;
    }
}
