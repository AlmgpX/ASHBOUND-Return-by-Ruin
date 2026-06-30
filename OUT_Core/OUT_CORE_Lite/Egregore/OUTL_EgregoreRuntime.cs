using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class OUTL_EgregoreRuntime
{
    public string EgregoreId;
    public OUTL_EgregoreScope Scope;
    public float Violence;
    public float Fear;
    public float Prosperity = 0.5f;
    public float Corruption;
    public float Alertness;
    public float Hostility;
    public float ResourcePressure;
    public float Entropy;
    public float PlayerReputation;
    public OUTL_EgregoreMood DominantMood = OUTL_EgregoreMood.Stable;
    public OUTL_EgregoreCyclePhase CurrentCyclePhase = OUTL_EgregoreCyclePhase.StableWorld;
    public OUTL_EgregoreArchetypeId DominantArchetype = OUTL_EgregoreArchetypeId.SelfCenter;
    public OUTL_EgregoreArchetypeId ShadowArchetype = OUTL_EgregoreArchetypeId.Shadow;
    public float UnresolvedTension;
    public float IntegrationProgress;
    public float CorruptionProgress;
    public float RenewalProgress;
    public float TraumaMemory;
    public float BoonMemory;
    public float SacrificeDebt;
    public bool ThresholdOpen;
    public string PendingTransformationOutputs;
    public float HungerPressure;
    public float AttractionPressure;
    public float SafetyTrust = 0.5f;
    public float RitualTension;
    public float SpawnPressure;
    public float QuestPressure;
    public float LootPressure;
    public float BehaviorPressure;
    public float LastUpdateTime;
    public int[] OwnedSectorIds;
    public string LastSignal;
    public string LastEffect;
    public int LastSectorEntityCount;
    public int LastSectorStimulusCount;

    private readonly List<OUTL_EgregoreSignal> signals = new List<OUTL_EgregoreSignal>(32);
    private readonly List<OUTL_EgregoreMemoryTrace> memoryTraces = new List<OUTL_EgregoreMemoryTrace>(32);
    private readonly float[] archetypePressure = new float[(int)OUTL_EgregoreArchetypeId.Count];
    private int memoryDecayCursor;
    private OUTL_EgregoreCyclePhase lastEmittedPhase;

    public int SignalCount { get { return signals.Count; } }
    public int MemoryTraceCount { get { return memoryTraces.Count; } }
    public int OwnedSectorCount { get { return OwnedSectorIds != null ? OwnedSectorIds.Length : 0; } }
    public OUTL_EgregoreMood Mood { get { return DominantMood; } set { DominantMood = value; } }
    public float Health { get { return Mathf.Clamp01(1f - Entropy); } set { Entropy = Mathf.Clamp01(1f - value); } }

    public void Initialize(OUTL_EgregoreDef def)
    {
        EgregoreId = def != null && !string.IsNullOrEmpty(def.EgregoreId) ? def.EgregoreId : "egregore_generic";
        Scope = def != null ? def.Scope : OUTL_EgregoreScope.Local;
        OwnedSectorIds = def != null ? def.OwnedSectorIds : null;
        ClearArchetypalState();
        CurrentCyclePhase = def != null && def.ArchetypalCycle != null ? def.ArchetypalCycle.InitialPhase : OUTL_EgregoreCyclePhase.StableWorld;
        lastEmittedPhase = CurrentCyclePhase;
        ApplyInitialPressures(def);
    }

    public void ApplyEvent(in OUTL_Event evt, OUTL_EgregoreDef def)
    {
        if (def == null) return;
        if (evt.Type == OUTL_EventType.Damaged)
        {
            float amount = Mathf.Max(0.05f, evt.FloatValue * 0.01f);
            Violence = Saturate(Violence + amount * def.ViolenceWeight);
            Fear = Saturate(Fear + def.FearWeight * 0.5f);
            Alertness = Saturate(Alertness + def.AlertnessWeight);
            AddTrace(OUTL_EgregoreTraceType.Combat, evt.Source, evt.Point, amount, "damage:" + evt.Key, "combat,shadow", def);
            AddArchetypePressure(OUTL_EgregoreArchetypeId.Warrior, amount * 0.5f);
            AddArchetypePressure(OUTL_EgregoreArchetypeId.Shadow, amount * 0.35f);
            UnresolvedTension = Saturate(UnresolvedTension + amount);
        }
        else if (evt.Type == OUTL_EventType.Killed)
        {
            Violence = Saturate(Violence + def.ViolenceWeight);
            Fear = Saturate(Fear + def.FearWeight);
            Entropy = Saturate(Entropy + 0.25f);
            Alertness = Saturate(Alertness + def.AlertnessWeight);
            TraumaMemory = Saturate(TraumaMemory + 0.25f);
            SacrificeDebt = Saturate(SacrificeDebt + 0.15f);
            UnresolvedTension = Saturate(UnresolvedTension + 0.25f);
            AddTrace(OUTL_EgregoreTraceType.Death, evt.Source, evt.Point, 1f, "death:" + evt.Key, "death,shadow,trauma", def);
            AddArchetypePressure(OUTL_EgregoreArchetypeId.Shadow, 0.65f);
            AddArchetypePressure(OUTL_EgregoreArchetypeId.VoidDeathRebirth, 0.45f);
            AddArchetypePressure(OUTL_EgregoreArchetypeId.Sacrifice, 0.25f);
        }
        else if (evt.Type == OUTL_EventType.ItemDropped)
        {
            ResourcePressure = Saturate(ResourcePressure + 0.05f);
            AddTrace(OUTL_EgregoreTraceType.ResourceDepletion, evt.Source, evt.Point, 0.35f, "loot:" + evt.Key, "loot,resource", def);
        }
        else if (evt.Type == OUTL_EventType.PickedUp || evt.Type == OUTL_EventType.ItemTaken)
        {
            Prosperity = Saturate(Prosperity + 0.04f * def.ProsperityWeight);
            AddTrace(OUTL_EgregoreTraceType.Trade, evt.Source, evt.Point, 0.25f, "pickup:" + evt.Key, "resource,trade", def);
        }
        else if (evt.Type == OUTL_EventType.ContainerOpened || evt.Type == OUTL_EventType.ContainerLooted)
        {
            Alertness = Saturate(Alertness + 0.15f * def.AlertnessWeight);
            AddTrace(OUTL_EgregoreTraceType.Theft, evt.Source, evt.Point, 0.35f, "container:" + evt.Key, "theft,trickster,alert", def);
            AddArchetypePressure(OUTL_EgregoreArchetypeId.Trickster, 0.3f);
            AddArchetypePressure(OUTL_EgregoreArchetypeId.Shadow, 0.15f);
        }
        else if (evt.Type == OUTL_EventType.QuestCompleted)
        {
            OUTL_EgregoreQuestHook hook = (OUTL_EgregoreQuestHook)Mathf.Clamp(evt.IntValue, 0, 8);
            ApplyQuestHook(hook, true, evt.Point, evt.Key, def);
        }
        else if (evt.Type == OUTL_EventType.QuestFailed)
        {
            OUTL_EgregoreQuestHook hook = (OUTL_EgregoreQuestHook)Mathf.Clamp(evt.IntValue, 0, 8);
            ApplyQuestHook(hook, false, evt.Point, evt.Key, def);
        }
    }

    public void ApplyStimulus(OUTL_Stimulus stimulus, OUTL_EgregoreDef def)
    {
        if (def == null) return;
        float p = Mathf.Clamp01(Mathf.Max(stimulus.Priority, stimulus.Strength));
        switch (stimulus.Type)
        {
            case OUTL_StimulusType.HeardCombat:
            case OUTL_StimulusType.Combat:
            case OUTL_StimulusType.Damage:
            case OUTL_StimulusType.TookDamage:
                Violence = Saturate(Violence + p * def.ViolenceWeight);
                Alertness = Saturate(Alertness + p * def.AlertnessWeight);
                UnresolvedTension = Saturate(UnresolvedTension + p * 0.35f);
                AddTrace(OUTL_EgregoreTraceType.Combat, stimulus.Source, stimulus.Position, p, "stimulus:" + stimulus.Key, "combat,shadow", def);
                AddArchetypePressure(OUTL_EgregoreArchetypeId.Warrior, p * 0.25f);
                AddArchetypePressure(OUTL_EgregoreArchetypeId.Shadow, p * 0.2f);
                break;
            case OUTL_StimulusType.Death:
                Fear = Saturate(Fear + p * def.FearWeight);
                Entropy = Saturate(Entropy + p * 0.25f);
                TraumaMemory = Saturate(TraumaMemory + p * 0.25f);
                SacrificeDebt = Saturate(SacrificeDebt + p * 0.1f);
                AddTrace(OUTL_EgregoreTraceType.Death, stimulus.Source, stimulus.Position, p, "stimulus:" + stimulus.Key, "death,shadow,trauma", def);
                AddArchetypePressure(OUTL_EgregoreArchetypeId.Shadow, p * 0.4f);
                AddArchetypePressure(OUTL_EgregoreArchetypeId.VoidDeathRebirth, p * 0.25f);
                break;
            case OUTL_StimulusType.Fear:
            case OUTL_StimulusType.Fire:
            case OUTL_StimulusType.SightDanger:
            case OUTL_StimulusType.Alert:
                Fear = Saturate(Fear + p * def.FearWeight);
                Alertness = Saturate(Alertness + p * def.AlertnessWeight);
                UnresolvedTension = Saturate(UnresolvedTension + p * 0.2f);
                AddTrace(OUTL_EgregoreTraceType.Raid, stimulus.Source, stimulus.Position, p * 0.5f, "danger:" + stimulus.Key, "danger,threshold", def);
                AddArchetypePressure(OUTL_EgregoreArchetypeId.ThresholdGuardian, p * 0.2f);
                break;
            case OUTL_StimulusType.Resource:
            case OUTL_StimulusType.SightFood:
                ResourcePressure = Mathf.MoveTowards(ResourcePressure, 0f, p * def.ResourceWeight);
                Prosperity = Saturate(Prosperity + p * def.ProsperityWeight);
                HungerPressure = Mathf.MoveTowards(HungerPressure, 0f, p * 0.35f);
                RenewalProgress = Saturate(RenewalProgress + p * 0.1f);
                AddTrace(OUTL_EgregoreTraceType.Prosperity, stimulus.Source, stimulus.Position, p * 0.5f, "resource:" + stimulus.Key, "resource,renewal", def);
                AddArchetypePressure(OUTL_EgregoreArchetypeId.GreatMother, p * 0.2f);
                break;
            case OUTL_StimulusType.Social:
                PlayerReputation = Mathf.Clamp(PlayerReputation + p * 0.05f, -1f, 1f);
                SafetyTrust = Saturate(SafetyTrust + p * 0.1f);
                AddTrace(OUTL_EgregoreTraceType.Trade, stimulus.Source, stimulus.Position, p * 0.25f, "social:" + stimulus.Key, "social,persona", def);
                AddArchetypePressure(OUTL_EgregoreArchetypeId.Persona, p * 0.1f);
                break;
            case OUTL_StimulusType.Egregore:
                Alertness = Saturate(Alertness + p * 0.1f);
                break;
            case OUTL_StimulusType.Territory:
            case OUTL_StimulusType.Scripted:
                ApplyKeyedStimulus(stimulus, p, def);
                break;
        }

        ApplyTags(stimulus.Tags, stimulus.Source, stimulus.Position, p, stimulus.Key, def);
    }

    public OUTL_EgregoreSignal Tick(OUTL_EgregoreDef def, float time, float deltaTime, Vector3 position)
    {
        if (def == null) return default(OUTL_EgregoreSignal);
        if (def.ArchetypalCycle != null) def.ArchetypalCycle.Sanitize();
        float calm = ResolveDecay(def, time) * Mathf.Max(0f, deltaTime);
        Violence = Mathf.MoveTowards(Violence, 0f, calm);
        Fear = Mathf.MoveTowards(Fear, 0f, calm);
        Alertness = Mathf.MoveTowards(Alertness, 0f, calm * 0.75f);
        ResourcePressure = Mathf.MoveTowards(ResourcePressure, 0f, calm * 0.5f);
        DecayArchetypalState(def, time, deltaTime);
        Corruption = Saturate(Corruption + def.CorruptionWeight * deltaTime);
        Hostility = Saturate(Violence * def.HostilityWeight + Fear * 0.2f + Corruption * 0.25f);
        Entropy = Saturate(Entropy + Violence * 0.005f * deltaTime);
        ResolveArchetypalState(def);
        ResolveOutputPressures();
        DominantMood = ResolveMood();
        LastUpdateTime = time;

        if (CurrentCyclePhase != lastEmittedPhase)
        {
            lastEmittedPhase = CurrentCyclePhase;
            return MakeSignal(OUTL_EgregoreSignalType.CyclePhaseChanged, Mathf.Max(0.25f, Mathf.Max(UnresolvedTension, Mathf.Max(CorruptionProgress, RenewalProgress))), time, position, BuildCycleKey());
        }

        if (ThresholdOpen && CurrentCyclePhase == OUTL_EgregoreCyclePhase.Threshold)
            return MakeSignal(OUTL_EgregoreSignalType.OpenThreshold, UnresolvedTension, time, position, BuildCycleKey());
        if (CurrentCyclePhase == OUTL_EgregoreCyclePhase.CorruptionLoop)
            return MakeSignal(OUTL_EgregoreSignalType.CollapseWarning, CorruptionProgress, time, position, BuildCycleKey());
        if (CurrentCyclePhase == OUTL_EgregoreCyclePhase.Renewal)
            return MakeSignal(OUTL_EgregoreSignalType.RenewalPulse, RenewalProgress, time, position, BuildCycleKey());
        if (Alertness >= def.AlertThreshold)
            return MakeSignal(OUTL_EgregoreSignalType.RaiseAlert, Alertness, time, position, "alert");
        if (Hostility >= def.HostilityThreshold)
            return MakeSignal(OUTL_EgregoreSignalType.IncreaseHostility, Hostility, time, position, "hostility");
        if (Fear >= def.FearThreshold)
            return MakeSignal(OUTL_EgregoreSignalType.CalmWildlife, Fear, time, position, "fear");

        return default(OUTL_EgregoreSignal);
    }

    public OUTL_EgregoreField BuildField(Vector3 position, float cellSize, float time)
    {
        OUTL_WorldAddress address = OUTL_WorldAddress.FromWorldPosition(position, cellSize);
        return new OUTL_EgregoreField
        {
            EgregoreId = EgregoreId,
            Cell = address.ActivityCell,
            Mood = DominantMood,
            CyclePhase = CurrentCyclePhase,
            DominantArchetype = DominantArchetype,
            ShadowArchetype = ShadowArchetype,
            Fear = Fear,
            Violence = Violence,
            Prosperity = Prosperity,
            Corruption = Mathf.Max(Corruption, CorruptionProgress),
            Alertness = Alertness,
            Hostility = Hostility,
            Hunger = HungerPressure,
            Desire = AttractionPressure,
            Safety = SafetyTrust,
            RitualTension = RitualTension,
            SpawnPressure = SpawnPressure,
            QuestPressure = QuestPressure,
            LootPressure = LootPressure,
            BehaviorPressure = BehaviorPressure,
            LastUpdatedTime = time
        };
    }

    public string BuildStimulusKey(string fallback)
    {
        string key = !string.IsNullOrEmpty(fallback) ? fallback : "egregore";
        return key + "|" + EgregoreId + "|" + CurrentCyclePhase + "|" + DominantArchetype + "|" + ShadowArchetype;
    }

    public float GetArchetypePressure(OUTL_EgregoreArchetypeId archetype)
    {
        int index = (int)archetype;
        if (index <= 0 || index >= archetypePressure.Length) return 0f;
        return archetypePressure[index];
    }

    public int CopyMemoryTraces(List<OUTL_EgregoreMemoryTrace> output)
    {
        if (output == null) return 0;
        output.Clear();
        for (int i = 0; i < memoryTraces.Count; i++) output.Add(memoryTraces[i]);
        return output.Count;
    }

    public void Capture(OUTL_ComponentSaveWriter writer)
    {
        if (writer == null) return;
        writer.SetString("id", EgregoreId);
        writer.SetInt("phase", (int)CurrentCyclePhase);
        writer.SetInt("dominantArchetype", (int)DominantArchetype);
        writer.SetInt("shadowArchetype", (int)ShadowArchetype);
        writer.SetFloat("violence", Violence);
        writer.SetFloat("fear", Fear);
        writer.SetFloat("prosperity", Prosperity);
        writer.SetFloat("corruption", Corruption);
        writer.SetFloat("alertness", Alertness);
        writer.SetFloat("hostility", Hostility);
        writer.SetFloat("resourcePressure", ResourcePressure);
        writer.SetFloat("entropy", Entropy);
        writer.SetFloat("reputation", PlayerReputation);
        writer.SetFloat("tension", UnresolvedTension);
        writer.SetFloat("integration", IntegrationProgress);
        writer.SetFloat("corruptionProgress", CorruptionProgress);
        writer.SetFloat("renewal", RenewalProgress);
        writer.SetFloat("trauma", TraumaMemory);
        writer.SetFloat("boon", BoonMemory);
        writer.SetFloat("sacrificeDebt", SacrificeDebt);
        writer.SetFloat("hunger", HungerPressure);
        writer.SetFloat("desire", AttractionPressure);
        writer.SetFloat("safety", SafetyTrust);
        writer.SetFloat("ritual", RitualTension);
        writer.SetString("outputs", PendingTransformationOutputs);
        writer.SetFlag("thresholdOpen", ThresholdOpen);

        int pressureCount = 0;
        for (int i = 1; i < archetypePressure.Length; i++)
        {
            if (archetypePressure[i] <= 0.001f) continue;
            writer.SetFloat("arch." + i, archetypePressure[i]);
            pressureCount++;
        }
        writer.SetInt("archetypePressureCount", pressureCount);

        int traceCount = Mathf.Min(memoryTraces.Count, 16);
        writer.SetInt("traceCount", traceCount);
        for (int i = 0; i < traceCount; i++)
        {
            OUTL_EgregoreMemoryTrace trace = memoryTraces[i];
            string p = "trace." + i + ".";
            writer.SetInt(p + "type", (int)trace.Type);
            writer.SetInt(p + "source", trace.Source.Value);
            writer.SetInt(p + "cellX", trace.Cell.X);
            writer.SetInt(p + "cellZ", trace.Cell.Z);
            writer.SetInt(p + "cellLayer", (int)trace.Cell.Layer);
            writer.SetFloat(p + "x", trace.Position.x);
            writer.SetFloat(p + "y", trace.Position.y);
            writer.SetFloat(p + "z", trace.Position.z);
            writer.SetFloat(p + "intensity", trace.Intensity);
            writer.SetFloat(p + "time", trace.Time);
            writer.SetFloat(p + "decay", trace.DecayRate);
            writer.SetString(p + "key", trace.Key);
            writer.SetString(p + "tags", trace.Tags);
        }
    }

    public void Restore(OUTL_ComponentSaveReader reader)
    {
        if (reader == null || !reader.HasPayload) return;
        EgregoreId = reader.GetString("id", EgregoreId);
        CurrentCyclePhase = ClampPhase(reader.GetInt("phase", (int)CurrentCyclePhase));
        lastEmittedPhase = CurrentCyclePhase;
        DominantArchetype = ClampArchetype(reader.GetInt("dominantArchetype", (int)DominantArchetype));
        ShadowArchetype = ClampArchetype(reader.GetInt("shadowArchetype", (int)ShadowArchetype));
        Violence = Saturate(reader.GetFloat("violence", Violence));
        Fear = Saturate(reader.GetFloat("fear", Fear));
        Prosperity = Saturate(reader.GetFloat("prosperity", Prosperity));
        Corruption = Saturate(reader.GetFloat("corruption", Corruption));
        Alertness = Saturate(reader.GetFloat("alertness", Alertness));
        Hostility = Saturate(reader.GetFloat("hostility", Hostility));
        ResourcePressure = Saturate(reader.GetFloat("resourcePressure", ResourcePressure));
        Entropy = Saturate(reader.GetFloat("entropy", Entropy));
        PlayerReputation = Mathf.Clamp(reader.GetFloat("reputation", PlayerReputation), -1f, 1f);
        UnresolvedTension = Saturate(reader.GetFloat("tension", UnresolvedTension));
        IntegrationProgress = Saturate(reader.GetFloat("integration", IntegrationProgress));
        CorruptionProgress = Saturate(reader.GetFloat("corruptionProgress", CorruptionProgress));
        RenewalProgress = Saturate(reader.GetFloat("renewal", RenewalProgress));
        TraumaMemory = Saturate(reader.GetFloat("trauma", TraumaMemory));
        BoonMemory = Saturate(reader.GetFloat("boon", BoonMemory));
        SacrificeDebt = Saturate(reader.GetFloat("sacrificeDebt", SacrificeDebt));
        HungerPressure = Saturate(reader.GetFloat("hunger", HungerPressure));
        AttractionPressure = Saturate(reader.GetFloat("desire", AttractionPressure));
        SafetyTrust = Saturate(reader.GetFloat("safety", SafetyTrust));
        RitualTension = Saturate(reader.GetFloat("ritual", RitualTension));
        PendingTransformationOutputs = reader.GetString("outputs", PendingTransformationOutputs);
        ThresholdOpen = reader.GetFlag("thresholdOpen", ThresholdOpen);

        for (int i = 0; i < archetypePressure.Length; i++) archetypePressure[i] = 0f;
        for (int i = 1; i < archetypePressure.Length; i++)
            archetypePressure[i] = Saturate(reader.GetFloat("arch." + i, archetypePressure[i]));

        memoryTraces.Clear();
        int traceCount = Mathf.Clamp(reader.GetInt("traceCount", 0), 0, 16);
        for (int i = 0; i < traceCount; i++)
        {
            string p = "trace." + i + ".";
            memoryTraces.Add(new OUTL_EgregoreMemoryTrace
            {
                Type = (OUTL_EgregoreTraceType)Mathf.Clamp(reader.GetInt(p + "type", 0), 0, 255),
                Source = new OUTL_EntityId(reader.GetInt(p + "source", 0)),
                Cell = new OUTL_WorldCellKey(reader.GetInt(p + "cellX", 0), reader.GetInt(p + "cellZ", 0), (OUTL_WorldCellLayer)Mathf.Clamp(reader.GetInt(p + "cellLayer", (int)OUTL_WorldCellLayer.ActivityCell), 0, 32)),
                Position = new Vector3(reader.GetFloat(p + "x", 0f), reader.GetFloat(p + "y", 0f), reader.GetFloat(p + "z", 0f)),
                Intensity = Saturate(reader.GetFloat(p + "intensity", 0f)),
                Time = reader.GetFloat(p + "time", 0f),
                DecayRate = Mathf.Max(0f, reader.GetFloat(p + "decay", 0f)),
                Key = reader.GetString(p + "key", string.Empty),
                Tags = reader.GetString(p + "tags", string.Empty)
            });
        }
    }

    public void ReceiveSignal(OUTL_EgregoreSignal signal)
    {
        if (signal.SignalType == OUTL_EgregoreSignalType.None) return;
        signals.Add(signal);
        LastSignal = signal.SignalType + ":" + signal.Key;
        if (signal.SignalType == OUTL_EgregoreSignalType.RaiseAlert) Alertness = Saturate(Alertness + signal.Intensity * 0.25f);
        else if (signal.SignalType == OUTL_EgregoreSignalType.IncreaseHostility) Hostility = Saturate(Hostility + signal.Intensity * 0.25f);
        else if (signal.SignalType == OUTL_EgregoreSignalType.CalmWildlife) Fear = Mathf.MoveTowards(Fear, 0f, signal.Intensity * 0.15f);
    }

    public int ProcessSignals(float time, int budget)
    {
        int processed = 0;
        for (int i = signals.Count - 1; i >= 0 && processed < budget; i--)
        {
            OUTL_EgregoreSignal signal = signals[i];
            if (signal.Ttl <= 0f || time - signal.Time >= signal.Ttl)
            {
                signals.RemoveAt(i);
                processed++;
            }
        }
        return processed;
    }

    private void ClearArchetypalState()
    {
        signals.Clear();
        memoryTraces.Clear();
        for (int i = 0; i < archetypePressure.Length; i++) archetypePressure[i] = 0f;
        UnresolvedTension = 0f;
        IntegrationProgress = 0f;
        CorruptionProgress = 0f;
        RenewalProgress = 0f;
        TraumaMemory = 0f;
        BoonMemory = 0f;
        SacrificeDebt = 0f;
        ThresholdOpen = false;
        PendingTransformationOutputs = string.Empty;
        HungerPressure = 0f;
        AttractionPressure = 0f;
        SafetyTrust = 0.5f;
        RitualTension = 0f;
        SpawnPressure = 0f;
        QuestPressure = 0f;
        LootPressure = 0f;
        BehaviorPressure = 0f;
        memoryDecayCursor = 0;
    }

    private void ApplyInitialPressures(OUTL_EgregoreDef def)
    {
        AddArchetypePressure(OUTL_EgregoreArchetypeId.SelfCenter, 0.25f);
        if (def != null && def.ArchetypeProfile != null && def.ArchetypeProfile.DefaultPressures != null)
            ApplyPressureArray(def.ArchetypeProfile.DefaultPressures);
        if (def != null && def.InitialArchetypePressures != null)
            ApplyPressureArray(def.InitialArchetypePressures);
        ResolveDominantArchetypes();
    }

    private void ApplyPressureArray(OUTL_EgregoreArchetypePressure[] pressures)
    {
        for (int i = 0; i < pressures.Length; i++)
            AddArchetypePressure(pressures[i].Archetype, pressures[i].Pressure);
    }

    private void AddTrace(OUTL_EgregoreTraceType type, OUTL_EntityId source, Vector3 position, float intensity, string key, string tags, OUTL_EgregoreDef def)
    {
        if (type == OUTL_EgregoreTraceType.None) return;
        OUTL_EgregoreArchetypalCycle cycle = def != null ? def.ArchetypalCycle : null;
        int max = cycle != null ? Mathf.Max(4, cycle.MaxMemoryTraces) : 32;
        if (memoryTraces.Count >= max) memoryTraces.RemoveAt(0);

        OUTL_World world = OUTL_World.Instance;
        float time = world != null ? world.WorldTime : Time.time;
        float cellSize = world != null ? world.WorldLedger.ActivityCellSize : Mathf.Max(1f, def != null ? def.InfluenceRadius : 64f);
        OUTL_WorldAddress address = OUTL_WorldAddress.FromWorldPosition(position, cellSize);
        memoryTraces.Add(new OUTL_EgregoreMemoryTrace
        {
            Type = type,
            Source = source,
            Cell = address.ActivityCell,
            Position = position,
            Intensity = Saturate(intensity),
            Time = time,
            DecayRate = cycle != null ? cycle.MemoryDecay : 0.015f,
            Key = key ?? string.Empty,
            Tags = tags ?? string.Empty
        });

        ApplyTraceRules(type, intensity, def);
    }

    private void ApplyTraceRules(OUTL_EgregoreTraceType type, float intensity, OUTL_EgregoreDef def)
    {
        if (def == null || def.ShadowRules == null) return;
        for (int i = 0; i < def.ShadowRules.Length; i++)
        {
            OUTL_EgregoreShadowRule rule = def.ShadowRules[i];
            if (rule == null || rule.TraceType != type) continue;
            AddArchetypePressure(rule.ShadowArchetype, intensity * rule.Pressure);
            TraumaMemory = Saturate(TraumaMemory + rule.Trauma * intensity);
            CorruptionProgress = Saturate(CorruptionProgress + rule.Corruption * intensity);
        }
    }

    private void AddArchetypePressure(OUTL_EgregoreArchetypeId archetype, float amount)
    {
        int index = (int)archetype;
        if (index <= 0 || index >= archetypePressure.Length || amount <= 0f) return;
        archetypePressure[index] = Saturate(archetypePressure[index] + amount);
    }

    private void ApplyQuestHook(OUTL_EgregoreQuestHook hook, bool success, Vector3 position, string key, OUTL_EgregoreDef def)
    {
        if (hook == OUTL_EgregoreQuestHook.None) hook = OUTL_EgregoreQuestHook.CallQuest;
        if (success)
        {
            AddTrace(OUTL_EgregoreTraceType.QuestCompleted, OUTL_EntityId.None, position, 0.75f, "quest_completed:" + key, "quest,boon,integration", def);
            IntegrationProgress = Saturate(IntegrationProgress + 0.24f);
            RenewalProgress = Saturate(RenewalProgress + (hook == OUTL_EgregoreQuestHook.IntegrationQuest || hook == OUTL_EgregoreQuestHook.ReturnQuest ? 0.22f : 0.12f));
            BoonMemory = Saturate(BoonMemory + (hook == OUTL_EgregoreQuestHook.BoonQuest ? 0.35f : 0.15f));
            UnresolvedTension = Mathf.MoveTowards(UnresolvedTension, 0f, 0.18f);
            CorruptionProgress = Mathf.MoveTowards(CorruptionProgress, 0f, 0.16f);
            Corruption = Mathf.MoveTowards(Corruption, 0f, 0.1f);
            Fear = Mathf.MoveTowards(Fear, 0f, 0.08f);
            AddArchetypePressure(OUTL_EgregoreArchetypeId.Hero, 0.18f);
            AddArchetypePressure(OUTL_EgregoreArchetypeId.Sage, 0.12f);
            AddArchetypePressure(OUTL_EgregoreArchetypeId.SelfCenter, 0.25f);
            ApplyIntegrationRules(hook, def);
            if (hook == OUTL_EgregoreQuestHook.BoonQuest) CurrentCyclePhase = OUTL_EgregoreCyclePhase.RevelationOrBoon;
            else if (hook == OUTL_EgregoreQuestHook.ReturnQuest) CurrentCyclePhase = OUTL_EgregoreCyclePhase.Return;
            else if (hook == OUTL_EgregoreQuestHook.IntegrationQuest) CurrentCyclePhase = OUTL_EgregoreCyclePhase.Integration;
        }
        else
        {
            AddTrace(OUTL_EgregoreTraceType.QuestFailed, OUTL_EntityId.None, position, 0.8f, "quest_failed:" + key, "quest,corruption,shadow", def);
            UnresolvedTension = Saturate(UnresolvedTension + 0.22f);
            CorruptionProgress = Saturate(CorruptionProgress + 0.24f);
            TraumaMemory = Saturate(TraumaMemory + 0.12f);
            SacrificeDebt = Saturate(SacrificeDebt + 0.1f);
            IntegrationProgress = Mathf.MoveTowards(IntegrationProgress, 0f, 0.12f);
            AddArchetypePressure(OUTL_EgregoreArchetypeId.Shadow, 0.25f);
            AddArchetypePressure(OUTL_EgregoreArchetypeId.Trickster, 0.14f);
            AddArchetypePressure(OUTL_EgregoreArchetypeId.Devourer, 0.16f);
            if (CorruptionProgress >= (def != null ? def.CollapseThreshold : 0.85f)) CurrentCyclePhase = OUTL_EgregoreCyclePhase.Collapse;
            else CurrentCyclePhase = OUTL_EgregoreCyclePhase.CorruptionLoop;
        }
    }

    private void ApplyIntegrationRules(OUTL_EgregoreQuestHook hook, OUTL_EgregoreDef def)
    {
        if (def == null || def.IntegrationRules == null) return;
        for (int i = 0; i < def.IntegrationRules.Length; i++)
        {
            OUTL_EgregoreIntegrationRule rule = def.IntegrationRules[i];
            if (rule == null || rule.Hook != hook) continue;
            IntegrationProgress = Saturate(IntegrationProgress + rule.Integration);
            RenewalProgress = Saturate(RenewalProgress + rule.Renewal);
            CorruptionProgress = Mathf.MoveTowards(CorruptionProgress, 0f, rule.CorruptionRelief);
            AddArchetypePressure(rule.Archetype, rule.Integration + rule.Renewal);
        }
    }

    private void ApplyKeyedStimulus(OUTL_Stimulus stimulus, float priority, OUTL_EgregoreDef def)
    {
        string key = stimulus.Key ?? string.Empty;
        if (HasKey(key, "ritual"))
        {
            RitualTension = Saturate(RitualTension + priority * 0.35f);
            UnresolvedTension = Saturate(UnresolvedTension + priority * 0.2f);
            AddTrace(OUTL_EgregoreTraceType.Ritual, stimulus.Source, stimulus.Position, priority, "ritual:" + key, "ritual,threshold", def);
            AddArchetypePressure(OUTL_EgregoreArchetypeId.ThresholdGuardian, priority * 0.2f);
            AddArchetypePressure(OUTL_EgregoreArchetypeId.Sage, priority * 0.1f);
        }
        if (HasKey(key, "hunger") || HasKey(key, "famine"))
        {
            HungerPressure = Saturate(HungerPressure + priority * 0.35f);
            ResourcePressure = Saturate(ResourcePressure + priority * 0.2f);
            AddTrace(OUTL_EgregoreTraceType.Hunger, stimulus.Source, stimulus.Position, priority, "hunger:" + key, "hunger,devourer", def);
            AddArchetypePressure(OUTL_EgregoreArchetypeId.Devourer, priority * 0.3f);
        }
        if (HasKey(key, "desire") || HasKey(key, "lover"))
        {
            AttractionPressure = Saturate(AttractionPressure + priority * 0.25f);
            AddTrace(OUTL_EgregoreTraceType.Desire, stimulus.Source, stimulus.Position, priority, "desire:" + key, "desire,lover", def);
            AddArchetypePressure(OUTL_EgregoreArchetypeId.Lover, priority * 0.25f);
        }
        if (HasKey(key, "raid"))
        {
            TraumaMemory = Saturate(TraumaMemory + priority * 0.2f);
            CorruptionProgress = Saturate(CorruptionProgress + priority * 0.15f);
            AddTrace(OUTL_EgregoreTraceType.Raid, stimulus.Source, stimulus.Position, priority, "raid:" + key, "raid,wounded", def);
            AddArchetypePressure(OUTL_EgregoreArchetypeId.WoundedKing, priority * 0.3f);
        }
    }

    private void ApplyTags(string[] tags, OUTL_EntityId source, Vector3 position, float priority, string key, OUTL_EgregoreDef def)
    {
        if (tags == null || tags.Length == 0) return;
        for (int i = 0; i < tags.Length; i++)
        {
            string tag = tags[i];
            if (HasKey(tag, "Ritual")) ApplyKeyedStimulus(new OUTL_Stimulus(OUTL_StimulusType.Scripted, source, position, 0f, priority, 1f, priority, 4f, "ritual:" + key, tags), priority, def);
            else if (HasKey(tag, "Food") || HasKey(tag, "Resource")) RenewalProgress = Saturate(RenewalProgress + priority * 0.05f);
            else if (HasKey(tag, "Forbidden") || HasKey(tag, "Cursed")) CorruptionProgress = Saturate(CorruptionProgress + priority * 0.08f);
        }
    }

    private void DecayArchetypalState(OUTL_EgregoreDef def, float time, float deltaTime)
    {
        OUTL_EgregoreArchetypalCycle cycle = def != null ? def.ArchetypalCycle : null;
        float tensionDecay = (cycle != null ? cycle.TensionDecay : 0.025f) * deltaTime;
        float integrationDecay = (cycle != null ? cycle.IntegrationDecay : 0.01f) * deltaTime;
        UnresolvedTension = Mathf.MoveTowards(UnresolvedTension, 0f, tensionDecay);
        IntegrationProgress = Mathf.MoveTowards(IntegrationProgress, 0f, integrationDecay * 0.5f);
        RenewalProgress = Mathf.MoveTowards(RenewalProgress, 0f, integrationDecay * 0.35f);
        TraumaMemory = Mathf.MoveTowards(TraumaMemory, 0f, (cycle != null ? cycle.MemoryDecay : 0.015f) * deltaTime * 0.25f);
        BoonMemory = Mathf.MoveTowards(BoonMemory, 0f, (cycle != null ? cycle.MemoryDecay : 0.015f) * deltaTime * 0.35f);
        SacrificeDebt = Mathf.MoveTowards(SacrificeDebt, 0f, (cycle != null ? cycle.MemoryDecay : 0.015f) * deltaTime * 0.2f);
        HungerPressure = Mathf.MoveTowards(HungerPressure, 0f, tensionDecay * 0.5f);
        AttractionPressure = Mathf.MoveTowards(AttractionPressure, 0f, tensionDecay * 0.35f);
        RitualTension = Mathf.MoveTowards(RitualTension, 0f, tensionDecay * 0.25f);
        SafetyTrust = Mathf.MoveTowards(SafetyTrust, 0.5f, deltaTime * 0.01f);

        for (int i = 1; i < archetypePressure.Length; i++)
            archetypePressure[i] = Mathf.MoveTowards(archetypePressure[i], 0f, deltaTime * 0.01f);

        int budget = cycle != null ? Mathf.Max(1, cycle.MemoryDecayBudget) : 8;
        for (int i = 0; i < budget && memoryTraces.Count > 0; i++)
        {
            if (memoryDecayCursor >= memoryTraces.Count) memoryDecayCursor = 0;
            OUTL_EgregoreMemoryTrace trace = memoryTraces[memoryDecayCursor];
            trace.Intensity = Mathf.MoveTowards(trace.Intensity, 0f, Mathf.Max(0f, trace.DecayRate) * deltaTime);
            if (!trace.IsAlive(time))
            {
                int last = memoryTraces.Count - 1;
                memoryTraces[memoryDecayCursor] = memoryTraces[last];
                memoryTraces.RemoveAt(last);
                if (memoryDecayCursor >= memoryTraces.Count) memoryDecayCursor = 0;
            }
            else
            {
                memoryTraces[memoryDecayCursor] = trace;
                memoryDecayCursor++;
            }
        }
    }

    private void ResolveArchetypalState(OUTL_EgregoreDef def)
    {
        ResolveDominantArchetypes();
        ThresholdOpen = UnresolvedTension >= (def != null ? def.ThresholdOpenTension : 0.55f)
            || CurrentCyclePhase == OUTL_EgregoreCyclePhase.Threshold
            || CurrentCyclePhase == OUTL_EgregoreCyclePhase.Descent
            || CurrentCyclePhase == OUTL_EgregoreCyclePhase.Trials;

        OUTL_EgregoreCyclePhase next = CurrentCyclePhase;
        if (CorruptionProgress >= (def != null ? def.CollapseThreshold : 0.85f) || Entropy >= 0.9f)
            next = OUTL_EgregoreCyclePhase.Collapse;
        else if (CorruptionProgress >= 0.65f || Corruption >= 0.75f)
            next = OUTL_EgregoreCyclePhase.CorruptionLoop;
        else if (RenewalProgress >= (def != null ? def.RenewalThreshold : 0.7f))
            next = OUTL_EgregoreCyclePhase.Renewal;
        else if (IntegrationProgress >= 0.65f)
            next = OUTL_EgregoreCyclePhase.Integration;
        else if (BoonMemory >= 0.55f)
            next = OUTL_EgregoreCyclePhase.RevelationOrBoon;
        else if (SacrificeDebt >= 0.65f)
            next = OUTL_EgregoreCyclePhase.SacrificeOrDeath;
        else if (UnresolvedTension >= (def != null ? def.CrisisTension : 0.7f) || TraumaMemory >= 0.7f)
            next = OUTL_EgregoreCyclePhase.Crisis;
        else if (GetArchetypePressure(OUTL_EgregoreArchetypeId.Shadow) >= 0.55f || GetArchetypePressure(OUTL_EgregoreArchetypeId.VoidDeathRebirth) >= 0.5f)
            next = OUTL_EgregoreCyclePhase.ShadowConfrontation;
        else if (UnresolvedTension >= 0.55f && ThresholdOpen)
            next = RitualTension >= 0.35f ? OUTL_EgregoreCyclePhase.Threshold : OUTL_EgregoreCyclePhase.Trials;
        else if (UnresolvedTension >= (def != null ? def.ThresholdOpenTension : 0.55f))
            next = OUTL_EgregoreCyclePhase.Threshold;
        else if (UnresolvedTension >= 0.30f)
            next = OUTL_EgregoreCyclePhase.Call;
        else if (Alertness >= 0.25f || Fear >= 0.25f)
            next = OUTL_EgregoreCyclePhase.Disturbance;
        else
            next = OUTL_EgregoreCyclePhase.StableWorld;

        next = ApplyTransformationRules(next, def);
        CurrentCyclePhase = next;
    }

    private OUTL_EgregoreCyclePhase ApplyTransformationRules(OUTL_EgregoreCyclePhase fallback, OUTL_EgregoreDef def)
    {
        if (def == null || def.TransformationRules == null) return fallback;
        for (int i = 0; i < def.TransformationRules.Length; i++)
        {
            OUTL_EgregoreTransformationRule rule = def.TransformationRules[i];
            if (rule == null || rule.From != CurrentCyclePhase) continue;
            if (UnresolvedTension < rule.MinTension || CorruptionProgress < rule.MinCorruption || IntegrationProgress < rule.MinIntegration) continue;
            if (rule.RequiredArchetype != OUTL_EgregoreArchetypeId.None && GetArchetypePressure(rule.RequiredArchetype) <= 0.05f) continue;
            PendingTransformationOutputs = rule.OutputKey;
            return rule.To;
        }
        return fallback;
    }

    private void ResolveDominantArchetypes()
    {
        float dominant = -1f;
        OUTL_EgregoreArchetypeId dominantId = OUTL_EgregoreArchetypeId.SelfCenter;
        for (int i = 1; i < archetypePressure.Length; i++)
        {
            if (archetypePressure[i] <= dominant) continue;
            dominant = archetypePressure[i];
            dominantId = (OUTL_EgregoreArchetypeId)i;
        }
        DominantArchetype = dominantId;

        OUTL_EgregoreArchetypeId shadow = OUTL_EgregoreArchetypeId.Shadow;
        float shadowValue = GetArchetypePressure(OUTL_EgregoreArchetypeId.Shadow);
        PickShadowCandidate(OUTL_EgregoreArchetypeId.Devourer, ref shadow, ref shadowValue);
        PickShadowCandidate(OUTL_EgregoreArchetypeId.WoundedKing, ref shadow, ref shadowValue);
        PickShadowCandidate(OUTL_EgregoreArchetypeId.Beast, ref shadow, ref shadowValue);
        PickShadowCandidate(OUTL_EgregoreArchetypeId.VoidDeathRebirth, ref shadow, ref shadowValue);
        PickShadowCandidate(OUTL_EgregoreArchetypeId.Trickster, ref shadow, ref shadowValue);
        ShadowArchetype = shadow;
    }

    private void PickShadowCandidate(OUTL_EgregoreArchetypeId candidate, ref OUTL_EgregoreArchetypeId current, ref float currentValue)
    {
        float value = GetArchetypePressure(candidate);
        if (value <= currentValue) return;
        current = candidate;
        currentValue = value;
    }

    private void ResolveOutputPressures()
    {
        SpawnPressure = OUTL_EgregoreUtility.SpawnPressureForPhase(CurrentCyclePhase, Hostility, CorruptionProgress, TraumaMemory);
        QuestPressure = OUTL_EgregoreUtility.QuestPressureForPhase(CurrentCyclePhase, UnresolvedTension, IntegrationProgress, RenewalProgress);
        LootPressure = OUTL_EgregoreUtility.LootPressureForPhase(CurrentCyclePhase, BoonMemory, CorruptionProgress);
        BehaviorPressure = OUTL_EgregoreUtility.BehaviorPressureForPhase(CurrentCyclePhase, Fear, Hostility, CorruptionProgress);
    }

    private string BuildCycleKey()
    {
        return "cycle:" + CurrentCyclePhase + ":dominant:" + DominantArchetype + ":shadow:" + ShadowArchetype;
    }

    private static bool HasKey(string value, string token)
    {
        return !string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(token) && value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static OUTL_EgregoreCyclePhase ClampPhase(int value)
    {
        return (OUTL_EgregoreCyclePhase)Mathf.Clamp(value, 0, (int)OUTL_EgregoreCyclePhase.Collapse);
    }

    private static OUTL_EgregoreArchetypeId ClampArchetype(int value)
    {
        return (OUTL_EgregoreArchetypeId)Mathf.Clamp(value, 0, (int)OUTL_EgregoreArchetypeId.VoidDeathRebirth);
    }

    private OUTL_EgregoreSignal MakeSignal(OUTL_EgregoreSignalType type, float intensity, float time, Vector3 position, string key)
    {
        LastEffect = type.ToString();
        return new OUTL_EgregoreSignal
        {
            SourceId = EgregoreId,
            TargetId = string.Empty,
            SignalType = type,
            Intensity = Mathf.Clamp01(intensity),
            Ttl = 6f,
            Position = position,
            Key = key,
            Time = time
        };
    }

    private float ResolveDecay(OUTL_EgregoreDef def, float time)
    {
        if (def.DecayCurve == null || def.DecayCurve.length == 0) return 0.03f;
        return Mathf.Max(0f, def.DecayCurve.Evaluate(time));
    }

    private OUTL_EgregoreMood ResolveMood()
    {
        if (Entropy >= 0.65f) return OUTL_EgregoreMood.Entropic;
        if (Hostility >= 0.65f) return OUTL_EgregoreMood.Hostile;
        if (Fear >= 0.6f) return OUTL_EgregoreMood.Afraid;
        if (Alertness >= 0.45f) return OUTL_EgregoreMood.Alert;
        if (Corruption >= 0.55f) return OUTL_EgregoreMood.Corrupt;
        if (Prosperity >= 0.65f) return OUTL_EgregoreMood.Prosperous;
        return OUTL_EgregoreMood.Stable;
    }

    private static float Saturate(float value)
    {
        return Mathf.Clamp01(value);
    }
}
