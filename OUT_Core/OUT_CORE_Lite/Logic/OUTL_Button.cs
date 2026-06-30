using UnityEngine;

[DisallowMultipleComponent]
public class OUTL_Button : MonoBehaviour, OUTL_ICommandReceiver, OUTL_IComponentSaveParticipant
{
    public OUTL_EntityAdapter Entity;
    public bool Toggle;
    public bool State;
    [Tooltip("When true, fired outputs receive 1/0 as IntValue for pressed/released state. Disable for logic links that use OutputLink.IntValue as an input index.")]
    public bool OverrideOutputIntWithState = true;
    public OUTL_OutputLink[] Outputs;
    public string PressEventName = "OnPressed";
    public string ReleaseEventName = "OnReleased";
    public AudioSource AudioSource;
    public AudioClip PressSound;

    [Header("Visual")]
    public Transform VisualRoot;
    public Vector3 PressedLocalOffset = new Vector3(0f, -0.08f, 0f);
    public bool ApplyVisualState = true;

    public string OUTL_SaveKey { get { return "OUTL_Button"; } }

    private Vector3 releasedLocalPosition;

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (AudioSource == null) AudioSource = GetComponent<AudioSource>();
        if (VisualRoot == null) VisualRoot = transform;
        releasedLocalPosition = VisualRoot.localPosition;
        ApplyVisual();
    }

    public bool OUTL_CanReceive(in OUTL_Command command, OUTL_World world)
    {
        return command.Type == OUTL_CommandType.Use || command.Type == OUTL_CommandType.Activate || command.Type == OUTL_CommandType.Deactivate;
    }

    public void OUTL_Receive(in OUTL_Command command, OUTL_World world)
    {
        if (world == null) world = OUTL_World.Instance;
        if (world == null) return;

        if (Toggle)
            State = !State;
        else
            State = command.Type != OUTL_CommandType.Deactivate;

        if (Entity != null && Entity.Runtime != null) Entity.Runtime.State.SetFlag("On", State);
        ApplyVisual();
        if (AudioSource != null && PressSound != null) AudioSource.PlayOneShot(PressSound);

        string eventName = State ? PressEventName : ReleaseEventName;
        OUTL_EntityId source = Entity != null ? Entity.Id : command.Source;
        OUTL_OutputDispatcher.Fire(world, source, this, transform.position, Outputs, eventName, eventName, State ? 1 : 0, OverrideOutputIntWithState);
    }

    public void OUTL_Capture(OUTL_ComponentSaveWriter writer)
    {
        if (writer == null) return;
        writer.SetInt("state", State ? 1 : 0);
        OUTL_OutputSaveUtility.Capture(writer, "outputs", Outputs);
    }

    public void OUTL_Restore(OUTL_ComponentSaveReader reader)
    {
        if (reader == null) return;
        State = reader.GetInt("state", 0) != 0;
        OUTL_OutputSaveUtility.Restore(reader, "outputs", Outputs);
        if (Entity != null && Entity.Runtime != null) Entity.Runtime.State.SetFlag("On", State);
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (!ApplyVisualState) return;
        if (VisualRoot == null) VisualRoot = transform;
        VisualRoot.localPosition = releasedLocalPosition + (State ? PressedLocalOffset : Vector3.zero);
    }

    [ContextMenu("OUT Reset Output Once Flags")]
    public void ResetOutputOnceFlags()
    {
        OUTL_OutputDispatcher.ResetOnceFlags(Outputs);
    }
}
