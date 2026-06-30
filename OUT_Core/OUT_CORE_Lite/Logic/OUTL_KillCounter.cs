using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_KillCounter : MonoBehaviour, OUTL_IEventListener, OUTL_ICommandReceiver, OUTL_IComponentSaveParticipant
{
    public OUTL_EntityAdapter Entity;
    public string CounterId = "kill_counter";
    public string[] RequiredTargetTags;
    public int RequiredKills = 10;
    public int CurrentKills;
    public bool CountOnlyOnce = true;
    public bool ResetOnDeactivate;
    public string CountStateKey = "KillCount";
    public string CompleteFlag = "Complete";
    public string CompletedEventName = "OnCompleted";
    public OUTL_OutputLink[] Outputs;

    private bool completed;
    private bool registered;

    public string OUTL_SaveKey { get { return "OUTL_KillCounter"; } }

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
    }

    private void OnEnable()
    {
        Register();
    }

    private void OnDisable()
    {
        Unregister();
    }

    public bool OUTL_CanReceive(in OUTL_Command command, OUTL_World world)
    {
        return command.Type == OUTL_CommandType.Activate || command.Type == OUTL_CommandType.Deactivate || command.Type == OUTL_CommandType.Use || command.Type == OUTL_CommandType.Custom || command.Type == OUTL_CommandType.SendSignal;
    }

    public void OUTL_Receive(in OUTL_Command command, OUTL_World world)
    {
        if (command.Type == OUTL_CommandType.Deactivate && ResetOnDeactivate)
        {
            ResetCounter();
            return;
        }

        if (command.Type == OUTL_CommandType.Activate || command.Type == OUTL_CommandType.Use || command.Type == OUTL_CommandType.SendSignal || command.Type == OUTL_CommandType.Custom)
            Register();
    }

    public void OUTL_OnEvent(in OUTL_Event evt, OUTL_World world)
    {
        if (world == null || evt.Type != OUTL_EventType.Killed) return;
        if (CountOnlyOnce && completed) return;

        OUTL_EntityRuntime target;
        if (!world.Registry.TryGet(evt.Target, out target)) return;
        if (!MatchesTags(target)) return;

        CurrentKills++;
        WriteState();

        if (CurrentKills >= Mathf.Max(1, RequiredKills))
            Complete(world, evt);
    }

    [ContextMenu("OUT Reset Kill Counter")]
    public void ResetCounter()
    {
        CurrentKills = 0;
        completed = false;
        OUTL_OutputDispatcher.ResetOnceFlags(Outputs);
        WriteState();
    }

    private void Complete(OUTL_World world, in OUTL_Event evt)
    {
        if (completed && CountOnlyOnce) return;
        completed = true;
        WriteState();

        OUTL_EntityId source = Entity != null ? Entity.Id : evt.Source;
        OUTL_OutputDispatcher.Fire(world, source, this, evt.Point, Outputs, CompletedEventName, CounterId, CurrentKills, true);
    }

    private void Register()
    {
        OUTL_World world = OUTL_World.Instance;
        if (registered || world == null) return;
        world.Events.Register(this, OUTL_EventType.Killed);
        registered = true;
        WriteState();
    }

    private void Unregister()
    {
        OUTL_World world = OUTL_World.Instance;
        if (!registered || world == null) return;
        world.Events.Unregister(this);
        registered = false;
    }

    private bool MatchesTags(OUTL_EntityRuntime target)
    {
        if (RequiredTargetTags == null || RequiredTargetTags.Length == 0) return true;
        if (target == null) return false;
        for (int i = 0; i < RequiredTargetTags.Length; i++)
        {
            string tag = RequiredTargetTags[i];
            if (!string.IsNullOrEmpty(tag) && target.HasTag(tag)) return true;
        }
        return false;
    }

    private void WriteState()
    {
        if (Entity == null || Entity.Runtime == null) return;
        Entity.Runtime.State.SetInt(CountStateKey, CurrentKills);
        Entity.Runtime.State.SetFlag(CompleteFlag, completed);
    }

    public void OUTL_Capture(OUTL_ComponentSaveWriter writer)
    {
        if (writer == null) return;
        writer.SetInt("kills", CurrentKills);
        writer.SetInt("completed", completed ? 1 : 0);
        OUTL_OutputSaveUtility.Capture(writer, "outputs", Outputs);
    }

    public void OUTL_Restore(OUTL_ComponentSaveReader reader)
    {
        if (reader == null) return;
        CurrentKills = Mathf.Max(0, reader.GetInt("kills", 0));
        completed = reader.GetInt("completed", 0) != 0;
        OUTL_OutputSaveUtility.Restore(reader, "outputs", Outputs);
        WriteState();
    }
}
