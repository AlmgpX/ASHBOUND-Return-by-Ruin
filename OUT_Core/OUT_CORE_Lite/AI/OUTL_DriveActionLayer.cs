using System;
using System.Collections.Generic;
using UnityEngine;

public enum OUTL_DriveId : byte
{
    Fear = 0,
    Hunger = 1,
    Thirst = 2,
    Fatigue = 3,
    Pain = 4,
    Aggression = 5,
    Curiosity = 6,
    Territory = 7,
    SocialHerd = 8,
    Comfort = 9,
    Greed = 10,
    Duty = 11,
    AlcoholIntoxication = 12,
    Corruption = 13,
    Ritual = 14,
    ReproductionPressure = 15,
    PairBond = 16,
    Nesting = 17,
    BroodCare = 18,
    Rivalry = 19,
    SeasonalRutHeat = 20,
    OffspringProtection = 21,
    Count = 22
}

public enum OUTL_BehaviorActionId : byte
{
    Idle = 0,
    Wander = 1,
    FleeFromThreat = 2,
    FindFood = 3,
    Eat = 4,
    FindWater = 5,
    Rest = 6,
    FollowHerd = 7,
    AvoidArea = 8,
    AttackTarget = 9,
    Ambush = 10,
    Guard = 11,
    Patrol = 12,
    Trade = 13,
    Hide = 14,
    InvestigateStimulus = 15,
    WasteDrop = 16,
    SwimWander = 17,
    StayInMedium = 18,
    CallForHelp = 19,
    SeekMate = 20,
    Courtship = 21,
    PairBond = 22,
    MoveToNest = 23,
    ReproduceAbstract = 24,
    ProtectOffspring = 25,
    RivalChallenge = 26,
    LeaveGroup = 27,
    JoinHerd = 28,
    Count = 29
}

[Serializable]
public sealed class OUTL_DriveTuning
{
    public OUTL_DriveId Drive = OUTL_DriveId.Fear;
    [Range(0f, 1f)] public float InitialValue;
    public float GrowthPerSecond;
    public float DecayPerSecond;
    [Range(0f, 1f)] public float Minimum;
    [Range(0f, 1f)] public float Maximum = 1f;
    [Range(0f, 1f)] public float Threshold = 0.5f;
    public string[] Tags;
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Living/Drive Profile", fileName = "OUTL_DriveProfile")]
public sealed partial class OUTL_DriveProfileDef
{
    public string ProfileId = "living_drives";
    public OUTL_DriveTuning[] Drives;
    public bool InitializeMissingDrives;
    public int LocalSeedSalt = 7919;

    public OUTL_DriveTuning Find(OUTL_DriveId id)
    {
        if (Drives == null) return null;
        for (int i = 0; i < Drives.Length; i++)
            if (Drives[i] != null && Drives[i].Drive == id)
                return Drives[i];
        return null;
    }
}

[Serializable]
public sealed class OUTL_ActionDriveWeight
{
    public OUTL_DriveId Drive;
    public float Weight = 1f;
}

[Serializable]
public sealed class OUTL_ActionStimulusWeight
{
    public OUTL_StimulusType Stimulus;
    public float Weight = 1f;
}

[Serializable]
public sealed class OUTL_ActionEgregoreWeight
{
    public OUTL_EgregoreCyclePhase Phase;
    public float Weight = 1f;
}

public enum OUTL_LivingActionEffectType : byte
{
    None = 0,
    ModifyDrive = 1,
    EmitStimulus = 2,
    ConsumeNearbyResource = 3,
    SpawnDrop = 4,
    SendEvent = 5,
    RequestAbstractOffspring = 6,
    SetBehaviorMode = 7,
    SetTargetFromStimulus = 8,
    FleeFromLastThreat = 9,
    MoveToLastResource = 10
}

[Serializable]
public sealed class OUTL_LivingActionEffect
{
    public OUTL_LivingActionEffectType Type;
    public OUTL_DriveId Drive = OUTL_DriveId.Count;
    public float FloatValue;
    public int IntValue = 1;
    [Min(0f)] public float Radius = 1.8f;
    public string RequiredTag;
    public OUTL_StimulusType StimulusType = OUTL_StimulusType.None;
    public string Key;
    public string[] Tags;
    [Range(0f, 1f)] public float Strength = 1f;
    [Range(0f, 1f)] public float Confidence = 1f;
    [Range(0f, 1f)] public float Priority = 0.25f;
    [Min(0f)] public float DecayTime = 2f;
    public GameObject Prefab;
    public OUTL_ItemDef Item;
    public OUTL_EventType EventType = OUTL_EventType.Custom;
    public OUTL_BehaviorModeId BehaviorMode = OUTL_BehaviorModeId.Normal;
    [Min(0f)] public float MoveDistance = 8f;
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Living/Behavior Action", fileName = "OUTL_BehaviorAction")]
public sealed partial class OUTL_BehaviorActionDef
{
    public string ActionId = "idle";
    public OUTL_BehaviorActionId Type = OUTL_BehaviorActionId.Idle;
    public OUTL_NPCScheduleActionType OutputAction = OUTL_NPCScheduleActionType.Idle;
    public string[] Tags;

    [Header("Scoring")]
    public float BaseWeight = 0.1f;
    public OUTL_ActionDriveWeight[] DriveWeights;
    public OUTL_ActionStimulusWeight[] StimulusWeights;
    public OUTL_ActionEgregoreWeight[] EgregoreWeights;
    public float LocalDangerWeight;
    public float LocalSafetyWeight;
    public float RandomJitter = 0.025f;

    [Header("Timing")]
    [Min(0f)] public float Cooldown;
    [Min(0f)] public float MinDuration = 0.25f;
    [Min(0f)] public float MaxDuration;

    [Header("Conditions")]
    public OUTL_DriveId RequiredDrive = OUTL_DriveId.Count;
    [Range(0f, 1f)] public float RequiredDriveMinimum;
    public bool RequiresAdult;
    public bool RequiresSafeArea;
    public bool CannotRunInCombat;
    public bool SupportsAbstractMode = true;

    [Header("Outputs")]
    public OUTL_EffectDef[] OnStartEffects;
    public OUTL_EffectDef[] OnCompleteEffects;
    public OUTL_LivingActionEffect[] OnStartLivingEffects;
    public OUTL_StimulusType OutputStimulus = OUTL_StimulusType.None;
    public string OutputKey;
    [Min(0f)] public float OutputStimulusRadius = 6f;
    [Range(0f, 1f)] public float OutputStimulusPriority = 0.25f;
    [Range(0f, 1f)] public float OutputStimulusStrength = 1f;
    [Range(0f, 1f)] public float OutputStimulusConfidence = 1f;
    [Min(0f)] public float OutputStimulusDecayTime = 2f;
}

[CreateAssetMenu(menuName = "OUT CORE Lite/Living/Behavior Action Set", fileName = "OUTL_BehaviorActionSet")]
public sealed partial class OUTL_BehaviorActionSetDef
{
    public string ActionSetId = "living_actions";
    public OUTL_BehaviorActionDef[] Actions;
    public OUTL_BehaviorActionDef FallbackAction;
    public float SwitchHysteresis = 0.1f;
    public string[] SpeciesTags;
}

[DisallowMultipleComponent]
public sealed partial class OUTL_DriveRuntime
{
    public OUTL_EntityAdapter Entity;
    public OUTL_NPCBehaviorController Behavior;
    public OUTL_LivingActionTargetMemory TargetMemory;
    public OUTL_DriveProfileDef DriveProfile;
    public OUTL_BehaviorActionSetDef ActionSet;
    public bool AutoRegister = true;
    public bool ApplyActionToNPCBehavior = true;
    public bool EmitActionStimuli = true;
    public bool UseWorldLedgerEgregore = true;
    [Min(0.01f)] public float TickInterval = 0.5f;
    [Min(0f)] public float StimulusMemoryRadius = 24f;
    [Min(1)] public int MaxStimuliObservedPerTick = 12;
    [Min(0.1f)] public float EatResourceRadius = 1.8f;
    [Min(0.1f)] public float WanderRadius = 6f;
    [Min(0.1f)] public float FleeDistance = 8f;

    [Header("Runtime Debug")]
    public OUTL_BehaviorActionId CurrentAction = OUTL_BehaviorActionId.Idle;
    public string CurrentActionId = "idle";
    public float CurrentActionScore;
    public float CurrentActionStartTime;
    public string LastDecisionReason;
    public float LastEgregoreScoreContribution;
    public float LastDangerScoreContribution;
    public float LastSafetyScoreContribution;
    public float LastStimulusScoreContribution;
    public OUTL_EntityId LastThreat = OUTL_EntityId.None;
    public OUTL_EntityId PairBondTarget = OUTL_EntityId.None;
    public int NestCell;
    public int OffspringPending;
    public float LastReproductionTime = -999f;
    public float LastWasteDropTime = -999f;
    public int LocalSeed;

    private readonly float[] values = new float[(int)OUTL_DriveId.Count];
    private readonly float[] cooldownUntil = new float[(int)OUTL_BehaviorActionId.Count];
    private readonly List<OUTL_Stimulus> stimulusBuffer = new List<OUTL_Stimulus>(16);
    private bool registered;

    public string OUTL_SaveKey { get { return "OUTL_DriveRuntime"; } }
    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled && Entity != null && Entity.Runtime != null && !Entity.Runtime.Dead; } }
    public OUTL_TickLane OUTL_TickLane { get { return OUTL_TickLane.AI; } }
    public float OUTL_TickInterval { get { return Mathf.Max(0.01f, TickInterval); } }

    private void Awake()
    {
        ResolveReferences();
        InitializeFromProfile(false);
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (AutoRegister) Register();
    }

    private void OnDisable()
    {
        Unregister();
    }

    public void OUTL_OnPoolSpawn()
    {
        ResolveReferences();
        ResetRuntimeState();
        InitializeFromProfile(true);
        if (AutoRegister) Register();
    }

    public void OUTL_OnPoolRelease()
    {
        Unregister();
    }

    public void Register()
    {
        if (registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Register(this);
        registered = true;
    }

    public void Unregister()
    {
        if (!registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Unregister(this);
        registered = false;
    }

    public float GetDrive(OUTL_DriveId id)
    {
        int index = (int)id;
        if (index < 0 || index >= values.Length) return 0f;
        return values[index];
    }

    public void SetDrive(OUTL_DriveId id, float value)
    {
        int index = (int)id;
        if (index < 0 || index >= values.Length) return;
        values[index] = Mathf.Clamp01(value);
    }

    public void AddDrive(OUTL_DriveId id, float delta)
    {
        SetDrive(id, GetDrive(id) + delta);
    }

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        ResolveReferences();
        UpdateDrives(deltaTime);
        RefreshLivingMemory(time);
        OUTL_BehaviorActionDef best = SelectBestAction(world, time);
        if (best != null) ApplyAction(world, best, time);
        PushStateToRuntime();
    }

    public void OUTL_Capture(OUTL_ComponentSaveWriter writer)
    {
        if (writer == null) return;
        for (int i = 0; i < values.Length; i++) writer.SetFloat("drive." + ((OUTL_DriveId)i), values[i]);
        writer.SetInt("action", (int)CurrentAction);
        writer.SetString("actionId", CurrentActionId);
        writer.SetFloat("actionScore", CurrentActionScore);
        writer.SetFloat("actionStart", CurrentActionStartTime);
        writer.SetString("decisionReason", LastDecisionReason);
        writer.SetFloat("score.egregore", LastEgregoreScoreContribution);
        writer.SetFloat("score.danger", LastDangerScoreContribution);
        writer.SetFloat("score.safety", LastSafetyScoreContribution);
        writer.SetFloat("score.stimulus", LastStimulusScoreContribution);
        writer.SetInt("lastThreat", LastThreat.Value);
        writer.SetInt("pairBond", PairBondTarget.Value);
        writer.SetInt("nestCell", NestCell);
        writer.SetInt("offspringPending", OffspringPending);
        writer.SetFloat("lastReproduction", LastReproductionTime);
        writer.SetFloat("lastWasteDrop", LastWasteDropTime);
        writer.SetInt("seed", LocalSeed);
        for (int i = 0; i < cooldownUntil.Length; i++) writer.SetFloat("cooldown." + ((OUTL_BehaviorActionId)i), cooldownUntil[i]);
    }

    public void OUTL_Restore(OUTL_ComponentSaveReader reader)
    {
        if (reader == null) return;
        for (int i = 0; i < values.Length; i++) values[i] = Mathf.Clamp01(reader.GetFloat("drive." + ((OUTL_DriveId)i), values[i]));
        CurrentAction = (OUTL_BehaviorActionId)Mathf.Clamp(reader.GetInt("action", (int)CurrentAction), 0, (int)OUTL_BehaviorActionId.Count - 1);
        CurrentActionId = reader.GetString("actionId", CurrentActionId);
        CurrentActionScore = reader.GetFloat("actionScore", CurrentActionScore);
        CurrentActionStartTime = reader.GetFloat("actionStart", CurrentActionStartTime);
        LastDecisionReason = reader.GetString("decisionReason", LastDecisionReason);
        LastEgregoreScoreContribution = reader.GetFloat("score.egregore", LastEgregoreScoreContribution);
        LastDangerScoreContribution = reader.GetFloat("score.danger", LastDangerScoreContribution);
        LastSafetyScoreContribution = reader.GetFloat("score.safety", LastSafetyScoreContribution);
        LastStimulusScoreContribution = reader.GetFloat("score.stimulus", LastStimulusScoreContribution);
        LastThreat = new OUTL_EntityId(reader.GetInt("lastThreat", LastThreat.Value));
        PairBondTarget = new OUTL_EntityId(reader.GetInt("pairBond", PairBondTarget.Value));
        NestCell = reader.GetInt("nestCell", NestCell);
        OffspringPending = reader.GetInt("offspringPending", OffspringPending);
        LastReproductionTime = reader.GetFloat("lastReproduction", LastReproductionTime);
        LastWasteDropTime = reader.GetFloat("lastWasteDrop", LastWasteDropTime);
        LocalSeed = reader.GetInt("seed", LocalSeed != 0 ? LocalSeed : BuildSeed());
        for (int i = 0; i < cooldownUntil.Length; i++) cooldownUntil[i] = reader.GetFloat("cooldown." + ((OUTL_BehaviorActionId)i), cooldownUntil[i]);
        PushStateToRuntime();
    }

    private void ResolveReferences()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (Behavior == null) Behavior = GetComponent<OUTL_NPCBehaviorController>();
        if (TargetMemory == null) TargetMemory = GetComponent<OUTL_LivingActionTargetMemory>();
        if (LocalSeed == 0) LocalSeed = BuildSeed();
    }

    private void ResetRuntimeState()
    {
        Array.Clear(values, 0, values.Length);
        Array.Clear(cooldownUntil, 0, cooldownUntil.Length);
        CurrentAction = OUTL_BehaviorActionId.Idle;
        CurrentActionId = "idle";
        CurrentActionScore = 0f;
        CurrentActionStartTime = 0f;
        LastDecisionReason = "";
        LastEgregoreScoreContribution = 0f;
        LastDangerScoreContribution = 0f;
        LastSafetyScoreContribution = 0f;
        LastStimulusScoreContribution = 0f;
        LastThreat = OUTL_EntityId.None;
        PairBondTarget = OUTL_EntityId.None;
        NestCell = 0;
        OffspringPending = 0;
        LastReproductionTime = -999f;
        LastWasteDropTime = -999f;
        LocalSeed = BuildSeed();
    }

    private int BuildSeed()
    {
        string stable = Entity != null && !string.IsNullOrEmpty(Entity.StableId) ? Entity.StableId : name;
        unchecked
        {
            int hash = OUTL_WorldCellUtility.StableStringHash(stable);
            hash = hash * 31 + (DriveProfile != null ? DriveProfile.LocalSeedSalt : 7919);
            return hash != 0 ? hash : 1;
        }
    }

    private void InitializeFromProfile(bool force)
    {
        if (DriveProfile == null || DriveProfile.Drives == null) return;
        for (int i = 0; i < DriveProfile.Drives.Length; i++)
        {
            OUTL_DriveTuning tuning = DriveProfile.Drives[i];
            if (tuning == null) continue;
            int index = (int)tuning.Drive;
            if (index < 0 || index >= values.Length) continue;
            if (force || Mathf.Approximately(values[index], 0f))
                values[index] = Mathf.Clamp(tuning.InitialValue, tuning.Minimum, tuning.Maximum);
        }
    }

    private void UpdateDrives(float deltaTime)
    {
        if (DriveProfile == null || DriveProfile.Drives == null) return;
        float dt = Mathf.Max(0f, deltaTime);
        for (int i = 0; i < DriveProfile.Drives.Length; i++)
        {
            OUTL_DriveTuning tuning = DriveProfile.Drives[i];
            if (tuning == null) continue;
            int index = (int)tuning.Drive;
            if (index < 0 || index >= values.Length) continue;
            float value = values[index] + tuning.GrowthPerSecond * dt - tuning.DecayPerSecond * dt;
            values[index] = Mathf.Clamp(value, Mathf.Min(tuning.Minimum, tuning.Maximum), Mathf.Max(tuning.Minimum, tuning.Maximum));
        }
    }

    private void RefreshLivingMemory(float time)
    {
        if (TargetMemory == null || StimulusMemoryRadius <= 0f) return;
        OUTL_StimulusQuery query = new OUTL_StimulusQuery
        {
            Position = transform.position,
            Radius = StimulusMemoryRadius,
            Type = OUTL_StimulusType.None,
            MinPriority = 0f,
            MaxCount = Mathf.Max(1, MaxStimuliObservedPerTick),
            IgnoreSource = Entity != null ? Entity.Id : OUTL_EntityId.None
        };
        OUTL_StimulusBus.Query(query, stimulusBuffer);
        for (int i = 0; i < stimulusBuffer.Count; i++)
        {
            OUTL_Stimulus stimulus = stimulusBuffer[i];
            TargetMemory.Observe(stimulus, time);
            if (IsThreatStimulus(stimulus.Type))
            {
                LastThreat = stimulus.Source;
                AddDrive(OUTL_DriveId.Fear, Mathf.Clamp01(stimulus.Priority) * 0.04f);
                AddDrive(OUTL_DriveId.Pain, stimulus.Type == OUTL_StimulusType.TookDamage ? 0.03f : 0f);
            }
        }
    }

    private OUTL_BehaviorActionDef SelectBestAction(OUTL_World world, float time)
    {
        if (ActionSet == null || ActionSet.Actions == null || ActionSet.Actions.Length == 0) return ActionSet != null ? ActionSet.FallbackAction : null;
        OUTL_BehaviorActionDef currentDef = FindCurrentActionDef();
        if (currentDef != null && currentDef.MinDuration > 0f && time < CurrentActionStartTime + currentDef.MinDuration) return currentDef;

        OUTL_BehaviorActionDef best = ActionSet.FallbackAction;
        float bestScore = best != null ? ScoreAction(best, world, time, false) : float.NegativeInfinity;
        for (int i = 0; i < ActionSet.Actions.Length; i++)
        {
            OUTL_BehaviorActionDef action = ActionSet.Actions[i];
            if (action == null) continue;
            float score = ScoreAction(action, world, time, false);
            if (score > bestScore)
            {
                bestScore = score;
                best = action;
            }
        }

        if (currentDef != null && best != currentDef && bestScore < CurrentActionScore + Mathf.Max(0f, ActionSet.SwitchHysteresis))
        {
            CurrentActionScore = ScoreAction(currentDef, world, time, true);
            LastDecisionReason = BuildDecisionReason(currentDef);
            return currentDef;
        }

        CurrentActionScore = ScoreAction(best, world, time, true);
        LastDecisionReason = BuildDecisionReason(best);
        return best;
    }

    private OUTL_BehaviorActionDef FindCurrentActionDef()
    {
        if (ActionSet == null || ActionSet.Actions == null) return null;
        for (int i = 0; i < ActionSet.Actions.Length; i++)
            if (ActionSet.Actions[i] != null && ActionSet.Actions[i].Type == CurrentAction)
                return ActionSet.Actions[i];
        return null;
    }

    private float ScoreAction(OUTL_BehaviorActionDef action, OUTL_World world, float time, bool capture)
    {
        if (action == null) return float.NegativeInfinity;
        if (time < cooldownUntil[(int)action.Type]) return float.NegativeInfinity;
        if (action.CannotRunInCombat && Behavior != null && Behavior.Runtime != null && Behavior.Runtime.CurrentAction == OUTL_NPCScheduleActionType.Combat) return float.NegativeInfinity;
        if (action.RequiredDrive != OUTL_DriveId.Count && GetDrive(action.RequiredDrive) < action.RequiredDriveMinimum) return float.NegativeInfinity;
        if (action.RequiresSafeArea && Behavior != null && Behavior.Runtime != null && Behavior.Runtime.LocalSafety < 0.35f) return float.NegativeInfinity;
        if (!action.SupportsAbstractMode && Entity != null && Entity.Runtime != null && (Entity.Runtime.Tier == OUTL_RuntimeTier.Far || Entity.Runtime.Tier == OUTL_RuntimeTier.Dormant)) return float.NegativeInfinity;
        if (action.Type == OUTL_BehaviorActionId.Eat && !HasEdibleResourceNear()) return float.NegativeInfinity;

        float score = action.BaseWeight;
        if (action.DriveWeights != null)
            for (int i = 0; i < action.DriveWeights.Length; i++)
                if (action.DriveWeights[i] != null) score += GetDrive(action.DriveWeights[i].Drive) * action.DriveWeights[i].Weight;

        float dangerContribution = 0f;
        float safetyContribution = 0f;
        float egregoreContribution = 0f;
        float stimulusContribution = 0f;
        if (Behavior != null && Behavior.Runtime != null)
        {
            dangerContribution = Behavior.Runtime.LocalDanger * action.LocalDangerWeight;
            safetyContribution = Behavior.Runtime.LocalSafety * action.LocalSafetyWeight;
            score += dangerContribution;
            score += safetyContribution;
            if (action.EgregoreWeights != null)
                for (int i = 0; i < action.EgregoreWeights.Length; i++)
                    if (action.EgregoreWeights[i] != null && action.EgregoreWeights[i].Phase == Behavior.Runtime.LocalEgregorePhase)
                        egregoreContribution += action.EgregoreWeights[i].Weight;
            egregoreContribution += ComputeGenericEgregoreBias(action, Behavior.Runtime.LocalEgregorePhase, Behavior.Runtime.LocalDanger, Behavior.Runtime.LocalSafety);
            score += egregoreContribution;
        }

        if (action.StimulusWeights != null && Behavior != null && Behavior.Runtime != null)
            for (int i = 0; i < action.StimulusWeights.Length; i++)
                if (action.StimulusWeights[i] != null && action.StimulusWeights[i].Stimulus == Behavior.Runtime.LastStimulus)
                    stimulusContribution += action.StimulusWeights[i].Weight;

        if (TargetMemory != null)
        {
            if (IsThreatAction(action.Type) && TargetMemory.HasFreshThreat(time)) stimulusContribution += 0.65f;
            if (IsFeedingAction(action.Type) && TargetMemory.HasFreshFood(time)) stimulusContribution += 0.45f;
        }
        score += stimulusContribution;

        if (action.RandomJitter > 0f)
            score += DeterministicJitter(action, time) * action.RandomJitter;
        if (capture)
        {
            LastDangerScoreContribution = dangerContribution;
            LastSafetyScoreContribution = safetyContribution;
            LastEgregoreScoreContribution = egregoreContribution;
            LastStimulusScoreContribution = stimulusContribution;
        }
        return score;
    }

    private float DeterministicJitter(OUTL_BehaviorActionDef action, float time)
    {
        unchecked
        {
            int h = LocalSeed;
            h = h * 31 + (int)action.Type;
            h = h * 31 + Mathf.FloorToInt(time / Mathf.Max(0.1f, TickInterval));
            uint x = (uint)h;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            return (x & 0xffff) / 65535f;
        }
    }

    private void ApplyAction(OUTL_World world, OUTL_BehaviorActionDef action, float time)
    {
        if (action == null) return;
        bool changed = action.Type != CurrentAction;
        if (changed)
        {
            OUTL_BehaviorActionDef previous = FindCurrentActionDef();
            if (previous != null && previous.Cooldown > 0f) cooldownUntil[(int)previous.Type] = time + previous.Cooldown;
            CurrentAction = action.Type;
            CurrentActionId = string.IsNullOrEmpty(action.ActionId) ? action.Type.ToString() : action.ActionId;
            CurrentActionStartTime = time;
            if (world != null && Entity != null) world.Effects.ApplyAll(action.OnStartEffects, Entity.Id, Entity.Id, transform.position);
            ApplyActionSideEffects(action, time);
            ApplyLivingActionEffects(world, action.OnStartLivingEffects, time);
        }

        if (ApplyActionToNPCBehavior && Behavior != null && Behavior.Runtime != null)
        {
            Behavior.Runtime.CurrentAction = action.OutputAction;
            Behavior.Runtime.CurrentEntryId = "drive:" + CurrentActionId;
            Behavior.Runtime.CurrentBehaviorMode = MapBehaviorMode(action.Type);
            Behavior.Runtime.BehaviorModeSource = "drive:" + CurrentActionId;
            ApplyContinuousActionIntent(action, time);
        }

        if (EmitActionStimuli && changed && action.OutputStimulus != OUTL_StimulusType.None)
        {
            OUTL_StimulusBus.Emit(new OUTL_Stimulus(
                action.OutputStimulus,
                Entity != null ? Entity.Id : OUTL_EntityId.None,
                transform.position,
                Mathf.Max(0.1f, action.OutputStimulusRadius),
                Mathf.Clamp01(action.OutputStimulusStrength),
                Mathf.Clamp01(action.OutputStimulusConfidence),
                Mathf.Clamp01(action.OutputStimulusPriority),
                Mathf.Max(0f, action.OutputStimulusDecayTime),
                action.OutputKey,
                action.Tags));
        }
    }

    private void ApplyActionSideEffects(OUTL_BehaviorActionDef action, float time)
    {
        if (action == null) return;
        switch (action.Type)
        {
            case OUTL_BehaviorActionId.Eat:
                SetDrive(OUTL_DriveId.Hunger, Mathf.Max(0f, GetDrive(OUTL_DriveId.Hunger) - 0.35f));
                SetDrive(OUTL_DriveId.Comfort, Mathf.Clamp01(GetDrive(OUTL_DriveId.Comfort) + 0.05f));
                break;
            case OUTL_BehaviorActionId.FleeFromThreat:
                SetDrive(OUTL_DriveId.Fear, Mathf.Max(0f, GetDrive(OUTL_DriveId.Fear) - 0.05f));
                break;
            case OUTL_BehaviorActionId.Rest:
                SetDrive(OUTL_DriveId.Fatigue, Mathf.Max(0f, GetDrive(OUTL_DriveId.Fatigue) - 0.25f));
                break;
            case OUTL_BehaviorActionId.WasteDrop:
                LastWasteDropTime = time;
                SetDrive(OUTL_DriveId.Comfort, Mathf.Clamp01(GetDrive(OUTL_DriveId.Comfort) + 0.10f));
                break;
            case OUTL_BehaviorActionId.SeekMate:
                SetDrive(OUTL_DriveId.PairBond, Mathf.Clamp01(GetDrive(OUTL_DriveId.PairBond) + 0.10f));
                break;
            case OUTL_BehaviorActionId.ReproduceAbstract:
                LastReproductionTime = time;
                OffspringPending = Mathf.Max(0, OffspringPending + 1);
                SetDrive(OUTL_DriveId.ReproductionPressure, 0f);
                SetDrive(OUTL_DriveId.PairBond, Mathf.Clamp01(GetDrive(OUTL_DriveId.PairBond) + 0.15f));
                break;
        }
    }

    private void ApplyLivingActionEffects(OUTL_World world, OUTL_LivingActionEffect[] effects, float time)
    {
        if (effects == null) return;
        for (int i = 0; i < effects.Length; i++)
        {
            OUTL_LivingActionEffect effect = effects[i];
            if (effect == null || effect.Type == OUTL_LivingActionEffectType.None) continue;
            switch (effect.Type)
            {
                case OUTL_LivingActionEffectType.ModifyDrive:
                    if (effect.Drive != OUTL_DriveId.Count) AddDrive(effect.Drive, effect.FloatValue);
                    break;
                case OUTL_LivingActionEffectType.EmitStimulus:
                    EmitLivingStimulus(effect);
                    break;
                case OUTL_LivingActionEffectType.ConsumeNearbyResource:
                    ConsumeNearbyResource(effect, time);
                    break;
                case OUTL_LivingActionEffectType.SpawnDrop:
                    SpawnLivingDrop(world, effect);
                    break;
                case OUTL_LivingActionEffectType.SendEvent:
                    SendLivingEvent(world, effect);
                    break;
                case OUTL_LivingActionEffectType.RequestAbstractOffspring:
                    OffspringPending = Mathf.Max(0, OffspringPending + Mathf.Max(1, effect.IntValue));
                    LastReproductionTime = time;
                    SendLivingEvent(world, effect);
                    break;
                case OUTL_LivingActionEffectType.SetBehaviorMode:
                    if (Behavior != null && Behavior.Runtime != null)
                    {
                        Behavior.Runtime.CurrentBehaviorMode = effect.BehaviorMode;
                        Behavior.Runtime.BehaviorModeSource = "living-effect:" + effect.Key;
                    }
                    break;
                case OUTL_LivingActionEffectType.SetTargetFromStimulus:
                    ApplyTargetFromMemory(effect);
                    break;
                case OUTL_LivingActionEffectType.FleeFromLastThreat:
                    ApplyFleeTarget(effect.MoveDistance > 0f ? effect.MoveDistance : FleeDistance);
                    break;
                case OUTL_LivingActionEffectType.MoveToLastResource:
                    ApplyFoodTarget();
                    break;
            }
        }
    }

    private void ApplyContinuousActionIntent(OUTL_BehaviorActionDef action, float time)
    {
        if (Behavior == null || Behavior.Runtime == null || action == null) return;
        switch (action.Type)
        {
            case OUTL_BehaviorActionId.FleeFromThreat:
                Behavior.Runtime.CurrentAction = OUTL_NPCScheduleActionType.Flee;
                ApplyFleeTarget(FleeDistance);
                break;
            case OUTL_BehaviorActionId.FindFood:
                if (ApplyFoodTarget()) Behavior.Runtime.CurrentAction = OUTL_NPCScheduleActionType.TravelTo;
                break;
            case OUTL_BehaviorActionId.Eat:
                Behavior.Runtime.CurrentAction = OUTL_NPCScheduleActionType.Eat;
                ApplyFoodTarget();
                break;
            case OUTL_BehaviorActionId.Wander:
                Behavior.Runtime.CurrentAction = OUTL_NPCScheduleActionType.Wander;
                EnsureWanderTarget(time);
                break;
            case OUTL_BehaviorActionId.SeekMate:
                Behavior.Runtime.CurrentAction = OUTL_NPCScheduleActionType.Wander;
                EnsureWanderTarget(time);
                break;
        }
    }

    private void EmitLivingStimulus(OUTL_LivingActionEffect effect)
    {
        if (effect == null || effect.StimulusType == OUTL_StimulusType.None) return;
        OUTL_StimulusBus.Emit(new OUTL_Stimulus(
            effect.StimulusType,
            Entity != null ? Entity.Id : OUTL_EntityId.None,
            transform.position,
            Mathf.Max(0.1f, effect.Radius),
            Mathf.Clamp01(effect.Strength),
            Mathf.Clamp01(effect.Confidence),
            Mathf.Clamp01(effect.Priority),
            Mathf.Max(0f, effect.DecayTime),
            effect.Key,
            effect.Tags));
    }

    private void ConsumeNearbyResource(OUTL_LivingActionEffect effect, float time)
    {
        OUTL_LivingResourceSource source;
        float consumed;
        float radius = effect.Radius > 0f ? effect.Radius : EatResourceRadius;
        string tag = string.IsNullOrEmpty(effect.RequiredTag) ? "Food" : effect.RequiredTag;
        float amount = effect.IntValue > 0 ? effect.IntValue : 1f;
        if (!OUTL_LivingResourceSource.TryConsumeNearest(transform.position, radius, tag, Entity, amount, out source, out consumed)) return;
        if (source != null && TargetMemory != null)
        {
            OUTL_Stimulus stimulus = new OUTL_Stimulus(OUTL_StimulusType.SightFood, source.Entity != null ? source.Entity.Id : OUTL_EntityId.None, source.transform.position, radius, consumed, 1f, effect.Priority, effect.DecayTime, "resource.consumed", source.ResourceTags);
            TargetMemory.Observe(stimulus, time);
        }
    }

    private void SpawnLivingDrop(OUTL_World world, OUTL_LivingActionEffect effect)
    {
        if (effect == null || effect.Prefab == null) return;
        Vector3 position = transform.position + Vector3.up * 0.25f + transform.right * 0.25f;
        GameObject go = OUTL_PoolSystem.SpawnShared(effect.Prefab, position, transform.rotation);
        if (go == null) return;

        OUTL_ItemPickup pickup = go.GetComponent<OUTL_ItemPickup>();
        if (pickup != null)
        {
            pickup.Item = effect.Item != null ? effect.Item : pickup.Item;
            pickup.Count = Mathf.Max(1, effect.IntValue);
            pickup.Source = Entity;
            pickup.PickupKey = string.IsNullOrEmpty(effect.Key) ? "living.drop" : effect.Key;
        }

        OUTL_EntityAdapter adapter = go.GetComponent<OUTL_EntityAdapter>();
        if (world != null && adapter != null && adapter.Runtime == null) adapter.RegisterNow(world);
        OUTL_StimulusBus.EmitResource(Entity != null ? Entity.Id : OUTL_EntityId.None, position, 8f, 0.5f, 0.35f, string.IsNullOrEmpty(effect.Key) ? "living.drop" : effect.Key);
        if (world != null)
            world.Events.Emit(new OUTL_Event(OUTL_EventType.ItemDropped, Entity != null ? Entity.Id : OUTL_EntityId.None, adapter != null ? adapter.Id : OUTL_EntityId.None) { Key = effect.Key, IntValue = Mathf.Max(1, effect.IntValue), Point = position });
    }

    private void SendLivingEvent(OUTL_World world, OUTL_LivingActionEffect effect)
    {
        if (world == null || effect == null || effect.EventType == OUTL_EventType.None) return;
        world.Events.Emit(new OUTL_Event(effect.EventType, Entity != null ? Entity.Id : OUTL_EntityId.None, Entity != null ? Entity.Id : OUTL_EntityId.None)
        {
            Key = effect.Key,
            FloatValue = effect.FloatValue,
            IntValue = effect.IntValue,
            Point = transform.position
        });
    }

    private void ApplyTargetFromMemory(OUTL_LivingActionEffect effect)
    {
        if (Behavior == null || Behavior.Runtime == null || TargetMemory == null) return;
        if (effect != null && IsThreatStimulus(effect.StimulusType) && TargetMemory.LastThreatPosition != Vector3.zero)
        {
            Behavior.Runtime.CurrentTargetPosition = TargetMemory.LastThreatPosition;
            return;
        }
        if (TargetMemory.LastFoodPosition != Vector3.zero) Behavior.Runtime.CurrentTargetPosition = TargetMemory.LastFoodPosition;
    }

    private void ApplyFleeTarget(float distance)
    {
        if (Behavior == null || Behavior.Runtime == null) return;
        Vector3 threat = TargetMemory != null && TargetMemory.LastThreatPosition != Vector3.zero ? TargetMemory.LastThreatPosition : transform.position - transform.forward;
        Vector3 away = transform.position - threat;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = -transform.forward;
        Behavior.Runtime.CurrentTargetPosition = transform.position + away.normalized * Mathf.Max(0.1f, distance);
    }

    private bool ApplyFoodTarget()
    {
        if (Behavior == null || Behavior.Runtime == null) return false;
        OUTL_LivingResourceSource source;
        if (OUTL_LivingResourceSource.TryFindNearest(transform.position, StimulusMemoryRadius, "Food", out source) && source != null)
        {
            Behavior.Runtime.CurrentTargetPosition = source.transform.position;
            if (TargetMemory != null)
            {
                OUTL_Stimulus stimulus = new OUTL_Stimulus(OUTL_StimulusType.SightFood, source.Entity != null ? source.Entity.Id : OUTL_EntityId.None, source.transform.position, source.Radius, source.Strength, source.Confidence, source.Priority, source.DecayTime, "resource:" + source.ResourceId, source.ResourceTags);
                TargetMemory.Observe(stimulus, OUTL_World.Instance != null ? OUTL_World.Instance.WorldTime : Time.time);
            }
            return true;
        }

        if (TargetMemory != null && TargetMemory.LastFoodPosition != Vector3.zero)
        {
            Behavior.Runtime.CurrentTargetPosition = TargetMemory.LastFoodPosition;
            return true;
        }
        return false;
    }

    private void EnsureWanderTarget(float time)
    {
        if (Behavior == null || Behavior.Runtime == null) return;
        Vector3 target = Behavior.Runtime.CurrentTargetPosition;
        if (target != Vector3.zero && (target - transform.position).sqrMagnitude > 2.25f) return;
        float angle = DeterministicUnit(time) * Mathf.PI * 2f;
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Mathf.Max(0.1f, WanderRadius);
        Behavior.Runtime.CurrentTargetPosition = transform.position + offset;
    }

    private bool HasEdibleResourceNear()
    {
        OUTL_LivingResourceSource source;
        return OUTL_LivingResourceSource.TryFindNearest(transform.position, EatResourceRadius, "Food", out source);
    }

    private float ComputeGenericEgregoreBias(OUTL_BehaviorActionDef action, OUTL_EgregoreCyclePhase phase, float danger, float safety)
    {
        if (action == null) return 0f;
        bool feeding = IsFeedingAction(action.Type);
        bool reproduction = IsReproductionAction(action.Type);
        bool threat = IsThreatAction(action.Type);
        bool calm = action.Type == OUTL_BehaviorActionId.Wander || action.Type == OUTL_BehaviorActionId.Rest || feeding || reproduction;
        float bias = 0f;
        switch (phase)
        {
            case OUTL_EgregoreCyclePhase.StableWorld:
            case OUTL_EgregoreCyclePhase.RevelationOrBoon:
            case OUTL_EgregoreCyclePhase.Return:
            case OUTL_EgregoreCyclePhase.Integration:
            case OUTL_EgregoreCyclePhase.Renewal:
                if (calm) bias += 0.25f;
                if (reproduction) bias += 0.25f;
                if (threat) bias -= 0.25f;
                break;
            case OUTL_EgregoreCyclePhase.ShadowConfrontation:
            case OUTL_EgregoreCyclePhase.Crisis:
            case OUTL_EgregoreCyclePhase.SacrificeOrDeath:
            case OUTL_EgregoreCyclePhase.Collapse:
                if (threat) bias += 0.45f;
                if (feeding) bias -= 0.35f;
                if (reproduction) bias -= 0.55f;
                break;
            case OUTL_EgregoreCyclePhase.CorruptionLoop:
                if (threat || action.Type == OUTL_BehaviorActionId.Wander || action.Type == OUTL_BehaviorActionId.InvestigateStimulus) bias += 0.22f;
                if (reproduction) bias -= 0.45f;
                break;
            case OUTL_EgregoreCyclePhase.Disturbance:
            case OUTL_EgregoreCyclePhase.Call:
            case OUTL_EgregoreCyclePhase.Threshold:
            case OUTL_EgregoreCyclePhase.Descent:
            case OUTL_EgregoreCyclePhase.Trials:
                if (threat) bias += 0.15f;
                if (action.Type == OUTL_BehaviorActionId.Wander || action.Type == OUTL_BehaviorActionId.InvestigateStimulus) bias += 0.12f;
                break;
        }

        if (feeding || reproduction) bias += safety * 0.15f;
        if (threat) bias += danger * 0.30f;
        return bias;
    }

    private string BuildDecisionReason(OUTL_BehaviorActionDef action)
    {
        string id = action != null ? (string.IsNullOrEmpty(action.ActionId) ? action.Type.ToString() : action.ActionId) : "none";
        OUTL_EgregoreCyclePhase phase = Behavior != null && Behavior.Runtime != null ? Behavior.Runtime.LocalEgregorePhase : OUTL_EgregoreCyclePhase.StableWorld;
        float danger = Behavior != null && Behavior.Runtime != null ? Behavior.Runtime.LocalDanger : 0f;
        float safety = Behavior != null && Behavior.Runtime != null ? Behavior.Runtime.LocalSafety : 0f;
        return id +
               " score=" + CurrentActionScore.ToString("0.00") +
               " phase=" + phase +
               " danger=" + danger.ToString("0.00") +
               " safety=" + safety.ToString("0.00") +
               " egregore=" + LastEgregoreScoreContribution.ToString("0.00") +
               " stimulus=" + LastStimulusScoreContribution.ToString("0.00");
    }

    private float DeterministicUnit(float time)
    {
        unchecked
        {
            int h = LocalSeed;
            h = h * 31 + Mathf.FloorToInt(time / Mathf.Max(0.1f, TickInterval));
            uint x = (uint)h;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            return (x & 0xffff) / 65535f;
        }
    }

    private static bool IsThreatAction(OUTL_BehaviorActionId action)
    {
        return action == OUTL_BehaviorActionId.FleeFromThreat ||
               action == OUTL_BehaviorActionId.Hide ||
               action == OUTL_BehaviorActionId.AvoidArea ||
               action == OUTL_BehaviorActionId.CallForHelp;
    }

    private static bool IsFeedingAction(OUTL_BehaviorActionId action)
    {
        return action == OUTL_BehaviorActionId.FindFood ||
               action == OUTL_BehaviorActionId.Eat;
    }

    private static bool IsReproductionAction(OUTL_BehaviorActionId action)
    {
        return action == OUTL_BehaviorActionId.SeekMate ||
               action == OUTL_BehaviorActionId.Courtship ||
               action == OUTL_BehaviorActionId.PairBond ||
               action == OUTL_BehaviorActionId.MoveToNest ||
               action == OUTL_BehaviorActionId.ReproduceAbstract;
    }

    private static bool IsThreatStimulus(OUTL_StimulusType type)
    {
        return type == OUTL_StimulusType.SightDanger ||
               type == OUTL_StimulusType.TookDamage ||
               type == OUTL_StimulusType.HeardCombat ||
               type == OUTL_StimulusType.Death ||
               type == OUTL_StimulusType.Fear ||
               type == OUTL_StimulusType.Combat ||
               type == OUTL_StimulusType.Alert;
    }

    private static OUTL_BehaviorModeId MapBehaviorMode(OUTL_BehaviorActionId action)
    {
        switch (action)
        {
            case OUTL_BehaviorActionId.FleeFromThreat: return OUTL_BehaviorModeId.Flee;
            case OUTL_BehaviorActionId.AttackTarget:
            case OUTL_BehaviorActionId.Ambush:
            case OUTL_BehaviorActionId.RivalChallenge: return OUTL_BehaviorModeId.Raid;
            case OUTL_BehaviorActionId.Guard:
            case OUTL_BehaviorActionId.ProtectOffspring: return OUTL_BehaviorModeId.Guard;
            case OUTL_BehaviorActionId.Patrol: return OUTL_BehaviorModeId.Patrol;
            case OUTL_BehaviorActionId.Trade: return OUTL_BehaviorModeId.Trade;
            case OUTL_BehaviorActionId.Hide: return OUTL_BehaviorModeId.Hide;
            case OUTL_BehaviorActionId.InvestigateStimulus: return OUTL_BehaviorModeId.Alert;
            case OUTL_BehaviorActionId.Rest: return OUTL_BehaviorModeId.Sleep;
            case OUTL_BehaviorActionId.FindFood:
            case OUTL_BehaviorActionId.Eat:
            case OUTL_BehaviorActionId.Wander:
            case OUTL_BehaviorActionId.FollowHerd:
            case OUTL_BehaviorActionId.SeekMate:
            case OUTL_BehaviorActionId.Courtship:
            case OUTL_BehaviorActionId.PairBond:
            case OUTL_BehaviorActionId.MoveToNest:
            case OUTL_BehaviorActionId.ReproduceAbstract:
                return OUTL_BehaviorModeId.Work;
            default: return OUTL_BehaviorModeId.Normal;
        }
    }

    private void PushStateToRuntime()
    {
        if (Entity == null || Entity.Runtime == null) return;
        Entity.Runtime.State.SetString("DriveAction", CurrentAction.ToString());
        Entity.Runtime.State.SetString("DriveDecisionReason", LastDecisionReason);
        Entity.Runtime.State.SetFloat("DriveActionScore", CurrentActionScore);
        Entity.Runtime.State.SetFloat("DriveScore.Egregore", LastEgregoreScoreContribution);
        Entity.Runtime.State.SetFloat("DriveScore.Danger", LastDangerScoreContribution);
        Entity.Runtime.State.SetFloat("DriveScore.Safety", LastSafetyScoreContribution);
        Entity.Runtime.State.SetFloat("DriveScore.Stimulus", LastStimulusScoreContribution);
        for (int i = 0; i < values.Length; i++) Entity.Runtime.State.SetFloat("Drive." + ((OUTL_DriveId)i), values[i]);
        Entity.Runtime.State.SetInt("PairBondTarget", PairBondTarget.Value);
        Entity.Runtime.State.SetInt("NestCell", NestCell);
        Entity.Runtime.State.SetInt("OffspringPending", OffspringPending);
    }
}

[DisallowMultipleComponent]
public sealed partial class OUTL_LivingResourceSource
{
    private static readonly List<OUTL_LivingResourceSource> activeSources = new List<OUTL_LivingResourceSource>(128);

    public OUTL_EntityAdapter Entity;
    public string ResourceId = "resource.food";
    public string[] ResourceTags = { "Resource", "Food", "Grass" };
    public OUTL_StimulusType StimulusType = OUTL_StimulusType.SightFood;
    public bool AlsoEmitResourceStimulus = true;
    [Min(0f)] public float Radius = 8f;
    [Range(0f, 1f)] public float Priority = 0.35f;
    [Range(0f, 1f)] public float Strength = 1f;
    [Range(0f, 1f)] public float Confidence = 1f;
    [Min(0f)] public float DecayTime = 2.5f;
    [Min(0f)] public float Amount = 10f;
    [Min(0f)] public float MaxAmount = 10f;
    [Min(0f)] public float RegenerationPerSecond = 0.1f;
    [Min(0f)] public float ConsumeAmount = 1f;
    public bool DepleteWhenEmpty = true;
    public bool EmitWhenEmpty;
    public string[] EgregoreTags;
    [Min(0.05f)] public float TickInterval = 1f;
    public int LocalSeed;

    private bool registered;
    private bool depleted;

    public static int ActiveCount { get { return activeSources.Count; } }
    public bool IsDepleted { get { return depleted || (DepleteWhenEmpty && Amount <= 0f); } }
    public string OUTL_SaveKey { get { return "OUTL_LivingResourceSource:" + (string.IsNullOrEmpty(ResourceId) ? name : ResourceId); } }
    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled; } }
    public OUTL_TickLane OUTL_TickLane { get { return OUTL_TickLane.AI; } }
    public float OUTL_TickInterval { get { return Mathf.Max(0.05f, TickInterval); } }

    private void Awake()
    {
        ResolveReferences();
        if (LocalSeed == 0) LocalSeed = BuildSeed();
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (!activeSources.Contains(this)) activeSources.Add(this);
        Register();
    }

    private void OnDisable()
    {
        activeSources.Remove(this);
        Unregister();
    }

    public void Register()
    {
        if (registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Register(this);
        registered = true;
    }

    public void Unregister()
    {
        if (!registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Scheduler.Unregister(this);
        registered = false;
    }

    public void OUTL_OnPoolSpawn()
    {
        depleted = false;
        if (MaxAmount > 0f && Amount <= 0f) Amount = MaxAmount;
        if (!activeSources.Contains(this)) activeSources.Add(this);
        Register();
    }

    public void OUTL_OnPoolRelease()
    {
        Unregister();
        activeSources.Remove(this);
    }

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        if (MaxAmount > 0f && RegenerationPerSecond > 0f)
        {
            Amount = Mathf.Min(MaxAmount, Amount + RegenerationPerSecond * Mathf.Max(0f, deltaTime));
            if (Amount > 0f) depleted = false;
        }

        if (!EmitWhenEmpty && IsDepleted) return;
        OUTL_EntityId source = Entity != null ? Entity.Id : OUTL_EntityId.None;
        float strength = MaxAmount > 0f ? Mathf.Clamp01(Amount / MaxAmount) * Strength : Strength;
        EmitStimulus(StimulusType, source, strength, "resource:" + ResourceId);
        if (AlsoEmitResourceStimulus && StimulusType != OUTL_StimulusType.Resource)
            EmitStimulus(OUTL_StimulusType.Resource, source, strength, "resource:" + ResourceId);
    }

    public bool TryConsume(float requested, OUTL_EntityAdapter consumer, out float consumed)
    {
        consumed = 0f;
        if (IsDepleted || Amount <= 0f) return false;

        float request = requested > 0f ? requested : Mathf.Max(0.01f, ConsumeAmount);
        consumed = Mathf.Min(Amount, request);
        Amount = Mathf.Max(0f, Amount - consumed);
        depleted = DepleteWhenEmpty && Amount <= 0f;

        OUTL_EntityId source = consumer != null ? consumer.Id : OUTL_EntityId.None;
        OUTL_StimulusBus.Emit(new OUTL_Stimulus(
            OUTL_StimulusType.Resource,
            source,
            transform.position,
            Mathf.Max(0.1f, Radius * 0.5f),
            Mathf.Clamp01(consumed),
            1f,
            Mathf.Clamp01(Priority),
            Mathf.Max(0f, DecayTime),
            "resource.consumed:" + ResourceId,
            ResourceTags));
        return true;
    }

    public bool HasTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return true;
        return ContainsTag(ResourceTags, tag) || ContainsTag(EgregoreTags, tag);
    }

    public static bool TryFindNearest(Vector3 position, float radius, string requiredTag, out OUTL_LivingResourceSource source)
    {
        source = null;
        float bestSqr = Mathf.Max(0f, radius) * Mathf.Max(0f, radius);
        for (int i = 0; i < activeSources.Count; i++)
        {
            OUTL_LivingResourceSource candidate = activeSources[i];
            if (candidate == null || !candidate.isActiveAndEnabled || candidate.IsDepleted) continue;
            if (!candidate.HasTag(requiredTag)) continue;
            float sqr = (candidate.transform.position - position).sqrMagnitude;
            float effective = radius > 0f ? bestSqr : candidate.Radius * candidate.Radius;
            if (sqr > effective) continue;
            if (source != null && sqr >= bestSqr) continue;
            source = candidate;
            bestSqr = sqr;
        }
        return source != null;
    }

    public static bool TryConsumeNearest(Vector3 position, float radius, string requiredTag, OUTL_EntityAdapter consumer, float requested, out OUTL_LivingResourceSource source, out float consumed)
    {
        consumed = 0f;
        if (!TryFindNearest(position, radius, requiredTag, out source)) return false;
        return source.TryConsume(requested, consumer, out consumed);
    }

    public void OUTL_Capture(OUTL_ComponentSaveWriter writer)
    {
        if (writer == null) return;
        writer.SetFloat("amount", Amount);
        writer.SetFloat("maxAmount", MaxAmount);
        writer.SetFlag("depleted", depleted);
        writer.SetInt("seed", LocalSeed);
    }

    public void OUTL_Restore(OUTL_ComponentSaveReader reader)
    {
        if (reader == null) return;
        Amount = Mathf.Max(0f, reader.GetFloat("amount", Amount));
        MaxAmount = Mathf.Max(0f, reader.GetFloat("maxAmount", MaxAmount));
        depleted = reader.GetFlag("depleted", depleted);
        LocalSeed = reader.GetInt("seed", LocalSeed != 0 ? LocalSeed : BuildSeed());
    }

    private void ResolveReferences()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (MaxAmount <= 0f && Amount > 0f) MaxAmount = Amount;
    }

    private void EmitStimulus(OUTL_StimulusType type, OUTL_EntityId source, float strength, string key)
    {
        if (type == OUTL_StimulusType.None) return;
        OUTL_StimulusBus.Emit(new OUTL_Stimulus(
            type,
            source,
            transform.position,
            Mathf.Max(0.1f, Radius),
            Mathf.Clamp01(strength),
            Mathf.Clamp01(Confidence),
            Mathf.Clamp01(Priority),
            Mathf.Max(0f, DecayTime),
            key,
            ResourceTags));
    }

    private int BuildSeed()
    {
        unchecked
        {
            int hash = OUTL_WorldCellUtility.StableStringHash(ResourceId);
            hash = hash * 31 + OUTL_WorldCellUtility.StableStringHash(name);
            return hash != 0 ? hash : 1;
        }
    }

    private static bool ContainsTag(string[] tags, string required)
    {
        if (tags == null || string.IsNullOrEmpty(required)) return false;
        for (int i = 0; i < tags.Length; i++)
            if (string.Equals(tags[i], required, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}

[DisallowMultipleComponent]
public sealed partial class OUTL_LivingActionTargetMemory
{
    public OUTL_EntityAdapter Entity;
    public Vector3 LastThreatPosition;
    public Vector3 LastFoodPosition;
    public OUTL_EntityId LastThreatEntity = OUTL_EntityId.None;
    public OUTL_EntityId LastFoodEntity = OUTL_EntityId.None;
    public OUTL_EntityId LastHerdEntity = OUTL_EntityId.None;
    public OUTL_EntityId LastMateCandidate = OUTL_EntityId.None;
    public OUTL_StimulusType LastStimulusType = OUTL_StimulusType.None;
    public string LastStimulusKey;
    public float LastStimulusTime = -999f;
    [Min(0f)] public float MemoryDuration = 12f;

    public string OUTL_SaveKey { get { return "OUTL_LivingActionTargetMemory"; } }

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
    }

    public void Observe(in OUTL_Stimulus stimulus, float time)
    {
        LastStimulusType = stimulus.Type;
        LastStimulusKey = stimulus.Key;
        LastStimulusTime = time;

        if (IsThreat(stimulus.Type))
        {
            LastThreatPosition = stimulus.Position;
            LastThreatEntity = stimulus.Source;
            return;
        }

        if (IsFood(stimulus.Type, stimulus.Tags))
        {
            LastFoodPosition = stimulus.Position;
            LastFoodEntity = stimulus.Source;
            return;
        }

        if (stimulus.Type == OUTL_StimulusType.Social || stimulus.Type == OUTL_StimulusType.SightAlly)
        {
            LastHerdEntity = stimulus.Source;
            LastMateCandidate = stimulus.Source;
        }
    }

    public bool HasFreshThreat(float time)
    {
        return time - LastStimulusTime <= MemoryDuration && (LastThreatEntity.IsValid || LastThreatPosition != Vector3.zero);
    }

    public bool HasFreshFood(float time)
    {
        return time - LastStimulusTime <= MemoryDuration && (LastFoodEntity.IsValid || LastFoodPosition != Vector3.zero);
    }

    public void OUTL_OnPoolSpawn()
    {
        Clear();
    }

    public void OUTL_OnPoolRelease()
    {
        Clear();
    }

    public void OUTL_Capture(OUTL_ComponentSaveWriter writer)
    {
        if (writer == null) return;
        writer.SetFloat("threat.x", LastThreatPosition.x);
        writer.SetFloat("threat.y", LastThreatPosition.y);
        writer.SetFloat("threat.z", LastThreatPosition.z);
        writer.SetFloat("food.x", LastFoodPosition.x);
        writer.SetFloat("food.y", LastFoodPosition.y);
        writer.SetFloat("food.z", LastFoodPosition.z);
        writer.SetInt("threatEntity", LastThreatEntity.Value);
        writer.SetInt("foodEntity", LastFoodEntity.Value);
        writer.SetInt("herdEntity", LastHerdEntity.Value);
        writer.SetInt("mateEntity", LastMateCandidate.Value);
        writer.SetInt("stimulusType", (int)LastStimulusType);
        writer.SetString("stimulusKey", LastStimulusKey);
        writer.SetFloat("stimulusTime", LastStimulusTime);
    }

    public void OUTL_Restore(OUTL_ComponentSaveReader reader)
    {
        if (reader == null) return;
        LastThreatPosition = new Vector3(reader.GetFloat("threat.x", LastThreatPosition.x), reader.GetFloat("threat.y", LastThreatPosition.y), reader.GetFloat("threat.z", LastThreatPosition.z));
        LastFoodPosition = new Vector3(reader.GetFloat("food.x", LastFoodPosition.x), reader.GetFloat("food.y", LastFoodPosition.y), reader.GetFloat("food.z", LastFoodPosition.z));
        LastThreatEntity = new OUTL_EntityId(reader.GetInt("threatEntity", LastThreatEntity.Value));
        LastFoodEntity = new OUTL_EntityId(reader.GetInt("foodEntity", LastFoodEntity.Value));
        LastHerdEntity = new OUTL_EntityId(reader.GetInt("herdEntity", LastHerdEntity.Value));
        LastMateCandidate = new OUTL_EntityId(reader.GetInt("mateEntity", LastMateCandidate.Value));
        LastStimulusType = (OUTL_StimulusType)Mathf.Clamp(reader.GetInt("stimulusType", (int)LastStimulusType), 0, (int)OUTL_StimulusType.Egregore);
        LastStimulusKey = reader.GetString("stimulusKey", LastStimulusKey);
        LastStimulusTime = reader.GetFloat("stimulusTime", LastStimulusTime);
    }

    private void Clear()
    {
        LastThreatPosition = Vector3.zero;
        LastFoodPosition = Vector3.zero;
        LastThreatEntity = OUTL_EntityId.None;
        LastFoodEntity = OUTL_EntityId.None;
        LastHerdEntity = OUTL_EntityId.None;
        LastMateCandidate = OUTL_EntityId.None;
        LastStimulusType = OUTL_StimulusType.None;
        LastStimulusKey = "";
        LastStimulusTime = -999f;
    }

    private static bool IsThreat(OUTL_StimulusType type)
    {
        return type == OUTL_StimulusType.SightDanger ||
               type == OUTL_StimulusType.TookDamage ||
               type == OUTL_StimulusType.HeardCombat ||
               type == OUTL_StimulusType.Death ||
               type == OUTL_StimulusType.Fear ||
               type == OUTL_StimulusType.Combat ||
               type == OUTL_StimulusType.Alert;
    }

    private static bool IsFood(OUTL_StimulusType type, string[] tags)
    {
        if (type == OUTL_StimulusType.SightFood || type == OUTL_StimulusType.Resource) return true;
        if (tags == null) return false;
        for (int i = 0; i < tags.Length; i++)
            if (string.Equals(tags[i], "Food", StringComparison.OrdinalIgnoreCase) || string.Equals(tags[i], "Grass", StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}
