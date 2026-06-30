using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_PathNode : MonoBehaviour, OUTL_IComponentSaveParticipant
{
    public string NodeId;
    public OUTL_PathNode Next;
    public OUTL_PathNode Previous;

    [Header("Motion Overrides")]
    public bool OverrideSpeed;
    public float Speed = 3f;
    public bool OverrideWait;
    public float Wait = 0f;
    public bool PauseUntilTriggered;
    public bool TeleportToThisNode;
    public bool SnapRotationToNode;
    public bool UseNodeRotation;

    [Header("Commands On Arrival")]
    public OUTL_EntityAdapter[] Targets;
    public OUTL_CommandType CommandOnArrival = OUTL_CommandType.Activate;
    public string CommandKey = "path_node";
    public bool FireOnce;

    private bool fired;

    public string OUTL_SaveKey { get { return "OUTL_PathNode:" + GetStableNodeId(); } }
    public Vector3 Position { get { return transform.position; } }
    public Quaternion Rotation { get { return transform.rotation; } }

    public void FireArrival(OUTL_EntityId source)
    {
        if (FireOnce && fired) return;
        fired = true;

        OUTL_World world = OUTL_World.Instance;
        if (world == null || Targets == null) return;

        for (int i = 0; i < Targets.Length; i++)
        {
            OUTL_EntityAdapter target = Targets[i];
            if (target == null || !target.Id.IsValid) continue;
            world.Commands.Send(new OUTL_Command(CommandOnArrival, source, target.Id)
            {
                Key = CommandKey,
                Point = transform.position,
                Context = this
            });
        }
    }

    public void OUTL_Capture(OUTL_ComponentSaveWriter writer)
    {
        if (writer == null) return;
        writer.SetInt("fired", fired ? 1 : 0);
    }

    public void OUTL_Restore(OUTL_ComponentSaveReader reader)
    {
        if (reader == null) return;
        fired = reader.GetInt("fired", 0) != 0;
    }

    public string GetStableNodeId()
    {
        return !string.IsNullOrEmpty(NodeId) ? NodeId : name;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.18f);
        if (Next != null)
        {
            Gizmos.DrawLine(transform.position, Next.transform.position);
            Vector3 dir = (Next.transform.position - transform.position);
            if (dir.sqrMagnitude > 0.001f)
            {
                dir.Normalize();
                Vector3 p = Vector3.Lerp(transform.position, Next.transform.position, 0.82f);
                Gizmos.DrawLine(p, p - Quaternion.Euler(0f, 25f, 0f) * dir * 0.35f);
                Gizmos.DrawLine(p, p - Quaternion.Euler(0f, -25f, 0f) * dir * 0.35f);
            }
        }
    }
}
