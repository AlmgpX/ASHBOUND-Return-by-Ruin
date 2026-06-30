using UnityEngine;

[DisallowMultipleComponent]
public class OUTL_TouchTrigger : MonoBehaviour, OUTL_IComponentSaveParticipant
{
    public OUTL_EntityAdapter Entity;
    public OUTL_EventType EnterEvent = OUTL_EventType.Signal;
    public OUTL_EventType ExitEvent = OUTL_EventType.Signal;
    public string EnterEventName = "OnEnter";
    public string ExitEventName = "OnExit";
    public string Key = "touch";
    public OUTL_OutputLink[] Outputs;

    [Header("Input Filter")]
    public bool Once;
    public bool RequireEntity = true;
    public string[] RequiredTags;

    private bool fired;

    public string OUTL_SaveKey { get { return "OUTL_TouchTrigger"; } }

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Touch(other, true);
    }

    private void OnTriggerExit(Collider other)
    {
        Touch(other, false);
    }

    private void Touch(Collider other, bool enter)
    {
        if (Once && fired) return;
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return;

        OUTL_EntityAdapter otherEntity = other != null ? other.GetComponentInParent<OUTL_EntityAdapter>() : null;
        if (RequireEntity && otherEntity == null) return;
        if (!TagsMatch(otherEntity)) return;

        OUTL_EntityId source = otherEntity != null ? otherEntity.Id : OUTL_EntityId.None;
        OUTL_EntityId self = Entity != null ? Entity.Id : OUTL_EntityId.None;
        Vector3 point = other != null ? other.transform.position : transform.position;
        string eventName = enter ? EnterEventName : ExitEventName;

        OUTL_OutputDispatcher.Fire(world, source, this, point, Outputs, eventName, Key);

        OUTL_EventType evt = enter ? EnterEvent : ExitEvent;
        if (evt != OUTL_EventType.None)
            world.Events.Emit(new OUTL_Event(evt, source, self) { Key = Key, Point = point });

        if (enter) fired = true;
    }

    private bool TagsMatch(OUTL_EntityAdapter otherEntity)
    {
        if (RequiredTags == null || RequiredTags.Length == 0) return true;
        if (otherEntity == null || otherEntity.Runtime == null) return false;
        for (int i = 0; i < RequiredTags.Length; i++)
            if (otherEntity.Runtime.HasTag(RequiredTags[i]))
                return true;
        return false;
    }

    [ContextMenu("OUT Reset Trigger")]
    public void ResetTrigger()
    {
        fired = false;
        OUTL_OutputDispatcher.ResetOnceFlags(Outputs);
    }

    public void OUTL_Capture(OUTL_ComponentSaveWriter writer)
    {
        if (writer == null) return;
        writer.SetInt("fired", fired ? 1 : 0);
        OUTL_OutputSaveUtility.Capture(writer, "outputs", Outputs);
    }

    public void OUTL_Restore(OUTL_ComponentSaveReader reader)
    {
        if (reader == null) return;
        fired = reader.GetInt("fired", 0) != 0;
        OUTL_OutputSaveUtility.Restore(reader, "outputs", Outputs);
    }
}
