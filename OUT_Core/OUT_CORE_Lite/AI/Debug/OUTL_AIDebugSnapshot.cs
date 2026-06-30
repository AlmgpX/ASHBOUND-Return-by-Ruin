using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct OUTL_AIDebugSnapshotRow
{
    public string Entity;
    public OUTL_AIStateId State;
    public OUTL_TacticalIntentId TacticalIntent;
    public string Goal;
    public string Stimulus;
    public string Target;
    public string WeaponProfile;
    public string AimCommand;
    public float Health;
    public float Fear;
    public float Aggression;
    public float Morale;
    public float Distance;
    public bool Visible;
    public string Cover;
    public string SquadOrder;
    public string LastEvent;
}

[DisallowMultipleComponent]
public sealed class OUTL_AIDebugSnapshot : MonoBehaviour
{
    public int MaxRows = 64;
    public OUTL_AIDebugSnapshotRow[] Rows = new OUTL_AIDebugSnapshotRow[64];
    public int RowCount;

    private readonly List<OUTL_EntityRuntime> entities = new List<OUTL_EntityRuntime>(128);

    [ContextMenu("OUT Refresh AI Debug Snapshot")]
    public void Refresh()
    {
        RowCount = 0;
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return;
        world.Registry.CopyAll(entities);
        int limit = Mathf.Min(Mathf.Max(1, MaxRows), Rows != null ? Rows.Length : 0);
        for (int i = 0; i < entities.Count && RowCount < limit; i++)
        {
            OUTL_EntityRuntime runtime = entities[i];
            if (runtime == null || runtime.Adapter == null) continue;
            OUTL_AIActor ai = runtime.Adapter.GetComponent<OUTL_AIActor>();
            OUTL_TacticalPlanner tactical = runtime.Adapter.GetComponent<OUTL_TacticalPlanner>();
            OUTL_AimPlanner aim = runtime.Adapter.GetComponent<OUTL_AimPlanner>();
            if (ai == null && tactical == null) continue;

            OUTL_AIDebugSnapshotRow row = default(OUTL_AIDebugSnapshotRow);
            row.Entity = !string.IsNullOrEmpty(runtime.TargetName) ? runtime.TargetName : (!string.IsNullOrEmpty(runtime.ClassName) ? runtime.ClassName : runtime.Id.ToString());
            row.State = ai != null ? ai.CurrentState : OUTL_AIStateId.Idle;
            row.TacticalIntent = tactical != null ? tactical.CurrentDecision.Intent : OUTL_TacticalIntentId.None;
            row.Goal = ai != null ? ai.CurrentGoal : "";
            row.Stimulus = ai != null ? ai.LastStimulusType + ":" + ai.LastStimulusKey : "";
            row.Target = ai != null && ai.CurrentTarget.IsValid ? ai.CurrentTarget.ToString() : "-";
            row.WeaponProfile = tactical != null && tactical.CurrentWeaponSelection.AttackProfile != null ? tactical.CurrentWeaponSelection.AttackProfile.name : (ai != null && ai.CurrentAttackProfile != null ? ai.CurrentAttackProfile.name : "-");
            row.AimCommand = aim != null ? aim.CurrentState.Command.ToString() + ":" + aim.CurrentState.Reason : "-";
            row.Health = runtime.Stats.Get(OUTL_StatId.Health, 0f);
            row.Fear = ai != null ? ai.CurrentFear : 0f;
            row.Aggression = ai != null ? ai.CurrentAggression : 0f;
            row.Morale = ai != null ? ai.CurrentMorale : 0f;
            row.Distance = ai != null ? ai.CurrentTargetDistance : 0f;
            row.Visible = ai != null && ai.CurrentTargetVisible;
            row.Cover = ai != null && ai.CurrentCover != null ? ai.CurrentCover.name : "-";
            row.SquadOrder = ai != null ? ai.CurrentOrder.Type.ToString() : "-";
            row.LastEvent = ai != null ? ai.LastEvent : "";
            Rows[RowCount++] = row;
        }
    }
}
