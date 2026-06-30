using UnityEngine;

[CreateAssetMenu(menuName = "OUT CORE Lite/Logic/Train Preset", fileName = "OUTL_TrainPreset")]
public sealed class OUTL_TrainPreset : ScriptableObject
{
    public string PresetId = "outl.train.basic";
    public OUTL_PathMoverMode Mode = OUTL_PathMoverMode.Loop;
    public float Speed = 4.5f;
    public float RotationSpeed = 180f;
    public float DefaultWait = 0f;
    public bool StartOnEnable = true;
    public bool TeleportToFirstNodeOnStart = true;
    public bool RotateToPathDirection = true;
    public bool UseNodeRotation;

    [Header("Carrier")]
    public bool AddCarrier = true;
    public Vector3 CarrierLocalBoxCenter = new Vector3(0f, 0.85f, 0f);
    public Vector3 CarrierLocalBoxSize = new Vector3(4f, 0.7f, 4f);
    public bool CarryCharacterControllers = true;
    public bool CarryRigidbodies = true;
    public bool TransferRotationYaw = true;

    [Header("Crush")]
    public bool CheckBlockers = true;
    public float CrushDamage = 10f;
    public string CrushDamageKey = "crush.train";
    public float CrushDamageInterval = 0.5f;
    public bool StopOnBlock;
    public bool ReverseOnBlock;

    [Header("Audio")]
    public AudioClip MovingLoop;
    public AudioClip StartSound;
    public AudioClip StopSound;
    [Range(0f, 1f)] public float Volume = 0.9f;

    public void Apply(OUTL_PathMover mover)
    {
        if (mover == null) return;
        mover.Mode = Mode;
        mover.Speed = Mathf.Max(0.001f, Speed);
        mover.RotationSpeed = Mathf.Max(0.001f, RotationSpeed);
        mover.DefaultWait = Mathf.Max(0f, DefaultWait);
        mover.StartOnEnable = StartOnEnable;
        mover.TeleportToFirstNodeOnStart = TeleportToFirstNodeOnStart;
        mover.RotateToPathDirection = RotateToPathDirection;
        mover.UseNodeRotation = UseNodeRotation;
        mover.CheckBlockers = CheckBlockers;
        mover.CrushDamage = Mathf.Max(0f, CrushDamage);
        mover.CrushDamageKey = string.IsNullOrEmpty(CrushDamageKey) ? "crush.train" : CrushDamageKey;
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
