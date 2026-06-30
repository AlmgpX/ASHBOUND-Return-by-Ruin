using System;
using UnityEngine;

public enum OUTL_AIStateId : byte
{
    Idle = 0,
    Patrol = 1,
    Work = 2,
    Investigate = 3,
    Alert = 4,
    Search = 5,
    TakeCover = 6,
    AttackRanged = 7,
    AttackMelee = 8,
    SwitchWeapon = 9,
    EatOrUseResource = 10,
    Flee = 11,
    Dead = 12
}

[Serializable]
public class OUTL_AIStateTableRow
{
    public OUTL_AIStateId State = OUTL_AIStateId.Idle;
    public string EntryConditions;
    public string ExitConditions;
    public string Interrupts;
    public string MainCommand;
    public string TargetRule;
    public string MovementRule;
    public OUTL_AttackProfile AttackProfile;
    public string AnimationHint;
    public Color DebugColor = Color.white;
    [TextArea] public string Notes;
}

[CreateAssetMenu(menuName = "OUT CORE Lite/AI/State Table", fileName = "OUTL_AIStateTable")]
public class OUTL_AIStateTable : ScriptableObject
{
    public OUTL_AIStateTableRow[] Rows;

    public bool TryGetRow(OUTL_AIStateId state, out OUTL_AIStateTableRow row)
    {
        row = null;
        if (Rows == null) return false;
        for (int i = 0; i < Rows.Length; i++)
        {
            OUTL_AIStateTableRow candidate = Rows[i];
            if (candidate != null && candidate.State == state)
            {
                row = candidate;
                return true;
            }
        }
        return false;
    }

    public static Color DefaultColor(OUTL_AIStateId state)
    {
        switch (state)
        {
            case OUTL_AIStateId.Patrol: return new Color(0.3f, 0.75f, 1f, 1f);
            case OUTL_AIStateId.Work: return new Color(0.45f, 0.85f, 0.65f, 1f);
            case OUTL_AIStateId.Investigate: return new Color(1f, 0.8f, 0.25f, 1f);
            case OUTL_AIStateId.Alert: return new Color(1f, 0.55f, 0.15f, 1f);
            case OUTL_AIStateId.Search: return new Color(0.9f, 0.65f, 1f, 1f);
            case OUTL_AIStateId.TakeCover: return new Color(0.35f, 0.45f, 1f, 1f);
            case OUTL_AIStateId.AttackRanged: return new Color(1f, 0.25f, 0.15f, 1f);
            case OUTL_AIStateId.AttackMelee: return new Color(1f, 0.1f, 0.05f, 1f);
            case OUTL_AIStateId.SwitchWeapon: return new Color(1f, 0.7f, 0.2f, 1f);
            case OUTL_AIStateId.EatOrUseResource: return new Color(0.25f, 1f, 0.35f, 1f);
            case OUTL_AIStateId.Flee: return new Color(0.2f, 0.9f, 1f, 1f);
            case OUTL_AIStateId.Dead: return new Color(0.35f, 0.35f, 0.35f, 1f);
            case OUTL_AIStateId.Idle:
            default: return Color.white;
        }
    }
}

[CreateAssetMenu(menuName = "OUT CORE Lite/AI/Perception Profile", fileName = "OUTL_AIPerceptionProfile")]
public partial class OUTL_AIPerceptionProfile
{
    [Header("Sight")]
    public float SightConeAngle = 120f;
    public float SightDistance = 30f;
    public bool RequireLineOfSight = true;
    public LayerMask SightBlockMask = ~0;

    [Header("Hearing / Memory")]
    public float HearingRadius = 16f;
    public float DangerRadius = 18f;
    public float FoodRadius = 12f;
    public float MemoryDuration = 8f;

    [Header("Filters")]
    public bool UseFactionFilter = true;
    public bool UseProfileEnemyTags = true;
    public string[] DangerTags;
    public string[] FoodTags;
    public float TargetPriority = 1f;
}

public struct OUTL_AIStateDebugRow
{
    public string Entity;
    public OUTL_AIStateId State;
    public string Goal;
    public string Stimulus;
    public string Target;
    public string Weapon;
    public string AttackProfile;
    public string AnimationHint;
    public float Health;
    public float Fear;
    public float Aggression;
    public float Morale;
    public float Suspicion;
    public float Distance;
    public bool Visibility;
    public float Danger;
    public float Food;
    public string NextAction;
    public string LastEvent;
    public Color DebugColor;
}
