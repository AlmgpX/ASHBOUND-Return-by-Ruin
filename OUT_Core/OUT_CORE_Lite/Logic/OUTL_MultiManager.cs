using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_MultiManager : MonoBehaviour, OUTL_ICommandReceiver, OUTL_IComponentSaveParticipant
{
    public OUTL_EntityAdapter Entity;
    public OUTL_OutputLink[] Outputs;
    public string DefaultEventName = "OnTrigger";
    public bool DebugLog;

    public string OUTL_SaveKey { get { return "OUTL_MultiManager"; } }

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
    }

    public bool OUTL_CanReceive(in OUTL_Command command, OUTL_World world)
    {
        return command.Type == OUTL_CommandType.Use || command.Type == OUTL_CommandType.Activate || command.Type == OUTL_CommandType.Deactivate || command.Type == OUTL_CommandType.SendSignal || command.Type == OUTL_CommandType.Custom || command.Type == OUTL_CommandType.Open || command.Type == OUTL_CommandType.Close;
    }

    public void OUTL_Receive(in OUTL_Command command, OUTL_World world)
    {
        if (world == null) world = OUTL_World.Instance;
        if (world == null) return;

        OUTL_EntityId source = Entity != null ? Entity.Id : command.Source;
        string eventName = !string.IsNullOrEmpty(command.Key) ? command.Key : (!string.IsNullOrEmpty(DefaultEventName) ? DefaultEventName : command.Type.ToString());
        int fired = OUTL_OutputDispatcher.Fire(world, source, this, transform.position, Outputs, eventName, eventName, command.IntValue, command.IntValue != 0);

        if (DebugLog)
            OUTL_DebugLog.Log(OUTL_DebugChannel.Events, "[MULTI_MANAGER] " + name + " event=" + eventName + " fired=" + fired, true);
    }

    [ContextMenu("OUT Reset Output Once Flags")]
    public void ResetOnceFlags()
    {
        OUTL_OutputDispatcher.ResetOnceFlags(Outputs);
    }

    public void OUTL_Capture(OUTL_ComponentSaveWriter writer)
    {
        OUTL_OutputSaveUtility.Capture(writer, "outputs", Outputs);
    }

    public void OUTL_Restore(OUTL_ComponentSaveReader reader)
    {
        OUTL_OutputSaveUtility.Restore(reader, "outputs", Outputs);
    }
}
