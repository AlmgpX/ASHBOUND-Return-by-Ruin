using UnityEngine;

public enum OUTL_RelayGateMode : byte
{
    Any = 0,
    All = 1,
    None = 2,
    Nand = 3,
    Xor = 4,
    Exactly = 5,
    AtLeast = 6,
    AtMost = 7
}

[DisallowMultipleComponent]
public sealed class OUTL_LogicRelay : MonoBehaviour, OUTL_ICommandReceiver, OUTL_IComponentSaveParticipant
{
    public OUTL_EntityAdapter Entity;
    public OUTL_RelayGateMode Gate = OUTL_RelayGateMode.All;

    [Header("Input State Query")]
    public OUTL_MultiSourceInput[] Inputs;
    public string DefaultInputFlag = "On";
    public int Threshold = 1;

    [Header("Output")]
    public OUTL_OutputLink[] Outputs;
    public string TrueEventName = "OnTrue";
    public string FalseEventName = "OnFalse";
    public string OutputFlag = "On";

    [Header("Evaluation")]
    public bool EvaluateOnEnable = true;
    public bool SendOnlyOnChange = true;
    public bool CurrentState;
    public bool DebugLog;

    private bool hasEvaluated;
    private readonly System.Collections.Generic.List<OUTL_EntityRuntime> buffer = new System.Collections.Generic.List<OUTL_EntityRuntime>(8);

    public string OUTL_SaveKey { get { return "OUTL_LogicRelay"; } }

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
    }

    private void OnEnable()
    {
        if (EvaluateOnEnable) EvaluateAndSend(false);
    }

    public bool OUTL_CanReceive(in OUTL_Command command, OUTL_World world)
    {
        return command.Type == OUTL_CommandType.Use || command.Type == OUTL_CommandType.Activate || command.Type == OUTL_CommandType.Deactivate || command.Type == OUTL_CommandType.SendSignal || command.Type == OUTL_CommandType.Custom;
    }

    public void OUTL_Receive(in OUTL_Command command, OUTL_World world)
    {
        EvaluateAndSend(true);
    }

    [ContextMenu("OUT Evaluate Logic Relay")]
    public void EvaluateNow()
    {
        EvaluateAndSend(true);
    }

    public bool Evaluate()
    {
        int active = CountActiveInputs();
        int total = Inputs != null ? Inputs.Length : 0;
        switch (Gate)
        {
            case OUTL_RelayGateMode.Any: return active > 0;
            case OUTL_RelayGateMode.All: return total > 0 && active == total;
            case OUTL_RelayGateMode.None: return active == 0;
            case OUTL_RelayGateMode.Nand: return !(total > 0 && active == total);
            case OUTL_RelayGateMode.Xor: return (active & 1) == 1;
            case OUTL_RelayGateMode.Exactly: return active == Mathf.Max(0, Threshold);
            case OUTL_RelayGateMode.AtLeast: return active >= Mathf.Max(0, Threshold);
            case OUTL_RelayGateMode.AtMost: return active <= Mathf.Max(0, Threshold);
            default: return false;
        }
    }

    private void EvaluateAndSend(bool explicitPulse)
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return;

        bool value = Evaluate();
        bool changed = !hasEvaluated || value != CurrentState;
        CurrentState = value;
        hasEvaluated = true;

        if (Entity != null && Entity.Runtime != null)
            Entity.Runtime.State.SetFlag(OutputFlag, CurrentState);

        if (SendOnlyOnChange && !changed && !explicitPulse) return;

        string eventName = CurrentState ? TrueEventName : FalseEventName;
        OUTL_EntityId source = Entity != null ? Entity.Id : OUTL_EntityId.None;
        OUTL_OutputDispatcher.Fire(world, source, this, transform.position, Outputs, eventName, OutputFlag, CurrentState ? 1 : 0, true);

        if (DebugLog)
            OUTL_DebugLog.Log(OUTL_DebugChannel.Events, "[LOGIC] " + name + " gate=" + Gate + " state=" + CurrentState + " active=" + CountActiveInputs(), true);
    }

    private int CountActiveInputs()
    {
        if (Inputs == null || OUTL_World.Instance == null) return 0;
        int active = 0;
        for (int i = 0; i < Inputs.Length; i++)
        {
            OUTL_MultiSourceInput input = Inputs[i];
            if (string.IsNullOrEmpty(input.TargetName)) continue;
            string flag = string.IsNullOrEmpty(input.Flag) ? DefaultInputFlag : input.Flag;
            bool value = IsTargetNameFlagOn(input.TargetName, flag);
            if (input.Invert) value = !value;
            if (value) active++;
        }
        return active;
    }

    private bool IsTargetNameFlagOn(string targetName, string flag)
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return false;
        world.Registry.CopyByTargetName(targetName, buffer);
        for (int i = 0; i < buffer.Count; i++)
        {
            OUTL_EntityRuntime runtime = buffer[i];
            if (runtime != null && runtime.State.GetFlag(flag, false))
            {
                buffer.Clear();
                return true;
            }
        }
        buffer.Clear();
        return false;
    }

    [ContextMenu("OUT Reset Output Once Flags")]
    public void ResetOutputOnceFlags()
    {
        OUTL_OutputDispatcher.ResetOnceFlags(Outputs);
    }

    public void OUTL_Capture(OUTL_ComponentSaveWriter writer)
    {
        if (writer == null) return;
        writer.SetInt("state", CurrentState ? 1 : 0);
        writer.SetInt("evaluated", hasEvaluated ? 1 : 0);
        OUTL_OutputSaveUtility.Capture(writer, "outputs", Outputs);
    }

    public void OUTL_Restore(OUTL_ComponentSaveReader reader)
    {
        if (reader == null) return;
        CurrentState = reader.GetInt("state", 0) != 0;
        hasEvaluated = reader.GetInt("evaluated", 0) != 0;
        OUTL_OutputSaveUtility.Restore(reader, "outputs", Outputs);
        if (Entity != null && Entity.Runtime != null) Entity.Runtime.State.SetFlag(OutputFlag, CurrentState);
    }
}
