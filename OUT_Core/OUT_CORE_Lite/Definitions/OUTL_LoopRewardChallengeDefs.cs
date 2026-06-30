using System;
using UnityEngine;

public enum OUTL_EntityRoleKind
{
    Any = 0,
    ControlledActor = 1,
    AutonomousActor = 2,
    Opposition = 3,
    Ally = 4,
    Neutral = 5,
    Objective = 6,
    Interactable = 7,
    CombatEmitter = 8,
    SpawnPoint = 9,
    CameraRig = 10,
    UIAnchor = 11
}

[Serializable]
public sealed class OUTL_EntityRoleBinding
{
    public string RoleId = "role.actor";
    public string DisplayName = "Actor Role";
    public OUTL_EntityRoleKind Kind = OUTL_EntityRoleKind.Any;
    public string[] Tags;
    public OUTL_EntityDef EntityDef;
    public OUTL_FactionDef Faction;
    public OUTL_AIProfile AIProfile;
    public OUTL_AttackProfile PrimaryAttack;
    public OUTL_AttackProfile SecondaryAttack;
    public OUTL_AttackProfile MeleeAttack;
    public OUTL_RuntimeTier DefaultTier = OUTL_RuntimeTier.Full;
    public bool Spawnable = true;
    public bool NetworkReplicated = true;
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Game Loop Def", fileName = "OUTL_GameLoopDef")]
public sealed partial class OUTL_GameLoopDef
{
    public string LoopId = "loop.core";
    public string DisplayName = "Core Loop";
    public OUTL_ChallengeDef[] StartupChallenges;
    public OUTL_RewardDef[] RewardTable;
    public OUTL_EffectDef[] OnSessionStart;
    public OUTL_EffectDef[] OnSessionFail;
    public OUTL_EffectDef[] OnSessionWin;
    public bool AutoStart = true;
    public bool ResetChallengesOnStart = true;
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Reward Def", fileName = "OUTL_RewardDef")]
public sealed partial class OUTL_RewardDef
{
    public string RewardId = "reward";
    public string DisplayName = "Reward";
    public string CurrencyId = "score";
    public int Points;
    public int XP;
    public OUTL_ItemDef[] Items;
    public OUTL_EffectDef[] Effects;
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Challenge Def", fileName = "OUTL_ChallengeDef")]
public sealed partial class OUTL_ChallengeDef
{
    public string ChallengeId = "challenge";
    public string DisplayName = "Challenge";
    public string[] Tags;
    public OUTL_EventType ListenEvent = OUTL_EventType.Killed;
    public string ListenKey;
    public string[] RequiredSourceTags;
    public string[] RequiredTargetTags;
    public int TargetCount = 1;
    public bool CompleteOnlyOnce = true;
    public bool ResetOnSessionStart = true;
    public OUTL_ConditionDef[] Conditions;
    public OUTL_RewardDef[] Rewards;
    public OUTL_EffectDef[] OnProgressEffects;
    public OUTL_EffectDef[] OnCompleteEffects;
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Genre Preset Def", fileName = "OUTL_GenrePresetDef")]
public sealed partial class OUTL_GenrePresetDef
{
    public string GenreId = "action_kernel";
    public string DisplayName = "Action Kernel";
    public OUTL_GameLoopDef GameLoop;
    public OUTL_EntityRoleBinding[] Roles;

    [Header("Legacy compatibility fields")]
    public OUTL_EntityDef PlayerDef;
    public OUTL_EntityDef[] EnemyDefs;
    public OUTL_UIBindingTemplateDef[] UITemplates;
    public OUTL_ProcessingProfileAsset ProcessingProfile;
    public bool EnableNetworkPreset;
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Kernel Preset Def", fileName = "OUTL_KernelPresetDef")]
public sealed partial class OUTL_KernelPresetDef
{
    public string KernelId = "kernel.action";
    public string DisplayName = "Action Kernel";
    public OUTL_GameLoopDef[] GameLoops;
    public OUTL_EntityRoleBinding[] Roles;
    public OUTL_UIBindingTemplateDef[] UITemplates;
    public OUTL_ProcessingProfileAsset ProcessingProfile;
    public bool OfflineReady = true;
    public bool HostAuthorityReady = true;
    public bool ClientPredictionRequired;
}

[CreateAssetMenu(menuName = "OUT CORE Lite/UI Binding Template", fileName = "OUTL_UIBindingTemplateDef")]
public sealed class OUTL_UIBindingTemplateDef : ScriptableObject
{
    public string TemplateId = "hud.core";
    public GameObject ViewPrefab;
    public OUTL_UIDataBinding[] ScalarBindings;
    public OUTL_UICollectionBinding[] Collections;
}

[Serializable]
public class OUTL_UICollectionBinding
{
    public string CollectionId = "rewards";
    public string Source = "rewards";
    public GameObject ItemPrefab;
    public Transform TargetRoot;
    public int MaxItems = 8;
}
