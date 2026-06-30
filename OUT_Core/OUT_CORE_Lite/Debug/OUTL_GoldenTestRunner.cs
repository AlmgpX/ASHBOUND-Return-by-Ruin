using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_GoldenTestRunner : MonoBehaviour
{
    public bool RunOnStart;
    public bool LogPassed = true;
    public bool CreateTestEntitiesIfMissing = true;
    public int TestEntityCount = 16;
    public float EntitySpacing = 32f;
    public int StressPoolCount = 1000;
    public OUTL_ChunkProcessingDriver ChunkDriver;

    private readonly List<string> failures = new List<string>(32);
    private readonly List<OUTL_EntityRuntime> entities = new List<OUTL_EntityRuntime>(256);
    private readonly CombatEventProbe damagedProbe = new CombatEventProbe();
    private readonly CombatEventProbe killProbe = new CombatEventProbe();
    private readonly List<GameObject> pooledStress = new List<GameObject>(1024);
    private OUTL_AttackProfile goldenMelee;
    private OUTL_AttackProfile goldenProjectile;
    private GameObject goldenPoolPrefab;
    private GameObject goldenManagedPrefab;
    private GameObject goldenProjectilePrefab;

    private void Start()
    {
        if (RunOnStart) RunAll();
    }

    [ContextMenu("OUT Run Golden Tests")]
    public void RunAll()
    {
        failures.Clear();
        OUTL_World world = OUTL_World.Instance;
        if (world == null) Fail("OUTL_World.Instance missing");
        if (world == null) { Report(); return; }

        if (CreateTestEntitiesIfMissing) EnsureTestEntities();
        if (ChunkDriver == null) ChunkDriver = FindObjectOfType<OUTL_ChunkProcessingDriver>();
        if (ChunkDriver != null) ChunkDriver.ProcessAllNow();

        TestRegistry(world);
        TestTargetNameDispatch(world);
        TestSaveCapture(world);
        TestChunkProcessing(world);
        TestChunkTierWrites(world);
        Pool_Facade_SpawnRelease_ReusesInstance(world);
        Pool_DoubleRelease_DoesNotDuplicate(world);
        Pool_RigidbodyReset_Works(world);
        Pool_EntityAdapter_RegistersAndUnregisters(world);
        TestAITargetAcquisition(world);
        TestHearingStimulus(world);
        TestStimulusBudget(world);
        Stimulus_Store_EmitQueryDecay(world);
        AI_ReceivesStimulus_FromStore(world);
        SectorGrid_RegisterMoveUnregister(world);
        Egregore_AggregatesStimuli(world);
        Egregore_SendsSignal(world);
        TickProfile_AppliesIntervals(world);
        TestProjectileReuse(world);
        TestDeadState(world);
        TestNPCMeleeDamageAndFrag(world);
        TestPlayerStackIfPresent();
        Report();
    }

    private void EnsureTestEntities()
    {
        if (GameObject.Find("OUTL_Golden_Entity_00") != null) return;
        for (int i = 0; i < Mathf.Max(1, TestEntityCount); i++)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "OUTL_Golden_Entity_" + i.ToString("00");
            go.transform.position = new Vector3((i - TestEntityCount / 2) * EntitySpacing, 0.5f, (i % 4) * EntitySpacing);
            OUTL_EntityAdapter e = go.AddComponent<OUTL_EntityAdapter>();
            e.ClassNameOverride = "golden_test_entity";
            e.TargetName = "golden_entity_" + i.ToString("00");
            e.StableId = e.TargetName;
            e.SavePersistent = true;
            e.RegisterInSectors = true;
            e.TickLane = OUTL_TickLane.Logic;
            e.TickInterval = 0.25f;
            OUTL_DamageReceiver dr = go.AddComponent<OUTL_DamageReceiver>();
            dr.Entity = e;
            OUTL_Vitals vitals = go.AddComponent<OUTL_Vitals>();
            vitals.Entity = e;
            vitals.InitializeMissingStats = true;
            vitals.DefaultHealth = 100f;
            vitals.DefaultMaxHealth = 100f;
            OUTL_DeathHandler death = go.AddComponent<OUTL_DeathHandler>();
            death.Entity = e;
            death.QueueDespawn = false;
        }
    }

    private void TestRegistry(OUTL_World world)
    {
        world.Registry.CopyAll(entities);
        if (entities.Count == 0) Fail("Registry has no entities");
        bool hasStable = false;
        for (int i = 0; i < entities.Count; i++)
            if (entities[i] != null && !string.IsNullOrEmpty(entities[i].StableId)) { hasStable = true; break; }
        if (!hasStable) Fail("No entity with StableId found");
    }

    private void TestTargetNameDispatch(OUTL_World world)
    {
        OUTL_EntityRuntime target = null;
        world.Registry.CopyAll(entities);
        for (int i = 0; i < entities.Count; i++)
            if (entities[i] != null && !string.IsNullOrEmpty(entities[i].TargetName) && entities[i].Adapter != null && entities[i].Adapter.GetComponent<OUTL_DamageReceiver>() != null) { target = entities[i]; break; }
        if (target == null) { Fail("No damage receiver target for command dispatch"); return; }
        float before = target.Stats.Get(OUTL_StatId.Health, 100f);
        if (before <= 0f) target.Stats.Set(OUTL_StatId.Health, 100f);
        int receivers = world.Commands.SendToTargetName(target.TargetName, new OUTL_Command(OUTL_CommandType.Damage, OUTL_EntityId.None, OUTL_EntityId.None) { FloatValue = 1f, Key = "golden" });
        if (receivers <= 0) Fail("Command dispatch to TargetName returned 0 receivers: " + target.TargetName);
    }

    private void TestSaveCapture(OUTL_World world)
    {
        OUTL_WorldSaveFile file = world.Save.Capture();
        if (file == null) Fail("Save capture returned null");
        else if (file.Entities == null || file.Entities.Count == 0) Fail("Save capture has no entities");
    }

    private void TestChunkProcessing(OUTL_World world)
    {
        if (ChunkDriver == null) { Fail("ChunkDriver missing"); return; }
        world.Registry.CopyAll(entities);
        bool foundChunkState = false;
        bool foundTier = false;
        for (int i = 0; i < entities.Count; i++)
        {
            OUTL_EntityRuntime e = entities[i];
            if (e == null) continue;
            if (e.State.GetString("Chunk.Tier", string.Empty) != string.Empty) foundChunkState = true;
            if (e.Tier != OUTL_RuntimeTier.Dormant) foundTier = true;
        }
        if (!foundChunkState) Fail("Chunk processing did not write Chunk.Tier state");
        if (!foundTier) Fail("Chunk processing did not assign any active tier");
    }

    private void TestChunkTierWrites(OUTL_World world)
    {
        if (ChunkDriver == null) { Fail("Chunk tier write test missing ChunkDriver"); return; }
        ChunkDriver.ProcessAllNow();
        world.Registry.CopyAll(entities);
        int checkedEntities = 0;
        int tierStateRows = 0;
        for (int i = 0; i < entities.Count; i++)
        {
            OUTL_EntityRuntime entity = entities[i];
            if (entity == null || entity.Adapter == null) continue;
            checkedEntities++;
            if (!string.IsNullOrEmpty(entity.State.GetString("Chunk.Tier", string.Empty))) tierStateRows++;
        }

        if (checkedEntities == 0) Fail("Chunk tier write test had no registered entities");
        if (tierStateRows == 0) Fail("Chunk tier write test found no Chunk.Tier rows after ProcessAllNow");
    }

    private void TestPoolReuseAndFacade(OUTL_World world)
    {
        OUTL_PoolSystem pool = EnsurePoolSystem(world);
        if (pool == null) { Fail("Pool reuse test missing OUTL_PoolSystem"); return; }

        GameObject prefab = GetGoldenPoolPrefab();
        OutCore.pool.OUT.Prewarm(prefab, 1);
        GameObject first = OutCore.pool.OUT.Instantiate(prefab, new Vector3(12f, 0.5f, 0f), Quaternion.identity);
        if (first == null) { Fail("OutCore.pool.OUT.Instantiate returned null for pool prefab"); return; }

        OUTL_GoldenPoolProbe probe = first.GetComponent<OUTL_GoldenPoolProbe>();
        Rigidbody body = first.GetComponent<Rigidbody>();
        if (body != null) body.velocity = new Vector3(7f, 0f, 0f);

        OutCore.pool.OUT.Destroy(first);
        GameObject second = OutCore.pool.OUT.Instantiate(prefab, new Vector3(14f, 0.5f, 0f), Quaternion.identity);
        if (second == null) { Fail("OutCore.pool.OUT.Instantiate returned null on pooled reuse"); return; }
        if (!object.ReferenceEquals(first, second)) Fail("Pool did not reuse the same GameObject instance");
        if (!OutCore.pool.OUT.IsManaged(second)) Fail("OutCore.pool.OUT.IsManaged did not recognize a pooled foreign prefab");

        probe = second.GetComponent<OUTL_GoldenPoolProbe>();
        if (probe == null) Fail("Pool probe missing after reuse");
        else
        {
            if (probe.SpawnCount < 2) Fail("Pool probe did not receive spawn reset twice");
            if (probe.ReleaseCount < 1) Fail("Pool probe did not receive release reset");
        }

        body = second.GetComponent<Rigidbody>();
        if (body != null && body.velocity.sqrMagnitude > 0.001f) Fail("Pool reset did not clear rigidbody velocity");
        OutCore.pool.OUT.Destroy(second);

        GameObject managedPrefab = GetGoldenManagedPrefab();
        GameObject managed = OutCore.pool.OUT.Instantiate(managedPrefab, new Vector3(16f, 0.5f, 0f), Quaternion.identity);
        if (managed == null) { Fail("Managed facade spawn returned null"); return; }
        OUTL_EntityAdapter adapter = managed.GetComponent<OUTL_EntityAdapter>();
        if (adapter == null || adapter.Runtime == null) Fail("Managed facade spawn did not register OUTL_EntityAdapter through OUTL_World");
        OUTL_EntityId id = adapter != null ? adapter.Id : OUTL_EntityId.None;
        OUTL_EntityRuntime runtime;
        if (!id.IsValid || !world.Registry.TryGet(id, out runtime)) Fail("Managed facade spawn is not visible in OUTL_World.Registry");
        OutCore.pool.OUT.Destroy(managed);
        if (id.IsValid && world.Registry.TryGet(id, out runtime)) Fail("Managed facade destroy did not unregister entity from OUTL_World.Registry");
        if (managed != null && managed.activeSelf) Fail("Managed facade destroy did not return object to pool/inactive state");
    }

    private void TestPoolStatsAndDoubleRelease(OUTL_World world)
    {
        OUTL_PoolSystem pool = EnsurePoolSystem(world);
        if (pool == null) { Fail("Pool stats test missing OUTL_PoolSystem"); return; }

        GameObject prefab = GetGoldenPoolPrefab();
        int count = Mathf.Clamp(StressPoolCount, 1, 1000);
        OutCore.pool.OUT.Prewarm(prefab, count);
        pooledStress.Clear();
        for (int i = 0; i < count; i++)
        {
            GameObject go = OutCore.pool.OUT.Instantiate(prefab, new Vector3(40f + i * 0.05f, 0.5f, 0f), Quaternion.identity);
            if (go != null) pooledStress.Add(go);
        }

        for (int i = 0; i < pooledStress.Count; i++)
            OutCore.pool.OUT.Destroy(pooledStress[i]);

        if (pooledStress.Count > 0) OutCore.pool.OUT.Destroy(pooledStress[0]);

        OUTL_PoolStats stats;
        if (!OutCore.pool.OUT.TryGetPoolStats(prefab, out stats)) Fail("Pool stats unavailable for golden prefab");
        else
        {
            if (stats.TotalCreated <= 0) Fail("Pool stats TotalCreated did not increase");
            if (stats.ActiveCount != 0) Fail("Pool stats ActiveCount should be zero after stress release, got " + stats.ActiveCount);
            if (stats.DoubleReleaseWarnings <= 0) Fail("Pool double release guard did not record warning");
        }

        pooledStress.Clear();
    }

    private void Pool_Facade_SpawnRelease_ReusesInstance(OUTL_World world)
    {
        TestPoolReuseAndFacade(world);
    }

    private void Pool_DoubleRelease_DoesNotDuplicate(OUTL_World world)
    {
        TestPoolStatsAndDoubleRelease(world);
    }

    private void Pool_RigidbodyReset_Works(OUTL_World world)
    {
        OUTL_PoolSystem pool = EnsurePoolSystem(world);
        if (pool == null) { Fail("Pool_RigidbodyReset_Works missing OUTL_PoolSystem"); return; }
        GameObject prefab = GetGoldenPoolPrefab();
        GameObject first = OutCore.pool.OUT.Instantiate(prefab, new Vector3(52f, 0.5f, 0f), Quaternion.identity);
        if (first == null) { Fail("Pool_RigidbodyReset_Works spawn returned null"); return; }
        Rigidbody body = first.GetComponent<Rigidbody>();
        if (body == null) { Fail("Pool_RigidbodyReset_Works prefab has no Rigidbody"); OutCore.pool.OUT.Destroy(first); return; }
        body.velocity = new Vector3(11f, 0f, 0f);
        body.angularVelocity = new Vector3(0f, 9f, 0f);
        OutCore.pool.OUT.Destroy(first);

        GameObject second = OutCore.pool.OUT.Instantiate(prefab, new Vector3(54f, 0.5f, 0f), Quaternion.identity);
        if (second == null) { Fail("Pool_RigidbodyReset_Works reuse spawn returned null"); return; }
        Rigidbody reused = second.GetComponent<Rigidbody>();
        if (reused != null && (reused.velocity.sqrMagnitude > 0.001f || reused.angularVelocity.sqrMagnitude > 0.001f))
            Fail("Pool_RigidbodyReset_Works did not clear Rigidbody velocity/angularVelocity");
        OutCore.pool.OUT.Destroy(second);
    }

    private void Pool_EntityAdapter_RegistersAndUnregisters(OUTL_World world)
    {
        GameObject managedPrefab = GetGoldenManagedPrefab();
        GameObject managed = OutCore.pool.OUT.Instantiate(managedPrefab, new Vector3(56f, 0.5f, 0f), Quaternion.identity);
        if (managed == null) { Fail("Pool_EntityAdapter_RegistersAndUnregisters spawn returned null"); return; }
        OUTL_EntityAdapter adapter = managed.GetComponent<OUTL_EntityAdapter>();
        OUTL_EntityId id = adapter != null ? adapter.Id : OUTL_EntityId.None;
        OUTL_EntityRuntime runtime;
        if (adapter == null || !id.IsValid || !world.Registry.TryGet(id, out runtime))
            Fail("Pool_EntityAdapter_RegistersAndUnregisters did not register spawned adapter");
        OutCore.pool.OUT.Destroy(managed);
        if (id.IsValid && world.Registry.TryGet(id, out runtime))
            Fail("Pool_EntityAdapter_RegistersAndUnregisters did not unregister adapter on destroy");
    }

    private void TestAITargetAcquisition(OUTL_World world)
    {
        OUTL_EntityRuntime actor = EnsureCombatTestEntity(world, "OUTL_Golden_AI_Acquirer", new Vector3(20f, 0.5f, 0f));
        OUTL_EntityRuntime target = EnsureCombatTestEntity(world, "OUTL_Golden_AI_Target", new Vector3(23f, 0.5f, 0f));
        if (actor == null || target == null) { Fail("AI target acquisition could not create entities"); return; }

        EnsureDef(actor.Adapter, "golden_ai_actor", new[] { "Actor", "golden_ai_actor" });
        EnsureDef(target.Adapter, "golden_ai_target", new[] { "Actor", "Damageable", "golden_ai_enemy" });
        actor.Adapter.RebindRuntime(world);
        target.Adapter.RebindRuntime(world);

        OUTL_AIProfile profile = ScriptableObject.CreateInstance<OUTL_AIProfile>();
        profile.ProfileId = "golden.ai.acquire";
        profile.UseFactionHostility = false;
        profile.EnemyTags = new[] { "golden_ai_enemy" };
        profile.ViewDistance = 30f;

        OUTL_AIActor ai = actor.Adapter.GetComponent<OUTL_AIActor>();
        if (ai == null) ai = actor.Adapter.gameObject.AddComponent<OUTL_AIActor>();
        ai.Entity = actor.Adapter;
        ai.Profile = profile;
        ai.RequireLineOfSightToAcquireTarget = false;

        OUTL_EntityRuntime found = OUTL_AIPerceptionUtility.FindTarget(world, actor, actor.Adapter.transform, profile, profile.EnemyTags, false, 1.4f, 1.0f, ~0);
        if (found == null || found.Id != target.Id) Fail("AI target acquisition did not find tagged enemy through sectors");
    }

    private void TestHearingStimulus(OUTL_World world)
    {
        OUTL_EntityRuntime listener = EnsureCombatTestEntity(world, "OUTL_Golden_Hearing_Listener", new Vector3(28f, 0.5f, 0f));
        if (listener == null) { Fail("Hearing stimulus could not create listener"); return; }

        OUTL_AIActor ai = listener.Adapter.GetComponent<OUTL_AIActor>();
        if (ai == null) ai = listener.Adapter.gameObject.AddComponent<OUTL_AIActor>();
        ai.Entity = listener.Adapter;
        ai.UseStimulusInterrupts = true;

        OUTL_HearingSensor hearing = listener.Adapter.GetComponent<OUTL_HearingSensor>();
        if (hearing == null) hearing = listener.Adapter.gameObject.AddComponent<OUTL_HearingSensor>();
        hearing.Actor = ai;
        hearing.Entity = listener.Adapter;
        hearing.Enabled = true;
        hearing.UseOcclusionRaycast = false;
        hearing.MinPriority = 0.01f;
        hearing.HearingMultiplier = 1f;
        hearing.enabled = false;
        hearing.enabled = true;

        OUTL_StimulusBus.EmitSound(OUTL_EntityId.None, listener.Adapter.transform.position + Vector3.right, 10f, 1f, 1f, "golden.hearing");
        if (ai.LastStimulusType != OUTL_StimulusType.HeardNoise) Fail("Hearing stimulus did not interrupt AI actor with HeardNoise");
        if (hearing.LastHeardPriority <= 0f) Fail("Hearing sensor did not record audible priority");
    }

    private void TestStimulusBudget(OUTL_World world)
    {
        OUTL_StimulusBus.Clear();
        int count = 1000;
        for (int i = 0; i < count; i++)
            OUTL_StimulusBus.Emit(new OUTL_Stimulus(OUTL_StimulusType.HeardNoise, OUTL_EntityId.None, new Vector3(i % 32, 0f, i / 32), 12f, 1f, 1f, 1f, 0.1f, "golden.stimulus", null));

        int stored = OUTL_StimulusBus.StoredCount;
        int processed = OUTL_StimulusBus.Tick(world.WorldTime + 10f, count);
        if (stored <= 0) Fail("Stimulus budget test stored no stimuli");
        if (processed <= 0) Fail("Stimulus budget test processed no stimuli");
        if (OUTL_StimulusBus.StoredCount >= stored) Fail("Stimulus budget cleanup did not remove expired stimuli");
    }

    private void Stimulus_Store_EmitQueryDecay(OUTL_World world)
    {
        OUTL_StimulusBus.Clear();
        Vector3 position = new Vector3(60f, 0f, 0f);
        OUTL_StimulusBus.Emit(new OUTL_Stimulus(OUTL_StimulusType.Alert, OUTL_EntityId.None, position, 12f, 1f, 1f, 1f, 0.05f, "golden.store.alert", null));

        List<OUTL_Stimulus> buffer = new List<OUTL_Stimulus>(4);
        OUTL_StimulusQuery query = new OUTL_StimulusQuery
        {
            Position = position,
            Radius = 12f,
            Type = OUTL_StimulusType.Alert,
            MinPriority = 0.1f,
            MaxCount = 4
        };
        int found = OUTL_StimulusBus.Query(query, buffer);
        if (found <= 0) Fail("Stimulus_Store_EmitQueryDecay query did not find emitted Alert stimulus");
        OUTL_StimulusBus.Tick(world.WorldTime + 10f, 32);
        buffer.Clear();
        found = OUTL_StimulusBus.Query(query, buffer);
        if (found != 0) Fail("Stimulus_Store_EmitQueryDecay decay did not expire stored stimulus");
    }

    private void AI_ReceivesStimulus_FromStore(OUTL_World world)
    {
        OUTL_EntityRuntime listener = EnsureCombatTestEntity(world, "OUTL_Golden_StoreStimulus_AI", new Vector3(64f, 0.5f, 0f));
        if (listener == null) { Fail("AI_ReceivesStimulus_FromStore could not create listener"); return; }

        OUTL_AIActor ai = listener.Adapter.GetComponent<OUTL_AIActor>();
        if (ai == null) ai = listener.Adapter.gameObject.AddComponent<OUTL_AIActor>();
        ai.Entity = listener.Adapter;
        ai.Profile = ScriptableObject.CreateInstance<OUTL_AIProfile>();
        ai.Profile.ProfileId = "golden.ai.store";
        ai.PerceptionProfile = ScriptableObject.CreateInstance<OUTL_AIPerceptionProfile>();
        ai.PerceptionProfile.HearingRadius = 16f;
        ai.StimulusPriorityThreshold = 0.01f;

        OUTL_StimulusSensor sensor = listener.Adapter.GetComponent<OUTL_StimulusSensor>();
        if (sensor == null) sensor = listener.Adapter.gameObject.AddComponent<OUTL_StimulusSensor>();
        sensor.Actor = ai;
        sensor.Entity = listener.Adapter;
        sensor.Mode = OUTL_StimulusSensorMode.Hearing;
        sensor.Radius = 16f;
        sensor.MinPriority = 0.01f;
        sensor.IgnoreSelf = true;

        OUTL_StimulusBus.Clear();
        OUTL_StimulusBus.EmitSound(OUTL_EntityId.None, listener.Adapter.transform.position + Vector3.right, 8f, 1f, 1f, "golden.store.heard");
        sensor.OUTL_Tick(world, world.WorldTime, 0.1f);
        if (ai.LastStimulusType != OUTL_StimulusType.HeardNoise)
            Fail("AI_ReceivesStimulus_FromStore did not deliver HeardNoise from StimulusStore");
    }

    private void SectorGrid_RegisterMoveUnregister(OUTL_World world)
    {
        OUTL_EntityRuntime entity = EnsureCombatTestEntity(world, "OUTL_Golden_Sector_Move", new Vector3(70f, 0.5f, 0f));
        if (entity == null) { Fail("SectorGrid_RegisterMoveUnregister could not create entity"); return; }

        world.Sectors.RegisterOrUpdate(entity);
        long firstCell;
        if (!world.Sectors.TryGetEntityCell(entity.Id, out firstCell)) { Fail("SectorGrid_RegisterMoveUnregister did not register entity"); return; }
        entity.Adapter.transform.position += new Vector3(256f, 0f, 0f);
        world.Sectors.RegisterOrUpdate(entity);
        long secondCell;
        if (!world.Sectors.TryGetEntityCell(entity.Id, out secondCell)) { Fail("SectorGrid_RegisterMoveUnregister lost entity after move"); return; }
        if (firstCell == secondCell) Fail("SectorGrid_RegisterMoveUnregister did not move entity to a new sector cell");
        world.Sectors.Unregister(entity.Id);
        if (world.Sectors.Contains(entity.Id)) Fail("SectorGrid_RegisterMoveUnregister did not unregister entity");
        world.Sectors.RegisterOrUpdate(entity);
    }

    private void TestEgregoreRuntime(OUTL_World world)
    {
        Type defType = FindRuntimeType("OUTL_EgregoreDef");
        Type componentType = FindRuntimeType("OUTL_EgregoreComponent");
        if (defType == null || componentType == null)
        {
            Fail("Egregore runtime types were not found. Unity project files may need regeneration/import.");
            return;
        }

        ScriptableObject def = ScriptableObject.CreateInstance(defType);
        SetField(def, "EgregoreId", "golden_egregore");
        SetField(def, "AlertThreshold", 0.05f);
        GameObject go = new GameObject("OUTL_Golden_Egregore");
        Component component = go.AddComponent(componentType);
        SetField(component, "Def", def);
        Behaviour behaviour = component as Behaviour;
        if (behaviour != null)
        {
            behaviour.enabled = false;
            behaviour.enabled = true;
        }

        OUTL_StimulusBus.Emit(new OUTL_Stimulus(OUTL_StimulusType.Combat, OUTL_EntityId.None, go.transform.position, 32f, 1f, 1f, 1f, 2f, "golden.egregore.combat", null));
        System.Reflection.MethodInfo tick = componentType.GetMethod("OUTL_Tick");
        if (tick != null) tick.Invoke(component, new object[] { world, world.WorldTime, 1f });
        object runtime = GetProperty(component, "Runtime");
        if (runtime == null) Fail("Egregore runtime missing");
        else
        {
            float violence = GetFloatField(runtime, "Violence");
            float alertness = GetFloatField(runtime, "Alertness");
            if (violence <= 0f && alertness <= 0f) Fail("Egregore did not react to combat stimulus");
        }
        Destroy(go);
    }

    private void Egregore_AggregatesStimuli(OUTL_World world)
    {
        OUTL_EgregoreDef def = ScriptableObject.CreateInstance<OUTL_EgregoreDef>();
        def.EgregoreId = "golden_egregore_aggregate";
        def.AlertThreshold = 0.95f;
        GameObject go = new GameObject("OUTL_Golden_Egregore_Aggregate");
        OUTL_EgregoreComponent component = go.AddComponent<OUTL_EgregoreComponent>();
        component.Def = def;
        component.enabled = false;
        component.enabled = true;
        OUTL_StimulusBus.Emit(new OUTL_Stimulus(OUTL_StimulusType.Combat, OUTL_EntityId.None, go.transform.position, 32f, 1f, 1f, 1f, 2f, "golden.egregore.aggregate", null));
        component.OUTL_Tick(world, world.WorldTime, 1f);
        if (component.Runtime.Violence <= 0f && component.Runtime.Alertness <= 0f)
            Fail("Egregore_AggregatesStimuli did not aggregate combat stimulus");
        Destroy(go);
    }

    private void Egregore_SendsSignal(OUTL_World world)
    {
        OUTL_EgregoreDef def = ScriptableObject.CreateInstance<OUTL_EgregoreDef>();
        def.EgregoreId = "golden_egregore_signal";
        def.AlertThreshold = 0.01f;
        def.InfluenceRadius = 32f;
        GameObject go = new GameObject("OUTL_Golden_Egregore_Signal");
        OUTL_EgregoreComponent component = go.AddComponent<OUTL_EgregoreComponent>();
        component.Def = def;
        component.enabled = false;
        component.enabled = true;
        OUTL_StimulusBus.Clear();
        OUTL_StimulusBus.EmitCombat(OUTL_EntityId.None, go.transform.position, 16f, 1f, 1f, "golden.egregore.signal");
        component.OUTL_Tick(world, world.WorldTime, 1f);
        if (component.Runtime.SignalCount <= 0 && OUTL_StimulusBus.StoredCount <= 0)
            Fail("Egregore_SendsSignal did not broadcast signal or ambient egregore stimulus");
        Destroy(go);
    }

    private void TickProfile_AppliesIntervals(OUTL_World world)
    {
        float oldLogic = world.LogicTickInterval;
        float oldAI = world.AITickInterval;
        float oldQuest = world.QuestTickInterval;
        float oldCustom = world.CustomTickInterval;
        float oldStimulus = world.StimulusTickInterval;
        int oldStimBudget = world.MaxStimuliProcessedPerFrame;
        int oldSectorBudget = world.MaxSectorUpdatesPerFrame;
        int oldEgregoreBudget = world.MaxEgregoreSignalsPerFrame;

        OUTL_TickProfile profile = ScriptableObject.CreateInstance<OUTL_TickProfile>();
        profile.logicInterval = 0.11f;
        profile.aiNearInterval = 0.12f;
        profile.questInterval = 0.53f;
        profile.chunkProcessingInterval = 0.31f;
        profile.stimulusInterval = 0.27f;
        profile.maxStimuliProcessedPerFrame = 123;
        profile.maxSectorUpdatesPerFrame = 45;
        profile.maxEgregoreSignalsPerFrame = 17;
        world.ApplyTickProfile(profile);

        if (Mathf.Abs(world.LogicTickInterval - 0.11f) > 0.0001f) Fail("TickProfile_AppliesIntervals did not apply logic interval");
        if (Mathf.Abs(world.AITickInterval - 0.12f) > 0.0001f) Fail("TickProfile_AppliesIntervals did not apply AI near interval");
        if (Mathf.Abs(world.QuestTickInterval - 0.53f) > 0.0001f) Fail("TickProfile_AppliesIntervals did not apply quest interval");
        if (Mathf.Abs(world.CustomTickInterval - 0.31f) > 0.0001f) Fail("TickProfile_AppliesIntervals did not apply chunk/custom interval");
        if (Mathf.Abs(world.StimulusTickInterval - 0.27f) > 0.0001f) Fail("TickProfile_AppliesIntervals did not apply stimulus interval");
        if (world.MaxStimuliProcessedPerFrame != 123 || world.MaxSectorUpdatesPerFrame != 45 || world.MaxEgregoreSignalsPerFrame != 17)
            Fail("TickProfile_AppliesIntervals did not apply budgets");

        world.LogicTickInterval = oldLogic;
        world.AITickInterval = oldAI;
        world.QuestTickInterval = oldQuest;
        world.CustomTickInterval = oldCustom;
        world.StimulusTickInterval = oldStimulus;
        world.MaxStimuliProcessedPerFrame = oldStimBudget;
        world.MaxSectorUpdatesPerFrame = oldSectorBudget;
        world.MaxEgregoreSignalsPerFrame = oldEgregoreBudget;
    }

    private void TestProjectileReuse(OUTL_World world)
    {
        GameObject prefab = GetGoldenProjectilePrefab();
        OUTL_AttackProfile profile = GetGoldenProjectileProfile(prefab);
        GameObject first = OutCore.pool.OUT.Instantiate(prefab, new Vector3(34f, 0.5f, 0f), Quaternion.identity);
        if (first == null) { Fail("Projectile pool spawn returned null"); return; }

        OUTL_Projectile projectile = first.GetComponent<OUTL_Projectile>();
        if (projectile == null) { Fail("Projectile prefab missing OUTL_Projectile in golden test"); return; }
        projectile.Launch(OUTL_EntityId.None, profile, Vector3.forward);
        OutCore.pool.OUT.Destroy(first);

        GameObject second = OutCore.pool.OUT.Instantiate(prefab, new Vector3(36f, 0.5f, 0f), Quaternion.identity);
        if (second == null) { Fail("Projectile pool reuse spawn returned null"); return; }
        if (!object.ReferenceEquals(first, second)) Fail("Projectile pool did not reuse the same GameObject instance");

        OUTL_Projectile reused = second.GetComponent<OUTL_Projectile>();
        if (reused == null) Fail("Reused projectile missing OUTL_Projectile");
        else
        {
            if (reused.Source.IsValid) Fail("Projectile pool reset did not clear Source");
            if (reused.Profile != null) Fail("Projectile pool reset did not clear Profile");
            if (reused.Velocity.sqrMagnitude > 0.001f) Fail("Projectile pool reset did not clear Velocity");
        }

        Rigidbody body = second.GetComponent<Rigidbody>();
        if (body != null && body.velocity.sqrMagnitude > 0.001f) Fail("Projectile pool reset did not clear rigidbody velocity");
        OutCore.pool.OUT.Destroy(second);
    }

    private void TestDeadState(OUTL_World world)
    {
        OUTL_EntityRuntime target = null;
        world.Registry.CopyAll(entities);
        for (int i = 0; i < entities.Count; i++)
            if (entities[i] != null && entities[i].Adapter != null && entities[i].Adapter.GetComponent<OUTL_Vitals>() != null) { target = entities[i]; break; }
        if (target == null) { Fail("No vitals entity for death test"); return; }
        target.Stats.Set(OUTL_StatId.Health, 0f);
        OUTL_Vitals v = target.Adapter.GetComponent<OUTL_Vitals>();
        v.EvaluateNow();
        if (!target.State.GetFlag(OUTL_StateId.Dead)) Fail("Health <= 0 did not set Dead state");
    }

    private void TestNPCMeleeDamageAndFrag(OUTL_World world)
    {
        OUTL_EntityRuntime attacker = EnsureCombatTestEntity(world, "OUTL_Golden_Melee_Attacker", new Vector3(-1.1f, 0.5f, 0f));
        OUTL_EntityRuntime target = EnsureCombatTestEntity(world, "OUTL_Golden_Melee_Target", new Vector3(0.25f, 0.5f, 0f));
        if (attacker == null || target == null) { Fail("Could not create melee combat golden entities"); return; }

        target.State.SetFlag(OUTL_StateId.Dead, false);
        target.Stats.Set(OUTL_StatId.Health, 100f);
        target.Stats.Set("MaxHealth", 100f);
        attacker.State.SetFlag(OUTL_StateId.Dead, false);
        attacker.Stats.Set(OUTL_StatId.Health, 100f);
        OUTL_Vitals targetVitals = target.Adapter.GetComponent<OUTL_Vitals>();
        if (targetVitals != null) targetVitals.EvaluateNow();
        OUTL_Vitals attackerVitals = attacker.Adapter.GetComponent<OUTL_Vitals>();
        if (attackerVitals != null) attackerVitals.EvaluateNow();
        OUTL_DeathHandler targetDeath = target.Adapter.GetComponent<OUTL_DeathHandler>();
        if (targetDeath != null)
        {
            targetDeath.enabled = false;
            targetDeath.enabled = true;
            targetDeath.Entity = target.Adapter;
            targetDeath.QueueDespawn = false;
            targetDeath.DisableAI = false;
            targetDeath.DisableColliders = true;
            targetDeath.DisableRenderers = false;
        }
        Collider targetCollider = target.Adapter.GetComponentInChildren<Collider>(true);
        if (targetCollider != null) targetCollider.enabled = true;

        OUTL_AttackDriver driver = attacker.Adapter.GetComponent<OUTL_AttackDriver>();
        if (driver == null) driver = attacker.Adapter.gameObject.AddComponent<OUTL_AttackDriver>();
        driver.Source = attacker.Adapter;
        driver.Muzzle = EnsureChild(attacker.Adapter.transform, "Muzzle", new Vector3(0f, 0.85f, 0.55f));
        driver.Primary = GetGoldenMeleeProfile();
        driver.Melee = driver.Primary;
        driver.SmartMeleeWhenFireAtPrimary = true;
        driver.RespectCooldownOnFireAt = true;

        damagedProbe.Reset(OUTL_EventType.Damaged, attacker.Id, target.Id);
        world.Events.Register(damagedProbe, OUTL_EventType.Damaged);
        float before = target.Stats.Get(OUTL_StatId.Health, 0f);
        bool fired = driver.FireAt(driver.Primary, target.Adapter.transform.position + Vector3.up);
        world.Events.Flush();
        world.Events.Unregister(damagedProbe);
        float after = target.Stats.Get(OUTL_StatId.Health, 0f);
        if (!fired) Fail("NPC melee golden attack did not fire");
        if (after >= before) Fail("NPC melee golden attack did not reduce health");
        if (damagedProbe.Count != 1) Fail("Expected exactly one Damaged event for actor melee, got " + damagedProbe.Count);
        if (damagedProbe.Key != "golden.melee") Fail("Damaged event key should preserve base hit damage key");

        killProbe.Reset(OUTL_EventType.Killed, attacker.Id, target.Id);
        world.Events.Register(killProbe, OUTL_EventType.Killed);
        target.State.SetFlag(OUTL_StateId.Dead, false);
        target.Stats.Set(OUTL_StatId.Health, 20f);
        OUTL_Combat.ApplyDamage(attacker.Id, target.Id, 40f, target.Adapter.transform.position, "golden.frag");
        world.Events.Flush();
        world.Events.Unregister(killProbe);
        if (killProbe.Count != 1) Fail("Expected exactly one Killed event for combat frag, got " + killProbe.Count);
        if (killProbe.Source != attacker.Id || killProbe.Target != target.Id) Fail("Killed event did not preserve attacker/target ids");
        if (targetCollider != null && targetCollider.enabled) Fail("DeathHandler did not react to Killed by disabling target collider");
    }

    private void TestPlayerStackIfPresent()
    {
        OUTL_BasicPlayerController[] players = FindObjectsOfType<OUTL_BasicPlayerController>(true);
        for (int i = 0; i < players.Length; i++)
        {
            OUTL_BasicPlayerController player = players[i];
            if (player == null) continue;
            if (player.MotorProfile == null) Fail("Player " + player.name + " has no OUTL_PlayerMotorProfile");
            if (player.GetComponent<CharacterController>() == null) Fail("Player " + player.name + " has no CharacterController");
            if (player.GetComponentInChildren<OUTL_CharacterAnimationBridge>(true) == null) Fail("Player " + player.name + " has no animation bridge");
            OUTL_AttackDriver attack = player.GetComponent<OUTL_AttackDriver>();
            if (attack == null) Fail("Player " + player.name + " has no OUTL_AttackDriver");
            else if (attack.Primary == null && attack.Secondary == null && attack.Melee == null) Fail("Player " + player.name + " has no weapon attack profiles");
            if (player.EnableFallDamage && player.FallDamageFatalSpeed <= player.FallDamageMinSpeed) Fail("Player " + player.name + " fall damage thresholds are invalid");
        }
    }

    private OUTL_EntityRuntime EnsureCombatTestEntity(OUTL_World world, string name, Vector3 position)
    {
        if (world == null) return null;
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.position = position;
        }

        OUTL_EntityAdapter adapter = go.GetComponent<OUTL_EntityAdapter>();
        if (adapter == null) adapter = go.AddComponent<OUTL_EntityAdapter>();
        adapter.ClassNameOverride = "golden_combat_actor";
        adapter.TargetName = name.ToLowerInvariant();
        adapter.StableId = adapter.TargetName;
        adapter.SavePersistent = false;
        adapter.RegisterInSectors = true;

        OUTL_DamageReceiver receiver = go.GetComponent<OUTL_DamageReceiver>();
        if (receiver == null) receiver = go.AddComponent<OUTL_DamageReceiver>();
        receiver.Entity = adapter;

        OUTL_Hitbox hitbox = go.GetComponent<OUTL_Hitbox>();
        if (hitbox == null) hitbox = go.AddComponent<OUTL_Hitbox>();
        hitbox.Entity = adapter;
        hitbox.Zone = OUTL_HitboxZone.Generic;
        hitbox.DamageMultiplier = 1f;

        OUTL_Vitals vitals = go.GetComponent<OUTL_Vitals>();
        if (vitals == null) vitals = go.AddComponent<OUTL_Vitals>();
        vitals.Entity = adapter;
        vitals.InitializeMissingStats = true;
        vitals.DefaultHealth = 100f;
        vitals.DefaultMaxHealth = 100f;

        OUTL_DeathHandler death = go.GetComponent<OUTL_DeathHandler>();
        if (death == null) death = go.AddComponent<OUTL_DeathHandler>();
        death.Entity = adapter;
        death.QueueDespawn = false;
        death.DisableAI = false;
        death.DisableColliders = false;
        death.DisableRenderers = false;

        Collider[] colliders = go.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            if (colliders[i] != null) colliders[i].enabled = true;

        adapter.RebuildCommandReceiverCache();
        if (adapter.Runtime == null) adapter.RegisterNow(world);
        else adapter.RebindRuntime(world);
        return adapter.Runtime;
    }

    private OUTL_AttackProfile GetGoldenMeleeProfile()
    {
        if (goldenMelee != null) return goldenMelee;
        goldenMelee = ScriptableObject.CreateInstance<OUTL_AttackProfile>();
        goldenMelee.name = "OUTL_Golden_Melee_Profile";
        goldenMelee.AttackId = "golden.melee";
        goldenMelee.Mode = OUTL_AttackMode.Melee;
        goldenMelee.Damage = 25f;
        goldenMelee.Range = 1.85f;
        goldenMelee.Radius = 0.8f;
        goldenMelee.Cooldown = 0f;
        goldenMelee.HitMask = ~0;
        goldenMelee.HitDamageKey = "golden.melee";
        goldenMelee.MeleeArcDegrees = 160f;
        goldenMelee.MeleeMinRadius = 0.65f;
        goldenMelee.MeleeHeight = 1.5f;
        goldenMelee.MeleeForwardBias = 0.65f;
        return goldenMelee;
    }

    private OUTL_AttackProfile GetGoldenProjectileProfile(GameObject prefab)
    {
        if (goldenProjectile != null) return goldenProjectile;
        goldenProjectile = ScriptableObject.CreateInstance<OUTL_AttackProfile>();
        goldenProjectile.name = "OUTL_Golden_Projectile_Profile";
        goldenProjectile.AttackId = "golden.projectile";
        goldenProjectile.Mode = OUTL_AttackMode.Projectile;
        goldenProjectile.Damage = 5f;
        goldenProjectile.Range = 60f;
        goldenProjectile.Cooldown = 0f;
        goldenProjectile.ProjectilePrefab = prefab;
        goldenProjectile.ProjectileSpeed = 12f;
        goldenProjectile.ProjectileLifetime = 2f;
        goldenProjectile.ProjectileIgnoreTriggers = false;
        goldenProjectile.ProjectileDetonateOnEntityHit = false;
        goldenProjectile.ProjectileDetonateOnWorldHit = false;
        goldenProjectile.HitDamageKey = "golden.projectile";
        goldenProjectile.HitMask = ~0;
        return goldenProjectile;
    }

    private GameObject GetGoldenPoolPrefab()
    {
        if (goldenPoolPrefab != null) return goldenPoolPrefab;
        goldenPoolPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        goldenPoolPrefab.name = "OUTL_Golden_PoolPrefab";
        goldenPoolPrefab.transform.position = new Vector3(12f, -100f, 0f);
        Rigidbody body = goldenPoolPrefab.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = false;
        TrailRenderer trail = goldenPoolPrefab.AddComponent<TrailRenderer>();
        trail.time = 0.25f;
        trail.emitting = true;
        goldenPoolPrefab.AddComponent<OUTL_GoldenPoolProbe>();
        goldenPoolPrefab.SetActive(false);
        return goldenPoolPrefab;
    }

    private GameObject GetGoldenManagedPrefab()
    {
        if (goldenManagedPrefab != null) return goldenManagedPrefab;
        goldenManagedPrefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        goldenManagedPrefab.name = "OUTL_Golden_ManagedPrefab";
        goldenManagedPrefab.transform.position = new Vector3(16f, -100f, 0f);
        OUTL_EntityAdapter adapter = goldenManagedPrefab.AddComponent<OUTL_EntityAdapter>();
        adapter.ClassNameOverride = "golden_managed_actor";
        adapter.TargetName = "golden_managed_actor";
        adapter.StableId = string.Empty;
        adapter.SavePersistent = false;
        adapter.RegisterInSectors = true;
        EnsureDef(adapter, "golden_managed_actor", new[] { "Actor", "Damageable", "golden_managed_actor" });
        goldenManagedPrefab.SetActive(false);
        return goldenManagedPrefab;
    }

    private GameObject GetGoldenProjectilePrefab()
    {
        if (goldenProjectilePrefab != null) return goldenProjectilePrefab;
        goldenProjectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        goldenProjectilePrefab.name = "OUTL_Golden_ProjectilePrefab";
        goldenProjectilePrefab.transform.position = new Vector3(34f, -100f, 0f);
        Collider collider = goldenProjectilePrefab.GetComponent<Collider>();
        if (collider != null) collider.isTrigger = true;
        Rigidbody body = goldenProjectilePrefab.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
        body.detectCollisions = true;
        goldenProjectilePrefab.AddComponent<OUTL_Projectile>();
        goldenProjectilePrefab.SetActive(false);
        return goldenProjectilePrefab;
    }

    private OUTL_PoolSystem EnsurePoolSystem(OUTL_World world)
    {
        if (OUTL_PoolSystem.Instance != null) return OUTL_PoolSystem.Instance;
        if (world == null) return null;
        OUTL_PoolSystem pool = world.GetComponent<OUTL_PoolSystem>();
        if (pool == null) pool = world.gameObject.AddComponent<OUTL_PoolSystem>();
        return pool;
    }

    private OUTL_EntityDef EnsureDef(OUTL_EntityAdapter adapter, string className, string[] tags)
    {
        if (adapter == null) return null;
        OUTL_EntityDef def = adapter.Def;
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<OUTL_EntityDef>();
            def.name = "OUTL_Golden_Def_" + className;
            adapter.Def = def;
        }

        def.ClassName = className;
        def.DisplayName = className;
        def.Tags = tags;
        def.BaseStats = new[]
        {
            new OUTL_StatEntry { Key = "Health", Value = 100f },
            new OUTL_StatEntry { Key = "MaxHealth", Value = 100f },
            new OUTL_StatEntry { Key = "Damage", Value = 10f }
        };
        adapter.ClassNameOverride = className;
        adapter.MarkAddressDirty();
        if (OUTL_World.Instance != null && adapter.Runtime != null) adapter.RebindRuntime(OUTL_World.Instance);
        return def;
    }

    private static Transform EnsureChild(Transform parent, string name, Vector3 localPosition)
    {
        Transform existing = parent != null ? parent.Find(name) : null;
        if (existing != null) return existing;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;
        return go.transform;
    }

    private static Type FindRuntimeType(string typeName)
    {
        Type type = Type.GetType(typeName);
        if (type != null) return type;
        System.Reflection.Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            type = assemblies[i].GetType(typeName);
            if (type != null) return type;
        }
        return null;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        if (target == null || string.IsNullOrEmpty(fieldName)) return;
        System.Reflection.FieldInfo field = target.GetType().GetField(fieldName);
        if (field != null) field.SetValue(target, value);
    }

    private static object GetProperty(object target, string propertyName)
    {
        if (target == null || string.IsNullOrEmpty(propertyName)) return null;
        System.Reflection.PropertyInfo property = target.GetType().GetProperty(propertyName);
        return property != null ? property.GetValue(target, null) : null;
    }

    private static float GetFloatField(object target, string fieldName)
    {
        if (target == null || string.IsNullOrEmpty(fieldName)) return 0f;
        System.Reflection.FieldInfo field = target.GetType().GetField(fieldName);
        if (field == null) return 0f;
        object value = field.GetValue(target);
        return value is float ? (float)value : 0f;
    }

    private void Fail(string msg) { failures.Add(msg); }

    private void Report()
    {
        if (failures.Count == 0)
        {
            if (LogPassed) Debug.Log("[OUTL GoldenTest] PASS");
            return;
        }
        for (int i = 0; i < failures.Count; i++) Debug.LogError("[OUTL GoldenTest] FAIL: " + failures[i], this);
    }

    private sealed class CombatEventProbe : OUTL_IEventListener
    {
        public OUTL_EventType ExpectedType;
        public OUTL_EntityId Source;
        public OUTL_EntityId Target;
        public string Key;
        public int Count;
        private OUTL_EntityId expectedSource;
        private OUTL_EntityId expectedTarget;

        public void Reset(OUTL_EventType eventType, OUTL_EntityId source, OUTL_EntityId target)
        {
            ExpectedType = eventType;
            expectedSource = source;
            expectedTarget = target;
            Source = OUTL_EntityId.None;
            Target = OUTL_EntityId.None;
            Key = string.Empty;
            Count = 0;
        }

        public void OUTL_OnEvent(in OUTL_Event evt, OUTL_World world)
        {
            if (evt.Type != ExpectedType) return;
            if (evt.Source != expectedSource || evt.Target != expectedTarget) return;
            Source = evt.Source;
            Target = evt.Target;
            Key = evt.Key;
            Count++;
        }
    }
}

public sealed class OUTL_GoldenPoolProbe : MonoBehaviour, OUTL_IPoolReset
{
    public int SpawnCount;
    public int ReleaseCount;

    public void OUTL_OnPoolSpawn()
    {
        SpawnCount++;
    }

    public void OUTL_OnPoolRelease()
    {
        ReleaseCount++;
    }
}
