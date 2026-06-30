using System;
using UnityEngine;

public enum OUTL_ActorMedium : byte
{
    Ground = 0,
    Water = 1,
    Air = 2,
    Climb = 3,
    Burrow = 4
}

public enum OUTL_HurtboxShape : byte
{
    Box = 0,
    Sphere = 1,
    Capsule = 2
}

[Flags]
public enum OUTL_HurtboxTagFlags
{
    None = 0,
    Head = 1 << 0,
    Body = 1 << 1,
    Leg = 1 << 2,
    WeakPoint = 1 << 3,
    Armor = 1 << 4,
    Shell = 1 << 5,
    SensorOnly = 1 << 6
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Living/Actor Shape Profile", fileName = "OUTL_ActorShapeProfile")]
public sealed partial class OUTL_ActorShapeProfileDef : ScriptableObject
{
    public string ShapeId = "humanoid";
    public OUTL_ActorMedium Medium = OUTL_ActorMedium.Ground;

    [Header("Body")]
    [Min(0.01f)] public float BodyLength = 0.6f;
    [Min(0.01f)] public float BodyHeight = 1.8f;
    [Min(0.01f)] public float BodyWidth = 0.6f;
    public Vector3 CenterOffset = Vector3.up * 0.9f;
    public float GroundOffset;
    [Min(0f)] public float EyeHeight = 1.62f;

    [Header("Movement Hull")]
    [Min(0.01f)] public float MovementRadius = 0.32f;
    [Min(0.01f)] public float NavAgentHeight = 1.8f;
    [Min(0.01f)] public float NavAgentRadius = 0.32f;
    [Min(0.01f)] public float InteractionRadius = 1.25f;

    [Header("Optional Pose")]
    public bool HasCrouchPose;
    [Min(0.01f)] public float CrouchHeight = 1.0f;
    [Min(0f)] public float CrouchEyeHeight = 0.92f;

    public Bounds BuildLocalBodyBounds()
    {
        return new Bounds(CenterOffset, new Vector3(BodyWidth, BodyHeight, BodyLength));
    }
}

[Serializable]
public sealed class OUTL_HurtboxProfileEntry
{
    public string Id = "body";
    public OUTL_HurtboxShape Shape = OUTL_HurtboxShape.Capsule;
    public OUTL_HitboxZone Zone = OUTL_HitboxZone.Torso;
    public OUTL_HurtboxTagFlags Tags = OUTL_HurtboxTagFlags.Body;
    [Min(0f)] public float DamageMultiplier = 1f;
    public bool EnabledByDefault = true;
    public bool IsTrigger;
    public int Layer = -1;

    [Header("Local Transform")]
    public Vector3 LocalCenter = Vector3.up * 0.9f;
    public Vector3 LocalEuler;

    [Header("Shape")]
    public Vector3 BoxSize = new Vector3(0.5f, 1f, 0.5f);
    [Min(0.001f)] public float Radius = 0.25f;
    [Min(0.001f)] public float Height = 1f;
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Living/Hurtbox Profile", fileName = "OUTL_HurtboxProfile")]
public sealed partial class OUTL_HurtboxProfileDef
{
    public string ProfileId = "humanoid_hurtboxes";
    public OUTL_HurtboxProfileEntry[] Hurtboxes;
    public bool AllowProjectileHits = true;
    public bool AllowMeleeHits = true;
    public bool FriendlyFireUsesCombatRules = true;
}

[DisallowMultipleComponent]
public sealed partial class OUTL_ActorShapeRuntime
{
    public OUTL_EntityAdapter Entity;
    public OUTL_ActorShapeProfileDef ShapeProfile;
    public OUTL_HurtboxProfileDef HurtboxProfile;
    public bool AutoResolveOnAwake = true;
    public bool AutoApplyHurtboxesOnAwake;
    public Transform HurtboxRoot;

    private void Awake()
    {
        if (AutoResolveOnAwake) ResolveReferences();
        if (AutoApplyHurtboxesOnAwake) ApplyHurtboxProfile(false);
    }

    public void OUTL_OnPoolSpawn()
    {
        ResolveReferences();
    }

    public void OUTL_OnPoolRelease() { }

    [ContextMenu("OUT Apply Hurtbox Profile")]
    public void ApplyHurtboxProfileContext()
    {
        ApplyHurtboxProfile(true);
    }

    public void ResolveReferences()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (HurtboxRoot == null)
        {
            Transform existing = transform.Find("OUTL_Hurtboxes");
            if (existing != null) HurtboxRoot = existing;
        }
    }

    public void ApplyShapeToNavAgent(UnityEngine.AI.NavMeshAgent agent)
    {
        if (agent == null || ShapeProfile == null) return;
        agent.height = Mathf.Max(0.01f, ShapeProfile.NavAgentHeight);
        agent.radius = Mathf.Max(0.01f, ShapeProfile.NavAgentRadius);
        agent.baseOffset = Mathf.Max(0f, ShapeProfile.GroundOffset);
    }

    public void ApplyShapeToCharacterController(CharacterController controller)
    {
        if (controller == null || ShapeProfile == null) return;
        controller.height = Mathf.Max(0.01f, ShapeProfile.BodyHeight);
        controller.radius = Mathf.Max(0.01f, ShapeProfile.MovementRadius);
        controller.center = ShapeProfile.CenterOffset;
    }

    public void ApplyHurtboxProfile(bool clearExisting)
    {
        ResolveReferences();
        if (HurtboxProfile == null || HurtboxProfile.Hurtboxes == null) return;
        if (Application.isPlaying)
        {
            Debug.LogWarning("OUTL ActorShapeRuntime refused runtime hurtbox construction. Author hurtboxes in the prefab/editor.", this);
            return;
        }

#if UNITY_EDITOR
        ApplyHurtboxProfileEditorBoundary(clearExisting);
#else
        Debug.LogWarning("OUTL ActorShapeRuntime hurtbox authoring is editor-only.", this);
#endif
    }

#if UNITY_EDITOR
    partial void ApplyHurtboxProfileEditorBoundary(bool clearExisting);
#endif
}
