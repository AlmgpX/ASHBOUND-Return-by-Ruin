using UnityEngine;

[DisallowMultipleComponent]
public class OUTL_LogicGate : MonoBehaviour, OUTL_ICommandReceiver, OUTL_IComponentSaveParticipant
{
    public OUTL_EntityAdapter Entity;
    public OUTL_BooleanGateMode Mode = OUTL_BooleanGateMode.And;
    public int InputCount = 2;
    public bool[] Inputs;
    public bool Output;

    [Header("Output")]
    public OUTL_OutputLink[] Outputs;
    public string TrueEventName = "OnTrue";
    public string FalseEventName = "OnFalse";
    public string OutputFlag = "On";

    public string OUTL_SaveKey { get { return "OUTL_LogicGate"; } }

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (Inputs == null || Inputs.Length != Mathf.Max(1, InputCount)) Inputs = new bool[Mathf.Max(1, InputCount)];
    }

    public bool OUTL_CanReceive(in OUTL_Command command, OUTL_World world)
    {
        return command.Type == OUTL_CommandType.Activate || command.Type == OUTL_CommandType.Deactivate || command.Type == OUTL_CommandType.Use || command.Type == OUTL_CommandType.SendSignal || command.Type == OUTL_CommandType.Custom;
    }

    public void OUTL_Receive(in OUTL_Command command, OUTL_World world)
    {
        if (world == null) world = OUTL_World.Instance;
        if (world == null) return;

        if (Inputs == null || Inputs.Length != Mathf.Max(1, InputCount)) Inputs = new bool[Mathf.Max(1, InputCount)];
        int index = Mathf.Clamp(command.IntValue, 0, Inputs.Length - 1);
        Inputs[index] = command.Type != OUTL_CommandType.Deactivate;
        bool next = Evaluate();
        if (next == Output) return;

        Output = next;
        if (Entity != null && Entity.Runtime != null) Entity.Runtime.State.SetFlag(OutputFlag, Output);

        string eventName = Output ? TrueEventName : FalseEventName;
        OUTL_EntityId source = Entity != null ? Entity.Id : command.Source;
        OUTL_OutputDispatcher.Fire(world, source, this, transform.position, Outputs, eventName, OutputFlag, Output ? 1 : 0, true);
    }

    public void OUTL_Capture(OUTL_ComponentSaveWriter writer)
    {
        if (writer == null) return;
        writer.SetInt("output", Output ? 1 : 0);
        int count = Inputs != null ? Inputs.Length : 0;
        writer.SetInt("inputCount", count);
        for (int i = 0; i < count; i++) writer.SetInt("input." + i, Inputs[i] ? 1 : 0);
        OUTL_OutputSaveUtility.Capture(writer, "outputs", Outputs);
    }

    public void OUTL_Restore(OUTL_ComponentSaveReader reader)
    {
        if (reader == null) return;
        int count = Mathf.Max(1, reader.GetInt("inputCount", Mathf.Max(1, InputCount)));
        InputCount = count;
        if (Inputs == null || Inputs.Length != count) Inputs = new bool[count];
        for (int i = 0; i < Inputs.Length; i++) Inputs[i] = reader.GetInt("input." + i, 0) != 0;
        Output = reader.GetInt("output", Evaluate() ? 1 : 0) != 0;
        OUTL_OutputSaveUtility.Restore(reader, "outputs", Outputs);
        if (Entity != null && Entity.Runtime != null) Entity.Runtime.State.SetFlag(OutputFlag, Output);
    }

    private bool Evaluate()
    {
        if (Inputs == null || Inputs.Length == 0) return false;
        int on = 0;
        for (int i = 0; i < Inputs.Length; i++) if (Inputs[i]) on++;
        switch (Mode)
        {
            case OUTL_BooleanGateMode.Or: return on > 0;
            case OUTL_BooleanGateMode.Not: return on == 0;
            case OUTL_BooleanGateMode.Xor: return on == 1;
            case OUTL_BooleanGateMode.Nand: return on < Inputs.Length;
            case OUTL_BooleanGateMode.Nor: return on == 0;
            case OUTL_BooleanGateMode.And:
            default: return on == Inputs.Length;
        }
    }

    [ContextMenu("OUT Reset Output Once Flags")]
    public void ResetOutputOnceFlags()
    {
        OUTL_OutputDispatcher.ResetOnceFlags(Outputs);
    }
}
