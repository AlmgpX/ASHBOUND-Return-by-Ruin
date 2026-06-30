using System.Reflection;
using UnityEngine;
using Time = OUT_SimTime;

[DisallowMultipleComponent]
public class OUT_SoldierTacticalEvaluator : MonoBehaviour
{
    [Header("Mind")]
    [SerializeField] private OUT_EntityMind mind;
    [SerializeField] private OUT_EntityMindProfile fallbackMindProfile;

    [Header("Tactical Bias")]
    [SerializeField][Range(0f, 1f)] private float suppressBias = 0.55f;
    [SerializeField][Range(0f, 1f)] private float coverBias = 0.45f;
    [SerializeField][Range(0f, 1f)] private float fallbackBias = 0.35f;
    [SerializeField][Range(0f, 1f)] private float repositionBias = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool logIntentChanges = false;
    [SerializeField] private float minLogInterval = 0.25f;

    private OUT_AITacticalIntent lastIntent = OUT_AITacticalIntent.None;
    private string lastReason = string.Empty;
    private float lastLogTime;

    public OUT_AITacticalIntent LastIntent { get { return lastIntent; } }
    public string LastReason { get { return lastReason; } }

    private void Awake()
    {
        if (mind == null)
            mind = GetComponent<OUT_EntityMind>();
    }

    public OUT_AITacticalIntent Evaluate(
        OUT_AIState state,
        OUT_AIConditionFlags conditions,
        OUT_AIBlackboard blackboard,
        bool canShootPrimary,
        bool canShootSecondary,
        bool forcedFallback,
        bool shouldTakeCoverFirst,
        bool needsReposition,
        out string reason)
    {
        OUT_EntityMindProfile profile = mind != null ? mind.Profile : fallbackMindProfile;

        float aggressionBase = GetProfile01(profile, "Aggression", 0.5f);
        float shadow = GetProfile01(profile, "Shadow", aggressionBase);
        float cowardice = GetProfile01(profile, "Cowardice", 0.35f);
        float egoStrength = GetProfile01(profile, "EgoStrength", 0.5f);
        float disciplineBase = GetProfile01(profile, "Discipline", 0.5f);
        float persona = GetProfile01(profile, "Persona", disciplineBase);
        float thinking = GetProfile01(profile, "Thinking", disciplineBase);
        float curiosityBase = GetProfile01(profile, "Curiosity", 0.35f);
        float intuition = GetProfile01(profile, "Intuition", curiosityBase);
        float dangerSensitivity = GetProfile01(profile, "DangerSensitivity", GetProfile01(profile, "Fear", cowardice));

        float aggression = Mathf.Clamp01((aggressionBase + shadow) * 0.5f);
        float caution = Mathf.Clamp01((cowardice + (1f - egoStrength) + dangerSensitivity) / 3f);
        float discipline = Mathf.Clamp01((disciplineBase + persona + thinking) / 3f);
        float curiosity = Mathf.Clamp01((curiosityBase + intuition) * 0.5f);

        bool heavyDamage = (conditions & OUT_AIConditionFlags.HeavyDamage) != 0;
        bool lightDamage = (conditions & OUT_AIConditionFlags.LightDamage) != 0;
        bool noAmmo = (conditions & OUT_AIConditionFlags.NoAmmoLoaded) != 0;
        bool hasEnemy = blackboard != null && blackboard.Enemy != null;
        bool hasLkp = blackboard != null && blackboard.EnemyLastKnownPosition != Vector3.zero;
        bool hasInterest = blackboard != null && blackboard.InterestPoint != Vector3.zero && blackboard.InterestStrength > 0.05f;

        OUT_AITacticalIntent intent;

        if (heavyDamage && caution + coverBias >= aggression)
        {
            intent = OUT_AITacticalIntent.TakeCover;
            reason = "heavy damage + caution";
            return Commit(intent, reason);
        }

        if (noAmmo)
        {
            intent = OUT_AITacticalIntent.ReloadSafe;
            reason = "no ammo";
            return Commit(intent, reason);
        }

        if (!hasEnemy)
        {
            if (hasLkp)
            {
                intent = OUT_AITacticalIntent.Hunt;
                reason = "enemy lost, has LKP";
                return Commit(intent, reason);
            }

            if (hasInterest && curiosity >= 0.25f)
            {
                intent = OUT_AITacticalIntent.Investigate;
                reason = "interest + curiosity";
                return Commit(intent, reason);
            }

            intent = OUT_AITacticalIntent.Idle;
            reason = "no enemy";
            return Commit(intent, reason);
        }

        if (forcedFallback && (caution + fallbackBias > aggression || heavyDamage || lightDamage))
        {
            intent = OUT_AITacticalIntent.Fallback;
            reason = "enemy too close / fallback pressure";
            return Commit(intent, reason);
        }

        if (canShootSecondary && aggression >= 0.35f)
        {
            intent = OUT_AITacticalIntent.Suppress;
            reason = "secondary ready + aggression";
            return Commit(intent, reason);
        }

        if (shouldTakeCoverFirst && discipline + coverBias >= aggression)
        {
            intent = OUT_AITacticalIntent.TakeCover;
            reason = "order/discipline prefers cover";
            return Commit(intent, reason);
        }

        if (canShootPrimary)
        {
            float suppressScore = aggression * suppressBias + discipline * 0.25f;
            float burstScore = aggression * 0.65f + discipline * 0.2f;

            if (suppressScore > burstScore + 0.1f)
            {
                intent = OUT_AITacticalIntent.Suppress;
                reason = "aggressive suppress";
                return Commit(intent, reason);
            }

            intent = OUT_AITacticalIntent.BurstFire;
            reason = "clear primary shot";
            return Commit(intent, reason);
        }

        if (needsReposition)
        {
            intent = OUT_AITacticalIntent.Reposition;
            reason = "no shot + bad position";
            return Commit(intent, reason);
        }

        intent = OUT_AITacticalIntent.CombatWait;
        reason = "combat hold";
        return Commit(intent, reason);
    }

    private static float GetProfile01(OUT_EntityMindProfile profile, string memberName, float fallback)
    {
        if (profile == null || string.IsNullOrEmpty(memberName))
            return Mathf.Clamp01(fallback);

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        System.Type type = profile.GetType();

        FieldInfo field = type.GetField(memberName, flags);
        if (field != null)
        {
            object value = field.GetValue(profile);
            if (value is float f) return Mathf.Clamp01(f);
            if (value is int i) return Mathf.Clamp01(i);
        }

        PropertyInfo property = type.GetProperty(memberName, flags);
        if (property != null && property.CanRead)
        {
            object value = property.GetValue(profile, null);
            if (value is float f) return Mathf.Clamp01(f);
            if (value is int i) return Mathf.Clamp01(i);
        }

        return Mathf.Clamp01(fallback);
    }

    private OUT_AITacticalIntent Commit(OUT_AITacticalIntent intent, string reason)
    {
        if (intent != lastIntent || reason != lastReason)
        {
            lastIntent = intent;
            lastReason = reason;

            if (logIntentChanges && Time.time - lastLogTime >= minLogInterval)
            {
                lastLogTime = Time.time;
                OUT_AIDebugLogService.Log(this, OUT_AIDebugLogService.AIEventKind.Brain, "intent " + intent + " reason:" + reason);
            }
        }

        return intent;
    }
}
