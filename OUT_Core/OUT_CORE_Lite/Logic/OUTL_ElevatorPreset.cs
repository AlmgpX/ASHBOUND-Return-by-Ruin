using UnityEngine;

[CreateAssetMenu(menuName = "OUT CORE Lite/Logic/Elevator Preset", fileName = "OUTL_ElevatorPreset")]
public sealed class OUTL_ElevatorPreset : ScriptableObject
{
    public string PresetId = "outl.elevator.basic";
    public float Speed = 2.4f;
    public float RotationSpeed = 360f;
    public float WaitAtNode = 1.0f;
    public bool PauseUntilCommand;
    public bool PingPong = true;
    public bool StartOnEnable;
    public bool TeleportToFirstNodeOnStart = true;

    [Header("Carrier")]
    public bool AddCarrier = true;
    public Vector3 CarrierLocalBoxCenter = new Vector3(0f, 0.75f, 0f);
    public Vector3 CarrierLocalBoxSize = new Vector3(3f, 0.55f, 3f);
    public bool CarryCharacterControllers = true;
    public bool CarryRigidbodies = true;
    public bool TransferRotationYaw;

    [Header("Crush")]
    public bool CheckBlockers = true;
    public float CrushDamage = 8f;
    public string CrushDamageKey = "crush.elevator";
    public float CrushDamageInterval = 0.5f;
    public bool StopOnBlock = true;
    public bool ReverseOnBlock;

    [Header("Audio")]
    public AudioClip MovingLoop;
    public AudioClip StartSound;
    public AudioClip StopSound;
    [Range(0f, 1f)] public float Volume = 0.85f;

    public void Apply(OUTL_PathMover mover)
    {
        if (mover == null) return;
        mover.Mode = PingPong ? OUTL_PathMoverMode.PingPong : OUTL_PathMoverMode.Once;
        mover.Speed = Mathf.Max(0.001f, Speed);
        mover.RotationSpeed = Mathf.Max(0.001f, RotationSpeed);
        mover.DefaultWait = Mathf.Max(0f, WaitAtNode);
        mover.PauseOnNodeUntilCommand = PauseUntilCommand;
        mover.StartOnEnable = StartOnEnable;
        mover.TeleportToFirstNodeOnStart = TeleportToFirstNodeOnStart;
        mover.CheckBlockers = CheckBlockers;
        mover.CrushDamage = Mathf.Max(0f, CrushDamage);
        mover.CrushDamageKey = string.IsNullOrEmpty(CrushDamageKey) ? "crush.elevator" : CrushDamageKey;
        mover.CrushDamageInterval = Mathf.Max(0.01f, CrushDamageInterval);
        mover.StopOnBlock = StopOnBlock;
        mover.ReverseOnBlock = ReverseOnBlock;
        mover.MovingLoop = MovingLoop;
        mover.StartSound = StartSound;
        mover.StopSound = StopSound;
        mover.Volume = Volume;
    }

    public void Apply(OUTL_MovingPlatformCarrier carrier)
    {
        if (carrier == null) return;
        carrier.LocalBoxCenter = CarrierLocalBoxCenter;
        carrier.LocalBoxSize = CarrierLocalBoxSize;
        carrier.CarryCharacterControllers = CarryCharacterControllers;
        carrier.CarryRigidbodies = CarryRigidbodies;
        carrier.TransferRotationYaw = TransferRotationYaw;
    }
}
